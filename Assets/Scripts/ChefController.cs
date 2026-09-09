using UnityEngine;

namespace ThrownTogether
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ChefController : MonoBehaviour
    {
        public CarrySlot Hands;
        public float speed=4.2f;
        public float reach=2.0f;
        public Interactable Focus { get; private set; }
        public string Feedback { get; private set; }
        private float feedbackUntil;
        private CharacterController motor;
        public event System.Action<Interactable,bool,bool> InteractionSucceeded;
        private void Awake() => motor=GetComponent<CharacterController>();
        public void Move(Vector2 input, float seconds)
        {
            Vector3 direction=new Vector3(input.x,0,input.y);
            direction=Vector3.ClampMagnitude(direction,1);
            if (direction.sqrMagnitude>.002f) transform.rotation=Quaternion.LookRotation(direction);
            motor.Move((direction*speed+Vector3.down*3)*seconds);
        }
        public void FindFocus()
        {
            Focus=null; float best=float.MaxValue;
            foreach (var station in Interactable.Active)
            {
                if (station == null) continue;
                var offset=station.transform.position-transform.position; offset.y=0;
                float distance=offset.magnitude;
                float facing=Vector3.Dot(transform.forward,offset.normalized);
                if (distance>reach || facing<-.1f) continue;
                float score=distance-facing*.35f;
                if (score<best) { best=score; Focus=station; }
            }
        }
        public bool Use()
        {
            FindFocus();
            if (Focus == null) { Feedback="Move closer and face a station"; feedbackUntil=Time.time+1.5f; return false; }
            Feedback=Focus.Prompt(this); feedbackUntil=Time.time+1.5f;
            bool hadItem=Hands.Item != null;
            bool plated=Focus is CounterStation counter && counter.CanCombine(this);
            bool used=Focus.Interact(this);
            if(used) Feedback=Hands.Item!=null ? "Holding "+Hands.Item.Payload.Label : "Placed / started at "+Focus.stationName;
            if(used) InteractionSucceeded?.Invoke(Focus,hadItem,plated);
            return used;
        }
        private void LateUpdate() { FindFocus(); if (Time.time>feedbackUntil) Feedback=""; }
    }
}
