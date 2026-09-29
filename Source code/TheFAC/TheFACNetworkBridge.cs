using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;
using TMPro;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🛰️ THE FAC NETWORK BRIDGE: TWO-PART WELCOME, GLOBAL RADAR & TOP TEXTS (v15.0.0)
    // ============================================================================
    [HarmonyPatch]
    public static class TheFACNetworkBridge
    {
        private static GameObject facLoadingTextObject;
        private static TextMeshProUGUI facLoadingTextComponent;
        private static float facLoadingTextCycleTime = 0f;
        private static int facLoadingTextIndex = 0;

        // Памет на The FAC за жива класификация на всички модове в реално време
        public static Dictionary<byte, string> DetectedModdedPlayers = new Dictionary<byte, string>();

        private static string[] facLoadingLines = new string[] {
            "<color=#00FFFF>[The FAC]: Initializing Advanced Network Overclock...</color>",
            "<color=#FFD700>[System]: Double Defensive Wall Status: ACTIVE.</color>",
            "<color=#00FF00>[Tip]: Type /fix (ID) in chat to stabilize lagging players.</color>",
            "<color=#FF0000>[Warning]: Cheat menus detected on handshake will be instantly nuked!</color>",
            "<color=#8A2BE2>[The FAC]: Stealth Module Cloaking Active.</color>",
            "<color=#00FF00>[The FAC Core]: Anti-Exploit Matrix is now online.</color>",
            "<color#8A2BE2>[Tip]: Use Anti-Host Spoof protection at all times!</color>",
            "<color##8A2BE2>[The FAC]: During extreme network stress the room'll autoclose.</color>",
            "<color=#00FFFF>[The FAC Alert]: Cheat menu detected! Canceling all RPC packets from that user.</color>"
        };

        // ----------------------------------------------------------------
        // 🤝 ПАЧ 1: ГЛОБАЛЕН КЛАСИФИКАЦИОНЕН RPC СКЕНЕР ЗА МОДОВЕ И ЧИЙТ МЕНЮТА
        // ----------------------------------------------------------------
        [HarmonyPatch(typeof(global::InnerNet.InnerNetClient), "HandleGameData")]
        [HarmonyPrefix]
        public static bool Prefix(global::InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] Hazel.MessageReader parentReader)
        {
            if (__instance == null || parentReader == null || GameData.Instance == null) return true;

            try {
                Hazel.MessageReader subReader = Hazel.MessageReader.Get(parentReader);
                byte callId = subReader.Tag;
                byte senderId = parentReader.Tag; // Вземаме Client ID-то на изпращача

                // 📡 1. Сканиране на пакетиран Custom RPC трафик (CancelPet = Rpc ID 21)
                if (callId == 21)
                {
                    try {
                        var rpcCheckReader = Hazel.MessageReader.Get(subReader);
                        string flag = rpcCheckReader.ReadString();
                        rpcCheckReader.Recycle();

                        if (!DetectedModdedPlayers.ContainsKey(senderId))
                        {
                            if (flag == "BAU_CUSTOM_RPC" || flag.Contains("BAU")) {
                                DetectedModdedPlayers[senderId] = "BAU";
                            }
                            else if (flag.Contains("TOU") || flag.Contains("TownOfUs")) {
                                DetectedModdedPlayers[senderId] = "TOU";
                            }
                            else if (flag.Contains("SUBMERGED") || flag.Contains("Sub")) {
                                DetectedModdedPlayers[senderId] = "SUB";
                            }
                            else if (flag.Contains("HYDRA") || flag.Contains("FinalSuspect")) {
                                DetectedModdedPlayers[senderId] = "HYDRA";
                            }
                            else if (flag.Contains("BAN_MOD") || flag.Contains("BanMod")) {
                                DetectedModdedPlayers[senderId] = "BAN_MOD";
                            }
                        }
                    } catch { }
                }

                // 👿 2. Хващане на сурови сигнатури на нелегални чийт менюта в лобито (ID 101, 119, 145, 250)
                if (!DetectedModdedPlayers.ContainsKey(senderId))
                {
                    if (callId == 101 || callId == 250) {
                        DetectedModdedPlayers[senderId] = "KN_SICKO";
                    }
                    else if (callId == 119) {
                        DetectedModdedPlayers[senderId] = "AUM";
                    }
                    else if (callId == 145) {
                        DetectedModdedPlayers[senderId] = "AUNLOCKER";
                    }
                    else if (callId >= 200 && callId <= 210) {
                        DetectedModdedPlayers[senderId] = "MMC_EXPLOIT";
                    }
                }

                subReader.Recycle();
            } catch { }
            return true;
        }

        // ----------------------------------------------------------------
        // ⚙️ ПАЧ 2: ДВУКОМПОНЕНТНО ПРИВЕТСТВИЕ С ТАЙМЕР ПОД 100 СИМВОЛА ЛИМИТ
        // ----------------------------------------------------------------
        [HarmonyPatch(typeof(global::AmongUsClient), "OnGameJoined")]
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (PlayerControl.LocalPlayer != null) {
             global::AmongUsClient.Instance.Invoke("CoSendSplitWelcomeMessage", 0.1f); 
            }
        }

        private static IEnumerator CoSendSplitWelcomeMessage()
        {
            yield return new WaitForSeconds(10.0f);

            if (global::HudManager.Instance != null && global::HudManager.Instance.Chat != null)
            {
                // Първа част от приветствието на The FAC
                string msg1 = "<color=#8A2BE2>Welcome to The Final Anti-Cheat mod.</color> This is a <i><color=#00FF00>client only</color></i> mod coded with a powerful AC.";
                global::HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg1, false);

                yield return new WaitForSeconds(3.0f);

                // Втора част от приветствието на The FAC
                string msg2 = "<color=#00FFFF>Do /commands to view overrides.</color> Enjoy hacker-free game sessions, logs, ban systems & more!";
                global::HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg2, false);
            }
        }

        // ----------------------------------------------------------------
        // ⚙️ ПАЧ 3: ТЕКСТОВЕ НАЙ-ГОРЕ НА ЕКРАНА САМО ПРИ ЗАРЕЖДАНЕ
        // ----------------------------------------------------------------
        [HarmonyPatch(typeof(global::HudManager), "Update")]
        [HarmonyPostfix]
        public static void HudManager_Update_Postfix(global::HudManager __instance)
        {
            if (__instance == null || AmongUsClient.Instance == null) return;

            try {
                bool showLoading = PlayerControl.LocalPlayer == null && AmongUsClient.Instance.InOnlineScene;

                if (facLoadingTextObject == null && __instance.GetComponentInChildren<Canvas>() != null) {
                    facLoadingTextObject = new GameObject("TheFAC_TopLoadingOverlay");
                    UnityEngine.Object.DontDestroyOnLoad(facLoadingTextObject);

                    var rect = facLoadingTextObject.AddComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -40f);
                    rect.sizeDelta = new Vector2(900f, 50f);

                    facLoadingTextComponent = facLoadingTextObject.AddComponent<TextMeshProUGUI>();
                    facLoadingTextComponent.alignment = TextAlignmentOptions.Center;
                    facLoadingTextComponent.fontSize = 21;
                    facLoadingTextComponent.color = new Color(0f, 1f, 1f, 1f);
                    facLoadingTextComponent.text = facLoadingLines[0]; // Сигурно подаване на първия низ

                    facLoadingTextObject.transform.SetParent(__instance.GetComponentInChildren<Canvas>().transform, false);
                }

                if (facLoadingTextObject != null) {
                    facLoadingTextObject.SetActive(showLoading);
                    if (showLoading && Time.time >= facLoadingTextCycleTime) {
                        facLoadingTextIndex = (facLoadingTextIndex + 1) % facLoadingLines.Length;
                        facLoadingTextCycleTime = Time.time + 3.8f;
                        facLoadingTextComponent.text = facLoadingLines[facLoadingTextIndex];
                    }
                }
            } catch { }
        }
    }
}
