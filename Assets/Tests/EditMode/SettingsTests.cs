using NUnit.Framework;
using UnityEngine;

namespace ThrownTogether.Tests
{
    public sealed class SettingsTests
    {
        private sealed class MemoryStorage : ISettingsStorage
        { public string json; public string Read()=>json; public void Write(string value)=>json=value; }
        [Test] public void SettingsRoundTripPreservesAllChannels()
        {
            var store=new MemoryStorage(); var repository=new SettingsRepository(store); repository.Load();
            Assert.That(repository.Status,Is.EqualTo(SettingsLoadStatus.Defaults));
            var a=repository.Audio; a.master=.2f; a.music=.3f; a.sfx=.4f; a.ui=.5f; a.ambience=.6f;
            Assert.That(repository.Save(),Is.True);
            var restored=new SettingsRepository(store); restored.Load();
            Assert.That(restored.Status,Is.EqualTo(SettingsLoadStatus.Loaded));
            Assert.That(new[]{restored.Audio.master,restored.Audio.music,restored.Audio.sfx,restored.Audio.ui,restored.Audio.ambience},Is.EqualTo(new[]{.2f,.3f,.4f,.5f,.6f}));
        }
        [TestCase("{\"schemaVersion\":99,\"audio\":{}}",SettingsLoadStatus.FutureVersion)]
        [TestCase("not json",SettingsLoadStatus.Invalid)]
        [TestCase("{}",SettingsLoadStatus.Invalid)]
        [TestCase("{\"schemaVersion\":1}",SettingsLoadStatus.Invalid)]
        public void UnknownOrInvalidDataIsNeverOverwritten(string json,SettingsLoadStatus status)
        { var store=new MemoryStorage { json=json }; var r=new SettingsRepository(store); r.Load(); Assert.That(r.Status,Is.EqualTo(status)); Assert.That(r.Save(),Is.False); Assert.That(store.json,Is.EqualTo(json)); }
        [Test] public void VersionZeroMigratesWithoutImplicitWrite()
        {
            var store=new MemoryStorage { json="{\"schemaVersion\":0,\"masterVolume\":0.25}" };
            var r=new SettingsRepository(store); r.Load();
            Assert.That(r.Status,Is.EqualTo(SettingsLoadStatus.Migrated)); Assert.That(r.Audio.master,Is.EqualTo(.25f)); Assert.That(r.Audio.sfx,Is.EqualTo(1));
            Assert.That(store.json,Does.Contain("masterVolume")); Assert.That(r.Save(),Is.True); Assert.That(store.json,Does.Contain("\"schemaVersion\":1"));
        }
        [Test] public void AudioLevelsClampAndConvertToMixerDecibels()
        {
            var a=new AudioSettingsData { master=-1,music=2,sfx=float.NaN,ui=float.PositiveInfinity,ambience=.5f }; a.Normalize();
            Assert.That(new[]{a.master,a.music,a.sfx,a.ui,a.ambience},Is.EqualTo(new[]{0f,1,1,1,.5f}));
            Assert.That(AudioSettingsData.Decibels(0),Is.EqualTo(-80)); Assert.That(AudioSettingsData.Decibels(1),Is.EqualTo(0)); Assert.That(AudioSettingsData.Decibels(.5f),Is.EqualTo(-6.0206f).Within(.001f));
        }
        [Test] public void MissingAudioClipsAreSafe()
        {
            var cue=ScriptableObject.CreateInstance<AudioCue>();
            try { Assert.That(cue.SelectClip(new System.Random(1)),Is.Null); cue.clips=new AudioClip[]{null,null}; Assert.That(cue.SelectClip(new System.Random(1)),Is.Null); }
            finally { Object.DestroyImmediate(cue); }
        }
    }
}
