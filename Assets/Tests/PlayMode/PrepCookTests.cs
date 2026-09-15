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
    public sealed class PrepCookTests
    {
        sealed class Memory:ISettingsStorage{public string json="";public string Read()=>json;public void Write(string value)=>json=value;}
        Scene scene,original;GameObject[] suspended;RestaurantDay day;PrepBin bin;ProcessingStation prep;SourceStation source;RecipeDefinition fries;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;RestaurantAccounts.UseStorage(new Memory());var account=RestaurantAccounts.Current;
            int paid=account.StartDay();account.Settle(paid,2000,0);
            Assert.IsTrue(account.Buy("prep-cook",150));Assert.IsTrue(account.BuyEquipment("prep-bin",60));
            Assert.IsTrue(account.BuyEquipment("expo-desk",0));
            if(TestContext.CurrentContext.Test.Name.StartsWith("Restock"))
            {
                account.BuyEquipment("prep-bin",60);var owned=account.Equipment("prep-bin",60).ToArray();
                var potato=DailyMenu.Catalog.SelectMany(r=>r.steps).Select(p=>p.ingredient).First(i=>i!=null && i.visualKind==IngredientVisualKind.Potato);
                var mushroom=DailyMenu.Catalog.SelectMany(r=>r.steps).Select(p=>p.ingredient).First(i=>i!=null && i.visualKind==IngredientVisualKind.Mushroom);
                Assert.IsTrue(account.AssignPrepBins(0,true,new[]{new PrepBinAssignment{bin="purchase:"+owned[0].instanceId,ingredient=potato.id},new PrepBinAssignment{bin="purchase:"+owned[1].instanceId,ingredient=mushroom.id}}));
            }
            account.SetMenu(DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0).Select(r=>r.id).ToArray());
            SessionOptions.ShiftOrders=0;SessionOptions.Kitchen=0;original=SceneManager.GetActiveScene();
            suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];
            foreach(var go in suspended)go.SetActive(false);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;
            day=Object.FindObjectsByType<RestaurantDay>().Single(d=>d.gameObject.scene==scene);
            bin=Object.FindObjectsByType<PrepBin>().First(b=>b.gameObject.scene==scene);
            prep=Object.FindObjectsByType<ProcessingStation>().First(p=>p.gameObject.scene==scene && p.requiresAttendance);
            fries=DailyMenu.Catalog.First(r=>r.ingredient.visualKind==IngredientVisualKind.Potato && r.requiredState==FoodState.Cooked && r.additionalIngredients.Length==0);
            source=Object.FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==scene && s.Offers(fries.ingredient));
            Assert.IsNotNull(day.PrepCook);Assert.IsNotNull(day.Expo);Assert.IsFalse(day.AwaitingMenu);
            for(int i=0;i<1000 && day.StaffEntering;i++)day.Advance(.1f);Assert.IsFalse(day.StaffEntering);
            foreach(var input in Object.FindObjectsByType<ChefInput>().Where(i=>i.gameObject.scene==scene))input.enabled=false;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(RestaurantMenu.Instance!=null && RestaurantMenu.Instance.IsOpen)RestaurantMenu.Instance.Close();
            Time.timeScale=1;SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);
            foreach(var go in suspended)if(go!=null)go.SetActive(true);
            RestaurantAccounts.ResetCache();SessionOptions.ShiftOrders=6;SessionOptions.Kitchen=0;
        }
        KitchenTicket Seat(int index=0)
        {var t=day.Tables[index];t.ReserveSeat();t.Seat(fries);return day.Expo.TicketFor(t);}
        void Tick(int frames=800){for(int i=0;i<frames;i++)day.PrepCook.Advance(.1f);}
        [Test] public void RestockBreakFinishesOneStartedPortionThenResumesFillingBins()
        {
            var member=day.Staff.Single(s=>s.Role=="prep-cook");
            for(int i=0;i<800 && !prep.Busy;i++)day.PrepCook.Advance(.1f);Assert.IsTrue(prep.Busy,day.PrepCook.Status);
            Assert.IsTrue(member.ToggleBreak());for(int i=0;i<1200&&!member.OnBreak;i++)day.PrepCook.Advance(.1f);
            Assert.IsTrue(member.OnBreak,member.DisplayStatus);int stored=day.GetComponent<KitchenFurniture>().PrepBins.Sum(p=>p.Value.Count);
            Assert.AreEqual(1,stored,"Only the portion already being chopped may finish");Tick(200);Assert.AreEqual(stored,day.GetComponent<KitchenFurniture>().PrepBins.Sum(p=>p.Value.Count));
            Assert.IsTrue(member.ToggleBreak());Tick(3000);foreach(var assigned in RestaurantAccounts.Current.PrepAssignment(0).bins)Assert.AreEqual(5,day.GetComponent<KitchenFurniture>().PrepBins.Single(p=>p.Key==assigned.bin).Value.Count);
        }
        [Test] public void RestockFillsTwoAssignedBinsWithoutOrdersAndReplacesTakenFood()
        {
            var assignment=RestaurantAccounts.Current.PrepAssignment(0);var bins=day.GetComponent<KitchenFurniture>().PrepBins;
            Assert.IsEmpty(day.Expo.OpenTickets);Tick(5000);
            foreach(var a in assignment.bins){var target=bins.Single(b=>b.Key==a.bin).Value;Assert.AreEqual(5,target.Count,day.PrepCook.Status);Assert.AreEqual(a.ingredient,target.Ingredient.id);}
            var first=bins.Single(b=>b.Key==assignment.bins[0].bin).Value;
            var player=Object.FindObjectsByType<ChefController>().First(c=>c.gameObject.scene==scene);
            Assert.IsTrue(first.Take(player.Hands));Tick(1000);Assert.AreEqual(5,first.Count);Assert.IsNotNull(player.Hands.Item,"Restocking must count bin stock, not player-held stock");
            Assert.IsEmpty(day.Expo.OpenTickets);Assert.IsEmpty(day.PrepCook.Production.Ledger.Claims);
        }
        [Test] public void RestockRetainsFoodWhenPlayerFillsItsBinAndResumesAfterSpaceClears()
        {
            for(int i=0;i<500 && day.PrepCook.Hands.Item==null;i++)day.PrepCook.Advance(.1f);
            var item=day.PrepCook.Hands.Item;Assert.IsNotNull(item,day.PrepCook.Status);
            var assigned=RestaurantAccounts.Current.PrepAssignment(0).bins.Single(b=>b.ingredient==item.Payload.ingredient.id);
            var target=day.GetComponent<KitchenFurniture>().PrepBins.Single(b=>b.Key==assigned.bin).Value;
            for(int i=0;i<5;i++){var portion=Object.Instantiate(source.itemPrefab);portion.Configure(new ItemPayload{ingredient=item.Payload.ingredient,state=FoodState.Cut});Assert.IsTrue(target.Store(portion));}
            Tick(1000);Assert.AreSame(item,day.PrepCook.Hands.Item);Assert.AreEqual(FoodState.Cut,item.Payload.state);Assert.AreEqual(5,target.Count);
            var player=Object.FindObjectsByType<ChefController>().First(c=>c.gameObject.scene==scene);Assert.IsTrue(target.Take(player.Hands));Tick(500);
            Assert.AreSame(target,item.GetComponentInParent<PrepBin>());Assert.AreEqual(5,target.Count);
        }
        Carryable ManualPortion(FoodState state)
        {var item=Object.Instantiate(source.itemPrefab);item.Configure(new ItemPayload{ingredient=fries.ingredient,state=state});return item;}
        [Test] public void FiredOrderGetsPhysicalPrepAndHoldPreservesTheCollectedFood()
        {
            var ticket=Seat();Tick(100);Assert.Zero(bin.Count);Assert.IsNull(day.PrepCook.Hands.Item);
            Assert.IsTrue(day.Expo.TryFire(ticket));
            for(int i=0;i<400 && day.PrepCook.ReservedItem==null;i++)day.PrepCook.Advance(.1f);
            var originalFood=day.PrepCook.ReservedItem;Assert.IsNotNull(originalFood,day.PrepCook.Status);
            Assert.IsTrue(day.Expo.TryHold(ticket));Tick();
            Assert.AreEqual(1,bin.Count,day.PrepCook.Status);Assert.AreEqual(FoodState.Cut,originalFood.Payload.state);
            Assert.AreSame(bin,originalFood.GetComponentInParent<PrepBin>());Assert.IsEmpty(day.PrepCook.Production.Ledger.Claims);
            Assert.IsTrue(day.Expo.TryFire(ticket));Tick(300);Assert.AreEqual(1,bin.Count,"Existing cut portion must satisfy returning demand");
        }
        [Test] public void ManualStockCountsOnceAcrossTwoIdenticalOrders()
        {
            Assert.IsTrue(bin.Store(ManualPortion(FoodState.Cut)));
            Assert.IsTrue(day.Expo.TryFire(Seat(0)));Assert.IsTrue(day.Expo.TryFire(Seat(1)));Tick();
            Assert.AreEqual(2,bin.Count,day.PrepCook.Status);Tick(200);Assert.AreEqual(2,bin.Count);
            var player=Object.FindObjectsByType<ChefController>().First(c=>c.gameObject.scene==scene);
            Assert.IsTrue(bin.Take(player.Hands));Tick(200);Assert.AreEqual(1,bin.Count,"Player-held cut food still counts");
        }
        [Test] public void OccupiedPrepWaitsThenRecoversWithoutStealingPlayerFood()
        {
            var blocking=ManualPortion(FoodState.Cut);blocking.Payload.ingredient=source.Ingredients.First(i=>i!=fries.ingredient);prep.slot.TryTake(blocking);
            Assert.IsTrue(day.Expo.TryFire(Seat()));Tick(120);
            Assert.AreSame(blocking,prep.slot.Item);Assert.Zero(bin.Count);Assert.IsNull(day.PrepCook.Hands.Item);
            prep.slot.Release();Object.Destroy(blocking.gameObject);Tick();Assert.AreEqual(1,bin.Count,day.PrepCook.Status);
        }
        [Test] public void HoldBeforeRetrievalReleasesClaimWithoutSpawningFood()
        {
            var ticket=Seat();day.Expo.TryFire(ticket);
            for(int i=0;i<20 && day.PrepCook.Production.Ledger.Claims.Count==0;i++)day.PrepCook.Advance(.1f);
            Assert.AreEqual(1,day.PrepCook.Production.Ledger.Claims.Count);Assert.IsNull(day.PrepCook.ReservedItem);
            day.Expo.TryHold(ticket);Tick(100);Assert.IsEmpty(day.PrepCook.Production.Ledger.Claims);Assert.Zero(bin.Count);Assert.IsNull(day.PrepCook.Hands.Item);
        }
        [Test] public void FullOutputsWaitAndRecoverWhenPlayerClearsSpace()
        {
            for(int i=0;i<5;i++){var item=ManualPortion(FoodState.Cut);item.Payload.ingredient=source.Ingredients.First(p=>p!=fries.ingredient);bin.Store(item);}
            var counters=Interactable.Active.Where(s=>s.gameObject.scene==scene && s.GetType()==typeof(CounterStation)).Cast<CounterStation>().ToArray();
            foreach(var counter in counters){var item=ManualPortion(FoodState.Cut);item.Payload.ingredient=source.Ingredients.First(p=>p!=fries.ingredient);counter.slot.TryTake(item);}
            day.Expo.TryFire(Seat());Tick(100);Assert.IsNull(day.PrepCook.Hands.Item);StringAssert.Contains("counter",day.PrepCook.Status);
            var free=counters[0];var removed=free.slot.Release();Object.Destroy(removed.gameObject);Tick();
            Assert.IsNotNull(free.slot.Item,day.PrepCook.Status);Assert.AreSame(fries.ingredient,free.slot.Item.Payload.ingredient);Assert.AreEqual(FoodState.Cut,free.slot.Item.Payload.state);
        }
        [Test] public void CompletedHeldPortionCanBeCookedPlatedAndServed()
        {
            var ticket=Seat();day.Expo.TryFire(ticket);Tick();Assert.AreEqual(1,bin.Count,day.PrepCook.Status);day.Expo.TryHold(ticket);
            var player=Object.FindObjectsByType<ChefController>().First(c=>c.gameObject.scene==scene);bin.Take(player.Hands);
            var fryer=Object.FindObjectsByType<ProcessingStation>().First(p=>p.gameObject.scene==scene && !p.requiresAttendance && p.appliance.id.Contains("fryer"));
            Assert.IsTrue(fryer.Interact(player));fryer.Advance(20);Assert.IsTrue(fryer.Interact(player));
            var stock=Object.FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==scene && s.plates);Assert.IsTrue(stock.Interact(player));
            Assert.IsTrue(day.Tables[0].Interact(player));Assert.AreEqual(KitchenTicketState.Served,ticket.State);
            Assert.AreEqual(1,day.Served);Tick(100);Assert.Zero(bin.Count);
        }
        [Test] public void ControllerAssignmentAndPurchasedBinUseExistingManagementFlow()
        {
            day.Advance(700);Assert.IsTrue(day.Closed);Assert.IsTrue(day.Paid);
            var menu=day.GetComponent<RestaurantMenu>();menu.OpenRestaurant();
            var pad=InputSystem.AddDevice<Gamepad>();var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            void Press(GamepadButton button){InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();menu.Tick(true);InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));InputSystem.Update();menu.Tick(true);}
            try
            {
                menu.SelectRow(System.Array.FindIndex(menu.VisibleOptions,s=>s=="Employee Management"));Press(GamepadButton.South);Assert.AreEqual("Employees",menu.Page);
                menu.SelectRow(System.Array.FindIndex(menu.VisibleOptions,s=>s=="Prep cook assignment"));Press(GamepadButton.South);Assert.AreEqual("Prep cook",menu.Page);
                Press(GamepadButton.DpadRight);Assert.IsNotEmpty(RestaurantAccounts.Current.PrepAssignment(0).station);
                Press(GamepadButton.DpadDown);Assert.AreEqual(1,menu.Selection);Press(GamepadButton.South);Assert.IsTrue(RestaurantAccounts.Current.PrepAssignment(0).restock);
                Press(GamepadButton.DpadDown);Press(GamepadButton.South);Assert.AreEqual(1,RestaurantAccounts.Current.PrepAssignment(0).bins.Length);
                Press(GamepadButton.DpadUp);Press(GamepadButton.South);Assert.IsFalse(RestaurantAccounts.Current.PrepAssignment(0).restock);
                Press(GamepadButton.DpadDown);Press(GamepadButton.South);Assert.IsNotEmpty(RestaurantAccounts.Current.PrepAssignment(0).ingredient);
                Press(GamepadButton.East);Assert.AreEqual("Employees",menu.Page);Press(GamepadButton.East);Assert.AreEqual("Restaurant",menu.Page);
                var furniture=day.GetComponent<KitchenFurniture>();var offer=day.Settings.purchases.First(o=>o.id=="prep-bin");
                Assert.IsTrue(furniture.TryPurchase(offer),furniture.Message);var owned=RestaurantAccounts.Current.Data.equipment.Last();
                Assert.IsTrue(furniture.Begin());int slot=furniture.AssignedSlot("purchase:"+owned.instanceId);
                Assert.IsTrue(furniture.TryMove("purchase:"+owned.instanceId,slot,1),furniture.Message);Assert.IsTrue(furniture.Save(),furniture.Message);
                Assert.IsTrue(RestaurantAccounts.Current.Data.furniture.Any(p=>p.id=="purchase:"+owned.instanceId && p.turns==1));
                int cash=RestaurantAccounts.Current.Data.cash;Assert.IsTrue(RestaurantAccounts.Current.SellEquipment(owned.instanceId,"prep-bin",60));furniture.RemovePurchased(owned.instanceId);
                Assert.AreEqual(cash+45,RestaurantAccounts.Current.Data.cash);
            }
            finally{InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
        }
    }
}
