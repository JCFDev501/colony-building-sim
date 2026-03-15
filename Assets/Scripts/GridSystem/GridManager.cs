using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages generation, storage, and debug visualization of a square grid.
/// The owning GameObject's transform position is used as the grid origin.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int m_gridWidth = 10;
    [SerializeField] private int m_gridHeight = 10;
    [SerializeField] private float m_cellSize = 1.0f;
    [SerializeField] private bool m_generateOnStart = true;

    // Stores generated tiles by grid coordinate for fast lookup.
    private readonly Dictionary<Vector2Int, GridTile> m_tiles = new();

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
    /// Generates tile data for the configured grid size using the current transform as the origin.
    /// Existing tile data is cleared before regeneration.
    /// </summary>
    public void GenerateGrid()
    {
        m_tiles.Clear();

        Vector3 origin = transform.position;

        for (int y = 0; y < m_gridHeight; y++)
        {
            for (int x = 0; x < m_gridWidth; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);

                // Offset each tile to the center of its cell.
                float xOffset = (x * m_cellSize) + (m_cellSize * 0.5f);
                float zOffset = (y * m_cellSize) + (m_cellSize * 0.5f);

                Vector3 worldPosition = origin + new Vector3(xOffset, 0.0f, zOffset);

                GridTile tile = new GridTile(coordinates, worldPosition);
                m_tiles.Add(coordinates, tile);
            }
        }

        Debug.Log("Generated grid with " + m_tiles.Count + " tiles.");
    }

    /// <summary>
    /// Attempts to retrieve a tile by its grid coordinates.
    /// </summary>
    public bool TryGetTile(Vector2Int coordinates, out GridTile tile)
    {
        return m_tiles.TryGetValue(coordinates, out tile);
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
        if (m_gridWidth <= 0 || m_gridHeight <= 0 || m_cellSize <= 0.0f)
        {
            return;
        }

        Vector3 origin = transform.position;

        for (int y = 0; y < m_gridHeight; y++)
        {
            for (int x = 0; x < m_gridWidth; x++)
            {
                float xOffset = (x * m_cellSize) + (m_cellSize * 0.5f);
                float zOffset = (y * m_cellSize) + (m_cellSize * 0.5f);

                Vector3 worldPosition = origin + new Vector3(xOffset, 0.0f, zOffset);
                Vector3 tileSize = new Vector3(m_cellSize, 0.05f, m_cellSize);

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(worldPosition, tileSize);
            }
        }
    }
}