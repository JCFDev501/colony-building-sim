using UnityEngine;

namespace ColonyBuildingSim.WorldContext
{
    /// <summary>
    /// Draws world context debug information on screen.
    /// </summary>
    public class WorldContextDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldContextManager m_worldContextManager;

        [Header("Debug")]
        [SerializeField] private bool m_showWorldContextPanel = true;

        [Header("Panel Layout")]
        [SerializeField] private int m_windowId = 1003;
        [SerializeField] private float m_panelX = 500.0f;
        [SerializeField] private float m_panelY = 10.0f;
        [SerializeField] private float m_panelWidth = 380.0f;
        [SerializeField] private float m_maxPanelHeight = 420.0f;
        [SerializeField] private float m_lineHeight = 20.0f;
        [SerializeField] private float m_padding = 10.0f;

        private Rect m_panelRect;
        private Vector2 m_scrollPosition = Vector2.zero;

        /// <summary>
        /// Finds required references and initializes panel layout.
        /// </summary>
        private void Start()
        {
            if (m_worldContextManager == null)
            {
                m_worldContextManager = FindFirstObjectByType<WorldContextManager>();
            }

            if (m_worldContextManager == null)
            {
                Debug.LogError("WorldContextDebug is missing a WorldContextManager reference.", this);
            }

            m_panelRect = new Rect(
                m_panelX,
                m_panelY,
                m_panelWidth,
                CalculatePanelHeight());
        }

        /// <summary>
        /// Draws the world context debug panel.
        /// </summary>
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

            float calculatedHeight = CalculatePanelHeight();
            m_panelRect.height = Mathf.Min(calculatedHeight, m_maxPanelHeight);

            if (m_panelRect.Contains(Event.current.mousePosition))
            {
                DebugPanelInputBlocker.BlockPointerInput();
            }

            m_panelRect = GUI.Window(m_windowId, m_panelRect, DrawWorldContextWindow, "World Context Debug");
        }

        /// <summary>
        /// Draws the draggable GUI window contents.
        /// </summary>
        private void DrawWorldContextWindow(int windowId)
        {
            float contentHeight = CalculatePanelHeight();

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

            DrawDateTimeSection(contentX, contentWidth, ref currentY);
            currentY += 5.0f;

            DrawSeasonSection(contentX, contentWidth, ref currentY);
            currentY += 5.0f;

            DrawSimulationSection(contentX, contentWidth, ref currentY);

            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 22.0f));
        }

        /// <summary>
        /// Draws current world date and time information.
        /// </summary>
        private void DrawDateTimeSection(float x, float width, ref float y)
        {
            DrawLine(x, ref y, width, "Date / Time");
            DrawLine(x + 10.0f, ref y, width, "Date: " + BuildDateTimeString());
            DrawLine(x + 10.0f, ref y, width, "Time: " + BuildTimeString());
            DrawLine(x + 10.0f, ref y, width, "Day: " + m_worldContextManager.Day);
            DrawLine(x + 10.0f, ref y, width, "Month: " + m_worldContextManager.WorldMonth + " (" + m_worldContextManager.Month + ")");
            DrawLine(x + 10.0f, ref y, width, "Year: " + m_worldContextManager.Year);
        }

        /// <summary>
        /// Draws current season, phase, and temperature information.
        /// </summary>
        private void DrawSeasonSection(float x, float width, ref float y)
        {
            DrawLine(x, ref y, width, "Environment");
            DrawLine(x + 10.0f, ref y, width, "Season: " + m_worldContextManager.Season);
            DrawLine(x + 10.0f, ref y, width, "Time Phase: " + m_worldContextManager.TimePhase);
            DrawLine(x + 10.0f, ref y, width, "Temperature: " + m_worldContextManager.Temperature + "°F");
        }

        /// <summary>
        /// Draws simulation timing and speed information.
        /// </summary>
        private void DrawSimulationSection(float x, float width, ref float y)
        {
            DrawLine(x, ref y, width, "Simulation");
            DrawLine(x + 10.0f, ref y, width, "Paused: " + m_worldContextManager.IsWorldPaused);
            DrawLine(x + 10.0f, ref y, width, "Time Scale: " + m_worldContextManager.TimeScale);
            DrawLine(x + 10.0f, ref y, width, "World Speed Multiplier: " + m_worldContextManager.WorldTimeScaleMultiplier.ToString("F2"));
            DrawLine(x + 10.0f, ref y, width, "Simulation Delta: " + m_worldContextManager.SimulationDeltaTime.ToString("F4"));
            DrawLine(x + 10.0f, ref y, width, "Game Minutes Delta: " + m_worldContextManager.SimulationGameMinutesDeltaTime.ToString("F2"));
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
        /// Calculates the debug panel height based on the displayed sections.
        /// </summary>
        private float CalculatePanelHeight()
        {
            float totalHeight = 35.0f;

            totalHeight += m_lineHeight * 6.0f;
            totalHeight += 5.0f;

            totalHeight += m_lineHeight * 4.0f;
            totalHeight += 5.0f;

            totalHeight += m_lineHeight * 6.0f;

            return totalHeight;
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