using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Displays a simple runtime debug panel for work orders and pawn tasks.
    /// This is only for playtesting visibility and does not modify work/task data.
    /// </summary>
    public class WorkTaskDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorkOrderManager m_workOrderManager;
        [SerializeField] private TaskManager m_taskManager;

        [Header("Debug Panel")]
        [SerializeField] private bool m_showWorkTaskPanel = true;
        [SerializeField] private bool m_showWorkOrders = true;
        [SerializeField] private bool m_showTasks = true;
        [SerializeField] private int m_maxRowsPerSection = 8;

        [Header("Panel Layout")]
        [SerializeField] private float m_panelX = 950.0f;
        [SerializeField] private float m_panelY = 10.0f;
        [SerializeField] private float m_panelWidth = 520.0f;
        [SerializeField] private float m_lineHeight = 20.0f;
        [SerializeField] private float m_padding = 10.0f;

        /// <summary>
        /// Finds required references if they were not assigned in the Inspector.
        /// </summary>
        private void Start()
        {
            if (m_workOrderManager == null)
            {
                m_workOrderManager = FindFirstObjectByType<WorkOrderManager>();
            }

            if (m_taskManager == null)
            {
                m_taskManager = FindFirstObjectByType<TaskManager>();
            }

            if (m_workOrderManager == null)
            {
                Debug.LogError("WorkTaskDebugPanel is missing a WorkOrderManager reference.", this);
            }

            if (m_taskManager == null)
            {
                Debug.LogError("WorkTaskDebugPanel is missing a TaskManager reference.", this);
            }
        }

        /// <summary>
        /// Draws the work/task debug panel.
        /// </summary>
        private void OnGUI()
        {
            if (!m_showWorkTaskPanel)
            {
                return;
            }

            if (m_workOrderManager == null || m_taskManager == null)
            {
                return;
            }

            float panelHeight = CalculatePanelHeight();
            GUI.Box(new Rect(m_panelX, m_panelY, m_panelWidth, panelHeight), "Work / Task Debug");

            float currentY = m_panelY + 25.0f;
            float contentX = m_panelX + m_padding;
            float contentWidth = m_panelWidth - (m_padding * 2.0f);

            if (m_showWorkOrders)
            {
                DrawWorkOrdersSection(contentX, contentWidth, ref currentY);
                currentY += 5.0f;
            }

            if (m_showTasks)
            {
                DrawTasksSection(contentX, contentWidth, ref currentY);
            }
        }

        /// <summary>
        /// Draws active work-order debug information.
        /// </summary>
        private void DrawWorkOrdersSection(float x, float width, ref float y)
        {
            List<WorkOrder> activeWorkOrders = m_workOrderManager.GetActiveWorkOrders();

            DrawLine(x, ref y, width, "Active Work Orders: " + activeWorkOrders.Count);

            if (activeWorkOrders.Count == 0)
            {
                DrawLine(x + 10.0f, ref y, width, "None");
                return;
            }

            int rowsToDraw = Mathf.Min(activeWorkOrders.Count, Mathf.Max(1, m_maxRowsPerSection));

            for (int i = 0; i < rowsToDraw; i++)
            {
                WorkOrder pWorkOrder = activeWorkOrders[i];

                if (pWorkOrder == null)
                {
                    continue;
                }

                DrawLine(
                    x + 10.0f,
                    ref y,
                    width,
                    BuildWorkOrderLine(pWorkOrder));
            }

            if (activeWorkOrders.Count > rowsToDraw)
            {
                DrawLine(x + 10.0f, ref y, width, "... +" + (activeWorkOrders.Count - rowsToDraw) + " more");
            }
        }

        /// <summary>
        /// Draws active task debug information.
        /// </summary>
        private void DrawTasksSection(float x, float width, ref float y)
        {
            IReadOnlyList<PawnTask> tasks = m_taskManager.Tasks;
            int activeTaskCount = CountActiveTasks(tasks);

            DrawLine(x, ref y, width, "Active Tasks: " + activeTaskCount);

            if (activeTaskCount == 0)
            {
                DrawLine(x + 10.0f, ref y, width, "None");
                return;
            }

            int rowsDrawn = 0;
            int maxRows = Mathf.Max(1, m_maxRowsPerSection);

            for (int i = 0; i < tasks.Count; i++)
            {
                PawnTask pTask = tasks[i];

                if (!IsActiveTask(pTask))
                {
                    continue;
                }

                DrawLine(
                    x + 10.0f,
                    ref y,
                    width,
                    BuildTaskLine(pTask));

                ++rowsDrawn;

                if (rowsDrawn >= maxRows)
                {
                    break;
                }
            }

            if (activeTaskCount > rowsDrawn)
            {
                DrawLine(x + 10.0f, ref y, width, "... +" + (activeTaskCount - rowsDrawn) + " more");
            }
        }

        /// <summary>
        /// Builds one readable line for a work order.
        /// </summary>
        private string BuildWorkOrderLine(WorkOrder pWorkOrder)
        {
            return ShortenId(pWorkOrder.WorkOrderId)
                + " | "
                + pWorkOrder.WorkType
                + " | "
                + pWorkOrder.State
                + " | Target "
                + pWorkOrder.TargetCoordinates;
        }

        /// <summary>
        /// Builds one readable line for a pawn task.
        /// </summary>
        private string BuildTaskLine(PawnTask pTask)
        {
            return ShortenId(pTask.TaskId)
                + " | "
                + pTask.TaskType
                + " / "
                + pTask.ParentWorkType
                + " | "
                + pTask.State
                + " | Target "
                + pTask.TargetCoordinates
                + " | Owner "
                + GetClaimedPawnName(pTask);
        }

        /// <summary>
        /// Returns the display name or ID of the pawn claiming a task.
        /// </summary>
        private string GetClaimedPawnName(PawnTask pTask)
        {
            if (pTask == null || pTask.ClaimedPawn == null)
            {
                return "None";
            }

            Pawn claimedPawn = pTask.ClaimedPawn;

            if (claimedPawn.Profile != null && !string.IsNullOrWhiteSpace(claimedPawn.Profile.DisplayName))
            {
                return claimedPawn.Profile.DisplayName;
            }

            return claimedPawn.PawnId;
        }

        /// <summary>
        /// Counts tasks that are still relevant for active debug display.
        /// </summary>
        private int CountActiveTasks(IReadOnlyList<PawnTask> tasks)
        {
            int activeTaskCount = 0;

            for (int i = 0; i < tasks.Count; i++)
            {
                if (IsActiveTask(tasks[i]))
                {
                    ++activeTaskCount;
                }
            }

            return activeTaskCount;
        }

        /// <summary>
        /// Returns true when a task should be shown in the active task list.
        /// </summary>
        private bool IsActiveTask(PawnTask pTask)
        {
            return pTask != null
                && pTask.State != TaskState.Complete
                && pTask.State != TaskState.Cancelled;
        }

        /// <summary>
        /// Shortens long GUID-style IDs so the panel remains readable.
        /// </summary>
        private string ShortenId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "None";
            }

            if (id.Length <= 8)
            {
                return id;
            }

            return id.Substring(0, 8);
        }

        /// <summary>
        /// Draws one text line in the debug panel.
        /// </summary>
        private void DrawLine(float x, ref float y, float width, string text)
        {
            GUI.Label(new Rect(x, y, width, m_lineHeight), text);
            y += m_lineHeight;
        }

        /// <summary>
        /// Calculates panel height based on visible sections and row limits.
        /// </summary>
        private float CalculatePanelHeight()
        {
            float totalHeight = 35.0f;

            if (m_showWorkOrders)
            {
                int workOrderRows = 2;

                if (m_workOrderManager != null)
                {
                    int activeWorkOrderCount = m_workOrderManager.GetActiveWorkOrders().Count;
                    workOrderRows = 1 + Mathf.Min(activeWorkOrderCount, Mathf.Max(1, m_maxRowsPerSection));

                    if (activeWorkOrderCount == 0)
                    {
                        workOrderRows = 2;
                    }
                    else if (activeWorkOrderCount > m_maxRowsPerSection)
                    {
                        ++workOrderRows;
                    }
                }

                totalHeight += workOrderRows * m_lineHeight;
                totalHeight += 5.0f;
            }

            if (m_showTasks)
            {
                int taskRows = 2;

                if (m_taskManager != null)
                {
                    int activeTaskCount = CountActiveTasks(m_taskManager.Tasks);
                    taskRows = 1 + Mathf.Min(activeTaskCount, Mathf.Max(1, m_maxRowsPerSection));

                    if (activeTaskCount == 0)
                    {
                        taskRows = 2;
                    }
                    else if (activeTaskCount > m_maxRowsPerSection)
                    {
                        ++taskRows;
                    }
                }

                totalHeight += taskRows * m_lineHeight;
            }

            return totalHeight;
        }
    }
}