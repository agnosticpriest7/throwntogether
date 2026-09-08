using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ThrownTogether.Tests
{
    public sealed class AudioFoundationTests
    {
        [UnityTest] public IEnumerator MissingFeedbackDoesNotCreateVoicesOrErrorsOnRequest()
        {
            var root=new GameObject("Audio test"); var cue=ScriptableObject.CreateInstance<AudioCue>();
            if(Object.FindAnyObjectByType<AudioListener>() == null) root.AddComponent<AudioListener>();
            try
            {
                var playback=root.AddComponent<AudioPlayback>();
                int count=root.GetComponents<AudioSource>().Length;
                Assert.That(playback.Play(null),Is.Null); Assert.That(playback.Play(cue),Is.Null);
                Assert.That(root.GetComponents<AudioSource>().Length,Is.EqualTo(count));
                var settings=root.AddComponent<SettingsService>(); Assert.That(settings.Apply(),Is.False);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.Destroy(root); Object.Destroy(cue); }
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator AuthoredMixerAcceptsAndRestoresEveryVolume()
        {
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VerticalSlice/RestaurantAudio.prefab");
            var root=Object.Instantiate(prefab);
            if(Object.FindAnyObjectByType<AudioListener>() == null) root.AddComponent<AudioListener>();
            var service=root.GetComponent<SettingsService>();
            string[] names={"MasterVolume","MusicVolume","SFXVolume","UIVolume","AmbienceVolume"};
            var previous=new float[5]; for(int i=0;i<5;i++) Assert.That(service.mixer.GetFloat(names[i],out previous[i]),Is.True);
            try
            {
                yield return null;
                var data=service.Repository.Audio; data.master=.5f; data.music=0; data.sfx=.25f; data.ui=1; data.ambience=.75f;
                Assert.That(service.Apply(),Is.True);
                float[] expected={.5f,0,.25f,1,.75f};
                for(int i=0;i<5;i++) { Assert.That(service.mixer.GetFloat(names[i],out float db),Is.True); Assert.That(db,Is.EqualTo(AudioSettingsData.Decibels(expected[i])).Within(.001f)); }
                Assert.That(root.GetComponent<RestaurantAudioFeedback>().sizzle.SelectClip(new System.Random(1)),Is.Not.Null);
            }
            finally { for(int i=0;i<5;i++) service.mixer.SetFloat(names[i],previous[i]); Object.Destroy(root); }
        }
#endif
    }
}
