using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Derives the current movement command group from pawn selection
/// and deputization state. Only pawns that are both selected and
/// deputized are included in the commanded pawn set.
/// </summary>
public class PawnCommandManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PawnSelectionManager m_pawnSelectionManager;
    [SerializeField] private PawnDeputizationManager m_pawnDeputizationManager;

    [Header("Debug")]
    [SerializeField] private bool m_logCommandedPawnState = true;

    /// <summary>
    /// Returns the pawns that are currently eligible to receive movement commands.
    /// The returned order matches selection order.
    /// </summary>
    public List<Pawn> GetCommandedPawns()
    {
        List<Pawn> commandedPawns = new List<Pawn>();

        if (m_pawnSelectionManager == null || m_pawnDeputizationManager == null)
        {
            return commandedPawns;
        }

        foreach (Pawn pPawn in m_pawnSelectionManager.SelectedPawns)
        {
            if (pPawn == null)
            {
                continue;
            }

            if (!m_pawnDeputizationManager.IsPawnDeputized(pPawn))
            {
                continue;
            }

            commandedPawns.Add(pPawn);
        }

        return commandedPawns;
    }

    /// <summary>
    /// Attempts to return the current anchor pawn.
    /// The anchor pawn is the first selected pawn that is also deputized.
    /// </summary>
    public bool TryGetAnchorPawn(out Pawn anchorPawn)
    {
        anchorPawn = null;

        List<Pawn> commandedPawns = GetCommandedPawns();

        if (commandedPawns.Count == 0)
        {
            return false;
        }

        anchorPawn = commandedPawns[0];
        return true;
    }

    /// <summary>
    /// Returns whether a valid commanded pawn group currently exists.
    /// </summary>
    public bool HasCommandedPawns()
    {
        return GetCommandedPawns().Count > 0;
    }

    /// <summary>
    /// Logs the current commanded pawn group for prototype testing.
    /// </summary>
    [ContextMenu("Log Commanded Pawn State")]
    public void LogCommandedPawnState()
    {
        if (!m_logCommandedPawnState)
        {
            return;
        }

        List<Pawn> commandedPawns = GetCommandedPawns();

        Debug.Log("Commanded pawn count: " + commandedPawns.Count, this);

        if (TryGetAnchorPawn(out Pawn anchorPawn))
        {
            Debug.Log("Anchor pawn: " + anchorPawn.PawnId, anchorPawn);
        }
        else
        {
            Debug.Log("Anchor pawn: none", this);
        }

        for (int i = 0; i < commandedPawns.Count; i++)
        {
            Pawn pPawn = commandedPawns[i];
            Debug.Log("Commanded pawn [" + i + "]: " + pPawn.PawnId, pPawn);
        }
    }
}