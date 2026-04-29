using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages pawn selection state for the prototype.
/// This class owns the rules for selecting, deselecting,
/// and clearing selected pawns.
/// </summary>
public class PawnSelectionManager : MonoBehaviour
{
    private readonly List<Pawn> m_selectedPawns = new();

    /// <summary>
    /// Gets the currently selected pawns.
    /// </summary>
    public IReadOnlyList<Pawn> SelectedPawns
    {
        get { return m_selectedPawns; }
    }

    /// <summary>
    /// Selects only the provided pawn and clears all other selections.
    /// </summary>
    public void SelectSinglePawn(Pawn pPawn)
    {
        ClearSelection();

        if (pPawn == null)
        {
            return;
        }

        m_selectedPawns.Add(pPawn);
        pPawn.SetSelected(true);
    }

    /// <summary>
    /// Replaces the current selection with the provided pawn collection.
    /// Duplicate and null pawns are ignored.
    /// </summary>
    public void SelectPawns(IEnumerable<Pawn> pawns)
    {
        ClearSelection();

        if (pawns == null)
        {
            return;
        }

        HashSet<Pawn> uniquePawns = new();

        foreach (Pawn pPawn in pawns)
        {
            if (pPawn == null)
            {
                continue;
            }

            if (!uniquePawns.Add(pPawn))
            {
                continue;
            }

            m_selectedPawns.Add(pPawn);
            pPawn.SetSelected(true);
        }
    }

    /// <summary>
    /// Adds the pawn to the current selection if it is not already selected.
    /// </summary>
    public void AddPawnToSelection(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return;
        }

        if (m_selectedPawns.Contains(pPawn))
        {
            return;
        }

        m_selectedPawns.Add(pPawn);
        pPawn.SetSelected(true);
    }

    /// <summary>
    /// Removes the pawn from the current selection if it is selected.
    /// </summary>
    public void RemovePawnFromSelection(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return;
        }

        if (!m_selectedPawns.Remove(pPawn))
        {
            return;
        }

        pPawn.SetSelected(false);
    }

    /// <summary>
    /// Toggles whether the pawn is part of the current selection.
    /// </summary>
    public void TogglePawnSelection(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return;
        }

        if (m_selectedPawns.Contains(pPawn))
        {
            RemovePawnFromSelection(pPawn);
        }
        else
        {
            AddPawnToSelection(pPawn);
        }
    }

    /// <summary>
    /// Toggles each pawn in the provided collection.
    /// Duplicate and null pawns are ignored.
    /// </summary>
    public void TogglePawnSelection(IEnumerable<Pawn> pawns)
    {
        if (pawns == null)
        {
            return;
        }

        HashSet<Pawn> uniquePawns = new();

        foreach (Pawn pPawn in pawns)
        {
            if (pPawn == null)
            {
                continue;
            }

            if (!uniquePawns.Add(pPawn))
            {
                continue;
            }

            TogglePawnSelection(pPawn);
        }
    }

    /// <summary>
    /// Clears the current selection.
    /// Deputized state is intentionally left unchanged.
    /// </summary>
    public void ClearSelection()
    {
        foreach (Pawn pPawn in m_selectedPawns)
        {
            if (pPawn != null)
            {
                pPawn.SetSelected(false);
            }
        }

        m_selectedPawns.Clear();
    }

    /// <summary>
    /// Returns whether the pawn is currently selected.
    /// </summary>
    public bool IsPawnSelected(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return false;
        }

        return m_selectedPawns.Contains(pPawn);
    }
}