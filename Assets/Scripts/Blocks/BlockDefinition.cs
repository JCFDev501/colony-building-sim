using UnityEngine;

[CreateAssetMenu(fileName = "BlockDefinition", menuName = "ColonySim/Blocks/Block Definition")]
public class BlockDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private BlockType m_blockType = BlockType.None;
    [SerializeField] private string m_displayName = "Block";

    [Header("Visuals")]
    [SerializeField] private Material m_material;
    [SerializeField] private Color m_fallbackColor = Color.white;

    [Header("Gameplay")]
    [SerializeField] private int m_maxHitPoints = 1;
    [SerializeField] private bool m_blocksMovement = true;
    [SerializeField] private bool m_blocksBuilding = true;

    public BlockType BlockType
    {
        get { return m_blockType; }
    }

    public string DisplayName
    {
        get { return m_displayName; }
    }

    public Material Material
    {
        get { return m_material; }
    }

    public Color FallbackColor
    {
        get { return m_fallbackColor; }
    }

    public int MaxHitPoints
    {
        get { return m_maxHitPoints; }
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