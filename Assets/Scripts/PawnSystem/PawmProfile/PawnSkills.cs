using System;
using UnityEngine;

/// <summary>
/// Stores trainable pawn skill values.
/// Skills use a 0-10 range where 0 is poor and 10 is master-level.
/// </summary>
[Serializable]
public class PawnSkills
{
    [SerializeField] [Range(0, 10)] private int m_plant = 0;
    [SerializeField] [Range(0, 10)] private int m_cut = 0;
    [SerializeField] [Range(0, 10)] private int m_construct = 0;
    [SerializeField] [Range(0, 10)] private int m_cook = 0;
    [SerializeField] [Range(0, 10)] private int m_craft = 0;
    [SerializeField] [Range(0, 10)] private int m_mine = 0;

    public int Plant
    {
        get { return m_plant; }
        set { m_plant = Mathf.Clamp(value, 0, 10); }
    }

    public int Cut
    {
        get { return m_cut; }
        set { m_cut = Mathf.Clamp(value, 0, 10); }
    }

    public int Construct
    {
        get { return m_construct; }
        set { m_construct = Mathf.Clamp(value, 0, 10); }
    }

    public int Cook
    {
        get { return m_cook; }
        set { m_cook = Mathf.Clamp(value, 0, 10); }
    }

    public int Craft
    {
        get { return m_craft; }
        set { m_craft = Mathf.Clamp(value, 0, 10); }
    }

    public int Mine
    {
        get { return m_mine; }
        set { m_mine = Mathf.Clamp(value, 0, 10); }
    }
}