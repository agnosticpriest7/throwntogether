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
        private RestaurantMenu menu;
        private int lastCompleted;
        private float completedUntil;
        private void Awake()
        {
            menu=gameObject.AddComponent<RestaurantMenu>();
#if UNITY_EDITOR || THROWNTOGETHER_DIAGNOSTICS
            gameObject.AddComponent<DevelopmentDiagnostics>();
#endif
        }
        private void Start()
        {
            Carryable assets=null;
            foreach(var source in FindObjectsByType<SourceStation>(FindObjectsSortMode.None))
            {
                if(source.gameObject.scene!=gameObject.scene) continue;
                assets=source.itemPrefab;
                if(source.plates) continue;
                var preview=Instantiate(source.itemPrefab,source.transform);
                preview.name="Ingredient display"; preview.transform.localPosition=new Vector3(0,1.65f,0);
                preview.transform.localScale=Vector3.one*1.35f;
                preview.Configure(new ItemPayload { ingredient=source.ingredient,state=FoodState.Raw });
            }
            if(assets!=null) foreach(var station in FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None))
                if(station.gameObject.scene==gameObject.scene) station.gameObject.AddComponent<StationPresentation>().Initialize(station,assets);
        }
        private void Update()
        {
            int completed=shift!=null ? shift.CompletedCount : order.Phase==OrderPhase.Complete ? 1:0;
            if(completed>lastCompleted) completedUntil=Time.time+2;
            lastCompleted=completed;
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
            label.fontSize=Mathf.RoundToInt(18*RestaurantMenu.Display.TextScale);
            small.fontSize=Mathf.RoundToInt(15*RestaurantMenu.Display.TextScale);
            label.wordWrap=true; small.wordWrap=true;
            label.normal.textColor=Color.white; small.normal.textColor=Color.white; title.normal.textColor=Color.white;
            var previous=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            Panel(new Rect(20,14,1240,94));
            GUI.Label(new Rect(30,16,1000,34),shift==null ? "THROWN TOGETHER / PRACTICE" : "THROWN TOGETHER / RESTAURANT SHIFT",title);
            if(GUI.Button(new Rect(1050,20,190,28),"Y / Esc — Menu")) menu.Open();
            if(shift==null) DrawTicket(order,new Rect(250,50,780,54),1);
            if(shift!=null)
            {
                if(shift.Complete) GUI.Label(new Rect(30,51,1220,45),"SHIFT COMPLETE • "+shift.CompletedCount+" dishes • "+System.TimeSpan.FromSeconds(shift.ElapsedSeconds).ToString(@"mm\:ss"),label);
                else for(int i=0;i<shift.seats.Length;i++) DrawTicket(shift.seats[i],new Rect(30+i*600,50,590,54),i+1);
                GUI.Label(new Rect(1040,108,210,25),lastCompleted+" / "+shift.definition.orders.Length+" served",small);
            }
            foreach(var station in Interactable.Active)
            {
                if (station == null || station.gameObject.scene!=gameObject.scene) continue;
                var p=gameplayCamera.WorldToViewportPoint(station.transform.position+Vector3.up*1.6f);
                float stationWidth=shift!=null && station is SourceStation ? 145 : 188;
                var rect=new Rect(p.x*1280-stationWidth/2,(1-p.y)*720-54,stationWidth,50);
                bool p1=station==chef.Focus, p2=coop!=null && coop.PlayerTwo!=null && station==coop.PlayerTwo.Focus;
                GUI.backgroundColor=p1 ? new Color(.15f,.9f,.65f) : p2 ? new Color(1,.38f,.3f) : Color.black;
                Panel(rect); GUI.Label(rect,StationLabel(station),small);
                if(p1 || p2) GUI.Label(new Rect(rect.x,rect.y-20,rect.width,22),(p1 ? "P1 " : "")+(p2 ? "P2" : ""),small);
                if (station.Progress>=0) { GUI.color=new Color(.3f,1,.65f); GUI.DrawTexture(new Rect(rect.x,rect.yMax,rect.width*station.Progress,7),Texture2D.whiteTexture); GUI.color=Color.white; }
            }
            GUI.backgroundColor=Color.black;
            DrawChefBadge(chef,"P1"); if(coop!=null && coop.PlayerTwo!=null) DrawChefBadge(coop.PlayerTwo,"P2");
            Panel(new Rect(20,590,1240,110));
            DrawPlayer(chef,"P1",30,coop!=null && coop.PlayerTwo!=null ? 600 : 1220);
            if(coop!=null && coop.PlayerTwo!=null) DrawPlayer(coop.PlayerTwo,"P2",650,600);
            GUI.Label(new Rect(30,676,1220,23),(coop!=null ? coop.Status : "Solo")+" | Y / Esc: menu",small);
            if(Time.time<completedUntil) { Panel(new Rect(450,115,380,32)); GUI.Label(new Rect(450,115,380,32),"DISH SERVED ✓",label); }
            GUI.matrix=previous; GUI.color=Color.white; GUI.backgroundColor=Color.white;
        }
        private void DrawPlayer(ChefController player,string id,float x,float width)
        {
            string prompt=player.Focus!=null ? "[A / E] "+player.Focus.Prompt(player) : "Approach and face a station";
            GUI.Label(new Rect(x,593,width,37),id+" • "+prompt,label);
            GUI.Label(new Rect(x,632,width,25),"Holding: "+(player.Hands.Item==null ? "Nothing" : player.Hands.Item.Payload.Label),label);
            GUI.Label(new Rect(x,658,width,20),string.IsNullOrEmpty(player.Feedback) ? "Move: WASD / stick • Use: E / Space / A" : player.Feedback,small);
        }
        private static void Panel(Rect rect)
        {
            GUI.color=RestaurantMenu.Display.highContrast ? Color.black : new Color(.04f,.08f,.11f,.94f);
            GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white;
        }
        private static string StationLabel(Interactable station)
        {
            if(station is SourceStation source) return source.plates ? "Plates" : source.ingredient.displayName;
            if(station is ProcessingStation process) return (process.recipe.input==FoodState.Raw ? "Prep":"Fryer")+
                (process.Busy ? "\nWorking "+Mathf.RoundToInt(process.Progress*100)+"%" : process.slot.Item!=null ? "\nREADY":"");
            if(station is ServiceStation) return "Service pickup"+(station.Progress>=0 ? "\nDelivering":"");
            if(station is CounterStation counter) return (station.stationName=="PLATING COUNTER" ? "Plating":"Spare counter")+
                (counter.slot.Item==null ? "" : "\n"+(counter.slot.Item.Payload.isPlate ? counter.slot.Item.Payload.EmptyPlate ? "Clean plate":"Plated dish" : counter.slot.Item.Payload.state.ToString()));
            return station.stationName;
        }
        private void DrawTicket(CustomerOrder ticket,Rect rect,int number)
        {
            if(!ticket.Active) { GUI.Label(rect,"No remaining ticket",label); return; }
            FoodIcon.Draw(new Rect(rect.x+12,rect.y+5,40,40),ticket.recipe.ingredient);
            string state=ticket.Phase==OrderPhase.Waiting ? "WAITING" : ticket.Phase==OrderPhase.Delivering ? "DELIVERING →" : ticket.Phase==OrderPhase.Eating ? "EATING" : "SERVED ✓";
            GUI.Label(new Rect(rect.x+60,rect.y,rect.width-65,rect.height),"#"+number+" "+ticket.recipe.displayName+"\n"+state,label);
        }
        private void DrawChefBadge(ChefController player,string name)
        {
            var p=gameplayCamera.WorldToViewportPoint(player.transform.position+Vector3.up*2.1f);
            var rect=new Rect(p.x*1280-24,(1-p.y)*720-12,48,24); Panel(rect); GUI.Label(rect,name,small);
        }
    }
}
