using UnityEngine;

namespace ThrownTogether
{
    public enum OrderPhase { Waiting, Delivering, Eating, Complete }
    public sealed class CustomerOrder : MonoBehaviour
    {
        public RecipeDefinition recipe;
        public CarrySlot tableSlot;
        public Transform customerVisual;
        public OrderPhase Phase { get; private set; }
        public bool Active { get; private set; }=true;
        private float eatingTime;
        private Vector3 visualPosition;
        private void Awake() { if (customerVisual != null) visualPosition=customerVisual.localPosition; }
        public bool CanAccept(ItemPayload dish) => Active && Phase == OrderPhase.Waiting && recipe!=null && recipe.Matches(dish);
        public void ResetOrder(RecipeDefinition next)
        {
            recipe=next; Active=next!=null; Phase=OrderPhase.Waiting; eatingTime=0;
            if(customerVisual!=null) { customerVisual.localPosition=visualPosition; customerVisual.gameObject.SetActive(Active); }
        }
        public bool Reserve(ItemPayload dish) { if (!CanAccept(dish)) return false; Phase=OrderPhase.Delivering; return true; }
        public void Receive(Carryable dish) { tableSlot.TryTake(dish); Phase=OrderPhase.Eating; eatingTime=0; }
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if (Phase != OrderPhase.Eating) return;
            eatingTime+=Mathf.Max(0,seconds);
            if (customerVisual != null) customerVisual.localPosition=visualPosition+Vector3.up*(RestaurantMenu.Display.reducedEffects ? 0 : Mathf.Sin(eatingTime*8)*.07f);
            if (eatingTime >= 2) { Phase=OrderPhase.Complete; if (customerVisual != null) customerVisual.localPosition=visualPosition; }
        }
    }
}
