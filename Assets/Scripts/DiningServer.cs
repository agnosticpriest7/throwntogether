using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class DiningServer : MonoBehaviour
    {
        enum Destination { Idle, Pass, Table }
        RestaurantDay day;ServiceStation pass;DiningWalker walker;CarrySlot hands;DiningTable target;Destination destination;
        ExpoDeliveryClaim claim;
        StaffMember member;
        Interactable breakTarget;
        Vector3 Home=>day.GetComponent<KitchenFurniture>().Homes.Home("server");
        Vector3 Pickup=>KitchenStaffRoute.Approach(pass.transform)??Home;
        void Travel(Destination next,Vector3 point){destination=next;var path=next==Destination.Pass?KitchenStaffRoute.ToStation(walker.transform.position,pass.transform):KitchenStaffRoute.ToPoint(walker.transform.position,point);if(path!=null)walker.Go(path);}
        public void Initialize(RestaurantDay owner,ServiceStation station)
        {
            day=owner;pass=station;var go=new GameObject("Hired server");go.transform.SetParent(transform);go.transform.position=Home;
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,2);
            var grip=new GameObject("Carried plate");grip.transform.SetParent(go.transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
            member=StaffMember.Create(day,"server",walker,hands);
            member.ConfigureBreaks(()=>Status,OnBreakChanged);
        }
        void OnBreakChanged(bool requested)
        {
            if(requested)return;
            // Break storage and home travel may have replaced the role's old route.
            // Resume from the physical walker, retaining any carried meal.
            if(breakTarget!=null || destination==Destination.Idle)
            {
                breakTarget=null;
                if(hands.Item!=null)Travel(Destination.Pass,Pickup);
                else {destination=Destination.Idle;walker.Go(walker.transform.position);}
            }
        }
        string Status=>hands.Item!=null?"Delivering or storing carried meal":destination==Destination.Pass?"Going to serving counter":destination==Destination.Table?"Delivering meal":"Waiting for meals";
        void ReleaseDelivery()
        {
            if(day.Expo!=null)day.Expo.ReleaseClaim(claim);else if(target!=null)target.ReservedForServer=false;
            claim=null;target=null;
        }
        void FinishBreak(){ReleaseDelivery();destination=Destination.Idle;breakTarget=null;member.BeginBreak();}
        void StoreForBreak()
        {
            if(hands.Item==null){FinishBreak();return;}
            var origin=walker.transform.position;
            if(pass.pickupSlot.Item==null && KitchenStaffRoute.ToStation(origin,pass.transform)!=null)breakTarget=pass;
            else breakTarget=Interactable.Active.Where(s=>s!=null && s.gameObject.scene==gameObject.scene && s.GetType()==typeof(CounterStation)).Cast<CounterStation>().Where(c=>c.slot.Item==null && KitchenStaffRoute.ToStation(origin,c.transform)!=null).OrderBy(c=>(c.transform.position-origin).sqrMagnitude).FirstOrDefault();
            if(breakTarget==null)return;
            var route=KitchenStaffRoute.ToStation(origin,breakTarget.transform);if(route!=null)walker.Go(route);else breakTarget=null;
        }
        bool HasWork()=>pass.pickupSlot.Item!=null && (day.Expo!=null?day.Expo.CanCollect(pass.pickupSlot.Item):day.Tables.Any(t=>!t.ReservedForServer && t.CanServe(pass.pickupSlot.Item.Payload)));
        private void OnDestroy(){day?.Expo?.ReleaseClaim(claim);}
        public void Advance(float seconds)
        {
            if(day.Closed)return;
            if(!member.AllowWork(seconds))return;
            if(member.BreakRequested)
            {
                bool validDelivery=destination==Destination.Table && hands.Item!=null && target!=null && (day.Expo==null || claim!=null);
                if(!validDelivery)
                {
                    ReleaseDelivery();
                    if(hands.Item==null){FinishBreak();return;}
                    if(breakTarget==null)StoreForBreak();
                    if(breakTarget==null)return;
                    walker.Advance(seconds,day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed(day.Settings.serverRole.id),true);
                    if(!walker.Arrived)return;
                    if(Vector3.Distance(walker.transform.position,breakTarget.transform.position)>1.85f){breakTarget=null;return;}
                    bool stored=breakTarget==pass?pass.pickupSlot.TryTake(hands.Item):breakTarget is CounterStation counter && counter.slot.TryTake(hands.Item);
                    if(stored)FinishBreak();else{breakTarget=null;StoreForBreak();}
                    return;
                }
            }
            if(destination==Destination.Idle && !HasWork()){member.Idle(seconds);return;}
            if(destination==Destination.Idle && HasWork())Travel(Destination.Pass,Pickup);
            walker.Advance(seconds,day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed(day.Settings.serverRole.id),hands.Item!=null);if(!walker.Arrived)return;
            if(destination==Destination.Table)
            {
                if(hands.Item!=null && target!=null && (day.Expo==null || claim!=null))target.Deliver(hands.Item,claim);
                ReleaseDelivery();
                if(member.BreakRequested){FinishBreak();return;}
                bool more=hands.Item!=null || HasWork();Travel(more?Destination.Pass:Destination.Idle,more?Pickup:Home);return;
            }
            if(destination==Destination.Idle)
            {
                var waiting=pass.pickupSlot.Item;
                if(waiting!=null && HasWork())Travel(Destination.Pass,Pickup);
                return;
            }
            // Only inspect/transfer the pass item after physically reaching the pass.
            var dish=hands.Item??pass.pickupSlot.Item;
            if(day.Expo!=null){claim=day.Expo.TryClaim(dish,this);target=claim==null?null:day.Expo.TableFor(claim.Ticket);}
            else target=dish==null?null:day.Tables.FirstOrDefault(t=>!t.ReservedForServer && t.CanServe(dish.Payload));
            if(target==null)
            {
                if(hands.Item!=null && !pass.pickupSlot.TryTake(hands.Item))return;
                Travel(Destination.Idle,Home);return;
            }
            if(hands.Item==null && !hands.TryTake(dish)){day.Expo?.ReleaseClaim(claim);claim=null;target=null;Travel(Destination.Idle,Home);return;}
            if(day.Expo==null)target.ReservedForServer=true;Travel(Destination.Table,day.TableApproach(target));
        }
    }
}
