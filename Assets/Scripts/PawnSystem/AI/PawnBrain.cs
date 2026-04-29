using System.Collections.Generic;
using ColonyBuildingSim.WorldContext;
using UnityEngine;

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

    [Header("Brain State")]
    [SerializeField] private PawnBrainState m_currentState = PawnBrainState.Idle;

    [Header("Idle Wander Settings")]
    [SerializeField] private bool m_enableIdleWandering = true;
    [SerializeField] private float m_thinkIntervalSeconds = 2.0f;
    [SerializeField] private int m_localWanderRadius = 4;
    [SerializeField] private int m_maxWanderTargetAttempts = 12;

    [Header("Debug")]
    [SerializeField] private bool m_logWanderTargetSearch = false;

    private float m_thinkTimer = 0.0f;
    private bool m_hasActiveAutonomousMovement = false;

    /// <summary>
    /// Gets the current autonomous brain state.
    /// </summary>
    public PawnBrainState CurrentState
    {
        get { return m_currentState; }
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
    }

    /// <summary>
    /// Runs first-pass autonomous behavior checks and starts idle wandering when appropriate.
    /// </summary>
    private void Update()
    {
        if (m_pPawn != null && m_pPawn.IsDeputized)
        {
            CancelAutonomousMovementForPlayerControl();
            m_currentState = PawnBrainState.Disabled;
            ResetThinkTimer();
            return;
        }

        if (!CanRunBrain())
        {
            m_currentState = PawnBrainState.Disabled;
            ResetThinkTimer();
            return;
        }

        if (m_pPawn.IsMoving)
        {
            UpdateMovingState();
            ResetThinkTimer();
            return;
        }

        m_hasActiveAutonomousMovement = false;
        m_currentState = PawnBrainState.Idle;

        if (!m_enableIdleWandering)
        {
            ResetThinkTimer();
            return;
        }

        UpdateThinkTimer();
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
        if (m_hasActiveAutonomousMovement)
        {
            m_currentState = PawnBrainState.Wandering;
            return;
        }

        m_currentState = PawnBrainState.Moving;
    }

    /// <summary>
    /// Advances the brain think timer while the pawn is idle.
    /// When the interval elapses, the pawn attempts to start a local wander.
    /// </summary>
    private void UpdateThinkTimer()
    {
        m_thinkTimer += Time.deltaTime;

        if (m_thinkTimer < m_thinkIntervalSeconds)
        {
            return;
        }

        ResetThinkTimer();
        TryStartLocalWander();
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
    /// Attempts to find a nearby valid tile that this pawn can use as a local wander target.
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

        for (int i = 0; i < safeAttemptCount; i++)
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
                "PawnBrain could not find local wander target for " + m_pPawn.PawnId,
                this);
        }

        return false;
    }

    /// <summary>
    /// Returns whether the candidate coordinate is valid enough to be considered for local wandering.
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
            if (m_hasActiveAutonomousMovement)
            {
                return "Wandering";
            }

            return "Moving";
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