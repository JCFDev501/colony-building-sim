using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction with the grid for the prototype.
/// This controller owns hover and selection state while using
/// the GridManager for tile lookup and grid-related data.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager m_gridManager;
    [SerializeField] private Camera m_playerCamera;

    [Header("Hover State")]
    [SerializeField] private bool m_hasHoveredTile = false;
    [SerializeField] private Vector2Int m_hoveredTileCoordinates = Vector2Int.zero;

    [Header("Debug")]
    [SerializeField] private bool m_logHoveredTileChanges = true;
    [SerializeField] private bool m_showTileDebugPanel = true;
    [SerializeField] private bool m_showTileWorldPosition = true;
    [SerializeField] private bool m_showTileState = true;
    [SerializeField] private bool m_showTileNeighbors = true;
    [SerializeField] private bool m_logSelectedTileChanges = true;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject m_hoverHighlight;
    [SerializeField] private GameObject m_selectedHighlight;
    [SerializeField] private float m_selectedHighlightYOffset = 0.03f;

    [Header("Selection State")]
    [SerializeField] private bool m_hasSelectedTile = false;
    [SerializeField] private Vector2Int m_selectedTileCoordinates = Vector2Int.zero;

    /// <summary>
    /// Returns whether the player is currently hovering a valid tile.
    /// </summary>
    public bool HasHoveredTile
    {
        get { return m_hasHoveredTile; }
    }

    /// <summary>
    /// Returns the coordinates of the currently hovered tile.
    /// Only meaningful when HasHoveredTile is true.
    /// </summary>
    public Vector2Int HoveredTileCoordinates
    {
        get { return m_hoveredTileCoordinates; }
    }

    /// <summary>
    /// Returns whether the player currently has a selected tile.
    /// </summary>
    public bool HasSelectedTile
    {
        get { return m_hasSelectedTile; }
    }

    /// <summary>
    /// Returns the coordinates of the currently selected tile.
    /// Only meaningful when HasSelectedTile is true.
    /// </summary>
    public Vector2Int SelectedTileCoordinates
    {
        get { return m_selectedTileCoordinates; }
    }

    /// <summary>
    /// Validates required references when play begins.
    /// </summary>
    private void Start()
    {
        if (m_gridManager == null)
        {
            Debug.LogError("PlayerController is missing a GridManager reference.", this);
        }

        if (m_playerCamera == null)
        {
            Debug.LogError("PlayerController is missing a Camera reference.", this);
        }

        if (m_hoverHighlight == null)
        {
            Debug.LogError("PlayerController is missing a hover highlight reference.", this);
        }

        if (m_selectedHighlight == null)
        {
            Debug.LogError("PlayerController is missing a selected highlight reference.", this);
        }
    }

    /// <summary>
    /// Updates player interaction each frame.
    /// </summary>
    private void Update()
    {
        UpdateHoveredTile();
        UpdateTileSelection();
        UpdateHoverHighlight();
        UpdateSelectedHighlight();
    }

    /// <summary>
    /// Updates hovered tile state from player input.
    /// Uses the grid plane instead of Physics.Raycast so hover remains accurate
    /// across camera movement, zoom, and rotation.
    /// </summary>
    private void UpdateHoveredTile()
    {
        if (m_gridManager == null)
        {
            return;
        }

        if (m_playerCamera == null)
        {
            return;
        }

        bool hadHoveredTileBeforeUpdate = m_hasHoveredTile;
        Vector2Int previousHoveredTileCoordinates = m_hoveredTileCoordinates;

        if (Mouse.current == null)
        {
            ClearHoveredTile();
            LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTileCoordinates);
            return;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        if (!m_gridManager.TryGetCoordinatesFromScreenPoint(
            m_playerCamera,
            mouseScreenPosition,
            out Vector2Int hoveredCoordinates,
            out _))
        {
            ClearHoveredTile();
            LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTileCoordinates);
            return;
        }

        m_hasHoveredTile = true;
        m_hoveredTileCoordinates = hoveredCoordinates;

        LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTileCoordinates);
    }

    /// <summary>
    /// Updates the hover highlight visibility and position based on current hover state.
    /// </summary>
    private void UpdateHoverHighlight()
    {
        if (m_hoverHighlight == null)
        {
            return;
        }

        if (!m_hasHoveredTile)
        {
            m_hoverHighlight.SetActive(false);
            return;
        }

        Vector3 hoverWorldPosition = m_gridManager.GetWorldPosition(m_hoveredTileCoordinates);
        hoverWorldPosition.y += 0.02f;

        m_hoverHighlight.transform.position = hoverWorldPosition;
        m_hoverHighlight.SetActive(true);
    }

    /// <summary>
    /// Updates the selected highlight visibility and position based on current selection state.
    /// </summary>
    private void UpdateSelectedHighlight()
    {
        if (m_selectedHighlight == null)
        {
            return;
        }

        if (!m_hasSelectedTile)
        {
            m_selectedHighlight.SetActive(false);
            return;
        }

        Vector3 selectedWorldPosition = m_gridManager.GetWorldPosition(m_selectedTileCoordinates);
        selectedWorldPosition.y += m_selectedHighlightYOffset;

        m_selectedHighlight.transform.position = selectedWorldPosition;
        m_selectedHighlight.SetActive(true);
    }

    /// <summary>
    /// Updates tile selection input for the prototype.
    /// </summary>
    private void UpdateTileSelection()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        bool hadSelectedTileBeforeUpdate = m_hasSelectedTile;
        Vector2Int previousSelectedTileCoordinates = m_selectedTileCoordinates;

        if (!m_hasHoveredTile)
        {
            return;
        }

        m_hasSelectedTile = true;
        m_selectedTileCoordinates = m_hoveredTileCoordinates;

        LogSelectedTileChange(hadSelectedTileBeforeUpdate, previousSelectedTileCoordinates);
    }

    /// <summary>
    /// Logs selected tile changes only when the selected state actually changes.
    /// </summary>
    private void LogSelectedTileChange(bool hadSelectedTileBeforeUpdate, Vector2Int previousSelectedTileCoordinates)
    {
        if (!m_logSelectedTileChanges)
        {
            return;
        }

        if (!HasSelectedTileChanged(hadSelectedTileBeforeUpdate, previousSelectedTileCoordinates))
        {
            return;
        }

        Vector3 worldPosition = m_gridManager.GetWorldPosition(m_selectedTileCoordinates);
        Debug.Log("Selected tile: " + m_selectedTileCoordinates + " | World Position: " + worldPosition);
    }

    /// <summary>
    /// Returns whether the selected tile state changed during this update.
    /// </summary>
    private bool HasSelectedTileChanged(bool hadSelectedTileBeforeUpdate, Vector2Int previousSelectedTileCoordinates)
    {
        if (hadSelectedTileBeforeUpdate != m_hasSelectedTile)
        {
            return true;
        }

        if (!m_hasSelectedTile)
        {
            return false;
        }

        return previousSelectedTileCoordinates != m_selectedTileCoordinates;
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
            m_hasHoveredTile,
            m_hoveredTileCoordinates,
            panelX + padding,
            ref currentY,
            lineHeight);

        currentY += 10.0f;

        DrawTileDebugSection(
            "Selected Tile",
            m_hasSelectedTile,
            m_selectedTileCoordinates,
            panelX + padding,
            ref currentY,
            lineHeight);
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
            Vector3 worldPosition = m_gridManager.GetWorldPosition(tileCoordinates);
            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "World: " + worldPosition);
            y += lineHeight;
        }

        if (m_showTileState)
        {
            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Walkable: " + m_gridManager.IsWalkable(tileCoordinates));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Occupied: " + m_gridManager.IsOccupied(tileCoordinates));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Reserved: " + m_gridManager.IsReserved(tileCoordinates));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Content: " + m_gridManager.GetContentType(tileCoordinates));
            y += lineHeight;
        }

        if (m_showTileNeighbors)
        {
            List<Vector2Int> neighbors = m_gridManager.GetNeighborCoordinates(tileCoordinates);
            List<Vector2Int> enterableNeighbors = m_gridManager.GetEnterableNeighborCoordinates(tileCoordinates);

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Neighbor Count: " + neighbors.Count);
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Neighbors: " + BuildCoordinateListString(neighbors));
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Enterable Count: " + enterableNeighbors.Count);
            y += lineHeight;

            GUI.Label(new Rect(x + 10.0f, y, 420.0f, lineHeight), "Enterable: " + BuildCoordinateListString(enterableNeighbors));
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
    /// Logs hovered tile changes only when the hovered state actually changes.
    /// </summary>
    private void LogHoverStateChange(bool hadHoveredTileBeforeUpdate, Vector2Int previousHoveredTileCoordinates)
    {
        if (!m_logHoveredTileChanges)
        {
            return;
        }

        if (!HasHoverChanged(hadHoveredTileBeforeUpdate, previousHoveredTileCoordinates))
        {
            return;
        }

        if (m_hasHoveredTile)
        {
            Vector3 worldPosition = m_gridManager.GetWorldPosition(m_hoveredTileCoordinates);
            string logMessage = "Currently hovered tile: " + m_hoveredTileCoordinates + " | World Position: " + worldPosition;

            if (m_showTileState)
            {
                logMessage += " | Walkable: " + m_gridManager.IsWalkable(m_hoveredTileCoordinates);
                logMessage += " | Occupied: " + m_gridManager.IsOccupied(m_hoveredTileCoordinates);
                logMessage += " | Reserved: " + m_gridManager.IsReserved(m_hoveredTileCoordinates);
                logMessage += " | Content: " + m_gridManager.GetContentType(m_hoveredTileCoordinates);
            }

            if (m_showTileNeighbors)
            {
                List<Vector2Int> neighbors = m_gridManager.GetNeighborCoordinates(m_hoveredTileCoordinates);
                List<Vector2Int> enterableNeighbors = m_gridManager.GetEnterableNeighborCoordinates(m_hoveredTileCoordinates);

                logMessage += " | Neighbor Count: " + neighbors.Count;
                logMessage += " | Enterable Count: " + enterableNeighbors.Count;
            }

            Debug.Log(logMessage);
        }
        else
        {
            Debug.Log("Currently hovered tile: none");
        }
    }

    /// <summary>
    /// Returns whether the hovered tile state changed during this update.
    /// </summary>
    private bool HasHoverChanged(bool hadHoveredTileBeforeUpdate, Vector2Int previousHoveredTileCoordinates)
    {
        if (hadHoveredTileBeforeUpdate != m_hasHoveredTile)
        {
            return true;
        }

        if (!m_hasHoveredTile)
        {
            return false;
        }

        return previousHoveredTileCoordinates != m_hoveredTileCoordinates;
    }

    /// <summary>
    /// Clears the current hovered tile state.
    /// </summary>
    private void ClearHoveredTile()
    {
        m_hasHoveredTile = false;
        m_hoveredTileCoordinates = Vector2Int.zero;
    }
}