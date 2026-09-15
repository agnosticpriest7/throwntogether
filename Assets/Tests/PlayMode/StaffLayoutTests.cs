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
    }
}
