using UnityEngine;

namespace ColonyBuildingSim.Buildables
{
    /// <summary>
    /// Defines authored construction data for a buildable structure.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BuildableDefinition",
        menuName = "ColonySim/Buildables/Buildable Definition")]
    public class BuildableDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private BuildableType m_buildableType = BuildableType.Campfire;
        [SerializeField] private string m_displayName = "New Buildable";

        [Header("Visuals")]
        [SerializeField] private GameObject m_prefab;
        [SerializeField] private Vector3 m_placementOffset = Vector3.zero;

        [Header("Construction")]
        [SerializeField] private int m_requiredWood = 0;
        [SerializeField] private int m_requiredStone = 0;
        [SerializeField] private float m_workDuration = 3.0f;

        [Header("Tile Blocking")]
        [SerializeField] private bool m_blocksMovement = true;
        [SerializeField] private bool m_blocksBuilding = true;

        public BuildableType BuildableType
        {
            get { return m_buildableType; }
        }
        
        public Vector3 PlacementOffset
        {
            get { return m_placementOffset; }
        }

        public string DisplayName
        {
            get { return m_displayName; }
        }

        public GameObject Prefab
        {
            get { return m_prefab; }
        }

        public int RequiredWood
        {
            get { return m_requiredWood; }
        }

        public int RequiredStone
        {
            get { return m_requiredStone; }
        }

        public float WorkDuration
        {
            get { return m_workDuration; }
        }

        public bool BlocksMovement
        {
            get { return m_blocksMovement; }
        }

        public bool BlocksBuilding
        {
            get { return m_blocksBuilding; }
        }
    }
}