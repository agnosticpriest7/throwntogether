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
        public bool HoldAction {get;private set;}
        public void ChooseAction(bool hold)=>HoldAction=hold;
        string message="";
        float messageUntil;
        public void Open(ExpoStation station)
        {
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
        string Seat(KitchenTicket ticket)
        {
            var expo=Station!=null ? Station.Expo:null;var day=Station!=null ? Station.Day:null;
            var table=expo?.TableFor(ticket);
            if(table==null || day==null || day.Tables==null)return "Seat —";
            int seat=Array.IndexOf(day.Tables,table);
            return seat>=0 ? "Table "+(seat+1):"Seated";
        }
        static string StateLabel(KitchenTicketState state)=>state==KitchenTicketState.Waiting ? "HOLD":
            state==KitchenTicketState.Ready ? "READY":"MAKE";
        public void Draw(bool second,float opacity=1)
        {
            var expo=Station!=null?Station.Expo:null;if(expo==null)return;Sync();
            var hud=Station.Day.GetComponent<RestaurantHud>();
            bool split=hud.chef.GetComponent<ChefInput>().Expo!=null && hud.coop?.PlayerTwo!=null && hud.coop.PlayerTwo.GetComponent<ChefInput>().Expo!=null;
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.depth=-100;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            float x=split&&second?650:18,width=split?612:1244,top=560,height=134;
            var text=new GUIStyle(GUI.skin.label){fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=false};text.normal.textColor=Color.white;
            var small=new GUIStyle(text){fontSize=14};var left=new GUIStyle(text){alignment=TextAnchor.MiddleLeft};
            GUI.color=new Color(.035f,.055f,.07f,.96f*opacity);GUI.DrawTexture(new Rect(x,top,width,height),Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);
            GUI.Label(new Rect(x+8,top,width-16,24),(second?"P2":"P1")+" EXPO  |  Active "+expo.ActiveCount+" / "+expo.Capacity+"  |  "+(rows.Count==0?"No orders":"Order "+(index+1)+" / "+rows.Count)+(scroll>0?"  ◀":"")+(scroll+VisibleRows<rows.Count?"  ▶":""),text);
            float card=(width-16)/VisibleRows;
            for(int column=0;column<VisibleRows;column++)
            {
                int i=scroll+column;if(i>=rows.Count)break;var ticket=rows[i];bool chosen=i==index;
                var rect=new Rect(x+8+column*card,top+26,card-5,68);
                GUI.color=chosen?new Color(1,.86f,.3f,opacity):new Color(.2f,.25f,.28f,opacity);GUI.DrawTexture(rect,Texture2D.whiteTexture);
                var tint=ExpoOrderStrip.Tint(ticket);GUI.color=new Color(tint.r,tint.g,tint.b,opacity);GUI.DrawTexture(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);
                float icon=split?42:56;if(ticket.Recipe!=null)FoodIcon.DrawOrder(new Rect(rect.x+5,rect.y+5,icon,icon),ticket.Recipe,opacity);
                float infoX=rect.x+icon+7,infoW=rect.width-icon-12;
                GUI.Label(new Rect(infoX,rect.y+4,infoW,23),Seat(ticket),small);
                GUI.Label(new Rect(infoX,rect.y+26,infoW,23),StateLabel(ticket.State),small);
                float patience=Mathf.Clamp01(expo.PatienceRemaining(ticket));var bar=new Rect(infoX,rect.y+53,infoW,7);
                GUI.color=new Color(.12f,.16f,.17f,opacity);GUI.DrawTexture(bar,Texture2D.whiteTexture);bar.width*=patience;
                GUI.color=Color.Lerp(new Color(.9f,.22f,.12f,opacity),new Color(.22f,.7f,.36f,opacity),patience);GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);
            }
            string selectedLabel=Selected==null?"Waiting for seated customers":Label(Selected);
            GUI.Label(new Rect(x+8,top+96,width-16,19),Message.Length>0?Message:selectedLabel+"  |  "+(HoldAction?"HOLD selected":"FIRE / MAKE selected"),small);
            GUI.Label(new Rect(x+8,top+115,width-16,19),"← / →: order   ↑: Make   ↓: Hold   A: apply   B: close",small);
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}