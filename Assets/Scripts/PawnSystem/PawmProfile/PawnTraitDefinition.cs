using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines reusable authored trait data for generated pawns.
/// Traits provide gameplay modifiers but do not contain pawn generation logic.
/// </summary>
[CreateAssetMenu(fileName = "PawnTraitDefinition", menuName = "ColonySim/Pawns/Trait Definition")]
public class PawnTraitDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string m_traitId = "trait_id";
    [SerializeField] private string m_displayName = "Trait";
    [SerializeField] [TextArea] private string m_description = string.Empty;

    [Header("Generation")]
    [SerializeField] private PawnTraitCategory m_traitCategory = PawnTraitCategory.Positive;
    [SerializeField] [Min(0.0f)] private float m_generationWeight = 1.0f;

    [Header("Skill Modifiers")]
    [SerializeField] private PawnSkillModifier m_skillModifiers = new PawnSkillModifier();

    [Header("Gameplay Modifiers")]
    [SerializeField] private float m_workSpeedModifier = 0.0f;
    [SerializeField] private float m_moveSpeedModifier = 0.0f;
    [SerializeField] private float m_craftQualityModifier = 0.0f;

    [Header("Conflicts")]
    [SerializeField] private List<PawnTraitDefinition> m_conflictingTraits = new List<PawnTraitDefinition>();

    public string TraitId
    {
        get { return m_traitId; }
    }

    public string DisplayName
    {
        get { return m_displayName; }
    }

    public string Description
    {
        get { return m_description; }
    }

    public PawnTraitCategory TraitCategory
    {
        get { return m_traitCategory; }
    }

    public float GenerationWeight
    {
        get { return m_generationWeight; }
    }

    public PawnSkillModifier SkillModifiers
    {
        get { return m_skillModifiers; }
    }

    public float WorkSpeedModifier
    {
        get { return m_workSpeedModifier; }
    }

    public float MoveSpeedModifier
    {
        get { return m_moveSpeedModifier; }
    }

    public float CraftQualityModifier
    {
        get { return m_craftQualityModifier; }
    }

    public IReadOnlyList<PawnTraitDefinition> ConflictingTraits
    {
        get { return m_conflictingTraits; }
    }
}