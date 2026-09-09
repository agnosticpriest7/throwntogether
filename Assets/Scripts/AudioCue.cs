using UnityEngine;
using UnityEngine.Audio;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Audio Cue")]
    public sealed class AudioCue : ScriptableObject
    {
        public AudioClip[] clips = new AudioClip[0];
        public TextAsset[] pcmClips = new TextAsset[0];
        private AudioClip[] decodedClips;
        public AudioMixerGroup group;
        [Range(0,1)] public float volume = .5f;
        [Range(0,.2f)] public float volumeVariation = .04f;
        [Range(0,.2f)] public float pitchVariation = .03f;
        public bool loop;

        public AudioClip SelectClip(System.Random random)
        {
            if(pcmClips != null && pcmClips.Length>0)
            {
                if(decodedClips==null) decodedClips=new AudioClip[pcmClips.Length];
                int index=random.Next(pcmClips.Length);
                if(decodedClips[index]==null && pcmClips[index]!=null)
                    decodedClips[index]=PcmWave.CreateClip(pcmClips[index].bytes,pcmClips[index].name);
                return decodedClips[index];
            }
            if (clips == null || clips.Length == 0) return null;
            int start=random.Next(clips.Length);
            for(int i=0;i<clips.Length;i++)
                if(clips[(start+i)%clips.Length] != null) return clips[(start+i)%clips.Length];
            return null;
        }
        private void OnDisable()
        {
            if(decodedClips!=null) foreach(var clip in decodedClips) if(clip!=null) Destroy(clip);
            decodedClips=null;
        }
    }
}
