namespace ColonyBuildingSim.Crops
{
    /// <summary>
    /// Defines the specific farming action represented by a Plant work order.
    /// This keeps planting and harvesting under the shared Plant work priority bucket.
    /// </summary>
    public enum PlantWorkAction
    {
        PlantCrop = 0,
        HarvestCrop = 1
    }
}