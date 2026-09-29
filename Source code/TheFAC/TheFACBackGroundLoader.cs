using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🌍 THE FAC CLOUD ENGINE: MULTI-BACKGROUND ROTATOR & BDAY DETECTOR (v16.8.5)
    // ============================================================================
    public static class TheFACCloudEngine
    {
        private static readonly string BaseCdnUrl = "https://jsdelivr.net";

        public static readonly string EacFileName = "EAC.cs";
        public static readonly string ModStampFileName = "Modstamp.png";
        public static readonly string GavelCursorFileName = "Gavelcursor.png";

        // 🎨 ТВОЯТ ПЪЛЕН СПИСЪК СЪС СПЕЦИФИЧНИ ФОНОВЕ!
        private static readonly List<string> BackgroundPool = new List<string>
        {
            "JapaneseTemple.png",
            "Mountain.png",
            "PhoneNeonBean.png",
            "RailTracks.png",
            "Squarles.png",
            "Waterfall.png"
        };

        public static string ActiveBackgroundName = "Mountain.png"; // Fallback по подразбиране

        public static void SyncSovereignAssets()
        {
            System.Console.WriteLine("[The FAC Cloud Engine]: Triggering parallel CDN asset matrix...");

            // ============================================================================
            // 🎂 АВТОМАТИЧЕН ИНТЕЛИГЕНТЕН ДЕТЕКТОР ЗА РОЖДЕН ДЕН (Bday.png)
            // ============================================================================
            // Когато решиш кога е официалната дата, ще я набием тук. Засега проверяваме системния часовник:
            DateTime today = DateTime.Today;
            
            // Пример: Ако е денят на релиза (напр. 19 Септември), форсираме празничния фон за 24 часа!
            if (today.Month == 6 && today.Day == 15) 
            {
                ActiveBackgroundName = "Bday.png";
                System.Console.WriteLine("[The FAC Event Matrix]: Happy Birthday AmongUs! Forcing Bday.png for 24 hours!");
            }
            else
            {
                // През останалото време: Избира напълно случаен фон от твоя списък!
                var random = new System.Random();
                int randomIndex = random.Next(BackgroundPool.Count);
                ActiveBackgroundName = BackgroundPool[randomIndex];
                System.Console.WriteLine($"[The FAC Event Matrix]: Standard day. Rolled random background vector: {ActiveBackgroundName}");
            }

            // 📡 1. СВАЛЯНЕ НА ГЛОБАЛНИЯ EAC БАН СПИСЪК
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
                    System.Console.WriteLine("[The FAC Cloud Engine SUCCESS]: Global EAC.cs ban list synced successfully.");
                }
            }));

            // 📡 2. СВАЛЯНЕ НА ЛИЛАВОТО ЛОГО (Modstamp)
            AmongUsClient.Instance.StartCoroutine(CoDownloadTextureLayer(ModStampFileName, tex => {
                ExpertManagerPlugin.CustomModStampTexture = tex;
                System.Console.WriteLine("[The FAC Cloud Engine SUCCESS]: Modstamp.png rendered beautifully.");
            }));

            // 📡 3. СВАЛЯНЕ НА СЪДИЙСКОТО ЧУКЧЕ (Gavelcursor)
            AmongUsClient.Instance.StartCoroutine(CoDownloadTextureLayer(GavelCursorFileName, tex => {
                Cursor.SetCursor(tex, Vector2.zero, CursorMode.ForceSoftware);
                System.Console.WriteLine("[The FAC Cloud Engine SUCCESS]: Custom Gavel hardware cursor enforced.");
            }));

            // 📡 4. СВАЛЯНЕ НА АКТИВНИЯ СЕЛЕКТИРАН ФОН (Случаен или Bday!)
            AmongUsClient.Instance.StartCoroutine(CoDownloadTextureLayer(ActiveBackgroundName, tex => {
                System.Console.WriteLine($"[The FAC Cloud Engine SUCCESS]: Active background '{ActiveBackgroundName}' successfully bound to VRAM.");
            }));
        }

        private static System.Collections.IEnumerator CoDownloadTextAsset(string fileName, Action<string> callback)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(BaseCdnUrl + "TheFAC_Data/BanPlayers.txt")) {
                www.SetRequestHeader("User-Agent", "AmongUsPCMod-TheFAC-EACLoader");
                www.SetRequestHeader("Accept", "text/plain");
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success) callback?.Invoke(www.downloadHandler.text);
                else ExpertManagerPlugin.LoadAllFiles();
            }
        }

        private static System.Collections.IEnumerator CoDownloadTextureLayer(string fileName, Action<Texture2D> callback)
        {
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(BaseCdnUrl + fileName)) {
                www.SetRequestHeader("User-Agent", "AmongUsPCMod-TheFAC-ImageLoader");
                www.SetRequestHeader("Accept", "image/png");
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success) {
                    Texture2D tex = DownloadHandlerTexture.GetContent(www);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.Apply(false, true);
                    callback?.Invoke(tex);
                }
            }
        }
    }
}