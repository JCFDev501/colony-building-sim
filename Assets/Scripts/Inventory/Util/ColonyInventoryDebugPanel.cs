using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Inventory
{
    /// <summary>
    /// Displays a simple runtime debug panel for colony inventory resources.
    /// This panel is read-only and does not modify inventory data.
    /// </summary>
    public class ColonyInventoryDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ColonyInventoryManager m_colonyInventoryManager;

        [Header("Debug Panel")]
        [SerializeField] private bool m_showInventoryPanel = true;

        [Header("Panel Layout")]
        [SerializeField] private float m_panelX = 950.0f;
        [SerializeField] private float m_panelY = 230.0f;
        [SerializeField] private float m_panelWidth = 260.0f;
        [SerializeField] private float m_lineHeight = 20.0f;
        [SerializeField] private float m_padding = 10.0f;

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

            float panelHeight = CalculatePanelHeight();
            GUI.Box(new Rect(m_panelX, m_panelY, m_panelWidth, panelHeight), "Colony Inventory");

            float currentY = m_panelY + 25.0f;
            float contentX = m_panelX + m_padding;
            float contentWidth = m_panelWidth - (m_padding * 2.0f);

            DrawInventoryLines(contentX, contentWidth, ref currentY);
        }

        /// <summary>
        /// Draws one line for every ResourceType so zero-count resources are still visible.
        /// </summary>
        private void DrawInventoryLines(float x, float width, ref float y)
        {
            Dictionary<ResourceType, int> resourceAmounts = m_colonyInventoryManager.GetAllResourceAmounts();

            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                int amount = 0;

                if (resourceAmounts.TryGetValue(resourceType, out int storedAmount))
                {
                    amount = storedAmount;
                }

                DrawLine(x, ref y, width, resourceType + ": " + amount);
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
        /// Calculates panel height based on the number of resource types.
        /// </summary>
        private float CalculatePanelHeight()
        {
            int resourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;
            return 35.0f + (resourceTypeCount * m_lineHeight);
        }
    }
}