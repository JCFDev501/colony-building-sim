using ColonyBuildingSim.WorldContext;
using UnityEngine;

/// <summary>
/// Ticks pawn needs over world simulation time.
/// </summary>
public class PawnNeedsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pawn m_pPawn;
    [SerializeField] private WorldContextManager m_pWorldContextManager;

    [Header("Need Decay Per Simulation Second")]
    [SerializeField] private float m_foodDecayPerSecond = 0.03f;
    [SerializeField] private float m_sleepDecayPerSecond = 0.03f;

    [Header("Need Thresholds")]
    [SerializeField] private int m_eatThreshold = 35;
    [SerializeField] private int m_criticalEatThreshold = 15;
    [SerializeField] private int m_foodRestoreTarget = 85;
    [SerializeField] private int m_sleepThreshold = 25;
    [SerializeField] private int m_criticalSleepThreshold = 10;
    [SerializeField] private int m_sleepRestoreTarget = 100;

    [Header("Sleep Recovery")]
    [SerializeField] private float m_fullSleepRecoveryGameHours = 8.0f;

    [Header("Starvation")]
    [SerializeField] private float m_starvationDeathGameHours = 12.0f;

    [Header("Need Speed Penalties")]
    [SerializeField] private float m_hungryWorkSpeedPenalty = 0.10f;
    [SerializeField] private float m_starvingWorkSpeedPenalty = 0.30f;
    [SerializeField] private float m_tiredWorkSpeedPenalty = 0.10f;
    [SerializeField] private float m_exhaustedWorkSpeedPenalty = 0.25f;
    [SerializeField] private float m_starvingMoveSpeedPenalty = 0.20f;
    [SerializeField] private float m_exhaustedMoveSpeedPenalty = 0.20f;

    private const float kGameMinutesPerHour = 60.0f;

    private bool m_hasInitializedNeedValues = false;
    private bool m_isRecoveringSleep = false;
    private float m_currentFoodValue = 100.0f;
    private float m_currentSleepValue = 100.0f;
    private float m_starvationGameMinutes = 0.0f;

    /// <summary>
    /// Gets the current movement speed multiplier caused by pawn needs.
    /// </summary>
    public float NeedMoveSpeedMultiplier
    {
        get { return GetNeedMoveSpeedMultiplier(); }
    }

    /// <summary>
    /// Gets the current work speed multiplier caused by pawn needs.
    /// </summary>
    public float NeedWorkSpeedMultiplier
    {
        get { return GetNeedWorkSpeedMultiplier(); }
    }

    /// <summary>
    /// Gets whether the pawn should try to eat based on current Food.
    /// </summary>
    public bool ShouldEat
    {
        get { return GetFoodValue() <= m_eatThreshold; }
    }

    /// <summary>
    /// Gets whether the pawn's Food is critically low.
    /// </summary>
    public bool IsFoodCritical
    {
        get { return GetFoodValue() <= m_criticalEatThreshold; }
    }

    /// <summary>
    /// Gets whether the pawn should try to sleep based on current Sleep.
    /// </summary>
    public bool ShouldSleep
    {
        get { return GetSleepValue() <= m_sleepThreshold; }
    }

    /// <summary>
    /// Gets whether the pawn's Sleep is critically low.
    /// </summary>
    public bool IsSleepCritical
    {
        get { return GetSleepValue() <= m_criticalSleepThreshold; }
    }

    /// <summary>
    /// Gets the Food value the pawn should restore to after eating.
    /// </summary>
    public int FoodRestoreTarget
    {
        get { return m_foodRestoreTarget; }
    }

    /// <summary>
    /// Gets the Sleep value the pawn should restore to after sleeping.
    /// </summary>
    public int SleepRestoreTarget
    {
        get { return m_sleepRestoreTarget; }
    }

    /// <summary>
    /// Gets whether this pawn is currently recovering Sleep.
    /// </summary>
    public bool IsRecoveringSleep
    {
        get { return m_isRecoveringSleep; }
    }

    /// <summary>
    /// Gets how many in-game minutes this pawn has been at 0 Food.
    /// </summary>
    public float StarvationGameMinutes
    {
        get { return m_starvationGameMinutes; }
    }

    /// <summary>
    /// Gets how many in-game minutes this pawn can survive at 0 Food before death.
    /// </summary>
    public float StarvationDeathGameMinutes
    {
        get { return Mathf.Max(1.0f, m_starvationDeathGameHours) * kGameMinutesPerHour; }
    }

    /// <summary>
    /// Finds required references if they were not assigned in the Inspector.
    /// </summary>
    private void Awake()
    {
        CacheReferences();
    }

    /// <summary>
    /// Ticks pawn needs using world simulation time.
    /// </summary>
    private void Update()
    {
        TickNeeds();
    }

    /// <summary>
    /// Restores Food to the configured target value.
    /// </summary>
    public void RestoreFoodToTarget()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return;
        }

        InitializeNeedValuesIfNeeded(condition);

        m_currentFoodValue = Mathf.Clamp(m_foodRestoreTarget, 0.0f, 100.0f);
        m_starvationGameMinutes = 0.0f;

        condition.Food = Mathf.FloorToInt(m_currentFoodValue);

        if (condition.Health == PawnHealthState.AtRisk)
        {
            condition.Health = PawnHealthState.Stable;
        }
    }

    /// <summary>
    /// Restores Sleep to the configured target value immediately.
    /// This should only be used for debug or special cases.
    /// </summary>
    public void RestoreSleepToTarget()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return;
        }

        InitializeNeedValuesIfNeeded(condition);

        m_currentSleepValue = Mathf.Clamp(m_sleepRestoreTarget, 0.0f, 100.0f);
        condition.Sleep = Mathf.FloorToInt(m_currentSleepValue);
    }

    /// <summary>
    /// Starts sleep recovery and pauses normal Sleep decay.
    /// </summary>
    public void BeginSleepRecovery()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return;
        }

        InitializeNeedValuesIfNeeded(condition);
        m_isRecoveringSleep = true;
    }

    /// <summary>
    /// Stops sleep recovery and allows normal Sleep decay again.
    /// </summary>
    public void EndSleepRecovery()
    {
        m_isRecoveringSleep = false;
    }

    /// <summary>
    /// Recovers Sleep gradually based on in-game minutes slept.
    /// Returns true when the configured sleep target has been reached.
    /// </summary>
    public bool RecoverSleepForGameMinutes(float gameMinutes)
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return false;
        }

        InitializeNeedValuesIfNeeded(condition);

        if (gameMinutes <= 0.0f)
        {
            return false;
        }

        float safeFullRecoveryHours = Mathf.Max(1.0f, m_fullSleepRecoveryGameHours);
        float fullRecoveryGameMinutes = safeFullRecoveryHours * kGameMinutesPerHour;
        float recoveryPerGameMinute = 100.0f / fullRecoveryGameMinutes;

        m_currentSleepValue += recoveryPerGameMinute * gameMinutes;
        m_currentSleepValue = Mathf.Clamp(m_currentSleepValue, 0.0f, m_sleepRestoreTarget);

        condition.Sleep = Mathf.FloorToInt(m_currentSleepValue);

        return condition.Sleep >= m_sleepRestoreTarget;
    }

    /// <summary>
    /// Finds the pawn and world context references needed by this controller.
    /// </summary>
    private void CacheReferences()
    {
        if (m_pPawn == null)
        {
            m_pPawn = GetComponent<Pawn>();
        }

        if (m_pWorldContextManager == null)
        {
            m_pWorldContextManager = FindFirstObjectByType<WorldContextManager>();
        }
    }

    /// <summary>
    /// Updates Food and Sleep over simulation time.
    /// </summary>
    private void TickNeeds()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return;
        }

        if (condition.Health == PawnHealthState.Dead)
        {
            return;
        }

        InitializeNeedValuesIfNeeded(condition);

        float simulationDeltaTime = GetSimulationDeltaTime();

        if (simulationDeltaTime <= 0.0f)
        {
            return;
        }

        m_currentFoodValue -= m_foodDecayPerSecond * simulationDeltaTime;

        if (!m_isRecoveringSleep)
        {
            m_currentSleepValue -= m_sleepDecayPerSecond * simulationDeltaTime;
        }

        m_currentFoodValue = Mathf.Clamp(m_currentFoodValue, 0.0f, 100.0f);
        m_currentSleepValue = Mathf.Clamp(m_currentSleepValue, 0.0f, 100.0f);

        condition.Food = Mathf.FloorToInt(m_currentFoodValue);
        condition.Sleep = Mathf.FloorToInt(m_currentSleepValue);

        TickStarvation(condition);
    }

    /// <summary>
    /// Tracks how long the pawn has been at 0 Food and kills the pawn after the configured starvation duration.
    /// </summary>
    private void TickStarvation(PawnCondition condition)
    {
        if (condition == null || condition.Health == PawnHealthState.Dead)
        {
            return;
        }

        if (condition.Food > 0)
        {
            m_starvationGameMinutes = 0.0f;

            if (condition.Health == PawnHealthState.AtRisk)
            {
                condition.Health = PawnHealthState.Stable;
            }

            return;
        }

        condition.Health = PawnHealthState.AtRisk;

        float gameMinutesDeltaTime = GetSimulationGameMinutesDeltaTime();

        if (gameMinutesDeltaTime <= 0.0f)
        {
            return;
        }

        m_starvationGameMinutes += gameMinutesDeltaTime;

        if (m_starvationGameMinutes < StarvationDeathGameMinutes)
        {
            return;
        }

        condition.Health = PawnHealthState.Dead;
        m_isRecoveringSleep = false;

        Debug.Log("Pawn died from starvation: " + GetPawnDebugName(), this);
    }

    /// <summary>
    /// Initializes internal float need values from the pawn condition once profile data is available.
    /// </summary>
    private void InitializeNeedValuesIfNeeded(PawnCondition condition)
    {
        if (m_hasInitializedNeedValues)
        {
            return;
        }

        if (condition == null)
        {
            return;
        }

        m_currentFoodValue = condition.Food;
        m_currentSleepValue = condition.Sleep;
        m_starvationGameMinutes = 0.0f;
        m_hasInitializedNeedValues = true;
    }

    /// <summary>
    /// Gets simulation delta time from world context when available.
    /// </summary>
    private float GetSimulationDeltaTime()
    {
        if (m_pWorldContextManager == null)
        {
            return Time.deltaTime;
        }

        return m_pWorldContextManager.SimulationDeltaTime;
    }

    /// <summary>
    /// Gets in-game minutes passed this frame from world context when available.
    /// </summary>
    private float GetSimulationGameMinutesDeltaTime()
    {
        if (m_pWorldContextManager == null)
        {
            return Time.deltaTime / kGameMinutesPerHour;
        }

        return m_pWorldContextManager.SimulationGameMinutesDeltaTime;
    }

    /// <summary>
    /// Attempts to get this pawn's condition data.
    /// </summary>
    private bool TryGetCondition(out PawnCondition condition)
    {
        condition = null;

        if (m_pPawn == null || m_pPawn.Profile == null || m_pPawn.Profile.Condition == null)
        {
            return false;
        }

        condition = m_pPawn.Profile.Condition;
        return true;
    }

    /// <summary>
    /// Gets the current Food value.
    /// </summary>
    private int GetFoodValue()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return 100;
        }

        return condition.Food;
    }

    /// <summary>
    /// Gets the current Sleep value.
    /// </summary>
    private int GetSleepValue()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return 100;
        }

        return condition.Sleep;
    }

    /// <summary>
    /// Calculates the movement speed multiplier from current need states.
    /// </summary>
    private float GetNeedMoveSpeedMultiplier()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return 1.0f;
        }

        float multiplier = 1.0f;

        if (condition.FoodState == PawnFoodState.Starving)
        {
            multiplier -= m_starvingMoveSpeedPenalty;
        }

        if (condition.SleepState == PawnSleepState.Exhausted)
        {
            multiplier -= m_exhaustedMoveSpeedPenalty;
        }

        return Mathf.Max(0.1f, multiplier);
    }

    /// <summary>
    /// Calculates the work speed multiplier from current need states.
    /// </summary>
    private float GetNeedWorkSpeedMultiplier()
    {
        if (!TryGetCondition(out PawnCondition condition))
        {
            return 1.0f;
        }

        float multiplier = 1.0f;

        switch (condition.FoodState)
        {
            case PawnFoodState.Hungry:
                multiplier -= m_hungryWorkSpeedPenalty;
                break;

            case PawnFoodState.Starving:
                multiplier -= m_starvingWorkSpeedPenalty;
                break;
        }

        switch (condition.SleepState)
        {
            case PawnSleepState.Tired:
                multiplier -= m_tiredWorkSpeedPenalty;
                break;

            case PawnSleepState.Exhausted:
                multiplier -= m_exhaustedWorkSpeedPenalty;
                break;
        }

        return Mathf.Max(0.1f, multiplier);
    }

    /// <summary>
    /// Returns a readable pawn name for debug logs.
    /// </summary>
    private string GetPawnDebugName()
    {
        if (m_pPawn == null)
        {
            return "Unknown";
        }

        if (m_pPawn.Profile != null && !string.IsNullOrWhiteSpace(m_pPawn.Profile.DisplayName))
        {
            return m_pPawn.Profile.DisplayName;
        }

        return m_pPawn.PawnId;
    }
}