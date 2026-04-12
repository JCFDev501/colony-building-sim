using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField] private PawnSelectionManager m_pawnSelectionManager;
    [SerializeField] private PawnDeputizationManager m_pawnDeputizationManager;
    [SerializeField] private PawnMoveCommandExecutor m_pawnMoveCommandExecutor;

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

    [Header("Drag Selection")]
    [SerializeField] private float m_dragSelectionThreshold = 8.0f;
    [SerializeField] private Canvas m_dragSelectionCanvas;
    [SerializeField] private RectTransform m_dragSelectionBox;

    private bool m_isTrackingLeftSelectionGesture = false;
    private bool m_isDragSelecting = false;
    private Vector2 m_dragStartScreenPosition = Vector2.zero;
    private Vector2 m_dragCurrentScreenPosition = Vector2.zero;

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
    /// Returns whether move preview mode is currently active.
    /// Move preview is active only while right mouse is being held.
    /// </summary>
    public bool IsMoveCommandPreviewActive
    {
        get
        {
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
        }
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

        if (m_pawnSelectionManager == null)
        {
            Debug.LogError("PlayerController is missing a PawnSelectionManager reference.", this);
        }

        if (m_pawnDeputizationManager == null)
        {
            Debug.LogError("PlayerController is missing a PawnDeputizationManager reference.", this);
        }

        if (m_pawnMoveCommandExecutor == null)
        {
            Debug.LogError("PlayerController is missing a PawnMoveCommandExecutor reference.", this);
        }

        if (m_hoverHighlight == null)
        {
            Debug.LogError("PlayerController is missing a hover highlight reference.", this);
        }

        if (m_selectedHighlight == null)
        {
            Debug.LogError("PlayerController is missing a selected highlight reference.", this);
        }

        if (m_dragSelectionBox != null && m_dragSelectionCanvas == null)
        {
            m_dragSelectionCanvas = m_dragSelectionBox.GetComponentInParent<Canvas>();
        }

        if (m_dragSelectionBox == null)
        {
            Debug.LogError("PlayerController is missing a drag selection box reference.", this);
        }

        if (m_dragSelectionCanvas == null)
        {
            Debug.LogError("PlayerController is missing a drag selection canvas reference.", this);
        }

        ConfigureDragSelectionBox();
        HideDragSelectionBox();
    }

    /// <summary>
    /// Updates player interaction each frame.
    /// </summary>
    private void Update()
    {
        UpdateHoveredTile();
        UpdateLeftMouseSelectionInput();
        UpdateDeputizationInput();
        UpdateMoveCommandInput();
        UpdateHoverHighlight();
        UpdateSelectedHighlight();
    }

    /// <summary>
    /// Updates hovered tile state from player input.
    /// Pawn hover takes priority over grid-plane hover so the hovered tile
    /// matches the tile the pawn is actually standing on.
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

        if (TryGetHoveredPawn(out Pawn hoveredPawn))
        {
            m_hasHoveredTile = true;
            m_hoveredTileCoordinates = hoveredPawn.GridCoordinate;
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
    /// Updates left mouse input for click selection and drag selection.
    /// </summary>
    private void UpdateLeftMouseSelectionInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            BeginLeftSelectionGesture();
        }

        if (m_isTrackingLeftSelectionGesture && Mouse.current.leftButton.isPressed)
        {
            UpdateLeftSelectionGesture();
        }

        if (m_isTrackingLeftSelectionGesture && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndLeftSelectionGesture();
        }
    }

    /// <summary>
    /// Begins tracking a possible click or drag selection gesture.
    /// </summary>
    private void BeginLeftSelectionGesture()
    {
        m_isTrackingLeftSelectionGesture = true;
        m_isDragSelecting = false;
        m_dragStartScreenPosition = Mouse.current.position.ReadValue();
        m_dragCurrentScreenPosition = m_dragStartScreenPosition;

        HideDragSelectionBox();
    }

    /// <summary>
    /// Updates the active click or drag selection gesture.
    /// Once the threshold is passed, drag selection becomes active.
    /// </summary>
    private void UpdateLeftSelectionGesture()
    {
        m_dragCurrentScreenPosition = Mouse.current.position.ReadValue();

        if (!m_isDragSelecting)
        {
            float dragDistance = Vector2.Distance(m_dragStartScreenPosition, m_dragCurrentScreenPosition);

            if (dragDistance >= m_dragSelectionThreshold)
            {
                m_isDragSelecting = true;
            }
        }

        if (m_isDragSelecting)
        {
            ShowDragSelectionBox();
            UpdateDragSelectionBoxVisual();
        }
    }

    /// <summary>
    /// Finishes the active click or drag selection gesture.
    /// Clicks use existing click-selection rules.
    /// Drags use box-selection rules.
    /// </summary>
    private void EndLeftSelectionGesture()
    {
        if (m_isDragSelecting)
        {
            ExecuteDragSelection();
        }
        else
        {
            ExecuteClickSelection();
        }

        m_isTrackingLeftSelectionGesture = false;
        m_isDragSelecting = false;
        m_dragStartScreenPosition = Vector2.zero;
        m_dragCurrentScreenPosition = Vector2.zero;

        HideDragSelectionBox();
    }

    /// <summary>
    /// Executes normal click selection behavior for the prototype.
    /// Pawn clicks take priority over tile clicks.
    /// Shift-click toggles a pawn in the current selection.
    /// Clicking non-pawn space clears the current pawn selection.
    /// </summary>
    private void ExecuteClickSelection()
    {
        if (TryGetClickedPawn(out Pawn clickedPawn))
        {
            if (m_pawnSelectionManager != null)
            {
                if (IsShiftHeld())
                {
                    m_pawnSelectionManager.TogglePawnSelection(clickedPawn);
                }
                else
                {
                    m_pawnSelectionManager.SelectSinglePawn(clickedPawn);
                }
            }

            return;
        }

        if (m_pawnSelectionManager != null)
        {
            m_pawnSelectionManager.ClearSelection();
        }

        if (!m_hasHoveredTile)
        {
            return;
        }

        m_hasSelectedTile = true;
        m_selectedTileCoordinates = m_hoveredTileCoordinates;
    }

    /// <summary>
    /// Executes box selection using the current drag rectangle.
    /// Only pawns inside the screen-space box are considered.
    /// </summary>
    private void ExecuteDragSelection()
    {
        if (m_pawnSelectionManager == null)
        {
            return;
        }

        List<Pawn> pawnsInsideSelection = GetPawnsInsideDragSelection();

        if (IsShiftHeld())
        {
            m_pawnSelectionManager.TogglePawnSelection(pawnsInsideSelection);
            return;
        }

        m_pawnSelectionManager.SelectPawns(pawnsInsideSelection);
    }

    /// <summary>
    /// Finds all pawns whose screen-space position is inside the current drag rectangle.
    /// </summary>
    private List<Pawn> GetPawnsInsideDragSelection()
    {
        List<Pawn> pawnsInsideSelection = new();

        if (m_playerCamera == null)
        {
            return pawnsInsideSelection;
        }

        Rect dragRect = GetCurrentDragScreenRect();
        Pawn[] pawns = FindObjectsByType<Pawn>(FindObjectsSortMode.None);

        foreach (Pawn pPawn in pawns)
        {
            if (pPawn == null)
            {
                continue;
            }

            Vector3 pawnScreenPosition = m_playerCamera.WorldToScreenPoint(pPawn.WorldPosition);

            if (pawnScreenPosition.z < 0.0f)
            {
                continue;
            }

            Vector2 pawnScreenPoint = new Vector2(pawnScreenPosition.x, pawnScreenPosition.y);

            if (!dragRect.Contains(pawnScreenPoint))
            {
                continue;
            }

            pawnsInsideSelection.Add(pPawn);
        }

        return pawnsInsideSelection;
    }

    /// <summary>
    /// Returns the current drag selection rectangle in screen-space coordinates.
    /// </summary>
    private Rect GetCurrentDragScreenRect()
    {
        float minX = Mathf.Min(m_dragStartScreenPosition.x, m_dragCurrentScreenPosition.x);
        float maxX = Mathf.Max(m_dragStartScreenPosition.x, m_dragCurrentScreenPosition.x);
        float minY = Mathf.Min(m_dragStartScreenPosition.y, m_dragCurrentScreenPosition.y);
        float maxY = Mathf.Max(m_dragStartScreenPosition.y, m_dragCurrentScreenPosition.y);

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Configures the drag selection box to use bottom-left anchoring
    /// so screen-space rectangle math behaves predictably.
    /// </summary>
    private void ConfigureDragSelectionBox()
    {
        if (m_dragSelectionBox == null)
        {
            return;
        }

        m_dragSelectionBox.anchorMin = Vector2.zero;
        m_dragSelectionBox.anchorMax = Vector2.zero;
        m_dragSelectionBox.pivot = Vector2.zero;
        m_dragSelectionBox.anchoredPosition = Vector2.zero;
        m_dragSelectionBox.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Shows the drag selection box if the UI references are valid.
    /// </summary>
    private void ShowDragSelectionBox()
    {
        if (m_dragSelectionBox == null)
        {
            return;
        }

        if (!m_dragSelectionBox.gameObject.activeSelf)
        {
            m_dragSelectionBox.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the drag selection box if the UI reference is valid.
    /// </summary>
    private void HideDragSelectionBox()
    {
        if (m_dragSelectionBox == null)
        {
            return;
        }

        m_dragSelectionBox.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates the drag selection box UI to match the current drag rectangle.
    /// </summary>
    private void UpdateDragSelectionBoxVisual()
    {
        if (m_dragSelectionBox == null || m_dragSelectionCanvas == null)
        {
            return;
        }

        RectTransform parentRect = m_dragSelectionBox.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        m_dragSelectionBox.anchorMin = Vector2.zero;
        m_dragSelectionBox.anchorMax = Vector2.zero;
        m_dragSelectionBox.pivot = Vector2.zero;

        Camera uiCamera = null;

        if (m_dragSelectionCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = m_dragSelectionCanvas.worldCamera;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            m_dragStartScreenPosition,
            uiCamera,
            out Vector2 localStartPoint);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            m_dragCurrentScreenPosition,
            uiCamera,
            out Vector2 localCurrentPoint);

        Vector2 anchoredStartPoint = localStartPoint - parentRect.rect.min;
        Vector2 anchoredCurrentPoint = localCurrentPoint - parentRect.rect.min;

        Vector2 min = Vector2.Min(anchoredStartPoint, anchoredCurrentPoint);
        Vector2 max = Vector2.Max(anchoredStartPoint, anchoredCurrentPoint);

        m_dragSelectionBox.anchoredPosition = min;
        m_dragSelectionBox.sizeDelta = max - min;
    }

    /// <summary>
    /// Returns whether either Shift key is currently held.
    /// </summary>
    private bool IsShiftHeld()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
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
    /// Updates deputization input for the currently selected pawns.
    /// Press R to toggle deputization on each selected pawn.
    /// </summary>
    private void UpdateDeputizationInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.rKey.wasPressedThisFrame)
        {
            return;
        }

        if (m_pawnSelectionManager == null || m_pawnDeputizationManager == null)
        {
            return;
        }

        foreach (Pawn pPawn in m_pawnSelectionManager.SelectedPawns)
        {
            if (pPawn == null)
            {
                continue;
            }

            m_pawnDeputizationManager.ToggleDeputizedPawn(pPawn);
        }
    }

    /// <summary>
    /// While right click is held, preview mode is active.
    /// When right click is released, the current move command is executed.
    /// </summary>
    private void UpdateMoveCommandInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (!Mouse.current.rightButton.wasReleasedThisFrame)
        {
            return;
        }

        if (m_pawnMoveCommandExecutor == null)
        {
            return;
        }

        m_pawnMoveCommandExecutor.TryExecuteMoveCommand();
    }

    /// <summary>
    /// Attempts to raycast from the mouse cursor to a pawn in the scene.
    /// </summary>
    private bool TryGetClickedPawn(out Pawn clickedPawn)
    {
        clickedPawn = null;

        if (m_playerCamera == null)
        {
            return false;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Ray ray = m_playerCamera.ScreenPointToRay(mouseScreenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            return false;
        }

        clickedPawn = hitInfo.collider.GetComponentInParent<Pawn>();
        return clickedPawn != null;
    }

    /// <summary>
    /// Attempts to find the pawn currently under the mouse cursor.
    /// </summary>
    private bool TryGetHoveredPawn(out Pawn hoveredPawn)
    {
        hoveredPawn = null;

        if (m_playerCamera == null)
        {
            return false;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Ray ray = m_playerCamera.ScreenPointToRay(mouseScreenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            return false;
        }

        hoveredPawn = hitInfo.collider.GetComponentInParent<Pawn>();
        return hoveredPawn != null;
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