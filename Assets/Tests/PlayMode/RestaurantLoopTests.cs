using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ThrownTogether.Tests
{
    public class RestaurantLoopTests
    {
        private Scene original, testScene;
        private ChefController chef;
        private GameObject[] suspended;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            original=SceneManager.GetActiveScene();
            suspended=Object.FindObjectsByType<RestaurantHud>().Any(x=>x.gameObject.scene==original)
                ? original.GetRootGameObjects().Where(x=>x.activeSelf).ToArray() : new GameObject[0];
            foreach(var root in suspended) root.SetActive(false);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantDevelopment.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantDevelopment",LoadSceneMode.Additive);
#endif
            testScene=SceneManager.GetSceneAt(SceneManager.sceneCount-1); SceneManager.SetActiveScene(testScene);
            chef=Object.FindObjectsByType<ChefController>().First(x=>x.gameObject.scene==testScene);
            chef.GetComponent<ChefInput>().enabled=false;
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if(original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if(testScene.IsValid() && testScene.isLoaded) yield return SceneManager.UnloadSceneAsync(testScene);
            foreach(var root in suspended) if(root != null) root.SetActive(true);
        }
        private T Find<T>(string label) where T:Interactable => Interactable.Active.OfType<T>().First(x=>x.gameObject.scene==testScene && x.stationName.Contains(label));
        private void Approach(Interactable station)
        {
            var motor=chef.GetComponent<CharacterController>(); motor.enabled=false;
            chef.transform.position=station.transform.position+new Vector3(0,.04f,-1.5f); chef.transform.rotation=Quaternion.identity; motor.enabled=true;
            chef.FindFocus(); Assert.That(chef.Focus,Is.EqualTo(station));
        }
        private void Use(Interactable station) { Approach(station); Assert.That(chef.Use(),Is.True,station.stationName); }
        [UnityTest]
        public IEnumerator VirtualGamepadDrivesMovementAndSouthButtonPickup()
        {
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var gamepad=InputSystem.AddDevice<Gamepad>();
            var input=chef.GetComponent<ChefInput>(); input.BindDevices(gamepad); input.enabled=true;
            try
            {
                var before=chef.transform.position;
                var deadline=Time.realtimeSinceStartup+3;
                while(chef.transform.position.x<before.x+.2f && Time.realtimeSinceStartup<deadline)
                {
                    InputSystem.QueueStateEvent(gamepad,new GamepadState { leftStick=Vector2.right*.5f });
                    yield return null;
                }
                Assert.That(chef.transform.position.x,Is.GreaterThan(before.x+.1f));
                InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return null;
                Approach(Find<SourceStation>("POTATOES"));
                InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));
                yield return null; yield return null;
                Assert.That(chef.Hands.Item,Is.Not.Null);
                Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Raw));
            }
            finally
            {
                input.enabled=false; InputSystem.RemoveDevice(gamepad);
                InputSystem.settings.backgroundBehavior=background;
                InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
        }
        [UnityTest]
        public IEnumerator ChefMovesAndStopsWithoutSlidingAndCannotReachRemoteStations()
        {
            var before=chef.transform.position;
            for(int i=0;i<15;i++) { chef.Move(Vector2.right,1f/60); yield return null; }
            Assert.That(chef.transform.position.x,Is.GreaterThan(before.x+.7f));
            var stopped=chef.transform.position;
            for(int i=0;i<5;i++) { chef.Move(Vector2.zero,1f/60); yield return null; }
            Assert.That(chef.transform.position.x,Is.EqualTo(stopped.x).Within(.01f));
            var motor=chef.GetComponent<CharacterController>(); motor.enabled=false; chef.transform.position=new Vector3(2,0,-3); motor.enabled=true;
            chef.FindFocus(); Assert.That(chef.Focus,Is.Null);
        }
        [UnityTest]
        public IEnumerator FullLoopRejectsInvalidInputsThenDeliversPlatedFries()
        {
            var source=Find<SourceStation>("POTATOES"); var prep=Find<ProcessingStation>("PREP"); var fryer=Find<ProcessingStation>("FRYER");
            var counter=Find<CounterStation>("PLATING"); var plates=Find<SourceStation>("PLATES"); var service=Find<ServiceStation>("PICKUP");
            Use(source); var potato=chef.Hands.Item; Assert.That(potato.Payload.state,Is.EqualTo(FoodState.Raw));
            Approach(source); Assert.That(chef.Use(),Is.False); Assert.That(chef.Hands.Item,Is.SameAs(potato));
            Approach(fryer); Assert.That(chef.Use(),Is.False); Assert.That(fryer.slot.Item,Is.Null);
            Approach(service); Assert.That(chef.Use(),Is.False);
            Use(prep); Assert.That(chef.Hands.Item,Is.Null); Assert.That(prep.Busy,Is.True);
            Assert.That(chef.Use(),Is.False,"Cannot remove processing food");
            prep.Advance(.5f); Assert.That(potato.Payload.state,Is.EqualTo(FoodState.Raw));
            yield return new WaitUntil(()=>!prep.Busy);
            Assert.That(potato.Payload.state,Is.EqualTo(FoodState.Cut)); Use(prep);
            Use(fryer); Assert.That(fryer.Busy,Is.True);
            fryer.Advance(1); Assert.That(potato.Payload.state,Is.EqualTo(FoodState.Cut));
            yield return new WaitUntil(()=>!fryer.Busy);
            Assert.That(potato.Payload.state,Is.EqualTo(FoodState.Cooked)); Use(fryer);
            Approach(service); Assert.That(chef.Use(),Is.False,"Unplated fries must not serve");
            Use(counter); Use(plates); Use(counter);
            Assert.That(chef.Hands.Item.Payload.isPlate,Is.True); Assert.That(service.order.recipe.Matches(chef.Hands.Item.Payload),Is.True);
            Assert.That(counter.slot.Item,Is.Null);
            Use(service); Assert.That(chef.Hands.Item,Is.Null); Assert.That(service.order.Phase,Is.EqualTo(OrderPhase.Delivering));
            yield return new WaitUntil(()=>service.order.Phase==OrderPhase.Complete);
            Assert.That(service.order.tableSlot.Item,Is.Not.Null);
            Assert.That(service.order.recipe.Matches(service.order.tableSlot.Item.Payload),Is.True);
            Assert.That(service.order.CanAccept(service.order.tableSlot.Item.Payload),Is.False);
        }
    }
}
