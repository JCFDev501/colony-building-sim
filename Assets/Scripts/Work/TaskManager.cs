using System.Collections.Generic;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Inventory;
using ColonyBuildingSim.Crops;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Stores pawn tasks during play and provides task query helpers.
    /// This manager can generate simple task entries from ready work orders, but does not choose tasks for pawns.
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        [SerializeField] private List<PawnTask> m_tasks = new List<PawnTask>();
        [SerializeField] private float m_defaultCutWorkDuration = 3.0f;
        [SerializeField] private float m_defaultMineWorkDuration = 4.0f;
        [SerializeField] private int m_woodPerTree = 3;
        [SerializeField] private int m_stonePerBlock = 4;
        [SerializeField] private float m_cookWorkDuration = 4.0f;
        [SerializeField] private int m_foodPerMeal = 2;
        
        // Task Services
        private ResourceGatherTaskCompletionService m_resourceGatherTaskCompletionService;
        private ConstructionResourceService m_constructionResourceService;
        private StructurePlacementService m_structurePlacementService;
        private DoorRotationService m_doorRotationService;
        private ConstructTaskCompletionService m_constructTaskCompletionService;
        private CookingTaskCompletionService m_cookingTaskCompletionService;
        private CropTaskCompletionService m_cropTaskCompletionService;
        private TaskGenerationService m_taskGenerationService;
        private TaskCompletionService m_taskCompletionService;
        
        // Task Generation
        private CookingTaskGenerator m_cookingTaskGenerator;
        private CropTaskGenerator m_cropTaskGenerator;
        private ConstructTaskGenerator m_constructTaskGenerator;
        private ResourceGatherTaskGenerator m_resourceGatherTaskGenerator;

        [Header("References")]
        [SerializeField] private GridManager m_gridManager;
        [SerializeField] private WorkOrderManager m_workOrderManager;
        [SerializeField] private WorldObjectRenderer m_worldObjectRenderer;
        [SerializeField] private BlockRenderer m_blockRenderer;
        [SerializeField] private ColonyInventoryManager m_colonyInventoryManager;
        [SerializeField] private BuildableDefinitionLibrary m_buildableDefinitionLibrary;
        [SerializeField] private StructureRegistry m_structureRegistry;
        [SerializeField] private CropManager m_cropManager;
        [SerializeField] private CropDefinitionLibrary m_cropDefinitionLibrary;

        public IReadOnlyList<PawnTask> Tasks { get { return m_tasks; } }

        /// <summary>
        /// Finds optional scene references used to apply completed task effects.
        /// </summary>
        private void Awake()
        {
            if (m_gridManager == null)
            {
                m_gridManager = FindFirstObjectByType<GridManager>();
            }

            if (m_worldObjectRenderer == null)
            {
                m_worldObjectRenderer = FindFirstObjectByType<WorldObjectRenderer>();
            }

            if (m_workOrderManager == null)
            {
                m_workOrderManager = FindFirstObjectByType<WorkOrderManager>();
            }

            if (m_colonyInventoryManager == null)
            {
                m_colonyInventoryManager = FindFirstObjectByType<ColonyInventoryManager>();
            }

            if (m_buildableDefinitionLibrary == null)
            {
                m_buildableDefinitionLibrary = FindFirstObjectByType<BuildableDefinitionLibrary>();
            }

            if (m_structureRegistry == null)
            {
                m_structureRegistry = FindFirstObjectByType<StructureRegistry>();
            }
            
            if (m_cropManager == null)
            {
                m_cropManager = FindFirstObjectByType<CropManager>();
            }

            if (m_cropDefinitionLibrary == null)
            {
                m_cropDefinitionLibrary = FindFirstObjectByType<CropDefinitionLibrary>();
            }
            if (m_blockRenderer == null)
            {
                m_blockRenderer = FindFirstObjectByType<BlockRenderer>();
            }
            
            CreateTaskServices();
            CreateTaskGenerators();
        }

        /// <summary>
        /// Creates all task completion and support services used by the task system.
        /// </summary>
        private void CreateTaskServices()
        {
            m_resourceGatherTaskCompletionService = new ResourceGatherTaskCompletionService(
                m_gridManager,
                m_worldObjectRenderer,
                m_blockRenderer,
                m_colonyInventoryManager,
                m_woodPerTree,
                m_stonePerBlock);
    
            m_constructionResourceService = new ConstructionResourceService(
                m_colonyInventoryManager);
    
            m_structurePlacementService = new StructurePlacementService(
                m_gridManager,
                m_structureRegistry);
    
            m_doorRotationService = new DoorRotationService(
                m_gridManager,
                m_structureRegistry);
    
            m_constructTaskCompletionService = new ConstructTaskCompletionService(
                m_gridManager,
                m_buildableDefinitionLibrary,
                m_constructionResourceService,
                m_structurePlacementService,
                m_doorRotationService);
    
            m_cookingTaskCompletionService = new CookingTaskCompletionService(
                m_colonyInventoryManager,
                m_foodPerMeal);
    
            m_cropTaskCompletionService = new CropTaskCompletionService(
                m_gridManager,
                m_cropManager,
                m_cropDefinitionLibrary,
                m_colonyInventoryManager);

            m_taskCompletionService = new TaskCompletionService(
                m_resourceGatherTaskCompletionService,
                m_constructTaskCompletionService,
                m_cropTaskCompletionService,
                m_cookingTaskCompletionService);
        }

        /// <summary>
        /// Creates all task generators used to create available pawn tasks.
        /// </summary>
        private void CreateTaskGenerators()
        {
            m_cookingTaskGenerator = new CookingTaskGenerator(
                m_structureRegistry,
                m_colonyInventoryManager,
                m_foodPerMeal,
                m_cookWorkDuration);
    
            m_cropTaskGenerator = new CropTaskGenerator(
                m_cropDefinitionLibrary);
    
            m_constructTaskGenerator = new ConstructTaskGenerator(
                m_buildableDefinitionLibrary,
                m_constructionResourceService);
    
            m_resourceGatherTaskGenerator = new ResourceGatherTaskGenerator(
                m_defaultCutWorkDuration,
                m_defaultMineWorkDuration);

            m_taskGenerationService = new TaskGenerationService(
                m_resourceGatherTaskGenerator,
                m_constructTaskGenerator,
                m_cropTaskGenerator,
                m_cookingTaskGenerator);
        }

        /// <summary>
        /// Updates task generation from active work orders.
        /// This creates available tasks but does not assign them to pawns.
        /// </summary>
        private void Update()
        {
            GenerateTasksFromWorkOrders();
        }

        /// <summary>
        /// Generates pawn tasks from ready work orders and persistent workstations.
        /// </summary>
        private void GenerateTasksFromWorkOrders()
        {
            if (m_taskGenerationService == null)
            {
                return;
            }

            m_taskGenerationService.GenerateTasks(
                m_workOrderManager,
                m_gridManager,
                m_tasks,
                HasDuplicateActiveTask,
                AddTask);
        }

        /// <summary>
        /// Adds a task if another active task with the same parent work order and task type does not already exist.
        /// </summary>
        public bool AddTask(PawnTask pTask)
        {
            if (pTask == null || HasDuplicateActiveTask(pTask.ParentWorkOrder, pTask.TaskType))
            {
                return false;
            }

            m_tasks.Add(pTask);

            return true;
        }
        
        /// <summary>
        /// Returns all tasks that are available to be claimed.
        /// Completed, cancelled, claimed, and in-progress tasks are excluded.
        /// </summary>
        public List<PawnTask> GetAvailableTasks()
        {
            List<PawnTask> availableTasks = new List<PawnTask>();

            foreach (PawnTask pTask in m_tasks)
            {
                if (pTask != null && pTask.State == TaskState.Available)
                {
                    availableTasks.Add(pTask);
                }
            }

            return availableTasks;
        }

        /// <summary>
        /// Returns all available tasks that belong to the requested parent work type.
        /// </summary>
        public List<PawnTask> GetAvailableTasksByWorkType(WorkType workType)
        {
            List<PawnTask> matchingTasks = new List<PawnTask>();

            foreach (PawnTask pTask in m_tasks)
            {
                if (pTask != null
                    && pTask.State == TaskState.Available
                    && pTask.ParentWorkType == workType)
                {
                    matchingTasks.Add(pTask);
                }
            }

            return matchingTasks;
        }

        /// <summary>
        /// Removes completed and cancelled tasks from the stored task list.
        /// </summary>
        public void ClearCompletedAndCancelledTasks()
        {
            for (int taskIndex = m_tasks.Count - 1; taskIndex >= 0; --taskIndex)
            {
                PawnTask pTask = m_tasks[taskIndex];

                if (pTask == null
                    || pTask.State == TaskState.Complete
                    || pTask.State == TaskState.Cancelled)
                {
                    m_tasks.RemoveAt(taskIndex);
                }
            }
        }

        /// <summary>
        /// Completes a task through the task completion service.
        /// </summary>
        public bool CompleteTask(PawnTask pTask)
        {
            if (m_taskCompletionService == null)
            {
                return false;
            }

            return m_taskCompletionService.CompleteTask(pTask);
        }
        
        /// <summary>
        /// Returns true when an active task already exists for the same parent work order and task type.
        /// </summary>
        public bool HasDuplicateActiveTask(WorkOrder pParentWorkOrder, TaskType taskType)
        {
            if (pParentWorkOrder == null)
            {
                return false;
            }

            foreach (PawnTask pTask in m_tasks)
            {
                if (IsActiveTask(pTask)
                    && pTask.ParentWorkOrder == pParentWorkOrder
                    && pTask.TaskType == taskType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when a task is still active in the task system.
        /// </summary>
        private bool IsActiveTask(PawnTask pTask)
        {
            return pTask != null
                && pTask.State != TaskState.Complete
                && pTask.State != TaskState.Cancelled;
        }
    }
}