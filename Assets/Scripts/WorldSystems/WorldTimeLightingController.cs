using UnityEngine;

namespace ColonyBuildingSim.WorldContext
{
    /// <summary>
    /// Updates world lighting based on the current world time phase.
    /// </summary>
    public class WorldTimeLightingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldContextManager m_worldContextManager;
        [SerializeField] private Light m_directionalLight;

        [Header("Transition")]
        [SerializeField] private float m_transitionSpeed = 2.0f;

        [Header("Early Morning Lighting")]
        [SerializeField] private float m_earlyMorningLightIntensity = 0.7f;
        [SerializeField] private Color m_earlyMorningLightColor = new Color(1.0f, 0.78f, 0.55f);
        [SerializeField] private Color m_earlyMorningAmbientColor = new Color(0.45f, 0.42f, 0.38f);

        [Header("Morning Lighting")]
        [SerializeField] private float m_morningLightIntensity = 1.0f;
        [SerializeField] private Color m_morningLightColor = new Color(1.0f, 0.92f, 0.78f);
        [SerializeField] private Color m_morningAmbientColor = new Color(0.62f, 0.60f, 0.55f);

        [Header("Afternoon Lighting")]
        [SerializeField] private float m_afternoonLightIntensity = 1.2f;
        [SerializeField] private Color m_afternoonLightColor = Color.white;
        [SerializeField] private Color m_afternoonAmbientColor = new Color(0.70f, 0.70f, 0.70f);

        [Header("Evening Lighting")]
        [SerializeField] private float m_eveningLightIntensity = 0.6f;
        [SerializeField] private Color m_eveningLightColor = new Color(1.0f, 0.55f, 0.35f);
        [SerializeField] private Color m_eveningAmbientColor = new Color(0.40f, 0.32f, 0.35f);

        [Header("Night Lighting")]
        [SerializeField] private float m_nightLightIntensity = 0.18f;
        [SerializeField] private Color m_nightLightColor = new Color(0.45f, 0.55f, 1.0f);
        [SerializeField] private Color m_nightAmbientColor = new Color(0.12f, 0.14f, 0.22f);

        [Header("Late Night Lighting")]
        [SerializeField] private float m_lateNightLightIntensity = 0.1f;
        [SerializeField] private Color m_lateNightLightColor = new Color(0.30f, 0.38f, 0.85f);
        [SerializeField] private Color m_lateNightAmbientColor = new Color(0.07f, 0.08f, 0.14f);

        private void Start()
        {
            ForceLightingForCurrentTimePhase();
        }

        private void Update()
        {
            UpdateLightingForCurrentTimePhase();
        }

        /// <summary>
        /// Smoothly updates lighting values based on the current world time phase.
        /// </summary>
        private void UpdateLightingForCurrentTimePhase()
        {
            if (m_worldContextManager == null || m_directionalLight == null)
            {
                return;
            }

            GetLightingForTimePhase(
                m_worldContextManager.TimePhase,
                out float targetIntensity,
                out Color targetLightColor,
                out Color targetAmbientColor);

            float blendAmount = Time.deltaTime * m_transitionSpeed;

            m_directionalLight.intensity = Mathf.Lerp(
                m_directionalLight.intensity,
                targetIntensity,
                blendAmount);

            m_directionalLight.color = Color.Lerp(
                m_directionalLight.color,
                targetLightColor,
                blendAmount);

            RenderSettings.ambientLight = Color.Lerp(
                RenderSettings.ambientLight,
                targetAmbientColor,
                blendAmount);
        }

        /// <summary>
        /// Immediately applies lighting values for the current world time phase.
        /// </summary>
        private void ForceLightingForCurrentTimePhase()
        {
            if (m_worldContextManager == null || m_directionalLight == null)
            {
                return;
            }

            GetLightingForTimePhase(
                m_worldContextManager.TimePhase,
                out float targetIntensity,
                out Color targetLightColor,
                out Color targetAmbientColor);

            m_directionalLight.intensity = targetIntensity;
            m_directionalLight.color = targetLightColor;
            RenderSettings.ambientLight = targetAmbientColor;
        }

        /// <summary>
        /// Gets the target lighting values for the supplied world time phase.
        /// </summary>
        private void GetLightingForTimePhase(
            WorldTimePhase timePhase,
            out float lightIntensity,
            out Color lightColor,
            out Color ambientColor)
        {
            lightIntensity = m_afternoonLightIntensity;
            lightColor = m_afternoonLightColor;
            ambientColor = m_afternoonAmbientColor;

            switch (timePhase)
            {
                case WorldTimePhase.EarlyMorning:
                    lightIntensity = m_earlyMorningLightIntensity;
                    lightColor = m_earlyMorningLightColor;
                    ambientColor = m_earlyMorningAmbientColor;
                    break;

                case WorldTimePhase.Morning:
                    lightIntensity = m_morningLightIntensity;
                    lightColor = m_morningLightColor;
                    ambientColor = m_morningAmbientColor;
                    break;

                case WorldTimePhase.Afternoon:
                    lightIntensity = m_afternoonLightIntensity;
                    lightColor = m_afternoonLightColor;
                    ambientColor = m_afternoonAmbientColor;
                    break;

                case WorldTimePhase.Evening:
                    lightIntensity = m_eveningLightIntensity;
                    lightColor = m_eveningLightColor;
                    ambientColor = m_eveningAmbientColor;
                    break;

                case WorldTimePhase.Night:
                    lightIntensity = m_nightLightIntensity;
                    lightColor = m_nightLightColor;
                    ambientColor = m_nightAmbientColor;
                    break;

                case WorldTimePhase.LateNight:
                    lightIntensity = m_lateNightLightIntensity;
                    lightColor = m_lateNightLightColor;
                    ambientColor = m_lateNightAmbientColor;
                    break;
            }
        }
    }
}