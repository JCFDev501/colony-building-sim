using System.Collections.Generic;
using ColonyBuildingSim.Crops;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Generates crop-related Plant tasks from ready Plant work orders.
    /// This handles both planting and harvesting task creation.
    /// </summary>
    public class CropTaskGenerator
    {
        private readonly CropDefinitionLibrary m_cropDefinitionLibrary;

        public CropTaskGenerator(CropDefinitionLibrary pCropDefinitionLibrary)
        {
            m_cropDefinitionLibrary = pCropDefinitionLibrary;
        }

        /// <summary>
        /// Generates available Plant tasks from active Ready Plant work orders.
        /// Plant task duration comes from the linked CropDefinition and PlantWorkAction.
        /// </summary>
        public void GeneratePlantTasksFromWorkOrders(
            WorkOrderManager pWorkOrderManager,
            GridManager pGridManager,
            System.Func<WorkOrder, TaskType, bool> hasDuplicateActiveTask,
            System.Func<PawnTask, bool> addTask)
        {
            if (pWorkOrderManager == null
                || pGridManager == null
                || m_cropDefinitionLibrary == null
                || hasDuplicateActiveTask == null
                || addTask == null)
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

                if (hasDuplicateActiveTask(pWorkOrder, TaskType.Plant))
                {
                    continue;
                }

                float workDuration = GetPlantTaskWorkDuration(pWorkOrder, cropDefinition);
                addTask(new PawnTask(TaskType.Plant, pWorkOrder, workDuration));
            }
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
    }
}