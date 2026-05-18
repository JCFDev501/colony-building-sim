using ColonyBuildingSim.Inventory;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Completes resource-gathering tasks that remove world resources and add materials to colony inventory.
    /// This service handles Cut and Mine task effects.
    /// </summary>
    public class ResourceGatherTaskCompletionService
    {
        private readonly GridManager m_gridManager;
        private readonly WorldObjectRenderer m_worldObjectRenderer;
        private readonly BlockRenderer m_blockRenderer;
        private readonly ColonyInventoryManager m_colonyInventoryManager;
        private readonly int m_woodPerTree;
        private readonly int m_stonePerBlock;

        public ResourceGatherTaskCompletionService(
            GridManager pGridManager,
            WorldObjectRenderer pWorldObjectRenderer,
            BlockRenderer pBlockRenderer,
            ColonyInventoryManager pColonyInventoryManager,
            int woodPerTree,
            int stonePerBlock)
        {
            m_gridManager = pGridManager;
            m_worldObjectRenderer = pWorldObjectRenderer;
            m_blockRenderer = pBlockRenderer;
            m_colonyInventoryManager = pColonyInventoryManager;
            m_woodPerTree = woodPerTree;
            m_stonePerBlock = stonePerBlock;
        }

        /// <summary>
        /// Completes a Cut task by clearing the tree target from tile data and refreshing world-object visuals.
        /// </summary>
        public bool CompleteCutTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null)
            {
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!WorkTargetValidator.IsValidCutTarget(m_gridManager, targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            m_gridManager.SetWorldObjectType(targetCoordinates, WorldObjectType.None);
            m_gridManager.SetContentType(targetCoordinates, TileContentType.Empty);
            m_gridManager.SetWalkable(targetCoordinates, true);
            m_gridManager.SetReserved(targetCoordinates, false);

            if (m_colonyInventoryManager != null)
            {
                m_colonyInventoryManager.AddResource(ResourceType.Wood, m_woodPerTree);
            }

            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            if (m_worldObjectRenderer != null)
            {
                m_worldObjectRenderer.RemoveWorldObjectAt(targetCoordinates);
            }

            return true;
        }

        /// <summary>
        /// Completes a Mine task by clearing the stone block target from tile data,
        /// removing the block visual, and adding Stone to inventory.
        /// </summary>
        public bool CompleteMineTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null)
            {
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!WorkTargetValidator.IsValidMineTarget(m_gridManager, targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            m_gridManager.SetBlockType(targetCoordinates, BlockType.None);
            m_gridManager.SetContentType(targetCoordinates, TileContentType.Empty);
            m_gridManager.SetWalkable(targetCoordinates, true);
            m_gridManager.SetReserved(targetCoordinates, false);

            if (m_colonyInventoryManager != null)
            {
                m_colonyInventoryManager.AddResource(ResourceType.Stone, m_stonePerBlock);
            }

            if (m_blockRenderer != null)
            {
                m_blockRenderer.RemoveBlockAt(targetCoordinates);
            }

            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            return true;
        }
    }
}