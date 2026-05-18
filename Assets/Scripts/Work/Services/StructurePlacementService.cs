using ColonyBuildingSim.Buildables;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Handles completed structure placement, registration, and tile-state updates.
    /// </summary>
    public class StructurePlacementService
    {
        private readonly GridManager m_gridManager;
        private readonly StructureRegistry m_structureRegistry;

        public StructurePlacementService(
            GridManager pGridManager,
            StructureRegistry pStructureRegistry)
        {
            m_gridManager = pGridManager;
            m_structureRegistry = pStructureRegistry;
        }

        /// <summary>
        /// Places a completed structure at the target tile and applies its gameplay tile state.
        /// Returns the created StructureInstance, or null if placement fails.
        /// </summary>
        public StructureInstance PlaceStructure(
            BuildableDefinition buildableDefinition,
            Vector2Int targetCoordinates,
            Quaternion spawnRotation)
        {
            if (buildableDefinition == null
                || buildableDefinition.Prefab == null
                || m_gridManager == null)
            {
                return null;
            }

            StructureInstance pStructureInstance = SpawnStructure(
                buildableDefinition,
                targetCoordinates,
                spawnRotation);

            if (pStructureInstance == null)
            {
                return null;
            }

            if (m_structureRegistry != null)
            {
                m_structureRegistry.RegisterStructure(pStructureInstance);
            }

            ApplyStructureTileState(buildableDefinition, targetCoordinates);

            return pStructureInstance;
        }

        /// <summary>
        /// Spawns a completed structure prefab and initializes its StructureInstance.
        /// </summary>
        private StructureInstance SpawnStructure(
            BuildableDefinition buildableDefinition,
            Vector2Int targetCoordinates,
            Quaternion spawnRotation)
        {
            Vector3 spawnPosition = m_gridManager.GetWorldPosition(targetCoordinates);
            spawnPosition += buildableDefinition.PlacementOffset;

            GameObject pStructureObject = Object.Instantiate(
                buildableDefinition.Prefab,
                spawnPosition,
                spawnRotation);

            StructureInstance pStructureInstance = pStructureObject.GetComponent<StructureInstance>();

            if (pStructureInstance == null)
            {
                pStructureInstance = pStructureObject.AddComponent<StructureInstance>();
            }

            pStructureInstance.Initialize(buildableDefinition, targetCoordinates);

            return pStructureInstance;
        }

        /// <summary>
        /// Applies tile state for a completed structure.
        /// Exact structure identity is tracked by StructureRegistry.
        /// </summary>
        private void ApplyStructureTileState(
            BuildableDefinition buildableDefinition,
            Vector2Int targetCoordinates)
        {
            m_gridManager.SetContentType(targetCoordinates, TileContentType.Structure);
            m_gridManager.SetWalkable(targetCoordinates, !buildableDefinition.BlocksMovement);
            m_gridManager.SetReserved(targetCoordinates, false);
        }
    }
}