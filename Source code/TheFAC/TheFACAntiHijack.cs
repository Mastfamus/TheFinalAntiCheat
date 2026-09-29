using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🛡️ THE FAC ACCOUNT HIJACK SHIELD: HARDWARE DISCRETE MATRIX (v1.1.0 - LOCKED)
    // ============================================================================
    [HarmonyPatch]
    public static class FACAccountHijackShield
    {
        private static readonly Dictionary<string, int> HardwareSessionRegistry = new Dictionary<string, int>();

        [HarmonyPatch(typeof(global::PlayerControl), nameof(global::PlayerControl.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(global::PlayerControl __instance)
        {
            if (__instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            HudManager.Instance.StartCoroutine(CoRegisterHardwareIdentity(__instance));
        }

        private static System.Collections.IEnumerator CoRegisterHardwareIdentity(global::PlayerControl player)
        {
            yield return null;

            if (player == null || player.Data == null) yield break;

            try
            {
                string friendCode = (player.Data.FriendCode ?? "").Trim().ToLowerInvariant();
                string puid = player.OwnerId.ToString();

                bool isStaffOrVip = false;
                
                // Проверяваме админ листата
                lock (ExpertManagerPlugin.immuneLock)
                {
                    if (ExpertManagerPlugin.ImmunePlayersList.Contains(friendCode) || ExpertManagerPlugin.ImmunePlayersList.Contains(puid))
                    {
                        isStaffOrVip = true;
                    }
                }

                // Проверяваме и новата VIP листа
                lock (ExpertManagerPlugin.vipLock)
                {
                    if (ExpertManagerPlugin.VipPlayersList != null && 
                        (ExpertManagerPlugin.VipPlayersList.Contains(friendCode) || ExpertManagerPlugin.VipPlayersList.Contains(puid)))
                    {
                        isStaffOrVip = true;
                    }
                }

                if (isStaffOrVip && !string.IsNullOrWhiteSpace(puid))
                {
                    var clientData = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(c => c != null && c.Character != null && c.Character.PlayerId == player.PlayerId);
                    if (clientData != null)
                    {
                        HardwareSessionRegistry[puid] = clientData.Id;
                        System.Console.WriteLine($"[THE FAC IDENTITY]: Sealed sealed token for protected user. Session Locked to Connection ID: {clientData.Id}");
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 🔒 ХЕРМЕТИЧЕН СИСТЕМЕН ПРЕКЪСВАЧ: Наказва само реалния източник на Puppet Master хака!
        /// </summary>
        internal static bool VerifyPacketOriginIntegrity(PlayerControl sender, byte callId)
        {
            if (sender == null || sender.Data == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;
            if (sender.AmOwner) return true;

            try
            {
                string puid = sender.OwnerId.ToString();

                if (HardwareSessionRegistry.ContainsKey(puid))
                {
                    int authorizedConnectionId = HardwareSessionRegistry[puid];
                    var currentPacketSource = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(c => c != null && c.Character != null && c.Character.PlayerId == sender.PlayerId);
                    
                    if (currentPacketSource != null)
                    {
                        // АКО ХАКЕР СЕ ОПИТВА ДА УПРАВЛЯВА ЧУЖДО ИМЕ (Мрежовите ID се различават!)
                        if (currentPacketSource.Id != authorizedConnectionId)
                        {
                            string pName = sender.Data.PlayerName ?? "Unknown";
                            System.Console.WriteLine($"[THE FAC HIJACK BLOCK]: Hijack Blocked! Remote injection on user '{pName}' intercepted from Connection ID {currentPacketSource.Id}!");

                            // 1. Свири Reactor алармата
                            ExpertManagerPlugin.PlayReactorAlarm("cheat");

                            // 2. Глобално известие в чата
                            ExpertManagerPlugin.SendSteamChatMessage($"<color=#FF0000>[THE FAC BLOCKADE]: Identity theft exploit detected against Staff/VIP Account. Severing fraudulent link.</color>");

                            // 3. СРИВАМЕ ХАКЕРА: Банираме единствено реалния източник на пакета (порт-а на хакера)!
                            ExpertManagerPlugin.PunishPlayer(currentPacketSource, $"[THE FAC HARD-BAN]: Remote Session Hijack Attempt on Tag {callId}", 0);

                            return false; // Спираме пакета, невинният играч остава чист и не бива баннат
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[THE FAC HIJACK ERROR]: Integrity check bypass failure: {ex.Message}");
            }

            return true;
        }

        public static void ClearPlayerFromHijackRegistry(string puid)
        {
            if (!string.IsNullOrWhiteSpace(puid))
            {
                HardwareSessionRegistry.Remove(puid);
            }
        }
    }
}
