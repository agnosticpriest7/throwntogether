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
            var unassigned=InputSystem.AddDevice<Gamepad>();
            var input=chef.GetComponent<ChefInput>(); input.BindDevices(gamepad); input.enabled=true;
            try
            {
                var before=chef.transform.position;
                Assert.That(Object.FindObjectsByType<ChefController>().Count(x=>x.gameObject.scene==testScene),Is.EqualTo(1));
                Assert.That(input.AcceptsDevice(gamepad),Is.True); Assert.That(input.AcceptsDevice(unassigned),Is.False);
                InputSystem.QueueStateEvent(unassigned,new GamepadState { leftStick=Vector2.right }.WithButton(GamepadButton.South));
                InputSystem.Update(); input.Tick(1f/60);
                Assert.That(chef.transform.position.x,Is.EqualTo(before.x).Within(.001f));
                Assert.That(chef.Hands.Item,Is.Null);
                // Drive input and gameplay together, independent of Editor frame scheduling.
                for(int frame=0;frame<15;frame++)
                {
                    InputSystem.QueueStateEvent(gamepad,new GamepadState { leftStick=Vector2.right*.5f });
                    InputSystem.Update(); input.Tick(1f/60);
                }
                Assert.That(chef.transform.position.x,Is.GreaterThan(before.x+.1f));
                InputSystem.QueueStateEvent(gamepad,new GamepadState()); InputSystem.Update();
                Approach(Find<SourceStation>("POTATOES"));
                InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));
                InputSystem.Update(); input.Tick(1f/60);
                Assert.That(chef.Hands.Item,Is.Not.Null);
                Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Raw));
                Assert.That(input.LastActiveDevice,Is.SameAs(gamepad));
            }
            finally
            {
                input.enabled=false; InputSystem.RemoveDevice(gamepad);
                InputSystem.RemoveDevice(unassigned);
                InputSystem.settings.backgroundBehavior=background;
                InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
            yield return null;
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
        [UnityTest]
        public IEnumerator InvalidSequenceNeverReservesOrCompletesOrder()
        {
            var service=Find<ServiceStation>("PICKUP");
            Use(Find<SourceStation>("POTATOES"));
            Approach(Find<ProcessingStation>("FRYER")); Assert.That(chef.Use(),Is.False);
            Approach(service); Assert.That(chef.Use(),Is.False);
            Use(Find<CounterStation>("SPARE")); Use(Find<SourceStation>("PLATES"));
            Approach(service); Assert.That(chef.Use(),Is.False,"Empty plate is not an order");
            yield return null;
            Assert.That(service.order.Phase,Is.EqualTo(OrderPhase.Waiting));
            Assert.That(service.order.tableSlot.Item,Is.Null);
        }
        [UnityTest]
        public IEnumerator RestartRestoresInitialSinglePlayerSlice()
        {
            var spawn=chef.transform.position;
            Use(Find<SourceStation>("POTATOES")); Use(Find<ProcessingStation>("PREP"));
            Assert.That(Find<ProcessingStation>("PREP").Busy,Is.True);
            chef.GetComponent<ChefInput>().RestartSlice();
            yield return null;
            testScene=SceneManager.GetActiveScene();
            // Single-mode restart replaces the test host as well as the restaurant.
            if(!original.IsValid() || !original.isLoaded) original=SceneManager.CreateScene("Restart test host");
            chef=Object.FindObjectsByType<ChefController>().Single(x=>x.gameObject.scene==testScene);
            chef.GetComponent<ChefInput>().enabled=false;
            Assert.That(Vector3.Distance(chef.transform.position,spawn),Is.LessThan(.1f));
            Assert.That(chef.Hands.Item,Is.Null);
            Assert.That(Find<ServiceStation>("PICKUP").order.Phase,Is.EqualTo(OrderPhase.Waiting));
            Assert.That(Find<ServiceStation>("PICKUP").order.tableSlot.Item,Is.Null);
            foreach(var station in Interactable.Active.OfType<CounterStation>().Where(x=>x.gameObject.scene==testScene)) Assert.That(station.slot.Item,Is.Null);
            foreach(var station in Interactable.Active.OfType<ProcessingStation>().Where(x=>x.gameObject.scene==testScene)) Assert.That(station.Busy,Is.False);
#if UNITY_EDITOR || THROWNTOGETHER_DIAGNOSTICS
            Assert.That(Object.FindObjectsByType<DevelopmentDiagnostics>().Single(x=>x.gameObject.scene==testScene).Expanded,Is.False);
#endif
            Use(Find<SourceStation>("POTATOES")); Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Raw));
        }
    }
}
