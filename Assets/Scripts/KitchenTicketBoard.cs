using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace ThrownTogether
{
    // One board per service day. Pure synchronous queue rules and nothing else:
    // the board never inspects food, transfers slots, records income, starts or stops
    // patience, touches customers, frees plates or binds dishes. Those invariants stay
    // at the authoritative delivery boundary.
    // Calls are expected on Unity's main thread; validation and mutation are one
    // synchronous step, so no locking is required.
    public sealed class KitchenTicketBoard
    {
        public const int DefaultCapacity=2;
        readonly List<KitchenTicket> tickets=new List<KitchenTicket>();
        // Keyed by the ticket's stable id, but membership also requires the same object,
        // so a ticket from an earlier service can never mutate this board.
        readonly Dictionary<Guid,KitchenTicket> owned=new Dictionary<Guid,KitchenTicket>();
        long nextFireSequence=1;
        public KitchenTicketBoard(int capacity=DefaultCapacity)
        {
            if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity),"A board needs at least one active slot.");
            Capacity=capacity;
        }
        public int Capacity {get;}
        // Ready still occupies a slot: a plated dish is not free kitchen space until it
        // reaches the diner or the ticket ends.
        public int ActiveCount {get {int count=0;foreach(var ticket in tickets)if(Holding(ticket.State))count++;return count;}}
        public IReadOnlyList<KitchenTicket> Tickets=>Snapshot(tickets);
        public IReadOnlyList<KitchenTicket> WaitingTickets
        {
            get
            {
                var waiting=new List<KitchenTicket>();
                foreach(var ticket in tickets)if(ticket.State==KitchenTicketState.Waiting)waiting.Add(ticket);
                return Snapshot(waiting);
            }
        }
        public IReadOnlyList<KitchenTicket> ActiveTickets
        {
            get
            {
                var active=new List<KitchenTicket>();
                foreach(var ticket in tickets)if(Holding(ticket.State))active.Add(ticket);
                active.Sort((a,b)=>a.FireSequence.CompareTo(b.FireSequence));
                return Snapshot(active);
            }
        }
        public KitchenTicket Create(RecipeDefinition recipe,float createdAt)
        {
            if(recipe==null)throw new ArgumentNullException(nameof(recipe));
            if(!Usable(createdAt))throw new ArgumentOutOfRangeException(nameof(createdAt),"Ticket times are finite service-day seconds.");
            var ticket=new KitchenTicket(recipe,createdAt);
            tickets.Add(ticket);owned.Add(ticket.Id,ticket);return ticket;
        }
        public bool TryFire(KitchenTicket ticket,float firedAt)
        {
            if(!Owns(ticket) || ticket.State!=KitchenTicketState.Waiting)return false;
            if(!Usable(firedAt) || firedAt<ticket.CreatedAt)return false;
            if(ActiveCount>=Capacity)return false;
            // The sequence is consumed only here, so a rejected fire never leaves a gap
            // and never reorders the tickets that did succeed.
            ticket.Fire(firedAt,nextFireSequence++);return true;
        }
        public bool TryMarkReady(KitchenTicket ticket)=>Move(ticket,KitchenTicketState.Active,KitchenTicketState.Ready);
        public bool TryInvalidateReady(KitchenTicket ticket)=>Move(ticket,KitchenTicketState.Ready,KitchenTicketState.Active);
        // Active is accepted directly so a player carrying a matching dish to the table
        // does not have to stage it first.
        public bool TryServe(KitchenTicket ticket)
        {
            if(!Owns(ticket) || !Holding(ticket.State))return false;
            ticket.MoveTo(KitchenTicketState.Served);return true;
        }
        public bool TryCancel(KitchenTicket ticket)
        {
            if(!Owns(ticket) || Terminal(ticket.State))return false;
            ticket.MoveTo(KitchenTicketState.Cancelled);return true;
        }
        bool Move(KitchenTicket ticket,KitchenTicketState from,KitchenTicketState to)
        {
            if(!Owns(ticket) || ticket.State!=from)return false;
            ticket.MoveTo(to);return true;
        }
        bool Owns(KitchenTicket ticket)=>ticket!=null && owned.TryGetValue(ticket.Id,out var mine) && ReferenceEquals(mine,ticket);
        static bool Holding(KitchenTicketState state)=>state==KitchenTicketState.Active || state==KitchenTicketState.Ready;
        static bool Terminal(KitchenTicketState state)=>state==KitchenTicketState.Served || state==KitchenTicketState.Cancelled;
        static bool Usable(float seconds)=>!float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds>=0;
        // A fresh copy behind ReadOnlyCollection: callers cannot reach the backing list
        // by casting, and an old snapshot can never write back into the board.
        static IReadOnlyList<KitchenTicket> Snapshot(List<KitchenTicket> source)=>new ReadOnlyCollection<KitchenTicket>(new List<KitchenTicket>(source));
    }
}
