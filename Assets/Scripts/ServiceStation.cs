using UnityEngine;

namespace ThrownTogether
{
    public sealed class ServiceStation : Interactable
    {
        public CustomerOrder order;
        public CarrySlot pickupSlot;
        private Carryable delivery;
        private float elapsed;
        private Vector3 origin;
        public override string Status => stationName + (order.Phase == OrderPhase.Delivering ? "\nDelivering…" : "");
        public override string Prompt(ChefController chef) => order.Phase != OrderPhase.Waiting ? "Order served" :
            chef.Hands.Item != null && order.CanAccept(chef.Hands.Item.Payload) ? "Serve plated fries" : "Needs plated fries";
        public override bool Interact(ChefController chef)
        {
            var item=chef.Hands.Item;
            if (item == null || !order.Reserve(item.Payload)) return false;
            pickupSlot.TryTake(item); delivery=pickupSlot.Release(); origin=delivery.transform.position; elapsed=0;
            return true;
        }
        private void Update()
        {
            if (delivery == null) return;
            elapsed+=Time.deltaTime;
            // Temporary automatic delivery; the order boundary can later be used by servers.
            float t=Mathf.Clamp01((elapsed-.4f)/1.2f);
            delivery.transform.position=Vector3.Lerp(origin,order.tableSlot.transform.position,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.3f;
            if (t>=1) { order.Receive(delivery); delivery=null; }
        }
    }
}
