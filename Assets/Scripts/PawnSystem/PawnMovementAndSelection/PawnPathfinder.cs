using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates grid-based paths for pawns using A*.
/// This version uses the GridManager's traversable neighbor rules,
/// including 8-directional movement and diagonal corner-cut prevention,
/// while also supporting pawn-aware and command-group-aware tile validation.
/// </summary>
public class PawnPathfinder : MonoBehaviour
{
    private class PathNode
    {
        public Vector2Int Coordinates;
        public Vector2Int ParentCoordinates;
        public int GCost;
        public int HCost;
        public int FCost
        {
            get { return GCost + HCost; }
        }

        public PathNode(Vector2Int coordinates, Vector2Int parentCoordinates, int gCost, int hCost)
        {
            Coordinates = coordinates;
            ParentCoordinates = parentCoordinates;
            GCost = gCost;
            HCost = hCost;
        }
    }

    /// <summary>
    /// Attempts to generate a pawn-aware path from the start tile to the goal tile.
    /// Returns true and outputs the path if reachable. Returns false if unreachable.
    /// This overload preserves the original behavior where other pawns remain blockers.
    /// </summary>
    public bool TryFindPath(
        Pawn pPawn,
        GridManager pGridManager,
        Vector2Int startCoordinates,
        Vector2Int goalCoordinates,
        out List<Vector2Int> path)
    {
        return TryFindPath(
            pPawn,
            null,
            pGridManager,
            startCoordinates,
            goalCoordinates,
            out path);
    }

    /// <summary>
    /// Attempts to generate a pawn-aware path from the start tile to the goal tile.
    /// During planning, tiles occupied by other pawns in the same commanded group
    /// are treated as temporarily enterable so the group does not block itself.
    /// Non-commanded pawns still block as normal.
    /// </summary>
    public bool TryFindPath(
        Pawn pPawn,
        List<Pawn> commandedPawns,
        GridManager pGridManager,
        Vector2Int startCoordinates,
        Vector2Int goalCoordinates,
        out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();

        if (pPawn == null || pGridManager == null)
        {
            return false;
        }

        if (!pGridManager.IsInBounds(startCoordinates) || !pGridManager.IsInBounds(goalCoordinates))
        {
            return false;
        }

        if (startCoordinates == goalCoordinates)
        {
            path.Add(startCoordinates);
            return true;
        }

        if (!CanTreatTileAsEnterableForPathfinding(pPawn, commandedPawns, goalCoordinates))
        {
            return false;
        }

        Dictionary<Vector2Int, PathNode> openNodes = new Dictionary<Vector2Int, PathNode>();
        Dictionary<Vector2Int, PathNode> closedNodes = new Dictionary<Vector2Int, PathNode>();

        PathNode startNode = new PathNode(
            startCoordinates,
            startCoordinates,
            0,
            CalculateHeuristicCost(startCoordinates, goalCoordinates));

        openNodes.Add(startCoordinates, startNode);

        while (openNodes.Count > 0)
        {
            PathNode currentNode = GetLowestCostNode(openNodes);

            if (currentNode.Coordinates == goalCoordinates)
            {
                path = ReconstructPath(currentNode, closedNodes);
                return true;
            }

            openNodes.Remove(currentNode.Coordinates);
            closedNodes[currentNode.Coordinates] = currentNode;

            foreach (Vector2Int neighborCoordinates in GetTraversableNeighborCoordinatesForPawn(
                pPawn,
                commandedPawns,
                pGridManager,
                currentNode.Coordinates))
            {
                if (closedNodes.ContainsKey(neighborCoordinates))
                {
                    continue;
                }

                int stepCost = GetStepCost(pGridManager, currentNode.Coordinates, neighborCoordinates);
                int tentativeGCost = currentNode.GCost + stepCost;

                if (!openNodes.TryGetValue(neighborCoordinates, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(
                        neighborCoordinates,
                        currentNode.Coordinates,
                        tentativeGCost,
                        CalculateHeuristicCost(neighborCoordinates, goalCoordinates));

                    openNodes.Add(neighborCoordinates, neighborNode);
                    continue;
                }

                if (tentativeGCost >= neighborNode.GCost)
                {
                    continue;
                }

                neighborNode.GCost = tentativeGCost;
                neighborNode.ParentCoordinates = currentNode.Coordinates;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns traversable neighbor coordinates using pawn-aware and command-group-aware tile validation.
    /// Orthogonal neighbors must be enterable for this pawn.
    /// Diagonal neighbors must also pass the corner-cutting rule for this pawn.
    /// </summary>
    private List<Vector2Int> GetTraversableNeighborCoordinatesForPawn(
        Pawn pPawn,
        List<Pawn> commandedPawns,
        GridManager pGridManager,
        Vector2Int coordinates)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        foreach (Vector2Int neighborCoordinates in pGridManager.GetOrthogonalNeighborCoordinates(coordinates))
        {
            if (CanTreatTileAsEnterableForPathfinding(pPawn, commandedPawns, neighborCoordinates))
            {
                neighbors.Add(neighborCoordinates);
            }
        }

        foreach (Vector2Int neighborCoordinates in pGridManager.GetDiagonalNeighborCoordinates(coordinates))
        {
            if (IsDiagonalTraversalValidForPawn(pPawn, commandedPawns, pGridManager, coordinates, neighborCoordinates))
            {
                neighbors.Add(neighborCoordinates);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Returns whether a diagonal move is valid for this pawn based on
    /// pawn-aware and command-group-aware tile validation and corner-cutting rules.
    /// </summary>
    private bool IsDiagonalTraversalValidForPawn(
        Pawn pPawn,
        List<Pawn> commandedPawns,
        GridManager pGridManager,
        Vector2Int fromCoordinates,
        Vector2Int toCoordinates)
    {
        if (!pGridManager.IsDiagonalStep(fromCoordinates, toCoordinates))
        {
            return false;
        }

        if (!CanTreatTileAsEnterableForPathfinding(pPawn, commandedPawns, toCoordinates))
        {
            return false;
        }

        Vector2Int delta = toCoordinates - fromCoordinates;

        Vector2Int horizontalSideCoordinates = fromCoordinates + new Vector2Int(delta.x, 0);
        Vector2Int verticalSideCoordinates = fromCoordinates + new Vector2Int(0, delta.y);

        return CanTreatTileAsEnterableForPathfinding(pPawn, commandedPawns, horizontalSideCoordinates) &&
               CanTreatTileAsEnterableForPathfinding(pPawn, commandedPawns, verticalSideCoordinates);
    }

    /// <summary>
    /// Returns whether the tile can be treated as enterable during pathfinding.
    /// The pawn's own current/reserved logic is preserved through Pawn.CanTreatTileAsEnterable.
    /// In addition, tiles occupied by other pawns in the same commanded group are treated
    /// as temporarily enterable for planning purposes.
    /// </summary>
    private bool CanTreatTileAsEnterableForPathfinding(
        Pawn pPawn,
        List<Pawn> commandedPawns,
        Vector2Int tileCoordinates)
    {
        if (pPawn.CanTreatTileAsEnterable(tileCoordinates))
        {
            return true;
        }

        return IsTileOccupiedByOtherCommandedPawn(pPawn, commandedPawns, tileCoordinates);
    }

    /// <summary>
    /// Returns whether the tile is currently occupied by a different pawn
    /// in the same commanded group.
    /// </summary>
    private bool IsTileOccupiedByOtherCommandedPawn(
        Pawn currentPawn,
        List<Pawn> commandedPawns,
        Vector2Int tileCoordinates)
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
    /// Returns the node with the lowest total estimated cost from the open set.
    /// </summary>
    private PathNode GetLowestCostNode(Dictionary<Vector2Int, PathNode> openNodes)
    {
        PathNode bestNode = null;

        foreach (KeyValuePair<Vector2Int, PathNode> entry in openNodes)
        {
            PathNode node = entry.Value;

            if (bestNode == null ||
                node.FCost < bestNode.FCost ||
                (node.FCost == bestNode.FCost && node.HCost < bestNode.HCost))
            {
                bestNode = node;
            }
        }

        return bestNode;
    }

    /// <summary>
    /// Reconstructs the final path by following parent links back to the start.
    /// </summary>
    private List<Vector2Int> ReconstructPath(PathNode goalNode, Dictionary<Vector2Int, PathNode> closedNodes)
    {
        List<Vector2Int> reversedPath = new List<Vector2Int>();
        PathNode currentNode = goalNode;

        while (true)
        {
            reversedPath.Add(currentNode.Coordinates);

            if (currentNode.Coordinates == currentNode.ParentCoordinates)
            {
                break;
            }

            currentNode = closedNodes[currentNode.ParentCoordinates];
        }

        reversedPath.Reverse();
        return reversedPath;
    }
    
    /// <summary>
    /// Returns the movement cost between two adjacent tiles.
    /// Orthogonal steps cost 10 and diagonal steps cost 14 before terrain traversal cost is applied.
    /// </summary>
    private int GetStepCost(GridManager pGridManager, Vector2Int fromCoordinates, Vector2Int toCoordinates)
    {
        Vector2Int delta = toCoordinates - fromCoordinates;
        int baseStepCost = 10;

        if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
        {
            baseStepCost = 14;
        }

        float terrainCostMultiplier = 1.0f;

        if (pGridManager != null)
        {
            terrainCostMultiplier = pGridManager.GetTerrainTraversalCostMultiplier(toCoordinates);
        }

        return Mathf.RoundToInt(baseStepCost * terrainCostMultiplier);
    }

    /// <summary>
    /// Returns the octile distance heuristic for 8-directional movement.
    /// </summary>
    private int CalculateHeuristicCost(Vector2Int fromCoordinates, Vector2Int toCoordinates)
    {
        int deltaX = Mathf.Abs(fromCoordinates.x - toCoordinates.x);
        int deltaY = Mathf.Abs(fromCoordinates.y - toCoordinates.y);

        int diagonalSteps = Mathf.Min(deltaX, deltaY);
        int straightSteps = Mathf.Abs(deltaX - deltaY);

        return (diagonalSteps * 14) + (straightSteps * 10);
    }
}