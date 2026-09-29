using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace AmongUsPCMod;

public static class AudioManager
{
    public static readonly List<string> Extensions = [".zip", ".wav", ".flac", ".aif", ".aiff", ".mp3"];

    public static void ReloadTag()
    {
        try
        {
            Init();
            
            // Чете от вашата сигурна Steam/Android папка STAR_Data
            string musicFolder = AmongUsPCMod.TheFACCloudEngine.GetMusicFolder();
            if (!Directory.Exists(musicFolder)) Directory.CreateDirectory(musicFolder);
            
            var files = Directory.GetFiles(musicFolder);

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName)) continue;

                // Зарежда се безопасно като автономно неофициално парче
                FinalMusic.CreateMusic(fileName, SupportedMusics.UnOfficial);
                System.Console.WriteLine($"[THE FAC AUDIO SUCCESS]: Registered track from STAR_Data: {fileName}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("[FAC Audio Error] Load Audios Failed: \n" + ex);
        }
    }

    private static void Init()
    {
        FinalMusic.InitializeAll();
    }

    public static bool ConvertExtension(ref string path)
    {
        if (path == null) return false;
        var extensions = Extensions.ToArray().ToList();

        while (!File.Exists(path))
        {
            var currentPath = path;
            var extensionsArray = extensions.ToArray();
            if (extensionsArray.Length == 0) return false;
            var matchingKey = extensions.FirstOrDefault(currentPath.Contains);
            if (matchingKey is null) return false;
            var currentIndex = Array.IndexOf(extensionsArray, matchingKey);
            if (currentIndex == -1) return false;

            var nextIndex = (currentIndex + 1) % extensionsArray.Length;
            path = path.Replace(matchingKey, extensionsArray[nextIndex]);
            extensions.Remove(matchingKey);
        }
        return true;
    }
}

public enum SupportedMusics { UnOfficial }
public enum AudiosStates { NotExist, DownLoading, Exist, Playing, Pausing, Parsing }

public class FinalMusic
{
    public static readonly List<FinalMusic> Musics = [];
    private static readonly object finalMusicsLock = new();
    public string Author;
    public UnityEngine.AudioClip Clip;
    public SupportedMusics CurrentAudio;
    public AudiosStates CurrentAudioStates;
    public string Name;
    public string FileName;
    public string FilePath;
    public AudiosStates LastAudioStates;
    public bool PlayAsMainMenuMusic;
    public bool UnOfficial;

    public static void InitializeAll()
    {
        lock (finalMusicsLock)
        {
            if (Musics.Count > 0) return;
        }
        CreateMusic("", SupportedMusics.UnOfficial);
    }

    public static void CreateMusic(string name = "", SupportedMusics music = SupportedMusics.UnOfficial)
    {
        var mus = new FinalMusic();
        mus.Create(name, music);
    }

    public async Task Load()
    {
        if (CurrentAudioStates != AudiosStates.Exist || Clip != null) return;
        var task = AudioLoader.LoadAudioClipAsync(FilePath);
        
        _ = new BepInEx.Unity.IL2CPP.Utils.MainThreadTask(() =>
        {
            LastAudioStates = CurrentAudioStates = AudiosStates.Parsing;
            MyMusicPanel.RefreshTagList();
        }, "Update Audio States Start");
        
        await task;
        
        _ = new BepInEx.Unity.IL2CPP.Utils.MainThreadTask(() =>
        {
            GC.Collect();
            if (task.Result != null) Clip = task.Result;
            LastAudioStates = CurrentAudioStates = Clip ? AudiosStates.Exist : AudiosStates.NotExist;
            MyMusicPanel.RefreshTagList();
        }, "Update Audio States Finish");
    }

    private void Create(string name, SupportedMusics music)
    {
        FileName = Name = name;
        Author = "";
        UnOfficial = true;
        CurrentAudio = music;
        
        FilePath = Path.Combine(AmongUsPCMod.TheFACCloudEngine.GetMusicFolder(), FileName);
        CurrentAudioStates = LastAudioStates = AudioManager.ConvertExtension(ref FilePath) ? AudiosStates.Exist : AudiosStates.NotExist;

        lock (finalMusicsLock)
        {
            var file = Musics.Find(x => x.FileName == FileName);
            if (file != null)
            {
                file.FilePath = FilePath;
                if (CurrentAudioStates is AudiosStates.NotExist) file.CurrentAudioStates = file.LastAudioStates = CurrentAudioStates;
            }
            else if (!string.IsNullOrEmpty(Name))
            {
                Musics.Add(this);
            }
        }
    }
}