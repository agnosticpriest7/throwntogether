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
        [UnityTest] public IEnumerator PurchasedEquipmentCanBeArrangedAndControllerCanSaveWithoutMouse()
        {
            var account=RestaurantAccounts.Current;int funded=account.StartDay();Assert.That(account.Settle(funded,1000,0),Is.True);
            foreach(var offer in day.Settings.purchases.Where(p=>p.stationPrefab!=null))Assert.That(account.Buy(offer.id,offer.cost),Is.True);
            var f=hud.GetComponent<KitchenFurniture>();Assert.That(f.Begin(),Is.True);Assert.That(f.Count,Is.EqualTo(12));
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
            Assert.That(f.Count,Is.EqualTo(12));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MovesSaveReloadCancelAndServiceLock()
        {
            var furniture=hud.GetComponent<KitchenFurniture>();Assert.That(furniture,Is.Not.Null);Assert.That(furniture.Count,Is.EqualTo(8));
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

