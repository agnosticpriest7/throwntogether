using UnityEngine;
namespace ThrownTogether
{
    // Dining-only waypoints; no changes to player collision or movement.
    public sealed class DiningWalker : MonoBehaviour
    {
        Vector3[] route=new Vector3[0];int waypoint;ChefAppearance appearance;float clock;
        StaffCongestion congestion;
        public bool Arrived=>waypoint>=route.Length;
        public Vector3 Destination=>route.Length==0?transform.position:route[route.Length-1];
        public bool Moving {get;private set;}
        public void Initialize(GameObject prefab,int look,bool customer=false)
        {
            if(!customer)congestion=GetComponent<StaffCongestion>()??gameObject.AddComponent<StaffCongestion>();
            var visual=Instantiate(prefab,transform);appearance=visual.GetComponent<ChefAppearance>()??visual.GetComponentInChildren<ChefAppearance>();
            if(customer)visual.transform.localScale=Vector3.one*.85f;
            if(appearance!=null){appearance.enabled=false;appearance.usePlayerSelection=false;if(customer)CustomerPresentation.ApplyCustomerLook(appearance,look);else appearance.Apply(ChefAppearanceData.Example(look));}
        }
        public void Go(params Vector3[] points){route=points;waypoint=0;}
        public void Advance(float seconds,float speed,bool carrying=false)
        {
            Vector3 before=transform.position;
            Vector3 heading=Vector3.zero;
            for(int i=waypoint;i<route.Length;i++)if((route[i]-transform.position).sqrMagnitude>.0001f){heading=route[i]-transform.position;break;}
            float distance=Mathf.Max(0,seconds)*speed*(congestion!=null?congestion.Factor(heading):1);
            while(!Arrived && distance>0)
            {
                Vector3 delta=route[waypoint]-transform.position;float length=delta.magnitude;
                if(length>.001f)transform.rotation=Quaternion.LookRotation(delta.normalized,Vector3.up);
                if(length<=distance){transform.position=route[waypoint++];distance-=length;}
                else{transform.position+=delta.normalized*distance;distance=0;}
            }
            Moving=(transform.position-before).sqrMagnitude>.000001f;
            clock+=seconds;appearance?.Pose(carrying?1:0,Moving?Mathf.Sin(clock*8)*18:0,seconds);
        }
    }
}
