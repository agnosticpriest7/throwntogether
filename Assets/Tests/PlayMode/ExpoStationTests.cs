using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace ThrownTogether.Tests
{
    public sealed class ExpoStationTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public string Read()=>json;public void Write(string value)=>json=value;}
        sealed class DeviceScope:System.IDisposable
        {
            readonly InputSettings.BackgroundBehavior background;readonly InputSettings.EditorInputBehaviorInPlayMode editor;
            public DeviceScope()
            {
                background=InputSystem.settings.backgroundBehavior;editor=InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            }
            public void Dispose()
            {InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
        }
        Scene original,scene;GameObject[] suspended;RestaurantHud hud;RestaurantDay day;ChefController chef;Interactable[] stations;
        ExpoStation station;int capacity;
        // The counter sits clear of the authored kitchen run so station focus is unambiguous.
        static readonly Vector3 Counter=new Vector3(12.7f,0,-5.1f);
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;RestaurantAccounts.UseStorage(new Memory());
            RestaurantAccounts.Current.SetMenu(DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0).Select(r=>r.id).ToArray());
            SessionOptions.ShiftOrders=0;SessionOptions.Kitchen=0;
            original=SceneManager.GetActiveScene();
            suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];
            foreach(var root in suspended)root.SetActive(false);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;
            hud=Object.FindObjectsByType<RestaurantHud>().Single(h=>h.gameObject.scene==scene);
            day=Object.FindObjectsByType<RestaurantDay>().Single(d=>d.gameObject.scene==scene);
            chef=Object.FindObjectsByType<ChefController>().Single(c=>c.gameObject.scene==scene);
            chef.GetComponent<ChefInput>().enabled=false;
            stations=Interactable.Active.Where(s=>s.gameObject.scene==scene).ToArray();
            capacity=day.Settings.activeQueueCapacity;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            day.Settings.activeQueueCapacity=capacity;
            if(RestaurantMenu.Instance!=null && RestaurantMenu.Instance.IsOpen)RestaurantMenu.Instance.Close();
            Time.timeScale=1;SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);
            foreach(var root in suspended)if(root!=null)root.SetActive(true);
            RestaurantAccounts.ResetCache();SessionOptions.ShiftOrders=6;SessionOptions.Kitchen=0;
        }
        ExpoStation Install(Vector3 at,bool initialize=true)
        {
            var go=new GameObject("Expo counter");SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=at;
            var installed=go.AddComponent<ExpoStation>();installed.stationName="Expo counter";
            if(initialize)installed.Initialize(day);
            return installed;
        }
        ChefInput Ready(ChefController actor,Interactable target)
        {
            var motor=actor.GetComponent<CharacterController>();bool on=motor.enabled;motor.enabled=false;
            actor.transform.SetPositionAndRotation(target.transform.position+new Vector3(0,0,-1.1f),Quaternion.LookRotation(Vector3.forward));
            motor.enabled=on;Physics.SyncTransforms();actor.FindFocus();
            var input=actor.GetComponent<ChefInput>();input.enabled=true;return input;
        }
        // Real arrivals and seating, so every ticket comes from the shipped lifecycle.
        void SeatTwo()
        {
            for(int i=0;i<900 && day.Tables.Count(t=>t.WaitingForMeal)<2;i++)day.Advance(.1f);
            Assert.That(day.Tables.Count(t=>t.WaitingForMeal),Is.GreaterThanOrEqualTo(2),"Two seated diners are needed for this scenario");
        }
        Carryable Plate(RecipeDefinition recipe)
        {
            var plate=stations.OfType<SourceStation>().Single(s=>s.plates).TakeCleanPlate();
            var payload=new ItemPayload{isPlate=true,ingredient=recipe.ingredient,state=recipe.requiredState};
            foreach(var part in recipe.additionalIngredients)payload.additions.Add(new IngredientPortion{ingredient=part.ingredient,state=part.state});
            plate.Configure(payload);return plate;
        }
        SourceStation Produce()=>stations.OfType<SourceStation>().First(s=>!s.plates && s.storage!=null && s.storage.displayName=="Produce Rack");
        // Extra seated diners for list-length scenarios. These are real DiningTable seatings
        // that register through the shipped path, parked far away so they never take focus.
        // Production table limits and progression are untouched.
        DiningTable[] ExtraSeats(int count)
        {
            var recipe=day.Menu[0];var made=new DiningTable[count];
            for(int i=0;i<count;i++)
            {
                var root=new GameObject("Test seat "+(i+1));SceneManager.MoveGameObjectToScene(root,scene);
                root.transform.position=new Vector3(-60-i*3,0,-60);
                var slot=new GameObject("Plate slot");slot.transform.SetParent(root.transform,false);
                var order=root.AddComponent<CustomerOrder>();order.tableSlot=slot.AddComponent<CarrySlot>();
                var table=root.AddComponent<DiningTable>();table.day=day;table.order=order;table.stationName="Test seat "+(i+1);
                table.ReserveSeat();table.Seat(recipe);made[i]=table;
                Assert.That(day.Expo.TicketFor(table),Is.Not.Null,"The seating path should register a ticket");
            }
            return made;
        }
        static void Send(Gamepad pad,ChefInput target,GamepadState state)
        {InputSystem.QueueStateEvent(pad,state);InputSystem.Update();target.Tick(0);}
        static void Press(Gamepad pad,ChefInput target,GamepadButton button)
        {Send(pad,target,new GamepadState());Send(pad,target,new GamepadState().WithButton(button));}
        // Rows reorder as orders are fired, so walk to the top and then down.
        static void SelectRow(Gamepad pad,ChefInput input,KitchenTicket target)
        {
            for(int i=0;i<12 && input.SelectedExpoTicket!=target;i++)Press(pad,input,GamepadButton.DpadUp);
            for(int i=0;i<12 && input.SelectedExpoTicket!=target;i++)Press(pad,input,GamepadButton.DpadDown);
            Assert.That(input.SelectedExpoTicket,Is.SameAs(target),"Navigation should reach the intended order");
        }

        [UnityTest] public IEnumerator InstalledStationIsFocusableOpensWithHeldFoodAndIgnoresInvalidSetup()
        {
            var stray=new GameObject("Stray expo");SceneManager.MoveGameObjectToScene(stray,original);
            var outsider=stray.AddComponent<ExpoStation>();
            try
            {
                outsider.Initialize(day);
                Assert.That(outsider.Day,Is.Null,"A station must not adopt a day from another scene");
                Assert.That(outsider.Expo,Is.Null);Assert.That(outsider.Interact(chef),Is.False);
            }
            finally{Object.DestroyImmediate(stray);}
            var blank=Install(Counter+Vector3.left*6,false);
            Assert.That(blank.Day,Is.Null);Assert.That(blank.Interact(chef),Is.False,"An uninitialized station stays unavailable");
            blank.Initialize(null);Assert.That(blank.Day,Is.Null);
            Object.DestroyImmediate(blank.gameObject);
            station=Install(Counter);SeatTwo();
            Assert.That(station.Day,Is.SameAs(day));Assert.That(station.Expo,Is.SameAs(day.Expo));
            Assert.That(station.Prompt(chef),Does.Contain("0 / "+station.Expo.Capacity));
            var input=Ready(chef,station);
            Assert.That(chef.Focus,Is.SameAs(station),"The installed counter must be focusable like any station");
            var held=Plate(day.Tables.First(t=>t.WaitingForMeal).order.recipe);
            Assert.That(chef.Hands.TryTake(held),Is.True);
            Assert.That(chef.Use(),Is.True);
            Assert.That(input.Expo,Is.SameAs(station));
            Assert.That(chef.Hands.Item,Is.SameAs(held),"Opening Expo neither needs nor drops held food");
            Assert.That(input.SelectedExpoTicket,Is.Not.Null);
            Assert.That(input.SelectedExpoTicket.State,Is.EqualTo(KitchenTicketState.Waiting));
            input.CloseExpo();Assert.That(input.Expo,Is.Null);Assert.That(chef.Hands.Item,Is.SameAs(held));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator OpeningPressCannotFireAndAReleasedRepressFiresExactlyOnce()
        {
            station=Install(Counter);SeatTwo();var expo=station.Expo;
            var input=Ready(chef,station);
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Press(pad,input,GamepadButton.South);
                    Assert.That(input.Expo,Is.SameAs(station),"A opens the browser");
                    var ticket=input.SelectedExpoTicket;Assert.That(ticket,Is.Not.Null);
                    Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Waiting),"The opening press must not also fire");
                    Send(pad,input,new GamepadState().WithButton(GamepadButton.South));
                    Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Waiting),"A held button must not fire");
                    Press(pad,input,GamepadButton.South);
                    Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Active));
                    Assert.That(expo.ActiveCount,Is.EqualTo(1));
                    Assert.That(input.Expo,Is.SameAs(station),"Firing keeps the browser open");
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(ticket),"Selection stays on the order just fired");
                    Press(pad,input,GamepadButton.South);
                    Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Active),"A fired order is inspectable, never refired or cancelled");
                    Assert.That(expo.ActiveCount,Is.EqualTo(1));
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NavigationReachesEveryRowAndAFullQueueRefusesWithoutMutation()
        {
            station=Install(Counter);var expo=station.Expo;var seats=ExtraSeats(3);
            Assert.That(expo.Capacity,Is.EqualTo(2));var input=Ready(chef,station);
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Assert.That(input.OpenExpo(station),Is.True);var first=input.SelectedExpoTicket;
                    Press(pad,input,GamepadButton.DpadDown);var second=input.SelectedExpoTicket;
                    Assert.That(second,Is.Not.SameAs(first));
                    Send(pad,input,new GamepadState());Send(pad,input,new GamepadState{leftStick=Vector2.up});
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(first));
                    Send(pad,input,new GamepadState{leftStick=Vector2.right});Assert.That(input.SelectedExpoTicket,Is.SameAs(first));
                    Assert.That(input.FireSelectedExpoTicket(),Is.True);Assert.That(expo.TryFire(second),Is.True);
                    var third=expo.TicketFor(seats[2]);SelectRow(pad,input,third);
                    Assert.That(input.FireSelectedExpoTicket(),Is.False);Assert.That(third.State,Is.EqualTo(KitchenTicketState.Waiting));
                    Assert.That(expo.ActiveCount,Is.EqualTo(2));Assert.That(input.Expo,Is.SameAs(station));
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(third));Assert.That(station.Prompt(chef),Does.Contain("full"));
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TwoChefsBrowseIndependentlyAndOneTicketFiresOnce()
        {
            station=Install(Counter);SeatTwo();var expo=station.Expo;
            using(new DeviceScope())
            {
                var one=InputSystem.AddDevice<Gamepad>();var two=InputSystem.AddDevice<Gamepad>();
                try
                {
                    hud.coop.BindPlayerOne(one);Assert.That(hud.coop.Join(two),Is.True);
                    var second=hud.coop.PlayerTwo;Assert.That(second,Is.Not.Null);
                    var inputOne=Ready(chef,station);var inputTwo=Ready(second,station);
                    inputOne.BindDevices(one);inputTwo.BindDevices(two);
                    Assert.That(chef.Use(),Is.True);
                    Assert.That(inputOne.Expo,Is.SameAs(station));
                    Assert.That(inputTwo.Expo,Is.Null,"Using the station opens only the acting chef's browser");
                    Assert.That(inputTwo.OpenExpo(station),Is.True);
                    var shared=inputOne.SelectedExpoTicket;
                    Assert.That(inputTwo.SelectedExpoTicket,Is.SameAs(shared));
                    Press(two,inputTwo,GamepadButton.DpadDown);
                    var theirs=inputTwo.SelectedExpoTicket;
                    Assert.That(theirs,Is.Not.SameAs(shared),"P2 moved their own cursor");
                    Assert.That(inputOne.SelectedExpoTicket,Is.SameAs(shared),"P1's cursor is untouched");
                    Assert.That(inputTwo.FireSelectedExpoTicket(),Is.True,"Either chef may fire");
                    Assert.That(theirs.State,Is.EqualTo(KitchenTicketState.Active));
                    SelectRow(two,inputTwo,shared);
                    int before=expo.ActiveCount;
                    Assert.That(inputOne.FireSelectedExpoTicket(),Is.True);
                    Assert.That(inputTwo.FireSelectedExpoTicket(),Is.False,"A second confirm cannot fire the same order twice");
                    Assert.That(shared.State,Is.EqualTo(KitchenTicketState.Active));
                    Assert.That(expo.ActiveCount,Is.EqualTo(before+1),"One ticket produced exactly one Fire transition");
                }
                finally
                {
                    if(hud.coop.PlayerTwo!=null){var carried=hud.coop.PlayerTwo.Hands.Release();if(carried!=null)Object.Destroy(carried.gameObject);hud.coop.LeavePlayerTwo();}
                    InputSystem.RemoveDevice(one);InputSystem.RemoveDevice(two);
                }
            }
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SelectionSurvivesReorderingAndCannotRedirectAHeldConfirm()
        {
            station=Install(Counter);SeatTwo();var expo=station.Expo;
            var input=Ready(chef,station);
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Assert.That(input.OpenExpo(station),Is.True);
                    var first=input.SelectedExpoTicket;
                    Press(pad,input,GamepadButton.DpadDown);
                    var chosen=input.SelectedExpoTicket;Assert.That(chosen,Is.Not.SameAs(first));
                    Press(pad,input,GamepadButton.South);
                    Assert.That(chosen.State,Is.EqualTo(KitchenTicketState.Active));
                    Assert.That(expo.ActiveCount,Is.EqualTo(1));
                    // The fired order moves into the active rows; identity must hold the cursor.
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(chosen),"Stable identity survives a reordering list");
                    var seat=expo.TableFor(chosen);Assert.That(seat,Is.Not.Null);
                    // Still holding A, the selected diner leaves and the row disappears.
                    seat.BeginDeparture();
                    Assert.That(chosen.State,Is.EqualTo(KitchenTicketState.Cancelled));
                    Send(pad,input,new GamepadState().WithButton(GamepadButton.South));
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(first),"A surviving row is selected");
                    Assert.That(first.State,Is.EqualTo(KitchenTicketState.Waiting),"The held press must not fire a different diner");
                    Assert.That(input.AwaitUseRelease,Is.True,"A fresh press is required after the selection vanished");
                    Assert.That(expo.ActiveCount,Is.Zero);
                    Assert.That(input.Expo,Is.SameAs(station));
                    Press(pad,input,GamepadButton.South);
                    Assert.That(first.State,Is.EqualTo(KitchenTicketState.Active),"A deliberate new press still fires normally");
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EmptyAndFiredOnlyListsStayNavigableAndEveryCloseRouteIsClean()
        {
            station=Install(Counter);var expo=station.Expo;
            var input=Ready(chef,station);
            Assert.That(expo.Tickets,Is.Empty);
            Assert.That(input.OpenExpo(station),Is.True,"An empty board still opens");
            Assert.That(input.SelectedExpoTicket,Is.Null);
            Assert.That(input.FireSelectedExpoTicket(),Is.False);
            Assert.That(input.Expo,Is.SameAs(station));
            input.CloseExpo();Assert.That(input.Expo,Is.Null);
            SeatTwo();
            var held=Plate(day.Tables.First(t=>t.WaitingForMeal).order.recipe);
            Assert.That(chef.Hands.TryTake(held),Is.True);
            foreach(var ticket in expo.WaitingTickets.ToArray())Assert.That(expo.TryFire(ticket),Is.True);
            Assert.That(input.OpenExpo(station),Is.True,"An all-fired board stays navigable");
            Assert.That(input.SelectedExpoTicket,Is.Not.Null);
            Assert.That(input.FireSelectedExpoTicket(),Is.False,"Already fired orders are inspectable only");
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Press(pad,input,GamepadButton.East);
                    Assert.That(input.Expo,Is.Null,"B closes the browser");
                    Assert.That(chef.Hands.Item,Is.SameAs(held));
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            float scale=Time.timeScale;
            Assert.That(input.OpenExpo(station),Is.True);
            RestaurantMenu.Instance.Open();input.Tick(0);
            Assert.That(input.Expo,Is.Null,"The pause menu closes the browser");
            RestaurantMenu.Instance.Close();
            Assert.That(Time.timeScale,Is.EqualTo(scale).Within(.0001f));
            // RestaurantMenu keeps gameplay blocked through the frame after closing.
            yield return null;yield return null;
            Assert.That(input.OpenExpo(station),Is.True);
            input.SetInputFocus(false);input.Tick(0);
            Assert.That(input.Expo,Is.Null,"Losing input focus closes the browser");
            input.SetInputFocus(true);
            Assert.That(input.OpenExpo(station),Is.True);
            var motor=chef.GetComponent<CharacterController>();motor.enabled=false;
            chef.transform.position=station.transform.position+new Vector3(0,0,-12);
            motor.enabled=true;Physics.SyncTransforms();input.Tick(0);
            Assert.That(input.Expo,Is.Null,"Walking out of reach closes the browser");
            Assert.That(chef.Hands.Item,Is.SameAs(held),"No close route drops held food");
            Ready(chef,station);
            Assert.That(input.OpenExpo(station),Is.True);
            input.enabled=false;
            Assert.That(input.Expo,Is.Null,"Disabling input closes the browser");
            input.enabled=true;
            Assert.That(input.OpenExpo(station),Is.True);
            Object.DestroyImmediate(station.gameObject);input.Tick(0);
            Assert.That(input.Expo,Is.Null,"A destroyed station closes the browser");
            Assert.That(input.AwaitUseRelease,Is.True,"Closing leaves no phantom action pending");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BrowserKeepsTimeAndPatienceRunningAndStaysExclusiveWithStorage()
        {
            station=Install(Counter);SeatTwo();var expo=station.Expo;
            var input=Ready(chef,station);
            Assert.That(input.OpenExpo(station),Is.True);
            var ticket=input.SelectedExpoTicket;
            float before=expo.PatienceRemaining(ticket);
            day.Advance(10);
            Assert.That(expo.PatienceRemaining(ticket),Is.LessThan(before),"Browsing does not pause customer patience");
            Assert.That(Time.timeScale,Is.EqualTo(1).Within(.0001f),"The browser never pauses global time");
            var produce=Produce();
            Ready(chef,produce);
            input.OpenStorage(produce);
            Assert.That(input.Expo,Is.Null,"Opening storage closes Expo");
            Assert.That(input.Storage,Is.SameAs(produce));
            Ready(chef,station);
            Assert.That(input.OpenExpo(station),Is.True);
            Assert.That(input.Storage,Is.Null,"Opening Expo closes storage");
            input.CloseExpo();
            Ready(chef,produce);
            input.OpenStorage(produce);
            Assert.That(input.ChooseIngredient(0),Is.True,"Ordinary ingredient selection still works");
            Assert.That(chef.Hands.Item,Is.Not.Null);
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DirectConfirmationRefusesStaleSelectionAndUnavailableInput()
        {
            station=Install(Counter);var expo=station.Expo;
            var seats=ExtraSeats(3);
            var input=Ready(chef,station);
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Assert.That(input.OpenExpo(station),Is.True);
                    var stale=input.SelectedExpoTicket;Assert.That(stale,Is.Not.Null);
                    var survivor=expo.TicketFor(seats[1]);var last=expo.TicketFor(seats[2]);
                    // No Tick runs between the departure and the confirmation.
                    seats[0].BeginDeparture();
                    Assert.That(stale.State,Is.EqualTo(KitchenTicketState.Cancelled));
                    Assert.That(input.FireSelectedExpoTicket(),Is.False,"A vanished selection must not retarget the command");
                    Assert.That(survivor.State,Is.EqualTo(KitchenTicketState.Waiting));
                    Assert.That(last.State,Is.EqualTo(KitchenTicketState.Waiting));
                    Assert.That(expo.ActiveCount,Is.Zero);
                    Assert.That(input.FireSelectedExpoTicket(),Is.True,"A deliberate second confirmation still works");
                    Assert.That(survivor.State,Is.EqualTo(KitchenTicketState.Active));
                    SelectRow(pad,input,last);
                    RestaurantMenu.Instance.Open();
                    Assert.That(input.FireSelectedExpoTicket(),Is.False,"Paused gameplay refuses direct confirmation");
                    RestaurantMenu.Instance.Close();
                    yield return null;yield return null;
                    input.SetInputFocus(false);
                    Assert.That(input.FireSelectedExpoTicket(),Is.False,"Unfocused input refuses direct confirmation");
                    input.SetInputFocus(true);
                    var motor=chef.GetComponent<CharacterController>();motor.enabled=false;
                    chef.transform.position=station.transform.position+new Vector3(0,0,-12);
                    motor.enabled=true;Physics.SyncTransforms();
                    Assert.That(input.FireSelectedExpoTicket(),Is.False,"An out-of-reach chef refuses direct confirmation");
                    Ready(chef,station);
                    station.enabled=false;
                    Assert.That(input.FireSelectedExpoTicket(),Is.False,"A closed station refuses direct confirmation");
                    station.enabled=true;
                    input.enabled=false;
                    Assert.That(input.FireSelectedExpoTicket(),Is.False,"Disabled input refuses direct confirmation");
                    input.enabled=true;
                    Assert.That(last.State,Is.EqualTo(KitchenTicketState.Waiting),"No refusal changed any ticket");
                    Assert.That(expo.ActiveCount,Is.EqualTo(1));
                    Assert.That(input.OpenExpo(station),Is.True);
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(last));
                    Assert.That(input.FireSelectedExpoTicket(),Is.True,"The guards were the only obstacle");
                    Assert.That(last.State,Is.EqualTo(KitchenTicketState.Active));
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SelectionLossSurvivesAnEmptyBoardBeforeTheNextDiner()
        {
            station=Install(Counter);var expo=station.Expo;
            var only=ExtraSeats(1);
            var input=Ready(chef,station);
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Assert.That(input.OpenExpo(station),Is.True);
                    var chosen=input.SelectedExpoTicket;Assert.That(chosen,Is.Not.Null);
                    only[0].BeginDeparture();
                    // Empty frames and ordinary GUI property reads must not erase the loss.
                    Send(pad,input,new GamepadState());
                    Assert.That(input.SelectedExpoTicket,Is.Null);
                    Assert.That(input.ExpoRowCount,Is.Zero);
                    Send(pad,input,new GamepadState());
                    Assert.That(input.FireSelectedExpoTicket(),Is.False,"An empty board has nothing to fire");
                    var arrival=ExtraSeats(1);
                    var fresh=expo.TicketFor(arrival[0]);Assert.That(fresh,Is.Not.Null);
                    Send(pad,input,new GamepadState().WithButton(GamepadButton.South));
                    Assert.That(fresh.State,Is.EqualTo(KitchenTicketState.Waiting),"Confirm on the recovery frame must not fire the new diner");
                    Assert.That(expo.ActiveCount,Is.Zero);
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(fresh));
                    Press(pad,input,GamepadButton.South);
                    Assert.That(fresh.State,Is.EqualTo(KitchenTicketState.Active),"A released and deliberate press still fires");
                    Assert.That(expo.ActiveCount,Is.EqualTo(1));
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ScrollingReachesEveryRowAndKeepsTheSelectionVisible()
        {
            station=Install(Counter);var expo=station.Expo;
            var seats=ExtraSeats(7);
            int window=ChefInput.ExpoVisibleRows;
            var input=Ready(chef,station);
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Assert.That(input.OpenExpo(station),Is.True);
                    Assert.That(input.ExpoRowCount,Is.EqualTo(7),"Every seated diner has a row");
                    Assert.That(input.ExpoSelectedRow,Is.Zero);Assert.That(input.ExpoVisibleTop,Is.Zero);
                    void Visible(string because)
                    {
                        Assert.That(input.ExpoSelectedRow,Is.GreaterThanOrEqualTo(input.ExpoVisibleTop),because);
                        Assert.That(input.ExpoSelectedRow,Is.LessThan(input.ExpoVisibleTop+window),because);
                        Assert.That(input.ExpoVisibleTop,Is.InRange(0,Mathf.Max(0,input.ExpoRowCount-window)));
                    }
                    for(int i=0;i<6;i++)Press(pad,input,GamepadButton.DpadDown);
                    Assert.That(input.ExpoSelectedRow,Is.EqualTo(6),"Navigation reaches the last row");
                    Assert.That(input.ExpoVisibleTop,Is.EqualTo(7-window),"The window scrolled to keep the last row visible");
                    Visible("after scrolling to the end");
                    for(int i=0;i<6;i++)Press(pad,input,GamepadButton.DpadUp);
                    Assert.That(input.ExpoSelectedRow,Is.Zero,"Navigation reaches the first row");
                    Assert.That(input.ExpoVisibleTop,Is.Zero);Visible("after scrolling back to the start");
                    for(int i=0;i<3;i++)Press(pad,input,GamepadButton.DpadDown);
                    var moved=input.SelectedExpoTicket;Assert.That(input.ExpoSelectedRow,Is.EqualTo(3));
                    Press(pad,input,GamepadButton.South);
                    Assert.That(moved.State,Is.EqualTo(KitchenTicketState.Active));
                    // Firing sends the row to the fired section at the end of the list.
                    Assert.That(input.SelectedExpoTicket,Is.SameAs(moved),"Selection follows the ticket through reordering");
                    Assert.That(input.ExpoSelectedRow,Is.EqualTo(6));
                    Visible("after the list reordered");
                    var doomed=expo.TableFor(moved);Assert.That(doomed,Is.Not.Null);
                    doomed.BeginDeparture();
                    Send(pad,input,new GamepadState());
                    Assert.That(input.ExpoRowCount,Is.EqualTo(6),"The departed diner left the list");
                    Assert.That(input.ExpoSelectedRow,Is.EqualTo(5),"Deletion at the end clamps the cursor");
                    Assert.That(input.ExpoVisibleTop,Is.EqualTo(6-window),"Scrolling clamps with the shorter list");
                    Visible("after deleting near the end");
                    for(int i=0;i<8;i++)Press(pad,input,GamepadButton.DpadUp);
                    Assert.That(input.ExpoSelectedRow,Is.Zero);Assert.That(input.ExpoVisibleTop,Is.Zero);
                    Visible("after returning to the top of the shorter list");
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            Assert.That(seats.Length,Is.EqualTo(7));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ProductionExpoStripTracksHoldMakeReadyAndImmediateRemoval()
        {
            station=day.GetComponent<KitchenFurniture>().Find("expo").GetComponent<ExpoStation>();
            Assert.That(station,Is.Not.Null,"Career startup installs the physical station");
            Assert.That(day.Expo,Is.SameAs(station.Expo));SeatTwo();var expo=day.Expo;
            var ticket=expo.WaitingTickets[0];Assert.That(ExpoOrderStrip.Label(ticket),Is.EqualTo("HOLD"));
            Assert.That(ExpoOrderStrip.Tint(ticket).r,Is.GreaterThan(ExpoOrderStrip.Tint(ticket).g));
            var pass=stations.OfType<ServiceStation>().Single();var dish=Plate(ticket.Recipe);
            Assert.That(pass.pickupSlot.TryTake(dish),Is.True);
            Assert.That(expo.TryStage(dish),Is.False,"Advance preparation must not bypass Fire");
            Assert.That(expo.TryFire(ticket),Is.True);
            Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Ready),"A matching dish already on the pass becomes READY on Fire");
            Assert.That(ExpoOrderStrip.Label(ticket),Is.EqualTo("READY"));Assert.That(ExpoOrderStrip.Tint(ticket).g,Is.GreaterThan(ExpoOrderStrip.Tint(ticket).r));
            Assert.That(ExpoOrderStrip.Visible(expo),Does.Contain(ticket));
            Assert.That(expo.TableFor(ticket).Deliver(dish),Is.True);
            Assert.That(ExpoOrderStrip.Visible(expo),Does.Not.Contain(ticket),"Served tile vanishes immediately");
            var next=expo.WaitingTickets[0];Assert.That(expo.TryFire(next),Is.True);Assert.That(ExpoOrderStrip.Label(next),Is.EqualTo("MAKE"));
            expo.TableFor(next).BeginDeparture();Assert.That(ExpoOrderStrip.Visible(expo),Does.Not.Contain(next));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FiringStaysPossibleAfterTenPmUntilTheDayActuallyCloses()
        {
            station=Install(Counter);var expo=station.Expo;
            day.Advance(301);
            Assert.That(day.AdmissionsClosed,Is.True);Assert.That(day.Closed,Is.False);
            var table=day.Tables.First(t=>t.WaitingForMeal);
            var ticket=expo.TicketFor(table);Assert.That(ticket,Is.Not.Null);
            var input=Ready(chef,station);
            using(new DeviceScope())
            {
                var pad=InputSystem.AddDevice<Gamepad>();input.BindDevices(pad);
                try
                {
                    Assert.That(input.OpenExpo(station),Is.True,"The Expo stays usable after closing time");
                    SelectRow(pad,input,ticket);
                    Press(pad,input,GamepadButton.South);
                    Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Active),"A seated diner can still be fired after 10 PM");
                }
                finally{input.BindDevices((InputDevice[])null);InputSystem.RemoveDevice(pad);}
            }
            Assert.That(table.Deliver(Plate(ticket.Recipe)),Is.True,"Late service still completes");
            Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Served));
            Assert.That(day.Served,Is.EqualTo(1));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
    }
}
