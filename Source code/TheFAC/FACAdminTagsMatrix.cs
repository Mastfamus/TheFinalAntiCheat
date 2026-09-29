using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AmongUsPCMod
{
    // ============================================================================
    // 👑 THE FAC HIERARCHY MATRIX: SECURE PUBLIC TAGS & VIP ALLOCATION (v6.0.0)
    // ============================================================================
    [HarmonyPatch]
    public static class FACAdminTagsMatrix
    {
        /// <summary>
        /// 🏷️ СВЕТКАВИЧЕН МРЕЖОВ СИНХРОНИЗАТОР: Слага Vanilla-Safe тагове, видими за АБСОЛЮТНО ВСИЧКИ в стаята!
        /// </summary>
        [HarmonyPatch(typeof(global::PlayerControl), nameof(global::PlayerControl.Awake))]
        [HarmonyPostfix]
        private static void PlayerControl_Awake_Postfix(global::PlayerControl __instance)
        {
            if (__instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            // Извикваме микро-буфер, за да изчакаме зареждането на името на клиента без черни екрани
            HudManager.Instance.StartCoroutine(CoDeployVanillaSafeRankTags(__instance));
        }

        private static IEnumerator CoDeployVanillaSafeRankTags(global::PlayerControl player)
        {
            yield return null;
            yield return null; // Изчакваме точно 2 фрейма на енджина

            if (player == null || player.Data == null || !GameState.IsLobby) yield break;

            try
            {
                string friendCode = (player.Data.FriendCode ?? "").Trim().ToLowerInvariant();
                string puid = player.OwnerId.ToString();
                string currentName = player.Data.PlayerName ?? "";

                // Проверяваме дали играчът е Админ (от ImmunePlayers.txt)
                bool isVerifiedAdmin = false;
                lock (ExpertManagerPlugin.immuneLock)
                {
                    if (ExpertManagerPlugin.ImmunePlayersList.Contains(friendCode) || ExpertManagerPlugin.ImmunePlayersList.Contains(puid))
                    {
                        isVerifiedAdmin = true;
                    }
                }

                // 👑 1. АДМИНИСТРАТОРСКИ ТАГ (Видим за всички Vanilla играчи)
                if (isVerifiedAdmin)
                {
                    if (!currentName.StartsWith("[Admin]") && !currentName.StartsWith("[FAC]"))
                    {
                        string newAdminName = "[Admin] " + currentName;
                        if (newAdminName.Length > 15) newAdminName = newAdminName.Substring(0, 15);

                        player.RpcSetName(newAdminName);
                        System.Console.WriteLine($"[THE FAC TAGS]: Networked Admin Tag synchronized for: {newAdminName}");
                    }
                    yield break; // Спираме тук, админът има приоритет пред VIP
                }

                // ⭐ 2. VIP РАНК СИСТЕМA (Зарежда се от списъка ExpertManagerPlugin.VipPlayersList)
                bool isVerifiedVip = false;
                lock (ExpertManagerPlugin.immuneLock) 
                {
                    if (ExpertManagerPlugin.VipPlayersList != null && 
                        (ExpertManagerPlugin.VipPlayersList.Contains(friendCode) || ExpertManagerPlugin.VipPlayersList.Contains(puid)))
                    {
                        isVerifiedVip = true;
                    }
                }

                if (isVerifiedVip && !currentName.StartsWith("[VIP]"))
                {
                    string newVipName = "[VIP] " + currentName;
                    if (newVipName.Length > 15) newVipName = newVipName.Substring(0, 15);

                    player.RpcSetName(newVipName);
                    System.Console.WriteLine($"[THE FAC TAGS]: Networked VIP Tag synchronized for: {newVipName}");
                }
            }
            catch { }
        }

        /// <summary>
        /// 🔒 ЙЕРАРХИЯ НА ПРАВАТА: Предотвратява самобаниране, банване на Хоста и банване между Админи
        /// </summary>
        internal static bool ValidateAdminAuthorityOverTarget(PlayerControl adminSender, PlayerControl targetPlayer)
        {
            if (adminSender == null || targetPlayer == null) return false;

            // 1. ЗАЩИТА НА ХОСТА: Никой админ не може да банне/кикне Хоста (Вас)!
            if (targetPlayer.AmOwner)
            {
                System.Console.WriteLine($"[FAC PROTECTION]: Blocked illegal attempt by Admin '{adminSender.Data.PlayerName}' to punish the Host!");
                return false; 
            }

            // 2. ЗАЩИТА ОТ САМОНАРАНЯВАНЕ: Админ не може да банне самия себе си!
            if (adminSender.PlayerId == targetPlayer.PlayerId)
            {
                System.Console.WriteLine($"[FAC PROTECTION]: Admin '{adminSender.Data.PlayerName}' attempted to execute a command on themselves. Aborted.");
                return false;
            }

            // 3. ЗАЩИТА МЕЖДУ АДМИНИ: Админ не може да банне друг легален Админ!
            string targetCode = (targetPlayer.Data.FriendCode ?? "").Trim().ToLowerInvariant();
            string targetPuid = targetPlayer.OwnerId.ToString();

            lock (ExpertManagerPlugin.immuneLock)
            {
                if (ExpertManagerPlugin.ImmunePlayersList.Contains(targetCode) || ExpertManagerPlugin.ImmunePlayersList.Contains(targetPuid))
                {
                    System.Console.WriteLine($"[FAC PROTECTION]: Blocked Admin cross-ban attempt against fellow Administrator '{targetPlayer.Data.PlayerName}'.");
                    return false;
                }
            }

            return true; // Целта е обикновен играч – правото се разрешава!
        }
    }
}