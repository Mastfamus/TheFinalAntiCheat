using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using InnerNet;

namespace AmongUsPCMod
{
    // ============================================================================
    // 📢 MEETING DC MONITOR: DIAGNOSTIC REASON ENFORCER (v16.0.5)
    // ============================================================================
    [HarmonyPatch]
    public static class TheFACMeetingMonitor
    {
        [HarmonyPatch(typeof(global::AmongUsClient), nameof(global::AmongUsClient.OnPlayerLeft))]
        [HarmonyPostfix]
        public static void OnPlayerLeft_Postfix(global::AmongUsClient __instance, global::InnerNet.ClientData data, global::DisconnectReasons reason)
        {
            // ⚡ ФИКС 1: Сменено на 'InOnlineScene'
            if (data == null || data.Character == null || !AmongUsClient.Instance.InOnlineScene) return;

            try
            {
                string pName = data.Character.Data.PlayerName;
                string reasonText = "Left the game.";

                if (reason == DisconnectReasons.Error || reason == DisconnectReasons.ClientTimeout)
                {
                    reasonText = "due to an error.";
                }
                else if (reason == DisconnectReasons.Banned)
                {
                    reasonText = "was banned by the server.";
                }

                if (global::HudManager.Instance != null && global::HudManager.Instance.Notifier != null)
                {
                    string fullNotice = $"<color=#CCCCCC>{pName} {reasonText}</color>";
                    global::HudManager.Instance.Notifier.AddDisconnectMessage(fullNotice);
                }

                if (global::MeetingHud.Instance != null && global::MeetingHud.Instance.playerStates != null)
                {
                    var statesList = global::MeetingHud.Instance.playerStates;
                    for (int i = 0; i < statesList.Count; i++)
                    {
                        var state = statesList[i];
                        // ⚡ ФИКС 2: Сменено от 'TargetPlayerId' на нативното 'PlayerId'
                        if (state != null && state.PlayerId == data.Character.PlayerId)
                        {
                            if (reason == DisconnectReasons.Banned)
                            {
                                state.NameText.SetText("<color=#FF0000>The FAC BAN [Cheater]</color>");
                            }
                            else
                            {
                                state.NameText.SetText("<color=#8A8A8A>Disconnected</color>");
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }
    }
}

