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
        private bool awaitButtonRelease;
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
            restart=controls.AddAction("Restart",InputActionType.Button); restart.AddBinding("<Keyboard>/r"); restart.AddBinding("<Gamepad>/start");
            controls.actionTriggered += context => { if (context.performed) LastActiveDevice=context.control.device; };
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
            if(!focused) { controls.Disable(); awaitButtonRelease=true; }
            else if(isActiveAndEnabled) controls.Enable();
        }
        public void Tick(float seconds)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetInputFocus(WebInputFocus.HasFocus);
#endif
            if(!InputFocused) return;
            chef.Move(move.ReadValue<Vector2>(),seconds);
            if(awaitButtonRelease)
            {
                if(!use.IsPressed() && !restart.IsPressed()) awaitButtonRelease=false;
                return;
            }
            if (use.WasPressedThisFrame()) chef.Use();
            if (restart.WasPressedThisFrame())
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
