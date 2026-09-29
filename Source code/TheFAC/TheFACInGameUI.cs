using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace AmongUsPCMod
{
    [HarmonyPatch]
    public static class TheFACInGameUI
    {
        private static GameObject roleDescBtnObj = null;
        private static GameObject adminPanelBtnObj = null;

        [HarmonyPatch(typeof(global::HudManager), nameof(global::HudManager.Start))]
        [HarmonyPostfix]
        public static void HudManager_Start_Postfix(global::HudManager __instance)
        {
            if (__instance == null || __instance.UseButton == null) return;

            try
            {
                System.Console.WriteLine("[The FAC UI]: Instantiating in-game utility button nodes next to action canvas...");
                GameObject baseTemplate = __instance.UseButton.gameObject;

                // 🔮 БУТОН 1: [ ROLE ]
                roleDescBtnObj = UnityEngine.Object.Instantiate(baseTemplate, __instance.transform, false);
                roleDescBtnObj.name = "TheFAC_RoleDescriptionButton";
                roleDescBtnObj.transform.localPosition = new Vector3(5.2f, 1.2f, -1f); 
                roleDescBtnObj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

                var roleSprite = roleDescBtnObj.GetComponent<SpriteRenderer>();
                if (roleSprite != null) roleSprite.color = new Color(0.54f, 0.17f, 0.89f, 1f); 

                var roleText = roleDescBtnObj.GetComponentInChildren<TextMeshPro>();
                if (roleText != null) roleText.SetText("<color=#FFFFFF><b>ROLE</b></color>");

                var rolePassive = roleDescBtnObj.GetComponent<PassiveButton>();
                if (rolePassive != null)
                {
                    rolePassive.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    var roleAction = new UnityEngine.Events.UnityAction(new Action(() =>
                    {
                        string rDesc = "[The FAC]:  The Impostor's goal is to eliminate all Crewmates or sabotage the ship. Crewmates must complete tasks or identify and vote out the Impostor to win.";
                        AmongUsPCMod.ExpertManagerPlugin.SendLocalCommandFeedback(rDesc);
                    }));
                    rolePassive.OnClick.AddListener(roleAction);
                }
 
                // 🔮 БУТОН 2: [ PANEL ]
                adminPanelBtnObj = UnityEngine.Object.Instantiate(baseTemplate, __instance.transform, false);
                adminPanelBtnObj.name = "TheFAC_AdminPanelButton";
                adminPanelBtnObj.transform.localPosition = new Vector3(5.2f, 0.5f, -1f);
                adminPanelBtnObj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

                var panelSprite = adminPanelBtnObj.GetComponent<SpriteRenderer>();
                if (panelSprite != null) panelSprite.color = new Color(0.35f, 0.35f, 0.35f, 1f);

                var panelText = adminPanelBtnObj.GetComponentInChildren<TextMeshPro>();
                if (panelText != null) panelText.SetText("<color=#FFFFFF><b>PANEL</b></color>");

                var panelPassive = adminPanelBtnObj.GetComponent<PassiveButton>();
                if (panelPassive != null)
                {
                    panelPassive.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent(); 
                    var panelAction = new UnityEngine.Events.UnityAction(new Action(() =>
                    {
                        AmongUsPCMod.ExpertManagerPlugin.showMenu = !AmongUsPCMod.ExpertManagerPlugin.showMenu;
                    }));
                    panelPassive.OnClick.AddListener(panelAction);
                }

                roleDescBtnObj.SetActive(true);
                adminPanelBtnObj.SetActive(true);
            }
            catch { }
        }

        [HarmonyPatch(typeof(global::ChatController), nameof(global::ChatController.Update))]
        [HarmonyPostfix]
        public static void ChatController_Update_Postfix(global::ChatController __instance)
        {
            if (__instance == null || global::HudManager.Instance == null || global::HudManager.Instance.Chat == null) return;

            try
            {
                var chatDisplay = global::HudManager.Instance.Chat;
                if (!chatDisplay.gameObject.activeSelf) chatDisplay.gameObject.SetActive(true);

                // Защитата се активира единствено по време на раунд на живо, лобито остава отпушено!
                if (ExpertManagerPlugin.IsInActiveGameplayRound) 
                {
                    var activeBubbles = chatDisplay.chatBubblePool.activeChildren;
                    if (activeBubbles == null) return;

                    for (int i = 0; i < activeBubbles.Count; i++)
                    {
                        var bubbleObj = activeBubbles[i];
                        if (bubbleObj == null) continue;

                        var bubble = bubbleObj.GetComponent<global::ChatBubble>();
                        if (bubble != null && bubble.TextArea != null && bubble.NameText != null)
                        {
                            bubble.NameText.SetText("???");
                            bubble.TextArea.SetText("<color=#555555>" + bubble.TextArea.text + "</color>");
                            
                            var bgSprite = bubble.transform.Find("Background")?.GetComponent<SpriteRenderer>();
                            if (bgSprite != null) bgSprite.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                        }
                    }
                }
            }
            catch { }
        }
    }
}