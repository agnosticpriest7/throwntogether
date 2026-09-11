using UnityEngine;
namespace ThrownTogether
{
    // The table, rather than the kitchen pass, is the delivery boundary for a service day.
    public sealed class DiningTable : Interactable
    {
        public CustomerOrder order;
        public RestaurantDay day;
        public bool Occupied {get;private set;}
        public bool Arriving {get;private set;}
        public bool Leaving {get;private set;}
        public bool WaitingForMeal => Occupied && !Arriving && !Leaving && order.Phase==OrderPhase.Waiting;
        public float PatienceRemaining => WaitingForMeal ? Mathf.Clamp01(1-(day.Elapsed-SeatedAt)/Mathf.Max(1,day.Settings.seatedPatience)):1;
        public void BeginDeparture(){Leaving=true;ReservedForServer=false;order.ResetOrder(null);SetGuestVisible(false);}
        public bool ReservedForServer {get;set;}
        public float SeatedAt {get;private set;}
        public bool Clean => !Occupied && order.tableSlot.Item==null;
        public bool LastInteractionServed {get;private set;}
        public void ReserveSeat(){Occupied=true;Arriving=true;Leaving=false;}
        public void Seat(RecipeDefinition recipe,int look=0)
        {
            Arriving=false;order.ResetOrder(recipe);SeatedAt=day.Elapsed;
            var presentation=order.GetComponent<CustomerPresentation>();if(presentation?.seatedVisual!=null)CustomerPresentation.ApplyCustomerLook(presentation.seatedVisual,look);
            SetGuestVisible(true);
        }
        public void Depart(){Occupied=false;Leaving=false;Arriving=false;SetGuestVisible(false);if(order.tableSlot.Item==null)order.ResetOrder(null);}
        public void SetGuestVisible(bool visible)
        {var presentation=order.GetComponent<CustomerPresentation>();if(presentation!=null && presentation.seatedVisual!=null)presentation.seatedVisual.gameObject.SetActive(visible);}
        public bool CanServe(ItemPayload dish)=>!day.Closed && !Arriving && !Leaving && Occupied && order.tableSlot.Item==null && order.CanAccept(dish);
        public bool Deliver(Carryable dish)
        {
            if(dish==null || !CanServe(dish.Payload))return false;
            order.Receive(dish);ReservedForServer=false;
            day.RecordMeal(order.recipe,day.Elapsed-SeatedAt);ShowSuccess(true);return true;
        }
        public override string Prompt(ChefController chef)
        {
            if(order.tableSlot.Item?.Payload.dirty==true)return chef.Hands.Item==null ? "Clear dirty plate":"Hands full — put your item down";
            if(Leaving)return "Customer leaving";
            if(Arriving)return "Customer arriving";
            if(!Occupied)return "Clean table — ready for a customer";
            if(order.Phase==OrderPhase.Eating)return "Customer eating";
            return chef.Hands.Item!=null && CanServe(chef.Hands.Item.Payload) ? "Serve "+order.recipe.displayName:"Needs "+order.recipe.displayName;
        }
        public override bool Interact(ChefController chef)
        {
            LastInteractionServed=false;
            if(day.Closed)return false;
            if(chef.Hands.Item==null && order.tableSlot.Item?.Payload.dirty==true)
            {if(!chef.Hands.TryTake(order.tableSlot.Item))return false;if(!Occupied)order.ResetOrder(null);return true;}
            LastInteractionServed=Deliver(chef.Hands.Item);return LastInteractionServed;
        }
    }
}
