using System.Collections.Generic;
using ColonyBuildingSim.WorldContext;
using ColonyBuildingSim.Work;
using UnityEngine;

namespace ColonyBuildingSim.Crops
{
    /// <summary>
    /// Tracks planted crops during play and updates crop growth over time.
    /// This manager owns crop registration and crop lookup, but does not choose pawn tasks.
    /// </summary>
    public class CropManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager m_gridManager;
        [SerializeField] private WorldContextManager m_worldContextManager;
        [SerializeField] private WorkOrderManager m_workOrderManager;

        [Header("Runtime Crops")]
        [SerializeField] private List<CropInstance> m_crops = new List<CropInstance>();

        private readonly Dictionary<Vector2Int, CropInstance> m_cropByCoordinates = new Dictionary<Vector2Int, CropInstance>();

        public IReadOnlyList<CropInstance> Crops
        {
            get { return m_crops; }
        }

        /// <summary>
        /// Finds required scene references if they were not assigned in the Inspector.
        /// </summary>
        private void Awake()
        {
            if (m_gridManager == null)
            {
                m_gridManager = FindFirstObjectByType<GridManager>();
            }

            if (m_worldContextManager == null)
            {
                m_worldContextManager = FindFirstObjectByType<WorldContextManager>();
            }

            if (m_workOrderManager == null)
            {
                m_workOrderManager = FindFirstObjectByType<WorkOrderManager>();
            }
        }

        /// <summary>
        /// Updates crop growth using world simulation time.
        /// </summary>
        private void Update()
        {
            UpdateCropGrowth();
        }

        /// <summary>
        /// Registers a newly planted crop if the tile does not already contain a crop.
        /// </summary>
        public bool RegisterCrop(CropInstance pCropInstance)
        {
            if (pCropInstance == null)
            {
                return false;
            }

            Vector2Int coordinates = pCropInstance.GridCoordinates;

            if (m_cropByCoordinates.ContainsKey(coordinates))
            {
                Debug.LogWarning("CropManager already has a crop at " + coordinates + ".", this);
                return false;
            }

            m_crops.Add(pCropInstance);
            m_cropByCoordinates.Add(coordinates, pCropInstance);

            return true;
        }

        /// <summary>
        /// Removes a crop from tracking and optionally destroys its GameObject.
        /// </summary>
        public bool RemoveCrop(CropInstance pCropInstance, bool destroyObject)
        {
            if (pCropInstance == null)
            {
                return false;
            }

            m_crops.Remove(pCropInstance);

            Vector2Int coordinates = pCropInstance.GridCoordinates;

            if (m_cropByCoordinates.TryGetValue(coordinates, out CropInstance pRegisteredCrop)
                && pRegisteredCrop == pCropInstance)
            {
                m_cropByCoordinates.Remove(coordinates);
            }

            if (destroyObject)
            {
                Destroy(pCropInstance.gameObject);
            }

            return true;
        }

        /// <summary>
        /// Attempts to find a crop at the requested tile coordinates.
        /// </summary>
        public bool TryGetCropAt(Vector2Int coordinates, out CropInstance pCropInstance)
        {
            return m_cropByCoordinates.TryGetValue(coordinates, out pCropInstance);
        }

        /// <summary>
        /// Returns all crops that are currently mature.
        /// </summary>
        public List<CropInstance> GetMatureCrops()
        {
            List<CropInstance> matureCrops = new List<CropInstance>();

            foreach (CropInstance pCropInstance in m_crops)
            {
                if (pCropInstance == null)
                {
                    continue;
                }

                if (pCropInstance.IsMature)
                {
                    matureCrops.Add(pCropInstance);
                }
            }

            return matureCrops;
        }

        /// <summary>
        /// Clears all crop tracking without destroying crop GameObjects.
        /// </summary>
        public void ClearRegistry()
        {
            m_crops.Clear();
            m_cropByCoordinates.Clear();
        }

        /// <summary>
        /// Advances growth for all registered crops.
        /// </summary>
        private void UpdateCropGrowth()
        {
            float simulationDeltaTime = GetGrowthDeltaTime();

            if (simulationDeltaTime <= 0.0f)
            {
                return;
            }

            for (int cropIndex = 0; cropIndex < m_crops.Count; ++cropIndex)
            {
                CropInstance pCropInstance = m_crops[cropIndex];

                if (pCropInstance == null)
                {
                    continue;
                }

                bool becameMature = pCropInstance.AddGrowth(simulationDeltaTime);

                if (becameMature)
                {
                    if (pCropInstance.CropDefinition != null)
                    {
                        pCropInstance.SetVisual(pCropInstance.CropDefinition.MaturePrefab);
                    }

                    Debug.Log(
                        "Crop matured: "
                        + pCropInstance.CropType
                        + " at "
                        + pCropInstance.GridCoordinates,
                        this);

                    CreateHarvestWorkOrderForCrop(pCropInstance);
                }
            }
        }

        /// <summary>
        /// Gets delta time for crop growth from world context when available.
        /// This keeps crop growth aligned with world pause and world speed.
        /// </summary>
        private float GetGrowthDeltaTime()
        {
            if (m_worldContextManager == null)
            {
                return Time.deltaTime;
            }

            return m_worldContextManager.SimulationDeltaTime;
        }

        /// <summary>
        /// Creates HarvestCrop Plant work for a mature crop if it does not already have harvest work.
        /// </summary>
        private void CreateHarvestWorkOrderForCrop(CropInstance pCropInstance)
        {
            if (pCropInstance == null || m_workOrderManager == null)
            {
                return;
            }

            if (!pCropInstance.IsMature || pCropInstance.HasHarvestWorkOrder)
            {
                return;
            }

            Vector2Int cropCoordinates = pCropInstance.GridCoordinates;

            if (m_workOrderManager.HasDuplicateActiveWorkOrder(WorkType.Plant, cropCoordinates))
            {
                pCropInstance.HasHarvestWorkOrder = true;
                return;
            }

            WorkOrder harvestWorkOrder = new WorkOrder(
                WorkType.Plant,
                cropCoordinates,
                pCropInstance.CropType,
                PlantWorkAction.HarvestCrop);

            harvestWorkOrder.MarkReady();

            if (!m_workOrderManager.AddWorkOrder(harvestWorkOrder))
            {
                return;
            }

            pCropInstance.HasHarvestWorkOrder = true;

            Debug.Log(
                "Created HarvestCrop work order for "
                + pCropInstance.CropType
                + " at "
                + cropCoordinates,
                this);
        }
    }
}