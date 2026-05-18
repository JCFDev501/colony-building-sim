using System.Collections.Generic;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Generates resource-gathering tasks from ready work orders.
    /// This handles Cut and Mine task creation.
    /// </summary>
    public class ResourceGatherTaskGenerator
    {
        private readonly float m_defaultCutWorkDuration;
        private readonly float m_defaultMineWorkDuration;

        public ResourceGatherTaskGenerator(
            float defaultCutWorkDuration,
            float defaultMineWorkDuration)
        {
            m_defaultCutWorkDuration = defaultCutWorkDuration;
            m_defaultMineWorkDuration = defaultMineWorkDuration;
        }

        /// <summary>
        /// Generates available Cut tasks from active Ready Cut work orders.
        /// Invalid Cut work orders are ignored for now.
        /// </summary>
        public void GenerateCutTasksFromWorkOrders(
            WorkOrderManager pWorkOrderManager,
            GridManager pGridManager,
            System.Func<WorkOrder, TaskType, bool> hasDuplicateActiveTask,
            System.Func<PawnTask, bool> addTask)
        {
            if (pWorkOrderManager == null
                || pGridManager == null
                || hasDuplicateActiveTask == null
                || addTask == null)
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

                if (hasDuplicateActiveTask(pWorkOrder, TaskType.Cut))
                {
                    continue;
                }

                addTask(new PawnTask(TaskType.Cut, pWorkOrder, m_defaultCutWorkDuration));
            }
        }

        /// <summary>
        /// Generates available Mine tasks from active Ready Mine work orders.
        /// Invalid Mine work orders are ignored for now.
        /// </summary>
        public void GenerateMineTasksFromWorkOrders(
            WorkOrderManager pWorkOrderManager,
            GridManager pGridManager,
            System.Func<WorkOrder, TaskType, bool> hasDuplicateActiveTask,
            System.Func<PawnTask, bool> addTask)
        {
            if (pWorkOrderManager == null
                || pGridManager == null
                || hasDuplicateActiveTask == null
                || addTask == null)
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

                if (hasDuplicateActiveTask(pWorkOrder, TaskType.Mine))
                {
                    continue;
                }

                addTask(new PawnTask(TaskType.Mine, pWorkOrder, m_defaultMineWorkDuration));
            }
        }
    }
}