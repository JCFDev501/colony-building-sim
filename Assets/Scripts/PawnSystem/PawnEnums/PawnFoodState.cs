/// <summary>
/// Defines the display state derived from a pawn's food value.
/// This is derived from stored food value and does not tick down in V1.
/// </summary>
public enum PawnFoodState
{
    Full,
    Content,
    Hungry,
    Starving
}