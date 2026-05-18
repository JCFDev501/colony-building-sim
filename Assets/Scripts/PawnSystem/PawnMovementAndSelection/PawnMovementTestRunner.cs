using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// helper that sends the current anchor pawn along
/// a computed path to the currently resolved preview destination.
/// This is used to validate smooth path-following before full command execution is added.
/// </summary>
public class PawnMovementTestRunner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PawnCommandManager m_pawnCommandManager;
    [SerializeField] private PawnMovePreviewManager m_pawnMovePreviewManager;
    [SerializeField] private PawnPathfinder m_pawnPathfinder;
    [SerializeField] private GridManager m_gridManager;

    [Header("Debug")]
    [SerializeField] private bool m_enableDebugLogs = true;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.tKey.wasPressedThisFrame)
        {
            return;
        }

        if (m_enableDebugLogs)
        {
            Debug.Log("PawnMovementTestRunner: T key pressed.", this);
        }

        TryRunAnchorMovementTest();
    }

    /// <summary>
    /// Attempts to generate a path for the current anchor pawn and start smooth movement.
    /// </summary>
    private void TryRunAnchorMovementTest()
    {
        if (m_pawnCommandManager == null)
        {
            Debug.LogWarning("Movement test failed: PawnCommandManager reference is missing.", this);
            return;
        }

        if (m_pawnMovePreviewManager == null)
        {
            Debug.LogWarning("Movement test failed: PawnMovePreviewManager reference is missing.", this);
            return;
        }

        if (m_pawnPathfinder == null)
        {
            Debug.LogWarning("Movement test failed: PawnPathfinder reference is missing.", this);
            return;
        }

        if (m_gridManager == null)
        {
            Debug.LogWarning("Movement test failed: GridManager reference is missing.", this);
            return;
        }

        if (!m_pawnCommandManager.TryGetAnchorPawn(out Pawn anchorPawn))
        {
            Debug.Log("Movement test failed: no anchor pawn.", this);
            return;
        }

        if (anchorPawn == null)
        {
            Debug.Log("Movement test failed: anchor pawn reference was null.", this);
            return;
        }

        if (m_enableDebugLogs)
        {
            Debug.Log("Movement test: anchor pawn = " + anchorPawn.PawnId, anchorPawn);
        }

        if (!m_pawnMovePreviewManager.TryGetCurrentResolvedPreviewTile(out Vector2Int resolvedPreviewTile))
        {
            Debug.Log("Movement test failed: no resolved preview tile.", this);
            return;
        }

        if (m_enableDebugLogs)
        {
            Debug.Log("Movement test: resolved preview tile = " + resolvedPreviewTile, this);
        }

        Vector2Int pathStartCoordinates = anchorPawn.GetPathStartCoordinates();

        if (!m_pawnPathfinder.TryFindPath(
                anchorPawn,
                m_gridManager,
                pathStartCoordinates,
                resolvedPreviewTile,
                out List<Vector2Int> path))
        {
            Debug.Log("Movement test failed: no path found from " +
                pathStartCoordinates + " to " + resolvedPreviewTile + ".", this);
            return;
        }

        if (m_enableDebugLogs)
        {
            Debug.Log("Movement test: path found with " + path.Count + " steps.", this);
        }

        if (!anchorPawn.TryStartPathMovement(path))
        {
            Debug.Log("Movement test failed: pawn rejected movement start.", anchorPawn);
            return;
        }

        Debug.Log("Movement test started for " + anchorPawn.PawnId + " with path length " + path.Count, anchorPawn);
    }
}