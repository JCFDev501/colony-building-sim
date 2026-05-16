using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns pawn profile generation data and settings for the prototype.
/// This manager creates and stores pawn profiles only. It does not spawn GameObjects,
/// control world generation, or manage runtime pawn behavior.
/// </summary>
public class PawnGenerationManager : MonoBehaviour
{
    [Header("Name Generation")]
    [SerializeField] private PawnNameGenerator m_nameGenerator = new PawnNameGenerator();

    [Header("Definition Pools")]
    [SerializeField] private List<PawnBackgroundDefinition> m_availableBackgroundDefinitions = new List<PawnBackgroundDefinition>();
    [SerializeField] private List<PawnTraitDefinition> m_availableTraitDefinitions = new List<PawnTraitDefinition>();

    [Header("Generation Counts")]
    [SerializeField] private int m_debugStarterPawnCount = 4;
    [SerializeField] private int m_generatedCandidateCount = 8;

    [Header("Movement Settings")]
    [SerializeField] private float m_baseMoveSpeedMin = 2.75f;
    [SerializeField] private float m_baseMoveSpeedMax = 3.25f;

    [Header("Base Skill Settings")]
    [SerializeField] [Range(0, 10)] private int m_baseSkillMin = 0;
    [SerializeField] [Range(0, 10)] private int m_baseSkillMax = 4;

    [Header("Standout Skill Settings")]
    [SerializeField] [Range(0.0f, 1.0f)] private float m_standoutSkillChance = 0.35f;
    [SerializeField] [Range(0, 10)] private int m_standoutSkillMin = 4;
    [SerializeField] [Range(0, 10)] private int m_standoutSkillMax = 6;

    [Header("Trait Selection Settings")]
    [SerializeField] [Range(0.0f, 1.0f)] private float m_negativeOrMixedTraitChance = 0.45f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_additionalTraitChance = 0.35f;

    [Header("Generated Profile Storage")]
    [SerializeField] private List<PawnProfile> m_generatedCandidateProfiles = new List<PawnProfile>();
    [SerializeField] private List<PawnProfile> m_selectedStarterProfiles = new List<PawnProfile>();

    [Header("Debug")]
    [SerializeField] private bool m_logGeneratedPawnProfiles = true;
    [SerializeField] private bool m_logSelectedBackground = false;
    [SerializeField] private bool m_logSelectedTraits = false;
    [SerializeField] private bool m_logRejectedTraitConflicts = false;

    private readonly System.Random m_random = new System.Random();

    /// <summary>
    /// Gets the currently available background definition pool.
    /// </summary>
    public IReadOnlyList<PawnBackgroundDefinition> AvailableBackgroundDefinitions
    {
        get { return m_availableBackgroundDefinitions; }
    }

    /// <summary>
    /// Gets the currently available trait definition pool.
    /// </summary>
    public IReadOnlyList<PawnTraitDefinition> AvailableTraitDefinitions
    {
        get { return m_availableTraitDefinitions; }
    }

    /// <summary>
    /// Gets the generated candidate pawn profiles.
    /// These are profiles the player may later choose from.
    /// </summary>
    public IReadOnlyList<PawnProfile> GeneratedCandidateProfiles
    {
        get { return m_generatedCandidateProfiles; }
    }

    /// <summary>
    /// Gets the selected starter pawn profiles.
    /// These are the profiles that should later be spawned as starter colonists.
    /// </summary>
    public IReadOnlyList<PawnProfile> SelectedStarterProfiles
    {
        get { return m_selectedStarterProfiles; }
    }

    /// <summary>
    /// Clears the generated candidate profile storage.
    /// </summary>
    public void ClearGeneratedCandidateProfiles()
    {
        m_generatedCandidateProfiles.Clear();
    }

    /// <summary>
    /// Clears the selected starter profile storage.
    /// </summary>
    public void ClearSelectedStarterProfiles()
    {
        m_selectedStarterProfiles.Clear();
    }

    /// <summary>
    /// Adds a generated candidate profile to storage if it is valid and not already stored.
    /// </summary>
    public void AddGeneratedCandidateProfile(PawnProfile pawnProfile)
    {
        if (pawnProfile == null)
        {
            return;
        }

        if (m_generatedCandidateProfiles.Contains(pawnProfile))
        {
            return;
        }

        m_generatedCandidateProfiles.Add(pawnProfile);
    }

    /// <summary>
    /// Adds a selected starter profile to storage if it is valid and not already stored.
    /// </summary>
    public void AddSelectedStarterProfile(PawnProfile pawnProfile)
    {
        if (pawnProfile == null)
        {
            return;
        }

        if (m_selectedStarterProfiles.Contains(pawnProfile))
        {
            return;
        }

        m_selectedStarterProfiles.Add(pawnProfile);
    }

    /// <summary>
    /// Generates one complete pawn profile with identity, skills, work priorities,
    /// background, traits, condition values, and derived movement data.
    /// </summary>
    public PawnProfile GeneratePawnProfile()
    {
        PawnProfile pawnProfile = new PawnProfile();

        pawnProfile.PawnId = Guid.NewGuid().ToString();

        PawnGender gender = GenerateGender();
        pawnProfile.Gender = gender;

        pawnProfile.FirstName = m_nameGenerator.GenerateFirstName(gender, m_random);
        pawnProfile.LastName = m_nameGenerator.GenerateLastName(m_random);
        pawnProfile.DisplayName = pawnProfile.FirstName + " " + pawnProfile.LastName;

        pawnProfile.Age = m_random.Next(18, 66);
        pawnProfile.AgeBand = ResolveAgeBand(pawnProfile.Age);

        pawnProfile.Skills = GenerateBaseSkills();
        pawnProfile.WorkPriorities = new PawnWorkPriorities();

        ApplyStandoutSkill(pawnProfile.Skills);

        PawnBackgroundDefinition backgroundDefinition = SelectBackgroundDefinition();
        ApplyBackgroundDefinition(pawnProfile, backgroundDefinition);

        List<PawnTraitDefinition> traitDefinitions = SelectTraitDefinitions();
        ApplyTraitDefinitions(pawnProfile, traitDefinitions);

        pawnProfile.Condition = GenerateCondition();

        pawnProfile.BaseMoveSpeed = GenerateBaseMoveSpeed();
        pawnProfile.FinalMoveSpeed = CalculateFinalMoveSpeed(pawnProfile);
        pawnProfile.FinalWorkSpeedModifier = CalculateFinalWorkSpeedModifier(pawnProfile);
        pawnProfile.FinalCraftQualityModifier = CalculateFinalCraftQualityModifier(pawnProfile);

        return pawnProfile;
    }

    /// <summary>
    /// Generates debug starter pawn profiles and stores them as selected starter profiles.
    /// This is a debug helper only and does not spawn runtime pawn GameObjects.
    /// </summary>
    [ContextMenu("Generate Debug Starter Pawn Profiles")]
    public void GenerateDebugStarterPawnProfiles()
    {
        ClearSelectedStarterProfiles();

        int safeStarterCount = Mathf.Max(0, m_debugStarterPawnCount);

        for (int i = 0; i < safeStarterCount; i++)
        {
            PawnProfile pawnProfile = GeneratePawnProfile();
            AddSelectedStarterProfile(pawnProfile);

            if (m_logGeneratedPawnProfiles)
            {
                LogGeneratedPawnProfile(pawnProfile);
            }
        }

        Debug.Log("Generated debug starter pawn profiles: " + m_selectedStarterProfiles.Count, this);
    }

    /// <summary>
    /// Generates debug candidate pawn profiles and stores them as generated candidates.
    /// This is a debug helper only and does not spawn runtime pawn GameObjects.
    /// </summary>
    [ContextMenu("Generate Debug Candidate Pawn Profiles")]
    public void GenerateDebugCandidatePawnProfiles()
    {
        ClearGeneratedCandidateProfiles();

        int safeCandidateCount = Mathf.Max(0, m_generatedCandidateCount);

        for (int i = 0; i < safeCandidateCount; i++)
        {
            PawnProfile pawnProfile = GeneratePawnProfile();
            AddGeneratedCandidateProfile(pawnProfile);

            if (m_logGeneratedPawnProfiles)
            {
                LogGeneratedPawnProfile(pawnProfile);
            }
        }

        Debug.Log("Generated debug candidate pawn profiles: " + m_generatedCandidateProfiles.Count, this);
    }

    /// <summary>
    /// Randomly selects one supported pawn gender.
    /// Gender is profile/display data only in V1.
    /// </summary>
    private PawnGender GenerateGender()
    {
        int genderIndex = m_random.Next(0, 3);

        switch (genderIndex)
        {
            case 0:
                return PawnGender.Male;

            case 1:
                return PawnGender.Female;

            default:
                return PawnGender.Nonbinary;
        }
    }

    /// <summary>
    /// Resolves the pawn age band from the generated age value.
    /// Age band is flavor/display data only in V1.
    /// </summary>
    private PawnAgeBand ResolveAgeBand(int age)
    {
        if (age <= 24)
        {
            return PawnAgeBand.YoungAdult;
        }

        if (age <= 39)
        {
            return PawnAgeBand.Adult;
        }

        if (age <= 54)
        {
            return PawnAgeBand.Mature;
        }

        return PawnAgeBand.Elder;
    }

    /// <summary>
    /// Generates uneven base skill values within the configured base skill range.
    /// </summary>
    private PawnSkills GenerateBaseSkills()
    {
        int skillMin = Mathf.Clamp(m_baseSkillMin, 0, 10);
        int skillMax = Mathf.Clamp(m_baseSkillMax, skillMin, 10);

        PawnSkills skills = new PawnSkills
        {
            Plant = m_random.Next(skillMin, skillMax + 1),
            Cut = m_random.Next(skillMin, skillMax + 1),
            Construct = m_random.Next(skillMin, skillMax + 1),
            Cook = m_random.Next(skillMin, skillMax + 1),
            Craft = m_random.Next(skillMin, skillMax + 1),
            Mine = m_random.Next(skillMin, skillMax + 1),
        };

        return skills;
    }

    /// <summary>
    /// Gives the pawn a chance to receive one stronger starting skill.
    /// This does not guarantee a standout skill for every pawn.
    /// </summary>
    private void ApplyStandoutSkill(PawnSkills skills)
    {
        if (skills == null)
        {
            return;
        }

        if (NextFloat01() > m_standoutSkillChance)
        {
            return;
        }

        int standoutMin = Mathf.Clamp(m_standoutSkillMin, 0, 10);
        int standoutMax = Mathf.Clamp(m_standoutSkillMax, standoutMin, 10);
        int standoutValue = m_random.Next(standoutMin, standoutMax + 1);
        int skillIndex = m_random.Next(0, 6);

        switch (skillIndex)
        {
            case 0:
                skills.Plant = Mathf.Max(skills.Plant, standoutValue);
                break;

            case 1:
                skills.Cut = Mathf.Max(skills.Cut, standoutValue);
                break;

            case 2:
                skills.Construct = Mathf.Max(skills.Construct, standoutValue);
                break;

            case 3:
                skills.Cook = Mathf.Max(skills.Cook, standoutValue);
                break;

            case 4:
                skills.Craft = Mathf.Max(skills.Craft, standoutValue);
                break;

            default:
                skills.Mine = Mathf.Max(skills.Mine, standoutValue);
                break;
        }
    }

    /// <summary>
    /// Selects one pawn background definition from the available weighted background pool.
    /// Returns null if no valid background definitions are assigned.
    /// </summary>
    private PawnBackgroundDefinition SelectBackgroundDefinition()
    {
        float totalWeight = 0.0f;

        for (int i = 0; i < m_availableBackgroundDefinitions.Count; i++)
        {
            PawnBackgroundDefinition backgroundDefinition = m_availableBackgroundDefinitions[i];

            if (backgroundDefinition == null)
            {
                continue;
            }

            totalWeight += Mathf.Max(0.0f, backgroundDefinition.GenerationWeight);
        }

        if (totalWeight <= 0.0f)
        {
            return null;
        }

        float roll = NextFloat(0.0f, totalWeight);
        float runningWeight = 0.0f;

        for (int i = 0; i < m_availableBackgroundDefinitions.Count; i++)
        {
            PawnBackgroundDefinition backgroundDefinition = m_availableBackgroundDefinitions[i];

            if (backgroundDefinition == null)
            {
                continue;
            }

            runningWeight += Mathf.Max(0.0f, backgroundDefinition.GenerationWeight);

            if (roll <= runningWeight)
            {
                return backgroundDefinition;
            }
        }

        return null;
    }

    /// <summary>
    /// Applies the selected background to the pawn profile and adds its starting skill modifiers.
    /// Backgrounds only affect starting skills in V1.
    /// </summary>
    private void ApplyBackgroundDefinition(PawnProfile pawnProfile, PawnBackgroundDefinition backgroundDefinition)
    {
        if (pawnProfile == null || backgroundDefinition == null)
        {
            return;
        }

        pawnProfile.BackgroundDefinition = backgroundDefinition;
        pawnProfile.BackgroundId = backgroundDefinition.BackgroundId;

        ApplySkillModifier(pawnProfile.Skills, backgroundDefinition.SkillModifiers);

        if (m_logSelectedBackground)
        {
            Debug.Log(
                "Selected Background: " + backgroundDefinition.DisplayName +
                " | BackgroundId: " + backgroundDefinition.BackgroundId +
                " | Skill Modifiers: Plant " + backgroundDefinition.SkillModifiers.Plant +
                " | Cut " + backgroundDefinition.SkillModifiers.Cut +
                " | Construct " + backgroundDefinition.SkillModifiers.Construct +
                " | Cook " + backgroundDefinition.SkillModifiers.Cook +
                " | Craft " + backgroundDefinition.SkillModifiers.Craft +
                " | Mine " + backgroundDefinition.SkillModifiers.Mine,
                this);
        }
    }

    /// <summary>
    /// Selects 1-3 compatible pawn traits.
    /// Every pawn receives at least one positive trait when available.
    /// Negative or mixed traits are possible but not guaranteed.
    /// </summary>
    private List<PawnTraitDefinition> SelectTraitDefinitions()
    {
        List<PawnTraitDefinition> selectedTraits = new List<PawnTraitDefinition>();

        PawnTraitDefinition positiveTrait = SelectWeightedTraitDefinitionByCategory(PawnTraitCategory.Positive, selectedTraits);

        if (positiveTrait != null)
        {
            AddSelectedTrait(selectedTraits, positiveTrait);
        }

        if (selectedTraits.Count < 3 && NextFloat01() <= m_negativeOrMixedTraitChance)
        {
            PawnTraitDefinition negativeOrMixedTrait = SelectWeightedNegativeOrMixedTraitDefinition(selectedTraits);

            if (negativeOrMixedTrait != null)
            {
                AddSelectedTrait(selectedTraits, negativeOrMixedTrait);
            }
        }

        if (selectedTraits.Count < 3 && NextFloat01() <= m_additionalTraitChance)
        {
            PawnTraitDefinition additionalTrait = SelectWeightedCompatibleTraitDefinition(selectedTraits);

            if (additionalTrait != null)
            {
                AddSelectedTrait(selectedTraits, additionalTrait);
            }
        }

        if (selectedTraits.Count == 0)
        {
            PawnTraitDefinition fallbackTrait = SelectWeightedCompatibleTraitDefinition(selectedTraits);

            if (fallbackTrait != null)
            {
                AddSelectedTrait(selectedTraits, fallbackTrait);
            }
        }

        return selectedTraits;
    }

    /// <summary>
    /// Selects one weighted trait from a specific category while respecting already selected traits.
    /// </summary>
    private PawnTraitDefinition SelectWeightedTraitDefinitionByCategory(
        PawnTraitCategory traitCategory,
        List<PawnTraitDefinition> selectedTraits)
    {
        List<PawnTraitDefinition> candidates = new List<PawnTraitDefinition>();

        for (int i = 0; i < m_availableTraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = m_availableTraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            if (traitDefinition.TraitCategory != traitCategory)
            {
                continue;
            }

            if (!CanAddTraitDefinition(selectedTraits, traitDefinition))
            {
                continue;
            }

            candidates.Add(traitDefinition);
        }

        return SelectWeightedTraitFromCandidates(candidates);
    }

    /// <summary>
    /// Selects one weighted negative or mixed trait while respecting already selected traits.
    /// </summary>
    private PawnTraitDefinition SelectWeightedNegativeOrMixedTraitDefinition(List<PawnTraitDefinition> selectedTraits)
    {
        List<PawnTraitDefinition> candidates = new List<PawnTraitDefinition>();

        for (int i = 0; i < m_availableTraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = m_availableTraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            bool isNegativeOrMixed =
                traitDefinition.TraitCategory == PawnTraitCategory.Negative ||
                traitDefinition.TraitCategory == PawnTraitCategory.Mixed;

            if (!isNegativeOrMixed)
            {
                continue;
            }

            if (!CanAddTraitDefinition(selectedTraits, traitDefinition))
            {
                continue;
            }

            candidates.Add(traitDefinition);
        }

        return SelectWeightedTraitFromCandidates(candidates);
    }

    /// <summary>
    /// Selects one weighted trait from any category while respecting already selected traits.
    /// </summary>
    private PawnTraitDefinition SelectWeightedCompatibleTraitDefinition(List<PawnTraitDefinition> selectedTraits)
    {
        List<PawnTraitDefinition> candidates = new List<PawnTraitDefinition>();

        for (int i = 0; i < m_availableTraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = m_availableTraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            if (!CanAddTraitDefinition(selectedTraits, traitDefinition))
            {
                continue;
            }

            candidates.Add(traitDefinition);
        }

        return SelectWeightedTraitFromCandidates(candidates);
    }

    /// <summary>
    /// Selects one weighted trait from a pre-filtered candidate list.
    /// </summary>
    private PawnTraitDefinition SelectWeightedTraitFromCandidates(List<PawnTraitDefinition> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        float totalWeight = 0.0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            PawnTraitDefinition traitDefinition = candidates[i];

            if (traitDefinition == null)
            {
                continue;
            }

            totalWeight += Mathf.Max(0.0f, traitDefinition.GenerationWeight);
        }

        if (totalWeight <= 0.0f)
        {
            return null;
        }

        float roll = NextFloat(0.0f, totalWeight);
        float runningWeight = 0.0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            PawnTraitDefinition traitDefinition = candidates[i];

            if (traitDefinition == null)
            {
                continue;
            }

            runningWeight += Mathf.Max(0.0f, traitDefinition.GenerationWeight);

            if (roll <= runningWeight)
            {
                return traitDefinition;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds a selected trait to the temporary selected-traits list and logs the choice when enabled.
    /// </summary>
    private void AddSelectedTrait(List<PawnTraitDefinition> selectedTraits, PawnTraitDefinition traitDefinition)
    {
        if (selectedTraits == null || traitDefinition == null)
        {
            return;
        }

        if (!CanAddTraitDefinition(selectedTraits, traitDefinition))
        {
            return;
        }

        selectedTraits.Add(traitDefinition);

        if (m_logSelectedTraits)
        {
            Debug.Log(
                "Selected Trait: " + traitDefinition.DisplayName +
                " | TraitId: " + traitDefinition.TraitId +
                " | Category: " + traitDefinition.TraitCategory,
                this);
        }
    }

    /// <summary>
    /// Returns whether the candidate trait can be added without duplicating or conflicting
    /// with traits already selected for the pawn.
    /// </summary>
    private bool CanAddTraitDefinition(List<PawnTraitDefinition> selectedTraits, PawnTraitDefinition candidateTrait)
    {
        if (candidateTrait == null)
        {
            return false;
        }

        if (selectedTraits == null)
        {
            return true;
        }

        if (selectedTraits.Contains(candidateTrait))
        {
            return false;
        }

        for (int i = 0; i < selectedTraits.Count; i++)
        {
            PawnTraitDefinition selectedTrait = selectedTraits[i];

            if (selectedTrait == null)
            {
                continue;
            }

            if (DoesTraitConflict(selectedTrait, candidateTrait))
            {
                LogRejectedTraitConflict(candidateTrait, selectedTrait);
                return false;
            }

            if (DoesTraitConflict(candidateTrait, selectedTrait))
            {
                LogRejectedTraitConflict(candidateTrait, selectedTrait);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns whether the source trait explicitly lists the target trait as a conflict.
    /// </summary>
    private bool DoesTraitConflict(PawnTraitDefinition sourceTrait, PawnTraitDefinition targetTrait)
    {
        if (sourceTrait == null || targetTrait == null)
        {
            return false;
        }

        IReadOnlyList<PawnTraitDefinition> conflictingTraits = sourceTrait.ConflictingTraits;

        for (int i = 0; i < conflictingTraits.Count; i++)
        {
            if (conflictingTraits[i] == targetTrait)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Logs a rejected trait conflict when conflict logging is enabled.
    /// </summary>
    private void LogRejectedTraitConflict(PawnTraitDefinition rejectedTrait, PawnTraitDefinition existingTrait)
    {
        if (!m_logRejectedTraitConflicts)
        {
            return;
        }

        string rejectedName = rejectedTrait != null ? rejectedTrait.DisplayName : "Unknown";
        string existingName = existingTrait != null ? existingTrait.DisplayName : "Unknown";

        Debug.Log(
            "Rejected Trait Conflict: " + rejectedName +
            " conflicts with already selected trait " + existingName,
            this);
    }

    /// <summary>
    /// Applies selected traits to the pawn profile and adds their starting skill modifiers.
    /// Trait movement/work/craft modifiers are applied later during derived-stat calculation.
    /// </summary>
    private void ApplyTraitDefinitions(PawnProfile pawnProfile, List<PawnTraitDefinition> traitDefinitions)
    {
        if (pawnProfile == null || traitDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < traitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = traitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            pawnProfile.TraitDefinitions.Add(traitDefinition);
            pawnProfile.TraitIds.Add(traitDefinition.TraitId);

            ApplySkillModifier(pawnProfile.Skills, traitDefinition.SkillModifiers);
        }
    }

    /// <summary>
    /// Generates initial pawn condition and need values.
    /// These values are display-only in V1 and do not tick down yet.
    /// </summary>
    private PawnCondition GenerateCondition()
    {
        PawnCondition condition = new PawnCondition
        {
            Mobility = GenerateMobilityState(),
            Food = m_random.Next(70, 101),
            Sleep = m_random.Next(70, 101),
            Temperature = PawnTemperatureState.Comfortable,
            Health = PawnHealthState.Stable,
        };

        return condition;
    }

    /// <summary>
    /// Generates the pawn's starting mobility state.
    /// Most pawns start normal, with a small chance to start slower or impaired.
    /// </summary>
    private PawnMobilityState GenerateMobilityState()
    {
        float roll = NextFloat01();

        if (roll < 0.80f)
        {
            return PawnMobilityState.Normal;
        }

        if (roll < 0.95f)
        {
            return PawnMobilityState.Slow;
        }

        return PawnMobilityState.Impaired;
    }

    /// <summary>
    /// Generates the pawn's natural base movement speed from the configured range.
    /// </summary>
    private float GenerateBaseMoveSpeed()
    {
        float minMoveSpeed = Mathf.Max(0.0f, m_baseMoveSpeedMin);
        float maxMoveSpeed = Mathf.Max(minMoveSpeed, m_baseMoveSpeedMax);

        return NextFloat(minMoveSpeed, maxMoveSpeed);
    }

    /// <summary>
    /// Calculates final movement speed from base movement, trait modifiers, and mobility condition.
    /// </summary>
    private float CalculateFinalMoveSpeed(PawnProfile pawnProfile)
    {
        if (pawnProfile == null)
        {
            return 0.0f;
        }

        float finalMoveSpeed = pawnProfile.BaseMoveSpeed;

        finalMoveSpeed += GetTraitMoveSpeedModifierTotal(pawnProfile);
        finalMoveSpeed += GetMobilityMoveSpeedModifier(pawnProfile.Condition);

        return Mathf.Max(0.1f, finalMoveSpeed);
    }

    /// <summary>
    /// Returns the total movement speed modifier from all selected traits.
    /// </summary>
    private float GetTraitMoveSpeedModifierTotal(PawnProfile pawnProfile)
    {
        if (pawnProfile == null)
        {
            return 0.0f;
        }

        float modifierTotal = 0.0f;

        for (int i = 0; i < pawnProfile.TraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = pawnProfile.TraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            modifierTotal += traitDefinition.MoveSpeedModifier;
        }

        return modifierTotal;
    }

    /// <summary>
    /// Calculates the final work speed modifier from selected traits.
    /// </summary>
    private float CalculateFinalWorkSpeedModifier(PawnProfile pawnProfile)
    {
        if (pawnProfile == null)
        {
            return 0.0f;
        }

        float modifierTotal = 0.0f;

        for (int i = 0; i < pawnProfile.TraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = pawnProfile.TraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            modifierTotal += traitDefinition.WorkSpeedModifier;
        }

        return modifierTotal;
    }

    /// <summary>
    /// Calculates the final craft quality modifier from selected traits.
    /// </summary>
    private float CalculateFinalCraftQualityModifier(PawnProfile pawnProfile)
    {
        if (pawnProfile == null)
        {
            return 0.0f;
        }

        float modifierTotal = 0.0f;

        for (int i = 0; i < pawnProfile.TraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = pawnProfile.TraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            modifierTotal += traitDefinition.CraftQualityModifier;
        }

        return modifierTotal;
    }

    /// <summary>
    /// Returns the movement speed modifier caused by the pawn's mobility condition.
    /// </summary>
    private float GetMobilityMoveSpeedModifier(PawnCondition condition)
    {
        if (condition == null)
        {
            return 0.0f;
        }

        switch (condition.Mobility)
        {
            case PawnMobilityState.Slow:
                return -0.25f;

            case PawnMobilityState.Impaired:
                return -0.60f;

            default:
                return 0.0f;
        }
    }

    /// <summary>
    /// Logs one generated pawn profile in a readable debug format.
    /// </summary>
    private void LogGeneratedPawnProfile(PawnProfile pawnProfile)
    {
        if (pawnProfile == null)
        {
            return;
        }

        Debug.Log(
            "Generated Pawn Profile" +
            "\nName: " + pawnProfile.DisplayName +
            "\nPawnId: " + pawnProfile.PawnId +
            "\nGender: " + pawnProfile.Gender +
            "\nAge: " + pawnProfile.Age + " (" + pawnProfile.AgeBand + ")" +
            "\nBackground: " + GetBackgroundDebugName(pawnProfile) +
            "\nTraits: " + GetTraitDebugNames(pawnProfile) +
            "\nSkills: Plant " + pawnProfile.Skills.Plant +
            " | Cut " + pawnProfile.Skills.Cut +
            " | Construct " + pawnProfile.Skills.Construct +
            " | Cook " + pawnProfile.Skills.Cook +
            " | Craft " + pawnProfile.Skills.Craft +
            " | Mine " + pawnProfile.Skills.Mine +
            "\nWork Priorities: " + GetWorkPrioritiesDebugText(pawnProfile) +
            "\nCondition: Mobility " + pawnProfile.Condition.Mobility +
            " | Food " + pawnProfile.Condition.Food + " (" + pawnProfile.Condition.FoodState + ")" +
            " | Sleep " + pawnProfile.Condition.Sleep + " (" + pawnProfile.Condition.SleepState + ")" +
            " | Temperature " + pawnProfile.Condition.Temperature +
            " | Health " + pawnProfile.Condition.Health +
            "\nMove Speed: Base " + pawnProfile.BaseMoveSpeed.ToString("F2") +
            " | Final " + pawnProfile.FinalMoveSpeed.ToString("F2") +
            "\nDerived Modifiers: Work Speed " + pawnProfile.FinalWorkSpeedModifier.ToString("F2") +
            " | Craft Quality " + pawnProfile.FinalCraftQualityModifier.ToString("F2"),
            this);
    }

    /// <summary>
    /// Returns a readable background name for debug output.
    /// </summary>
    private string GetBackgroundDebugName(PawnProfile pawnProfile)
    {
        if (pawnProfile == null || pawnProfile.BackgroundDefinition == null)
        {
            return "None";
        }

        return pawnProfile.BackgroundDefinition.DisplayName;
    }

    /// <summary>
    /// Returns readable trait names for debug output.
    /// </summary>
    private string GetTraitDebugNames(PawnProfile pawnProfile)
    {
        if (pawnProfile == null || pawnProfile.TraitDefinitions.Count == 0)
        {
            return "None";
        }

        List<string> traitNames = new List<string>();

        for (int i = 0; i < pawnProfile.TraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = pawnProfile.TraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            traitNames.Add(traitDefinition.DisplayName);
        }

        if (traitNames.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", traitNames);
    }

    /// <summary>
    /// Returns readable work priority values for debug output.
    /// </summary>
    private string GetWorkPrioritiesDebugText(PawnProfile pawnProfile)
    {
        if (pawnProfile == null || pawnProfile.WorkPriorities == null)
        {
            return "None";
        }

        PawnWorkPriorities workPriorities = pawnProfile.WorkPriorities;

        return "Plant " + workPriorities.PlantPriority +
               " | Cut " + workPriorities.CutPriority +
               " | Construct " + workPriorities.ConstructPriority +
               " | Cook " + workPriorities.CookPriority +
               " | Craft " + workPriorities.CraftPriority +
               " | Mine " + workPriorities.MinePriority;
    }

    /// <summary>
    /// Applies a skill modifier to existing pawn skills and keeps skill values in the valid 0-10 range.
    /// </summary>
    private void ApplySkillModifier(PawnSkills skills, PawnSkillModifier skillModifier)
    {
        if (skills == null || skillModifier == null)
        {
            return;
        }

        skills.Plant = Mathf.Clamp(skills.Plant + skillModifier.Plant, 0, 10);
        skills.Cut = Mathf.Clamp(skills.Cut + skillModifier.Cut, 0, 10);
        skills.Construct = Mathf.Clamp(skills.Construct + skillModifier.Construct, 0, 10);
        skills.Cook = Mathf.Clamp(skills.Cook + skillModifier.Cook, 0, 10);
        skills.Craft = Mathf.Clamp(skills.Craft + skillModifier.Craft, 0, 10);
        skills.Mine = Mathf.Clamp(skills.Mine + skillModifier.Mine, 0, 10);
    }

    /// <summary>
    /// Returns a random float in the range [0, 1).
    /// </summary>
    private float NextFloat01()
    {
        return (float)m_random.NextDouble();
    }

    /// <summary>
    /// Returns a random float in the range [minInclusive, maxInclusive).
    /// </summary>
    private float NextFloat(float minInclusive, float maxInclusive)
    {
        return minInclusive + ((float)m_random.NextDouble() * (maxInclusive - minInclusive));
    }
}