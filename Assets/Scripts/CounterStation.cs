namespace ThrownTogether
{
    public class CounterStation : Interactable
    {
        public CarrySlot slot;
        public override string Status => stationName + (slot.Item != null ? "\n" + slot.Item.Payload.Label : "");
        public override string Prompt(ChefController chef)
        {
            if (CanCombine(chef)) return "Combine food and plate";
            if (chef.Hands.Item == null) return slot.Item != null ? "Pick up " + slot.Item.Payload.Label : "Counter empty";
            return slot.Item == null ? "Place " + chef.Hands.Item.Payload.Label : "Counter occupied";
        }
        public bool CanCombine(ChefController chef) => slot.Item != null && chef.Hands.Item != null &&
            (ItemPayload.CanPlate(slot.Item.Payload,chef.Hands.Item.Payload) || ItemPayload.CanPlate(chef.Hands.Item.Payload,slot.Item.Payload));
        public override bool Interact(ChefController chef)
        {
            if (CanCombine(chef))
            {
                var plate=slot.Item.Payload.isPlate ? slot.Item : chef.Hands.Item;
                var food=slot.Item.Payload.isPlate ? chef.Hands.Item : slot.Item;
                plate.Payload.AddFood(food.Payload);
                food.Owner.Release(); Destroy(food.gameObject);
                // Assembly belongs to the counter regardless of which item arrived first.
                if (plate.Owner != slot) slot.TryTake(plate);
                plate.RefreshVisual(); return true;
            }
            if (chef.Hands.Item == null) return chef.Hands.TryTake(slot.Item);
            return slot.TryTake(chef.Hands.Item);
        }
    }
}
