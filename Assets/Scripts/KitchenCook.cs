using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // One physical hot-line cook. Expo selects demand; food and appliance ownership
    // remain the same CarrySlots used by players. Appliance Update owns cooking time.
    public sealed class KitchenCook : MonoBehaviour
    {
        enum Phase { Idle, Fetch, Load, Cooking, Plate, Stage, Park }
        RestaurantDay day;DiningWalker walker;CarrySlot hands;ServiceStation pass;
        KitchenTicket ticket;ProcessingRecipe process;ProcessingStation appliance;
        Carryable selected,job;Interactable target;Phase phase;float retry;
        public string Status {get;private set;}="Waiting for fired orders";
        public CarrySlot Hands=>hands;
        public KitchenTicket CurrentTicket=>ticket;
        public static ProcessingRecipe HotStep(RecipeDefinition recipe)=>recipe==null || recipe.additionalIngredients.Length!=0?null:
            recipe.steps.LastOrDefault(s=>s!=null && s.ingredient==recipe.ingredient && s.output==recipe.requiredState &&
                (s.output==FoodState.Cooked || s.output==FoodState.Grilled || s.output==FoodState.Griddled));
        public void Initialize(RestaurantDay owner)
        {
            day=owner;pass=FindObjectsByType<ServiceStation>(FindObjectsSortMode.None).First(s=>s.gameObject.scene==gameObject.scene);
            transform.position=KitchenStaffRoute.Approach(pass.transform)??new Vector3(0,0,0);
            walker=gameObject.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,1);
            var grip=new GameObject("Cook hands");grip.transform.SetParent(transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
        }
        bool Fired=>ticket!=null && ticket.State==KitchenTicketState.Active && day.Expo?.TableFor(ticket)?.WaitingForMeal==true;
        bool Near(Interactable s)=>s!=null && Vector3.Distance(transform.position,s.transform.position)<=1.85f;
        bool Go(Interactable s)
        {
            if(s==null || !s.isActiveAndEnabled){target=null;return false;}
            var route=KitchenStaffRoute.ToStation(transform.position,s.transform);if(route==null){target=null;return false;}
            target=s;walker.Go(route);return true;
        }
        void Wait(string reason){Status=reason;retry=.4f;}
        void Idle(){phase=Phase.Idle;ticket=null;job=null;selected=null;target=null;retry=.15f;Status="Waiting for fired orders";}
        static bool Plain(Carryable item,IngredientDefinition food,FoodState state)=>item!=null && item.Payload!=null && !item.Payload.isPlate && !item.Payload.dirty && item.Payload.additions.Count==0 && item.Payload.ingredient==food && item.Payload.state==state;
        Interactable Location(Carryable item)
        {
            if(item?.Owner==null)return null;
            var bin=item.Owner.GetComponentInParent<PrepBin>();if(bin!=null)return bin;
            var counter=item.Owner.GetComponentInParent<CounterStation>();
            return counter!=null && counter.slot==item.Owner?counter:null;
        }
        bool Pickup(Carryable item)
        {
            var location=Location(item);if(location==null || !Near(location))return false;
            if(location is PrepBin bin)return bin.Take(hands);
            if(location is ProcessingStation p && p.Busy)return false;
            return hands.TryTake(item);
        }
        bool CoveredByPlayerWork()
        {
            var food=FindObjectsByType<Carryable>(FindObjectsSortMode.None).Where(i=>i!=job && i!=selected && i.gameObject.scene==gameObject.scene && i.Owner!=null && i.Owner.GetComponentInParent<DiningTable>()==null).ToArray();
            var used=new HashSet<Carryable>();
            foreach(var order in day.Expo.OpenTickets)
            {
                var hot=HotStep(order.Recipe);if(hot==null)continue;
                var found=food.FirstOrDefault(i=>!used.Contains(i) && day.Expo.BoundTicket(i)==order)??food.FirstOrDefault(i=>!used.Contains(i) && day.Expo.BoundTicket(i)==null &&
                    (order.Recipe.Matches(i.Payload) || Plain(i,hot.ingredient,hot.output) || Location(i) is ProcessingStation p && p.CurrentProcess==hot));
                if(found!=null)used.Add(found);
                if(order==ticket)return found!=null;
            }
            return false;
        }
        void FindWork()
        {
            if(day.Expo==null || !day.HasInstalledExpo){Wait("Requires an installed Expo desk");return;}
            var food=FindObjectsByType<Carryable>(FindObjectsSortMode.None).Where(i=>i.gameObject.scene==gameObject.scene && i.Owner!=null && i.Owner.GetComponentInParent<DiningTable>()==null).ToArray();
            var used=new HashSet<Carryable>();string reason="Waiting for fired single-dish orders";
            foreach(var order in day.Expo.OpenTickets)
            {
                var hot=HotStep(order.Recipe);if(hot==null)continue;
                var existing=food.FirstOrDefault(i=>!used.Contains(i) && day.Expo.BoundTicket(i)==order)??
                    food.FirstOrDefault(i=>!used.Contains(i) && day.Expo.BoundTicket(i)==null &&
                        (order.Recipe.Matches(i.Payload) || Plain(i,hot.ingredient,hot.output) ||
                        Location(i) is ProcessingStation p && p.CurrentProcess==hot));
                if(existing!=null)used.Add(existing);
                if(order.State!=KitchenTicketState.Active)continue;
                if(existing!=null)
                {
                    var place=Location(existing);
                    if(place==null || place is ProcessingStation busy && busy.Busy){reason="Order already in player/station work";continue;}
                    ticket=order;process=hot;selected=existing;
                    if(Go(place)){phase=Phase.Fetch;Status="Collecting "+order.Recipe.displayName;return;}
                    reason="Finished food path blocked";continue;
                }
                var free=FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None).FirstOrDefault(p=>p.gameObject.scene==gameObject.scene && p.isActiveAndEnabled && !p.requiresAttendance && !p.Busy && p.slot.Item==null &&
                    p.ProcessFor(new ItemPayload{ingredient=hot.ingredient,state=hot.input})==hot && KitchenStaffRoute.ToStation(transform.position,p.transform)!=null);
                if(free==null){reason="Waiting for a compatible appliance";continue;}
                var input=food.FirstOrDefault(i=>!used.Contains(i) && Plain(i,hot.ingredient,hot.input) && Location(i)!=null &&
                    !(Location(i) is ProcessingStation cooking && cooking.Busy) && KitchenStaffRoute.ToStation(transform.position,Location(i).transform)!=null);
                Interactable pickup=input!=null?Location(input):hot.input==FoodState.Raw?FindObjectsByType<SourceStation>(FindObjectsSortMode.None).FirstOrDefault(s=>s.gameObject.scene==gameObject.scene && s.Offers(hot.ingredient) && hot.ingredient.Unlocked && KitchenStaffRoute.ToStation(transform.position,s.transform)!=null):null;
                if(pickup==null){reason="Needs "+hot.ingredient.NameFor(hot.input);continue;}
                ticket=order;process=hot;appliance=free;selected=input;
                if(Go(pickup)){phase=Phase.Fetch;Status="Collecting for "+order.Recipe.displayName;return;}
            }
            Wait(reason);
        }
        void Park()
        {
            phase=Phase.Park;
            foreach(var s in Interactable.Active.Where(s=>s!=null && s.gameObject.scene==gameObject.scene &&
                (s is PrepBin b && b.CanStore(hands.Item.Payload) || s.GetType()==typeof(CounterStation) && ((CounterStation)s).slot.Item==null)))if(Go(s)){Status="Holding order — storing food safely";return;}
            target=null;Wait("Hold/finished order — needs free counter; food retained");
        }
        void ToPlate()
        {
            if(hands.Item.Payload.isPlate){phase=Phase.Stage;if(!Go(pass))Wait("Pass path blocked");return;}
            phase=Phase.Plate;var stock=FindObjectsByType<SourceStation>(FindObjectsSortMode.None).FirstOrDefault(s=>s.gameObject.scene==gameObject.scene && s.plates);
            if(!Go(stock)){target=null;Wait("Plate stack unavailable — food retained");}
            else Status="Plating "+process.ingredient.NameFor(process.output);
        }
        public void Advance(float seconds)
        {
            if(day==null || day.Closed || day.AwaitingMenu || !isActiveAndEnabled || seconds<=0)return;
            if(retry>0){retry-=seconds;return;}
            if(phase==Phase.Idle){FindWork();return;}
            if(phase==Phase.Fetch && (!Fired || CoveredByPlayerWork())){Idle();return;}
            if(phase==Phase.Load && (!Fired || CoveredByPlayerWork()) && hands.Item!=null){Park();return;}
            if(phase!=Phase.Fetch && (job==null || job.Owner!=hands && (appliance==null || job.Owner!=appliance.slot))){Idle();return;}
            if(target==null || !target.isActiveAndEnabled)
            {if(hands.Item==null){Idle();return;}if(phase==Phase.Park)Park();else if(phase==Phase.Plate)ToPlate();else if(!Go(phase==Phase.Stage?(Interactable)pass:appliance))Wait("Destination unavailable — food retained");return;}
            if(!walker.Arrived)
            {if(!KitchenStaffRoute.Clear(transform.position)){Wait("Path blocked");return;}walker.Advance(seconds,day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed("cook"),hands.Item!=null);return;}
            if(!Near(target)){if(!Go(target))Wait("Path blocked");return;}
            transform.LookAt(new Vector3(target.transform.position.x,transform.position.y,target.transform.position.z));
            if(phase==Phase.Fetch)
            {
                bool got=selected!=null?Pickup(selected):target is SourceStation source && source.DispenseTo(this,hands,process.ingredient);
                if(!got){Idle();return;}job=hands.Item;
                if(job.Payload.isPlate || job.Payload.state==process.output){ToPlate();return;}
                phase=Phase.Load;Status="Loading "+appliance.stationName;if(!Go(appliance))Wait("Appliance path blocked");return;
            }
            if(phase==Phase.Load)
            {
                if(!appliance.StartHotBy(this,hands)){Wait("Appliance occupied — food retained");return;}
                phase=Phase.Cooking;Status="Cooking "+ticket.Recipe.displayName;return;
            }
            if(phase==Phase.Cooking)
            {
                if(appliance.Busy)return;
                if(!hands.TryTake(job)){Idle();return;}ToPlate();return;
            }
            if(phase==Phase.Plate)
            {
                var plate=((SourceStation)target).TakeCleanPlate();if(plate==null){Wait("No clean plates — food retained");return;}
                plate.Payload.AddFood(job.Payload);hands.Release();Destroy(job.gameObject);plate.RefreshVisual();hands.TryTake(plate);job=plate;
                phase=Phase.Stage;Status="Taking dish to pass";Go(pass);return;
            }
            if(phase==Phase.Stage)
            {
                if(!pass.pickupSlot.TryTake(job)){Wait("Serving counter full — holding dish");return;}
                if(!day.Expo.TryStageFor(ticket,job))day.Expo.TryStage(job);pass.ShowSuccess();Idle();return;
            }
            if(phase==Phase.Park)
            {
                bool stored=target is PrepBin bin?bin.Store(job):target.GetType()==typeof(CounterStation) && ((CounterStation)target).slot.TryTake(job);
                if(stored){Idle();return;}Park();
            }
        }
    }
}
