using ColonyBuildingSim.Buildables;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Handles wooden door rotation based on nearby wall structures.
    /// </summary>
    public class DoorRotationService
    {
        private readonly GridManager m_gridManager;
        private readonly StructureRegistry m_structureRegistry;

        public DoorRotationService(
            GridManager pGridManager,
            StructureRegistry pStructureRegistry)
        {
            m_gridManager = pGridManager;
            m_structureRegistry = pStructureRegistry;
        }

        /// <summary>
        /// Returns the rotation that should be used when spawning a completed structure.
        /// Doors align with neighboring WoodenWall structures when possible.
        /// </summary>
        public Quaternion GetStructureSpawnRotation(
            BuildableDefinition buildableDefinition,
            Vector2Int targetCoordinates)
        {
            if (buildableDefinition == null)
            {
                return Quaternion.identity;
            }

            if (buildableDefinition.BuildableType != BuildableType.WoodenDoor)
            {
                return Quaternion.identity;
            }

            return GetWoodenDoorRotation(targetCoordinates);
        }

        /// <summary>
        /// Refreshes nearby WoodenDoor rotations after a structure is completed.
        /// This handles cases where a door is built before the walls around it.
        /// </summary>
        public void RefreshDoorRotationsNear(Vector2Int completedStructureCoordinates)
        {
            if (m_gridManager == null)
            {
                return;
            }

            RefreshDoorRotationAt(completedStructureCoordinates);

            foreach (Vector2Int neighborCoordinates in m_gridManager.GetOrthogonalNeighborCoordinates(completedStructureCoordinates))
            {
                RefreshDoorRotationAt(neighborCoordinates);
            }
        }

        /// <summary>
        /// Returns the best current rotation for a WoodenDoor based on adjacent WoodenWall structures.
        /// </summary>
        private Quaternion GetWoodenDoorRotation(Vector2Int doorCoordinates)
        {
            bool hasLeftWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.left, BuildableType.WoodenWall);
            bool hasRightWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.right, BuildableType.WoodenWall);
            bool hasUpWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.up, BuildableType.WoodenWall);
            bool hasDownWall = HasAdjacentStructureOfType(doorCoordinates + Vector2Int.down, BuildableType.WoodenWall);

            bool hasHorizontalWallConnection = hasLeftWall || hasRightWall;
            bool hasVerticalWallConnection = hasUpWall || hasDownWall;

            if (hasHorizontalWallConnection && !hasVerticalWallConnection)
            {
                return Quaternion.Euler(0.0f, 90.0f, 0.0f);
            }

            if (hasVerticalWallConnection && !hasHorizontalWallConnection)
            {
                return Quaternion.identity;
            }

            if (hasLeftWall && hasRightWall)
            {
                return Quaternion.Euler(0.0f, 90.0f, 0.0f);
            }

            if (hasUpWall && hasDownWall)
            {
                return Quaternion.identity;
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// Refreshes a WoodenDoor rotation at the requested tile if one is registered there.
        /// </summary>
        private void RefreshDoorRotationAt(Vector2Int coordinates)
        {
            if (m_structureRegistry == null)
            {
                return;
            }

            if (!m_structureRegistry.TryGetStructureAt(coordinates, out StructureInstance pStructureInstance))
            {
                return;
            }

            if (pStructureInstance == null || pStructureInstance.BuildableType != BuildableType.WoodenDoor)
            {
                return;
            }

            pStructureInstance.transform.rotation = GetWoodenDoorRotation(coordinates);
        }

        /// <summary>
        /// Returns true when the requested tile has a registered structure of the requested buildable type.
        /// </summary>
        private bool HasAdjacentStructureOfType(Vector2Int coordinates, BuildableType buildableType)
        {
            if (m_structureRegistry == null)
            {
                return false;
            }

            if (!m_structureRegistry.TryGetStructureAt(coordinates, out StructureInstance pStructureInstance))
            {
                return false;
            }

            return pStructureInstance != null && pStructureInstance.BuildableType == buildableType;
        }
    }
}