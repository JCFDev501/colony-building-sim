using System;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Inventory;
using UnityEngine;

namespace ColonyBuildingSim.Objectives
{
    /// <summary>
    /// Defines one requirement for completing a prototype objective.
    /// This is plain serializable data so the ObjectiveManager can track different requirement types.
    /// </summary>
    [Serializable]
    public class ObjectiveRequirement
    {
        [SerializeField] private string m_displayName = "New Requirement";
        [SerializeField] private ObjectiveRequirementType m_requirementType = ObjectiveRequirementType.BuiltStructure;
        [SerializeField] private int m_requiredAmount = 1;

        [Header("Structure Requirement")]
        [SerializeField] private BuildableType m_buildableType = BuildableType.Campfire;

        [Header("Resource Requirement")]
        [SerializeField] private ResourceType m_resourceType = ResourceType.Meal;

        public string DisplayName
        {
            get { return m_displayName; }
        }

        public ObjectiveRequirementType RequirementType
        {
            get { return m_requirementType; }
        }

        public int RequiredAmount
        {
            get { return Mathf.Max(1, m_requiredAmount); }
        }

        public BuildableType BuildableType
        {
            get { return m_buildableType; }
        }

        public ResourceType ResourceType
        {
            get { return m_resourceType; }
        }
    }
}