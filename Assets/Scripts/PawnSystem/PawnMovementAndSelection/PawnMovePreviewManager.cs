using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the candidate anchor move preview for the prototype.
/// The preview only appears when a valid commanded pawn group exists,
/// right-click preview mode is active, and the hovered tile resolves to a valid preview tile.
/// Invalid hovered tiles can resolve to the nearest valid fallback tile.
///
/// During a right-click hold, the first valid resolved anchor tile is locked in place.
/// Mouse movement after that point changes only the group arrangement preference,
/// not the anchor preview position.
/// </summary>
public class PawnMovePreviewManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private PawnCommandManager m_pawnCommandManager;
    [SerializeField] private GridManager m_gridManager;
    [SerializeField] private GameObject m_movePreviewHighlight;
    [SerializeField] private Renderer m_movePreviewRenderer;

    [Header("Preview Materials")]
    [SerializeField] private Material m_validPreviewMaterial;
    [SerializeField] private Material m_invalidPreviewMaterial;

    [Header("Preview Settings")]
    [SerializeField] private float m_previewYOffset = 0.03f;
    [SerializeField] private int m_maxFallbackSearchDistance = 8;

    private bool m_hasResolvedPreviewTile = false;
    private Vector2Int m_resolvedPreviewTileCoordinates = Vector2Int.zero;

    private bool m_hasLastResolvedPreviewTile = false;
    private Vector2Int m_lastResolvedPreviewTileCoordinates = Vector2Int.zero;

    private bool m_hasLockedPreviewTile = false;
    private Vector2Int m_lockedPreviewTileCoordinates = Vector2Int.zero;

    private Vector2Int m_currentArrangementDirection = Vector2Int.zero;
    private Vector2Int m_lastArrangementDirection = Vector2Int.zero;

    private void Update()
    {
        UpdateMovePreview();
    }

    /// <summary>
    /// Attempts to return the current resolved preview tile.
    /// If the visible preview is no longer active this frame, falls back to the
    /// last valid resolved preview tile so move execution can still use release-frame data.
    /// </summary>
    public bool TryGetCurrentResolvedPreviewTile(out Vector2Int resolvedPreviewTileCoordinates)
    {
        if (m_hasResolvedPreviewTile)
        {
            resolvedPreviewTileCoordinates = m_resolvedPreviewTileCoordinates;
            return true;
        }

        if (m_hasLastResolvedPreviewTile)
        {
            resolvedPreviewTileCoordinates = m_lastResolvedPreviewTileCoordinates;
            return true;
        }

        resolvedPreviewTileCoordinates = Vector2Int.zero;
        return false;
    }

    /// <summary>
    /// Attempts to return the current arrangement direction preference.
    /// Falls back to the last valid arrangement direction so move execution
    /// can match the final visible preview on right-click release.
    /// </summary>
    public bool TryGetCurrentArrangementDirection(out Vector2Int arrangementDirection)
    {
        if (m_hasResolvedPreviewTile)
        {
            arrangementDirection = m_currentArrangementDirection;
            return true;
        }

        if (m_hasLastResolvedPreviewTile)
        {
            arrangementDirection = m_lastArrangementDirection;
            return true;
        }

        arrangementDirection = Vector2Int.zero;
        return false;
    }

    /// <summary>
    /// Clears cached preview data after a move command has been consumed successfully.
    /// </summary>
    public void ClearLastResolvedPreviewTile()
    {
        m_hasLastResolvedPreviewTile = false;
        m_lastResolvedPreviewTileCoordinates = Vector2Int.zero;
        m_lastArrangementDirection = Vector2Int.zero;
    }

    /// <summary>
    /// Updates the anchor move preview based on the current hover state
    /// and whether a valid commanded pawn group exists.
    ///
    /// The anchor preview locks to the first valid resolved tile found during
    /// a right-click hold. Mouse movement after that only updates arrangement direction.
    /// </summary>
    private void UpdateMovePreview()
    {
        m_hasResolvedPreviewTile = false;
        m_resolvedPreviewTileCoordinates = Vector2Int.zero;
        m_currentArrangementDirection = Vector2Int.zero;

        if (m_playerController == null ||
            m_pawnCommandManager == null ||
            m_gridManager == null ||
            m_movePreviewHighlight == null)
        {
            ResetCurrentPreviewHoldState();
            HidePreview();
            return;
        }

        if (!m_playerController.IsMoveCommandPreviewActive)
        {
            ResetCurrentPreviewHoldState();
            HidePreview();
            return;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ResetCurrentPreviewHoldState();
        }

        if (!m_pawnCommandManager.HasCommandedPawns())
        {
            HidePreview();
            return;
        }

        if (!m_pawnCommandManager.TryGetAnchorPawn(out Pawn anchorPawn))
        {
            HidePreview();
            return;
        }

        if (m_hasLockedPreviewTile)
        {
            m_hasResolvedPreviewTile = true;
            m_resolvedPreviewTileCoordinates = m_lockedPreviewTileCoordinates;
            m_currentArrangementDirection = GetArrangementDirectionFromCurrentHover(m_lockedPreviewTileCoordinates);
            m_lastArrangementDirection = m_currentArrangementDirection;

            ShowResolvedPreview(m_lockedPreviewTileCoordinates);
            return;
        }

        if (!m_playerController.HasHoveredTile)
        {
            HidePreview();
            return;
        }

        Vector2Int hoveredTileCoordinates = m_playerController.HoveredTileCoordinates;

        if (TryResolvePreviewTile(anchorPawn, hoveredTileCoordinates, out Vector2Int resolvedTileCoordinates))
        {
            m_hasLockedPreviewTile = true;
            m_lockedPreviewTileCoordinates = resolvedTileCoordinates;

            m_hasResolvedPreviewTile = true;
            m_resolvedPreviewTileCoordinates = resolvedTileCoordinates;

            m_hasLastResolvedPreviewTile = true;
            m_lastResolvedPreviewTileCoordinates = resolvedTileCoordinates;

            m_currentArrangementDirection = GetArrangementDirectionFromCurrentHover(resolvedTileCoordinates);
            m_lastArrangementDirection = m_currentArrangementDirection;

            ShowResolvedPreview(resolvedTileCoordinates);
            return;
        }

        ShowInvalidPreview(hoveredTileCoordinates);
    }

    /// <summary>
    /// Attempts to resolve the hovered tile to a valid preview tile.
    /// Valid hovered tiles remain unchanged. Invalid hovered tiles search
    /// outward for the nearest valid fallback tile.
    /// </summary>
    private bool TryResolvePreviewTile(Pawn anchorPawn, Vector2Int hoveredTileCoordinates, out Vector2Int resolvedTileCoordinates)
    {
        resolvedTileCoordinates = hoveredTileCoordinates;

        if (!m_gridManager.IsInBounds(hoveredTileCoordinates))
        {
            return false;
        }

        if (IsValidPreviewTile(anchorPawn, hoveredTileCoordinates))
        {
            resolvedTileCoordinates = hoveredTileCoordinates;
            return true;
        }

        return TryFindNearestValidFallbackTile(anchorPawn, hoveredTileCoordinates, out resolvedTileCoordinates);
    }

    /// <summary>
    /// Returns whether the tile is currently valid for preview use.
    /// This uses pawn-aware tile checks so the moving pawn can treat its own
    /// current tile and reserved next tile as valid.
    /// </summary>
    private bool IsValidPreviewTile(Pawn anchorPawn, Vector2Int tileCoordinates)
    {
        if (anchorPawn == null)
        {
            return false;
        }

        return anchorPawn.CanTreatTileAsEnterable(tileCoordinates);
    }

    /// <summary>
    /// Searches outward in square rings for the nearest valid fallback tile.
    /// Returns the first valid tile found at the nearest search distance.
    /// </summary>
    private bool TryFindNearestValidFallbackTile(Pawn anchorPawn, Vector2Int originCoordinates, out Vector2Int fallbackCoordinates)
    {
        fallbackCoordinates = Vector2Int.zero;

        for (int distance = 1; distance <= m_maxFallbackSearchDistance; distance++)
        {
            if (TryFindValidTileAtDistance(anchorPawn, originCoordinates, distance, out fallbackCoordinates))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Searches the perimeter of a square ring at the given distance from the origin.
    /// </summary>
    private bool TryFindValidTileAtDistance(Pawn anchorPawn, Vector2Int originCoordinates, int distance, out Vector2Int validCoordinates)
    {
        validCoordinates = Vector2Int.zero;

        int minX = originCoordinates.x - distance;
        int maxX = originCoordinates.x + distance;
        int minY = originCoordinates.y - distance;
        int maxY = originCoordinates.y + distance;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                bool isOnPerimeter =
                    x == minX ||
                    x == maxX ||
                    y == minY ||
                    y == maxY;

                if (!isOnPerimeter)
                {
                    continue;
                }

                Vector2Int candidateCoordinates = new Vector2Int(x, y);

                if (!IsValidPreviewTile(anchorPawn, candidateCoordinates))
                {
                    continue;
                }

                validCoordinates = candidateCoordinates;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns an 8-way arrangement direction from the locked anchor tile
    /// to the currently hovered tile. If no useful direction exists, returns zero.
    /// </summary>
    private Vector2Int GetArrangementDirectionFromCurrentHover(Vector2Int anchorCoordinates)
    {
        if (m_playerController == null || !m_playerController.HasHoveredTile)
        {
            return Vector2Int.zero;
        }

        Vector2Int hoveredTileCoordinates = m_playerController.HoveredTileCoordinates;
        Vector2Int delta = hoveredTileCoordinates - anchorCoordinates;

        if (delta == Vector2Int.zero)
        {
            return Vector2Int.zero;
        }

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        if (angle < 0.0f)
        {
            angle += 360.0f;
        }

        if (angle >= 337.5f || angle < 22.5f)
        {
            return Vector2Int.right;
        }

        if (angle < 67.5f)
        {
            return new Vector2Int(1, 1);
        }

        if (angle < 112.5f)
        {
            return Vector2Int.up;
        }

        if (angle < 157.5f)
        {
            return new Vector2Int(-1, 1);
        }

        if (angle < 202.5f)
        {
            return Vector2Int.left;
        }

        if (angle < 247.5f)
        {
            return new Vector2Int(-1, -1);
        }

        if (angle < 292.5f)
        {
            return Vector2Int.down;
        }

        return new Vector2Int(1, -1);
    }

    /// <summary>
    /// Clears current hold-only preview state while preserving cached
    /// last-frame preview data for release-frame move execution.
    /// </summary>
    private void ResetCurrentPreviewHoldState()
    {
        m_hasLockedPreviewTile = false;
        m_lockedPreviewTileCoordinates = Vector2Int.zero;
        m_hasResolvedPreviewTile = false;
        m_resolvedPreviewTileCoordinates = Vector2Int.zero;
        m_currentArrangementDirection = Vector2Int.zero;
    }

    /// <summary>
    /// Shows the preview object at the resolved anchor tile.
    /// </summary>
    private void ShowResolvedPreview(Vector2Int resolvedTileCoordinates)
    {
        Vector3 previewWorldPosition = m_gridManager.GetWorldPosition(resolvedTileCoordinates);
        previewWorldPosition.y += m_previewYOffset;

        m_movePreviewHighlight.transform.position = previewWorldPosition;
        m_movePreviewHighlight.SetActive(true);
        UpdatePreviewMaterial(true);
    }

    /// <summary>
    /// Shows the preview object at the currently hovered tile in an invalid state.
    /// </summary>
    private void ShowInvalidPreview(Vector2Int hoveredTileCoordinates)
    {
        Vector3 invalidPreviewWorldPosition = m_gridManager.GetWorldPosition(hoveredTileCoordinates);
        invalidPreviewWorldPosition.y += m_previewYOffset;

        m_movePreviewHighlight.transform.position = invalidPreviewWorldPosition;
        m_movePreviewHighlight.SetActive(true);
        UpdatePreviewMaterial(false);
    }

    /// <summary>
    /// Hides the preview object if it exists.
    /// </summary>
    private void HidePreview()
    {
        if (m_movePreviewHighlight == null)
        {
            return;
        }

        m_movePreviewHighlight.SetActive(false);
    }

    /// <summary>
    /// Updates the preview material to show either a valid or invalid state.
    /// </summary>
    private void UpdatePreviewMaterial(bool isValidPreview)
    {
        if (m_movePreviewRenderer == null)
        {
            return;
        }

        if (isValidPreview)
        {
            if (m_validPreviewMaterial != null)
            {
                m_movePreviewRenderer.material = m_validPreviewMaterial;
            }

            return;
        }

        if (m_invalidPreviewMaterial != null)
        {
            m_movePreviewRenderer.material = m_invalidPreviewMaterial;
        }
    }
}