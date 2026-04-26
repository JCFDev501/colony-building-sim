using UnityEngine;

/// <summary>
/// Defines the base ground terrain for a tile.
/// Terrain describes what the ground is, separate from content placed on top of it.
/// </summary>
public enum TileTerrainType
{
    Grass,
    Dirt,
    ForestFloor,
    Water
}

/// <summary>
/// Defines the broad water-distance classification used to shape later terrain assignment.
/// Water tiles themselves use None because near/mid/far only applies to non-water land tiles.
/// </summary>
public enum TileWaterDistanceBand
{
    None,
    Near,
    Mid,
    Far
}

/// <summary>
/// Defines the broad kind of gameplay content currently associated with a tile.
/// This stays intentionally simple for the prototype so tiles can change state
/// without requiring many specialized tile classes.
/// </summary>
public enum TileContentType
{
    Empty,
    NaturalBlocker,
    Resource,
    Structure
}

/// <summary>
/// Defines the specific non-block world object currently associated with a tile.
/// This stays separate from broad content type so the game can distinguish
/// exact world-object identity such as trees later.
/// </summary>
public enum WorldObjectType
{
    None,
    Tree
}

/// <summary>
/// Stores gameplay-relevant state for a single grid tile.
/// A tile keeps its identity data for its lifetime, while gameplay state such as
/// terrain, water-distance band, block type, world object type, content, walkability,
/// occupancy, and reservation may change during runtime.
/// </summary>
public class GridTile
{
    private Vector2Int m_coordinates;
    private Vector3 m_worldPosition;
    private TileTerrainType m_terrainType;
    private TileWaterDistanceBand m_waterDistanceBand;
    private BlockType m_blockType;
    private WorldObjectType m_worldObjectType;
    private bool m_isWalkable;
    private bool m_isOccupied;
    private bool m_isReserved;
    private TileContentType m_contentType;

    /// <summary>
    /// Creates a new tile with default prototype gameplay state.
    /// Tiles begin as grass, with no water-distance classification, no block,
    /// no world object, empty, walkable, unoccupied, and unreserved unless specified otherwise.
    /// </summary>
    public GridTile(Vector2Int coordinates, Vector3 worldPosition)
    {
        m_coordinates = coordinates;
        m_worldPosition = worldPosition;
        m_terrainType = TileTerrainType.Grass;
        m_waterDistanceBand = TileWaterDistanceBand.None;
        m_blockType = BlockType.None;
        m_worldObjectType = WorldObjectType.None;
        m_isWalkable = true;
        m_isOccupied = false;
        m_isReserved = false;
        m_contentType = TileContentType.Empty;
    }

    /// <summary>
    /// Gets the grid coordinates that identify this tile.
    /// </summary>
    public Vector2Int Coordinates
    {
        get { return m_coordinates; }
    }

    /// <summary>
    /// Gets the world-space center position of this tile.
    /// </summary>
    public Vector3 WorldPosition
    {
        get { return m_worldPosition; }
    }

    /// <summary>
    /// Gets or sets the base terrain type for this tile.
    /// Terrain is stored separately from gameplay content placed on the tile.
    /// </summary>
    public TileTerrainType TerrainType
    {
        get { return m_terrainType; }
        set { m_terrainType = value; }
    }

    /// <summary>
    /// Gets or sets the broad water-distance band for this tile.
    /// This is used later for terrain assignment. Water tiles should generally use None.
    /// </summary>
    public TileWaterDistanceBand WaterDistanceBand
    {
        get { return m_waterDistanceBand; }
        set { m_waterDistanceBand = value; }
    }

    /// <summary>
    /// Gets or sets the structural block identity for this tile.
    /// This is stored separately from terrain and broad content classification.
    /// </summary>
    public BlockType BlockType
    {
        get { return m_blockType; }
        set { m_blockType = value; }
    }

    /// <summary>
    /// Gets or sets the specific non-block world object identity for this tile.
    /// This is stored separately from terrain, block type, and broad content classification.
    /// </summary>
    public WorldObjectType WorldObjectType
    {
        get { return m_worldObjectType; }
        set { m_worldObjectType = value; }
    }

    /// <summary>
    /// Gets or sets whether this tile may currently be traversed.
    /// </summary>
    public bool IsWalkable
    {
        get { return m_isWalkable; }
        set { m_isWalkable = value; }
    }

    /// <summary>
    /// Gets or sets whether this tile is currently occupied.
    /// </summary>
    public bool IsOccupied
    {
        get { return m_isOccupied; }
        set { m_isOccupied = value; }
    }

    /// <summary>
    /// Gets or sets whether this tile is currently reserved.
    /// </summary>
    public bool IsReserved
    {
        get { return m_isReserved; }
        set { m_isReserved = value; }
    }

    /// <summary>
    /// Gets or sets the current broad gameplay content category for this tile.
    /// </summary>
    public TileContentType ContentType
    {
        get { return m_contentType; }
        set { m_contentType = value; }
    }

    /// <summary>
    /// Returns whether the tile currently contains no broad gameplay content.
    /// </summary>
    public bool IsEmpty
    {
        get { return m_contentType == TileContentType.Empty; }
    }

    /// <summary>
    /// Resets mutable gameplay state back to an empty, usable prototype tile.
    /// Identity data is preserved.
    /// Terrain, block type, and world object type are preserved so world-generation results
    /// are not lost accidentally during general gameplay-state resets.
    /// Water-distance band is reset so later generation stages can recompute it cleanly.
    /// </summary>
    public void ClearDynamicState()
    {
        m_waterDistanceBand = TileWaterDistanceBand.None;
        m_isWalkable = true;
        m_isOccupied = false;
        m_isReserved = false;
        m_contentType = TileContentType.Empty;
    }
}