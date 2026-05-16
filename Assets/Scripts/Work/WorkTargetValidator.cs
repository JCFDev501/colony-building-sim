using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Provides reusable validation checks for work-order and pawn-task targets.
    /// This class only validates world targets and does not create tasks or control pawns.
    /// </summary>
    public static class WorkTargetValidator
    {
        /// <summary>
        /// Returns true when the target tile contains a tree-style world object that can be cut.
        /// </summary>
        public static bool IsValidCutTarget(GridManager pGridManager, Vector2Int targetCoordinates)
        {
            if (pGridManager == null || !pGridManager.TryGetTile(targetCoordinates, out GridTile targetTile))
            {
                return false;
            }

            return targetTile.WorldObjectType == WorldObjectType.Tree;
        }

        /// <summary>
        /// Returns true when the target tile contains a stone block that can be mined.
        /// </summary>
        public static bool IsValidMineTarget(GridManager pGridManager, Vector2Int targetCoordinates)
        {
            if (pGridManager == null || !pGridManager.TryGetTile(targetCoordinates, out GridTile targetTile))
            {
                return false;
            }

            return targetTile.BlockType == BlockType.Stone;
        }

        /// <summary>
        /// Returns true when the target tile is valid land for first-pass construction work.
        /// </summary>
        public static bool IsValidConstructTarget(GridManager pGridManager, Vector2Int targetCoordinates)
        {
            if (pGridManager == null || !pGridManager.TryGetTile(targetCoordinates, out GridTile targetTile))
            {
                return false;
            }

            if (!IsBuildValidTerrain(targetTile.TerrainType))
            {
                return false;
            }

            if (targetTile.BlockType != BlockType.None)
            {
                return false;
            }

            if (targetTile.WorldObjectType != WorldObjectType.None)
            {
                return false;
            }

            if (targetTile.IsOccupied || targetTile.IsReserved)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Placeholder for future planting target validation.
        /// </summary>
        public static bool IsValidPlantTarget(GridManager pGridManager, Vector2Int targetCoordinates)
        {
            return false;
        }

        /// <summary>
        /// Placeholder for future cooking target validation.
        /// </summary>
        public static bool IsValidCookTarget(GridManager pGridManager, Vector2Int targetCoordinates)
        {
            return false;
        }

        /// <summary>
        /// Placeholder for future crafting target validation.
        /// </summary>
        public static bool IsValidCraftTarget(GridManager pGridManager, Vector2Int targetCoordinates)
        {
            return false;
        }

        /// <summary>
        /// Returns true when terrain can support first-pass construction placement.
        /// </summary>
        private static bool IsBuildValidTerrain(TileTerrainType terrainType)
        {
            return terrainType == TileTerrainType.Grass
                || terrainType == TileTerrainType.Dirt
                || terrainType == TileTerrainType.ForestFloor;
        }
    }
}