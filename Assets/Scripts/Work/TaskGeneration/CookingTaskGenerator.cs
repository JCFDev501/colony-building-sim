using System.Collections.Generic;
using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Inventory;
using UnityEngine;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Generates Cook tasks from completed campfire structures when the colony has enough Food.
    /// Campfires act as persistent workstations, so cook tasks are created only when requirements are met.
    /// </summary>
    public class CookingTaskGenerator
    {
        private readonly StructureRegistry m_structureRegistry;
        private readonly ColonyInventoryManager m_colonyInventoryManager;
        private readonly int m_foodPerMeal;
        private readonly float m_cookWorkDuration;

        public CookingTaskGenerator(
            StructureRegistry pStructureRegistry,
            ColonyInventoryManager pColonyInventoryManager,
            int foodPerMeal,
            float cookWorkDuration)
        {
            m_structureRegistry = pStructureRegistry;
            m_colonyInventoryManager = pColonyInventoryManager;
            m_foodPerMeal = foodPerMeal;
            m_cookWorkDuration = cookWorkDuration;
        }

        /// <summary>
        /// Generates available Cook tasks from completed Campfire structures when enough Food exists.
        /// </summary>
        public void GenerateCookTasksFromCampfires(
            List<PawnTask> pTasks,
            System.Func<PawnTask, bool> addTask)
        {
            if (pTasks == null || addTask == null)
            {
                return;
            }

            if (m_structureRegistry == null || m_colonyInventoryManager == null)
            {
                return;
            }

            if (!m_colonyInventoryManager.HasResource(ResourceType.Food, m_foodPerMeal))
            {
                return;
            }

            List<StructureInstance> campfires = m_structureRegistry.GetStructuresByType(BuildableType.Campfire);

            foreach (StructureInstance pCampfire in campfires)
            {
                if (pCampfire == null)
                {
                    continue;
                }

                if (HasActiveCookTaskAt(pTasks, pCampfire.GridCoordinates))
                {
                    continue;
                }

                WorkOrder cookWorkOrder = new WorkOrder(
                    WorkType.Cook,
                    pCampfire.GridCoordinates);

                cookWorkOrder.MarkReady();

                PawnTask cookTask = new PawnTask(
                    TaskType.Cook,
                    cookWorkOrder,
                    m_cookWorkDuration);

                addTask(cookTask);
            }
        }

        /// <summary>
        /// Returns true when an active Cook task already targets the requested coordinates.
        /// </summary>
        private bool HasActiveCookTaskAt(
            List<PawnTask> pTasks,
            Vector2Int targetCoordinates)
        {
            foreach (PawnTask pTask in pTasks)
            {
                if (!IsActiveTask(pTask))
                {
                    continue;
                }

                if (pTask.TaskType != TaskType.Cook)
                {
                    continue;
                }

                if (pTask.TargetCoordinates == targetCoordinates)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when a task is still active in the task system.
        /// </summary>
        private bool IsActiveTask(PawnTask pTask)
        {
            return pTask != null
                && pTask.State != TaskState.Complete
                && pTask.State != TaskState.Cancelled;
        }
    }
}