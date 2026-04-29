using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores generated pawn profile data independently from the runtime Pawn MonoBehaviour.
/// This is runtime/save data, not reusable authored content, so it must not be a ScriptableObject.
/// </summary>
[Serializable]
public class PawnProfile
{
    [Header("Identity")]
    [SerializeField] private string m_pawnId = string.Empty;
    [SerializeField] private string m_firstName = string.Empty;
    [SerializeField] private string m_lastName = string.Empty;
    [SerializeField] private string m_displayName = string.Empty;
    [SerializeField] private int m_age = 18;
    [SerializeField] private PawnAgeBand m_ageBand = PawnAgeBand.YoungAdult;
    [SerializeField] private PawnGender m_gender = PawnGender.Nonbinary;

    [Header("Background")]
    [SerializeField] private PawnBackgroundDefinition m_backgroundDefinition;
    [SerializeField] private string m_backgroundId = string.Empty;

    [Header("Traits")]
    [SerializeField] private List<PawnTraitDefinition> m_traitDefinitions = new List<PawnTraitDefinition>();
    [SerializeField] private List<string> m_traitIds = new List<string>();

    [Header("Skills")]
    [SerializeField] private PawnSkills m_skills = new PawnSkills();

    [Header("Work Priorities")]
    [SerializeField] private PawnWorkPriorities m_workPriorities = new PawnWorkPriorities();

    [Header("Condition")]
    [SerializeField] private PawnCondition m_condition = new PawnCondition();

    [Header("Movement")]
    [SerializeField] private float m_baseMoveSpeed = 3.0f;
    [SerializeField] private float m_finalMoveSpeed = 3.0f;

    [Header("Derived Stat Modifiers")]
    [SerializeField] private float m_finalWorkSpeedModifier = 0.0f;
    [SerializeField] private float m_finalCraftQualityModifier = 0.0f;

    public string PawnId
    {
        get { return m_pawnId; }
        set { m_pawnId = value; }
    }

    public string FirstName
    {
        get { return m_firstName; }
        set { m_firstName = value; }
    }

    public string LastName
    {
        get { return m_lastName; }
        set { m_lastName = value; }
    }

    public string DisplayName
    {
        get { return m_displayName; }
        set { m_displayName = value; }
    }

    public int Age
    {
        get { return m_age; }
        set { m_age = Mathf.Clamp(value, 18, 65); }
    }

    public PawnAgeBand AgeBand
    {
        get { return m_ageBand; }
        set { m_ageBand = value; }
    }

    public PawnGender Gender
    {
        get { return m_gender; }
        set { m_gender = value; }
    }

    public PawnBackgroundDefinition BackgroundDefinition
    {
        get { return m_backgroundDefinition; }
        set { m_backgroundDefinition = value; }
    }

    public string BackgroundId
    {
        get { return m_backgroundId; }
        set { m_backgroundId = value; }
    }

    public List<PawnTraitDefinition> TraitDefinitions
    {
        get { return m_traitDefinitions; }
    }

    public List<string> TraitIds
    {
        get { return m_traitIds; }
    }

    public PawnSkills Skills
    {
        get { return m_skills; }
        set { m_skills = value; }
    }

    public PawnWorkPriorities WorkPriorities
    {
        get
        {
            if (m_workPriorities == null)
            {
                m_workPriorities = new PawnWorkPriorities();
            }

            return m_workPriorities;
        }

        set
        {
            if (value == null)
            {
                m_workPriorities = new PawnWorkPriorities();
                return;
            }

            m_workPriorities = value;
        }
    }

    public PawnCondition Condition
    {
        get { return m_condition; }
        set { m_condition = value; }
    }

    public float BaseMoveSpeed
    {
        get { return m_baseMoveSpeed; }
        set { m_baseMoveSpeed = Mathf.Max(0.0f, value); }
    }

    public float FinalMoveSpeed
    {
        get { return m_finalMoveSpeed; }
        set { m_finalMoveSpeed = Mathf.Max(0.0f, value); }
    }

    public float FinalWorkSpeedModifier
    {
        get { return m_finalWorkSpeedModifier; }
        set { m_finalWorkSpeedModifier = value; }
    }

    public float FinalCraftQualityModifier
    {
        get { return m_finalCraftQualityModifier; }
        set { m_finalCraftQualityModifier = value; }
    }
}