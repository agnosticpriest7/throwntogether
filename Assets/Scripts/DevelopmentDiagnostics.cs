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
            if(seconds>=.5f) { fps=frames/seconds; seconds=0; frames=0; }
        }
        private void OnGUI()
        {
            if(text==null) text=new GUIStyle(GUI.skin.label) { fontSize=16, wordWrap=true };
            var matrix=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            // Clickable in browser/controller pointer mode without adding a gamepad binding.
            if(GUI.Button(new Rect(800,703,460,17),"DEV "+version+" | Pads: "+Gamepad.all.Count+" | F3 / click diagnostics")) Expanded=!Expanded;
            if(Expanded)
            {
                GUI.Box(new Rect(24,105,540,220),GUIContent.none);
                var pads=string.Join(", ",Gamepad.all.Select(p=>p.displayName+" #"+p.deviceId));
                var item=chef != null && chef.Hands.Item != null ? chef.Hands.Item.Payload : null;
                var active=input != null ? input.LastActiveDevice : null;
                string details="Build: "+stamp+"\nLast active input: "+(active==null ? "None yet" : active.displayName+(active.added ? "" : " (disconnected)"))+
                    "\nConnected gamepads: "+(pads.Length==0 ? "None detected (press a controller button in Web)" : pads)+
                    "\nTarget: "+(chef != null && chef.Focus != null ? chef.Focus.stationName : "None")+
                    "\nHeld: "+(item==null ? "Nothing" : item.Label)+" | State: "+(item==null ? "—" : item.EmptyPlate ? "Empty plate" : item.state.ToString())+
                    "\nFPS: "+fps.ToString("F0")+" | F3 or click version to close";
                GUI.Label(new Rect(34,112,520,210),details,text);
            }
            GUI.matrix=matrix;
        }
    }
}
#endif
