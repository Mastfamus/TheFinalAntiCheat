using System;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmongUsPCMod
{
    // ============================================================================
    // 📊 THE FAC ENGINE TRACKER: HIGH-FPS BOOST & GEOMETRIC CANVAS MATRIX (v17.5.5)
    // ============================================================================
    [HarmonyPatch]
    public static class TheFACEngineTracker
    {
        private static GameObject trackerObj;
        private static TextMeshPro trackerText;
        private static GameObject chatThanksObj;
        private static float fpsTimer = 0f;
        private static int fpsCounter = 0;
        private static int currentFpsOutput = 60;
        private static float[] lastPacketTimestamps = new float[256]; 

        [HarmonyPatch(typeof(global::HudManager), nameof(global::HudManager.Start))]
        [HarmonyPostfix]
        public static void HudManager_Start_Postfix(global::HudManager __instance)
        {
            if (__instance == null || __instance.transform == null) return;

            try
            {
                QualitySettings.vSyncCount = 0; 
                int monitorHz = (int)Math.Round(Screen.currentResolution.refreshRateRatio.value);
                Application.targetFrameRate = monitorHz > 60 ? monitorHz : 165;

                // 📊 Диагностичен панел (Горе вдясно)
                trackerObj = new GameObject("TheFAC_PerformanceTracker");
                trackerObj.transform.SetParent(__instance.transform, false);
                
                var aspect = trackerObj.AddComponent<global::AspectPosition>();
                aspect.Alignment = AspectPosition.EdgeAlignments.RightTop;
                aspect.DistanceFromEdge = new Vector3(0.5f, 0.9f, -5f); 

                trackerText = trackerObj.AddComponent<TextMeshPro>();
                trackerText.fontSize = 3.8f; 
                trackerText.alignment = TextAlignmentOptions.Right;
                trackerText.outlineColor = Color.black;
                trackerText.outlineWidth = 0.28f;

                trackerObj.AddComponent<FACUpdateBridge>();

                // ============================================================================
                // 💜 АДАПТИВНА UI ЗАСТРАХОВКА ЗА "THANKS" ЕТИКЕТА (by Mastfamus Еволюция)
                // ============================================================================
                if (__instance.Chat != null && __instance.Chat.gameObject != null)
                {
                    Transform sendBtnTransform = __instance.Chat.transform.Find("SendButton") ?? 
                                                 __instance.Chat.transform.Find("Content/SendButton");

                    chatThanksObj = new GameObject("TheFAC_ChatThanksLabel");
                    chatThanksObj.transform.SetParent(__instance.Chat.transform, false);
                    chatThanksObj.transform.localPosition = new Vector3(1.95f, -1.6f, -1f);

                    var thanksTmp = chatThanksObj.GetComponent<TextMeshPro>();
                    if (thanksTmp == null) thanksTmp = chatThanksObj.AddComponent<TextMeshPro>();

                    thanksTmp.fontSize = 1.3f;
                    thanksTmp.fontStyle = FontStyles.Bold;
                    thanksTmp.alignment = TextAlignmentOptions.Center;
                    thanksTmp.outlineColor = Color.black;
                    thanksTmp.outlineWidth = 0.15f;

                    // Изграждаме марковия Rich Text под кутията
                    StringBuilder chatUiBuilder = new StringBuilder();
                    chatUiBuilder.Append("<color=#8A2BE2><b>Thanks for downloading and using The FAC!</b></color>\n");
                    chatUiBuilder.Append("<color=#8A2BE2><b>The Final Anti-Cheat v16.5.0</b></color>\n");
                    chatUiBuilder.Append("by <color=#6b2fbb><i>Mastfamus</i></color>\n"); 
                    chatUiBuilder.Append("<color=#8A8A8A><size=70%>Engine Overclocked & Secured</size></color>");
                    thanksTmp.text = chatUiBuilder.ToString();

                    if (sendBtnTransform != null && sendBtnTransform.gameObject.activeInHierarchy)
                    {
                        chatThanksObj.transform.SetParent(sendBtnTransform, false);
                        chatThanksObj.transform.localPosition = new Vector3(0f, 0.75f, -1f); 
                    }
                    else if (__instance.Chat.InputField != null)
                    {
                        chatThanksObj.transform.SetParent(__instance.Chat.InputField.transform, false);
                        chatThanksObj.transform.localPosition = new Vector3(0f, -0.75f, -1f); 
                    }
                    chatThanksObj.SetActive(true);
                }
            }
            catch { }
        }

        public static void RefreshTrackerMetrics()
        {
            if (trackerText == null || AmongUsClient.Instance == null) return;
            try
            {
                fpsCounter++;
                fpsTimer += Time.deltaTime;
                if (fpsTimer >= 1.0f)
                {
                    currentFpsOutput = fpsCounter;
                    fpsCounter = 0;
                    fpsTimer = 0f;
                }

                StringBuilder sb = new StringBuilder();
                if (AmongUsClient.Instance.InOnlineScene)
                {
                    sb.Append($"PING: <b>{AmongUsClient.Instance.Ping} ms</b>\n");
                }
                sb.Append($"FPS: <b>{currentFpsOutput}</b>\n");

                if (ExpertManagerPlugin.isTimerTracking && global::LobbyBehaviour.Instance != null)
                {
                    float timeInLobby = UnityEngine.Time.time - ExpertManagerPlugin.lobbyEnterTime;
                    sb.Append($"<size=180%><color=#8A2BE2><b>LOBBY TIMER: {TimeSpan.FromSeconds(timeInLobby).ToString(@"mm\:ss")}</b></color></size>");
                }

                trackerText.SetText(sb.ToString());
            }
            catch { }
        }

        public static void ProcessRemotePlayerFpsLock(global::PlayerControl player)
        {
            if (player == null || player.AmOwner || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            if (ExpertManagerPlugin.EnableFpsShocker == null || !ExpertManagerPlugin.EnableFpsShocker.Value) return;

            try
            {
                byte pId = player.PlayerId;
                float currentRealTime = UnityEngine.Time.time;
                float timeSinceLastUpdate = currentRealTime - lastPacketTimestamps[pId];
                lastPacketTimestamps[pId] = currentRealTime;

                if (timeSinceLastUpdate > 0.08f && timeSinceLastUpdate < 2.0f && player.cosmetics != null)
                {
                    player.transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(global::PlayerControl), nameof(global::PlayerControl.FixedUpdate))]
    public static class FACPerformanceShockerBridge
    {
        [HarmonyPostfix]
        public static void Postfix(global::PlayerControl __instance)
        {
            TheFACEngineTracker.ProcessRemotePlayerFpsLock(__instance);
        }
    }

    [RegisterInIl2Cpp]
    public class FACUpdateBridge : MonoBehaviour
    {
        private void Update()
        {
            TheFACEngineTracker.RefreshTrackerMetrics();
        }
    }
}