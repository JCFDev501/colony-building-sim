using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Buildables
{
    /// <summary>
    /// Provides lookup access from BuildableType to BuildableDefinition.
    /// </summary>
    public class BuildableDefinitionLibrary : MonoBehaviour
    {
        [Header("Buildable Definitions")]
        [SerializeField] private List<BuildableDefinition> m_buildableDefinitions = new List<BuildableDefinition>();

        private readonly Dictionary<BuildableType, BuildableDefinition> m_definitionLookup = new Dictionary<BuildableType, BuildableDefinition>();

        /// <summary>
        /// Builds the definition lookup when play begins.
        /// </summary>
        private void Awake()
        {
            BuildDefinitionLookup();
        }

        /// <summary>
        /// Attempts to find the definition for the requested buildable type.
        /// </summary>
        public bool TryGetDefinition(BuildableType buildableType, out BuildableDefinition buildableDefinition)
        {
            return m_definitionLookup.TryGetValue(buildableType, out buildableDefinition);
        }

        /// <summary>
        /// Rebuilds the lookup from the assigned definition list.
        /// Duplicate definitions are ignored after the first match.
        /// </summary>
        private void BuildDefinitionLookup()
        {
            m_definitionLookup.Clear();

            foreach (BuildableDefinition buildableDefinition in m_buildableDefinitions)
            {
                if (buildableDefinition == null)
                {
                    continue;
                }

                if (m_definitionLookup.ContainsKey(buildableDefinition.BuildableType))
                {
                    Debug.LogWarning(
                        "Duplicate BuildableDefinition found for BuildableType: " + buildableDefinition.BuildableType,
                        this);

                    continue;
                }

                m_definitionLookup.Add(buildableDefinition.BuildableType, buildableDefinition);
            }
        }
    }
}