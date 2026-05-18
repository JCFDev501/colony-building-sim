using ColonyBuildingSim.Crops;
using ColonyBuildingSim.Inventory;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Completes crop-related plant work, including planting new crops and harvesting mature crops.
    /// </summary>
    public class CropTaskCompletionService
    {
        private readonly GridManager m_gridManager;
        private readonly CropManager m_cropManager;
        private readonly CropDefinitionLibrary m_cropDefinitionLibrary;
        private readonly ColonyInventoryManager m_colonyInventoryManager;

        public CropTaskCompletionService(
            GridManager pGridManager,
            CropManager pCropManager,
            CropDefinitionLibrary pCropDefinitionLibrary,
            ColonyInventoryManager pColonyInventoryManager)
        {
            m_gridManager = pGridManager;
            m_cropManager = pCropManager;
            m_cropDefinitionLibrary = pCropDefinitionLibrary;
            m_colonyInventoryManager = pColonyInventoryManager;
        }

        /// <summary>
        /// Completes Plant tasks by routing to the correct plant work action.
        /// </summary>
        public bool CompletePlantTask(PawnTask pTask)
        {
            if (pTask == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasCropType || !pWorkOrder.HasPlantWorkAction)
            {
                pTask.Cancel();
                return false;
            }

            switch (pWorkOrder.PlantWorkAction)
            {
                case PlantWorkAction.PlantCrop:
                    return CompletePlantCropTask(pTask);

                case PlantWorkAction.HarvestCrop:
                    return CompleteHarvestCropTask(pTask);

                default:
                    pTask.Cancel();
                    return false;
            }
        }

        /// <summary>
        /// Completes PlantCrop work by spawning and registering a new crop instance.
        /// </summary>
        private bool CompletePlantCropTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null || m_cropManager == null || m_cropDefinitionLibrary == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasCropType)
            {
                pTask.Cancel();
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!IsValidPlantCropCompletionTarget(targetCoordinates))
            {
                pTask.Cancel();
                return false;
            }

            if (!m_cropDefinitionLibrary.TryGetDefinition(pWorkOrder.CropType, out CropDefinition cropDefinition))
            {
                pTask.Cancel();
                return false;
            }

            if (cropDefinition == null || cropDefinition.GrowingPrefab == null)
            {
                pTask.Cancel();
                return false;
            }

            Vector3 spawnPosition = m_gridManager.GetWorldPosition(targetCoordinates);
            spawnPosition += cropDefinition.PlacementOffset;

            GameObject pCropObject = new GameObject(cropDefinition.DisplayName + " Crop");
            pCropObject.transform.position = spawnPosition;
            pCropObject.transform.rotation = Quaternion.identity;

            CropInstance pCropInstance = pCropObject.GetComponent<CropInstance>();

            if (pCropInstance == null)
            {
                pCropInstance = pCropObject.AddComponent<CropInstance>();
            }

            pCropInstance.Initialize(cropDefinition, targetCoordinates);

            if (!m_cropManager.RegisterCrop(pCropInstance))
            {
                Object.Destroy(pCropObject);
                pTask.Cancel();
                return false;
            }

            m_gridManager.SetContentType(targetCoordinates, TileContentType.Crop);
            m_gridManager.SetWalkable(targetCoordinates, !cropDefinition.BlocksMovement);
            m_gridManager.SetReserved(targetCoordinates, false);

            pTask.Complete();

            if (pWorkOrder != null)
            {
                pWorkOrder.MarkComplete();
            }

            return true;
        }

        /// <summary>
        /// Completes HarvestCrop work by adding Food to inventory and resetting the mature crop.
        /// </summary>
        private bool CompleteHarvestCropTask(PawnTask pTask)
        {
            if (pTask == null || m_gridManager == null || m_cropManager == null || m_colonyInventoryManager == null)
            {
                return false;
            }

            WorkOrder pWorkOrder = pTask.ParentWorkOrder;

            if (pWorkOrder == null || !pWorkOrder.HasCropType)
            {
                pTask.Cancel();
                return false;
            }

            Vector2Int targetCoordinates = pTask.TargetCoordinates;

            if (!m_cropManager.TryGetCropAt(targetCoordinates, out CropInstance pCropInstance))
            {
                pTask.Cancel();
                return false;
            }

            if (pCropInstance == null || !pCropInstance.IsMature)
            {
                pTask.Release();
                return false;
            }

            CropDefinition cropDefinition = pCropInstance.CropDefinition;

            if (cropDefinition == null)
            {
                pTask.Cancel();
                return false;
            }

            m_colonyInventoryManager.AddResource(ResourceType.Food, cropDefinition.FoodYield);

            pCropInstance.ResetAfterHarvest();

            m_gridManager.SetContentType(targetCoordinates, TileContentType.Crop);
            m_gridManager.SetWalkable(targetCoordinates, !cropDefinition.BlocksMovement);
            m_gridManager.SetReserved(targetCoordinates, false);

            pTask.Complete();

            if (pWorkOrder != null)
            {
                pWorkOrder.MarkComplete();
            }

            return true;
        }

        /// <summary>
        /// Returns whether a crop can still be planted at the requested target when the PlantCrop task completes.
        /// </summary>
        private bool IsValidPlantCropCompletionTarget(Vector2Int targetCoordinates)
        {
            if (m_gridManager == null || m_cropManager == null)
            {
                return false;
            }

            if (!m_gridManager.IsInBounds(targetCoordinates))
            {
                return false;
            }

            if (m_gridManager.GetTerrainType(targetCoordinates) == TileTerrainType.Water)
            {
                return false;
            }

            if (!m_gridManager.CanEnterTile(targetCoordinates))
            {
                return false;
            }

            if (m_gridManager.GetBlockType(targetCoordinates) != BlockType.None)
            {
                return false;
            }

            if (m_gridManager.GetWorldObjectType(targetCoordinates) != WorldObjectType.None)
            {
                return false;
            }

            if (m_gridManager.GetContentType(targetCoordinates) != TileContentType.Empty)
            {
                return false;
            }

            if (m_cropManager.TryGetCropAt(targetCoordinates, out CropInstance existingCrop)
                && existingCrop != null)
            {
                return false;
            }

            return true;
        }
    }
}