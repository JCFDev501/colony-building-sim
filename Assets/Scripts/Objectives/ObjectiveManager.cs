using System.Collections.Generic;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Inventory;
using UnityEngine;

namespace ColonyBuildingSim.Objectives
{
    /// <summary>
    /// Tracks the prototype win objective.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StructureRegistry m_structureRegistry;
        [SerializeField] private ColonyInventoryManager m_colonyInventoryManager;

        [Header("Runtime Objective Loading")]
        [SerializeField] private string m_objectiveResourcePath = "Objectives/StarterCampObjective";
        [SerializeField] private ObjectiveDefinition m_loadedObjectiveDefinition;

        [Header("Fallback Objective")]
        [SerializeField] private string m_fallbackObjectiveTitle = "Build a Starter Camp";
        [SerializeField] private List<ObjectiveRequirement> m_fallbackRequirements = new List<ObjectiveRequirement>();

        [Header("State")]
        [SerializeField] private bool m_isObjectiveComplete = false;

        public string ObjectiveTitle
        {
            get
            {
                if (m_loadedObjectiveDefinition != null)
                {
                    return m_loadedObjectiveDefinition.ObjectiveTitle;
                }

                return m_fallbackObjectiveTitle;
            }
        }

        public IReadOnlyList<ObjectiveRequirement> Requirements
        {
            get
            {
                if (m_loadedObjectiveDefinition != null)
                {
                    return m_loadedObjectiveDefinition.Requirements;
                }

                return m_fallbackRequirements;
            }
        }

        public bool IsObjectiveComplete
        {
            get { return m_isObjectiveComplete; }
        }

        /// <summary>
        /// Finds required references and loads objective data when play begins.
        /// </summary>
        private void Start()
        {
            if (m_structureRegistry == null)
            {
                m_structureRegistry = FindFirstObjectByType<StructureRegistry>();
            }

            if (m_colonyInventoryManager == null)
            {
                m_colonyInventoryManager = FindFirstObjectByType<ColonyInventoryManager>();
            }

            if (m_structureRegistry == null)
            {
                Debug.LogError("ObjectiveManager is missing a StructureRegistry reference.", this);
            }

            if (m_colonyInventoryManager == null)
            {
                Debug.LogError("ObjectiveManager is missing a ColonyInventoryManager reference.", this);
            }

            LoadObjectiveDefinition();
            RefreshObjectiveState();
        }

        /// <summary>
        /// Updates objective completion state during play.
        /// </summary>
        private void Update()
        {
            if (m_isObjectiveComplete)
            {
                return;
            }

            RefreshObjectiveState();
        }

        /// <summary>
        /// Returns current progress for the requested requirement.
        /// </summary>
        public int GetRequirementProgress(ObjectiveRequirement requirement)
        {
            if (requirement == null)
            {
                return 0;
            }

            switch (requirement.RequirementType)
            {
                case ObjectiveRequirementType.BuiltStructure:
                    return GetBuiltStructureCount(requirement.BuildableType);

                case ObjectiveRequirementType.ResourceAmount:
                    return GetResourceAmount(requirement.ResourceType);
            }

            return 0;
        }

        /// <summary>
        /// Returns whether the requested requirement is complete.
        /// </summary>
        public bool IsRequirementComplete(ObjectiveRequirement requirement)
        {
            if (requirement == null)
            {
                return false;
            }

            return GetRequirementProgress(requirement) >= requirement.RequiredAmount;
        }

        /// <summary>
        /// Loads the active objective definition from the Resources folder at runtime.
        /// </summary>
        private void LoadObjectiveDefinition()
        {
            if (string.IsNullOrWhiteSpace(m_objectiveResourcePath))
            {
                Debug.LogWarning("ObjectiveManager has no objective resource path. Using fallback objective data.", this);
                return;
            }

            m_loadedObjectiveDefinition = Resources.Load<ObjectiveDefinition>(m_objectiveResourcePath);

            if (m_loadedObjectiveDefinition == null)
            {
                Debug.LogError(
                    "ObjectiveManager failed to load ObjectiveDefinition at Resources path: "
                    + m_objectiveResourcePath
                    + ". Using fallback objective data.",
                    this);
            }
        }

        /// <summary>
        /// Refreshes the full objective completion state.
        /// </summary>
        private void RefreshObjectiveState()
        {
            IReadOnlyList<ObjectiveRequirement> requirements = Requirements;

            if (requirements == null || requirements.Count == 0)
            {
                return;
            }

            foreach (ObjectiveRequirement requirement in requirements)
            {
                if (!IsRequirementComplete(requirement))
                {
                    return;
                }
            }

            CompleteObjective();
        }

        /// <summary>
        /// Marks the prototype objective complete.
        /// </summary>
        private void CompleteObjective()
        {
            if (m_isObjectiveComplete)
            {
                return;
            }

            m_isObjectiveComplete = true;
            Debug.Log("Objective complete: " + ObjectiveTitle, this);
        }

        /// <summary>
        /// Counts registered structures of a requested buildable type.
        /// </summary>
        private int GetBuiltStructureCount(BuildableType buildableType)
        {
            if (m_structureRegistry == null)
            {
                return 0;
            }

            return m_structureRegistry.GetStructuresByType(buildableType).Count;
        }

        /// <summary>
        /// Gets the current colony amount for a requested resource type.
        /// </summary>
        private int GetResourceAmount(ResourceType resourceType)
        {
            if (m_colonyInventoryManager == null)
            {
                return 0;
            }

            return m_colonyInventoryManager.GetResourceAmount(resourceType);
        }
    }
}