using System.Collections.Generic;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Crops;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Stores lightweight player work designations and promotes a limited number
    /// into real active work orders over time.
    /// </summary>
    public class WorkDesignationManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager m_gridManager;
        [SerializeField] private WorkOrderManager m_workOrderManager;
        [SerializeField] private CropManager m_cropManager;
        [SerializeField] private CropDefinitionLibrary m_cropDefinitionLibrary;

        [Header("Promotion Caps")]
        [SerializeField] private int m_maxActiveCutWorkOrders = 20;
        [SerializeField] private int m_maxActiveMineWorkOrders = 20;
        [SerializeField] private int m_maxActivePlantWorkOrders = 20;
        [SerializeField] private int m_maxActiveConstructWorkOrders = 20;

        [Header("Promotion Timing")]
        [SerializeField] private float m_promotionIntervalSeconds = 0.25f;
        [SerializeField] private int m_maxPromotionAttemptsPerTick = 25;

        [Header("Debug")]
        [SerializeField] private bool m_logDesignationAdds = false;
        [SerializeField] private bool m_logPromotionResults = false;

        private readonly List<WorkDesignation> m_designations = new List<WorkDesignation>();
        private readonly HashSet<string> m_activeDesignationKeys = new HashSet<string>();

        private float m_promotionTimer = 0.0f;

        /// <summary>
        /// Gets all tracked designations.
        /// </summary>
        public IReadOnlyList<WorkDesignation> Designations
        {
            get { return m_designations; }
        }

        /// <summary>
        /// Gets the number of pending designations waiting to become real work orders.
        /// </summary>
        public int PendingDesignationCount
        {
            get
            {
                int count = 0;

                foreach (WorkDesignation designation in m_designations)
                {
                    if (designation == null)
                    {
                        continue;
                    }

                    if (designation.State == WorkDesignationState.Pending)
                    {
                        ++count;
                    }
                }

                return count;
            }
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
                Debug.LogError("WorkDesignationManager is missing a GridManager reference.", this);
            }

            if (m_workOrderManager == null)
            {
                Debug.LogError("WorkDesignationManager is missing a WorkOrderManager reference.", this);
            }
        }

        /// <summary>
        /// Periodically syncs promoted designations and promotes pending designations.
        /// </summary>
        private void Update()
        {
            m_promotionTimer += Time.deltaTime;

            if (m_promotionTimer < m_promotionIntervalSeconds)
            {
                return;
            }

            m_promotionTimer = 0.0f;

            SyncPromotedDesignations();
            PromotePendingDesignations();
        }

        /// <summary>
        /// Adds a Cut designation.
        /// </summary>
        public bool AddCutDesignation(Vector2Int targetCoordinates)
        {
            WorkDesignation designation = new WorkDesignation(WorkType.Cut, targetCoordinates);
            return AddDesignation(designation);
        }

        /// <summary>
        /// Adds a Mine designation.
        /// </summary>
        public bool AddMineDesignation(Vector2Int targetCoordinates)
        {
            WorkDesignation designation = new WorkDesignation(WorkType.Mine, targetCoordinates);
            return AddDesignation(designation);
        }

        /// <summary>
        /// Adds a Plant designation.
        /// </summary>
        public bool AddPlantDesignation(Vector2Int targetCoordinates, CropType cropType)
        {
            WorkDesignation designation = new WorkDesignation(WorkType.Plant, targetCoordinates, cropType);
            return AddDesignation(designation);
        }

        /// <summary>
        /// Adds a Construct designation.
        /// </summary>
        public bool AddConstructDesignation(Vector2Int targetCoordinates, BuildableType buildableType)
        {
            WorkDesignation designation = new WorkDesignation(WorkType.Construct, targetCoordinates, buildableType);
            return AddDesignation(designation);
        }

        /// <summary>
        /// Adds a designation if an active designation does not already exist for the same target and payload.
        /// </summary>
        public bool AddDesignation(WorkDesignation designation)
        {
            if (designation == null)
            {
                return false;
            }

            string designationKey = BuildDesignationKey(designation);

            if (m_activeDesignationKeys.Contains(designationKey))
            {
                return false;
            }

            m_designations.Add(designation);
            m_activeDesignationKeys.Add(designationKey);

            if (m_logDesignationAdds)
            {
                Debug.Log(
                    "Added "
                    + designation.WorkType
                    + " designation at "
                    + designation.TargetCoordinates
                    + ". Pending count: "
                    + PendingDesignationCount,
                    this);
            }

            return true;
        }

        /// <summary>
        /// Cancels a designation and releases its duplicate-prevention key.
        /// </summary>
        public bool CancelDesignation(WorkDesignation designation)
        {
            if (designation == null)
            {
                return false;
            }

            if (designation.State == WorkDesignationState.Cancelled
                || designation.State == WorkDesignationState.Complete
                || designation.State == WorkDesignationState.Invalid)
            {
                return false;
            }

            designation.Cancel();
            m_activeDesignationKeys.Remove(BuildDesignationKey(designation));

            return true;
        }

        /// <summary>
        /// Marks a designation complete and releases its duplicate-prevention key.
        /// </summary>
        public bool MarkDesignationComplete(WorkDesignation designation)
        {
            if (designation == null)
            {
                return false;
            }

            if (designation.State == WorkDesignationState.Complete)
            {
                return false;
            }

            designation.MarkComplete();
            m_activeDesignationKeys.Remove(BuildDesignationKey(designation));

            return true;
        }

        /// <summary>
        /// Marks a designation invalid and releases its duplicate-prevention key.
        /// </summary>
        public bool MarkDesignationInvalid(WorkDesignation designation)
        {
            if (designation == null)
            {
                return false;
            }

            if (designation.State == WorkDesignationState.Invalid)
            {
                return false;
            }

            designation.MarkInvalid();
            m_activeDesignationKeys.Remove(BuildDesignationKey(designation));

            return true;
        }

        /// <summary>
        /// Clears all stored designations.
        /// </summary>
        public void ClearDesignations()
        {
            m_designations.Clear();
            m_activeDesignationKeys.Clear();
        }

        /// <summary>
        /// Syncs promoted designations with their promoted work order state.
        /// </summary>
        private void SyncPromotedDesignations()
        {
            if (m_workOrderManager == null)
            {
                return;
            }

            foreach (WorkDesignation designation in m_designations)
            {
                if (designation == null)
                {
                    continue;
                }

                if (designation.State != WorkDesignationState.Promoted)
                {
                    continue;
                }

                if (!m_workOrderManager.TryGetWorkOrderById(designation.PromotedWorkOrderId, out WorkOrder workOrder))
                {
                    designation.MarkPending();
                    continue;
                }

                if (workOrder.State == WorkOrderState.Complete)
                {
                    MarkDesignationComplete(designation);
                    continue;
                }

                if (workOrder.State == WorkOrderState.Cancelled)
                {
                    designation.MarkPending();
                }
            }
        }

        /// <summary>
        /// Promotes pending designations into real active work orders while respecting active caps.
        /// </summary>
        private void PromotePendingDesignations()
        {
            if (m_workOrderManager == null || m_gridManager == null)
            {
                return;
            }

            int attempts = 0;
            int promotedCount = 0;

            foreach (WorkDesignation designation in m_designations)
            {
                if (attempts >= Mathf.Max(1, m_maxPromotionAttemptsPerTick))
                {
                    break;
                }

                if (designation == null)
                {
                    continue;
                }

                if (designation.State != WorkDesignationState.Pending)
                {
                    continue;
                }

                ++attempts;

                if (!CanPromoteMoreOfType(designation.WorkType))
                {
                    continue;
                }

                if (!IsDesignationTargetStillValid(designation))
                {
                    MarkDesignationInvalid(designation);
                    continue;
                }

                WorkOrder workOrder = CreateWorkOrderFromDesignation(designation);

                if (workOrder == null)
                {
                    continue;
                }

                if (!m_workOrderManager.AddWorkOrder(workOrder))
                {
                    continue;
                }

                designation.MarkPromoted(workOrder.WorkOrderId);
                ++promotedCount;
            }

            if (m_logPromotionResults && (attempts > 0 || promotedCount > 0))
            {
                Debug.Log(
                    "WorkDesignationManager promotion tick. Attempts: "
                    + attempts
                    + ", Promoted: "
                    + promotedCount
                    + ", Pending: "
                    + PendingDesignationCount,
                    this);
            }
        }

        /// <summary>
        /// Returns whether more active work orders of a type may be promoted.
        /// </summary>
        private bool CanPromoteMoreOfType(WorkType workType)
        {
            if (m_workOrderManager == null)
            {
                return false;
            }

            int activeCount = m_workOrderManager.CountActiveWorkOrdersByType(workType);

            switch (workType)
            {
                case WorkType.Cut:
                    return activeCount < m_maxActiveCutWorkOrders;

                case WorkType.Mine:
                    return activeCount < m_maxActiveMineWorkOrders;

                case WorkType.Plant:
                    return activeCount < m_maxActivePlantWorkOrders;

                case WorkType.Construct:
                    return activeCount < m_maxActiveConstructWorkOrders;
            }

            return false;
        }

        /// <summary>
        /// Returns whether a designation target still supports its requested work.
        /// </summary>
        private bool IsDesignationTargetStillValid(WorkDesignation designation)
        {
            if (designation == null || m_gridManager == null)
            {
                return false;
            }

            switch (designation.WorkType)
            {
                case WorkType.Cut:
                    return WorkTargetValidator.IsValidCutTarget(m_gridManager, designation.TargetCoordinates);

                case WorkType.Mine:
                    return WorkTargetValidator.IsValidMineTarget(m_gridManager, designation.TargetCoordinates);

                case WorkType.Plant:
                    return IsValidPlantDesignationTarget(designation);

                case WorkType.Construct:
                    return WorkTargetValidator.IsValidConstructTarget(m_gridManager, designation.TargetCoordinates);
            }

            return false;
        }

        /// <summary>
        /// Creates a work order from a pending designation.
        /// </summary>
        private WorkOrder CreateWorkOrderFromDesignation(WorkDesignation designation)
        {
            if (designation == null)
            {
                return null;
            }

            WorkOrder workOrder = null;

            switch (designation.WorkType)
            {
                case WorkType.Cut:
                    workOrder = new WorkOrder(WorkType.Cut, designation.TargetCoordinates);
                    break;

                case WorkType.Mine:
                    workOrder = new WorkOrder(WorkType.Mine, designation.TargetCoordinates);
                    break;

                case WorkType.Plant:
                    if (!designation.HasCropType)
                    {
                        return null;
                    }

                    workOrder = new WorkOrder(
                        WorkType.Plant,
                        designation.TargetCoordinates,
                        designation.CropType,
                        PlantWorkAction.PlantCrop);
                    break;

                case WorkType.Construct:
                    if (!designation.HasBuildableType)
                    {
                        return null;
                    }

                    workOrder = new WorkOrder(
                        WorkType.Construct,
                        designation.TargetCoordinates,
                        designation.BuildableType);
                    break;
            }

            if (workOrder == null)
            {
                return null;
            }

            workOrder.MarkReady();
            return workOrder;
        }

        /// <summary>
        /// Returns whether a plant designation can still be promoted.
        /// </summary>
        private bool IsValidPlantDesignationTarget(WorkDesignation designation)
        {
            if (designation == null || m_gridManager == null || m_cropManager == null || m_cropDefinitionLibrary == null)
            {
                return false;
            }

            if (!designation.HasCropType)
            {
                return false;
            }

            if (!m_cropDefinitionLibrary.TryGetDefinition(designation.CropType, out CropDefinition cropDefinition))
            {
                return false;
            }

            if (!m_gridManager.IsInBounds(designation.TargetCoordinates))
            {
                return false;
            }

            if (m_gridManager.GetTerrainType(designation.TargetCoordinates) == TileTerrainType.Water)
            {
                return false;
            }

            if (!m_gridManager.CanEnterTile(designation.TargetCoordinates))
            {
                return false;
            }

            if (m_gridManager.GetBlockType(designation.TargetCoordinates) != BlockType.None)
            {
                return false;
            }

            if (m_gridManager.GetWorldObjectType(designation.TargetCoordinates) != WorldObjectType.None)
            {
                return false;
            }

            if (m_gridManager.GetContentType(designation.TargetCoordinates) != TileContentType.Empty)
            {
                return false;
            }

            if (m_cropManager.TryGetCropAt(designation.TargetCoordinates, out CropInstance existingCrop)
                && existingCrop != null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Builds a stable duplicate-prevention key for a designation.
        /// </summary>
        private string BuildDesignationKey(WorkDesignation designation)
        {
            string buildableValue = designation.HasBuildableType
                ? designation.BuildableType.ToString()
                : "None";

            string cropValue = designation.HasCropType
                ? designation.CropType.ToString()
                : "None";

            return designation.WorkType
                + "|"
                + designation.TargetCoordinates.x
                + ","
                + designation.TargetCoordinates.y
                + "|"
                + buildableValue
                + "|"
                + cropValue;
        }
    }
}