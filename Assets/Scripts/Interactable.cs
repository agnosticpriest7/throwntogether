using System.Collections.Generic;
using UnityEngine;

namespace ThrownTogether
{
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> Active = new List<Interactable>();
        public string stationName;
        // Presentation only; scaled time freezes cues while paused.
        private float successUntil;
        public bool SuccessCheck { get; private set; }
        public float SuccessOpacity => Time.time < successUntil
            ? (RestaurantMenu.Display.reducedEffects ? 1 : Mathf.Clamp01((successUntil-Time.time)/.25f)) : 0;
        public void ShowSuccess(bool check = false)
        {
            SuccessCheck=check; successUntil=Time.time+(check ? .85f:.45f);
        }
        protected virtual void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        protected virtual void OnDisable() { Active.Remove(this); successUntil=0; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Active.Clear();
        public abstract string Prompt(ChefController chef);
        public abstract bool Interact(ChefController chef);
        public virtual string Status => stationName;
        public virtual float Progress => -1;
    }
}
