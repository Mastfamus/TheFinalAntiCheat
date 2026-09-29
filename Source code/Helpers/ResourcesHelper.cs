using System.Collections.Generic;

namespace AmongUsPCMod
{
    public static class ResourcesHelper
    {
        public static List<string> PreReadyRemoteImageList = new List<string>()
        {
            "Modstamp.png",
            "GavelCursur.png",
            "EAC.txt"
        };

        public static List<string> RemoteImageList = new List<string>();

        public static List<string> RemoteDependList = new List<string>()
        {
#if Windows
            "NAudio.dll",
            "NAudio.Asio.dll",
            "NAudio.Core.dll",
            "NAudio.Flac.dll",
            "NAudio.Wasapi.dll",
            "NAudio.WinMM.dll",
#endif
            "YamlDotNet.dll",
            "YamlDotNet.xml"
        };

        public static readonly List<string> RemoteModNewsList = new List<string>();
    }
}