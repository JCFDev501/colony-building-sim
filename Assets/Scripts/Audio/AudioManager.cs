using UnityEngine;

/// <summary>
/// Provides simple shared audio playback for music and UI sound effects.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource m_musicSource;
    [SerializeField] private AudioSource m_soundEffectSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip m_backgroundMusicClip;
    [SerializeField] private AudioClip m_buttonClickClip;

    [Header("Volume")]
    [SerializeField] private float m_musicVolume = 0.25f;
    [SerializeField] private float m_soundEffectVolume = 0.7f;

    /// <summary>
    /// Initializes audio sources and starts background music if assigned.
    /// </summary>
    private void Start()
    {
        ConfigureAudioSources();
        PlayBackgroundMusic();
    }

    /// <summary>
    /// Plays the shared UI button click sound.
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySoundEffect(m_buttonClickClip);
    }

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    public void PlaySoundEffect(AudioClip audioClip)
    {
        if (m_soundEffectSource == null || audioClip == null)
        {
            return;
        }

        m_soundEffectSource.PlayOneShot(audioClip, m_soundEffectVolume);
    }

    /// <summary>
    /// Starts looping background music.
    /// </summary>
    private void PlayBackgroundMusic()
    {
        if (m_musicSource == null || m_backgroundMusicClip == null)
        {
            return;
        }

        m_musicSource.clip = m_backgroundMusicClip;
        m_musicSource.volume = m_musicVolume;
        m_musicSource.loop = true;
        m_musicSource.Play();
    }

    /// <summary>
    /// Applies basic settings to the assigned audio sources.
    /// </summary>
    private void ConfigureAudioSources()
    {
        if (m_musicSource != null)
        {
            m_musicSource.playOnAwake = false;
            m_musicSource.loop = true;
            m_musicSource.volume = m_musicVolume;
        }

        if (m_soundEffectSource != null)
        {
            m_soundEffectSource.playOnAwake = false;
            m_soundEffectSource.loop = false;
            m_soundEffectSource.volume = m_soundEffectVolume;
        }
    }
}