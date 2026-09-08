using UnityEngine;

namespace ThrownTogether
{
    public sealed class SourceStation : Interactable
    {
        public IngredientDefinition ingredient;
        public Carryable itemPrefab;
        public bool plates;
        public override string Prompt(ChefController chef) => chef.Hands.Item == null ? (plates ? "Take clean plate" : "Take potato") : "Hands full — use a counter";
        public override bool Interact(ChefController chef)
        {
            if (chef.Hands.Item != null) return false;
            // Replace this supply policy with inventory-backed availability later.
            var item=Instantiate(itemPrefab); item.Configure(plates ? ItemPayload.Plate() : ItemPayload.Food(ingredient));
            return chef.Hands.TryTake(item);
        }
    }
}
