using HarmonyLib;
using UnityEngine;
using System.Text;
using AmongUs.GameOptions;
using UnityEngine.UI;

namespace AmongUsPCMod;

[HarmonyPatch]
public static class FACChatEnforcer
{
    public static bool IsStealthChatActive = false;

    // Засичане на стартирането на мача (Когато лоби броячът удари точно 0)
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    public static void WatchLobbyCountdown(GameStartManager __instance)
    {
        if (__instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        if (__instance.startState == GameStartManager.StartingStates.Countdown && __instance.countDownTimer <= 0f)
        {
            IsStealthChatActive = true; // Активира се стелт протоколът в момента на стартиране
        }
    }

    // Предотвратяване изпращането на всякакви текстове, освен команди с "/"
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    [HarmonyPrefix]
    public static bool EnforceOnlyCommandsRestriction(ChatController __instance)
    {
        if (__instance == null || __instance.TextArea == null) return true;

        string originalText = __instance.TextArea.text;
        if (string.IsNullOrWhiteSpace(originalText)) return true;

        // Проверяваме дали стелт режимът е активен на сървъра
        if (IsStealthChatActive)
        {
            // Ако съобщението НЕ започва с "/", блокираме изпращането на мига
            if (!originalText.Trim().StartsWith("/"))
            {
                ExpertManagerPlugin.SendSteamChatMessage("<color=#FF0000>[FAC GUARD]: Outgoing chat blocked! Only administrative slash commands (/) are allowed during the operation.</color>");
                __instance.TextArea.text = string.Empty;
                return false; // Прекратява обработката на фабричния метод на играта
            }
        }
        return true;
    }

    // Прехващане и рендериране на чат съобщенията (Превръщане в ***** и сиви аватари)
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
    [HarmonyPostfix]
    public static void TransformBubbleToGreyStealth(ChatBubble __instance, [HarmonyArgument(0)] ChatBubbleData data)
    {
        ApplyStealthFormatting(__instance, data);
    }

    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetLeft))]
    [HarmonyPostfix]
    public static void TransformBubbleToGreyStealthLeft(ChatBubble __instance, [HarmonyArgument(0)] ChatBubbleData data)
    {
        ApplyStealthFormatting(__instance, data);
    }

    private static void ApplyStealthFormatting(ChatBubble bubble, ChatBubbleData data)
    {
        if (bubble == null || data == null || data.Sender == null) return;

        // Стелт чатът е активен САМО по време на геймплей round и СЕ ИЗКЛЮЧВА напълно вътре в MeetingHud
        bool isInMeeting = MeetingHud.Instance != null;
        bool isVictoryScreen = Object.FindObjectOfType<EndGameManager>() != null;

        if (IsStealthChatActive && !isInMeeting && !isVictoryScreen)
        {
            // 1. МАСКИРАНЕ НА ТЕКСТА В ЗВЕЗДИЧКИ
            if (bubble.TextArea != null && !data.Text.StartsWith("/"))
            {
                int len = data.Text.Length > 0 ? data.Text.Length : 5;
                if (len > 15) len = 15; // Заковаваме максимална дължина за компактност на балона
                bubble.TextArea.text = new string('*', len);
            }

            // 2. ОТКРИВАНЕ И СИВО МАСКИРАНЕ НА МИНИ-СПРАЙТ АВАТАРА
            if (bubble.UserIcon != null)
            {
                bubble.UserIcon.gameObject.SetActive(true);
                
                // Превръщаме спрайт рендерера на аватара в изцяло сив цвят
                bubble.UserIcon.color = new Color(0.35f, 0.35f, 0.35f, 1f); 

                // Подсигуряваме, че мащабът му е умален като мини-версия в чата
                bubble.UserIcon.transform.localScale = Vector3.one * 0.45f;
            }
        }
        else
        {
            // Извън игра или по време на митинги връщаме стандартния нативен цвят на аватарите на играчите
            if (bubble.UserIcon != null)
            {
                bubble.UserIcon.color = Color.white;
            }
        }
    }

    // Автоматично изключване на Стелт чата при краен екран (Victory/Defeat) или връщане в лоби
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void DisableStealthChatOnMatchEnd()
    {
        IsStealthChatActive = false;
    }
}
