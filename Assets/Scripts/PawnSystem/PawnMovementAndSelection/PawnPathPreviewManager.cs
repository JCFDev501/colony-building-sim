using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws simple path previews for all currently assigned commanded pawns
/// using the current resolved move preview tile and destination assignments.
/// Path previews are shown only while right-click preview mode is active.
///
/// While preview is active, path previews use the same arrangement direction
/// as destination preview and final move execution so all preview layers match.
/// Group path planning also treats other pawns in the same commanded group as
/// temporarily enterable so the preview reflects actual group command behavior.
/// </summary>
public class PawnPathPreviewManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private PawnCommandManager m_pawnCommandManager;
    [SerializeField] private PawnMovePreviewManager m_pawnMovePreviewManager;
    [SerializeField] private PawnDestinationAssignmentManager m_pawnDestinationAssignmentManager;
    [SerializeField] private PawnPathfinder m_pawnPathfinder;
    [SerializeField] private GridManager m_gridManager;

    [Header("Marker Setup")]
    [SerializeField] private GameObject m_pathMarkerPrefab;
    [SerializeField] private Transform m_markerParent;
    [SerializeField] private int m_markerPoolSize = 256;
    [SerializeField] private float m_markerYOffset = 0.05f;

    private readonly List<GameObject> m_markerPool = new();

    private void Awake()
    {
        CreateMarkerPool();
    }

    private void Update()
    {
        UpdatePathPreview();
    }

    /// <summary>
    /// Creates a reusable pool of path preview markers.
    /// </summary>
    private void CreateMarkerPool()
    {
        if (m_pathMarkerPrefab == null)
        {
            return;
        }

        for (int i = 0; i < m_markerPoolSize; i++)
        {
            GameObject markerInstance = Instantiate(m_pathMarkerPrefab);

            if (m_markerParent != null)
            {
                markerInstance.transform.SetParent(m_markerParent, false);
            }

            markerInstance.SetActive(false);
            m_markerPool.Add(markerInstance);
        }
    }

    /// <summary>
    /// Updates path previews for all assigned commanded pawns.
    /// Uses the same arrangement direction as destination preview and final execution.
    /// </summary>
    private void UpdatePathPreview()
    {
        HideAllMarkers();

        if (m_playerController == null ||
            m_pawnCommandManager == null ||
            m_pawnMovePreviewManager == null ||
            m_pawnDestinationAssignmentManager == null ||
            m_pawnPathfinder == null ||
            m_gridManager == null)
        {
            return;
        }

        if (!m_playerController.IsMoveCommandPreviewActive)
        {
            return;
        }

        List<Pawn> commandedPawns = m_pawnCommandManager.GetCommandedPawns();

        if (commandedPawns.Count == 0)
        {
            return;
        }

        if (!m_pawnMovePreviewManager.TryGetCurrentResolvedPreviewTile(out Vector2Int resolvedAnchorTile))
        {
            return;
        }

        m_pawnMovePreviewManager.TryGetCurrentArrangementDirection(out Vector2Int arrangementDirection);

        if (!m_pawnDestinationAssignmentManager.TryAssignDestinations(
                commandedPawns,
                resolvedAnchorTile,
                arrangementDirection,
                m_gridManager,
                out Dictionary<Pawn, Vector2Int> assignments))
        {
            return;
        }

        int markerIndex = 0;

        foreach (KeyValuePair<Pawn, Vector2Int> entry in assignments)
        {
            Pawn pPawn = entry.Key;
            Vector2Int assignedDestination = entry.Value;

            if (pPawn == null)
            {
                continue;
            }

            Vector2Int pathStartCoordinates = pPawn.GetPathStartCoordinates();

            if (!m_pawnPathfinder.TryFindPath(
                    pPawn,
                    commandedPawns,
                    m_gridManager,
                    pathStartCoordinates,
                    assignedDestination,
                    out List<Vector2Int> path))
            {
                continue;
            }

            for (int i = 0; i < path.Count; i++)
            {
                if (markerIndex >= m_markerPool.Count)
                {
                    return;
                }

                Vector3 markerWorldPosition = m_gridManager.GetWorldPosition(path[i]);
                markerWorldPosition.y += m_markerYOffset;

                GameObject marker = m_markerPool[markerIndex];
                marker.transform.position = markerWorldPosition;
                marker.SetActive(true);

                ++markerIndex;
            }
        }
    }

    /// <summary>
    /// Hides all pooled path markers.
    /// </summary>
    private void HideAllMarkers()
    {
        foreach (GameObject marker in m_markerPool)
        {
            if (marker != null)
            {
                marker.SetActive(false);
            }
        }
    }
}