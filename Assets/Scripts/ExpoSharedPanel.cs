using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace ThrownTogether
{
    // Drawn once by RestaurantHud. Browsers retain independent input and selection.
    public static class ExpoSharedPanel
    {
        sealed class View
        {
            public readonly ExpoPanelModel Model=new ExpoPanelModel();
            public CookProduction.Order[] Plans=Array.Empty<CookProduction.Order>();
            public float RefreshAt=-1;
        }
        static readonly ConditionalWeakTable<RestaurantDay,View> models=new ConditionalWeakTable<RestaurantDay,View>();
        static readonly Color One=new Color(.3f,.9f,1),Two=new Color(1,.8f,.25f);
        public static void Draw(RestaurantDay day,ExpoTicketBrowser first,ExpoTicketBrowser second,float opacity)
        {
            if(day.Closed || day.AwaitingMenu)return;
            var p1=first?.Station!=null?first:null;var p2=second?.Station!=null?second:null;
            if(p1==null && p2==null)return;
            var focus=p2!=null && (p1==null || p2.SelectionRevision>p1.SelectionRevision)?p2:p1;
            var view=models.GetValue(day,_=>new View());var model=view.Model;model.Refresh(day.Expo.Tickets,day.Expo.Capacity,focus.Selected);
            var prior=GUI.color;int depth=GUI.depth;GUI.depth=-100;
            var style=new GUIStyle(GUI.skin.label){fontSize=15,alignment=TextAnchor.MiddleLeft,clipping=TextClipping.Clip};style.normal.textColor=Color.white;
            void Box(Rect r,Color c){GUI.color=new Color(c.r,c.g,c.b,c.a*opacity);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);}
            void Text(Rect r,string s,Color? c=null){GUI.color=c.HasValue?new Color(c.Value.r,c.Value.g,c.Value.b,opacity):new Color(1,1,1,opacity);GUI.Label(r,s,style);GUI.color=new Color(1,1,1,opacity);}
            Box(new Rect(18,522,1244,178),new Color(.025f,.045f,.055f,.97f));
            Text(new Rect(28,524,390,22),"EXPO  •  ACTIVE "+day.Expo.ActiveCount+" / "+day.Expo.Capacity);
            string hidden=model.Waiting.Length>5?"  ["+(model.WaitingTop+1)+"–"+Math.Min(model.WaitingTop+5,model.Waiting.Length)+" / "+model.Waiting.Length+"]":"";
            Text(new Rect(450,524,800,22),"WAITING / HOLD"+hidden+Offscreen(p1,"P1",model)+Offscreen(p2,"P2",model));
            void Card(Rect r,KitchenTicket t,string header)
            {
                Box(r,t==null?new Color(.13f,.18f,.2f):ExpoOrderStrip.Tint(t));
                Text(new Rect(r.x+5,r.y+1,r.width-10,20),header+(t!=null?"  #"+model.Number(t):"  EMPTY"));
                if(t==null)return;
                FoodIcon.DrawOrder(new Rect(r.x+5,r.y+22,44,36),t.Recipe,opacity);
                Text(new Rect(r.x+52,r.y+24,r.width-55,22),t.State==KitchenTicketState.Ready?"READY":t.State==KitchenTicketState.Active?"FIRED":"HOLD");
                float patience=Mathf.Clamp01(day.Expo.PatienceRemaining(t));var bar=new Rect(r.x+5,r.y+60,r.width-10,5);
                Box(bar,new Color(.1f,.13f,.15f));bar.width*=patience;Box(bar,patience<.25f?new Color(1,.55f,.15f):new Color(.4f,.95f,.55f));
                if(p1?.Selected==t){Box(new Rect(r.x,r.y,r.width,3),One);Text(new Rect(r.x+r.width-44,r.y+2,42,20),"P1",One);}
                if(p2?.Selected==t){Box(new Rect(r.x,r.y+r.height-3,r.width,3),Two);Text(new Rect(r.x+r.width-44,r.y+43,42,20),"P2",Two);}
            }
            float slotWidth=404f/Math.Max(1,model.Slots.Length);
            for(int i=0;i<model.Slots.Length;i++)Card(new Rect(28+i*slotWidth,549,slotWidth-6,68),model.Slots[i],"SLOT "+(i+1));
            for(int i=0;i<5;i++){int at=model.WaitingTop+i;if(at<model.Waiting.Length)Card(new Rect(450+i*160,549,154,68),model.Waiting[at],"");}
            if(model.Waiting.Length==0)Text(new Rect(460,566,750,25),"No waiting orders");
            // Inventory inspection is shared and throttled; IMGUI may draw several times per frame.
            if(Time.unscaledTime>=view.RefreshAt){view.Plans=CookProduction.Snapshot(day);view.RefreshAt=Time.unscaledTime+.15f;}
            var plans=view.Plans;
            void Details(ExpoTicketBrowser browser,string who,Color tint,float x,float width)
            {
                if(browser==null)return;var t=browser.Selected;
                if(t==null){Text(new Rect(x,620,width,25),who+" • Waiting for customers",tint);return;}
                var table=day.Expo.TableFor(t);int seat=Array.IndexOf(day.Tables,table)+1;
                string state=t.State==KitchenTicketState.Ready?"READY":t.State==KitchenTicketState.Active?"FIRED":"HOLD";
                Text(new Rect(x,620,width,20),who+"  #"+model.Number(t)+" "+t.Recipe.displayName+" — "+(seat>0?"Table "+seat:"Seated customer"),tint);
                Text(new Rect(x,640,width,20),state+" • "+Mathf.RoundToInt(day.Expo.PatienceRemaining(t)*100)+"% patience  |  A: "+(browser.HoldAction?"HOLD":"FIRE"),tint);
                var plan=plans.FirstOrDefault(o=>o.Ticket==t);
                string parts=plan==null?"":string.Join("  |  ",plan.Components.Select(c=>c.Ingredient.NameFor(c.State)+": "+(c.OnPlate?"plated":c.Supply==null?"needed":CookProduction.Location(c.Supply) is ProcessingStation station && station.Busy?"cooking":"ready")));
                string note=browser.Message;
                if(note.StartsWith("Green:") || note.StartsWith("MAKE:") || note.StartsWith("HOLD:"))note="";
                style.fontSize=13;Text(new Rect(x,660,width,20),note.Length>0?note:parts);style.fontSize=15;
            }
            bool two=p1!=null && p2!=null;
            Details(p1,"P1",One,28,two?595:1215);Details(p2,"P2",Two,two?650:28,two?595:1215);
            // Keep help visible without taking another full-width row from the restaurant.
            style.fontSize=13;
            Text(new Rect(28,683,1215,17),"Left / right: ticket number   Up: Fire   Down: Hold   A: apply   B: close • Serving always allowed");
            GUI.color=prior;GUI.depth=depth;
        }
        static string Offscreen(ExpoTicketBrowser browser,string who,ExpoPanelModel model)
        {
            if(browser==null)return "";int at=Array.IndexOf(model.Waiting,browser.Selected);
            return at>=0 && (at<model.WaitingTop || at>=model.WaitingTop+5)?"  "+who+" #"+model.Number(browser.Selected)+(at<model.WaitingTop?" ◀":" ▶"):"";
        }
    }
}
