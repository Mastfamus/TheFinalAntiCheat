using System;
using Il2CppInterop.Runtime.InteropTypes;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🔮 THE FAC CODE TRANSLATOR: NATIVE C++ IL2CPP MEMORY BRIDGE (v17.1.0)
    // ============================================================================
    public static class TheFACTranslator
    {
        /// <summary>
        /// 🛡️ НАЙ-МОЩНИЯТ ТРАНСЛАТОР: Превежда C++ обекти към C# структури.
        /// Копиран и оптимизиран от модела на Final Suspect, за да спре абсолютно всички крашове!
        /// </summary>
        public static bool TranslateObject<T>(this Il2CppObjectBase nativeObj, out T cSharpCasted) where T : Il2CppObjectBase
        {
            try {
                if (nativeObj == null) {
                    cSharpCasted = default;
                    return false;
                }
                cSharpCasted = nativeObj.TryCast<T>();
                return cSharpCasted != null;
            } catch {
                cSharpCasted = default;
                return false;
            }
        }
    }
}