using UnityEngine;

namespace ThrownTogether
{
    public sealed class ProcessingStation : CounterStation
    {
        public ProcessingRecipe recipe;
        public ApplianceDefinition appliance;
        private ProcessingRecipe activeRecipe;
        public bool requiresAttendance;
        [System.NonSerialized] public float processingSpeed=1;
        private readonly WorkAttendance attendance=new WorkAttendance();
        private MonoBehaviour staff;
        bool StaffPresent=>staff!=null && staff.isActiveAndEnabled && Vector3.Distance(staff.transform.position,transform.position)<=1.85f;
        public bool Working => Busy && (!requiresAttendance || attendance.Running || StaffPresent);
        public bool SupportsPrep(IngredientDefinition food)=>requiresAttendance && Select(ItemPayload.Food(food))?.output==FoodState.Cut;
        public bool StartBy(MonoBehaviour worker,CarrySlot hands)
        {
            if(worker==null || !worker.isActiveAndEnabled || hands?.Item==null || Busy || slot.Item!=null ||
                Vector3.Distance(worker.transform.position,transform.position)>1.85f || !SupportsPrep(hands.Item.Payload.ingredient))return false;
            var selected=Select(hands.Item.Payload);if(selected==null || selected.output!=FoodState.Cut)return false;
            if(!slot.TryTake(hands.Item))return false;
            activeRecipe=selected;elapsed=0;Busy=true;staff=worker;return true;
        }
        public void WorkBy(MonoBehaviour worker,float seconds)
        {if(staff==worker && StaffPresent)Advance(seconds);}
        public void ReleaseStaff(MonoBehaviour worker){if(staff==worker)staff=null;}
        private ProcessingRecipe Select(ItemPayload item)
        {
            if(appliance!=null) foreach(var candidate in appliance.supportedProcesses) if(candidate!=null && candidate.Accepts(item)) return candidate;
            return recipe!=null && recipe.Accepts(item) ? recipe : null;
        }
        public ProcessingRecipe CurrentProcess=>Busy?activeRecipe:null;
        public ProcessingRecipe ProcessFor(ItemPayload item)=>Select(item);
        public bool StartHotBy(MonoBehaviour worker,CarrySlot hands)
        {
            if(requiresAttendance || worker==null || !worker.isActiveAndEnabled || hands?.Item==null || Busy || slot.Item!=null || Vector3.Distance(worker.transform.position,transform.position)>1.85f)return false;
            var selected=Select(hands.Item.Payload);if(selected==null)return false;
            if(!slot.TryTake(hands.Item))return false;
            activeRecipe=selected;elapsed=0;Busy=true;staff=null;return true;
        }
        private float elapsed;
        public bool Busy { get; private set; }
        public override float Progress => Busy ? Mathf.Clamp01(elapsed/activeRecipe.duration) : -1;
        public override string Status => stationName + (Busy ? "\nWorking…" : slot.Item != null ? "\nREADY" : "");
        public override string Prompt(ChefController chef)
        {
            if (Busy) return requiresAttendance ? (Working ? "Cutting — stay still" : "Press Use to resume cutting; stay still") : "Processing — wait for ready";
            if (slot.Item == null && chef.Hands.Item != null && Select(chef.Hands.Item.Payload)==null)
                return "Needs a compatible "+recipe.input+" ingredient";
            return base.Prompt(chef);
        }
        public override bool Interact(ChefController chef)
        {
            if (Busy) {if(StaffPresent)return false;staff=null;return requiresAttendance && chef.Hands.Item==null && attendance.Begin(chef);}
            if (slot.Item == null && chef.Hands.Item != null)
            {
                var selected=Select(chef.Hands.Item.Payload);
                if(selected==null || appliance!=null && !appliance.Supports(selected)) return false;
                if (!slot.TryTake(chef.Hands.Item)) return false;
                activeRecipe=selected; elapsed=0; Busy=true; if(requiresAttendance) attendance.Begin(chef); return true;
            }
            return base.Interact(chef);
        }
        private void Update() {if(staff==null)Advance(Time.deltaTime);}
        public void Advance(float seconds)
        {
            if(Busy && (slot.Item==null || !activeRecipe.Accepts(slot.Item.Payload)))
            {Busy=false;staff=null;attendance.Release();return;}
            if (!Working || seconds <= 0) return;
            elapsed+=seconds*Mathf.Max(1,processingSpeed);
            if (elapsed < activeRecipe.duration) return;
            slot.Item.Payload.state=activeRecipe.output; slot.Item.RefreshVisual(); Busy=false;staff=null;attendance.Release();
            if(requiresAttendance) ShowSuccess(true);
        }
    }
}
