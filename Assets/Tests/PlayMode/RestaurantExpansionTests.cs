using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ThrownTogether.Tests
{
    public sealed class RestaurantExpansionTests
    {
        sealed class Memory:ISettingsStorage{public string json="";public string Read()=>json;public void Write(string s)=>json=s;}
        Memory memory;Scene original,scene;GameObject[] suspended;RestaurantDay day;RestaurantHud hud;
        [UnitySetUp] public IEnumerator Setup()
        {
            original=SceneManager.GetActiveScene();suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];foreach(var g in suspended)g.SetActive(false);
            memory=new Memory();RestaurantAccounts.UseStorage(memory);SessionOptions.Kitchen=0;SessionOptions.ShiftOrders=0;SessionOptions.ManageBeforeService=true;Time.timeScale=1;
            int n=RestaurantAccounts.Current.StartDay();RestaurantAccounts.Current.Settle(n,3000,0);
            RestaurantAccounts.Current.SetMenu(DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0).Select(r=>r.id).ToArray());
            yield return Load();
        }
        IEnumerator Load()
        {
            if(scene.IsValid()&&scene.isLoaded){SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);}
            SessionOptions.ManageBeforeService=true;Time.timeScale=1;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;yield return null;
            hud=Object.FindObjectsByType<RestaurantHud>().Single(h=>h.gameObject.scene==scene);day=hud.shift.Day;hud.chef.GetComponent<ChefInput>().enabled=false;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);SceneManager.SetActiveScene(original);
            foreach(var g in suspended)if(g!=null)g.SetActive(true);Time.timeScale=1;SessionOptions.ManageBeforeService=false;SessionOptions.ShiftOrders=6;SessionOptions.Kitchen=0;RestaurantAccounts.ResetCache();
        }
        void Buy(string id){var offer=day.Settings.purchases.Single(p=>p.id==id);Assert.That(RestaurantAccounts.Current.Buy(id,offer.cost),Is.True);day.RefreshExpansion();}
        [UnityTest] public IEnumerator ExtensionsPreserveOriginalBaysAndPersistNewPlacements()
        {
            var f=hud.GetComponent<KitchenFurniture>();var prior=Enumerable.Range(0,12).Select(i=>"base:"+i).Where(id=>f.Find(id)!=null).ToDictionary(id=>id,id=>f.Find(id).position);
            Assert.That(f.Bays.Length,Is.EqualTo(21));Buy(RestaurantExpansion.KitchenId);
            Assert.That(f.Bays.Length,Is.EqualTo(28));foreach(var pair in prior)Assert.That(f.Find(pair.Key).position,Is.EqualTo(pair.Value));
            Assert.That(f.Begin(),Is.True);
            for(int slot=21;slot<28;slot++)Assert.That(f.TryMove("base:3",slot,0),Is.True,"Bay "+(slot+1)+": "+f.Message);
            var seen=new HashSet<int>{0};var queue=new Queue<int>();queue.Enqueue(0);
            while(queue.Count>0){int source=queue.Dequeue();foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right}){f.Select(source);f.Navigate(direction);if(seen.Add(f.Selected))queue.Enqueue(f.Selected);}}
            Assert.That(seen.Count,Is.EqualTo(30),"Every expansion bay, Save and Cancel is controller reachable");
            Assert.That(f.Save(),Is.True,f.Message);int cash=RestaurantAccounts.Current.Data.cash;
            RestaurantAccounts.UseStorage(memory);yield return Load();f=hud.GetComponent<KitchenFurniture>();
            Assert.That(f.AssignedSlot("base:3"),Is.EqualTo(27));Assert.That(f.Find("base:3").position,Is.EqualTo(KitchenFurniture.ExpandedSlots[27]));
            Assert.That(RestaurantAccounts.Current.Data.cash,Is.EqualTo(cash));Assert.That(f.Validate(out var why),Is.True,why);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DiningAddsRealAccessibleSeatsAndCameraSurvivesDayNight()
        {
            Buy("dining-tables");Buy(RestaurantExpansion.DiningId);Buy(RestaurantExpansion.KitchenId);
            Assert.That(day.StartService(),Is.True);Assert.That(day.Tables.Length,Is.EqualTo(6));
            var camera=hud.gameplayCamera;float size=camera.orthographicSize;
            Assert.That(camera.orthographic,Is.True);Assert.That(camera.transform.eulerAngles.x,Is.EqualTo(45).Within(.01));
            foreach(var table in day.Tables)
            {
                var point=camera.WorldToViewportPoint(table.transform.position+Vector3.up);Assert.That(point.x,Is.InRange(.03f,.97f));Assert.That(point.y,Is.InRange(.05f,.90f));
                KitchenTestAccess.Approach(hud.chef,table);Assert.That(hud.chef.Focus,Is.SameAs(table));
                Assert.That(KitchenStaffRoute.ToPoint(new Vector3(6.3f,0,-5),day.TableApproach(table)),Is.Not.Null);
            }
            Assert.That(size,Is.GreaterThan(7));
            day.Advance(1000);Assert.That(day.Closed,Is.True);
            var presentation=hud.GetComponent<DayPresentation>();presentation.AdvancePresentation(DayPresentation.TransitionSeconds);
            Assert.That(presentation.Current,Is.EqualTo(DayPresentation.Phase.Night));
            Assert.That(camera.orthographicSize,Is.EqualTo(size*DayPresentation.CameraPullback).Within(.01));
            RestaurantAccounts.Current.PrepareCareer(0);RestaurantAccounts.UseStorage(memory);yield return Load();
            Assert.That(day.Tables.Length,Is.EqualTo(6));Assert.That(hud.gameplayCamera.orthographicSize,Is.EqualTo(size).Within(.02));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WaitingCustomersFormSpacedFifoLineAndAdvanceWhenSeatClears()
        {
            Assert.That(day.StartService(),Is.True);var settings=day.Settings;float interval=settings.arrivalInterval,patience=settings.outsidePatience;
            try
            {
                settings.arrivalInterval=1;settings.outsidePatience=100;
                foreach(var table in day.Tables)table.ReserveSeat();day.Advance(24);
                var line=day.OutsidePositions;Assert.That(line.Length,Is.GreaterThanOrEqualTo(8));
                for(int i=0;i<line.Length;i++)Assert.That(Vector3.Distance(line[i],day.QueuePosition(i)),Is.LessThan(.03f));
                day.Tables[0].Depart();day.Advance(.1f);Assert.That(day.Tables[0].Arriving,Is.True);
                day.Advance(1);Assert.That(day.WaitingOutside,Is.EqualTo(line.Length-1));
                Assert.That(Vector3.Distance(day.OutsidePositions[0],day.QueuePosition(0)),Is.LessThan(.03f));
                Assert.That(day.OutsidePositions.All(p=>p.x<settings.entrance.x-.8f),Is.True,"Entrance stays clear");
            }
            finally{settings.arrivalInterval=interval;settings.outsidePatience=patience;}
            yield return null;LogAssert.NoUnexpectedReceived();
        }
    }
}
