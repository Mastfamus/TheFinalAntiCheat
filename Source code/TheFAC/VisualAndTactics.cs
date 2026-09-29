using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using InnerNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using TMPro;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🎨 СТЪПКА 4: ОБЕДИНЕН ЦВЕТЕН ИНТЕРФЕЙС И ХАРДУЕРНИ ПЛАТФОРМИ ПОД КРАКАТА (v17.0.0)
    // ============================================================================
    [HarmonyPatch(typeof(global::PlayerPhysics), nameof(global::PlayerPhysics.FixedUpdate))]
    public static class NameplateInfoPatch
    {
        private static float lastRefreshTimestamp = 0f;

        [HarmonyPostfix]
        public static void Postfix(global::PlayerPhysics __instance)
        {
            if (__instance == null || __instance.myPlayer == null || __instance.myPlayer.Data == null || __instance.myPlayer.cosmetics == null || __instance.myPlayer.cosmetics.nameText == null) return;
            if (global::LobbyBehaviour.Instance == null || AmongUsClient.Instance == null) return;

            var playerControl = __instance.myPlayer;

            try 
            {
                if (playerControl.Data.DefaultOutfit != null && playerControl.Data.DefaultOutfit.ColorId == 18 && AmongUsClient.Instance.AmHost)
                {
                    System.Console.WriteLine($"[THE FAC SECURITY]: Blocked live Fortegreen shift exploit for Player ID: {playerControl.PlayerId}");
                    global::AmongUsClient.Instance.KickPlayer(playerControl.PlayerId, false);
                    return;
                }

                if (Time.time - lastRefreshTimestamp < 1.2f) return;
                lastRefreshTimestamp = Time.time;

                ApplyDynamicVisuals(playerControl);
            } 
            catch { }
        }

        public static void RefreshAllNameplates()
        {
            try {
                if (global::PlayerControl.AllPlayerControls == null) return;
                var currentList = global::PlayerControl.AllPlayerControls;
                for (int i = 0; i < currentList.Count; i++)
                {
                    var player = currentList[i];
                    if (player != null) ApplyDynamicVisuals(player);
                }
            } catch { }
        }

        private static void ApplyDynamicVisuals(global::PlayerControl playerControl)
        {
            if (playerControl == null || playerControl.Data == null || playerControl.cosmetics == null || playerControl.cosmetics.nameText == null) return;

            try {
                int playerId = (int)playerControl.PlayerId;
                string originalName = !string.IsNullOrWhiteSpace(playerControl.Data.PlayerName) ? playerControl.Data.PlayerName : playerControl.name;
                
                uint level = (uint)GetPlayerLevel(playerControl);
                string modTag = DetectModTag(playerControl, originalName);

                var clientData = AmongUsClient.Instance?.allClients?.ToArray().FirstOrDefault(cd => cd != null && cd.Character != null && cd.Character.PlayerId == playerControl.PlayerId);
                string rawPlatformName = (clientData != null && clientData.PlatformData != null) ? clientData.PlatformData.Platform.ToString() : "PC";
                string friendCode = playerControl.Data.FriendCode ?? "No-Code";

                bool isStarlightClient = rawPlatformName.ToLowerInvariant().Contains("starlight") || friendCode.ToLowerInvariant().Contains("starlight");
                string platformTag = "";

                if (isStarlightClient)
                {
                    platformTag = "<color=#8A2BE2><b>[Starlight PC]</b></color>";
                }
                else
                {
                    switch (clientData?.PlatformData?.Platform)
                    {
                        case Platforms.StandaloneSteamPC:
                            platformTag = "<color=#4391CD>[Steam]</color>";
                            break;
                        case Platforms.StandaloneEpicPC:
                            platformTag = "<color=#905CDA>[Epic]</color>";
                            break;
                        case Platforms.StandaloneWin10:
                            platformTag = "<color=#0078d4>[MS Store]</color>";
                            break;
                        case Platforms.Android:
                            platformTag = "<color=#1EA21A>[Android]</color>";
                            break;
                        case Platforms.Switch:
                            platformTag = "<color=#FF0000>Nintendo</color><color=#4391CD>Switch</color>";
                            break;
                        case Platforms.Playstation:
                            platformTag = "<color=#0014b4>[PlayStation]</color>";
                            break;
                        case Platforms.IPhone:
                            platformTag = "<color=#E35F5F>[iOS]</color>";
                            break;
                        default:
                            platformTag = $"<color=#A9A9A9>[{rawPlatformName.Replace("Standalone", "")}]</color>";
                            break;
                    }
                }

                bool isMatchActive = AmongUsClient.Instance != null && AmongUsClient.Instance.InOnlineScene && ShipStatus.Instance != null;
                string displayText;

                if (isMatchActive)
                {
                    if (ExpertManagerPlugin.IsInActiveGameplayRound && global::MeetingHud.Instance == null)
                    {
                        displayText = string.IsNullOrEmpty(modTag)
                            ? $"<color=#9AA0A6>[ID: {playerId} | Lv.{level}]</color>\n<color=#9AA0A6>???</color>"
                            : $"{modTag}<color=#9AA0A6>[ID: {playerId} | Lv.{level}]</color>\n<color=#9AA0A6>???</color>";
                    }
                    else
                    {
                        displayText = string.IsNullOrEmpty(modTag) 
                            ? $"{originalName}\n<size=80%><b>{platformTag} <color=#8A2BE2>{friendCode}</color></b></size>" 
                            : $"{modTag}{originalName}\n<size=80%><b>{platformTag} <color=#8A2BE2>{friendCode}</color></b></size>";
                    }
                }
                else
                {
                    displayText = string.IsNullOrEmpty(modTag)
                        ? $"<color=#8A2BE2>[ID: {playerId} | Lv.{level}]</color>\n{originalName}\n<size=24%><b>{platformTag} <color=#8A2BE2>{friendCode}</color></b></size>"
                        : $"{modTag}<color=#8A2BE2>[ID: {playerId} | Lv.{level}]</color>\n{originalName}\n<size=24%><b>{platformTag} <color=#8A2BE2>{friendCode}</color></b></size>";
                }

                playerControl.cosmetics.nameText.text = displayText;
            } catch { }
        }

        private static string DetectModTag(global::PlayerControl player, string originalName)
        {
            try {
                if (player == null || player.Data == null) return "";
                string haystack = ((originalName ?? "") + " " + (player.Data.FriendCode ?? "") + " " + (player.name ?? "")).ToLowerInvariant();

                if (haystack.Contains("finalsuspect")) return "<color=#8A2BE2>[FinalSuspect]</color> ";
                if (haystack.Contains("ehr")) return "<color=#0000FF>[EHR]</color> ";
                if (haystack.Contains("betteramongus")) return "<color=#00FF00>[BetterAmongUs]</color> ";
                if (haystack.Contains("crewlink") || haystack.Contains("bettercrewlink")) return "<color=#FFFF00>[CrewLink]</color> ";
                if (haystack.Contains("aunlocker")) return "<color=#FFA500>[AUnlocker]</color> ";
                if (haystack.Contains("impostorstats")) return "<color=#FFB6C1>[ImpostorStats]</color> ";
                if (haystack.Contains("glance")) return "<color=#32CD32>[Glance]</color> ";
                
                if (haystack.Contains("neonmenu") || haystack.Contains("hydramenu") || haystack.Contains("elysium") || haystack.Contains("emm") || haystack.Contains("sickomenu") || haystack.Contains("malummenu"))
                    return "<color=#FF0000>[HACKER]</color> ";
            } catch { }
            return "";
        }

        public static int GetPlayerLevel(global::PlayerControl player)
        {
            try {
                if (player == null || player.Data == null) return 1;
                var dataType = player.Data.GetType();
                string[] propertyNames = { "Level", "PlayerLevel", "AccountLevel" };

                foreach (var propertyName in propertyNames)
                {
                    var property = dataType.GetProperty(propertyName);
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
            } catch { }
            return 1;
        }
    }

    // ============================================================================
    // ⚔️ СТЪПКА 5: ВОЕННИТЕ КОДОВЕ И ПРОТОКОЛ ЖАБА ПО ВРЕМЕ НА СЪБРАНИЯ
    // ============================================================================
    [HarmonyPatch(typeof(global::ChatController), nameof(global::ChatController.AddChat))]
    public static class ToxicChatHandler
    {
        private static System.DateTime lastMessageTime = System.DateTime.MinValue;

        [HarmonyPrefix]
        public static bool Prefix(global::ChatController __instance, [HarmonyArgument(0)] global::PlayerControl sender, [HarmonyArgument(1)] ref string chatText)
        {
if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || sender == null || sender.AmOwner)
 return true;
 if (string.IsNullOrWhiteSpace(chatText)) 
 return true;
 try 
 {
    string cleanMessage = chatText.Trim().ToLower();
    bool isMeetingActive = global::MeetingHud.Instance != null;
    bool isCooldownOver = (System.DateTime.Now - lastMessageTime).TotalSeconds >= 3.0;
    if (cleanMessage.Contains("killnetwork") || cleanMessage.Contains("crash") || cleanMessage.Contains("freeze")) 
    {
        if (ExpertManagerPlugin.configEnableTacticalCodes && isMeetingActive && isCooldownOver) 
        {
            ExpertManagerPlugin.SendSteamChatMessage("<color=#000000>[CRITICAL]: Command we have a code Black,we are under extreme cheat attack");
            lastMessageTime = System.DateTime.Now;
            }
            AmongUsClient.Instance.KickPlayer(sender.PlayerId, true);
            ExpertManagerPlugin.PlayReactorAlarm("cheat");
            return false;
            }
            // Поправка: Четем списъка с малка буква "L", съобразно дефиницията му в ядрото!
        foreach (string forbiddenWord in ExpertManagerPlugin.WordBlackList) 
        {
            if (cleanMessage.Contains(forbiddenWord)) 
            {
            if (ExpertManagerPlugin.configEnableTacticalCodes && isMeetingActive && isCooldownOver) 
            {
                ExpertManagerPlugin.SendSteamChatMessage("<color=#FF0000>[ALERT]: Command we have a code red, begin intelligence gathering ");
                lastMessageTime = System.DateTime.Now;
                }
                AmongUsClient.Instance.KickPlayer(sender.PlayerId, false);
                ExpertManagerPlugin.PlayReactorAlarm("hack");
                return false;
                }
                }
                } catch { }
                return true;
                }
                }
                [HarmonyPatch(typeof(global::MapConsole), nameof(global::MapConsole.Use))]
                public static class ProtocolFrogPatch
                {
                    private static System.DateTime lastFrogMessageTime = System.DateTime.MinValue;
                    [HarmonyPrefix]
                    public static bool Prefix(global::MapConsole __instance)
                    {
                        if (__instance == null || __instance.gameObject == null || !ExpertManagerPlugin.configProtocolFrog) 
                        return true;
                        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) 
                        return true;
                        try {
                        global::PlayerControl violator = global::PlayerControl.LocalPlayer;
                        if (violator != null && !violator.AmOwner) 
                        {
                            bool isMeetingActive = global::MeetingHud.Instance != null;
                            bool isCooldownOver = (System.DateTime.Now - lastFrogMessageTime).TotalSeconds >= 3.0;
                            if (ExpertManagerPlugin.configEnableTacticalCodes && isMeetingActive && isCooldownOver) 
                            {
                                ExpertManagerPlugin.SendSteamChatMessage("<color=#00FF00>[SECURITY]: Command we have a code green, initialize protocol Frog...");
                                lastFrogMessageTime = System.DateTime.Now;
                                }
                                AmongUsClient.Instance.KickPlayer(violator.PlayerId, false);
                                ExpertManagerPlugin.PlayReactorAlarm("cam");
                                return false;
                                }
                                } catch { }
                                return true;
                                }
                                }
                                [HarmonyPatch(typeof(global::HudManager), nameof(global::HudManager.Update))]
                                public static class LoadingScreenTextPatch
                                {
                                    [HarmonyPostfix]
                                    public static void Postfix(global::HudManager __instance)
                                    {
                                    if (__instance == null || AmongUsClient.Instance == null) return;
                                    try {
                                        NameplateInfoPatch.RefreshAllNameplates();
                                        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F3)) 
                                        {
                                            string legendMessage = "<color=#00FFFF>========== Final Anti-Cheat Color Legend ==========\n" +
                                            "<color=#8A2BE2>[FinalSuspect] = Purple mod tag\n" +
                                            "<color=#0000FF>[EHR] = Blue admin / EHR tag\n" +
                                            "<color=#00FF00>[BetterAmongUs] = Green mod tag\n" +
                                            "<color=#FFFF00>[CrewLink] = Yellow voice/chat tag\n" +
                                            "<color=#FFA500>[AUnlocker] = Orange unlocker tag\n" +
                                            "<color=#FF0000>[HACKER] = Red banned cheat menu tag\n" +
                                            "<color=#00FFFF>===========================================";
                                            ExpertManagerPlugin.SendLocalCommandFeedback(legendMessage);
                                            ExpertManagerPlugin.PlayReactorAlarm("cheat");
                                            }
                                            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F6) && global::MeetingHud.Instance != null && ExpertManagerPlugin.configEnableF6Meeting && AmongUsClient.Instance.AmHost)
                                            {
                                                var hazelWriter = AmongUsClient.Instance.StartRpcImmediately(global::MeetingHud.Instance.NetId, (byte)4, Hazel.SendOption.Reliable, -1);
                                                hazelWriter.Write((byte)255);
                                                AmongUsClient.Instance.FinishRpcImmediately(hazelWriter);
                                                System.Console.WriteLine("[FAC-ADMIN]: F6 Emergency Meeting Override executed via Hazels.");
                                                }
                                                } catch { }
                                                }
         // ============================================================================
        // 🔮 THE FAC DYNAMIC TRANSLATOR: AUTO-SWITCHING MULTI-LANGUAGE ENGINE
        // ============================================================================
        public static string GetSecureTranslation(string nodeName)
        {
            try 
            {
                // По подразбиране езикът е Английски
                string activeLanguageName = "English"; 

                // Четем нативния Innersloth превключватель за езици
                if (AmongUs.Data.DataManager.Settings != null && AmongUs.Data.DataManager.Settings.Language != null)
                {
                    string systemLang = AmongUs.Data.DataManager.Settings.Language.CurrentLanguage.ToString();
                    
                    if (systemLang.Contains("German")) activeLanguageName = "German";
                    else if (systemLang.Contains("Spanish")) activeLanguageName = "Spanish";
                    else if (systemLang.Contains("French")) activeLanguageName = "French";
                    else if (systemLang.Contains("Italian")) activeLanguageName = "Italian";
                    else if (systemLang.Contains("Portuguese")) activeLanguageName = "Portuguese";
                    else if (systemLang.Contains("Russian")) activeLanguageName = "Russian";
                    else if (systemLang.Contains("Filipino") || systemLang.Contains("Bisaya")) activeLanguageName = "Bisaya"; // 🔮 Себуано / Бисая Тунел! [1]
                    else if (systemLang.Contains("Chinese") || systemLang.Contains("Simplified")) activeLanguageName = "Chinese"; // 🔮 Китайски Мост!
                }

                // Сглобяваме пътя до конкретния XML файл в Steam директорията
                string xmlPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data", "Translations", $"{activeLanguageName}.xml");
                
                if (!File.Exists(xmlPath))
                {
                    // Fallback към Английски при липса на файл
                    xmlPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data", "Translations", "English.xml");
                    if (!File.Exists(xmlPath)) return nodeName;
                }

                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.Load(xmlPath);
                
                System.Xml.XmlNode node = xmlDoc.SelectSingleNode($"//string[@name='{nodeName}']");
                if (node != null) return node.InnerText;
            } 
            catch { }
            
            return nodeName; 
        }
    }
}