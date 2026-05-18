using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Stores persistent colony-level work orders during play.
    /// This manager does not generate pawn tasks or control pawn behavior.
    /// </summary>
    public class WorkOrderManager : MonoBehaviour
    {
        [SerializeField] private List<WorkOrder> m_workOrders = new List<WorkOrder>();

        public IReadOnlyList<WorkOrder> WorkOrders
        {
            get { return m_workOrders; }
        }

        /// <summary>
        /// Adds a new work order if another active order of the same type does not already exist on the target tile.
        /// </summary>
        public bool AddWorkOrder(WorkOrder pWorkOrder)
        {
            if (pWorkOrder == null || HasDuplicateActiveWorkOrder(pWorkOrder.WorkType, pWorkOrder.TargetCoordinates))
            {
                return false;
            }

            m_workOrders.Add(pWorkOrder);

            return true;
        }

        /// <summary>
        /// Cancels an active work order by ID.
        /// </summary>
        public bool CancelWorkOrder(string workOrderId)
        {
            if (string.IsNullOrWhiteSpace(workOrderId))
            {
                return false;
            }

            WorkOrder pWorkOrder = FindWorkOrderById(workOrderId);

            if (pWorkOrder == null || !IsActiveWorkOrder(pWorkOrder))
            {
                return false;
            }

            pWorkOrder.Cancel();

            return true;
        }

        /// <summary>
        /// Returns all work orders that are not complete or cancelled.
        /// </summary>
        public List<WorkOrder> GetActiveWorkOrders()
        {
            List<WorkOrder> activeWorkOrders = new List<WorkOrder>();

            foreach (WorkOrder pWorkOrder in m_workOrders)
            {
                if (IsActiveWorkOrder(pWorkOrder))
                {
                    activeWorkOrders.Add(pWorkOrder);
                }
            }

            return activeWorkOrders;
        }

        /// <summary>
        /// Returns all active work orders matching the requested work type.
        /// </summary>
        public List<WorkOrder> GetWorkOrdersByType(WorkType workType)
        {
            List<WorkOrder> matchingWorkOrders = new List<WorkOrder>();

            foreach (WorkOrder pWorkOrder in m_workOrders)
            {
                if (IsActiveWorkOrder(pWorkOrder) && pWorkOrder.WorkType == workType)
                {
                    matchingWorkOrders.Add(pWorkOrder);
                }
            }

            return matchingWorkOrders;
        }

        /// <summary>
        /// Returns the number of active work orders matching the requested work type.
        /// </summary>
        public int CountActiveWorkOrdersByType(WorkType workType)
        {
            int activeCount = 0;

            foreach (WorkOrder pWorkOrder in m_workOrders)
            {
                if (IsActiveWorkOrder(pWorkOrder) && pWorkOrder.WorkType == workType)
                {
                    ++activeCount;
                }
            }

            return activeCount;
        }

        /// <summary>
        /// Returns true when an active work order already exists for the same target tile and work type.
        /// </summary>
        public bool HasDuplicateActiveWorkOrder(WorkType workType, Vector2Int targetCoordinates)
        {
            foreach (WorkOrder pWorkOrder in m_workOrders)
            {
                if (IsActiveWorkOrder(pWorkOrder)
                    && pWorkOrder.WorkType == workType
                    && pWorkOrder.TargetCoordinates == targetCoordinates)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to find a stored work order by its unique ID.
        /// </summary>
        public bool TryGetWorkOrderById(string workOrderId, out WorkOrder pWorkOrder)
        {
            pWorkOrder = FindWorkOrderById(workOrderId);
            return pWorkOrder != null;
        }

        /// <summary>
        /// Returns true when the provided work order is still active.
        /// </summary>
        public bool IsWorkOrderActive(WorkOrder pWorkOrder)
        {
            return IsActiveWorkOrder(pWorkOrder);
        }

        /// <summary>
        /// Finds a stored work order by its unique ID.
        /// </summary>
        private WorkOrder FindWorkOrderById(string workOrderId)
        {
            foreach (WorkOrder pWorkOrder in m_workOrders)
            {
                if (pWorkOrder != null && pWorkOrder.WorkOrderId == workOrderId)
                {
                    return pWorkOrder;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true when a work order is still available for work-order queries.
        /// </summary>
        private bool IsActiveWorkOrder(WorkOrder pWorkOrder)
        {
            return pWorkOrder != null
                && pWorkOrder.State != WorkOrderState.Complete
                && pWorkOrder.State != WorkOrderState.Cancelled;
        }
    }
}