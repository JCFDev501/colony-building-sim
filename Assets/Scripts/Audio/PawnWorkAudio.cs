using ColonyBuildingSim.Work;
using UnityEngine;

/// <summary>
/// Plays work-related sound effects from a pawn while work is being performed.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PawnWorkAudio : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource m_audioSource;

    [Header("Work Sound Effects")]
    [SerializeField] private AudioClip m_cutSoundClip;
    [SerializeField] private AudioClip m_mineSoundClip;
    [SerializeField] private AudioClip m_constructSoundClip;
    [SerializeField] private AudioClip m_plantSoundClip;
    [SerializeField] private AudioClip m_cookSoundClip;

    [Header("Timing")]
    [SerializeField] private float m_defaultRepeatInterval = 0.75f;
    [SerializeField] private float m_cutRepeatInterval = 0.7f;
    [SerializeField] private float m_mineRepeatInterval = 0.85f;
    [SerializeField] private float m_constructRepeatInterval = 0.8f;
    [SerializeField] private float m_plantRepeatInterval = 0.9f;
    [SerializeField] private float m_cookRepeatInterval = 1.0f;

    [Header("Volume")]
    [SerializeField] private float m_workSoundVolume = 0.75f;

    private bool m_isPlayingWorkAudio = false;
    private WorkType m_currentWorkType = WorkType.Cut;
    private float m_soundTimer = 0.0f;

    /// <summary>
    /// Finds the AudioSource if it was not assigned in the Inspector.
    /// </summary>
    private void Awake()
    {
        if (m_audioSource == null)
        {
            m_audioSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    /// <summary>
    /// Updates repeated work sound playback while work is active.
    /// </summary>
    private void Update()
    {
        if (!m_isPlayingWorkAudio)
        {
            return;
        }

        m_soundTimer -= Time.deltaTime;

        if (m_soundTimer > 0.0f)
        {
            return;
        }

        PlayWorkSoundOnce(m_currentWorkType);
        m_soundTimer = GetRepeatInterval(m_currentWorkType);
    }

    /// <summary>
    /// Starts playing repeated work audio for the requested work type.
    /// </summary>
    public void StartWorkAudio(WorkType workType)
    {
        if (m_isPlayingWorkAudio && m_currentWorkType == workType)
        {
            return;
        }

        m_currentWorkType = workType;
        m_isPlayingWorkAudio = true;
        m_soundTimer = 0.0f;
    }

    /// <summary>
    /// Stops repeated work audio and cuts off any currently playing work sound.
    /// </summary>
    public void StopWorkAudio()
    {
        m_isPlayingWorkAudio = false;
        m_soundTimer = 0.0f;

        if (m_audioSource != null)
        {
            m_audioSource.Stop();
        }
    }

    /// <summary>
    /// Plays one sound effect for the provided work type.
    /// </summary>
    private void PlayWorkSoundOnce(WorkType workType)
    {
        switch (workType)
        {
            case WorkType.Cut:
                PlaySound(m_cutSoundClip);
                return;

            case WorkType.Mine:
                PlaySound(m_mineSoundClip);
                return;

            case WorkType.Construct:
                PlaySound(m_constructSoundClip);
                return;

            case WorkType.Plant:
                PlaySound(m_plantSoundClip);
                return;

            case WorkType.Cook:
                PlaySound(m_cookSoundClip);
                return;
        }
    }

    /// <summary>
    /// Returns the sound repeat interval for a work type.
    /// </summary>
    private float GetRepeatInterval(WorkType workType)
    {
        switch (workType)
        {
            case WorkType.Cut:
                return m_cutRepeatInterval;

            case WorkType.Mine:
                return m_mineRepeatInterval;

            case WorkType.Construct:
                return m_constructRepeatInterval;

            case WorkType.Plant:
                return m_plantRepeatInterval;

            case WorkType.Cook:
                return m_cookRepeatInterval;
        }

        return m_defaultRepeatInterval;
    }

    /// <summary>
    /// Plays one sound effect through the pawn's AudioSource without overlapping the previous work sound.
    /// </summary>
    private void PlaySound(AudioClip audioClip)
    {
        if (m_audioSource == null || audioClip == null)
        {
            return;
        }

        if (m_audioSource.isPlaying)
        {
            return;
        }

        m_audioSource.clip = audioClip;
        m_audioSource.volume = m_workSoundVolume;
        m_audioSource.Play();
    }

    /// <summary>
    /// Applies basic 3D audio settings for pawn-positioned work sounds.
    /// </summary>
    private void ConfigureAudioSource()
    {
        if (m_audioSource == null)
        {
            return;
        }

        m_audioSource.playOnAwake = false;
        m_audioSource.loop = false;
        m_audioSource.spatialBlend = 1.0f;
        m_audioSource.minDistance = 4.0f;
        m_audioSource.maxDistance = 25.0f;
    }
}