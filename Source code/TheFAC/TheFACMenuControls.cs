using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace AmongUsPCMod
{
    // ============================================================================
    // ⚙️ THE FAC MENU CONTROLS: LAPTOP STABLE INTERFACE (v16.1.1)
    // ============================================================================
    [HarmonyPatch(typeof(global::OptionsMenuBehaviour), nameof(global::OptionsMenuBehaviour.Start))]
    public static class TheFACMenuControls
    {
        public static bool AntiCheatActive = true;
        public static bool PerformanceModeActive = true;
        public static bool VisualRadarActive = true;
        public static bool MusicPlayerActive = true;

        [HarmonyPostfix]
        public static void Postfix(global::OptionsMenuBehaviour __instance)
        {
            if (__instance == null || __instance.transform == null) return;

            try
            {
                // Намираме скрол контейнера на лаптопа нативно и сигурно
                Transform scrollContent = __instance.transform.Find("SlideArea/ScrollContainer") ?? __instance.transform;

                System.Console.WriteLine("[The FAC UI]: Injecting stable laptop controls...");

                GameObject acHeader = new GameObject("TheFAC_AC_Header");
                acHeader.transform.SetParent(scrollContent, false);
                acHeader.transform.localPosition = new Vector3(-0.5f, -4.5f, -0.5f); 

                var acHeaderText = acHeader.AddComponent<TextMeshPro>();
                acHeaderText.fontSize = 2.0f;
                acHeaderText.fontStyle = FontStyles.Bold;
                acHeaderText.color = new Color(0.54f, 0.17f, 0.89f);
                acHeaderText.text = "--- [ THE FAC CORE CONTROLS ] ---";

                CreateFACSwitch("Enable Anti-Cheat Core", new Vector3(-1.8f, -5.1f, -0.5f), scrollContent, AntiCheatActive, (state) => {
                    AntiCheatActive = state;
                });

                CreateFACSwitch("Enable FPS Unlocker", new Vector3(-1.8f, -5.7f, -0.5f), scrollContent, PerformanceModeActive, (state) => {
                    PerformanceModeActive = state;
                    UnityEngine.QualitySettings.vSyncCount = state ? 0 : 1;
                });
            }
            catch { }
        }

        private static void CreateFACSwitch(string textLabel, Vector3 localPos, Transform parent, bool defaultState, Action<bool> onToggleChanged)
        {
            try
            {
                GameObject toggleRow = new GameObject($"TheFAC_Btn_{textLabel.Replace(" ", "")}");
                toggleRow.transform.SetParent(parent, false);
                toggleRow.transform.localPosition = localPos;

                GameObject textObj = new GameObject("Label");
                textObj.transform.SetParent(toggleRow.transform, false);
                textObj.transform.localPosition = Vector3.zero;
                var tmp = textObj.AddComponent<TextMeshPro>();
                tmp.fontSize = 1.6f;
                tmp.color = Color.white;
                tmp.text = textLabel;

                GameObject boxObj = new GameObject("CheckboxText");
                boxObj.transform.SetParent(toggleRow.transform, false);
                boxObj.transform.localPosition = new Vector3(2.3f, 0f, 0f); 
                var boxTmp = boxObj.AddComponent<TextMeshPro>();
                boxObj.AddComponent<BoxCollider2D>().size = new Vector2(1.0f, 0.4f);
                
                bool currentState = defaultState;
                boxTmp.fontSize = 1.6f;
                boxTmp.fontStyle = FontStyles.Bold;
                boxTmp.text = currentState ? "<color=#00FF00>[ ON ]</color>" : "<color=#FF0000>[ OFF ]</color>";

                var passiveButton = boxObj.AddComponent<global::PassiveButton>();
                passiveButton.OnClick = new Button.ButtonClickedEvent();
                passiveButton.OnClick.AddListener(new UnityAction(() => {
                    currentState = !currentState;
                    boxTmp.text = currentState ? "<color=#00FF00>[ ON ]</color>" : "<color=#FF0000>[ OFF ]</color>";
                    onToggleChanged?.Invoke(currentState);
                }));
            }
            catch { }
        }
    }
}
