using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // One physical cook. Completed components are assembled on ordinary counters.
    // The projection can be stale; every transfer checks the real item/slot again.
    public sealed class KitchenCook : MonoBehaviour
    {
        enum Phase { Idle, Fetch, Load, Cooking, Plate, Assemble, SetDown, Swap, Stage, Park }
        RestaurantDay day;DiningWalker walker;CarrySlot hands;ServiceStation pass;
        KitchenTicket ticket;CookProduction.Component component;ProcessingStation appliance;
        Carryable selected,job,assemblyPlate;Interactable target;Phase phase;float retry;
        int clearing;
        CounterStation clearingCounter;
        Carryable parkedInput;
        StaffMember member;
        public string Status {get;private set;}="Waiting for fired orders";
        public string DisplayStatus=>member?.DisplayStatus??Status;
        public CarrySlot Hands=>hands;
        public KitchenTicket CurrentTicket=>ticket;
        public static ProcessingRecipe HotStep(RecipeDefinition recipe)=>recipe==null || recipe.additionalIngredients.Length!=0?null:CookProduction.HotFor(recipe,recipe.ingredient,recipe.requiredState);
        public void Initialize(RestaurantDay owner)
        {
            day=owner;pass=FindObjectsByType<ServiceStation>(FindObjectsSortMode.None).First(s=>s.gameObject.scene==gameObject.scene);
            transform.position=KitchenStaffRoute.Approach(pass.transform)??Vector3.zero;
            walker=gameObject.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,1);
            var grip=new GameObject("Cook hands");grip.transform.SetParent(transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
            member=StaffMember.Create(day,"cook",walker,hands);
            member.ConfigureBreaks(()=>Status);
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
        void Idle(){phase=Phase.Idle;ticket=null;component=null;job=null;selected=null;assemblyPlate=null;target=null;retry=.15f;Status="Waiting for fired orders";}
        void FinishBreak(){Idle();if(member.BreakRequested)member.BeginBreak();}
        static Interactable Location(Carryable i)=>CookProduction.Location(i);
        bool Reachable(Interactable s)=>s!=null && s.isActiveAndEnabled && KitchenStaffRoute.ToStation(transform.position,s.transform)!=null;
        bool CanCollect(Carryable item)=>item!=null && Location(item)!=null && !(Location(item) is ProcessingStation p && p.Busy) && !(day.PrepCook!=null && day.PrepCook.ReservedItem==item) && Reachable(Location(item));
        bool Pickup(Carryable item)
        {
            var location=Location(item);if(!CanCollect(item) || !Near(location))return false;
            if(location is PrepBin bin)return bin.Take(hands);
            return hands.TryTake(item);
        }
        CookProduction.Order Plan(Carryable excluded=null)=>CookProduction.Snapshot(day,excluded).FirstOrDefault(o=>o.Ticket==ticket);
        bool CoveredByPlayerWork()
        {
            if(component==null)return false;
            var plan=Plan(job);var c=plan?.Components.FirstOrDefault(p=>p.Ingredient==component.Ingredient && p.State==component.State);
            return c!=null && (c.OnPlate || c.Supply!=null && c.Supply!=selected);
        }
        CounterStation FreeCounter()=>Interactable.Active.Where(s=>s!=null && s.gameObject.scene==gameObject.scene && s.GetType()==typeof(CounterStation)).Cast<CounterStation>().Where(s=>s.slot.Item==null && Reachable(s)).OrderBy(s=>(s.transform.position-transform.position).sqrMagnitude).FirstOrDefault();
        void BreakPark()
        {
            if(hands.Item==null){FinishBreak();return;}
            job=hands.Item;
            if(ticket!=null && ticket.Recipe.Matches(job.Payload) && pass.pickupSlot.Item==null && Reachable(pass))
            {phase=Phase.Stage;Go(pass);Status="Finishing: taking completed meal to pass";return;}
            phase=Phase.Park;var counter=FreeCounter();
            if(counter!=null && Go(counter)){Status="Finishing: putting food on counter";return;}
            target=null;Wait("Finishing: needs free counter — food retained");
        }
        void FindWork()
        {
            if(day.Expo==null || !day.HasInstalledExpo){Wait("Requires an installed Expo desk");return;}
            var plans=CookProduction.Snapshot(day);
            var allocated=plans.SelectMany(p=>p.Components).Where(c=>c.Supply!=null).Select(c=>c.Supply).ToHashSet();
            string reason="Waiting for fired orders";
            foreach(var order in plans.Where(o=>o.Ticket.State==KitchenTicketState.Active))
            {
                if(order.Plate!=null && order.Ticket.Recipe.Matches(order.Plate.Payload))
                {
                    if(!CanCollect(order.Plate)){reason="Completed dish with player / server";continue;}
                    ticket=order.Ticket;selected=order.Plate;component=null;Go(Location(selected));phase=Phase.Fetch;Status="Collecting "+ticket.Recipe.displayName;return;
                }
                if(order.Plate!=null && !CanCollect(order.Plate)){reason="Assembly plate with player or path blocked";continue;}
                // With no empty counter, use a stocked component first so pickup
                // frees the very surface needed for assembling the meal.
                bool noCounter=order.Plate==null && FreeCounter()==null;
                foreach(var c in order.Components.Where(c=>!c.OnPlate).OrderBy(c=>noCounter && c.Hot?.input==FoodState.Raw?1:0))
                {
                    if(c.Supply!=null)
                    {
                        if(!CanCollect(c.Supply)){reason=Location(c.Supply) is ProcessingStation p && p.Busy?"Waiting for "+c.Ingredient.NameFor(c.State):"Component with player / prep worker";continue;}
                        ticket=order.Ticket;component=c;selected=c.Supply;Go(Location(selected));phase=Phase.Fetch;Status="Collecting "+c.Ingredient.NameFor(c.State);return;
                    }
                    if(c.Hot==null){reason="Needs "+c.Ingredient.NameFor(c.State);continue;}
                    var compatible=FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None).Where(p=>p.gameObject.scene==gameObject.scene && p.isActiveAndEnabled && !p.requiresAttendance && p.ProcessFor(new ItemPayload{ingredient=c.Ingredient,state=c.Hot.input})==c.Hot).ToArray();
                    var free=compatible.FirstOrDefault(p=>!p.Busy && p.slot.Item==null && Reachable(p));
                    if(free==null && FreeCounter()!=null)free=compatible.FirstOrDefault(p=>!p.Busy && p.slot.Item!=null && Reachable(p));
                    if(free==null){reason=compatible.Length==0?"Needs appliance for "+c.Ingredient.NameFor(c.State):"Appliance busy or path blocked";continue;}
                    var input=FindObjectsByType<Carryable>(FindObjectsSortMode.InstanceID).FirstOrDefault(i=>i.gameObject.scene==gameObject.scene && !allocated.Contains(i) && CookProduction.Plain(i,c.Ingredient,c.Hot.input) && CanCollect(i));
                    Interactable pickup=input!=null?Location(input):c.Hot.input==FoodState.Raw?FindObjectsByType<SourceStation>(FindObjectsSortMode.None).FirstOrDefault(s=>s.gameObject.scene==gameObject.scene && s.Offers(c.Ingredient) && c.Ingredient.Unlocked && Reachable(s)):null;
                    if(pickup==null){reason="Needs "+c.Ingredient.NameFor(c.Hot.input);continue;}
                    ticket=order.Ticket;component=c;appliance=free;selected=input;Go(pickup);phase=Phase.Fetch;Status="Collecting "+c.Ingredient.NameFor(c.Hot.input);return;
                }
            }
            Wait(reason);
        }
        void Park()
        {
            if(member.BreakRequested){BreakPark();return;}
            // Hold may be lifted while every output surface remains occupied.
            // Resume only a still-needed finished portion, never raw or duplicate work.
            if(Fired && job!=null && hands.Item==job && component!=null &&
                CookProduction.Plain(job,component.Ingredient,component.State) && !CoveredByPlayerWork())
            {ToPlate();return;}
            phase=Phase.Park;
            foreach(var s in Interactable.Active.Where(s=>s!=null && s.gameObject.scene==gameObject.scene &&
                (s is PrepBin b && b.CanStore(hands.Item.Payload) || s.GetType()==typeof(CounterStation) && ((CounterStation)s).slot.Item==null)))if(Go(s)){Status="Storing unused component";return;}
            target=null;Wait("Needs free counter — food retained");
        }
        void SetDown()
        {
            phase=Phase.SetDown;
            if(!Go(FreeCounter()) && !SwapRoute())Wait("Needs free assembly counter — plate retained");
            else Status="Setting down assembly plate";
        }
        bool SwapRoute()
        {
            if(!Fired)return false;
            var plans=CookProduction.Snapshot(day);var plan=plans.FirstOrDefault(p=>p.Ticket==ticket);
            if(plan==null || plan.Plate!=hands.Item)return false;
            var reserved=plans.Where(p=>p!=plan).SelectMany(p=>p.Components).Select(c=>c.Supply).Where(i=>i!=null).ToHashSet();
            foreach(var c in plan.Components.Where(c=>!c.OnPlate))
            foreach(var counter in Interactable.Active.Where(s=>s!=null && s.gameObject.scene==gameObject.scene && s.GetType()==typeof(CounterStation)).Cast<CounterStation>())
            {
                var input=counter.slot.Item;if(input==null || reserved.Contains(input) || !CanCollect(input))continue;
                bool ready=CookProduction.Plain(input,c.Ingredient,c.State);
                if(!ready && (c.Hot==null || !CookProduction.Plain(input,c.Ingredient,c.Hot.input)))continue;
                var free=ready?null:FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None).FirstOrDefault(p=>p.gameObject.scene==gameObject.scene && !p.requiresAttendance && !p.Busy && p.slot.Item==null && p.ProcessFor(input.Payload)==c.Hot && Reachable(p));
                if(!ready && free==null)continue;
                component=c;selected=input;appliance=free;phase=Phase.Swap;Go(counter);return true;
            }
            return false;
        }
        void ToPlate()
        {
            if(hands.Item.Payload.isPlate)
            {
                if(ticket.Recipe.Matches(hands.Item.Payload)){phase=Phase.Stage;Status="Taking "+ticket.Recipe.displayName+" to pass";if(!Go(pass))Wait("Pass path blocked");}
                else SetDown();
                return;
            }
            // Re-read the shared plate every time. A player may have moved, finished,
            // or taken it while this component was cooking.
            var plan=Plan(job);assemblyPlate=plan?.Plate;
            if(assemblyPlate!=null)
            {
                if(assemblyPlate.Payload.Contains(component.Ingredient,component.State)){Park();return;}
                phase=Phase.Assemble;
                if(!CanCollect(assemblyPlate) || Location(assemblyPlate)?.GetType()!=typeof(CounterStation)){target=null;Wait("Assembly plate with player — component retained");return;}
                Go(Location(assemblyPlate));Status="Assembling "+ticket.Recipe.displayName;return;
            }
            if(ticket.Recipe.additionalIngredients.Length>0 && !Fired){Park();return;}
            phase=Phase.Plate;var stock=FindObjectsByType<SourceStation>(FindObjectsSortMode.None).FirstOrDefault(s=>s.gameObject.scene==gameObject.scene && s.plates && Reachable(s));
            if(!Go(stock))Wait("Plate stack path blocked — food retained");
            else Status="Plating "+component.Ingredient.NameFor(component.State);
        }
        bool BeginClearing()
        {
            if(appliance==null || appliance.Busy || appliance.slot.Item==null || hands.Item!=job)return false;
            clearingCounter=FreeCounter();if(clearingCounter==null)return false;
            parkedInput=job;clearing=1;Go(clearingCounter);Status="Making room on appliance";return true;
        }
        void ClearAppliance(float seconds)
        {
            // Only real, nearby slot transfers. The buffer is shared with players, so
            // every leg revalidates it before exchanging anything.
            if(target==null || !target.isActiveAndEnabled)
            {clearing=0;if(hands.Item!=null){job=hands.Item;component=null;Park();}else Idle();return;}
            if(!walker.Arrived){if(!KitchenStaffRoute.Clear(transform.position)){Wait("Clearing route blocked — food retained");return;}walker.Advance(seconds,day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed("cook"),hands.Item!=null);return;}
            if(!Near(target)){if(!Go(target))Wait("Clearing route blocked — food retained");return;}
            if(clearing==1)
            {
                if(hands.Item!=parkedInput){clearing=0;Idle();return;}
                if(!clearingCounter.slot.TryTake(parkedInput)){clearing=0;phase=Phase.Load;Go(appliance);return;}
                if(member.BreakRequested){clearing=0;FinishBreak();return;}
                clearing=2;Go(appliance);return;
            }
            if(clearing==2)
            {
                if(clearingCounter==null || clearingCounter.slot.Item!=parkedInput){clearing=0;Idle();return;}
                if(member.BreakRequested){clearing=0;FinishBreak();return;}
                if(appliance.Busy){Wait("Waiting to clear finished food");return;}
                if(appliance.slot.Item!=null && !Pickup(appliance.slot.Item)){Wait("Finished food unavailable");return;}
                clearing=3;Go(clearingCounter);return;
            }
            if(clearingCounter.slot.Item==parkedInput)
            {
                if(member.BreakRequested){clearing=0;job=hands.Item;BreakPark();return;}
                var output=hands.Release();var input=clearingCounter.slot.Release();
                if(output!=null)clearingCounter.slot.TryTake(output);
                hands.TryTake(input);job=input;clearing=0;phase=Phase.Load;Go(appliance);Status="Resuming cooking";return;
            }
            // A player took/replaced the waiting input. Preserve the output and re-plan.
            clearing=0;
            if(hands.Item==null){Idle();return;}
            if(clearingCounter.slot.Item==null && clearingCounter.slot.TryTake(hands.Item)){Idle();return;}
            job=hands.Item;component=null;Park();
        }
        public void Advance(float seconds)
        {
            if(day==null || day.Closed || day.AwaitingMenu || !isActiveAndEnabled || seconds<=0)return;
            if(!member.AllowWork(seconds))return;
            if(member.BreakRequested && clearing==0)
            {
                if(phase==Phase.Idle || phase==Phase.Fetch && hands.Item==null){FinishBreak();return;}
                if(phase!=Phase.Cooking && phase!=Phase.Park && phase!=Phase.Stage)BreakPark();
            }
            if(phase==Phase.Idle && hands.Item==null)member.Idle(seconds);
            if(retry>0){retry-=seconds;return;}
            if(clearing>0){ClearAppliance(seconds);return;}
            if(phase==Phase.Idle){FindWork();return;}
            if(phase==Phase.Fetch && !Fired){Idle();return;}
            if(phase==Phase.Load && !Fired && hands.Item!=null){Park();return;}
            if(phase!=Phase.Fetch && (job==null || job.Owner!=hands && (appliance==null || job.Owner!=appliance.slot))){Idle();return;}
            if(target==null || !target.isActiveAndEnabled)
            {
                if(hands.Item==null){Idle();return;}
                if(phase==Phase.Park)Park();else if(phase==Phase.SetDown || phase==Phase.Swap)SetDown();else if(phase==Phase.Plate || phase==Phase.Assemble)ToPlate();else if(!Go(phase==Phase.Stage?(Interactable)pass:appliance))Wait("Destination unavailable — food retained");return;
            }
            if(!walker.Arrived)
            {if(!KitchenStaffRoute.Clear(transform.position)){Wait("Path blocked");return;}walker.Advance(seconds,day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed("cook"),hands.Item!=null);return;}
            if(!Near(target)){if(!Go(target))Wait("Path blocked");return;}
            transform.LookAt(new Vector3(target.transform.position.x,transform.position.y,target.transform.position.z));
            if(phase==Phase.Fetch)
            {
                if(CoveredByPlayerWork()){Idle();return;}
                if(selected!=null && (component==null?!ticket.Recipe.Matches(selected.Payload):
                    !CookProduction.Plain(selected,component.Ingredient,component.State) && (component.Hot==null || !CookProduction.Plain(selected,component.Ingredient,component.Hot.input)))){Idle();return;}
                bool got=selected!=null?Pickup(selected):target is SourceStation source && source.DispenseTo(this,hands,component.Ingredient);
                if(!got){Idle();return;}job=hands.Item;
                if(job.Payload.isPlate || job.Payload.state==component.State){ToPlate();return;}
                phase=Phase.Load;Status="Loading "+appliance.stationName;if(!Go(appliance))Wait("Appliance path blocked");return;
            }
            if(phase==Phase.Load)
            {
                if(CoveredByPlayerWork()){Park();return;}
                if(!appliance.StartHotBy(this,hands))
                {
                    var other=FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None).FirstOrDefault(p=>p!=appliance && p.gameObject.scene==gameObject.scene && !p.requiresAttendance && !p.Busy && p.slot.Item==null && p.ProcessFor(job.Payload)==component.Hot && Reachable(p));
                    if(other!=null){appliance=other;Go(other);Status="Using another available appliance";}
                    else if(!BeginClearing())Wait(appliance.Busy?"Appliance cooking — food retained":"Needs free counter to clear appliance — food retained");return;
                }
                phase=Phase.Cooking;Status="Cooking "+component.Ingredient.NameFor(component.State);return;
            }
            if(phase==Phase.Cooking)
            {
                if(appliance.Busy)return;
                if(!hands.TryTake(job)){Idle();return;}if(member.BreakRequested)BreakPark();else ToPlate();return;
            }
            if(phase==Phase.Plate)
            {
                if(Plan(job)?.Plate!=null){ToPlate();return;}
                if(ticket.Recipe.additionalIngredients.Length>0 && !Fired){Park();return;}
                var plate=((SourceStation)target).TakeCleanPlate();if(plate==null){Wait("No clean plates — food retained");return;}
                plate.Payload.AddFood(job.Payload);hands.Release();Destroy(job.gameObject);plate.RefreshVisual();hands.TryTake(plate);job=plate;ToPlate();return;
            }
            if(phase==Phase.Assemble)
            {
                if(assemblyPlate==null || Location(assemblyPlate)!=target || !ticket.Recipe.AcceptsPortions(assemblyPlate.Payload) || !ItemPayload.CanPlate(assemblyPlate.Payload,job.Payload)){ToPlate();return;}
                assemblyPlate.Payload.AddFood(job.Payload);hands.Release();Destroy(job.gameObject);assemblyPlate.RefreshVisual();target.ShowSuccess();
                if(ticket.Recipe.Matches(assemblyPlate.Payload)){hands.TryTake(assemblyPlate);job=assemblyPlate;ToPlate();}else Idle();return;
            }
            if(phase==Phase.SetDown)
            {
                if(target.GetType()==typeof(CounterStation) && ((CounterStation)target).slot.TryTake(job)){target.ShowSuccess();Idle();}else SetDown();return;
            }
            if(phase==Phase.Swap)
            {
                var counter=(CounterStation)target;
                if(!Fired || counter.slot.Item!=selected || !ticket.Recipe.AcceptsPortions(job.Payload) || job.Payload.Contains(component.Ingredient,component.State) ||
                    !CookProduction.Plain(selected,component.Ingredient,component.State) && (component.Hot==null || !CookProduction.Plain(selected,component.Ingredient,component.Hot.input))){SetDown();return;}
                // Both slots are validated and exchanged synchronously, never dropped.
                var plate=hands.Release();var input=counter.slot.Release();counter.slot.TryTake(plate);hands.TryTake(input);job=input;
                if(input.Payload.state==component.State)ToPlate();else{phase=Phase.Load;Go(appliance);Status="Loading next component";}return;
            }
            if(phase==Phase.Stage)
            {
                if(!ticket.Recipe.Matches(job.Payload)){Park();return;}
                if(!pass.pickupSlot.TryTake(job)){if(member.BreakRequested)BreakPark();else Wait("Serving counter full — holding dish");return;}
                if(!day.Expo.TryStageFor(ticket,job))day.Expo.TryStage(job);pass.ShowSuccess();if(member.BreakRequested)FinishBreak();else Idle();return;
            }
            if(phase==Phase.Park)
            {
                bool stored=target is PrepBin bin?bin.Store(job):target.GetType()==typeof(CounterStation) && ((CounterStation)target).slot.TryTake(job);
                if(stored){if(member.BreakRequested)FinishBreak();else Idle();return;}if(member.BreakRequested)BreakPark();else Park();
            }
        }
    }
}
