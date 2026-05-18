using System;

namespace ColonyBuildingSim.Persistence
{
    /// <summary>
    /// Stores lightweight persistent player progress across play sessions.
    /// This is not a full world save. It tracks prototype completion history.
    /// </summary>
    [Serializable]
    public class PersistentGameData
    {
        public bool HasCompletedPrototype;
        public int TimesCompletedPrototype;
        public string LastPlayedUtc;
        public string LastCompletedUtc;

        /// <summary>
        /// Creates default persistent data for a new player.
        /// </summary>
        public PersistentGameData()
        {
            HasCompletedPrototype = false;
            TimesCompletedPrototype = 0;
            LastPlayedUtc = string.Empty;
            LastCompletedUtc = string.Empty;
        }
    }
}