using System;
using UnityEngine;

namespace ColonyBuildingSim.Buildables
{
    /// <summary>
    /// Stores runtime identity data for a completed built structure.
    /// Structure instances are registered after construction finishes.
    /// </summary>
    public class StructureInstance : MonoBehaviour
    {
        [Header("Runtime Structure Data")]
        [SerializeField] private string m_structureId = string.Empty;
        [SerializeField] private BuildableType m_buildableType = BuildableType.Campfire;
        [SerializeField] private Vector2Int m_gridCoordinates = Vector2Int.zero;
        [SerializeField] private BuildableDefinition m_buildableDefinition;

        public string StructureId
        {
            get { return m_structureId; }
        }

        public BuildableType BuildableType
        {
            get { return m_buildableType; }
        }

        public Vector2Int GridCoordinates
        {
            get { return m_gridCoordinates; }
        }

        public BuildableDefinition BuildableDefinition
        {
            get { return m_buildableDefinition; }
        }

        /// <summary>
        /// Initializes this completed structure instance after it is placed in the world.
        /// </summary>
        public void Initialize(BuildableDefinition buildableDefinition, Vector2Int gridCoordinates)
        {
            m_structureId = Guid.NewGuid().ToString();
            m_buildableDefinition = buildableDefinition;
            m_gridCoordinates = gridCoordinates;

            if (buildableDefinition != null)
            {
                m_buildableType = buildableDefinition.BuildableType;
            }
        }
    }
}