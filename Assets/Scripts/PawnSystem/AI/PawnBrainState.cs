/// <summary>
/// Defines the first-pass runtime state for pawn autonomous behavior.
/// This is intentionally small, but now includes personal need behavior states.
/// </summary>
public enum PawnBrainState
{
    Disabled = 0,
    Idle = 1,
    Moving = 2,
    Wandering = 3,
    Eating = 4,
    MovingToSleep = 5,
    Sleeping = 6
}