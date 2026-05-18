using ColonyBuildingSim.Buildables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the player-facing work action bar.
/// This stores the currently selected work placement mode for PlayerController.
/// </summary>
public class PlayerWorkActionBarController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button m_cutButton;
    [SerializeField] private Button m_mineButton;
    [SerializeField] private Button m_plantButton;
    [SerializeField] private Button m_cancelButton;

    [Header("Construct Dropdown")]
    [SerializeField] private TMP_Dropdown m_constructDropdown;

    [Header("Button Text")]
    [SerializeField] private TMP_Text m_cutButtonText;
    [SerializeField] private TMP_Text m_mineButtonText;
    [SerializeField] private TMP_Text m_plantButtonText;
    [SerializeField] private TMP_Text m_cancelButtonText;

    private PlayerWorkMode m_currentWorkMode = PlayerWorkMode.None;

    /// <summary>
    /// Gets the currently selected player work mode.
    /// </summary>
    public PlayerWorkMode CurrentWorkMode
    {
        get { return m_currentWorkMode; }
    }

    /// <summary>
    /// Gets whether the player currently has a work mode selected.
    /// </summary>
    public bool HasActiveWorkMode
    {
        get { return m_currentWorkMode != PlayerWorkMode.None; }
    }

    /// <summary>
    /// Wires button and dropdown events.
    /// </summary>
    private void Start()
    {
        RegisterButtonEvents();
        ConfigureConstructDropdown();
        RefreshVisualState();
    }

    /// <summary>
    /// Selects a work mode from code.
    /// </summary>
    public void SetWorkMode(PlayerWorkMode workMode)
    {
        m_currentWorkMode = workMode;

        if (!IsConstructMode(workMode) && m_constructDropdown != null)
        {
            m_constructDropdown.SetValueWithoutNotify(0);
        }

        RefreshVisualState();
    }

    /// <summary>
    /// Clears the selected work mode.
    /// </summary>
    public void ClearWorkMode()
    {
        m_currentWorkMode = PlayerWorkMode.None;

        if (m_constructDropdown != null)
        {
            m_constructDropdown.SetValueWithoutNotify(0);
        }

        RefreshVisualState();
    }

    /// <summary>
    /// Registers all work action bar UI events.
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (m_cutButton != null)
        {
            m_cutButton.onClick.AddListener(HandleCutClicked);
        }

        if (m_mineButton != null)
        {
            m_mineButton.onClick.AddListener(HandleMineClicked);
        }

        if (m_plantButton != null)
        {
            m_plantButton.onClick.AddListener(HandlePlantClicked);
        }

        if (m_cancelButton != null)
        {
            m_cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        if (m_constructDropdown != null)
        {
            m_constructDropdown.onValueChanged.AddListener(HandleConstructDropdownChanged);
        }
    }

    /// <summary>
    /// Ensures the construct dropdown has the expected options.
    /// </summary>
    private void ConfigureConstructDropdown()
    {
        if (m_constructDropdown == null)
        {
            return;
        }

        if (m_constructDropdown.options.Count >= 4)
        {
            return;
        }

        m_constructDropdown.ClearOptions();
        m_constructDropdown.options.Add(new TMP_Dropdown.OptionData("Construct"));
        m_constructDropdown.options.Add(new TMP_Dropdown.OptionData("Campfire"));
        m_constructDropdown.options.Add(new TMP_Dropdown.OptionData("Wall"));
        m_constructDropdown.options.Add(new TMP_Dropdown.OptionData("Door"));
        m_constructDropdown.SetValueWithoutNotify(0);
        m_constructDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Handles Cut button clicks.
    /// </summary>
    private void HandleCutClicked()
    {
        ToggleWorkMode(PlayerWorkMode.Cut);
    }

    /// <summary>
    /// Handles Mine button clicks.
    /// </summary>
    private void HandleMineClicked()
    {
        ToggleWorkMode(PlayerWorkMode.Mine);
    }

    /// <summary>
    /// Handles Plant button clicks.
    /// </summary>
    private void HandlePlantClicked()
    {
        ToggleWorkMode(PlayerWorkMode.Plant);
    }

    /// <summary>
    /// Handles Cancel button clicks.
    /// </summary>
    private void HandleCancelClicked()
    {
        ClearWorkMode();
    }

    /// <summary>
    /// Handles construct dropdown selection.
    /// </summary>
    private void HandleConstructDropdownChanged(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 1:
                SetWorkMode(PlayerWorkMode.BuildCampfire);
                break;

            case 2:
                SetWorkMode(PlayerWorkMode.BuildWoodenWall);
                break;

            case 3:
                SetWorkMode(PlayerWorkMode.BuildWoodenDoor);
                break;

            default:
                if (IsConstructMode(m_currentWorkMode))
                {
                    ClearWorkMode();
                }
                break;
        }
    }

    /// <summary>
    /// Toggles a mode on when inactive, or clears it when clicking the same active mode again.
    /// </summary>
    private void ToggleWorkMode(PlayerWorkMode workMode)
    {
        if (m_currentWorkMode == workMode)
        {
            ClearWorkMode();
            return;
        }

        SetWorkMode(workMode);
    }

    /// <summary>
    /// Refreshes simple text indicators for active modes.
    /// </summary>
    private void RefreshVisualState()
    {
        SetText(m_cutButtonText, BuildButtonText("Cut", PlayerWorkMode.Cut));
        SetText(m_mineButtonText, BuildButtonText("Mine", PlayerWorkMode.Mine));
        SetText(m_plantButtonText, BuildButtonText("Plant", PlayerWorkMode.Plant));
        SetText(m_cancelButtonText, "Cancel");

        RefreshConstructDropdownLabel();
    }

    /// <summary>
    /// Refreshes the construct dropdown selected value based on current mode.
    /// </summary>
    private void RefreshConstructDropdownLabel()
    {
        if (m_constructDropdown == null)
        {
            return;
        }

        if (m_currentWorkMode == PlayerWorkMode.BuildCampfire)
        {
            m_constructDropdown.SetValueWithoutNotify(1);
        }
        else if (m_currentWorkMode == PlayerWorkMode.BuildWoodenWall)
        {
            m_constructDropdown.SetValueWithoutNotify(2);
        }
        else if (m_currentWorkMode == PlayerWorkMode.BuildWoodenDoor)
        {
            m_constructDropdown.SetValueWithoutNotify(3);
        }
        else
        {
            m_constructDropdown.SetValueWithoutNotify(0);
        }

        m_constructDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Builds selected button text.
    /// </summary>
    private string BuildButtonText(string label, PlayerWorkMode workMode)
    {
        if (m_currentWorkMode == workMode)
        {
            return "[" + label + "]";
        }

        return label;
    }

    /// <summary>
    /// Returns true when the mode is one of the construct placement modes.
    /// </summary>
    private bool IsConstructMode(PlayerWorkMode workMode)
    {
        return workMode == PlayerWorkMode.BuildCampfire
            || workMode == PlayerWorkMode.BuildWoodenWall
            || workMode == PlayerWorkMode.BuildWoodenDoor;
    }

    /// <summary>
    /// Safely sets TMP text.
    /// </summary>
    private void SetText(TMP_Text textField, string value)
    {
        if (textField == null)
        {
            return;
        }

        textField.text = value;
    }
}