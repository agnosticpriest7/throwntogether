using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class DiningServer : MonoBehaviour
    {
        enum Destination { Idle, Pass, Table }
        RestaurantDay day;ServiceStation pass;DiningWalker walker;CarrySlot hands;DiningTable target;Destination destination;
        Vector3 Home=>day.Settings.serverIdle;
        Vector3 Pickup=>KitchenStaffRoute.Approach(pass.transform)??Home;
        void Travel(Destination next,Vector3 point){destination=next;var path=next==Destination.Pass?KitchenStaffRoute.ToStation(walker.transform.position,pass.transform):KitchenStaffRoute.ToPoint(walker.transform.position,point);if(path!=null)walker.Go(path);}
        public void Initialize(RestaurantDay owner,ServiceStation station)
        {
            day=owner;pass=station;var go=new GameObject("Hired server");go.transform.SetParent(transform);go.transform.position=Home;
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,2);
            var grip=new GameObject("Carried plate");grip.transform.SetParent(go.transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
        }
        public void Advance(float seconds)
        {
            if(day.Closed)return;
            walker.Advance(seconds,day.Settings.walkingSpeed,hands.Item!=null);if(!walker.Arrived)return;
            if(destination==Destination.Table)
            {
                if(hands.Item!=null)target.Deliver(hands.Item);
                target.ReservedForServer=false;target=null;
                Travel(hands.Item!=null?Destination.Pass:Destination.Idle,hands.Item!=null?Pickup:Home);return;
            }
            if(destination==Destination.Idle)
            {
                var waiting=pass.pickupSlot.Item;
                if(waiting!=null && day.Tables.Any(t=>!t.ReservedForServer && t.CanServe(waiting.Payload)))Travel(Destination.Pass,Pickup);
                return;
            }
            // Only inspect/transfer the pass item after physically reaching the pass.
            var dish=hands.Item??pass.pickupSlot.Item;
            target=dish==null?null:day.Tables.FirstOrDefault(t=>!t.ReservedForServer && t.CanServe(dish.Payload));
            if(target==null)
            {
                if(hands.Item!=null && !pass.pickupSlot.TryTake(hands.Item))return;
                Travel(Destination.Idle,Home);return;
            }
            if(hands.Item==null && !hands.TryTake(dish)){target=null;Travel(Destination.Idle,Home);return;}
            target.ReservedForServer=true;Travel(Destination.Table,day.TableApproach(target));
        }
    }
}
