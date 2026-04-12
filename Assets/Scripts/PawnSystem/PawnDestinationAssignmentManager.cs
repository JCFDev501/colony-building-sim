using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assigns destination tiles for the current commanded pawn group.
/// The anchor pawn receives the anchor destination first, then the remaining
/// pawns receive valid unique tiles grown outward from the current assigned cluster.
///
/// Non-anchor assignment order can be biased by an arrangement direction,
/// allowing live preview of different group layouts around the fixed anchor.
///
/// During group assignment, tiles occupied by other pawns in the same commanded
/// group are treated as temporarily usable so the group does not block itself
/// during preview and destination assignment.
/// </summary>
public class PawnDestinationAssignmentManager : MonoBehaviour
{
    private static readonly Vector2Int[] kNeighborDirections =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1),
    };

    /// <summary>
    /// Attempts to assign unique destination tiles for the commanded pawn group.
    /// Uses neutral ordering for non-anchor pawn assignment.
    /// </summary>
    public bool TryAssignDestinations(
        List<Pawn> commandedPawns,
        Vector2Int anchorDestination,
        GridManager pGridManager,
        out Dictionary<Pawn, Vector2Int> assignments)
    {
        return TryAssignDestinations(
            commandedPawns,
            anchorDestination,
            Vector2Int.zero,
            pGridManager,
            out assignments);
    }

    /// <summary>
    /// Attempts to assign unique destination tiles for the commanded pawn group.
    /// The anchor pawn must receive the anchor destination or the assignment fails.
    /// Remaining pawns are assigned valid unique tiles grown from the current
    /// assigned cluster using the provided arrangement direction as a preference.
    /// </summary>
    public bool TryAssignDestinations(
        List<Pawn> commandedPawns,
        Vector2Int anchorDestination,
        Vector2Int arrangementDirection,
        GridManager pGridManager,
        out Dictionary<Pawn, Vector2Int> assignments)
    {
        assignments = new Dictionary<Pawn, Vector2Int>();

        if (commandedPawns == null || commandedPawns.Count == 0)
        {
            return false;
        }

        if (pGridManager == null)
        {
            return false;
        }

        Pawn anchorPawn = commandedPawns[0];

        if (!IsValidAssignmentTile(anchorPawn, tileCoordinates: anchorDestination, commandedPawns, pGridManager, assignments))
        {
            return false;
        }

        assignments.Add(anchorPawn, anchorDestination);

        for (int i = 1; i < commandedPawns.Count; i++)
        {
            Pawn pPawn = commandedPawns[i];

            if (pPawn == null)
            {
                continue;
            }

            if (TryFindNextClusterTile(
                    pPawn,
                    commandedPawns,
                    anchorDestination,
                    arrangementDirection,
                    pGridManager,
                    assignments,
                    out Vector2Int assignedTile))
            {
                assignments.Add(pPawn, assignedTile);
            }
        }

        return true;
    }

    /// <summary>
    /// Returns whether the tile is currently valid for assignment.
    /// Tiles already assigned in this command are never valid.
    ///
    /// The pawn's own current tile is allowed.
    /// Tiles occupied by other pawns in the same commanded group are also allowed,
    /// because those pawns are expected to move as part of this command.
    ///
    /// Tiles occupied by pawns outside the commanded group remain blocked.
    /// </summary>
    private bool IsValidAssignmentTile(
        Pawn pPawn,
        Vector2Int tileCoordinates,
        List<Pawn> commandedPawns,
        GridManager pGridManager,
        Dictionary<Pawn, Vector2Int> assignments)
    {
        if (!pGridManager.IsInBounds(tileCoordinates))
        {
            return false;
        }

        foreach (KeyValuePair<Pawn, Vector2Int> entry in assignments)
        {
            if (entry.Value == tileCoordinates)
            {
                return false;
            }
        }

        if (pPawn != null && pPawn.GridCoordinate == tileCoordinates)
        {
            return true;
        }

        if (IsTileOccupiedByOtherCommandedPawn(tileCoordinates, pPawn, commandedPawns))
        {
            return true;
        }

        return pGridManager.CanEnterTile(tileCoordinates);
    }

    /// <summary>
    /// Returns whether the tile is currently occupied by a different pawn
    /// that is part of the same commanded group.
    /// </summary>
    private bool IsTileOccupiedByOtherCommandedPawn(
        Vector2Int tileCoordinates,
        Pawn currentPawn,
        List<Pawn> commandedPawns)
    {
        if (commandedPawns == null)
        {
            return false;
        }

        for (int i = 0; i < commandedPawns.Count; i++)
        {
            Pawn pPawn = commandedPawns[i];

            if (pPawn == null)
            {
                continue;
            }

            if (pPawn == currentPawn)
            {
                continue;
            }

            if (pPawn.GridCoordinate == tileCoordinates)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the next valid tile by expanding outward from all currently assigned
    /// destination tiles rather than searching only around the anchor.
    /// </summary>
    private bool TryFindNextClusterTile(
        Pawn pPawn,
        List<Pawn> commandedPawns,
        Vector2Int anchorDestination,
        Vector2Int arrangementDirection,
        GridManager pGridManager,
        Dictionary<Pawn, Vector2Int> assignments,
        out Vector2Int assignedTile)
    {
        assignedTile = Vector2Int.zero;

        List<Vector2Int> candidates = CollectClusterFrontierCandidates(
            pPawn,
            commandedPawns,
            pGridManager,
            assignments);

        if (candidates.Count == 0)
        {
            return false;
        }

        SortCandidatesByArrangementPreference(candidates, anchorDestination, arrangementDirection);

        assignedTile = candidates[0];
        return true;
    }

    /// <summary>
    /// Collects all unique valid candidate tiles adjacent to any currently assigned tile.
    /// This allows the destination cluster to grow from the whole assigned group,
    /// not just from the anchor tile.
    /// </summary>
    private List<Vector2Int> CollectClusterFrontierCandidates(
        Pawn pPawn,
        List<Pawn> commandedPawns,
        GridManager pGridManager,
        Dictionary<Pawn, Vector2Int> assignments)
    {
        List<Vector2Int> candidates = new();
        HashSet<Vector2Int> uniqueCandidates = new();

        foreach (KeyValuePair<Pawn, Vector2Int> entry in assignments)
        {
            Vector2Int assignedTile = entry.Value;

            for (int i = 0; i < kNeighborDirections.Length; i++)
            {
                Vector2Int candidate = assignedTile + kNeighborDirections[i];

                if (!uniqueCandidates.Add(candidate))
                {
                    continue;
                }

                if (!IsValidAssignmentTile(pPawn, candidate, commandedPawns, pGridManager, assignments))
                {
                    continue;
                }

                candidates.Add(candidate);
            }
        }

        return candidates;
    }

    /// <summary>
    /// Orders candidates so tiles more aligned with the preferred arrangement direction
    /// are tried first. When there is no preferred direction, candidates stay compact
    /// relative to the anchor.
    /// </summary>
    private void SortCandidatesByArrangementPreference(
        List<Vector2Int> candidates,
        Vector2Int anchorDestination,
        Vector2Int arrangementDirection)
    {
        bool hasPreferredDirection = arrangementDirection != Vector2Int.zero;

        Vector2 preferredDirection = Vector2.zero;
        Vector2 perpendicularDirection = Vector2.zero;

        if (hasPreferredDirection)
        {
            preferredDirection = new Vector2(arrangementDirection.x, arrangementDirection.y).normalized;
            perpendicularDirection = new Vector2(-preferredDirection.y, preferredDirection.x);
        }

        candidates.Sort((left, right) =>
        {
            Vector2 leftOffset = new Vector2(left.x - anchorDestination.x, left.y - anchorDestination.y);
            Vector2 rightOffset = new Vector2(right.x - anchorDestination.x, right.y - anchorDestination.y);

            int leftDistance = Mathf.Abs(left.x - anchorDestination.x) + Mathf.Abs(left.y - anchorDestination.y);
            int rightDistance = Mathf.Abs(right.x - anchorDestination.x) + Mathf.Abs(right.y - anchorDestination.y);

            if (hasPreferredDirection)
            {
                Vector2 leftNormalized = leftOffset.sqrMagnitude > 0.0f ? leftOffset.normalized : Vector2.zero;
                Vector2 rightNormalized = rightOffset.sqrMagnitude > 0.0f ? rightOffset.normalized : Vector2.zero;

                float leftForwardScore = Vector2.Dot(leftNormalized, preferredDirection);
                float rightForwardScore = Vector2.Dot(rightNormalized, preferredDirection);

                int forwardComparison = rightForwardScore.CompareTo(leftForwardScore);

                if (forwardComparison != 0)
                {
                    return forwardComparison;
                }

                float leftSideScore = Mathf.Abs(Vector2.Dot(leftNormalized, perpendicularDirection));
                float rightSideScore = Mathf.Abs(Vector2.Dot(rightNormalized, perpendicularDirection));

                int sideComparison = leftSideScore.CompareTo(rightSideScore);

                if (sideComparison != 0)
                {
                    return sideComparison;
                }

                int distanceComparison = leftDistance.CompareTo(rightDistance);

                if (distanceComparison != 0)
                {
                    return distanceComparison;
                }
            }
            else
            {
                int distanceComparison = leftDistance.CompareTo(rightDistance);

                if (distanceComparison != 0)
                {
                    return distanceComparison;
                }
            }

            if (left.y != right.y)
            {
                return right.y.CompareTo(left.y);
            }

            return left.x.CompareTo(right.x);
        });
    }
}