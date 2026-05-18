using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central runtime visibility manager for developer debug panels.
/// This keeps debug UI available during development while allowing it to be hidden for player-facing builds.
/// </summary>
public class DebugPanelManager : MonoBehaviour
{
    [Header("Debug Panel Groups")]
    [SerializeField] private List<GameObject> m_allDebugPanels = new List<GameObject>();

    [Header("Specific Panels")]
    [SerializeField] private GameObject m_pawnProfilePanel;
    [SerializeField] private GameObject m_tileDebugPanel;
    [SerializeField] private GameObject m_worldContextPanel;
    [SerializeField] private GameObject m_inventoryPanel;
    [SerializeField] private GameObject m_workTaskPanel;

    [Header("Startup Visibility")]
    [SerializeField] private bool m_showPanelsInEditorOnStart = true;
    [SerializeField] private bool m_showPanelsInPlayerOnStart = false;

    [Header("Runtime Toggle Input")]
    [SerializeField] private bool m_allowRuntimeToggle = true;
    [SerializeField] private Key m_toggleAllPanelsKey = Key.F1;
    [SerializeField] private Key m_togglePawnProfilePanelKey = Key.F2;
    [SerializeField] private Key m_toggleTileDebugPanelKey = Key.F3;
    [SerializeField] private Key m_toggleWorldContextPanelKey = Key.F4;
    [SerializeField] private Key m_toggleInventoryPanelKey = Key.F6;
    [SerializeField] private Key m_toggleWorkTaskPanelKey = Key.F7;

    private bool m_areAllPanelsVisible = false;

    /// <summary>
    /// Sets the initial debug panel visibility based on runtime environment.
    /// </summary>
    private void Start()
    {
#if UNITY_EDITOR
        SetAllPanelsVisible(m_showPanelsInEditorOnStart);
#else
        SetAllPanelsVisible(m_showPanelsInPlayerOnStart);
#endif
    }

    /// <summary>
    /// Handles runtime debug panel toggle input.
    /// </summary>
    private void Update()
    {
        if (!m_allowRuntimeToggle || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[m_toggleAllPanelsKey].wasPressedThisFrame)
        {
            SetAllPanelsVisible(!m_areAllPanelsVisible);
            return;
        }

        if (Keyboard.current[m_togglePawnProfilePanelKey].wasPressedThisFrame)
        {
            TogglePanel(m_pawnProfilePanel);
            return;
        }

        if (Keyboard.current[m_toggleTileDebugPanelKey].wasPressedThisFrame)
        {
            TogglePanel(m_tileDebugPanel);
            return;
        }

        if (Keyboard.current[m_toggleWorldContextPanelKey].wasPressedThisFrame)
        {
            TogglePanel(m_worldContextPanel);
            return;
        }

        if (Keyboard.current[m_toggleInventoryPanelKey].wasPressedThisFrame)
        {
            TogglePanel(m_inventoryPanel);
            return;
        }

        if (Keyboard.current[m_toggleWorkTaskPanelKey].wasPressedThisFrame)
        {
            TogglePanel(m_workTaskPanel);
        }
    }

    /// <summary>
    /// Sets every registered debug panel active or inactive.
    /// </summary>
    public void SetAllPanelsVisible(bool isVisible)
    {
        m_areAllPanelsVisible = isVisible;

        for (int i = 0; i < m_allDebugPanels.Count; ++i)
        {
            if (m_allDebugPanels[i] == null)
            {
                continue;
            }

            m_allDebugPanels[i].SetActive(isVisible);
        }
    }

    /// <summary>
    /// Toggles one debug panel if assigned.
    /// </summary>
    private void TogglePanel(GameObject panelObject)
    {
        if (panelObject == null)
        {
            return;
        }

        panelObject.SetActive(!panelObject.activeSelf);
        UpdateAllPanelsVisibleState();
    }

    /// <summary>
    /// Refreshes the all-panels state after a single panel has been toggled.
    /// </summary>
    private void UpdateAllPanelsVisibleState()
    {
        if (m_allDebugPanels.Count == 0)
        {
            m_areAllPanelsVisible = false;
            return;
        }

        for (int i = 0; i < m_allDebugPanels.Count; ++i)
        {
            if (m_allDebugPanels[i] == null)
            {
                continue;
            }

            if (!m_allDebugPanels[i].activeSelf)
            {
                m_areAllPanelsVisible = false;
                return;
            }
        }

        m_areAllPanelsVisible = true;
    }
}