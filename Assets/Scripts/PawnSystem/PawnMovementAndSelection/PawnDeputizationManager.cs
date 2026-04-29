using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages pawn deputization state for the prototype.
/// Deputization is separate from selection, but a pawn must be
/// selected before it can be deputized.
/// </summary>
public class PawnDeputizationManager : MonoBehaviour
{
    private readonly List<Pawn> m_deputizedPawns = new();

    /// <summary>
    /// Gets the currently deputized pawns.
    /// </summary>
    public IReadOnlyList<Pawn> DeputizedPawns
    {
        get { return m_deputizedPawns; }
    }

    /// <summary>
    /// Attempts to deputize the pawn.
    /// A pawn must already be selected before it can be deputized.
    /// </summary>
    public bool TryDeputizePawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return false;
        }

        if (!pPawn.IsSelected)
        {
            return false;
        }

        if (m_deputizedPawns.Contains(pPawn))
        {
            return true;
        }

        m_deputizedPawns.Add(pPawn);
        pPawn.SetDeputized(true);
        return true;
    }

    /// <summary>
    /// Removes deputized state from the pawn if present.
    /// </summary>
    public void UndeputizePawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return;
        }

        if (!m_deputizedPawns.Remove(pPawn))
        {
            return;
        }

        pPawn.SetDeputized(false);
    }

    /// <summary>
    /// Toggles deputized state for the pawn.
    /// If the pawn is not already deputized, it must be selected first.
    /// </summary>
    public bool ToggleDeputizedPawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return false;
        }

        if (m_deputizedPawns.Contains(pPawn))
        {
            UndeputizePawn(pPawn);
            return true;
        }

        return TryDeputizePawn(pPawn);
    }

    /// <summary>
    /// Returns whether the pawn is currently deputized.
    /// </summary>
    public bool IsPawnDeputized(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return false;
        }

        return m_deputizedPawns.Contains(pPawn);
    }
}