using ColonyBuildingSim.Buildables;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Completes construction tasks by validating the work order, consuming resources,
    /// placing the completed structure, and finishing the linked task/work order.
    /// </summary>
    public class ConstructTaskCompletionService
    {
        private readonly GridManager m_gridManager;
        private readonly BuildableDefinitionLibrary m_buildableDefinitionLibrary;
        private readonly ConstructionResourceService m_constructionResourceService;
        private readonly StructurePlacementService m_structurePlacementService;
        private readonly DoorRotationService m_doorRotationService;

        public ConstructTaskCompletionService(
            GridManager pGridManager,
            BuildableDefinitionLibrary pBuildableDefinitionLibrary,
            ConstructionResourceService pConstructionResourceService,
            StructurePlacementService pStructurePlacementService,
            DoorRotationService pDoorRotationService)
        {
            m_gridManager = pGridManager;
            m_buildableDefinitionLibrary = pBuildableDefinitionLibrary;
            m_constructionResourceService = pConstructionResourceService;
            m_structurePlacementService = pStructurePlacementService;
            m_doorRotationService = pDoorRotationService;
        }

        /// <summary>
        /// Completes a Construct task by consuming resources, placing the built structure, and registering it.
        /// </summary>
        public bool CompleteConstructTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null || m_buildableDefinitionLibrary == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasBuildableType)
            {
                pTask.Cancel();
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!WorkTargetValidator.IsValidConstructTarget(m_gridManager, targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            if (!m_buildableDefinitionLibrary.TryGetDefinition(pWorkOrder.BuildableType, out BuildableDefinition buildableDefinition))
            {
                pTask.Cancel();
                return false;
            }

            if (buildableDefinition == null || buildableDefinition.Prefab == null)
            {
                pTask.Cancel();
                return false;
            }

            if (m_constructionResourceService == null
                || !m_constructionResourceService.TryConsumeConstructionResources(buildableDefinition))
            {
                pTask.Release();
                return false;
            }

            if (m_structurePlacementService == null)
            {
                pTask.Cancel();
                return false;
            }

            Quaternion spawnRotation = Quaternion.identity;

            if (m_doorRotationService != null)
            {
                spawnRotation = m_doorRotationService.GetStructureSpawnRotation(
                    buildableDefinition,
                    targetCoordinates);
            }

            StructureInstance pStructureInstance = m_structurePlacementService.PlaceStructure(
                buildableDefinition,
                targetCoordinates,
                spawnRotation);

            if (pStructureInstance == null)
            {
                pTask.Cancel();
                return false;
            }

            pTask.Complete();

            if (pWorkOrder != null)
            {
                pWorkOrder.MarkComplete();
            }

            if (m_doorRotationService != null)
            {
                m_doorRotationService.RefreshDoorRotationsNear(targetCoordinates);
            }

            return true;
        }
    }
}