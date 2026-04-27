using UnityEngine;

/// <summary>
/// Stores runtime data for a spawned world object instance.
/// This keeps the spawned prefab linked to its reusable definition and tile location.
/// </summary>
public class WorldObjectInstance : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private WorldObjectDefinition m_worldObjectDefinition;
    [SerializeField] private Vector2Int m_tileCoordinates;

    public WorldObjectDefinition WorldObjectDefinition
    {
        get { return m_worldObjectDefinition; }
    }

    public Vector2Int TileCoordinates
    {
        get { return m_tileCoordinates; }
    }

    /// <summary>
    /// Initializes this runtime instance from a reusable world object definition
    /// and the tile coordinates it belongs to.
    /// </summary>
    public void Initialize(WorldObjectDefinition worldObjectDefinition, Vector2Int tileCoordinates)
    {
        if (worldObjectDefinition == null)
        {
            Debug.LogError("WorldObjectInstance.Initialize failed: worldObjectDefinition is null.", this);
            return;
        }

        m_worldObjectDefinition = worldObjectDefinition;
        m_tileCoordinates = tileCoordinates;
    }
}