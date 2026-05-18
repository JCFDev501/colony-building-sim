using TMPro;
using UnityEngine;

/// <summary>
/// Displays concise player-facing information for the currently hovered tile.
/// </summary>
public class PlayerTileHoverHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private TMP_Text m_tileHoverText;

    [Header("Display")]
    [SerializeField] private string m_noTileText = "Hover a tile";

    /// <summary>
    /// Finds required references if they were not assigned in the Inspector.
    /// </summary>
    private void Start()
    {
        if (m_playerController == null)
        {
            m_playerController = FindFirstObjectByType<PlayerController>();
        }

        if (m_playerController == null)
        {
            Debug.LogError("PlayerTileHoverHudController is missing a PlayerController reference.", this);
        }

        RefreshHoverText();
    }

    /// <summary>
    /// Refreshes the hover text every frame so the HUD follows the current hovered tile.
    /// </summary>
    private void Update()
    {
        RefreshHoverText();
    }

    /// <summary>
    /// Updates the player-facing tile hover text.
    /// </summary>
    private void RefreshHoverText()
    {
        if (m_tileHoverText == null)
        {
            return;
        }

        if (m_playerController == null || m_playerController.GridManager == null)
        {
            m_tileHoverText.text = m_noTileText;
            return;
        }

        if (!m_playerController.HasHoveredTile)
        {
            m_tileHoverText.text = m_noTileText;
            return;
        }

        Vector2Int hoveredCoordinates = m_playerController.HoveredTileCoordinates;
        GridManager gridManager = m_playerController.GridManager;

        TileTerrainType terrainType = gridManager.GetTerrainType(hoveredCoordinates);
        BlockType blockType = gridManager.GetBlockType(hoveredCoordinates);
        WorldObjectType worldObjectType = gridManager.GetWorldObjectType(hoveredCoordinates);
        TileContentType contentType = gridManager.GetContentType(hoveredCoordinates);
        bool isWalkable = gridManager.IsWalkable(hoveredCoordinates);

        m_tileHoverText.text = BuildHoverText(
            terrainType,
            blockType,
            worldObjectType,
            contentType,
            isWalkable);
    }

    /// <summary>
    /// Builds concise player-facing hover text from tile state.
    /// </summary>
    private string BuildHoverText(
        TileTerrainType terrainType,
        BlockType blockType,
        WorldObjectType worldObjectType,
        TileContentType contentType,
        bool isWalkable)
    {
        if (blockType != BlockType.None)
        {
            return blockType + " | " + BuildWalkableText(isWalkable);
        }

        if (worldObjectType != WorldObjectType.None)
        {
            return terrainType + " | " + worldObjectType;
        }

        if (contentType != TileContentType.Empty)
        {
            return terrainType + " | " + contentType;
        }

        return terrainType + " | " + BuildWalkableText(isWalkable);
    }

    /// <summary>
    /// Builds a compact walkability label.
    /// </summary>
    private string BuildWalkableText(bool isWalkable)
    {
        return isWalkable ? "Walkable" : "Blocked";
    }
}