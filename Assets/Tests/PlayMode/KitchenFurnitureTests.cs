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
    public sealed class KitchenFurnitureTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public bool fail;public string Read()=>json;public void Write(string s){if(fail)throw new System.IO.IOException();json=s;}}
        Scene original,scene;GameObject[] suspended;Memory memory;RestaurantHud hud;RestaurantDay day;ChefController chef;
        [UnitySetUp] public IEnumerator Setup()
        {
            original=SceneManager.GetActiveScene();suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];foreach(var g in suspended)g.SetActive(false);
            memory=new Memory();RestaurantAccounts.UseStorage(memory);SessionOptions.Kitchen=0;SessionOptions.ShiftOrders=0;Time.timeScale=1;
            yield return Load();
        }
        IEnumerator Load()
        {
            if(scene.IsValid() && scene.isLoaded){SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);}
            Time.timeScale=1;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;yield return null;
            hud=Object.FindObjectsByType<RestaurantHud>().Single(h=>h.gameObject.scene==scene);day=hud.shift.Day;chef=hud.chef;chef.GetComponent<ChefInput>().enabled=false;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(scene.IsValid() && scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            SceneManager.SetActiveScene(original);foreach(var g in suspended)if(g!=null)g.SetActive(true);
            Time.timeScale=1;SessionOptions.ShiftOrders=6;SessionOptions.Kitchen=0;RestaurantAccounts.ResetCache();
        }
        [UnityTest] public IEnumerator ManagementHubRepeatEquipmentAndPlateSupplySurviveReload()
        {
            var account=RestaurantAccounts.Current;int paid=account.StartDay();account.Settle(paid,3000,0);
            var menu=hud.GetComponent<RestaurantMenu>();menu.OpenRestaurant();menu.NavigateBack();Assert.That(menu.Page,Is.EqualTo("Restaurant"));
            Assert.That(menu.VisibleOptions[0],Is.EqualTo("Employee Management"));Assert.That(menu.VisibleOptions[3],Does.StartWith("Next Day"));
            menu.SelectRow(0);menu.ActivateSelection();Assert.That(menu.Page,Is.EqualTo("Employees"));menu.NavigateBack();Assert.That(menu.Page,Is.EqualTo("Restaurant"));
            var f=hud.GetComponent<KitchenFurniture>();var offer=day.Settings.purchases.First(p=>p.stationPrefab!=null && p.kind==RestaurantPurchaseKind.FryerBay);
            Assert.That(f.TryPurchase(offer),Is.True,f.Message);Assert.That(f.TryPurchase(offer),Is.True,f.Message);Assert.That(account.Quantity(offer.id),Is.EqualTo(2));
            var owned=account.Equipment(offer.id,offer.cost);var position=f.Find("purchase:"+owned[0].instanceId).position;
            account.BuyEquipment("extra-plate",20);account.BuyEquipment("extra-plate",20);
            RestaurantAccounts.UseStorage(memory);yield return Load();f=hud.GetComponent<KitchenFurniture>();
            Assert.That(f.Find("purchase:"+owned[0].instanceId).position,Is.EqualTo(position));Assert.That(f.Validate(out var why),Is.True,why);
            var stock=Object.FindObjectsByType<SourceStation>().First(x=>x.gameObject.scene==scene && x.plates);Assert.That(stock.TotalCapacity,Is.EqualTo(7));
            for(int i=0;i<7;i++)Assert.That(stock.TakeCleanPlate(),Is.Not.Null);Assert.That(stock.TakeCleanPlate(),Is.Null);
        }
        [UnityTest] public IEnumerator AllKitchenEquipmentStartsOnBaysAndEveryUiControlIsReachable()
        {
            var f=hud.GetComponent<KitchenFurniture>();Assert.That(f.Validate(out var why),Is.True,why);Assert.That(f.Begin(),Is.True);
            foreach(int anchor in new[]{0,1,3,4,5,6,7,8,10,11})Assert.That(f.AssignedSlot("base:"+anchor),Is.GreaterThanOrEqualTo(0));
            var seen=new System.Collections.Generic.HashSet<int>{0};var pending=new System.Collections.Generic.Queue<int>();pending.Enqueue(0);
            while(pending.Count>0){int start=pending.Dequeue();foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right}){f.Select(start);f.Navigate(direction);if(seen.Add(f.Selected))pending.Enqueue(f.Selected);}}
            Assert.That(seen.Count,Is.EqualTo(KitchenFurniture.Slots.Length+2),"Every bay, Save and Cancel must be reachable by D-pad");
            Assert.That(f.TryMove("base:8",6,0),Is.True,f.Message);Assert.That(f.TryMove("base:11",9,0),Is.True,f.Message);f.Cancel();
            var menu=hud.GetComponent<RestaurantMenu>();menu.OpenFrontEnd();var pad=InputSystem.AddDevice<Gamepad>();
            try{hud.coop.UseControllerPlayerOne();hud.coop.BindPlayerOne(null);Assert.That(hud.coop.Join(pad),Is.False);hud.coop.RefreshPlayerOneAssignment();Assert.That(hud.coop.PlayerOnePad,Is.SameAs(pad));Assert.That(hud.coop.PlayerTwo,Is.Null);}
            finally{InputSystem.RemoveDevice(pad);menu.Close();}
            yield return null;
        }
        [UnityTest] public IEnumerator CareerResetDefaultsToCancelAndWriteFailureKeepsMenuOpen()
        {
            var account=RestaurantAccounts.Current;int funded=account.StartDay();account.Settle(funded,100,0);
            var menu=hud.GetComponent<RestaurantMenu>();menu.OpenRestaurant();menu.RequestCareerReset();
            Assert.That(menu.Page,Is.EqualTo("Reset career"));Assert.That(menu.Selection,Is.Zero);
            menu.ActivateSelection();Assert.That(menu.Page,Is.EqualTo("Restaurant"));Assert.That(account.Data.cash,Is.EqualTo(100));
            menu.RequestCareerReset();var pad=InputSystem.AddDevice<Gamepad>();
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                void Send(GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();menu.Tick(true);}
                memory.fail=true;Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadDown));Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.South));
                Assert.That(menu.Page,Is.EqualTo("Reset career"));Assert.That(menu.IsOpen,Is.True);Assert.That(account.Data.cash,Is.EqualTo(100));
                Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.East));Assert.That(menu.Page,Is.EqualTo("Restaurant"));
            }
            finally{InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;memory.fail=false;}
            yield return null;
        }
        [UnityTest] public IEnumerator PurchasedEquipmentCanBeArrangedAndControllerCanSaveWithoutMouse()
        {
            var account=RestaurantAccounts.Current;int funded=account.StartDay();Assert.That(account.Settle(funded,1000,0),Is.True);
            foreach(var offer in day.Settings.purchases.Where(p=>p.stationPrefab!=null))Assert.That(account.Buy(offer.id,offer.cost),Is.True);
            var f=hud.GetComponent<KitchenFurniture>();Assert.That(f.Begin(),Is.True);Assert.That(f.Count,Is.EqualTo(14));
            Assert.That(f.TryMove("purchase:counter-bay",10,0),Is.True,f.Message);Assert.That(f.Save(),Is.True,f.Message);
            var menu=hud.GetComponent<RestaurantMenu>();menu.OpenRestaurant();
            var pad=InputSystem.AddDevice<Gamepad>();
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                void Send(GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();menu.Tick(true);}
                Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadDown));Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.South));
                Assert.That(menu.Page,Is.EqualTo("Kitchen layout"));
                Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.RightShoulder));Assert.That(f.Held,Is.GreaterThanOrEqualTo(0));
                Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.East));Assert.That(f.Held,Is.EqualTo(-1));
                Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.North));Assert.That(menu.Page,Is.EqualTo("Restaurant"));
            }
            finally{InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
            RestaurantAccounts.UseStorage(memory);yield return Load();f=hud.GetComponent<KitchenFurniture>();Assert.That(f.Find("purchase:counter-bay").position,Is.EqualTo(KitchenFurniture.Slots[10]));
            Assert.That(f.Count,Is.EqualTo(14));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MovesSaveReloadCancelAndServiceLock()
        {
            var furniture=hud.GetComponent<KitchenFurniture>();Assert.That(furniture,Is.Not.Null);Assert.That(furniture.Count,Is.EqualTo(10));
            Assert.That(furniture.Validate(out var reason),Is.True,reason);Assert.That(furniture.Begin(),Is.True);
            var prep=furniture.Find("base:3");var originalPosition=prep.position;
            Assert.That(furniture.TryMove("base:3",6,0),Is.True,furniture.Message);
            furniture.Cancel();Assert.That(prep.position,Is.EqualTo(originalPosition));
            Assert.That(furniture.Begin(),Is.True);Assert.That(furniture.TryMove("base:3",6,0),Is.True,furniture.Message);
            Assert.That(furniture.Save(),Is.True,furniture.Message);RestaurantAccounts.UseStorage(memory);yield return Load();
            furniture=hud.GetComponent<KitchenFurniture>();Assert.That(furniture.Find("base:3").position,Is.EqualTo(KitchenFurniture.Slots[6]));
            foreach(var r in DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0))DailyMenu.Toggle(RestaurantAccounts.Current,r);
            Assert.That(hud.GetComponent<RestaurantMenu>().StartSelectedMenu(),Is.True);yield return null;yield return null;
            Assert.That(furniture.Begin(),Is.False,"No movement during service");
            var potato=DailyMenu.Catalog.First(r=>r.id.Contains("fries")).ingredient;KitchenTestAccess.Take(chef,potato);
            var station=furniture.Find("base:3").GetComponentInChildren<ProcessingStation>();KitchenTestAccess.Approach(chef,station);Assert.That(chef.Use(),Is.True);
            station.Advance(30);Assert.That(chef.Use(),Is.True);Assert.That(chef.Hands.Item,Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CollisionRejectedAndOccupiedSlotsSwapWithoutLosingEquipment()
        {
            var f=hud.GetComponent<KitchenFurniture>();Assert.That(f.Begin(),Is.True);
            Assert.That(f.TryMove("base:6",13,0),Is.True,f.Message);
            Assert.That(f.Find("base:6").position,Is.EqualTo(KitchenFurniture.Slots[13]));
            Assert.That(f.Find("base:7").position,Is.EqualTo(KitchenFurniture.Slots[12]));
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(blocker,scene);blocker.transform.position=KitchenFurniture.Slots[3]+Vector3.up*.7f;
            Assert.That(f.TryMove("base:6",3,0),Is.False);Assert.That(f.Find("base:6").position,Is.EqualTo(KitchenFurniture.Slots[13]));
            Object.DestroyImmediate(blocker);f.Cancel();yield return null;LogAssert.NoUnexpectedReceived();
        }
    }
}

