using System.Runtime.InteropServices;
using UnityEngine;

namespace ThrownTogether
{
    public static class WebInputFocus
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern bool TT_HasInputFocus();
        [DllImport("__Internal")] private static extern bool TT_FocusFromGamepad();
        public static bool FocusFromGamepad()=>TT_FocusFromGamepad();
        [DllImport("__Internal")] private static extern string TT_BrowserGamepads();
        [DllImport("__Internal")] private static extern void TT_SampleGamepadHistory();
        public static void SampleGamepadHistory() => TT_SampleGamepadHistory();
        public static bool HasFocus => TT_HasInputFocus();
        public static string BrowserGamepads => TT_BrowserGamepads();
#else
        public static bool HasFocus => Application.isFocused;
        public static bool FocusFromGamepad()=>Application.isFocused;
        public static void SampleGamepadHistory() { }
        public static string BrowserGamepads => "Not a browser (Editor/native)";
#endif
    }
}
