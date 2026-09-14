using System.Collections;
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
    public sealed class CookTests
    {
        sealed class Memory:ISettingsStorage{public string json="";public string Read()=>json;public void Write(string s)=>json=s;}
        Scene scene,original;GameObject[] suspended;RestaurantDay day;ChefController chef;ServiceStation pass;SourceStation plates;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;RestaurantAccounts.UseStorage(new Memory());var a=RestaurantAccounts.Current;int d=a.StartDay();a.Settle(d,4000,0);
            a.SetMenu(DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0).Select(r=>r.id).ToArray());
            SessionOptions.ShiftOrders=0;SessionOptions.Kitchen=0;SessionOptions.ManageBeforeService=true;
            original=SceneManager.GetActiveScene();suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];foreach(var g in suspended)g.SetActive(false);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;yield return null;
            day=Object.FindObjectsByType<RestaurantDay>().Single(d=>d.gameObject.scene==scene);chef=Object.FindObjectsByType<ChefController>().Single(c=>c.gameObject.scene==scene);chef.GetComponent<ChefInput>().enabled=false;
            pass=Object.FindObjectsByType<ServiceStation>().Single(s=>s.gameObject.scene==scene);plates=Object.FindObjectsByType<SourceStation>().Single(s=>s.gameObject.scene==scene && s.plates);
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            day.GetComponent<RestaurantMenu>().Close();Time.timeScale=1;SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);foreach(var g in suspended)if(g!=null)g.SetActive(true);
            RestaurantAccounts.ResetCache();SessionOptions.ManageBeforeService=false;SessionOptions.ShiftOrders=6;SessionOptions.Kitchen=0;
        }
        void StartCook()
        {
            var f=day.GetComponent<KitchenFurniture>();foreach(string id in new[]{"expo-desk","grill","griddle"})Assert.IsTrue(f.TryPurchase(day.Settings.purchases.Single(p=>p.id==id)),f.Message);
            Assert.IsTrue(RestaurantAccounts.Current.Buy("cook",200));RestaurantAccounts.Current.SetMenu(DailyMenu.Catalog.Select(r=>r.id).ToArray());
            Assert.IsTrue(day.StartService());day.GetComponent<RestaurantMenu>().Close();Assert.IsNotNull(day.Cook);
        }
        RecipeDefinition Fries=>DailyMenu.Catalog.First(r=>r.ingredient.visualKind==IngredientVisualKind.Potato && r.requiredState==FoodState.Cooked && r.additionalIngredients.Length==0);
        KitchenTicket Seat(RecipeDefinition recipe,int index=0){var table=day.Tables[index];table.ReserveSeat();table.Seat(recipe);return day.Expo.TicketFor(table);}
        void Tick(int count=1500){for(int i=0;i<count;i++){foreach(var p in Object.FindObjectsByType<ProcessingStation>().Where(s=>s.gameObject.scene==scene))p.Advance(.1f);day.Cook.Advance(.1f);}}
        Carryable Stock(RecipeDefinition recipe)
        {
            var hot=KitchenCook.HotStep(recipe);var source=Object.FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==scene && s.Offers(recipe.ingredient));
            var item=Object.Instantiate(source.itemPrefab);item.Configure(new ItemPayload{ingredient=recipe.ingredient,state=hot.input});
            var counter=Object.FindObjectsByType<CounterStation>().First(c=>c.gameObject.scene==scene && c.GetType()==typeof(CounterStation) && c.slot.Item==null);counter.slot.TryTake(item);return item;
        }
        [Test] public void ManagementBeforeServiceAndInstalledExpoGateWork()
        {
            Assert.IsTrue(day.AwaitingMenu);Assert.IsNull(day.Expo);Assert.IsFalse(day.HasInstalledExpo);Assert.AreEqual("Restaurant",day.GetComponent<RestaurantMenu>().Page);
            day.Advance(100);Assert.Zero(day.CustomersArrived);Assert.Zero(day.DayNumber);Assert.IsTrue(day.GetComponent<KitchenFurniture>().Begin());day.GetComponent<KitchenFurniture>().Cancel();
            Assert.IsFalse(RestaurantAccounts.Current.Buy("cook",200));StartCook();Assert.IsFalse(day.AwaitingMenu);Assert.IsTrue(day.HasInstalledExpo);
        }
        [Test] public void AllSingleComponentHotRecipesCookPlateAndStage()
        {
            StartCook();foreach(var recipe in DailyMenu.Catalog.Where(r=>KitchenCook.HotStep(r)!=null))
            {
                if(KitchenCook.HotStep(recipe).input!=FoodState.Raw)Stock(recipe);
                var ticket=Seat(recipe);Assert.IsTrue(day.Expo.TryFire(ticket));Tick();
                Assert.IsNotNull(pass.pickupSlot.Item,recipe.displayName+": "+day.Cook.Status);Assert.IsTrue(recipe.Matches(pass.pickupSlot.Item.Payload),recipe.displayName);
                Assert.AreSame(ticket,day.Expo.BoundTicket(pass.pickupSlot.Item));Assert.IsTrue(day.Tables[0].Deliver(pass.pickupSlot.Item));
                var plate=day.Tables[0].order.tableSlot.Release();plate.Configure(ItemPayload.Plate());Assert.IsTrue(plates.ReturnCleanPlate(plate));day.Tables[0].Depart();Tick(5);
            }
        }
        [Test] public void HoldBeforePickupStopsWorkAndHoldDuringCookingKeepsDish()
        {
            StartCook();Stock(Fries);var ticket=Seat(Fries);Tick(200);Assert.IsNull(day.Cook.Hands.Item);Assert.IsNull(pass.pickupSlot.Item);
            day.Expo.TryFire(ticket);day.Cook.Advance(.5f);day.Expo.TryHold(ticket);Tick(100);Assert.IsNull(day.Cook.Hands.Item);
            day.Expo.TryFire(ticket);for(int i=0;i<1000 && !Object.FindObjectsByType<ProcessingStation>().Any(p=>p.gameObject.scene==scene && p.Busy);i++)day.Cook.Advance(.1f);
            Assert.IsTrue(Object.FindObjectsByType<ProcessingStation>().Any(p=>p.gameObject.scene==scene && p.Busy));day.Expo.TryHold(ticket);Tick();
            Assert.IsNotNull(pass.pickupSlot.Item,day.Cook.Status);Assert.IsTrue(day.Tables[0].Deliver(pass.pickupSlot.Item));
        }
        [Test] public void PlayerCookingAndHeldFinishedFoodPreventDuplicateWork()
        {
            StartCook();var raw=Stock(Fries);chef.Hands.TryTake(raw);var fryer=Object.FindObjectsByType<ProcessingStation>().First(p=>p.gameObject.scene==scene && p.ProcessFor(raw.Payload)==KitchenCook.HotStep(Fries));
            Assert.IsTrue(fryer.Interact(chef));Stock(Fries);var ticket=Seat(Fries);day.Expo.TryFire(ticket);day.Cook.Advance(1);Assert.IsNull(day.Cook.Hands.Item);
            fryer.Advance(10);Assert.IsTrue(fryer.Interact(chef));Tick(300);Assert.IsNull(day.Cook.Hands.Item);Assert.IsNull(pass.pickupSlot.Item);
        }
        [Test] public void NoPlatesAndBlockedPassRecoverWithoutLosingFood()
        {
            StartCook();var held=new System.Collections.Generic.List<Carryable>();while(plates.CleanPlatesRemaining>0)held.Add(plates.TakeCleanPlate());
            Stock(Fries);var ticket=Seat(Fries);day.Expo.TryFire(ticket);Tick();Assert.IsNotNull(day.Cook.Hands.Item);Assert.IsFalse(day.Cook.Hands.Item.Payload.isPlate);StringAssert.Contains("plates",day.Cook.Status);
            pass.pickupSlot.TryTake(held[0]);Assert.IsTrue(plates.ReturnCleanPlate(held[1]));Tick();Assert.IsTrue(day.Cook.Hands.Item.Payload.isPlate);Assert.AreSame(held[0],pass.pickupSlot.Item);
            Assert.IsTrue(plates.ReturnCleanPlate(held[0]));Tick();Assert.IsNotNull(pass.pickupSlot.Item);Assert.IsTrue(Fries.Matches(pass.pickupSlot.Item.Payload));
        }
        [Test] public void PlayerStartingOrderDuringCollectionPreventsSecondPortionCooking()
        {
            StartCook();Stock(Fries);var ticket=Seat(Fries);day.Expo.TryFire(ticket);
            for(int i=0;i<500 && day.Cook.Hands.Item==null;i++)day.Cook.Advance(.1f);
            var retained=day.Cook.Hands.Item;Assert.IsNotNull(retained);
            var human=Stock(Fries);chef.Hands.TryTake(human);var fryer=Object.FindObjectsByType<ProcessingStation>().First(p=>p.gameObject.scene==scene && p.ProcessFor(human.Payload)==KitchenCook.HotStep(Fries));Assert.IsTrue(fryer.Interact(chef));
            for(int i=0;i<600;i++)day.Cook.Advance(.1f);
            Assert.AreEqual(FoodState.Cut,retained.Payload.state);Assert.AreNotSame(fryer.slot,retained.Owner);Assert.AreSame(human,fryer.slot.Item);
        }
        [Test] public void PrepCookCookServerBusserAndDishwasherCompleteOnePhysicalChain()
        {
            var a=RestaurantAccounts.Current;foreach(var role in new[]{day.Settings.prepCookRole,day.Settings.serverRole,day.Settings.busserRole,day.Settings.dishwasherRole})Assert.IsTrue(a.Buy(role.id,role.hireCost));
            StartCook();var ticket=Seat(Fries);day.Expo.TryFire(ticket);
            var server=day.GetComponent<DiningServer>();var busser=day.GetComponent<DiningBusser>();var washer=day.GetComponent<KitchenDishwasher>();
            void Step(){foreach(var p in Object.FindObjectsByType<ProcessingStation>().Where(s=>s.gameObject.scene==scene))p.Advance(.1f);day.PrepCook.Advance(.1f);day.Cook.Advance(.1f);server.Advance(.1f);busser.Advance(.1f);washer.Advance(.1f);}
            for(int i=0;i<4000 && ticket.State!=KitchenTicketState.Served;i++)Step();
            Assert.AreEqual(KitchenTicketState.Served,ticket.State,day.PrepCook.Status+" / "+day.Cook.Status);Assert.AreEqual(4,plates.CleanPlatesRemaining);
            day.Tables[0].order.Advance(10);day.Tables[0].BeginDeparture();day.Tables[0].Depart();
            for(int i=0;i<4000 && plates.CleanPlatesRemaining<5;i++)Step();Assert.AreEqual(5,plates.CleanPlatesRemaining);Assert.IsNull(day.Tables[0].order.tableSlot.Item);
        }
        Carryable Portion(IngredientDefinition ingredient,FoodState state)
        {
            var source=Object.FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==scene && s.Offers(ingredient));
            var item=Object.Instantiate(source.itemPrefab);item.Configure(new ItemPayload{ingredient=ingredient,state=state});
            var counter=Object.FindObjectsByType<CounterStation>().First(c=>c.gameObject.scene==scene && c.GetType()==typeof(CounterStation) && c.slot.Item==null);Assert.IsTrue(counter.slot.TryTake(item));return item;
        }
        void StockParts(RecipeDefinition recipe)
        {
            foreach(var part in CookProduction.Portions(recipe))
            {
                var hot=CookProduction.HotFor(recipe,part.ingredient,part.state);
                if(hot==null || hot.input!=FoodState.Raw)Portion(part.ingredient,hot?.input??part.state);
            }
        }
        RecipeDefinition ChickenFries=>DailyMenu.Catalog.Single(r=>r.id=="chicken-fries" || r.displayName=="Chicken & Fries");
        void ReturnServedPlate(int index=0)
        {
            var plate=day.Tables[index].order.tableSlot.Release();plate.Configure(ItemPayload.Plate());Assert.IsTrue(plates.ReturnCleanPlate(plate));day.Tables[index].Depart();Tick(5);
        }
        [Test] public void EntireMenuIncludingCompoundAndColdDishesAssemblesFromPhysicalPortions()
        {
            StartCook();foreach(var recipe in DailyMenu.Catalog)
            {
                StockParts(recipe);var ticket=Seat(recipe);Assert.IsTrue(day.Expo.TryFire(ticket));Tick(3000);
                Assert.IsNotNull(pass.pickupSlot.Item,recipe.displayName+": "+day.Cook.Status);Assert.IsTrue(recipe.Matches(pass.pickupSlot.Item.Payload),recipe.displayName);
                Assert.AreSame(ticket,day.Expo.BoundTicket(pass.pickupSlot.Item));Assert.AreEqual(4,plates.CleanPlatesRemaining);
                Assert.IsTrue(day.Tables[0].Deliver(pass.pickupSlot.Item));ReturnServedPlate();
            }
        }
        [Test] public void CompoundHoldFinishesCurrentComponentButDoesNotStartNext()
        {
            StartCook();StockParts(ChickenFries);var ticket=Seat(ChickenFries);day.Expo.TryFire(ticket);
            var grill=Object.FindObjectsByType<ProcessingStation>().First(s=>s.gameObject.scene==scene && s.ProcessFor(ItemPayload.Food(ChickenFries.ingredient))!=null);
            for(int i=0;i<1000 && !grill.Busy;i++)day.Cook.Advance(.1f);Assert.IsTrue(grill.Busy);day.Expo.TryHold(ticket);Tick(1500);
            Assert.IsNull(pass.pickupSlot.Item);Assert.AreEqual(5,plates.CleanPlatesRemaining);
            Assert.IsTrue(Object.FindObjectsByType<Carryable>().Any(i=>i.gameObject.scene==scene && CookProduction.Plain(i,ChickenFries.ingredient,ChickenFries.requiredState)));
            Assert.IsFalse(Object.FindObjectsByType<Carryable>().Any(i=>i.gameObject.scene==scene && CookProduction.Plain(i,Fries.ingredient,FoodState.Cooked)));
            day.Expo.TryFire(ticket);Tick(2500);Assert.IsTrue(ChickenFries.Matches(pass.pickupSlot.Item?.Payload),day.Cook.Status);
        }
        [Test] public void PlayerMovingPartialPlatePausesAssemblyAndResumesWithoutAnotherPlate()
        {
            StartCook();var ticket=Seat(ChickenFries);day.Expo.TryFire(ticket);
            Tick(1200);var partial=CookProduction.Snapshot(day).Single(o=>o.Ticket==ticket).Plate;Assert.IsNotNull(partial,day.Cook.Status);Assert.AreEqual(1,partial.Payload.IngredientCount);
            var counter=(CounterStation)CookProduction.Location(partial);chef.Hands.TryTake(partial);StockParts(ChickenFries);Tick(600);
            Assert.IsNull(pass.pickupSlot.Item);Assert.AreEqual(4,plates.CleanPlatesRemaining);Assert.AreSame(partial,chef.Hands.Item);StringAssert.Contains("player",day.Cook.Status);
            var free=Object.FindObjectsByType<CounterStation>().First(c=>c.gameObject.scene==scene && c.GetType()==typeof(CounterStation) && c.slot.Item==null);
            Assert.IsTrue(free.slot.TryTake(partial));Tick(2500);Assert.AreSame(partial,pass.pickupSlot.Item,day.Cook.Status);Assert.IsTrue(ChickenFries.Matches(partial.Payload));
        }
        [Test] public void AllocationCountsSharedPortionsAndWholePlatesOnlyOnce()
        {
            StartCook();var first=Seat(ChickenFries);var second=Seat(Fries,1);day.Expo.TryFire(first);day.Expo.TryFire(second);
            var shared=Portion(Fries.ingredient,FoodState.Cooked);var snapshot=CookProduction.Snapshot(day);
            Assert.AreEqual(1,snapshot.SelectMany(o=>o.Components).Count(c=>c.Supply==shared));
            Assert.AreSame(shared,snapshot.Single(o=>o.Ticket==first).Components.Single(c=>c.Ingredient==Fries.ingredient).Supply);
            var plate=plates.TakeCleanPlate();plate.Payload.AddFood(shared.Payload);plate.Payload.AddFood(new ItemPayload{ingredient=ChickenFries.ingredient,state=ChickenFries.requiredState});
            var slot=shared.Owner;slot.Release();Object.DestroyImmediate(shared.gameObject);slot.TryTake(plate);snapshot=CookProduction.Snapshot(day);
            Assert.AreSame(plate,snapshot.Single(o=>o.Ticket==first).Plate);Assert.IsNull(snapshot.Single(o=>o.Ticket==second).Plate);Assert.IsNull(snapshot.Single(o=>o.Ticket==second).Components.Single().Supply);
        }
        [Test] public void PlayerCookingCompoundComponentIsReusedWithoutDuplicate()
        {
            StartCook();var potato=Portion(Fries.ingredient,FoodState.Cut);chef.Hands.TryTake(potato);
            var fryer=Object.FindObjectsByType<ProcessingStation>().First(p=>p.gameObject.scene==scene && p.ProcessFor(potato.Payload)==Hot());
            ProcessingRecipe Hot()=>KitchenCook.HotStep(Fries);
            Assert.IsTrue(fryer.Interact(chef));var ticket=Seat(ChickenFries);day.Expo.TryFire(ticket);Tick(2500);
            Assert.IsTrue(ChickenFries.Matches(pass.pickupSlot.Item?.Payload),day.Cook.Status);
            Assert.AreEqual(1,Object.FindObjectsByType<Carryable>().Count(i=>i.gameObject.scene==scene && i.Owner!=null && i.Payload.Contains(Fries.ingredient,FoodState.Cooked)));
            Assert.IsNull(fryer.slot.Item);
        }
        [Test] public void CancelledCompoundOrderRetainsFoodAndDoesNotStageIncompleteDish()
        {
            StartCook();var ticket=Seat(ChickenFries);day.Expo.TryFire(ticket);Tick(1200);
            var partial=CookProduction.Snapshot(day).Single(o=>o.Ticket==ticket).Plate;Assert.IsNotNull(partial);day.Tables[0].BeginDeparture();Tick(800);
            Assert.IsNull(pass.pickupSlot.Item);Assert.IsNotNull(partial.Owner);Assert.AreEqual(1,partial.Payload.IngredientCount);Assert.AreEqual(4,plates.CleanPlatesRemaining);
        }
        [Test] public void PrepAndCookCompleteThreeComponentOmeletWithoutManualStock()
        {
            Assert.IsTrue(RestaurantAccounts.Current.Buy(day.Settings.prepCookRole.id,day.Settings.prepCookRole.hireCost));StartCook();
            var recipe=DailyMenu.Catalog.Single(r=>r.displayName=="Garden Omelet");var ticket=Seat(recipe);day.Expo.TryFire(ticket);
            for(int i=0;i<5000 && pass.pickupSlot.Item==null;i++){day.PrepCook.Advance(.1f);Tick(1);}
            Assert.IsTrue(recipe.Matches(pass.pickupSlot.Item?.Payload),day.PrepCook.Status+" / "+day.Cook.Status);Assert.AreEqual(4,plates.CleanPlatesRemaining);
        }
        [Test] public void PlayerCompletingCompoundDishDuringCollectionStopsNewCooking()
        {
            StartCook();StockParts(ChickenFries);var ticket=Seat(ChickenFries);day.Expo.TryFire(ticket);
            for(int i=0;i<1000 && day.Cook.Hands.Item==null;i++)day.Cook.Advance(.1f);
            var input=day.Cook.Hands.Item;Assert.IsNotNull(input);
            var plate=plates.TakeCleanPlate();foreach(var p in CookProduction.Portions(ChickenFries))plate.Payload.AddFood(new ItemPayload{ingredient=p.ingredient,state=p.state});chef.Hands.TryTake(plate);
            Tick(1000);Assert.AreEqual(FoodState.Raw,input.Payload.state);Assert.IsNotNull(input.Owner);Assert.IsNull(pass.pickupSlot.Item);Assert.AreSame(plate,chef.Hands.Item);
        }
        [Test] public void MissingPreparedIngredientStatusIdentifiesWhatPlayerMustSupply()
        {
            StartCook();var ticket=Seat(ChickenFries);day.Expo.TryFire(ticket);Tick(1200);
            StringAssert.Contains(Fries.ingredient.NameFor(FoodState.Cut),day.Cook.Status);Assert.IsNull(pass.pickupSlot.Item);
            StockParts(ChickenFries);Tick(2500);Assert.IsTrue(ChickenFries.Matches(pass.pickupSlot.Item?.Payload),day.Cook.Status);
        }
        [Test] public void CookingInputCannotAlsoSupplyColdRecipe()
        {
            StartCook();var hot=DailyMenu.Catalog.Single(r=>r.displayName=="Grilled Tomato");var cold=DailyMenu.Catalog.First(r=>r.HasPortion(hot.ingredient,FoodState.Cut) && r.steps.All(s=>s.output==FoodState.Cut));
            var coldTicket=Seat(cold);var hotTicket=Seat(hot,1);day.Expo.TryFire(coldTicket);day.Expo.TryFire(hotTicket);
            var input=Portion(hot.ingredient,FoodState.Cut);chef.Hands.TryTake(input);var grill=Object.FindObjectsByType<ProcessingStation>().First(p=>p.gameObject.scene==scene && p.ProcessFor(input.Payload)==KitchenCook.HotStep(hot));Assert.IsTrue(grill.Interact(chef));
            var plans=CookProduction.Snapshot(day);Assert.IsNull(plans.Single(p=>p.Ticket==coldTicket).Components.Single(c=>c.Ingredient==hot.ingredient).Supply,"Cooking input is not usable chopped stock");
            Assert.AreSame(input,plans.Single(p=>p.Ticket==hotTicket).Components.Single().Supply);Assert.AreSame(grill.slot,input.Owner);
        }
        [TestCase(false)] [TestCase(true)] public void RefireRecoversFinishedComponentParkedWithEveryCounterFull(bool playerAlreadyCompleted)
        {
            StartCook();var ticket=Seat(ChickenFries);day.Expo.TryFire(ticket);
            for(int i=0;i<1000 && !CookProduction.Plain(day.Cook.Hands.Item,ChickenFries.ingredient,ChickenFries.requiredState);i++)Tick(1);
            var retained=day.Cook.Hands.Item;Assert.IsNotNull(retained);Assert.IsFalse(retained.Payload.isPlate);
            StockParts(ChickenFries);var mushroom=DailyMenu.Catalog.Select(r=>r.ingredient).First(i=>i.visualKind==IngredientVisualKind.Mushroom);Portion(mushroom,FoodState.Cut);
            Assert.IsTrue(day.Expo.TryHold(ticket));Tick(300);Assert.AreSame(retained,day.Cook.Hands.Item);Assert.AreEqual(5,plates.CleanPlatesRemaining);StringAssert.Contains("free counter",day.Cook.Status);
            if(playerAlreadyCompleted)
            {
                var plate=plates.TakeCleanPlate();foreach(var p in CookProduction.Portions(ChickenFries))plate.Payload.AddFood(new ItemPayload{ingredient=p.ingredient,state=p.state});chef.Hands.TryTake(plate);
                Assert.IsTrue(day.Expo.TryFire(ticket));Tick(500);Assert.AreSame(retained,day.Cook.Hands.Item);Assert.AreSame(plate,chef.Hands.Item);Assert.IsNull(pass.pickupSlot.Item);Assert.AreEqual(4,plates.CleanPlatesRemaining);return;
            }
            Assert.IsTrue(day.Expo.TryFire(ticket));Tick(3000);
            Assert.IsTrue(ChickenFries.Matches(pass.pickupSlot.Item?.Payload),day.Cook.Status);Assert.AreEqual(4,plates.CleanPlatesRemaining);
        }
        [Test] public void CountersFilledDuringCookingExchangePlateForNextComponentWithoutDeadlock()
        {
            StartCook();var recipe=DailyMenu.Catalog.Single(r=>r.displayName=="Garden Omelet");var ticket=Seat(recipe);day.Expo.TryFire(ticket);
            for(int i=0;i<1000 && day.Cook.Hands.Item?.Payload.isPlate!=true;i++)Tick(1);
            var plate=day.Cook.Hands.Item;Assert.IsNotNull(plate);Assert.IsTrue(plate.Payload.isPlate);
            StockParts(recipe);Assert.IsFalse(Object.FindObjectsByType<CounterStation>().Any(c=>c.gameObject.scene==scene && c.GetType()==typeof(CounterStation) && c.slot.Item==null));
            Tick(3500);Assert.AreSame(plate,pass.pickupSlot.Item,day.Cook.Status);Assert.IsTrue(recipe.Matches(plate.Payload));Assert.AreEqual(4,plates.CleanPlatesRemaining);
        }
    }
}
