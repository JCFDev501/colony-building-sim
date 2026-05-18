using TMPro;
using UnityEngine;

/// <summary>
/// Controls the gameplay loading overlay while the gameplay scene finishes startup.
/// This prevents the player from seeing partial world generation before GameFlowManager is ready.
/// </summary>
public class GameplayLoadingScreenController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowManager m_gameFlowManager;

    [Header("UI")]
    [SerializeField] private GameObject m_loadingPanel;
    [SerializeField] private TMP_Text m_loadingText;

    [Header("Display Text")]
    [SerializeField] private string m_defaultLoadingText = "Generating Colony...";
    [SerializeField] private string m_generatingPawnsText = "Preparing Colonists...";
    [SerializeField] private string m_generatingWorldText = "Generating World...";
    [SerializeField] private string m_spawningPawnsText = "Spawning Colonists...";
    [SerializeField] private string m_failedText = "Startup Failed";

    /// <summary>
    /// Finds required references if they were not assigned in the Inspector.
    /// </summary>
    private void Start()
    {
        if (m_gameFlowManager == null)
        {
            m_gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        }

        if (m_loadingPanel != null)
        {
            m_loadingPanel.SetActive(true);
        }

        if (m_gameFlowManager == null)
        {
            Debug.LogError("GameplayLoadingScreenController is missing a GameFlowManager reference.", this);
        }

        RefreshLoadingScreen();
    }

    /// <summary>
    /// Keeps the loading overlay synced with the current game flow state.
    /// </summary>
    private void Update()
    {
        RefreshLoadingScreen();
    }

    /// <summary>
    /// Updates the loading panel visibility and message.
    /// </summary>
    private void RefreshLoadingScreen()
    {
        if (m_gameFlowManager == null)
        {
            SetLoadingText(m_defaultLoadingText);
            return;
        }

        if (m_gameFlowManager.CurrentState == GameFlowState.Ready)
        {
            HideLoadingPanel();
            return;
        }

        if (m_loadingPanel != null && !m_loadingPanel.activeSelf)
        {
            m_loadingPanel.SetActive(true);
        }

        SetLoadingText(GetLoadingTextForState(m_gameFlowManager.CurrentState));
    }

    /// <summary>
    /// Returns player-facing loading text for the current game flow state.
    /// </summary>
    private string GetLoadingTextForState(GameFlowState state)
    {
        switch (state)
        {
            case GameFlowState.GeneratingStarterPawns:
                return m_generatingPawnsText;

            case GameFlowState.WaitingForStarterPawnSelection:
                return m_generatingPawnsText;

            case GameFlowState.GeneratingWorld:
                return m_generatingWorldText;

            case GameFlowState.SpawningStarterPawns:
                return m_spawningPawnsText;

            case GameFlowState.Failed:
                return m_failedText;

            default:
                return m_defaultLoadingText;
        }
    }

    /// <summary>
    /// Hides the gameplay loading panel.
    /// </summary>
    private void HideLoadingPanel()
    {
        if (m_loadingPanel == null)
        {
            return;
        }

        if (!m_loadingPanel.activeSelf)
        {
            return;
        }

        m_loadingPanel.SetActive(false);
    }

    /// <summary>
    /// Safely sets loading text.
    /// </summary>
    private void SetLoadingText(string text)
    {
        if (m_loadingText == null)
        {
            return;
        }

        m_loadingText.text = text;
    }
}