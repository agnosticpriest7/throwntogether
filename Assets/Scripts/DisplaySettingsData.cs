using System;
using UnityEngine;
namespace ThrownTogether
{
    [Serializable] public sealed class DisplaySettingsData
    {
        public int textSize;
        public bool highContrast, reducedEffects;
        public float TextScale => 1 + Mathf.Clamp(textSize,0,2)*.15f;
        public void Normalize() => textSize=Mathf.Clamp(textSize,0,2);
    }
}
