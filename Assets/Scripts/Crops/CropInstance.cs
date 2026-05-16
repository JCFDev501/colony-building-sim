using System;
using UnityEngine;

namespace ColonyBuildingSim.Crops
{
    /// <summary>
    /// Stores runtime state for a planted crop.
    /// Crop instances are managed by CropManager during growth and harvest.
    /// </summary>
    public class CropInstance : MonoBehaviour
    {
        [Header("Runtime Crop Data")]
        [SerializeField] private string m_cropId = string.Empty;
        [SerializeField] private CropType m_cropType = CropType.BerryBush;
        [SerializeField] private Vector2Int m_gridCoordinates = Vector2Int.zero;
        [SerializeField] private CropDefinition m_cropDefinition;
        [SerializeField] private float m_growthProgress = 0.0f;
        [SerializeField] private bool m_isMature = false;
        [SerializeField] private bool m_hasHarvestWorkOrder = false;
        [SerializeField] private GameObject m_currentVisualObject;

        public string CropId
        {
            get { return m_cropId; }
        }

        public CropType CropType
        {
            get { return m_cropType; }
        }

        public Vector2Int GridCoordinates
        {
            get { return m_gridCoordinates; }
        }

        public CropDefinition CropDefinition
        {
            get { return m_cropDefinition; }
        }

        public float GrowthProgress
        {
            get { return m_growthProgress; }
        }

        public bool IsMature
        {
            get { return m_isMature; }
        }

        public bool HasHarvestWorkOrder
        {
            get { return m_hasHarvestWorkOrder; }
            set { m_hasHarvestWorkOrder = value; }
        }

        /// <summary>
        /// Initializes this crop after it is planted in the world.
        /// </summary>
        public void Initialize(CropDefinition cropDefinition, Vector2Int gridCoordinates)
        {
            m_cropId = Guid.NewGuid().ToString();
            m_cropDefinition = cropDefinition;
            m_gridCoordinates = gridCoordinates;
            m_growthProgress = 0.0f;
            m_isMature = false;
            m_hasHarvestWorkOrder = false;

            if (cropDefinition != null)
            {
                m_cropType = cropDefinition.CropType;
                SetVisual(cropDefinition.GrowingPrefab);
            }
        }

        /// <summary>
        /// Replaces the current crop visual with the requested visual prefab.
        /// </summary>
        public void SetVisual(GameObject visualPrefab)
        {
            if (m_currentVisualObject != null)
            {
                Destroy(m_currentVisualObject);
                m_currentVisualObject = null;
            }

            if (visualPrefab == null)
            {
                return;
            }

            m_currentVisualObject = Instantiate(visualPrefab, transform.position, transform.rotation, transform);
        }

        /// <summary>
        /// Advances crop growth and returns true when the crop becomes mature this update.
        /// </summary>
        public bool AddGrowth(float growthAmount)
        {
            if (m_isMature)
            {
                return false;
            }

            if (m_cropDefinition == null)
            {
                return false;
            }

            m_growthProgress += Mathf.Max(0.0f, growthAmount);

            if (m_growthProgress < m_cropDefinition.GrowthDuration)
            {
                return false;
            }

            m_growthProgress = m_cropDefinition.GrowthDuration;
            m_isMature = true;
            return true;
        }

        /// <summary>
        /// Resets this crop after harvest so it can grow again without requiring another player plant order.
        /// </summary>
        public void ResetAfterHarvest()
        {
            m_growthProgress = 0.0f;
            m_isMature = false;
            m_hasHarvestWorkOrder = false;

            if (m_cropDefinition != null)
            {
                SetVisual(m_cropDefinition.GrowingPrefab);
            }
        }
    }
}