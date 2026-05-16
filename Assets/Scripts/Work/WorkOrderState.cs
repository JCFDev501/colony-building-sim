namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Represents the lifecycle state of a colony-level work order.
    /// Work orders describe requested outcomes, not individual pawn actions.
    /// </summary>
    public enum WorkOrderState
    {
        Planned = 0,
        WaitingForMaterials = 1,
        Ready = 2,
        InProgress = 3,
        Complete = 4,
        Cancelled = 5
    }
}