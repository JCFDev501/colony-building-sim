/// <summary>
/// Represents the high-level autonomous goal a pawn wants to perform.
/// This sits above the existing work priority system.
/// </summary>
public enum PawnUtilityGoal
{
    Work,
    Eat,
    Sleep,
    Wander
}