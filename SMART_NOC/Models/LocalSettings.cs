using System;
using System.IO;
using System.Text.Json;

namespace SMART_NOC.Services
{
    public class LocalSettings
    {
        public string GeminiApiKey { get; set; } = "";
        public bool UseAI { get; set; } = true;

        // --- FITUR BARU: AUTO SAVE ---
        public bool IsAutoSaveEnabled { get; set; } = false;

        // Path penyimpanan config.json (Logic Lama Dipertahankan)
        private static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "SMART_NOC");

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, "config.json");
        }

        public static LocalSettings Load()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<LocalSettings>(json) ?? new LocalSettings();
                }
            }
            catch { }
            return new LocalSettings();
        }

        public void Save()
        {
            try
            {
                string path = GetConfigPath();
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }
}