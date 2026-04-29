/// <summary>
/// Defines the first-pass runtime state for pawn autonomous behavior.
/// This is intentionally small until task selection and work execution are added later.
/// </summary>
public enum PawnBrainState
{
    Disabled = 0,
    Idle = 1,
    Moving = 2,
    Wandering = 3
}