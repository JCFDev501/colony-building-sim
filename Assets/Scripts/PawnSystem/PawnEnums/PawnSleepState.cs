/// <summary>
/// Defines the display state derived from a pawn's sleep value.
/// This is derived from stored sleep value and does not tick down in V1.
/// </summary>
public enum PawnSleepState
{
    Rested,
    Tired,
    Exhausted
}