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
        /// Generates pawn tasks from ready work orders.
        /// This keeps task creation owned by the task system instead of PawnBrain.
        /// </summary>
        private void GenerateTasksFromWorkOrders()
        {
            if (m_workOrderManager == null || m_gridManager == null)
            {
                return;
            }

            GenerateCutTasksFromWorkOrders(m_workOrderManager, m_gridManager);
            GenerateMineTasksFromWorkOrders(m_workOrderManager, m_gridManager);
            GenerateConstructTasksFromWorkOrders(m_workOrderManager, m_gridManager);
            GeneratePlantTasksFromWorkOrders(m_workOrderManager, m_gridManager);
            GenerateCookTasksFromCampfires();
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
        /// Generates available Cut tasks from active Ready Cut work orders.
        /// Invalid Cut work orders are ignored for now.
        /// </summary>
        public void GenerateCutTasksFromWorkOrders(WorkOrderManager pWorkOrderManager, GridManager pGridManager)
        {
            if (pWorkOrderManager == null || pGridManager == null)
            {
                return;
            }

            List<WorkOrder> cutWorkOrders = pWorkOrderManager.GetWorkOrdersByType(WorkType.Cut);

            foreach (WorkOrder pWorkOrder in cutWorkOrders)
            {
                if (pWorkOrder == null || pWorkOrder.State != WorkOrderState.Ready)
                {
                    continue;
                }

                if (!WorkTargetValidator.IsValidCutTarget(pGridManager, pWorkOrder.TargetCoordinates))
                {
                    continue;
                }

                if (HasDuplicateActiveTask(pWorkOrder, TaskType.Cut))
                {
                    continue;
                }

                AddTask(new PawnTask(TaskType.Cut, pWorkOrder, m_defaultCutWorkDuration));
            }
        }

        /// <summary>
        /// Generates available Mine tasks from active Ready Mine work orders.
        /// Invalid Mine work orders are ignored for now.
        /// </summary>
        public void GenerateMineTasksFromWorkOrders(WorkOrderManager pWorkOrderManager, GridManager pGridManager)
        {
            if (pWorkOrderManager == null || pGridManager == null)
            {
                return;
            }

            List<WorkOrder> mineWorkOrders = pWorkOrderManager.GetWorkOrdersByType(WorkType.Mine);

            foreach (WorkOrder pWorkOrder in mineWorkOrders)
            {
                if (pWorkOrder == null || pWorkOrder.State != WorkOrderState.Ready)
                {
                    continue;
                }

                if (!WorkTargetValidator.IsValidMineTarget(pGridManager, pWorkOrder.TargetCoordinates))
                {
                    continue;
                }

                if (HasDuplicateActiveTask(pWorkOrder, TaskType.Mine))
                {
                    continue;
                }

                AddTask(new PawnTask(TaskType.Mine, pWorkOrder, m_defaultMineWorkDuration));
            }
        }

        /// <summary>
        /// Generates available Construct tasks from active Ready Construct work orders.
        /// Construct task duration comes from the linked BuildableDefinition.
        /// </summary>
        public void GenerateConstructTasksFromWorkOrders(WorkOrderManager pWorkOrderManager, GridManager pGridManager)
        {
            if (pWorkOrderManager == null || pGridManager == null || m_buildableDefinitionLibrary == null)
            {
                return;
            }

            List<WorkOrder> constructWorkOrders = pWorkOrderManager.GetWorkOrdersByType(WorkType.Construct);

            foreach (WorkOrder pWorkOrder in constructWorkOrders)
            {
                if (pWorkOrder == null || pWorkOrder.State != WorkOrderState.Ready)
                {
                    continue;
                }

                if (!pWorkOrder.HasBuildableType)
                {
                    continue;
                }

                if (!WorkTargetValidator.IsValidConstructTarget(pGridManager, pWorkOrder.TargetCoordinates))
                {
                    continue;
                }

                if (!m_buildableDefinitionLibrary.TryGetDefinition(pWorkOrder.BuildableType, out BuildableDefinition buildableDefinition))
                {
                    continue;
                }

                if (!HasRequiredConstructionResources(buildableDefinition))
                {
                    continue;
                }

                if (HasDuplicateActiveTask(pWorkOrder, TaskType.Construct))
                {
                    continue;
                }

                AddTask(new PawnTask(TaskType.Construct, pWorkOrder, buildableDefinition.WorkDuration));
            }
        }
        
        /// <summary>
        /// Returns true when the colony has enough resources to build the requested structure.
        /// Requirements with a value of 0 are ignored.
        /// </summary>
        private bool HasRequiredConstructionResources(BuildableDefinition buildableDefinition)
        {
            if (buildableDefinition == null || m_colonyInventoryManager == null)
            {
                return false;
            }

            if (buildableDefinition.RequiredWood > 0
                && !m_colonyInventoryManager.HasResource(ResourceType.Wood, buildableDefinition.RequiredWood))
            {
                return false;
            }

            if (buildableDefinition.RequiredStone > 0
                && !m_colonyInventoryManager.HasResource(ResourceType.Stone, buildableDefinition.RequiredStone))
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Generates available Plant tasks from active Ready Plant work orders.
        /// Plant task duration comes from the linked CropDefinition and PlantWorkAction.
        /// </summary>
        public void GeneratePlantTasksFromWorkOrders(WorkOrderManager pWorkOrderManager, GridManager pGridManager)
        {
            if (pWorkOrderManager == null || pGridManager == null || m_cropDefinitionLibrary == null)
            {
                return;
            }

            List<WorkOrder> plantWorkOrders = pWorkOrderManager.GetWorkOrdersByType(WorkType.Plant);

            foreach (WorkOrder pWorkOrder in plantWorkOrders)
            {
                if (pWorkOrder == null || pWorkOrder.State != WorkOrderState.Ready)
                {
                    continue;
                }

                if (!pWorkOrder.HasCropType || !pWorkOrder.HasPlantWorkAction)
                {
                    continue;
                }

                if (!m_cropDefinitionLibrary.TryGetDefinition(pWorkOrder.CropType, out CropDefinition cropDefinition))
                {
                    continue;
                }

                if (HasDuplicateActiveTask(pWorkOrder, TaskType.Plant))
                {
                    continue;
                }

                float workDuration = GetPlantTaskWorkDuration(pWorkOrder, cropDefinition);
                AddTask(new PawnTask(TaskType.Plant, pWorkOrder, workDuration));
            }
        }
        
        /// <summary>
        /// Generates available Cook tasks from completed Campfire structures when enough Food exists.
        /// Campfires act as persistent workstations; Cook tasks are only created when requirements are met.
        /// </summary>
        public void GenerateCookTasksFromCampfires()
        {
            if (m_structureRegistry == null || m_colonyInventoryManager == null)
            {
                return;
            }

            if (!m_colonyInventoryManager.HasResource(ResourceType.Food, m_foodPerMeal))
            {
                return;
            }

            List<StructureInstance> campfires = m_structureRegistry.GetStructuresByType(BuildableType.Campfire);

            foreach (StructureInstance pCampfire in campfires)
            {
                if (pCampfire == null)
                {
                    continue;
                }

                if (HasActiveCookTaskAt(pCampfire.GridCoordinates))
                {
                    continue;
                }

                WorkOrder cookWorkOrder = new WorkOrder(
                    WorkType.Cook,
                    pCampfire.GridCoordinates);

                cookWorkOrder.MarkReady();

                PawnTask cookTask = new PawnTask(
                    TaskType.Cook,
                    cookWorkOrder,
                    m_cookWorkDuration);

                AddTask(cookTask);
            }
        }
        
        /// <summary>
        /// Returns true when an active Cook task already targets the requested coordinates.
        /// </summary>
        private bool HasActiveCookTaskAt(Vector2Int targetCoordinates)
        {
            foreach (PawnTask pTask in m_tasks)
            {
                if (!IsActiveTask(pTask))
                {
                    continue;
                }

                if (pTask.TaskType != TaskType.Cook)
                {
                    continue;
                }

                if (pTask.TargetCoordinates == targetCoordinates)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Returns the correct Plant task duration based on the PlantWorkAction.
        /// </summary>
        private float GetPlantTaskWorkDuration(WorkOrder pWorkOrder, CropDefinition cropDefinition)
        {
            if (pWorkOrder == null || cropDefinition == null)
            {
                return 3.0f;
            }

            switch (pWorkOrder.PlantWorkAction)
            {
                case PlantWorkAction.PlantCrop:
                    return cropDefinition.PlantWorkDuration;

                case PlantWorkAction.HarvestCrop:
                    return cropDefinition.HarvestWorkDuration;

                default:
                    return cropDefinition.PlantWorkDuration;
            }
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
        /// Completes a task and applies any task-specific world effects.
        /// </summary>
        public bool CompleteTask(PawnTask pTask)
        {
            if (pTask == null)
            {
                return false;
            }

            if (pTask.State == TaskState.Cancelled)
            {
                return false;
            }

            if (pTask.TaskType == TaskType.Cut)
            {
                return CompleteCutTask(pTask);
            }

            if (pTask.TaskType == TaskType.Mine)
            {
                return CompleteMineTask(pTask);
            }

            if (pTask.TaskType == TaskType.Construct)
            {
                return CompleteConstructTask(pTask);
            }

            if (pTask.TaskType == TaskType.Plant)
            {
                return CompletePlantTask(pTask);
            }

            if (pTask.TaskType == TaskType.Cook)
            {
                return CompleteCookTask(pTask);
            }

            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            return true;
        }

        /// <summary>
        /// Completes a Construct task by consuming resources, placing the built structure, and registering it.
        /// </summary>
        private bool CompleteConstructTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null || m_buildableDefinitionLibrary == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasBuildableType)
            {
                pTask.Cancel();
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!WorkTargetValidator.IsValidConstructTarget(m_gridManager, targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            if (!m_buildableDefinitionLibrary.TryGetDefinition(pWorkOrder.BuildableType, out BuildableDefinition buildableDefinition))
            {
                pTask.Cancel();
                return false;
            }

            if (buildableDefinition == null || buildableDefinition.Prefab == null)
            {
                pTask.Cancel();
                return false;
            }

            if (!TryConsumeConstructionResources(buildableDefinition))
            {
                pTask.Release();
                return false;
            }

            StructureInstance pStructureInstance = SpawnStructure(buildableDefinition, targetCoordinates);

            if (pStructureInstance == null)
            {
                pTask.Cancel();
                return false;
            }

            if (m_structureRegistry != null)
            {
                m_structureRegistry.RegisterStructure(pStructureInstance);
            }

            ApplyStructureTileState(buildableDefinition, targetCoordinates);

            pTask.Complete();

            if (pWorkOrder != null)
            {
                pWorkOrder.MarkComplete();
            }

            RefreshDoorRotationsNear(targetCoordinates);

            return true;
        }
        
        /// <summary>
        /// Completes Plant tasks by routing to the correct plant work action.
        /// </summary>
        private bool CompletePlantTask(PawnTask pTask)
        {
            if (pTask == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasCropType || !pWorkOrder.HasPlantWorkAction)
            {
                pTask.Cancel();
                return false;
            }

            switch (pWorkOrder.PlantWorkAction)
            {
                case PlantWorkAction.PlantCrop:
                    return CompletePlantCropTask(pTask);

                case PlantWorkAction.HarvestCrop:
                    return CompleteHarvestCropTask(pTask);

                default:
                    pTask.Cancel();
                    return false;
            }
        }
        
        /// <summary>
        /// Completes Cook work by consuming Food and adding Meal to colony inventory.
        /// </summary>
        private bool CompleteCookTask(PawnTask pTask)
        {
            if (pTask == null || m_colonyInventoryManager == null)
            {
                return false;
            }

            if (!m_colonyInventoryManager.TryRemoveResource(ResourceType.Food, m_foodPerMeal))
            {
                pTask.Cancel();
                return false;
            }

            m_colonyInventoryManager.AddResource(ResourceType.Meal, 1);

            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            return true;
        }
        
        /// <summary>
        /// Completes PlantCrop work by spawning and registering a new crop instance.
        /// </summary>
        private bool CompletePlantCropTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null || m_cropManager == null || m_cropDefinitionLibrary == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasCropType)
            {
                pTask.Cancel();
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!IsValidPlantCropCompletionTarget(targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            if (!m_cropDefinitionLibrary.TryGetDefinition(pWorkOrder.CropType, out CropDefinition cropDefinition))
            {
                pTask.Cancel();
                return false;
            }

            if (cropDefinition == null || cropDefinition.GrowingPrefab == null)
            {
                pTask.Cancel();
                return false;
            }

            Vector3 spawnPosition = m_gridManager.GetWorldPosition(targetCoordinates);
            spawnPosition += cropDefinition.PlacementOffset;

            GameObject pCropObject = new GameObject(cropDefinition.DisplayName + " Crop");
            pCropObject.transform.position = spawnPosition;
            pCropObject.transform.rotation = Quaternion.identity;

            CropInstance pCropInstance = pCropObject.GetComponent<CropInstance>();

            if (pCropInstance == null)
            {
                pCropInstance = pCropObject.AddComponent<CropInstance>();
            }

            pCropInstance.Initialize(cropDefinition, targetCoordinates);

            if (!m_cropManager.RegisterCrop(pCropInstance))
            {
                Destroy(pCropObject);
                pTask.Cancel();
                return false;
            }

            m_gridManager.SetContentType(targetCoordinates, TileContentType.Crop);
            m_gridManager.SetWalkable(targetCoordinates, !cropDefinition.BlocksMovement);
            m_gridManager.SetReserved(targetCoordinates, false);

            pTask.Complete();

            if (pWorkOrder != null)
            {
                pWorkOrder.MarkComplete();
            }

            return true;
        }
        
        /// <summary>
        /// Completes HarvestCrop work by adding Food to inventory and removing the mature crop.
        /// </summary>
        private bool CompleteHarvestCropTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null || m_cropManager == null || m_colonyInventoryManager == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasCropType)
            {
                pTask.Cancel();
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!m_cropManager.TryGetCropAt(targetCoordinates, out CropInstance pCropInstance))
            {
                pTask.Cancel();
                return false;
            }

            if (pCropInstance == null || !pCropInstance.IsMature)
            {
                pTask.Release();
                return false;
            }

            CropDefinition cropDefinition = pCropInstance.CropDefinition;

            if (cropDefinition == null)
            {
                pTask.Cancel();
                return false;
            }

            m_colonyInventoryManager.AddResource(ResourceType.Food, cropDefinition.FoodYield);

            pCropInstance.ResetAfterHarvest();

            m_gridManager.SetContentType(targetCoordinates, TileContentType.Crop);
            m_gridManager.SetWalkable(targetCoordinates, !cropDefinition.BlocksMovement);
            m_gridManager.SetReserved(targetCoordinates, false);

            pTask.Complete();

            if (pWorkOrder != null)
            {
                pWorkOrder.MarkComplete();
            }

            return true;
        }
        
        /// <summary>
        /// Returns whether a crop can still be planted at the requested target when the PlantCrop task completes.
        /// </summary>
        private bool IsValidPlantCropCompletionTarget(Vector2Int targetCoordinates)
        {
            if (m_gridManager == null || m_cropManager == null)
            {
                return false;
            }

            if (!m_gridManager.IsInBounds(targetCoordinates))
            {
                return false;
            }

            if (m_gridManager.GetTerrainType(targetCoordinates) == TileTerrainType.Water)
            {
                return false;
            }

            if (!m_gridManager.CanEnterTile(targetCoordinates))
            {
                return false;
            }

            if (m_gridManager.GetBlockType(targetCoordinates) != BlockType.None)
            {
                return false;
            }

            if (m_gridManager.GetWorldObjectType(targetCoordinates) != WorldObjectType.None)
            {
                return false;
            }

            if (m_gridManager.GetContentType(targetCoordinates) != TileContentType.Empty)
            {
                return false;
            }

            if (m_cropManager.TryGetCropAt(targetCoordinates, out CropInstance existingCrop)
                && existingCrop != null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Spawns a completed structure prefab, initializes its StructureInstance, and applies its current rotation.
        /// </summary>
        private StructureInstance SpawnStructure(BuildableDefinition buildableDefinition, Vector2Int targetCoordinates)
        {
            Vector3 spawnPosition = m_gridManager.GetWorldPosition(targetCoordinates);
            spawnPosition += buildableDefinition.PlacementOffset;

            Quaternion spawnRotation = GetStructureSpawnRotation(buildableDefinition, targetCoordinates);
            GameObject pStructureObject = Instantiate(buildableDefinition.Prefab, spawnPosition, spawnRotation);

            StructureInstance pStructureInstance = pStructureObject.GetComponent<StructureInstance>();

            if (pStructureInstance == null)
            {
                pStructureInstance = pStructureObject.AddComponent<StructureInstance>();
            }

            pStructureInstance.Initialize(buildableDefinition, targetCoordinates);

            return pStructureInstance;
        }

        /// <summary>
        /// Applies tile state for a completed structure.
        /// Exact structure identity is tracked by StructureRegistry.
        /// </summary>
        private void ApplyStructureTileState(BuildableDefinition buildableDefinition, Vector2Int targetCoordinates)
        {
            m_gridManager.SetContentType(targetCoordinates, TileContentType.Structure);
            m_gridManager.SetWalkable(targetCoordinates, !buildableDefinition.BlocksMovement);
            m_gridManager.SetReserved(targetCoordinates, false);
        }

        /// <summary>
        /// Returns the rotation that should be used when spawning a completed structure.
        /// Doors align with neighboring WoodenWall structures when possible.
        /// </summary>
        private Quaternion GetStructureSpawnRotation(BuildableDefinition buildableDefinition, Vector2Int targetCoordinates)
        {
            if (buildableDefinition == null)
            {
                return Quaternion.identity;
            }

            if (buildableDefinition.BuildableType != BuildableType.WoodenDoor)
            {
                return Quaternion.identity;
            }

            return GetWoodenDoorRotation(targetCoordinates);
        }

        /// <summary>
        /// Returns the best current rotation for a WoodenDoor based on adjacent WoodenWall structures.
        /// </summary>
        private Quaternion GetWoodenDoorRotation(Vector2Int doorCoordinates)
        {
            bool hasLeftWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.left, BuildableType.WoodenWall);
            bool hasRightWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.right, BuildableType.WoodenWall);
            bool hasUpWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.up, BuildableType.WoodenWall);
            bool hasDownWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.down, BuildableType.WoodenWall);

            bool hasHorizontalWallConnection = hasLeftWall || hasRightWall;
            bool hasVerticalWallConnection = hasUpWall || hasDownWall;

            if (hasHorizontalWallConnection && !hasVerticalWallConnection)
            {
                return Quaternion.Euler(0.0f, 90.0f, 0.0f);
            }

            if (hasVerticalWallConnection && !hasHorizontalWallConnection)
            {
                return Quaternion.identity;
            }

            if (hasLeftWall && hasRightWall)
            {
                return Quaternion.Euler(0.0f, 90.0f, 0.0f);
            }

            if (hasUpWall && hasDownWall)
            {
                return Quaternion.identity;
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// Refreshes nearby WoodenDoor rotations after a structure is completed.
        /// This handles cases where a door is built before the walls around it.
        /// </summary>
        private void RefreshDoorRotationsNear(Vector2Int completedStructureCoordinates)
        {
            RefreshDoorRotationAt(completedStructureCoordinates);

            foreach (Vector2Int neighborCoordinates in m_gridManager.GetOrthogonalNeighborCoordinates(completedStructureCoordinates))
            {
                RefreshDoorRotationAt(neighborCoordinates);
            }
        }

        /// <summary>
        /// Refreshes a WoodenDoor rotation at the requested tile if one is registered there.
        /// </summary>
        private void RefreshDoorRotationAt(Vector2Int coordinates)
        {
            if (m_structureRegistry == null)
            {
                return;
            }

            if (!m_structureRegistry.TryGetStructureAt(coordinates, out StructureInstance pStructureInstance))
            {
                return;
            }

            if (pStructureInstance == null || pStructureInstance.BuildableType != BuildableType.WoodenDoor)
            {
                return;
            }

            pStructureInstance.transform.rotation = GetWoodenDoorRotation(coordinates);
        }

        /// <summary>
        /// Returns true when the requested tile has a registered structure of the requested buildable type.
        /// </summary>
        private bool HasAdjacentStructureOfType(Vector2Int coordinates, BuildableType buildableType)
        {
            if (m_structureRegistry == null)
            {
                return false;
            }

            if (!m_structureRegistry.TryGetStructureAt(coordinates, out StructureInstance pStructureInstance))
            {
                return false;
            }

            return pStructureInstance != null && pStructureInstance.BuildableType == buildableType;
        }
        
        /// <summary>
        /// Consumes the resources required to build the requested structure.
        /// This checks all requirements before removing anything to avoid partial spending.
        /// </summary>
        private bool TryConsumeConstructionResources(BuildableDefinition buildableDefinition)
        {
            if (!HasRequiredConstructionResources(buildableDefinition))
            {
                return false;
            }

            if (buildableDefinition.RequiredWood > 0)
            {
                if (!m_colonyInventoryManager.TryRemoveResource(ResourceType.Wood, buildableDefinition.RequiredWood))
                {
                    return false;
                }
            }

            if (buildableDefinition.RequiredStone > 0)
            {
                if (!m_colonyInventoryManager.TryRemoveResource(ResourceType.Stone, buildableDefinition.RequiredStone))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Completes a Cut task by clearing the tree target from tile data and refreshing world-object visuals.
        /// </summary>
        private bool CompleteCutTask(PawnTask pTask)
        {
            if (m_gridManager == null)
            {
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!WorkTargetValidator.IsValidCutTarget(m_gridManager, targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            m_gridManager.SetWorldObjectType(targetCoordinates, WorldObjectType.None);
            m_gridManager.SetContentType(targetCoordinates, TileContentType.Empty);
            m_gridManager.SetWalkable(targetCoordinates, true);
            m_gridManager.SetReserved(targetCoordinates, false);

            if (m_colonyInventoryManager != null)
            {
                m_colonyInventoryManager.AddResource(ResourceType.Wood, m_woodPerTree);
            }

            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            if (m_worldObjectRenderer != null)
            {
                m_worldObjectRenderer.RemoveWorldObjectAt(targetCoordinates);
            }

            return true;
        }

        /// <summary>
        /// Completes a Mine task by clearing the stone block target from tile data,
        /// removing the block visual, and adding Stone to inventory.
        /// </summary>
        private bool CompleteMineTask(PawnTask pTask)
        {
            if (m_gridManager == null)
            {
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!WorkTargetValidator.IsValidMineTarget(m_gridManager, targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            m_gridManager.SetBlockType(targetCoordinates, BlockType.None);
            m_gridManager.SetContentType(targetCoordinates, TileContentType.Empty);
            m_gridManager.SetWalkable(targetCoordinates, true);
            m_gridManager.SetReserved(targetCoordinates, false);

            if (m_colonyInventoryManager != null)
            {
                m_colonyInventoryManager.AddResource(ResourceType.Stone, m_stonePerBlock);
            }

            if (m_blockRenderer != null)
            {
                m_blockRenderer.RemoveBlockAt(targetCoordinates);
            }

            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            return true;
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