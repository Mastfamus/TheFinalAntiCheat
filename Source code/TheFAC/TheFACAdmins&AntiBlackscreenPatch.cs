using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AmongUsPCMod
{
    // ============================================================================
    // 👑 THE FAC ADMIN PROXY ENGINE: SECURE FIREWALL DELEGATION (v5.1.0)
    // ============================================================================
    public static class FACAdminProxyEngine
    {
        /// <summary>
        /// 🛡️ ЦЕНТРАЛЕН АДМИН ПАРСЕР: Дава права на Админите за /kick, /ban, /sum, /gg, /rules
        /// </summary>
        internal static bool CheckAndExecuteAdminProxyCommand(PlayerControl sender, string chatText)
        {
            if (sender == null || sender.Data == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;

            string rawText = chatText.Trim();
            string lowerCmd = rawText.ToLowerInvariant();
            string[] args = lowerCmd.Split(' ');
            string baseCmd = args[0];

            string senderCode = (sender.Data.FriendCode ?? "").Trim().ToLowerInvariant();
            string senderPuid = sender.OwnerId.ToString();

            bool isVerifiedAdmin = false;
            lock (ExpertManagerPlugin.immuneLock)
            {
                if (ExpertManagerPlugin.ImmunePlayersList.Contains(senderCode) || ExpertManagerPlugin.ImmunePlayersList.Contains(senderPuid))
                {
                    isVerifiedAdmin = true;
                }
            }

            if (!isVerifiedAdmin) return true;

            // 🎯 АДМИН КОМАНДА: /start
            if (baseCmd == "/start" || baseCmd == "/startnow")
            {
                var startManager = UnityEngine.Object.FindObjectOfType<global::GameStartManager>();
                if (startManager != null)
                {
                    ExpertManagerPlugin.IsMatchLoadedAndActive = true;
                    startManager.startState = global::GameStartManager.StartingStates.Countdown;
                    startManager.countDownTimer = 0.0f;
                    startManager.BeginGame();
                }
                return false; 
            }
            //Admin command /id
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


            // 🎯 АДМИН КОМАНДА: /kick [ID]
            if (baseCmd == "/kick" && args.Length > 1)
            {
                if (int.TryParse(args[1], out int kickId))
                {
                    var targetPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == kickId);
                    if (targetPlayer != null)
                    {
                        // 🔒 Проверка на йерархията!
                        if (!FACAdminTagsMatrix.ValidateAdminAuthorityOverTarget(sender, targetPlayer)) return false;

                        AmongUsClient.Instance.KickPlayer(targetPlayer.PlayerId, false);
                    }
                }
                return false;
            }

            // 🎯 АДМИН КОМАНДА: /ban [ID]
            if (baseCmd == "/ban" && args.Length > 1)
            {
                if (int.TryParse(args[1], out int banId))
                {
                    var targetPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == banId);
                    if (targetPlayer != null && targetPlayer.Data != null)
                    {
                        // 🔒 Проверка на йерархията!
                        if (!FACAdminTagsMatrix.ValidateAdminAuthorityOverTarget(sender, targetPlayer)) return false;

                        string targetCode = (targetPlayer.Data.FriendCode ?? "").Trim().ToLower();
                        if (!string.IsNullOrWhiteSpace(targetCode))
                        {
                            ExpertManagerPlugin.bannedPuidsAndCodes.Add(targetCode);
                            ExpertManagerPlugin.AppendBanEntryToOurList(targetCode, targetPlayer.OwnerId.ToString(), targetPlayer.Data.PlayerName ?? "");
                            AmongUsClient.Instance.KickPlayer(targetPlayer.PlayerId, true);
                        }
                    }
                }
                return false;
            }

            // 🎯 АДМИН КОМАНДА: /gg
            if (baseCmd == "/gg")
            {
                ExpertManagerPlugin.SendSteamChatMessage("<color=#00FF00>GG everyone! THE GAME IS OVER. A summary of the game will be sent in a min.</color>");
                return false;
            }

            // 🎯 АДМИН КОМАНДА: /sum
            if (baseCmd == "/sum")
            {
                string imps = string.Join(", ", ExpertManagerPlugin.lastImpostors.Count > 0 ? ExpertManagerPlugin.lastImpostors : new System.Collections.Generic.List<string> { "None" });
                string eng = string.Join(", ", ExpertManagerPlugin.lastEngineers.Count > 0 ? ExpertManagerPlugin.lastEngineers : new System.Collections.Generic.List<string> { "None" });
                string det = string.Join(", ", ExpertManagerPlugin.lastDetectives.Count > 0 ? ExpertManagerPlugin.lastDetectives : new System.Collections.Generic.List<string> { "None" });
                string judge = string.Join(", ", ExpertManagerPlugin.lastJudges.Count > 0 ? ExpertManagerPlugin.lastJudges : new System.Collections.Generic.List<string> { "None" });

                ExpertManagerPlugin.SendSteamChatMessage($"<color=#8A2BE2><b>#MATCH RECAP#</b></color> -\n• Winner: ({ExpertManagerPlugin.lastWinnerTeam})\n• IMPS: ({imps})\n• ENG: ({eng})\n• DET: ({det})\n• JUDGE: ({judge})");
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class FACPreIntroSafetySwitch
    {
        /// <summary>
        /// 🔥 АВТОМАТИЧЕН ФИКСАТОР НА ЧЕРНИ ЕКРАНИ: Презарежда ролите точно преди "Shh" екрана (100% Vanilla Safe)
        /// </summary>
        [HarmonyPatch(typeof(global::GameStartManager), nameof(global::GameStartManager.BeginGame))]
        [HarmonyPrefix]
        public static void BeginGame_Prefix()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            try
            {
                var allPlayers = global::PlayerControl.AllPlayerControls.ToArray();
                foreach (var player in allPlayers)
                {
                    if (player == null || player.Data == null) continue;

                    // Изпращаме нативен рефреш на ролята - това извежда Unity от черния десинхронизационен екран
                    if (player.Data.Role != null)
                    {
                        player.RpcSetRole(player.Data.Role.Role, true);
                    }

                    if (player.NetTransform != null)
                    {
                        player.NetTransform.SnapTo(Vector2.zero);
                    }
                }
            }
            catch { }
        }
    }
}