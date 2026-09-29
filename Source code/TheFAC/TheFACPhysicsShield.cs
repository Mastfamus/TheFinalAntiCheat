using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🌌 THE FAC PHYSICS MATRIX: REFINED CORE LAYER (v15.0.8)
    // ============================================================================
    [HarmonyPatch]
    public static class TheFACPhysicsShield
    {
        private static readonly Dictionary<byte, Vector2> lastValidPositions = new Dictionary<byte, Vector2>();

        /// <summary>
        /// 🪓 ФИЗИЧЕСКИ ПАТРУЛ: Следи за преминаване през стени при всяко FixedUpdate обновяване
        /// </summary>
        [HarmonyPatch(typeof(global::PlayerControl), nameof(global::PlayerControl.FixedUpdate))]
        [HarmonyPostfix]
        public static void OnFixedUpdate_Postfix(global::PlayerControl __instance)
        {
            // 🔒 ПРОВЕРКА 1: Ако Хостът е изключил Анти-Чийта от лаптопа — прескачаме безшумно!
            if (!TheFACMenuControls.AntiCheatActive) return;

            // 🔒 ПР ПРОВЕРКА 2: Сканираме САМО ако сме в реална онлайн игра (По Файл 25)
            if (__instance == null || __instance.Data == null || AmongUsClient.Instance == null) return;
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Joined) return;

            try
            {
                byte playerId = __instance.PlayerId;

                // Не сканираме себе си и прескачаме призраците/мъртвите (те нямат физически колайдери)
                if (__instance.AmOwner || __instance.Data.IsDead) return;

                Vector2 currentPos = __instance.transform.position;

                if (!lastValidPositions.ContainsKey(playerId))
                {
                    lastValidPositions[playerId] = currentPos;
                    return;
                }

                Vector2 lastPos = lastValidPositions[playerId];

                // АНТИ-NOCLIP ФИЛТЪР: Пресичаме незаконното прелитане през стени (ShipWalls)
                int layerMask = LayerMask.GetMask("ShipWalls", "Obstacles");
                RaycastHit2D hit = Physics2D.Linecast(lastPos, currentPos, layerMask);

                if (hit.collider != null && !hit.collider.isTrigger)
                {
                    System.Console.WriteLine($"[The FAC Physics]: Noclip clip vector bypassed by Player {playerId} at wall: {hit.collider.name}");
                    
                    // Извикваме нативния Хост кик пакет, ако ние сме домакин на стаята (По Файл 23)
                    if (AmongUsClient.Instance.AmHost)
                    {
                        var cheaterClient = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(c => c != null && c.Id == playerId);
                        if (cheaterClient != null)
                        {
                            AmongUsClient.Instance.KickPlayer(cheaterClient.Id, false);
                        }
                    }
                    return;
                }

                // Ако позицията е легална, я записваме като валидна база за следващия фрейм
                lastValidPositions[playerId] = currentPos;
            }
            catch { }
        }

        /// <summary>
        /// Нулираме физическата матрица при край на играта, за да няма изтичане на RAM (По Файл 27)
        /// </summary>
        public static void FlushPhysicsMatrix()
        {
            lastValidPositions.Clear();
            System.Console.WriteLine("[The FAC Physics]: Position memory flushed clean.");
        }
    }

    // ============================================================================
    // 📡 АВТОМАТИЧНО ИЗЧИСТВАНЕ ПРИ КРАЙ НА МАЧА (По Файл 27)
    // ============================================================================
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    public class FACPhysicsResetPatch
    {
        public static void Postfix()
        {
            TheFACPhysicsShield.FlushPhysicsMatrix();
        }
    }
}
