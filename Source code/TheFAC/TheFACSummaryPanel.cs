using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HarmonyLib;

namespace AmongUsPCMod;

[HarmonyPatch]
public static class FACSummaryManager
{
    public static GameObject? SummaryUIContainer;
    public static GameObject? ToggleButtonInstance;
    public static bool IsPanelExplicitlyHidden = false;
    public static bool ForceShowViaEmergencyEnd = false;

    // Извиква се при зареждане на HUD Мениджъра или принудително завършване на мача (/end)
    public static void RenderOrUpdateSummaryPanel(HudManager hud)
    {
        if (hud == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.InOnlineScene) return;

        // Панелът СЕ СКРИВА автоматично по време на активна игра (рунд), освен ако не е задействан аварийния край
        bool isInActiveMatch = AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started;
        if (isInActiveMatch && !ForceShowViaEmergencyEnd && MeetingHud.Instance == null)
        {
            DestroySummaryUI();
            return;
        }

        // 1. СЪЗДАВАНЕ НА БУТОНА "SUMMARY" (ГОРЕН ЛЯВ ЪГЪЛ)
        if (ToggleButtonInstance == null)
        {
            // Създаваме бутона на базата на легална нативна подложка
            var baseButtonObj = GameObject.Find("ConsoleInLobby/PassiveButton") ?? GameObject.Find("StartButton");
            if (baseButtonObj != null)
            {
                ToggleButtonInstance = Object.Instantiate(baseButtonObj, hud.transform);
                ToggleButtonInstance.name = "FAC_SummaryToggleButton";
                ToggleButtonInstance.transform.localPosition = new Vector3(-5.2f, 3.4f, -10f); // Горе вляво
                ToggleButtonInstance.transform.localScale = new Vector3(0.5f, 0.4f, 1f);

                var txt = ToggleButtonInstance.GetComponentInChildren<TextMeshPro>();
                if (txt != null)
                {
                    txt.text = "Summary";
                    txt.fontSize = 2.5f;
                }

                var passiveBtn = ToggleButtonInstance.GetComponent<PassiveButton>();
                if (passiveBtn != null)
                {
                    passiveBtn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    passiveBtn.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
                        IsPanelExplicitlyHidden = !IsPanelExplicitlyHidden;
                        if (SummaryUIContainer != null) SummaryUIContainer.SetActive(!IsPanelExplicitlyHidden);
                    }));
                }
            }
        }

        // 2. СЪЗДАВАНЕ НА ОСНОВНИЯ КОНТЕЙНЕР ЗА СТАТИСТИКА
        if (SummaryUIContainer == null)
        {
            SummaryUIContainer = new GameObject("FAC_LobbySummaryPanel");
            SummaryUIContainer.transform.SetParent(hud.transform);
            SummaryUIContainer.transform.localPosition = new Vector3(-5.2f, 3.0f, -5f);
            SummaryUIContainer.transform.localScale = Vector3.one * 0.72f;

            var textMesh = SummaryUIContainer.AddComponent<TextMeshPro>();
            textMesh.fontSize = 2.1f;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.TopLeft;
            textMesh.color = Color.white;
            textMesh.outlineColor = Color.black;
            textMesh.outlineWidth = 0.15f;

            // Изграждане на лилавата фонова подложка
            var bgObj = new GameObject("FAC_SummaryBackground");
            bgObj.transform.SetParent(SummaryUIContainer.transform);
            bgObj.transform.localPosition = new Vector3(2.5f, -2.5f, 1f);
            var sRenderer = bgObj.AddComponent<SpriteRenderer>();
            sRenderer.sprite = AmongUsPCMod.TheFACAssetEngine.LoadSprite("LastResult-BG.png", 200f);
            sRenderer.color = new Color(0.18f, 0.02f, 0.25f, 0.85f); // Кралско FAC лилаво
            bgObj.transform.localScale = new Vector3(6.5f, 7.5f, 1f);
        }

        SummaryUIContainer.SetActive(!IsPanelExplicitlyHidden);

        // 3. КОМПИЛИРАНЕ НА ДАННИТЕ ЗА РЕГИОН, КОД И ИГРАЧИ
        StringBuilder sb = new StringBuilder();
        string roomCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
        string regionName = (ServerManager.Instance != null && ServerManager.Instance.CurrentRegion != null) ? ServerManager.Instance.CurrentRegion.Name.ToUpper() : "UNKNOWN";

        sb.AppendLine($"<color=#8A2BE2><b>[FAC SYSTEM RECAP]</b></color>");
        sb.AppendLine($"REGION: <color=#00FFFF>{regionName}</color> | CODE: <color=#FFFF00>{roomCode}</color>");
        sb.AppendLine("<color=#8A8A8A>---------------------------------------</color>");

        if (GameData.Instance != null && GameData.Instance.AllPlayers != null)
        {
            var allPlayers = GameData.Instance.AllPlayers;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                var pInfo = allPlayers[i];
                if (pInfo == null || pInfo.Disconnected) continue;

                string pName = pInfo.PlayerName;
                var role = pInfo.Role;
                string roleName = (role != null) ? role.Role.ToString() : "Crewmate";

                if (role != null && role.IsImpostor)
                {
                    // За Impostor: Показваме броя на неговите убийства по време на мача
                    int totalKills = ExpertManagerPlugin.CurrentMatchRoundCounter > 1 ? ExpertManagerPlugin.CurrentMatchRoundCounter - 1 : 0;
                    sb.AppendLine($"<color=#FF1919>★</color> {pName} - {roleName} (Kills: {totalKills})");
                }
                else
                {
                    // За Crewmate: Изчисляваме (Завършени задачи / Общ брой задачи)
                    int completedTasks = 0;
                    int totalTasks = 0;
                    if (pInfo.Tasks != null)
                    {
                        totalTasks = pInfo.Tasks.Count;
                        for (int j = 0; j < pInfo.Tasks.Count; j++)
                        {
                            if (pInfo.Tasks[j].Complete) completedTasks++;
                        }
                    }
                    sb.AppendLine($"  {pName} - {roleName} ({completedTasks}/{totalTasks})");
                }
            }
        }

        var tmProComponent = SummaryUIContainer.GetComponent<TextMeshPro>();
        if (tmProComponent != null) tmProComponent.text = sb.ToString();
    }

    public static void DestroySummaryUI()
    {
        if (SummaryUIContainer != null) { Object.Destroy(SummaryUIContainer); SummaryUIContainer = null; }
        if (ToggleButtonInstance != null) { Object.Destroy(ToggleButtonInstance); ToggleButtonInstance = null; }
    }

    // Принудително извикване при задействане на командата /end (Авариен авто-край)
    public static void TriggerEmergencyAutoEndVisibility(HudManager hud)
    {
        ForceShowViaEmergencyEnd = true;
        RenderOrUpdateSummaryPanel(hud);
    }

    // --- ИНТЕРЦЕПТОРНИ ХУКОВЕ (HARMONY PATCHES) ---

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManager_Update_Postfix(HudManager __instance)
    {
        RenderOrUpdateSummaryPanel(__instance);
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    [HarmonyPostfix]
    public static void EndGameManager_SetEverythingUp_Postfix()
    {
        ForceShowViaEmergencyEnd = false; // Преминаване в стандартен режим на Victory/Defeat
        IsPanelExplicitlyHidden = false; // Винаги форсираме показването при приключване
        var hud = HudManager.Instance;
        if (hud != null) RenderOrUpdateSummaryPanel(hud);
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    public static void CleanUpOnLeave()
    {
        DestroySummaryUI();
        ForceShowViaEmergencyEnd = false;
        IsPanelExplicitlyHidden = false;
    }
}