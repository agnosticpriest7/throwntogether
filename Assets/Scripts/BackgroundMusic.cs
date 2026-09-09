using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace ThrownTogether
{
    // Separate persistent voice: scene restarts and pooled SFX never interrupt music.
    public sealed class BackgroundMusic : MonoBehaviour
    {
        private static BackgroundMusic instance;
        private static float volume=.6f;
        private AudioSource voice;
        private readonly string[] tracks={"first-service.ogg","one-more-order.ogg"};
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void TT_MusicStart(string first,string second);
        [DllImport("__Internal")] private static extern void TT_MusicVolume(float value);
#endif
        public static void Ensure()
        {
            if(instance!=null) return;
            var root=new GameObject("Background music");
            instance=root.AddComponent<BackgroundMusic>();
            DontDestroyOnLoad(root);
        }
        public static void SetVolume(float value)
        {
            volume=AudioSettingsData.Safe(value);
#if UNITY_WEBGL && !UNITY_EDITOR
            TT_MusicVolume(volume);
#else
            if(instance!=null && instance.voice!=null) instance.voice.volume=volume;
#endif
        }
        private IEnumerator Start()
        {
            var folder=Application.streamingAssetsPath+"/Music/";
#if UNITY_WEBGL && !UNITY_EDITOR
            TT_MusicStart(folder+tracks[0],folder+tracks[1]);
            TT_MusicVolume(volume);
            yield break;
#else
            voice=gameObject.AddComponent<AudioSource>();
            voice.playOnAwake=false; voice.spatialBlend=0; voice.volume=volume;
            int index=0;
            while(true)
            {
                string uri=new System.Uri(folder+tracks[index]).AbsoluteUri;
                using(var request=UnityWebRequestMultimedia.GetAudioClip(uri,AudioType.OGGVORBIS))
                {
                    ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio=true;
                    yield return request.SendWebRequest();
                    if(request.result==UnityWebRequest.Result.Success)
                    {
                        var clip=DownloadHandlerAudioClip.GetContent(request);
                        voice.clip=clip; voice.Play();
                        while(voice.isPlaying) yield return null;
                        voice.clip=null; Destroy(clip);
                    }
                    else { Debug.LogWarning("Background music unavailable: "+tracks[index]); yield return new WaitForSecondsRealtime(5); }
                }
                index=(index+1)%tracks.Length;
            }
#endif
        }
        private void OnDestroy() { if(instance==this) instance=null; }
    }
}
