using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class DiningBusser : MonoBehaviour
    {
        RestaurantDay day;DishReturnStation rack;DiningWalker walker;CarrySlot hands;DiningTable target;
        Vector3 Home=>new Vector3(day.Settings.diningAisleX,0,rack.transform.position.z);
        public void Initialize(RestaurantDay owner)
        {
            day=owner;rack=FindObjectsByType<DishReturnStation>().First(s=>s.gameObject.scene==gameObject.scene);
            var go=new GameObject("Hired busser");go.transform.SetParent(transform);go.transform.position=Home;
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,1);
            var grip=new GameObject("Carried plate");grip.transform.SetParent(go.transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
        }
        public void Advance(float seconds)
        {
            if(day.Closed || walker==null)return;
            walker.Advance(seconds,day.Settings.walkingSpeed,hands.Item!=null);if(!walker.Arrived)return;
            if(target!=null)
            {target.TakeDirty(hands);target=null;walker.Go(day.DiningRoute(walker.transform.position,Home));return;}
            if(hands.Item!=null){rack.Return(hands.Item);return;}
            target=day.Tables.FirstOrDefault(t=>t.order.tableSlot.Item?.Payload.dirty==true);
            if(target!=null)walker.Go(day.DiningRoute(walker.transform.position,day.TableApproach(target)));
        }
    }
}
