using System;
using UnityEngine;

/// <summary>
/// Stores per-pawn work priority values for each first-pass colony work type.
/// Lower numbers are higher priority, and values are clamped between 1 and 4.
/// </summary>
[Serializable]
public class PawnWorkPriorities
{
    private const int kHighestPriority = 1;
    private const int kLowestPriority = 4;

    [SerializeField] private int m_plantPriority = kLowestPriority;
    [SerializeField] private int m_cutPriority = kLowestPriority;
    [SerializeField] private int m_constructPriority = kLowestPriority;
    [SerializeField] private int m_cookPriority = kLowestPriority;
    [SerializeField] private int m_craftPriority = kLowestPriority;
    [SerializeField] private int m_minePriority = kLowestPriority;

    /// <summary>
    /// Gets or sets the Plant work priority.
    /// </summary>
    public int PlantPriority
    {
        get { return m_plantPriority; }
        set { m_plantPriority = ClampPriority(value); }
    }

    /// <summary>
    /// Gets or sets the Cut work priority.
    /// </summary>
    public int CutPriority
    {
        get { return m_cutPriority; }
        set { m_cutPriority = ClampPriority(value); }
    }

    /// <summary>
    /// Gets or sets the Construct work priority.
    /// </summary>
    public int ConstructPriority
    {
        get { return m_constructPriority; }
        set { m_constructPriority = ClampPriority(value); }
    }

    /// <summary>
    /// Gets or sets the Cook work priority.
    /// </summary>
    public int CookPriority
    {
        get { return m_cookPriority; }
        set { m_cookPriority = ClampPriority(value); }
    }

    /// <summary>
    /// Gets or sets the Craft work priority.
    /// </summary>
    public int CraftPriority
    {
        get { return m_craftPriority; }
        set { m_craftPriority = ClampPriority(value); }
    }

    /// <summary>
    /// Gets or sets the Mine work priority.
    /// </summary>
    public int MinePriority
    {
        get { return m_minePriority; }
        set { m_minePriority = ClampPriority(value); }
    }

    /// <summary>
    /// Gets the priority value for the provided work type.
    /// Lower numbers are better.
    /// </summary>
    public int GetPriority(WorkType workType)
    {
        switch (workType)
        {
            case WorkType.Plant:
                return m_plantPriority;

            case WorkType.Cut:
                return m_cutPriority;

            case WorkType.Construct:
                return m_constructPriority;

            case WorkType.Cook:
                return m_cookPriority;

            case WorkType.Craft:
                return m_craftPriority;

            case WorkType.Mine:
                return m_minePriority;

            default:
                return kLowestPriority;
        }
    }

    /// <summary>
    /// Sets the priority value for the provided work type.
    /// The assigned value is clamped between 1 and 4.
    /// </summary>
    public void SetPriority(WorkType workType, int priority)
    {
        int clampedPriority = ClampPriority(priority);

        switch (workType)
        {
            case WorkType.Plant:
                m_plantPriority = clampedPriority;
                break;

            case WorkType.Cut:
                m_cutPriority = clampedPriority;
                break;

            case WorkType.Construct:
                m_constructPriority = clampedPriority;
                break;

            case WorkType.Cook:
                m_cookPriority = clampedPriority;
                break;

            case WorkType.Craft:
                m_craftPriority = clampedPriority;
                break;

            case WorkType.Mine:
                m_minePriority = clampedPriority;
                break;
        }
    }

    /// <summary>
    /// Compares two work types using priority first and enum order second.
    /// Returns a negative value when the first work type wins,
    /// a positive value when the second work type wins,
    /// and zero when they are equivalent.
    /// </summary>
    public int CompareWorkTypes(WorkType firstWorkType, WorkType secondWorkType)
    {
        int firstPriority = GetPriority(firstWorkType);
        int secondPriority = GetPriority(secondWorkType);

        if (firstPriority != secondPriority)
        {
            return firstPriority.CompareTo(secondPriority);
        }

        return ((int)firstWorkType).CompareTo((int)secondWorkType);
    }

    /// <summary>
    /// Clamps priority values into the supported V1 range.
    /// </summary>
    private int ClampPriority(int priority)
    {
        return Mathf.Clamp(priority, kHighestPriority, kLowestPriority);
    }
}