using UnityEngine;

[CreateAssetMenu(fileName = "WorldObjectDefinition", menuName = "ColonySim/WorldObjects/World Object Definition")]
public class WorldObjectDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private WorldObjectType m_worldObjectType = WorldObjectType.None;
    [SerializeField] private string m_displayName = "World Object";

    [Header("Visuals")]
    [SerializeField] private GameObject m_prefab;

    [Header("Gameplay")]
    [SerializeField] private bool m_blocksMovement = true;
    [SerializeField] private bool m_blocksBuilding = true;
    [SerializeField] private bool m_isCuttable = false;
    [SerializeField] private bool m_isHarvestable = false;
    [SerializeField] private TileContentType m_contentType = TileContentType.Empty;

    public WorldObjectType WorldObjectType
    {
        get { return m_worldObjectType; }
    }

    public string DisplayName
    {
        get { return m_displayName; }
    }

    public GameObject Prefab
    {
        get { return m_prefab; }
    }

    public bool BlocksMovement
    {
        get { return m_blocksMovement; }
    }

    public bool BlocksBuilding
    {
        get { return m_blocksBuilding; }
    }

    public bool IsCuttable
    {
        get { return m_isCuttable; }
    }

    public bool IsHarvestable
    {
        get { return m_isHarvestable; }
    }

    public TileContentType ContentType
    {
        get { return m_contentType; }
    }
}