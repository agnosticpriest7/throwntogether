using UnityEngine;

namespace ThrownTogether
{
    public sealed class RestaurantHud : MonoBehaviour
    {
        public ChefController chef;
        public CustomerOrder order;
        public Camera gameplayCamera;
        private GUIStyle label, small, title;
        private void OnGUI()
        {
            if (chef == null || order == null) return;
            if (label == null)
            {
                label=new GUIStyle(GUI.skin.label) { fontSize=21, alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold };
                small=new GUIStyle(label) { fontSize=17 };
                title=new GUIStyle(label) { fontSize=26 };
            }
            var previous=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.Box(new Rect(20,14,1240,82),GUIContent.none);
            GUI.Label(new Rect(30,16,1220,34),"THROWN TOGETHER  /  FIRST SERVICE",title);
            string status=order.Phase == OrderPhase.Waiting ? "ORDER: 1 PLATE OF FRIES" : order.Phase == OrderPhase.Delivering ? "DELIVERING FRIES…" : order.Phase == OrderPhase.Eating ? "CUSTOMER IS EATING…" : "ORDER COMPLETE!  Thanks, chef!   •   R / Start to play again";
            GUI.Label(new Rect(30,53,1220,30),status,label);
            foreach(var station in Interactable.Active)
            {
                if (station == null) continue;
                var p=gameplayCamera.WorldToViewportPoint(station.transform.position+Vector3.up*1.6f);
                var rect=new Rect(p.x*1280-94,(1-p.y)*720-54,188,44);
                GUI.backgroundColor=station==chef.Focus ? new Color(.15f,.9f,.65f) : Color.black;
                GUI.Box(rect,GUIContent.none); GUI.Label(rect,station.Status,small);
                if (station.Progress>=0) { GUI.color=new Color(.3f,1,.65f); GUI.DrawTexture(new Rect(rect.x,rect.yMax,rect.width*station.Progress,7),Texture2D.whiteTexture); GUI.color=Color.white; }
            }
            GUI.backgroundColor=Color.black;
            GUI.Box(new Rect(20,605,1240,100),GUIContent.none);
            string prompt=chef.Focus != null ? "[E / A] " + chef.Focus.Prompt(chef) : "Approach a station and face it";
            GUI.Label(new Rect(30,610,1220,30),prompt,label);
            GUI.Label(new Rect(30,644,1220,25),"CARRYING: " + (chef.Hands.Item == null ? "Nothing" : chef.Hands.Item.Payload.Label) + "    •    Move: WASD / arrows / left stick    •    Use: E / Space / A",small);
            GUI.Label(new Rect(30,675,1220,22),"Potato → Prep → Fryer → Combine with a plate → Pickup     |     R / Start: restart",small);
            GUI.matrix=previous; GUI.color=Color.white; GUI.backgroundColor=Color.white;
        }
    }
}
