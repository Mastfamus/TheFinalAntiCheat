using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AmongUsPCMod
{
    // ============================================================================
    // ⚙️ THE FAC NOTIFICATIONS MANAGER: HARDWARE SFX AUDIO SHIELD (v1.3.0 - LOCKED)
    // ============================================================================
    internal static class FACNotificationsManager
    {
        internal static bool NotifyCheat(PlayerControl sender, string formatActionText, bool forceban = false)
        {
            if (sender == null || sender.Data == null) return false;

            string playerName = sender.Data.PlayerName ?? "Unknown";
            string friendCode = sender.Data.FriendCode ?? "No-Code";

            // 🔊 1. АКТИВИРАНЕ НА ЗВУКОВА АЛАРМА
            try { ExpertManagerPlugin.PlayReactorAlarm("cheat"); } catch { }

            // 📝 2. ЗАПИС В КОНЗОЛАТА
            System.Console.WriteLine($"[THE FAC SECURITY MATRIX]: Alert! Suspect '{playerName}' triggered code validation. Type: {formatActionText}");

            // 🔒 3. СКРИТ ЧАТ ДО ХОСТА
            UI.Chat.FACChatUtilities.AddFACChatPrivate(
                $"<color=#FF0000><b>[FAC ALERTS]</b></color> Suspect: <color=#FFD700>{playerName}</color> | Trigger: {formatActionText}"
            );

            // 🪓 4. АВТОМАТИЧЕН FORCEBAN (Ако хендлърът изисква твърдо прекъсване)
            if (forceban)
            {
                var clientData = AmongUsClient.Instance?.allClients?.ToArray().FirstOrDefault(c => c != null && c.Character != null && c.Character.PlayerId == sender.PlayerId);
                if (clientData != null && AmongUsClient.Instance.AmHost)
                {
                    ExpertManagerPlugin.PunishPlayer(clientData, $"[THE FAC FORCE-BAN]: {formatActionText}", 0);
                }
            }

            return true; 
        }
    }
}
