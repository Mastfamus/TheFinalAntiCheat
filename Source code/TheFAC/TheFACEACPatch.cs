using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using InnerNet;
using AmongUs.GameOptions;
using Hqzel;

namespace AmongUsPCMod
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    public static class EACAntiCheatPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ClientData clientData)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (clientData == null || clientData.CharacterInfo == null) return;

            try
            {
                string eacFilePath = Path.Combine(TheFACCloudEngine.GetSecureDataFolder(), TheFACCloudEngine.EacFileName);
                if (!File.Exists(eacFilePath)) return;

                string[] bannedIdentities = File.ReadAllLines(eacFilePath)
                                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                                .Select(line => line.Trim().ToLowerInvariant())
                                                .ToArray();

                string playerFriendCode = clientData.CharacterInfo.FriendCode?.Trim().ToLowerInvariant();
                string playerPUID = clientData.CharacterInfo.PUID?.Trim().ToLowerInvariant(); 

                bool shouldBan = false;

                if (!string.IsNullOrEmpty(playerFriendCode) && bannedIdentities.Contains(playerFriendCode)) shouldBan = true;
                else if (!string.IsNullOrEmpty(playerPUID) && bannedIdentities.Contains(playerPUID)) shouldBan = true;

                if (shouldBan)
                {
                    ExpertManagerPlugin.PunishPlayer(clientData.CharacterInfo.PlayerId);
                    System.Console.WriteLine($"[EAC ENFORCER]: Automatically banned blacklisted player ID: {clientData.CharacterInfo.PlayerId}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[EAC Enforcement Error]: " + ex.Message);
            }
        }
    }
}