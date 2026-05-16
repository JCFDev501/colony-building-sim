using UnityEngine;

namespace ColonyBuildingSim.Crops
{
    /// <summary>
    /// Defines authored crop data for planting, growth, and harvesting.
    /// Crop definitions let farming systems support new crop types without hardcoding every crop.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CropDefinition",
        menuName = "ColonySim/Crops/Crop Definition")]
    public class CropDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private CropType m_cropType = CropType.BerryBush;
        [SerializeField] private string m_displayName = "New Crop";

        [Header("Visuals")]
        [SerializeField] private GameObject m_growingPrefab;
        [SerializeField] private GameObject m_maturePrefab;
        [SerializeField] private Vector3 m_placementOffset = Vector3.zero;

        [Header("Work")]
        [SerializeField] private float m_plantWorkDuration = 3.0f;
        [SerializeField] private float m_harvestWorkDuration = 3.0f;

        [Header("Growth")]
        [SerializeField] private float m_growthDuration = 45.0f;
        [SerializeField] private int m_foodYield = 3;

        [Header("Tile Blocking")]
        [SerializeField] private bool m_blocksMovement = false;

        public CropType CropType
        {
            get { return m_cropType; }
        }

        public string DisplayName
        {
            get { return m_displayName; }
        }

        public GameObject GrowingPrefab
        {
            get { return m_growingPrefab; }
        }

        public GameObject MaturePrefab
        {
            get { return m_maturePrefab; }
        }

        public Vector3 PlacementOffset
        {
            get { return m_placementOffset; }
        }

        public float PlantWorkDuration
        {
            get { return m_plantWorkDuration; }
        }

        public float HarvestWorkDuration
        {
            get { return m_harvestWorkDuration; }
        }

        public float GrowthDuration
        {
            get { return m_growthDuration; }
        }

        public int FoodYield
        {
            get { return m_foodYield; }
        }

        public bool BlocksMovement
        {
            get { return m_blocksMovement; }
        }
    }
}