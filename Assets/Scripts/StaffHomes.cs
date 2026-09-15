using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace ThrownTogether
{
    [Serializable] public sealed class StaffHomePlacement {public int kitchen;public string role;public Vector3 position;}
    public sealed class StaffHomes:MonoBehaviour
    {
        public static readonly string[] Roles={"server","host","busser","dishwasher","prep-cook","cook"};
        public static string PurchaseId(string role)=>role=="busser"?"hire-busser":role=="dishwasher"?"hire-dishwasher":role;
        readonly Dictionary<string,Vector3> draft=new Dictionary<string,Vector3>();
        readonly Dictionary<string,Vector3> defaults=new Dictionary<string,Vector3>();
        public void Invalidate(){defaults.Clear();}
        KitchenFurniture furniture;RestaurantDay day;string baseline;
        public int SelectedRole {get;private set;}
        public Vector3 Cursor {get;private set;}
        public string Message {get;private set;}="";
        public static Vector3 Entrance(RestaurantDay day)=>new Vector3(day.Settings.entrance.x,0,-4.8f);
        public StaffHomePlacement[] Draft=>draft.Select(p=>new StaffHomePlacement{kitchen=SessionOptions.Kitchen,role=p.Key,position=p.Value}).OrderBy(p=>p.role).ToArray();
        string Signature()=>string.Join("|",Draft.Select(p=>p.role+":"+p.position.ToString("R")));
        public bool Changed=>Signature()!=baseline;
        public void Initialize(KitchenFurniture value,RestaurantDay owner){furniture=value;day=owner;BeginDraft();}
        public void BeginDraft()
        {
            Invalidate();draft.Clear();foreach(var p in RestaurantAccounts.Current.Data.staffHomes.Where(p=>p!=null && p.kitchen==SessionOptions.Kitchen && Roles.Contains(p.role)))draft[p.role]=p.position;
            baseline=Signature();SelectedRole=0;ResetCursor();Message="";
        }
        public Vector3 Home(string role)
        {
            if(draft.TryGetValue(role,out var p))return p;
            if(defaults.TryGetValue(role,out p))return p;
            if(role=="server")return day.Settings.serverIdle;
            if(role=="host")return day.Settings.hostIdle;
            if(role=="busser")return day.Settings.busserIdle;
            var station=furniture.Find(role=="dishwasher"?"base:10":role=="prep-cook"?"base:3":"base:8");
            p=station!=null?KitchenStaffRoute.Approach(station)??Entrance(day):Entrance(day);defaults[role]=p;return p;
        }
        public bool Valid(Vector3 point,out string reason)
        {
            reason="Keep home on a clear floor route, away from the entrance.";
            if(!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z) || Mathf.Abs(point.y)>.01f || Vector3.Distance(point,Entrance(day))<.8f || !KitchenStaffRoute.Clear(point) || KitchenStaffRoute.ToPoint(Entrance(day),point)==null)return false;
            reason="";return true;
        }
        public bool Validate(out string reason)
        {
            foreach(var p in draft)if(!Valid(p.Value,out reason)){reason=p.Key+" home blocked. Move its home or the equipment.";return false;}
            reason="";return true;
        }
        public bool TrySet(string role,Vector3 point)
        {
            if(!furniture.Editing || !furniture.CanEdit || !Roles.Contains(role))return false;
            point.y=0;if(!Valid(point,out var why)){Message=why;return false;}
            if(draft.Any(p=>p.Key!=role && Vector3.Distance(p.Value,point)<.65f)){Message="Leave space between staff homes.";return false;}
            draft[role]=point;Message="Home placed — save layout to keep it.";return true;
        }
        public void ResetCursor(){if(day.Settings!=null)Cursor=Home(Roles[SelectedRole]);}
        public void Cycle(){SelectedRole=(SelectedRole+1)%Roles.Length;ResetCursor();}
        public void Navigate(Vector2 direction)
        {
            var p=Cursor+new Vector3(direction.x,0,direction.y)*.4f;
            p.x=Mathf.Clamp(p.x,RestaurantAccounts.Current.Owns(RestaurantExpansion.KitchenId)?-11.8f:-8,RestaurantAccounts.Current.Owns(RestaurantExpansion.DiningId)?16.8f:12.6f);
            p.z=Mathf.Clamp(p.z,-5.2f,6.4f);Cursor=p;
        }
        public bool Place()=>TrySet(Roles[SelectedRole],Cursor);
        public void Draw(Camera camera)
        {
            Vector2 Screen(Vector3 p){var s=camera.WorldToViewportPoint(p+Vector3.up*.2f);return new Vector2(s.x*1280,(1-s.y)*720);}
            var style=new GUIStyle(GUI.skin.label){fontSize=16,alignment=TextAnchor.MiddleCenter};style.normal.textColor=Color.white;
            foreach(var role in Roles)
            {
                var p=Screen(Home(role));GUI.color=new Color(.3f,.8f,1);GUI.DrawTexture(new Rect(p.x-3,p.y-3,6,6),Texture2D.whiteTexture);
                float top=p.y-42-(role=="busser"?30:0)+(role=="host"?30:0);GUI.color=new Color(.08f,.3f,.5f,.9f);GUI.DrawTexture(new Rect(p.x-49,top,98,30),Texture2D.whiteTexture);
                GUI.color=Color.white;GUI.Label(new Rect(p.x-49,top,98,30),role,style);
            }
            var cursor=Screen(Cursor);GUI.color=Valid(Cursor,out _)?Color.yellow:new Color(1,.25f,.2f);GUI.DrawTexture(new Rect(cursor.x-7,cursor.y-7,14,14),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Box(new Rect(240,630,800,68),"STAFF HOME: "+Roles[SelectedRole]+" — RB / Tab: next employee\nStick / D-pad: move marker   A: place   LB: equipment   Y: save\n"+Message);
        }
    }
}
