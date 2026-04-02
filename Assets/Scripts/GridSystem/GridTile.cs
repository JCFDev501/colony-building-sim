using UnityEngine;

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
/// Stores gameplay-relevant state for a single grid tile.
/// A tile keeps its identity data for its lifetime, while gameplay state such as
/// walkability, occupancy, reservation, and content may change during runtime.
/// </summary>
public class GridTile
{
    private Vector2Int m_coordinates;
    private Vector3 m_worldPosition;
    private bool m_isWalkable;
    private bool m_isOccupied;
    private bool m_isReserved;
    private TileContentType m_contentType;

    /// <summary>
    /// Creates a new tile with default prototype gameplay state.
    /// Tiles begin empty, walkable, unoccupied, and unreserved unless specified otherwise.
    /// </summary>
    public GridTile(Vector2Int coordinates, Vector3 worldPosition)
    {
        m_coordinates = coordinates;
        m_worldPosition = worldPosition;
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
    /// Gets or sets the current gameplay content category for this tile.
    /// </summary>
    public TileContentType ContentType
    {
        get { return m_contentType; }
        set { m_contentType = value; }
    }

    /// <summary>
    /// Returns whether the tile currently contains no gameplay content.
    /// </summary>
    public bool IsEmpty
    {
        get { return m_contentType == TileContentType.Empty; }
    }

    /// <summary>
    /// Resets mutable gameplay state back to an empty, usable prototype tile.
    /// Identity data is preserved.
    /// </summary>
    public void ClearDynamicState()
    {
        m_isWalkable = true;
        m_isOccupied = false;
        m_isReserved = false;
        m_contentType = TileContentType.Empty;
    }
}