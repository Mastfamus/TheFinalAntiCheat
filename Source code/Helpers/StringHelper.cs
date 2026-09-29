using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

namespace AmongUsPCMod;

    public static class StringHelper
    {
        private static readonly Encoding shiftJIS = CodePagesEncodingProvider.Instance.GetEncoding("Shift_JIS");

        public static string ColorString(Color32 color, string str)
        {
            return $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
        }

        public static string ColorString(Color color, string str)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
        }

        public static string Mark(this string self, Color color, bool bright = true)
        {
            var markingColor = color.ToMarkingColor(bright);
            var markingColorCode = ColorUtility.ToHtmlStringRGBA(markingColor);
            return $"<mark=#{markingColorCode}>{self}</mark>";
        }

        public static int GetByteCount(this string self)
        {
            return shiftJIS.GetByteCount(self);
        }

        public static string RemoveHtmlTags(this string self)
        {
            return Regex.Replace(self, "<[^>]*?>", string.Empty);
        }

        public static string RemoveHtmlTagsExcept(this string self, string exceptionLabel)
        {
            return Regex.Replace(self, "<(?!/*" + exceptionLabel + ")[^>]*?>", string.Empty);
        }

        public static string RemoveColorTags(this string self)
        {
            return Regex.Replace(self, "</?color(=#[0-9a-fA-F]*)?>", "");
        }
    }
