using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace AmongUsPCMod
{
    // ============================================================================
    // ⚙️ THE FAC CONFIG P2: SOVEREIGN CONFIGURATION MATRIX (v2.0.0)
    // ============================================================================
    internal static class FACConfigsP2
    {
        private static ConfigFile CustomP2ConfigFile;

        internal static ConfigEntry<bool> InterceptCheatClients { get; private set; }
        internal static ConfigEntry<bool> DropInvalidNetworkPackets { get; private set; }
        internal static ConfigEntry<string> CustomCommandPrefix { get; private set; }

        /// <summary>
        /// Инициализира напълно изолиран физически .cfg файл на диска без конфликти
        /// </summary>
        internal static void LoadSovereignP2Config()
        {
            try
            {
                string configFolderPath = Paths.ConfigPath;
                string finalP2ConfigPath = Path.Combine(configFolderPath, "com.Mastfamus.The-Final-Anti-CheatP2.cfg");

                CustomP2ConfigFile = new ConfigFile(finalP2ConfigPath, true);

                InterceptCheatClients = CustomP2ConfigFile.Bind(
                    "1. Advanced Handshake Shield", 
                    "DetectCheatClients", 
                    true, 
                    "True = Instantly scan and drop traffic from known illegal client extensions."
                );

                DropInvalidNetworkPackets = CustomP2ConfigFile.Bind(
                    "2. Network Payload Integrity", 
                    "DropInvalidRpcs", 
                    true, 
                    "True = Cancel and drop custom malformed RPC packets before they execute."
                );

                CustomCommandPrefix = CustomP2ConfigFile.Bind(
                    "3. Operational Parameters", 
                    "AdminCommandPrefix", 
                    "/", 
                    "The administrative symbol used for executing Part 2 commands."
                );

                System.Console.WriteLine("[THE FAC CONFIG P2]: com.Mastfamus.The-Final-Anti-CheatP2.cfg generated successfully.");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[THE FAC CONFIG ERROR]: Failed to launch independent config file: {ex.Message}");
            }
        }
    }
}
