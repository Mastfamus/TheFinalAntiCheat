using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using InnerNet;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🔮 THE FAC VISUAL RADAR: HIGH-FIDELITY OVERLAY MATRIX (v16.0.6)
    // ============================================================================
    public static class TheFACVisualRadar
    {
            public static void ExecuteFACRadarLogic()
        {
            if (!AmongUsPCMod.TheFACMenuControls.VisualRadarActive) return;
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.cosmetics == null) return;

            try
            {
                var nameTextObj = PlayerControl.LocalPlayer.cosmetics.nameText;
                if (nameTextObj == null) return;

                // ⚡ ФИКС 1: Свързваме се с нативния масив allClients
                var client = AmongUsClient.Instance?.allClients?.ToArray().FirstOrDefault(c => c != null && c.Character != null && c.Character.PlayerId == PlayerControl.LocalPlayer.PlayerId);
                
                // ⚡ ФИКС 2: Коригирано на uint с правилен синтаксис
                uint level = (uint)AmongUsPCMod.ExpertManagerPlugin.GetPlayerLevel(PlayerControl.LocalPlayer);
                byte id = PlayerControl.LocalPlayer.PlayerId;
                
                string fCode = (client != null && client.FriendCode != null) ? client.FriendCode : "N/A";
                
                // 🦾 НАПЪЛНО ВЪЗСТАНОВЕНО: Извличаме платформата нативно през PlatformData (По Файл 20)
                string platform = "PC";
                if (client != null && client.PlatformData != null)
                {
                    platform = client.PlatformData.Platform.ToString().Replace("Standalone", "");
                }

                bool hasModDetected = false;
                string modNameText = "";
                Color outlineColor = Color.white;

                if (fCode.Contains("bau") || (fCode == "" && level == 1)) 
                {
                    hasModDetected = true;
                    modNameText = "<color=#00FF00><font=\"LiberationSans SDF\">BetterUser</font></color>\n";
                    outlineColor = Color.green;
                }

                // 1. ИЗГРАЖДАНЕ НА ГОРНИЯ ЛИЛАВ ЕТИКЕТ (ID + Level)
                string topText = "";
                if (hasModDetected) topText += modNameText;
                topText += $"<color=#8A2BE2><font=\"LiberationSans SDF\">ID: {id} | LVL: {level}</font></color>\n";

                nameTextObj.text = topText + nameTextObj.text;

                // 2. ИЗГРАЖДАНЕ НА ДОЛНИЯ ЕТИКЕТ (Platform + Friend Code под краката)
                GameObject bottomTextObj = new GameObject("TheFAC_BottomPlate");
                bottomTextObj.transform.SetParent(PlayerControl.LocalPlayer.transform);
                bottomTextObj.transform.localPosition = new Vector3(0f, -0.9f, -0.1f);
                bottomTextObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

                var tmp = bottomTextObj.AddComponent<TextMeshPro>();
                tmp.fontSize = 4f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.outlineColor = Color.black;
                tmp.outlineWidth = 0.25f;
                tmp.text = $"<color=#CCCCCC><size=80><font=\"LiberationSans SDF\">{platform} | {fCode}</font></size></color>";

                if (PlayerControl.LocalPlayer.GetComponent<SpriteRenderer>())
                {
                    PlayerControl.LocalPlayer.GetComponent<SpriteRenderer>().material.SetColor("_OutlineColor", outlineColor);
                }
            }
            catch { }
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer == null) return;

            try
            {
                // 🔒 СТРИКТЕН ПРИЗРАЧЕН ФИЛТЪР (По твоето изрично поръчение!)
                // Проверяваме дали локалният играч е призрак, но НЕ е Guardian Angel и НЕ е жив!
                bool isLocalDead = PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsDead;
                bool isGuardianAngel = PlayerControl.LocalPlayer.Data != null && 
                                       PlayerControl.LocalPlayer.Data.RoleType == AmongUs.GameOptions.RoleTypes.GuardianAngel;

                // АКО СИ ЖИВ ИЛИ СИ АНГЕЛ ПАЗИТЕЛ: Радарът умира и скрива позициите веднага!
                if (!isLocalDead || isGuardianAngel) return;

                var client = AmongUsClient.Instance?.allClients?.ToArray().FirstOrDefault(c => c != null && c.Character != null && c.Character.PlayerId == PlayerControl.LocalPlayer.PlayerId);
                uint level = (uint)AmongUsPCMod.ExpertManagerPlugin.GetPlayerLevel(PlayerControl.LocalPlayer);

                if (PlayerControl.LocalPlayer.cosmetics != null && PlayerControl.LocalPlayer.cosmetics.nameText != null)
                {
                    // Нативно чертаем координатите и нивата над главите само за мъртвите призраци!
                   if (PlayerControl.LocalPlayer.cosmetics != null && PlayerControl.LocalPlayer.cosmetics.nameText != null) {PlayerControl.LocalPlayer.cosmetics.nameText.SetText($"{PlayerControl.LocalPlayer.Data.PlayerName} <color=#8A2BE2>[Lvl {level}]</color> <color=#FF0000>[XYZ: {(int)PlayerControl.LocalPlayer.transform.position.x},{(int)PlayerControl.LocalPlayer.transform.position.y}]</color>"); }
                    {
                    }
                }
            }
            catch { }
            
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer == null) return;

            try
            {
                // Поправка на ред 87 от лога: Четем нативната променлива за умрял призрак
                bool isLocalDead = PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsDead;
                bool isGuardianAngel = PlayerControl.LocalPlayer.Data != null && 
                                       PlayerControl.LocalPlayer.Data.RoleType == AmongUs.GameOptions.RoleTypes.GuardianAngel;

                if (!isLocalDead || isGuardianAngel) return;

                uint level = (uint)AmongUsPCMod.ExpertManagerPlugin.GetPlayerLevel(PlayerControl.LocalPlayer);

                // Поправка на редове 97 и 100 от лога: Пренасочваме през козметичния слой на Innersloth
                if (PlayerControl.LocalPlayer.cosmetics != null && PlayerControl.LocalPlayer.cosmetics.nameText != null && PlayerControl.LocalPlayer.Data != null)
                {
                    PlayerControl.LocalPlayer.cosmetics.nameText.SetText($"{PlayerControl.LocalPlayer.Data.PlayerName} <color=#8A2BE2>[Lvl {level}]</color> <color=#FF0000>[XYZ: {(int)PlayerControl.LocalPlayer.transform.position.x},{(int)PlayerControl.LocalPlayer.transform.position.y}]</color>");
                }
            }
            catch { }
        }
    }
}