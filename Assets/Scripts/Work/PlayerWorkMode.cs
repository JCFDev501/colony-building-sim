/// <summary>
/// Defines the player-facing work placement mode selected from the work action bar.
/// None means normal selection/movement input is active.
/// </summary>
public enum PlayerWorkMode
{
    None = 0,
    Cut = 1,
    Mine = 2,
    Plant = 3,
    BuildCampfire = 4,
    BuildWoodenWall = 5,
    BuildWoodenDoor = 6
}