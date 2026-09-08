#if UNITY_EDITOR || THROWNTOGETHER_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Profiling;

namespace ThrownTogether
{
    public sealed class DevelopmentDiagnostics : MonoBehaviour
    {
        [Serializable] private sealed class BuildStamp { public string commit; public string builtAtUtc; public string developmentVersion; }
        public bool Expanded { get; private set; }
        private ChefController chef;
        private ChefInput input;
        private string version="Editor / unbuilt";
        private string stamp="Editor session";
        private float seconds, fps;
        private int frames;
        private GUIStyle text;
        private string browserPads="Checking browser…";
        private SettingsService settings;
        private RestaurantAudioFeedback audioFeedback;
        private string saveStatus="Settings save only; no progression";
        private ProfilerRecorder gcRecorder;
        private long gcTotal;
        private string gcSample="Unavailable in this player";
        public string ControllerEvent { get; private set; } = "No connection changes this session";
        private void OnEnable()
        {
            InputSystem.onDeviceChange+=DeviceChanged;
            gcRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
        }
        private void OnDisable() { InputSystem.onDeviceChange-=DeviceChanged; gcRecorder.Dispose(); }
        private void DeviceChanged(InputDevice device,InputDeviceChange change)
        {
            if(device is Gamepad) ControllerEvent=device.displayName+": "+change;
        }
        private void Awake()
        {
            chef=GetComponent<RestaurantHud>().chef;
            input=chef != null ? chef.GetComponent<ChefInput>() : null;
            settings=GetComponent<RestaurantHud>().settings;
            audioFeedback=GetComponent<RestaurantHud>().audioFeedback;
            var asset=Resources.Load<TextAsset>("DevelopmentBuildStamp");
            if (asset != null)
            {
                var data=JsonUtility.FromJson<BuildStamp>(asset.text);
                stamp=data.developmentVersion+" / "+data.commit+" / "+data.builtAtUtc;
                version=data.developmentVersion+" "+data.commit.Substring(0,Math.Min(8,data.commit.Length));
            }
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) Expanded=!Expanded;
            seconds+=Time.unscaledDeltaTime; frames++;
            if(gcRecorder.Valid) gcTotal+=gcRecorder.LastValue;
            if(seconds>=.5f) { fps=frames/seconds; if(gcRecorder.Valid) gcSample=(gcTotal/frames)+" B/frame (whole player; Editor includes Editor work)"; gcTotal=0; seconds=0; frames=0; browserPads=WebInputFocus.BrowserGamepads; }
        }
        private void OnGUI()
        {
            if(text==null) text=new GUIStyle(GUI.skin.label) { fontSize=16, wordWrap=true };
            var matrix=GUI.matrix;
            // Depth belongs to this GUI behaviour, unlike the shared matrix/color state.
            GUI.depth=-100;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            // Clickable in browser/controller pointer mode without adding a gamepad binding.
            if(GUI.Button(new Rect(800,703,460,17),"DEV "+version+" | Pads: "+Gamepad.all.Count+" | F3 / click")) { Expanded=!Expanded; if(audioFeedback != null) audioFeedback.Click(); }
            if(Expanded)
            {
                var color=GUI.color;
                GUI.color=new Color(.035f,.055f,.07f,1);
                GUI.DrawTexture(new Rect(24,105,650,325),Texture2D.whiteTexture);
                GUI.color=color;
                GUI.Box(new Rect(24,105,650,325),GUIContent.none);
                var pads=string.Join(", ",Gamepad.all.Select(p=>p.displayName+" #"+p.deviceId+" (P1: "+(input != null && input.AcceptsDevice(p) ? "yes" : "no")+")"));
                var item=chef != null && chef.Hands.Item != null ? chef.Hands.Item.Payload : null;
                var active=input != null ? input.LastActiveDevice : null;
                string details="Build: "+stamp+"\nLast active input: "+(active==null ? "None yet" : active.displayName+(active.added ? "" : " (disconnected)"))+
                    "\nConnected gamepads: "+(pads.Length==0 ? "None detected (press a controller button in Web)" : pads)+
                    "\nAssignment: "+(input != null && input.HasExplicitDeviceAssignment ? "Explicit device set" : "Player 1 solo fallback; all mapped pads eligible")+
                    "\nBrowser API: "+browserPads+
                    "\nInput focus: "+(WebInputFocus.HasFocus ? "YES" : "NO — click game / press A with page active")+
                    "\nController event: "+ControllerEvent+
                    "\nTarget: "+(chef != null && chef.Focus != null ? chef.Focus.stationName : "None")+
                    "\nHeld: "+(item==null ? "Nothing" : item.Label)+" | State: "+(item==null ? "—" : item.EmptyPlate ? "Empty plate" : item.state.ToString())+
                    "\nFPS: "+fps.ToString("F0")+" | GC: "+gcSample;
                GUI.Label(new Rect(34,112,630,310),details,text);
                DrawAudioSettings();
            }
            GUI.matrix=matrix;
        }
        private void DrawAudioSettings()
        {
            if(settings == null || settings.Repository == null) return;
            var color=GUI.color; GUI.color=new Color(.035f,.055f,.07f,1);
            GUI.DrawTexture(new Rect(700,105,360,280),Texture2D.whiteTexture); GUI.color=color;
            GUI.Label(new Rect(714,112,330,24),"Development audio settings",text);
            var data=settings.Repository.Audio;
            bool changed=false;
            data.master=Volume("Master",data.master,148,ref changed);
            data.music=Volume("Music",data.music,181,ref changed);
            data.sfx=Volume("SFX",data.sfx,214,ref changed);
            data.ui=Volume("UI",data.ui,247,ref changed);
            data.ambience=Volume("Ambience",data.ambience,280,ref changed);
            if(changed) settings.Apply();
            bool enabled=GUI.enabled; GUI.enabled=settings.Repository.CanSave;
            if(GUI.Button(new Rect(714,316,150,25),"Save audio settings"))
            { saveStatus=settings.Save() ? "Saved locally" : "Save unavailable"; if(audioFeedback != null) audioFeedback.Click(); }
            GUI.enabled=enabled;
            GUI.Label(new Rect(714,349,335,32),settings.Repository.CanSave ? saveStatus : "Existing save protected: "+settings.Repository.Status,text);
        }
        private float Volume(string label,float value,int y,ref bool changed)
        {
            GUI.Label(new Rect(714,y,105,25),label,text);
            float next=GUI.HorizontalSlider(new Rect(825,y+7,215,20),value,0,1);
            changed |= !Mathf.Approximately(value,next); return next;
        }
    }
}
#endif
