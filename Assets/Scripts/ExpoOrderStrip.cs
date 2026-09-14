using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // Presentation only. The registry remains the authority for firing and serving.
    public static class ExpoOrderStrip
    {
        static Texture2D readyMark;
        static Texture2D ReadyMark
        {
            get
            {
                if(readyMark!=null)return readyMark;
                readyMark=new Texture2D(16,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};
                var pixels=new Color[256];
                for(int x=2;x<=13;x++)
                {
                    int y=x<=6?10-x:x-2;
                    for(int offset=0;offset<3;offset++)pixels[(y+offset)*16+x]=Color.white;
                }
                readyMark.SetPixels(pixels);readyMark.Apply(false,true);return readyMark;
            }
        }
        public static string Label(KitchenTicket ticket)=>ticket.State==KitchenTicketState.Waiting?"HOLD":ticket.State==KitchenTicketState.Ready?"READY":"MAKE";
        public static Color Tint(KitchenTicket ticket)=>ticket.State==KitchenTicketState.Waiting?new Color(.62f,.13f,.12f):new Color(.08f,.48f,.25f);
        public static KitchenTicket[] Visible(RestaurantExpo expo)=>expo.ActiveTickets.Concat(expo.WaitingTickets.Take(4)).ToArray();
        public static void Draw(RestaurantDay day,float opacity)
        {
            var expo=day.Expo;if(expo==null || day.Closed || day.AwaitingMenu)return;
            var active=expo.ActiveTickets;var waiting=expo.WaitingTickets;int slots=expo.Capacity+Mathf.Min(4,waiting.Count);
            float width=slots*88+12;float x=(1280-width)*.5f;
            var prior=GUI.color;var depth=GUI.depth;GUI.depth=-20;
            var text=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};text.normal.textColor=Color.white;
            GUI.color=new Color(.025f,.045f,.055f,.94f*opacity);GUI.DrawTexture(new Rect(x,606,width,94),Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,opacity);
            string extra=expo.WaitingTickets.Count>4?" • +"+(expo.WaitingTickets.Count-4)+" held":"";
            GUI.Label(new Rect(230,581,820,24),"EXPO • "+expo.ActiveCount+" / "+expo.Capacity+" making • Visit Expo + A: fire • Red HOLD / Green MAKE"+extra,text);
            for(int i=0;i<slots;i++)
            {
                KitchenTicket ticket=i<expo.Capacity?(i<active.Count?active[i]:null):waiting[i-expo.Capacity];
                float left=x+6+i*88;var tile=new Rect(left,612,82,82);
                GUI.color=ticket==null?new Color(.15f,.2f,.22f,opacity):new Color(Tint(ticket).r,Tint(ticket).g,Tint(ticket).b,opacity);
                GUI.DrawTexture(tile,Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);
                if(ticket==null){GUI.Label(tile,"OPEN",text);continue;}
                FoodIcon.DrawOrder(new Rect(left+13,614,56,50),ticket.Recipe,opacity);
                var table=expo.TableFor(ticket);int seat=table==null?-1:System.Array.IndexOf(day.Tables,table);
                GUI.Label(new Rect(left,669,82,22),Label(ticket)+(seat>=0?" T"+(seat+1):""),text);
                if(ticket.State==KitchenTicketState.Ready)
                    GUI.DrawTexture(new Rect(left+61,615,18,18),ReadyMark);
            }
            GUI.color=prior;GUI.depth=depth;
        }
    }
}
