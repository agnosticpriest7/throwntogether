using UnityEngine;
using UnityEngine.Audio;

namespace ThrownTogether
{
    public sealed class SettingsService : MonoBehaviour
    {
        public AudioMixer mixer;
        public SettingsRepository Repository { get; private set; }
        private void Awake() { Repository=new SettingsRepository(new PlayerPrefsSettingsStorage()); Repository.Load(); }
        // Mixer exposed parameters must be applied after Awake/OnEnable.
        private void Start() => Apply();
        public bool Apply()
        {
            if(mixer == null || Repository == null) return false;
            var data=Repository.Audio; data.Normalize();
            bool valid=mixer.SetFloat("MasterVolume",AudioSettingsData.Decibels(data.master));
            valid &= mixer.SetFloat("MusicVolume",AudioSettingsData.Decibels(data.music));
            valid &= mixer.SetFloat("SFXVolume",AudioSettingsData.Decibels(data.sfx));
            valid &= mixer.SetFloat("UIVolume",AudioSettingsData.Decibels(data.ui));
            valid &= mixer.SetFloat("AmbienceVolume",AudioSettingsData.Decibels(data.ambience));
            return valid;
        }
        public bool Save() => Repository != null && Repository.Save();
    }
}
