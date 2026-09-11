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
        private PracticeGuide guide;
        private SessionSummary summary;
        private int lastCompleted;
        private float completedUntil;
        private void Awake()
        {
            menu=gameObject.AddComponent<RestaurantMenu>();
            gameObject.AddComponent<KitchenPresentation>();
            summary=gameObject.AddComponent<SessionSummary>();
            guide=gameObject.AddComponent<PracticeGuide>();
#if UNITY_EDITOR || THROWNTOGETHER_DIAGNOSTICS
            gameObject.AddComponent<DevelopmentDiagnostics>();
#endif
        }
        public bool ShowControlHelp => guide!=null && guide.Active;
        private void Update()
        {
            int completed=shift!=null ? shift.CompletedCount : order.Phase==OrderPhase.Complete ? 1:0;
            if(completed>lastCompleted) completedUntil=Time.time+2;
            lastCompleted=completed;
        }
        private void OnGUI()
        {
            if(chef==null || order==null || menu.IsOpen) return;
            if(label==null)
            {
                label=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,wordWrap=true };
                small=new GUIStyle(label); title=new GUIStyle(label);
            }
            label.fontSize=Mathf.RoundToInt(19*RestaurantMenu.Display.TextScale);
            small.fontSize=Mathf.RoundToInt(15*RestaurantMenu.Display.TextScale);
            title.fontSize=Mathf.RoundToInt(22*RestaurantMenu.Display.TextScale);
            label.normal.textColor=small.normal.textColor=title.normal.textColor=Color.white;
            var previous=GUI.matrix;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            int total=shift!=null ? shift.TotalOrders:1;
            var day=shift?.Day;
            Panel(new Rect(20,12,300,48));
            GUI.Label(new Rect(26,18,188,35),lastCompleted+" / "+total+" served",label);
            GUI.Label(new Rect(214,18,98,35),day!=null?day.Clock:System.TimeSpan.FromSeconds(summary.ElapsedSeconds).ToString(@"mm\:ss"),small);
            if(day!=null)
            {
                Panel(new Rect(340,12,690,48));
                GUI.Label(new Rect(350,15,670,42),"Day "+day.ServiceDayNumber+" | Bank $"+RestaurantAccounts.Current.Data.cash+" | Today $"+day.NetIncome+" | Outside "+day.WaitingOutside+" ("+Mathf.CeilToInt(Mathf.Max(0,day.Settings.outsidePatience-day.OldestWait))+"s) | Left "+day.LostCustomers,small);
                if(day.Elapsed-day.LastWasteAt<2){Panel(new Rect(490,94,300,30));GUI.Label(new Rect(490,94,300,30),"Food discarded: $"+day.Settings.wasteCost+" waste",small);}
                if(day.Elapsed-day.LastLostAt<3){Panel(new Rect(390,66,500,28));GUI.Label(new Rect(395,66,490,28),"Customer left unhappy — waited too long",small);}
            }
            if(GUI.Button(new Rect(1060,18,182,39),"Y / Esc: Menu")) menu.Open();
            if(shift==null) DrawOrderBubble(order,1);
            else for(int i=0;i<shift.seats.Length;i++)DrawOrderBubble(shift.seats[i],i+1);
            foreach(var station in Interactable.Active)
            {
                if(station==null || station.gameObject.scene!=gameObject.scene || station.Progress<0) continue;
                var p=gameplayCamera.WorldToViewportPoint(station.transform.position+new Vector3(0,1.27f,-.63f));
                var rect=new Rect(p.x*1280-37,(1-p.y)*720,74,8);
                GUI.color=new Color(.07f,.1f,.1f); GUI.DrawTexture(rect,Texture2D.whiteTexture);
                GUI.color=new Color(1,.75f,.2f); rect.width*=station.Progress; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white;
            }
            DrawSuccessCues();
            // Identity is carried by apron colors and target borders; no floating panels cover chefs.
            bool two=coop!=null && coop.PlayerTwo!=null;
            if(ShowControlHelp)
            {
                DrawPlayer(chef,"P1",new Color(.2f,1,.7f),two?20:240,two ? 612:800);
                if(two) DrawPlayer(coop.PlayerTwo,"P2",new Color(1,.42f,.32f),648,612);
            }
            if(coop!=null && !string.IsNullOrEmpty(coop.ConnectionHelp))
            {
                Panel(new Rect(160,93,960,45)); GUI.Label(new Rect(170,93,940,45),coop.ConnectionHelp,small);
            }
            else if(Time.time<completedUntil)
            { Panel(new Rect(490,94,300,30)); GUI.Label(new Rect(490,94,300,30),"Dish served!",label); }
            if(guide.Active)
            {
                Panel(new Rect(170,555,940,55)); GUI.Label(new Rect(180,555,920,55),guide.Instruction,label);
                if(guide.SuggestedTarget!=null)
                {
                    var p=gameplayCamera.WorldToViewportPoint(guide.SuggestedTarget.transform.position+Vector3.up*2.05f);
                    GUI.color=new Color(1,.83f,.25f); GUI.Label(new Rect(p.x*1280-20,(1-p.y)*720-25,40,30),"v",title); GUI.color=Color.white;
                }
            }
            bool complete=shift!=null ? shift.Complete : order.Phase==OrderPhase.Complete;
            if(complete && day!=null)
            {
                Panel(new Rect(340,170,600,285));
                GUI.Label(new Rect(355,180,570,45),"10 PM — DAY "+day.ServiceDayNumber+" COMPLETE",title);
                GUI.Label(new Rect(355,230,570,90),day.Served+" meals: $"+day.BaseIncome+"\nQuick-service bonus: $"+day.Bonuses+"  |  Customers lost: "+day.LostCustomers+"\nFood waste: -$"+day.WasteFees,label);
                GUI.Label(new Rect(355,325,570,45),day.Paid?"Paid to your bank: $"+day.NetIncome:RestaurantAccounts.Current.Problem,small);
                if(GUI.Button(new Rect(395,385,490,45),"Y / Esc menu — improvements and next day"))menu.OpenRestaurant();
            }
            else if(complete)
            {
                Panel(new Rect(340,170,600,285));
                GUI.Label(new Rect(355,178,570,45),shift!=null ? "SHIFT COMPLETE":"FIRST SERVICE COMPLETE",title);
                GUI.Label(new Rect(355,230,570,45),lastCompleted+" dishes  |  "+System.TimeSpan.FromSeconds(summary.ElapsedSeconds).ToString(@"mm\:ss"),label);
                GUI.Label(new Rect(355,285,570,85),"P1  "+summary.PlayerOne.Description+"\nP2  "+summary.PlayerTwo.Description,small);
                GUI.Label(new Rect(355,380,570,40),"Y / Esc: replay, choose a shift or practice",label);
            }
            GUI.matrix=previous; GUI.color=Color.white; GUI.backgroundColor=Color.white;
        }
        private void DrawSuccessCues()
        {
            foreach(var station in Interactable.Active)
            {
                if(station==null || station.gameObject.scene!=gameObject.scene || station.SuccessOpacity<=0) continue;
                var p=gameplayCamera.WorldToViewportPoint(station.transform.position+Vector3.up*1.3f);
                if(p.z<=0 || p.x<0 || p.x>1 || p.y<0 || p.y>1) continue;
                float x=p.x*1280, y=(1-p.y)*720;
                GUI.color=RestaurantMenu.Display.highContrast ? new Color(1,1,1,station.SuccessOpacity) : new Color(.55f,1,.78f,station.SuccessOpacity);
                if(station.SuccessCheck)
                {
                    // Two strokes avoid relying on a font's Unicode checkmark glyph.
                    var matrix=GUI.matrix;
                    GUIUtility.RotateAroundPivot(45,new Vector2(x-5,y-20));
                    GUI.DrawTexture(new Rect(x-5,y-20,9,3),Texture2D.whiteTexture);
                    GUI.matrix=matrix;
                    GUIUtility.RotateAroundPivot(-45,new Vector2(x,y-14));
                    GUI.DrawTexture(new Rect(x,y-14,19,3),Texture2D.whiteTexture);
                    GUI.matrix=matrix;
                }
                else
                {
                    GUI.DrawTexture(new Rect(x-24,y-12,48,2),Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x-24,y+12,48,2),Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x-24,y-12,2,26),Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x+22,y-12,2,26),Texture2D.whiteTexture);
                }
            }
            GUI.color=Color.white;
        }
        private void DrawPlayer(ChefController player,string id,Color color,float x,float width)
        {
            Panel(new Rect(x,624,width,68));
            GUI.color=color; GUI.DrawTexture(new Rect(x,624,5,68),Texture2D.whiteTexture); GUI.color=Color.white;
            string prompt=player.Focus!=null ? "A / E: "+player.Focus.Prompt(player):"Face a station to interact";
            GUI.Label(new Rect(x+12,628,width-24,29),id+"  "+prompt,label);
            GUI.Label(new Rect(x+12,657,width-24,29),player.Hands.Item==null ? "Hands empty" : player.Hands.Item.Payload.Label,small);
        }
        private static void Panel(Rect rect)
        {
            GUI.color=RestaurantMenu.Display.highContrast ? Color.black:new Color(.035f,.055f,.07f,.88f);
            GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white;
        }
        private void DrawOrderBubble(CustomerOrder ticket,int number)
        {
            if(!ticket.Active||ticket.recipe==null||ticket.customerVisual==null)return;
            var point=gameplayCamera.WorldToViewportPoint(ticket.customerVisual.position+Vector3.up*1.7f);if(point.z<=0)return;
            float scale=Mathf.Clamp(RestaurantMenu.Display.TextScale,1,1.4f);
            var rect=OrderBubbleLayout.ForSeat(new Vector2(point.x*1280,(1-point.y)*720),scale);
            bool contrast=RestaurantMenu.Display.highContrast;
            var ink=contrast?Color.white:new Color(.16f,.23f,.24f);
            GUI.color=new Color(.23f,.31f,.32f);GUI.DrawTexture(new Rect(rect.x-7,rect.y+27*scale,10,10),Texture2D.whiteTexture);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=contrast?Color.black:new Color(.97f,.94f,.85f);GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),Texture2D.whiteTexture);GUI.color=Color.white;
            small.fontSize=Mathf.RoundToInt(13*scale);small.normal.textColor=ink;
            GUI.Label(new Rect(rect.x+6,rect.y+3,rect.width-12,22*scale),"TABLE "+number,small);
            FoodIcon.Draw(new Rect(rect.x+8,rect.y+28*scale,48*scale,42*scale),ticket.recipe.ingredient);
            label.fontSize=Mathf.RoundToInt(18*scale);label.normal.textColor=ink;
            GUI.Label(new Rect(rect.x+62*scale,rect.y+23*scale,rect.width-68*scale,49*scale),ticket.recipe.displayName,label);
            GUI.color=ticket.Phase==OrderPhase.Waiting?new Color(.04f,.12f,.13f):ticket.Phase==OrderPhase.Delivering?new Color(.17f,.09f,.025f):new Color(.05f,.14f,.04f);
            GUI.DrawTexture(new Rect(rect.x+6,rect.y+75*scale,rect.width-12,20*scale),Texture2D.whiteTexture);GUI.color=Color.white;small.normal.textColor=Color.white;
            GUI.Label(new Rect(rect.x+6,rect.y+74*scale,rect.width-12,22*scale),OrderBubbleLayout.State(ticket.Phase),small);
            var table=ticket.manualService ? ticket.tableSlot.GetComponentInParent<DiningTable>():null;
            if(table!=null && table.WaitingForMeal)
            {
                var bar=new Rect(rect.x+6,rect.y+97*scale,rect.width-12,7*scale);
                GUI.color=new Color(.12f,.16f,.17f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
                bar.width*=table.PatienceRemaining;GUI.color=Color.Lerp(new Color(.9f,.22f,.12f),new Color(.22f,.7f,.36f),table.PatienceRemaining);GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=Color.white;
            }
            label.fontSize=Mathf.RoundToInt(19*RestaurantMenu.Display.TextScale);small.fontSize=Mathf.RoundToInt(15*RestaurantMenu.Display.TextScale);label.normal.textColor=Color.white;
        }
    }
}
