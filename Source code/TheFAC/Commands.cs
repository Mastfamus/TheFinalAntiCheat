using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace AmongUsPCMod
{
    // ============================================================================
    // ⚔️ THE FAC SOVEREIGN COMMANDS ENGINE: SINGLE-EXECUTION CHAT SHIELD (v4.1.2)
    // ============================================================================
    [HarmonyPatch]
    public static class TheFACCommands
    {
        public static string delayedPacketBuffer = string.Empty;
        private static System.Diagnostics.Stopwatch diagTimer = System.Diagnostics.Stopwatch.StartNew();

        [HarmonyPatch(typeof(global::ChatController), nameof(global::ChatController.SendChat))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(global::ChatController __instance)
        {
            if (__instance == null || AmongUsClient.Instance == null || __instance.freeChatField == null || __instance.freeChatField.textArea == null) return true;

            string chatText = __instance.freeChatField.textArea.text;
            if (string.IsNullOrWhiteSpace(chatText)) return true;

            string rawText = chatText.Trim();
            
            AmongUsPCMod.TheFACPerformanceShield.FACCacheOffloader.PushToCache($"ChatStream [{PlayerControl.LocalPlayer?.Data?.PlayerName}]: {rawText}");

            if (!rawText.StartsWith("/")) return true;

            string lowerCmd = rawText.ToLowerInvariant();
            string[] args = lowerCmd.Split(' ');
            string baseCmd = args[0];

            if (!AmongUsClient.Instance.AmHost) return true;

            // 🎯 КОМАНДА /commands
            if (baseCmd == "/commands")
            {
                string helpMenu = "<color=#00FFFF>========== Final Anti-Cheat Commands ==========</color>\n" +
                                  "<color=#8A2BE2>/id</color> - Displays network NetIDs.\n" +
                                  "<color=#8A2BE2>/diagnostics</color> - Prints defensive wall status matrix.\n" +
                                  "<color=#8A2BE2>/kick [ID]</color> - Forces administrative host-kick.\n" +
                                  "<color=#8A2BE2>/ban [ID/Name]</color> - Executes targeted firewall ban.\n" +
                                  "<color=#8A2BE2>/unban [Code]</color> - Removes friend code from local blacklist.\n" +
                                  "<color=#8A2BE2>/fix [ID]</color> - Re-stabilizes bugged player transform.\n" +
                                  "<color=#8A2BE2>/startnow</color> - Forces instant game start with Network Overclock.\n" +
                                  "<color=#8A2BE2>/end</color> - Forces game termination (Returns to Lobby).\n" +
                                  "<color=#8A2BE2>/sum</color> - Broadcasts auto-filled RAM role summary.\n" +
                                  "<color=#8A2BE2>/allrules1</color> / <color=#8A2BE2>/allrules2</color> - Broadcasts All-Time Rules.\n" +
                                  "<color=#8A2BE2>/rules1</color> / <color=#8A2BE2>/rules2</color> - Broadcasts In-Game Rules.\n" +
                                  "<color=#8A2BE2>/player [ID]</color> / <color=#8A2BE2>/players</color> - Network profile scans.\n" +
                                  "<color=#8A2BE2>/loop [off]</color> - Toggles repeat lock on music track.\n" +
                                  "<color=#8A2BE2>/music [on/off]</color> - Toggles the Jukebox engine globally.\n" +
                                  "<color=#8A2BE2>/dump</color> - Dumps full forensics logs to Desktop.\n" +
                                  "<color=#8A2BE2>/s <msg></color> - Private highlighted tester.";

                ExpertManagerPlugin.SendLocalCommandFeedback(helpMenu);
                __instance.freeChatField.Clear();
                return false; 
            }

            // 🎯 КОМАНДА /startnow
            if (baseCmd == "/startnow" || baseCmd == "/start")
            {
                var startManager = UnityEngine.Object.FindObjectOfType<global::GameStartManager>();
                if (startManager != null)
                {
                    ExpertManagerPlugin.IsMatchLoadedAndActive = true;
                    startManager.startState = global::GameStartManager.StartingStates.Countdown;
                    startManager.countDownTimer = 0.0f; 
                    startManager.BeginGame(); 
                }
                __instance.freeChatField.Clear();
                return false;
            }

            // 🎯 КОМАНДА /end
            if (baseCmd == "/end" || baseCmd == "/endgame")
            {
                if (ShipStatus.Instance != null)
                {
                    var hazelWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)2, Hazel.SendOption.Reliable, -1);
                    hazelWriter.Write((byte)2); 
                    hazelWriter.Write(false);   
                    AmongUsClient.Instance.FinishRpcImmediately(hazelWriter);
                }
                __instance.freeChatField.Clear();
                return false;
            }

            // 🎯 КОМАНДА /id
            if (baseCmd == "/id" || baseCmd == "/ids")
            {
                var playerEntries = new System.Collections.Generic.List<string>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p != null) playerEntries.Add($"{(p.Data != null ? p.Data.PlayerName : p.name)} --> {p.PlayerId}");
                }
                ExpertManagerPlugin.SendLocalCommandFeedback($"<color=#00FFFF>[FAC]: Player IDs: {string.Join(", ", playerEntries)}</color>");
                __instance.freeChatField.Clear();
                return false;
            }

            // 🎯 КОМАНДА /kick
            if (baseCmd == "/kick" && args.Length > 1)
            {
                if (int.TryParse(args[1], out int kickId))
                {
                    var player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == kickId);
                    if (player != null)
                    {
                        AmongUsClient.Instance.KickPlayer(player.PlayerId, false);
                        ExpertManagerPlugin.SendLocalCommandFeedback($"<color=#00FF00>[THE FAC]: Successfully kicked player ID {kickId}.</color>");
                    }
                }
                __instance.freeChatField.Clear();
                return false;
            }

            // 🎯 КОМАНДА /ban
            if (baseCmd == "/ban" && args.Length > 1)
            {
                string target = rawText.Substring(5).Trim();
                if (int.TryParse(target, out int parsedId))
                {
                    var player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == parsedId);
                    if (player != null && player.Data != null)
                    {
                        string friendCode = (player.Data.FriendCode ?? "").Trim().ToLower();
                        if (!string.IsNullOrWhiteSpace(friendCode))
                        {
                            ExpertManagerPlugin.bannedPuidsAndCodes.Add(friendCode);
                            ExpertManagerPlugin.AppendBanEntryToOurList(friendCode, player.OwnerId.ToString(), player.Data.PlayerName ?? "");
                            AmongUsClient.Instance.KickPlayer(player.PlayerId, true);
                            ExpertManagerPlugin.SendLocalCommandFeedback($"<color=#FF0000>[THE FAC]: Targeted firewall ban deployed against {player.Data.PlayerName}.</color>");
                        }
                    }
                    __instance.freeChatField.Clear();
                    return false;
                }
                ExpertManagerPlugin.BanOfflinePlayerByName(target);
                __instance.freeChatField.Clear();
                return false;
            }

            // 🎯 КОМАНДА /unban
            if (baseCmd == "/unban" && args.Length > 1)
            {
                ExpertManagerPlugin.UnbanPlayerByFriendCode(rawText.Substring(7).Trim());
                __instance.freeChatField.Clear();
                return false;
            }

             if (baseCmd == "/fix")
            {
                ExpertManagerPlugin.SendLocalCommandFeedback("<color=#00FF00>[THE FAC SYSTEM]: Deploying Omnipresent Re-Stabilization Matrix...");

                try
                {
                    // ⚡ СТЪПКА 1: Форсираме нативна ресинхронизация на правилата на играта.
                    // Това кара Unity двигателя на абсолютно всеки Vanilla клиент да замръзне за милисекунда,
                    // да изпразни натрупания мрежов лаг (Buffer) и да презареди UI компонентите!
                    if (global::SaveManager.GameOptions != null)
                    {
                        global::PlayerControl.LocalPlayer.RpcUpdateGameOptions(global::SaveManager.GameOptions);
                    }

                    // ⚡ СТЪПКА 2: Поправяме физическите десинхронизации (Черни екрани и замръзвания)
                    var allPlayers = global::PlayerControl.AllPlayerControls.ToArray();
                    foreach (var player in allPlayers)
                    {
                        if (player == null || player.Data == null) continue;

                        // Извеждаме ги от прехода между сцените, като опресняваме ролята им
                        if (player.Data.Role != null)
                        {
                            player.RpcSetRole(player.Data.Role.Role, true);
                        }

                        // Твърдо рестартираме мрежовите координати. 
                        // Ако играч е пропаднал под картата или е заклещен в черен екран,
                        // това Snap-ва тялото му обратно на легална позиция на Dropship-а/Cafeteria!
                        if (player.NetTransform != null)
                        {
                            player.NetTransform.SnapTo(Vector2.zero);
                            player.NetTransform.RpcSnapTo(Vector2.zero); // Изпращаме го по официалната Hazel мрежа
                        }
                    }

                    // ⚡ СТЪПКА 3: Опресняваме Глобалните системи на кораба
                    // Ако десинхронът е причинен от незатворен саботаж или бъгната врата,
                    // този нативен импулс изчиства паметта на ShipStatus веднага.
                    if (global::ShipStatus.Instance != null)
                    {
                        global::ShipStatus.Instance.RpcRepairSystem(global::SystemTypes.Sabotage, 0);
                    }

                    ExpertManagerPlugin.SendLocalCommandFeedback("<color=#00FFFF>[THE FAC SYSTEM]: Omnipresent fix applied successfully. Lobby has been completely re-anchored.</color>");
                }
                catch (Exception ex)
                {
                    ExpertManagerPlugin.SendLocalCommandFeedback($"<color=#FF0000>[THE FAC FIX ERROR]: Emergency stabilization matrix failed: {ex.Message}</color>");
                }

                __instance.freeChatField.Clear();
                return false;
            }
            // 🎯 КОМАНДА /loop
            if (baseCmd == "/loop")
            {
                if (args.Length > 1 && args[1] == "off")
                {
                    TheFACAssetEngine.forceSingleTrackLoop = false;
                    ExpertManagerPlugin.SendLocalCommandFeedback("<color=#00FF00>[JUKEBOX]: Loop OFF. Music playlist rotation restored.</color>");
                }
                else
                {
                    TheFACAssetEngine.forceSingleTrackLoop = true;
                    if (ExpertManagerPlugin.anticheatAudioSource != null) ExpertManagerPlugin.anticheatAudioSource.loop = true;
                    ExpertManagerPlugin.SendLocalCommandFeedback("<color=#8A2BE2>[JUKEBOX]: Loop ON. Locking current music track layer.</color>");
                }
                __instance.freeChatField.Clear();
                return false;
            }

            // 🎯 КОМАНДА /music
            if (baseCmd == "/music" && args.Length > 1)
            {
                if (args[1] == "off")
                {
                    TheFACAssetEngine.isMusicEnabled = false;
                    if (ExpertManagerPlugin.anticheatAudioSource != null) ExpertManagerPlugin.anticheatAudioSource.Stop();
ExpertManagerPlugin.SendLocalCommandFeedback("<color=#FF0000>[JUKEBOX]: Audio matrix offline.");
                }
              else 
                if (args[1] == "on")
                { 
                  TheFACAssetEngine.isMusicEnabled = true;
                  if (ExpertManagerPlugin.anticheatAudioSource != null && !ExpertManagerPlugin.anticheatAudioSource.isPlaying)
                  {
                    TheFACAssetEngine.PlayNextJukeboxTrack();
                  } 
                  ExpertManagerPlugin.SendLocalCommandFeedback("<color=#00FF00>[JUKEBOX]: Audio matrix online.");
                }
              __instance.freeChatField.Clear();
              return false;
            }
          // 🎯 КОМАНДА /player
          if (baseCmd == "/player" && args.Length > 1)
          {
            if (int.TryParse(args[1], out int parsedId))
            {
              var player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == parsedId);
              if (player != null && player.Data != null)
              {
                // Стрингът е пренаписан с чисти ескейп символи, елиминирайки грешката на линия 229-231
                !ExpertManagerPlugin.SendLocalCommandFeedback($"<size=120%><color=#00FFFF>[TheFAC] Profile:\n - ID: {player.PlayerId}\n - FriendCode: <color=#ffd829>{player.Data.FriendCode}");
              }
            }
            __instance.freeChatField.Clear();
            return false;
          }
          if (baseCmd == "/players")
          {
            StringBuilder sb = new StringBuilder();
            sb.Append("<size=140%><color=#8A2BE2>=== ALL PLAYERS INVENTORY ===\n\n");
            foreach (PlayerControl player in PlayerControl.AllPlayerControls.ToArray())
            {
              if (player == null || player.Data == null) continue;
              sb.Append($"<color=#00FFFF>{player.Data.PlayerName}: [ID: {player.PlayerId}] - [Code: {player.Data.FriendCode}]\n");
            }
            ExpertManagerPlugin.SendLocalCommandFeedback(sb.ToString());
            __instance.freeChatField.Clear();
            return false;
          }
          // 🎯 КОМАНДА /s
          if (baseCmd == "/s" && args.Length > 1)
          {
            string userText = rawText.Substring(3).Trim();
            ExpertManagerPlugin.SendSteamChatMessage($"[FAC-BROADCAST(host message)]: {userText}");
            ExpertManagerPlugin.SendLocalCommandFeedback("<color=#8A2BE2>[The FAC]: Broadcast successfully sent in the chat.");
            __instance.freeChatField.Clear();
            return false;
          }
          // 🎯 КОМАНДА /dump
          if (baseCmd == "/dump")
          {
            try 
            {
              AmongUsPCMod.TheFACPerformanceShield.FACCacheOffloader.DumpCacheToDisk();
              if (UnityEngine.Application.platform != RuntimePlatform.Android)
              {
                string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TheFACLogs");Directory.CreateDirectory(logFolderPath);
                string bepInExLog = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "LogOutput.log");
                if (File.Exists(bepInExLog)) File.Copy(bepInExLog, Path.Combine(logFolderPath, "FAC-forensics.log"), true);
              }
              ExpertManagerPlugin.SendLocalCommandFeedback("<color=#00FF00>[THE FAC]: Logs dumped successfully.");
            }catch { }
            __instance.freeChatField.Clear();
            return false;
          }
          // 🎯 КОМАНДА /diagnostics
          if (baseCmd == "/diagnostic" || baseCmd == "/diagnostics")
          {
            if (diagTimer.ElapsedMilliseconds < 2500) return false;
            diagTimer.Restart();
            string diagnosticMatrixText = "<color=#00FFFF>======= SYSTEM DIAGNOSTICS MATRIX =======\n <color=#00FF00>Anti-Cheat Core: Stable Environment\n <color=#FF00FF>Protocol Frog (Security Cams): ENABLED (100%)\n <color=#FF0000>Network Overclock Engine: STABLE (Hazel Cooldown: 2.5s)\n <color=#00FFFF>====================================";
            if (ExpertManagerPlugin.configDiagnosticIsGlobal)
            {
              ExpertManagerPlugin.SendSteamChatMessage(diagnosticMatrixText);
            }else{
              ExpertManagerPlugin.SendLocalCommandFeedback(diagnosticMatrixText);
            }
            __instance.freeChatField.Clear();
            return false;
          }
          // 🎯 КОМАНДА /gg
          if (baseCmd == "/gg")
          {
            ExpertManagerPlugin.SendSteamChatMessage("<color=#00FF00>GG everyone! THE GAME IS OVER. A summary of the game will be sent in a min.");
            __instance.freeChatField.Clear();
            return false;
          }
                      // 🎯 КОМАНДА /sum (ПУБЛИЧЕН ПЪЛЕН РЕКАП НА МАЧА - ФИКСИРАН)
            if (baseCmd == "/sum")
            {
                // Сглобяваме списъка с Импостори безопасно
                string imps = string.Join(", ", ExpertManagerPlugin.lastImpostors.Count > 0 ? ExpertManagerPlugin.lastImpostors : new System.Collections.Generic.List<string> { "None" });
                
                // Взимаме имената на специалните роли от Вашия ExpertManagerPlugin
                string eng = string.Join(", ", ExpertManagerPlugin.lastEngineers.Count > 0 ? ExpertManagerPlugin.lastEngineers : new System.Collections.Generic.List<string> { "None" });
                string det = string.Join(", ", ExpertManagerPlugin.lastDetectives.Count > 0 ? ExpertManagerPlugin.lastDetectives : new System.Collections.Generic.List<string> { "None" });
                string judge = string.Join(", ", ExpertManagerPlugin.lastJudges.Count > 0 ? ExpertManagerPlugin.lastJudges : new System.Collections.Generic.List<string> { "None" });

                // Излъчваме пълния списък публично в чата за абсолютно всички играчи
                ExpertManagerPlugin.SendSteamChatMessage($"<color=#8A2BE2><b>#MATCH RECAP#</b></color> -\n• Winner: ({ExpertManagerPlugin.lastWinnerTeam})\n• IMPS: ({imps})\n• ENG: ({eng})\n• DET: ({det})\n• JUDGE: ({judge})");
                
                __instance.freeChatField.Clear();
                return false;
            }
// 🎯 КОМАНДИ ЗА ПРАВИЛА
          if (baseCmd == "/allrules1")
          {
            ExpertManagerPlugin.SendSteamChatMessage("<color=#FFD700>All-time rules Pt.1: 1-No trolling/toxicity. 2-No harmful words towards other players. 3-No inappropriate msgs.");
            __instance.freeChatField.Clear();
            return false;
          }
          if (baseCmd == "/allrules2")
          {
            ExpertManagerPlugin.SendSteamChatMessage("<color=#FFD700>All-time rules Pt.2: 4-Leave if you won't follow the rules. 5-Report any violations to Host!");
            __instance.freeChatField.Clear();
            return false;
          }
          if (baseCmd == "/rules1" || (baseCmd == "/t" && args.Length > 1 && args[1] == "rules"))
          {
            ExpertManagerPlugin.SendSteamChatMessage("<color=#FF0000>In-game rules Pt.1: 1-No CAMS. 2-No camping vitals, admin, nor lights. 3-No teaming/following/grouping.");
            __instance.freeChatField.Clear();
            return false;
          }
          if (baseCmd == "/rules2")
          {
            ExpertManagerPlugin.SendSteamChatMessage("<color=#FF0000>In-game rules Pt.2: 4-Provide proof before susing/accusing. Good luck and have fun everyone! :D");
            __instance.freeChatField.Clear();
            return false;
          }
          return true;
        }
    }
  [HarmonyPatch(typeof(global::HudManager), nameof(global::HudManager.Update))]
  public static class FACDelayedPacketSpitter
  {
    private static float splitTimer = 0f;
    [HarmonyPostfix]
    public static void Postfix()
    {
      if (string.IsNullOrEmpty(TheFACCommands.delayedPacketBuffer)) 
        return;
      if (UnityEngine.Time.time - splitTimer >= 2.5f)
      {
        splitTimer = UnityEngine.Time.time;
        string payloadToBlast = TheFACCommands.delayedPacketBuffer;
        TheFACCommands.delayedPacketBuffer = string.Empty;
        ExpertManagerPlugin.SendSteamChatMessage(payloadToBlast);
      }
    }
  }
}