using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders structural block visuals from grid tile block data.
/// This stays separate from world generation so it can rebuild visuals
/// whenever the grid data changes.
/// </summary>
public class BlockRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager m_gridManager;
    [SerializeField] private GameObject m_blockPrefab;

    [Header("Block Definitions")]
    [SerializeField] private List<BlockDefinition> m_blockDefinitions = new List<BlockDefinition>();

    private readonly Dictionary<BlockType, BlockDefinition> m_blockDefinitionLookup = new();
    private readonly List<GameObject> m_spawnedBlocks = new List<GameObject>();

    private void Awake()
    {
        BuildBlockDefinitionLookup();
    }

    /// <summary>
    /// Rebuilds all rendered block visuals from current grid tile block data.
    /// Existing spawned visuals are cleared first.
    /// </summary>
    public void RebuildBlocks()
    {
        ClearBlocks();

        if (m_gridManager == null)
        {
            Debug.LogError("BlockRenderer is missing a GridManager reference.", this);
            return;
        }

        if (m_blockPrefab == null)
        {
            Debug.LogError("BlockRenderer is missing a block prefab reference.", this);
            return;
        }

        foreach (KeyValuePair<Vector2Int, GridTile> tileEntry in m_gridManager.Tiles)
        {
            GridTile tile = tileEntry.Value;

            if (tile == null)
            {
                continue;
            }

            if (tile.BlockType == BlockType.None)
            {
                continue;
            }

            SpawnBlockForTile(tile);
        }
    }

    /// <summary>
    /// Clears all currently spawned block visuals.
    /// </summary>
    public void ClearBlocks()
    {
        for (int i = 0; i < m_spawnedBlocks.Count; i++)
        {
            if (m_spawnedBlocks[i] == null)
            {
                continue;
            }

            Destroy(m_spawnedBlocks[i]);
        }

        m_spawnedBlocks.Clear();
    }

    /// <summary>
    /// Builds the lookup used to match block types to reusable block definitions.
    /// </summary>
    private void BuildBlockDefinitionLookup()
    {
        m_blockDefinitionLookup.Clear();

        foreach (BlockDefinition blockDefinition in m_blockDefinitions)
        {
            if (blockDefinition == null)
            {
                continue;
            }

            if (m_blockDefinitionLookup.ContainsKey(blockDefinition.BlockType))
            {
                Debug.LogWarning("Duplicate BlockDefinition found for BlockType: " + blockDefinition.BlockType, this);
                continue;
            }

            m_blockDefinitionLookup.Add(blockDefinition.BlockType, blockDefinition);
        }
    }

    /// <summary>
    /// Spawns and initializes a block visual for a tile with block data.
    /// </summary>
    private void SpawnBlockForTile(GridTile tile)
    {
        if (!m_blockDefinitionLookup.TryGetValue(tile.BlockType, out BlockDefinition blockDefinition))
        {
            Debug.LogWarning("No BlockDefinition found for BlockType: " + tile.BlockType, this);
            return;
        }

        Vector3 spawnPosition = tile.WorldPosition;
        GameObject spawnedBlock = Instantiate(m_blockPrefab, spawnPosition, Quaternion.identity, transform);

        Renderer blockRenderer = spawnedBlock.GetComponent<Renderer>();

        if (blockRenderer != null)
        {
            spawnPosition.y += blockRenderer.bounds.extents.y;
            spawnedBlock.transform.position = spawnPosition;
        }

        BlockInstance blockInstance = spawnedBlock.GetComponent<BlockInstance>();

        if (blockInstance == null)
        {
            Debug.LogError("Spawned block prefab is missing a BlockInstance component.", spawnedBlock);
            Destroy(spawnedBlock);
            return;
        }

        blockInstance.Initialize(blockDefinition, tile.Coordinates);
        m_spawnedBlocks.Add(spawnedBlock);
    }
}