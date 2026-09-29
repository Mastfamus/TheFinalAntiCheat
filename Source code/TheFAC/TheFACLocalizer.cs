using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🌍 THE FAC LOCALIZER: OFFLINE ASSET TRANSLATION MATRIX (v16.5.5)
    // ============================================================================
    public static class TheFACLocalizer
    {
        private static Dictionary<string, string> localizedCache = new Dictionary<string, string>();
        private static readonly string TranslationFilePath = Path.Combine(Application.persistentDataPath, "TheFAC_Data", "Translations");

        public static void LoadTranslationAssets()
        {
            try
            {
                localizedCache.Clear();

                if (!File.Exists(TranslationFilePath))
                {
                    string dataDir = Path.GetDirectoryName(TranslationFilePath);
                    if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

                    List<string> defaultLines = new List<string>
                    {
                        "LobbyTimeout=Lobby Timeout remaining",
                        "RoleReveal=Role Reveal Game Start",
                        "Summary=Match Summary Recap"
                    };
                    File.WriteAllLines(TranslationFilePath, defaultLines.ToArray());
                }

                string[] lines = File.ReadAllLines(TranslationFilePath);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || !line.Contains("=")) continue;
                    
                    string[] parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim().ToLowerInvariant();
                        string value = parts[1].Trim();
                        localizedCache[key] = value;
                    }
                }
                System.Console.WriteLine($"[The FAC Localizer]: Offline dictionary cached successfully. Loaded {localizedCache.Count} translation strings.");
            }
            catch { }
        }

        public static string GetText(string key, string defaultValue)
        {
            string lowerKey = key.Trim().ToLowerInvariant();
            if (localizedCache.TryGetValue(lowerKey, out string translated)) return translated;
            return defaultValue;
        }
    }
}
