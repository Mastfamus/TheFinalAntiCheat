using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🛡️ THE FAC LEVEL SHIELD: TIME-BUFFERED ANTI-LEVEL SPOOF (v19.9.5 - FIXED)
    // ============================================================================
    [HarmonyPatch]
    public static class TheFACLevelKicker
    {
        private static Dictionary<byte, int> authenticatedLevelsCache = new Dictionary<byte, int>();
        private static Dictionary<byte, float> playerJoinTimestamps = new Dictionary<byte, float>();

        [HarmonyPatch(typeof(global::PlayerPhysics), nameof(global::PlayerPhysics.FixedUpdate))]
        [HarmonyPostfix]
        public static void Postfix(global::PlayerPhysics __instance)
        {
            if (__instance == null || __instance.myPlayer == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            try
            {
                var player = __instance.myPlayer;
                if (player.Data == null || player.AmOwner) return;

                byte pId = player.PlayerId;
                
                // Записваме кога точно играчът е стъпил физически в стаята
                if (!playerJoinTimestamps.ContainsKey(pId))
                {
                    playerJoinTimestamps[pId] = Time.time;
                    return; // Пропускаме първия фрейм, докато трае Вълна 1
                }

                // ⏱️ ВРЕМЕВИ БУФЕР ПРОТИВ МРЕЖОВИ ВЪЛНИ:
                // Изчакваме точно 3.0 секунди, за да завърши облачната синхронизация на Innersloth (Вълна 2 и 3)
                if (Time.time - playerJoinTimestamps[pId] < 3.0f) return;

                int currentDisplayedLevel = ExpertManagerPlugin.GetPlayerLevel(player);

                // ФИКС: Игнорираме грешните нулеви стойности по време на лаг
                if (currentDisplayedLevel <= 0) return;

                // 🛡️ ЩИТ А: LEVEL KICKER (Изхвърля играчи под конфигурирания праг)
                if (currentDisplayedLevel < ExpertManagerPlugin.KickBelowLevelThreshold)
                {
                    System.Console.WriteLine($"[THE FAC LEVEL SHIELD]: Level {currentDisplayedLevel} is below threshold. Nuking Client.");
                    ExecuteAdministrativeLevelNuke(player, $"Level too low ({currentDisplayedLevel})");
                    return;
                }

                // 🛡️ ЩИТ Б: ANTI-LEVEL SPOOF (Хакер изкуствено си вдига нивата в движение)
                if (!authenticatedLevelsCache.ContainsKey(pId))
                {
                    authenticatedLevelsCache[pId] = currentDisplayedLevel;
                }
                else
                {
                    int originalAuthedLevel = authenticatedLevelsCache[pId];
                    if (currentDisplayedLevel != originalAuthedLevel)
                    {
                        System.Console.WriteLine($"[THE FAC LEVEL Spoof]: Player changed level mid-game from {originalAuthedLevel} to {currentDisplayedLevel}!");
                        ExecuteAdministrativeLevelNuke(player, $"Level Spoofing ({originalAuthedLevel} -> {currentDisplayedLevel})");
                    }
                }
            }
            catch { }
        }

        private static void ExecuteAdministrativeLevelNuke(global::PlayerControl player, string reason)
        {
            if (player == null) return;
            
            var clientData = AmongUsClient.Instance.allClients?.ToArray()
                .FirstOrDefault(c => c != null && c.Character != null && c.Character.PlayerId == player.PlayerId);

            if (clientData != null && AmongUsClient.Instance.AmHost)
            {
                // ИЗЧИСТВАМЕ КЕША ПРЕДИ ИЗХВЪРЛЯНЕТО
                byte pId = player.PlayerId;
                playerJoinTimestamps.Remove(pId);
                authenticatedLevelsCache.Remove(pId);

                // 🪓 ЕДИНСТВЕНИЯТ КРАЕН ЕКЗЕКУТОР: Извикваме Вашия нативен метод PunishPlayer
                ExpertManagerPlugin.PunishPlayer(clientData, $"[THE FAC LEVEL MATRIX]: {reason}", 0);
            }
        }

        // Автоматично изчистване на паметта, когато играч напусне стаята
        public static void ClearPlayerFromLevelCache(byte playerId)
        {
            playerJoinTimestamps.Remove(playerId);
            authenticatedLevelsCache.Remove(playerId);
        }
    }
}