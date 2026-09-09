using System;
using UnityEngine;

namespace ThrownTogether
{
    [Serializable]
    public sealed class AudioSettingsData
    {
        public float master=1, music=.6f, sfx=1, ui=1, ambience=1;
        public void Normalize()
        { master=Safe(master); music=Safe(music); sfx=Safe(sfx); ui=Safe(ui); ambience=Safe(ambience); }
        public static float Safe(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1 : Mathf.Clamp01(value);
        public static float Decibels(float linear) => linear <= 0 ? -80 : Mathf.Max(-80,20*Mathf.Log10(Safe(linear)));
    }
}
