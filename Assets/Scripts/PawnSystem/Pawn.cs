using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the minimum pawn data needed for prototype selection,
/// command, and movement foundation work.
/// </summary>
public class Pawn : MonoBehaviour
{
    [Header("Pawn Identity")]
    [SerializeField] private string m_pawnId = "Pawn";

    [Header("Pawn State")]
    [SerializeField] private bool m_isSelected = false;
    [SerializeField] private bool m_isDeputized = false;

    [Header("Pawn Grid Position")]
    [SerializeField] private Vector2Int m_gridCoordinate = Vector2Int.zero;

    [Header("Pawn Setup")]
    [SerializeField] private bool m_snapToGridOnAwake = true;
    [SerializeField] private float m_visualHeightOffset = 0.5f;

    [Header("Pawn Movement")]
    [SerializeField] private float m_moveSpeed = 3.0f;
    [SerializeField] private float m_arrivalThreshold = 0.02f;

    [Header("Pawn Visuals")]
    [SerializeField] private GameObject m_stateIndicator;
    [SerializeField] private Renderer m_stateIndicatorRenderer;
    [SerializeField] private Material m_selectedMaterial;
    [SerializeField] private Material m_deputizedMaterial;
    [SerializeField] private Material m_selectedAndDeputizedMaterial;

    [Header("Destination Preview")]
    [SerializeField] private GameObject m_destinationPreviewHighlight;
    [SerializeField] private Renderer m_destinationPreviewRenderer;
    [SerializeField] private Material m_destinationPreviewMaterial;
    [SerializeField] private Material m_anchorDestinationPreviewMaterial;

    private PawnManager m_pPawnManager;
    private GridManager m_pGridManager;
    private PawnPathfinder m_pPawnPathfinder;
    private Coroutine m_pStartupOccupancyRoutine;

    private readonly List<Vector2Int> m_activePath = new();
    private int m_currentPathIndex = -1;
    private bool m_isMoving = false;

    private bool m_hasMovementDestination = false;
    private Vector2Int m_movementDestinationCoordinates = Vector2Int.zero;

    private bool m_hasOccupiedTile = false;
    private Vector2Int m_occupiedTileCoordinates = Vector2Int.zero;

    /// <summary>
    /// Gets the pawn's ID/reference string.
    /// </summary>
    public string PawnId
    {
        get { return m_pawnId; }
    }

    /// <summary>
    /// Gets whether this pawn is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get { return m_isSelected; }
    }

    /// <summary>
    /// Gets whether this pawn is currently deputized.
    /// </summary>
    public bool IsDeputized
    {
        get { return m_isDeputized; }
    }

    /// <summary>
    /// Gets the pawn's current grid coordinate.
    /// </summary>
    public Vector2Int GridCoordinate
    {
        get { return m_gridCoordinate; }
    }

    /// <summary>
    /// Gets the pawn's current world position.
    /// </summary>
    public Vector3 WorldPosition
    {
        get { return transform.position; }
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

    private void Awake()
    {
        m_pPawnManager = FindFirstObjectByType<PawnManager>();
        m_pGridManager = FindFirstObjectByType<GridManager>();
        m_pPawnPathfinder = FindFirstObjectByType<PawnPathfinder>();

        if (m_snapToGridOnAwake)
        {
            UpdateGridCoordinateFromWorldPosition();
            SnapToGridCoordinate();
        }

        HideDestinationPreview();
        UpdateStateIndicator();
    }

    private void OnEnable()
    {
        if (m_pPawnManager == null)
        {
            m_pPawnManager = FindFirstObjectByType<PawnManager>();
        }

        if (m_pGridManager == null)
        {
            m_pGridManager = FindFirstObjectByType<GridManager>();
        }

        if (m_pPawnPathfinder == null)
        {
            m_pPawnPathfinder = FindFirstObjectByType<PawnPathfinder>();
        }

        if (m_pPawnManager != null)
        {
            m_pPawnManager.RegisterPawn(this);
        }

        if (m_pStartupOccupancyRoutine != null)
        {
            StopCoroutine(m_pStartupOccupancyRoutine);
        }

        m_pStartupOccupancyRoutine = StartCoroutine(InitializeOccupancyWhenGridIsReady());

        HideDestinationPreview();
        UpdateStateIndicator();
    }

    private void OnDisable()
    {
        if (m_pStartupOccupancyRoutine != null)
        {
            StopCoroutine(m_pStartupOccupancyRoutine);
            m_pStartupOccupancyRoutine = null;
        }

        StopMovement();
        HideDestinationPreview();
        ClearOccupiedTile();

        if (m_pPawnManager != null)
        {
            m_pPawnManager.UnregisterPawn(this);
        }
    }

    private void Update()
    {
        UpdateMovement();
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
    /// Sets whether this pawn is selected.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        m_isSelected = isSelected;
        UpdateStateIndicator();
    }

    /// <summary>
    /// Sets whether this pawn is deputized.
    /// </summary>
    public void SetDeputized(bool isDeputized)
    {
        m_isDeputized = isDeputized;
        UpdateStateIndicator();
    }

    /// <summary>
    /// Sets the pawn's current grid coordinate.
    /// This does not yet handle movement transitions. It only updates the stored coordinate.
    /// </summary>
    public void SetGridCoordinate(Vector2Int gridCoordinate)
    {
        m_gridCoordinate = gridCoordinate;
    }

    /// <summary>
    /// Shows the pawn's destination preview highlight at the provided world position.
    /// Uses anchor styling when requested.
    /// </summary>
    public void ShowDestinationPreview(Vector3 worldPosition, bool isAnchorPreview)
    {
        if (m_destinationPreviewHighlight == null)
        {
            return;
        }

        if (m_destinationPreviewRenderer != null)
        {
            if (isAnchorPreview)
            {
                if (m_anchorDestinationPreviewMaterial != null)
                {
                    m_destinationPreviewRenderer.material = m_anchorDestinationPreviewMaterial;
                }
            }
            else
            {
                if (m_destinationPreviewMaterial != null)
                {
                    m_destinationPreviewRenderer.material = m_destinationPreviewMaterial;
                }
            }
        }

        m_destinationPreviewHighlight.transform.position = worldPosition;
        m_destinationPreviewHighlight.SetActive(true);
    }

    /// <summary>
    /// Hides the pawn's destination preview highlight.
    /// </summary>
    public void HideDestinationPreview()
    {
        if (m_destinationPreviewHighlight == null)
        {
            return;
        }

        m_destinationPreviewHighlight.SetActive(false);
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
    /// Updates smooth movement along the active path.
    /// In this prototype pass, pawns do not reserve tiles and do not block each other.
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

        Vector2Int targetCoordinates = m_activePath[m_currentPathIndex];

        Vector3 targetWorldPosition = m_pGridManager.GetWorldPosition(targetCoordinates);
        targetWorldPosition.y += m_visualHeightOffset;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            m_moveSpeed * Time.deltaTime);

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
        if (m_pPawnPathfinder == null || m_pGridManager == null)
        {
            return false;
        }

        if (!m_hasMovementDestination)
        {
            return false;
        }

        Vector2Int repathStartCoordinates = GetPathStartCoordinates();

        if (!m_pPawnPathfinder.TryFindPath(
                this,
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

    /// <summary>
    /// Updates the state indicator visibility and material based on pawn state.
    /// Green = selected and deputized.
    /// Red = deputized only.
    /// Blue = selected only.
    /// Hidden = neither.
    /// </summary>
    private void UpdateStateIndicator()
    {
        if (m_stateIndicator == null)
        {
            return;
        }

        bool shouldShowIndicator = m_isSelected || m_isDeputized;
        m_stateIndicator.SetActive(shouldShowIndicator);

        if (!shouldShowIndicator)
        {
            return;
        }

        if (m_stateIndicatorRenderer == null)
        {
            return;
        }

        if (m_isSelected && m_isDeputized)
        {
            if (m_selectedAndDeputizedMaterial != null)
            {
                m_stateIndicatorRenderer.material = m_selectedAndDeputizedMaterial;
            }

            return;
        }

        if (m_isDeputized)
        {
            if (m_deputizedMaterial != null)
            {
                m_stateIndicatorRenderer.material = m_deputizedMaterial;
            }

            return;
        }

        if (m_isSelected)
        {
            if (m_selectedMaterial != null)
            {
                m_stateIndicatorRenderer.material = m_selectedMaterial;
            }
        }
    }
}