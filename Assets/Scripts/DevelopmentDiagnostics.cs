#if UNITY_EDITOR || THROWNTOGETHER_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThrownTogether
{
    public sealed class DevelopmentDiagnostics : MonoBehaviour
    {
        [Serializable] private sealed class BuildStamp { public string commit; public string builtAtUtc; }
        public bool Expanded { get; private set; }
        private ChefController chef;
        private ChefInput input;
        private string version="Editor / unbuilt";
        private string stamp="Editor session";
        private float seconds, fps;
        private int frames;
        private GUIStyle text;
        private string browserPads="Checking browser…";
        public string ControllerEvent { get; private set; } = "No connection changes this session";
        private void OnEnable() => InputSystem.onDeviceChange+=DeviceChanged;
        private void OnDisable() => InputSystem.onDeviceChange-=DeviceChanged;
        private void DeviceChanged(InputDevice device,InputDeviceChange change)
        {
            if(device is Gamepad) ControllerEvent=device.displayName+": "+change;
        }
        private void Awake()
        {
            chef=GetComponent<RestaurantHud>().chef;
            input=chef != null ? chef.GetComponent<ChefInput>() : null;
            var asset=Resources.Load<TextAsset>("DevelopmentBuildStamp");
            if (asset != null)
            {
                var data=JsonUtility.FromJson<BuildStamp>(asset.text);
                stamp=data.commit+" / "+data.builtAtUtc;
                version=data.commit.Substring(0,Math.Min(8,data.commit.Length));
            }
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) Expanded=!Expanded;
            seconds+=Time.unscaledDeltaTime; frames++;
            if(seconds>=.5f) { fps=frames/seconds; seconds=0; frames=0; browserPads=WebInputFocus.BrowserGamepads; }
        }
        private void OnGUI()
        {
            if(text==null) text=new GUIStyle(GUI.skin.label) { fontSize=16, wordWrap=true };
            var matrix=GUI.matrix;
            // Depth belongs to this GUI behaviour, unlike the shared matrix/color state.
            GUI.depth=-100;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            // Clickable in browser/controller pointer mode without adding a gamepad binding.
            if(GUI.Button(new Rect(800,703,460,17),"DEV "+version+" | Pads: "+Gamepad.all.Count+" | F3 / click diagnostics")) Expanded=!Expanded;
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
                    "\nFPS: "+fps.ToString("F0")+" | F3 or click version to close";
                GUI.Label(new Rect(34,112,630,310),details,text);
            }
            GUI.matrix=matrix;
        }
    }
}
#endif
