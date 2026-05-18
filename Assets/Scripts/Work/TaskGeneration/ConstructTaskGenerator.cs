using System.Collections.Generic;
using ColonyBuildingSim.Buildables;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Generates Construct tasks from ready Construct work orders.
    /// </summary>
    public class ConstructTaskGenerator
    {
        private readonly BuildableDefinitionLibrary m_buildableDefinitionLibrary;
        private readonly ConstructionResourceService m_constructionResourceService;

        public ConstructTaskGenerator(
            BuildableDefinitionLibrary pBuildableDefinitionLibrary,
            ConstructionResourceService pConstructionResourceService)
        {
            m_buildableDefinitionLibrary = pBuildableDefinitionLibrary;
            m_constructionResourceService = pConstructionResourceService;
        }

        /// <summary>
        /// Generates available Construct tasks from active Ready Construct work orders.
        /// Construct task duration comes from the linked BuildableDefinition.
        /// </summary>
        public void GenerateConstructTasksFromWorkOrders(
            WorkOrderManager pWorkOrderManager,
            GridManager pGridManager,
            System.Func<WorkOrder, TaskType, bool> hasDuplicateActiveTask,
            System.Func<PawnTask, bool> addTask)
        {
            if (pWorkOrderManager == null
                || pGridManager == null
                || m_buildableDefinitionLibrary == null
                || m_constructionResourceService == null
                || hasDuplicateActiveTask == null
                || addTask == null)
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

                if (!m_constructionResourceService.HasRequiredConstructionResources(buildableDefinition))
                {
                    continue;
                }

                if (hasDuplicateActiveTask(pWorkOrder, TaskType.Construct))
                {
                    continue;
                }

                addTask(new PawnTask(TaskType.Construct, pWorkOrder, buildableDefinition.WorkDuration));
            }
        }
    }
}