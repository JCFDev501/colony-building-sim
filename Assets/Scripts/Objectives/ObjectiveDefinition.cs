using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Objectives
{
    /// <summary>
    /// Defines objective data that can be loaded at runtime.
    /// This allows the active objective to live as a reusable asset instead of scene-only data.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ObjectiveDefinition",
        menuName = "ColonySim/Objectives/Objective Definition")]
    public class ObjectiveDefinition : ScriptableObject
    {
        [SerializeField] private string m_objectiveTitle = "Build a Starter Camp";
        [SerializeField] private List<ObjectiveRequirement> m_requirements = new List<ObjectiveRequirement>();

        public string ObjectiveTitle
        {
            get { return m_objectiveTitle; }
        }

        public IReadOnlyList<ObjectiveRequirement> Requirements
        {
            get { return m_requirements; }
        }
    }
}