using HarmonyLib;
using AmongUs.GameOptions;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AmongUsPCMod;

[HarmonyPatch]
public static class FACRoleColorEnforcer
{
    /// <summary>
    /// Пач за TextMeshPro компонентите или текстовите стрингове в играта.
    /// Прехваща метода, който връща името на ролята с вграден Text-Color таг.
    /// </summary>
    [HarmonyPatch(typeof(RoleOptions), nameof(RoleOptions.GetRoleNameWithColor))]
    [HarmonyPrefix]
    public static bool OverrideVanillaRoleColor(RoleTypes role, ref string __result)
    {
        // 1. Взимаме нативното име на ролята през Вашата съществуваща логика
        string roleName = RoleHelper.GetRoleName(role);

        // 2. Издърпваме персонализирания Hex код от Вашата матрица в RoleHelper.cs
        string customHex = RoleHelper.GetRoleColorCode(role);

        // Fail-safe: Ако за дадена роля липсва Hex код, оставяме играта да ползва фабричния си цвят
        if (string.IsNullOrEmpty(customHex)) return true; 

        // 3. Форматираме стринга с TextMeshPro съвместим таг, ползвайки Вашия цвят
        __result = $"<color={customHex}>{roleName}</color>";

        return false; // Спираме фабричния метод на играта и инжектираме нашия цвят
    }
}
