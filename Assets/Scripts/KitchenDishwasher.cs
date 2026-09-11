using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    // Small static kitchen route grid. Only employees use it; player motors are unchanged.
    public static class KitchenStaffRoute
    {
        const float Step=.4f, Radius=.29f;
        static Vector3 Point(Vector2Int p)=>new Vector3(p.x*Step,0,p.y*Step);
        static Vector2Int Cell(Vector3 p)=>new Vector2Int(Mathf.RoundToInt(p.x/Step),Mathf.RoundToInt(p.z/Step));
        public static bool Clear(Vector3 p)
        {
            return !Physics.OverlapCapsule(p+Vector3.up*.4f,p+Vector3.up*1.35f,Radius,~0,QueryTriggerInteraction.Ignore)
                .Any(c=>!(c is CharacterController) && c.GetComponentInParent<ChefController>()==null);
        }
        static bool Segment(Vector3 a,Vector3 b)
        {int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/.1f);for(int i=0;i<=steps;i++)if(!Clear(Vector3.Lerp(a,b,steps==0?0:(float)i/steps)))return false;return true;}
        public static Vector3[] ToStation(Vector3 from,Transform target)
            =>Search(from,p=>{var delta=target.position-p;delta.y=0;return delta.magnitude<=1.75f && Clear(p);});
        public static Vector3[] ToPoint(Vector3 from,Vector3 target)
        {
            var exact=new Vector3(target.x,0,target.z);var route=Search(from,p=>Vector3.Distance(p,exact)<.3f && Clear(p));
            if(route==null || !Segment(route[route.Length-1],exact))return null;
            return route.Concat(new[]{exact}).ToArray();
        }
        static Vector3[] Search(Vector3 from,System.Func<Vector3,bool> reached)
        {
            var start=Cell(from);var queue=new Queue<Vector2Int>();var parents=new Dictionary<Vector2Int,Vector2Int>();
            queue.Enqueue(start);parents[start]=start;
            var directions=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();var p=Point(cell);
                if(reached(p))
                {
                    var path=new List<Vector3>{p};while(cell!=start){cell=parents[cell];path.Add(Point(cell));}path.Reverse();
                    if(!Segment(from,path[0]))return null;return path.ToArray();
                }
                foreach(var d in directions)
                {
                    var next=cell+d;if(parents.ContainsKey(next)||next.x < -22 || next.x>32 || next.y < -13 || next.y>20)continue;
                    var q=Point(next);if(!Segment(p,q))continue;parents[next]=cell;queue.Enqueue(next);
                }
            }
            return null;
        }
        public static Vector3? Approach(Transform station)
        {
            for(int ring=3;ring<=4;ring++)foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            {var p=Point(Cell(station.position+direction*(ring*Step)));if(Clear(p))return p;}
            return null;
        }
    }
    // Shared ownership rules keep manual washing/clearing available alongside employees.
    public sealed class KitchenDishwasher : MonoBehaviour
    {
        RestaurantDay day;WashingStation sink;SourceStation stock;DishReturnStation rack;DiningWalker walker;CarrySlot hands;Transform destination;float retry;
        public void Initialize(RestaurantDay owner)
        {
            day=owner;sink=FindObjectsByType<WashingStation>().First(s=>s.gameObject.scene==gameObject.scene);
            stock=FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==gameObject.scene && s.plates);
            rack=FindObjectsByType<DishReturnStation>().First(s=>s.gameObject.scene==gameObject.scene);
            var start=KitchenStaffRoute.Approach(sink.transform);if(start==null){Debug.LogError("Dishwasher has no clear sink approach");enabled=false;return;}
            var go=new GameObject("Hired dishwasher");go.transform.SetParent(transform);go.transform.position=start.Value;
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,0);
            var appearance=go.GetComponentInChildren<ChefAppearance>();if(appearance!=null){var look=ChefAppearanceData.Example(0);look.clothing=2;look.clothingColor=4;look.headwear=0;appearance.Apply(look);}
            var grip=new GameObject("Carried plate");grip.transform.SetParent(go.transform,false);grip.transform.localPosition=new Vector3(0,1.25f,.65f);hands=grip.AddComponent<CarrySlot>();destination=sink.transform;
        }
        bool Travel(Transform target)
        {var route=KitchenStaffRoute.ToStation(walker.transform.position,target);if(route==null){retry=1;return false;}destination=target;walker.Go(route);return true;}
        public void Advance(float seconds)
        {
            if(day.Closed || walker==null || seconds<=0)return;
            if(retry>0){retry-=seconds;return;}
            float speed=RestaurantAccounts.Current.StaffSpeed(day.Settings.dishwasherRole.id);
            walker.Advance(seconds,day.Settings.walkingSpeed*speed,hands.Item!=null);if(!walker.Arrived)return;
            if(hands.Item!=null)
            {
                var target=hands.Item.Payload.dirty?sink.transform:stock.transform;
                if(destination!=target){Travel(target);return;}
                if(hands.Item.Payload.dirty){if(sink.Enqueue(hands.Item))Travel(sink.transform);}
                else if(stock.ReturnCleanPlate(hands.Item))Travel(sink.Count==0 && rack.Count>0?rack.transform:sink.transform);
                return;
            }
            if(destination==rack.transform)
            {rack.TakeDirty(hands);Travel(sink.transform);return;}
            if(destination!=sink.transform){Travel(sink.transform);return;}
            walker.transform.LookAt(new Vector3(sink.transform.position.x,walker.transform.position.y,sink.transform.position.z));
            if(sink.Count>0)
            {
                if(!sink.Busy){if(sink.TakeClean(hands))Travel(stock.transform);}
                else sink.WashBy(this,seconds*speed);
            }
            else if(rack.Count>0)Travel(rack.transform);
        }
        void OnDisable(){if(sink!=null)sink.ReleaseWorker(this);}
    }
}
