using UnityEngine;

namespace ThrownTogether
{
    // Fixed voice budget; audio never allocates gameplay objects or changes Unity's random state.
    public sealed class AudioPlayback : MonoBehaviour
    {
        private readonly System.Random random=new System.Random();
        private AudioSource[] voices;
        readonly System.Collections.Generic.HashSet<AudioSource> loops=new System.Collections.Generic.HashSet<AudioSource>();
        public int StartedCount {get;private set;}
        public void StopLoop(AudioSource voice){if(voice==null)return;voice.Stop();loops.Remove(voice);}
        private void Awake()
        {
            voices=new AudioSource[10];
            for(int i=0;i<voices.Length;i++)
            {
                voices[i]=gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake=false; voices[i].spatialBlend=0;
            }
        }
        public AudioSource Play(AudioCue cue)
        {
            if(!isActiveAndEnabled || cue == null || voices == null) return null;
            var clip=cue.SelectClip(random);
            if(clip == null) return null;
            foreach(var voice in voices)
            {
                if(voice.isPlaying || loops.Contains(voice)) continue;
                voice.clip=clip; voice.loop=cue.loop; voice.outputAudioMixerGroup=cue.group;
                voice.volume=Mathf.Clamp01(cue.volume+Variation(cue.volumeVariation));
                voice.pitch=Mathf.Clamp(1+Variation(cue.pitchVariation),.8f,1.2f);
                voice.Play();if(cue.loop)loops.Add(voice);StartedCount++; return voice;
            }
            return null; // Saturation drops feedback, never an in-progress loop.
        }
        private float Variation(float amount) => ((float)random.NextDouble()*2-1)*amount;
        private void OnDisable() { loops.Clear(); if(voices != null) foreach(var voice in voices) if(voice != null) voice.Stop(); }
    }
}
