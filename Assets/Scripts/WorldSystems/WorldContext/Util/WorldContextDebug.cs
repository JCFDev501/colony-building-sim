using UnityEngine;

namespace ColonyBuildingSim.WorldContext
{
    /// <summary>
    /// Draws world context debug information on screen.
    /// This keeps debug display separate from the core world context logic.
    /// </summary>
    public class WorldContextDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldContextManager m_worldContextManager;

        [Header("Debug")]
        [SerializeField] private bool m_showWorldContextPanel = true;

        private void OnGUI()
        {
            if (!m_showWorldContextPanel)
            {
                return;
            }

            if (m_worldContextManager == null)
            {
                return;
            }

            const float panelX = 500.0f;
            const float panelY = 10.0f;
            const float panelWidth = 340.0f;
            const float panelHeight = 210.0f;
            const float lineHeight = 20.0f;
            const float padding = 10.0f;

            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "World Context Debug");

            float currentY = panelY + 25.0f;
            float labelX = panelX + padding;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Date: " + BuildDateTimeString());
            currentY += lineHeight;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Time: " + BuildTimeString());
            currentY += lineHeight;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Day: " + m_worldContextManager.Day);
            currentY += lineHeight;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Month: " + m_worldContextManager.WorldMonth + " (" + m_worldContextManager.Month + ")");
            currentY += lineHeight;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Year: " + m_worldContextManager.Year);
            currentY += lineHeight;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Season: " + m_worldContextManager.Season);
            currentY += lineHeight;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Time Phase: " + m_worldContextManager.TimePhase);
            currentY += lineHeight;

            GUI.Label(new Rect(labelX, currentY, panelWidth, lineHeight), "Temperature: " + m_worldContextManager.Temperature + "°F");
        }

        /// <summary>
        /// Builds a formatted world date and time string for debug display.
        /// </summary>
        private string BuildDateTimeString()
        {
            return "Year " + m_worldContextManager.Year
                + ", " + m_worldContextManager.WorldMonth
                + ", Day " + m_worldContextManager.Day
                + ", " + BuildTimeString();
        }

        /// <summary>
        /// Builds a formatted time string from the current world hour and minute.
        /// </summary>
        private string BuildTimeString()
        {
            return m_worldContextManager.Hour.ToString("00") + ":" + m_worldContextManager.Minute.ToString("00");
        }
    }
}