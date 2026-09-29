using UnityEngine;

namespace AmongUsPCMod
{
    public static class AudioSourceHelper
    {
        public static float GetPlaybackTime(this AudioSource source)
        {
            return source != null ? source.time : 0f;
        }

        public static float GetPlaybackProgress(this AudioSource source)
        {
            if (source == null || source.clip == null) return 0f;
            return Mathf.Clamp01(source.time / source.clip.length);
        }

        public static float GetRemainingTime(this AudioSource source)
        {
            if (source == null || source.clip == null) return 0f;
            return source.clip.length - source.time;
        }

        public static bool IsPlaying(this AudioSource source)
        {
            return source != null && source.isPlaying;
        }
    }
}