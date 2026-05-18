using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders non-block world object visuals from grid tile data.
/// </summary>
public class WorldObjectRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager m_gridManager;

    [Header("World Object Definitions")]
    [SerializeField] private List<WorldObjectDefinition> m_worldObjectDefinitions = new List<WorldObjectDefinition>();

    private readonly Dictionary<WorldObjectType, WorldObjectDefinition> m_worldObjectDefinitionLookup = new();
    private readonly Dictionary<Vector2Int, GameObject> m_spawnedWorldObjectLookup = new();
    private readonly List<GameObject> m_spawnedWorldObjects = new List<GameObject>();

    private void Awake()
    {
        BuildWorldObjectDefinitionLookup();
    }

    /// <summary>
    /// Rebuilds all rendered world object visuals from current grid tile data.
    /// Existing spawned visuals are cleared first.
    /// </summary>
    public void RebuildWorldObjects()
    {
        ClearWorldObjects();

        if (m_gridManager == null)
        {
            Debug.LogError("WorldObjectRenderer is missing a GridManager reference.", this);
            return;
        }

        foreach (KeyValuePair<Vector2Int, GridTile> tileEntry in m_gridManager.Tiles)
        {
            GridTile tile = tileEntry.Value;

            if (tile == null)
            {
                continue;
            }

            if (tile.WorldObjectType == WorldObjectType.None)
            {
                continue;
            }

            SpawnWorldObjectForTile(tile);
        }
    }

    /// <summary>
    /// Removes the spawned world object visual at the requested tile coordinates.
    /// This is intended for runtime gameplay changes such as cutting one tree.
    /// </summary>
    public void RemoveWorldObjectAt(Vector2Int coordinates)
    {
        if (!m_spawnedWorldObjectLookup.TryGetValue(coordinates, out GameObject spawnedWorldObject))
        {
            return;
        }

        if (spawnedWorldObject != null)
        {
            Destroy(spawnedWorldObject);
        }

        m_spawnedWorldObjectLookup.Remove(coordinates);
        m_spawnedWorldObjects.Remove(spawnedWorldObject);
    }

    /// <summary>
    /// Clears all currently spawned world object visuals.
    /// </summary>
    public void ClearWorldObjects()
    {
        for (int i = 0; i < m_spawnedWorldObjects.Count; i++)
        {
            if (m_spawnedWorldObjects[i] == null)
            {
                continue;
            }

            Destroy(m_spawnedWorldObjects[i]);
        }

        m_spawnedWorldObjects.Clear();
        m_spawnedWorldObjectLookup.Clear();
    }

    /// <summary>
    /// Builds the lookup used to match world object types to reusable definitions.
    /// </summary>
    private void BuildWorldObjectDefinitionLookup()
    {
        m_worldObjectDefinitionLookup.Clear();

        foreach (WorldObjectDefinition worldObjectDefinition in m_worldObjectDefinitions)
        {
            if (worldObjectDefinition == null)
            {
                continue;
            }

            if (m_worldObjectDefinitionLookup.ContainsKey(worldObjectDefinition.WorldObjectType))
            {
                Debug.LogWarning("Duplicate WorldObjectDefinition found for WorldObjectType: " + worldObjectDefinition.WorldObjectType, this);
                continue;
            }

            m_worldObjectDefinitionLookup.Add(worldObjectDefinition.WorldObjectType, worldObjectDefinition);
        }
    }

    /// <summary>
    /// Spawns and initializes a world object visual for a tile with world object data.
    /// </summary>
    private void SpawnWorldObjectForTile(GridTile tile)
    {
        if (!m_worldObjectDefinitionLookup.TryGetValue(tile.WorldObjectType, out WorldObjectDefinition worldObjectDefinition))
        {
            Debug.LogWarning("No WorldObjectDefinition found for WorldObjectType: " + tile.WorldObjectType, this);
            return;
        }

        if (worldObjectDefinition.Prefab == null)
        {
            Debug.LogWarning("WorldObjectDefinition is missing a prefab for WorldObjectType: " + worldObjectDefinition.WorldObjectType, this);
            return;
        }

        Vector3 spawnPosition = tile.WorldPosition;
        GameObject spawnedWorldObject = Instantiate(worldObjectDefinition.Prefab, spawnPosition, Quaternion.identity, transform);

        WorldObjectInstance worldObjectInstance = spawnedWorldObject.GetComponent<WorldObjectInstance>();

        if (worldObjectInstance == null)
        {
            Debug.LogError("Spawned world object prefab is missing a WorldObjectInstance component.", spawnedWorldObject);
            Destroy(spawnedWorldObject);
            return;
        }

        worldObjectInstance.Initialize(worldObjectDefinition, tile.Coordinates);
        m_spawnedWorldObjects.Add(spawnedWorldObject);
        m_spawnedWorldObjectLookup[tile.Coordinates] = spawnedWorldObject;
    }
}