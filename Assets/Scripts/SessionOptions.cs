using UnityEngine;
namespace ThrownTogether
{
    // A session-only choice: no restaurant progression or balance is persisted.
    public static class SessionOptions
    {
        public static int ShiftOrders { get; set; }=0;
        public static int Kitchen { get; set; }
        public static bool KeyboardPlayerOne { get; set; }
        public static string Training { get; set; }="Free practice";
        public static string ShiftLabel => ShiftOrders==0 ? "Restaurant day (5 minutes)" : ShiftOrders==3 ? "Practice (3 dishes)" : ShiftOrders==12 ? "Practice (12 dishes)" : "Practice (6 dishes)";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { ShiftOrders=0; Kitchen=0; KeyboardPlayerOne=false; Training="Free practice"; }
    }
}
