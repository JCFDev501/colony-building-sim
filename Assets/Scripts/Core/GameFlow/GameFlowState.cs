/// <summary>
/// Defines the high-level startup state of the prototype game scene.
/// GameFlowManager uses this state to coordinate pawn profile generation,
/// future starter pawn selection, world generation, starter pawn spawning,
/// and future new-game/load-game flow.
/// </summary>
public enum GameFlowState
{
    NotStarted,
    GeneratingStarterPawns,
    WaitingForStarterPawnSelection,
    GeneratingWorld,
    SpawningStarterPawns,
    Ready,
    Failed
}