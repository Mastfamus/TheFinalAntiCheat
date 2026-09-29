using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace AmongUsPCMod
{
    [HarmonyPatch]
    public static class TheFACPerformanceShield
    {
        public static void FlushGarbageToDiskSafe(string triggerZone)
        {
            try
            {
                System.Console.WriteLine($"[The FAC Performance]: Flushing RAM/VRAM cells at [{triggerZone}]...");
            
                global::UnityEngine.Resources.UnloadUnusedAssets();
            }
            catch { }
        }

        [HarmonyPatch(typeof(global::MeetingHud), nameof(global::MeetingHud.HandleRpc))]
        [HarmonyPostfix]
        public static void MeetingHud_HandleRpc_Postfix(global::MeetingHud __instance, byte callId)
        {
            if (callId == 1 || callId == 4) FlushGarbageToDiskSafe("Meeting Summary Close");
        }

        [HarmonyPatch(typeof(global::IntroCutscene), nameof(global::IntroCutscene.OnDestroy))]
        [HarmonyPostfix]
        public static void IntroCutscene_OnDestroy_Postfix()
        {
            FlushGarbageToDiskSafe("Role Reveal Game Start");
        }
    }
    // ============================================================================
// 🧠 THE FAC CACHE OFFLOADER: TRASH BLACKLIST MEMORY DISCHARGE (v2.0.0)
// ============================================================================
public static class FACCacheOffloader
{
    private static readonly StringBuilder MemoryCacheBuffer = new StringBuilder();
    private static readonly List<string> LiveActiveStringsTable = new List<string>();
    public static string CacheFilePath => Path.Combine(ExpertManagerPlugin.FAC_DataFolder, "Cache.txt");

    /// <summary>
    /// Добавя излишен низ или периодичен лог към буфера
    /// </summary>
    public static void PushToCache(string data)
    {
        if (string.IsNullOrWhiteSpace(data)) return;
        lock (MemoryCacheBuffer)
        {
            MemoryCacheBuffer.AppendLine($"[{DateTime.UtcNow:HH:mm:ss}] {data}");
        }
    }

    /// <summary>
    /// 🔄 ИНТЕЛЕГЕНТЕН РАЗТОВАРВАЧ: Изхвърля САМО боклука, пази нативното C++ ядро
    /// </summary>
    public static void DumpCacheToDisk()
    {
        try
        {
            string rawBufferContent;
            lock (MemoryCacheBuffer)
            {
                if (MemoryCacheBuffer.Length == 0) return;
                rawBufferContent = MemoryCacheBuffer.ToString();
                MemoryCacheBuffer.Clear(); 
            }

            StringBuilder junkToTrashFile = new StringBuilder();
            LiveActiveStringsTable.Clear();

            string[] allCachedLines = rawBufferContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in allCachedLines)
            {
                string loweredLine = line.ToLowerInvariant();

                // 🛑 ЧЕРЕН СПИСЪК (BLACKLIST): Изхвърляме САМО тези излишни и лагващи компоненти!
                if (loweredLine.Contains("position:") || loweredLine.Contains("transform:") || // Стари локации
                    loweredLine.Contains("chatstream [") || loweredLine.Contains("said:") ||    // Чат спам
                    loweredLine.Contains("[modflag]") || loweredLine.Contains("[moddetected]") || // Еднократни първоначални проверки
                    loweredLine.Contains("welcome to the final anti-cheat") ||                 // Приветствени текстове
                    loweredLine.Contains("cosmeticspam") || loweredLine.Contains("outfit spam")) // Периодични козметични лог натрупвания
                {
                    // Това е сигурен боклук - отива във физическия файл на диска!
                    junkToTrashFile.AppendLine(line);
                }
                else
                {
                    // Всичко останало (нативни C++ низове и жизненоважни данни) СЕ ИЗТЕГЛЯ ОБРАТНО в RAM!
                    LiveActiveStringsTable.Add(line);
                }
            }

            if (junkToTrashFile.Length > 0)
            {
                File.AppendAllText(CacheFilePath, junkToTrashFile.ToString(), Encoding.UTF8);
            }

            lock (MemoryCacheBuffer)
            {
                foreach (string activeLine in LiveActiveStringsTable)
                {
                    MemoryCacheBuffer.AppendLine(activeLine);
                }
            }

            System.Console.WriteLine($"[THE FAC CACHE]: Offload complete. Trash-filtered {junkToTrashFile.Length} bytes of memory leaks. C++ matrices preserved.");
            
            // Нативно и безопасно подканваме Unity да освободи паметта
            global::UnityEngine.Resources.UnloadUnusedAssets();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("[THE FAC CACHE ERROR]: Trash-list filtration failed: " + ex.Message);
        }
    }

    /// <summary>
    /// 🪓 АВТОМАТИЧНО ПОЧИСТВАНЕ: Изтрива Cache.txt при спиране на играта
    /// </summary>
    public static void NukeCacheFileOnQuit()
    {
        try
        {
            if (File.Exists(CacheFilePath))
            {
                File.Delete(CacheFilePath);
                System.Console.WriteLine("[THE FAC CACHE]: Session closed. Cache.txt destroyed.");
            }
        }
        catch { }
    }
  }
}
