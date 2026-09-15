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
        Vector3 Home=>day.GetComponent<KitchenFurniture>().Homes.Home("server");
        Vector3 Pickup=>KitchenStaffRoute.Approach(pass.transform)??Home;
        void Travel(Destination next,Vector3 point){destination=next;var path=next==Destination.Pass?KitchenStaffRoute.ToStation(walker.transform.position,pass.transform):KitchenStaffRoute.ToPoint(walker.transform.position,point);if(path!=null)walker.Go(path);}
        public void Initialize(RestaurantDay owner,ServiceStation station)
        {
            day=owner;pass=station;var go=new GameObject("Hired server");go.transform.SetParent(transform);go.transform.position=Home;
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,2);
            var grip=new GameObject("Carried plate");grip.transform.SetParent(go.transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
            member=StaffMember.Create(day,"server",walker,hands);
        }
        bool HasWork()=>pass.pickupSlot.Item!=null && (day.Expo!=null?day.Expo.CanCollect(pass.pickupSlot.Item):day.Tables.Any(t=>!t.ReservedForServer && t.CanServe(pass.pickupSlot.Item.Payload)));
        private void OnDestroy(){day?.Expo?.ReleaseClaim(claim);}
        public void Advance(float seconds)
        {
            if(day.Closed)return;
            if(!member.AllowWork(seconds))return;
            if(destination==Destination.Idle && !HasWork()){member.Idle(seconds);return;}
            if(destination==Destination.Idle && HasWork())Travel(Destination.Pass,Pickup);
            walker.Advance(seconds,day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed(day.Settings.serverRole.id),hands.Item!=null);if(!walker.Arrived)return;
            if(destination==Destination.Table)
            {
                if(hands.Item!=null && target!=null && (day.Expo==null || claim!=null))target.Deliver(hands.Item,claim);
                if(day.Expo!=null)day.Expo.ReleaseClaim(claim);else if(target!=null)target.ReservedForServer=false;
                claim=null;target=null;
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
