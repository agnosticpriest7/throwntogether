using System;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace ThrownTogether
{
    // Shared presentation only. Input and ticket authority remain in their existing owners.
    public static class ExpoSharedPanel
    {
        static readonly ConditionalWeakTable<RestaurantDay,ExpoPanelModel> models=new ConditionalWeakTable<RestaurantDay,ExpoPanelModel>();
        static readonly Color One=new Color(.3f,.9f,1),Two=new Color(1,.8f,.25f);
        public static void Draw(RestaurantDay day,ExpoTicketBrowser first,ExpoTicketBrowser second,float opacity)
        {
            if(day.Closed || day.AwaitingMenu)return;
            var p1=first?.Station!=null?first:null;var p2=second?.Station!=null?second:null;
            if(p1==null && p2==null)return;
            var focus=p2!=null && (p1==null || p2.SelectionRevision>p1.SelectionRevision)?p2:p1;
            var model=models.GetValue(day,_=>new ExpoPanelModel());model.Refresh(day.Expo.Tickets,day.Expo.Capacity,focus.Selected);
            bool expanded=model.Waiting.Length>0 && (p1?.WaitingExpanded==true || p2?.WaitingExpanded==true);
            var prior=GUI.color;int depth=GUI.depth;GUI.depth=-100;
            var style=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleLeft,clipping=TextClipping.Clip};style.normal.textColor=Color.white;
            void Box(Rect r,Color c){GUI.color=new Color(c.r,c.g,c.b,c.a*opacity);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);}
            void Text(Rect r,string s,Color? c=null){GUI.color=c.HasValue?new Color(c.Value.r,c.Value.g,c.Value.b,opacity):new Color(1,1,1,opacity);GUI.Label(r,s,style);GUI.color=new Color(1,1,1,opacity);}
            Box(new Rect(18,590,1244,110),new Color(.025f,.045f,.055f,.97f));
            void Card(Rect r,KitchenTicket t,string header)
            {
                Box(r,t==null?new Color(.13f,.18f,.2f):ExpoOrderStrip.Tint(t));
                Text(new Rect(r.x+5,r.y,r.width-10,20),header+(t!=null?" #"+model.Number(t):""));
                if(t==null)return;
                FoodIcon.DrawOrder(new Rect(r.x+5,r.y+19,39,32),t.Recipe,opacity);
                Text(new Rect(r.x+47,r.y+21,r.width-48,22),t.State==KitchenTicketState.Ready?"READY":t.State==KitchenTicketState.Active?"FIRED":"HOLD");
                var bar=new Rect(r.x+5,r.y+r.height-7,r.width-10,4);float patience=Mathf.Clamp01(day.Expo.PatienceRemaining(t));
                Box(bar,new Color(.1f,.13f,.15f));bar.width*=patience;Box(bar,patience<.25f?new Color(1,.55f,.15f):new Color(.4f,.95f,.55f));
                if(p1?.Selected==t){Box(new Rect(r.x,r.y,r.width,3),One);Text(new Rect(r.x+r.width-26,r.y,26,20),"P1",One);}
                if(p2?.Selected==t){Box(new Rect(r.x,r.y+r.height-3,r.width,3),Two);Text(new Rect(r.x+r.width-26,r.y+35,26,20),"P2",Two);}
            }
            int count=Math.Max(3,model.Slots.Length);float width=450f/count;
            for(int i=0;i<count;i++)
            {
                var ticket=i<model.Slots.Length?model.Slots[i]:null;
                Card(new Rect(28+i*width,598,width-6,62),ticket,"BAY "+(i+1));
                if(ticket==null)Text(new Rect(33+i*width,622,width-12,22),i<model.Slots.Length?"Empty":"Unavailable");
            }
            var control=new Rect(490,598,132,62);
            Box(control,new Color(.13f,.18f,.2f));
            if(GUI.Button(control,"WAITING "+model.Waiting.Length))focus.BrowseWaiting();
            if(!expanded && focus.Selected?.State==KitchenTicketState.Waiting)
                Text(new Rect(490,665,135,23),(focus==p1?"P1":"P2")+" #"+model.Number(focus.Selected),focus==p1?One:Two);
            if(expanded)
            {
                Box(new Rect(18,520,1244,70),new Color(.025f,.045f,.055f,.97f));
                Text(new Rect(28,521,250,20),"WAITING "+model.Waiting.Length+"  "+(model.WaitingTop+1)+"–"+Math.Min(model.WaitingTop+5,model.Waiting.Length));
                for(int i=0;i<5;i++){int at=model.WaitingTop+i;if(at<model.Waiting.Length)Card(new Rect(280+i*193,524,186,62),model.Waiting[at],"");}
                Text(new Rect(28,548,245,22),"B Back");
            }
            void Details(ExpoTicketBrowser browser,string who,Color tint,float y)
            {
                if(browser==null || browser.Selected==null)return;var t=browser.Selected;
                int seat=Array.IndexOf(day.Tables,day.Expo.TableFor(t))+1;
                Text(new Rect(640,y,610,22),who+" #"+model.Number(t)+" "+t.Recipe.displayName+(seat>0?" — T"+seat:""),tint);
                string note=browser.Message;
                bool error=note.Length>0 && !note.StartsWith("Green:") && !note.StartsWith("MAKE:") && !note.StartsWith("HOLD:");
                string hint=browser.HoldAction?"A Hold":t.State==KitchenTicketState.Waiting?"A Fire":"B Back";
                Text(new Rect(640,y+21,610,22),error?note:Mathf.RoundToInt(day.Expo.PatienceRemaining(t)*100)+"% • "+hint,tint);
            }
            Details(p1,"P1",One,596);Details(p2,"P2",Two,p1!=null?646:596);
            GUI.color=prior;GUI.depth=depth;
        }
    }
}
