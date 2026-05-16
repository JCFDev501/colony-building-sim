/// <summary>
/// Defines the first-pass colony work categories that pawns can prioritize.
/// The enum order is used as the fallback tie-breaker when two work types have the same priority.
/// </summary>
public enum WorkType
{
    Plant = 0,
    Cut = 1,
    Construct = 2,
    Cook = 3,
    Craft = 4,
    Mine = 5
}