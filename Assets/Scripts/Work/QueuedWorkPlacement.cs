using ColonyBuildingSim.Buildables;
using ColonyBuildingSim.Crops;
using UnityEngine;
/// [Deprecated Do not use] --- Delete this eventually
/// <summary>
/// Represents one requested player work placement that can be processed later by WorkOrderPlacementQueue.
/// This keeps large drag placement requests from creating every work order in one frame.
/// </summary>
public readonly struct QueuedWorkPlacement
{
    public readonly PlayerWorkMode WorkMode;
    public readonly Vector2Int TargetCoordinates;
    public readonly CropType CropType;
    public readonly BuildableType BuildableType;

    /// <summary>
    /// Creates a queued work placement request.
    /// </summary>
    public QueuedWorkPlacement(
        PlayerWorkMode workMode,
        Vector2Int targetCoordinates,
        CropType cropType,
        BuildableType buildableType)
    {
        WorkMode = workMode;
        TargetCoordinates = targetCoordinates;
        CropType = cropType;
        BuildableType = buildableType;
    }

    /// <summary>
    /// Creates a queued placement for non-build work.
    /// </summary>
    public static QueuedWorkPlacement CreateWorkPlacement(
        PlayerWorkMode workMode,
        Vector2Int targetCoordinates,
        CropType cropType)
    {
        return new QueuedWorkPlacement(
            workMode,
            targetCoordinates,
            cropType,
            BuildableType.Campfire);
    }

    /// <summary>
    /// Creates a queued placement for construction work.
    /// </summary>
    public static QueuedWorkPlacement CreateBuildPlacement(
        PlayerWorkMode workMode,
        Vector2Int targetCoordinates,
        BuildableType buildableType)
    {
        return new QueuedWorkPlacement(
            workMode,
            targetCoordinates,
            CropType.BerryBush,
            buildableType);
    }
}