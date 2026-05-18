using System.Text;
using TMPro;
using UnityEngine;

namespace ColonyBuildingSim.Objectives
{
    /// <summary>
    /// Updates the player-facing objective HUD panel.
    /// This displays current objective requirements and progress.
    /// </summary>
    public class ObjectiveHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ObjectiveManager m_objectiveManager;

        [Header("UI")]
        [SerializeField] private TMP_Text m_objectiveTitleText;
        [SerializeField] private TMP_Text m_objectiveProgressText;

        [Header("Display")]
        [SerializeField] private string m_completeText = "Objective Complete!";

        /// <summary>
        /// Finds required references if they were not assigned in the Inspector.
        /// </summary>
        private void Start()
        {
            if (m_objectiveManager == null)
            {
                m_objectiveManager = FindFirstObjectByType<ObjectiveManager>();
            }

            if (m_objectiveManager == null)
            {
                Debug.LogError("ObjectiveHudController is missing an ObjectiveManager reference.", this);
            }

            RefreshObjectiveHud();
        }

        /// <summary>
        /// Keeps the objective HUD synced with current objective progress.
        /// </summary>
        private void Update()
        {
            RefreshObjectiveHud();
        }

        /// <summary>
        /// Refreshes title and progress text.
        /// </summary>
        private void RefreshObjectiveHud()
        {
            if (m_objectiveManager == null)
            {
                SetText(m_objectiveTitleText, "Objective");
                SetText(m_objectiveProgressText, "Unavailable");
                return;
            }

            SetText(m_objectiveTitleText, m_objectiveManager.ObjectiveTitle);

            if (m_objectiveManager.IsObjectiveComplete)
            {
                SetText(m_objectiveProgressText, m_completeText);
                return;
            }

            SetText(m_objectiveProgressText, BuildObjectiveProgressText());
        }

        /// <summary>
        /// Builds readable objective progress text from the ObjectiveManager requirements.
        /// </summary>
        private string BuildObjectiveProgressText()
        {
            StringBuilder builder = new StringBuilder();

            foreach (ObjectiveRequirement requirement in m_objectiveManager.Requirements)
            {
                if (requirement == null)
                {
                    continue;
                }

                int currentAmount = m_objectiveManager.GetRequirementProgress(requirement);
                int requiredAmount = requirement.RequiredAmount;

                builder.Append(requirement.DisplayName);
                builder.Append(": ");
                builder.Append(Mathf.Min(currentAmount, requiredAmount));
                builder.Append(" / ");
                builder.Append(requiredAmount);

                if (m_objectiveManager.IsRequirementComplete(requirement))
                {
                    builder.Append(" - Done");
                }

                builder.AppendLine();
            }

            return builder.ToString();
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
}