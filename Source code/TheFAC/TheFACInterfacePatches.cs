using System;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🎨 THE FAC INTERFACE PATCHES: VRAM STAMP & CURSOR MATRIX (v16.2.6)
    // ============================================================================
    [HarmonyPatch]
    public static class TheFACInterfacePatches
    {
        private static GameObject facStampObj;

        /// <summary>
        /// 📡 ХУК ПРИ СТАРТ НА МЕНЮТО: Лепим логото на екрана директно през инжектирания Cloud VRAM клъстер!
        /// </summary>
        [HarmonyPatch(typeof(global::MainMenuManager), nameof(global::MainMenuManager.Start))]
        [HarmonyPostfix]
        public static void MainMenuManager_Start_Postfix()
        {
            try
            {
                if (facStampObj != null || AmongUsPCMod.ExpertManagerPlugin.CustomModStampTexture == null) return;

                System.Console.WriteLine("[The FAC UI]: Injecting custom Modstamp watermark into canvas...");

                facStampObj = new GameObject("Modstamp.png");
                UnityEngine.Object.DontDestroyOnLoad(facStampObj);

                var renderer = facStampObj.AddComponent<SpriteRenderer>();
                Texture2D tex = AmongUsPCMod.ExpertManagerPlugin.CustomModStampTexture;
                
                renderer.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0f, 0f), 100f);
                
                facStampObj.transform.position = new Vector3(-5.5f, -3.2f, 5f);
                facStampObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f); 
            }
            catch { }
        }

        /// <summary>
        /// 📡 ХУК В ЛОБИТО: Курсорът-чукче вече се набива хардуерно през Cloud Engine, така че тук поддържаме празен филтър
        /// </summary>
        [HarmonyPatch(typeof(global::GameStartManager), nameof(global::GameStartManager.Start))]
        [HarmonyPostfix]
        public static void GameStartManager_Start_Postfix() { }
    }
}
