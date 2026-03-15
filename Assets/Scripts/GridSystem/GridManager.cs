using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages generation, storage, and debug visualization of a square grid.
/// The owning GameObject's transform position is used as the center of the grid.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int m_gridWidth = 10;
    [SerializeField] private int m_gridHeight = 10;
    [SerializeField] private float m_cellSize = 1.0f;
    [SerializeField] private bool m_generateOnStart = true;
    [SerializeField] private bool m_showSceneDebugGrid = true;
    [SerializeField] private bool m_logHoveredTileChanges = true;

    // Stores generated tiles by grid coordinate for fast lookup.
    private readonly Dictionary<Vector2Int, GridTile> m_tiles = new();

    private bool m_hasHoveredTile = false;
    private Vector2Int m_hoveredTileCoordinates = Vector2Int.zero;

    /// <summary>
    /// Provides read-only access to the generated tiles.
    /// </summary>
    public IReadOnlyDictionary<Vector2Int, GridTile> Tiles
    {
        get { return m_tiles; }
    }

    public bool HasHoveredTile
    {
        get { return m_hasHoveredTile; }
    }

    public Vector2Int HoveredTileCoordinates
    {
        get { return m_hoveredTileCoordinates; }
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

    private void Update()
    {
        UpdateHoveredTile();
    }

    private void UpdateHoveredTile()
    {
        bool hadHoveredTileBeforeUpdate = m_hasHoveredTile;
        Vector2Int previousHoveredTile = m_hoveredTileCoordinates;

        m_hasHoveredTile = false;
        m_hoveredTileCoordinates = Vector2Int.zero;

        if (Camera.main == null)
        {
            LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTile);
            return;
        }

        if (Mouse.current == null)
        {
            LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTile);
            return;
        }

        Ray mouseRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(mouseRay, out RaycastHit hitInfo))
        {
            LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTile);
            return;
        }

        if (!TryGetCoordinatesFromWorldPosition(hitInfo.point, out Vector2Int hoveredCoordinates))
        {
            LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTile);
            return;
        }

        m_hasHoveredTile = true;
        m_hoveredTileCoordinates = hoveredCoordinates;

        LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTile);
    }

    /// <summary>
    /// Logs the current hovered tile only when the hovered state changes.
    /// </summary>
    private void LogHoverStateChange(bool hadHoveredTileBeforeUpdate, Vector2Int previousHoveredTile)
    {
        if (!m_logHoveredTileChanges)
        {
            return;
        }

        if (m_hasHoveredTile)
        {
            if (!hadHoveredTileBeforeUpdate || previousHoveredTile != m_hoveredTileCoordinates)
            {
                Vector3 worldPosition = GetWorldPosition(m_hoveredTileCoordinates);
                Debug.Log("Currently hovered tile: " + m_hoveredTileCoordinates + " | World Position: " + worldPosition);
            }
        }
        else if (hadHoveredTileBeforeUpdate)
        {
            Debug.Log("Currently hovered tile: none");
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

    public Vector3 GetWorldPosition(Vector2Int coordinates)
    {
        if (!IsInBounds(coordinates))
        {
            return Vector3.zero;
        }

        Vector3 origin = transform.position;

        float gridWorldWidth = m_gridWidth * m_cellSize;
        float gridWorldHeight = m_gridHeight * m_cellSize;

        Vector3 startPosition = origin - new Vector3(gridWorldWidth * 0.5f, 0.0f, gridWorldHeight * 0.5f);

        float xOffset = (coordinates.x * m_cellSize) + (m_cellSize * 0.5f);
        float zOffset = (coordinates.y * m_cellSize) + (m_cellSize * 0.5f);

        return startPosition + new Vector3(xOffset, 0.0f, zOffset);
    }

    public bool TryGetCoordinatesFromWorldPosition(Vector3 worldPosition, out Vector2Int coordinates)
    {
        coordinates = Vector2Int.zero;

        Vector3 origin = transform.position;

        float gridWorldWidth = m_gridWidth * m_cellSize;
        float gridWorldHeight = m_gridHeight * m_cellSize;

        Vector3 startPosition = origin - new Vector3(gridWorldWidth * 0.5f, 0.0f, gridWorldHeight * 0.5f);
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

                if (m_hasHoveredTile && coordinates == m_hoveredTileCoordinates)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                Gizmos.DrawWireCube(worldPosition, tileSize);
            }
        }
    }
}