using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace ThrownTogether.Tests
{
    public sealed class ClarityAndPracticeTests
    {
        private Scene original, scene;
        private GameObject[] suspended;
        private ChefController chef;
        private RestaurantHud hud;
        [UnitySetUp] public IEnumerator Load()
        {
            SessionOptions.Training="Free practice"; SessionOptions.ShiftOrders=6; SessionOptions.Kitchen=0;
            original=SceneManager.GetActiveScene();
            suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)
                ? original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray() : new GameObject[0];
            foreach(var root in suspended) root.SetActive(false);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1); SceneManager.SetActiveScene(scene);
            yield return null;
            hud=Object.FindObjectsByType<RestaurantHud>().Single(h=>h.gameObject.scene==scene);
            chef=hud.chef; chef.GetComponent<ChefInput>().enabled=false;
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            SessionOptions.Training="Free practice"; SessionOptions.ShiftOrders=6; SessionOptions.Kitchen=0;
            if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            SceneManager.SetActiveScene(original); foreach(var root in suspended) if(root!=null) root.SetActive(true);
        }
        [UnityTest] public IEnumerator EveryLayoutHasConnectedSpawnsAndReachableStations()
        {
            var layout=hud.GetComponent<KitchenLayout>();
            var motor=chef.GetComponent<CharacterController>();
            var cameraPosition=hud.gameplayCamera.transform.position;float cameraSize=hud.gameplayCamera.orthographicSize;
            for(int index=0;index<3;index++)
            {
                Assert.That(layout.Apply(index),Is.True);motor.enabled=false;Physics.SyncTransforms();
                const float cell=.25f; const int width=65,height=49;
                var free=new bool[width,height];var visited=new bool[width,height];
                for(int x=0;x<width;x++) for(int z=0;z<height;z++)
                {
                    var p=new Vector3(-8+x*cell,.4f,-5+z*cell);
                    free[x,z]=!Physics.CheckCapsule(p,p+Vector3.up*1.1f,.32f,~0,QueryTriggerInteraction.Ignore);
                }
                var start=new Vector2Int(Mathf.RoundToInt((chef.transform.position.x+8)/cell),Mathf.RoundToInt((chef.transform.position.z+5)/cell));
                Assert.That(free[start.x,start.y],Is.True,"P1 spawn "+index);
                var queue=new System.Collections.Generic.Queue<Vector2Int>();queue.Enqueue(start);visited[start.x,start.y]=true;
                var directions=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
                while(queue.Count>0)
                {
                    var current=queue.Dequeue();
                    foreach(var direction in directions) {var n=current+direction;if(n.x<0||n.y<0||n.x>=width||n.y>=height||visited[n.x,n.y]||!free[n.x,n.y])continue;visited[n.x,n.y]=true;queue.Enqueue(n);}
                }
                var second=layout.secondSpawn.position;
                Assert.That(visited[Mathf.RoundToInt((second.x+8)/cell),Mathf.RoundToInt((second.z+5)/cell)],Is.True,"P2 spawn connected "+index);
                foreach(var anchor in layout.anchors)
                {
                    bool reachable=false;
                    for(int x=0;x<width&&!reachable;x++) for(int z=0;z<height;z++)
                        if(visited[x,z] && Vector2.Distance(new Vector2(-8+x*cell,-5+z*cell),new Vector2(anchor.position.x,anchor.position.z))<1.8f) {reachable=true;break;}
                    Assert.That(reachable,Is.True,layout.CurrentName+": "+anchor.name);
                }
                Assert.That(chef.speed,Is.EqualTo(4.2f));Assert.That(chef.reach,Is.EqualTo(2));
                Assert.That(hud.gameplayCamera.transform.position,Is.EqualTo(cameraPosition));Assert.That(hud.gameplayCamera.orthographicSize,Is.EqualTo(cameraSize));
                yield return null;
            }
            Assert.That(layout.Apply(-1),Is.False);Assert.That(layout.Apply(99),Is.False);
            layout.Apply(0);motor.enabled=true;
        }
        [UnityTest] public IEnumerator ReconnectingPlayerTwoKeepsChefFoodAndReleaseGate()
        {
            var one=InputSystem.AddDevice<Gamepad>();var two=InputSystem.AddDevice<Gamepad>();Gamepad replacement=null;
            try
            {
                hud.coop.BindPlayerOne(one);Assert.That(hud.coop.Join(two),Is.True);var second=hud.coop.PlayerTwo;
                var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene&&!s.plates);
                Assert.That(source.Interact(second),Is.True);var item=second.Hands.Item;
                InputSystem.RemoveDevice(two);yield return null;
                Assert.That(hud.coop.ConnectionHelp,Does.Contain("P2 disconnected"));Assert.That(second.GetComponent<ChefInput>().enabled,Is.False);
                replacement=InputSystem.AddDevice<Gamepad>();Assert.That(hud.coop.Join(replacement),Is.True);
                Assert.That(hud.coop.PlayerTwo,Is.SameAs(second));Assert.That(second.Hands.Item,Is.SameAs(item));Assert.That(item.Owner,Is.SameAs(second.Hands));
                Assert.That(second.GetComponent<ChefInput>().AwaitUseRelease,Is.True);Assert.That(hud.coop.LeavePlayerTwo(),Is.False);
                InputSystem.RemoveDevice(one);Assert.That(hud.coop.ConnectionHelp,Does.Contain("P1 disconnected"));
            }
            finally {if(one.added)InputSystem.RemoveDevice(one);if(two.added)InputSystem.RemoveDevice(two);if(replacement!=null&&replacement.added)InputSystem.RemoveDevice(replacement);}
        }
        [UnityTest] public IEnumerator EveryOrdinaryCounterPlatesBothIngredientsInEitherOrder()
        {
            var sources=Interactable.Active.OfType<SourceStation>().Where(s=>s.gameObject.scene==scene).ToArray();
            var plateSource=sources.Single(s=>s.plates);
            foreach(var counter in Interactable.Active.OfType<CounterStation>().Where(c=>c.gameObject.scene==scene && !(c is ProcessingStation)))
            foreach(var source in sources.Where(s=>!s.plates))
            foreach(bool plateFirst in new[]{false,true})
            {
                Assert.That((plateFirst ? plateSource:source).Interact(chef),Is.True);
                if(!plateFirst) chef.Hands.Item.Payload.state=source.ingredient.platingState;
                Assert.That(counter.Interact(chef),Is.True);
                Assert.That((plateFirst ? source:plateSource).Interact(chef),Is.True);
                if(plateFirst) chef.Hands.Item.Payload.state=source.ingredient.platingState;
                Assert.That(counter.Interact(chef),Is.True);
                Assert.That(chef.Hands.Item,Is.Null);
                Assert.That(counter.slot.Item.Payload.isPlate,Is.True);
                Assert.That(counter.slot.Item.Payload.ingredient,Is.SameAs(source.ingredient));
                Assert.That(counter.slot.Item.Owner,Is.SameAs(counter.slot));
                Assert.That(counter.Interact(chef),Is.True);
                Object.Destroy(chef.Hands.Release().gameObject);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator RawAndCutFoodCannotBePlatedAndNoItemsAreLost()
        {
            var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene && !s.plates && s.ingredient.visualKind==IngredientVisualKind.Potato);
            var plates=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene && s.plates);
            var counter=Interactable.Active.OfType<CounterStation>().First(c=>c.gameObject.scene==scene && !(c is ProcessingStation));
            foreach(var state in new[]{FoodState.Raw,FoodState.Cut})
            {
                source.Interact(chef); var food=chef.Hands.Item; food.Payload.state=state; counter.Interact(chef);
                plates.Interact(chef); var plate=chef.Hands.Item;
                Assert.That(counter.Interact(chef),Is.False); Assert.That(counter.slot.Item,Is.SameAs(food)); Assert.That(chef.Hands.Item,Is.SameAs(plate));
                Object.Destroy(counter.slot.Release().gameObject); Object.Destroy(chef.Hands.Release().gameObject);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator TwoChefsCompeteForOnePlateWithoutDuplicatingIt()
        {
            var one=InputSystem.AddDevice<Gamepad>(); var two=InputSystem.AddDevice<Gamepad>();
            try
            {
                hud.coop.BindPlayerOne(one); Assert.That(hud.coop.Join(two),Is.True);
                var second=hud.coop.PlayerTwo; second.GetComponent<ChefInput>().enabled=false;
                var plates=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene && s.plates);
                var counter=Interactable.Active.OfType<CounterStation>().First(c=>c.gameObject.scene==scene && !(c is ProcessingStation));
                plates.Interact(chef); counter.Interact(chef); var item=counter.slot.Item;
                Assert.That(counter.Interact(chef),Is.True); Assert.That(counter.Interact(second),Is.False);
                Assert.That(chef.Hands.Item,Is.SameAs(item)); Assert.That(second.Hands.Item,Is.Null); Assert.That(counter.slot.Item,Is.Null);
                var menu=hud.GetComponent<RestaurantMenu>(); menu.Open();
                Assert.That(hud.coop.Join(two),Is.False,"Join cannot bypass a paused menu"); menu.Close();
            }
            finally { InputSystem.RemoveDevice(one); InputSystem.RemoveDevice(two); }
            yield return null;
        }
        [UnityTest] public IEnumerator ShiftLengthsUseExistingRecipesAndFinishWithoutExpiry()
        {
            var shift=hud.shift; var recipes=shift.definition.orders.ToArray();
            foreach(int count in new[]{3,6,12})
            {
                SessionOptions.ShiftOrders=count; shift.Begin(); Assert.That(shift.TotalOrders,Is.EqualTo(count));
                int safety=0;
                while(!shift.Complete && safety++<count+1)
                {
                    var seat=shift.seats.First(s=>s.Active && s.Phase==OrderPhase.Waiting);
                    Assert.That(recipes,Does.Contain(seat.recipe));
                    shift.Advance(1000); Assert.That(seat.Phase,Is.EqualTo(OrderPhase.Waiting),"No expiry/failure timer");
                    var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene && !s.plates && s.ingredient==seat.recipe.ingredient);
                    var dish=Object.Instantiate(source.itemPrefab); dish.Configure(new ItemPayload{isPlate=true,ingredient=source.ingredient,state=seat.recipe.requiredState});
                    Assert.That(seat.Reserve(dish.Payload),Is.True); seat.Receive(dish); seat.Advance(2); shift.Advance(0);
                }
                Assert.That(shift.Complete,Is.True); Assert.That(shift.CompletedCount,Is.EqualTo(count)); Assert.That(shift.seats.All(s=>!s.Active),Is.True);
            }
            Assert.That(shift.definition.orders,Is.EqualTo(recipes),"Session choices do not mutate shared data"); yield return null;
        }
        [UnityTest] public IEnumerator VisualDressingAddsNoCollidersAndDefaultPracticeIsOptional()
        {
            Assert.That(hud.GetComponent<KitchenPresentation>(),Is.Not.Null);
            Assert.That(hud.GetComponent<PracticeGuide>().Active,Is.False);
            var previews=Object.FindObjectsByType<Carryable>().Where(c=>c.gameObject.scene==scene && c.name=="Pantry ingredient display").ToArray();
            Assert.That(previews.Length,Is.EqualTo(12));
            foreach(var preview in previews) Assert.That(preview.GetComponentsInChildren<Collider>(),Is.Empty);
            foreach(var station in Interactable.Active.Where(s=>s.gameObject.scene==scene))
                Assert.That(station.transform.Find("P1 target").GetComponentsInChildren<Collider>(),Is.Empty);
            yield return null;
        }
        [UnityTest] public IEnumerator EveryPracticeStartingPointCanCompleteTheRealCookingLoop()
        {
            foreach(string training in new[]{"Guided full loop","Prep","Frying","Plating","Serving","Tomato salad"})
            {
                yield return SceneManager.UnloadSceneAsync(scene);
                SessionOptions.Training=training;
#if UNITY_EDITOR
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantDevelopment.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
                yield return SceneManager.LoadSceneAsync("RestaurantDevelopment",LoadSceneMode.Additive);
#endif
                scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1); SceneManager.SetActiveScene(scene); yield return null;
                hud=Object.FindObjectsByType<RestaurantHud>().Single(h=>h.gameObject.scene==scene); chef=hud.chef; chef.GetComponent<ChefInput>().enabled=false;
                var guide=hud.GetComponent<PracticeGuide>(); Assert.That(guide.Active,Is.True); Assert.That(guide.Instruction,Is.Not.Empty);
                var stations=Interactable.Active.Where(s=>s.gameObject.scene==scene).ToArray();
                var source=stations.OfType<SourceStation>().Single(s=>!s.plates && s.ingredient==hud.order.recipe.ingredient); var plates=stations.OfType<SourceStation>().Single(s=>s.plates);
                var prep=stations.OfType<ProcessingStation>().Single(s=>s.recipe.input==FoodState.Raw);
                var fryer=stations.OfType<ProcessingStation>().Single(s=>s.recipe.input==FoodState.Cut);
                var counter=stations.OfType<CounterStation>().First(s=>!(s is ProcessingStation));
                var service=stations.OfType<ServiceStation>().Single();
                if(training=="Guided full loop" || training=="Tomato salad") {Assert.That(chef.Hands.Item,Is.Null); Assert.That(source.Interact(chef),Is.True);}
                if(training=="Guided full loop" || training=="Prep" || training=="Tomato salad")
                {Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Raw)); Assert.That(prep.Interact(chef),Is.True); prep.Advance(1.5f); Assert.That(prep.Interact(chef),Is.True);}
                if(training=="Guided full loop" || training=="Prep" || training=="Frying")
                {Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Cut)); Assert.That(fryer.Interact(chef),Is.True); fryer.Advance(5); Assert.That(fryer.Interact(chef),Is.True);}
                if(training!="Serving")
                {
                    Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(hud.order.recipe.requiredState)); Assert.That(counter.Interact(chef),Is.True);
                    Assert.That(plates.Interact(chef),Is.True); Assert.That(counter.Interact(chef),Is.True);
                    yield return null; Assert.That(guide.Instruction,Does.Contain("Pick up your finished plate"));
                    Assert.That(counter.Interact(chef),Is.True);
                }
                Assert.That(chef.Hands.Item.Payload.isPlate,Is.True); Assert.That(service.Interact(chef),Is.True);
                yield return new WaitUntil(()=>hud.order.Phase==OrderPhase.Complete);
                yield return null; Assert.That(guide.Instruction,Does.Contain("complete"));
            }
        }
    }
}

