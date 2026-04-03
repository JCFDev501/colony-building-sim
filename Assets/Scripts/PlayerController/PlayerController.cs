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

    [Header("Visual Feedback")]
    [SerializeField] private GameObject m_hoverHighlight;
    [SerializeField] private GameObject m_selectedHighlight;
    [SerializeField] private float m_selectedHighlightYOffset = 0.03f;

    [Header("Selection State")]
    [SerializeField] private bool m_hasSelectedTile = false;
    [SerializeField] private Vector2Int m_selectedTileCoordinates = Vector2Int.zero;

    /// <summary>
    /// Returns the grid manager used by this controller.
    /// </summary>
    public GridManager GridManager
    {
        get { return m_gridManager; }
    }

    /// <summary>
    /// Returns the camera used by this controller.
    /// </summary>
    public Camera PlayerCamera
    {
        get { return m_playerCamera; }
    }

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
            ClearHoveredTile();
            return;
        }

        if (m_playerCamera == null)
        {
            ClearHoveredTile();
            return;
        }

        if (Mouse.current == null)
        {
            ClearHoveredTile();
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
            return;
        }

        m_hasHoveredTile = true;
        m_hoveredTileCoordinates = hoveredCoordinates;
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

        if (!m_hasHoveredTile)
        {
            return;
        }

        m_hasSelectedTile = true;
        m_selectedTileCoordinates = m_hoveredTileCoordinates;
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