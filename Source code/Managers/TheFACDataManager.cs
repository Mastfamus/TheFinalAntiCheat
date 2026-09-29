using System;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;
using InnerNet;
using System.Linq;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🧠 THE FAC DATA MANAGER: SOVEREIGN CLIENT SNAPSHOT SYSTEM (v16.0.0 - FIXED)
    // ============================================================================
    public static class TheFACDataManager
    {
        // ⚡ ФИКС 1: Използваме Вашия нативен път, дефиниран в ExpertManagerPlugin, вместо persistentDataPath
        private static string FACBanPath => ExpertManagerPlugin.banFilePath;
        private static readonly HashSet<string> LocalBanCache = new HashSet<string>();

        /// <summary>
        /// Инициализира нашия суверенен черен списък директно от Вашия BanPlayers.txt
        /// </summary>
        public static void InitBanMatrix()
        {
            try
            {
                if (string.IsNullOrEmpty(FACBanPath)) return;

                string dir = Path.GetDirectoryName(FACBanPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                
                // Извикваме Вашия нативен метод, за да синхронизираме базата данни
                ExpertManagerPlugin.LoadAllFiles();

                System.Console.WriteLine($"[The FAC Data]: Sovereign Ban Matrix successfully synced with Master List.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[The FAC Data Error]: Failed to init local bans: " + ex.Message);
            }
        }

        /// <summary>
        /// 🔒 ДОБАВЯНЕ В СУВЕРЕННИЯ БАН ЛИСТ: Записва хакера директно под правилния етикет
        /// </summary>
        public static void AddToFACBans(string friendCode, string puid, string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(friendCode)) return;

                string normalizedCode = friendCode.Trim().ToLowerInvariant();

                // Проверяваме за имунитет през Вашия списък (ImmunePlayers.txt)
                lock (ExpertManagerPlugin.immuneLock)
                {
                    if (ExpertManagerPlugin.ImmunePlayersList.Contains(normalizedCode) || 
                        ExpertManagerPlugin.ImmunePlayersList.Contains(playerName.ToLowerInvariant()))
                        return;
                }

                // Записваме хакера в Master файла чрез Вашия оригинален метод под правилната категория
                if (!ExpertManagerPlugin.bannedPuidsAndCodes.Contains(normalizedCode))
                {
                    ExpertManagerPlugin.bannedPuidsAndCodes.Add(normalizedCode);
                    
                    // Извикваме Вашия фабричен метод от ExpertManagerPlugin
                    ExpertManagerPlugin.AppendBanEntryToOurList(normalizedCode, puid, playerName);
                }

                System.Console.WriteLine($"[The FAC Fortress]: Permanent ban locked into master list for: {playerName}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[The FAC Data Error]: Ban entry injection failed: " + ex.Message);
            }
        }

        /// <summary>
        /// ПРОВЕРКА НА ИГРАЧ: Сканира нативно през Вашия зареден кеш масив
        /// </summary>
        public static bool IsPlayerBannedByTheFAC(string friendCode, string puid)
        {
            if (!string.IsNullOrEmpty(friendCode) && ExpertManagerPlugin.bannedPuidsAndCodes.Contains(friendCode.ToLowerInvariant())) return true;
            if (!string.IsNullOrEmpty(puid) && ExpertManagerPlugin.bannedPuidsAndCodes.Contains(puid.ToLowerInvariant())) return true;
            return false;
        }

        /// <summary>
        /// 🦾 НАЙ-МОЩНИЯТ ИНСТРУМЕНТ: Превръща ClientID в реален обект без бъгове (Кросплатформен)
        /// </summary>
        public static ClientData GetClientDataFromId(int clientId)
        {
            try
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.allClients == null) return null;

                var clientList = AmongUsClient.Instance.allClients.ToArray();
                for (int i = 0; i < clientList.Length; i++)
                {
                    var client = clientList[i];
                    if (client != null && client.Id == clientId)
                    {
                        return client;
                    }
                }
            }
            catch { }
            return null;
        }
    }

    // ============================================================================
    // 🪓 ХАРМОНИ ПАЧ ЗА АВТОНОМЕН КОНТРОЛ НА КИКОВЕТЕ
    // ============================================================================
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
    public static class FACKickInterceptorPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref int clientId, ref bool ban)
        {
            try
            {
                if (ban)
                {
                    ClientData target = TheFACDataManager.GetClientDataFromId(clientId);
                    if (target != null)
                    {
                        string fCode = target.FriendCode;
                        string puid = target.ProductUserId;
                        string pName = target.PlayerName;

                        // Записва директно в Master листа без риск от Android IO блокировки
                        TheFACDataManager.AddToFACBans(fCode, puid, pName);
                    }
                }
            }
            catch { }
        }
    }
}