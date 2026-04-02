using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages generation, storage, and debug visualization of a square grid.
/// The owning GameObject's transform position is used as the center of the grid.
/// This class also provides helper methods for querying and updating tile gameplay state.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int m_gridWidth = 10;
    [SerializeField] private int m_gridHeight = 10;
    [SerializeField] private float m_cellSize = 1.0f;
    [SerializeField] private bool m_generateOnStart = true;
    [SerializeField] private bool m_showSceneDebugGrid = true;

    // Stores generated tiles by grid coordinate for fast lookup.
    private readonly Dictionary<Vector2Int, GridTile> m_tiles = new();

    // Cardinal neighbor offsets for prototype movement and pathfinding.
    private static readonly Vector2Int[] s_cardinalNeighborOffsets =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
    };

    /// <summary>
    /// Provides read-only access to the generated tiles.
    /// </summary>
    public IReadOnlyDictionary<Vector2Int, GridTile> Tiles
    {
        get { return m_tiles; }
    }

    /// <summary>
    /// Generates the grid automatically when play begins if enabled.
    /// </summary>
    private void Start()
    {
        if (m_generateOnStart)
        {
            GenerateGrid();
        }
    }

    /// <summary>
    /// Generates tile data for the configured grid size using the current transform as the grid center.
    /// Existing tile data is cleared before regeneration.
    /// </summary>
    public void GenerateGrid()
    {
        m_tiles.Clear();

        for (int y = 0; y < m_gridHeight; y++)
        {
            for (int x = 0; x < m_gridWidth; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                Vector3 worldPosition = GetWorldPosition(coordinates);

                GridTile tile = new GridTile(coordinates, worldPosition);
                m_tiles.Add(coordinates, tile);
            }
        }

        Debug.Log("Generated grid with " + m_tiles.Count + " tiles.");
    }

    /// <summary>
    /// Returns the world-space position of the lower-left corner origin used for tile calculations.
    /// </summary>
    private Vector3 GetGridStartPosition()
    {
        Vector3 origin = transform.position;

        float gridWorldWidth = m_gridWidth * m_cellSize;
        float gridWorldHeight = m_gridHeight * m_cellSize;

        return origin - new Vector3(gridWorldWidth * 0.5f, 0.0f, gridWorldHeight * 0.5f);
    }

    /// <summary>
    /// Returns the world-space center position of the given tile coordinates.
    /// </summary>
    public Vector3 GetWorldPosition(Vector2Int coordinates)
    {
        if (!IsInBounds(coordinates))
        {
            return Vector3.zero;
        }

        Vector3 startPosition = GetGridStartPosition();

        float xOffset = (coordinates.x * m_cellSize) + (m_cellSize * 0.5f);
        float zOffset = (coordinates.y * m_cellSize) + (m_cellSize * 0.5f);

        return startPosition + new Vector3(xOffset, 0.0f, zOffset);
    }

    /// <summary>
    /// Attempts to convert a world-space position into grid coordinates.
    /// </summary>
    public bool TryGetCoordinatesFromWorldPosition(Vector3 worldPosition, out Vector2Int coordinates)
    {
        coordinates = Vector2Int.zero;

        Vector3 startPosition = GetGridStartPosition();
        Vector3 localPosition = worldPosition - startPosition;

        int x = Mathf.FloorToInt(localPosition.x / m_cellSize);
        int y = Mathf.FloorToInt(localPosition.z / m_cellSize);

        coordinates = new Vector2Int(x, y);

        if (!IsInBounds(coordinates))
        {
            coordinates = Vector2Int.zero;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to intersect a ray with the grid plane.
    /// The grid plane is assumed to lie flat on the XZ plane at this transform's Y level.
    /// </summary>
    public bool TryGetWorldPositionFromRay(Ray ray, out Vector3 worldPosition)
    {
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0.0f, transform.position.y, 0.0f));

        if (!gridPlane.Raycast(ray, out float enter))
        {
            worldPosition = Vector3.zero;
            return false;
        }

        worldPosition = ray.GetPoint(enter);
        return true;
    }

    /// <summary>
    /// Attempts to convert a screen point into a world-space point on the grid plane using the provided camera.
    /// </summary>
    public bool TryGetWorldPositionFromScreenPoint(Camera cameraComponent, Vector2 screenPoint, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (cameraComponent == null)
        {
            return false;
        }

        Ray ray = cameraComponent.ScreenPointToRay(screenPoint);
        return TryGetWorldPositionFromRay(ray, out worldPosition);
    }

    /// <summary>
    /// Attempts to convert a screen point into grid coordinates using the provided camera.
    /// </summary>
    public bool TryGetCoordinatesFromScreenPoint(Camera cameraComponent, Vector2 screenPoint, out Vector2Int coordinates)
    {
        coordinates = Vector2Int.zero;

        if (!TryGetWorldPositionFromScreenPoint(cameraComponent, screenPoint, out Vector3 worldPosition))
        {
            return false;
        }

        return TryGetCoordinatesFromWorldPosition(worldPosition, out coordinates);
    }

    /// <summary>
    /// Attempts to convert a screen point into both a world-space position on the grid plane and grid coordinates.
    /// </summary>
    public bool TryGetCoordinatesFromScreenPoint(
        Camera cameraComponent,
        Vector2 screenPoint,
        out Vector2Int coordinates,
        out Vector3 worldPosition)
    {
        coordinates = Vector2Int.zero;
        worldPosition = Vector3.zero;

        if (!TryGetWorldPositionFromScreenPoint(cameraComponent, screenPoint, out worldPosition))
        {
            return false;
        }

        return TryGetCoordinatesFromWorldPosition(worldPosition, out coordinates);
    }

    /// <summary>
    /// Attempts to retrieve a tile by its grid coordinates.
    /// </summary>
    public bool TryGetTile(Vector2Int coordinates, out GridTile tile)
    {
        return m_tiles.TryGetValue(coordinates, out tile);
    }

    /// <summary>
    /// Attempts to retrieve a tile from a world-space position.
    /// </summary>
    public bool TryGetTileFromWorldPosition(Vector3 worldPosition, out GridTile tile)
    {
        tile = null;

        if (!TryGetCoordinatesFromWorldPosition(worldPosition, out Vector2Int coordinates))
        {
            return false;
        }

        return TryGetTile(coordinates, out tile);
    }

    /// <summary>
    /// Returns whether the provided coordinates fall within the current grid bounds.
    /// </summary>
    public bool IsInBounds(Vector2Int coordinates)
    {
        return coordinates.x >= 0 &&
               coordinates.x < m_gridWidth &&
               coordinates.y >= 0 &&
               coordinates.y < m_gridHeight;
    }

    /// <summary>
    /// Returns whether a tile exists at the provided coordinates.
    /// </summary>
    public bool HasTile(Vector2Int coordinates)
    {
        return m_tiles.ContainsKey(coordinates);
    }

    /// <summary>
    /// Returns whether the tile at the given coordinates is currently walkable.
    /// </summary>
    public bool IsWalkable(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        return tile.IsWalkable;
    }

    /// <summary>
    /// Returns whether the tile at the given coordinates is currently occupied.
    /// </summary>
    public bool IsOccupied(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        return tile.IsOccupied;
    }

    /// <summary>
    /// Returns whether the tile at the given coordinates is currently reserved.
    /// </summary>
    public bool IsReserved(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        return tile.IsReserved;
    }

    /// <summary>
    /// Returns the current content type for the tile at the given coordinates.
    /// Invalid coordinates return Empty.
    /// </summary>
    public TileContentType GetContentType(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return TileContentType.Empty;
        }

        return tile.ContentType;
    }

    /// <summary>
    /// Returns whether the tile at the given coordinates currently contains no gameplay content.
    /// </summary>
    public bool IsEmpty(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        return tile.IsEmpty;
    }

    /// <summary>
    /// Returns whether the tile can currently be entered based on simple prototype rules.
    /// A tile must exist, be walkable, not be occupied, and not be reserved.
    /// </summary>
    public bool CanEnterTile(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        return tile.IsWalkable &&
               !tile.IsOccupied &&
               !tile.IsReserved;
    }

    /// <summary>
    /// Sets whether the tile at the given coordinates is walkable.
    /// Returns false if the tile does not exist.
    /// </summary>
    public bool SetWalkable(Vector2Int coordinates, bool isWalkable)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.IsWalkable = isWalkable;
        return true;
    }

    /// <summary>
    /// Sets whether the tile at the given coordinates is occupied.
    /// Returns false if the tile does not exist.
    /// </summary>
    public bool SetOccupied(Vector2Int coordinates, bool isOccupied)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.IsOccupied = isOccupied;
        return true;
    }

    /// <summary>
    /// Sets whether the tile at the given coordinates is reserved.
    /// Returns false if the tile does not exist.
    /// </summary>
    public bool SetReserved(Vector2Int coordinates, bool isReserved)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.IsReserved = isReserved;
        return true;
    }

    /// <summary>
    /// Sets the content type for the tile at the given coordinates.
    /// Returns false if the tile does not exist.
    /// </summary>
    public bool SetContentType(Vector2Int coordinates, TileContentType contentType)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.ContentType = contentType;
        return true;
    }

    /// <summary>
    /// Clears the mutable state for the tile at the given coordinates.
    /// Returns false if the tile does not exist.
    /// </summary>
    public bool ClearTileState(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.ClearDynamicState();
        return true;
    }

    /// <summary>
    /// Applies a simple natural blocker state to the tile.
    /// This is a prototype helper for future environment and PCG work.
    /// </summary>
    public bool SetNaturalBlocker(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.ContentType = TileContentType.NaturalBlocker;
        tile.IsWalkable = false;
        tile.IsOccupied = false;
        tile.IsReserved = false;
        return true;
    }

    /// <summary>
    /// Applies a simple resource state to the tile.
    /// This keeps the tile walkability explicit instead of hardcoding one rule forever.
    /// </summary>
    public bool SetResourceTile(Vector2Int coordinates, bool isWalkable)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.ContentType = TileContentType.Resource;
        tile.IsWalkable = isWalkable;
        tile.IsOccupied = false;
        tile.IsReserved = false;
        return true;
    }

    /// <summary>
    /// Applies a simple structure state to the tile.
    /// Structures are treated as non-walkable in the prototype by default.
    /// </summary>
    public bool SetStructureTile(Vector2Int coordinates)
    {
        if (!TryGetTile(coordinates, out GridTile tile))
        {
            return false;
        }

        tile.ContentType = TileContentType.Structure;
        tile.IsWalkable = false;
        tile.IsOccupied = false;
        tile.IsReserved = false;
        return true;
    }

    /// <summary>
    /// Returns the valid cardinal neighbor coordinates for the given tile.
    /// Out-of-bounds neighbors are excluded.
    /// </summary>
    public List<Vector2Int> GetNeighborCoordinates(Vector2Int coordinates)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        if (!IsInBounds(coordinates))
        {
            return neighbors;
        }

        foreach (Vector2Int offset in s_cardinalNeighborOffsets)
        {
            Vector2Int neighborCoordinates = coordinates + offset;

            if (!IsInBounds(neighborCoordinates))
            {
                continue;
            }

            neighbors.Add(neighborCoordinates);
        }

        return neighbors;
    }

    /// <summary>
    /// Returns the valid cardinal neighbor tiles for the given tile.
    /// Out-of-bounds neighbors are excluded.
    /// </summary>
    public List<GridTile> GetNeighborTiles(Vector2Int coordinates)
    {
        List<GridTile> neighbors = new List<GridTile>();

        foreach (Vector2Int neighborCoordinates in GetNeighborCoordinates(coordinates))
        {
            if (TryGetTile(neighborCoordinates, out GridTile tile))
            {
                neighbors.Add(tile);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Returns the valid cardinal neighbor coordinates that can currently be entered.
    /// This is useful for future movement and pathfinding work.
    /// </summary>
    public List<Vector2Int> GetEnterableNeighborCoordinates(Vector2Int coordinates)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        foreach (Vector2Int neighborCoordinates in GetNeighborCoordinates(coordinates))
        {
            if (CanEnterTile(neighborCoordinates))
            {
                neighbors.Add(neighborCoordinates);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Returns the valid cardinal neighbor tiles that can currently be entered.
    /// This is useful for future movement and pathfinding work.
    /// </summary>
    public List<GridTile> GetEnterableNeighborTiles(Vector2Int coordinates)
    {
        List<GridTile> neighbors = new List<GridTile>();

        foreach (Vector2Int neighborCoordinates in GetEnterableNeighborCoordinates(coordinates))
        {
            if (TryGetTile(neighborCoordinates, out GridTile tile))
            {
                neighbors.Add(tile);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Returns whether two tiles are cardinally adjacent.
    /// Diagonals do not count as adjacent in the prototype rules.
    /// </summary>
    public bool AreTilesAdjacent(Vector2Int firstCoordinates, Vector2Int secondCoordinates)
    {
        if (!IsInBounds(firstCoordinates) || !IsInBounds(secondCoordinates))
        {
            return false;
        }

        Vector2Int delta = secondCoordinates - firstCoordinates;
        int manhattanDistance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

        return manhattanDistance == 1;
    }

    /// <summary>
    /// Regenerates the grid from the component context menu in the Inspector.
    /// </summary>
    [ContextMenu("Regenerate Grid")]
    private void RegenerateGrid()
    {
        GenerateGrid();
    }

    /// <summary>
    /// Draws a wireframe preview of the grid in the Scene view while this object is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!m_showSceneDebugGrid)
        {
            return;
        }

        if (m_gridWidth <= 0 || m_gridHeight <= 0 || m_cellSize <= 0.0f)
        {
            return;
        }

        for (int y = 0; y < m_gridHeight; y++)
        {
            for (int x = 0; x < m_gridWidth; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                Vector3 worldPosition = GetWorldPosition(coordinates);
                Vector3 tileSize = new Vector3(m_cellSize, 0.05f, m_cellSize);

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(worldPosition, tileSize);
            }
        }
    }
}