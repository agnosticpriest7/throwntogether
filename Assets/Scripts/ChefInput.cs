using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ThrownTogether
{
    [RequireComponent(typeof(ChefController))]
    public sealed class ChefInput : MonoBehaviour
    {
        private InputActionMap controls;
        private InputAction move, use, restart, cancelStorage, storageNavigate;
        public SourceStation Storage {get;private set;}
        public int StorageIndex {get;private set;}
        private bool storageNavigationHeld;
        private readonly System.Collections.Generic.Dictionary<SourceStation,int> storageChoices=new System.Collections.Generic.Dictionary<SourceStation,int>();
        public void OpenStorage(SourceStation source)
        {
            if(source==null || chef.Hands.Item!=null)return;
            Storage=source;StorageIndex=storageChoices.TryGetValue(source,out int index)?Mathf.Clamp(index,0,source.Ingredients.Length-1):0;
            storageNavigationHeld=true;RequireActionRelease();chef.CancelWork();
        }
        public bool ChooseIngredient(int index)
        {
            if(Storage==null || index<0 || index>=Storage.Ingredients.Length)return false;
            if(!Storage.Dispense(chef,Storage.Ingredients[index]))return false;
            storageChoices[Storage]=index;Storage=null;RequireActionRelease();return true;
        }
        public void CloseStorage(){Storage=null;RequireActionRelease();}
        private void TickStorage(Vector2 axis)
        {
            var delta=chef.transform.position-Storage.transform.position;delta.y=0;
            if(!Storage.isActiveAndEnabled || delta.magnitude>chef.reach || cancelStorage.WasPressedThisFrame()){CloseStorage();return;}
            if(axis.sqrMagnitude<.25f)storageNavigationHeld=false;
            else if(!storageNavigationHeld)
            {
                int step=Mathf.Abs(axis.x)>Mathf.Abs(axis.y)?(axis.x>0?1:-1):(axis.y>0?-2:2);
                int count=Storage.Ingredients.Length;if(count<=2)step=step>0?1:-1;
                StorageIndex=(StorageIndex+step+count)%count;storageNavigationHeld=true;
            }
            if(AwaitUseRelease){if(!use.IsPressed())AwaitUseRelease=false;}
            else if(use.WasPressedThisFrame())ChooseIngredient(StorageIndex);
        }
        private void OnGUI()
        {
            if(Storage==null || RestaurantMenu.GameplayBlocked)return;
            var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            bool second=FindFirstObjectByType<LocalCoopSession>()?.PlayerTwo==chef;float x=second?666:18;
            GUI.color=new Color(.035f,.055f,.07f,.96f);GUI.DrawTexture(new Rect(x,480,596,214),Texture2D.whiteTexture);GUI.color=Color.white;
            var style=new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleCenter};
            GUI.Label(new Rect(x,484,596,32),(second?"P2 — ":"P1 — ")+Storage.storage.displayName,style);
            var button=new GUIStyle(GUI.skin.button){fontSize=21};
            for(int i=0;i<Storage.Ingredients.Length;i++)
            {
                var food=Storage.Ingredients[i];GUI.backgroundColor=i==StorageIndex?new Color(.2f,.8f,.6f):Color.gray;
                string label=food.displayName+(food.Unlocked?"":food.requiredPurchase=="grill"?" — Requires Grill":" — Requires Griddle");
                if(GUI.Button(new Rect(x+12+(i%2)*287,526+(i/2)*72,280,62),(i==StorageIndex?"> ":"")+label,button)){StorageIndex=i;ChooseIngredient(i);break;}
            }
            style.fontSize=18;GUI.Label(new Rect(x,671,596,22),"Stick / D-pad: choose • A / E: take • B / Q: cancel",style);
            GUI.backgroundColor=Color.white;GUI.color=color;GUI.matrix=matrix;
        }
        private ChefController chef;
        public InputDevice LastActiveDevice { get; private set; }
        public bool InputFocused { get; private set; } = true;
        public bool HasExplicitDeviceAssignment => controls.devices.HasValue;
        public bool AwaitUseRelease { get; private set; }
        public bool AwaitRestartRelease { get; private set; }
        public bool UsePressed => use.IsPressed();
        public int UseAttempts { get; private set; }
        public string LastUseResult { get; private set; } = "None yet";
        public int UseSignals { get; private set; }
        public string LastUseSignal { get; private set; } = "None yet";
        public static bool BrowserOwnsMenu => Application.platform==RuntimePlatform.WebGLPlayer;
        public static string RestartHint => "Y / Esc: menu • R: restart confirmation";
        public static InputAction CreateRestartAction(InputActionMap map, bool browserOwnsMenu)
        {
            var action=map.AddAction("Restart",InputActionType.Button);
            action.AddBinding("<Keyboard>/r");
            // Edge needs Menu to enter/leave its native controller mode.
            if(!browserOwnsMenu) action.AddBinding("<Gamepad>/start");
            return action;
        }
        public bool AcceptsDevice(InputDevice device) => device != null && device.added && (!controls.devices.HasValue || controls.devices.Value.Contains(device));
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput=false;
#endif
            chef=GetComponent<ChefController>(); controls=new InputActionMap("Chef");
            move=controls.AddAction("Move",InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            use=controls.AddAction("Use",InputActionType.Button); use.AddBinding("<Keyboard>/e"); use.AddBinding("<Keyboard>/space"); use.AddBinding("<Gamepad>/buttonSouth");
            storageNavigate=controls.AddAction("Ingredient navigation",InputActionType.Value);storageNavigate.AddBinding("<Gamepad>/dpad");
            cancelStorage=controls.AddAction("Close ingredient selector",InputActionType.Button);cancelStorage.AddBinding("<Gamepad>/buttonEast");cancelStorage.AddBinding("<Keyboard>/q");
            restart=CreateRestartAction(controls,BrowserOwnsMenu);
            controls.actionTriggered += context => {
                if (!context.performed) return;
                LastActiveDevice=context.control.device;
                if(context.action==use) { UseSignals++; LastUseSignal=context.control.path+" focus="+InputFocused+" gate="+AwaitUseRelease; }
            };
        }
        // A future local join flow can restrict each instance to its assigned devices.
        public void BindDevices(params InputDevice[] devices)
        {
            // Explicit nullable assignment avoids converting null into an empty ReadOnlyArray.
            if(devices==null) controls.devices=null;
            else controls.devices=devices;
            LastActiveDevice=null;
        }
        private void OnEnable() { if(InputFocused) controls.Enable(); }
        private void OnDisable() { controls.Disable(); chef.CancelWork(); CloseStorage(); }
#if UNITY_WEBGL && !UNITY_EDITOR
        private void OnApplicationFocus(bool focused) { if(!focused) SetInputFocus(false); }
        private void OnApplicationPause(bool paused) { if(paused) SetInputFocus(false); }
#endif
        private void OnDestroy() => controls.Dispose();
        private void Update() => Tick(Time.deltaTime);
        public void RequireActionRelease() { AwaitUseRelease=true; AwaitRestartRelease=true; }
        public void SetInputFocus(bool focused)
        {
            if(InputFocused==focused) return;
            InputFocused=focused;
            if(!focused) { controls.Disable(); AwaitUseRelease=true; AwaitRestartRelease=true; }
            else if(isActiveAndEnabled) controls.Enable();
        }
        public void Tick(float seconds)
        {
            if(RestaurantMenu.GameplayBlocked) { CloseStorage(); return; }
#if UNITY_WEBGL && !UNITY_EDITOR
            SetInputFocus(WebInputFocus.HasFocus);
#endif
            if(!InputFocused){CloseStorage();return;}
            if(Storage!=null){TickStorage((move.ReadValue<Vector2>()+storageNavigate.ReadValue<Vector2>()).normalized);return;}
            chef.Move(move.ReadValue<Vector2>(),seconds);
            // Each action must release independently: a browser-held Menu signal
            // must not prevent the player from using an unrelated button.
            if(AwaitUseRelease)
            {
                if(!use.IsPressed()) AwaitUseRelease=false;
            }
            else if(use.WasPressedThisFrame())
            {
                UseAttempts++;
                LastUseResult="Entered interaction";
                try
                {
                    bool accepted=chef.Use();
                    LastUseResult=(accepted ? "Accepted" : "Rejected")+" at "+(chef.Focus != null ? chef.Focus.stationName : "None")+
                        " | "+chef.Feedback+" | held="+(chef.Hands.Item != null ? chef.Hands.Item.Payload.Label : "Nothing");
                }
                catch(System.Exception exception)
                {
                    LastUseResult="Exception: "+exception.GetType().Name+" | "+exception.Message;
                    throw;
                }
            }
            if(AwaitRestartRelease)
            {
                if(!restart.IsPressed()) AwaitRestartRelease=false;
            }
            else if (restart.WasPressedThisFrame())
            { if(RestaurantMenu.Instance!=null) RestaurantMenu.Instance.RequestRestart(); else RestartSlice(); }
        }
        public void RestartSlice()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(gameObject.scene.path,new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(gameObject.scene.name);
#endif
        }
    }
}
