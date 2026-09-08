using System.Runtime.InteropServices;
using UnityEngine;

namespace ThrownTogether
{
    public static class WebInputFocus
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern bool TT_HasInputFocus();
        [DllImport("__Internal")] private static extern string TT_BrowserGamepads();
        public static bool HasFocus => TT_HasInputFocus();
        public static string BrowserGamepads => TT_BrowserGamepads();
#else
        public static bool HasFocus => Application.isFocused;
        public static string BrowserGamepads => "Not a browser (Editor/native)";
#endif
    }
}
