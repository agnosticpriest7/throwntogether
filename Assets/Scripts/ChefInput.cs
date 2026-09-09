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
        private InputAction move, use, restart;
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
        public static string RestartHint => BrowserOwnsMenu ? "R: restart (DEV button on controller)" : "R / Start: restart";
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
        private void OnDisable() => controls.Disable();
#if UNITY_WEBGL && !UNITY_EDITOR
        private void OnApplicationFocus(bool focused) { if(!focused) SetInputFocus(false); }
        private void OnApplicationPause(bool paused) { if(paused) SetInputFocus(false); }
#endif
        private void OnDestroy() => controls.Dispose();
        private void Update() => Tick(Time.deltaTime);
        public void SetInputFocus(bool focused)
        {
            if(InputFocused==focused) return;
            InputFocused=focused;
            if(!focused) { controls.Disable(); AwaitUseRelease=true; AwaitRestartRelease=true; }
            else if(isActiveAndEnabled) controls.Enable();
        }
        public void Tick(float seconds)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetInputFocus(WebInputFocus.HasFocus);
#endif
            if(!InputFocused) return;
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
                RestartSlice();
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
