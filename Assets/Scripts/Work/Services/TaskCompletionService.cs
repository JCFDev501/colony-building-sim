namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Routes task completion requests to the correct task-specific completion service.
    /// This keeps TaskManager from knowing how each task type is completed.
    /// </summary>
    public class TaskCompletionService
    {
        private readonly ResourceGatherTaskCompletionService m_resourceGatherTaskCompletionService;
        private readonly ConstructTaskCompletionService m_constructTaskCompletionService;
        private readonly CropTaskCompletionService m_cropTaskCompletionService;
        private readonly CookingTaskCompletionService m_cookingTaskCompletionService;

        public TaskCompletionService(
            ResourceGatherTaskCompletionService pResourceGatherTaskCompletionService,
            ConstructTaskCompletionService pConstructTaskCompletionService,
            CropTaskCompletionService pCropTaskCompletionService,
            CookingTaskCompletionService pCookingTaskCompletionService)
        {
            m_resourceGatherTaskCompletionService = pResourceGatherTaskCompletionService;
            m_constructTaskCompletionService = pConstructTaskCompletionService;
            m_cropTaskCompletionService = pCropTaskCompletionService;
            m_cookingTaskCompletionService = pCookingTaskCompletionService;
        }

        /// <summary>
        /// Completes a task and applies any task-specific world effects.
        /// </summary>
        public bool CompleteTask(PawnTask pTask)
        {
            if (pTask == null)
            {
                return false;
            }

            if (pTask.State == TaskState.Cancelled)
            {
                return false;
            }

            switch (pTask.TaskType)
            {
                case TaskType.Cut:
                    return CompleteCutTask(pTask);

                case TaskType.Mine:
                    return CompleteMineTask(pTask);

                case TaskType.Construct:
                    return CompleteConstructTask(pTask);

                case TaskType.Plant:
                    return CompletePlantTask(pTask);

                case TaskType.Cook:
                    return CompleteCookTask(pTask);

                default:
                    return CompleteDefaultTask(pTask);
            }
        }

        /// <summary>
        /// Completes a Cut task through the resource-gathering completion service.
        /// </summary>
        private bool CompleteCutTask(PawnTask pTask)
        {
            if (m_resourceGatherTaskCompletionService == null)
            {
                return false;
            }

            return m_resourceGatherTaskCompletionService.CompleteCutTask(pTask);
        }

        /// <summary>
        /// Completes a Mine task through the resource-gathering completion service.
        /// </summary>
        private bool CompleteMineTask(PawnTask pTask)
        {
            if (m_resourceGatherTaskCompletionService == null)
            {
                return false;
            }

            return m_resourceGatherTaskCompletionService.CompleteMineTask(pTask);
        }

        /// <summary>
        /// Completes a Construct task through the construction completion service.
        /// </summary>
        private bool CompleteConstructTask(PawnTask pTask)
        {
            if (m_constructTaskCompletionService == null)
            {
                return false;
            }

            return m_constructTaskCompletionService.CompleteConstructTask(pTask);
        }

        /// <summary>
        /// Completes a Plant task through the crop completion service.
        /// </summary>
        private bool CompletePlantTask(PawnTask pTask)
        {
            if (m_cropTaskCompletionService == null)
            {
                return false;
            }

            return m_cropTaskCompletionService.CompletePlantTask(pTask);
        }

        /// <summary>
        /// Completes a Cook task through the cooking completion service.
        /// </summary>
        private bool CompleteCookTask(PawnTask pTask)
        {
            if (m_cookingTaskCompletionService == null)
            {
                return false;
            }

            return m_cookingTaskCompletionService.CompleteCookTask(pTask);
        }

        /// <summary>
        /// Completes task types that do not have custom completion behavior.
        /// </summary>
        private bool CompleteDefaultTask(PawnTask pTask)
        {
            pTask.Complete();

            if (pTask.ParentWorkOrder != null)
            {
                pTask.ParentWorkOrder.MarkComplete();
            }

            return true;
        }
    }
}