using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Buildables
{
    /// <summary>
    /// Tracks completed built structures during play.
    /// Future systems can query this registry for objectives, cooking stations, doors, shelter, and other structure-driven logic.
    /// </summary>
    public class StructureRegistry : MonoBehaviour
    {
        [SerializeField] private List<StructureInstance> m_structures = new List<StructureInstance>();

        private readonly Dictionary<Vector2Int, StructureInstance> m_structureByCoordinates = new Dictionary<Vector2Int, StructureInstance>();

        public IReadOnlyList<StructureInstance> Structures
        {
            get { return m_structures; }
        }

        /// <summary>
        /// Registers a completed structure if its tile is not already registered.
        /// </summary>
        public bool RegisterStructure(StructureInstance pStructureInstance)
        {
            if (pStructureInstance == null)
            {
                return false;
            }

            Vector2Int coordinates = pStructureInstance.GridCoordinates;

            if (m_structureByCoordinates.ContainsKey(coordinates))
            {
                Debug.LogWarning("StructureRegistry already has a structure at " + coordinates + ".", this);
                return false;
            }

            m_structures.Add(pStructureInstance);
            m_structureByCoordinates.Add(coordinates, pStructureInstance);

            return true;
        }

        /// <summary>
        /// Unregisters a completed structure from the registry.
        /// </summary>
        public bool UnregisterStructure(StructureInstance pStructureInstance)
        {
            if (pStructureInstance == null)
            {
                return false;
            }

            m_structures.Remove(pStructureInstance);

            Vector2Int coordinates = pStructureInstance.GridCoordinates;

            if (m_structureByCoordinates.TryGetValue(coordinates, out StructureInstance pRegisteredStructure)
                && pRegisteredStructure == pStructureInstance)
            {
                m_structureByCoordinates.Remove(coordinates);
            }

            return true;
        }

        /// <summary>
        /// Returns all registered structures of the requested buildable type.
        /// </summary>
        public List<StructureInstance> GetStructuresByType(BuildableType buildableType)
        {
            List<StructureInstance> matchingStructures = new List<StructureInstance>();

            foreach (StructureInstance pStructureInstance in m_structures)
            {
                if (pStructureInstance == null)
                {
                    continue;
                }

                if (pStructureInstance.BuildableType == buildableType)
                {
                    matchingStructures.Add(pStructureInstance);
                }
            }

            return matchingStructures;
        }

        /// <summary>
        /// Attempts to find a registered structure at the requested grid coordinates.
        /// </summary>
        public bool TryGetStructureAt(Vector2Int coordinates, out StructureInstance pStructureInstance)
        {
            return m_structureByCoordinates.TryGetValue(coordinates, out pStructureInstance);
        }

        /// <summary>
        /// Clears all registry data without destroying structure GameObjects.
        /// </summary>
        public void ClearRegistry()
        {
            m_structures.Clear();
            m_structureByCoordinates.Clear();
        }
    }
}