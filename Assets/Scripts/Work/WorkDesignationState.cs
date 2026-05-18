namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Defines the lightweight lifecycle state for player-marked work designations.
    /// Designations are not pawn tasks and are not full work orders.
    /// They represent player intent until promoted into real work orders.
    /// </summary>
    public enum WorkDesignationState
    {
        Pending,
        Promoted,
        Complete,
        Cancelled,
        Invalid
    }
}