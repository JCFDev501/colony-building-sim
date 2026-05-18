using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Inventory
{
    /// <summary>
    /// Displays a runtime debug panel for colony inventory resources.
    /// This panel is read-only and does not modify inventory data.
    /// </summary>
    public class ColonyInventoryDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ColonyInventoryManager m_colonyInventoryManager;

        [Header("Debug Panel")]
        [SerializeField] private bool m_showInventoryPanel = true;

        [Header("Panel Layout")]
        [SerializeField] private int m_windowId = 1004;
        [SerializeField] private float m_panelX = 950.0f;
        [SerializeField] private float m_panelY = 230.0f;
        [SerializeField] private float m_panelWidth = 300.0f;
        [SerializeField] private float m_maxPanelHeight = 360.0f;
        [SerializeField] private float m_lineHeight = 20.0f;
        [SerializeField] private float m_padding = 10.0f;

        private Rect m_panelRect;
        private Vector2 m_scrollPosition = Vector2.zero;

        /// <summary>
        /// Finds required references if they were not assigned in the Inspector.
        /// </summary>
        private void Start()
        {
            if (m_colonyInventoryManager == null)
            {
                m_colonyInventoryManager = FindFirstObjectByType<ColonyInventoryManager>();
            }

            if (m_colonyInventoryManager == null)
            {
                Debug.LogError("ColonyInventoryDebugPanel is missing a ColonyInventoryManager reference.", this);
            }

            m_panelRect = new Rect(
                m_panelX,
                m_panelY,
                m_panelWidth,
                CalculatePanelHeight());
        }

        /// <summary>
        /// Draws the colony inventory debug panel.
        /// </summary>
        private void OnGUI()
        {
            if (!m_showInventoryPanel)
            {
                return;
            }

            if (m_colonyInventoryManager == null)
            {
                return;
            }

            float calculatedHeight = CalculatePanelHeight();
            m_panelRect.height = Mathf.Min(calculatedHeight, m_maxPanelHeight);

            if (m_panelRect.Contains(Event.current.mousePosition))
            {
                DebugPanelInputBlocker.BlockPointerInput();
            }

            m_panelRect = GUI.Window(m_windowId, m_panelRect, DrawInventoryWindow, "Colony Inventory");
        }

        /// <summary>
        /// Draws the draggable GUI window contents.
        /// </summary>
        private void DrawInventoryWindow(int windowId)
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

            DrawInventorySummary(contentX, contentWidth, ref currentY);
            currentY += 5.0f;

            DrawInventoryLines(contentX, contentWidth, ref currentY);

            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 22.0f));
        }

        /// <summary>
        /// Draws high-level inventory debug information.
        /// </summary>
        private void DrawInventorySummary(float x, float width, ref float y)
        {
            Dictionary<ResourceType, int> resourceAmounts = m_colonyInventoryManager.GetAllResourceAmounts();

            DrawLine(x, ref y, width, "Inventory Summary");
            DrawLine(x + 10.0f, ref y, width, "Tracked Resource Types: " + Enum.GetValues(typeof(ResourceType)).Length);
            DrawLine(x + 10.0f, ref y, width, "Stored Resource Entries: " + resourceAmounts.Count);
        }

        /// <summary>
        /// Draws one line for every ResourceType so zero-count resources are still visible.
        /// </summary>
        private void DrawInventoryLines(float x, float width, ref float y)
        {
            Dictionary<ResourceType, int> resourceAmounts = m_colonyInventoryManager.GetAllResourceAmounts();

            DrawLine(x, ref y, width, "Resources");

            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                int amount = 0;

                if (resourceAmounts.TryGetValue(resourceType, out int storedAmount))
                {
                    amount = storedAmount;
                }

                DrawLine(x + 10.0f, ref y, width, resourceType + ": " + amount);
            }
        }

        /// <summary>
        /// Draws one text line in the inventory debug panel.
        /// </summary>
        private void DrawLine(float x, ref float y, float width, string text)
        {
            GUI.Label(new Rect(x, y, width, m_lineHeight), text);
            y += m_lineHeight;
        }

        /// <summary>
        /// Calculates panel height based on the number of resource types and summary lines.
        /// </summary>
        private float CalculatePanelHeight()
        {
            int resourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;

            float totalHeight = 35.0f;

            totalHeight += m_lineHeight * 3.0f;
            totalHeight += 5.0f;

            totalHeight += m_lineHeight;
            totalHeight += resourceTypeCount * m_lineHeight;

            return totalHeight;
        }
    }
}