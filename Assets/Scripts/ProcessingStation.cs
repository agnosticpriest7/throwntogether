using UnityEngine;

namespace ThrownTogether
{
    public sealed class ProcessingStation : CounterStation
    {
        public ProcessingRecipe recipe;
        private float elapsed;
        public bool Busy { get; private set; }
        public override float Progress => Busy ? Mathf.Clamp01(elapsed/recipe.duration) : -1;
        public override string Status => stationName + (Busy ? "\nWorking…" : slot.Item != null ? "\nREADY" : "");
        public override string Prompt(ChefController chef)
        {
            if (Busy) return "Processing — wait for ready";
            if (slot.Item == null && chef.Hands.Item != null && !recipe.Accepts(chef.Hands.Item.Payload))
                return "Needs " + recipe.ingredient.NameFor(recipe.input);
            return base.Prompt(chef);
        }
        public override bool Interact(ChefController chef)
        {
            if (Busy) return false;
            if (slot.Item == null && chef.Hands.Item != null)
            {
                if (!recipe.Accepts(chef.Hands.Item.Payload)) return false;
                if (!slot.TryTake(chef.Hands.Item)) return false;
                elapsed=0; Busy=true; return true;
            }
            return base.Interact(chef);
        }
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if (!Busy || seconds <= 0) return;
            elapsed+=seconds;
            if (elapsed < recipe.duration) return;
            slot.Item.Payload.state=recipe.output; slot.Item.RefreshVisual(); Busy=false;
        }
    }
}
