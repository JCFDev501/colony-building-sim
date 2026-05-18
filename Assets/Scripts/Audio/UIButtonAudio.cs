using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a shared button click sound when a UI Button is clicked.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioManager m_audioManager;

    private Button m_button;

    /// <summary>
    /// Finds references and registers the click sound event.
    /// </summary>
    private void Awake()
    {
        m_button = GetComponent<Button>();

        if (m_audioManager == null)
        {
            m_audioManager = FindFirstObjectByType<AudioManager>();
        }

        if (m_button != null)
        {
            m_button.onClick.AddListener(PlayClickSound);
        }
    }

    /// <summary>
    /// Removes the click sound event when destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (m_button != null)
        {
            m_button.onClick.RemoveListener(PlayClickSound);
        }
    }

    /// <summary>
    /// Plays the shared UI click sound.
    /// </summary>
    private void PlayClickSound()
    {
        if (m_audioManager == null)
        {
            return;
        }

        m_audioManager.PlayButtonClick();
    }
}