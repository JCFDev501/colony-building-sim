using System.Collections.Generic;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Crops;
using ColonyBuildingSim.Work;
using UnityEngine;

/// <summary>
/// Processes player-requested work placements over multiple frames.
/// This allows players to mark large areas without forcing all work orders
/// to be created in a single frame.
/// </summary>
public class WorkOrderPlacementQueue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager m_gridManager;
    [SerializeField] private WorkOrderManager m_workOrderManager;
    [SerializeField] private CropManager m_cropManager;
    [SerializeField] private CropDefinitionLibrary m_cropDefinitionLibrary;

    [Header("Processing")]
    [SerializeField] private int m_workOrdersProcessedPerFrame = 25;
    [SerializeField] private bool m_logBatchResults = false;

    private readonly Queue<QueuedWorkPlacement> m_pendingPlacements = new Queue<QueuedWorkPlacement>();
    private readonly HashSet<string> m_pendingPlacementKeys = new HashSet<string>();

    /// <summary>
    /// Gets the number of placement requests waiting to be processed.
    /// </summary>
    public int PendingPlacementCount
    {
        get { return m_pendingPlacements.Count; }
    }

    /// <summary>
    /// Gets whether the queue currently has pending placement requests.
    /// </summary>
    public bool HasPendingPlacements
    {
        get { return m_pendingPlacements.Count > 0; }
    }

    /// <summary>
    /// Finds required references if they were not assigned in the Inspector.
    /// </summary>
    private void Start()
    {
        if (m_gridManager == null)
        {
            m_gridManager = FindFirstObjectByType<GridManager>();
        }

        if (m_workOrderManager == null)
        {
            m_workOrderManager = FindFirstObjectByType<WorkOrderManager>();
        }

        if (m_cropManager == null)
        {
            m_cropManager = FindFirstObjectByType<CropManager>();
        }

        if (m_cropDefinitionLibrary == null)
        {
            m_cropDefinitionLibrary = FindFirstObjectByType<CropDefinitionLibrary>();
        }

        if (m_gridManager == null)
        {
            Debug.LogError("WorkOrderPlacementQueue is missing a GridManager reference.", this);
        }

        if (m_workOrderManager == null)
        {
            Debug.LogError("WorkOrderPlacementQueue is missing a WorkOrderManager reference.", this);
        }
    }

    /// <summary>
    /// Processes a small number of queued placements each frame.
    /// </summary>
    private void Update()
    {
        ProcessQueuedPlacements();
    }

    /// <summary>
    /// Adds one placement request to the queue.
    /// Duplicate pending requests are ignored.
    /// </summary>
    public bool EnqueuePlacement(QueuedWorkPlacement placement)
    {
        string placementKey = BuildPlacementKey(placement);

        if (m_pendingPlacementKeys.Contains(placementKey))
        {
            return false;
        }

        m_pendingPlacements.Enqueue(placement);
        m_pendingPlacementKeys.Add(placementKey);

        return true;
    }

    /// <summary>
    /// Adds many placement requests to the queue.
    /// Returns how many were accepted into the queue.
    /// </summary>
    public int EnqueuePlacements(IEnumerable<QueuedWorkPlacement> placements)
    {
        if (placements == null)
        {
            return 0;
        }

        int acceptedCount = 0;

        foreach (QueuedWorkPlacement placement in placements)
        {
            if (EnqueuePlacement(placement))
            {
                ++acceptedCount;
            }
        }

        return acceptedCount;
    }

    /// <summary>
    /// Clears all pending placement requests.
    /// </summary>
    public void ClearQueue()
    {
        m_pendingPlacements.Clear();
        m_pendingPlacementKeys.Clear();
    }

    /// <summary>
    /// Processes a limited batch of placements this frame.
    /// </summary>
    private void ProcessQueuedPlacements()
    {
        if (m_pendingPlacements.Count == 0)
        {
            return;
        }

        int attemptsThisFrame = Mathf.Max(1, m_workOrdersProcessedPerFrame);
        int processedCount = 0;
        int createdCount = 0;

        while (processedCount < attemptsThisFrame && m_pendingPlacements.Count > 0)
        {
            QueuedWorkPlacement placement = m_pendingPlacements.Dequeue();
            m_pendingPlacementKeys.Remove(BuildPlacementKey(placement));

            if (TryCreateWorkOrderFromPlacement(placement))
            {
                ++createdCount;
            }

            ++processedCount;
        }

        if (m_logBatchResults && processedCount > 0)
        {
            Debug.Log(
                "Processed "
                + processedCount
                + " queued work placements. Created "
                + createdCount
                + " work orders. Remaining: "
                + m_pendingPlacements.Count,
                this);
        }
    }

    /// <summary>
    /// Creates a work order from one queued placement if the target is still valid.
    /// </summary>
    private bool TryCreateWorkOrderFromPlacement(QueuedWorkPlacement placement)
    {
        switch (placement.WorkMode)
        {
            case PlayerWorkMode.Cut:
                return TryCreateCutWorkOrder(placement.TargetCoordinates);

            case PlayerWorkMode.Mine:
                return TryCreateMineWorkOrder(placement.TargetCoordinates);

            case PlayerWorkMode.Plant:
                return TryCreatePlantWorkOrder(placement.TargetCoordinates, placement.CropType);

            case PlayerWorkMode.BuildCampfire:
            case PlayerWorkMode.BuildWoodenWall:
            case PlayerWorkMode.BuildWoodenDoor:
                return TryCreateBuildWorkOrder(placement.TargetCoordinates, placement.BuildableType);
        }

        return false;
    }

    /// <summary>
    /// Attempts to create a Cut work order.
    /// </summary>
    private bool TryCreateCutWorkOrder(Vector2Int targetCoordinates)
    {
        if (m_workOrderManager == null || m_gridManager == null)
        {
            return false;
        }

        if (!WorkTargetValidator.IsValidCutTarget(m_gridManager, targetCoordinates))
        {
            return false;
        }

        if (m_workOrderManager.HasDuplicateActiveWorkOrder(WorkType.Cut, targetCoordinates))
        {
            return false;
        }

        WorkOrder workOrder = new WorkOrder(WorkType.Cut, targetCoordinates);
        workOrder.MarkReady();

        return m_workOrderManager.AddWorkOrder(workOrder);
    }

    /// <summary>
    /// Attempts to create a Mine work order.
    /// </summary>
    private bool TryCreateMineWorkOrder(Vector2Int targetCoordinates)
    {
        if (m_workOrderManager == null || m_gridManager == null)
        {
            return false;
        }

        if (!WorkTargetValidator.IsValidMineTarget(m_gridManager, targetCoordinates))
        {
            return false;
        }

        if (m_workOrderManager.HasDuplicateActiveWorkOrder(WorkType.Mine, targetCoordinates))
        {
            return false;
        }

        WorkOrder workOrder = new WorkOrder(WorkType.Mine, targetCoordinates);
        workOrder.MarkReady();

        return m_workOrderManager.AddWorkOrder(workOrder);
    }

    /// <summary>
    /// Attempts to create a Plant work order.
    /// </summary>
    private bool TryCreatePlantWorkOrder(Vector2Int targetCoordinates, CropType cropType)
    {
        if (m_workOrderManager == null || m_gridManager == null || m_cropManager == null || m_cropDefinitionLibrary == null)
        {
            return false;
        }

        if (!m_cropDefinitionLibrary.TryGetDefinition(cropType, out CropDefinition cropDefinition))
        {
            return false;
        }

        if (!IsValidPlantPlacementTarget(targetCoordinates))
        {
            return false;
        }

        if (m_workOrderManager.HasDuplicateActiveWorkOrder(WorkType.Plant, targetCoordinates))
        {
            return false;
        }

        WorkOrder workOrder = new WorkOrder(
            WorkType.Plant,
            targetCoordinates,
            cropType,
            PlantWorkAction.PlantCrop);

        workOrder.MarkReady();

        return m_workOrderManager.AddWorkOrder(workOrder);
    }

    /// <summary>
    /// Attempts to create a Construct work order.
    /// </summary>
    private bool TryCreateBuildWorkOrder(Vector2Int targetCoordinates, BuildableType buildableType)
    {
        if (m_workOrderManager == null || m_gridManager == null)
        {
            return false;
        }

        if (!WorkTargetValidator.IsValidConstructTarget(m_gridManager, targetCoordinates))
        {
            return false;
        }

        if (m_workOrderManager.HasDuplicateActiveWorkOrder(WorkType.Construct, targetCoordinates))
        {
            return false;
        }

        WorkOrder workOrder = new WorkOrder(
            WorkType.Construct,
            targetCoordinates,
            buildableType);

        workOrder.MarkReady();

        return m_workOrderManager.AddWorkOrder(workOrder);
    }

    /// <summary>
    /// Returns whether a tile can receive a plant work order.
    /// </summary>
    private bool IsValidPlantPlacementTarget(Vector2Int targetCoordinates)
    {
        if (m_gridManager == null || m_cropManager == null)
        {
            return false;
        }

        if (!m_gridManager.IsInBounds(targetCoordinates))
        {
            return false;
        }

        if (m_gridManager.GetTerrainType(targetCoordinates) == TileTerrainType.Water)
        {
            return false;
        }

        if (!m_gridManager.CanEnterTile(targetCoordinates))
        {
            return false;
        }

        if (m_gridManager.GetBlockType(targetCoordinates) != BlockType.None)
        {
            return false;
        }

        if (m_gridManager.GetWorldObjectType(targetCoordinates) != WorldObjectType.None)
        {
            return false;
        }

        if (m_gridManager.GetContentType(targetCoordinates) != TileContentType.Empty)
        {
            return false;
        }

        if (m_cropManager.TryGetCropAt(targetCoordinates, out CropInstance existingCrop)
            && existingCrop != null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a unique key for pending placement deduplication.
    /// </summary>
    private string BuildPlacementKey(QueuedWorkPlacement placement)
    {
        return placement.WorkMode
            + "|"
            + placement.TargetCoordinates.x
            + ","
            + placement.TargetCoordinates.y
            + "|"
            + placement.CropType
            + "|"
            + placement.BuildableType;
    }
}