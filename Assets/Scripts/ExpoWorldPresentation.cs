using UnityEngine;
namespace ThrownTogether
{
    // Screen-aligned feedback anchored to the existing customer bubbles. No order panel.
    public static class ExpoWorldPresentation
    {
        static void Border(Rect r,Color color,float width)
        {
            GUI.color=color;
            GUI.DrawTexture(new Rect(r.x,r.y,r.width,width),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,r.yMax-width,r.width,width),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,r.y,width,r.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax-width,r.y,width,r.height),Texture2D.whiteTexture);
        }
        public static void Bubble(Rect rect,KitchenTicket ticket,bool selected,float opacity)
        {
            if(ticket==null)return;
            var before=GUI.color;bool fired=ticket.Priority>0;
            var color=fired?new Color(.12f,.95f,.3f,opacity):new Color(.95f,.25f,.22f,opacity);
            if(selected){var outside=rect;outside.xMin-=6;outside.yMin-=6;outside.xMax+=6;outside.yMax+=6;Border(outside,new Color(1,.87f,.2f,opacity),3);}
            Border(rect,color,3);
            var label=new Rect(rect.x-9,rect.y-21,rect.width+18,20);
            GUI.color=new Color(.025f,.045f,.055f,opacity*.95f);GUI.DrawTexture(label,Texture2D.whiteTexture);
            GUI.color=color;var style=new GUIStyle(GUI.skin.label){fontSize=14,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            style.normal.textColor=Color.white;
            GUI.Label(label,fired?ticket.Priority+" · "+(ticket.State==KitchenTicketState.Ready?"READY":"FIRED"):"HOLD",style);
            GUI.color=before;
        }
        public static void Hint(RestaurantDay day,float opacity)
        {
            var input=day.Expo?.Operator;if(input==null)return;
            var browser=input.ExpoBrowser;var ticket=browser.Selected;
            string action=ticket==null?"No orders":ticket.Priority>0?"A Hold":"A Fire";
            string message=browser.Message;
            // Keep feedback bounded even for long recipe names.
            if(message.Length>42)message=message.Substring(0,39)+"…";
            string text=action+"   X Priority ↑   B Exit";
            var old=GUI.color;GUI.color=new Color(.025f,.045f,.055f,opacity*.92f);
            GUI.DrawTexture(new Rect(400,650,480,46),Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,opacity);var style=new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleCenter};style.normal.textColor=Color.white;
            GUI.Label(new Rect(400,650,480,24),text,style);style.fontSize=13;
            GUI.Label(new Rect(400,674,480,20),string.IsNullOrEmpty(message)?"Select a customer bubble with stick / D-pad":message,style);GUI.color=old;
        }
    }
}
