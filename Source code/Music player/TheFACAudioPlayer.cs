using System;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx.Unity.IL2CPP.Utils;
using Object = UnityEngine.Object;

namespace AmongUsPCMod;

public static class AudioPlayer
{
    private static FinalMusic _currentMusic;

    public static FinalMusic CurrentMusic
    {
        get => _currentMusic ?? FinalMusic.Musics?.FirstOrDefault(x => x.CurrentAudioStates is AudiosStates.Playing or AudiosStates.Pausing);
        private set => _currentMusic = value;
    }

    public static async void Play(FinalMusic audio, bool asMainMenuMusic = false)
    {
        try
        {
            if (audio.CurrentAudioStates is AudiosStates.NotExist or AudiosStates.Playing) return;
            StopPlayMod(true);
            await audio.Load();

            _ = new MainThreadTask(() =>
            {
                foreach (var file in FinalMusic.Musics.Where(file => file.FileName == audio.FileName))
                {
                    file.CurrentAudioStates = AudiosStates.Playing;
                    file.PlayAsMainMenuMusic = asMainMenuMusic;
                }

                MyMusicPanel.RefreshTagList();
                SoundManager.Instance.CrossFadeSound(audio.FileName, audio.Clip, 0.7f);
                CurrentMusic = audio;
            }, "Start Play");
        }
        catch { }
    }

    public static void StopPlayMod(bool playNew = false)
    {
        FinalMusic.Musics.ForEach(x =>
        {
            Object.Destroy(x.Clip);
            x.Clip = null;
            x.CurrentAudioStates = x.LastAudioStates;
            x.PlayAsMainMenuMusic = false;
            SoundManager.Instance.StopNamedSound(x.FileName);
        });
        CurrentMusic = null;
        _ = new MainThreadTask(MyMusicPanel.RefreshTagList, "Refresh Tag List");
    }
}

[HarmonyPatch(typeof(SoundManager), nameof(SoundManager.PlaySound))]
public class PlaySoundPatch {
    public static bool Prefix() => !FinalMusic.Musics.Any(x => x.CurrentAudioStates == AudiosStates.Playing);
}

[HarmonyPatch(typeof(SoundManager), nameof(SoundManager.CrossFadeSound))]
public class CrossFadeSoundPatch {
    public static bool Prefix(string name) => name != "MainBG" && (FinalMusic.Musics.Any(x => x.FileName == name) || !FinalMusic.Musics.Any(x => x.CurrentAudioStates == AudiosStates.Playing));
}