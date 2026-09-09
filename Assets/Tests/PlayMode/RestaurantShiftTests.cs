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
                    var ticket=shift.seats.First(s=>s.Active && s.Phase==OrderPhase.Waiting);
                    var source=stations.OfType<SourceStation>().Single(s=>!s.plates && s.ingredient==ticket.recipe.ingredient);
                    Use(chef,source); Assert.That(service.Interact(chef),Is.False,"Raw ingredients cannot fulfil a ticket");
                    Use(chef,prep); prep.Advance(1.5f); Use(chef,prep);
                    if(ticket.recipe.requiredState==FoodState.Cooked) {Use(chef,fryer); fryer.Advance(5); Use(chef,fryer);}
                    else Assert.That(fryer.Interact(chef),Is.False,"Cold salad must not enter the fryer");
                    Assert.That(chef.Hands.Item.Payload.state,Is.EqualTo(ticket.recipe.requiredState));
                    Use(chef,plating); Use(chef,plates); Use(chef,plating); Use(chef,plating);
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
                SceneManager.SetActiveScene(original);
                foreach(var root in suspended) if(root!=null) root.SetActive(true);
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        private static void Use(ChefController chef,Interactable station)
        {
            var motor=chef.GetComponent<CharacterController>(); motor.enabled=false;
            chef.transform.position=station.transform.position+new Vector3(0,.04f,-1.5f);
            chef.transform.rotation=Quaternion.identity; motor.enabled=true;
            Assert.That(chef.Use(),Is.True,"Use failed at "+station.stationName+": "+chef.Feedback);
        }
    }
}
