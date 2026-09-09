using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class RestaurantShift : MonoBehaviour
    {
        public ShiftDefinition definition;
        public CustomerOrder[] seats;
        public int CompletedCount { get; private set; }
        public int NextOrder { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool Complete => definition!=null && definition.orders.Length>0 && CompletedCount==definition.orders.Length;
        public string Tickets => string.Join(" | ",seats.Where(s=>s.Active).Select((s)=>s.recipe.displayName+" — "+s.Phase));
        private void Start() => Begin();
        public void Begin()
        {
            CompletedCount=0; NextOrder=0; ElapsedSeconds=0;
            foreach(var seat in seats) AssignNext(seat);
        }
        private void AssignNext(CustomerOrder seat)
        {
            var dish=seat.tableSlot.Release(); if(dish!=null) Destroy(dish.gameObject);
            seat.ResetOrder(NextOrder<definition.orders.Length ? definition.orders[NextOrder++] : null);
        }
        public CustomerOrder FindOrder(ItemPayload item) => seats.FirstOrDefault(s=>s.CanAccept(item));
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if(Complete) return;
            ElapsedSeconds+=Mathf.Max(0,seconds);
            foreach(var seat in seats)
                if(seat.Active && seat.Phase==OrderPhase.Complete) { CompletedCount++; AssignNext(seat); }
        }
    }
}
