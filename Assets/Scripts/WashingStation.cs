using UnityEngine;
namespace ThrownTogether
{
    public sealed class WashingStation : Interactable
    {
        public CarrySlot slot;
        public float duration=3;
        private readonly WorkAttendance attendance=new WorkAttendance();
        private float elapsed;
        public bool Busy {get;private set;}
        public bool Working => Busy && attendance.Running;
        public override float Progress => Busy ? Mathf.Clamp01(elapsed/duration):-1;
        public override string Prompt(ChefController chef) => Busy ? (Working ? "Washing — stay still":"Press Use to resume washing; stay still") :
            slot.Item!=null ? "Pick up clean plate" : "Place a dirty plate to begin washing";
        public override bool Interact(ChefController chef)
        {
            if(Busy) return chef.Hands.Item==null && attendance.Begin(chef);
            if(slot.Item!=null) return chef.Hands.Item==null && chef.Hands.TryTake(slot.Item);
            var item=chef.Hands.Item;
            if(item==null || !item.Payload.dirty || !slot.TryTake(item)) return false;
            elapsed=0;Busy=true;attendance.Begin(chef);return true;
        }
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if(!Working || seconds<=0) return;
            elapsed+=seconds;if(elapsed<duration) return;
            slot.Item.Configure(ItemPayload.Plate());Busy=false;attendance.Release();
        }
    }
}
