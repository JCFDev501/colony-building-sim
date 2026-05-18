using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Crops
{
    /// <summary>
    /// Provides lookup access from CropType to CropDefinition.
    /// </summary>
    public class CropDefinitionLibrary : MonoBehaviour
    {
        [Header("Crop Definitions")]
        [SerializeField] private List<CropDefinition> m_cropDefinitions = new List<CropDefinition>();

        private readonly Dictionary<CropType, CropDefinition> m_definitionLookup = new Dictionary<CropType, CropDefinition>();

        /// <summary>
        /// Builds the definition lookup when play begins.
        /// </summary>
        private void Awake()
        {
            BuildDefinitionLookup();
        }

        /// <summary>
        /// Attempts to find the definition for the requested crop type.
        /// </summary>
        public bool TryGetDefinition(CropType cropType, out CropDefinition cropDefinition)
        {
            return m_definitionLookup.TryGetValue(cropType, out cropDefinition);
        }

        /// <summary>
        /// Rebuilds the lookup from the assigned definition list.
        /// Duplicate definitions are ignored after the first match.
        /// </summary>
        private void BuildDefinitionLookup()
        {
            m_definitionLookup.Clear();

            foreach (CropDefinition cropDefinition in m_cropDefinitions)
            {
                if (cropDefinition == null)
                {
                    continue;
                }

                if (m_definitionLookup.ContainsKey(cropDefinition.CropType))
                {
                    Debug.LogWarning(
                        "Duplicate CropDefinition found for CropType: " + cropDefinition.CropType,
                        this);

                    continue;
                }

                m_definitionLookup.Add(cropDefinition.CropType, cropDefinition);
            }
        }
    }
}