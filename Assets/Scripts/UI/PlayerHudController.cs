using System.Text;
using ColonyBuildingSim.Inventory;
using ColonyBuildingSim.WorldContext;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the player-facing HUD using runtime colony, world, and pawn state.
/// This is player UI, not a developer debug panel.
/// </summary>
public class PlayerHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ColonyInventoryManager m_colonyInventoryManager;
    [SerializeField] private WorldContextManager m_worldContextManager;
    [SerializeField] private PawnManager m_pawnManager;

    [Header("Resource Text")]
    [SerializeField] private TMP_Text m_woodText;
    [SerializeField] private TMP_Text m_stoneText;
    [SerializeField] private TMP_Text m_foodText;
    [SerializeField] private TMP_Text m_mealText;

    [Header("World Text")]
    [SerializeField] private TMP_Text m_dateText;
    [SerializeField] private TMP_Text m_timeText;

    [Header("World Buttons")]
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private TMP_Text m_pauseButtonText;
    [SerializeField] private Button m_speedOneButton;
    [SerializeField] private TMP_Text m_speedOneButtonText;
    [SerializeField] private Button m_speedTwoButton;
    [SerializeField] private TMP_Text m_speedTwoButtonText;
    [SerializeField] private Button m_speedThreeButton;
    [SerializeField] private TMP_Text m_speedThreeButtonText;

    [Header("Colonist Text")]
    [SerializeField] private TMP_Text m_colonistSummaryText;

    /// <summary>
    /// Finds missing references and wires button events.
    /// </summary>
    private void Start()
    {
        CacheReferences();
        RegisterButtonEvents();
        RefreshHud();
    }

    /// <summary>
    /// Refreshes HUD text every frame so player information stays current.
    /// </summary>
    private void Update()
    {
        RefreshHud();
    }

    /// <summary>
    /// Finds required scene references if they were not assigned in the Inspector.
    /// </summary>
    private void CacheReferences()
    {
        if (m_colonyInventoryManager == null)
        {
            m_colonyInventoryManager = FindFirstObjectByType<ColonyInventoryManager>();
        }

        if (m_worldContextManager == null)
        {
            m_worldContextManager = FindFirstObjectByType<WorldContextManager>();
        }

        if (m_pawnManager == null)
        {
            m_pawnManager = FindFirstObjectByType<PawnManager>();
        }

        if (m_colonyInventoryManager == null)
        {
            Debug.LogError("PlayerHudController is missing a ColonyInventoryManager reference.", this);
        }

        if (m_worldContextManager == null)
        {
            Debug.LogError("PlayerHudController is missing a WorldContextManager reference.", this);
        }

        if (m_pawnManager == null)
        {
            Debug.LogError("PlayerHudController is missing a PawnManager reference.", this);
        }
    }

    /// <summary>
    /// Registers HUD button click events.
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (m_pauseButton != null)
        {
            m_pauseButton.onClick.AddListener(HandlePauseButtonClicked);
        }

        if (m_speedOneButton != null)
        {
            m_speedOneButton.onClick.AddListener(HandleSpeedOneButtonClicked);
        }

        if (m_speedTwoButton != null)
        {
            m_speedTwoButton.onClick.AddListener(HandleSpeedTwoButtonClicked);
        }

        if (m_speedThreeButton != null)
        {
            m_speedThreeButton.onClick.AddListener(HandleSpeedThreeButtonClicked);
        }
    }

    /// <summary>
    /// Refreshes all HUD sections.
    /// </summary>
    private void RefreshHud()
    {
        RefreshResourceText();
        RefreshWorldText();
        RefreshWorldButtonText();
        RefreshColonistSummaryText();
    }

    /// <summary>
    /// Refreshes resource display values.
    /// </summary>
    private void RefreshResourceText()
    {
        if (m_colonyInventoryManager == null)
        {
            SetText(m_woodText, "Wood: --");
            SetText(m_stoneText, "Stone: --");
            SetText(m_foodText, "Food: --");
            SetText(m_mealText, "Meals: --");
            return;
        }

        SetText(m_woodText, "Wood: " + m_colonyInventoryManager.GetResourceAmount(ResourceType.Wood));
        SetText(m_stoneText, "Stone: " + m_colonyInventoryManager.GetResourceAmount(ResourceType.Stone));
        SetText(m_foodText, "Food: " + m_colonyInventoryManager.GetResourceAmount(ResourceType.Food));
        SetText(m_mealText, "Meals: " + m_colonyInventoryManager.GetResourceAmount(ResourceType.Meal));
    }

    /// <summary>
    /// Refreshes world date, time, pause, and speed display values.
    /// </summary>
    private void RefreshWorldText()
    {
        if (m_worldContextManager == null)
        {
            SetText(m_dateText, "World unavailable");
            SetText(m_timeText, "--:--");
            return;
        }

        SetText(
            m_dateText,
            "Day " + m_worldContextManager.Day
            + ", " + m_worldContextManager.WorldMonth
            + " — " + m_worldContextManager.Season);

        SetText(
            m_timeText,
            BuildTimeString()
            + " | "
            + BuildPauseStateString()
            + " | "
            + BuildSpeedString());
    }

    /// <summary>
    /// Refreshes world control button text.
    /// </summary>
    private void RefreshWorldButtonText()
    {
        if (m_worldContextManager == null)
        {
            SetText(m_pauseButtonText, "Pause");
            SetText(m_speedOneButtonText, "1x");
            SetText(m_speedTwoButtonText, "2x");
            SetText(m_speedThreeButtonText, "3x");
            return;
        }

        SetText(m_pauseButtonText, m_worldContextManager.IsWorldPaused ? "Resume" : "Pause");
        SetText(m_speedOneButtonText, BuildSpeedButtonText(WorldTimeScale.OneX, "1x"));
        SetText(m_speedTwoButtonText, BuildSpeedButtonText(WorldTimeScale.TwoX, "2x"));
        SetText(m_speedThreeButtonText, BuildSpeedButtonText(WorldTimeScale.ThreeX, "3x"));
    }

    /// <summary>
    /// Refreshes the single-line colonist summary text.
    /// </summary>
    private void RefreshColonistSummaryText()
    {
        if (m_pawnManager == null)
        {
            SetText(m_colonistSummaryText, "Colonists: unavailable");
            return;
        }

        if (m_pawnManager.ColonistPawns.Count == 0)
        {
            SetText(m_colonistSummaryText, "Colonists: None");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("Colonists: ");

        for (int i = 0; i < m_pawnManager.ColonistPawns.Count; ++i)
        {
            Pawn pawn = m_pawnManager.ColonistPawns[i];

            if (i > 0)
            {
                builder.Append(" | ");
            }

            builder.Append(GetPawnDisplayName(pawn));
            builder.Append(" — ");
            builder.Append(GetPawnHealthText(pawn));
        }

        SetText(m_colonistSummaryText, builder.ToString());
    }

    /// <summary>
    /// Handles pause/resume button clicks.
    /// </summary>
    private void HandlePauseButtonClicked()
    {
        if (m_worldContextManager == null)
        {
            return;
        }

        m_worldContextManager.ToggleWorldPaused();
        RefreshHud();
    }

    /// <summary>
    /// Handles 1x speed button clicks.
    /// </summary>
    private void HandleSpeedOneButtonClicked()
    {
        SetWorldSpeed(WorldTimeScale.OneX);
    }

    /// <summary>
    /// Handles 2x speed button clicks.
    /// </summary>
    private void HandleSpeedTwoButtonClicked()
    {
        SetWorldSpeed(WorldTimeScale.TwoX);
    }

    /// <summary>
    /// Handles 3x speed button clicks.
    /// </summary>
    private void HandleSpeedThreeButtonClicked()
    {
        SetWorldSpeed(WorldTimeScale.ThreeX);
    }

    /// <summary>
    /// Applies a world speed value through WorldContextManager.
    /// </summary>
    private void SetWorldSpeed(WorldTimeScale worldTimeScale)
    {
        if (m_worldContextManager == null)
        {
            return;
        }

        m_worldContextManager.SetWorldTimeScale(worldTimeScale);
        RefreshHud();
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
    /// Builds a formatted world time string.
    /// </summary>
    private string BuildTimeString()
    {
        if (m_worldContextManager == null)
        {
            return "--:--";
        }

        return m_worldContextManager.Hour.ToString("00") + ":" + m_worldContextManager.Minute.ToString("00");
    }

    /// <summary>
    /// Builds a player-facing pause state string.
    /// </summary>
    private string BuildPauseStateString()
    {
        if (m_worldContextManager == null)
        {
            return "Unknown";
        }

        return m_worldContextManager.IsWorldPaused ? "Paused" : "Running";
    }

    /// <summary>
    /// Builds a compact speed string.
    /// </summary>
    private string BuildSpeedString()
    {
        if (m_worldContextManager == null)
        {
            return "1x";
        }

        return m_worldContextManager.WorldTimeScaleMultiplier.ToString("F0") + "x";
    }

    /// <summary>
    /// Adds a selected marker to the active speed button label.
    /// </summary>
    private string BuildSpeedButtonText(WorldTimeScale worldTimeScale, string label)
    {
        if (m_worldContextManager != null && m_worldContextManager.TimeScale == worldTimeScale)
        {
            return "[" + label + "]";
        }

        return label;
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
    /// Returns a readable health state for a pawn.
    /// </summary>
    private string GetPawnHealthText(Pawn pawn)
    {
        if (pawn == null || pawn.Profile == null || pawn.Profile.Condition == null)
        {
            return "Unknown";
        }

        return pawn.Profile.Condition.Health.ToString();
    }
}