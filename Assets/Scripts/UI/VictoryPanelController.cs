using ColonyBuildingSim.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonyBuildingSim.Objectives
{
    /// <summary>
    /// Controls the post-game victory panel.
    /// The panel appears when the prototype objective is complete.
    /// </summary>
    public class VictoryPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ObjectiveManager m_objectiveManager;
        [SerializeField] private PersistentDataManager m_persistentDataManager;

        [Header("Panel")]
        [SerializeField] private GameObject m_victoryPanel;

        [Header("Buttons")]
        [SerializeField] private Button m_keepPlayingButton;
        [SerializeField] private Button m_returnToMenuButton;
        [SerializeField] private Button m_quitButton;

        [Header("Scene Names")]
        [SerializeField] private string m_mainMenuSceneName = "MainMenuScene";

        private bool m_hasShownVictoryPanel = false;

        /// <summary>
        /// Finds required references and registers button events.
        /// </summary>
        private void Start()
        {
            if (m_objectiveManager == null)
            {
                m_objectiveManager = FindFirstObjectByType<ObjectiveManager>();
            }

            if (m_persistentDataManager == null)
            {
                m_persistentDataManager = FindFirstObjectByType<PersistentDataManager>();
            }

            if (m_objectiveManager == null)
            {
                Debug.LogError("VictoryPanelController is missing an ObjectiveManager reference.", this);
            }

            if (m_persistentDataManager == null)
            {
                Debug.LogError("VictoryPanelController is missing a PersistentDataManager reference.", this);
            }

            if (m_victoryPanel != null)
            {
                m_victoryPanel.SetActive(false);
            }

            RegisterButtonEvents();
        }

        /// <summary>
        /// Shows the victory panel once when the objective is complete.
        /// </summary>
        private void Update()
        {
            if (m_hasShownVictoryPanel)
            {
                return;
            }

            if (m_objectiveManager == null || !m_objectiveManager.IsObjectiveComplete)
            {
                return;
            }

            ShowVictoryPanel();
        }

        /// <summary>
        /// Registers victory panel button events.
        /// </summary>
        private void RegisterButtonEvents()
        {
            if (m_keepPlayingButton != null)
            {
                m_keepPlayingButton.onClick.AddListener(HandleKeepPlayingClicked);
            }

            if (m_returnToMenuButton != null)
            {
                m_returnToMenuButton.onClick.AddListener(HandleReturnToMenuClicked);
            }

            if (m_quitButton != null)
            {
                m_quitButton.onClick.AddListener(HandleQuitClicked);
            }
        }

        /// <summary>
        /// Shows the victory panel, saves completion progress, and pauses gameplay time.
        /// </summary>
        private void ShowVictoryPanel()
        {
            m_hasShownVictoryPanel = true;

            if (m_persistentDataManager != null)
            {
                m_persistentDataManager.RecordPrototypeCompleted();
            }

            if (m_victoryPanel != null)
            {
                m_victoryPanel.SetActive(true);
            }

            Time.timeScale = 0.0f;
        }

        /// <summary>
        /// Hides the victory panel and lets the player continue playing the current world.
        /// </summary>
        private void HandleKeepPlayingClicked()
        {
            if (m_victoryPanel != null)
            {
                m_victoryPanel.SetActive(false);
            }

            Time.timeScale = 1.0f;
        }

        /// <summary>
        /// Returns to the main menu scene.
        /// </summary>
        private void HandleReturnToMenuClicked()
        {
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(m_mainMenuSceneName);
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        private void HandleQuitClicked()
        {
            Time.timeScale = 1.0f;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}