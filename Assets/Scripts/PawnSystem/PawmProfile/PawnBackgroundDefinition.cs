using UnityEngine;

/// <summary>
/// Defines reusable authored background data for generated pawns.
/// Backgrounds represent prior occupation/history and only affect starting skills.
/// </summary>
[CreateAssetMenu(fileName = "PawnBackgroundDefinition", menuName = "ColonySim/Pawns/Background Definition")]
public class PawnBackgroundDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string m_backgroundId = "background_id";
    [SerializeField] private string m_displayName = "Background";
    [SerializeField] [TextArea] private string m_description = string.Empty;

    [Header("Generation")]
    [SerializeField] [Min(0.0f)] private float m_generationWeight = 1.0f;

    [Header("Skill Modifiers")]
    [SerializeField] private PawnSkillModifier m_skillModifiers = new PawnSkillModifier();

    public string BackgroundId
    {
        get { return m_backgroundId; }
    }

    public string DisplayName
    {
        get { return m_displayName; }
    }

    public string Description
    {
        get { return m_description; }
    }

    public float GenerationWeight
    {
        get { return m_generationWeight; }
    }

    public PawnSkillModifier SkillModifiers
    {
        get { return m_skillModifiers; }
    }
}