using System.Collections.Generic;
using UnityEngine;

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

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(worldPosition, tileSize);
            }
        }
    }
}