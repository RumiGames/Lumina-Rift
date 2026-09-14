using System;
using System.IO;
using UnityEngine;

namespace LuminaRift
{
    public sealed class LocalSaveSystem
    {
        private const string FileName = "lumina-rift-save.json";
        public string SavePath { get; private set; }

        public LocalSaveSystem()
        {
            SavePath = Path.Combine(Application.persistentDataPath, FileName);
        }

        public PlayerSaveData Load()
        {
            if (!File.Exists(SavePath)) return null;
            try
            {
                string json = File.ReadAllText(SavePath);
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                return data != null && data.saveVersion == 1 ? data : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Lumina Rift save could not be loaded: " + exception.Message);
                return null;
            }
        }

        public void Save(PlayerSaveData data)
        {
            if (data == null) return;
            try
            {
                data.lastSaveUtcTicks = DateTime.UtcNow.Ticks;
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string temporaryPath = SavePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
                File.Copy(temporaryPath, SavePath, true);
                File.Delete(temporaryPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Lumina Rift save could not be written: " + exception.Message);
            }
        }

        public void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            string temporaryPath = SavePath + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
