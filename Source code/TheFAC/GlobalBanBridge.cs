using System; // 🔮 FIX CS0246: Добавено директно ядро за прихващане на уеб грешки!
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using InnerNet;

namespace AmongUsPCMod
{
    public static class GlobalBanBridge
    {
        private static HashSet<string> cloudBannedCache = new HashSet<string>();
        private static readonly object cloudLock = new object();

        public static void InitializeCloudSync() { ReloadCloudRegistryFromLocalCache(); }

        public static void ReloadCloudRegistryFromLocalCache()
        {
            try {
                lock (cloudLock) {
                    cloudBannedCache.Clear();
                    string localCachePath = Path.Combine(ExpertManagerPlugin.FAC_DataFolder, "BanPlayers.txt");
                    if (!File.Exists(localCachePath)) return;

                    string localData = File.ReadAllText(localCachePath, Encoding.UTF8);
                    using (StringReader reader = new StringReader(localData)) 
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null) 
                        {
                            string trimmed = line.Trim();
                            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("[")) continue;
                            cloudBannedCache.Add(trimmed.ToLowerInvariant());
                        }
                    }
                    System.Console.WriteLine($"[EAC GATEKEEPER]: Core RAM cache updated. Active signatures: {cloudBannedCache.Count}");
                }
            } 
            catch { }
        }

        public static bool IsPlayerCloudBanned(string friendCode, string playerName = "", string puid = "")
        {
            lock (cloudLock) {
                if (string.IsNullOrWhiteSpace(friendCode)) return false;
                return cloudBannedCache.Contains(friendCode.Trim().ToLowerInvariant())
                    || cloudBannedCache.Contains(playerName.Trim().ToLowerInvariant())
                    || cloudBannedCache.Contains(puid.Trim().ToLowerInvariant());
            }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), "OnPlayerJoined")]
    public static class CloudBanEnforcerPatch
    {
        [HarmonyPostfix]
        public static void Postfix([HarmonyArgument(0)] ClientData client)
        {
            if (client == null || client.Character == null || client.Character.Data == null) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            string friendCode = client.Character.Data.FriendCode;
            string playerName = client.Character.Data.PlayerName ?? "Unknown";
            string puid = client.Id.ToString();
            int colorId = client.Character.Data.DefaultOutfit != null ? client.Character.Data.DefaultOutfit.ColorId : -1;

            if (string.IsNullOrWhiteSpace(friendCode) || friendCode.Trim().ToLowerInvariant() == "no-code")
            {
                global::AmongUsClient.Instance.KickPlayer(client.Id, true);
                return;
            }

            if (colorId == 18)
            {
                global::AmongUsClient.Instance.KickPlayer(client.Id, false);
                return;
            }

            if (GlobalBanBridge.IsPlayerCloudBanned(friendCode, playerName, puid))
            {
                global::AmongUsClient.Instance.KickPlayer(client.Id, true);
            }
        }
    }
}
