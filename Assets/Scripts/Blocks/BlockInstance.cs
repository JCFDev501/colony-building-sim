using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private BlockDefinition m_blockDefinition;
    [SerializeField] private int m_currentHitPoints = 0;
    [SerializeField] private Vector2Int m_tileCoordinates;

    public BlockDefinition BlockDefinition
    {
        get { return m_blockDefinition; }
    }

    public int CurrentHitPoints
    {
        get { return m_currentHitPoints; }
    }

    public Vector2Int TileCoordinates
    {
        get { return m_tileCoordinates; }
    }

    public void Initialize(BlockDefinition blockDefinition, Vector2Int tileCoordinates)
    {
        if (blockDefinition == null)
        {
            Debug.LogError("BlockInstance.Initialize failed: blockDefinition is null.", this);
            return;
        }

        m_blockDefinition = blockDefinition;
        m_currentHitPoints = blockDefinition.MaxHitPoints;
        m_tileCoordinates = tileCoordinates;

        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (m_blockDefinition == null)
        {
            return;
        }

        Renderer blockRenderer = GetComponentInChildren<Renderer>();

        if (blockRenderer == null)
        {
            Debug.LogWarning("BlockInstance could not find a Renderer to apply visuals.", this);
            return;
        }

        if (m_blockDefinition.Material != null)
        {
            blockRenderer.sharedMaterial = m_blockDefinition.Material;
            return;
        }

        if (blockRenderer.sharedMaterial != null && blockRenderer.sharedMaterial.HasProperty("_BaseColor"))
        {
            blockRenderer.sharedMaterial.color = m_blockDefinition.FallbackColor;
            return;
        }

        Debug.LogWarning("BlockDefinition has no material assigned and the renderer material does not support color fallback.", this);
    }
}