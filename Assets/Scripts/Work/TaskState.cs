namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Represents the lifecycle state of an actionable pawn task.
    /// Tasks describe work a pawn can claim and perform.
    /// </summary>
    public enum TaskState
    {
        Available = 0,
        Claimed = 1,
        InProgress = 2,
        Complete = 3,
        Cancelled = 4
    }
}