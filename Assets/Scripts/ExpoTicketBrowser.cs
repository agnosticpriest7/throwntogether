using System;
using System.Collections.Generic;
using UnityEngine;
namespace ThrownTogether
{
    // One browser per ChefInput: the cursor, scroll position and drawing for a single
    // chef. There is no shared cursor and no global input polling. The browser never
    // mutates a ticket; RestaurantExpo.TryFire remains the only authority, and this
    // cached view is allowed to be stale.
    public sealed class ExpoTicketBrowser
    {
        public const int VisibleRows=5;
        public ExpoStation Station {get;private set;}
        readonly List<KitchenTicket> rows=new List<KitchenTicket>();
        // Selection is held by stable ticket identity, never by row index or table, so
        // another player firing or a list reordering cannot move this chef's choice.
        Guid selected;
        int index,scroll;
        bool recovered;
        static long nextRevision;
        public long SelectionRevision {get;private set;}
        public bool HoldAction {get;private set;}
        public void ChooseAction(bool hold){HoldAction=hold;SelectionRevision=++nextRevision;}
        string message="";
        float messageUntil;
        public void Open(ExpoStation station)
        {
            SelectionRevision=++nextRevision;
            Station=station;selected=Guid.Empty;index=0;scroll=0;recovered=false;HoldAction=false;message="Green: Make • Red: Hold. Matching food can still be served.";messageUntil=Time.unscaledTime+8;Sync();
        }
        public void Close()
        {
            Station=null;rows.Clear();selected=Guid.Empty;index=0;scroll=0;recovered=false;message="";messageUntil=0;
        }
        // True once after the selected order vanished and a neighbour was chosen, so the
        // caller can demand a fresh press before anything is fired.
        public bool ConsumeRecovered(){bool value=recovered;recovered=false;return value;}
        // Re-reads the live board. Callers must do this before ConsumeRecovered so a row
        // that vanished since the last frame is noticed rather than missed.
        public void Refresh()=>Sync();
        public IReadOnlyList<KitchenTicket> Rows {get{Sync();return rows;}}
        public KitchenTicket Selected {get{Sync();return index>=0 && index<rows.Count ? rows[index]:null;}}
        // Read-only view diagnostics. They describe what is on screen; they decide nothing.
        public int RowCount {get{Sync();return rows.Count;}}
        public int SelectedIndex {get{Sync();return rows.Count==0 ? -1:index;}}
        public int ScrollOffset {get{Sync();return rows.Count==0 ? 0:scroll;}}
        public string Message=>Time.unscaledTime<messageUntil ? message:"";
        public bool Usable(ChefController chef)
        {
            if(Station==null || chef==null || Station.Expo==null)return false;
            var delta=chef.transform.position-Station.transform.position;delta.y=0;
            return delta.magnitude<=chef.reach;
        }
        void Note(string text){message=text;messageUntil=Time.unscaledTime+3;}
        void Sync()
        {
            rows.Clear();
            var expo=Station!=null ? Station.Expo:null;
            if(expo==null){index=0;scroll=0;return;}
            // Arrival order is stable across Fire/Hold/Ready, for both players.
            foreach(var ticket in expo.OpenTickets)rows.Add(ticket);
            // Keep the lost id while the board is empty. Clearing it here would hide the
            // loss, and the next diner to be seated would look like an ordinary selection.
            if(rows.Count==0){index=0;scroll=0;return;}
            int found=-1;
            for(int i=0;i<rows.Count;i++)if(rows[i].Id==selected){found=i;break;}
            if(found>=0)index=found;
            else
            {
                // The chosen diner's order is gone. Keep a sensible neighbour, but make the
                // player release and press again so one confirm cannot fire someone else.
                if(selected!=Guid.Empty)recovered=true;
                index=Mathf.Clamp(index,0,rows.Count-1);selected=rows[index].Id;
            }
            if(index<scroll)scroll=index;
            if(index>=scroll+VisibleRows)scroll=index-VisibleRows+1;
            scroll=Mathf.Clamp(scroll,0,Mathf.Max(0,rows.Count-VisibleRows));
        }
        public void Navigate(int step)
        {
            SelectionRevision=++nextRevision;
            Sync();if(rows.Count==0)return;
            index=Mathf.Clamp(index+step,0,rows.Count-1);selected=rows[index].Id;Sync();
        }
        public bool Fire()
        {
            Sync();
            // A selection that vanished must never be silently retargeted by this call,
            // including when the caller never ran a Tick to notice the loss. The pending
            // flag is spent here, so a deliberate second confirmation still works.
            if(ConsumeRecovered()){Note("Selection changed — confirm again");return false;}
            var expo=Station!=null ? Station.Expo:null;var ticket=Selected;
            if(expo==null || ticket==null){Note("No waiting orders");return false;}
            if(ticket.State!=KitchenTicketState.Waiting){Note("Already making this order");return false;}
            if(expo.ActiveCount>=expo.Capacity){Note("Queue full — Hold or serve an order first.");return false;}
            if(!expo.TryFire(ticket)){Note("That order can no longer be fired");Sync();return false;}
            selected=ticket.Id;Note("MAKE: "+Label(ticket));Sync();return true;
        }
        public bool Hold()
        {
            Sync();if(ConsumeRecovered()){Note("Selection changed — confirm again");return false;}
            var expo=Station!=null?Station.Expo:null;var ticket=Selected;
            if(expo==null || ticket==null){Note("No orders");return false;}
            if(!expo.TryHold(ticket)){Note(ticket.State==KitchenTicketState.Waiting?"Already on Hold":"That order can no longer be held");return false;}
            Note("HOLD: "+Label(ticket));Sync();return true;
        }
        static string Label(KitchenTicket ticket)=>ticket.Recipe!=null ? ticket.Recipe.displayName:"Order";
    }
}
