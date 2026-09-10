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
        [UnityTest]
        public IEnumerator BrowserMenuNeverTriggersRestartButKeyboardAndNativeRestartRemainAvailable()
        {
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad=InputSystem.AddDevice<Gamepad>();
            var keyboard=InputSystem.AddDevice<Keyboard>();
            using var web=new InputActionMap("WebTest");
            using var native=new InputActionMap("NativeTest");
            var webRestart=ChefInput.CreateRestartAction(web,true);
            var nativeRestart=ChefInput.CreateRestartAction(native,false);
            web.Enable(); native.Enable();
            try
            {
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Start));
                InputSystem.Update();
                Assert.That(webRestart.WasPressedThisFrame(),Is.False,"Menu belongs to the browser");
                Assert.That(nativeRestart.WasPressedThisFrame(),Is.True,"Native Start is unchanged");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));
                InputSystem.Update();
                Assert.That(webRestart.WasPressedThisFrame(),Is.True,"Web keyboard restart remains usable");
            }
            finally
            {
                InputSystem.RemoveDevice(pad); InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior=background;
                InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
            yield return null;
        }
        private Scene original, testScene;
        [UnityTest] public IEnumerator MenuPausesGameplayAndCancelPreservesFood()
        {
            var menu=Object.FindObjectsByType<RestaurantMenu>().Single(m=>m.gameObject.scene==testScene);
            var pad=InputSystem.AddDevice<Gamepad>(); var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var input=chef.GetComponent<ChefInput>();
            try
            {
                Use(Find<SourceStation>("POTATOES")); var item=chef.Hands.Item;
                input.BindDevices(pad); input.enabled=true;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.North)); InputSystem.Update();
                menu.Tick(false); Assert.That(menu.IsOpen,Is.False,"Unfocused menu ignores input"); menu.Tick(true);
                Assert.That(menu.IsOpen,Is.True); Assert.That(Time.timeScale,Is.Zero);
                var position=chef.transform.position;
                InputSystem.QueueStateEvent(pad,new GamepadState {leftStick=Vector2.right}.WithButton(GamepadButton.South)); InputSystem.Update();
                input.Tick(.5f); Assert.That(chef.transform.position,Is.EqualTo(position)); Assert.That(input.UseAttempts,Is.Zero);
                Assert.That(chef.Hands.Item,Is.SameAs(item));
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadDown)); InputSystem.Update(); menu.Tick(true);
                Assert.That(menu.Selection,Is.EqualTo(1),"D-pad navigates the menu");
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update(); menu.Tick(true);
                Assert.That(menu.Page,Is.EqualTo("Recipes"),"A opens the recipe book without using held food");
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East)); InputSystem.Update(); menu.Tick(true);
                Assert.That(menu.Page,Is.EqualTo("Main"),"B returns without changing scene");
                menu.RequestRestart(); Assert.That(menu.Page,Is.EqualTo("Confirm")); Assert.That(menu.Selection,Is.Zero);
                menu.ActivateSelection(); Assert.That(menu.Page,Is.EqualTo("Main")); Assert.That(chef.Hands.Item,Is.SameAs(item));
                menu.Close(); Assert.That(Time.timeScale,Is.EqualTo(1));
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update();
                yield return null; yield return null;
                input.Tick(.1f); Assert.That(input.UseAttempts,Is.Zero,"Held menu A cannot become gameplay Use");
                InputSystem.QueueStateEvent(pad,new GamepadState()); InputSystem.Update(); input.Tick(0);
                Assert.That(input.AwaitUseRelease,Is.False);
            }
            finally {menu.Close(); InputSystem.RemoveDevice(pad); InputSystem.settings.backgroundBehavior=background; InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;}
        }
        [UnityTest] public IEnumerator SharedPrepRejectsSecondInputAndPlayerCannotLeaveWithFood()
        {
            var session=Object.FindObjectsByType<LocalCoopSession>().Single(s=>s.gameObject.scene==testScene);
            var one=InputSystem.AddDevice<Gamepad>(); var two=InputSystem.AddDevice<Gamepad>();
            try
            {
                session.BindPlayerOne(one); Assert.That(session.Join(two),Is.True);
                var second=session.PlayerTwo; second.GetComponent<ChefInput>().enabled=false;
                Use(Find<SourceStation>("POTATOES")); second.Hands.TryTake(chef.Hands.Item);
                Assert.That(session.LeavePlayerTwo(),Is.False); var secondItem=second.Hands.Item;
                Use(Find<SourceStation>("POTATOES")); var prep=Find<ProcessingStation>("PREP");
                Assert.That(prep.Interact(chef),Is.True); Assert.That(prep.Interact(second),Is.False);
                Assert.That(second.Hands.Item,Is.SameAs(secondItem)); prep.Advance(1.5f);
                Assert.That(prep.Interact(second),Is.False,"A chef holding food cannot take another chef's prepared item");
                Assert.That(Find<CounterStation>("SPARE").Interact(second),Is.True);
                Assert.That(session.LeavePlayerTwo(),Is.True); Assert.That(session.PlayerTwo,Is.Null);
                Assert.That(session.Join(two),Is.True,"An empty-handed player can leave and join again");
            }
            finally {InputSystem.RemoveDevice(one); InputSystem.RemoveDevice(two);}
            yield return null;
        }
        [UnityTest]
        public IEnumerator TwoPlayersHaveIsolatedInputAndReconnectRetainsHeldItem()
        {
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var one=InputSystem.AddDevice<Gamepad>(); var two=InputSystem.AddDevice<Gamepad>(); Gamepad replacement=null;
            var session=Object.FindObjectsByType<LocalCoopSession>().Single(s=>s.gameObject.scene==testScene);
            try
            {
                Assert.That(session.PlayerTwo,Is.Null);
                session.BindPlayerOne(one); Assert.That(session.Join(two),Is.True);
                Assert.That(session.Join(one),Is.False); Assert.That(session.Join(two),Is.False);
                var second=session.PlayerTwo; var firstInput=chef.GetComponent<ChefInput>(); firstInput.enabled=true;
                var secondInput=second.GetComponent<ChefInput>();
                Assert.That(firstInput.AcceptsDevice(two),Is.False); Assert.That(secondInput.AcceptsDevice(one),Is.False);
                var before=chef.transform.position; var secondBefore=second.transform.position;
                InputSystem.QueueStateEvent(two,new GamepadState { leftStick=Vector2.right }); InputSystem.Update();
                firstInput.Tick(.1f); secondInput.Tick(.1f);
                Assert.That(chef.transform.position.x,Is.EqualTo(before.x).Within(.001f));
                Assert.That(second.transform.position.x,Is.GreaterThan(secondBefore.x));
                firstInput.enabled=false; secondInput.enabled=false;
                Use(Find<SourceStation>("POTATOES")); var item=chef.Hands.Item;
                Assert.That(second.Hands.TryTake(item),Is.True);
                InputSystem.RemoveDevice(two); yield return null;
                Assert.That(second.Hands.Item,Is.SameAs(item)); Assert.That(secondInput.enabled,Is.False);
                replacement=InputSystem.AddDevice<Gamepad>(); Assert.That(session.Join(replacement),Is.True);
                Assert.That(session.PlayerTwo,Is.SameAs(second)); Assert.That(second.Hands.Item,Is.SameAs(item));
                Use(Find<SourceStation>("POTATOES")); var counter=Find<CounterStation>("SPARE");
                Assert.That(counter.Interact(chef),Is.True); Assert.That(counter.Interact(second),Is.False);
                Assert.That(second.Hands.Item,Is.SameAs(item));
            }
            finally
            {
                InputSystem.RemoveDevice(one); if(two.added) InputSystem.RemoveDevice(two); if(replacement!=null) InputSystem.RemoveDevice(replacement);
                InputSystem.settings.backgroundBehavior=background; InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
        }
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
            KitchenTestAccess.Approach(chef,station);
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
                Assert.That(input.UseSignals,Is.EqualTo(1));
                Assert.That(input.LastUseResult,Does.Contain("Accepted at "+Find<SourceStation>("POTATOES").stationName));
                var result=input.LastUseResult;
                input.SetInputFocus(false); input.SetInputFocus(true);
                Assert.That(input.LastUseResult,Is.EqualTo(result),"Focus changes retain interaction evidence");
                input.BindDevices(); Assert.That(input.AcceptsDevice(gamepad),Is.False,"Empty assignment disables device input");
                input.BindDevices((InputDevice[])null);
                Assert.That(input.AcceptsDevice(gamepad),Is.True,"Null restores single-player fallback");
                Assert.That(input.AcceptsDevice(unassigned),Is.True);
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
            var motor=chef.GetComponent<CharacterController>(); motor.enabled=false; chef.transform.position=new Vector3(100,0,-100); motor.enabled=true;
            chef.FindFocus(); Assert.That(chef.Focus,Is.Null);
        }
        [UnityTest]
        public IEnumerator FocusBlocksInputAndHeldButtonsRequireReleaseAfterRegain()
        {
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad=InputSystem.AddDevice<Gamepad>();
            var input=chef.GetComponent<ChefInput>(); input.BindDevices(pad); input.enabled=true;
            try
            {
                Approach(Find<SourceStation>("POTATOES")); var before=chef.transform.position;
                input.SetInputFocus(false);
                InputSystem.QueueStateEvent(pad,new GamepadState {leftStick=Vector2.right}.WithButton(GamepadButton.South));
                InputSystem.Update(); input.Tick(1f/60);
                Assert.That(chef.transform.position,Is.EqualTo(before)); Assert.That(chef.Hands.Item,Is.Null);
                input.SetInputFocus(true);
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update(); input.Tick(1f/60);
                Assert.That(chef.Hands.Item,Is.Null,"Focus gesture must not also pick up an item");
                InputSystem.QueueStateEvent(pad,new GamepadState()); InputSystem.Update(); input.Tick(1f/60);
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update(); input.Tick(1f/60);
                Assert.That(chef.Hands.Item,Is.Not.Null);
                Assert.That(input.LastActiveDevice,Is.SameAs(pad));
            }
            finally
            {
                input.enabled=false; InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior=background; InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator HeldMenuAfterFocusRegainDoesNotBlockSouthButtonUse()
        {
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad=InputSystem.AddDevice<Gamepad>();
            var input=chef.GetComponent<ChefInput>(); input.BindDevices(pad); input.enabled=true;
            try
            {
                Approach(Find<SourceStation>("POTATOES"));
                input.SetInputFocus(false);
                InputSystem.QueueStateEvent(pad,new GamepadState()); InputSystem.Update();
                input.SetInputFocus(true);
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Start));
                InputSystem.Update(); input.Tick(1f/60);
                Assert.That(chef.Hands.Item,Is.Null,"Regaining focus must not act or restart");
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Start).WithButton(GamepadButton.South));
                InputSystem.Update(); input.Tick(1f/60);
                Assert.That(chef.Hands.Item,Is.Not.Null,"A released independently of Menu must remain usable");
                Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Raw));
            }
            finally
            {
                input.enabled=false; InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior=background; InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator SoloFallbackRecognizesNewControllerAfterRemoval()
        {
            var input=chef.GetComponent<ChefInput>(); input.BindDevices((InputDevice[])null);
            var first=InputSystem.AddDevice<Gamepad>();
            Assert.That(input.AcceptsDevice(first),Is.True);
            InputSystem.RemoveDevice(first);
            Assert.That(input.AcceptsDevice(first),Is.False);
            var replacement=InputSystem.AddDevice<Gamepad>();
            try
            {
                Assert.That(input.AcceptsDevice(replacement),Is.True);
                Assert.That(input.HasExplicitDeviceAssignment,Is.False);
#if UNITY_EDITOR || THROWNTOGETHER_DIAGNOSTICS
                var diagnostics=Object.FindObjectsByType<DevelopmentDiagnostics>().Single(x=>x.gameObject.scene==testScene);
                Assert.That(diagnostics.ControllerEvent,Does.Contain("Added"));
#endif
            }
            finally { InputSystem.RemoveDevice(replacement); }
            yield return null;
        }
        [UnityTest]
        public IEnumerator FullLoopRejectsInvalidInputsThenDeliversPlatedFries()
        {
            var source=Find<SourceStation>("POTATOES"); var prep=Find<ProcessingStation>("PREP"); var fryer=Find<ProcessingStation>("FRYER");
            var counter=Find<CounterStation>("COUNTER"); var plates=Find<SourceStation>("PLATES"); var service=Find<ServiceStation>("PICKUP");
            Use(source); var potato=chef.Hands.Item; Assert.That(potato.Payload.state,Is.EqualTo(FoodState.Raw));
            Assert.That(potato.GetComponentsInChildren<Collider>(true),Is.Empty,"Item visuals must not require stripped collider types");
            Assert.That(potato.GetComponentInChildren<MeshFilter>().sharedMesh,Is.SameAs(source.itemPrefab.sphereMesh));
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
            Assert.That(chef.Hands.Item,Is.Null,"Assembly leaves the finished plate on the counter");
            Use(counter);
            Assert.That(chef.Hands.Item.Payload.isPlate,Is.True); Assert.That(service.order.recipe.Matches(chef.Hands.Item.Payload),Is.True);
            Assert.That(counter.slot.Item,Is.Null);
            Use(service); Assert.That(chef.Hands.Item,Is.Null); Assert.That(service.order.Phase,Is.EqualTo(OrderPhase.Delivering));
            yield return new WaitUntil(()=>service.order.Phase==OrderPhase.Complete);
            Assert.That(service.order.tableSlot.Item,Is.Null);
            Assert.That(service.order.dishReturn.Count,Is.EqualTo(1),"Finished meals return one dirty plate");
            var count=Object.FindObjectsByType<SessionSummary>().Single(s=>s.gameObject.scene==testScene).PlayerOne;
            Assert.That(count.prep,Is.EqualTo(1)); Assert.That(count.fry,Is.EqualTo(1));
            Assert.That(count.plates,Is.EqualTo(1)); Assert.That(count.served,Is.EqualTo(1));
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
