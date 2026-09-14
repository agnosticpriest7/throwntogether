using System;
using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // One physical prep specialist. Uses normal carry slots, source stock and station
    // processing; never manufactures a prepared portion or moves food remotely.
    public sealed class KitchenPrepCook : MonoBehaviour
    {
        readonly Guid workerId=Guid.NewGuid();
        RestaurantDay day;DiningWalker walker;CarrySlot hands;PrepProduction production;
        PrepWorkClaim claim;IngredientDefinition ingredient;ProcessingStation station;
        SourceStation source;Interactable destination;Carryable jobItem;float retry;
        enum Work {Idle,Fetch,ToPrep,Prep,Deposit}
        Work phase;
        PrepBin restockBin;bool restockJob;int nextBin;
        public string Status {get;private set;}="Waiting for fired orders";
        public Carryable ReservedItem=>claim!=null?jobItem:null;
        public CarrySlot Hands=>hands;
        public PrepProduction Production=>production;
        public void Initialize(RestaurantDay owner)
        {
            day=owner;production=new PrepProduction(owner);
            var board=FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None).FirstOrDefault(p=>p.gameObject.scene==gameObject.scene && p.requiresAttendance);
            var start=board!=null?KitchenStaffRoute.Approach(board.transform):null;
            if(start==null){Status="No reachable prep station";enabled=false;return;}
            transform.position=start.Value;
            walker=gameObject.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,3);
            var grip=new GameObject("Prep cook hands");grip.transform.SetParent(transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
        }
        bool Near(Interactable target)=>target!=null && Vector3.Distance(transform.position,target.transform.position)<=1.85f;
        bool Go(Interactable target)
        {
            if(target==null || !target.isActiveAndEnabled)return false;
            var route=KitchenStaffRoute.ToStation(transform.position,target.transform);if(route==null)return false;
            destination=target;walker.Go(route);return true;
        }
        void Wait(string status){Status=status;retry=.5f;}
        void Release()
        {
            // Make the physical object count before returning the reservation.
            var heldClaim=claim;claim=null;if(day!=null)production.Refresh();
            if(heldClaim!=null)production.Ledger.Release(workerId,heldClaim.Id);
        }
        void Idle()
        {station?.ReleaseStaff(this);Release();phase=Work.Idle;jobItem=null;destination=null;restockBin=null;restockJob=false;retry=.2f;}
        bool OutputExists(ItemPayload food)=>Outputs(food).Any();
        Interactable[] Outputs(ItemPayload food)
        {
            return Interactable.Active.Where(s=>s!=null && s.isActiveAndEnabled && s.gameObject.scene==gameObject.scene &&
                (s is PrepBin bin && bin.CanStore(food) || s.GetType()==typeof(CounterStation) && ((CounterStation)s).slot.Item==null))
                .OrderBy(s=>s is PrepBin?0:1).ThenBy(s=>Vector3.SqrMagnitude(s.transform.position-transform.position)).ToArray();
        }
        bool DepositRoute()
        {
            if(restockJob)
            {
                if(restockBin!=null && restockBin.CanStore(hands.Item.Payload) && Go(restockBin))return true;
                Wait("Assigned bin full, blocked or unavailable — food retained");return false;
            }
            foreach(var output in Outputs(hands.Item.Payload))if(Go(output))return true;
            Wait("Waiting for free counter or prep bin");return false;
        }
        void FindWork()
        {
            production.Refresh();var assignment=RestaurantAccounts.Current.PrepAssignment(SessionOptions.Kitchen);
            var boards=day.GetComponent<KitchenFurniture>().PrepStations
                .Where(p=>string.IsNullOrEmpty(assignment.station) || p.Key==assignment.station).Select(p=>p.Value).ToArray();
            if(boards.Length==0){Wait("Assigned prep station missing — update assignment");return;}
            if(assignment.restock)
            {
                var bins=day.GetComponent<KitchenFurniture>().PrepBins;
                var ingredients=DailyMenu.Catalog.SelectMany(r=>r.steps).Where(s=>s!=null && s.input==FoodState.Raw && s.output==FoodState.Cut).Select(s=>s.ingredient).Where(i=>i!=null && i.Unlocked).Distinct().ToArray();
                for(int n=0;n<assignment.bins.Length;n++)
                {
                    int index=(nextBin+n)%assignment.bins.Length;var assigned=assignment.bins[index];
                    var target=bins.FirstOrDefault(b=>b.Key==assigned.bin).Value;
                    var wanted=ingredients.FirstOrDefault(i=>i.id==assigned.ingredient);
                    if(target==null || !target.isActiveAndEnabled || wanted==null || !target.CanStore(new ItemPayload{ingredient=wanted,state=FoodState.Cut}))continue;
                    station=boards.FirstOrDefault(p=>p.isActiveAndEnabled && !p.Busy && p.slot.Item==null && p.SupportsPrep(wanted) && KitchenStaffRoute.ToStation(transform.position,p.transform)!=null);
                    source=FindObjectsByType<SourceStation>(FindObjectsSortMode.None).FirstOrDefault(s=>s.gameObject.scene==gameObject.scene && s.Offers(wanted) && KitchenStaffRoute.ToStation(transform.position,s.transform)!=null);
                    if(station==null || source==null || KitchenStaffRoute.ToStation(transform.position,target.transform)==null)continue;
                    ingredient=wanted;restockJob=true;restockBin=target;nextBin=(index+1)%assignment.bins.Length;
                    if(!Go(source)){Idle();continue;}
                    phase=Work.Fetch;Status="Restocking "+ingredient.displayName;return;
                }
                Wait(assignment.bins.Length==0?"Assign ingredients to prep bins in Employees":"Assigned bins stocked or blocked — waiting");return;
            }
            foreach(var wanted in production.Needed)
            {
                if(!string.IsNullOrEmpty(assignment.ingredient) && wanted.id!=assignment.ingredient || production.Ledger.Available(wanted.id)==0)continue;
                var preview=new ItemPayload{ingredient=wanted,state=FoodState.Cut};
                if(!OutputExists(preview)){Wait("Waiting for free counter or prep bin");return;}
                station=boards.FirstOrDefault(p=>p.isActiveAndEnabled && !p.Busy && p.slot.Item==null && p.SupportsPrep(wanted) && KitchenStaffRoute.ToStation(transform.position,p.transform)!=null);
                if(station==null){Wait("Waiting for assigned prep station");return;}
                source=FindObjectsByType<SourceStation>(FindObjectsSortMode.None).FirstOrDefault(s=>s.gameObject.scene==gameObject.scene && s.Offers(wanted) && wanted.Unlocked && KitchenStaffRoute.ToStation(transform.position,s.transform)!=null);
                if(source==null){Wait("Ingredient storage blocked or unavailable");return;}
                claim=production.Ledger.TryClaim(workerId,wanted.id);if(claim==null)continue;
                ingredient=wanted;if(!Go(source)){Idle();Wait("Path to ingredient storage blocked");return;}
                phase=Work.Fetch;Status="Fetching "+ingredient.displayName;return;
            }
            Wait("Waiting for fired orders / needed ingredients");
        }
        public void Advance(float seconds)
        {
            if(day==null || day.Closed || day.AwaitingMenu || walker==null || !isActiveAndEnabled || seconds<=0)return;
            if(retry>0){retry-=seconds;return;}
            float speed=RestaurantAccounts.Current.StaffSpeed("prep-cook");
            if(phase==Work.Idle){FindWork();return;}
            if(phase!=Work.Fetch && jobItem==null){Idle();return;}
            if(phase==Work.Fetch)
            {
                production.Refresh();
                if(restockJob ? restockBin==null || !restockBin.CanStore(new ItemPayload{ingredient=ingredient,state=FoodState.Cut}) : production.Deficit(ingredient.id)==0){Idle();return;}
            }
            if(destination==null || !destination.isActiveAndEnabled)
            {
                if(hands.Item==null){Idle();return;}
                if(hands.Item.Payload.state==FoodState.Cut){phase=Work.Deposit;DepositRoute();return;}
                Wait("Assigned station unavailable — food retained");return;
            }
            // Recheck the static route while traveling; layout edits remain between days.
            if(!walker.Arrived)
            {
                if(!KitchenStaffRoute.Clear(transform.position)){Wait("Path blocked — waiting");return;}
                walker.Advance(seconds,day.Settings.walkingSpeed*speed,hands.Item!=null);return;
            }
            if(!Near(destination)){if(!Go(destination))Wait("Path blocked — waiting");return;}
            transform.LookAt(new Vector3(destination.transform.position.x,transform.position.y,destination.transform.position.z));
            if(phase==Work.Fetch)
            {
                if(station==null || station.Busy || station.slot.Item!=null){Idle();return;}
                if(!source.DispenseTo(this,hands,ingredient)){Idle();return;}
                jobItem=hands.Item;phase=Work.ToPrep;Status="Taking "+ingredient.displayName+" to prep";
                if(!Go(station))Wait("Path to prep blocked — food retained");return;
            }
            if(phase==Work.ToPrep)
            {
                if(hands.Item!=jobItem){Idle();return;}
                if(!Near(station)){if(!Go(station))Wait("Path to prep blocked — food retained");return;}
                if(!station.StartBy(this,hands)){Wait("Waiting for prep station — food retained");return;}
                phase=Work.Prep;Status="Preparing "+ingredient.displayName;return;
            }
            if(phase==Work.Prep)
            {
                if(station.slot.Item!=jobItem){Idle();return;}
                station.WorkBy(this,seconds*speed);
                if(station.Busy)return;
                if(!hands.TryTake(jobItem)){Idle();return;}
                phase=Work.Deposit;Status="Storing "+ingredient.NameFor(FoodState.Cut);DepositRoute();return;
            }
            if(phase==Work.Deposit)
            {
                if(hands.Item!=jobItem){Idle();return;}
                if(restockJob && destination!=restockBin){DepositRoute();return;}
                bool stored=destination is PrepBin bin?bin.Store(jobItem):destination.GetType()==typeof(CounterStation) && ((CounterStation)destination).slot.TryTake(jobItem);
                if(stored){destination.ShowSuccess();Idle();return;}
                DepositRoute();
            }
        }
        void OnDisable(){station?.ReleaseStaff(this);if(production!=null)Release();}
    }
}
