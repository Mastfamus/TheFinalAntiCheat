using System;
using HarmonyLib;
using UnityEngine;

namespace AmongUsPCMod
{
    // ============================================================================
    // ⚓ THE FAC TACTICAL VISUALS: LIVE CAMERA ZOOM OVERRIDE MODULE
    // ============================================================================
    [HarmonyPatch(typeof(global::HudManager), nameof(global::HudManager.Update))]
    public static class TheFACZoomModule
    {
        private static bool _wasZooming = false;

        [HarmonyPostfix]
        public static void Postfix(global::HudManager __instance)
        {
            if (global::PlayerControl.LocalPlayer == null || global::PlayerControl.LocalPlayer.Data == null || Camera.main == null) return;

            // Разрешено ни е да зумваме само в лобито или ако сме мъртви
            bool canZoom = global::LobbyBehaviour.Instance != null || global::PlayerControl.LocalPlayer.Data.IsDead;

            if (canZoom)
            {
                // Скролване нагоре = Зумване навътре
                if (UnityEngine.Input.mouseScrollDelta.y > 0 && Camera.main.orthographicSize > 3.0f)
                {
                    _wasZooming = true;
                    Camera.main.orthographicSize -= 1.0f;
                    if (__instance != null && __instance.UICamera != null) __instance.UICamera.orthographicSize -= 1.0f;
                }
                // Скролване надолу = Зумване навън
                else if (UnityEngine.Input.mouseScrollDelta.y < 0 && Camera.main.orthographicSize < 15.0f)
                {
                    _wasZooming = true;
                    Camera.main.orthographicSize += 2.0f;
                    if (__instance != null && __instance.UICamera != null) __instance.UICamera.orthographicSize += 2.0f;
                }
            }
            else if (_wasZooming)
            {
                // Автоматично нулиране на зума до 3.0 при старт на мача
                Camera.main.orthographicSize = 3.0f;
                if (__instance != null && __instance.UICamera != null) __instance.UICamera.orthographicSize = 3.0f;
                _wasZooming = false;
            }
        }
    }
}
