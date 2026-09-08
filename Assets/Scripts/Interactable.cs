using System.Collections.Generic;
using UnityEngine;

namespace ThrownTogether
{
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> Active = new List<Interactable>();
        public string stationName;
        protected virtual void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        protected virtual void OnDisable() { Active.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Active.Clear();
        public abstract string Prompt(ChefController chef);
        public abstract bool Interact(ChefController chef);
        public virtual string Status => stationName;
        public virtual float Progress => -1;
    }
}
