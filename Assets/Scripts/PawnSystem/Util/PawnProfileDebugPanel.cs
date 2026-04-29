using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Displays a simple debug inspection panel for the currently selected pawn.
/// This reads profile data from the runtime Pawn and does not edit pawn data.
/// </summary>
public class PawnProfileDebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PawnManager m_pawnManager;

    [Header("Debug Panel")]
    [SerializeField] private bool m_showPawnProfilePanel = true;
    [SerializeField] private bool m_showBrain = true;
    [SerializeField] private bool m_showIdentity = true;
    [SerializeField] private bool m_showBackgroundAndTraits = true;
    [SerializeField] private bool m_showSkills = true;
    [SerializeField] private bool m_showWorkPriorities = true;
    [SerializeField] private bool m_showCondition = true;
    [SerializeField] private bool m_showMovement = true;

    [Header("Panel Layout")]
    [SerializeField] private float m_panelX = 480.0f;
    [SerializeField] private float m_panelY = 10.0f;
    [SerializeField] private float m_panelWidth = 460.0f;
    [SerializeField] private float m_lineHeight = 20.0f;
    [SerializeField] private float m_padding = 10.0f;

    /// <summary>
    /// Finds required references if they were not assigned in the Inspector.
    /// </summary>
    private void Start()
    {
        if (m_pawnManager == null)
        {
            m_pawnManager = FindFirstObjectByType<PawnManager>();
        }

        if (m_pawnManager == null)
        {
            Debug.LogError("PawnProfileDebugPanel is missing a PawnManager reference.", this);
        }
    }

    /// <summary>
    /// Draws the selected pawn profile debug panel.
    /// </summary>
    private void OnGUI()
    {
        if (!m_showPawnProfilePanel)
        {
            return;
        }

        if (m_pawnManager == null)
        {
            return;
        }

        List<Pawn> selectedPawns = GetSelectedPawns();
        Pawn focusedPawn = selectedPawns.Count > 0 ? selectedPawns[0] : null;
        float panelHeight = CalculatePanelHeight(selectedPawns.Count, focusedPawn);

        GUI.Box(new Rect(m_panelX, m_panelY, m_panelWidth, panelHeight), "Selected Pawn Profile");

        float currentY = m_panelY + 25.0f;
        float contentX = m_panelX + m_padding;
        float contentWidth = m_panelWidth - (m_padding * 2.0f);

        DrawSelectionSummary(selectedPawns, focusedPawn, contentX, contentWidth, ref currentY);
        currentY += 5.0f;

        if (focusedPawn == null)
        {
            return;
        }

        if (m_showBrain)
        {
            DrawBrainSection(focusedPawn, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        PawnProfile profile = focusedPawn.Profile;

        if (profile == null)
        {
            DrawLine(contentX, ref currentY, contentWidth, "Profile: None");
            return;
        }

        if (m_showIdentity)
        {
            DrawIdentitySection(profile, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_showBackgroundAndTraits)
        {
            DrawBackgroundAndTraitsSection(profile, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_showSkills)
        {
            DrawSkillsSection(profile, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_showWorkPriorities)
        {
            DrawWorkPrioritiesSection(profile, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_showCondition)
        {
            DrawConditionSection(profile, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_showMovement)
        {
            DrawMovementSection(profile, contentX, contentWidth, ref currentY);
        }
    }

    /// <summary>
    /// Returns all currently selected pawns.
    /// The first selected pawn is treated as the focused debug pawn.
    /// </summary>
    private List<Pawn> GetSelectedPawns()
    {
        List<Pawn> selectedPawns = new List<Pawn>();

        IReadOnlyList<Pawn> activePawns = m_pawnManager.ActivePawns;

        for (int i = 0; i < activePawns.Count; i++)
        {
            Pawn pawn = activePawns[i];

            if (pawn == null)
            {
                continue;
            }

            if (pawn.IsSelected)
            {
                selectedPawns.Add(pawn);
            }
        }

        return selectedPawns;
    }

    /// <summary>
    /// Draws selected pawn count and focused pawn information.
    /// </summary>
    private void DrawSelectionSummary(
        List<Pawn> selectedPawns,
        Pawn focusedPawn,
        float x,
        float width,
        ref float y)
    {
        int selectedCount = selectedPawns != null ? selectedPawns.Count : 0;

        DrawLine(x, ref y, width, "Selected Pawns: " + selectedCount);

        if (selectedCount == 0 || focusedPawn == null)
        {
            DrawLine(x + 10.0f, ref y, width, "Focused Pawn: None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Focused Pawn: " + GetPawnDisplayName(focusedPawn));

        if (selectedCount > 1)
        {
            DrawLine(x + 10.0f, ref y, width, "Showing first selected pawn profile.");
        }
    }

    /// <summary>
    /// Draws autonomous brain information for the selected pawn.
    /// </summary>
    private void DrawBrainSection(Pawn pawn, float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Brain");

        if (pawn == null)
        {
            DrawLine(x + 10.0f, ref y, width, "Activity: None");
            return;
        }

        PawnBrain brain = pawn.GetComponent<PawnBrain>();

        if (brain == null)
        {
            DrawLine(x + 10.0f, ref y, width, "Activity: No PawnBrain");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Activity: " + brain.CurrentActivityLabel);
    }

    /// <summary>
    /// Draws identity information for the selected pawn profile.
    /// </summary>
    private void DrawIdentitySection(PawnProfile profile, float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Identity");
        DrawLine(x + 10.0f, ref y, width, "Name: " + profile.DisplayName);
        DrawLine(x + 10.0f, ref y, width, "PawnId: " + profile.PawnId);
        DrawLine(x + 10.0f, ref y, width, "Age: " + profile.Age + " (" + profile.AgeBand + ")");
        DrawLine(x + 10.0f, ref y, width, "Gender: " + profile.Gender);
    }

    /// <summary>
    /// Draws background and trait information for the selected pawn profile.
    /// </summary>
    private void DrawBackgroundAndTraitsSection(PawnProfile profile, float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Background / Traits");
        DrawLine(x + 10.0f, ref y, width, "Background: " + GetBackgroundDisplayName(profile));
        DrawLine(x + 10.0f, ref y, width, "Traits: " + BuildTraitDisplayString(profile));
    }

    /// <summary>
    /// Draws skill information for the selected pawn profile.
    /// </summary>
    private void DrawSkillsSection(PawnProfile profile, float x, float width, ref float y)
    {
        PawnSkills skills = profile.Skills;

        DrawLine(x, ref y, width, "Skills");

        if (skills == null)
        {
            DrawLine(x + 10.0f, ref y, width, "None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Plant: " + skills.Plant);
        DrawLine(x + 10.0f, ref y, width, "Cut: " + skills.Cut);
        DrawLine(x + 10.0f, ref y, width, "Construct: " + skills.Construct);
        DrawLine(x + 10.0f, ref y, width, "Cook: " + skills.Cook);
        DrawLine(x + 10.0f, ref y, width, "Craft: " + skills.Craft);
    }

    /// <summary>
    /// Draws work priority information for the selected pawn profile.
    /// Lower values are higher priority.
    /// </summary>
    private void DrawWorkPrioritiesSection(PawnProfile profile, float x, float width, ref float y)
    {
        PawnWorkPriorities workPriorities = profile.WorkPriorities;

        DrawLine(x, ref y, width, "Work Priorities");

        if (workPriorities == null)
        {
            DrawLine(x + 10.0f, ref y, width, "None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Plant: " + workPriorities.PlantPriority);
        DrawLine(x + 10.0f, ref y, width, "Cut: " + workPriorities.CutPriority);
        DrawLine(x + 10.0f, ref y, width, "Construct: " + workPriorities.ConstructPriority);
        DrawLine(x + 10.0f, ref y, width, "Cook: " + workPriorities.CookPriority);
        DrawLine(x + 10.0f, ref y, width, "Craft: " + workPriorities.CraftPriority);
    }

    /// <summary>
    /// Draws needs and condition information for the selected pawn profile.
    /// </summary>
    private void DrawConditionSection(PawnProfile profile, float x, float width, ref float y)
    {
        PawnCondition condition = profile.Condition;

        DrawLine(x, ref y, width, "Needs / Condition");

        if (condition == null)
        {
            DrawLine(x + 10.0f, ref y, width, "None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Mobility: " + condition.Mobility);
        DrawLine(x + 10.0f, ref y, width, "Food: " + condition.Food + " (" + condition.FoodState + ")");
        DrawLine(x + 10.0f, ref y, width, "Sleep: " + condition.Sleep + " (" + condition.SleepState + ")");
        DrawLine(x + 10.0f, ref y, width, "Temperature: " + condition.Temperature);
        DrawLine(x + 10.0f, ref y, width, "Health: " + condition.Health);
    }

    /// <summary>
    /// Draws movement and derived modifier information for the selected pawn profile.
    /// </summary>
    private void DrawMovementSection(PawnProfile profile, float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Movement / Derived Modifiers");
        DrawLine(x + 10.0f, ref y, width, "Base Move Speed: " + profile.BaseMoveSpeed.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Final Move Speed: " + profile.FinalMoveSpeed.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Work Speed Modifier: " + profile.FinalWorkSpeedModifier.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Craft Quality Modifier: " + profile.FinalCraftQualityModifier.ToString("F2"));
    }

    /// <summary>
    /// Draws one text line in the debug panel.
    /// </summary>
    private void DrawLine(float x, ref float y, float width, string text)
    {
        GUI.Label(new Rect(x, y, width, m_lineHeight), text);
        y += m_lineHeight;
    }

    /// <summary>
    /// Calculates the panel height based on enabled sections and selection state.
    /// </summary>
    private float CalculatePanelHeight(int selectedPawnCount, Pawn focusedPawn)
    {
        float totalHeight = 35.0f;

        totalHeight += m_lineHeight;

        if (focusedPawn == null)
        {
            totalHeight += m_lineHeight;
            return totalHeight;
        }

        totalHeight += m_lineHeight;

        if (selectedPawnCount > 1)
        {
            totalHeight += m_lineHeight;
        }

        totalHeight += 5.0f;

        if (m_showBrain)
        {
            totalHeight += m_lineHeight * 2.0f;
            totalHeight += 5.0f;
        }

        if (focusedPawn.Profile == null)
        {
            totalHeight += m_lineHeight;
            return totalHeight;
        }

        if (m_showIdentity)
        {
            totalHeight += m_lineHeight * 5.0f;
            totalHeight += 5.0f;
        }

        if (m_showBackgroundAndTraits)
        {
            totalHeight += m_lineHeight * 3.0f;
            totalHeight += 5.0f;
        }

        if (m_showSkills)
        {
            totalHeight += m_lineHeight * 6.0f;
            totalHeight += 5.0f;
        }

        if (m_showWorkPriorities)
        {
            totalHeight += m_lineHeight * 6.0f;
            totalHeight += 5.0f;
        }

        if (m_showCondition)
        {
            totalHeight += m_lineHeight * 6.0f;
            totalHeight += 5.0f;
        }

        if (m_showMovement)
        {
            totalHeight += m_lineHeight * 5.0f;
        }

        return totalHeight;
    }

    /// <summary>
    /// Returns the best display name for a runtime pawn.
    /// </summary>
    private string GetPawnDisplayName(Pawn pawn)
    {
        if (pawn == null)
        {
            return "None";
        }

        if (pawn.Profile != null && !string.IsNullOrWhiteSpace(pawn.Profile.DisplayName))
        {
            return pawn.Profile.DisplayName;
        }

        return pawn.PawnId;
    }

    /// <summary>
    /// Returns the selected pawn's background display name.
    /// </summary>
    private string GetBackgroundDisplayName(PawnProfile profile)
    {
        if (profile == null || profile.BackgroundDefinition == null)
        {
            return "None";
        }

        return profile.BackgroundDefinition.DisplayName;
    }

    /// <summary>
    /// Builds a readable comma-separated trait list for the selected pawn profile.
    /// </summary>
    private string BuildTraitDisplayString(PawnProfile profile)
    {
        if (profile == null || profile.TraitDefinitions.Count == 0)
        {
            return "None";
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < profile.TraitDefinitions.Count; i++)
        {
            PawnTraitDefinition traitDefinition = profile.TraitDefinitions[i];

            if (traitDefinition == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(traitDefinition.DisplayName);
        }

        if (builder.Length == 0)
        {
            return "None";
        }

        return builder.ToString();
    }
}