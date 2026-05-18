using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the player-facing selected pawn panel.
/// Shows one selected pawn's readable profile info, supports deputize controls,
/// and allows work priorities to be cycled for a single selected pawn.
/// </summary>
public class PlayerSelectedPawnPanelController : MonoBehaviour
{
    private const int kHighestPriority = 1;
    private const int kLowestPriority = 4;

    [Header("References")]
    [SerializeField] private PawnSelectionManager m_pawnSelectionManager;
    [SerializeField] private PawnDeputizationManager m_pawnDeputizationManager;

    [Header("Panel")]
    [SerializeField] private GameObject m_selectedPawnPanel;
    [SerializeField] private TMP_Text m_selectedPawnTitleText;
    [SerializeField] private TMP_Text m_selectedPawnInfoText;

    [Header("Deputize Button")]
    [SerializeField] private Button m_deputizeButton;
    [SerializeField] private TMP_Text m_deputizeButtonText;

    [Header("Priority Grid")]
    [SerializeField] private GameObject m_priorityGrid;

    [Header("Priority Buttons")]
    [SerializeField] private Button m_plantPriorityButton;
    [SerializeField] private TMP_Text m_plantPriorityButtonText;
    [SerializeField] private Button m_cutPriorityButton;
    [SerializeField] private TMP_Text m_cutPriorityButtonText;
    [SerializeField] private Button m_constructPriorityButton;
    [SerializeField] private TMP_Text m_constructPriorityButtonText;
    [SerializeField] private Button m_cookPriorityButton;
    [SerializeField] private TMP_Text m_cookPriorityButtonText;
    [SerializeField] private Button m_craftPriorityButton;
    [SerializeField] private TMP_Text m_craftPriorityButtonText;
    [SerializeField] private Button m_minePriorityButton;
    [SerializeField] private TMP_Text m_minePriorityButtonText;

    /// <summary>
    /// Finds required references and wires button events.
    /// </summary>
    private void Start()
    {
        CacheReferences();
        RegisterButtonEvents();
        RefreshPanel();
    }

    /// <summary>
    /// Keeps the selected pawn panel synced with current selection state.
    /// </summary>
    private void Update()
    {
        RefreshPanel();
    }

    /// <summary>
    /// Finds required scene references if they were not assigned in the Inspector.
    /// </summary>
    private void CacheReferences()
    {
        if (m_pawnSelectionManager == null)
        {
            m_pawnSelectionManager = FindFirstObjectByType<PawnSelectionManager>();
        }

        if (m_pawnDeputizationManager == null)
        {
            m_pawnDeputizationManager = FindFirstObjectByType<PawnDeputizationManager>();
        }

        if (m_pawnSelectionManager == null)
        {
            Debug.LogError("PlayerSelectedPawnPanelController is missing a PawnSelectionManager reference.", this);
        }

        if (m_pawnDeputizationManager == null)
        {
            Debug.LogError("PlayerSelectedPawnPanelController is missing a PawnDeputizationManager reference.", this);
        }
    }

    /// <summary>
    /// Registers all UI button click events.
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (m_deputizeButton != null)
        {
            m_deputizeButton.onClick.AddListener(HandleDeputizeButtonClicked);
        }

        RegisterPriorityButton(m_plantPriorityButton, WorkType.Plant);
        RegisterPriorityButton(m_cutPriorityButton, WorkType.Cut);
        RegisterPriorityButton(m_constructPriorityButton, WorkType.Construct);
        RegisterPriorityButton(m_cookPriorityButton, WorkType.Cook);
        RegisterPriorityButton(m_craftPriorityButton, WorkType.Craft);
        RegisterPriorityButton(m_minePriorityButton, WorkType.Mine);
    }

    /// <summary>
    /// Registers one priority button.
    /// </summary>
    private void RegisterPriorityButton(Button button, WorkType workType)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.AddListener(() => HandlePriorityButtonClicked(workType));
    }

    /// <summary>
    /// Refreshes the panel based on current pawn selection.
    /// </summary>
    private void RefreshPanel()
    {
        if (m_selectedPawnPanel == null)
        {
            return;
        }

        if (m_pawnSelectionManager == null)
        {
            m_selectedPawnPanel.SetActive(false);
            return;
        }

        IReadOnlyList<Pawn> selectedPawns = m_pawnSelectionManager.SelectedPawns;

        if (selectedPawns.Count == 0)
        {
            m_selectedPawnPanel.SetActive(false);
            return;
        }

        m_selectedPawnPanel.SetActive(true);

        if (selectedPawns.Count == 1)
        {
            RefreshSinglePawnPanel(selectedPawns[0]);
            return;
        }

        RefreshMultiplePawnPanel(selectedPawns);
    }

    /// <summary>
    /// Refreshes the panel for exactly one selected pawn.
    /// </summary>
    private void RefreshSinglePawnPanel(Pawn pawn)
    {
        SetObjectActive(m_priorityGrid, true);

        if (pawn == null)
        {
            SetText(m_selectedPawnTitleText, "Selected Pawn");
            SetText(m_selectedPawnInfoText, "Missing pawn");
            SetText(m_deputizeButtonText, "Deputize");
            RefreshPriorityText(null);
            return;
        }

        SetText(m_selectedPawnTitleText, GetPawnDisplayName(pawn));
        SetText(m_selectedPawnInfoText, BuildSinglePawnInfoText(pawn));
        SetText(m_deputizeButtonText, pawn.IsDeputized ? "Undeputize" : "Deputize");

        PawnWorkPriorities workPriorities = GetPawnWorkPriorities(pawn);
        RefreshPriorityText(workPriorities);
    }

    /// <summary>
    /// Refreshes the panel for multiple selected pawns.
    /// </summary>
    private void RefreshMultiplePawnPanel(IReadOnlyList<Pawn> selectedPawns)
    {
        SetObjectActive(m_priorityGrid, false);

        SetText(m_selectedPawnTitleText, "Selected Pawns");
        SetText(
            m_selectedPawnInfoText,
            "Multiple pawns selected\n"
            + "Selected Count: "
            + selectedPawns.Count);

        SetText(m_deputizeButtonText, AreAllSelectedPawnsDeputized(selectedPawns) ? "Undeputize Selected" : "Deputize Selected");
    }

    /// <summary>
    /// Handles the deputize button for one or many selected pawns.
    /// </summary>
    private void HandleDeputizeButtonClicked()
    {
        if (m_pawnSelectionManager == null || m_pawnDeputizationManager == null)
        {
            return;
        }

        IReadOnlyList<Pawn> selectedPawns = m_pawnSelectionManager.SelectedPawns;

        if (selectedPawns.Count == 0)
        {
            return;
        }

        bool shouldUndeputize = AreAllSelectedPawnsDeputized(selectedPawns);

        for (int i = 0; i < selectedPawns.Count; ++i)
        {
            Pawn pawn = selectedPawns[i];

            if (pawn == null)
            {
                continue;
            }

            if (shouldUndeputize)
            {
                m_pawnDeputizationManager.UndeputizePawn(pawn);
            }
            else
            {
                m_pawnDeputizationManager.TryDeputizePawn(pawn);
            }
        }

        RefreshPanel();
    }

    /// <summary>
    /// Handles a priority number button click for the single selected pawn.
    /// </summary>
    private void HandlePriorityButtonClicked(WorkType workType)
    {
        Pawn selectedPawn = GetSingleSelectedPawn();

        if (selectedPawn == null)
        {
            return;
        }

        PawnWorkPriorities workPriorities = GetPawnWorkPriorities(selectedPawn);

        if (workPriorities == null)
        {
            return;
        }

        int currentPriority = workPriorities.GetPriority(workType);
        int nextPriority = GetNextPriorityValue(currentPriority);

        workPriorities.SetPriority(workType, nextPriority);
        RefreshPanel();
    }

    /// <summary>
    /// Updates priority button labels.
    /// </summary>
    private void RefreshPriorityText(PawnWorkPriorities workPriorities)
    {
        if (workPriorities == null)
        {
            SetText(m_plantPriorityButtonText, "-");
            SetText(m_cutPriorityButtonText, "-");
            SetText(m_constructPriorityButtonText, "-");
            SetText(m_cookPriorityButtonText, "-");
            SetText(m_craftPriorityButtonText, "-");
            SetText(m_minePriorityButtonText, "-");
            return;
        }

        SetText(m_plantPriorityButtonText, workPriorities.PlantPriority.ToString());
        SetText(m_cutPriorityButtonText, workPriorities.CutPriority.ToString());
        SetText(m_constructPriorityButtonText, workPriorities.ConstructPriority.ToString());
        SetText(m_cookPriorityButtonText, workPriorities.CookPriority.ToString());
        SetText(m_craftPriorityButtonText, workPriorities.CraftPriority.ToString());
        SetText(m_minePriorityButtonText, workPriorities.MinePriority.ToString());
    }

    /// <summary>
    /// Builds the readable single-pawn info block.
    /// </summary>
    private string BuildSinglePawnInfoText(Pawn pawn)
    {
        if (pawn == null || pawn.Profile == null)
        {
            return "Profile unavailable";
        }

        PawnProfile profile = pawn.Profile;
        PawnCondition condition = profile.Condition;

        string healthText = "Unknown";
        string foodText = "Unknown";
        string sleepText = "Unknown";

        if (condition != null)
        {
            healthText = condition.Health.ToString();
            foodText = condition.Food + " / 100 (" + condition.FoodState + ")";
            sleepText = condition.Sleep + " / 100 (" + condition.SleepState + ")";
        }

        StringBuilder builder = new StringBuilder();

        builder.AppendLine("Health: " + healthText);
        builder.AppendLine("Food: " + foodText);
        builder.AppendLine("Sleep: " + sleepText);
        builder.AppendLine("Activity: " + GetPawnActivityText(pawn));
        builder.AppendLine();
        builder.AppendLine("Background: " + GetBackgroundDisplayName(profile));
        builder.AppendLine("Traits: " + BuildTraitDisplayString(profile));
        builder.AppendLine();
        builder.AppendLine("Skills");
        builder.Append(BuildSkillsDisplayString(profile));

        return builder.ToString();
    }

    /// <summary>
    /// Builds a compact skill display string for the player-facing pawn panel.
    /// </summary>
    private string BuildSkillsDisplayString(PawnProfile profile)
    {
        if (profile == null || profile.Skills == null)
        {
            return "None";
        }

        PawnSkills skills = profile.Skills;

        return "Plant " + skills.Plant
            + " | Cut " + skills.Cut
            + " | Construct " + skills.Construct
            + "\nCook " + skills.Cook
            + " | Craft " + skills.Craft
            + " | Mine " + skills.Mine;
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
    /// Returns the selected pawn only when exactly one pawn is selected.
    /// </summary>
    private Pawn GetSingleSelectedPawn()
    {
        if (m_pawnSelectionManager == null)
        {
            return null;
        }

        IReadOnlyList<Pawn> selectedPawns = m_pawnSelectionManager.SelectedPawns;

        if (selectedPawns.Count != 1)
        {
            return null;
        }

        return selectedPawns[0];
    }

    /// <summary>
    /// Gets priority data from the pawn profile.
    /// </summary>
    private PawnWorkPriorities GetPawnWorkPriorities(Pawn pawn)
    {
        if (pawn == null || pawn.Profile == null)
        {
            return null;
        }

        return pawn.Profile.WorkPriorities;
    }

    /// <summary>
    /// Returns the next priority value, looping after the lowest priority.
    /// </summary>
    private int GetNextPriorityValue(int currentPriority)
    {
        if (currentPriority < kHighestPriority || currentPriority >= kLowestPriority)
        {
            return kHighestPriority;
        }

        return currentPriority + 1;
    }

    /// <summary>
    /// Returns true when every selected pawn is already deputized.
    /// </summary>
    private bool AreAllSelectedPawnsDeputized(IReadOnlyList<Pawn> selectedPawns)
    {
        if (selectedPawns == null || selectedPawns.Count == 0 || m_pawnDeputizationManager == null)
        {
            return false;
        }

        for (int i = 0; i < selectedPawns.Count; ++i)
        {
            Pawn pawn = selectedPawns[i];

            if (pawn == null)
            {
                continue;
            }

            if (!m_pawnDeputizationManager.IsPawnDeputized(pawn))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns readable current pawn activity text.
    /// </summary>
    private string GetPawnActivityText(Pawn pawn)
    {
        if (pawn == null)
        {
            return "Unknown";
        }

        PawnBrain brain = pawn.GetComponent<PawnBrain>();

        if (brain == null)
        {
            return "No Brain";
        }

        return brain.CurrentActivityLabel;
    }

    /// <summary>
    /// Returns a readable display name for a pawn.
    /// </summary>
    private string GetPawnDisplayName(Pawn pawn)
    {
        if (pawn == null)
        {
            return "Unknown";
        }

        if (pawn.Profile != null && !string.IsNullOrWhiteSpace(pawn.Profile.DisplayName))
        {
            return pawn.Profile.DisplayName;
        }

        return pawn.PawnId;
    }

    /// <summary>
    /// Safely sets text on a TMP field.
    /// </summary>
    private void SetText(TMP_Text textField, string value)
    {
        if (textField == null)
        {
            return;
        }

        textField.text = value;
    }

    /// <summary>
    /// Safely toggles a GameObject active state.
    /// </summary>
    private void SetObjectActive(GameObject targetObject, bool isActive)
    {
        if (targetObject == null)
        {
            return;
        }

        if (targetObject.activeSelf == isActive)
        {
            return;
        }

        targetObject.SetActive(isActive);
    }
}