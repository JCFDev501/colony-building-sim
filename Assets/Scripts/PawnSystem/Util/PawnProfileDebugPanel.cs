using System.Collections.Generic;
using System.Text;
using ColonyBuildingSim.WorldContext;
using UnityEngine;

/// <summary>
/// Displays a detailed debug inspection panel for the currently selected pawn.
/// This is a developer-facing panel, not player UI.
/// </summary>
public class PawnProfileDebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PawnManager m_pawnManager;
    [SerializeField] private WorldContextManager m_worldContextManager;

    [Header("Debug Panel")]
    [SerializeField] private bool m_showPawnProfilePanel = true;
    [SerializeField] private bool m_showSelection = true;
    [SerializeField] private bool m_showSimulation = true;
    [SerializeField] private bool m_showBrain = true;
    [SerializeField] private bool m_showIdentity = true;
    [SerializeField] private bool m_showBackgroundAndTraits = true;
    [SerializeField] private bool m_showSkills = true;
    [SerializeField] private bool m_showWorkPriorities = true;
    [SerializeField] private bool m_showCondition = true;
    [SerializeField] private bool m_showMovement = true;

    [Header("Panel Layout")]
    [SerializeField] private int m_windowId = 1001;
    [SerializeField] private float m_panelX = 480.0f;
    [SerializeField] private float m_panelY = 10.0f;
    [SerializeField] private float m_panelWidth = 500.0f;
    [SerializeField] private float m_maxPanelHeight = 720.0f;
    [SerializeField] private float m_lineHeight = 20.0f;
    [SerializeField] private float m_padding = 10.0f;

    private Rect m_panelRect;
    private Vector2 m_scrollPosition = Vector2.zero;
    private List<Pawn> m_cachedSelectedPawns = new List<Pawn>();
    private Pawn m_cachedFocusedPawn;

    /// <summary>
    /// Finds required references if they were not assigned in the Inspector.
    /// </summary>
    private void Start()
    {
        if (m_pawnManager == null)
        {
            m_pawnManager = FindFirstObjectByType<PawnManager>();
        }

        if (m_worldContextManager == null)
        {
            m_worldContextManager = FindFirstObjectByType<WorldContextManager>();
        }

        if (m_pawnManager == null)
        {
            Debug.LogError("PawnProfileDebugPanel is missing a PawnManager reference.", this);
        }

        m_panelRect = new Rect(
            m_panelX,
            m_panelY,
            m_panelWidth,
            CalculatePanelHeight(0, null));
    }

    /// <summary>
    /// Draws the selected pawn profile debug panel.
    /// </summary>
    private void OnGUI()
    {
        if (Event.current.type == EventType.Layout)
        {
            DebugPanelInputBlocker.ResetFrameState();
        }

        if (!m_showPawnProfilePanel)
        {
            return;
        }

        if (m_pawnManager == null)
        {
            return;
        }

        m_cachedSelectedPawns = GetSelectedPawns();
        m_cachedFocusedPawn = m_cachedSelectedPawns.Count > 0 ? m_cachedSelectedPawns[0] : null;

        float calculatedHeight = CalculatePanelHeight(m_cachedSelectedPawns.Count, m_cachedFocusedPawn);
        m_panelRect.height = Mathf.Min(calculatedHeight, m_maxPanelHeight);

        if (m_panelRect.Contains(Event.current.mousePosition))
        {
            DebugPanelInputBlocker.BlockPointerInput();
        }

        m_panelRect = GUI.Window(m_windowId, m_panelRect, DrawPawnProfileWindow, "Selected Pawn Profile");
    }

    /// <summary>
    /// Draws the draggable GUI window contents.
    /// </summary>
    private void DrawPawnProfileWindow(int windowId)
    {
        float contentHeight = CalculatePanelHeight(
            m_cachedSelectedPawns.Count,
            m_cachedFocusedPawn);

        Rect viewRect = new Rect(
            0.0f,
            0.0f,
            m_panelRect.width - 25.0f,
            contentHeight);

        Rect scrollRect = new Rect(
            0.0f,
            22.0f,
            m_panelRect.width,
            m_panelRect.height - 22.0f);

        m_scrollPosition = GUI.BeginScrollView(scrollRect, m_scrollPosition, viewRect);

        float currentY = 5.0f;
        float contentX = m_padding;
        float contentWidth = viewRect.width - (m_padding * 2.0f);

        if (m_showSelection)
        {
            DrawSelectionSummary(m_cachedSelectedPawns, m_cachedFocusedPawn, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_showSimulation)
        {
            DrawSimulationSection(contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_cachedFocusedPawn == null)
        {
            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 22.0f));
            return;
        }

        if (m_showBrain)
        {
            DrawBrainSection(m_cachedFocusedPawn, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        PawnProfile profile = m_cachedFocusedPawn.Profile;

        if (profile == null)
        {
            DrawLine(contentX, ref currentY, contentWidth, "Profile: None");
            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 22.0f));
            return;
        }

        if (m_showIdentity)
        {
            DrawIdentitySection(m_cachedFocusedPawn, profile, contentX, contentWidth, ref currentY);
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
            DrawConditionSection(m_cachedFocusedPawn, profile, contentX, contentWidth, ref currentY);
            currentY += 5.0f;
        }

        if (m_showMovement)
        {
            DrawMovementSection(m_cachedFocusedPawn, profile, contentX, contentWidth, ref currentY);
        }

        GUI.EndScrollView();
        GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 22.0f));
    }

    /// <summary>
    /// Returns all currently selected pawns.
    /// The first selected pawn is treated as the focused debug pawn.
    /// </summary>
    private List<Pawn> GetSelectedPawns()
    {
        List<Pawn> selectedPawns = new List<Pawn>();

        IReadOnlyList<Pawn> activePawns = m_pawnManager.ActivePawns;

        for (int i = 0; i < activePawns.Count; ++i)
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

        DrawLine(x, ref y, width, "Selection");
        DrawLine(x + 10.0f, ref y, width, "Selected Pawns: " + selectedCount);

        if (selectedCount == 0 || focusedPawn == null)
        {
            DrawLine(x + 10.0f, ref y, width, "Focused Pawn: None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Focused Pawn: " + GetPawnDisplayName(focusedPawn));
        DrawLine(x + 10.0f, ref y, width, "Selected: " + focusedPawn.IsSelected);
        DrawLine(x + 10.0f, ref y, width, "Deputized: " + focusedPawn.IsDeputized);

        if (selectedCount > 1)
        {
            DrawLine(x + 10.0f, ref y, width, "Showing first selected pawn profile.");
        }
    }

    /// <summary>
    /// Draws world simulation timing information.
    /// </summary>
    private void DrawSimulationSection(float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "World Simulation");

        if (m_worldContextManager == null)
        {
            DrawLine(x + 10.0f, ref y, width, "World Context: None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Paused: " + m_worldContextManager.IsWorldPaused);
        DrawLine(x + 10.0f, ref y, width, "Time Scale: " + m_worldContextManager.TimeScale);
        DrawLine(x + 10.0f, ref y, width, "Scale Multiplier: " + m_worldContextManager.WorldTimeScaleMultiplier.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Simulation Delta: " + m_worldContextManager.SimulationDeltaTime.ToString("F4"));
        DrawLine(x + 10.0f, ref y, width, "Game Minutes Delta: " + m_worldContextManager.SimulationGameMinutesDeltaTime.ToString("F2"));
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
            DrawLine(x + 10.0f, ref y, width, "Utility Goal: None");
            return;
        }

        PawnBrain brain = pawn.GetComponent<PawnBrain>();

        if (brain == null)
        {
            DrawLine(x + 10.0f, ref y, width, "State: No PawnBrain");
            DrawLine(x + 10.0f, ref y, width, "Activity: None");
            DrawLine(x + 10.0f, ref y, width, "Utility Goal: None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "State: " + brain.CurrentState);
        DrawLine(x + 10.0f, ref y, width, "Activity: " + brain.CurrentActivityLabel);
        DrawLine(x + 10.0f, ref y, width, "Utility Goal: " + brain.CurrentUtilityGoal);
    }

    /// <summary>
    /// Draws identity information for the selected pawn profile.
    /// </summary>
    private void DrawIdentitySection(Pawn pawn, PawnProfile profile, float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Identity");
        DrawLine(x + 10.0f, ref y, width, "Name: " + profile.DisplayName);
        DrawLine(x + 10.0f, ref y, width, "PawnId: " + profile.PawnId);
        DrawLine(x + 10.0f, ref y, width, "Age: " + profile.Age + " (" + profile.AgeBand + ")");
        DrawLine(x + 10.0f, ref y, width, "Gender: " + profile.Gender);

        if (pawn == null)
        {
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Grid Coordinate: " + pawn.GridCoordinate);
        DrawLine(x + 10.0f, ref y, width, "World Position: " + FormatVector3(pawn.WorldPosition));
    }

    /// <summary>
    /// Draws background and trait information for the selected pawn profile.
    /// </summary>
    private void DrawBackgroundAndTraitsSection(PawnProfile profile, float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Background / Traits");
        DrawLine(x + 10.0f, ref y, width, "Background: " + GetBackgroundDisplayName(profile));
        DrawLine(x + 10.0f, ref y, width, "BackgroundId: " + GetBackgroundId(profile));
        DrawLine(x + 10.0f, ref y, width, "Traits: " + BuildTraitDisplayString(profile));
        DrawLine(x + 10.0f, ref y, width, "TraitIds: " + BuildTraitIdDisplayString(profile));
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
        DrawLine(x + 10.0f, ref y, width, "Mine: " + skills.Mine);
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
        DrawLine(x + 10.0f, ref y, width, "Mine: " + workPriorities.MinePriority);
    }

    /// <summary>
    /// Draws needs and condition information for the selected pawn profile.
    /// </summary>
    private void DrawConditionSection(Pawn pawn, PawnProfile profile, float x, float width, ref float y)
    {
        PawnCondition condition = profile.Condition;

        DrawLine(x, ref y, width, "Needs / Condition");

        if (condition == null)
        {
            DrawLine(x + 10.0f, ref y, width, "None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Mobility: " + condition.Mobility);
        DrawLine(x + 10.0f, ref y, width, "Food: " + condition.Food + " / 100 (" + condition.FoodState + ")");
        DrawLine(x + 10.0f, ref y, width, "Sleep: " + condition.Sleep + " / 100 (" + condition.SleepState + ")");
        DrawLine(x + 10.0f, ref y, width, "Temperature: " + condition.Temperature);
        DrawLine(x + 10.0f, ref y, width, "Health: " + condition.Health);

        if (pawn == null)
        {
            return;
        }

        PawnNeedsController needsController = pawn.GetComponent<PawnNeedsController>();

        if (needsController == null)
        {
            DrawLine(x + 10.0f, ref y, width, "Needs Controller: None");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Should Eat: " + pawn.ShouldEat);
        DrawLine(x + 10.0f, ref y, width, "Food Critical: " + pawn.IsFoodCritical);
        DrawLine(x + 10.0f, ref y, width, "Should Sleep: " + pawn.ShouldSleep);
        DrawLine(x + 10.0f, ref y, width, "Sleep Critical: " + pawn.IsSleepCritical);
        DrawLine(x + 10.0f, ref y, width, "Recovering Sleep: " + needsController.IsRecoveringSleep);
        DrawLine(x + 10.0f, ref y, width, "Starvation Minutes: " + needsController.StarvationGameMinutes.ToString("F1") + " / " + needsController.StarvationDeathGameMinutes.ToString("F1"));
    }

    /// <summary>
    /// Draws movement and derived modifier information for the selected pawn profile.
    /// </summary>
    private void DrawMovementSection(Pawn pawn, PawnProfile profile, float x, float width, ref float y)
    {
        DrawLine(x, ref y, width, "Movement / Derived Modifiers");
        DrawLine(x + 10.0f, ref y, width, "Base Move Speed: " + profile.BaseMoveSpeed.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Final Move Speed: " + profile.FinalMoveSpeed.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Profile Work Speed Modifier: " + profile.FinalWorkSpeedModifier.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Craft Quality Modifier: " + profile.FinalCraftQualityModifier.ToString("F2"));

        if (pawn == null)
        {
            DrawLine(x + 10.0f, ref y, width, "Need Move Multiplier: 1.00");
            DrawLine(x + 10.0f, ref y, width, "Need Work Multiplier: 1.00");
            return;
        }

        DrawLine(x + 10.0f, ref y, width, "Need Move Multiplier: " + pawn.NeedMoveSpeedMultiplier.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Need Work Multiplier: " + pawn.NeedWorkSpeedMultiplier.ToString("F2"));
        DrawLine(x + 10.0f, ref y, width, "Is Moving: " + pawn.IsMoving);
        DrawLine(x + 10.0f, ref y, width, "Has Destination: " + pawn.HasMovementDestination);

        if (pawn.HasMovementDestination)
        {
            DrawLine(x + 10.0f, ref y, width, "Destination: " + pawn.MovementDestinationCoordinates);
        }
        else
        {
            DrawLine(x + 10.0f, ref y, width, "Destination: None");
        }
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

        if (m_showSelection)
        {
            totalHeight += m_lineHeight * 2.0f;

            if (focusedPawn != null)
            {
                totalHeight += m_lineHeight * 3.0f;
            }

            if (selectedPawnCount > 1)
            {
                totalHeight += m_lineHeight;
            }

            totalHeight += 5.0f;
        }

        if (m_showSimulation)
        {
            totalHeight += m_lineHeight * 6.0f;
            totalHeight += 5.0f;
        }

        if (focusedPawn == null)
        {
            return totalHeight;
        }

        if (m_showBrain)
        {
            totalHeight += m_lineHeight * 4.0f;
            totalHeight += 5.0f;
        }

        if (focusedPawn.Profile == null)
        {
            totalHeight += m_lineHeight;
            return totalHeight;
        }

        if (m_showIdentity)
        {
            totalHeight += m_lineHeight * 7.0f;
            totalHeight += 5.0f;
        }

        if (m_showBackgroundAndTraits)
        {
            totalHeight += m_lineHeight * 5.0f;
            totalHeight += 5.0f;
        }

        if (m_showSkills)
        {
            totalHeight += m_lineHeight * 7.0f;
            totalHeight += 5.0f;
        }

        if (m_showWorkPriorities)
        {
            totalHeight += m_lineHeight * 7.0f;
            totalHeight += 5.0f;
        }

        if (m_showCondition)
        {
            totalHeight += m_lineHeight * 12.0f;
            totalHeight += 5.0f;
        }

        if (m_showMovement)
        {
            totalHeight += m_lineHeight * 9.0f;
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
    /// Returns the selected pawn's background ID.
    /// </summary>
    private string GetBackgroundId(PawnProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.BackgroundId))
        {
            return "None";
        }

        return profile.BackgroundId;
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

        for (int i = 0; i < profile.TraitDefinitions.Count; ++i)
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

    /// <summary>
    /// Builds a readable comma-separated trait ID list for the selected pawn profile.
    /// </summary>
    private string BuildTraitIdDisplayString(PawnProfile profile)
    {
        if (profile == null || profile.TraitIds.Count == 0)
        {
            return "None";
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < profile.TraitIds.Count; ++i)
        {
            string traitId = profile.TraitIds[i];

            if (string.IsNullOrWhiteSpace(traitId))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(traitId);
        }

        if (builder.Length == 0)
        {
            return "None";
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats a Vector3 for compact debug output.
    /// </summary>
    private string FormatVector3(Vector3 value)
    {
        return "("
               + value.x.ToString("F2")
               + ", "
               + value.y.ToString("F2")
               + ", "
               + value.z.ToString("F2")
               + ")";
    }
}