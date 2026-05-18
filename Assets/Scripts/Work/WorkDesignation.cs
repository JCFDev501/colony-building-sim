using System;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Crops;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Represents a lightweight player-marked work target.
    /// Designations allow players to mark large areas without immediately creating many active work orders.
    /// </summary>
    [Serializable]
    public class WorkDesignation
    {
        [SerializeField] private string m_designationId = string.Empty;
        [SerializeField] private WorkType m_workType = WorkType.Cut;
        [SerializeField] private Vector2Int m_targetCoordinates = Vector2Int.zero;
        [SerializeField] private WorkDesignationState m_state = WorkDesignationState.Pending;

        [SerializeField] private bool m_hasBuildableType = false;
        [SerializeField] private BuildableType m_buildableType = BuildableType.Campfire;

        [SerializeField] private bool m_hasCropType = false;
        [SerializeField] private CropType m_cropType = CropType.BerryBush;

        [SerializeField] private string m_promotedWorkOrderId = string.Empty;

        public string DesignationId
        {
            get { return m_designationId; }
        }

        public WorkType WorkType
        {
            get { return m_workType; }
        }

        public Vector2Int TargetCoordinates
        {
            get { return m_targetCoordinates; }
        }

        public WorkDesignationState State
        {
            get { return m_state; }
        }

        public bool HasBuildableType
        {
            get { return m_hasBuildableType; }
        }

        public BuildableType BuildableType
        {
            get { return m_buildableType; }
        }

        public bool HasCropType
        {
            get { return m_hasCropType; }
        }

        public CropType CropType
        {
            get { return m_cropType; }
        }

        public string PromotedWorkOrderId
        {
            get { return m_promotedWorkOrderId; }
        }

        /// <summary>
        /// Creates a lightweight designation for simple work types like Cut or Mine.
        /// </summary>
        public WorkDesignation(WorkType workType, Vector2Int targetCoordinates)
        {
            m_designationId = Guid.NewGuid().ToString();
            m_workType = workType;
            m_targetCoordinates = targetCoordinates;
            m_state = WorkDesignationState.Pending;
        }

        /// <summary>
        /// Creates a lightweight construction designation.
        /// </summary>
        public WorkDesignation(WorkType workType, Vector2Int targetCoordinates, BuildableType buildableType)
        {
            m_designationId = Guid.NewGuid().ToString();
            m_workType = workType;
            m_targetCoordinates = targetCoordinates;
            m_state = WorkDesignationState.Pending;
            m_hasBuildableType = true;
            m_buildableType = buildableType;
        }

        /// <summary>
        /// Creates a lightweight plant designation.
        /// </summary>
        public WorkDesignation(WorkType workType, Vector2Int targetCoordinates, CropType cropType)
        {
            m_designationId = Guid.NewGuid().ToString();
            m_workType = workType;
            m_targetCoordinates = targetCoordinates;
            m_state = WorkDesignationState.Pending;
            m_hasCropType = true;
            m_cropType = cropType;
        }

        /// <summary>
        /// Marks this designation as promoted into a real work order.
        /// </summary>
        public void MarkPromoted(string workOrderId)
        {
            if (m_state == WorkDesignationState.Cancelled
                || m_state == WorkDesignationState.Complete
                || m_state == WorkDesignationState.Invalid)
            {
                return;
            }

            m_state = WorkDesignationState.Promoted;
            m_promotedWorkOrderId = workOrderId;
        }

        /// <summary>
        /// Returns this designation to pending state so it can be promoted again later.
        /// </summary>
        public void MarkPending()
        {
            if (m_state == WorkDesignationState.Cancelled
                || m_state == WorkDesignationState.Complete
                || m_state == WorkDesignationState.Invalid)
            {
                return;
            }

            m_state = WorkDesignationState.Pending;
            m_promotedWorkOrderId = string.Empty;
        }

        /// <summary>
        /// Marks this designation complete.
        /// </summary>
        public void MarkComplete()
        {
            m_state = WorkDesignationState.Complete;
        }

        /// <summary>
        /// Cancels this designation.
        /// </summary>
        public void Cancel()
        {
            m_state = WorkDesignationState.Cancelled;
        }

        /// <summary>
        /// Marks this designation invalid because its target can no longer support the requested work.
        /// </summary>
        public void MarkInvalid()
        {
            m_state = WorkDesignationState.Invalid;
        }
    }
}