using System;
using UnityEngine;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Crops;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Represents a persistent colony-level work goal requested by the player or simulation.
    /// Work orders describe the desired outcome, while pawn tasks later describe individual actions.
    /// </summary>
    [Serializable]
    public class WorkOrder
    {
        [SerializeField] private string m_workOrderId = string.Empty;
        [SerializeField] private WorkType m_workType = WorkType.Cut;
        [SerializeField] private Vector2Int m_targetCoordinates = Vector2Int.zero;
        [SerializeField] private WorkOrderState m_state = WorkOrderState.Planned;
        [SerializeField] private float m_progress = 0.0f;
        [SerializeField] private bool m_hasBuildableType = false;
        [SerializeField] private BuildableType m_buildableType = BuildableType.Campfire;
        [SerializeField] private bool m_hasCropType = false;
        [SerializeField] private CropType m_cropType = CropType.BerryBush;
        [SerializeField] private bool m_hasPlantWorkAction = false;
        [SerializeField] private PlantWorkAction m_plantWorkAction = PlantWorkAction.PlantCrop;

        public string WorkOrderId { get { return m_workOrderId; } }
        public WorkType WorkType { get { return m_workType; } }
        public Vector2Int TargetCoordinates { get { return m_targetCoordinates; } }
        public WorkOrderState State { get { return m_state; } }
        public float Progress { get { return m_progress; } }
        public bool HasCropType { get { return m_hasCropType; } }
        public CropType CropType { get { return m_cropType; } }
        public bool HasPlantWorkAction { get { return m_hasPlantWorkAction; } }
        public PlantWorkAction PlantWorkAction { get { return m_plantWorkAction; } }
        public bool HasBuildableType { get { return m_hasBuildableType; } }

        public BuildableType BuildableType
        {
            get { return m_buildableType; }
        }

        /// <summary>
        /// Creates a work order for a specific work type and target tile.
        /// </summary>
        public WorkOrder(WorkType workType, Vector2Int targetCoordinates)
        {
            m_workOrderId = Guid.NewGuid().ToString();
            m_workType = workType;
            m_targetCoordinates = targetCoordinates;
            m_state = WorkOrderState.Planned;
            m_progress = 0.0f;
        }
        
        /// <summary>
        /// Creates a construction work order for a specific buildable type and target tile.
        /// </summary>
        public WorkOrder(WorkType workType, Vector2Int targetCoordinates, BuildableType buildableType)
        {
            m_workOrderId = Guid.NewGuid().ToString();
            m_workType = workType;
            m_targetCoordinates = targetCoordinates;
            m_state = WorkOrderState.Planned;
            m_progress = 0.0f;
            m_hasBuildableType = true;
            m_buildableType = buildableType;
        }
        
        /// <summary>
        /// Creates a plant work order for a specific crop type and plant work action.
        /// </summary>
        public WorkOrder(
            WorkType workType,
            Vector2Int targetCoordinates,
            CropType cropType,
            PlantWorkAction plantWorkAction)
        {
            m_workOrderId = Guid.NewGuid().ToString();
            m_workType = workType;
            m_targetCoordinates = targetCoordinates;
            m_state = WorkOrderState.Planned;
            m_progress = 0.0f;

            m_hasCropType = true;
            m_cropType = cropType;
            m_hasPlantWorkAction = true;
            m_plantWorkAction = plantWorkAction;
        }

        /// <summary>
        /// Marks this work order as complete and locks its progress at 100 percent.
        /// </summary>
        public void MarkComplete()
        {
            m_state = WorkOrderState.Complete;
            m_progress = 1.0f;
        }
        
        /// <summary>
        /// Marks this work order as ready so pawn tasks can be generated from it.
        /// </summary>
        public void MarkReady()
        {
            if (m_state == WorkOrderState.Complete || m_state == WorkOrderState.Cancelled)
            {
                return;
            }

            m_state = WorkOrderState.Ready;
        }

        /// <summary>
        /// Cancels this work order so it can no longer be completed by pawn tasks.
        /// </summary>
        public void Cancel()
        {
            m_state = WorkOrderState.Cancelled;
        }
    }
}