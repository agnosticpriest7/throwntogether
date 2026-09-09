using System;
using UnityEngine;

namespace ThrownTogether
{
    public interface ISettingsStorage { string Read(); void Write(string json); }
    public enum SettingsLoadStatus { Defaults, Loaded, Migrated, Invalid, FutureVersion, StorageUnavailable }

    public sealed class SettingsRepository
    {
        public const int CurrentVersion=2;
        [Serializable] private sealed class Envelope { public int schemaVersion; public AudioSettingsData audio; public DisplaySettingsData display; }
        // Version 0 is a documented migration fixture, never a restaurant/progression save.
        [Serializable] private sealed class Legacy { public int schemaVersion; public float masterVolume=1; }
        private readonly ISettingsStorage storage;
        public AudioSettingsData Audio { get; private set; } = new AudioSettingsData();
        public DisplaySettingsData Display { get; private set; } = new DisplaySettingsData();
        public SettingsLoadStatus Status { get; private set; }
        public bool CanSave => Status != SettingsLoadStatus.FutureVersion && Status != SettingsLoadStatus.Invalid && Status != SettingsLoadStatus.StorageUnavailable;
        public SettingsRepository(ISettingsStorage storage) { this.storage=storage; }
        public void Load()
        {
            Audio=new AudioSettingsData();
            Display=new DisplaySettingsData();
            string json;
            try { json=storage.Read(); } catch(Exception) { Status=SettingsLoadStatus.StorageUnavailable; return; }
            if(string.IsNullOrEmpty(json)) { Status=SettingsLoadStatus.Defaults; return; }
            try
            {
                if(!json.TrimStart().StartsWith("{") || !json.Contains("\"schemaVersion\"")) { Status=SettingsLoadStatus.Invalid; return; }
                var data=JsonUtility.FromJson<Envelope>(json);
                if(data.schemaVersion > CurrentVersion) { Status=SettingsLoadStatus.FutureVersion; return; }
                if(data.schemaVersion == 0 && json.Contains("\"masterVolume\""))
                { Audio.master=JsonUtility.FromJson<Legacy>(json).masterVolume; Status=SettingsLoadStatus.Migrated; }
                else if((data.schemaVersion == 1 || data.schemaVersion == CurrentVersion) && json.Contains("\"audio\"") && data.audio != null)
                { Audio=data.audio; Display=data.display ?? new DisplaySettingsData(); Status=data.schemaVersion==CurrentVersion ? SettingsLoadStatus.Loaded : SettingsLoadStatus.Migrated; }
                else { Status=SettingsLoadStatus.Invalid; return; }
                Audio.Normalize();
                Display.Normalize();
            }
            catch(ArgumentException) { Status=SettingsLoadStatus.Invalid; }
        }
        public bool Save()
        {
            if(!CanSave) return false;
            Audio.Normalize();
            Display.Normalize();
            try { storage.Write(JsonUtility.ToJson(new Envelope { schemaVersion=CurrentVersion, audio=Audio, display=Display })); Status=SettingsLoadStatus.Loaded; return true; }
            catch(Exception) { Status=SettingsLoadStatus.StorageUnavailable; return false; }
        }
    }
    public sealed class PlayerPrefsSettingsStorage : ISettingsStorage
    {
        private const string Key="ThrownTogether.Settings";
        public string Read() => PlayerPrefs.GetString(Key,"");
        public void Write(string json)
        {
            if(PlayerPrefs.HasKey(Key)) PlayerPrefs.SetString(Key+".backup",PlayerPrefs.GetString(Key));
            PlayerPrefs.SetString(Key,json); PlayerPrefs.Save();
        }
    }
}
