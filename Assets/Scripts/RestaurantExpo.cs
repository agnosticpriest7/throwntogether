using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    // A claim names a diner AND a physical dish. A table may be reused during travel.
    public sealed class ExpoDeliveryClaim
    {
        public KitchenTicket Ticket {get;}
        public Carryable Dish {get;}
        public Object Carrier {get;}
        internal ExpoDeliveryClaim(KitchenTicket ticket,Carryable dish,Object carrier)
        {Ticket=ticket;Dish=dish;Carrier=carrier;}
    }

    // Scene-side authority: the board owns queue rules; this registry owns seat,
    // dish and carrier associations. No associations are stored in food payloads.
    public sealed class RestaurantExpo
    {
        readonly RestaurantDay day;
        readonly KitchenTicketBoard board;
        readonly Dictionary<DiningTable,KitchenTicket> current=new Dictionary<DiningTable,KitchenTicket>();
        readonly Dictionary<KitchenTicket,DiningTable> seats=new Dictionary<KitchenTicket,DiningTable>();
        readonly Dictionary<KitchenTicket,Carryable> dishes=new Dictionary<KitchenTicket,Carryable>();
        readonly Dictionary<KitchenTicket,ExpoDeliveryClaim> claims=new Dictionary<KitchenTicket,ExpoDeliveryClaim>();
        public RestaurantExpo(RestaurantDay owner,int capacity)
        {day=owner;board=new KitchenTicketBoard(capacity);}
        public int Capacity=>board.Capacity;
        public int ActiveCount=>board.ActiveCount;
        public IReadOnlyList<KitchenTicket> Tickets=>board.Tickets;
        public IReadOnlyList<KitchenTicket> WaitingTickets=>board.WaitingTickets;
        public IReadOnlyList<KitchenTicket> ActiveTickets=>board.ActiveTickets;
        public KitchenTicket TicketFor(DiningTable table)=>table!=null && current.TryGetValue(table,out var ticket)?ticket:null;
        public DiningTable TableFor(KitchenTicket ticket)=>ticket!=null && seats.TryGetValue(ticket,out var table) && table!=null && TicketFor(table)==ticket?table:null;
        public float PatienceRemaining(KitchenTicket ticket)=>TableFor(ticket)?.PatienceRemaining??0;
        static bool Live(KitchenTicket ticket)=>ticket.State!=KitchenTicketState.Served && ticket.State!=KitchenTicketState.Cancelled;
        static bool Fired(KitchenTicket ticket)=>ticket!=null && (ticket.State==KitchenTicketState.Active || ticket.State==KitchenTicketState.Ready);

        internal void Seat(DiningTable table)
        {
            if(table==null || table.day!=day)return;
            var old=TicketFor(table);if(old!=null)Cancel(old);
            if(!table.WaitingForMeal || table.order.recipe==null)return;
            var ticket=board.Create(table.order.recipe,day.Elapsed);
            current[table]=ticket;seats[ticket]=table;
        }
        internal void Depart(DiningTable table)
        {var ticket=TicketFor(table);if(ticket!=null)Cancel(ticket);}
        void Cancel(KitchenTicket ticket)
        {board.TryCancel(ticket);Unbind(ticket);}
        void Unbind(KitchenTicket ticket)
        {dishes.Remove(ticket);claims.Remove(ticket);board.TryInvalidateReady(ticket);}
        internal void Refresh()
        {
            foreach(var pair in seats)
            {
                var ticket=pair.Key;var table=pair.Value;
                if(Live(ticket) && (day.Closed || table==null || TicketFor(table)!=ticket ||
                    !table.WaitingForMeal || table.order.recipe!=ticket.Recipe))Cancel(ticket);
            }
            foreach(var pair in dishes.ToArray())
                if(!Fired(pair.Key) || pair.Value==null || !pair.Key.Recipe.Matches(pair.Value.Payload))Unbind(pair.Key);
            foreach(var pair in claims.ToArray())
                if(pair.Value.Carrier==null)claims.Remove(pair.Key);
        }
        public bool TryFire(KitchenTicket ticket)
        {
            Refresh();
            var table=TableFor(ticket);
            bool fired=!day.Closed && !day.AwaitingMenu && table!=null && table.WaitingForMeal &&
                table.PatienceRemaining>0 && board.TryFire(ticket,day.Elapsed);
            if(fired)day.StageExpoPass();return fired;
        }
        public bool CanServe(DiningTable table,ItemPayload food)
        {
            var ticket=TicketFor(table);
            return !day.Closed && !day.AwaitingMenu && Fired(ticket) && table!=null &&
                table.WaitingForMeal && table.PatienceRemaining>0 && ticket.Recipe.Matches(food);
        }
        public KitchenTicket BoundTicket(Carryable dish)
        {
            Refresh();if(dish==null)return null;
            foreach(var pair in dishes)if(pair.Value==dish)return pair.Key;
            return null;
        }
        bool SameScene(Carryable dish)=>dish!=null && dish.gameObject.scene==day.gameObject.scene;
        KitchenTicket Candidate(Carryable dish)
        {
            if(day.Closed || day.AwaitingMenu || !SameScene(dish))return null;
            // Honor an existing association. An in-flight claim cannot be collected twice.
            foreach(var pair in dishes)
                if(pair.Value==dish)return !claims.ContainsKey(pair.Key) && TableFor(pair.Key)?.CanServe(dish.Payload)==true?pair.Key:null;
            return board.ActiveTickets.FirstOrDefault(t=>!dishes.ContainsKey(t) && !claims.ContainsKey(t) &&
                TableFor(t)?.CanServe(dish.Payload)==true);
        }
        void Bind(KitchenTicket ticket,Carryable dish)
        {
            // Deliberate manual retargeting invalidates both old associations atomically.
            foreach(var pair in dishes.ToArray())if(pair.Value==dish || pair.Key==ticket)Unbind(pair.Key);
            dishes[ticket]=dish;board.TryMarkReady(ticket);
        }
        public bool TryStage(Carryable dish)
        {
            Refresh();var ticket=Candidate(dish);if(ticket==null)return false;
            Bind(ticket,dish);return true;
        }
        public bool CanCollect(Carryable dish)
        {Refresh();return Candidate(dish)!=null;}
        public ExpoDeliveryClaim TryClaim(Carryable dish,Object carrier)
        {
            Refresh();if(carrier==null)return null;
            var ticket=Candidate(dish);if(ticket==null)return null;
            Bind(ticket,dish);
            var claim=new ExpoDeliveryClaim(ticket,dish,carrier);claims[ticket]=claim;return claim;
        }
        public void ReleaseClaim(ExpoDeliveryClaim claim)
        {
            if(claim!=null && claims.TryGetValue(claim.Ticket,out var owned) && ReferenceEquals(owned,claim))
                claims.Remove(claim.Ticket);
        }
        public bool TryDeliver(DiningTable table,Carryable dish,ExpoDeliveryClaim claim=null)
        {
            Refresh();
            if(table==null || table.day!=day || !SameScene(dish) || !table.CanServe(dish.Payload))return false;
            var ticket=TicketFor(table);
            if(claim!=null && (claim.Ticket!=ticket || claim.Dish!=dish || claim.Carrier==null ||
                !claims.TryGetValue(ticket,out var owned) || !ReferenceEquals(owned,claim)))return false;
            // No user callbacks/yields occur between transfer and ticket/payment commit.
            table.order.Receive(dish);
            if(table.order.tableSlot.Item!=dish || table.order.Phase!=OrderPhase.Eating)return false;
            if(!board.TryServe(ticket))throw new System.InvalidOperationException("Validated Expo delivery lost its ticket.");
            foreach(var pair in dishes.ToArray())if(pair.Value==dish || pair.Key==ticket)Unbind(pair.Key);
            table.ReservedForServer=false;
            day.RecordMeal(ticket.Recipe,day.Elapsed-table.SeatedAt);return true;
        }
    }
}
