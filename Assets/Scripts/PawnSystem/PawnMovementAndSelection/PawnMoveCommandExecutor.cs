using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Connects preview, destination assignment, pathfinding, and pawn movement
/// into a single executable move command.
/// </summary>
public class PawnMoveCommandExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PawnCommandManager m_pawnCommandManager;
    [SerializeField] private PawnMovePreviewManager m_pawnMovePreviewManager;
    [SerializeField] private PawnDestinationAssignmentManager m_pawnDestinationAssignmentManager;
    [SerializeField] private PawnPathfinder m_pawnPathfinder;
    [SerializeField] private GridManager m_gridManager;

    [Header("Debug")]
    [SerializeField] private bool m_logCommandExecution = true;

    /// <summary>
    /// Attempts to execute a move command using the current preview state.
    /// Returns true if at least the anchor pawn successfully starts moving.
    /// </summary>
    public bool TryExecuteMoveCommand()
    {
        if (m_pawnCommandManager == null ||
            m_pawnMovePreviewManager == null ||
            m_pawnDestinationAssignmentManager == null ||
            m_pawnPathfinder == null ||
            m_gridManager == null)
        {
            if (m_logCommandExecution)
            {
                Debug.LogWarning("Move command failed: missing required references.", this);
            }

            return false;
        }

        List<Pawn> commandedPawns = m_pawnCommandManager.GetCommandedPawns();

        if (commandedPawns.Count == 0)
        {
            if (m_logCommandExecution)
            {
                Debug.Log("Move command failed: no commanded pawns.", this);
            }

            return false;
        }

        if (!m_pawnMovePreviewManager.TryGetCurrentResolvedPreviewTile(out Vector2Int resolvedAnchorTile))
        {
            if (m_logCommandExecution)
            {
                Debug.Log("Move command failed: no resolved preview tile.", this);
            }

            return false;
        }

        m_pawnMovePreviewManager.TryGetCurrentArrangementDirection(out Vector2Int arrangementDirection);

        if (!m_pawnDestinationAssignmentManager.TryAssignDestinations(
                commandedPawns,
                resolvedAnchorTile,
                arrangementDirection,
                m_gridManager,
                out Dictionary<Pawn, Vector2Int> assignments))
        {
            if (m_logCommandExecution)
            {
                Debug.Log("Move command failed: destination assignment failed.", this);
            }

            return false;
        }

        Pawn anchorPawn = commandedPawns[0];

        if (!assignments.TryGetValue(anchorPawn, out Vector2Int anchorDestination))
        {
            if (m_logCommandExecution)
            {
                Debug.Log("Move command failed: anchor pawn did not receive an assignment.", this);
            }

            return false;
        }

        if (!m_pawnPathfinder.TryFindPath(
                anchorPawn,
                commandedPawns,
                m_gridManager,
                anchorPawn.GetPathStartCoordinates(),
                anchorDestination,
                out List<Vector2Int> anchorPath))
        {
            if (m_logCommandExecution)
            {
                Debug.Log("Move command failed: anchor pawn could not path to destination.", anchorPawn);
            }

            return false;
        }

        if (!anchorPawn.TryStartPathMovement(anchorPath))
        {
            if (m_logCommandExecution)
            {
                Debug.Log("Move command failed: anchor pawn rejected movement start.", anchorPawn);
            }

            return false;
        }

        if (m_logCommandExecution)
        {
            Debug.Log("Move command: anchor pawn started moving to " + anchorDestination, anchorPawn);
        }

        for (int i = 1; i < commandedPawns.Count; i++)
        {
            Pawn pPawn = commandedPawns[i];

            if (pPawn == null)
            {
                continue;
            }

            if (!assignments.TryGetValue(pPawn, out Vector2Int assignedDestination))
            {
                if (m_logCommandExecution)
                {
                    Debug.Log("Move command partial success: pawn received no destination assignment.", pPawn);
                }

                continue;
            }

            if (!m_pawnPathfinder.TryFindPath(
                    pPawn,
                    commandedPawns,
                    m_gridManager,
                    pPawn.GetPathStartCoordinates(),
                    assignedDestination,
                    out List<Vector2Int> path))
            {
                if (m_logCommandExecution)
                {
                    Debug.Log("Move command partial success: pawn could not path to assigned destination.", pPawn);
                }

                continue;
            }

            if (!pPawn.TryStartPathMovement(path))
            {
                if (m_logCommandExecution)
                {
                    Debug.Log("Move command partial success: pawn rejected movement start.", pPawn);
                }

                continue;
            }

            if (m_logCommandExecution)
            {
                Debug.Log("Move command: pawn started moving to " + assignedDestination, pPawn);
            }
        }

        m_pawnMovePreviewManager.ClearLastResolvedPreviewTile();

        return true;
    }
}