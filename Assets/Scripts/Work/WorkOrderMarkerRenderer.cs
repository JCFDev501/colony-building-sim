using System.Collections.Generic;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Displays simple prototype markers for active work orders.
    /// This renderer only handles debug visuals and does not create or modify work orders.
    /// </summary>
    public class WorkOrderMarkerRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager m_gridManager;
        [SerializeField] private WorkOrderManager m_workOrderManager;
        [SerializeField] private GameObject m_cutMarkerPrefab;
        [SerializeField] private GameObject m_constructMarkerPrefab;
        [SerializeField] private GameObject m_plantMarkerPrefab;
        [SerializeField] private GameObject m_mineMarkerPrefab;

        [Header("Marker Settings")]
        [SerializeField] private float m_markerYOffset = 0.08f;
        [SerializeField] private float m_blockMarkerBaseYOffset = 2.0f;

        private readonly Dictionary<string, GameObject> m_workOrderMarkers = new();

        /// <summary>
        /// Updates marker visuals so active work orders have visible markers.
        /// </summary>
        private void Update()
        {
            RefreshMarkers();
        }

        /// <summary>
        /// Synchronizes marker objects with active work orders.
        /// </summary>
        private void RefreshMarkers()
        {
            if (m_gridManager == null || m_workOrderManager == null)
            {
                ClearAllMarkers();
                return;
            }

            HashSet<string> activeMarkerIds = new HashSet<string>();

            AddMarkersForWorkType(WorkType.Cut, m_cutMarkerPrefab, activeMarkerIds);
            AddMarkersForWorkType(WorkType.Construct, m_constructMarkerPrefab, activeMarkerIds);
            AddMarkersForWorkType(WorkType.Plant, m_plantMarkerPrefab, activeMarkerIds);
            AddMarkersForWorkType(WorkType.Mine, m_mineMarkerPrefab, activeMarkerIds);

            RemoveInactiveMarkers(activeMarkerIds);
        }

        /// <summary>
        /// Ensures active work orders of the requested type have marker visuals.
        /// </summary>
        private void AddMarkersForWorkType(
            WorkType workType,
            GameObject markerPrefab,
            HashSet<string> activeMarkerIds)
        {
            if (markerPrefab == null)
            {
                return;
            }

            List<WorkOrder> workOrders = m_workOrderManager.GetWorkOrdersByType(workType);

            foreach (WorkOrder pWorkOrder in workOrders)
            {
                if (pWorkOrder == null)
                {
                    continue;
                }

                activeMarkerIds.Add(pWorkOrder.WorkOrderId);
                EnsureMarkerExists(pWorkOrder, markerPrefab);
            }
        }

        /// <summary>
        /// Creates a marker for the work order if one does not already exist.
        /// </summary>
        private void EnsureMarkerExists(WorkOrder pWorkOrder, GameObject markerPrefab)
        {
            if (pWorkOrder == null || markerPrefab == null)
            {
                return;
            }

            if (m_workOrderMarkers.ContainsKey(pWorkOrder.WorkOrderId))
            {
                return;
            }

            Vector3 markerPosition = GetMarkerWorldPosition(pWorkOrder.TargetCoordinates);

            GameObject pMarker = Instantiate(markerPrefab, markerPosition, Quaternion.identity, transform);
            pMarker.name = pWorkOrder.WorkType + " Marker " + pWorkOrder.TargetCoordinates;

            m_workOrderMarkers.Add(pWorkOrder.WorkOrderId, pMarker);
        }

        /// <summary>
        /// Returns a marker position that is raised for block tiles and slightly above ground tiles.
        /// </summary>
        private Vector3 GetMarkerWorldPosition(Vector2Int targetCoordinates)
        {
            Vector3 markerPosition = m_gridManager.GetWorldPosition(targetCoordinates);

            if (m_gridManager.GetBlockType(targetCoordinates) != BlockType.None)
            {
                markerPosition.y += m_blockMarkerBaseYOffset;
            }

            markerPosition.y += m_markerYOffset;
            return markerPosition;
        }

        /// <summary>
        /// Removes marker objects that no longer map to active work orders.
        /// </summary>
        private void RemoveInactiveMarkers(HashSet<string> activeMarkerIds)
        {
            List<string> markerIdsToRemove = new List<string>();

            foreach (KeyValuePair<string, GameObject> markerPair in m_workOrderMarkers)
            {
                if (!activeMarkerIds.Contains(markerPair.Key))
                {
                    if (markerPair.Value != null)
                    {
                        Destroy(markerPair.Value);
                    }

                    markerIdsToRemove.Add(markerPair.Key);
                }
            }

            foreach (string markerId in markerIdsToRemove)
            {
                m_workOrderMarkers.Remove(markerId);
            }
        }

        /// <summary>
        /// Destroys all currently spawned marker objects.
        /// </summary>
        private void ClearAllMarkers()
        {
            foreach (KeyValuePair<string, GameObject> markerPair in m_workOrderMarkers)
            {
                if (markerPair.Value != null)
                {
                    Destroy(markerPair.Value);
                }
            }

            m_workOrderMarkers.Clear();
        }
    }
}