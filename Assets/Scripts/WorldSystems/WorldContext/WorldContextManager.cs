using UnityEngine;

namespace ColonyBuildingSim.WorldContext
{
    /// <summary>
    /// Stores the current world context for the simulation.
    /// Other systems should read from this manager, but only this manager should modify the values.
    /// </summary>
    public class WorldContextManager : MonoBehaviour
    {
        private const int kMinutesPerHour = 60;
        private const int kHoursPerDay = 24;
        private const int kDaysPerMonth = 15;
        private const int kMonthsPerYear = 4;
        private const int kGameMinutesPerDay = kMinutesPerHour * kHoursPerDay;

        private const float kNormalRealSecondsPerDay = 1200.0f;
        private const float kFastDebugRealSecondsPerDay = 120.0f;
        private const float kStressTestRealSecondsPerDay = 30.0f;

        [Header("Starting Time")]
        [SerializeField] private int m_startMinute = 0;
        [SerializeField] private int m_startHour = 8;
        [SerializeField] private int m_startDay = 1;
        [SerializeField] private int m_startMonth = 1;
        [SerializeField] private int m_startYear = 1;

        [Header("Debug Time Progression")]
        [SerializeField] private WorldTimeDebugPreset m_debugPreset = WorldTimeDebugPreset.NormalDayTwentyMinutes;

        [Header("World Time Controls")]
        [SerializeField] private WorldTimeScale m_worldTimeScale = WorldTimeScale.OneX;
        [SerializeField] private bool m_isWorldPaused = false;

        [Header("Temperature Modifiers")]
        [SerializeField] private float m_worldEventTemperatureModifier = 0.0f;
        [SerializeField] private float m_temperatureChangeRatePerGameMinute = 0.05f;

        private float m_timeAccumulator = 0.0f;

        public int Minute { get; private set; }
        public int Hour { get; private set; }
        public int Day { get; private set; }
        public int Month { get; private set; }
        public int Year { get; private set; }

        public WorldMonth WorldMonth { get; private set; }
        public SeasonType Season { get; private set; }
        public WorldTimePhase TimePhase { get; private set; }
        public float Temperature { get; private set; }

        public bool IsWorldPaused
        {
            get { return m_isWorldPaused; }
        }

        public WorldTimeScale TimeScale
        {
            get { return m_worldTimeScale; }
        }

        private void Awake()
        {
            InitializeWorldContext();
        }

        private void Update()
        {
            UpdateTimeProgression();
        }

        /// <summary>
        /// Initializes the world context from Inspector defaults.
        /// </summary>
        private void InitializeWorldContext()
        {
            Minute = Mathf.Clamp(m_startMinute, 0, kMinutesPerHour - 1);
            Hour = Mathf.Clamp(m_startHour, 0, kHoursPerDay - 1);
            Day = Mathf.Clamp(m_startDay, 1, kDaysPerMonth);
            Month = Mathf.Clamp(m_startMonth, 1, kMonthsPerYear);
            Year = Mathf.Max(1, m_startYear);

            UpdateWorldMonth();
            UpdateSeason();
            UpdateTimePhase();

            Temperature = GetTargetTemperature();
        }

        /// <summary>
        /// Advances world time using the selected development time preset and world time scale.
        /// </summary>
        private void UpdateTimeProgression()
        {
            if (m_isWorldPaused)
            {
                return;
            }

            float realSecondsPerGameMinute = GetRealSecondsPerGameMinute();

            if (realSecondsPerGameMinute <= 0.0f)
            {
                return;
            }

            m_timeAccumulator += Time.deltaTime;

            while (m_timeAccumulator >= realSecondsPerGameMinute)
            {
                AdvanceMinute();
                m_timeAccumulator -= realSecondsPerGameMinute;
            }
        }

        /// <summary>
        /// Advances world time by one in-game minute and applies calendar rollover.
        /// </summary>
        private void AdvanceMinute()
        {
            ++Minute;

            if (Minute >= kMinutesPerHour)
            {
                Minute = 0;
                ++Hour;
            }

            if (Hour >= kHoursPerDay)
            {
                Hour = 0;
                ++Day;
            }

            if (Day > kDaysPerMonth)
            {
                Day = 1;
                ++Month;
            }

            if (Month > kMonthsPerYear)
            {
                Month = 1;
                ++Year;
            }

            UpdateDerivedWorldContext();
        }

        /// <summary>
        /// Updates values that are derived from the current date and time.
        /// </summary>
        private void UpdateDerivedWorldContext()
        {
            UpdateWorldMonth();
            UpdateSeason();
            UpdateTimePhase();
            UpdateTemperature();
        }

        /// <summary>
        /// Updates the current named world month from the current month index.
        /// </summary>
        private void UpdateWorldMonth()
        {
            switch (Month)
            {
                case 1:
                    WorldMonth = WorldMonth.Bloomtide;
                    break;

                case 2:
                    WorldMonth = WorldMonth.Suncrest;
                    break;

                case 3:
                    WorldMonth = WorldMonth.Harvestfall;
                    break;

                case 4:
                    WorldMonth = WorldMonth.Frostwane;
                    break;
            }
        }

        /// <summary>
        /// Updates the current season from the current month.
        /// </summary>
        private void UpdateSeason()
        {
            switch (Month)
            {
                case 1:
                    Season = SeasonType.Spring;
                    break;

                case 2:
                    Season = SeasonType.Summer;
                    break;

                case 3:
                    Season = SeasonType.Fall;
                    break;

                case 4:
                    Season = SeasonType.Winter;
                    break;
            }
        }

        /// <summary>
        /// Updates the current time phase from the current hour.
        /// </summary>
        private void UpdateTimePhase()
        {
            if (Hour >= 0 && Hour < 5)
            {
                TimePhase = WorldTimePhase.LateNight;
            }
            else if (Hour >= 5 && Hour < 8)
            {
                TimePhase = WorldTimePhase.EarlyMorning;
            }
            else if (Hour >= 8 && Hour < 12)
            {
                TimePhase = WorldTimePhase.Morning;
            }
            else if (Hour >= 12 && Hour < 17)
            {
                TimePhase = WorldTimePhase.Afternoon;
            }
            else if (Hour >= 17 && Hour < 21)
            {
                TimePhase = WorldTimePhase.Evening;
            }
            else
            {
                TimePhase = WorldTimePhase.Night;
            }
        }

        /// <summary>
        /// Moves the current temperature toward the target temperature.
        /// </summary>
        private void UpdateTemperature()
        {
            float targetTemperature = GetTargetTemperature();

            Temperature = Mathf.MoveTowards(
                Temperature,
                targetTemperature,
                m_temperatureChangeRatePerGameMinute);
        }

        /// <summary>
        /// Gets the target temperature from season, time phase, and future world-event modifiers.
        /// </summary>
        private float GetTargetTemperature()
        {
            return GetSeasonBaseTemperature()
                + GetTimePhaseTemperatureModifier()
                + m_worldEventTemperatureModifier;
        }

        /// <summary>
        /// Gets the base temperature for the current season.
        /// </summary>
        private float GetSeasonBaseTemperature()
        {
            switch (Season)
            {
                case SeasonType.Spring:
                    return 58.0f;

                case SeasonType.Summer:
                    return 80.0f;

                case SeasonType.Fall:
                    return 52.0f;

                case SeasonType.Winter:
                    return 32.0f;
            }

            return 55.0f;
        }

        /// <summary>
        /// Gets the temperature modifier based on the current time phase.
        /// </summary>
        private float GetTimePhaseTemperatureModifier()
        {
            switch (TimePhase)
            {
                case WorldTimePhase.LateNight:
                    return -8.0f;

                case WorldTimePhase.EarlyMorning:
                    return -5.0f;

                case WorldTimePhase.Morning:
                    return 0.0f;

                case WorldTimePhase.Afternoon:
                    return 7.0f;

                case WorldTimePhase.Evening:
                    return 1.0f;

                case WorldTimePhase.Night:
                    return -4.0f;
            }

            return 0.0f;
        }

        /// <summary>
        /// Gets the real seconds needed for one in-game minute based on the selected development preset.
        /// </summary>
        private float GetRealSecondsPerGameMinute()
        {
            float realSecondsPerDay = kNormalRealSecondsPerDay;

            switch (m_debugPreset)
            {
                case WorldTimeDebugPreset.NormalDayTwentyMinutes:
                    realSecondsPerDay = kNormalRealSecondsPerDay;
                    break;

                case WorldTimeDebugPreset.FastDayTwoMinutes:
                    realSecondsPerDay = kFastDebugRealSecondsPerDay;
                    break;

                case WorldTimeDebugPreset.StressDayThirtySeconds:
                    realSecondsPerDay = kStressTestRealSecondsPerDay;
                    break;
            }

            return (realSecondsPerDay / kGameMinutesPerDay) / GetWorldTimeScaleMultiplier();
        }

        /// <summary>
        /// Gets the numeric multiplier for the current world time scale.
        /// </summary>
        private float GetWorldTimeScaleMultiplier()
        {
            switch (m_worldTimeScale)
            {
                case WorldTimeScale.OneX:
                    return 1.0f;

                case WorldTimeScale.TwoX:
                    return 2.0f;

                case WorldTimeScale.ThreeX:
                    return 3.0f;
            }

            return 1.0f;
        }
    }
}