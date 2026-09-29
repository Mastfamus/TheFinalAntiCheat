using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using NAudio.Wave;

namespace AmongUsPCMod;

public static class AudioLoader
{
    private static readonly System.Collections.Generic.HashSet<int> supportedBits = new () { 8, 16 ,24, 32, 64};

    public static async Task<AudioClip> LoadAudioClipAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".wav" => await LoadWavAsync(filePath),
            ".mp3" or ".aiff" or ".aif" or ".flac" => await LoadWithNAudioAsync(filePath),
            _ => null
        };
    }

    private static async Task<AudioClip> LoadWavAsync(string path)
    {
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var bytes = new byte[fs.Length];
        _ = await fs.ReadAsync(bytes.AsMemory(0, (int)fs.Length));
        fs.Close();

        var name = Path.GetFileNameWithoutExtension(path);
        return await CreateClipFromRaw(bytes, name);
    }

    private static async Task<AudioClip> LoadWithNAudioAsync(string path)
    {
        return await Task.Run(() =>
        {
            using var reader = new AudioFileReader(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var fmt = reader.WaveFormat;

            var data = new float[reader.Length / (fmt.BitsPerSample / 8)];
            var read = reader.Read(data, 0, data.Length);
            
            var clip = AudioClip.Create(name, read / fmt.Channels, fmt.Channels, fmt.SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });
    }

    private static async Task<AudioClip> CreateClipFromRaw(byte[] raw, string name)
    {
        var dst = new float[raw.Length / 2];
        unsafe {
            fixed (byte* pByte = raw) fixed (float* pFloat = dst) {
                var pShort = (short*)pByte;
                for (var i = 0; i < dst.Length; i++) pFloat[i] = pShort[i] / 32768f;
            }
        }
        var clip = AudioClip.Create(name, dst.Length / 2, 2, 44100, false);
        clip.SetData(dst, 0);
        return clip;
    }
}