using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ThrownTogether
{
    // Run before chef input so opening/closing a menu never leaks its A press.
    [DefaultExecutionOrder(-1000)]
    public sealed class RestaurantMenu : MonoBehaviour
    {
        public static RestaurantMenu Instance { get; private set; }
        public static bool GameplayBlocked => Instance!=null && (Instance.IsOpen || Time.frameCount<=Instance.blockThroughFrame);
        public static DisplaySettingsData Display => Instance?.hud?.settings?.Repository?.Display ?? defaults;
        private static readonly DisplaySettingsData defaults=new DisplaySettingsData();
        private static bool booted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance=null; booted=false; }
        public bool IsOpen { get; private set; }
        public int Selection { get; private set; }
        public string Page { get; private set; }="Main";
        private RestaurantHud hud;
        private InputActionMap controls;
        private InputAction toggle, navigate, accept, back;
        private float savedTimeScale=1, nextNavigation;
        private int blockThroughFrame=-1;
        private Action pending;
        private string confirmation, message="";
        private bool navigationHeld;
        private GUIStyle buttonStyle, textStyle;
        private readonly List<Row> rows=new List<Row>();
        private sealed class Row { public string label; public Action select; public Action<int> adjust; }
        private void Awake()
        {
            hud=GetComponent<RestaurantHud>(); Instance=this;
            controls=new InputActionMap("Restaurant menu");
            toggle=controls.AddAction("Menu",InputActionType.Button); toggle.AddBinding("<Gamepad>/buttonNorth"); toggle.AddBinding("<Keyboard>/escape");
            navigate=controls.AddAction("Navigate",InputActionType.Value); navigate.AddBinding("<Gamepad>/dpad"); navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            accept=controls.AddAction("Accept",InputActionType.Button); accept.AddBinding("<Gamepad>/buttonSouth"); accept.AddBinding("<Keyboard>/enter");
            back=controls.AddAction("Back",InputActionType.Button); back.AddBinding("<Gamepad>/buttonEast"); back.AddBinding("<Keyboard>/backspace");
        }
        private void Start() { if(!booted && !Application.isEditor) Open(); booted=true; }
        private void OnEnable() { Instance=this; controls.Enable(); }
        private void OnDisable() { Close(); controls.Disable(); }
        private void OnDestroy() { if(Instance==this) Instance=null; controls.Dispose(); }
        public void Open()
        {
            if(!IsOpen) { savedTimeScale=Time.timeScale; Time.timeScale=0; IsOpen=true; }
            SetPage("Main");
        }
        public void Close()
        {
            if(!IsOpen) return;
            if(IsOpen) Time.timeScale=savedTimeScale;
            IsOpen=false; blockThroughFrame=Time.frameCount+1;
            hud.coop?.RefreshPlayerOneAssignment();
            foreach(var input in FindObjectsByType<ChefInput>(FindObjectsSortMode.None)) input.RequireActionRelease();
        }
        private void SetPage(string page) { Page=page; Selection=0; message=""; BuildRows(); }
        private void Add(string label,Action select,Action<int> adjust=null) => rows.Add(new Row {label=label,select=select,adjust=adjust});
        private void BuildRows()
        {
            rows.Clear();
            if(Page=="Confirm") { Add("Cancel — keep playing",()=>SetPage("Main")); Add("Confirm",()=>{var action=pending; Close(); action?.Invoke();}); return; }
            if(Page=="Display")
            {
                var d=Display;
                Add("Text size: "+new[]{"Standard","Large","Extra large"}[d.textSize],()=>d.textSize=(d.textSize+1)%3,dir=>d.textSize=Mathf.Clamp(d.textSize+dir,0,2));
                Add("High contrast: "+(d.highContrast ? "ON":"OFF"),()=>d.highContrast=!d.highContrast);
                Add("Reduced effects: "+(d.reducedEffects ? "ON":"OFF"),()=>d.reducedEffects=!d.reducedEffects);
                Add("Save settings",Save); Add("Back",()=>SetPage("Main")); return;
            }
            if(Page=="Audio")
            {
                var a=hud.settings.Repository.Audio;
                Volume("Master",()=>a.master,v=>a.master=v); Volume("Music",()=>a.music,v=>a.music=v);
                Volume("SFX",()=>a.sfx,v=>a.sfx=v); Volume("UI",()=>a.ui,v=>a.ui=v); Volume("Ambience",()=>a.ambience,v=>a.ambience=v);
                Add("Save settings",Save); Add("Back",()=>SetPage("Main")); return;
            }
            Add("Play / Resume",Close);
            Add(hud.shift==null ? "Play restaurant shift" : "Return to practice",()=>Confirm("Change mode? Current food/order progress will reset.",()=>Load(hud.shift==null ? "RestaurantShift":"RestaurantDevelopment")));
            Add("Restart this mode",RequestRestart);
            Add("Text and accessibility",()=>SetPage("Display")); Add("Audio settings",()=>SetPage("Audio"));
            if(hud.coop!=null)
            {
                if(hud.coop.PlayerTwo==null) Add(hud.coop.KeyboardPlayerOne ? "Keyboard P1 selected — A joins P2" : "Use keyboard P1 + one controller P2",()=>{hud.coop.UseKeyboardPlayerOne(); message="Resume, then press A on the controller to join P2.";});
                else Add("Player 2 leave",()=>{ if(hud.coop.PlayerTwo.Hands.Item!=null) message="P2 must place their held item on a counter before leaving."; else Confirm("Remove Player 2? The current shift continues.",()=>hud.coop.LeavePlayerTwo()); });
            }
        }
        private void Volume(string name,Func<float> read,Action<float> write)
        {
            Action<int> change=dir=>{write(Mathf.Round(Mathf.Clamp01(read()+dir*.1f)*10)/10); hud.settings.Apply();};
            Add(name+": "+Mathf.RoundToInt(read()*100)+"%   ← / →",()=>change(1),change);
        }
        private void Save() { message=hud.settings.Save() ? "Settings saved." : "Settings could not be saved; existing saved data was preserved."; }
        public void RequestRestart() => Confirm("Restart this mode? Current food/order progress will reset.",()=>hud.chef.GetComponent<ChefInput>().RestartSlice());
        private void Confirm(string text,Action action) { if(!IsOpen) Open(); pending=action; confirmation=text; SetPage("Confirm"); }
        public void ActivateSelection() { rows[Mathf.Clamp(Selection,0,rows.Count-1)].select(); hud.audioFeedback?.Click(); if(IsOpen) BuildRows(); }
        private void Update() => Tick(WebInputFocus.HasFocus);
        public void Tick(bool focused)
        {
            if(!focused) return;
            if(toggle.WasPressedThisFrame()) { if(IsOpen) Close(); else Open(); return; }
            if(!IsOpen) return;
            if(back.WasPressedThisFrame()) { if(Page=="Main") Close(); else SetPage("Main"); return; }
            var axis=navigate.ReadValue<Vector2>();
            if(axis.sqrMagnitude<.25f) navigationHeld=false;
            else if(!navigationHeld || Time.unscaledTime>=nextNavigation)
            {
                if(Mathf.Abs(axis.y)>=Mathf.Abs(axis.x)) Selection=(Selection+(axis.y>0 ? -1:1)+rows.Count)%rows.Count;
                else { rows[Selection].adjust?.Invoke(axis.x>0 ? 1:-1); BuildRows(); }
                nextNavigation=Time.unscaledTime+(navigationHeld ? .16f:.35f); navigationHeld=true;
            }
            if(accept.WasPressedThisFrame()) ActivateSelection();
        }
        private static void Load(string name)
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(name);
#endif
        }
        private void OnGUI()
        {
            if(!IsOpen) return;
            GUI.depth=-100;
            var matrix=GUI.matrix; GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.color=Color.black; GUI.DrawTexture(new Rect(0,0,1280,720),Texture2D.whiteTexture); GUI.color=Color.white;
            if(buttonStyle==null) { buttonStyle=new GUIStyle(GUI.skin.button) {alignment=TextAnchor.MiddleLeft}; textStyle=new GUIStyle(GUI.skin.label) {fontSize=22,wordWrap=true,alignment=TextAnchor.MiddleCenter}; }
            var style=buttonStyle; style.fontSize=Mathf.RoundToInt(21*Display.TextScale); var text=textStyle;
            text.normal.textColor=Color.white; style.normal.textColor=Color.white; style.hover.textColor=Color.white; style.active.textColor=Color.white;
            GUI.Label(new Rect(260,25,760,48),Page=="Main" ? "THROWN TOGETHER — MENU" : Page.ToUpperInvariant(),text);
            GUI.Label(new Rect(260,73,760,60),Page=="Confirm" ? confirmation : "Paused • D-pad / stick: navigate • A: select • B: back\nY / Escape: close • Xbox Menu belongs to Edge",text);
            for(int i=0;i<rows.Count;i++)
            {
                GUI.backgroundColor=i==Selection ? new Color(.2f,.8f,.6f):Color.gray;
                if(GUI.Button(new Rect(260,145+i*52,760,46),(i==Selection ? ">  ":"    ")+rows[i].label,style)) { Selection=i; ActivateSelection(); break; }
            }
            GUI.backgroundColor=Color.white;
            GUI.Label(new Rect(240,545,800,70),message,text);
            if(hud.coop!=null) GUI.Label(new Rect(180,620,920,75),hud.coop.DeviceSummary,text);
            GUI.matrix=matrix; GUI.depth=0;
        }
    }
}
