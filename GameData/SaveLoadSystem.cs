using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OHRRPGCEDX.GameData
{
    /// <summary>
    /// System for saving and loading game data
    /// </summary>
    public class SaveLoadSystem
    {
        private string saveDirectory;
        private const int MAX_SAVE_SLOTS = 10;
        private const string SAVE_FILE_EXTENSION = ".sav";

        public SaveLoadSystem()
        {
            saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OHRRPGCEDX", "Saves");
            Directory.CreateDirectory(saveDirectory);
        }

        /// <summary>
        /// Save game data to a slot
        /// </summary>
        public bool SaveGame(GameState gameState, int slot)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
                return false;

            try
            {
                var saveData = new SaveData
                {
                    SaveDate = DateTime.Now,
                    GameState = gameState,
                    Version = "1.0.0"
                };

                var filePath = GetSaveFilePath(slot);
                var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save game to slot {slot}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load game data from a slot
        /// </summary>
        public GameState LoadGame(int slot)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
                return null;

            try
            {
                var filePath = GetSaveFilePath(slot);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                var saveData = JsonConvert.DeserializeObject<SaveData>(json);
                return saveData.GameState;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load game from slot {slot}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if a save slot exists
        /// </summary>
        public bool SaveSlotExists(int slot)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
                return false;

            var filePath = GetSaveFilePath(slot);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Get save slot information
        /// </summary>
        public SaveSlotInfo GetSaveSlotInfo(int slot)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
                return null;

            try
            {
                var filePath = GetSaveFilePath(slot);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                var saveData = JsonConvert.DeserializeObject<SaveData>(json);

                return new SaveSlotInfo
                {
                    Slot = slot,
                    SaveDate = saveData.SaveDate,
                    MapName = saveData.GameState.map_name,
                    HeroCount = saveData.GameState.heroes?.Length ?? 0,
                    Level = saveData.GameState.heroes?[0]?.lev ?? 0,
                    Gold = saveData.GameState.gold
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get save slot info for slot {slot}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Delete a save slot
        /// </summary>
        public bool DeleteSaveSlot(int slot)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
                return false;

            try
            {
                var filePath = GetSaveFilePath(slot);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete save slot {slot}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all save slot information
        /// </summary>
        public List<SaveSlotInfo> GetAllSaveSlotInfo()
        {
            var slots = new List<SaveSlotInfo>();
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                var info = GetSaveSlotInfo(i);
                if (info != null)
                {
                    slots.Add(info);
                }
            }
            return slots;
        }

        /// <summary>
        /// Get the file path for a save slot
        /// </summary>
        private string GetSaveFilePath(int slot)
        {
            return Path.Combine(saveDirectory, $"save_{slot:D2}{SAVE_FILE_EXTENSION}");
        }

        /// <summary>
        /// Export save data to a file
        /// </summary>
        public bool ExportSave(GameState gameState, string filePath)
        {
            try
            {
                var saveData = new SaveMetadata
                {
                    SaveDate = DateTime.Now,
                    GameState = gameState,
                    Version = "1.0.0"
                };

                var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to export save: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import save data from a file
        /// </summary>
        public GameState ImportSave(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                var saveData = JsonConvert.DeserializeObject<SaveMetadata>(json);
                return saveData.GameState;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to import save: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Save metadata structure
    /// </summary>
    public class SaveMetadata
    {
        public DateTime SaveDate { get; set; }
        public GameState GameState { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// Save slot information
    /// </summary>
    public class SaveSlotInfo
    {
        public int Slot { get; set; }
        public DateTime SaveDate { get; set; }
        public string MapName { get; set; }
        public int HeroCount { get; set; }
        public int Level { get; set; }
        public int Gold { get; set; }
    }
}
