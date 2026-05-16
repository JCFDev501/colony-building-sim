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
    [SerializeField] private PawnProfile m_profile;

    [Header("Pawn State")]
    [SerializeField] private bool m_isSelected = false;
    [SerializeField] private bool m_isDeputized = false;

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
    private PawnMovementController m_pMovementController;
    private PawnNeedsController m_pNeedsController;

    /// <summary>
    /// Gets the pawn's ID/reference string.
    /// </summary>
    public string PawnId
    {
        get { return m_pawnId; }
    }

    /// <summary>
    /// Gets the generated pawn profile assigned to this runtime pawn.
    /// </summary>
    public PawnProfile Profile
    {
        get { return m_profile; }
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
    /// Gets the pawn's current movement speed multiplier from need state.
    /// </summary>
    public float NeedMoveSpeedMultiplier
    {
        get
        {
            EnsureNeedsController();

            if (m_pNeedsController == null)
            {
                return 1.0f;
            }

            return m_pNeedsController.NeedMoveSpeedMultiplier;
        }
    }

    /// <summary>
    /// Gets the pawn's current work speed multiplier from need state.
    /// </summary>
    public float NeedWorkSpeedMultiplier
    {
        get
        {
            EnsureNeedsController();

            if (m_pNeedsController == null)
            {
                return 1.0f;
            }

            return m_pNeedsController.NeedWorkSpeedMultiplier;
        }
    }
    
    /// <summary>
    /// Gets whether this pawn should try to eat based on its current Food need.
    /// </summary>
    public bool ShouldEat
    {
        get
        {
            EnsureNeedsController();

            if (m_pNeedsController == null)
            {
                return false;
            }

            return m_pNeedsController.ShouldEat;
        }
    }

    /// <summary>
    /// Gets whether this pawn's Food need is critically low.
    /// </summary>
    public bool IsFoodCritical
    {
        get
        {
            EnsureNeedsController();

            if (m_pNeedsController == null)
            {
                return false;
            }

            return m_pNeedsController.IsFoodCritical;
        }
    }

    /// <summary>
    /// Gets whether this pawn should try to sleep based on its current Sleep need.
    /// </summary>
    public bool ShouldSleep
    {
        get
        {
            EnsureNeedsController();

            if (m_pNeedsController == null)
            {
                return false;
            }

            return m_pNeedsController.ShouldSleep;
        }
    }

    /// <summary>
    /// Gets whether this pawn's Sleep need is critically low.
    /// </summary>
    public bool IsSleepCritical
    {
        get
        {
            EnsureNeedsController();

            if (m_pNeedsController == null)
            {
                return false;
            }

            return m_pNeedsController.IsSleepCritical;
        }
    }

    /// <summary>
    /// Gets the pawn's current grid coordinate.
    /// </summary>
    public Vector2Int GridCoordinate
    {
        get
        {
            EnsureMovementController();

            if (m_pMovementController == null)
            {
                return Vector2Int.zero;
            }

            return m_pMovementController.GridCoordinate;
        }
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
        get
        {
            EnsureMovementController();

            if (m_pMovementController == null)
            {
                return false;
            }

            return m_pMovementController.IsMoving;
        }
    }

    /// <summary>
    /// Returns whether the pawn currently has a tracked movement destination.
    /// </summary>
    public bool HasMovementDestination
    {
        get
        {
            EnsureMovementController();

            if (m_pMovementController == null)
            {
                return false;
            }

            return m_pMovementController.HasMovementDestination;
        }
    }

    /// <summary>
    /// Gets the pawn's current tracked movement destination.
    /// Only meaningful when HasMovementDestination is true.
    /// </summary>
    public Vector2Int MovementDestinationCoordinates
    {
        get
        {
            EnsureMovementController();

            if (m_pMovementController == null)
            {
                return Vector2Int.zero;
            }

            return m_pMovementController.MovementDestinationCoordinates;
        }
    }
    
    /// <summary>
    /// Restores this pawn's Food need to the configured target value.
    /// </summary>
    public void RestoreFoodToTarget()
    {
        EnsureNeedsController();

        if (m_pNeedsController == null)
        {
            return;
        }

        m_pNeedsController.RestoreFoodToTarget();
    }

    /// <summary>
    /// Restores this pawn's Sleep need to the configured target value.
    /// </summary>
    public void RestoreSleepToTarget()
    {
        EnsureNeedsController();

        if (m_pNeedsController == null)
        {
            return;
        }

        m_pNeedsController.RestoreSleepToTarget();
    }
    
    /// <summary>
    /// Starts this pawn's gradual Sleep recovery.
    /// </summary>
    public void BeginSleepRecovery()
    {
        EnsureNeedsController();

        if (m_pNeedsController == null)
        {
            return;
        }

        m_pNeedsController.BeginSleepRecovery();
    }

    /// <summary>
    /// Stops this pawn's gradual Sleep recovery.
    /// </summary>
    public void EndSleepRecovery()
    {
        EnsureNeedsController();

        if (m_pNeedsController == null)
        {
            return;
        }

        m_pNeedsController.EndSleepRecovery();
    }

    /// <summary>
    /// Recovers this pawn's Sleep need based on in-game minutes slept.
    /// Returns true when the sleep target has been reached.
    /// </summary>
    public bool RecoverSleepForGameMinutes(float gameMinutes)
    {
        EnsureNeedsController();

        if (m_pNeedsController == null)
        {
            return false;
        }

        return m_pNeedsController.RecoverSleepForGameMinutes(gameMinutes);
    }

    private void Awake()
    {
        m_pPawnManager = FindFirstObjectByType<PawnManager>();

        EnsureMovementController();
        EnsureNeedsController();

        if (m_pMovementController != null)
        {
            m_pMovementController.Initialize(this);
            m_pMovementController.HandlePawnAwake();
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

        EnsureMovementController();
        EnsureNeedsController();

        if (m_pPawnManager != null)
        {
            m_pPawnManager.RegisterPawn(this);
        }

        if (m_pMovementController != null)
        {
            m_pMovementController.Initialize(this);
            m_pMovementController.HandlePawnEnabled();
        }

        HideDestinationPreview();
        UpdateStateIndicator();
    }

    private void OnDisable()
    {
        if (m_pMovementController != null)
        {
            m_pMovementController.HandlePawnDisabled();
        }

        HideDestinationPreview();

        if (m_pPawnManager != null)
        {
            m_pPawnManager.UnregisterPawn(this);
        }
    }

    private void Update()
    {
        if (m_pMovementController != null)
        {
            m_pMovementController.HandlePawnUpdate();
        }
    }

    /// <summary>
    /// Assigns generated profile data to this runtime pawn.
    /// This does not generate profile data and does not modify the generation pipeline.
    /// </summary>
    public void InitializeFromProfile(PawnProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        m_profile = profile;
        m_pawnId = profile.PawnId;

        EnsureMovementController();
        EnsureNeedsController();

        if (m_pMovementController != null)
        {
            m_pMovementController.SetMoveSpeed(profile.FinalMoveSpeed);
        }

        if (!string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            Debug.Log("Initialized pawn from profile: " + profile.DisplayName, this);
        }
    }

    /// <summary>
    /// Starts movement along the provided path by forwarding to the movement controller.
    /// </summary>
    public bool TryStartPathMovement(List<Vector2Int> path)
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return false;
        }

        return m_pMovementController.TryStartPathMovement(path);
    }

    /// <summary>
    /// Stops the current path movement by forwarding to the movement controller.
    /// </summary>
    public void StopMovement()
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return;
        }

        m_pMovementController.StopMovement();
    }

    /// <summary>
    /// Cancels current movement without snapping the pawn to a tile center.
    /// </summary>
    public void CancelMovementInPlace()
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return;
        }

        m_pMovementController.CancelMovementInPlace();
    }

    /// <summary>
    /// Updates the pawn's grid coordinate using its current world position.
    /// </summary>
    public void UpdateGridCoordinateFromWorldPosition()
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return;
        }

        m_pMovementController.UpdateGridCoordinateFromWorldPosition();
    }

    /// <summary>
    /// Snaps the pawn to the world-space center of its current grid coordinate.
    /// </summary>
    public void SnapToGridCoordinate()
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return;
        }

        m_pMovementController.SnapToGridCoordinate();
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
    /// Sets the pawn's current grid coordinate through the movement controller.
    /// </summary>
    public void SetGridCoordinate(Vector2Int gridCoordinate)
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return;
        }

        m_pMovementController.SetGridCoordinate(gridCoordinate);
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
    /// </summary>
    public bool IsCurrentOccupiedTile(Vector2Int tileCoordinates)
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return false;
        }

        return m_pMovementController.IsCurrentOccupiedTile(tileCoordinates);
    }

    /// <summary>
    /// Returns whether the provided tile is this pawn's currently reserved next tile.
    /// </summary>
    public bool IsReservedNextTile(Vector2Int tileCoordinates)
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return false;
        }

        return m_pMovementController.IsReservedNextTile(tileCoordinates);
    }

    /// <summary>
    /// Returns whether this pawn can treat the tile as enterable for preview/path purposes.
    /// </summary>
    public bool CanTreatTileAsEnterable(Vector2Int tileCoordinates)
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return false;
        }

        return m_pMovementController.CanTreatTileAsEnterable(tileCoordinates);
    }

    /// <summary>
    /// Returns the best tile to use as the starting point for a new path while moving.
    /// </summary>
    public Vector2Int GetPathStartCoordinates()
    {
        EnsureMovementController();

        if (m_pMovementController == null)
        {
            return Vector2Int.zero;
        }

        return m_pMovementController.GetPathStartCoordinates();
    }

    /// <summary>
    /// Ensures this pawn has a movement controller component available.
    /// Existing prefabs are supported by adding the component at runtime when missing.
    /// </summary>
    private void EnsureMovementController()
    {
        if (m_pMovementController != null)
        {
            return;
        }

        m_pMovementController = GetComponent<PawnMovementController>();

        if (m_pMovementController == null)
        {
            m_pMovementController = gameObject.AddComponent<PawnMovementController>();
        }
    }

    /// <summary>
    /// Ensures this pawn has a needs controller component available.
    /// Existing prefabs are supported by adding the component at runtime when missing.
    /// </summary>
    private void EnsureNeedsController()
    {
        if (m_pNeedsController != null)
        {
            return;
        }

        m_pNeedsController = GetComponent<PawnNeedsController>();

        if (m_pNeedsController == null)
        {
            m_pNeedsController = gameObject.AddComponent<PawnNeedsController>();
        }
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