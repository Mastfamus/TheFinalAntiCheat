using System;
using System.IO;
using System.Text;

namespace AmongUsPCMod
{
    // ============================================================================
    // 👑 THE FAC CONFIG MATRIX: SINGLE DIRECT CONFIG INTEGRATION (v12.0.0 - FINAL)
    // ============================================================================
    internal static class FACGameSettings
    {
        internal static FACConfigBool UseFAC_KickWords = new FACConfigBool(() => EnforceMessageCooldowns);
        internal static FACConfigBool UseFAC_KickWordsOnlyLobby = new FACConfigBool(() => true);
        internal static FACConfigBool DetectedLevel = new FACConfigBool(() => true);
        internal static FACConfigInt DetectedLevelAbove = new FACConfigInt(() => 10000); 
        internal static FACConfigBool KickLevel = new FACConfigBool(() => true);
        internal static FACConfigInt KickLevelBelow = new FACConfigInt(() => 0);
        internal static FACConfigBool DetectCheatClients = new FACConfigBool(() => KickInvalidClients);
        internal static FACConfigBool DetectInvalidRpcs = new FACConfigBool(() => KickOnInvalidRPC);
        internal static FACConfigBool CancelInvalidSabotage = new FACConfigBool(() => true);
        internal static FACConfigBool RpcRateLimiting = new FACConfigBool(() => true);
        internal static FACConfigInt RpcRateLimit = new FACConfigInt(() => 60);
        internal static FACConfigBool CensorDetectionReason = new FACConfigBool(() => false);

        // ЖИВИ СТОЙНОСТИ В ПАМЕТТА НА ИГРАТА:
        public static bool EnforceMessageCooldowns { get; set; } = true;
        public static bool KickOnSpeedHack { get; set; } = true;
        public static bool KickInvalidClients { get; set; } = true;
        public static bool KickOnInvalidRPC { get; set; } = true;

        // ЕДИНСТВЕНИЯТ ГЛАВЕН КОНФИГУРАЦИОНЕН ФАЙЛ:
        private static readonly string MainConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "config", "com.Mastfamus.The-Final-Anti-Cheat.cfg");

        static FACGameSettings()
        {
            LoadSettingsFromDisk();
        }

        public static void LoadSettingsFromDisk()
        {
            try
            {
                if (!File.Exists(MainConfigPath))
                {
                    SaveSettingsToDisk();
                    return;
                }

                var lines = File.ReadAllLines(MainConfigPath);
                foreach (var line in lines)
                {
                    string clean = line.Trim();
                    if (clean.StartsWith("#") || !clean.Contains("=")) continue;

                    string[] parts = clean.Split('=');
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim().ToLowerInvariant();
                    string val = parts[1].Trim().ToLowerInvariant();

                    if (key == "enforcemessagecooldowns") EnforceMessageCooldowns = val == "true";
                    if (key == "kickonspeedhack") KickOnSpeedHack = val == "true";
                    if (key == "kickinvalidclients") KickInvalidClients = val == "true";
                    if (key == "kickoninvalidrpc") KickOnInvalidRPC = val == "true";
                }
            }
            catch { }
        }

        public static void SaveSettingsToDisk()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# ============================================================================");
                sb.AppendLine("# 🛡️ THE FINAL ANTI-CHEAT: SOVEREIGN DIRECT FILE CONFIGURATION");
                sb.AppendLine("# ============================================================================");
                sb.AppendLine();
                sb.AppendLine($"EnforceMessageCooldowns = {EnforceMessageCooldowns.ToString().ToLower()}");
                sb.AppendLine($"KickOnSpeedHack = {KickOnSpeedHack.ToString().ToLower()}");
                sb.AppendLine($"KickInvalidClients = {KickInvalidClients.ToString().ToLower()}");
                sb.AppendLine($"KickOnInvalidRPC = {KickOnInvalidRPC.ToString().ToLower()}");

                string mainDir = Path.GetDirectoryName(MainConfigPath);
                if (!Directory.Exists(mainDir)) Directory.CreateDirectory(mainDir);
                File.WriteAllText(MainConfigPath, sb.ToString(), Encoding.UTF8);

                System.Console.WriteLine("[THE FAC SINGLE-SYNC]: Main configuration file written cleanly to disk without duplication.");
            }
            catch { }
        }
    }

    internal class FACConfigBool 
    {
        private readonly Func<bool> _valueRetriever;
        public FACConfigBool(Func<bool> valueRetriever) => _valueRetriever = valueRetriever;
        public bool GetBool() => _valueRetriever();
    }

    internal class FACConfigInt 
    {
        private readonly Func<int> _valueRetriever;
        public FACConfigInt(Func<int> valueRetriever) => _valueRetriever = valueRetriever;
        public int GetInt() => _valueRetriever();
    }
}