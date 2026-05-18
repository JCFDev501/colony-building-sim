using System.Collections.Generic;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Coordinates task generation across all task generators.
    /// This keeps TaskManager from knowing the exact generation order for every work type.
    /// </summary>
    public class TaskGenerationService
    {
        private readonly ResourceGatherTaskGenerator m_resourceGatherTaskGenerator;
        private readonly ConstructTaskGenerator m_constructTaskGenerator;
        private readonly CropTaskGenerator m_cropTaskGenerator;
        private readonly CookingTaskGenerator m_cookingTaskGenerator;

        public TaskGenerationService(
            ResourceGatherTaskGenerator pResourceGatherTaskGenerator,
            ConstructTaskGenerator pConstructTaskGenerator,
            CropTaskGenerator pCropTaskGenerator,
            CookingTaskGenerator pCookingTaskGenerator)
        {
            m_resourceGatherTaskGenerator = pResourceGatherTaskGenerator;
            m_constructTaskGenerator = pConstructTaskGenerator;
            m_cropTaskGenerator = pCropTaskGenerator;
            m_cookingTaskGenerator = pCookingTaskGenerator;
        }

        /// <summary>
        /// Generates all available tasks from active work orders and persistent workstations.
        /// </summary>
        public void GenerateTasks(
            WorkOrderManager pWorkOrderManager,
            GridManager pGridManager,
            List<PawnTask> pTasks,
            System.Func<WorkOrder, TaskType, bool> hasDuplicateActiveTask,
            System.Func<PawnTask, bool> addTask)
        {
            if (pWorkOrderManager == null || pGridManager == null)
            {
                return;
            }

            if (m_resourceGatherTaskGenerator != null)
            {
                m_resourceGatherTaskGenerator.GenerateCutTasksFromWorkOrders(
                    pWorkOrderManager,
                    pGridManager,
                    hasDuplicateActiveTask,
                    addTask);

                m_resourceGatherTaskGenerator.GenerateMineTasksFromWorkOrders(
                    pWorkOrderManager,
                    pGridManager,
                    hasDuplicateActiveTask,
                    addTask);
            }

            if (m_constructTaskGenerator != null)
            {
                m_constructTaskGenerator.GenerateConstructTasksFromWorkOrders(
                    pWorkOrderManager,
                    pGridManager,
                    hasDuplicateActiveTask,
                    addTask);
            }

            if (m_cropTaskGenerator != null)
            {
                m_cropTaskGenerator.GeneratePlantTasksFromWorkOrders(
                    pWorkOrderManager,
                    pGridManager,
                    hasDuplicateActiveTask,
                    addTask);
            }

            if (m_cookingTaskGenerator != null)
            {
                m_cookingTaskGenerator.GenerateCookTasksFromCampfires(
                    pTasks,
                    addTask);
            }
        }
    }
}