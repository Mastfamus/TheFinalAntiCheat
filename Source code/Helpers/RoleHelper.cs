using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using AmongUs.GameOptions;

namespace AmongUsPCMod;

public static class RoleHelper
{
    private static readonly Dictionary<RoleTypes, string> roleColors = new()
    {
        { RoleTypes.CrewmateGhost, "#8CFFFF" },
        { RoleTypes.GuardianAngel, "#8CFFDB" },
        { RoleTypes.Crewmate, "#8CFFFF" },
        { RoleTypes.Scientist, "#ec7678" },
        { RoleTypes.Engineer, "#22c6cc" },
        { RoleTypes.Noisemaker, "#FFFBC9" },
        { RoleTypes.Tracker, "#eef11d" },
        { RoleTypes.Detective, "#4b81be" },
        { RoleTypes.Judge, "#6B2FBB" },
        { RoleTypes.ImpostorGhost, "#be0a0a" },
        { RoleTypes.Impostor, "#942020e5" },
        { RoleTypes.Shapeshifter, "#d60d0d" },
        { RoleTypes.Phantom, "#dd3d3d" },
        { RoleTypes.Viper, "#0bd136e8" }
    };

    public static RoleTypes GetRoleType(byte id)
    {
        return GetRoleById(id);
    }

    public static bool IsImpostor(this RoleTypes role)
    {
        return role switch
        {
            RoleTypes.Impostor
                or RoleTypes.Shapeshifter
                or RoleTypes.Phantom
                or RoleTypes.ImpostorGhost
                or RoleTypes.Viper => true,
            _ => false
        };
    }

    public static bool IsGhost(RoleTypes role)
    {
        return role switch
        {
            RoleTypes.ImpostorGhost or RoleTypes.CrewmateGhost or RoleTypes.GuardianAngel => true,
            _ => false
        };
    }

    public static string GetRoleName(RoleTypes role)
    {
        return GetRoleString(Enum.GetName(typeof(RoleTypes), role));
    }

    public static Color GetRoleColor(RoleTypes role)
    {
        roleColors.TryGetValue(role, out var hexColor);
        _ = ColorUtility.TryParseHtmlString(hexColor, out var c);
        return c;
    }

    public static string GetRoleColorCode(RoleTypes role)
    {
        roleColors.TryGetValue(role, out var hexColor);
        return hexColor;
    }

    public static string GetRoleInfoForVanilla(this RoleTypes role, bool roleHelp = false)
    {
        return IsNormalGame ? GetNormalGameRoleInfo(role, roleHelp) : GetHideNSeekRoleInfo(role, roleHelp);
    }

    private static string GetNormalGameRoleInfo(RoleTypes role, bool roleHelp)
    {
        var text = role.ToString();

        if (!roleHelp || role is RoleTypes.Crewmate or RoleTypes.Impostor)
        {
            return GetString($"{text}Blurb");
        }

        return $"{GetString($"RolesHelp_{text}_01")}\n{GetString($"RolesHelp_{text}_02")}";
    }

    private static string GetHideNSeekRoleInfo(RoleTypes role, bool roleHelp)
    {
        var text = role.ToString();

        if (!roleHelp)
        {
            return GetString($"HnS{text}Blurb");
        }

        return role switch
        {
            RoleTypes.Engineer => GetCrewmateRules(),
            RoleTypes.Impostor => GetImpostorRules(),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, $"Unsupported role type: {role}")
        };
    }

    private static string GetCrewmateRules() =>
        string.Join("\n",
            GetString(StringNames.RuleOneCrewmates),
            GetString(StringNames.RuleTwoCrewmates),
            GetString(StringNames.RuleThreeCrewmates));

    private static string GetImpostorRules() =>
        string.Join("\n",
            GetString(StringNames.RuleOneImpostor),
            GetString(StringNames.RuleTwoImpostor),
            GetString(StringNames.RuleThreeImpostor));
}