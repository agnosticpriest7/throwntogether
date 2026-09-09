using UnityEngine;
namespace ThrownTogether
{
    // A session-only choice: no restaurant progression or balance is persisted.
    public static class SessionOptions
    {
        public static int ShiftOrders { get; set; }=6;
        public static int Kitchen { get; set; }
        public static string Training { get; set; }="Free practice";
        public static string ShiftLabel => ShiftOrders==3 ? "Short (3 dishes)" : ShiftOrders==12 ? "Long (12 dishes)" : "Standard (6 dishes)";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { ShiftOrders=6; Kitchen=0; Training="Free practice"; }
    }
}
