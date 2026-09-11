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
    public sealed class RestaurantDayTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public string Read()=>json;public void Write(string value)=>json=value;}
        Scene original,scene;GameObject[] suspended;RestaurantDay day;ChefController chef;Interactable[] stations;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;RestaurantAccounts.UseStorage(new Memory());SessionOptions.ShiftOrders=0;SessionOptions.Kitchen=0;
            original=SceneManager.GetActiveScene();suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];foreach(var root in suspended)root.SetActive(false);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;
            day=Object.FindObjectsByType<RestaurantDay>().Single(s=>s.gameObject.scene==scene);chef=Object.FindObjectsByType<ChefController>().Single(c=>c.gameObject.scene==scene);chef.GetComponent<ChefInput>().enabled=false;
            stations=Interactable.Active.Where(s=>s.gameObject.scene==scene).ToArray();
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);
            foreach(var root in suspended)if(root!=null)root.SetActive(true);RestaurantAccounts.ResetCache();SessionOptions.ShiftOrders=6;SessionOptions.Kitchen=0;
        }
        void Use(Interactable station){KitchenTestAccess.Approach(chef,station);Assert.That(chef.Use(),Is.True,station.name);}
        Carryable CookFirstDish()=>CookDish(day.Tables.First(t=>t.order.Active).order.recipe);
        Carryable CookDish(RecipeDefinition recipe)
        {
            Use(stations.OfType<SourceStation>().Single(s=>!s.plates && s.ingredient==recipe.ingredient));
            var prep=stations.OfType<ProcessingStation>().Single(s=>s.requiresAttendance);Use(prep);prep.Advance(2);Use(prep);
            var fryer=stations.OfType<ProcessingStation>().Single(s=>!s.requiresAttendance);Use(fryer);fryer.Advance(5);Use(fryer);
            var counter=stations.OfType<CounterStation>().First(s=>s.GetType()==typeof(CounterStation));Use(counter);Use(stations.OfType<SourceStation>().Single(s=>s.plates));Use(counter);Use(counter);return chef.Hands.Item;
        }
        void WalkTo(float x,float z)
        {
            var target=new Vector3(x,0,z);
            for(int i=0;i<500;i++)
            {
                var delta=target-chef.transform.position;delta.y=0;if(delta.magnitude<.04f)return;
                chef.Move(new Vector2(delta.x,delta.z).normalized,Mathf.Min(.02f,delta.magnitude/chef.speed));
            }
            Assert.Fail("Chef could not walk to "+target+"; stopped at "+chef.transform.position);
        }
        [UnityTest] public IEnumerator WalkThroughDiningDoorAndServeBothVisibleTablesThenClearTheirPlates()
        {
            day.Advance(45);
            foreach(var table in day.Tables)
            {
                // The destination comes from the real plate slot, never the interaction component.
                // This catches order-logic roots incorrectly registered as targets at (0,0,0).
                var surface=table.order.tableSlot.transform.position;
                var dish=CookDish(table.order.recipe);
                var motor=chef.GetComponent<CharacterController>();motor.enabled=false;
                chef.transform.position=new Vector3(0,.04f,-3.35f);motor.enabled=true;Physics.SyncTransforms();
                WalkTo(3.6f,-3.35f);WalkTo(day.Settings.diningAisleX,-3.35f);WalkTo(day.Settings.diningAisleX,surface.z);
                chef.Move(Vector2.right,0);chef.FindFocus();
                Assert.That(chef.Focus,Is.SameAs(table),"Visible table must be targetable from its dining-side approach");
                Assert.That(chef.Use(),Is.True);Assert.That(table.order.tableSlot.Item,Is.SameAs(dish));Assert.That(table.order.Phase,Is.EqualTo(OrderPhase.Eating));
                table.order.Advance(8);day.Advance(18);chef.FindFocus();Assert.That(chef.Use(),Is.True,"Clear the dirty plate from this same visible table");
                Assert.That(chef.Hands.Item,Is.SameAs(dish));Assert.That(dish.Payload.dirty,Is.True);
                Use(stations.OfType<DishReturnStation>().Single());
            }
            Assert.That(day.Served,Is.EqualTo(2));LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator ManualMealLeavesRealDirtyPlateAndWashingRestoresStock()
        {
            Assert.That(day.Tables.All(t=>t.Clean),Is.True);day.Advance(18);var table=day.Tables.First(t=>t.order.Active);
            var dish=CookFirstDish();Use(table);Assert.That(table.order.tableSlot.Item,Is.SameAs(dish));Assert.That(day.Served,Is.EqualTo(1));Assert.That(RestaurantAccounts.Current.Data.cash,Is.Zero);
            table.order.Advance(8);day.Advance(18);Assert.That(table.Clean,Is.False);Assert.That(dish.Payload.dirty,Is.True);Assert.That(stations.OfType<DishReturnStation>().Single().Count,Is.Zero);
            Use(table);Assert.That(chef.Hands.Item,Is.SameAs(dish));Assert.That(table.Clean,Is.True);
            Use(stations.OfType<DishReturnStation>().Single());Use(stations.OfType<DishReturnStation>().Single());
            var sink=stations.OfType<WashingStation>().Single();Use(sink);sink.Advance(3);Use(sink);Use(stations.OfType<SourceStation>().Single(s=>s.plates));
            Assert.That(stations.OfType<SourceStation>().Single(s=>s.plates).CleanPlatesRemaining,Is.EqualTo(5));
            day.Advance(300);Assert.That(day.Clock,Is.EqualTo("10:00 PM"));Assert.That(day.Paid,Is.True);int cash=RestaurantAccounts.Current.Data.cash;day.Advance(300);day.RetryPayment();Assert.That(RestaurantAccounts.Current.Data.cash,Is.EqualTo(cash));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator UnservedGuestsAndOutsideQueueEventuallyLeave()
        {
            day.Advance(150);Assert.That(day.LostCustomers,Is.GreaterThan(0));Assert.That(day.Served,Is.Zero);
            day.Advance(150);Assert.That(day.Closed,Is.True);Assert.That(RestaurantAccounts.Current.Data.cash,Is.Zero);Assert.That(day.WaitingOutside,Is.LessThanOrEqualTo(3));yield return null;
        }
        [UnityTest] public IEnumerator PassDoesNotTeleportFoodAndServerMovesMatchingDishToTable()
        {
            day.Advance(18);var dish=CookFirstDish();var pass=stations.OfType<ServiceStation>().Single();Use(pass);
            Assert.That(day.Served,Is.Zero);Assert.That(pass.pickupSlot.Item,Is.SameAs(dish));yield return new WaitForSeconds(2);Assert.That(day.Served,Is.Zero);
            var server=day.gameObject.AddComponent<DiningServer>();server.Initialize(day,pass);for(int i=0;i<200 && day.Served==0;i++)server.Advance(.1f);
            Assert.That(day.Served,Is.EqualTo(1));Assert.That(day.Tables.Any(t=>t.order.tableSlot.Item==dish),Is.True);Assert.That(pass.pickupSlot.Item,Is.Null);yield return null;
            var delivered=day.Tables.Single(t=>t.order.tableSlot.Item==dish);
            var distance=delivered.order.tableSlot.transform.position-server.transform.Find("Hired server").position;distance.y=0;
            Assert.That(distance.magnitude,Is.LessThanOrEqualTo(chef.reach),"Server must reach the visible table before delivering");
        }
        [UnityTest] public IEnumerator WrongFoodCannotCompleteMealAndBonusFallsWithWait()
        {
            day.Advance(18);var table=day.Tables.First(t=>t.order.Active);var source=stations.OfType<SourceStation>().First(s=>!s.plates);Use(source);
            Assert.That(table.Interact(chef),Is.False);Assert.That(day.Served,Is.Zero);
            day.RecordMeal(table.order.recipe,0);int quick=day.Bonuses;day.RecordMeal(table.order.recipe,100);Assert.That(day.Bonuses,Is.EqualTo(quick));Assert.That(quick,Is.EqualTo(5));yield return null;
        }
        [UnityTest] public IEnumerator PurchasedBaysHaveClearApproachesAndDoNotOverlapStationsOrSpawns()
        {
            var layout=Object.FindObjectsByType<KitchenLayout>().Single(s=>s.gameObject.scene==scene);
            var offers=day.Settings.purchases.Where(p=>p.stationPrefab!=null).ToArray();
            for(int index=0;index<layout.choices.Length;index++)
            {
                layout.Apply(index);var added=offers.Select(p=>Object.Instantiate(p.stationPrefab,p.layoutPositions[index],Quaternion.identity)).ToArray();Physics.SyncTransforms();
                try
                {
                    foreach(var bay in added)
                    {
                        var station=bay.GetComponent<Interactable>();KitchenTestAccess.Approach(chef,station);
                        foreach(var collider in bay.GetComponentsInChildren<Collider>())
                        foreach(var other in Object.FindObjectsByType<Collider>())
                        {
                            if(other.gameObject.scene!=scene||other.transform.IsChildOf(bay.transform)||other is CharacterController||other.isTrigger)continue;
                            if(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth))
                                Assert.That(depth,Is.LessThan(.025f),"Layout "+index+" "+bay.name+" overlaps "+other.name);
                        }
                    }
                    var motor=chef.GetComponent<CharacterController>();motor.enabled=false;
                    foreach(var spawn in new[]{layout.choices[index].playerOneSpawn,layout.choices[index].playerTwoSpawn,new Vector3(3.6f,0,-3.35f)})
                        Assert.That(Physics.CheckCapsule(spawn+Vector3.up*.4f,spawn+Vector3.up*1.5f,.32f),Is.False,"Layout "+index+" spawn/door obstruction "+spawn);
                    motor.enabled=true;
                }
                finally{foreach(var bay in added)Object.DestroyImmediate(bay);}
            }
            yield return null;
        }
        [UnityTest] public IEnumerator PaidImprovementsApplyOnNextDayIncludingServerAndFryerSpeed()
        {
            var account=RestaurantAccounts.Current;Assert.That(account.Settle(day.DayNumber,700,0),Is.True);
            foreach(var offer in day.Settings.purchases)Assert.That(account.Buy(offer.id,offer.cost),Is.True);
            Assert.That(account.Buy(day.Settings.serverRole.id,day.Settings.serverRole.hireCost),Is.True);
            Assert.That(account.Buy(day.Settings.dishwasherRole.id,day.Settings.dishwasherRole.hireCost),Is.True);
            SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;
            day=Object.FindObjectsByType<RestaurantDay>().Single(s=>s.gameObject.scene==scene);
            Assert.That(day.DayNumber,Is.EqualTo(2));Assert.That(account.Data.cash,Is.EqualTo(195));
            Assert.That(day.GetComponent<DiningServer>(),Is.Not.Null);
            Assert.That(day.GetComponent<KitchenDishwasher>(),Is.Not.Null);
            var fryers=Object.FindObjectsByType<ProcessingStation>().Where(s=>s.gameObject.scene==scene && !s.requiresAttendance).ToArray();
            Assert.That(fryers.Length,Is.EqualTo(2));Assert.That(fryers.All(f=>Mathf.Approximately(f.processingSpeed,1.25f)),Is.True);
            Assert.That(Object.FindObjectsByType<CounterStation>().Count(s=>s.gameObject.scene==scene && s.GetType()==typeof(CounterStation)),Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PausingStopsClockAndArrivalSchedule()
        {
            float time=day.Elapsed;Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);
            Assert.That(day.Elapsed,Is.EqualTo(time));Assert.That(day.Tables.All(t=>t.Clean),Is.True);Time.timeScale=1;
        }
        [UnityTest] public IEnumerator PlateStackCombinesInHandsWithoutBypassingPreparationOrStock()
        {
            var source=stations.OfType<SourceStation>().Single(s=>s.plates);
            var potato=stations.OfType<SourceStation>().Single(s=>s.ingredient!=null && s.ingredient.id=="ingredient.potato");
            Use(potato);var food=chef.Hands.Item;
            Assert.That(source.Interact(chef),Is.False);Assert.That(source.CleanPlatesRemaining,Is.EqualTo(5));
            food.Payload.state=FoodState.Cooked;food.RefreshVisual();Use(source);
            Assert.That(chef.Hands.Item.Payload.isPlate,Is.True);Assert.That(chef.Hands.Item.Payload.ingredient,Is.SameAs(potato.ingredient));
            Assert.That(source.CleanPlatesRemaining,Is.EqualTo(4));
            var samePlate=chef.Hands.Item;samePlate.Payload.MakeDirty();samePlate.Configure(ItemPayload.Plate());Use(source);
            Assert.That(source.CleanPlatesRemaining,Is.EqualTo(5));
            var issued=new System.Collections.Generic.List<Carryable>();for(int i=0;i<5;i++)issued.Add(source.TakeCleanPlate());
            Use(potato);chef.Hands.Item.Payload.state=FoodState.Cooked;
            var held=chef.Hands.Item;Assert.That(source.Interact(chef),Is.False);Assert.That(chef.Hands.Item,Is.SameAs(held));
            Assert.That(issued.Distinct().Count(),Is.EqualTo(5));Assert.That(issued,Does.Contain(samePlate));
            foreach(var plate in issued)Assert.That(source.ReturnCleanPlate(plate),Is.True);
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SinkQueuesAllFivePlatesAndPreservesManualAttendance()
        {
            var stock=stations.OfType<SourceStation>().Single(s=>s.plates);var sink=stations.OfType<WashingStation>().Single();
            var plates=new System.Collections.Generic.List<Carryable>();
            for(int i=0;i<5;i++){Use(stock);plates.Add(chef.Hands.Item);chef.Hands.Item.Payload.MakeDirty();Use(sink);}
            Assert.That(sink.Count,Is.EqualTo(5));Assert.That(stock.CleanPlatesRemaining,Is.Zero);
            chef.Move(Vector2.right,.01f);sink.Advance(10);Assert.That(sink.Busy,Is.True);
            for(int i=0;i<5;i++)
            {
                Use(sink);sink.Advance(3);Use(sink);Assert.That(chef.Hands.Item,Is.SameAs(plates[i]));
                Assert.That(chef.Hands.Item.Payload.EmptyPlate,Is.True);Use(stock);
            }
            Assert.That(sink.Count,Is.Zero);Assert.That(stock.CleanPlatesRemaining,Is.EqualTo(5));yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SeatedPatienceExpiresOnceWithoutPaymentAndTableCanBeReused()
        {
            day.Advance(18);var table=day.Tables.First(t=>t.WaitingForMeal);var recipe=table.order.recipe;
            float left=day.Settings.seatedPatience-(day.Elapsed-table.SeatedAt);
            day.Advance(left/2);Assert.That(table.PatienceRemaining,Is.InRange(.45f,.51f));
            table.ReservedForServer=true;day.Advance(left/2+.3f);
            Assert.That(table.Leaving,Is.True);Assert.That(table.ReservedForServer,Is.False);Assert.That(table.order.Active,Is.False);
            Assert.That(table.CanServe(new ItemPayload{isPlate=true,ingredient=recipe.ingredient,state=FoodState.Cooked}),Is.False);
            Assert.That(day.Served,Is.Zero);Assert.That(day.BaseIncome,Is.Zero);Assert.That(day.LostCustomers,Is.GreaterThan(0));
            day.Advance(20);Assert.That(table.Leaving && table.Occupied,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator DishwasherReturnsSameFivePlatesWithoutTakingOverHumanWork()
        {
            var stock=stations.OfType<SourceStation>().Single(s=>s.plates);var sink=stations.OfType<WashingStation>().Single();
            for(int i=0;i<5;i++){Use(stock);chef.Hands.Item.Payload.MakeDirty();Use(sink);}
            var worker=day.gameObject.AddComponent<KitchenDishwasher>();worker.Initialize(day);
            for(int i=0;i<40;i++)worker.Advance(.1f);
            Assert.That(sink.Busy,Is.True,"Employee must not steal the player's active washing job");
            chef.CancelWork();
            for(int i=0;i<1600 && stock.CleanPlatesRemaining<5;i++)worker.Advance(.1f);
            Assert.That(stock.CleanPlatesRemaining,Is.EqualTo(5));Assert.That(sink.Count,Is.Zero);
            Assert.That(day.Served,Is.Zero);yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DishwasherHasClearSinkToStockRoutesInEveryKitchen()
        {
            var layout=Object.FindObjectsByType<KitchenLayout>().Single(s=>s.gameObject.scene==scene);
            var stock=stations.OfType<SourceStation>().Single(s=>s.plates);var sink=stations.OfType<WashingStation>().Single();
            for(int index=0;index<layout.choices.Length;index++)
            {
                layout.Apply(index);Physics.SyncTransforms();var start=KitchenStaffRoute.Approach(sink.transform);Assert.That(start,Is.Not.Null);
                var route=KitchenStaffRoute.ToStation(start.Value,stock.transform);Assert.That(route,Is.Not.Null,"Kitchen "+index);
                foreach(var point in route)Assert.That(KitchenStaffRoute.Clear(point),Is.True);
                Assert.That(KitchenStaffRoute.ToStation(route.Last(),sink.transform),Is.Not.Null);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator DiningRouteClearsTheOpenDoorAndTableEdges()
        {
            Physics.SyncTransforms();
            for(float z=-5;z<=6.5f;z+=.15f)
            {
                var p=new Vector3(day.Settings.diningAisleX,0,z);
                Assert.That(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.3f,.32f),Is.False,"NPC body corridor blocked at "+p);
            }
            yield return null;
        }
    }
}
