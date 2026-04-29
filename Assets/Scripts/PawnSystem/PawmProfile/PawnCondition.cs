using System;
using UnityEngine;

/// <summary>
/// Stores high-level pawn need and condition values.
/// These values are generated and displayed only in V1 and do not tick down yet.
/// </summary>
[Serializable]
public class PawnCondition
{
    [SerializeField] private PawnMobilityState m_mobility = PawnMobilityState.Normal;
    [SerializeField] [Range(0, 100)] private int m_food = 100;
    [SerializeField] [Range(0, 100)] private int m_sleep = 100;
    [SerializeField] private PawnTemperatureState m_temperature = PawnTemperatureState.Comfortable;
    [SerializeField] private PawnHealthState m_health = PawnHealthState.Stable;

    public PawnMobilityState Mobility
    {
        get { return m_mobility; }
        set { m_mobility = value; }
    }

    public int Food
    {
        get { return m_food; }
        set { m_food = Mathf.Clamp(value, 0, 100); }
    }

    public int Sleep
    {
        get { return m_sleep; }
        set { m_sleep = Mathf.Clamp(value, 0, 100); }
    }

    public PawnTemperatureState Temperature
    {
        get { return m_temperature; }
        set { m_temperature = value; }
    }

    public PawnHealthState Health
    {
        get { return m_health; }
        set { m_health = value; }
    }

    public PawnFoodState FoodState
    {
        get { return ResolveFoodState(m_food); }
    }

    public PawnSleepState SleepState
    {
        get { return ResolveSleepState(m_sleep); }
    }

    /// <summary>
    /// Resolves the display food state from a 0-100 food value.
    /// </summary>
    private PawnFoodState ResolveFoodState(int food)
    {
        if (food >= 80)
        {
            return PawnFoodState.Full;
        }

        if (food >= 50)
        {
            return PawnFoodState.Content;
        }

        if (food >= 20)
        {
            return PawnFoodState.Hungry;
        }

        return PawnFoodState.Starving;
    }

    /// <summary>
    /// Resolves the display sleep state from a 0-100 sleep value.
    /// </summary>
    private PawnSleepState ResolveSleepState(int sleep)
    {
        if (sleep >= 70)
        {
            return PawnSleepState.Rested;
        }

        if (sleep >= 30)
        {
            return PawnSleepState.Tired;
        }

        return PawnSleepState.Exhausted;
    }
}