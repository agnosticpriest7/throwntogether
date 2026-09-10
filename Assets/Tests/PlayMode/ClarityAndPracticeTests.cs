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
        [UnityTest] public IEnumerator ApprovedStationArtRemainsVisualOnlyAcrossAllLayouts()
        {
            var stations=Interactable.Active.Where(s=>s.gameObject.scene==scene).ToArray();
            foreach(var station in stations)
            {
                var art=station.GetComponent<StationArt>();Assert.That(art,Is.Not.Null,station.name);
                Assert.That(art.visual,Is.Not.Null);Assert.That(art.visual.GetComponentsInChildren<Collider>(),Is.Empty);
                Assert.That(station.transform.Find("Cabinet").GetComponent<Collider>().enabled,Is.True);
                Assert.That(station.transform.Find("Worktop").GetComponent<Collider>().enabled,Is.True);
                Assert.That(station.transform.Find("Cabinet").GetComponent<Renderer>().enabled,Is.False);
                Assert.That(station.transform.Find("Worktop").GetComponent<Renderer>().enabled,Is.False);
                Assert.That(station.transform.Find("Door seam"),Is.Null,"Legacy dressing must not duplicate authored doors.");
                foreach(var renderer in art.visual.GetComponentsInChildren<Renderer>())Assert.That(renderer.sharedMaterials.All(m=>m!=null && m.shader!=null),Is.True);
            }
            var layout=hud.GetComponent<KitchenLayout>();
            for(int i=0;i<layout.choices.Length;i++)
            {
                Assert.That(layout.Apply(i),Is.True);yield return null;
                foreach(var station in stations)
                {
                    Assert.That(station.GetComponent<StationArt>().visual.transform.position,Is.EqualTo(station.transform.position));
                    var point=hud.gameplayCamera.WorldToViewportPoint(station.transform.position+Vector3.up*1.3f);
                    Assert.That(point.x,Is.InRange(.05f,.95f));Assert.That(point.y,Is.InRange(.13f,.88f));
                }
            }
        }
        [UnityTest] public IEnumerator CombinedFloorArtPreservesExistingFloorBounds()
        {
            foreach(string name in new[]{"Kitchen","Dining"})
            {
                var roots=scene.GetRootGameObjects();
                var old=roots.Single(g=>g.name==name+" floor");
                var art=roots.Single(g=>g.name=="RestaurantShift"+name+"Floor art");
                Assert.That(art.GetComponentsInChildren<Collider>(),Is.Empty);
                Assert.That(old.GetComponent<Collider>().enabled,Is.True);Assert.That(old.GetComponent<Renderer>().enabled,Is.False);
                var physics=old.GetComponent<Collider>().bounds;var visual=art.GetComponent<Renderer>().bounds;
                Assert.That(visual.min.x,Is.EqualTo(physics.min.x).Within(.005f));Assert.That(visual.max.x,Is.EqualTo(physics.max.x).Within(.005f));
                Assert.That(visual.min.z,Is.EqualTo(physics.min.z).Within(.005f));Assert.That(visual.max.z,Is.EqualTo(physics.max.z).Within(.005f));
                Assert.That(art.GetComponent<MeshFilter>().sharedMesh.subMeshCount,Is.EqualTo(7));
            }
            yield return null;
        }
        [UnityTest] public IEnumerator FirstServiceIslandHasTwoChefPassingLanes()
        {
            var layout=hud.GetComponent<KitchenLayout>();Assert.That(layout.Apply(0),Is.True);
            var motor=chef.GetComponent<CharacterController>();motor.enabled=false;Physics.SyncTransforms();
            // Two separate 0.64m capsules fit beside one another on both sides of the island.
            // This checks collision clearance, not subjective controller comfort.
            foreach(float x in new[]{-3.8f,-3.0f,.9f,1.65f})
            for(float z=-2.8f;z<=2.8f;z+=.2f)
            {
                var p=new Vector3(x,.4f,z);
                Assert.That(Physics.CheckCapsule(p,p+Vector3.up*1.1f,.32f,~0,QueryTriggerInteraction.Ignore),Is.False,"Passing lane blocked at "+p);
            }
            motor.enabled=true;yield return null;
        }
        [UnityTest] public IEnumerator DiningAndCutawayArtPreservesTablesAndCustomerVisibility()
        {
            var room=scene.GetRootGameObjects().Single(g=>g.name=="Restaurant room art");
            Assert.That(room.GetComponentsInChildren<Collider>(),Is.Empty);
            Assert.That(room.GetComponentsInChildren<Transform>().Count(t=>t.name=="WindowWall"&&t.parent==room.transform),Is.EqualTo(3));
            var lights=room.GetComponentsInChildren<Light>();Assert.That(lights.Length,Is.EqualTo(2));
            foreach(var light in lights){Assert.That(light.shadows,Is.EqualTo(LightShadows.None));Assert.That(light.range,Is.LessThanOrEqualTo(4));}
            foreach(var order in Object.FindObjectsByType<CustomerOrder>().Where(o=>o.gameObject.scene==scene))
            {
                var table=order.tableSlot.transform.parent;
                var original=table.Find("Table");var art=table.Find("Dining table art");
                Assert.That(original.GetComponent<Renderer>().enabled,Is.False);
                Assert.That(original.GetComponent<Collider>().enabled,Is.True);
                var bounds=art.GetComponentInChildren<Renderer>().bounds;var physics=original.GetComponent<Collider>().bounds;
                Assert.That(bounds.size.x,Is.EqualTo(physics.size.x).Within(.01f));Assert.That(bounds.size.z,Is.EqualTo(physics.size.z).Within(.01f));Assert.That(bounds.max.y,Is.EqualTo(physics.max.y).Within(.01f));
                var chair=order.customerVisual.Find("Dining chair art");Assert.That(chair,Is.Not.Null);Assert.That(chair.GetComponentsInChildren<Collider>(),Is.Empty);
                Assert.That(order.customerVisual.Find("Chair").GetComponent<Renderer>().enabled,Is.False);
                var recipe=order.recipe;order.ResetOrder(null);Assert.That(chair.gameObject.activeInHierarchy,Is.False);order.ResetOrder(recipe);Assert.That(chair.gameObject.activeInHierarchy,Is.True);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator SeatedCustomersKeepChairsFixedAndPreserveOrderTiming()
        {
            var orders=Object.FindObjectsByType<CustomerOrder>().Where(o=>o.gameObject.scene==scene).ToArray();
            var source=Object.FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==scene&&!s.plates);
            Assert.That(orders.Select(o=>o.GetComponent<CustomerPresentation>().variant).Distinct().Count(),Is.EqualTo(orders.Length));
            foreach(var order in orders)
            {
                var presentation=order.GetComponent<CustomerPresentation>();var actor=presentation.seatedVisual;
                Assert.That(actor,Is.Not.Null);Assert.That(actor.enabled,Is.False);Assert.That(actor.usePlayerSelection,Is.False);
                Assert.That(actor.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(actor.GetComponentsInChildren<ChefController>(),Is.Empty);Assert.That(actor.GetComponentsInChildren<ChefInput>(),Is.Empty);
                var arm=actor.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="B_Arm_R");
                presentation.ApplyPose(OrderPhase.Eating,.1f,true);var rotation=arm.localRotation;presentation.ApplyPose(OrderPhase.Eating,8,true);Assert.That(arm.localRotation,Is.EqualTo(rotation),"Reduced effects has no rhythmic motion");
                presentation.ApplyPose(OrderPhase.Eating,0,false);var animatedStart=arm.localRotation;presentation.ApplyPose(OrderPhase.Eating,.31f,false);Assert.That(Quaternion.Angle(animatedStart,arm.localRotation),Is.GreaterThan(5),"Eating must animate the arm rig");
                var chair=order.customerVisual.Find("Dining chair art");var chairPosition=chair.position;var rootPosition=order.customerVisual.position;
                var dish=Object.Instantiate(source.itemPrefab);dish.Configure(new ItemPayload{isPlate=true,ingredient=order.recipe.ingredient,state=order.recipe.requiredState});
                Assert.That(order.Reserve(dish.Payload),Is.True);order.Receive(dish);order.Advance(.5f);presentation.ApplyPose(order.Phase,8,false);
                Assert.That(order.Phase,Is.EqualTo(OrderPhase.Eating));Assert.That(order.tableSlot.Item,Is.SameAs(dish));Assert.That(chair.position,Is.EqualTo(chairPosition));Assert.That(order.customerVisual.position,Is.EqualTo(rootPosition));
                order.Advance(1.5f);Assert.That(order.Phase,Is.EqualTo(OrderPhase.Complete));Assert.That(dish.Payload.dirty,Is.True);Assert.That(order.tableSlot.Item,Is.Null);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator FoodVisualRefreshPreservesOwnershipAndCompactFootprint()
        {
            var sources=Object.FindObjectsByType<SourceStation>().Where(s=>s.gameObject.scene==scene&&!s.plates).ToArray();
            var item=Object.Instantiate(sources[0].itemPrefab);Assert.That(item.plateMesh,Is.Not.Null);Assert.That(item.mushroomCapMesh,Is.Not.Null);
            chef.transform.rotation=Quaternion.identity;var anchor=chef.Hands.transform.localPosition;
            item.Configure(ItemPayload.Plate());Assert.That(chef.Hands.TryTake(item),Is.True);
            foreach(var source in sources)foreach(FoodState state in System.Enum.GetValues(typeof(FoodState)))
            {
                var payload=new ItemPayload{ingredient=source.ingredient,state=state};item.Configure(payload);
                Assert.That(item.Payload,Is.SameAs(payload));Assert.That(item.Owner,Is.SameAs(chef.Hands));Assert.That(chef.Hands.Item,Is.SameAs(item));
                Assert.That(item.transform.localPosition,Is.EqualTo(Vector3.zero));Assert.That(chef.Hands.transform.localPosition,Is.EqualTo(anchor));
                var renderers=item.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                Assert.That(bounds.size.x,Is.LessThan(1.01f));Assert.That(bounds.size.z,Is.LessThan(1.01f));
                Assert.That(item.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            item.Configure(ItemPayload.Plate());Assert.That(item.GetComponentsInChildren<MeshFilter>().Single().sharedMesh,Is.SameAs(item.plateMesh));
            item.Payload.MakeDirty();item.RefreshVisual();Assert.That(item.Payload.dirty,Is.True);Assert.That(item.Owner,Is.SameAs(chef.Hands));
            yield return null;
        }
        [UnityTest] public IEnumerator GardenSaladLooksIdenticalInEitherAssemblyOrder()
        {
            var sources=Object.FindObjectsByType<SourceStation>().Where(s=>s.gameObject.scene==scene&&!s.plates).ToArray();
            var lettuce=sources.Single(s=>s.ingredient.visualKind==IngredientVisualKind.Lettuce);var tomato=sources.Single(s=>s.ingredient.visualKind==IngredientVisualKind.Tomato);
            var a=Object.Instantiate(lettuce.itemPrefab);var b=Object.Instantiate(lettuce.itemPrefab);
            var first=ItemPayload.Plate();first.AddFood(new ItemPayload{ingredient=lettuce.ingredient,state=FoodState.Cut});first.AddFood(new ItemPayload{ingredient=tomato.ingredient,state=FoodState.Cut});
            var second=ItemPayload.Plate();second.AddFood(new ItemPayload{ingredient=tomato.ingredient,state=FoodState.Cut});second.AddFood(new ItemPayload{ingredient=lettuce.ingredient,state=FoodState.Cut});
            a.Configure(first);b.Configure(second);
            var ar=a.GetComponentsInChildren<MeshFilter>();var br=b.GetComponentsInChildren<MeshFilter>();Assert.That(ar.Length,Is.EqualTo(br.Length));
            for(int i=0;i<ar.Length;i++){Assert.That(ar[i].sharedMesh,Is.SameAs(br[i].sharedMesh));Assert.That(ar[i].transform.localPosition,Is.EqualTo(br[i].transform.localPosition));Assert.That(ar[i].transform.localScale,Is.EqualTo(br[i].transform.localScale));}
            var bounds=a.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in a.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
            Assert.That(bounds.size.y,Is.LessThan(.4f));Assert.That(bounds.size.x,Is.LessThan(1.01f));Assert.That(bounds.size.z,Is.LessThan(1.01f));
            Object.Destroy(a.gameObject);Object.Destroy(b.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator AuthoredServiceDisplayAvoidsDuplicateDressingAndKeepsFoodSpaceClear()
        {
            var stations=Object.FindObjectsByType<Interactable>().Where(s=>s.gameObject.scene==scene&&(s is ServiceStation||s is DishReturnStation)).ToArray();
            Assert.That(stations.Length,Is.EqualTo(2));
            foreach(var station in stations)
            {
                Assert.That(station.GetComponent<StationArt>().includesServiceDisplay,Is.True);
                var visual=station.transform.Find("Service presentation art");Assert.That(visual,Is.Not.Null);
                Assert.That(visual.GetComponentsInChildren<Collider>(),Is.Empty);
                Assert.That(visual.GetComponentsInChildren<Carryable>(),Is.Empty,"Decorations must not contain fake food");
                var bounds=visual.GetComponentInChildren<Renderer>().bounds;
                Assert.That(bounds.size.x,Is.LessThanOrEqualTo(1.91f));
                Assert.That(bounds.max.y-station.transform.position.y,Is.LessThan(1.60f),"No overhead shelf hiding the food");
                Assert.That(station.transform.Find("Pass shelf"),Is.Null);
                Assert.That(station.transform.Find("Service bell"),Is.Null);
                Assert.That(station.transform.Find("Return tray rim"),Is.Null);
                Assert.That(station.transform.Find("P1 target"),Is.Not.Null,"Authored dressing must preserve targeting feedback");
            }
            yield return null;
        }
        [UnityTest] public IEnumerator SuccessCuesRequireSuccessExpireAndRespectReducedEffects()
        {
            var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene&&!s.plates);
            var motor=chef.GetComponent<CharacterController>();motor.enabled=false;
            chef.transform.position=source.transform.position+Vector3.back*1.2f;
            chef.transform.rotation=Quaternion.identity;motor.enabled=true;
            bool reduced=RestaurantMenu.Display.reducedEffects;
            try
            {
                RestaurantMenu.Display.reducedEffects=true;
                Assert.That(chef.Use(),Is.True);Assert.That(chef.Focus,Is.SameAs(source));
                var held=chef.Hands.Item;
                Assert.That(source.SuccessOpacity,Is.EqualTo(1));Assert.That(source.SuccessCheck,Is.False);
                yield return new WaitForSeconds(.2f);
                Assert.That(source.SuccessOpacity,Is.EqualTo(1),"Reduced effects has no fade or motion");
                yield return new WaitForSeconds(.3f);
                Assert.That(source.SuccessOpacity,Is.Zero);
                Assert.That(chef.Use(),Is.False);Assert.That(source.SuccessOpacity,Is.Zero);
                Assert.That(chef.Hands.Item,Is.SameAs(held));
                source.ShowSuccess(true);Assert.That(source.SuccessCheck,Is.True);
                source.enabled=false;Assert.That(source.SuccessOpacity,Is.Zero);source.enabled=true;
            }
            finally { RestaurantMenu.Display.reducedEffects=reduced; }
        }
        [UnityTest] public IEnumerator ManualPrepStallsOnMovementAndCanResumeWithoutLosingProgress()
        {
            var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene&&!s.plates&&s.ingredient.visualKind==IngredientVisualKind.Potato);
            var prep=Interactable.Active.OfType<ProcessingStation>().Single(s=>s.gameObject.scene==scene&&s.requiresAttendance);
            var fryer=Interactable.Active.OfType<ProcessingStation>().Single(s=>s.gameObject.scene==scene&&!s.requiresAttendance);
            source.Interact(chef);Assert.That(prep.Interact(chef),Is.True);prep.Advance(.5f);float progress=prep.Progress;
            chef.Move(Vector2.right,.05f);prep.Advance(20);Assert.That(prep.Progress,Is.EqualTo(progress));Assert.That(prep.Working,Is.False);
            chef.Move(Vector2.zero,0);prep.Advance(20);Assert.That(prep.Progress,Is.EqualTo(progress),"Stopping alone does not restart an abandoned job");
            Assert.That(prep.Interact(chef),Is.True);prep.Advance(1);Assert.That(prep.Busy,Is.False);Assert.That(prep.Interact(chef),Is.True);
            Assert.That(prep.SuccessCheck,Is.True);Assert.That(prep.SuccessOpacity,Is.GreaterThan(0));
            Assert.That(fryer.Interact(chef),Is.True);chef.Move(Vector2.right,.05f);fryer.Advance(5);Assert.That(fryer.Busy,Is.False);Assert.That(fryer.slot.Item.Payload.state,Is.EqualTo(FoodState.Cooked));
            yield return null;
        }
        [UnityTest] public IEnumerator DirtyDishReturnsWashesAndCanBeReusedWithoutDuplication()
        {
            var seat=hud.shift.seats[0];var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene&&!s.plates);
            var dish=Object.Instantiate(source.itemPrefab);dish.Configure(new ItemPayload{isPlate=true,ingredient=seat.recipe.ingredient,state=seat.recipe.requiredState});
            Assert.That(seat.Reserve(dish.Payload),Is.True);seat.Receive(dish);seat.Advance(2);hud.shift.Advance(0);
            var rack=seat.dishReturn;Assert.That(rack.Count,Is.EqualTo(1));Assert.That(dish.Payload.dirty,Is.True);Assert.That(seat.tableSlot.Item,Is.Null);
            Assert.That(rack.Interact(chef),Is.True);Assert.That(rack.Count,Is.Zero);Assert.That(rack.Interact(chef),Is.False);
            Assert.That(ItemPayload.CanPlate(dish.Payload,ItemPayload.Food(source.ingredient)),Is.False);
            var sink=Interactable.Active.OfType<WashingStation>().Single(s=>s.gameObject.scene==scene);
            Assert.That(sink.Interact(chef),Is.True);sink.Advance(1);float progress=sink.Progress;chef.Move(Vector2.left,.05f);sink.Advance(10);Assert.That(sink.Progress,Is.EqualTo(progress));
            Assert.That(sink.Interact(chef),Is.True);sink.Advance(2);Assert.That(sink.Busy,Is.False);Assert.That(sink.Interact(chef),Is.True);
            Assert.That(sink.SuccessCheck,Is.True);Assert.That(sink.SuccessOpacity,Is.GreaterThan(0));
            Assert.That(chef.Hands.Item,Is.SameAs(dish));Assert.That(dish.Payload.EmptyPlate,Is.True);Assert.That(dish.Payload.dirty,Is.False);
            Assert.That(sink.Interact(chef),Is.False,"Clean plates must not start another wash");
            var prepared=ItemPayload.Food(source.ingredient);prepared.state=source.ingredient.platingState;Assert.That(ItemPayload.CanPlate(dish.Payload,prepared),Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator SecondChefCanResumeAbandonedPrepButCannotDoubleItsProgress()
        {
            var one=InputSystem.AddDevice<Gamepad>();var two=InputSystem.AddDevice<Gamepad>();
            try
            {
                hud.coop.BindPlayerOne(one);Assert.That(hud.coop.Join(two),Is.True);var second=hud.coop.PlayerTwo;second.GetComponent<ChefInput>().enabled=false;
                var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene&&!s.plates);
                var prep=Interactable.Active.OfType<ProcessingStation>().Single(s=>s.gameObject.scene==scene&&s.requiresAttendance);
                source.Interact(chef);prep.Interact(chef);prep.Advance(.5f);Assert.That(prep.Interact(second),Is.False,"Only one worker owns the job");
                chef.Move(Vector2.right,.05f);Assert.That(prep.Interact(second),Is.True);prep.Advance(1);Assert.That(prep.Busy,Is.False);Assert.That(prep.Interact(second),Is.True);Assert.That(second.Hands.Item,Is.Not.Null);
            }
            finally {InputSystem.RemoveDevice(one);InputSystem.RemoveDevice(two);}
            yield return null;
        }
        [UnityTest] public IEnumerator UnavailableModesIgnoreActivationAndQuickPlayRemainsSelectable()
        {
            var menu=hud.GetComponent<RestaurantMenu>();var pad=InputSystem.AddDevice<Gamepad>();
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                menu.OpenFrontEnd();Assert.That(menu.Selection,Is.EqualTo(1));
                InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.DpadUp));InputSystem.Update();menu.Tick(true);
                Assert.That(menu.Selection,Is.Zero);menu.ActivateSelection();Assert.That(menu.Page,Is.EqualTo("Title"),"Tutorial is unavailable");Assert.That(Time.timeScale,Is.Zero);
                menu.OpenFrontEnd();menu.ActivateSelection();Assert.That(menu.Page,Is.EqualTo("Levels"));
            }
            finally {menu.Close();InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
            yield return null;
        }
        [UnityTest] public IEnumerator OpeningMenuCannotBackIntoUnselectedGameplay()
        {
            var menu=hud.GetComponent<RestaurantMenu>();
            try
            {
                menu.OpenFrontEnd();Assert.That(menu.Page,Is.EqualTo("Title"));Assert.That(Time.timeScale,Is.Zero);
                menu.ShowLevels(false);Assert.That(menu.Page,Is.EqualTo("Levels"));menu.NavigateBack();Assert.That(menu.Page,Is.EqualTo("Title"));
                menu.NavigateBack();Assert.That(menu.IsOpen && RestaurantMenu.GameplayBlocked,Is.True);
                menu.OpenRecipeBook();Assert.That(menu.Page,Is.EqualTo("Recipes"));menu.NavigateBack();Assert.That(menu.Page,Is.EqualTo("Title"));
                yield return null;Assert.That(Time.timeScale,Is.Zero);
            }
            finally {menu.Close();}
        }
        [UnityTest] public IEnumerator ChoosingKitchenRequiresWardrobeBeforeGameplay()
        {
            var menu=hud.GetComponent<RestaurantMenu>();
            try
            {
                menu.OpenFrontEnd();menu.ShowLevels(false);menu.ActivateSelection();
                Assert.That(menu.Page,Is.EqualTo("Your chef"));Assert.That(RestaurantMenu.GameplayBlocked,Is.True);
                Assert.That(Time.timeScale,Is.Zero);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(scene));
                yield return null;
                Assert.That(hud.GetComponent<ChefWardrobePreview>().Image,Is.Not.Null);
                menu.NavigateBack();Assert.That(menu.Page,Is.EqualTo("Levels"));
            }
            finally {menu.Close();}
        }
        [UnityTest] public IEnumerator SecondPlayerGetsTheirOwnBuildAndClothesOnJoin()
        {
            var pad=InputSystem.AddDevice<Gamepad>();var saved=ChefWardrobe.ForPlayer(1).Copy();
            try
            {
                ChefWardrobe.ForPlayer(1).build=2;ChefWardrobe.ForPlayer(1).clothing=0;
                hud.coop.UseKeyboardPlayerOne();Assert.That(hud.coop.Join(pad),Is.True);yield return null;
                var visual=hud.coop.PlayerTwo.GetComponentInChildren<ChefAppearance>();
                Assert.That(visual.appearance.build,Is.EqualTo(2));Assert.That(visual.appearance.clothing,Is.Zero);
                Assert.That(visual.IsVisible("C_Torso2"),Is.True);Assert.That(visual.IsVisible("C_Bib2"),Is.False);
                Assert.That(chef.GetComponentInChildren<ChefAppearance>().appearance.build,Is.EqualTo(ChefWardrobe.ForPlayer(0).build));
            }
            finally {ChefWardrobe.ForPlayer(1).build=saved.build;ChefWardrobe.ForPlayer(1).clothing=saved.clothing;InputSystem.RemoveDevice(pad);}
        }
        [UnityTest] public IEnumerator PauseRecipeBookPreservesHeldItemAndSession()
        {
            var source=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==scene&&!s.plates);
            source.Interact(chef);var food=chef.Hands.Item;var menu=hud.GetComponent<RestaurantMenu>();
            try
            {
                menu.OpenRecipeBook();float elapsed=hud.shift.ElapsedSeconds;
                yield return null;yield return null;
                Assert.That(menu.Page,Is.EqualTo("Recipes"));Assert.That(Time.timeScale,Is.Zero);Assert.That(hud.shift.ElapsedSeconds,Is.EqualTo(elapsed));Assert.That(chef.Hands.Item,Is.SameAs(food));
                menu.NavigateBack();Assert.That(menu.Page,Is.EqualTo("Main"));Assert.That(menu.IsOpen,Is.True);
                menu.Close();Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(chef.GetComponent<ChefInput>().AwaitUseRelease,Is.True);
            }
            finally {menu.Close();}
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
                    KitchenTestAccess.Approach(chef,anchor.GetComponent<Interactable>());
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
                    foreach(var extra in seat.recipe.additionalIngredients) dish.Payload.additions.Add(new IngredientPortion{ingredient=extra.ingredient,state=extra.state});
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
            Assert.That(previews,Is.Empty,"Authored pantry meshes must not also spawn duplicate gameplay item previews.");
            var pantries=Interactable.Active.OfType<SourceStation>().Where(s=>s.gameObject.scene==scene&&!s.plates).ToArray();
            Assert.That(pantries.Length,Is.EqualTo(4));
            foreach(var pantry in pantries)
            {
                var art=pantry.GetComponent<StationArt>();Assert.That(art.includesPantryDisplay,Is.True);
                Assert.That(art.visual.GetComponentsInChildren<Collider>(),Is.Empty);
                Assert.That(art.visual.GetComponentsInChildren<Carryable>(),Is.Empty);
                Assert.That(art.visual.GetComponentInChildren<MeshFilter>().sharedMesh.name,Does.StartWith("Pantry"+pantry.ingredient.visualKind));
            }
            foreach(var station in Interactable.Active.Where(s=>s.gameObject.scene==scene))
                Assert.That(station.transform.Find("P1 target").GetComponentsInChildren<Collider>(),Is.Empty);
            yield return null;
        }
        [UnityTest] public IEnumerator EveryPracticeStartingPointCanCompleteTheRealCookingLoop()
        {
            foreach(string training in new[]{"Guided full loop","Prep","Frying","Plating","Serving","Garden salad"})
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
                if(training=="Guided full loop" || training=="Garden salad") {Assert.That(chef.Hands.Item,Is.Null); Assert.That(source.Interact(chef),Is.True);}
                if(training=="Guided full loop" || training=="Prep" || training=="Garden salad")
                {Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Raw)); Assert.That(prep.Interact(chef),Is.True); prep.Advance(1.5f); Assert.That(prep.Interact(chef),Is.True);}
                if(training=="Guided full loop" || training=="Prep" || training=="Frying")
                {Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Cut)); Assert.That(fryer.Interact(chef),Is.True); fryer.Advance(5); Assert.That(fryer.Interact(chef),Is.True);}
                if(training!="Serving")
                {
                    Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(hud.order.recipe.requiredState)); Assert.That(counter.Interact(chef),Is.True);
                    Assert.That(plates.Interact(chef),Is.True); Assert.That(counter.Interact(chef),Is.True);
                    if(training=="Garden salad")
                    {
                        var greens=stations.OfType<SourceStation>().Single(s=>!s.plates && s.ingredient==hud.order.recipe.additionalIngredients[0].ingredient);
                        Assert.That(greens.Interact(chef),Is.True);Assert.That(prep.Interact(chef),Is.True);prep.Advance(1.5f);Assert.That(prep.Interact(chef),Is.True);Assert.That(counter.Interact(chef),Is.True);
                    }
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

