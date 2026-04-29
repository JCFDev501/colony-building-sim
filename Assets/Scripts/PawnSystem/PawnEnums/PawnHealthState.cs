/// <summary>
/// Defines the pawn's high-level survival/health state.
/// Detailed sickness, injury, and body-part simulation are out of scope for V1.
/// </summary>
public enum PawnHealthState
{
    Stable,
    AtRisk,
    Dead
}