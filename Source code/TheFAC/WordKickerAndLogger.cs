using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace AmongUsPCMod
{
    [HarmonyPatch(typeof(global::InnerNet.InnerNetClient), "HandleGameData")]
    public static class PolarNightShield
    {
        private static readonly string bauLogPath = Path.Combine(Application.persistentDataPath, "TheFAC_Data", "AdvancedCheatLogs.txt");

        public static void LogRpcInfo(string info, string tag = "AntiCheat")
        {
            try {
                string cleanInfo = Regex.Replace(info, "<[^>]*>", "").Replace("\n", " ").Replace("\r", " ").Trim();
                string logMark = $"{DateTime.Now:HH:mm} [The FAC][{tag}]: {cleanInfo}";
                File.AppendAllText(bauLogPath, logMark + Environment.NewLine);
                System.Console.WriteLine($"[THEFAC-LOG][{tag}]: {cleanInfo}");
            } catch { }
        }

        [HarmonyPriority(Priority.High)]
        public static bool Prefix(global::InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] Hazel.MessageReader parentReader)
        {
            if (__instance == null || parentReader == null) return true;
            
            try 
            {
                Hazel.MessageReader subReader = Hazel.MessageReader.Get(parentReader);
                byte callId = subReader.Tag;
                
                // 🔒 2. АНТИ-HOST SPOOF SHIELD
                if (!AmongUsClient.Instance.AmHost)
                {
                    if (callId == 2 || callId == 7 || callId == 11 || callId == 29 || callId == 31)
                    {
                        LogRpcInfo($"Blocked illegal Host RPC spoof attempt! CallID: {callId}");
                        subReader.Recycle();
                        return false;
                    }
                }

                // 👿 3. Custom RPC ИНТЕРЦЕПТОР
                if (callId == 101 || callId == 119 || callId == 250)
                {
                    LogRpcInfo($"CRITICAL: Flagged cheat client custom RPC! CallID: {callId}", "ExploitInterceptor");
                    subReader.Recycle();
                    return false;
                }

                // 🐸 4. ЗАСИЧАНЕ НА НЕЗАКОННИ РОЛИ
                if (AmongUsClient.Instance.AmHost && GameData.Instance != null)
                {
                    var senderPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == parentReader.Tag);
                    if (senderPlayer != null && senderPlayer.Data != null)
                    {
                        if ((callId == 11 || callId == 12) && !senderPlayer.Data.Role.IsImpostor && senderPlayer.Data.Role.Role != (AmongUs.GameOptions.RoleTypes)4)
                        {
                            LogRpcInfo($"Flagged illegal Vent RPC! Player: {senderPlayer.Data.PlayerName}", "RoleAntiCheat");
                            subReader.Recycle();
                            return false;
                        }

                        if (callId == 32 && senderPlayer.Data.Role.Role != (AmongUs.GameOptions.RoleTypes)5)
                        {
                             LogRpcInfo($"Flagged illegal Angel Shield RPC! Player: {senderPlayer.Data.PlayerName}", "RoleAntiCheat");
                            subReader.Recycle();
                            return false;
                        }
                    }
                }

                

                subReader.Recycle();
            } 
            catch { }
            return true;
        }
    }

    [HarmonyPatch(typeof(global::ShipStatus), nameof(global::ShipStatus.Begin))]
    public static class SecureLobbyPrivacyGuardPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
            // 🔒 ПОПРАВЕНО (ред 162): Използва се класическият поддържан метод ChangeLobbyPublic!
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
              AmongUsClient.Instance.ChangeGamePublic(false);
               System.Console.WriteLine("[The FAC]: Room successfully locked to private state.");
            }
        }
    catch (System.Exception ex)
            {
                System.Console.WriteLine("[The FAC Warning]:Static privacy lock omitted:" + ex.Message);
         }
        }    
    }
    [HarmonyPatch(typeof(global::HudManager), "Update")]
    public static class FACNameplateGreyerPatch
    {
        [HarmonyPostfix]
        public static void Postfix(global::HudManager __instance)
        {
            bool isMatchActive = AmongUsClient.Instance != null && AmongUsClient.Instance.InOnlineScene && ShipStatus.Instance != null;
            bool isMeetingOpen = MeetingHud.Instance != null;

            if (isMatchActive && !isMeetingOpen)
            {
                try {
                    if (PlayerControl.AllPlayerControls == null) return;
                    foreach (var player in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (player == null || player.AmOwner || player.cosmetics == null || player.cosmetics.nameText == null) continue;
                        
                        string pName = player.Data != null ? player.Data.PlayerName : player.name;
                        byte clientId = player.PlayerId;

                        if (TheFACNetworkBridge.DetectedModdedPlayers.TryGetValue(clientId, out string modType))
                        {
                            switch (modType)
                            {
                                case "BAU":
                                    player.cosmetics.nameText.text = $"<color=#00FFFF>[BAU] {pName}</color>";
                                    break;
                                case "TOU":
                                    player.cosmetics.nameText.text = $"<color=#FFD700>[ToUR] {pName}</color>";
                                    break;
                                case "SUB":
                                    player.cosmetics.nameText.text = $"<color=#00FF00>[SUB] {pName}</color>";
                                    break;
                                case "HYDRA":
                                    player.cosmetics.nameText.text = $"<color=#8A2BE2>[HYDRA] {pName}</color>";
                                    break;
                                case "BAN_MOD":
player.cosmetics.nameText.text = $"<color=#FF1493>[BAN-MOD] {pName}";
break;
case "AUNLOCKER":player.cosmetics.nameText.text = $"<color=#FF4500>[A-UNLOCK] {pName}";
break;
case "KN_SICKO":player.cosmetics.nameText.text = $"<color=#FC0000>[ALERT-SICKO] {pName}";
break;
case "AUM":player.cosmetics.nameText.text = $"<color=#FC0000>[ALERT-AUM] {pName}";
break;
case "MMC_EXPLOIT":player.cosmetics.nameText.text = $"<color=#FC0000>[ALERT-MMC] {pName}";
break;
default:player.cosmetics.nameText.text = $"<color=#9AA0A6>{pName}";
break;
}
}
else {
    player.cosmetics.nameText.text = $"<color=#9AA0A6>{pName}";
    }
    }
    } catch { }
    }
   }
  }
}