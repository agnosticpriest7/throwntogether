using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    // Starter role: transport matching prepared plates only. Cooking and clearing stay hands-on.
    public sealed class DiningServer : MonoBehaviour
    {
        RestaurantDay day;ServiceStation pass;DiningWalker walker;CarrySlot hands;DiningTable target;
        Vector3 Home=>new Vector3(day.Settings.diningAisleX,0,pass.transform.position.z);
        public void Initialize(RestaurantDay owner,ServiceStation station)
        {
            day=owner;pass=station;var go=new GameObject("Hired server");go.transform.SetParent(transform);go.transform.position=Home;
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,2);
            var grip=new GameObject("Carried plate");grip.transform.SetParent(go.transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
        }
        public void Advance(float seconds)
        {
            if(day.Closed)return;
            walker.Advance(seconds,day.Settings.walkingSpeed,hands.Item!=null);
            if(!walker.Arrived)return;
            if(target!=null)
            {
                // A player may have served this table while the server was walking.
                if(hands.Item!=null && !target.Deliver(hands.Item))
                {target.ReservedForServer=false;target=null;walker.Go(Home);return;}
                target.ReservedForServer=false;target=null;walker.Go(Home);return;
            }
            var dish=hands.Item??pass.pickupSlot.Item;if(dish==null)return;
            target=day.Tables.FirstOrDefault(t=>!t.ReservedForServer && t.CanServe(dish.Payload));
            if(target==null)return;
            target.ReservedForServer=true;hands.TryTake(dish);
            walker.Go(new Vector3(day.Settings.diningAisleX,0,target.transform.position.z));
        }
    }
}
