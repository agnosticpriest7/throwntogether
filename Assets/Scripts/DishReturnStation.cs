using System.Collections.Generic;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class DishReturnStation : Interactable
    {
        public Transform stack;
        private readonly Queue<Carryable> dishes=new Queue<Carryable>();
        public int Count => dishes.Count;
        public override string Prompt(ChefController chef) => chef.Hands.Item?.Payload.dirty==true ? "Stack dirty plate" : Count==0 ? "No dirty plates yet" : chef.Hands.Item!=null ? "Hands full — use a counter" : "Take dirty plate ("+Count+")";
        public void Return(Carryable dish)
        {
            if(dish==null) return;
            dish.Owner?.Release();dish.Payload.MakeDirty();dish.RefreshVisual();
            dish.transform.SetParent(stack,false);dish.transform.localPosition=Vector3.up*(dishes.Count*.07f);
            dishes.Enqueue(dish);
        }
        public bool TakeDirty(CarrySlot hands)
        {
            if(Count==0 || !hands.TryTake(dishes.Peek()))return false;
            dishes.Dequeue();int i=0;foreach(var remaining in dishes)remaining.transform.localPosition=Vector3.up*(i++*.07f);return true;
        }
        public override bool Interact(ChefController chef)
        {
            if(chef.Hands.Item?.Payload.dirty==true){Return(chef.Hands.Item);return true;}
            return TakeDirty(chef.Hands);
        }
    }
}
