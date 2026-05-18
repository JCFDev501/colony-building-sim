using ColonyBuildingSim.Inventory;

namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Completes cooking tasks by consuming raw Food and adding finished Meals to colony inventory.
    /// </summary>
    public class CookingTaskCompletionService
    {
        private readonly ColonyInventoryManager m_colonyInventoryManager;
        private readonly int m_foodPerMeal;

        public CookingTaskCompletionService(
            ColonyInventoryManager pColonyInventoryManager,
            int foodPerMeal)
        {
            m_colonyInventoryManager = pColonyInventoryManager;
            m_foodPerMeal = foodPerMeal;
        }

        /// <summary>
        /// Completes Cook work by consuming Food and adding Meal to colony inventory.
        /// </summary>
        public bool CompleteCookTask(PawnTask pTask)
        {
            if (pTask == null || m_colonyInventoryManager == null)
            {
                return false;
            }

            if (!m_colonyInventoryManager.TryRemoveResource(ResourceType.Food, m_foodPerMeal))
            {
                pTask.Cancel();
                return false;
            }

            m_colonyInventoryManager.AddResource(ResourceType.Meal, 1);

            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            return true;
        }
    }
}