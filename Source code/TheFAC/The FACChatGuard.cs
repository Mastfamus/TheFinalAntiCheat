using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace AmongUsPCMod
{
    [HarmonyPatch(typeof(global::ChatController), nameof(global::ChatController.AddChat))]
    public static class TheFACChatGuard
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(global::ChatController __instance, global::PlayerControl sourcePlayer, ref string chatText)
        {
            if (global::LobbyBehaviour.Instance == null || AmongUsClient.Instance == null) return true;
            if (sourcePlayer == null || sourcePlayer.Data == null || string.IsNullOrEmpty(chatText)) return true;

            try
            {

                if (ExpertManagerPlugin.IsInActiveGameplayRound)
                {
                    string censorPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TheFAC_Data", "FAC_CensorWords.txt");
                    if (File.Exists(censorPath))
                    {
                        var censorWords = File.ReadAllLines(censorPath).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim().ToLowerInvariant());
                        foreach (var badWord in censorWords)
                        {
                            if (lowerChat.Contains(badWord))
                            {
                                chatText = System.Text.RegularExpressions.Regex.Replace(chatText, badWord, new string('*', badWord.Length), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            }
                        }
                    }
                }
            }
            catch { }
            return true;
        }
    }

    [HarmonyPatch(typeof(global::ChatBubble), nameof(global::ChatBubble.SetName))]
    public static class TheFACChatThemePatch
    {
        [HarmonyPostfix]
        public static void Postfix(global::ChatBubble __instance)
        {
            if (__instance == null || __instance.Background == null || __instance.TextArea == null || __instance.NameText == null) return;

            if (AmongUsPCMod.ExpertManagerPlugin.DarkTheme != null && AmongUsPCMod.ExpertManagerPlugin.DarkTheme.Value)
            {
                var chatPanel = __instance.GetComponentInParent<global::ChatController>();
                if (chatPanel != null)
                {
                    var bgImg = chatPanel.GetComponent<UnityEngine.UI.Image>();
                    if (bgImg != null) bgImg.color = new UnityEngine.Color(0.05f, 0.05f, 0.05f, 0.95f);
                }

                __instance.Background.color = new UnityEngine.Color(0.12f, 0.12f, 0.12f, 1f);
                __instance.TextArea.color = UnityEngine.Color.white;
                __instance.NameText.color = new UnityEngine.Color(0.54f, 0.17f, 0.89f, 1f);
            }
        }
    }
    [HarmonyPatch(typeof(global::LobbyBehaviour), nameof(global::LobbyBehaviour.Start))]
    public static class FACLobbyReturnInterceptor
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            ExpertManagerPlugin.IsInActiveGameplayRound = false;
        }
    }
}
