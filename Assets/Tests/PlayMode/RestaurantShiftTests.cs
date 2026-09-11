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
    public sealed class RestaurantShiftTests
    {
        [UnityTest] public IEnumerator SoloChefCompletesAllThreeDishesAndAllSixOrders()
        {
            int previousOrders=SessionOptions.ShiftOrders;SessionOptions.ShiftOrders=6;
            var original=SceneManager.GetActiveScene();
            var suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)
                ? original.GetRootGameObjects().Where(r=>r.activeSelf).ToArray() : new GameObject[0];
            foreach(var root in suspended) root.SetActive(false);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantShift",LoadSceneMode.Additive);
#endif
            var scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1); SceneManager.SetActiveScene(scene);
            try
            {
                yield return null;
                var chef=Object.FindObjectsByType<ChefController>().Single(c=>c.gameObject.scene==scene);
                chef.GetComponent<ChefInput>().enabled=false;
                var shift=Object.FindObjectsByType<RestaurantShift>().Single(s=>s.gameObject.scene==scene);
                var stations=Interactable.Active.Where(s=>s.gameObject.scene==scene).ToArray();
                var prep=stations.OfType<ProcessingStation>().Single(s=>s.recipe.input==FoodState.Raw);
                var fryer=stations.OfType<ProcessingStation>().Single(s=>s.recipe.input==FoodState.Cut);
                var plating=stations.OfType<CounterStation>().Single(s=>s.stationName=="COUNTER");
                var plates=stations.OfType<SourceStation>().Single(s=>s.plates);
                var service=stations.OfType<ServiceStation>().Single();
                Assert.That(shift.seats.Count(s=>s.Active),Is.EqualTo(2));
                Assert.That(shift.definition.orders.Select(r=>r.ingredient).Distinct().Count(),Is.EqualTo(3));
                for(int i=0;i<6;i++)
                {
                    if(plates.CleanPlatesRemaining==0)
                    {
                        Assert.That(plates.Interact(chef),Is.False,"Sixth plate cannot be minted");
                        Use(chef,stations.OfType<DishReturnStation>().Single());
                        var sink=stations.OfType<WashingStation>().Single();Use(chef,sink);sink.Advance(3);Use(chef,sink);Use(chef,plates);
                    }
                    var ticket=shift.seats.First(s=>s.Active && s.Phase==OrderPhase.Waiting);
                    var source=stations.OfType<SourceStation>().Single(s=>s.Offers(ticket.recipe.ingredient));
                    KitchenTestAccess.Take(chef,ticket.recipe.ingredient); Assert.That(service.Interact(chef),Is.False,"Raw ingredients cannot fulfil a ticket");
                    Use(chef,prep); prep.Advance(1.5f); Use(chef,prep);
                    if(ticket.recipe.requiredState==FoodState.Cooked) {Use(chef,fryer); fryer.Advance(5); Use(chef,fryer);}
                    else Assert.That(fryer.Interact(chef),Is.False,"Cold salad must not enter the fryer");
                    Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(ticket.recipe.requiredState));
                    Use(chef,plating); Use(chef,plates); Use(chef,plating);
                    foreach(var extra in ticket.recipe.additionalIngredients)
                    {
                        KitchenTestAccess.Take(chef,extra.ingredient);
                        Use(chef,prep);prep.Advance(1.5f);Use(chef,prep);Use(chef,plating);
                    }
                    Use(chef,plating);
                    Use(chef,service); Assert.That(ticket.Phase,Is.EqualTo(OrderPhase.Delivering));
                    yield return new WaitForSeconds(1.8f);
                    ticket.Advance(2); shift.Advance(.1f);
                    Assert.That(shift.CompletedCount,Is.EqualTo(i+1));
                    Assert.That(shift.seats.Count(s=>s.Active),Is.LessThanOrEqualTo(2));
                }
                Assert.That(shift.Complete,Is.True); Assert.That(shift.seats.All(s=>!s.Active),Is.True);
                float elapsed=shift.ElapsedSeconds; shift.Advance(10); Assert.That(shift.ElapsedSeconds,Is.EqualTo(elapsed));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                SessionOptions.ShiftOrders=previousOrders;
                SceneManager.SetActiveScene(original);
                foreach(var root in suspended) if(root!=null) root.SetActive(true);
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        private static void Use(ChefController chef,Interactable station)
        {
            KitchenTestAccess.Approach(chef,station);
            Assert.That(chef.Use(),Is.True,"Use failed at "+station.stationName+": "+chef.Feedback);
            KitchenTestAccess.SelectDefault(chef,station);
        }
    }
    internal static class KitchenTestAccess
    {
        public static void SelectDefault(ChefController chef,Interactable station)
        {
            if(station is SourceStation source && source.storage!=null && chef.GetComponent<ChefInput>().Storage!=null)
                Assert.That(chef.GetComponent<ChefInput>().ChooseIngredient(System.Array.FindIndex(source.Ingredients,x=>x==source.ingredient)),Is.True);
        }
        public static void Take(ChefController chef,IngredientDefinition ingredient)
        {
            var source=Interactable.Active.OfType<SourceStation>().Single(s=>s.gameObject.scene==chef.gameObject.scene && s.Offers(ingredient));
            Approach(chef,source);Assert.That(chef.Use(),Is.True);
            if(source.storage!=null)Assert.That(chef.GetComponent<ChefInput>().ChooseIngredient(System.Array.IndexOf(source.Ingredients,ingredient)),Is.True);
            Assert.That(chef.Hands.Item.Payload.ingredient,Is.SameAs(ingredient));
        }

        // Stations can now form connected runs. Approach from an actually clear side,
        // rather than teleporting inside the adjacent counter south of every station.
        public static void Approach(ChefController chef,Interactable station)
        {
            var motor=chef.GetComponent<CharacterController>();bool enabled=motor.enabled;motor.enabled=false;Physics.SyncTransforms();
            try
            {
                foreach(float distance in new[]{1.5f,1.3f,1.7f})
                foreach(var side in new[]{Vector3.back,Vector3.right,Vector3.forward,Vector3.left})
                {
                    var point=station.transform.position+side*distance;
                    if(Physics.CheckCapsule(point+Vector3.up*.4f,point+Vector3.up*1.5f,.32f,~0,QueryTriggerInteraction.Ignore))continue;
                    chef.transform.position=point+Vector3.up*.04f;chef.transform.rotation=Quaternion.LookRotation(-side);chef.FindFocus();
                    if(chef.Focus==station)return;
                }
                Assert.Fail("No clear interaction approach to "+station.stationName);
            }
            finally{motor.enabled=enabled;Physics.SyncTransforms();}
        }
    }
}
