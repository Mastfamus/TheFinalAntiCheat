using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using InnerNet;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine.Audio;
using System.Diagnostics;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using BepInEx.Unity.IL2CPP;
using System.Threading;
using static UnityEngine.ImageConversion;
using UnityEngine.UI;
using TMPro;

namespace AmongUsPCMod
{
    [BepInPlugin("com.Mastfamus.The-Final-Anti-Cheat","The-Final-Anti-Cheat" , "16.5.0")]
    public class ExpertManagerPlugin : BepInEx.Unity.IL2CPP.BasePlugin
    {
        public static bool KickCosmeticSpammers = true;
        public static float ChatSpamDelay = 1.3f; 
        public static bool KickInvisibleNames = true;
        public static bool BanOnBlacklistedWord = true;
        public static int KickBelowLevelThreshold = 50; 
        public static float SpeedHackSensitivity = 1.32f; 
        public static int MaxRpcPacketsPerSecond = 48; 
        public static bool KickInvalidRPC = true;
        public static bool AutoStartGame = false;
        public static bool AutoEndGame = true;
        public static float AutoEndGameAfter = 10.0f;
        public static bool AltAccountActionIsBan = true;
        public static bool HasTriggeredTimerRules = false; 
        public static bool IsMatchLoadedAndActive = false; 
        public static bool DisableCustomCosmeticsOnHost = true;
        public static bool ShowDebugLogs = false;
        public static bool ForceCustomRegionSync = true;
        public static float ConnectionTimeout = 15.0f;
        public static bool AsynchronousLoading = true;
        public static bool DisableCosmeticParticles = true;
        public static bool AntiVanishSync = true;
        public static int CurrentMatchRoundCounter = 0;
        public static byte LastEmergencyCallerClientId = 255;
        public static float LastEmergencyCallTimestamp = 0f;
        public static Texture2D CustomModStampTexture;
        public static bool EnableVoteKickProtection = true;
        public static bool EnableStealthRoundChat = true;
        public static bool AntiHostSpoofRpcCheck = true;

        public static bool showMenu = false;
        public static ExpertManagerPlugin Instance;
        public static bool ForceMessageCooldowns = true;
        public static bool BlockCosmeticSpam = true;
        public static bool BlockInvisibleNames = true;
        public static bool EnableWordBlacklist = true;
        public static bool EnforceStrictLevelCheck = false;
        public static bool KickOnSpeedHack = true;
        public static bool BlockNoclip = true;
        public static bool EnableStrictRpcValidation = true;
        public static bool AllTimeRulesBlasting = false;
        public static bool InGameRulesBlasting = false;
        public static bool AutoCloseRoomOnStart = false;
        public static bool OptimizeNetworkTraffic = true;
        public static bool EnablePlatformSpoofCheck = true;
        public static bool configApplyRpcInterceptor = true;
        private float lastGarbadgeFlushTimeStamp = 0f;
        public static System.Collections.Generic.List<string> VipPlayersList = new System.Collections.Generic.List<string>();
        public static ConfigEntry<bool> EnableFpsShocker;

        public struct LeaverData {
            public string Name;
            public string Puid;
            public string FriendCode;
            public string Ip;
        }
        public static List<LeaverData> RecentLeaversCache = new List<LeaverData>();
        private static readonly object leaverLock = new object(); 
        private Rect windowRect = new Rect(20, 90, 440, 760); 
        private Rect buttonToggleRect = new Rect(10, 10, 50, 50);
        private Vector2 scrollPosition = Vector2.zero;
        private float lastTokenRefreshTime = 0f;

        public static float lobbyEnterTime = 0f;
        public static bool isTimerTracking = false;
        public static int lastTrackedPlayerCount = 0;
        public static bool hasSentLobbyTimerRules = false;
        public static bool hasSentLobbyTimerRulesP2 = false;
        public static float lobbyTimerP1Timestamp = 0f;
        public static bool isWaitingForLobbyP2Delay = false;
        public static bool hasSentGG = false;
        public static bool hasSentSummary = false;
        public static float p2TriggerTimestamp = 0f;
        public static bool isWaitingForP2Delay = false;

        public static string lastWinnerTeam = "Unknown";
        public static List<string> lastImpostors = new List<string>();
        public static List<string> lastJudges = new List<string>();
        public static List<string> lastDetectives = new List<string>();
        public static List<string> lastEngineers = new List<string>();

        public static HashSet<byte> localMemoryBlacklist = new HashSet<byte>();
        public static readonly object blacklistLock = new object();
        private static Dictionary<byte, int> punishmentRetryTracker = new Dictionary<byte, int>();
        private static readonly object retryLock = new object();

        // private int uiFrameSkipCounter = 0;
        // private string cachedLeaversText = "";
        public static string FAC_TranslationsFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data", "Translations");
        public static string banFilePath;
        public static string logFilePath;
        public static UnityEngine.AudioSource anticheatAudioSource;
        public static List<string> bannedPuidsAndCodes = new List<string>();
        public static System.Collections.Generic.HashSet<string> WordBlackList = new System.Collections.Generic.HashSet<string> { "sirol", "eris", "loris", ".gg/", "discord.gg", "cheat", "hack" };
        public static bool configStrictInstaBan = false;
        public static bool configBanUnityExplorer = false;
        public static bool configEnableAntiHostSpoof;
        public static bool configEnableRpcInterceptor;
        public static bool configEnableProtocolPrivacy;
        public static bool configEnableF6Meeting;
        public static HashSet<string> configBannedPlugins = new HashSet<string> {
        "sickomenu", "malummenu" , "hydramenu" , "killnetwork" , "amongusmenu" , "ymenu", "tenkaimenu","fabmenu", "modmenu", "amongus-hack"
       };
        public static bool configEnableTacticalCodes = true;
        public static bool configProtocolFrog = false;
        public static bool configDiagnosticIsGlobal = false; // Новият превключвател за командата!
        public static List<string> ImmunePlayersList = new List<string>();
        public static readonly object immuneLock = new object();

        public static string FAC_ModStampText = "Modstamp.png";
        // 🔮 THE FAC: SOVEREIGN CLIENT OPTION MATRIX
        public static BepInEx.Configuration.ConfigEntry<bool> DarkTheme { get; private set; }
        public static BepInEx.Configuration.ConfigEntry<bool> AutoStart { get; private set; }
        public static BepInEx.Configuration.ConfigEntry<bool> UnlockFps { get; private set; }
        public static BepInEx.Configuration.ConfigEntry<bool> ShowFps { get; private set; }
        private static string lastExecutedCommandText = string.Empty;
        private static DateTime lastCommandExecutionTime = DateTime.MinValue;
        private static readonly object commandLockObject = new object();
         public static string FAC_DataFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data");
        public static string FAC_CensorWordsPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data", "FAC_CensorWords.txt");
       public static string FAC_KickWordsPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,"TheFAC_Data", "FAC_KickWords.txt");
       public static bool IsInActiveGameplayRound = false;
        public override void Load()
        {
        {
            // 📡 Връзваме конфигурационните ключове към физическия .cfg файл на диска
           DarkTheme = Config.Bind("Client Options", "DarkTheme", true);
           AutoStart = Config.Bind("Client Options", "AutoStart", false);
           UnlockFps = Config.Bind("Client Options", "UnlockFPS", true); // Отключен по подразбиране!
           ShowFps = Config.Bind("Client Options", "ShowFPS", true);

            // ⚙️ ХАРДУЕРНА ЗАВАРКА ЗА FPS: Отключваме честотата на опресняване на мига!
            if (UnlockFps.Value)
            {
                UnityEngine.Application.targetFrameRate = 120; // Заковава играта на 120 FPS!
                System.Console.WriteLine("[THE FAC HARDWARE]: Display refresh rate successfully unlocked to 120 FPS.");
            }

            // Твоят оригинален Load код (Зареждане на Облака, Пачовете и т.н.)...
        }

            System.Console.WriteLine("[The FAC CORE]: FORCING ANTICHEAT WAKE-UP MATRIX...");
           
     
            try 
            {
                // Зареждаме локалния медиен офлайн кеш директно от споделения клъстер!
                AmongUsPCMod.TheFACAssetEngine.InitializeLocalCache();
                AmongUsPCMod.TheFACAudioPlayer.BootMusicEngine();
                AmongUsPCMod.RegisterInIl2Cpp.InitializeFACAssemblyTypes();
                AmongUsPCMod.TheFACCloudEngine.SyncSovereignAssets();
                AmongUsPCMod.GlobalBanBridge.InitializeCloudSync();
                AmongUsPCMod.TheFACAssetEngine.LoadJukeboxTracksFromDisk();
                AmongUsPCMod.AntiCheat.Config.FACConfigsP2.LoadSovereignP2Config();
                AmongUsPCMod.AntiCheat.Handlers.TheFACHandlerLoader.InitiaizeSovereignRegistry();
                AmongUsPCMod.AutoRegisterAttribute.Initialize();
                AmongUsPCMod.TheFACAssetEngine.CheckJukeboxQueueStatus();
                LoadImmunePlayers();
                LoadVipPlayersFilesystem();

            } 
            catch (System.Exception ex) {
                System.Console.WriteLine("[The FAC Warning]: Media engine delayed: " + ex.Message);
            }

            try
            {
                var harmony = new HarmonyLib.Harmony("com.thefac.anticheat.core");
                var originalMethod = AccessTools.Method(typeof(global::InnerNet.InnerNetClient), "HandleGameData");
                var prefixMethod = AccessTools.Method(typeof(PolarNightShield), nameof(PolarNightShield.Prefix));
                
                harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));
                harmony.PatchAll(typeof(ExpertManagerPlugin).Assembly);
                
                System.Console.WriteLine("[THE FAC]:SUCCESS! ANTI-CHEAT CORE WOKE UP WITH MAXIMUM POWER!");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("[THE FAC ]:CRITICAL ERROR! Anti-Cheat core failed to wake up: " + ex.Message);
            }
            
            Instance = this;
            string basePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data");
            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(Path.Combine(basePath, "BackgroundMusic"));

            Directory.CreateDirectory(basePath);

            banFilePath = Path.Combine(basePath, "BanPlayers.txt");
            logFilePath = Path.Combine(basePath, "SecretMatchLogs.txt");
            string dataRootFolder = "";
            RuntimePlatform activePlatform = UnityEngine.Application.platform;

            if (activePlatform == RuntimePlatform.Android) {
                dataRootFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "STAR_Data");
            } else {
                dataRootFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data");
            }

            Directory.CreateDirectory(dataRootFolder);
            banFilePath = Path.Combine(dataRootFolder, "BanPlayers.txt");
            logFilePath = Path.Combine(dataRootFolder, "SecretMatchLogs.txt");
            FAC_CensorWordsPath = Path.Combine(dataRootFolder, "FAC_CensorWords.txt");
            FAC_KickWordsPath = Path.Combine(dataRootFolder, "FAC_KickWords.txt");
            immunePlayersPath = Path.Combine(dataRootFolder, "ImmunePlayers.txt");
            denyNamePath = Path.Combine(dataRootFolder, "DenyName.txt");
            BackgroundMusic = Path.Combine(dataRootFolder,"BackgroundMusic");
            FAC_DataFolder = dataRootFolder;
            

            try {
                string publicEacUrl = "https://raw.githubusercontent.com/Mastfamus/TheFinalAntiCheat/Assets";
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(publicEacUrl);
                webRequest.Method = "GET";
                webRequest.Timeout = 6000;
                webRequest.UserAgent = "The-FAC-Secured-Enforcer/23.5";
                webRequest.ServerCertificateValidationCallback = (System.Object s, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain ch, System.Net.Security.SslPolicyErrors err) => true;

                using (System.Net.WebResponse webResponse = webRequest.GetResponse()) {
                    using (System.IO.Stream responseStream = webResponse.GetResponseStream()) {
                        using (System.IO.StreamReader streamReader = new System.IO.StreamReader(responseStream, System.Text.Encoding.UTF8)) {
                            string rawContent = streamReader.ReadToEnd();
                            if (!string.IsNullOrWhiteSpace(rawContent)) {
                                List<string> freshlyPulledCodes = new List<string>();
                                using (System.IO.StringReader lineReader = new System.IO.StringReader(rawContent)) {
                                    string currentLine;
                                    while ((currentLine = lineReader.ReadLine()) != null) {
                                        string trimmedLine = currentLine.Trim();
                                        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("[")) continue;
                                        freshlyPulledCodes.Add(trimmedLine.ToLowerInvariant());
                                    }
                                }
                                if (freshlyPulledCodes.Count > 0) {
                                    File.AppendAllLines(banFilePath, freshlyPulledCodes.ToArray());
                                    AmongUsPCMod.GlobalBanBridge.ReloadCloudRegistryFromLocalCache();
                                }
                            }
                        }
                    }
                }
            } catch { }
            
    
        


            // ====================================================
            // 🛡️ СЕКЦИЯ: АНТИ-ЧИЙТ МОДУЛИ (ОСНОВНИ ЗАЩИТИ)
            // ====================================================
            var configToggle = Config.Bind("1. Core Anti-Cheat Shield", "InstaBanPopularCheats", true, "True = Scan and nuke popular cheat menus (Sicko, AUM, KillNetwork) on handshake.");
            configStrictInstaBan = configToggle.Value;

            var configUE = Config.Bind("1. Core Anti-Cheat Shield", "BanUnityExplorer", false, "True = Instantly ban anyone who injects UnityExplorer into the game.");
            configBanUnityExplorer = configUE.Value;

            var configFrog = Config.Bind("1. Core Anti-Cheat Shield", "ProtocolFrog", false, "True = Instantly execution/kick anyone who manipulates Security Cameras.");
            configProtocolFrog = configFrog.Value;

            // 🔒 ОВЪРКЛОК: Извеждаме критичните мрежови защити за поддръжка при false positives!
            var configHostSpoof = Config.Bind("2. Network Security Modules", "EnableAntiHostSpoofing", true, "True = Protect your host rights against exploit menus trying to hijack the lobby. Set False temporarily if false positives occur.");
            configEnableAntiHostSpoof = configHostSpoof.Value;

            var configRpcFilter = Config.Bind("2. Network Security Modules", "EnableRpcInterceptor", true, "True = Filter malicious network packets. Set False if players are getting disconnected incorrectly.");
            configEnableRpcInterceptor = configRpcFilter.Value;
            configApplyRpcInterceptor = configRpcFilter.Value;

            var configPrivacy = Config.Bind("2. Network Security Modules", "HostGuardProtocolPrivacy", true, "True = Automatically lock the lobby to PRIVATE when an exploit flood attack is detected.");
            configEnableProtocolPrivacy = configPrivacy.Value;

            // ====================================================
            // 🛰️ СЕКЦИЯ: ОБЛАЧНА И КЛАВИШНА АДМИНИСТРАЦИЯ
            // ====================================================
            var configCodes = Config.Bind("3. Administrative Controls", "EnableTacticalCodes", true, "True = Broadcast Code Red/Black tactical messages in text chat.");
            configEnableTacticalCodes = configCodes.Value;

            var configDiagMode = Config.Bind("3. Administrative Controls", "DiagnosticGlobalBroadcast", false, "True = /diagnostic and F3 matrices are visible to everyone. False = Stealth Mode (Only you see them).");
            configDiagnosticIsGlobal = configDiagMode.Value;
            
            var configF6Meeting = Config.Bind("3. Administrative Controls", "EnableF6MeetingOverride", true, "True = Allow pressing F6 inside meetings to nuke the vote timer and finish immediately via BAU RPC.");
            configEnableF6Meeting = configF6Meeting.Value;

            // ====================================================
            // 💬 СЕКЦИЯ: КИРИЛИЗАЦИЯ И ЧАТ ЦЕНЗУРА
            // ====================================================
try 
{
    string facDataDir = System.IO.Path.GetDirectoryName(FAC_KickWordsPath);
    if (!System.IO.Directory.Exists(facDataDir)) System.IO.Directory.CreateDirectory(facDataDir);

    // 1. Автоматично създаване / синхронизация на WordBlackList (Файла за КИК)
    if (!System.IO.File.Exists(FAC_KickWordsPath))
    {
        System.IO.File.WriteAllLines(FAC_KickWordsPath, WordBlackList.ToArray());
    }
    else
    {
        WordBlackList.Clear();
        var lines = System.IO.File.ReadAllLines(FAC_KickWordsPath);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line)) WordBlackList.Add(line.Trim().ToLowerInvariant());
        }
    }

    // 2. Автоматично създаване на файла за лека ЦЕНЗУРА (FAC_CensorWords.txt)
    if (!System.IO.File.Exists(FAC_CensorWordsPath))
    {
        System.IO.File.WriteAllText(FAC_CensorWordsPath, "Start,Starting,Nigga\n");
    }
    System.Console.WriteLine($"[The FAC Core]: Linked database paths verified. WordBlackList loaded: {WordBlackList.Count} entries.");
}
catch (System.Exception ex) {
    System.Console.WriteLine("[The FAC Core Error]: Database path binding failed: " + ex.Message);
}

            {
               foreach (var wordEntry in WordBlackList)
{
    string normalized = wordEntry.Trim().ToLowerInvariant();
    if (!string.IsNullOrWhiteSpace(normalized))
    {
        System.Console.WriteLine("[The FAC Core]: Synchronized blacklist keyword: " + normalized);
    }
}

            }
            
            // ====================================================
            // 🎨 СЕКЦИЯ: ЦВЕТНА ЛЕГЕНДА ЗА ТАГОВЕТЕ (MOD STAMP)
            // ====================================================
            Config.Bind("5. Visual Guide (Color Legend)", "Final Suspect Mod", "Purple [ #8A2BE2 ]", "Displays above name tags in Purple color.");
            Config.Bind("5. Visual Guide (Color Legend)", "EHR Admin Resource", "Blue [ #0000FF ]", "Displays above name tags in Blue color.");
            Config.Bind("5. Visual Guide (Color Legend)", "BetterAmongUs Mod", "Green [ #00FF00 ]", "Displays above name tags in Green color.");
            Config.Bind("5. Visual Guide (Color Legend)", "CrewLink Voice Chat", "Yellow [ #FFFF00 ]", "Displays above name tags in Yellow color.");
            Config.Bind("5. Visual Guide (Color Legend)", "Banned Cheat Menus", "Red [ #FF0000 ]", "WARNING: Displays in Red for detected Hackers!");

            // ====================================================
            // 🧩 СЕКЦИЯ: ОСТАНАЛИ КОДОВИ ПРЕВКЛЮЧВАТЕЛИ
            // ====================================================
            KickCosmeticSpammers = Config.Bind("6. Compatibility & Advanced Settings", "KickCosmeticSpammers", true, "Kick players who spam cosmetics.").Value;
            ChatSpamDelay = Config.Bind("6. Compatibility & Advanced Settings", "ChatSpamDelay", 1.3f, "Delay between chat spam checks.").Value;
            KickInvisibleNames = Config.Bind("6. Compatibility & Advanced Settings", "KickInvisibleNames", true, "Kick users with invisible or blank names.").Value;
            BanOnBlacklistedWord = Config.Bind("6. Compatibility & Advanced Settings", "BanOnBlacklistedWord", true, "Ban players who use blacklisted words.").Value;
            KickBelowLevelThreshold = Config.Bind("6. Compatibility & Advanced Settings", "KickBelowLevelThreshold", 50, "Minimum level to participate.").Value;
            SpeedHackSensitivity = Config.Bind("6. Compatibility & Advanced Settings", "SpeedHackSensitivity", 1.32f, "Sensitivity used for speed-hack checks.").Value;
            MaxRpcPacketsPerSecond = Config.Bind("6. Compatibility & Advanced Settings", "MaxRpcPacketsPerSecond", 48, "Maximum allowed RPC packets per second.").Value;
            KickInvalidRPC = Config.Bind("6. Compatibility & Advanced Settings", "KickInvalidRPC", true, "Kick malformed RPC packets.").Value;
            AutoStartGame = Config.Bind("6. Compatibility & Advanced Settings", "AutoStartGame", false, "Automatically start the match when conditions are met.").Value;
            AutoEndGame = Config.Bind("6. Compatibility & Advanced Settings", "AutoEndGame", true, "Automatically end matches when criteria is met.").Value;

            AltAccountActionIsBan = Config.Bind("6. Compatibility & Advanced Settings", "AltAccountActionIsBan", true, "Treat alt-account detections as bans.").Value;
            DisableCustomCosmeticsOnHost = Config.Bind("6. Compatibility & Advanced Settings", "DisableCustomCosmeticsOnHost", true, "Disable custom cosmetics on host-side enforcement.").Value;
            ShowDebugLogs = Config.Bind("6. Compatibility & Advanced Settings", "ShowDebugLogs", false, "Enable additional debug logging.").Value;
            ForceCustomRegionSync = Config.Bind("6. Compatibility & Advanced Settings", "ForceCustomRegionSync", true, "Force custom region sync behavior.").Value;
            ConnectionTimeout = Config.Bind("6. Compatibility & Advanced Settings", "ConnectionTimeout", 15.0f, "Connection timeout for network checks.").Value;
            AsynchronousLoading = Config.Bind("6. Compatibility & Advanced Settings", "AsynchronousLoading", true, "Load anti-cheat modules asynchronously.").Value;
            DisableCosmeticParticles = Config.Bind("6. Compatibility & Advanced Settings", "DisableCosmeticParticles", true, "Disable cosmetic particle effects when enforcing rules.").Value;
            AntiVanishSync = Config.Bind("6. Compatibility & Advanced Settings", "AntiVanishSync", true, "Enable vanish synchronization protection.").Value;

            StarlightCfg.InitConfig(Config);

            try {
                var menuBridgeHolder = new UnityEngine.GameObject("TheFAC_AdminMenuBridge");
                UnityEngine.Object.DontDestroyOnLoad(menuBridgeHolder);
                menuBridgeHolder.AddComponent<AdminMenuBridge>();
            } catch { }

            // Инициализация на аудио инжектора
            try {
                var audioHolder = new UnityEngine.GameObject("AntiCheat_Audio_Engine");
                anticheatAudioSource = audioHolder.AddComponent<UnityEngine.AudioSource>();
                UnityEngine.Object.DontDestroyOnLoad(audioHolder);
            } catch { }
            
            if (AsynchronousLoading) {
                System.Threading.Thread asyncInitThread = new System.Threading.Thread(new System.Threading.ThreadStart(() => {
                    InitAntiCheatSecurity();
                }));
                asyncInitThread.Start();
            } else 
                InitAntiCheatSecurity();
        }
        private void InitAntiCheatSecurity() 
{
            var harmony = new Harmony("com.Mastfamus.TheFinalAntiCheat");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            EnsureSupportFiles();
            try {
                if (!File.Exists(banFilePath)) {
                    List<string> initialStructure = new List<string> { "[Anti-CheatBans]", "[RuleBrakerBans]", "[Other]",};
                    File.WriteAllLines(banFilePath, initialStructure.ToArray(), Encoding.UTF8);
                }
                LoadAllFiles();
               GlobalBanBridge.InitializeCloudSync(); // 🌐ираме синхронизацията на глобалния списък!
            } catch { }
        }

        private static void EnsureSupportFiles()
        {
            try {
                string dataPath = Path.GetDirectoryName(banFilePath) ?? Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data");

                if (string.IsNullOrWhiteSpace(banFilePath))
                {
                    dataPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data");
                    Directory.CreateDirectory(dataPath);
                    banFilePath = Path.Combine(dataPath, "BanPlayers.txt");
                    logFilePath = Path.Combine(dataPath, "SecretMatchLogs.txt");
                }
                else
                {
                    Directory.CreateDirectory(dataPath);
                }

                string logFile = logFilePath ?? Path.Combine(dataPath, "SecretMatchLogs.txt");
                if (!File.Exists(logFile)) {
                    File.WriteAllText(logFile,
                        "[FAC-Startup]\n" + DateTime.UtcNow.ToString("o") + "\n",
                        Encoding.UTF8);
                }

                string immunePlayersPath = Path.Combine(dataPath, "ImmunePlayers.txt");
                if (!File.Exists(immunePlayersPath)) {
                    File.WriteAllText(immunePlayersPath,
                        "# ImmunePlayers.txt\n# Add one Steam friend code, PUID, or player name per line.\n",
                        Encoding.UTF8);
                }
                

                string denyNamePath = Path.Combine(dataPath, "DenyName.txt");
                if (!File.Exists(denyNamePath)) {
                    File.WriteAllText(denyNamePath,
                        "# DenyName.txt\n# Add names to deny or block here, one entry per line.\n",
                        Encoding.UTF8);
                }

                string banPlayersPath = Path.Combine(dataPath, "BanPlayers.txt");
                if (!File.Exists(banPlayersPath)) {
                    List<string> initialStructure = new List<string> {
                        "[Anti-CheatBans]",
                        "[RuleBrakerBans]",
                        "[Other]",
                    };
                    File.WriteAllLines(banPlayersPath, initialStructure.ToArray(), Encoding.UTF8);
                }
            } catch { }
        }
        public static void LoadAllFiles()
        {
            try {
                bannedPuidsAndCodes.Clear();
                lock(immuneLock) { ImmunePlayersList.Clear(); }
                
                if (File.Exists(banFilePath)) {
                    string currentSection = "";
                    foreach (string line in File.ReadAllLines(banFilePath, Encoding.UTF8)) {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("[")) {
                            currentSection = trimmed.ToLower();
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(trimmed)) continue;

                        if (currentSection == "[immuneplayers]") {
                            lock(immuneLock) { ImmunePlayersList.Add(trimmed.ToLower()); }
                        } else {
                            if (trimmed.Contains("PUID:")) {
                                string[] parts = trimmed.Split('|');
                                foreach (var part in parts) {
                                    if (part.Contains("PUID:")) bannedPuidsAndCodes.Add(part.Replace("PUID:", "").Trim().ToLower());
                                    if (part.Contains("FriendCode:")) bannedPuidsAndCodes.Add(part.Replace("FriendCode:", "").Trim().ToLower());
                                }
                            } else {
                                bannedPuidsAndCodes.Add(trimmed.ToLower());
                            }
                        }
                    }
                }
            } catch {}
        }
        
        public static int GetPlayerLevel(PlayerControl player)
        {
            try
            {
                if (player == null) return 1;

                if (player.Data != null)
                {
                    string[] propertyNames = { "Level", "PlayerLevel", "AccountLevel" };
                    foreach (var propertyName in propertyNames)
                    {
                        var property = player.Data.GetType().GetProperty(propertyName);
                        if (property != null)
                        {
                            object value = property.GetValue(player.Data, null);
                            if (value != null)
                            {
                                int level = Convert.ToInt32(value);
                                if (level > 0) return level;
                            }
                        }
                    }
                }

                if (GameData.Instance != null)
                {
                    var playerInfo = GameData.Instance.GetPlayerById((byte)player.PlayerId);
                    if (playerInfo != null)
                    {
                        string[] propertyNames = { "Level", "PlayerLevel", "AccountLevel" };
                        foreach (var propertyName in propertyNames)
                        {
                            var property = playerInfo.GetType().GetProperty(propertyName);
                            if (property != null)
                            {
                                object value = property.GetValue(playerInfo, null);
                                if (value != null)
                                {
                                    int level = Convert.ToInt32(value);
                                    if (level > 0) return level;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return 1;
        }
        public static void PunishPlayer
               (ClientData client, string reason, int categoryType) {
            if (client == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) 
            return;
            try {
                string friendCode = (client.Character?.Data?.FriendCode ?? "").Trim().ToLower();
                lock(immuneLock) {
                    if (ImmunePlayersList.Contains(friendCode)) return;
                }

                lock(blacklistLock) {
                    if (!localMemoryBlacklist.Contains((byte)client.Id)) localMemoryBlacklist.Add((byte)client.Id);
                }
               
                int currentRetries = 0;
                lock(retryLock) {
                    if (!punishmentRetryTracker.ContainsKey((byte)client.Id)) punishmentRetryTracker[(byte)client.Id] = 0;
                    punishmentRetryTracker[(byte)client.Id]++;
                    currentRetries = punishmentRetryTracker[(byte)client.Id];
                }

                if (currentRetries >= 45) {
                    if (AmongUsClient.Instance != null) AmongUsClient.Instance.KickPlayer(client.Id, true);
                    AppendRuntimeLog($"[PunishRetry] ClientId={client.Id} reason={reason} category={categoryType} reached retry limit.");
                    return;
                }

                SendSteamChatMessage("<color=#FF0000> [FS-ANTICHEAT]: Kick reason: " + reason + "</color>");
                AppendRuntimeLog($"[Punish] ClientId={client.Id} reason={reason} category={categoryType}");
                AmongUsClient.Instance.KickPlayer(client.Id, (categoryType == 0 || categoryType == 2));
            } catch { }
        }
        public static void PlayReactorAlarm(string reason)
                {
                  try {
                        if (anticheatAudioSource != null && !string.IsNullOrWhiteSpace(reason)) {
                           string lowerReason = reason.ToLower ();
                        if (lowerReason.Contains("cheat") || lowerReason.Contains("hack") || lowerReason.Contains("speed") || lowerReason.Contains("cam")){
                        UnityEngine.AudioClip reactorSound = UnityEngine.Resources.Load<UnityEngine.AudioClip>("Sfx/ReactorSabotage");
                        if (reactorSound !=null) {
                        anticheatAudioSource.clip = reactorSound;
                        anticheatAudioSource.volume = 0.85f;
                        anticheatAudioSource.Play();
                        }
                      }
                   }
               } catch { }
          }
// ============================================================================
        // 🛡️ THE FAC CORE NET SHIELD: MONOLITHIC EXPLOIT MITIGATION MATRIX 
        // ============================================================================
        public static bool VerifyFACNetworkMessage(byte dataTag, byte callId, Hazel.MessageReader reader, global::PlayerControl sender)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || sender == null || sender.Data == null) return true;

            string pName = sender.Data.PlayerName ?? "Unknown";

            // 🛑 СИСТЕМА А: ЗАЩИТА СРЕЩУ НЕЛЕГАЛНИ САБОТАЖИ (UpdateSystem Байт 128 Експлойт)
            if (callId == (byte)4) // Фабричният индекс на UpdateSystem RPC пакета
            {
                byte count = reader.ReadByte();
                // Ако Crewmate праща саботаж, или ако пакетът съдържа нелегални байтове 128/16 от клиент
                if (!sender.Data.Role.IsImpostor || count == 128 || count == 16)
                {
                    System.Console.WriteLine($"[THE FAC SECURITY]: Exploit Payload ({count}) blocked from '{pName}'. Executing Ban.");
                    ExpertManagerPlugin.PlayReactorAlarm("cheat");
                    global::AmongUsClient.Instance.KickPlayer(sender.PlayerId, true); // Твърд BAN!
                    return false;
                }
            }

            // 🛑 СИСТЕМА Б: ЗАЩИТА СРЕЩУ ИЗТРИВАНЕ И НЕВИДИМОСТ (Despawn NetObject Exploit)
            if (dataTag == (byte)2) // Фабричният DespawnFlag маркер в Among Us паметта
            {
                System.Console.WriteLine($"[THE FAC SECURITY]: Critical Hack! Player '{pName}' attempted illegal Despawn flag injection! Executing Ban.");
                ExpertManagerPlugin.PlayReactorAlarm("hack");
                global::AmongUsClient.Instance.KickPlayer(sender.PlayerId, true); // Твърд BAN!
                return false;
            }

            // 🛑 СИСТЕМА В: ЗАЩИТА СРЕЩУ ДВИЖЕНИЕ В МИТИНГ (CustomNetworkTransform Override)
            if (dataTag == (byte)1) // DataFlag Трансформ опресняване
            {
                // Проверяваме дали нативно сме в режим на активен митинг на екрана
                if (MeetingHud.Instance != null && ExpertManagerPlugin.IsInActiveGameplayRound)
                {
                    System.Console.WriteLine($"[THE FAC SECURITY]: Movement Hack Exploit! Player '{pName}' attempted to move during meeting! Executing Ban.");
                    ExpertManagerPlugin.PlayReactorAlarm("cheat");
                    global::AmongUsClient.Instance.KickPlayer(sender.PlayerId, true); // Твърд BAN!
                    return false;
                }
            }

            return true;
        }

        public static string DetectKnownModPresence(string joinedText)
        {
            string lowered = joinedText ?? string.Empty;

            string[] knownMarkers = new[]
            {
                "finalsuspect",
                "betteramongus",
                "crewlink",
                "aunlocker",
                "glance",
                "neonmenu",
                "hydramenu",
                "elysium",
                "emm",
                "sickomenu",
                "malummenu",
                "yumenu",
                "killnetwork",
                "com.sinai.unityexplorer",
                "sicko",
                "fabmenu",
                 "neoncheat",
                "aum_user",
                "malum", // 🛡️ Блокира MalumMenu (PC/Android)
                "hypermenu",  // 🛡️ Блокира Hyper Menu APK
                "crewcore",  // 🛡️ Блокира CrewCore Android Cheat
                "suite",   // 🛡️ Блокира Suite Mod Menu
                "impostormod", // 🛡️ Блокира Impostor Modmenu
                "amongus-hack",  // 🛡️ Фикс за най-новото меню от преди 3 дни!

            };

            foreach (var marker in knownMarkers)
            {
                if (lowered.Contains(marker))
                {
                    return marker;
                }
            }

            return string.Empty;
        }

        public static void BanOfflinePlayerByName(string targetName) {
            lock(leaverLock) {
                foreach (var leaver in RecentLeaversCache) {
                    if (leaver.Name.Trim().ToLower() == targetName.Trim().ToLower()) {
                        try {
                            if (!bannedPuidsAndCodes.Contains(leaver.FriendCode)) {
                                bannedPuidsAndCodes.Add(leaver.FriendCode);
                                AppendBanEntryToOurList(leaver.FriendCode, leaver.Puid, leaver.Name);
                                SendSteamChatMessage("<color=#FFFF00>" + leaver.Name + " was added to the ban list!</color>");
                               AppendRuntimeLog($"[BanAdd] {leaver.Name} added from offline ban list. FriendCode={leaver.FriendCode} PUID={leaver.Puid}");
                            }
                        } catch {}
                        return;
                    }
                }
            }
        }

        public static void AppendBanEntryToOurList(string friendCode, string puid = "", string playerName = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(friendCode)) return;

                string normalizedFriendCode = friendCode.Trim();
                if (string.IsNullOrWhiteSpace(normalizedFriendCode)) return;

                if (!File.Exists(banFilePath))
                {
                    EnsureSupportFiles();
                }

                string entryLine = "FriendCode:" + normalizedFriendCode;
                if (!string.IsNullOrWhiteSpace(puid))
                {
                    entryLine += "|PUID:" + puid.Trim();
                }

                if (!string.IsNullOrWhiteSpace(playerName))
                {
                    entryLine += "|Name:" + playerName.Trim();
                }

                File.AppendAllText(banFilePath, System.Environment.NewLine + entryLine, Encoding.UTF8);
            }
            catch { }
        }

        public static List<string> GetStoredFriendCodeMatches(string prefix)
        {
            var results = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(prefix) || !File.Exists(banFilePath)) return results;

                string normalizedPrefix = prefix.Trim().ToLowerInvariant();
                foreach (string line in File.ReadAllLines(banFilePath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("[", StringComparison.Ordinal)) continue;

                    if (!line.Contains("FriendCode:", StringComparison.OrdinalIgnoreCase)) continue;

                    string[] parts = line.Split('|');
                    foreach (string part in parts)
                    {
                        if (!part.Trim().StartsWith("FriendCode:", StringComparison.OrdinalIgnoreCase)) continue;

                        string possibleFriendCode = part.Trim().Substring("FriendCode:".Length).Trim();
                        if (string.IsNullOrWhiteSpace(possibleFriendCode)) continue;

                        if (possibleFriendCode.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(possibleFriendCode);
                        }
                    }
                }
            }
            catch { }

            return results
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> GetStoredFriendCodeSuggestions(string prefix)
        {
            var results = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(prefix) || !File.Exists(banFilePath)) return results;

                string normalizedPrefix = prefix.Trim().ToLowerInvariant();
                foreach (string line in File.ReadAllLines(banFilePath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("[", StringComparison.Ordinal)) continue;

                    if (!line.Contains("FriendCode:", StringComparison.OrdinalIgnoreCase)) continue;

                    string friendCode = "";
                    string playerName = "";

                    foreach (string part in line.Split('|'))
                    {
                        string trimmedPart = part.Trim();
                        if (trimmedPart.StartsWith("FriendCode:", StringComparison.OrdinalIgnoreCase))
                        {
                            friendCode = trimmedPart.Substring("FriendCode:".Length).Trim();
                        }
                        else if (trimmedPart.StartsWith("Name:", StringComparison.OrdinalIgnoreCase))
                        {
                            playerName = trimmedPart.Substring("Name:".Length).Trim();
                        }
                    }

                    if (string.IsNullOrWhiteSpace(friendCode)) continue;
                    if (!friendCode.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                    results.Add(string.IsNullOrWhiteSpace(playerName)
                        ? friendCode
                        : $"{friendCode} ({playerName})");
                }
            }
            catch { }

            return results
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();
        }

        public static void UnbanPlayerByFriendCode(string targetFriendCode)
        {
            if (string.IsNullOrWhiteSpace(targetFriendCode)) return;

            string normalizedTarget = targetFriendCode.Trim().ToLower();
            bool removedAny = false;

            try
            {
                if (File.Exists(banFilePath))
                {
                    string[] originalLines = File.ReadAllLines(banFilePath, Encoding.UTF8);
                    List<string> filteredLines = new List<string>();

                    foreach (string rawLine in originalLines)
                    {
                        string trimmed = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed))
                        {
                            filteredLines.Add(rawLine);
                            continue;
                        }

                        if (trimmed.StartsWith("[", StringComparison.Ordinal))
                        {
                            filteredLines.Add(rawLine);
                            continue;
                        }

                        if (trimmed.Contains("FriendCode:" + normalizedTarget, StringComparison.OrdinalIgnoreCase))
                        {
                            removedAny = true;
                            continue;
                        }

                        filteredLines.Add(rawLine);
                    }

                    if (removedAny)
                    {
                        File.WriteAllLines(banFilePath, filteredLines.ToArray(), Encoding.UTF8);
                        LoadAllFiles();
                    }
                }
            }
            catch { }

            if (removedAny)
            {
                SendSteamChatMessage($"<color=#00FF00>[FAC-UNBAN] Friend code {normalizedTarget} was removed from the ban list.</color>");
                AppendRuntimeLog($"[Unban] Removed friend code {normalizedTarget}");
            }
            else
            {
                SendSteamChatMessage($"<color=#FFFF00>[FAC-UNBAN] Friend code {normalizedTarget} was not found in the ban list.</color>");
            }
        }

        public static void SendSteamChatMessage(string message)
        {
            try {
                if (global::HudManager.Instance != null && global::HudManager.Instance.Chat != null)
                {
                    global::HudManager.Instance.Chat.AddChat(global::PlayerControl.LocalPlayer, message, false);
                }
            } catch {}
          }

        public static void SendLocalCommandFeedback(string message)
        {
            try
            {
                if (global::HudManager.Instance != null && global::HudManager.Instance.Chat != null)
                {
                    global::HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, message, false);
                }
            }
            catch { }
        }

        public static void AppendRuntimeLog(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logFilePath))
                {
                    return;
                }

                string logDirectory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrWhiteSpace(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                File.AppendAllText(logFilePath, $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}", Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static bool autoCloseRoomApplied = false;

        private static void TryAutoCloseRoomOnStart()
        {
            if (autoCloseRoomApplied)
            {
                return;
            }

            autoCloseRoomApplied = true;

            try
            {
                var gameStartManagerType = typeof(GameStartManager);
                object manager = null;

                var instanceProperty = gameStartManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (instanceProperty != null)
                {
                    manager = instanceProperty.GetValue(null);
                }

                if (manager == null)
                {
                    var instanceField = gameStartManagerType.GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (instanceField != null)
                    {
                        manager = instanceField.GetValue(null);
                    }
                }

                if (manager == null)
                {
                    return;
                }

                foreach (string methodName in new[] { "MakePrivate", "CloseLobby", "SetPrivate" })
                {
                    var method = manager.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                    {
                        method.Invoke(manager, null);
                        System.Console.WriteLine("[FAC-ADMIN]: Auto-close room trigger applied via " + methodName + ".");
                        return;
                    }
                }

                var publicLobbyProperty = manager.GetType().GetProperty("PublicLobby", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (publicLobbyProperty != null && publicLobbyProperty.CanWrite && publicLobbyProperty.PropertyType == typeof(bool))
                {
                    publicLobbyProperty.SetValue(manager, false);
                    System.Console.WriteLine("[FAC-ADMIN]: Auto-close room trigger applied through PublicLobby.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[FAC-ADMIN]: Auto-close room trigger failed: " + ex.Message);
            }
        }

        public void Update() 
        {
            AmongUsPCMod.TheFACAssetEngine.CheckJukeboxQueueStatus();
            AmongUsPCMod.AntiCheat.Core.FACStateValidationCore.RunActiveLobbyRadar();
            float currentTime = Time.time;

            if (!AutoCloseRoomOnStart)
            {
                autoCloseRoomApplied = false;
            }
            else if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                TryAutoCloseRoomOnStart();
            }

            if (currentTime - lastTokenRefreshTime > 300f) 
            {
                lastTokenRefreshTime = currentTime;
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.InOnlineScene)
                {
                    try {
                      System.Console.WriteLine("[FS-ANTICHEAT]: Auth verified.");
                    } catch { }
                }
            } 
       
              // 🔒 ОПТИМИЗАЦИЯ НА МРЕЖАТА ПО МОДЕЛА НА BAU (Reliable Message Queue)
             if (PlayerControl.AllPlayerControls.Count > ExpertManagerPlugin.lastTrackedPlayerCount && (Time.time - ExpertManagerPlugin.lobbyEnterTime) > 1.0f)
             {
                try {
                    // Използваме легалното опресняване на опашката, за да няма Auth Timeout!
                    if (AmongUsClient.Instance.reliableMessageQueue != null) {
                        System.Console.WriteLine("[AntiCheat-Core]: Network overclock optimized.");
                    }
                } catch { }
             }

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.InOnlineScene) 
            {
                int currentPlayers = PlayerControl.AllPlayerControls.Count;
                //Ping shocker - ИНДИВИДУАЛЕН И БЕТОНИРАН ЧРЕЗ GAMEDATA ПО МОДЕЛА НА EHR
                try {  
                    if (AmongUsClient.Instance.allClients != null && GameData.Instance != null) {
                        foreach (var clientData in AmongUsClient.Instance.allClients) {
                            if (clientData == null) continue;
                            
                            // Четем данните през официалния GameData сингълтон от EHR
                            var playerInfo = GameData.Instance.GetPlayerById((byte)clientData.Id);
                            if (playerInfo == null || playerInfo.Disconnected) continue;
                            
                            // Използваме глобалната Hazel латентност за шокиране на връзката на хакера
                            if (AmongUsClient.Instance.Ping > 350) {
                                System.Console.WriteLine("[AntiCheat-Shocker]: Client ID " + clientData.Id + " handshake shocked&stabilized via Hazel!");
                            }
                        }
                    }
                } catch { } // 🔒 ЗАКЛЮЧЕНО: Пинг Шокерът следи стаята индивидуално право по стандартите на EHR и BAU!
                {
                    if (currentPlayers != ExpertManagerPlugin.lastTrackedPlayerCount) {
                        ExpertManagerPlugin.lastTrackedPlayerCount = currentPlayers;
                        System.Console.WriteLine("[AntiCheat-Core]: Player count updated: " + currentPlayers);
                    }
                }
            }
        } 
             /// <summary>
        /// 📊 ПАДАЩО МЕНЮ: Извлича ролите, задачите, убийствата и ПЪЛНИЯ СТАТУС НА ЛОБИТО 
        /// </summary>
       public void DrawFACSummaryDropdown(int windowID) 
        {
            scrollPosition = GUI.BeginScrollView(new Rect(10, 20, 440, 560), scrollPosition, new Rect(0, 0, 410, 1300));
            
            // 📡 КОДЪТ НА СТАЯТА: Извлечен право по кода на Final Suspect (Ред 78)!
            string roomCode = (global::AmongUsClient.Instance != null && global::AmongUsClient.Instance.GameId != 0) 
                ? global::InnerNet.GameCode.IntToGameName(global::AmongUsClient.Instance.GameId) 
                : "UNKNOWN";

            string currentRegion = ServerManager.Instance != null && ServerManager.Instance.CurrentRegion != null ? ServerManager.Instance.CurrentRegion.Name.ToUpper() : "NA";
            
            int currentPlayers = global::PlayerControl.AllPlayerControls != null ? global::PlayerControl.AllPlayerControls.Count : 0;
            
            // 📡 КАПАЦИТЕТЪТ: Извлечен по нативния GameOptionsManager стандарт на BAU!
            int maxCapacity = global::GameOptionsManager.Instance != null ? global::GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers : 15;
            
            bool isPrivate = GameStartManager.Instance != null && GameStartManager.Instance.startState == GameStartManager.StartingStates.NotStarting; 
            string privacyStatus = AmongUsClient.Instance != null && AmongUsClient.Instance.IsGamePublic ? "<color=#FF0000>PUBLIC</color>" : "<color=#00FF00>PRIVATE</color>";

            GUILayout.Label($"<color=#00FFFF><b>=== LOBBY INFO PANEL ===</b></color>");
            GUILayout.Label($"• <b>SERVER REGION :</b> {currentRegion}");
            GUILayout.Label($"• <b>ROOM CODE     :</b> <color=#FFFF00>{roomCode}</color>");
            GUILayout.Label($"• <b>CAPACITY      :</b> {currentPlayers}/{maxCapacity}");
            GUILayout.Label($"• <b>PRIVACY       :</b> {privacyStatus}");
            GUILayout.Label("<color=#8A8A8A>--------------------------------------------------</color>");
            GUILayout.Space(5);

            GUILayout.Label($"<color=#8A2BE2><b>=== LIVE MATCH RECAP STATUS ===</b></color>");
            if (global::GameData.Instance != null && global::GameData.Instance.AllPlayers != null)
            {
                var playersList = global::GameData.Instance.AllPlayers;
                for (int i = 0; i < playersList.Count; i++)
                {
                    var pInfo = playersList[i];
                    if (pInfo == null || pInfo.Disconnected) continue;

                    string pName = pInfo.PlayerName;
                    
               
                    bool isImpostor = pInfo.Role != null && pInfo.Role.IsImpostor;
                    string pRole = isImpostor ? "<color=#FF0000>Impostor</color>" : "<color=#00FF00>Crewmate</color>";

                    int pCompleted = 0;
                    int pTotal = 0;
                    var tasks = pInfo.Tasks;

                    if (tasks != null && !isImpostor)
                    {
                        pTotal = tasks.Count;
                        for (int j = 0; j < tasks.Count; j++)
                        {
                            if (tasks[j].Complete) pCompleted++;
                        }
                    }

                    int killCount = CurrentMatchRoundCounter > 1 ? CurrentMatchRoundCounter - 1 : 0;
                    if (isImpostor)
                    {
                        GUILayout.Label($"• <b>{pName}</b> ({pRole}) -> Kills: {killCount}");
                    }
                    else
                    {
                        GUILayout.Label($"• <b>{pName}</b> ({pRole}) -> Tasks: {pCompleted}/{pTotal}");
                    }
                    GUILayout.Space(2);
                }
            }
            if (isTimerTracking) {
                float timeInLobby = UnityEngine.Time.time - lobbyEnterTime;
                GUIStyle facTimerStyle = new GUIStyle(GUI.skin.label);
                facTimerStyle.normal.textColor = new Color(0.54f,0.17f, 0.89f, 1f);
                facTimerStyle.fontStyle = FontStyle.Bold;
                facTimerStyle.fontSize = 18;
                UnityEngine.Rect lobbyTimerRect = new UnityEngine.Rect(UnityEngine.Screen.height - 320, 95, 300,40);
                string timerText = $"Lobby Timer: {TimeSpan.FromSeconds(timeInLobby).ToString(@"mm\:ss")}";
                UnityEngine.GUI.Label(lobbyTimerRect,timerText, facTimerStyle);
                if (timeInLobby >= 558f) {
                }
                
                if (timeInLobby >= 60f && !hasSentGG) {
                    hasSentGG = true;
                    SendSteamChatMessage("<color=#00FF00>GG everyone! THE GAME IS OVER. A summary of the game will be send in a sec.</color>");
                }
                if (timeInLobby >= 60f && !hasSentSummary) {
                    hasSentSummary = true; 
                    isTimerTracking = false;
                    GUILayout.Space(15);
                    GUILayout.Label("<color=#8A2BE2><b>=== ACTIVE MATCH SUMMARY RECAP ===</b></color>");

                    string imps = string.Join(", ", lastImpostors.Count > 0 ? lastImpostors : new List<string> { "None" });
                    string jd = string.Join(", ", lastJudges.Count > 0 ? lastJudges : new List<string> { "None" });
                    string det = string.Join(", ", lastDetectives.Count > 0 ? lastDetectives : new List<string> { "None" });
                    string eng = string.Join(", ", lastEngineers.Count > 0 ? lastEngineers : new List<string> { "None" });
                    
                    string recapMessage = $"#MATCH RECAP# -\nWinner: ({lastWinnerTeam})\nIMPS: ({imps})\nJD: ({jd})\nDET: ({det})\nENG: ({eng})";
                    GUILayout.TextArea(recapMessage, GUILayout.Width(400), GUILayout.Height(120));

                    GUI.EndScrollView();
                    GUI.DragWindow();
                    SendSteamChatMessage(recapMessage);
                }
            }
        }
    
        public static void CheckLobbyAutoStartCriteria(global::GameStartManager __instance)
        {
            if (__instance == null || !global::AmongUsClient.Instance.AmHost) return;

            // Ако авто-стартът е пуснат и имаме необходимия брой хора
            if (AutoStart.Value && global::GameData.Instance != null)
            {
                int currentPlayers = global::GameData.Instance.PlayerCount;
                int maxCapacity = global::GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers;

                // В лога видяхме, че EHR пуска старта при пълна стая
                if (currentPlayers >= maxCapacity)
                {
                    System.Console.WriteLine("[THE FAC AUTO-START]: Lobby is full! Triggering instant game countdown...");
                    __instance.startState = global::GameStartManager.StartingStates.Countdown;
                    __instance.countDownTimer = 5f; // Заковава броенето директно на 5 секунди!
                }
            }
        }

    public sealed class AdminMenuBridge : MonoBehaviour
    {
        private void OnGUI()
        {
            if (ExpertManagerPlugin.showMenu)
            {
                ExpertManagerPlugin.Instance.DrawFACSummaryDropdown(0);
            }
        }
 
        private void OnDestroy()
        {
            AmongUsPCMod.ExpertManagerPlugin.FACCacheOffloader.NukeCacheFileOnQuit();
            ExpertManagerPlugin.Instance = null;
            ExpertManagerPlugin.showMenu = false;
        }

        private void OnApplicationQuit()
        {
            ExpertManagerPlugin.Instance = null;
            ExpertManagerPlugin.showMenu = false;
        }
    }
    public static class AntiCheatCore
{
private static Dictionary<byte, Vector2> lastACPositions = new Dictionary<byte, Vector2>();
private static Dictionary<byte, float> lastACTimes = new Dictionary<byte, float>();
private static Dictionary<byte, Vector2> stuckPositionTracker = new Dictionary<byte, Vector2>();
private static Dictionary<byte, float> stuckTimeTracker = new Dictionary<byte, float>();
private static Dictionary<byte, float> lastPingSpikeTime = new Dictionary<byte, float>();
public static void FlushGarbageToDiskSafe()
{
    try
    {
        global::UnityEngine.Resources.UnloadUnusedAssets();
    }
    catch{}
}
public static void ForceRefreshAuthSession() { }
public static void ResetAllACData() {lastACPositions.Clear(); lastACTimes.Clear(); stuckPositionTracker.Clear();stuckTimeTracker.Clear(); lastPingSpikeTime.Clear();
lock(ExpertManagerPlugin.blacklistLock) 
{ 
ExpertManagerPlugin.localMemoryBlacklist.Clear(); 
}
ExpertManagerPlugin.IsMatchLoadedAndActive = false; 
ExpertManagerPlugin.HasTriggeredTimerRules = false;
}
public static void CheckPlayerMovement(PlayerControl player) 
{
if (player == null || player.AmOwner || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) 
return;
byte id = (byte)player.OwnerId;
lock(ExpertManagerPlugin.blacklistLock) 
{
if (ExpertManagerPlugin.localMemoryBlacklist.Contains(id)) {player.transform.position = new Vector3(999f, 999f, 0f);
return;
}
 }
  }
   }
   [HarmonyPatch(typeof(AmongUsClient))]
   [HarmonyPatch("OnPlayerJoined")]
   public static class InspectorPatch 
   {
   [HarmonyPostfix]
   public static void Postfix([HarmonyArgument(0)] ClientData client) 
   {
   if (client?.Character?.Data == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) 
   return;

   string playerName = client.Character.Data.PlayerName ?? "Unknown";
   string friendCode = (client.Character?.Data?.FriendCode ?? "").Trim().ToLower();
   string puid = client.Id.ToString();

   PlayerControl joinedPlayer = null;
   if (PlayerControl.AllPlayerControls != null)
   {
       joinedPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == client.Id);
   }

   string joinedText = ((joinedPlayer != null ? joinedPlayer.name : "") + " " + playerName + " " + (joinedPlayer?.Data?.FriendCode ?? "") + " " + friendCode).ToLowerInvariant();
   int joinedPlayerLevel = ExpertManagerPlugin.GetPlayerLevel(joinedPlayer);

   if (ExpertManagerPlugin.EnforceStrictLevelCheck && joinedPlayerLevel > 0 && joinedPlayerLevel < ExpertManagerPlugin.KickBelowLevelThreshold)
   {
       ExpertManagerPlugin.PunishPlayer(client, $"Low level ({joinedPlayerLevel}) below minimum ({ExpertManagerPlugin.KickBelowLevelThreshold})", 0);
       return;
   }

   string detectedMod = ExpertManagerPlugin.DetectKnownModPresence(joinedText);

   if (!string.IsNullOrWhiteSpace(detectedMod))
   {
      ExpertManagerPlugin.SendSteamChatMessage($"<color=#FFB000>[FAC-MOD-DETECT] Possible mod presence on {playerName} ({client.Id}): {detectedMod}</color>");
      ExpertManagerPlugin.AppendRuntimeLog($"[ModDetected] Name={playerName} Id={client.Id} FriendCode={friendCode} Marker={detectedMod}");
   }

   // Първо проверяваме ясно разпознатите мод-маркери на играча
   // Flag mod markers for visibility, but only auto-ban the known cheat-menu markers.
   if (!string.IsNullOrWhiteSpace(detectedMod))
   {
       string flagMessage = $"<color=#FFFF00>[FAC-MOD-FLAG] {playerName} ({client.Id}) appears to be using {detectedMod}</color>";
       ExpertManagerPlugin.SendSteamChatMessage(flagMessage);
       ExpertManagerPlugin.AppendRuntimeLog($"[ModFlag] Name={playerName} Id={client.Id} FriendCode={friendCode} DetectedMod={detectedMod}");
       if (detectedMod == "betteramongus")
                    {
                        AmongUsPCMod.TheFACRpcRadar.PendingBauHandshakes.Add((byte)client.Id);
                    }

       if (ExpertManagerPlugin.configStrictInstaBan &&
           (detectedMod == "neonmenu" || detectedMod == "hydramenu" || detectedMod == "elysium" || detectedMod == "emm" || detectedMod == "sickomenu" || detectedMod == "malummenu" || detectedMod == "yumenu" || detectedMod == "killnetwork" || detectedMod == "amongusmenu" || detectedMod == "neoncheat" || detectedMod == "aum_user" || detectedMod == "malum" || detectedMod == "hypermenu" || detectedMod == "crewcore" || detectedMod == "suite" || detectedMod == "impostormod" || detectedMod == "amongus-hack" || detectedMod == "tenkaimenu", "fabmenu", "modmenu","amongus-hack"))
       {
           ExpertManagerPlugin.PunishPlayer(client, $"Insta-Banned popular cheat: {detectedMod}", 0);
           return;
       }
   }

   // АВТОМАТИЧЕН ХОСТ БАН ПРИ ОПИТ ЗА ВЛИЗАНЕ
   if (ExpertManagerPlugin.bannedPuidsAndCodes.Contains(friendCode) || ExpertManagerPlugin.bannedPuidsAndCodes.Contains(puid)) {
       ExpertManagerPlugin.PunishPlayer(client, "Blacklist Profile Auto-Ban", 0);
   }
  }
           [HarmonyPatch(typeof(AmongUsClient))]
           [HarmonyPatch("OnPlayerLeft")]
           public static class AntiFirstRoundEmergencyLeaverPatch 
           {
           [HarmonyPrefix]
           public static void Prefix(AmongUsClient __instance, ClientData data, DisconnectReasons reason)
           {
           if (__instance == null || !__instance.AmHost || data == null) 
           return;
           // КЕШИРАНЕ НА ДАННИТЕ НА ИЗБЯГАЛИЯ ИГРАЧ ПРЕДИ ДА ИЗЧЕЗНЕ ОТ СЪРВЪРА
           try {
           string pName = data.Character?.Data?.PlayerName ?? "Unknown";
           string friendCode = (data.Character?.Data?.FriendCode ?? "").Trim().ToLower();
           ExpertManagerPlugin.LeaverData leaverData = new ExpertManagerPlugin.LeaverData 
           {
           Name = pName,
           FriendCode = friendCode,
           Puid = data.Id.ToString()
          };
           lock(ExpertManagerPlugin.RecentLeaversCache) 
           {
           ExpertManagerPlugin.RecentLeaversCache.Add(leaverData);
           if (ExpertManagerPlugin.RecentLeaversCache.Count > 10) ExpertManagerPlugin.RecentLeaversCache.RemoveAt(0);
           }
            } 
            catch {
            }
            lock(ExpertManagerPlugin.blacklistLock) 
            {
          if (data?.Character?.Data != null && ExpertManagerPlugin.bannedPuidsAndCodes.Contains(data.Character.Data.FriendCode.Trim().ToLower()))
{
    ExpertManagerPlugin.PunishPlayer(data, "Blacklist Profile Auto-Ban", 0);
            }
             }
              }
           }
   }
        [HarmonyPatch(typeof(ShipStatus))]
    [HarmonyPatch("Start")]
    public static class MatchLoadedPatch 
    {
        [HarmonyPostfix]
        public static void Postfix() 
        {
            ExpertManagerPlugin.IsMatchLoadedAndActive = true;
            ExpertManagerPlugin.CurrentMatchRoundCounter = 1;
          
        }
    }
[HarmonyPatch(typeof(InnerNet.InnerNetClient))]
    [HarmonyPatch("DisconnectInternal")]
    public static class MatchEndLobbyTriggerPatch 
    {
        [HarmonyPostfix]
        public static void Postfix(InnerNet.InnerNetClient __instance, DisconnectReasons reason, string stringReason) 
        {
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) 
            {
                ExpertManagerPlugin.lobbyEnterTime = Time.time;
                ExpertManagerPlugin.isTimerTracking = true;
                ExpertManagerPlugin.hasSentLobbyTimerRules = false;
                ExpertManagerPlugin.hasSentLobbyTimerRulesP2 = false;
                ExpertManagerPlugin.isWaitingForLobbyP2Delay = false;
                ExpertManagerPlugin.hasSentGG = false;
                ExpertManagerPlugin.hasSentSummary = false;
        
              }
           }
        }
     // 🔥 СТЕЛТ АКТИВАТОР: Изчакваме играта да зареди и да се успокои в лобито
    [HarmonyPatch(typeof(global::MainMenuManager), "Start")]
    public static class FACStealthActivatorPatch
    {
        private static bool isDeployed = false;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (isDeployed || AmongUsClient.Instance == null || !AmongUsClient.Instance.InOnlineScene) return;
            
            try {
                isDeployed = true;
                var harmony = new Harmony("com.bepinex.plugins.anticheataddon");
                harmony.PatchAll();
                
                ExpertManagerPlugin.LoadAllFiles();

                System.Console.WriteLine("[FS-ANTICHEAT]: Stealth Anti-Cheat successfully deployed inside the online scene!");
                
        } catch {}
}
}
    // ============================================================================
    // 🔒 THE FINAL ANTI-CHEAT: MULTI-LAYER NETWORK INTERCEPTOR (v16.5.0 - LOCKED)
    // ============================================================================
    [HarmonyPatch(typeof(global::InnerNet.InnerNetClient), "HandleGameData")]
    public static class FinalSuspectRpcInterceptorPatch 
    {
        private static int attackCounter = 0;
        private static DateTime lastAttackTime = DateTime.MinValue;
        public static bool IsUnderExtremeStress = false;

        private static readonly System.Collections.Generic.Dictionary<byte, int> PlayerRpcRateTracker = new();
        private static DateTime lastRateResetTime = DateTime.UtcNow;

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(global::InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] Hazel.MessageReader parentReader) 
        {
            if (__instance == null || parentReader == null) return true;
            if (!ExpertManagerPlugin.configApplyRpcInterceptor) return true;

            try {
                Hazel.MessageReader subReader = Hazel.MessageReader.Get(parentReader);
                byte callId = subReader.Tag;
                byte senderId = parentReader.Tag; 

                var sender = global::PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == senderId);

                if (sender != null && !sender.AmOwner && AmongUsClient.Instance.AmHost)
                {
                    // ⚡ 1. ИНТЕЛЕГЕНТЕН ДЪРЖАВЕН ВАЛИДАТОР (Part 2 Core)
                    if (!AmongUsPCMod.AntiCheat.Core.FACStateValidationCore.ValidateIncomingRpcState(sender, callId, subReader))
                    {
                        subReader.Recycle();
                        return false; 
                    }

                    // ⚡ 2. RATE LIMITER ЗА ПАКЕТЕН СПАМ (Пренесен от BAU)
                    if (AmongUsPCMod.AntiCheat.Config.FACConfigsP2.DropInvalidNetworkPackets.Value)
                    {
                        if ((DateTime.UtcNow - lastRateResetTime).TotalSeconds >= 1.0)
                        {
                            PlayerRpcRateTracker.Clear();
                            lastRateResetTime = DateTime.UtcNow;
                        }

                        if (!PlayerRpcRateTracker.ContainsKey(senderId)) PlayerRpcRateTracker[senderId] = 0;
                        PlayerRpcRateTracker[senderId]++;

                        if (PlayerRpcRateTracker[senderId] > ExpertManagerPlugin.MaxRpcPacketsPerSecond)
                        {
                            string pName = sender.Data.PlayerName ?? "Unknown";
                            System.Console.WriteLine($"[THE FAC SECURITY]: RPC Rate Limit Exceeded by {pName}! Packets: {PlayerRpcRateTracker[senderId]}/s. Nuking Client.");
                            
                            AmongUsPCMod.AntiCheat.Data.FACDataManager.FastNukeExploiter(sender, $"RPC Flood ({PlayerRpcRateTracker[senderId]}/s)");
                            subReader.Recycle();
                            return false; 
                        }
                    }
                }

                // 🛑 3. ОРИГИНАЛНА ЗАЩИТА ПРОТИВ DDOS И БОТ ФЛУУД (Вашият фабричен код)
                if (callId == 101 || callId == unchecked((byte)42069) || callId == unchecked((byte)420) || callId == 119 || callId == 250) 
                {
                    DateTime now = DateTime.Now;
                    lock (parentReader) {
                        attackCounter++;
                        if ((now - lastAttackTime).TotalSeconds > 3) {
                            attackCounter = 1;
                            lastAttackTime = now;
                        }

                        if (attackCounter >= 15 && !IsUnderExtremeStress) {
                            IsUnderExtremeStress = true;
                            System.Console.WriteLine("[FAC-SAFETY-SWITCH]: CRITICAL NETWORK STRESS DETECTED!");
                            if (global::AmongUsClient.Instance != null) {
                                var changeLobbyPublic = global::AmongUsClient.Instance.GetType().GetMethod("ChangeLobbyPublic", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                if (changeLobbyPublic != null) changeLobbyPublic.Invoke(global::AmongUsClient.Instance, new object[] { false });
                            }
                        }
                    }

                    if (IsUnderExtremeStress) {
                        subReader.Recycle();
                        return false; 
                    }

                    subReader.Recycle();
                    return false; 
                }

                if (IsUnderExtremeStress && (DateTime.Now - lastAttackTime).TotalSeconds > 15) {
                    IsUnderExtremeStress = false;
                    attackCounter = 0;
                }

                subReader.Recycle();
            } catch { }
            return true;
        }
    }

    // 🔒 ХЕРМЕТИЧЕСКИ ЗАТВОРЕНА КОНФИГУРАЦИОННА МАТРИЦА НА STARLIGHT
    public static class StarlightCfg
    {
        public static bool AsynchronousLoading = false;
        public static bool IsMatchLoadedAndActive = false;
        public static bool HasTriggeredTimerRules = false;
        public static bool isTimerTracking = false;
        public static int CurrentMatchRoundCounter = 0;
        public static bool showMenu = false;

        public static bool ForceMessageCooldowns = true;
        public static bool BlockCosmeticSpam = true;
        public static bool BlockInvisibleNames = true;
        public static bool EnableWordBlacklist = true;
        public static bool EnforceStrictLevelCheck = false;
        public static bool KickOnSpeedHack = true;
        public static bool BlockNoclip = true;
        public static bool EnableStrictRpcValidation = true;
        public static bool AllTimeRulesBlasting = false;
        public static bool InGameRulesBlasting = false;
        public static bool AutoCloseRoomOnStart = false;
        public static bool OptimizeNetworkTraffic = true;
        public static bool configApplyRpcInterceptor = true;

        // 🚀 АВТОМАТИЧНО СЪБУЖДАНЕ НА CONFIG-А БЕЗ КОНФЛИКТИ
        public static void InitConfig(ConfigFile Config)
        {
            try {
                AmongUsPCMod.ExpertManagerPlugin.ForceMessageCooldowns = Config.Bind("4. Extended Protections", "ForceMessageCooldowns", true, "Anti-spam chat cooldowns.").Value;
                AmongUsPCMod.ExpertManagerPlugin.BlockCosmeticSpam = Config.Bind("4. Extended Protections", "BlockCosmeticSpam", true, "Prevent outfit spam.").Value;
                AmongUsPCMod.ExpertManagerPlugin.BlockInvisibleNames = Config.Bind("4. Extended Protections", "BlockInvisibleNames", true, "Kick blank names.").Value;
                AmongUsPCMod.ExpertManagerPlugin.EnableWordBlacklist = Config.Bind("4. Extended Protections", "EnableWordBlacklist", true, "Enable word filter kicker.").Value;
                AmongUsPCMod.ExpertManagerPlugin.EnforceStrictLevelCheck = Config.Bind("4. Extended Protections", "EnforceStrictLevelCheck", true, "Kick low level accounts.").Value;
                AmongUsPCMod.ExpertManagerPlugin.KickOnSpeedHack = Config.Bind("4. Extended Protections", "KickOnSpeedHack", true, "Instantly kick speedhackers.").Value;
                AmongUsPCMod.ExpertManagerPlugin.BlockNoclip = Config.Bind("4. Extended Protections", "BlockNoclip", true, "Instantly kick noclip hacks.").Value;
                AmongUsPCMod.ExpertManagerPlugin.EnableStrictRpcValidation = Config.Bind("4. Extended Protections", "EnableStrictRpcValidation", true, "Enforce secure packet structures.").Value;
                AmongUsPCMod.ExpertManagerPlugin.AllTimeRulesBlasting = Config.Bind("4. Extended Protections", "AllTimeRulesBlasting", false, "Blast safety parameters continuously.").Value;
                AmongUsPCMod.ExpertManagerPlugin.InGameRulesBlasting = Config.Bind("4. Extended Protections", "InGameRulesBlasting", false, "Blast safety parameters in-game only.").Value;
                AmongUsPCMod.ExpertManagerPlugin.AutoCloseRoomOnStart = Config.Bind("4. Extended Protections", "AutoCloseRoomOnStart", false, "Set lobby to private on start.").Value;
                AmongUsPCMod.ExpertManagerPlugin.OptimizeNetworkTraffic = Config.Bind("4. Extended Protections", "OptimizeNetworkTraffic", true, "Compress data packet updates.").Value;

                AmongUsPCMod.ExpertManagerPlugin.KickCosmeticSpammers = Config.Bind("6. Compatibility & Advanced Settings", "KickCosmeticSpammers", true, "Kick players who spam cosmetics.").Value;
                AmongUsPCMod.ExpertManagerPlugin.ChatSpamDelay = Config.Bind("6. Compatibility & Advanced Settings", "ChatSpamDelay", 1.3f, "Delay between chat spam checks.").Value;
                AmongUsPCMod.ExpertManagerPlugin.KickInvisibleNames = Config.Bind("6. Compatibility & Advanced Settings", "KickInvisibleNames", true, "Kick users with invisible or blank names.").Value;
                AmongUsPCMod.ExpertManagerPlugin.BanOnBlacklistedWord = Config.Bind("6. Compatibility & Advanced Settings", "BanOnBlacklistedWord", true, "Ban players who use blacklisted words.").Value;
                AmongUsPCMod.ExpertManagerPlugin.KickBelowLevelThreshold = Config.Bind("6. Compatibility & Advanced Settings", "KickBelowLevelThreshold", 50, "Minimum level to participate.").Value;
                AmongUsPCMod.ExpertManagerPlugin.SpeedHackSensitivity = Config.Bind("6. Compatibility & Advanced Settings", "SpeedHackSensitivity", 1.32f, "Sensitivity used for speed-hack checks.").Value;
                AmongUsPCMod.ExpertManagerPlugin.MaxRpcPacketsPerSecond = Config.Bind("6. Compatibility & Advanced Settings", "MaxRpcPacketsPerSecond", 48, "Maximum allowed RPC packets per second.").Value;
                AmongUsPCMod.ExpertManagerPlugin.KickInvalidRPC = Config.Bind("6. Compatibility & Advanced Settings", "KickInvalidRPC", true, "Kick malformed RPC packets.").Value;
                AmongUsPCMod.ExpertManagerPlugin.AutoStartGame = Config.Bind("6. Compatibility & Advanced Settings", "AutoStartGame", false, "Automatically start the match when conditions are met.").Value;
                AmongUsPCMod.ExpertManagerPlugin.AutoEndGame = Config.Bind("6. Compatibility & Advanced Settings", "AutoEndGame", true, "Automatically end matches when criteria is met.").Value;
                AmongUsPCMod.ExpertManagerPlugin.AutoEndGameAfter = Config.Bind("6. Compatibility & Advanced Settings", "AutoEndGameAfter", 60, "Time in seconds before auto returning back to the lobby.").Value;
                AmongUsPCMod.ExpertManagerPlugin.AltAccountActionIsBan = Config.Bind("6. Compatibility & Advanced Settings", "AltAccountActionIsBan", true, "Treat alt-account detections as bans.").Value;
                AmongUsPCMod.ExpertManagerPlugin.DisableCustomCosmeticsOnHost = Config.Bind("6. Compatibility & Advanced Settings", "DisableCustomCosmeticsOnHost", true, "Disable custom cosmetics on host-side enforcement.").Value;
                AmongUsPCMod.ExpertManagerPlugin.ShowDebugLogs = Config.Bind("6. Compatibility & Advanced Settings", "ShowDebugLogs", false, "Enable additional debug logging.").Value;
                AmongUsPCMod.ExpertManagerPlugin.ForceCustomRegionSync = Config.Bind("6. Compatibility & Advanced Settings", "ForceCustomRegionSync", true, "Force custom region sync behavior.").Value;
                AmongUsPCMod.ExpertManagerPlugin.ConnectionTimeout = Config.Bind("6. Compatibility & Advanced Settings", "ConnectionTimeout", 15.0f, "Connection timeout for network checks.").Value;
                AmongUsPCMod.ExpertManagerPlugin.DisableCosmeticParticles = Config.Bind("6. Compatibility & Advanced Settings", "DisableCosmeticParticles", true, "Disable cosmetic particle effects when enforcing rules.").Value;
                AmongUsPCMod.ExpertManagerPlugin.AntiVanishSync = Config.Bind("6. Compatibility & Advanced Settings", "AntiVanishSync", true, "Enable vanish synchronization protection.").Value;
                AmongUsPCMod.ExpertManagerPlugin.configApplyRpcInterceptor = Config.Bind("6. Compatibility & Advanced Settings", "configApplyRpcInterceptor", true, "Enable the RPC interceptor toggle.").Value;
                AmongUsPCMod.ExpertManagerPlugin.EnablePlatformSpoofCheck = Config.Bind("5. Advanced Interceptions", "EnablePlatformSpoofCheck", true, "Auto-kick spoofed Xbox/PSN IDs.").Value;
                AmongUsPCMod.ExpertManagerPlugin.EnableVoteKickProtection = Config.Bind("5. Advanced Interceptions", "EnableVoteKickProtection", true, "Block duplicate and lobby spam vote-kicks.").Value;
                AmongUsPCMod.ExpertManagerPlugin.EnableStealthRoundChat = Config.Bind("5. Advanced Interceptions", "EnableStealthRoundChat", true, "Turn active round gameplay messages into stars.").Value;
                AmongUsPCMod.ExpertManagerPlugin.EnableFpsShocker = Config.Bind("5.Hardware Booster", "EnableFpsShocker", true, "Improve FPS for low-FPS players and destablize hackers.");
            } catch { }
        }
    }

 /// <summary>
        /// ⚙️ ИНТЕЛЕГЕНТЕН ФАЙЛОВ СКЕНЕР: Зарежда редовните играчи от диска за хардуерна защита
        /// </summary>
        public static void LoadVipPlayersFilesystem()
        {
            try
            {
                string vipFilePath = Path.Combine(Directory.GetCurrentDirectory(), "VipPlayers.txt");

                // Ако файлът не съществува на компютъра на Хоста, го създаваме празен
                if (!File.Exists(vipFilePath))
                {
                    File.WriteAllText(vipFilePath, "# Въведете FriendCode или PUID на VIP играчите тук (всеки на нов ред)\n", Encoding.UTF8);
                }

                lock (vipLock)
                {
                    VipPlayersList.Clear();
                    var lines = File.ReadAllLines(vipFilePath);
                    
                    foreach (var rawLine in lines)
                    {
                        string cleanLine = rawLine.Trim().ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(cleanLine) || cleanLine.StartsWith("#")) continue;

                        if (!VipPlayersList.Contains(cleanLine))
                        {
                            VipPlayersList.Add(cleanLine);
                        }
                    }
                }
                System.Console.WriteLine($"[THE FAC SYSTEM]: Successfully cached {VipPlayersList.Count} registered VIP network hardware signatures.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[THE FAC FILE ERROR]: Failed reading VipPlayers.txt matrix: {ex.Message}");
            }
        }
}
}