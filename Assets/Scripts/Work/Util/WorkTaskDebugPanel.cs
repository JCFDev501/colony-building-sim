using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Displays a runtime debug panel for work orders and pawn tasks.
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
        [SerializeField] private int m_maxRowsPerSection = 12;

        [Header("Panel Layout")]
        [SerializeField] private int m_windowId = 1005;
        [SerializeField] private float m_panelX = 950.0f;
        [SerializeField] private float m_panelY = 10.0f;
        [SerializeField] private float m_panelWidth = 620.0f;
        [SerializeField] private float m_maxPanelHeight = 520.0f;
        [SerializeField] private float m_lineHeight = 20.0f;
        [SerializeField] private float m_padding = 10.0f;

        private Rect m_panelRect;
        private Vector2 m_scrollPosition = Vector2.zero;

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

            m_panelRect = new Rect(
                m_panelX,
                m_panelY,
                m_panelWidth,
                CalculatePanelHeight());
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

            float calculatedHeight = CalculatePanelHeight();
            m_panelRect.height = Mathf.Min(calculatedHeight, m_maxPanelHeight);

            if (m_panelRect.Contains(Event.current.mousePosition))
            {
                DebugPanelInputBlocker.BlockPointerInput();
            }

            m_panelRect = GUI.Window(m_windowId, m_panelRect, DrawWorkTaskWindow, "Work / Task Debug");
        }

        /// <summary>
        /// Draws the draggable GUI window contents.
        /// </summary>
        private void DrawWorkTaskWindow(int windowId)
        {
            float contentHeight = CalculatePanelHeight();

            Rect viewRect = new Rect(
                0.0f,
                0.0f,
                m_panelRect.width - 25.0f,
                contentHeight);

            Rect scrollRect = new Rect(
                0.0f,
                22.0f,
                m_panelRect.width,
                m_panelRect.height - 22.0f);

            m_scrollPosition = GUI.BeginScrollView(scrollRect, m_scrollPosition, viewRect);

            float currentY = 5.0f;
            float contentX = m_padding;
            float contentWidth = viewRect.width - (m_padding * 2.0f);

            DrawSummarySection(contentX, contentWidth, ref currentY);
            currentY += 5.0f;

            if (m_showWorkOrders)
            {
                DrawWorkOrdersSection(contentX, contentWidth, ref currentY);
                currentY += 5.0f;
            }

            if (m_showTasks)
            {
                DrawTasksSection(contentX, contentWidth, ref currentY);
            }

            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 22.0f));
        }

        /// <summary>
        /// Draws high-level work/task counts.
        /// </summary>
        private void DrawSummarySection(float x, float width, ref float y)
        {
            List<WorkOrder> activeWorkOrders = m_workOrderManager.GetActiveWorkOrders();
            IReadOnlyList<PawnTask> tasks = m_taskManager.Tasks;

            DrawLine(x, ref y, width, "Summary");
            DrawLine(x + 10.0f, ref y, width, "Active Work Orders: " + activeWorkOrders.Count);
            DrawLine(x + 10.0f, ref y, width, "Stored Tasks: " + tasks.Count);
            DrawLine(x + 10.0f, ref y, width, "Active Tasks: " + CountActiveTasks(tasks));
            DrawLine(x + 10.0f, ref y, width, "Available Tasks: " + CountTasksByState(tasks, TaskState.Available));
            DrawLine(x + 10.0f, ref y, width, "Claimed Tasks: " + CountTasksByState(tasks, TaskState.Claimed));
            DrawLine(x + 10.0f, ref y, width, "In Progress Tasks: " + CountTasksByState(tasks, TaskState.InProgress));
        }

        /// <summary>
        /// Draws active work-order debug information.
        /// </summary>
        private void DrawWorkOrdersSection(float x, float width, ref float y)
        {
            List<WorkOrder> activeWorkOrders = m_workOrderManager.GetActiveWorkOrders();

            DrawLine(x, ref y, width, "Work Orders");

            if (activeWorkOrders.Count == 0)
            {
                DrawLine(x + 10.0f, ref y, width, "None");
                return;
            }

            int rowsToDraw = Mathf.Min(activeWorkOrders.Count, Mathf.Max(1, m_maxRowsPerSection));

            for (int i = 0; i < rowsToDraw; ++i)
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

            DrawLine(x, ref y, width, "Tasks");

            if (activeTaskCount == 0)
            {
                DrawLine(x + 10.0f, ref y, width, "None");
                return;
            }

            int rowsDrawn = 0;
            int maxRows = Mathf.Max(1, m_maxRowsPerSection);

            for (int i = 0; i < tasks.Count; ++i)
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
                + " | Type "
                + pWorkOrder.WorkType
                + " | State "
                + pWorkOrder.State
                + " | Target "
                + pWorkOrder.TargetCoordinates
                + BuildWorkOrderExtraInfo(pWorkOrder);
        }

        /// <summary>
        /// Builds one readable line for a pawn task.
        /// </summary>
        private string BuildTaskLine(PawnTask pTask)
        {
            return ShortenId(pTask.TaskId)
                + " | Type "
                + pTask.TaskType
                + " / "
                + pTask.ParentWorkType
                + " | State "
                + pTask.State
                + " | Target "
                + pTask.TargetCoordinates
                + " | Owner "
                + GetClaimedPawnName(pTask);
        }

        /// <summary>
        /// Adds type-specific work order debug information when available.
        /// </summary>
        private string BuildWorkOrderExtraInfo(WorkOrder pWorkOrder)
        {
            if (pWorkOrder == null)
            {
                return string.Empty;
            }

            if (pWorkOrder.HasBuildableType)
            {
                return " | Buildable " + pWorkOrder.BuildableType;
            }

            if (pWorkOrder.HasCropType)
            {
                return " | Crop " + pWorkOrder.CropType + " | Action " + pWorkOrder.PlantWorkAction;
            }

            return string.Empty;
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

            for (int i = 0; i < tasks.Count; ++i)
            {
                if (IsActiveTask(tasks[i]))
                {
                    ++activeTaskCount;
                }
            }

            return activeTaskCount;
        }

        /// <summary>
        /// Counts tasks currently in the requested task state.
        /// </summary>
        private int CountTasksByState(IReadOnlyList<PawnTask> tasks, TaskState taskState)
        {
            int matchingTaskCount = 0;

            for (int i = 0; i < tasks.Count; ++i)
            {
                PawnTask pTask = tasks[i];

                if (pTask == null)
                {
                    continue;
                }

                if (pTask.State == taskState)
                {
                    ++matchingTaskCount;
                }
            }

            return matchingTaskCount;
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

            totalHeight += m_lineHeight * 7.0f;
            totalHeight += 5.0f;

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