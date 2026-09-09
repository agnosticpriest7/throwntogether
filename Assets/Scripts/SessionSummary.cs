using UnityEngine;
namespace ThrownTogether
{
    // Counts accepted actions, not button presses. Helpers can contribute without serving the final dish.
    public sealed class SessionSummary : MonoBehaviour
    {
        public sealed class Contribution
        {
            public int pickups, prep, fry, plates, served;
            public string Description => "Prep "+prep+"  |  Fry "+fry+"  |  Plate "+plates+"  |  Serve "+served;
        }
        public Contribution PlayerOne {get;}=new Contribution();
        public Contribution PlayerTwo {get;}=new Contribution();
        public float ElapsedSeconds {get;private set;}
        private RestaurantHud hud;
        private ChefController second;
        private void Start() { hud=GetComponent<RestaurantHud>(); hud.chef.InteractionSucceeded+=One; }
        private void One(Interactable station,bool held,bool plated) => Record(PlayerOne,station,held,plated);
        private void Two(Interactable station,bool held,bool plated) => Record(PlayerTwo,station,held,plated);
        public static void Record(Contribution count,Interactable station,bool held,bool plated)
        {
            if(station is SourceStation) count.pickups++;
            if(plated) count.plates++;
            else if(station is ProcessingStation process && held && process.Busy)
            { if(process.recipe.input==FoodState.Raw) count.prep++; else count.fry++; }
            if(station is ServiceStation) count.served++;
        }
        private void Update()
        {
            if(hud==null) return;
            if(hud.shift!=null ? !hud.shift.Complete : hud.order.Phase!=OrderPhase.Complete) ElapsedSeconds+=Time.deltaTime;
            var next=hud.coop!=null ? hud.coop.PlayerTwo:null;
            if(next!=second)
            {
                if(second!=null) second.InteractionSucceeded-=Two;
                second=next; if(second!=null) second.InteractionSucceeded+=Two;
            }
        }
        private void OnDestroy()
        {
            if(hud!=null && hud.chef!=null) hud.chef.InteractionSucceeded-=One;
            if(second!=null) second.InteractionSucceeded-=Two;
        }
    }
}
