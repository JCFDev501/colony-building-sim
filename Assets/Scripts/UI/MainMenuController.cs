using System.Collections;
using ColonyBuildingSim.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu scene buttons.
/// This scene is pre-game UI and handles starting the gameplay scene or quitting.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PersistentDataManager m_persistentDataManager;

    [Header("Buttons")]
    [SerializeField] private Button m_startGameButton;
    [SerializeField] private Button m_quitButton;

    [Header("Panels")]
    [SerializeField] private GameObject m_mainMenuContent;
    [SerializeField] private GameObject m_loadingPanel;

    [Header("Status Text")]
    [SerializeField] private TMP_Text m_completionStatusText;

    [Header("Scene Names")]
    [SerializeField] private string m_gameplaySceneName = "ColonyGameplayScene";

    /// <summary>
    /// Registers main menu button events and refreshes saved progress display.
    /// </summary>
    private void Start()
    {
        Time.timeScale = 1.0f;

        if (m_persistentDataManager == null)
        {
            m_persistentDataManager = FindFirstObjectByType<PersistentDataManager>();
        }

        if (m_loadingPanel != null)
        {
            m_loadingPanel.SetActive(false);
        }

        if (m_mainMenuContent != null)
        {
            m_mainMenuContent.SetActive(true);
        }

        if (m_startGameButton != null)
        {
            m_startGameButton.onClick.AddListener(HandleStartGameClicked);
        }

        if (m_quitButton != null)
        {
            m_quitButton.onClick.AddListener(HandleQuitClicked);
        }

        ValidateReferences();
        RefreshCompletionStatusText();
    }

    /// <summary>
    /// Logs missing main menu references.
    /// </summary>
    private void ValidateReferences()
    {
        if (m_persistentDataManager == null)
        {
            Debug.LogError("MainMenuController is missing a PersistentDataManager reference.", this);
        }

        if (m_startGameButton == null)
        {
            Debug.LogError("MainMenuController is missing a Start Game button reference.", this);
        }

        if (m_quitButton == null)
        {
            Debug.LogError("MainMenuController is missing a Quit button reference.", this);
        }

        if (m_loadingPanel == null)
        {
            Debug.LogError("MainMenuController is missing a Loading Panel reference.", this);
        }

        if (m_mainMenuContent == null)
        {
            Debug.LogError("MainMenuController is missing a Main Menu Content reference.", this);
        }

        if (m_completionStatusText == null)
        {
            Debug.LogError("MainMenuController is missing a Completion Status Text reference.", this);
        }
    }

    /// <summary>
    /// Updates the main menu completion status from persistent data.
    /// </summary>
    private void RefreshCompletionStatusText()
    {
        if (m_completionStatusText == null)
        {
            return;
        }

        if (m_persistentDataManager == null || m_persistentDataManager.CurrentData == null)
        {
            m_completionStatusText.text = "Prototype Completion: Unavailable";
            return;
        }

        PersistentGameData data = m_persistentDataManager.CurrentData;

        if (!data.HasCompletedPrototype)
        {
            m_completionStatusText.text = "Prototype Completion: Not completed yet";
            return;
        }

        m_completionStatusText.text =
            "Prototype Completion: Completed\n"
            + "Completions: "
            + data.TimesCompletedPrototype;
    }

    /// <summary>
    /// Begins loading the gameplay scene.
    /// </summary>
    private void HandleStartGameClicked()
    {
        if (m_startGameButton != null)
        {
            m_startGameButton.interactable = false;
        }

        if (m_quitButton != null)
        {
            m_quitButton.interactable = false;
        }

        if (m_mainMenuContent != null)
        {
            m_mainMenuContent.SetActive(false);
        }

        if (m_loadingPanel != null)
        {
            m_loadingPanel.SetActive(true);
        }

        StartCoroutine(LoadGameplaySceneAsync());
    }

    /// <summary>
    /// Loads the gameplay scene asynchronously.
    /// </summary>
    private IEnumerator LoadGameplaySceneAsync()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(m_gameplaySceneName);

        if (loadOperation == null)
        {
            Debug.LogError("Failed to start loading scene: " + m_gameplaySceneName, this);
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}