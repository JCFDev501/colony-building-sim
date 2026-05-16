namespace ColonyBuildingSim.Work
{
    /// <summary>
    /// Defines the type of immediate action a pawn task represents.
    /// DeliverMaterials is included for future material delivery tasks.
    /// </summary>
    public enum TaskType
    {
        Plant = 0,
        Cut = 1,
        Construct = 2,
        Cook = 3,
        Craft = 4,
        Mine = 5,
        DeliverMaterials = 6
    }
}