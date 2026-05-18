using System.Collections;
using System.Collections.Generic;
using ColonyBuildingSim.WorldContext;
using UnityEngine;

/// <summary>
/// Controls pawn grid position, path movement, snapping, and tile occupancy.
/// This component is owned by a Pawn and is intentionally focused only on movement behavior.
/// </summary>
public class PawnMovementController : MonoBehaviour
{
    [Header("Pawn Grid Position")]
    [SerializeField] private Vector2Int m_gridCoordinate = Vector2Int.zero;

    [Header("Pawn Setup")]
    [SerializeField] private bool m_snapToGridOnAwake = true;
    [SerializeField] private float m_visualHeightOffset = 1.0f;

    [Header("Pawn Movement")]
    [SerializeField] private float m_moveSpeed = 3.0f;
    [SerializeField] private float m_arrivalThreshold = 0.02f;

    private Pawn m_pOwnerPawn;
    private GridManager m_pGridManager;
    private PawnPathfinder m_pPawnPathfinder;
    private WorldContextManager m_pWorldContextManager;
    private Coroutine m_pStartupOccupancyRoutine;

    private readonly List<Vector2Int> m_activePath = new();
    private int m_currentPathIndex = -1;
    private bool m_isMoving = false;

    private bool m_hasMovementDestination = false;
    private Vector2Int m_movementDestinationCoordinates = Vector2Int.zero;

    private bool m_hasOccupiedTile = false;
    private Vector2Int m_occupiedTileCoordinates = Vector2Int.zero;

    /// <summary>
    /// Gets the pawn's current grid coordinate.
    /// </summary>
    public Vector2Int GridCoordinate
    {
        get { return m_gridCoordinate; }
    }

    /// <summary>
    /// Returns whether the pawn is currently moving along an active path.
    /// </summary>
    public bool IsMoving
    {
        get { return m_isMoving; }
    }

    /// <summary>
    /// Returns whether the pawn currently has a tracked movement destination.
    /// </summary>
    public bool HasMovementDestination
    {
        get { return m_hasMovementDestination; }
    }

    /// <summary>
    /// Gets the pawn's current tracked movement destination.
    /// Only meaningful when HasMovementDestination is true.
    /// </summary>
    public Vector2Int MovementDestinationCoordinates
    {
        get { return m_movementDestinationCoordinates; }
    }

    /// <summary>
    /// Initializes this movement controller with its owning pawn.
    /// </summary>
    public void Initialize(Pawn ownerPawn)
    {
        m_pOwnerPawn = ownerPawn;
        CacheSceneReferences();
    }

    /// <summary>
    /// Performs movement setup that should happen during the owning pawn's Awake flow.
    /// </summary>
    public void HandlePawnAwake()
    {
        CacheOwnerPawn();
        CacheSceneReferences();

        if (m_snapToGridOnAwake)
        {
            UpdateGridCoordinateFromWorldPosition();
            SnapToGridCoordinate();
        }
    }

    /// <summary>
    /// Performs movement setup that should happen when the owning pawn is enabled.
    /// </summary>
    public void HandlePawnEnabled()
    {
        CacheOwnerPawn();
        CacheSceneReferences();

        if (m_pStartupOccupancyRoutine != null)
        {
            StopCoroutine(m_pStartupOccupancyRoutine);
        }

        m_pStartupOccupancyRoutine = StartCoroutine(InitializeOccupancyWhenGridIsReady());
    }

    /// <summary>
    /// Performs movement cleanup that should happen when the owning pawn is disabled.
    /// </summary>
    public void HandlePawnDisabled()
    {
        if (m_pStartupOccupancyRoutine != null)
        {
            StopCoroutine(m_pStartupOccupancyRoutine);
            m_pStartupOccupancyRoutine = null;
        }

        StopMovement();
        ClearOccupiedTile();
    }

    /// <summary>
    /// Updates movement behavior for the owning pawn.
    /// </summary>
    public void HandlePawnUpdate()
    {
        UpdateMovement();
    }

    /// <summary>
    /// Updates the pawn's movement speed.
    /// </summary>
    public void SetMoveSpeed(float moveSpeed)
    {
        m_moveSpeed = moveSpeed;
    }

    /// <summary>
    /// Starts movement along the provided path.
    /// The path is expected to begin at the pawn's current tile.
    /// While moving, the pawn no longer occupies any tile.
    /// </summary>
    public bool TryStartPathMovement(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
        {
            return false;
        }

        if (m_pGridManager == null)
        {
            return false;
        }

        ClearOccupiedTile();

        m_activePath.Clear();
        m_activePath.AddRange(path);

        m_currentPathIndex = 0;
        m_isMoving = true;

        m_hasMovementDestination = true;
        m_movementDestinationCoordinates = path[path.Count - 1];

        if (m_activePath.Count == 1)
        {
            StopMovement();
        }

        return true;
    }

    /// <summary>
    /// Stops the current path movement and clears movement state.
    /// Once stopped, the pawn occupies its current tile again.
    /// </summary>
    public void StopMovement()
    {
        m_activePath.Clear();
        m_currentPathIndex = -1;
        m_isMoving = false;
        m_hasMovementDestination = false;
        m_movementDestinationCoordinates = Vector2Int.zero;

        OccupyCurrentTile();
    }

    /// <summary>
    /// Cancels the current movement immediately without snapping the pawn.
    /// The pawn keeps its current world position and claims the tile under that position.
    /// </summary>
    public void CancelMovementInPlace()
    {
        m_activePath.Clear();
        m_currentPathIndex = -1;
        m_isMoving = false;
        m_hasMovementDestination = false;
        m_movementDestinationCoordinates = Vector2Int.zero;

        UpdateGridCoordinateFromWorldPosition();
        OccupyCurrentTile();
    }

    /// <summary>
    /// Updates the pawn's grid coordinate using its current world position.
    /// </summary>
    public void UpdateGridCoordinateFromWorldPosition()
    {
        if (m_pGridManager == null)
        {
            return;
        }

        if (m_pGridManager.TryGetCoordinatesFromWorldPosition(transform.position, out Vector2Int gridCoordinate))
        {
            m_gridCoordinate = gridCoordinate;
        }
    }

    /// <summary>
    /// Snaps the pawn to the world-space center of its current grid coordinate.
    /// A small vertical offset keeps the visual above the grid plane.
    /// </summary>
    public void SnapToGridCoordinate()
    {
        if (m_pGridManager == null)
        {
            return;
        }

        if (!m_pGridManager.IsInBounds(m_gridCoordinate))
        {
            return;
        }

        Vector3 worldPosition = m_pGridManager.GetWorldPosition(m_gridCoordinate);
        worldPosition.y += m_visualHeightOffset;
        transform.position = worldPosition;
    }

    /// <summary>
    /// Sets the pawn's current grid coordinate.
    /// This does not handle movement transitions. It only updates the stored coordinate.
    /// </summary>
    public void SetGridCoordinate(Vector2Int gridCoordinate)
    {
        m_gridCoordinate = gridCoordinate;
    }

    /// <summary>
    /// Returns whether the provided tile is this pawn's currently occupied tile.
    /// Occupancy only applies while the pawn is not moving.
    /// </summary>
    public bool IsCurrentOccupiedTile(Vector2Int tileCoordinates)
    {
        return m_hasOccupiedTile && m_occupiedTileCoordinates == tileCoordinates;
    }

    /// <summary>
    /// Returns whether the provided tile is this pawn's currently reserved next tile.
    /// Reservation blocking is intentionally disabled for this prototype pass.
    /// </summary>
    public bool IsReservedNextTile(Vector2Int tileCoordinates)
    {
        return false;
    }

    /// <summary>
    /// Returns whether this pawn can treat the tile as enterable for preview/path purposes.
    /// For this prototype pass, pawns can move through each other, so pawn occupancy
    /// and reservation are intentionally ignored here.
    /// </summary>
    public bool CanTreatTileAsEnterable(Vector2Int tileCoordinates)
    {
        if (m_pGridManager == null)
        {
            return false;
        }

        if (!m_pGridManager.IsInBounds(tileCoordinates))
        {
            return false;
        }

        return m_pGridManager.CanEnterTileIgnoringPawns(tileCoordinates);
    }

    /// <summary>
    /// Returns the best tile to use as the starting point for a new path while moving.
    /// </summary>
    public Vector2Int GetPathStartCoordinates()
    {
        if (m_currentPathIndex >= 0 && m_currentPathIndex < m_activePath.Count)
        {
            return m_activePath[m_currentPathIndex];
        }

        return m_gridCoordinate;
    }

    /// <summary>
    /// Caches the owning pawn if this controller has not been initialized explicitly.
    /// </summary>
    private void CacheOwnerPawn()
    {
        if (m_pOwnerPawn != null)
        {
            return;
        }

        m_pOwnerPawn = GetComponent<Pawn>();
    }

    /// <summary>
    /// Caches scene references needed for grid movement, pathfinding, and world pause state.
    /// </summary>
    private void CacheSceneReferences()
    {
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
    /// Gets delta time for pawn movement from world context when available.
    /// This keeps movement aligned with world pause and world speed.
    /// </summary>
    private float GetMovementDeltaTime()
    {
        if (m_pWorldContextManager == null)
        {
            return Time.deltaTime;
        }

        return m_pWorldContextManager.SimulationDeltaTime;
    }

    /// <summary>
    /// Gets the owner's need-based movement multiplier.
    /// </summary>
    private float GetNeedMoveSpeedMultiplier()
    {
        if (m_pOwnerPawn == null)
        {
            return 1.0f;
        }

        return m_pOwnerPawn.NeedMoveSpeedMultiplier;
    }

    /// <summary>
    /// Waits until the grid has generated tiles before syncing grid coordinate state
    /// and claiming the pawn's starting tile occupancy.
    /// </summary>
    private IEnumerator InitializeOccupancyWhenGridIsReady()
    {
        if (m_pGridManager == null)
        {
            yield break;
        }

        while (m_pGridManager.Tiles.Count == 0)
        {
            yield return null;
        }

        UpdateGridCoordinateFromWorldPosition();
        OccupyCurrentTile();
        m_pStartupOccupancyRoutine = null;
    }

    /// <summary>
    /// Updates smooth movement along the active path.
    /// pawns do not reserve tiles and do not block each other.
    /// </summary>
    private void UpdateMovement()
    {
        if (!m_isMoving)
        {
            return;
        }

        if (m_pGridManager == null)
        {
            StopMovement();
            return;
        }

        if (m_currentPathIndex < 0 || m_currentPathIndex >= m_activePath.Count)
        {
            StopMovement();
            return;
        }

        float movementDeltaTime = GetMovementDeltaTime();

        if (movementDeltaTime <= 0.0f)
        {
            return;
        }

        Vector2Int targetCoordinates = m_activePath[m_currentPathIndex];

        Vector3 targetWorldPosition = m_pGridManager.GetWorldPosition(targetCoordinates);
        targetWorldPosition.y += m_visualHeightOffset;

        float terrainSpeedMultiplier = m_pGridManager.GetTerrainMovementSpeedMultiplier(targetCoordinates);
        float needMoveSpeedMultiplier = GetNeedMoveSpeedMultiplier();

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            m_moveSpeed * terrainSpeedMultiplier * needMoveSpeedMultiplier * movementDeltaTime);

        if (Vector3.Distance(transform.position, targetWorldPosition) > m_arrivalThreshold)
        {
            return;
        }

        transform.position = targetWorldPosition;
        OnArrivedAtPathTile(targetCoordinates);

        ++m_currentPathIndex;

        if (m_currentPathIndex >= m_activePath.Count)
        {
            StopMovement();
        }
    }

    /// <summary>
    /// Attempts to rerun A* from the pawn's best current movement start tile
    /// to its tracked movement destination. Returns true if a new path was found and applied.
    /// </summary>
    private bool TryRepathToCurrentDestination()
    {
        if (m_pOwnerPawn == null || m_pPawnPathfinder == null || m_pGridManager == null)
        {
            return false;
        }

        if (!m_hasMovementDestination)
        {
            return false;
        }

        Vector2Int repathStartCoordinates = GetPathStartCoordinates();

        if (!m_pPawnPathfinder.TryFindPath(
                m_pOwnerPawn,
                m_pGridManager,
                repathStartCoordinates,
                m_movementDestinationCoordinates,
                out List<Vector2Int> repath))
        {
            return false;
        }

        if (repath.Count == 0)
        {
            return false;
        }

        m_activePath.Clear();
        m_activePath.AddRange(repath);
        m_currentPathIndex = 0;
        return true;
    }

    /// <summary>
    /// Updates pawn tile state after arriving at a path tile.
    /// While moving, the pawn updates its current coordinate but does not occupy the tile yet.
    /// </summary>
    private void OnArrivedAtPathTile(Vector2Int arrivedCoordinates)
    {
        m_gridCoordinate = arrivedCoordinates;
    }

    /// <summary>
    /// Marks the pawn's current tile as occupied, but only while stationary.
    /// </summary>
    private void OccupyCurrentTile()
    {
        if (m_pGridManager == null)
        {
            return;
        }

        if (m_isMoving)
        {
            return;
        }

        if (!m_pGridManager.IsInBounds(m_gridCoordinate))
        {
            return;
        }

        ClearOccupiedTile();

        if (!m_pGridManager.SetOccupied(m_gridCoordinate, true))
        {
            return;
        }

        m_hasOccupiedTile = true;
        m_occupiedTileCoordinates = m_gridCoordinate;
    }

    /// <summary>
    /// Clears the tile currently occupied by this pawn, if any.
    /// </summary>
    private void ClearOccupiedTile()
    {
        if (m_pGridManager == null)
        {
            return;
        }

        if (!m_hasOccupiedTile)
        {
            return;
        }

        m_pGridManager.SetOccupied(m_occupiedTileCoordinates, false);
        m_hasOccupiedTile = false;
        m_occupiedTileCoordinates = Vector2Int.zero;
    }
}