using UnityEngine;

namespace ThrownTogether
{
    public enum OrderPhase { Waiting, Delivering, Eating, Complete, Dirty }
    public sealed class CustomerOrder : MonoBehaviour
    {
        public RecipeDefinition recipe;
        public DishReturnStation dishReturn;
        public CarrySlot tableSlot;
        public Transform customerVisual;
        [System.NonSerialized] public bool manualService;
        [System.NonSerialized] public float mealSeconds=8;
        public OrderPhase Phase { get; private set; }
        public bool Active { get; private set; }=true;
        private float eatingTime;
        private Vector3 visualPosition;
        private void Awake() { if (customerVisual != null) visualPosition=customerVisual.localPosition; }
        public bool CanAccept(ItemPayload dish) => Active && Phase == OrderPhase.Waiting && recipe!=null && recipe.Matches(dish);
        public void ResetOrder(RecipeDefinition next)
        {
            recipe=next; Active=next!=null; Phase=OrderPhase.Waiting; eatingTime=0;
            if(customerVisual!=null && !manualService) { customerVisual.localPosition=visualPosition; customerVisual.gameObject.SetActive(Active); }
        }
        public bool Reserve(ItemPayload dish) { if (!CanAccept(dish)) return false; Phase=OrderPhase.Delivering; return true; }
        public void Receive(Carryable dish) { if(dish==null || !tableSlot.TryTake(dish))return; Phase=OrderPhase.Eating; eatingTime=0; }
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if (Phase != OrderPhase.Eating) return;
            eatingTime+=Mathf.Max(0,seconds);
            // Eating motion belongs to the visual rig, not the root shared with chair colliders.
            if (eatingTime >= (manualService ? mealSeconds:2))
            {
                if(manualService)
                {
                    Phase=OrderPhase.Dirty;
                    if(tableSlot.Item!=null){tableSlot.Item.Payload.MakeDirty();tableSlot.Item.RefreshVisual();}
                }
                else { Phase=OrderPhase.Complete; if(dishReturn!=null) dishReturn.Return(tableSlot.Item); if (customerVisual != null) customerVisual.localPosition=visualPosition; }
            }
        }
    }
}
