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
        string message="";
        float messageUntil;
        public void Open(ExpoStation station)
        {
            Station=station;selected=Guid.Empty;index=0;scroll=0;recovered=false;message="";messageUntil=0;Sync();
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
            // Waiting first, then the fired queue. Served and cancelled orders are absent
            // from both source lists, so they never appear as rows.
            foreach(var ticket in expo.WaitingTickets)rows.Add(ticket);
            foreach(var ticket in expo.ActiveTickets)rows.Add(ticket);
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
            if(ticket.State!=KitchenTicketState.Waiting){Note("Already fired");return false;}
            if(expo.ActiveCount>=expo.Capacity){Note("Kitchen queue full ("+expo.ActiveCount+" / "+expo.Capacity+") — serve an order first");return false;}
            if(!expo.TryFire(ticket)){Note("That order can no longer be fired");Sync();return false;}
            // Keep this exact ticket selected: it moves from the waiting rows into the
            // fired rows, and the next row must not be fired by the same press.
            selected=ticket.Id;Note("Fired "+Label(ticket));Sync();return true;
        }
        static string Label(KitchenTicket ticket)=>ticket.Recipe!=null ? ticket.Recipe.displayName:"Order";
        string Seat(KitchenTicket ticket)
        {
            var expo=Station!=null ? Station.Expo:null;var day=Station!=null ? Station.Day:null;
            var table=expo?.TableFor(ticket);
            if(table==null || day==null || day.Tables==null)return "Seat —";
            int seat=Array.IndexOf(day.Tables,table);
            return seat>=0 ? "Table "+(seat+1):"Seated";
        }
        static string StateLabel(KitchenTicketState state)=>state==KitchenTicketState.Waiting ? "WAITING":
            state==KitchenTicketState.Ready ? "READY":"FIRED";
        public void Draw(bool second,float opacity=1)
        {
            var expo=Station!=null ? Station.Expo:null;if(expo==null)return;
            Sync();
            var matrix=GUI.matrix;var color=GUI.color;var background=GUI.backgroundColor;bool enabled=GUI.enabled;int depth=GUI.depth;GUI.depth=-100;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            float x=second?666:18;
            int visible=Mathf.Clamp(rows.Count,1,VisibleRows);float height=98+visible*42;float top=694-height;
            var text=new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleLeft,wordWrap=false};
            text.normal.textColor=Color.white;
            var centred=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};
            var small=new GUIStyle(text){fontSize=15};
            GUI.color=new Color(.035f,.055f,.07f,.96f*opacity);GUI.DrawTexture(new Rect(x,top,596,height),Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,opacity);
            GUI.Label(new Rect(x+12,top+4,380,26),(second?"P2 — ":"P1 — ")+"EXPO ORDERS",centred);
            bool full=expo.ActiveCount>=expo.Capacity;
            GUI.Label(new Rect(x+396,top+4,188,26),"Active "+expo.ActiveCount+" / "+expo.Capacity+(full?"  FULL":""),centred);
            string note=Message;
            GUI.Label(new Rect(x+12,top+28,572,22),note.Length>0 ? note:rows.Count==0 ? "No waiting orders":"Up / down: choose an order",small);
            if(rows.Count==0)
            {
                GUI.Label(new Rect(x+12,top+56,572,42),"No orders yet.\nSeated customers appear here as soon as they order.",centred);
            }
            for(int row=0;row<VisibleRows;row++)
            {
                int i=scroll+row;if(i>=rows.Count)break;
                var ticket=rows[i];bool chosen=i==index;
                float y=top+54+row*42;
                GUI.color=new Color(chosen?.16f:.09f,chosen?.26f:.13f,chosen?.24f:.15f,opacity);
                GUI.DrawTexture(new Rect(x+10,y,576,38),Texture2D.whiteTexture);
                GUI.color=new Color(1,1,1,opacity);
                if(chosen){GUI.color=new Color(.2f,.8f,.6f,opacity);GUI.DrawTexture(new Rect(x+10,y,4,38),Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);}
                if(ticket.Recipe!=null)FoodIcon.DrawOrder(new Rect(x+20,y+3,32,32),ticket.Recipe,opacity);
                GUI.Label(new Rect(x+58,y,232,38),(chosen?"> ":"")+Label(ticket),text);
                GUI.Label(new Rect(x+292,y,96,38),Seat(ticket),small);
                GUI.Label(new Rect(x+388,y,86,38),StateLabel(ticket.State),small);
                float patience=Mathf.Clamp01(expo.PatienceRemaining(ticket));
                var bar=new Rect(x+478,y+14,84,9);
                GUI.color=new Color(.12f,.16f,.17f,opacity);GUI.DrawTexture(bar,Texture2D.whiteTexture);
                bar.width*=patience;
                GUI.color=Color.Lerp(new Color(.9f,.22f,.12f,opacity),new Color(.22f,.7f,.36f,opacity),patience);
                GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);
                GUI.Label(new Rect(x+478,y-6,84,38),Mathf.RoundToInt(patience*100)+"%",small);
            }
            if(rows.Count>VisibleRows)
                GUI.Label(new Rect(x+12,650,180,22),"Order "+(index+1)+" of "+rows.Count,small);
            GUI.Label(new Rect(x+196,650,388,22),"Stick / D-pad: choose • A / E: fire • B / Q: close",small);
            GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=background;GUI.enabled=enabled;GUI.depth=depth;
        }
    }
}
