using UnityEngine;

/// <summary>
/// Renders tile terrain from the grid onto a target ground material using a generated texture.
/// </summary>
public class WorldTerrainRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager m_gridManager;
    [SerializeField] private Renderer m_targetRenderer;

    [Header("Texture Settings")]
    [SerializeField] private int m_pixelsPerTile = 16;
    [SerializeField] private bool m_renderOnStart = true;
    [SerializeField] private bool m_autoRefresh = false;
    [SerializeField] private bool m_generateGridIfMissing = true;

    [Header("Plane Fitting")]
    [SerializeField] private bool m_fitTargetPlaneToGrid = true;
    [SerializeField] private bool m_assumeUnityBuiltInPlane = true;

    [Header("Terrain Source Textures")]
    [SerializeField] private Texture2D m_grassTexture;
    [SerializeField] private Texture2D m_dirtTexture;
    [SerializeField] private Texture2D m_forestFloorTexture;
    [SerializeField] private Texture2D m_waterTexture;

    [Header("Fallback Colors")]
    [SerializeField] private Color m_missingGrassColor = new Color(0.35f, 0.7f, 0.35f);
    [SerializeField] private Color m_missingDirtColor = new Color(0.55f, 0.4f, 0.25f);
    [SerializeField] private Color m_missingForestFloorColor = new Color(0.2f, 0.45f, 0.2f);
    [SerializeField] private Color m_missingWaterColor = new Color(0.2f, 0.45f, 0.85f);

    private Texture2D m_terrainTexture;

    /// <summary>
    /// Builds the terrain texture automatically on play if enabled.
    /// </summary>
    private void Start()
    {
        if (!m_renderOnStart)
        {
            return;
        }

        RenderTerrain();
    }

    /// <summary>
    /// Refreshes the rendered terrain every frame when auto-refresh is enabled.
    /// This is mainly useful for debug iteration and should stay off by default.
    /// </summary>
    private void Update()
    {
        if (!m_autoRefresh)
        {
            return;
        }

        RenderTerrain();
    }

    /// <summary>
    /// Rebuilds and applies the terrain texture from current grid tile terrain data.
    /// Each tile is filled using the source texture mapped to its terrain type.
    /// </summary>
    public void RenderTerrain()
    {
        if (m_gridManager == null)
        {
            Debug.LogError("WorldTerrainRenderer is missing a GridManager reference.", this);
            return;
        }

        if (m_targetRenderer == null)
        {
            Debug.LogError("WorldTerrainRenderer is missing a target Renderer reference.", this);
            return;
        }

        if (m_pixelsPerTile <= 0)
        {
            Debug.LogError("WorldTerrainRenderer requires pixels per tile to be greater than zero.", this);
            return;
        }

        EnsureGridIsAvailable();

        int gridWidth = m_gridManager.GridWidth;
        int gridHeight = m_gridManager.GridHeight;

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("WorldTerrainRenderer could not determine valid grid dimensions.", this);
            return;
        }

        if (m_fitTargetPlaneToGrid)
        {
            FitTargetRendererToGrid(gridWidth, gridHeight, m_gridManager.CellSize);
        }

        int textureWidth = gridWidth * m_pixelsPerTile;
        int textureHeight = gridHeight * m_pixelsPerTile;

        m_terrainTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        m_terrainTexture.filterMode = FilterMode.Point;
        m_terrainTexture.wrapMode = TextureWrapMode.Clamp;

        PaintTerrainTexture(gridWidth, gridHeight);
        m_terrainTexture.Apply();

        ApplyTextureToRenderer();
    }

    /// <summary>
    /// Ensures the grid has generated tile data before rendering begins.
    /// </summary>
    private void EnsureGridIsAvailable()
    {
        if (m_gridManager.Tiles.Count > 0)
        {
            return;
        }

        if (!m_generateGridIfMissing)
        {
            return;
        }

        m_gridManager.GenerateGrid();
    }

    /// <summary>
    /// Fits the target renderer transform to the grid so the displayed terrain texture
    /// lines up with the grid's world-space footprint.
    /// This assumes the target is a standard Unity built-in plane when enabled.
    /// </summary>
    private void FitTargetRendererToGrid(int gridWidth, int gridHeight, float cellSize)
    {
        Transform targetTransform = m_targetRenderer.transform;
        Transform gridTransform = m_gridManager.transform;

        targetTransform.position = gridTransform.position;

        if (!m_assumeUnityBuiltInPlane)
        {
            return;
        }

        float totalWorldWidth = gridWidth * cellSize;
        float totalWorldHeight = gridHeight * cellSize;

        Vector3 currentScale = targetTransform.localScale;
        currentScale.x = totalWorldWidth / 10.0f;
        currentScale.z = totalWorldHeight / 10.0f;
        targetTransform.localScale = currentScale;
    }

    /// <summary>
    /// Fills the generated terrain texture by painting each tile into its matching pixel region.
    /// </summary>
    private void PaintTerrainTexture(int gridWidth, int gridHeight)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                TileTerrainType terrainType = m_gridManager.GetTerrainType(coordinates);

                PaintTilePixels(coordinates, terrainType, gridWidth, gridHeight);
            }
        }
    }

    /// <summary>
    /// Paints one tile-sized block of pixels into the generated terrain texture.
    /// If a source texture exists for the tile's terrain type, it is sampled across the tile region.
    /// Otherwise, a fallback color is used.
    /// Both axes are flipped so texture space matches the plane's visible orientation.
    /// </summary>
    private void PaintTilePixels(Vector2Int coordinates, TileTerrainType terrainType, int gridWidth, int gridHeight)
    {
        int startX = (gridWidth - 1 - coordinates.x) * m_pixelsPerTile;
        int startY = (gridHeight - 1 - coordinates.y) * m_pixelsPerTile;

        Texture2D sourceTexture = GetTerrainSourceTexture(terrainType);

        for (int pixelY = 0; pixelY < m_pixelsPerTile; pixelY++)
        {
            for (int pixelX = 0; pixelX < m_pixelsPerTile; pixelX++)
            {
                Color pixelColor = GetTerrainPixelColor(sourceTexture, terrainType, pixelX, pixelY);
                m_terrainTexture.SetPixel(startX + pixelX, startY + pixelY, pixelColor);
            }
        }
    }

    /// <summary>
    /// Returns the source texture assigned to the given terrain type.
    /// </summary>
    private Texture2D GetTerrainSourceTexture(TileTerrainType terrainType)
    {
        switch (terrainType)
        {
            case TileTerrainType.Grass:
                return m_grassTexture;

            case TileTerrainType.Dirt:
                return m_dirtTexture;

            case TileTerrainType.ForestFloor:
                return m_forestFloorTexture;

            case TileTerrainType.Water:
                return m_waterTexture;

            default:
                return null;
        }
    }

    /// <summary>
    /// Returns the color that should be written for one output pixel of a tile.
    /// If a source texture is available, the texture is sampled across the tile.
    /// Otherwise, a fallback color is returned for visibility.
    /// </summary>
    private Color GetTerrainPixelColor(Texture2D sourceTexture, TileTerrainType terrainType, int pixelX, int pixelY)
    {
        if (sourceTexture == null)
        {
            return GetFallbackTerrainColor(terrainType);
        }

        float u = (pixelX + 0.5f) / m_pixelsPerTile;
        float v = (pixelY + 0.5f) / m_pixelsPerTile;

        return sourceTexture.GetPixelBilinear(u, v);
    }

    /// <summary>
    /// Returns a visible fallback color when a terrain source texture is missing.
    /// </summary>
    private Color GetFallbackTerrainColor(TileTerrainType terrainType)
    {
        switch (terrainType)
        {
            case TileTerrainType.Grass:
                return m_missingGrassColor;

            case TileTerrainType.Dirt:
                return m_missingDirtColor;

            case TileTerrainType.ForestFloor:
                return m_missingForestFloorColor;

            case TileTerrainType.Water:
                return m_missingWaterColor;

            default:
                return Color.magenta;
        }
    }

    /// <summary>
    /// Applies the generated terrain texture to the target renderer's material.
    /// </summary>
    private void ApplyTextureToRenderer()
    {
        Material materialInstance = m_targetRenderer.material;
        materialInstance.mainTexture = m_terrainTexture;
        materialInstance.mainTextureScale = Vector2.one;
        materialInstance.mainTextureOffset = Vector2.zero;
    }

    /// <summary>
    /// Rebuilds the terrain texture from the component context menu in the Inspector.
    /// </summary>
    [ContextMenu("Render Terrain")]
    private void RenderTerrainFromContextMenu()
    {
        RenderTerrain();
    }
}