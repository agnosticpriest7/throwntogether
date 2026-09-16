using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor.SceneManagement;
namespace ThrownTogether.Tests
{
    public sealed class StaffLayoutTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public bool fail;public string Read()=>json;public void Write(string s){if(fail)throw new System.IO.IOException();json=s;}}
        Scene original,scene;GameObject[] suspended;Memory memory;RestaurantDay day;KitchenFurniture furniture;
        [UnitySetUp] public IEnumerator Setup()
        {
            original=SceneManager.GetActiveScene();suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];foreach(var g in suspended)g.SetActive(false);
            memory=new Memory();RestaurantAccounts.UseStorage(memory);SessionOptions.Kitchen=0;SessionOptions.ShiftOrders=0;
            var a=RestaurantAccounts.Current;int n=a.StartDay();a.Settle(n,5000,0);a.SetMenu(DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0).Select(r=>r.id).ToArray());
            yield return Load();
        }
        IEnumerator Load()
        {
            if(scene.IsValid()&&scene.isLoaded){SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);}
            Time.timeScale=1;SessionOptions.ManageBeforeService=true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;yield return null;
            day=Object.FindObjectsByType<RestaurantDay>().Single(d=>d.gameObject.scene==scene);furniture=day.GetComponent<KitchenFurniture>();
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            SceneManager.SetActiveScene(original);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);foreach(var g in suspended)if(g!=null)g.SetActive(true);
            Time.timeScale=1;SessionOptions.Kitchen=0;SessionOptions.ShiftOrders=6;SessionOptions.ManageBeforeService=false;RestaurantAccounts.ResetCache();
        }
        [UnityTest] public IEnumerator AllFreshKitchensUseRegularReachableGrid()
        {
            for(int k=0;k<3;k++)
            {
                SessionOptions.Kitchen=k;yield return Load();
                foreach(int i in new[]{0,1,3,4,5,6,7,8,10,11})Assert.IsTrue(KitchenFurniture.Regular(furniture.AssignedSlot("base:"+i)),"Kitchen "+k+" station "+i);
                Assert.IsTrue(furniture.Validate(out var why),"Kitchen "+k+": "+why);
            }
        }
        [UnityTest] public IEnumerator HomeDraftSavesAtomicallyAndRejectsBlockedFloor()
        {
            Assert.IsTrue(furniture.Begin());furniture.ToggleHomes();Assert.IsTrue(furniture.HomeMode);
            var homes=furniture.Homes;Assert.IsFalse(homes.TrySet("server",furniture.Find("base:3").position));
            var chosen=new Vector3(10,0,-4.8f);Assert.IsTrue(homes.TrySet("server",chosen),homes.Message);
            int cash=RestaurantAccounts.Current.Data.cash;memory.fail=true;Assert.IsFalse(furniture.Save());Assert.AreEqual(cash,RestaurantAccounts.Current.Data.cash);Assert.IsEmpty(RestaurantAccounts.Current.Data.staffHomes);
            memory.fail=false;Assert.IsTrue(furniture.Save(),furniture.Message);Assert.AreEqual(cash-100,RestaurantAccounts.Current.Data.cash);
            RestaurantAccounts.UseStorage(memory);yield return Load();Assert.AreEqual(chosen,furniture.Homes.Home("server"));
            Assert.IsTrue(furniture.Begin());Assert.IsTrue(furniture.Homes.TrySet("server",new Vector3(10.4f,0,-4.8f)));furniture.Cancel();Assert.AreEqual(chosen,furniture.Homes.Home("server"));
        }
        [Test] public void QuickPlayUsesSameRegularStartingGridWithoutWritingCareer()
        {
            string before=memory.json;SessionOptions.ShiftOrders=6;var layout=day.GetComponent<KitchenLayout>();
            for(int k=0;k<3;k++)
            {
                Assert.IsTrue(layout.Apply(k));
                foreach(int i in new[]{0,1,3,4,5,6,7,8,10,11})Assert.IsTrue(Enumerable.Range(0,KitchenFurniture.GridSlots.Length).Any(s=>KitchenFurniture.Regular(s) && Vector3.Distance(KitchenFurniture.GridSlots[s],layout.anchors[i].position)<.01f));
                Assert.IsNotNull(KitchenStaffRoute.ToPoint(layout.choices[k].playerOneSpawn,StaffHomes.Entrance(day)),"Kitchen "+k);
            }
            Assert.AreEqual(before,memory.json);
        }
        [Test] public void StaffArriveBeforeOpeningAndLeaveAfterCustomers()
        {
            Assert.IsTrue(furniture.TryPurchase(day.Settings.purchases.Single(p=>p.id=="expo-desk")),furniture.Message);
            foreach(var role in StaffHomes.Roles)Assert.IsTrue(RestaurantAccounts.Current.Buy(StaffHomes.PurchaseId(role),0),role);
            Assert.IsEmpty(day.Staff,"Management must not spawn workers into a changing layout");Assert.IsTrue(day.StartService());Assert.AreEqual(6,day.Staff.Count);
            Assert.IsTrue(day.StaffEntering);day.Advance(1);Assert.Zero(day.Elapsed);Assert.Zero(day.CustomersArrived);
            for(int i=0;i<1000&&day.StaffEntering;i++)day.Advance(.1f);
            Assert.IsFalse(day.StaffEntering);Assert.Zero(day.CustomersArrived);Assert.AreEqual(6,day.Staff.Count);
            foreach(var s in day.Staff)Assert.That(Vector3.Distance(s.transform.position,s.Home),Is.LessThan(.05f));
            day.Advance(day.Settings.durationSeconds);Assert.IsFalse(day.Closed);Assert.IsFalse(day.StaffLeaving,"Last guests still finishing/leaving");
            for(int i=0;i<4000&&!day.StaffLeaving;i++)day.Advance(.1f);
            Assert.IsTrue(day.StaffLeaving);Assert.Zero(day.CustomersRemaining);Assert.IsFalse(day.Closed);
            for(int i=0;i<1500&&!day.Closed;i++)day.Advance(.1f);
            Assert.IsTrue(day.Closed);Assert.IsTrue(day.Paid);Assert.IsTrue(day.Staff.All(s=>s.Gone));
        }
        [Test] public void LeavingWorkerRetainsFoodUntilCounterSpaceIsAvailable()
        {
            Assert.IsTrue(RestaurantAccounts.Current.Buy("server",0));Assert.IsTrue(day.StartService());
            for(int i=0;i<1000&&day.StaffEntering;i++)day.Advance(.1f);Assert.IsFalse(day.StaffEntering);
            var member=day.Staff.Single();var hands=member.GetComponentInChildren<CarrySlot>();
            var source=Object.FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==scene && !s.plates);
            var food=Object.Instantiate(source.itemPrefab);food.Configure(new ItemPayload{ingredient=DailyMenu.Catalog.First().ingredient,state=FoodState.Cut});Assert.IsTrue(hands.TryTake(food));
            var counters=Object.FindObjectsByType<CounterStation>().Where(c=>c.gameObject.scene==scene && c.GetType()==typeof(CounterStation)).ToArray();
            foreach(var c in counters){var item=Object.Instantiate(source.itemPrefab);item.Configure(food.Payload);c.slot.TryTake(item);}
            member.BeginDeparture();member.AdvanceDeparture(1);Assert.AreSame(food,hands.Item);Assert.IsFalse(member.Gone);StringAssert.Contains("Clear a counter",member.Status);
            var cleared=counters[0].slot.Release();Object.DestroyImmediate(cleared.gameObject);
            for(int i=0;i<1500&&!member.Gone;i++)member.AdvanceDeparture(.1f);
            Assert.IsTrue(member.Gone,member.Status);Assert.AreSame(food,counters[0].slot.Item);Assert.IsNull(hands.Item);
        }
        [Test] public void EmployeeBreakMenuControlsEveryRoleAndDepartureOverridesRest()
        {
            Assert.IsTrue(furniture.TryPurchase(day.Settings.purchases.Single(p=>p.id=="expo-desk")),furniture.Message);
            foreach(var role in StaffHomes.Roles)Assert.IsTrue(RestaurantAccounts.Current.Buy(StaffHomes.PurchaseId(role),0),role);
            Assert.IsTrue(day.StartService());for(int i=0;i<1000&&day.StaffEntering;i++)day.Advance(.1f);Assert.IsFalse(day.StaffEntering);
            var menu=day.GetComponent<RestaurantMenu>();menu.Close();menu.Open();int controls=System.Array.FindIndex(menu.VisibleOptions,s=>s=="Employee Controls");Assert.That(controls,Is.GreaterThanOrEqualTo(0));
            menu.SelectRow(controls);menu.ActivateSelection();Assert.AreEqual("Employee Controls",menu.Page);Assert.AreEqual(7,menu.VisibleOptions.Length);
            var pad=InputSystem.AddDevice<Gamepad>();var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                void Press(GamepadButton b){InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(b));InputSystem.Update();menu.Tick(true);InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();menu.Tick(true);}
                for(int i=0;i<6;i++){Press(GamepadButton.South);Assert.AreEqual(i+1,day.Staff.Count(s=>s.BreakRequested));Press(GamepadButton.DpadDown);}
                Press(GamepadButton.East);Assert.AreEqual("Main",menu.Page);
            }
            finally{InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
            menu.Close();for(int i=0;i<400&&!day.Staff.All(s=>s.OnBreak);i++)day.Advance(.1f);
            Assert.IsTrue(day.Staff.All(s=>s.OnBreak),string.Join(" / ",day.Staff.Select(s=>s.Role+": "+s.DisplayStatus)));
            var prep=day.Staff.Single(s=>s.Role=="prep-cook");Assert.IsTrue(prep.ToggleBreak());Assert.IsTrue(prep.AvailableForWork);Assert.AreEqual("Working",prep.DisplayStatus=="On break"?"On break":"Working");
            foreach(var member in day.Staff)member.BeginDeparture();
            for(int i=0;i<1500&&!day.Staff.All(s=>s.Gone);i++)foreach(var member in day.Staff)member.AdvanceDeparture(.1f);
            Assert.IsTrue(day.Staff.All(s=>s.Gone));
        }
        [Test] public void BusserBreakReturnsCarriedPlateBeforeResting()
        {
            Assert.IsTrue(RestaurantAccounts.Current.Buy("hire-busser",0));Assert.IsTrue(day.StartService());
            for(int i=0;i<1000&&day.StaffEntering;i++)day.Advance(.1f);var member=day.Staff.Single();var hands=member.GetComponentInChildren<CarrySlot>();
            var source=Object.FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==scene);var plate=Object.Instantiate(source.itemPrefab);var payload=ItemPayload.Plate();payload.MakeDirty();plate.Configure(payload);Assert.IsTrue(hands.TryTake(plate));
            Assert.IsTrue(member.ToggleBreak());var busser=day.GetComponent<DiningBusser>();for(int i=0;i<1000&&!member.OnBreak;i++)busser.Advance(.1f);
            Assert.IsTrue(member.OnBreak,member.DisplayStatus);Assert.IsNull(hands.Item);Assert.AreEqual(1,Object.FindObjectsByType<DishReturnStation>().Single(r=>r.gameObject.scene==scene).Count);
        }
        [Test] public void ControllerCanChooseEveryHomePlaceAndSaveWithoutMovingEquipment()
        {
            var menu=day.GetComponent<RestaurantMenu>();Assert.IsTrue(menu.OpenKitchenLayout());var pad=InputSystem.AddDevice<Gamepad>();
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                void Press(GamepadButton b){InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(b));InputSystem.Update();menu.Tick(true);InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();menu.Tick(true);}
                var before=furniture.Find("base:3").position;Press(GamepadButton.LeftShoulder);Assert.IsTrue(furniture.HomeMode);
                for(int i=1;i<=6;i++){Press(GamepadButton.RightShoulder);Assert.AreEqual(i%6,furniture.Homes.SelectedRole);}
                Press(GamepadButton.DpadUp);var selected=furniture.Homes.Cursor;Press(GamepadButton.South);Assert.AreEqual(selected,furniture.Homes.Home("server"));
                Press(GamepadButton.North);Assert.IsFalse(furniture.Editing);Assert.AreEqual("Restaurant",menu.Page);Assert.AreEqual(before,furniture.Find("base:3").position);
                Assert.AreEqual(selected,RestaurantAccounts.Current.Data.staffHomes.Single().position);
                Assert.IsTrue(menu.OpenKitchenLayout());Press(GamepadButton.LeftShoulder);Press(GamepadButton.East);Assert.IsFalse(furniture.HomeMode);Assert.IsTrue(furniture.Editing);Press(GamepadButton.East);Assert.IsFalse(furniture.Editing);
            }
            finally{InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ServerBreakUsesPhysicalWalkerAndResumeRetainsDelivery(int resumeAt)
        {
            bool resumeCarrying=resumeAt==1;
            Assert.IsTrue(RestaurantAccounts.Current.Buy("server",0));Assert.IsTrue(day.StartService());
            for(int i=0;i<1000&&day.StaffEntering;i++)day.Advance(.1f);
            var member=day.Staff.Single();var server=day.GetComponent<DiningServer>();var hands=member.GetComponentInChildren<CarrySlot>();
            var pass=Object.FindObjectsByType<ServiceStation>().Single(s=>s.gameObject.scene==scene);
            var stock=Object.FindObjectsByType<SourceStation>().Single(s=>s.gameObject.scene==scene&&s.plates);
            var recipe=DailyMenu.Catalog.First(r=>r.requiredPurchases.Length==0&&r.additionalIngredients.Length==0);
            var meal=stock.TakeCleanPlate();meal.Payload.AddFood(new ItemPayload{ingredient=recipe.ingredient,state=recipe.requiredState});meal.RefreshVisual();
            Assert.IsTrue(hands.TryTake(meal));var blocker=stock.TakeCleanPlate();Assert.IsTrue(pass.pickupSlot.TryTake(blocker));
            // Only the component origin is obstructed. The employee has a valid physical route.
            var obstruction=new GameObject("Test blocked component origin");obstruction.transform.position=server.transform.position+Vector3.up*.5f;obstruction.AddComponent<BoxCollider>().size=new Vector3(.5f,1,.5f);Physics.SyncTransforms();
            try
            {
                Assert.IsTrue(member.ToggleBreak());
                for(int i=0;i<1200&&!member.OnBreak;i++)
                {
                    var previousOwner=meal.Owner;
                    server.Advance(.05f);
                    if(resumeCarrying&&i==1)
                    {
                        Assert.AreSame(meal,hands.Item);Assert.IsTrue(member.ToggleBreak());
                        day.Tables[0].ReserveSeat();day.Tables[0].Seat(recipe);Assert.IsTrue(stock.ReturnCleanPlate(blocker));
                    }
                    if(meal.Owner!=hands&&previousOwner==hands)
                    {
                        var station=meal.Owner.GetComponentInParent<Interactable>();
                        if(station!=null)Assert.LessOrEqual(Vector3.Distance(member.transform.position,station.transform.position),1.85f,"No remote storage transfer");
                        if(resumeCarrying)break;
                    }
                    if(resumeAt==2&&member.GoingToBreak)
                    {
                        server.Advance(.05f);var position=member.transform.position;
                        Assert.IsTrue(member.ToggleBreak());Assert.AreEqual(position,member.transform.position);
                        Assert.IsTrue(stock.ReturnCleanPlate(blocker));Assert.IsTrue(pass.pickupSlot.TryTake(meal));day.Tables[0].ReserveSeat();day.Tables[0].Seat(recipe);
                        for(int j=0;j<1200&&day.Tables[0].order.tableSlot.Item!=meal;j++)server.Advance(.05f);
                        Assert.AreSame(meal,day.Tables[0].order.tableSlot.Item);Assert.IsFalse(member.BreakRequested);return;
                    }
                }
                if(resumeCarrying){Assert.AreSame(meal,day.Tables[0].order.tableSlot.Item);Assert.IsNull(hands.Item);}
                else
                {
                    Assert.AreEqual(0,resumeAt,"Must exercise Resume during home travel");Assert.IsTrue(member.OnBreak,member.DisplayStatus);Assert.IsNotNull(meal.Owner);Assert.IsNull(hands.Item);
                    Assert.IsInstanceOf<CounterStation>(meal.Owner.GetComponentInParent<Interactable>());
                }
            }
            finally{Object.DestroyImmediate(obstruction);}
        }
        KitchenDishwasher StartWasher(out StaffMember member,out WashingStation sink,out SourceStation stock)
        {
            Assert.IsTrue(RestaurantAccounts.Current.Buy("hire-dishwasher",0));Assert.IsTrue(day.StartService());
            for(int i=0;i<1000&&day.StaffEntering;i++)day.Advance(.1f);
            Assert.IsFalse(day.StaffEntering);member=day.Staff.Single();
            sink=Object.FindObjectsByType<WashingStation>().Single(s=>s.gameObject.scene==scene);
            stock=Object.FindObjectsByType<SourceStation>().Single(s=>s.gameObject.scene==scene&&s.plates);
            return day.GetComponent<KitchenDishwasher>();
        }
        Carryable DirtyFromStock(SourceStation stock,WashingStation sink)
        {
            var plate=stock.TakeCleanPlate();Assert.IsNotNull(plate);plate.Payload.MakeDirty();plate.RefreshVisual();Assert.IsTrue(sink.Enqueue(plate));return plate;
        }
        [TestCase(false)] [TestCase(true)]
        public void WasherBreakDoesNotFollowPlayerCollectedPlateIntoNextJob(bool queued)
        {
            var washer=StartWasher(out var member,out var sink,out var stock);
            var first=DirtyFromStock(stock,sink);var second=queued?DirtyFromStock(stock,sink):null;
            for(int i=0;i<1000&&sink.Busy;i++)washer.Advance(.1f);
            Assert.IsFalse(sink.Busy);Assert.AreSame(first,sink.slot.Item);
            var chef=Object.FindObjectsByType<ChefController>().First(c=>c.gameObject.scene==scene);
            Assert.IsTrue(sink.TakeClean(chef.Hands));Assert.IsTrue(member.ToggleBreak());
            for(int i=0;i<1200&&!member.OnBreak;i++)washer.Advance(.1f);
            Assert.IsTrue(member.OnBreak,member.DisplayStatus);Assert.AreSame(first,chef.Hands.Item);
            Assert.AreEqual(queued?1:0,sink.Count);if(queued){Assert.IsTrue(second.Payload.dirty);Assert.AreSame(second,sink.slot.Item);Assert.Zero(sink.Progress);Assert.IsFalse(sink.Working);}
        }
        [Test] public void WasherBreakFinishesExactlyOneOwnedPlate()
        {
            var washer=StartWasher(out var member,out var sink,out var stock);
            var first=DirtyFromStock(stock,sink);var second=DirtyFromStock(stock,sink);
            for(int i=0;i<1000&&sink.Progress<=0;i++)washer.Advance(.1f);
            Assert.Greater(sink.Progress,0);Assert.IsTrue(member.ToggleBreak());
            for(int i=0;i<1200&&!member.OnBreak;i++)washer.Advance(.1f);
            Assert.IsTrue(member.OnBreak,member.DisplayStatus);Assert.AreEqual(4,stock.CleanPlatesRemaining);
            Assert.AreSame(second,sink.slot.Item);Assert.IsTrue(second.Payload.dirty);Assert.Zero(sink.Progress);Assert.IsFalse(sink.Working);
        }
        [Test] public void DepartingWasherReleasesSinkForPlayerWithoutLosingPlate()
        {
            var washer=StartWasher(out var member,out var sink,out var stock);var plate=DirtyFromStock(stock,sink);
            for(int i=0;i<1000&&sink.Progress<=0;i++)washer.Advance(.1f);
            Assert.Greater(sink.Progress,0);float before=sink.Progress;member.BeginDeparture();
            Assert.IsFalse(sink.Working);Assert.AreSame(plate,sink.slot.Item);Assert.AreEqual(before,sink.Progress);
            for(int i=0;i<1000&&!member.Gone;i++)member.AdvanceDeparture(.1f);Assert.IsTrue(member.Gone,member.Status);
            var chef=Object.FindObjectsByType<ChefController>().First(c=>c.gameObject.scene==scene);
            chef.transform.position=KitchenStaffRoute.Approach(sink.transform).Value;
            Assert.IsTrue(sink.Interact(chef),"Departed employee must not lock manual washing");sink.Advance(sink.duration+1);
            Assert.IsFalse(sink.Busy);Assert.AreSame(plate,sink.slot.Item);Assert.IsFalse(plate.Payload.dirty);
        }
    }
}
