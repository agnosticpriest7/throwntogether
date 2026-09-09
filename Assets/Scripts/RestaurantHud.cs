using UnityEngine;

namespace ThrownTogether
{
    public sealed class RestaurantHud : MonoBehaviour
    {
        public ChefController chef;
        public CustomerOrder order;
        public Camera gameplayCamera;
        public SettingsService settings;
        public RestaurantAudioFeedback audioFeedback;
        public LocalCoopSession coop;
        public RestaurantShift shift;
        private GUIStyle label, small, title;
        private void Awake()
        {
#if UNITY_EDITOR || THROWNTOGETHER_DIAGNOSTICS
            gameObject.AddComponent<DevelopmentDiagnostics>();
#endif
        }
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
            GUI.Label(new Rect(30,16,1000,34),shift==null ? "THROWN TOGETHER / PRACTICE" : "THROWN TOGETHER / RESTAURANT SHIFT",title);
            if(GUI.Button(new Rect(1050,20,190,28),shift==null ? "Play restaurant shift" : "Return to practice"))
                UnityEngine.SceneManagement.SceneManager.LoadScene(shift==null ? "RestaurantShift" : "RestaurantDevelopment");
            if(coop!=null && coop.PlayerTwo==null && GUI.Button(new Rect(1050,99,190,25),coop.KeyboardPlayerOne ? "P1 keyboard • A joins P2" : "Keyboard P1 + pad P2")) coop.UseKeyboardPlayerOne();
            string status=order.Phase == OrderPhase.Waiting ? "ORDER: 1 PLATE OF FRIES" : order.Phase == OrderPhase.Delivering ? "DELIVERING FRIES…" : order.Phase == OrderPhase.Eating ? "CUSTOMER IS EATING…" : "ORDER COMPLETE!  Thanks, chef!   •   "+ChefInput.RestartHint;
            if(shift==null) GUI.Label(new Rect(30,53,1220,30),status,label);
            if(shift!=null)
            {
                GUI.Box(new Rect(20,50,1240,50),GUIContent.none);
                GUI.Label(new Rect(30,51,1220,45),shift.Complete ? "SHIFT COMPLETE • "+shift.CompletedCount+" dishes • "+System.TimeSpan.FromSeconds(shift.ElapsedSeconds).ToString(@"mm\:ss")+" • Solo or co-op: try again!" :
                    shift.CompletedCount+" / "+shift.definition.orders.Length+" served | "+shift.Tickets,small);
            }
            foreach(var station in Interactable.Active)
            {
                if (station == null) continue;
                var p=gameplayCamera.WorldToViewportPoint(station.transform.position+Vector3.up*1.6f);
                float stationWidth=shift!=null && station is SourceStation ? 130 : 188;
                var rect=new Rect(p.x*1280-stationWidth/2,(1-p.y)*720-54,stationWidth,44);
                bool p1=station==chef.Focus, p2=coop!=null && coop.PlayerTwo!=null && station==coop.PlayerTwo.Focus;
                GUI.backgroundColor=p1 ? new Color(.15f,.9f,.65f) : p2 ? new Color(1,.38f,.3f) : Color.black;
                GUI.Box(rect,GUIContent.none); GUI.Label(rect,station.Status,small);
                if(p1 || p2) GUI.Label(new Rect(rect.x,rect.y-20,rect.width,22),(p1 ? "P1 " : "")+(p2 ? "P2" : ""),small);
                if (station.Progress>=0) { GUI.color=new Color(.3f,1,.65f); GUI.DrawTexture(new Rect(rect.x,rect.yMax,rect.width*station.Progress,7),Texture2D.whiteTexture); GUI.color=Color.white; }
            }
            GUI.backgroundColor=Color.black;
            GUI.Box(new Rect(20,605,1240,100),GUIContent.none);
            DrawPlayer(chef,"P1",30,coop!=null && coop.PlayerTwo!=null ? 600 : 1220);
            if(coop!=null && coop.PlayerTwo!=null) DrawPlayer(coop.PlayerTwo,"P2",650,600);
            GUI.Label(new Rect(30,675,1220,22),coop!=null ? coop.Status+" | "+ChefInput.RestartHint : ChefInput.RestartHint,small);
            GUI.matrix=previous; GUI.color=Color.white; GUI.backgroundColor=Color.white;
        }
        private void DrawPlayer(ChefController player,string id,float x,float width)
        {
            string prompt=player.Focus!=null ? "[A / E] "+player.Focus.Prompt(player) : "Approach and face a station";
            GUI.Label(new Rect(x,608,width,27),id+" • "+prompt,small);
            GUI.Label(new Rect(x,634,width,22),"Holding: "+(player.Hands.Item==null ? "Nothing" : player.Hands.Item.Payload.Label),small);
            GUI.Label(new Rect(x,655,width,20),string.IsNullOrEmpty(player.Feedback) ? "Move: WASD / stick • Use: E / Space / A" : player.Feedback,small);
        }
    }
}
