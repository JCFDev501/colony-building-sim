using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the in-game pause menu.
/// Escape toggles a full system pause overlay, while buttons resume, return to menu, or quit.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject m_pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button m_resumeButton;
    [SerializeField] private Button m_returnToMenuButton;
    [SerializeField] private Button m_quitButton;

    [Header("Scene Names")]
    [SerializeField] private string m_mainMenuSceneName = "MainMenuScene";

    private bool m_isPaused = false;

    public static bool IsSystemPaused { get; private set; }

    /// <summary>
    /// Initializes the pause menu state and registers button events.
    /// </summary>
    private void Start()
    {
        SetPaused(false);
        RegisterButtonEvents();
        ValidateReferences();
    }

    /// <summary>
    /// Resets pause state when this object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        IsSystemPaused = false;
        Time.timeScale = 1.0f;
    }

    /// <summary>
    /// Watches for Escape input and toggles the pause menu.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        TogglePause();
    }

    /// <summary>
    /// Registers pause menu button events.
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (m_resumeButton != null)
        {
            m_resumeButton.onClick.AddListener(HandleResumeClicked);
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
    /// Logs missing pause menu references.
    /// </summary>
    private void ValidateReferences()
    {
        if (m_pausePanel == null)
        {
            Debug.LogError("PauseMenuController is missing a PausePanel reference.", this);
        }

        if (m_resumeButton == null)
        {
            Debug.LogError("PauseMenuController is missing a ResumeButton reference.", this);
        }

        if (m_returnToMenuButton == null)
        {
            Debug.LogError("PauseMenuController is missing a ReturnToMenuButton reference.", this);
        }

        if (m_quitButton == null)
        {
            Debug.LogError("PauseMenuController is missing a QuitButton reference.", this);
        }
    }

    /// <summary>
    /// Toggles the pause menu open or closed.
    /// </summary>
    private void TogglePause()
    {
        SetPaused(!m_isPaused);
    }

    /// <summary>
    /// Applies pause state to the panel and simulation time.
    /// </summary>
    private void SetPaused(bool isPaused)
    {
        m_isPaused = isPaused;
        IsSystemPaused = isPaused;

        if (m_pausePanel != null)
        {
            m_pausePanel.SetActive(m_isPaused);
        }

        Time.timeScale = m_isPaused ? 0.0f : 1.0f;
    }

    /// <summary>
    /// Resumes gameplay.
    /// </summary>
    private void HandleResumeClicked()
    {
        SetPaused(false);
    }

    /// <summary>
    /// Returns to the main menu scene.
    /// </summary>
    private void HandleReturnToMenuClicked()
    {
        SetPaused(false);
        SceneManager.LoadScene(m_mainMenuSceneName);
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    private void HandleQuitClicked()
    {
        SetPaused(false);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}