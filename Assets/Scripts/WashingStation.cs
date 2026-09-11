using System.Collections.Generic;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class WashingStation : Interactable
    {
        public CarrySlot slot;
        public float duration=3;
        private readonly WorkAttendance attendance=new WorkAttendance();
        private readonly Queue<CarrySlot> queue=new Queue<CarrySlot>();
        private Object employee;
        private float elapsed;
        public int Count => queue.Count+(slot.Item!=null?1:0);
        public bool Busy {get;private set;}
        public bool Working => Busy && (attendance.Running || employee!=null);
        public override float Progress => Busy ? Mathf.Clamp01(elapsed/duration):-1;
        public override string Prompt(ChefController chef)
        {
            if(chef.Hands.Item?.Payload.dirty==true)return "Stack dirty plate in sink ("+Count+")";
            if(chef.Hands.Item!=null)return "Hands full — use a counter";
            if(!Busy && slot.Item!=null)return "Pick up clean plate ("+Count+" in sink)";
            return Busy ? (Working ? "Washing — stay still":"Press Use to resume washing; stay still") : "Place a dirty plate to begin washing";
        }
        public bool Enqueue(Carryable item)
        {
            if(item==null || !item.Payload.dirty || Count>=SourceStation.PlateCapacity)return false;
            if(item.Owner==slot)return false;
            foreach(var queued in queue)if(item.Owner==queued)return false;
            if(slot.Item==null){slot.TryTake(item);StartJob();}
            else
            {
                var holder=new GameObject("Queued dirty plate");holder.transform.SetParent(slot.transform,false);
                var carry=holder.AddComponent<CarrySlot>();carry.TryTake(item);queue.Enqueue(carry);ArrangeQueue();
            }
            return true;
        }
        void ArrangeQueue(){int i=1;foreach(var queued in queue)queued.transform.localPosition=Vector3.up*(i++*.08f);}
        void StartJob(){elapsed=0;Busy=slot.Item?.Payload.dirty==true;attendance.Release();employee=null;}
        void Next()
        {
            if(slot.Item==null && queue.Count>0){var next=queue.Dequeue();slot.TryTake(next.Item);Destroy(next.gameObject);ArrangeQueue();}
            StartJob();
        }
        public bool TakeClean(CarrySlot hands)
        {
            if(Busy || slot.Item==null || !hands.TryTake(slot.Item))return false;
            Next();return true;
        }
        public override bool Interact(ChefController chef)
        {
            if(chef.Hands.Item?.Payload.dirty==true)
            {if(!Enqueue(chef.Hands.Item))return false;if(employee==null && !attendance.Running)attendance.Begin(chef);return true;}
            if(chef.Hands.Item!=null)return false;
            if(!Busy)return TakeClean(chef.Hands);
            return employee==null && attendance.Begin(chef);
        }
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float seconds){if(Busy && attendance.Running && employee==null)Wash(seconds);}
        public bool WashBy(Object worker,float seconds)
        {
            if(worker==null || !Busy || attendance.Running || employee!=null && employee!=worker)return false;
            employee=worker;Wash(seconds);return true;
        }
        public void ReleaseWorker(Object worker){if(employee==worker)employee=null;}
        void Wash(float seconds)
        {
            if(seconds<=0)return;
            elapsed+=seconds;if(elapsed<duration)return;
            slot.Item.Configure(ItemPayload.Plate());Busy=false;attendance.Release();employee=null;ShowSuccess(true);
        }
    }
}
