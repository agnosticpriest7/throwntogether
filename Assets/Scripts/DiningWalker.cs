using UnityEngine;
namespace ThrownTogether
{
    // Dining-only waypoints; no changes to player collision or movement.
    public sealed class DiningWalker : MonoBehaviour
    {
        Vector3[] route=new Vector3[0];int waypoint;ChefAppearance appearance;float clock;
        public bool Arrived=>waypoint>=route.Length;
        public void Initialize(GameObject prefab,int look,bool customer=false)
        {
            var visual=Instantiate(prefab,transform);appearance=visual.GetComponent<ChefAppearance>()??visual.GetComponentInChildren<ChefAppearance>();
            if(customer)visual.transform.localScale=Vector3.one*.85f;
            if(appearance!=null){appearance.enabled=false;appearance.usePlayerSelection=false;if(customer)CustomerPresentation.ApplyCustomerLook(appearance,look);else appearance.Apply(ChefAppearanceData.Example(look));}
        }
        public void Go(params Vector3[] points){route=points;waypoint=0;}
        public void Advance(float seconds,float speed,bool carrying=false)
        {
            float distance=Mathf.Max(0,seconds)*speed;bool moving=!Arrived;
            while(!Arrived && distance>0)
            {
                Vector3 delta=route[waypoint]-transform.position;float length=delta.magnitude;
                if(length>.001f)transform.rotation=Quaternion.LookRotation(delta.normalized,Vector3.up);
                if(length<=distance){transform.position=route[waypoint++];distance-=length;}
                else{transform.position+=delta.normalized*distance;distance=0;}
            }
            clock+=seconds;appearance?.Pose(carrying?1:0,moving?Mathf.Sin(clock*8)*18:0,seconds);
        }
    }
}
