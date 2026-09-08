using UnityEngine;
using UnityEngine.Audio;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Audio Cue")]
    public sealed class AudioCue : ScriptableObject
    {
        public AudioClip[] clips = new AudioClip[0];
        public AudioMixerGroup group;
        [Range(0,1)] public float volume = .5f;
        [Range(0,.2f)] public float volumeVariation = .04f;
        [Range(0,.2f)] public float pitchVariation = .03f;
        public bool loop;

        public AudioClip SelectClip(System.Random random)
        {
            if (clips == null || clips.Length == 0) return null;
            int start=random.Next(clips.Length);
            for(int i=0;i<clips.Length;i++)
                if(clips[(start+i)%clips.Length] != null) return clips[(start+i)%clips.Length];
            return null;
        }
    }
}
