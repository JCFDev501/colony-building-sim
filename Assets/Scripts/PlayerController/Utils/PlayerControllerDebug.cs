using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Handles debug logging and on-screen debug display for PlayerController.
/// This keeps debug and testing behavior separate from core interaction flow.
/// </summary>
public class PlayerControllerDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;

    [Header("Debug")]
    [SerializeField] private bool m_logHoveredTileChanges = true;
    [SerializeField] private bool m_showTileDebugPanel = true;
    [SerializeField] private bool m_showTileWorldPosition = true;
    [SerializeField] private bool m_showTileState = true;
    [SerializeField] private bool m_showTileNeighbors = true;
    [SerializeField] private bool m_logSelectedTileChanges = true;

    private bool m_previousHasHoveredTile = false;
    private Vector2Int m_previousHoveredTileCoordinates = Vector2Int.zero;
    private bool m_previousHasSelectedTile = false;
    private Vector2Int m_previousSelectedTileCoordinates = Vector2Int.zero;

    /// <summary>
    /// Validates required references when play begins.
    /// </summary>
    private void Start()
    {
        if (m_playerController == null)
        {
            Debug.LogError("PlayerControllerDebug is missing a PlayerController reference.", this);
            return;
        }

        m_previousHasHoveredTile = m_playerController.HasHoveredTile;
        m_previousHoveredTileCoordinates = m_playerController.HoveredTileCoordinates;
        m_previousHasSelectedTile = m_playerController.HasSelectedTile;
        m_previousSelectedTileCoordinates = m_playerController.SelectedTileCoordinates;
    }

    /// <summary>
    /// Monitors controller state changes and outputs debug information when enabled.
    /// </summary>
    private void Update()
    {
        if (m_playerController == null)
        {
            return;
        }

        LogHoverStateChange();
        LogSelectedTileChange();
        CacheCurrentState();
    }

    /// <summary>
    /// Draws formatted hovered and selected tile debug information on screen.
    /// </summary>
    private void OnGUI()
    {
        if (!m_showTileDebugPanel)
        {
            return;
        }

        if (m_playerController == null)
        {
            return;
        }

        if (m_playerController.GridManager == null)
        {
            return;
        }

        const float panelX = 10.0f;
        const float panelY = 10.0f;
        const float panelWidth = 460.0f;
        const float lineHeight = 20.0f;
        const float padding = 10.0f;

        float panelHeight = CalculateDebugPanelHeight(lineHeight);
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "Tile Debug");

        float currentY = panelY + 25.0f;

        DrawTileDebugSection(
            "Hovered Tile",
            m_playerController.HasHoveredTile,
            m_playerController.HoveredTileCoordinates,
            panelX + padding,
            ref currentY,
            lineHeight);

        currentY += 10.0f;

        DrawTileDebugSection(
            "Selected Tile",
            m_playerController.HasSelectedTile,
            m_playerController.SelectedTileCoordinates,
            panelX + padding,
            ref currentY,
            lineHeight);
    }

    /// <summary>
    /// Logs hovered tile changes only when the hovered state actually changes.
    /// </summary>
    private void LogHoverStateChange()
    {
        if (!m_logHoveredTileChanges)
        {
            return;
        }

        if (!HasHoverChanged())
        {
            return;
        }

        GridManager gridManager = m_playerController.GridManager;

        if (gridManager == null)
        {
            return;
        }

        if (m_playerController.HasHoveredTile)
        {
            Vector2Int hoveredTileCoordinates = m_playerController.HoveredTileCoordinates;
            Vector3 worldPosition = gridManager.GetWorldPosition(hoveredTileCoordinates);
            string logMessage = "Currently hovered tile: " + hoveredTileCoordinates + " | World Position: " + worldPosition;

            if (m_showTileState)
            {
                logMessage += " | Walkable: " + gridManager.IsWalkable(hoveredTileCoordinates);
                logMessage += " | Occupied: " + gridManager.IsOccupied(hoveredTileCoordinates);
                logMessage += " | Reserved: " + gridManager.IsReserved(hoveredTileCoordinates);
                logMessage += " | Content: " + gridManager.GetContentType(hoveredTileCoordinates);
            }

            if (m_showTileNeighbors)
            {
                List<Vector2Int> neighbors = gridManager.GetAllNeighborCoordinates(hoveredTileCoordinates);
                List<Vector2Int> traversableNeighbors = gridManager.GetTraversableNeighborCoordinates(hoveredTileCoordinates);

                logMessage += " | Neighbor Count: " + neighbors.Count;
                logMessage += " | Traversable Count: " + traversableNeighbors.Count;
            }

            Debug.Log(logMessage);
        }
        else
        {
            Debug.Log("Currently hovered tile: none");
        }
    }

    /// <summary>
    /// Logs selected tile changes only when the selected state actually changes.
    /// </summary>
    private void LogSelectedTileChange()
    {
        if (!m_logSelectedTileChanges)
        {
            return;
        }

        if (!HasSelectedTileChanged())
        {
            return;
        }

        GridManager gridManager = m_playerController.GridManager;

        if (gridManager == null)
        {
            return;
        }

        if (!m_playerController.HasSelectedTile)
        {
            return;
        }

        Vector3 worldPosition = gridManager.GetWorldPosition(m_playerController.SelectedTileCoordinates);
        Debug.Log("Selected tile: " + m_playerController.SelectedTileCoordinates + " | World Position: " + worldPosition);
    }

    /// <summary>
    /// Draws a formatted debug section for a hovered or selected tile.
    /// </summary>
    private void DrawTileDebugSection(
        string title,
        bool hasTile,
        Vector2Int tileCoordinates,
        float x,
        ref float y,
        float lineHeight)
    {
        GridManager gridManager = m_playerController.GridManager;

        GUI.Label(new Rect(x, y, 420.0f, lineHeight), title + ":");
        y += lineHeight;

        if (!hasTile)
        {
            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "None");
            y += lineHeight;
            return;
        }

        GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Coordinates: " + tileCoordinates);
        y += lineHeight;

        if (m_showTileWorldPosition)
        {
            Vector3 worldPosition = gridManager.GetWorldPosition(tileCoordinates);
            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "World: " + worldPosition);
            y += lineHeight;
        }

        if (m_showTileState)
        {
            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Walkable: " + gridManager.IsWalkable(tileCoordinates));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Occupied: " + gridManager.IsOccupied(tileCoordinates));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Reserved: " + gridManager.IsReserved(tileCoordinates));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Content: " + gridManager.GetContentType(tileCoordinates));
            y += lineHeight;
        }

        if (m_showTileNeighbors)
        {
            List<Vector2Int> neighbors = gridManager.GetAllNeighborCoordinates(tileCoordinates);
            List<Vector2Int> traversableNeighbors = gridManager.GetTraversableNeighborCoordinates(tileCoordinates);

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Neighbor Count: " + neighbors.Count);
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Neighbors: " + BuildCoordinateListString(neighbors));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Traversable Count: " + traversableNeighbors.Count);
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Traversable: " + BuildCoordinateListString(traversableNeighbors));
            y += lineHeight;
        }
    }

    /// <summary>
    /// Calculates the debug panel height based on which sections are enabled.
    /// </summary>
    private float CalculateDebugPanelHeight(float lineHeight)
    {
        float sectionHeight = lineHeight + lineHeight;

        if (m_showTileWorldPosition)
        {
            sectionHeight += lineHeight;
        }

        if (m_showTileState)
        {
            sectionHeight += lineHeight * 4.0f;
        }

        if (m_showTileNeighbors)
        {
            sectionHeight += lineHeight * 4.0f;
        }

        float totalHeight = 25.0f;
        totalHeight += sectionHeight;
        totalHeight += 10.0f;
        totalHeight += sectionHeight;
        totalHeight += 10.0f;

        return totalHeight;
    }

    /// <summary>
    /// Builds a readable coordinate list string for debug display.
    /// </summary>
    private string BuildCoordinateListString(List<Vector2Int> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0)
        {
            return "None";
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < coordinates.Count; i++)
        {
            builder.Append(coordinates[i]);

            if (i < coordinates.Count - 1)
            {
                builder.Append(", ");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns whether the hovered tile state changed during this update.
    /// </summary>
    private bool HasHoverChanged()
    {
        if (m_previousHasHoveredTile != m_playerController.HasHoveredTile)
        {
            return true;
        }

        if (!m_playerController.HasHoveredTile)
        {
            return false;
        }

        return m_previousHoveredTileCoordinates != m_playerController.HoveredTileCoordinates;
    }

    /// <summary>
    /// Returns whether the selected tile state changed during this update.
    /// </summary>
    private bool HasSelectedTileChanged()
    {
        if (m_previousHasSelectedTile != m_playerController.HasSelectedTile)
        {
            return true;
        }

        if (!m_playerController.HasSelectedTile)
        {
            return false;
        }

        return m_previousSelectedTileCoordinates != m_playerController.SelectedTileCoordinates;
    }

    /// <summary>
    /// Stores the current controller state so changes can be detected next frame.
    /// </summary>
    private void CacheCurrentState()
    {
        m_previousHasHoveredTile = m_playerController.HasHoveredTile;
        m_previousHoveredTileCoordinates = m_playerController.HoveredTileCoordinates;
        m_previousHasSelectedTile = m_playerController.HasSelectedTile;
        m_previousSelectedTileCoordinates = m_playerController.SelectedTileCoordinates;
    }
}