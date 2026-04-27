using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores generated result data for a single tile independently from a live GridTile.
/// This is used as a temporary world-generation data model before results are applied
/// back to the runtime grid on the main thread.
/// </summary>
public class WorldGenerationTileResult
{
    public Vector2Int Coordinates { get; set; }

    public TileTerrainType TerrainType { get; set; } = TileTerrainType.Grass;

    public TileWaterDistanceBand WaterDistanceBand { get; set; } = TileWaterDistanceBand.None;

    public BlockType BlockType { get; set; } = BlockType.None;

    public WorldObjectType WorldObjectType { get; set; } = WorldObjectType.None;

    public TileContentType ContentType { get; set; } = TileContentType.Empty;

    public bool IsWalkable { get; set; } = true;

    public bool IsOccupied { get; set; } = false;

    public bool IsReserved { get; set; } = false;
}

/// <summary>
/// Stores the generated world result independently from live GridTile objects.
/// This model is intended to be written by generation stages first, then applied
/// back onto the runtime grid later.
/// </summary>
public class WorldGenerationResult
{
    private readonly Dictionary<Vector2Int, WorldGenerationTileResult> m_tileResults = new();

    /// <summary>
    /// Provides read-only access to generated tile results.
    /// </summary>
    public IReadOnlyDictionary<Vector2Int, WorldGenerationTileResult> TileResults
    {
        get { return m_tileResults; }
    }

    /// <summary>
    /// Returns how many tile results are currently stored.
    /// </summary>
    public int Count
    {
        get { return m_tileResults.Count; }
    }

    /// <summary>
    /// Adds or replaces the generated result for a tile coordinate.
    /// </summary>
    public void SetTileResult(WorldGenerationTileResult tileResult)
    {
        if (tileResult == null)
        {
            return;
        }

        m_tileResults[tileResult.Coordinates] = tileResult;
    }

    /// <summary>
    /// Returns whether a generated result exists for the given tile coordinates.
    /// </summary>
    public bool HasTileResult(Vector2Int coordinates)
    {
        return m_tileResults.ContainsKey(coordinates);
    }

    /// <summary>
    /// Attempts to retrieve the generated result for the given tile coordinates.
    /// </summary>
    public bool TryGetTileResult(Vector2Int coordinates, out WorldGenerationTileResult tileResult)
    {
        return m_tileResults.TryGetValue(coordinates, out tileResult);
    }

    /// <summary>
    /// Returns the terrain type for the given coordinates.
    /// Invalid coordinates return Grass as the default prototype terrain.
    /// </summary>
    public TileTerrainType GetTerrainType(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return TileTerrainType.Grass;
        }

        return tileResult.TerrainType;
    }

    /// <summary>
    /// Sets the terrain type for the given coordinates.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetTerrainType(Vector2Int coordinates, TileTerrainType terrainType)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.TerrainType = terrainType;
        return true;
    }

    /// <summary>
    /// Returns the water-distance band for the given coordinates.
    /// Invalid coordinates return None.
    /// </summary>
    public TileWaterDistanceBand GetWaterDistanceBand(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return TileWaterDistanceBand.None;
        }

        return tileResult.WaterDistanceBand;
    }

    /// <summary>
    /// Sets the water-distance band for the given coordinates.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetWaterDistanceBand(Vector2Int coordinates, TileWaterDistanceBand waterDistanceBand)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.WaterDistanceBand = waterDistanceBand;
        return true;
    }

    /// <summary>
    /// Returns the structural block type for the given coordinates.
    /// Invalid coordinates return None.
    /// </summary>
    public BlockType GetBlockType(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return BlockType.None;
        }

        return tileResult.BlockType;
    }

    /// <summary>
    /// Sets the structural block type for the given coordinates.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetBlockType(Vector2Int coordinates, BlockType blockType)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.BlockType = blockType;
        return true;
    }

    /// <summary>
    /// Returns the world object type for the given coordinates.
    /// Invalid coordinates return None.
    /// </summary>
    public WorldObjectType GetWorldObjectType(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return WorldObjectType.None;
        }

        return tileResult.WorldObjectType;
    }

    /// <summary>
    /// Sets the world object type for the given coordinates.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetWorldObjectType(Vector2Int coordinates, WorldObjectType worldObjectType)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.WorldObjectType = worldObjectType;
        return true;
    }

    /// <summary>
    /// Returns the current broad content type for the given coordinates.
    /// Invalid coordinates return Empty.
    /// </summary>
    public TileContentType GetContentType(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return TileContentType.Empty;
        }

        return tileResult.ContentType;
    }

    /// <summary>
    /// Sets the broad content type for the given coordinates.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetContentType(Vector2Int coordinates, TileContentType contentType)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.ContentType = contentType;
        return true;
    }

    /// <summary>
    /// Returns whether the given coordinates are currently walkable.
    /// Invalid coordinates return false.
    /// </summary>
    public bool IsWalkable(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        return tileResult.IsWalkable;
    }

    /// <summary>
    /// Sets whether the given coordinates are walkable.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetWalkable(Vector2Int coordinates, bool isWalkable)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.IsWalkable = isWalkable;
        return true;
    }

    /// <summary>
    /// Returns whether the given coordinates are currently occupied.
    /// Invalid coordinates return false.
    /// </summary>
    public bool IsOccupied(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        return tileResult.IsOccupied;
    }

    /// <summary>
    /// Sets whether the given coordinates are occupied.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetOccupied(Vector2Int coordinates, bool isOccupied)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.IsOccupied = isOccupied;
        return true;
    }

    /// <summary>
    /// Returns whether the given coordinates are currently reserved.
    /// Invalid coordinates return false.
    /// </summary>
    public bool IsReserved(Vector2Int coordinates)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        return tileResult.IsReserved;
    }

    /// <summary>
    /// Sets whether the given coordinates are reserved.
    /// Returns false if the tile result does not exist.
    /// </summary>
    public bool SetReserved(Vector2Int coordinates, bool isReserved)
    {
        if (!TryGetTileResult(coordinates, out WorldGenerationTileResult tileResult))
        {
            return false;
        }

        tileResult.IsReserved = isReserved;
        return true;
    }

    /// <summary>
    /// Clears all generated tile results.
    /// </summary>
    public void Clear()
    {
        m_tileResults.Clear();
    }
}