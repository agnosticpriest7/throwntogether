using UnityEngine;

namespace ThrownTogether
{
    public sealed class ServiceStation : Interactable
    {
        public CustomerOrder order;
        public RestaurantShift shift;
        private CustomerOrder deliveryOrder;
        public CarrySlot pickupSlot;
        private Carryable delivery;
        private float elapsed;
        private Vector3 origin;
        public override string Status => stationName + (delivery!=null ? "\nDelivering…" : "");
        private CustomerOrder Match(ItemPayload item) => shift!=null ? shift.FindOrder(item) : order.CanAccept(item) ? order : null;
        public override string Prompt(ChefController chef) => delivery!=null ? "Delivering — please wait" :
            chef.Hands.Item != null && Match(chef.Hands.Item.Payload)!=null ? "Serve "+chef.Hands.Item.Payload.Label :
            shift!=null ? "Needs a plated dish matching a ticket" : order.Phase!=OrderPhase.Waiting ? "Order served" : "Needs plated fries";
        public override bool Interact(ChefController chef)
        {
            var item=chef.Hands.Item;
            if(delivery!=null || item==null) return false;
            var target=Match(item.Payload);
            if(target==null || !target.Reserve(item.Payload)) return false;
            deliveryOrder=target;
            pickupSlot.TryTake(item); delivery=pickupSlot.Release(); origin=delivery.transform.position; elapsed=0;
            return true;
        }
        private void Update()
        {
            if (delivery == null) return;
            elapsed+=Time.deltaTime;
            // Temporary automatic delivery; the order boundary can later be used by servers.
            float t=Mathf.Clamp01((elapsed-.4f)/1.2f);
            delivery.transform.position=Vector3.Lerp(origin,deliveryOrder.tableSlot.transform.position,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.3f;
            if (t>=1) { deliveryOrder.Receive(delivery); delivery=null; }
        }
    }
}
