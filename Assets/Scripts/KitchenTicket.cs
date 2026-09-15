using System;
namespace ThrownTogether
{
    public enum KitchenTicketState { Waiting, Active, Ready, Served, Cancelled }
    // Transient per-diner identity, retained by its board for the service day.
    // Independent of reusable seats and physical dishes. The public API is read-only;
    // assembly-internal transitions are reserved by convention for KitchenTicketBoard.
    public sealed class KitchenTicket
    {
        public Guid Id {get;}
        public RecipeDefinition Recipe {get;}
        public KitchenTicketState State {get;private set;}
        public float CreatedAt {get;}
        // Null until a successful fire, then retained through Served/Cancelled so the
        // board can still explain how a terminal ticket was ordered.
        public float? FiredAt {get;private set;}
        public long FireSequence {get;private set;}
        // Table-ticket production rank; zero means not fired. Independent of bay order.
        public int Priority {get;internal set;}
        internal KitchenTicket(RecipeDefinition recipe,float createdAt)
        {Id=Guid.NewGuid();Recipe=recipe;CreatedAt=createdAt;State=KitchenTicketState.Waiting;}
        internal void Fire(float firedAt,long sequence)
        {State=KitchenTicketState.Active;FiredAt=firedAt;FireSequence=sequence;}
        internal void MoveTo(KitchenTicketState next)=>State=next;
    }
}
