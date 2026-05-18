using System;
using System.IO;
using UnityEngine;

namespace ColonyBuildingSim.Persistence
{
    /// <summary>
    /// Saves and loads lightweight persistent player progress.
    /// Data is stored as JSON under Application.persistentDataPath.
    /// </summary>
    public class PersistentDataManager : MonoBehaviour
    {
        private const string kSaveFileName = "persistent_game_data.json";

        [Header("Debug")]
        [SerializeField] private bool m_logSavePath = false;

        private PersistentGameData m_currentData = new PersistentGameData();

        public PersistentGameData CurrentData
        {
            get { return m_currentData; }
        }

        public string SaveFilePath
        {
            get { return Path.Combine(Application.persistentDataPath, kSaveFileName); }
        }

        /// <summary>
        /// Loads persistent data when the manager starts.
        /// </summary>
        private void Awake()
        {
            LoadData();
            RecordLastPlayed();

            if (m_logSavePath)
            {
                Debug.Log("Persistent data path: " + SaveFilePath, this);
            }
        }

        /// <summary>
        /// Loads persistent data from disk. Creates default data if no file exists.
        /// </summary>
        public void LoadData()
        {
            string saveFilePath = SaveFilePath;

            if (!File.Exists(saveFilePath))
            {
                m_currentData = new PersistentGameData();
                SaveData();
                return;
            }

            try
            {
                string json = File.ReadAllText(saveFilePath);
                PersistentGameData loadedData = JsonUtility.FromJson<PersistentGameData>(json);

                if (loadedData == null)
                {
                    m_currentData = new PersistentGameData();
                    SaveData();
                    return;
                }

                m_currentData = loadedData;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to load persistent data: " + exception.Message, this);
                m_currentData = new PersistentGameData();
            }
        }

        /// <summary>
        /// Saves current persistent data to disk.
        /// </summary>
        public void SaveData()
        {
            try
            {
                string json = JsonUtility.ToJson(m_currentData, true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to save persistent data: " + exception.Message, this);
            }
        }

        /// <summary>
        /// Records that the game was played during this session.
        /// </summary>
        public void RecordLastPlayed()
        {
            m_currentData.LastPlayedUtc = DateTime.UtcNow.ToString("o");
            SaveData();
        }

        /// <summary>
        /// Records that the player completed the prototype objective.
        /// </summary>
        public void RecordPrototypeCompleted()
        {
            m_currentData.HasCompletedPrototype = true;
            m_currentData.TimesCompletedPrototype += 1;
            m_currentData.LastCompletedUtc = DateTime.UtcNow.ToString("o");
            m_currentData.LastPlayedUtc = DateTime.UtcNow.ToString("o");

            SaveData();
        }

        /// <summary>
        /// Clears persistent data and writes default values.
        /// Useful for testing.
        /// </summary>
        public void ResetData()
        {
            m_currentData = new PersistentGameData();
            SaveData();
        }
    }
}