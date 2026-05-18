using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Inventory;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Handles resource checks and spending for construction work.
    /// </summary>
    public class ConstructionResourceService
    {
        private readonly ColonyInventoryManager m_colonyInventoryManager;

        public ConstructionResourceService(ColonyInventoryManager pColonyInventoryManager)
        {
            m_colonyInventoryManager = pColonyInventoryManager;
        }

        /// <summary>
        /// Returns true when the colony has enough resources to build the requested structure.
        /// Requirements with a value of 0 are ignored.
        /// </summary>
        public bool HasRequiredConstructionResources(BuildableDefinition buildableDefinition)
        {
            if (buildableDefinition == null || m_colonyInventoryManager == null)
            {
                return false;
            }

            if (buildableDefinition.RequiredWood > 0
                && !m_colonyInventoryManager.HasResource(ResourceType.Wood, buildableDefinition.RequiredWood))
            {
                return false;
            }

            if (buildableDefinition.RequiredStone > 0
                && !m_colonyInventoryManager.HasResource(ResourceType.Stone, buildableDefinition.RequiredStone))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Consumes the resources required to build the requested structure.
        /// This checks all requirements before removing anything to avoid partial spending.
        /// </summary>
        public bool TryConsumeConstructionResources(BuildableDefinition buildableDefinition)
        {
            if (!HasRequiredConstructionResources(buildableDefinition))
            {
                return false;
            }

            if (buildableDefinition.RequiredWood > 0)
            {
                if (!m_colonyInventoryManager.TryRemoveResource(ResourceType.Wood, buildableDefinition.RequiredWood))
                {
                    return false;
                }
            }

            if (buildableDefinition.RequiredStone > 0)
            {
                if (!m_colonyInventoryManager.TryRemoveResource(ResourceType.Stone, buildableDefinition.RequiredStone))
                {
                    return false;
                }
            }

            return true;
        }
    }
}