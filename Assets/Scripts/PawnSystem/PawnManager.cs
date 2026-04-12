using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active pawns in the scene so future selection,
/// command, and movement systems can reference them from one place.
/// </summary>
public class PawnManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool m_logRegistrationEvents = false;

    private readonly List<Pawn> m_activePawns = new();

    /// <summary>
    /// Gets the currently active pawns.
    /// </summary>
    public IReadOnlyList<Pawn> ActivePawns
    {
        get { return m_activePawns; }
    }

    /// <summary>
    /// Registers a pawn if it is valid and not already tracked.
    /// </summary>
    public void RegisterPawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            Debug.LogWarning("PawnManager.RegisterPawn was called with a null pawn.");
            return;
        }

        if (m_activePawns.Contains(pPawn))
        {
            return;
        }

        m_activePawns.Add(pPawn);

        if (m_logRegistrationEvents)
        {
            Debug.Log($"Registered pawn: {pPawn.PawnId}", pPawn);
        }
    }

    /// <summary>
    /// Unregisters a pawn if it is currently tracked.
    /// </summary>
    public void UnregisterPawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            Debug.LogWarning("PawnManager.UnregisterPawn was called with a null pawn.");
            return;
        }

        if (!m_activePawns.Remove(pPawn))
        {
            return;
        }

        if (m_logRegistrationEvents)
        {
            Debug.Log($"Unregistered pawn: {pPawn.PawnId}", pPawn);
        }
    }

    /// <summary>
    /// Returns true if the pawn is currently tracked.
    /// </summary>
    public bool IsPawnRegistered(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return false;
        }

        return m_activePawns.Contains(pPawn);
    }
}