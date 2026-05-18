using System.Collections.Generic;
using ColonyBuildingSim.Inventory;
using ColonyBuildingSim.WorldContext;
using UnityEngine;
using ColonyBuildingSim.Work;

/// <summary>
/// Controls first-pass autonomous pawn behavior.
/// This component does not select tasks, execute work orders, or replace player movement commands.
/// </summary>
public class PawnBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pawn m_pPawn;
    [SerializeField] private GridManager m_pGridManager;
    [SerializeField] private PawnPathfinder m_pPawnPathfinder;
    [SerializeField] private WorldContextManager m_pWorldContextManager;
    [SerializeField] private TaskManager m_pTaskManager;
    [SerializeField] private ColonyInventoryManager m_pColonyInventoryManager;
    [SerializeField] private PawnWorkAudio m_pPawnWorkAudio;

    [Header("Brain State")]
    [SerializeField] private PawnBrainState m_currentState = PawnBrainState.Idle;
    [SerializeField] private PawnUtilityGoal m_currentUtilityGoal = PawnUtilityGoal.Wander;

    [Header("Idle Wander Settings")]
    [SerializeField] private bool m_enableIdleWandering = true;
    [SerializeField] private float m_thinkIntervalSeconds = 2.0f;
    [SerializeField] private int m_localWanderRadius = 4;
    [SerializeField] private int m_maxWanderTargetAttempts = 12;

    [Header("Sleep Settings")]
    [SerializeField] private Vector3 m_sleepEulerRotation = new Vector3(90.0f, 0.0f, 0.0f);

    [Header("Debug")]
    [SerializeField] private bool m_logWanderTargetSearch = false;

    private float m_thinkTimer = 0.0f;
    private bool m_hasActiveAutonomousMovement = false;
    private PawnTask m_pClaimedTask = null;
    private Vector2Int m_taskInteractionCoordinates = Vector2Int.zero;
    private bool m_hasTaskInteractionCoordinates = false;

    private Quaternion m_awakeRotation = Quaternion.identity;
    private bool m_hasCachedAwakeRotation = false;
    private bool m_isSleepingPoseApplied = false;

    /// <summary>
    /// Gets the current autonomous brain state.
    /// </summary>
    public PawnBrainState CurrentState
    {
        get { return m_currentState; }
    }

    /// <summary>
    /// Gets the current high-level utility goal selected by the pawn.
    /// </summary>
    public PawnUtilityGoal CurrentUtilityGoal
    {
        get { return m_currentUtilityGoal; }
    }

    /// <summary>
    /// Gets a player-readable description of what this pawn is currently doing.
    /// </summary>
    public string CurrentActivityLabel
    {
        get { return GetCurrentActivityLabel(); }
    }

    /// <summary>
    /// Finds required references if they were not assigned in the Inspector.
    /// </summary>
    private void Awake()
    {
        CacheReferences();
        CacheAwakeRotation();
    }

    /// <summary>
    /// Runs first-pass autonomous behavior checks and starts idle wandering when appropriate.
    /// </summary>
    private void Update()
    {
        if (PauseMenuController.IsSystemPaused)
        {
            StopPawnWorkAudio();
            return;
        }

        if (m_pPawn != null && m_pPawn.IsDeputized)
        {
            CancelAutonomousMovementForPlayerControl();
            CancelSleepForInterruption();
            ReleaseClaimedTaskForInterruption();
            m_currentState = PawnBrainState.Disabled;
            m_currentUtilityGoal = PawnUtilityGoal.Wander;
            ResetThinkTimer();
            return;
        }

        if (!CanRunBrain())
        {
            StopPawnWorkAudio();

            if (IsPawnDead())
            {
                CancelClaimedTaskForInvalidState();
            }

            CancelSleepForInterruption();
            m_currentState = PawnBrainState.Disabled;
            m_currentUtilityGoal = PawnUtilityGoal.Wander;
            ResetThinkTimer();
            return;
        }

        if (m_currentState == PawnBrainState.Sleeping)
        {
            UpdateSleepExecution();
            ResetThinkTimer();
            return;
        }

        if (m_pPawn.IsMoving)
        {
            UpdateMovingState();
            ResetThinkTimer();
            return;
        }

        if (m_currentState == PawnBrainState.MovingToSleep && m_currentUtilityGoal == PawnUtilityGoal.Sleep)
        {
            BeginSleeping();
            ResetThinkTimer();
            return;
        }

        m_hasActiveAutonomousMovement = false;
        m_currentState = PawnBrainState.Idle;

        if (ShouldInterruptForCriticalFood())
        {
            ReleaseClaimedTaskForInterruption();

            if (TryEatMeal())
            {
                ResetThinkTimer();
                return;
            }
        }

        if (ShouldInterruptForCriticalSleep())
        {
            ReleaseClaimedTaskForInterruption();

            if (TryStartSleepBehavior())
            {
                ResetThinkTimer();
                return;
            }
        }

        if (m_pClaimedTask != null)
        {
            UpdateClaimedTaskExecution();
            ResetThinkTimer();
            return;
        }

        StopPawnWorkAudio();

        if (TryEatMeal())
        {
            ResetThinkTimer();
            return;
        }

        if (TryStartSleepBehavior())
        {
            ResetThinkTimer();
            return;
        }

        if (!m_enableIdleWandering)
        {
            ResetThinkTimer();
            return;
        }

        UpdateThinkTimer();
    }

    /// <summary>
    /// Cancels the currently claimed task because the pawn or task state became permanently invalid.
    /// </summary>
    private void CancelClaimedTaskForInvalidState()
    {
        if (m_pClaimedTask == null)
        {
            return;
        }

        StopPawnWorkAudio();

        if (m_pClaimedTask.ClaimedPawn == m_pPawn)
        {
            m_pClaimedTask.Cancel();
        }

        m_pClaimedTask = null;
        m_taskInteractionCoordinates = Vector2Int.zero;
        m_hasTaskInteractionCoordinates = false;
    }

    /// <summary>
    /// Releases the currently claimed task because player control or another temporary interruption took over.
    /// </summary>
    private void ReleaseClaimedTaskForInterruption()
    {
        ReleaseClaimedTask();
    }

    /// <summary>
    /// Finds scene and owner references needed for autonomous behavior.
    /// </summary>
    private void CacheReferences()
    {
        if (m_pPawn == null)
        {
            m_pPawn = GetComponent<Pawn>();
        }

        if (m_pGridManager == null)
        {
            m_pGridManager = FindFirstObjectByType<GridManager>();
        }

        if (m_pPawnPathfinder == null)
        {
            m_pPawnPathfinder = FindFirstObjectByType<PawnPathfinder>();
        }

        if (m_pWorldContextManager == null)
        {
            m_pWorldContextManager = FindFirstObjectByType<WorldContextManager>();
        }

        if (m_pTaskManager == null)
        {
            m_pTaskManager = FindFirstObjectByType<TaskManager>();
        }

        if (m_pColonyInventoryManager == null)
        {
            m_pColonyInventoryManager = FindFirstObjectByType<ColonyInventoryManager>();
        }

        if (m_pPawnWorkAudio == null)
        {
            m_pPawnWorkAudio = GetComponent<PawnWorkAudio>();
        }
    }

    /// <summary>
    /// Stores the pawn's normal upright rotation so sleep can reset cleanly.
    /// </summary>
    private void CacheAwakeRotation()
    {
        if (m_hasCachedAwakeRotation)
        {
            return;
        }

        m_awakeRotation = transform.rotation;
        m_hasCachedAwakeRotation = true;
    }

    /// <summary>
    /// Returns whether this pawn profile represents a dead pawn.
    /// Dead pawns should not run autonomous behavior.
    /// </summary>
    private bool IsPawnDead()
    {
        if (m_pPawn == null || m_pPawn.Profile == null || m_pPawn.Profile.Condition == null)
        {
            return false;
        }

        return m_pPawn.Profile.Condition.Health == PawnHealthState.Dead;
    }

    /// <summary>
    /// Returns whether this pawn is currently allowed to run autonomous brain behavior.
    /// </summary>
    private bool CanRunBrain()
    {
        if (m_pPawn == null)
        {
            return false;
        }

        if (m_pGridManager == null)
        {
            return false;
        }

        if (m_pPawnPathfinder == null)
        {
            return false;
        }

        if (m_pWorldContextManager != null && m_pWorldContextManager.IsWorldPaused)
        {
            return false;
        }

        if (m_pPawn.Profile == null)
        {
            return false;
        }

        if (IsPawnDead())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Updates the displayed moving state based on whether the active movement was started by the brain.
    /// </summary>
    private void UpdateMovingState()
    {
        if (m_currentUtilityGoal == PawnUtilityGoal.Sleep)
        {
            m_currentState = PawnBrainState.MovingToSleep;
            return;
        }

        if (m_pClaimedTask != null || m_currentUtilityGoal == PawnUtilityGoal.Work)
        {
            m_currentState = PawnBrainState.Moving;
            return;
        }

        if (m_hasActiveAutonomousMovement)
        {
            m_currentState = PawnBrainState.Wandering;
            return;
        }

        m_currentState = PawnBrainState.Moving;
    }

    /// <summary>
    /// Advances the brain think timer while the pawn is idle.
    /// When the interval elapses, the pawn checks utility goals before falling back to local wandering.
    /// </summary>
    private void UpdateThinkTimer()
    {
        m_thinkTimer += Time.deltaTime;

        if (m_thinkTimer < m_thinkIntervalSeconds)
        {
            return;
        }

        ResetThinkTimer();

        PawnUtilityGoal bestGoal = EvaluateBestUtilityGoal();
        m_currentUtilityGoal = bestGoal;

        if (bestGoal == PawnUtilityGoal.Eat)
        {
            if (TryEatMeal())
            {
                return;
            }
        }

        if (bestGoal == PawnUtilityGoal.Sleep)
        {
            if (TryStartSleepBehavior())
            {
                return;
            }
        }

        if (bestGoal == PawnUtilityGoal.Work)
        {
            if (TryClaimBestAvailableTask())
            {
                return;
            }
        }

        TryStartLocalWander();
    }

    /// <summary>
    /// Gets delta time for pawn work from world context when available.
    /// This keeps task progress aligned with world pause and world speed.
    /// </summary>
    private float GetWorkDeltaTime()
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
    private float GetSleepGameMinutesDeltaTime()
    {
        if (m_pWorldContextManager == null)
        {
            return Time.deltaTime;
        }

        return m_pWorldContextManager.SimulationGameMinutesDeltaTime;
    }

    /// <summary>
    /// Chooses the best high-level utility goal before work priority selection runs.
    /// </summary>
    private PawnUtilityGoal EvaluateBestUtilityGoal()
    {
        float eatScore = GetEatUtilityScore();
        float sleepScore = GetSleepUtilityScore();
        float workScore = GetWorkUtilityScore();
        float wanderScore = GetWanderUtilityScore();

        PawnUtilityGoal bestGoal = PawnUtilityGoal.Wander;
        float bestScore = wanderScore;

        if (workScore > bestScore)
        {
            bestGoal = PawnUtilityGoal.Work;
            bestScore = workScore;
        }

        if (sleepScore > bestScore)
        {
            bestGoal = PawnUtilityGoal.Sleep;
            bestScore = sleepScore;
        }

        if (eatScore > bestScore)
        {
            bestGoal = PawnUtilityGoal.Eat;
        }

        return bestGoal;
    }

    /// <summary>
    /// Returns the utility score for eating.
    /// Eating is only useful when Food is low and a Meal exists.
    /// </summary>
    private float GetEatUtilityScore()
    {
        if (m_pPawn == null || !m_pPawn.ShouldEat)
        {
            return 0.0f;
        }

        if (!HasMealAvailable())
        {
            return 0.0f;
        }

        if (m_pPawn.IsFoodCritical)
        {
            return 100.0f;
        }

        return 85.0f;
    }

    /// <summary>
    /// Returns the utility score for sleeping.
    /// Sleeping is useful when Sleep is low.
    /// </summary>
    private float GetSleepUtilityScore()
    {
        if (m_pPawn == null || !m_pPawn.ShouldSleep)
        {
            return 0.0f;
        }

        if (m_pPawn.IsSleepCritical)
        {
            return 95.0f;
        }

        return 80.0f;
    }

    /// <summary>
    /// Returns the utility score for work.
    /// Work remains delegated to the existing work priority system after this goal is selected.
    /// </summary>
    private float GetWorkUtilityScore()
    {
        if (m_pTaskManager == null)
        {
            return 0.0f;
        }

        List<PawnTask> availableTasks = m_pTaskManager.GetAvailableTasks();

        if (availableTasks.Count == 0)
        {
            return 0.0f;
        }

        PawnTask pBestTask = FindBestAvailableTask(availableTasks);

        if (pBestTask == null)
        {
            return 0.0f;
        }

        if (m_pPawn == null)
        {
            return 0.0f;
        }

        return 50.0f * m_pPawn.NeedWorkSpeedMultiplier;
    }

    /// <summary>
    /// Returns the fallback utility score for wandering.
    /// </summary>
    private float GetWanderUtilityScore()
    {
        if (!m_enableIdleWandering)
        {
            return 0.0f;
        }

        return 5.0f;
    }

    /// <summary>
    /// Returns whether the pawn should interrupt current work to eat.
    /// </summary>
    private bool ShouldInterruptForCriticalFood()
    {
        if (m_pPawn == null)
        {
            return false;
        }

        if (!m_pPawn.IsFoodCritical)
        {
            return false;
        }

        return HasMealAvailable();
    }

    /// <summary>
    /// Returns whether the pawn should interrupt current work to sleep.
    /// </summary>
    private bool ShouldInterruptForCriticalSleep()
    {
        if (m_pPawn == null)
        {
            return false;
        }

        return m_pPawn.IsSleepCritical;
    }

    /// <summary>
    /// Returns whether the colony currently has at least one Meal available.
    /// </summary>
    private bool HasMealAvailable()
    {
        if (m_pColonyInventoryManager == null)
        {
            return false;
        }

        return m_pColonyInventoryManager.HasResource(ResourceType.Meal, 1);
    }

    /// <summary>
    /// Consumes one Meal and restores this pawn's Food need when eating is useful.
    /// </summary>
    private bool TryEatMeal()
    {
        if (m_pPawn == null || !m_pPawn.ShouldEat)
        {
            return false;
        }

        if (m_pColonyInventoryManager == null)
        {
            return false;
        }

        if (!m_pColonyInventoryManager.TryRemoveResource(ResourceType.Meal, 1))
        {
            return false;
        }

        m_currentUtilityGoal = PawnUtilityGoal.Eat;
        m_currentState = PawnBrainState.Eating;

        m_pPawn.RestoreFoodToTarget();

        Debug.Log("PawnBrain ate Meal for " + m_pPawn.PawnId + ".", this);

        m_currentState = PawnBrainState.Idle;
        m_currentUtilityGoal = PawnUtilityGoal.Wander;

        return true;
    }

    /// <summary>
    /// Starts the first-pass sleep behavior by moving the pawn to a nearby valid tile.
    /// </summary>
    private bool TryStartSleepBehavior()
    {
        if (m_pPawn == null || !m_pPawn.ShouldSleep)
        {
            return false;
        }

        if (!TryFindLocalWanderTarget(out Vector2Int sleepTargetCoordinates))
        {
            return false;
        }

        Vector2Int startCoordinates = m_pPawn.GridCoordinate;

        if (!m_pPawnPathfinder.TryFindPath(
                m_pPawn,
                m_pGridManager,
                startCoordinates,
                sleepTargetCoordinates,
                out List<Vector2Int> path))
        {
            return false;
        }

        if (!m_pPawn.TryStartPathMovement(path))
        {
            return false;
        }

        m_hasActiveAutonomousMovement = true;
        m_currentUtilityGoal = PawnUtilityGoal.Sleep;
        m_currentState = PawnBrainState.MovingToSleep;

        Debug.Log(
            "PawnBrain moving to sleep tile for "
            + m_pPawn.PawnId
            + ": "
            + sleepTargetCoordinates,
            this);

        return true;
    }

    /// <summary>
    /// Starts sleeping once the pawn reaches its sleep tile.
    /// </summary>
    private void BeginSleeping()
    {
        m_hasActiveAutonomousMovement = false;
        m_currentUtilityGoal = PawnUtilityGoal.Sleep;
        m_currentState = PawnBrainState.Sleeping;

        m_pPawn.BeginSleepRecovery();

        ApplySleepingPose();

        Debug.Log("PawnBrain started sleeping for " + m_pPawn.PawnId + ".", this);
    }

    /// <summary>
    /// Advances sleep recovery using in-game minutes and wakes the pawn when fully rested.
    /// </summary>
    private void UpdateSleepExecution()
    {
        if (m_pPawn == null)
        {
            CancelSleepForInterruption();
            return;
        }

        float gameMinutesDeltaTime = GetSleepGameMinutesDeltaTime();

        if (gameMinutesDeltaTime <= 0.0f)
        {
            return;
        }

        bool isRested = m_pPawn.RecoverSleepForGameMinutes(gameMinutesDeltaTime);

        if (!isRested)
        {
            return;
        }

        m_pPawn.EndSleepRecovery();
        ResetSleepingPose();

        Debug.Log("PawnBrain finished sleeping for " + m_pPawn.PawnId + ".", this);

        m_currentState = PawnBrainState.Idle;
        m_currentUtilityGoal = PawnUtilityGoal.Wander;
    }

    /// <summary>
    /// Applies a simple laying rotation while the pawn sleeps.
    /// </summary>
    private void ApplySleepingPose()
    {
        CacheAwakeRotation();

        if (m_isSleepingPoseApplied)
        {
            return;
        }

        transform.rotation = m_awakeRotation * Quaternion.Euler(m_sleepEulerRotation);
        m_isSleepingPoseApplied = true;
    }

    /// <summary>
    /// Resets the pawn rotation after sleeping or sleep interruption.
    /// </summary>
    private void ResetSleepingPose()
    {
        if (!m_isSleepingPoseApplied)
        {
            return;
        }

        transform.rotation = m_awakeRotation;
        m_isSleepingPoseApplied = false;
    }

    /// <summary>
    /// Cancels sleep state and resets sleep visuals when another mode interrupts it.
    /// Partial Sleep recovery is kept because PawnCondition was updated over time.
    /// </summary>
    private void CancelSleepForInterruption()
    {
        if (m_currentState != PawnBrainState.Sleeping
            && m_currentState != PawnBrainState.MovingToSleep)
        {
            return;
        }

        if (m_pPawn != null)
        {
            m_pPawn.EndSleepRecovery();
        }

        ResetSleepingPose();

        if (m_currentUtilityGoal == PawnUtilityGoal.Sleep)
        {
            m_currentUtilityGoal = PawnUtilityGoal.Wander;
        }
    }

    /// <summary>
    /// Attempts to claim the best available task using this pawn's work priorities.
    /// </summary>
    private bool TryClaimBestAvailableTask()
    {
        if (m_pTaskManager == null)
        {
            return false;
        }

        List<PawnTask> availableTasks = m_pTaskManager.GetAvailableTasks();

        if (availableTasks.Count == 0)
        {
            return false;
        }

        PawnTask pBestTask = FindBestAvailableTask(availableTasks);

        if (pBestTask == null)
        {
            return false;
        }

        if (!pBestTask.TryClaim(m_pPawn))
        {
            return false;
        }

        m_pClaimedTask = pBestTask;
        m_currentState = PawnBrainState.Idle;
        m_currentUtilityGoal = PawnUtilityGoal.Work;

        Debug.Log(
            "PawnBrain claimed task for " + m_pPawn.PawnId +
            ": " + pBestTask.TaskType +
            " at " + pBestTask.TargetCoordinates,
            this);

        if (!TryMoveToClaimedTask())
        {
            ReleaseClaimedTask();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to move this pawn to a reachable interaction tile for the claimed task.
    /// </summary>
    private bool TryMoveToClaimedTask()
    {
        if (m_pClaimedTask == null || m_pClaimedTask.ClaimedPawn != m_pPawn)
        {
            return false;
        }

        if (!TryFindTaskInteractionTarget(m_pClaimedTask, out Vector2Int interactionCoordinates))
        {
            return false;
        }

        m_taskInteractionCoordinates = interactionCoordinates;
        m_hasTaskInteractionCoordinates = true;

        Vector2Int startCoordinates = m_pPawn.GetPathStartCoordinates();

        if (!m_pPawnPathfinder.TryFindPath(
                m_pPawn,
                m_pGridManager,
                startCoordinates,
                interactionCoordinates,
                out List<Vector2Int> path))
        {
            Debug.Log(
                "PawnBrain could not find path to claimed task for " + m_pPawn.PawnId +
                ": " + m_pClaimedTask.TaskType +
                " at " + m_pClaimedTask.TargetCoordinates,
                this);

            return false;
        }

        if (!m_pPawn.TryStartPathMovement(path))
        {
            return false;
        }

        m_hasActiveAutonomousMovement = true;
        m_currentState = PawnBrainState.Moving;
        m_currentUtilityGoal = PawnUtilityGoal.Work;

        return true;
    }

    /// <summary>
    /// Finds the best reachable tile the pawn should stand on to interact with a task target.
    /// Cut and Construct targets should be interacted with from an adjacent tile.
    /// </summary>
    private bool TryFindTaskInteractionTarget(PawnTask pTask, out Vector2Int interactionCoordinates)
    {
        interactionCoordinates = Vector2Int.zero;

        if (pTask == null || m_pGridManager == null || m_pPawnPathfinder == null || m_pPawn == null)
        {
            return false;
        }

        Vector2Int startCoordinates = m_pPawn.GetPathStartCoordinates();

        if (ShouldUseAdjacentInteractionTile(pTask))
        {
            return TryFindAdjacentTaskInteractionTarget(pTask, startCoordinates, out interactionCoordinates);
        }

        if (m_pGridManager.CanEnterTile(pTask.TargetCoordinates)
            && CanPathToTaskInteractionTile(startCoordinates, pTask.TargetCoordinates))
        {
            interactionCoordinates = pTask.TargetCoordinates;
            return true;
        }

        return TryFindAdjacentTaskInteractionTarget(pTask, startCoordinates, out interactionCoordinates);
    }

    /// <summary>
    /// Returns true when the pawn should stand next to the target instead of on top of it.
    /// </summary>
    private bool ShouldUseAdjacentInteractionTile(PawnTask pTask)
    {
        return pTask.TaskType == TaskType.Cut
               || pTask.TaskType == TaskType.Mine
               || pTask.TaskType == TaskType.Construct
               || pTask.TaskType == TaskType.Plant;
    }

    /// <summary>
    /// Finds a reachable adjacent tile the pawn can stand on to interact with a task target.
    /// </summary>
    private bool TryFindAdjacentTaskInteractionTarget(
        PawnTask pTask,
        Vector2Int startCoordinates,
        out Vector2Int interactionCoordinates)
    {
        interactionCoordinates = Vector2Int.zero;

        foreach (Vector2Int neighborCoordinates in m_pGridManager.GetAllNeighborCoordinates(pTask.TargetCoordinates))
        {
            if (!m_pGridManager.CanEnterTile(neighborCoordinates))
            {
                continue;
            }

            if (!CanPathToTaskInteractionTile(startCoordinates, neighborCoordinates))
            {
                continue;
            }

            interactionCoordinates = neighborCoordinates;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns whether this pawn can currently path to the provided task interaction tile.
    /// </summary>
    private bool CanPathToTaskInteractionTile(Vector2Int startCoordinates, Vector2Int interactionCoordinates)
    {
        return m_pPawnPathfinder.TryFindPath(
            m_pPawn,
            m_pGridManager,
            startCoordinates,
            interactionCoordinates,
            out List<Vector2Int> path);
    }

    /// <summary>
    /// Releases the currently claimed task, if one exists.
    /// </summary>
    private void ReleaseClaimedTask()
    {
        if (m_pClaimedTask == null)
        {
            return;
        }

        StopPawnWorkAudio();

        if (m_pClaimedTask.ClaimedPawn == m_pPawn)
        {
            m_pClaimedTask.Release();
        }

        m_pClaimedTask = null;
        m_taskInteractionCoordinates = Vector2Int.zero;
        m_hasTaskInteractionCoordinates = false;
    }

    /// <summary>
    /// Starts or updates simple task execution once the pawn has reached the task interaction tile.
    /// </summary>
    private void UpdateClaimedTaskExecution()
    {
        if (m_pClaimedTask == null)
        {
            StopPawnWorkAudio();
            return;
        }

        if (!IsClaimedTaskStillValid())
        {
            CancelClaimedTaskForInvalidState();
            return;
        }

        if (!CanStillReachClaimedTask())
        {
            ReleaseClaimedTaskForInterruption();
            return;
        }

        if (m_pClaimedTask.ClaimedPawn != m_pPawn)
        {
            StopPawnWorkAudio();
            m_pClaimedTask = null;
            m_taskInteractionCoordinates = Vector2Int.zero;
            m_hasTaskInteractionCoordinates = false;
            return;
        }

        if (!m_hasTaskInteractionCoordinates)
        {
            ReleaseClaimedTask();
            return;
        }

        if (m_pPawn.GridCoordinate != m_taskInteractionCoordinates)
        {
            StopPawnWorkAudio();
            return;
        }

        if (m_pClaimedTask.State == TaskState.Claimed)
        {
            m_pClaimedTask.MarkInProgress();
        }

        if (m_pClaimedTask.State != TaskState.InProgress)
        {
            StopPawnWorkAudio();
            return;
        }

        StartPawnWorkAudio(m_pClaimedTask.ParentWorkType);

        float workDeltaTime = GetWorkDeltaTime();

        if (workDeltaTime <= 0.0f)
        {
            return;
        }

        float workSpeedMultiplier = GetTaskWorkSpeedMultiplier(m_pClaimedTask);
        m_pClaimedTask.AddProgress(workDeltaTime * workSpeedMultiplier);

        if (m_pClaimedTask.State == TaskState.Complete)
        {
            StopPawnWorkAudio();

            if (m_pTaskManager != null)
            {
                m_pTaskManager.CompleteTask(m_pClaimedTask);
            }

            Debug.Log(
                "PawnBrain completed task for " + m_pPawn.PawnId +
                ": " + m_pClaimedTask.TaskType +
                " at " + m_pClaimedTask.TargetCoordinates,
                this);

            m_pClaimedTask = null;
            m_taskInteractionCoordinates = Vector2Int.zero;
            m_hasTaskInteractionCoordinates = false;
        }
    }

    /// <summary>
    /// Starts pawn-positioned work audio for the requested work type.
    /// </summary>
    private void StartPawnWorkAudio(WorkType workType)
    {
        if (m_pPawnWorkAudio == null)
        {
            return;
        }

        m_pPawnWorkAudio.StartWorkAudio(workType);
    }

    /// <summary>
    /// Stops any pawn-positioned work audio currently playing for this pawn.
    /// </summary>
    private void StopPawnWorkAudio()
    {
        if (m_pPawnWorkAudio == null)
        {
            return;
        }

        m_pPawnWorkAudio.StopWorkAudio();
    }

    /// <summary>
    /// Returns whether the pawn can still use or reach the current claimed task interaction tile.
    /// </summary>
    private bool CanStillReachClaimedTask()
    {
        if (m_pClaimedTask == null)
        {
            return false;
        }

        if (!m_hasTaskInteractionCoordinates)
        {
            return false;
        }

        if (m_pPawn == null || m_pGridManager == null || m_pPawnPathfinder == null)
        {
            return false;
        }

        if (m_pPawn.GridCoordinate == m_taskInteractionCoordinates)
        {
            return true;
        }

        if (!m_pGridManager.CanEnterTile(m_taskInteractionCoordinates))
        {
            return false;
        }

        Vector2Int startCoordinates = m_pPawn.GetPathStartCoordinates();

        return CanPathToTaskInteractionTile(startCoordinates, m_taskInteractionCoordinates);
    }

    /// <summary>
    /// Returns whether the currently claimed task still has a valid parent work order and world target.
    /// </summary>
    private bool IsClaimedTaskStillValid()
    {
        if (m_pClaimedTask == null)
        {
            return false;
        }

        if (m_pClaimedTask.ParentWorkOrder == null)
        {
            return false;
        }

        if (m_pClaimedTask.ParentWorkOrder.State == WorkOrderState.Cancelled
            || m_pClaimedTask.ParentWorkOrder.State == WorkOrderState.Complete)
        {
            return false;
        }

        if (m_pClaimedTask.TaskType == TaskType.Cut)
        {
            return WorkTargetValidator.IsValidCutTarget(m_pGridManager, m_pClaimedTask.TargetCoordinates);
        }

        if (m_pClaimedTask.TaskType == TaskType.Mine)
        {
            return WorkTargetValidator.IsValidMineTarget(m_pGridManager, m_pClaimedTask.TargetCoordinates);
        }

        return true;
    }

    /// <summary>
    /// Finds the best available task by first selecting the best work type from pawn priorities,
    /// then choosing the nearest reachable task inside that winning work type.
    /// </summary>
    private PawnTask FindBestAvailableTask(List<PawnTask> availableTasks)
    {
        WorkType bestWorkType = WorkType.Cut;
        bool hasBestWorkType = false;

        foreach (PawnTask pTask in availableTasks)
        {
            if (pTask == null || !pTask.CanBeClaimed())
            {
                continue;
            }

            if (!CanReachTask(pTask))
            {
                continue;
            }

            if (!hasBestWorkType || IsWorkTypePreferredOver(pTask.ParentWorkType, bestWorkType))
            {
                bestWorkType = pTask.ParentWorkType;
                hasBestWorkType = true;
            }
        }

        if (!hasBestWorkType)
        {
            return null;
        }

        return FindNearestReachableTaskOfWorkType(availableTasks, bestWorkType);
    }

    /// <summary>
    /// Returns true when one work type should be preferred over another based on this pawn's priorities.
    /// Lower priority values win. If priorities tie, lower WorkType enum order wins.
    /// </summary>
    private bool IsWorkTypePreferredOver(WorkType candidateWorkType, WorkType currentBestWorkType)
    {
        int candidatePriority = m_pPawn.Profile.WorkPriorities.GetPriority(candidateWorkType);
        int currentBestPriority = m_pPawn.Profile.WorkPriorities.GetPriority(currentBestWorkType);

        if (candidatePriority != currentBestPriority)
        {
            return candidatePriority < currentBestPriority;
        }

        return candidateWorkType < currentBestWorkType;
    }

    /// <summary>
    /// Returns the nearest reachable task of the requested work type.
    /// </summary>
    private PawnTask FindNearestReachableTaskOfWorkType(List<PawnTask> availableTasks, WorkType workType)
    {
        PawnTask pNearestTask = null;
        int nearestPathDistance = int.MaxValue;

        foreach (PawnTask pTask in availableTasks)
        {
            if (pTask == null || !pTask.CanBeClaimed())
            {
                continue;
            }

            if (pTask.ParentWorkType != workType)
            {
                continue;
            }

            if (!TryGetTaskPathDistance(pTask, out int pathDistance))
            {
                continue;
            }

            if (pNearestTask == null || pathDistance < nearestPathDistance)
            {
                pNearestTask = pTask;
                nearestPathDistance = pathDistance;
            }
        }

        return pNearestTask;
    }

    /// <summary>
    /// Returns true when this pawn can reach an interaction tile for the task.
    /// </summary>
    private bool CanReachTask(PawnTask pTask)
    {
        return TryGetTaskPathDistance(pTask, out int pathDistance);
    }

    /// <summary>
    /// Attempts to calculate the path distance from this pawn to a valid task interaction tile.
    /// </summary>
    private bool TryGetTaskPathDistance(PawnTask pTask, out int pathDistance)
    {
        pathDistance = int.MaxValue;

        if (pTask == null || m_pPawn == null || m_pGridManager == null || m_pPawnPathfinder == null)
        {
            return false;
        }

        Vector2Int startCoordinates = m_pPawn.GetPathStartCoordinates();

        if (!TryFindTaskInteractionTarget(pTask, out Vector2Int interactionCoordinates))
        {
            return false;
        }

        if (!m_pPawnPathfinder.TryFindPath(
                m_pPawn,
                m_pGridManager,
                startCoordinates,
                interactionCoordinates,
                out List<Vector2Int> path))
        {
            return false;
        }

        pathDistance = path.Count;
        return true;
    }

    /// <summary>
    /// Attempts to find a valid local wander target, validate a path, and command movement.
    /// </summary>
    private bool TryStartLocalWander()
    {
        if (!TryFindLocalWanderTarget(out Vector2Int wanderTargetCoordinates))
        {
            return false;
        }

        Vector2Int startCoordinates = m_pPawn.GridCoordinate;

        if (!m_pPawnPathfinder.TryFindPath(
                m_pPawn,
                m_pGridManager,
                startCoordinates,
                wanderTargetCoordinates,
                out List<Vector2Int> path))
        {
            if (m_logWanderTargetSearch)
            {
                Debug.Log(
                    "PawnBrain rejected unreachable local wander target for " + m_pPawn.PawnId +
                    ": " + wanderTargetCoordinates,
                    this);
            }

            return false;
        }

        if (!m_pPawn.TryStartPathMovement(path))
        {
            return false;
        }

        m_hasActiveAutonomousMovement = true;
        m_currentState = PawnBrainState.Wandering;
        m_currentUtilityGoal = PawnUtilityGoal.Wander;

        if (m_logWanderTargetSearch)
        {
            Debug.Log(
                "PawnBrain started local wander for " + m_pPawn.PawnId +
                ": " + startCoordinates + " -> " + wanderTargetCoordinates,
                this);
        }

        return true;
    }

    /// <summary>
    /// Attempts to find a nearby valid tile that this pawn can use as a local movement target.
    /// </summary>
    private bool TryFindLocalWanderTarget(out Vector2Int wanderTargetCoordinates)
    {
        wanderTargetCoordinates = Vector2Int.zero;

        if (m_pPawn == null || m_pGridManager == null)
        {
            return false;
        }

        int safeWanderRadius = Mathf.Max(1, m_localWanderRadius);
        int safeAttemptCount = Mathf.Max(1, m_maxWanderTargetAttempts);
        Vector2Int originCoordinates = m_pPawn.GridCoordinate;

        for (int i = 0; i < safeAttemptCount; ++i)
        {
            int xOffset = Random.Range(-safeWanderRadius, safeWanderRadius + 1);
            int yOffset = Random.Range(-safeWanderRadius, safeWanderRadius + 1);

            Vector2Int candidateCoordinates = originCoordinates + new Vector2Int(xOffset, yOffset);

            if (!IsValidLocalWanderTarget(originCoordinates, candidateCoordinates))
            {
                continue;
            }

            wanderTargetCoordinates = candidateCoordinates;
            return true;
        }

        if (m_logWanderTargetSearch)
        {
            Debug.Log(
                "PawnBrain could not find local movement target for " + m_pPawn.PawnId,
                this);
        }

        return false;
    }

    /// <summary>
    /// Returns the work speed multiplier this pawn should use for the given task.
    /// Skill gives a small bonus per level, profile modifiers apply afterward,
    /// and need state can reduce the final work speed.
    /// </summary>
    private float GetTaskWorkSpeedMultiplier(PawnTask pTask)
    {
        if (pTask == null || m_pPawn == null || m_pPawn.Profile == null || m_pPawn.Profile.Skills == null)
        {
            return 1.0f;
        }

        int skillValue = GetSkillValueForWorkType(pTask.ParentWorkType);
        float skillMultiplier = 1.0f + (skillValue * 0.05f);
        float profileMultiplier = 1.0f + m_pPawn.Profile.FinalWorkSpeedModifier;
        float needMultiplier = m_pPawn.NeedWorkSpeedMultiplier;

        return Mathf.Max(0.1f, skillMultiplier * profileMultiplier * needMultiplier);
    }

    /// <summary>
    /// Returns this pawn's skill value for the requested work type.
    /// </summary>
    private int GetSkillValueForWorkType(WorkType workType)
    {
        PawnSkills skills = m_pPawn.Profile.Skills;

        switch (workType)
        {
            case WorkType.Plant:
                return skills.Plant;

            case WorkType.Cut:
                return skills.Cut;

            case WorkType.Construct:
                return skills.Construct;

            case WorkType.Cook:
                return skills.Cook;

            case WorkType.Craft:
                return skills.Craft;

            case WorkType.Mine:
                return skills.Mine;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Returns whether the candidate coordinate is valid enough to be considered for local movement.
    /// Reachability is checked separately by PawnPathfinder before movement is commanded.
    /// </summary>
    private bool IsValidLocalWanderTarget(Vector2Int originCoordinates, Vector2Int candidateCoordinates)
    {
        if (candidateCoordinates == originCoordinates)
        {
            return false;
        }

        if (!m_pGridManager.IsInBounds(candidateCoordinates))
        {
            return false;
        }

        if (!m_pGridManager.CanEnterTile(candidateCoordinates))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Cancels brain-owned movement when the player takes direct control of the pawn.
    /// The pawn is not snapped, so the visual interruption stays smooth.
    /// </summary>
    private void CancelAutonomousMovementForPlayerControl()
    {
        if (!m_hasActiveAutonomousMovement)
        {
            return;
        }

        if (m_pPawn == null)
        {
            m_hasActiveAutonomousMovement = false;
            return;
        }

        StopPawnWorkAudio();

        m_pPawn.CancelMovementInPlace();
        m_hasActiveAutonomousMovement = false;

        if (m_logWanderTargetSearch)
        {
            Debug.Log("PawnBrain cancelled autonomous movement for player control: " + m_pPawn.PawnId, this);
        }
    }

    /// <summary>
    /// Gets a readable activity label for debug UI.
    /// </summary>
    private string GetCurrentActivityLabel()
    {
        if (m_pPawn == null)
        {
            return "Unavailable";
        }

        if (m_pWorldContextManager != null && m_pWorldContextManager.IsWorldPaused)
        {
            return "Paused";
        }

        if (m_pPawn.Profile == null)
        {
            return "Waiting for profile";
        }

        if (IsPawnDead())
        {
            return "Dead";
        }

        if (m_currentState == PawnBrainState.Eating)
        {
            return "Eating";
        }

        if (m_currentState == PawnBrainState.MovingToSleep)
        {
            return "Moving to sleep";
        }

        if (m_currentState == PawnBrainState.Sleeping)
        {
            return "Sleeping";
        }

        if (m_pPawn.IsDeputized)
        {
            if (m_pPawn.IsMoving)
            {
                return "Executing move order";
            }

            return "Awaiting orders";
        }

        if (m_pPawn.IsMoving)
        {
            if (m_pClaimedTask != null)
            {
                return "Moving to " + m_pClaimedTask.TaskType;
            }

            if (m_hasActiveAutonomousMovement)
            {
                return "Wandering";
            }

            return "Moving";
        }

        if (m_pClaimedTask != null)
        {
            if (m_pClaimedTask.State == TaskState.InProgress)
            {
                return "Performing " + m_pClaimedTask.TaskType;
            }

            return "Waiting at " + m_pClaimedTask.TaskType;
        }

        return "Idle";
    }

    /// <summary>
    /// Resets the brain think timer.
    /// </summary>
    private void ResetThinkTimer()
    {
        m_thinkTimer = 0.0f;
    }
}