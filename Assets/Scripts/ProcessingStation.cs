using UnityEngine;

namespace ThrownTogether
{
    public sealed class ProcessingStation : CounterStation
    {
        public ProcessingRecipe recipe;
        public ApplianceDefinition appliance;
        private ProcessingRecipe activeRecipe;
        private ProcessingRecipe Select(ItemPayload item)
        {
            if(appliance!=null) foreach(var candidate in appliance.supportedProcesses) if(candidate!=null && candidate.Accepts(item)) return candidate;
            return recipe!=null && recipe.Accepts(item) ? recipe : null;
        }
        private float elapsed;
        public bool Busy { get; private set; }
        public override float Progress => Busy ? Mathf.Clamp01(elapsed/activeRecipe.duration) : -1;
        public override string Status => stationName + (Busy ? "\nWorking…" : slot.Item != null ? "\nREADY" : "");
        public override string Prompt(ChefController chef)
        {
            if (Busy) return "Processing — wait for ready";
            if (slot.Item == null && chef.Hands.Item != null && Select(chef.Hands.Item.Payload)==null)
                return "Needs a compatible "+recipe.input+" ingredient";
            return base.Prompt(chef);
        }
        public override bool Interact(ChefController chef)
        {
            if (Busy) return false;
            if (slot.Item == null && chef.Hands.Item != null)
            {
                var selected=Select(chef.Hands.Item.Payload);
                if(selected==null || appliance!=null && !appliance.Supports(selected)) return false;
                if (!slot.TryTake(chef.Hands.Item)) return false;
                activeRecipe=selected; elapsed=0; Busy=true; return true;
            }
            return base.Interact(chef);
        }
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if (!Busy || seconds <= 0) return;
            elapsed+=seconds;
            if (elapsed < activeRecipe.duration) return;
            slot.Item.Payload.state=activeRecipe.output; slot.Item.RefreshVisual(); Busy=false;
        }
    }
}
