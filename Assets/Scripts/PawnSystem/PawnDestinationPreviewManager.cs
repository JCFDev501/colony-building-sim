using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows per-pawn destination preview highlights for all assigned commanded pawns.
/// Each selected and deputized pawn shows its own preview at its assigned tile,
/// but only while right-click preview mode is active.
///
/// While preview is active, the anchor destination remains fixed and non-anchor
/// preview assignments can be reordered live based on the current arrangement direction.
/// </summary>
public class PawnDestinationPreviewManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private PawnManager m_pawnManager;
    [SerializeField] private PawnCommandManager m_pawnCommandManager;
    [SerializeField] private PawnMovePreviewManager m_pawnMovePreviewManager;
    [SerializeField] private PawnDestinationAssignmentManager m_pawnDestinationAssignmentManager;
    [SerializeField] private GridManager m_gridManager;

    [Header("Preview Settings")]
    [SerializeField] private float m_markerYOffset = 0.04f;

    private void Update()
    {
        UpdateDestinationPreviewMarkers();
    }

    /// <summary>
    /// Updates the per-pawn destination preview highlights using the current
    /// commanded group, resolved anchor preview tile, and current arrangement direction.
    /// </summary>
    private void UpdateDestinationPreviewMarkers()
    {
        HideAllPawnDestinationPreviews();

        if (m_playerController == null ||
            m_pawnManager == null ||
            m_pawnCommandManager == null ||
            m_pawnMovePreviewManager == null ||
            m_pawnDestinationAssignmentManager == null ||
            m_gridManager == null)
        {
            return;
        }

        if (!m_playerController.IsMoveCommandPreviewActive)
        {
            return;
        }

        List<Pawn> commandedPawns = m_pawnCommandManager.GetCommandedPawns();

        if (commandedPawns.Count == 0)
        {
            return;
        }

        if (!m_pawnMovePreviewManager.TryGetCurrentResolvedPreviewTile(out Vector2Int resolvedAnchorTile))
        {
            return;
        }

        m_pawnMovePreviewManager.TryGetCurrentArrangementDirection(out Vector2Int arrangementDirection);

        if (!m_pawnDestinationAssignmentManager.TryAssignDestinations(
                commandedPawns,
                resolvedAnchorTile,
                arrangementDirection,
                m_gridManager,
                out Dictionary<Pawn, Vector2Int> assignments))
        {
            return;
        }

        Pawn anchorPawn = commandedPawns[0];

        foreach (KeyValuePair<Pawn, Vector2Int> entry in assignments)
        {
            Pawn pPawn = entry.Key;
            Vector2Int assignedTile = entry.Value;

            if (pPawn == null)
            {
                continue;
            }

            Vector3 previewWorldPosition = m_gridManager.GetWorldPosition(assignedTile);
            previewWorldPosition.y += m_markerYOffset;

            bool isAnchorPreview = pPawn == anchorPawn;
            pPawn.ShowDestinationPreview(previewWorldPosition, isAnchorPreview);
        }
    }

    /// <summary>
    /// Hides destination preview highlights for all active pawns.
    /// </summary>
    private void HideAllPawnDestinationPreviews()
    {
        if (m_pawnManager == null)
        {
            return;
        }

        foreach (Pawn pPawn in m_pawnManager.ActivePawns)
        {
            if (pPawn != null)
            {
                pPawn.HideDestinationPreview();
            }
        }
    }
}