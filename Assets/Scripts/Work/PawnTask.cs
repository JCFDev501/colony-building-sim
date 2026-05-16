using System;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Represents an actionable unit of work that one pawn can claim and perform.
    /// Pawn tasks are generated from work orders later, but this class only stores task data and claim state.
    /// </summary>
    [Serializable]
    public class PawnTask
    {
        [SerializeField] private string m_taskId = string.Empty;
        [SerializeField] private TaskType m_taskType = TaskType.Cut;
        [SerializeField] private WorkOrder m_pParentWorkOrder = null;
        [SerializeField] private WorkType m_parentWorkType = WorkType.Cut;
        [SerializeField] private Vector2Int m_targetCoordinates = Vector2Int.zero;
        [SerializeField] private TaskState m_state = TaskState.Available;
        [SerializeField] private Pawn m_pClaimedPawn = null;
        [SerializeField] private float m_workDuration = 1.0f;
        [SerializeField] private float m_workProgress = 0.0f;

        public string TaskId { get { return m_taskId; } }
        public TaskType TaskType { get { return m_taskType; } }
        public WorkOrder ParentWorkOrder { get { return m_pParentWorkOrder; } }
        public WorkType ParentWorkType { get { return m_parentWorkType; } }
        public Vector2Int TargetCoordinates { get { return m_targetCoordinates; } }
        public TaskState State { get { return m_state; } }
        public Pawn ClaimedPawn { get { return m_pClaimedPawn; } }
        public float WorkDuration { get { return m_workDuration; } }
        public float WorkProgress { get { return m_workProgress; } }

        /// <summary>
        /// Creates a pawn task linked to a parent work order.
        /// </summary>
        public PawnTask(TaskType taskType, WorkOrder parentWorkOrder, float workDuration)
        {
            m_taskId = Guid.NewGuid().ToString();
            m_taskType = taskType;
            m_pParentWorkOrder = parentWorkOrder;
            m_parentWorkType = parentWorkOrder.WorkType;
            m_targetCoordinates = parentWorkOrder.TargetCoordinates;
            m_state = TaskState.Available;
            m_pClaimedPawn = null;
            m_workDuration = Mathf.Max(0.0f, workDuration);
            m_workProgress = 0.0f;
        }

        /// <summary>
        /// Returns true when this task can currently be claimed by a pawn.
        /// </summary>
        public bool CanBeClaimed()
        {
            return m_state == TaskState.Available && m_pClaimedPawn == null;
        }

        /// <summary>
        /// Attempts to claim this task for a pawn.
        /// Only one pawn can claim a task at a time.
        /// </summary>
        public bool TryClaim(Pawn pPawn)
        {
            if (pPawn == null || !CanBeClaimed())
            {
                return false;
            }

            m_pClaimedPawn = pPawn;
            m_state = TaskState.Claimed;

            return true;
        }

        /// <summary>
        /// Releases the current pawn claim and makes the task available again.
        /// </summary>
        public void Release()
        {
            if (m_state == TaskState.Complete || m_state == TaskState.Cancelled)
            {
                return;
            }

            m_pClaimedPawn = null;
            m_state = TaskState.Available;
        }

        /// <summary>
        /// Marks this task as complete and clears the pawn claim.
        /// </summary>
        public void Complete()
        {
            m_workProgress = m_workDuration;
            m_state = TaskState.Complete;
            m_pClaimedPawn = null;
        }

        /// <summary>
        /// Cancels this task and clears the pawn claim.
        /// </summary>
        public void Cancel()
        {
            m_state = TaskState.Cancelled;
            m_pClaimedPawn = null;
        }
        
        /// <summary>
        /// Marks this task as actively being performed by its claimed pawn.
        /// </summary>
        public void MarkInProgress()
        {
            if (m_state == TaskState.Complete || m_state == TaskState.Cancelled)
            {
                return;
            }

            if (m_pClaimedPawn == null)
            {
                return;
            }

            m_state = TaskState.InProgress;
        }

        /// <summary>
        /// Adds work progress to this task and completes it when progress reaches the required duration.
        /// </summary>
        public void AddProgress(float progressAmount)
        {
            if (m_state != TaskState.InProgress)
            {
                return;
            }

            m_workProgress += Mathf.Max(0.0f, progressAmount);

            if (m_workProgress >= m_workDuration)
            {
                Complete();
            }
        }
    }
    
    
}