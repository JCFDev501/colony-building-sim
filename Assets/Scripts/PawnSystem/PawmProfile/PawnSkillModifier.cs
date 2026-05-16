using System;
using UnityEngine;

/// <summary>
/// Stores skill value changes that can be applied during pawn profile generation.
/// This is plain data and does not apply the modifiers by itself.
/// </summary>
[Serializable]
public class PawnSkillModifier
{
    [SerializeField] private int m_plant = 0;
    [SerializeField] private int m_cut = 0;
    [SerializeField] private int m_construct = 0;
    [SerializeField] private int m_cook = 0;
    [SerializeField] private int m_craft = 0;
    [SerializeField] private int m_mine = 0;

    public int Plant
    {
        get { return m_plant; }
    }

    public int Cut
    {
        get { return m_cut; }
    }

    public int Construct
    {
        get { return m_construct; }
    }

    public int Cook
    {
        get { return m_cook; }
    }

    public int Craft
    {
        get { return m_craft; }
    }

    public int Mine
    {
        get { return m_mine; }
    }
}