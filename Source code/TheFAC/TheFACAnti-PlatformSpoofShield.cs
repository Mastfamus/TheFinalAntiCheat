using System;
using System.Linq;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🛡️ THE FAC HARDWARE PROTECTION: ANTI-PLATFORM SPOOF MATRIX
    // ============================================================================
    [HarmonyPatch(typeof(global::PlatformSpecificData), nameof(global::PlatformSpecificData.Deserialize))]
    public static class TheFACPlatformSpoofShield
    {
        [HarmonyPostfix]
        public static void Postfix(global::PlatformSpecificData __instance)
        {
            if (__instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            try
            {
                if (AmongUsClient.Instance.allClients == null) return;

                // 🔮 ЧИСТ НА ТИВЕН МОДЕЛ: Търсим клиента директно в InnerNet масива без GetClient()!
                var clientData = AmongUsClient.Instance.allClients.ToArray()
                    .FirstOrDefault(cd => cd != null && cd.PlatformData == __instance);

                if (clientData == null || clientData.Character == null) return;

                var player = clientData.Character;
                if (player.Data == null) return;

                string pName = player.Data.PlayerName ?? "Hacker";
                string friendCode = player.Data.FriendCode ?? "";

                // 🔮 White-List имунизация за Starlight клиенти:
                if (friendCode.ToLowerInvariant().Contains("starlight") || __instance.Platform.ToString().ToLowerInvariant().Contains("starlight"))
                {
                    return; // Прескачаме защитата за легални къстъм клиенти
                }

                // Проверка за неизвестни или невалидни платформи
                if (__instance.Platform == Platforms.Unknown)
                {
                    System.Console.WriteLine($"[THE FAC SECURITY]: Platform Spoof Match! Unknown Platform token on player '{pName}'. Executing Ban.");
                    AmongUsClient.Instance.KickPlayer(player.PlayerId, true);
                }
            }
            catch { }
        }
    }
}
