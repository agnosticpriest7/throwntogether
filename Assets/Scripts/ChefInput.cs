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
        private void Awake()
        {
            chef=GetComponent<ChefController>(); controls=new InputActionMap("Chef");
            move=controls.AddAction("Move",InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            use=controls.AddAction("Use",InputActionType.Button); use.AddBinding("<Keyboard>/e"); use.AddBinding("<Keyboard>/space"); use.AddBinding("<Gamepad>/buttonSouth");
            restart=controls.AddAction("Restart",InputActionType.Button); restart.AddBinding("<Keyboard>/r"); restart.AddBinding("<Gamepad>/start");
        }
        // A future local join flow can restrict each instance to its assigned devices.
        public void BindDevices(params InputDevice[] devices) => controls.devices=devices;
        private void OnEnable() => controls.Enable();
        private void OnDisable() => controls.Disable();
        private void OnDestroy() => controls.Dispose();
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            chef.Move(move.ReadValue<Vector2>(),seconds);
            if (use.WasPressedThisFrame()) chef.Use();
            if (restart.WasPressedThisFrame())
            {
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(SceneManager.GetActiveScene().path,new LoadSceneParameters(LoadSceneMode.Single));
#else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
#endif
            }
        }
    }
}
