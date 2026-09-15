using System.Collections.Generic;
using UnityEngine;
namespace ThrownTogether
{
    // A bounded passing cost, not collision avoidance or another navigation system.
    // Customers never register. Separation by counters/walls does not count as congestion.
    public sealed class StaffCongestion : MonoBehaviour
    {
        public const float Range=1.15f,MinimumSpeed=.6f;
        static readonly HashSet<StaffCongestion> people=new HashSet<StaffCongestion>();
        public float SpeedFactor {get;private set;}=1;
        void OnEnable()
        {
            var motor=GetComponent<CharacterController>();
            foreach(var other in people)
            {
                if(other==null || other.gameObject.scene!=gameObject.scene)continue;
                var theirs=other.GetComponent<CharacterController>();
                if(motor!=null && theirs!=null)Physics.IgnoreCollision(motor,theirs,true);
            }
            people.Add(this);
        }
        void OnDisable(){people.Remove(this);SpeedFactor=1;}
        public float Factor(Vector3 motion)
        {
            motion.y=0;float factor=1;
            if(motion.sqrMagnitude<.0001f){SpeedFactor=1;return 1;}
            foreach(var other in people)
            {
                if(other==null || other==this || !other.isActiveAndEnabled || other.gameObject.scene!=gameObject.scene)continue;
                var delta=other.transform.position-transform.position;if(Mathf.Abs(delta.y)>.6f)continue;delta.y=0;
                float distance=delta.magnitude;
                if(distance>=Range || Vector3.Dot(motion.normalized,delta)<-.1f)continue; // Walking away must release immediately.
                bool wall=false;
                foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up,delta.normalized,distance,~0,QueryTriggerInteraction.Ignore))
                    if(!(hit.collider is CharacterController) && hit.collider.GetComponentInParent<StaffCongestion>()==null){wall=true;break;}
                if(wall)continue;
                factor=Mathf.Min(factor,Mathf.Lerp(MinimumSpeed,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,Range,distance))));
            }
            SpeedFactor=factor;return factor;
        }
    }
}
