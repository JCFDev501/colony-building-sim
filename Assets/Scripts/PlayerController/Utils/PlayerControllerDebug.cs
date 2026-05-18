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

    [Header("Panel Layout")]
    [SerializeField] private int m_windowId = 1002;
    [SerializeField] private float m_panelX = 10.0f;
    [SerializeField] private float m_panelY = 10.0f;
    [SerializeField] private float m_panelWidth = 500.0f;
    [SerializeField] private float m_maxPanelHeight = 620.0f;
    [SerializeField] private float m_lineHeight = 20.0f;
    [SerializeField] private float m_padding = 10.0f;

    private bool m_previousHasHoveredTile = false;
    private Vector2Int m_previousHoveredTileCoordinates = Vector2Int.zero;
    private bool m_previousHasSelectedTile = false;
    private Vector2Int m_previousSelectedTileCoordinates = Vector2Int.zero;

    private Rect m_panelRect;
    private Vector2 m_scrollPosition = Vector2.zero;

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

        m_panelRect = new Rect(
            m_panelX,
            m_panelY,
            m_panelWidth,
            CalculateDebugPanelHeight());
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

        float calculatedHeight = CalculateDebugPanelHeight();
        m_panelRect.height = Mathf.Min(calculatedHeight, m_maxPanelHeight);

        if (m_panelRect.Contains(Event.current.mousePosition))
        {
            DebugPanelInputBlocker.BlockPointerInput();
        }

        m_panelRect = GUI.Window(m_windowId, m_panelRect, DrawTileDebugWindow, "Tile Debug");
    }

    /// <summary>
    /// Draws the draggable GUI window contents.
    /// </summary>
    private void DrawTileDebugWindow(int windowId)
    {
        float contentHeight = CalculateDebugPanelHeight();

        Rect viewRect = new Rect(
            0.0f,
            0.0f,
            m_panelRect.width - 25.0f,
            contentHeight);

        Rect scrollRect = new Rect(
            0.0f,
            22.0f,
            m_panelRect.width,
            m_panelRect.height - 22.0f);

        m_scrollPosition = GUI.BeginScrollView(scrollRect, m_scrollPosition, viewRect);

        float currentY = 5.0f;
        float contentX = m_padding;
        float contentWidth = viewRect.width - (m_padding * 2.0f);

        DrawControllerSummary(contentX, contentWidth, ref currentY);
        currentY += 10.0f;

        DrawTileDebugSection(
            "Hovered Tile",
            m_playerController.HasHoveredTile,
            m_playerController.HoveredTileCoordinates,
            contentX,
            contentWidth,
            ref currentY);

        currentY += 10.0f;

        DrawTileDebugSection(
            "Selected Tile",
            m_playerController.HasSelectedTile,
            m_playerController.SelectedTileCoordinates,
            contentX,
            contentWidth,
            ref currentY);

        GUI.EndScrollView();
        GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 22.0f));
    }

    /// <summary>
    /// Draws general PlayerController debug state.
    /// </summary>
    private void DrawControllerSummary(float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Controller");
        DrawLine(x + 10.0f, ref y, width, "Has Hovered Tile: " + m_playerController.HasHoveredTile);
        DrawLine(x + 10.0f, ref y, width, "Hovered Coordinates: " + FormatOptionalCoordinates(m_playerController.HasHoveredTile, m_playerController.HoveredTileCoordinates));
        DrawLine(x + 10.0f, ref y, width, "Has Selected Tile: " + m_playerController.HasSelectedTile);
        DrawLine(x + 10.0f, ref y, width, "Selected Coordinates: " + FormatOptionalCoordinates(m_playerController.HasSelectedTile, m_playerController.SelectedTileCoordinates));
        DrawLine(x + 10.0f, ref y, width, "Move Preview Active: " + m_playerController.IsMoveCommandPreviewActive);
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
                logMessage += " | Terrain: " + gridManager.GetTerrainType(hoveredTileCoordinates);
                logMessage += " | Water Distance: " + gridManager.GetWaterDistanceBand(hoveredTileCoordinates);
                logMessage += " | Block: " + gridManager.GetBlockType(hoveredTileCoordinates);
                logMessage += " | World Object: " + gridManager.GetWorldObjectType(hoveredTileCoordinates);
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
        float width,
        ref float y)
    {
        GridManager gridManager = m_playerController.GridManager;

        DrawLine(x, ref y, width, title);

        if (!hasTile)
        {
            DrawLine(x + 10.0f, ref y, width, "None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Coordinates: " + tileCoordinates);

        if (m_showTileWorldPosition)
        {
            Vector3 worldPosition = gridManager.GetWorldPosition(tileCoordinates);
            DrawLine(x + 10.0f, ref y, width, "World: " + FormatVector3(worldPosition));
        }

        if (m_showTileState)
        {
            DrawLine(x + 10.0f, ref y, width, "Terrain: " + gridManager.GetTerrainType(tileCoordinates));
            DrawLine(x + 10.0f, ref y, width, "Water Distance: " + gridManager.GetWaterDistanceBand(tileCoordinates));
            DrawLine(x + 10.0f, ref y, width, "Block: " + gridManager.GetBlockType(tileCoordinates));
            DrawLine(x + 10.0f, ref y, width, "World Object: " + gridManager.GetWorldObjectType(tileCoordinates));
            DrawLine(x + 10.0f, ref y, width, "Walkable: " + gridManager.IsWalkable(tileCoordinates));
            DrawLine(x + 10.0f, ref y, width, "Occupied: " + gridManager.IsOccupied(tileCoordinates));
            DrawLine(x + 10.0f, ref y, width, "Reserved: " + gridManager.IsReserved(tileCoordinates));
            DrawLine(x + 10.0f, ref y, width, "Content: " + gridManager.GetContentType(tileCoordinates));
        }

        if (m_showTileNeighbors)
        {
            List<Vector2Int> neighbors = gridManager.GetAllNeighborCoordinates(tileCoordinates);
            List<Vector2Int> traversableNeighbors = gridManager.GetTraversableNeighborCoordinates(tileCoordinates);

            DrawLine(x + 10.0f, ref y, width, "Neighbor Count: " + neighbors.Count);
            DrawLine(x + 10.0f, ref y, width, "Neighbors: " + BuildCoordinateListString(neighbors));
            DrawLine(x + 10.0f, ref y, width, "Traversable Count: " + traversableNeighbors.Count);
            DrawLine(x + 10.0f, ref y, width, "Traversable: " + BuildCoordinateListString(traversableNeighbors));
        }
    }

    /// <summary>
    /// Draws one text line in the debug panel.
    /// </summary>
    private void DrawLine(float x, ref float y, float width, string text)
    {
        GUI.Label(new Rect(x, y, width, m_lineHeight), text);
        y += m_lineHeight;
    }

    /// <summary>
    /// Calculates the debug panel height based on which sections are enabled.
    /// </summary>
    private float CalculateDebugPanelHeight()
    {
        float totalHeight = 35.0f;

        totalHeight += m_lineHeight * 6.0f;
        totalHeight += 10.0f;

        totalHeight += CalculateTileDebugSectionHeight();
        totalHeight += 10.0f;

        totalHeight += CalculateTileDebugSectionHeight();
        totalHeight += 10.0f;

        return totalHeight;
    }

    /// <summary>
    /// Calculates one tile debug section height based on enabled subsections.
    /// </summary>
    private float CalculateTileDebugSectionHeight()
    {
        float sectionHeight = m_lineHeight * 2.0f;

        if (m_showTileWorldPosition)
        {
            sectionHeight += m_lineHeight;
        }

        if (m_showTileState)
        {
            sectionHeight += m_lineHeight * 8.0f;
        }

        if (m_showTileNeighbors)
        {
            sectionHeight += m_lineHeight * 4.0f;
        }

        return sectionHeight;
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

        for (int i = 0; i < coordinates.Count; ++i)
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

    /// <summary>
    /// Formats optional coordinate values for compact debug output.
    /// </summary>
    private string FormatOptionalCoordinates(bool hasCoordinates, Vector2Int coordinates)
    {
        if (!hasCoordinates)
        {
            return "None";
        }

        return coordinates.ToString();
    }

    /// <summary>
    /// Formats a Vector3 for compact debug output.
    /// </summary>
    private string FormatVector3(Vector3 value)
    {
        return "("
               + value.x.ToString("F2")
               + ", "
               + value.y.ToString("F2")
               + ", "
               + value.z.ToString("F2")
               + ")";
    }
}