using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class DiningBusser : MonoBehaviour
    {
        RestaurantDay day;DishReturnStation rack;DiningWalker walker;CarrySlot hands;DiningTable target;bool returning;
        Vector3 Home=>day.Settings.busserIdle;
        Vector3 DropOff=>KitchenStaffRoute.Approach(rack.transform)??Home;
        void Travel(Vector3 point){var path=returning?KitchenStaffRoute.ToStation(walker.transform.position,rack.transform):KitchenStaffRoute.ToPoint(walker.transform.position,point);if(path!=null)walker.Go(path);}
        public void Initialize(RestaurantDay owner)
        {
            day=owner;rack=FindObjectsByType<DishReturnStation>().First(s=>s.gameObject.scene==gameObject.scene);
            var go=new GameObject("Hired busser");go.transform.SetParent(transform);go.transform.position=Home;
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,1);
            var grip=new GameObject("Carried plate");grip.transform.SetParent(go.transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();
        }
        bool FindTask()
        {target=day.Tables.FirstOrDefault(t=>t.order.tableSlot.Item?.Payload.dirty==true);if(target==null)return false;Travel(day.TableApproach(target));return true;}
        public void Advance(float seconds)
        {
            if(day.Closed || walker==null)return;
            if(target==null && !returning)FindTask();
            walker.Advance(seconds,day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed(day.Settings.busserRole.id),hands.Item!=null);if(!walker.Arrived)return;
            if(target!=null)
            {target.TakeDirty(hands);target=null;returning=hands.Item!=null;Travel(returning?DropOff:Home);return;}
            if(returning){if(hands.Item!=null)rack.Return(hands.Item);returning=false;if(!FindTask())Travel(Home);return;}
            target=day.Tables.FirstOrDefault(t=>t.order.tableSlot.Item?.Payload.dirty==true);
            if(target!=null)Travel(day.TableApproach(target));
        }
    }
}
