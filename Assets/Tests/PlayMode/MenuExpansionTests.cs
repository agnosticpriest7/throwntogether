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
    public sealed class MenuExpansionTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public string Read()=>json;public void Write(string s)=>json=s;}
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
        void BaseMenu(){foreach(var r in DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0))DailyMenu.Toggle(RestaurantAccounts.Current,r);}
        void Use(Interactable station){KitchenTestAccess.Approach(chef,station);Assert.That(chef.Use(),Is.True,station.stationName+": "+chef.Feedback);KitchenTestAccess.SelectDefault(chef,station);}
        T Find<T>() where T:Component=>Object.FindObjectsByType<T>().First(t=>t.gameObject.scene==scene);
        void UnlockAll()
        {
            var a=RestaurantAccounts.Current;int id=a.StartDay();Assert.That(a.Settle(id,1500,0),Is.True);
            foreach(var offer in day.Settings.purchases)Assert.That(a.Buy(offer.id,offer.cost),Is.True);
            Assert.That(a.SetMenu(DailyMenu.Catalog.Select(r=>r.id).ToArray()),Is.True);
        }
        [UnityTest] public IEnumerator FirstMenuPurchaseNextMenuOrdersAndVarietySettlementAreOneFlow()
        {
            var menu=hud.GetComponent<RestaurantMenu>();Assert.That(day.AwaitingMenu,Is.True);Assert.That(menu.Page,Is.EqualTo("Today's Menu"));
            day.Advance(100);Assert.That(day.Elapsed,Is.Zero);Assert.That(day.DayNumber,Is.Zero);Assert.That(menu.StartSelectedMenu(),Is.False);
            BaseMenu();Assert.That(menu.StartSelectedMenu(),Is.True);yield return null;yield return null;
            Assert.That(day.AwaitingMenu,Is.False);Assert.That(day.Menu.Length,Is.EqualTo(3));
            day.Advance(18);Assert.That(day.Tables.Where(t=>t.order.Active).All(t=>day.Menu.Contains(t.order.recipe)),Is.True);
            foreach(var r in day.Menu)day.RecordMeal(r,0);day.Advance(300);Assert.That(day.VarietyBonus,Is.Zero);Assert.That(day.Paid,Is.True);
            var a=RestaurantAccounts.Current;int funding=a.StartDay();Assert.That(a.Settle(funding,300,0),Is.True);
            var offer=day.Settings.purchases.Single(p=>p.id=="grill");Assert.That(a.Buy(offer.id,offer.cost),Is.True);
            var chosen=DailyMenu.Catalog.Where(r=>r.Unlocked(a)).Take(4).ToArray();Assert.That(a.SetMenu(chosen.Select(r=>r.id).ToArray()),Is.True);
            RestaurantAccounts.UseStorage(memory);yield return Load();
            Assert.That(day.Menu,Is.EquivalentTo(chosen));Assert.That(Find<RestaurantDay>().DayNumber,Is.GreaterThan(1));
            Assert.That(Object.FindObjectsByType<ProcessingStation>().Any(p=>p.gameObject.scene==scene && p.appliance.id=="appliance.grill"),Is.True);
            // Changing a draft does not change customers' service-day menu snapshot.
            Assert.That(RestaurantAccounts.Current.SetMenu(new string[0]),Is.True);
            for(int i=0;i<8;i++){day.Advance(8);Assert.That(day.Tables.Where(t=>t.order.Active).All(t=>chosen.Contains(t.order.recipe)),Is.True);}
            foreach(var r in chosen)day.RecordMeal(r,0);int income=day.BaseIncome,bonus=day.Bonuses,cash=RestaurantAccounts.Current.Data.cash;
            day.Advance(300);Assert.That(day.VarietyBonus,Is.EqualTo(income/20));Assert.That(RestaurantAccounts.Current.Data.cash,Is.EqualTo(cash+income+bonus+income/20));
            day.RetryPayment();Assert.That(RestaurantAccounts.Current.Data.cash,Is.EqualTo(cash+income+bonus+income/20));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AllThirteenRecipesCookPlateAndServeWithRealStationsAndFinitePlates()
        {
            UnlockAll();yield return Load();var menu=hud.GetComponent<RestaurantMenu>();menu.Close();yield return null;yield return null;
            var stations=Interactable.Active.Where(s=>s.gameObject.scene==scene).ToArray();var stock=stations.OfType<SourceStation>().Single(s=>s.plates);
            var counter=stations.OfType<CounterStation>().First(s=>s.GetType()==typeof(CounterStation));var sink=stations.OfType<WashingStation>().Single();var table=day.Tables[0];
            foreach(var recipe in DailyMenu.Catalog)
            {
                var parts=new[]{new IngredientPortion{ingredient=recipe.ingredient,state=recipe.requiredState}}.Concat(recipe.additionalIngredients);
                bool first=true;
                foreach(var part in parts)
                {
                    KitchenTestAccess.Take(chef,part.ingredient);
                    foreach(var step in recipe.steps.Where(p=>p.ingredient==part.ingredient))
                    {
                        var station=stations.OfType<ProcessingStation>().First(s=>s.appliance.Supports(step));Use(station);Assert.That(station.Busy,Is.True);station.Advance(step.duration);Use(station);
                    }
                    Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(part.state),recipe.displayName);
                    if(first){Use(stock);first=false;}Use(counter);
                }
                Use(counter);Assert.That(recipe.Matches(chef.Hands.Item.Payload),Is.True,recipe.displayName);
                table.ReserveSeat();table.Seat(recipe);Use(table);Assert.That(table.order.Phase,Is.EqualTo(OrderPhase.Eating));
                table.order.Advance(9);Use(table);table.Depart();Use(sink);sink.Advance(3);Use(sink);Use(stock);
                Assert.That(stock.CleanPlatesRemaining,Is.EqualTo(5));
            }
            Assert.That(day.Served,Is.EqualTo(13));Assert.That(day.BaseIncome,Is.EqualTo(DailyMenu.Catalog.Sum(r=>r.salePrice)));
            yield return null;Capture("all-appliances");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EveryPurchasedStationAndCategoryRemainsAccessibleInAllLayouts()
        {
            UnlockAll();
            for(int i=0;i<3;i++)
            {
                SessionOptions.Kitchen=i;yield return Load();hud.GetComponent<RestaurantMenu>().Close();yield return null;yield return null;
                CheckConnectedAccess();
                foreach(var station in Interactable.Active.Where(s=>s.gameObject.scene==scene))KitchenTestAccess.Approach(chef,station);
                var foods=Interactable.Active.OfType<SourceStation>().Where(s=>s.gameObject.scene==scene && !s.plates).ToArray();
                Assert.That(foods.Length,Is.EqualTo(2));Assert.That(foods.SelectMany(s=>s.Ingredients).Distinct().Count(),Is.EqualTo(6));
                foreach(var food in foods.SelectMany(s=>s.Ingredients))
                {KitchenTestAccess.Take(chef,food);Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(FoodState.Raw));Object.Destroy(chef.Hands.Release().gameObject);}
                yield return null;Capture("layout-"+i);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ControllerSelectsStartingMenuAndStartsWithoutMouse()
        {
            var menu=hud.GetComponent<RestaurantMenu>();var pad=InputSystem.AddDevice<Gamepad>();
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                void Send(GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();menu.Tick(true);}
                void Press(GamepadButton button){Send(new GamepadState());Send(new GamepadState().WithButton(button));Send(new GamepadState());}
                Press(GamepadButton.South);Assert.That(day.AwaitingMenu,Is.True);
                for(int i=0;i<3;i++){Press(GamepadButton.DpadDown);Press(GamepadButton.South);}
                Assert.That(DailyMenu.Resolve(RestaurantAccounts.Current).Length,Is.EqualTo(3));
                for(int i=0;i<3;i++)Press(GamepadButton.DpadUp);Press(GamepadButton.South);
                Assert.That(day.AwaitingMenu,Is.False);Assert.That(menu.IsOpen,Is.False);Assert.That(chef.Hands.Item,Is.Null);
            }
            finally{InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
            yield return null;
        }
        void CheckConnectedAccess()
        {
            var motor=chef.GetComponent<CharacterController>();bool enabled=motor.enabled;var position=chef.transform.position;var rotation=chef.transform.rotation;motor.enabled=false;Physics.SyncTransforms();
            try
            {
                const float cell=.25f;const int width=81,height=49;
                var free=new bool[width,height];var seen=new bool[width,height];
                for(int x=0;x<width;x++)for(int z=0;z<height;z++)
                {var p=new Vector3(-8+x*cell,.4f,-5+z*cell);free[x,z]=!Physics.CheckCapsule(p,p+Vector3.up*1.1f,.32f,~0,QueryTriggerInteraction.Ignore);}
                var start=new Vector2Int(Mathf.RoundToInt((position.x+8)/cell),Mathf.RoundToInt((position.z+5)/cell));Assert.That(free[start.x,start.y],Is.True);
                var queue=new System.Collections.Generic.Queue<Vector2Int>();queue.Enqueue(start);seen[start.x,start.y]=true;
                while(queue.Count>0)
                {var p=queue.Dequeue();foreach(var delta in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var n=p+delta;if(n.x<0||n.y<0||n.x>=width||n.y>=height||seen[n.x,n.y]||!free[n.x,n.y])continue;seen[n.x,n.y]=true;queue.Enqueue(n);}}
                var second=hud.GetComponent<KitchenLayout>().secondSpawn.position;Assert.That(seen[Mathf.RoundToInt((second.x+8)/cell),Mathf.RoundToInt((second.z+5)/cell)],Is.True,"P2 spawn remains connected");
                foreach(var station in Interactable.Active.Where(s=>s.gameObject.scene==scene))
                {
                    bool reached=false;
                    for(int x=0;x<width&&!reached;x++)for(int z=0;z<height&&!reached;z++)
                    {
                        if(!seen[x,z])continue;var point=new Vector3(-8+x*cell,.04f,-5+z*cell);var delta=station.transform.position-point;delta.y=0;
                        if(delta.magnitude>1.8f || delta.sqrMagnitude<.01f)continue;chef.transform.position=point;chef.transform.rotation=Quaternion.LookRotation(delta);chef.FindFocus();reached=chef.Focus==station;
                    }
                    if(!reached)
                    {
                        var text=new System.Text.StringBuilder("Target "+station.transform.position+"\n");
                        for(int x=0;x<width;x++)for(int z=0;z<height;z++)
                        {var point=new Vector3(-8+x*cell,0,-5+z*cell);if(free[x,z] && Vector3.Distance(point,station.transform.position)<2.2f)text.AppendLine(point+" connected="+seen[x,z]);}
                        System.IO.File.WriteAllText("../MenuExpansion/access.txt",text.ToString());
                    }
                    Assert.That(reached,Is.True,"Connected approach: "+station.stationName+" in layout "+SessionOptions.Kitchen);
                }
            }
            finally{chef.transform.SetPositionAndRotation(position,rotation);motor.enabled=enabled;Physics.SyncTransforms();}
        }
        [UnityTest] public IEnumerator TwoControllersChooseIndependentlyAndOpeningPressDoesNotDispense()
        {
            BaseMenu();Assert.That(hud.GetComponent<RestaurantMenu>().StartSelectedMenu(),Is.True);yield return null;yield return null;
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var one=InputSystem.AddDevice<Gamepad>();var two=InputSystem.AddDevice<Gamepad>();
            try
            {
                hud.coop.BindPlayerOne(one);Assert.That(hud.coop.Join(two),Is.True);var second=hud.coop.PlayerTwo;
                var input=chef.GetComponent<ChefInput>();var inputTwo=second.GetComponent<ChefInput>();input.enabled=true;
                var produce=Interactable.Active.OfType<SourceStation>().Single(s=>s.gameObject.scene==scene && s.storage?.displayName=="Produce Rack");
                KitchenTestAccess.Approach(chef,produce);KitchenTestAccess.Approach(second,produce);
                void Send(Gamepad pad,ChefInput target,GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();target.Tick(0);}
                Send(one,input,new GamepadState());Send(two,inputTwo,new GamepadState());
                Send(one,input,new GamepadState().WithButton(GamepadButton.South));Assert.That(input.Storage,Is.SameAs(produce));Assert.That(chef.Hands.Item,Is.Null);
                Send(two,inputTwo,new GamepadState().WithButton(GamepadButton.South));Assert.That(inputTwo.Storage,Is.SameAs(produce));
                Send(one,input,new GamepadState());Send(two,inputTwo,new GamepadState());
                Send(two,inputTwo,new GamepadState().WithButton(GamepadButton.DpadRight));Assert.That(inputTwo.StorageIndex,Is.EqualTo(1));Assert.That(input.StorageIndex,Is.Zero);
                Send(one,input,new GamepadState().WithButton(GamepadButton.South));Send(two,inputTwo,new GamepadState().WithButton(GamepadButton.South));
                Assert.That(chef.Hands.Item.Payload.ingredient.visualKind,Is.EqualTo(IngredientVisualKind.Potato));Assert.That(second.Hands.Item.Payload.ingredient.visualKind,Is.EqualTo(IngredientVisualKind.Tomato));
                Object.Destroy(chef.Hands.Release().gameObject);Object.Destroy(second.Hands.Release().gameObject);
                inputTwo.OpenStorage(produce);Assert.That(inputTwo.StorageIndex,Is.EqualTo(1));inputTwo.SetInputFocus(false);inputTwo.Tick(0);Assert.That(inputTwo.Storage,Is.Null);
                input.OpenStorage(produce);Send(one,input,new GamepadState().WithButton(GamepadButton.East));Assert.That(input.Storage,Is.Null);
            }
            finally{InputSystem.RemoveDevice(one);InputSystem.RemoveDevice(two);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
            LogAssert.NoUnexpectedReceived();
        }
        void Capture(string label)
        {
            string path=System.IO.Path.GetFullPath("../MenuExpansion/"+label+".png");System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var cam=hud.gameplayCamera;var target=RenderTexture.GetTemporary(1280,800,24);var old=cam.targetTexture;var active=RenderTexture.active;
            try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();System.IO.File.WriteAllBytes(path,image.EncodeToPNG());Object.Destroy(image);}
            finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
        }
    }
}
