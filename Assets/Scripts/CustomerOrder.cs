using UnityEngine;

namespace ThrownTogether
{
    public enum OrderPhase { Waiting, Delivering, Eating, Complete }
    public sealed class CustomerOrder : MonoBehaviour
    {
        public DishRecipe recipe;
        public CarrySlot tableSlot;
        public Transform customerVisual;
        public OrderPhase Phase { get; private set; }
        private float eatingTime;
        private Vector3 visualPosition;
        private void Awake() { if (customerVisual != null) visualPosition=customerVisual.localPosition; }
        public bool CanAccept(ItemPayload dish) => Phase == OrderPhase.Waiting && recipe.Matches(dish);
        public bool Reserve(ItemPayload dish) { if (!CanAccept(dish)) return false; Phase=OrderPhase.Delivering; return true; }
        public void Receive(Carryable dish) { tableSlot.TryTake(dish); Phase=OrderPhase.Eating; eatingTime=0; }
        private void Update()
        {
            if (Phase != OrderPhase.Eating) return;
            eatingTime+=Time.deltaTime;
            if (customerVisual != null) customerVisual.localPosition=visualPosition+Vector3.up*Mathf.Sin(eatingTime*8)*.07f;
            if (eatingTime >= 2) { Phase=OrderPhase.Complete; if (customerVisual != null) customerVisual.localPosition=visualPosition; }
        }
    }
}
