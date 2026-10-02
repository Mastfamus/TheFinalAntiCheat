using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using InnerNet;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace AmongUsPCMod 
{
public static class ModStamp
{
    public static void Initialize()
    {
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)((scene, _) =>
        {
            if (scene.name == "MainMenu")
            {
                ModManager.Instance.ShowModStamp();
            }
        }));
    }
}
    [HarmonyPatch]
    public static class TheFACAssetEngine
    {
        public static UnityEngine.Texture2D CustomModStampTexture = null;
        public static UnityEngine.Texture2D GavelCursorTexture = null;
        
        public static System.Collections.Generic.List<string> jukeboxPlaylist = new System.Collections.Generic.List<string>();
        public static int currentTrackIndex = 0;
        public static bool isMusicEnabled = true;
        public static bool forceSingleTrackLoop = false;

        public static void InitializeLocalCache()
        {
            try
            {
                CustomModStampTexture = LoadTextureFromFACData("Modstamp.png");
                if (CustomModStampTexture != null)
                {
                    System.Console.WriteLine("[The FAC AssetEngine]: Modstamp successfully cached via raw IL2CPP bytes.");
                }

                GavelCursorTexture = LoadSpriteFromFACData("Gavelcursor.png");
                if (GavelCursorTexture != null)
                {
                    System.Console.WriteLine("[The FAC AssetEngine]: Gavel Cursor successfully cached via raw IL2CPP bytes.");
                    ApplyCustomCursorNow();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[The FAC AssetEngine Error]: " + ex.Message);
            }
        }

        private static UnityEngine.Texture2D LoadTextureFromFACData(string fileName)
        {
            try
            {
                string filePath = Path.Combine(ExpertManagerPlugin.FAC_DataFolder, fileName);
                if (!File.Exists(filePath)) return null;

                byte[] rawBytes = File.ReadAllBytes(filePath);
                UnityEngine.Texture2D texture = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, false);

                if (BepInEx.Unity.IL2CPP.Utils.ImageConversion.LoadImage(texture, rawBytes))
                {
                    texture.filterMode = UnityEngine.FilterMode.Bilinear;
                    return texture;
                }
            }
            catch { }
            return null;
        }

        public static void ApplyCustomCursorNow()
        {
            try
            {
                RuntimePlatform platform = UnityEngine.Application.platform;
                if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.OSXPlayer)
                {
                    if (GavelCursorTexture != null)
                    {
                        UnityEngine.Cursor.SetCursor(GavelCursorTexture, UnityEngine.Vector2.zero, UnityEngine.CursorMode.Auto);
                    }
                }
            }
            catch { }
        }

        // ============================================================================
        // 🎵 ЮВЕЛИРЕН JUKEBOX ПЛЕЙЛИСТ МОДУЛ: Върти всички песни в папката
        // ============================================================================
        public static void LoadJukeboxTracksFromDisk()
        {
            try
            {
                string musicFolder = Path.Combine(ExpertManagerPlugin.FAC_DataFolder, "BackgroundMusic");
                if (!Directory.Exists(musicFolder)) return;

                string[] audioFiles = Directory.GetFiles(musicFolder, "*.wav");
                if (audioFiles.Length == 0) return;

                jukeboxPlaylist.Clear();
                foreach (string file in audioFiles) jukeboxPlaylist.Add(file);

                currentTrackIndex = 0;
                PlayNextJukeboxTrack();
            }
            catch { }
        }

        public static void PlayNextJukeboxTrack()
        {
            if (jukeboxPlaylist.Count == 0 || ExpertManagerPlugin.anticheatAudioSource == null || !isMusicEnabled) return;

            try
            {
                if (currentTrackIndex >= jukeboxPlaylist.Count) currentTrackIndex = 0;

                string targetTrack = jukeboxPlaylist[currentTrackIndex];
                var uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + targetTrack, UnityEngine.AudioType.WAV);
                uwr.SendWebRequest();

                while (!uwr.isDone) { }

                if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr);
                    if (clip != null)
                    {
                        ExpertManagerPlugin.anticheatAudioSource.clip = clip;
                        ExpertManagerPlugin.anticheatAudioSource.loop = forceSingleTrackLoop; 
                        ExpertManagerPlugin.anticheatAudioSource.Play();
                        System.Console.WriteLine($"[The FAC Jukebox]: Now playing: {Path.GetFileName(targetTrack)}");
                    }
                }
            }
            catch { }
        }

        public static void CheckJukeboxQueueStatus()
        {
            if (ExpertManagerPlugin.anticheatAudioSource == null || jukeboxPlaylist.Count == 0 || !isMusicEnabled) return;

            if (!ExpertManagerPlugin.anticheatAudioSource.isPlaying)
            {
                if (!forceSingleTrackLoop)
                {
                    currentTrackIndex++;
                }
                PlayNextJukeboxTrack();
            }
        }
    }

    [HarmonyPatch(typeof(global::ModManager))]
    public static class TheFACModManagerPatch
    {
        [HarmonyPatch(typeof(global::ModManager), nameof(global::ModManager.ShowModStamp))]
        [HarmonyPostfix]
        public static void ShowModStamp_Postfix()
        {
            TheFACAssetEngine.ApplyCustomCursorNow();
        }
    }
}
