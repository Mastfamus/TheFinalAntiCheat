using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;

namespace AmongUsPCMod
{
    public static class TheFACCloudEngine
    {
        private const string BaseCdnUrl = "https://raw.githubusercontent.com/Mastfamus/TheFinalAntiCheat";

        public static readonly string EacFileName = "EAC.txt";
        public static readonly string ModStampFileName = "Modstamp.png";
        public static readonly string GavelCursorFileName = "GavelCursor.png";

        private static readonly List<string> BackgroundPool = new List<string>
        {
            "JapaneseTemple.png", "Mountain.png", "PhoneNeonBean.png", "RailTracks.png", "Squarles.png", "Waterfall.png"
        };

        public static string ActiveBackgroundName = "Mountain.png"; 
        public static Texture2D ActiveBackgroundTexture { get; private set; }

        public static string GetSecureDataFolder()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return Path.Combine(Application.persistentDataPath, "STAR_Data");
            }
            string pcPath = Path.Combine(Paths.GameRootPath, "STAR_Data");
            if (!Directory.Exists(pcPath)) Directory.CreateDirectory(pcPath);
            return pcPath;
        }

        public static string GetMusicFolder()
        {
            string musicPath = Path.Combine(GetSecureDataFolder(), "Musics");
            if (!Directory.Exists(musicPath)) Directory.CreateDirectory(musicPath);
            return musicPath;
        }

        public static void SyncSovereignAssets()
        {
            DateTime today = DateTime.Today;
            if (today.Month == 6 && today.Day == 15) ActiveBackgroundName = "Bday.png";
            else if (today.Month == 9 && today.Day == 26) ActiveBackgroundName = "Bday.png";
            else
            {
                var random = new System.Random();
                ActiveBackgroundName = BackgroundPool[random.Next(BackgroundPool.Count)];
            }

            if (AmongUsClient.Instance != null)
            {
                AmongUsClient.Instance.StartCoroutine(CoDownloadTextAsset(EacFileName, rawText => {
                    if (!string.IsNullOrWhiteSpace(rawText)) {
                        lock (ExpertManagerPlugin.WordBlackList) {
                            string[] lines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines) {
                                string trimmed = line.Trim().ToLowerInvariant();
                                if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("[")) {
                                    ExpertManagerPlugin.bannedPuidsAndCodes.Add(trimmed);
                                }
                            }
                        }
                    }
                }));

                AmongUsClient.Instance.StartCoroutine(CoDownloadTextureLayer(ModStampFileName, tex => {
                    ExpertManagerPlugin.CustomModStampTexture = tex;
                }));

                AmongUsClient.Instance.StartCoroutine(CoDownloadTextureLayer(GavelCursorFileName, tex => {
                    Cursor.SetCursor(tex, Vector2.zero, CursorMode.ForceSoftware);
                }));

                AmongUsClient.Instance.StartCoroutine(CoDownloadTextureLayer(ActiveBackgroundName, tex => {
                    ActiveBackgroundTexture = tex;
                }));
            }
        }

        private static IEnumerator CoDownloadTextAsset(string fileName, Action<string> callback)
        {
            string url = BaseCdnUrl + fileName;
            using (UnityWebRequest www = UnityWebRequest.Get(url)) {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success) {
                    string localPath = Path.Combine(GetSecureDataFolder(), fileName);
                    File.WriteAllText(localPath, www.downloadHandler.text);
                    callback?.Invoke(www.downloadHandler.text);
                } else {
                    string localPath = Path.Combine(GetSecureDataFolder(), fileName);
                    if (File.Exists(localPath)) callback?.Invoke(File.ReadAllText(localPath));
                }
            }
        }

        private static IEnumerator CoDownloadTextureLayer(string fileName, Action<Texture2D> callback)
        {
            string url = BaseCdnUrl + fileName;
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url)) {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success) {
                    Texture2D tex = DownloadHandlerTexture.GetContent(www);
                    tex.Apply(false, true);
                    File.WriteAllBytes(Path.Combine(GetSecureDataFolder(), fileName), www.downloadHandler.data);
                    callback?.Invoke(tex);
                } else {
                    string localPath = Path.Combine(GetSecureDataFolder(), fileName);
                    if (File.Exists(localPath)) {
                        byte[] bytes = File.ReadAllBytes(localPath);
                        Texture2D tex = new Texture2D(2, 2);
                        if (ImageConversion.LoadImage(tex, bytes)) callback?.Invoke(tex);
                    }
                }
            }
        }
    }
}