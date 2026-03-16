using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction with the grid for the prototype.
/// This controller will own hover and selection state while using
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
    [SerializeField] private bool m_showHoveredTileDebug = true;
    [SerializeField] private bool m_showHoveredTileWorldPosition = true;
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
        Ray mouseRay = m_playerCamera.ScreenPointToRay(mouseScreenPosition);

        if (!Physics.Raycast(mouseRay, out RaycastHit hitInfo))
        {
            ClearHoveredTile();
            LogHoverStateChange(hadHoveredTileBeforeUpdate, previousHoveredTileCoordinates);
            return;
        }

        Vector3 hitWorldPosition = hitInfo.point;

        if (!m_gridManager.TryGetCoordinatesFromWorldPosition(hitWorldPosition, out Vector2Int hoveredCoordinates))
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
    /// Draws simple hovered and selected tile debug information on screen.
    /// </summary>
    private void OnGUI()
    {
        if (!m_showHoveredTileDebug)
        {
            return;
        }

        string hoveredText = "Hovered Tile: none";

        if (m_hasHoveredTile)
        {
            hoveredText = "Hovered Tile: " + m_hoveredTileCoordinates;

            if (m_showHoveredTileWorldPosition)
            {
                Vector3 hoveredWorldPosition = m_gridManager.GetWorldPosition(m_hoveredTileCoordinates);
                hoveredText += " | World: " + hoveredWorldPosition;
            }
        }

        string selectedText = "Selected Tile: none";

        if (m_hasSelectedTile)
        {
            selectedText = "Selected Tile: " + m_selectedTileCoordinates;

            if (m_showHoveredTileWorldPosition)
            {
                Vector3 selectedWorldPosition = m_gridManager.GetWorldPosition(m_selectedTileCoordinates);
                selectedText += " | World: " + selectedWorldPosition;
            }
        }

        GUI.Label(new Rect(10.0f, 10.0f, 700.0f, 25.0f), hoveredText);
        GUI.Label(new Rect(10.0f, 35.0f, 700.0f, 25.0f), selectedText);
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
            Debug.Log("Currently hovered tile: " + m_hoveredTileCoordinates + " | World Position: " + worldPosition);
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