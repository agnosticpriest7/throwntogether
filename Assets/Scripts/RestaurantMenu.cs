using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ThrownTogether
{
    // Run before chef input so opening/closing a menu never leaks its A press.
    [DefaultExecutionOrder(-1000)]
    public sealed class RestaurantMenu : MonoBehaviour
    {
        public static RestaurantMenu Instance { get; private set; }
        public static bool GameplayBlocked => Instance!=null && (Instance.IsOpen || Instance.GetComponent<DayPresentation>()?.Transitioning==true || Instance.hud?.shift?.Day?.Closed==true || Instance.hud?.shift?.Day?.AwaitingMenu==true || Time.frameCount<=Instance.blockThroughFrame);
        public static DisplaySettingsData Display => Instance?.hud?.settings?.Repository?.Display ?? defaults;
        private static readonly DisplaySettingsData defaults=new DisplaySettingsData();
        private static bool booted;
        private static bool forceFrontEnd;
        public static void ShowFrontEndOnNextLoad(){forceFrontEnd=true;gateAfterLoad=true;}
        private static bool gateAfterLoad;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance=null; booted=false; gateAfterLoad=false; forceFrontEnd=false; }
        public bool IsOpen { get; private set; }
        public bool IsFrontEnd { get; private set; }
        public int Selection { get; private set; }
        public string Page { get; private set; }="Main";
        private RestaurantHud hud;
        private InputActionMap controls;
        private InputAction toggle, navigate, accept, back, rotateLayout, cycleFurniture;
        private float savedTimeScale=1, nextNavigation;
        private int blockThroughFrame=-1;
        private Action pending;
        private string confirmation, message="";
        private bool navigationHeld;
        private bool practiceLevel;
        private Action startMenuDay;
        private RecipeBook recipeBook;
        private int recipeIndex;
        private int wardrobePlayer, pendingKitchen;
        private bool wardrobeBeforeLaunch;
        private ChefWardrobePreview wardrobePreview;
        private bool WardrobePage => Page=="Your chef" || Page=="Face" || Page=="Accessories";
        private GUIStyle buttonStyle, textStyle;
        private Texture2D mainMenuBackground;
        private string resetReturnPage, confirmReturnPage;
        private int shopTab;
        private readonly string[] shopTabs={"Appliances","Counters","Dishes","Upgrades","Sell"};
        private bool BetweenDays=>!IsFrontEnd && hud.shift?.Day!=null && (hud.shift.Day.Closed || hud.shift.Day.AwaitingMenu);
        public string[] VisibleOptions=>rows.Select(r=>r.label).ToArray();
        public void SelectRow(int value){Selection=Mathf.Clamp(value,0,rows.Count-1);}
        public void OpenShop(){SetPage("Shop");}
        private readonly List<Row> rows=new List<Row>();
        private sealed class Row { public string label; public Action select; public Action<int> adjust; public bool enabled=true; }
        private void Awake()
        {
            hud=GetComponent<RestaurantHud>(); Instance=this;
            recipeBook=Resources.Load<RecipeBook>("RecipeBook");
            mainMenuBackground=Resources.Load<Texture2D>("MainMenuBackground");
            controls=new InputActionMap("Restaurant menu");
            rotateLayout=controls.AddAction("Rotate furniture",InputActionType.Button);rotateLayout.AddBinding("<Gamepad>/buttonWest");rotateLayout.AddBinding("<Keyboard>/r");
            cycleFurniture=controls.AddAction("Choose furniture",InputActionType.Button);cycleFurniture.AddBinding("<Gamepad>/rightShoulder");cycleFurniture.AddBinding("<Keyboard>/tab");
            toggle=controls.AddAction("Menu",InputActionType.Button); toggle.AddBinding("<Gamepad>/buttonNorth"); toggle.AddBinding("<Keyboard>/escape");
            navigate=controls.AddAction("Navigate",InputActionType.Value); navigate.AddBinding("<Gamepad>/dpad"); navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            accept=controls.AddAction("Accept",InputActionType.Button); accept.AddBinding("<Gamepad>/buttonSouth"); accept.AddBinding("<Keyboard>/enter");
            back=controls.AddAction("Back",InputActionType.Button); back.AddBinding("<Gamepad>/buttonEast"); back.AddBinding("<Keyboard>/backspace");
        }
        private void Start()
        {
            if(forceFrontEnd || (!booted && !Application.isEditor)) OpenFrontEnd(); booted=true; forceFrontEnd=false;
            if(gateAfterLoad)
            {
                blockThroughFrame=Time.frameCount+1;
                foreach(var input in FindObjectsByType<ChefInput>(FindObjectsSortMode.None)) input.RequireActionRelease();
                gateAfterLoad=false;
            }
        }
        private void OnEnable() { Instance=this; controls.Enable(); }
        private void OnDisable() { Close(); controls.Disable(); }
        private void OnDestroy() { if(Instance==this) Instance=null; controls.Dispose(); }
        public void Open()
        {
            if(!IsOpen) { savedTimeScale=Time.timeScale; Time.timeScale=0; IsOpen=true; }
            SetPage(BetweenDays?"Restaurant":"Main");
        }
        public void OpenFrontEnd() { IsFrontEnd=true; Open(); }
        public void OpenRecipeBook() { if(!IsOpen) Open(); SetPage("Recipes"); }
        public void ShowLevels(bool practice) { practiceLevel=practice; SetPage("Levels"); }
        private void LaunchLevel(int index)
        {
            pendingKitchen=index; wardrobeBeforeLaunch=false; FinishLaunch();
        }
        private void FinishLaunch()
        {
            int index=pendingKitchen;
            SessionOptions.Kitchen=index; SessionOptions.Training="Free practice";
            Action launch=()=>{IsFrontEnd=false;Close();Load(practiceLevel ? "RestaurantDevelopment":"RestaurantShift");};
            if(!practiceLevel && SessionOptions.ShiftOrders==0)ChooseDailyMenu(launch);else launch();
        }
        public void ChooseDailyMenu(Action start)
        {
            if(!IsOpen)Open();startMenuDay=start;SetPage("Today's Menu");
        }
        public bool StartSelectedMenu()
        {
            if(!DailyMenu.CanStart(RestaurantAccounts.Current)){message=DailyMenu.StartProblem(RestaurantAccounts.Current);return false;}
            var start=startMenuDay;if(start==null)return false;start.Invoke();
            if(hud.shift?.Day?.AwaitingMenu==true){message=RestaurantAccounts.Current.Problem;return false;}
            startMenuDay=null;return true;
        }
        public void ShowWardrobe()
        {
            if(!IsOpen) Open();
            if(wardrobePreview==null) wardrobePreview=gameObject.AddComponent<ChefWardrobePreview>();
            SetPage("Your chef"); RefreshAppearance();
        }
        private void RefreshAppearance()
        {
            var choice=ChefWardrobe.ForPlayer(wardrobePlayer); choice.Normalize();
            wardrobePreview?.Show(choice);
            var chef=wardrobePlayer==0 ? hud.chef:hud.coop?.PlayerTwo;
            chef?.GetComponentInChildren<ChefAppearance>()?.Apply(choice);
        }
        private void Choice(string label,string[] names,Func<int> read,Action<int> write)
        {
            Action<int> change=dir=>{write((read()+dir+names.Length)%names.Length);RefreshAppearance();};
            Add(label+": "+names[read()]+"  < >",()=>change(1),change);
        }
        public void NavigateBack()
        {
            if(Page=="Restaurant")return;
            if(Page=="Confirm"){SetPage(confirmReturnPage);return;}
            if(Page=="Employees" || Page=="Shop" || Page=="Today's Menu" && !IsFrontEnd){SetPage("Restaurant");return;}
            if(BetweenDays && (Page=="Settings" || Page=="Audio" || Page=="Display")){SetPage(Page=="Settings"?"Restaurant":"Settings");return;}
            if(Page=="Reset career"){SetPage(resetReturnPage);return;}
            if(Page=="Face" || Page=="Accessories") {SetPage("Your chef");return;}
            if(Page=="Your chef") {SetPage(wardrobeBeforeLaunch ? "Levels":"Main");return;}
            if(IsFrontEnd) SetPage("Title");
            else if(Page=="Main") Close(); else SetPage("Main");
        }
        public void Close()
        {
            if(!IsOpen) return;
            GetComponent<KitchenFurniture>()?.Cancel();
            if(IsOpen) Time.timeScale=savedTimeScale;
            IsOpen=false; blockThroughFrame=Time.frameCount+1;
            hud.coop?.RefreshPlayerOneAssignment();
            foreach(var input in FindObjectsByType<ChefInput>(FindObjectsSortMode.None)) input.RequireActionRelease();
        }
        private void SetPage(string page) { Page=page=="Main" && IsFrontEnd ? "Title":page; Selection=page=="Main" && IsFrontEnd || page=="Title" ? 1:0; message=""; BuildRows(); }
        private void Add(string label,Action select,Action<int> adjust=null) => rows.Add(new Row {label=label,select=select,adjust=adjust});
        private void AddUnavailable(string label) { Add(label+" — coming later",()=>{}); rows[rows.Count-1].enabled=false; }
        private void BuildRows()
        {
            rows.Clear();
            if(Page=="Kitchen layout")return;
            if(Page=="Reset career")
            {
                Add("Cancel — keep career",()=>SetPage(resetReturnPage));
                Add("Reset career — erase progress",()=>{
                    if(!RestaurantAccounts.Current.ResetCareer()){message=RestaurantAccounts.Current.Problem;return;}
                    startMenuDay=null;SessionOptions.ShiftOrders=0;SessionOptions.Kitchen=0;
                    ShowFrontEndOnNextLoad();Close();Load("RestaurantDevelopment");
                });return;
            }
            if(Page=="Title")
            {
                AddUnavailable("Tutorial");
                Add("Quick Play",()=>ShowLevels(false));
                Add("Career — restaurant days",()=>{SessionOptions.ShiftOrders=0;ShowLevels(false);});
                AddUnavailable("Trials");
                AddUnavailable("Endless");
                Add("Recipe book",OpenRecipeBook);
                Add("Co-op setup",()=>SetPage("Co-op"));
                Add("Choose your chef",()=>{wardrobeBeforeLaunch=false;ShowWardrobe();});
                Add("Settings",()=>SetPage("Settings")); return;
            }
            if(WardrobePage)
            {
                var a=ChefWardrobe.ForPlayer(wardrobePlayer);
                if(Page=="Your chef")
                {
                    Choice("Player",new[]{"Player 1","Player 2"},()=>wardrobePlayer,v=>wardrobePlayer=v);
                    Choice("Build",ChefWardrobe.Builds,()=>a.build,v=>a.build=v);
                    Choice("Clothing",ChefWardrobe.Clothes,()=>a.clothing,v=>a.clothing=v);
                    Choice("Clothing color",ChefWardrobe.ClothColors,()=>a.clothingColor,v=>a.clothingColor=v);
                    Choice("Body color",ChefWardrobe.Colors,()=>a.bodyColor,v=>a.bodyColor=v);
                    Add("Face",()=>SetPage("Face")); Add("Hair and accessories",()=>SetPage("Accessories"));
                    Add(wardrobeBeforeLaunch ? "Ready — start cooking":"Done",()=>{if(wardrobeBeforeLaunch) FinishLaunch();else SetPage("Main");});
                    Add("Back",NavigateBack);
                }
                else if(Page=="Face")
                {
                    Choice("Eyes",ChefWardrobe.Eyes,()=>a.eyes,v=>a.eyes=v);
                    Choice("Mouth",ChefWardrobe.Mouths,()=>a.mouth,v=>a.mouth=v);
                    Add("Back",NavigateBack);
                }
                else
                {
                    Choice("Hair",ChefWardrobe.Hair,()=>a.hair,v=>a.hair=v);
                    Choice("Hair color",ChefWardrobe.HairColors,()=>a.hairColor,v=>a.hairColor=v);
                    Choice("Headwear",ChefWardrobe.Hats,()=>a.headwear,v=>a.headwear=v);
                    Choice("Glasses",new[]{"None","Round glasses"},()=>a.glasses,v=>a.glasses=v);
                    Add("Back",NavigateBack);
                }
                return;
            }
            if(Page=="Levels")
            {
                var layouts=hud.GetComponent<KitchenLayout>().choices;
                for(int i=0;i<layouts.Length;i++) {int index=i;Add(layouts[i].displayName,()=>LaunchLevel(index));}
                if(!practiceLevel) Add("Shift length: "+SessionOptions.ShiftLabel,()=>CycleLength(1),CycleLength);
                if(!practiceLevel && SessionOptions.ShiftOrders==0)Add("Reset career",RequestCareerReset);
                Add("Back",()=>SetPage("Title"));return;
            }
            if(Page=="Settings")
            {Add("Audio",()=>SetPage("Audio"));Add("Text and accessibility",()=>SetPage("Display"));Add("Back",NavigateBack);return;}
            if(Page=="Today's Menu")
            {
                var account=RestaurantAccounts.Current;
                Add(DailyMenu.CanStart(account)?"Start day — "+DailyMenu.Resolve(account).Length+" dishes":DailyMenu.StartProblem(account),()=>StartSelectedMenu());
                rows[0].enabled=DailyMenu.CanStart(account);
                foreach(var recipe in DailyMenu.Catalog)
                {
                    var choice=recipe;bool unlocked=choice.Unlocked(account);
                    Add((System.Array.IndexOf(account.Data.selectedMenu,choice.id)>=0?"[x] ":"[ ] ")+choice.displayName+" — $"+choice.salePrice+(unlocked?"":" — "+choice.LockReason),()=>{if(!DailyMenu.Toggle(account,choice))message=account.Problem;});
                    rows[rows.Count-1].enabled=unlocked;
                }
                Add(IsFrontEnd?"Back":"Back to Day Complete",()=>SetPage(IsFrontEnd?"Title":"Restaurant"));return;
            }
            if(Page=="Employees")
            {
                var config=Resources.Load<DayServiceDefinition>("ServiceDay");var account=RestaurantAccounts.Current;
                foreach(var role in new[]{config.serverRole,config.dishwasherRole,config.busserRole})
                {
                    var employee=role;if(employee==null)continue;
                    bool hired=account.Owns(employee.id);int level=account.TrainingLevel(employee.id);
                    Add(employee.displayName+(hired?" — Hired • Training "+level+"/3":" — Hire $"+employee.hireCost),()=>Buy(employee.id,employee.hireCost));rows[rows.Count-1].enabled=!hired;
                    if(hired){Add(level<3?"Train "+employee.displayName+" — $"+account.TrainingPrice(employee.id)+" • speed +"+((level+1)*10)+"%":"Maximum training — speed +30%",()=>{message=account.Train(employee.id)?"Training purchased. Applies next service.":string.IsNullOrEmpty(account.Problem)?"Not enough money.":account.Problem;});rows[rows.Count-1].enabled=level<3;}
                }
                Add("Back to Day Complete",NavigateBack);return;
            }
            if(Page=="Shop") { BuildShopRows();return; }
            if(Page=="Restaurant")
            {
                var day=hud.shift?.Day;var account=RestaurantAccounts.Current;
                Add("Employee Management",()=>SetPage("Employees"));
                Add("Arrange Kitchen — "+(account.ArrangementFee==0?"Free this break":"$100 this break"),()=>OpenKitchenLayout());
                Add("Appliances, Counters & Dishes",()=>SetPage("Shop"));
                Add("Next Day → Choose Menu",()=>{if(day!=null && day.AwaitingMenu)ChooseDailyMenu(()=>{if(day.StartService())Close();});else if(day!=null && !day.Closed)message="Finish this day before starting the next.";else if(day!=null && !day.Paid)message="Save today's earnings before continuing. Use Retry below.";else {var presentation=GetComponent<DayPresentation>(); Action start=()=>{SessionOptions.ShiftOrders=0;Close();Load("RestaurantShift");}; ChooseDailyMenu(()=>{if(presentation==null || day==null)start();else presentation.BeginNextDay(start);});}});
                if(day!=null && day.Closed && !day.Paid)Add("Retry saving today's earnings",()=>message=day.RetryPayment()?"Earnings saved.":account.Problem);
                Add("Settings",()=>SetPage("Settings"));
                Add("Quit to main menu",()=>Confirm("Return to main menu? Saved earnings and purchases are kept.",OpenFrontEnd));
                Add("Reset career",RequestCareerReset);return;
            }
            if(Page=="Recipes")
            {
                if(recipeBook!=null && recipeBook.recipes!=null) for(int i=0;i<recipeBook.recipes.Length;i++)
                {int index=i;Add(recipeBook.recipes[i].displayName,()=>recipeIndex=index);}
                Add("Back",()=>SetPage("Main"));return;
            }
            if(Page=="Session")
            {
                Add("Kitchen: "+hud.GetComponent<KitchenLayout>().choices[SessionOptions.Kitchen].displayName,()=>SetPage("Kitchen"));
                Add("Shift length: "+SessionOptions.ShiftLabel,()=>CycleLength(1),CycleLength);
                Add("Start selected shift",()=>Confirm("Start "+SessionOptions.ShiftLabel+"? Current progress will reset.",()=>Load("RestaurantShift")));
                Add("Guided full cooking loop",()=>StartTraining("Guided full loop"));
                Add("Practice one step",()=>SetPage("Practice"));
                Add("Free practice",()=>StartTraining("Free practice"));
                Add("Back",()=>SetPage("Main")); return;
            }
            if(Page=="Kitchen")
            {
                var layouts=hud.GetComponent<KitchenLayout>().choices;
                for(int i=0;i<layouts.Length;i++)
                {
                    int index=i;
                    Add(layouts[i].displayName,()=>Confirm("Switch to "+layouts[index].displayName+"? Current progress resets.",()=>{SessionOptions.Kitchen=index; Load(SceneManager.GetActiveScene().name);}));
                }
                Add("Back",()=>SetPage("Session")); return;
            }
            if(Page=="Co-op")
            {
                Add(IsFrontEnd ? "Back to main menu":"Resume / ready to join",()=>{if(IsFrontEnd) SetPage("Title");else Close();});
                Add("Keyboard P1 + controller P2",()=>{if(hud.coop.PlayerTwo!=null) message="Put down P2's item and leave before changing setup."; else {hud.coop.UseKeyboardPlayerOne();message="Keyboard stays P1. Resume; press A on a pad for P2.";}});
                Add("Controller P1 (keyboard also available)",()=>{hud.coop.UseControllerPlayerOne();message="Resume and press A on the first controller.";});
                Add("Player 2 leave",()=>{if(hud.coop.PlayerTwo==null) message="Player 2 has not joined."; else if(hud.coop.PlayerTwo.Hands.Item!=null) message="Place P2's item on a counter before leaving."; else Confirm("Remove Player 2? The shift continues.",()=>hud.coop.LeavePlayerTwo());});
                Add("Back",()=>SetPage("Main")); return;
            }
            if(Page=="Practice")
            {
                foreach(var step in new[]{"Prep","Frying","Plating","Serving"})
                { var selected=step; Add(selected+" — start with the needed item",()=>StartTraining(selected)); }
                Add("Garden salad — guided cold prep",()=>StartTraining("Garden salad"));
                Add("Back",()=>SetPage("Session")); return;
            }
            if(Page=="Confirm") { Add("Cancel",()=>SetPage(confirmReturnPage)); Add("Confirm",()=>{var action=pending;SetPage(confirmReturnPage);action?.Invoke();}); return; }
            if(Page=="Display")
            {
                var d=Display;
                Add("Text size: "+new[]{"Standard","Large","Extra large"}[d.textSize],()=>d.textSize=(d.textSize+1)%3,dir=>d.textSize=Mathf.Clamp(d.textSize+dir,0,2));
                Add("High contrast: "+(d.highContrast ? "ON":"OFF"),()=>d.highContrast=!d.highContrast);
                Add("Reduced effects: "+(d.reducedEffects ? "ON":"OFF"),()=>d.reducedEffects=!d.reducedEffects);
                Add("Save settings",Save); Add("Back",NavigateBack); return;
            }
            if(Page=="Audio")
            {
                var a=hud.settings.Repository.Audio;
                Volume("Master",()=>a.master,v=>a.master=v); Volume("Music",()=>a.music,v=>a.music=v);
                Volume("SFX",()=>a.sfx,v=>a.sfx=v); Volume("UI",()=>a.ui,v=>a.ui=v); Volume("Ambience",()=>a.ambience,v=>a.ambience=v);
                Add("Save settings",Save); Add("Back",NavigateBack); return;
            }
            Add("Play / Resume",Close);
            Add("Recipe book",OpenRecipeBook);
            Add("Choose your chef",()=>{wardrobeBeforeLaunch=false;ShowWardrobe();});
            Add("Restart this mode",RequestRestart);
            Add("Kitchen, shift length and practice",()=>SetPage("Session"));
            if(hud.coop!=null) Add("Co-op setup and controller help",()=>SetPage("Co-op"));
            Add("Settings",()=>SetPage("Settings"));
            Add(hud.shift?.Day!=null ? "Earnings and restaurant improvements":"Session results",()=>SetPage(hud.shift?.Day!=null?"Restaurant":"Results"));
            Add("Return to main menu",()=>Confirm("Leave this level? Starting another level resets progress.",OpenFrontEnd));
            if(Page=="Results") { rows.Clear(); Add("Back",()=>SetPage("Main")); }
        }
        private void BuildShopRows()
        {
            var config=Resources.Load<DayServiceDefinition>("ServiceDay");var account=RestaurantAccounts.Current;
            Add("Category: "+shopTabs[shopTab]+"   ← / →",()=>{shopTab=(shopTab+1)%shopTabs.Length;},dir=>shopTab=(shopTab+dir+shopTabs.Length)%shopTabs.Length);
            if(shopTab==2)Add("Buy a plate — $20 • Total "+(5+account.Quantity("extra-plate")),()=>message=account.BuyEquipment("extra-plate",20)?"Plate purchased — available next day.":"Not enough money or service still active.");
            foreach(var offer in config.purchases)
            {
                if(offer==null)continue;var item=offer;
                bool station=item.stationPrefab!=null;
                if(shopTab==0 && station && item.kind!=RestaurantPurchaseKind.CounterBay || shopTab==1 && item.kind==RestaurantPurchaseKind.CounterBay || shopTab==3 && !station)
                {Add(item.displayName+" — $"+item.cost+" • Owned "+account.Quantity(item.id),()=>{if(station){var f=GetComponent<KitchenFurniture>();message=f.TryPurchase(item)?f.Message:f.Message;}else Buy(item.id,item.cost);});if(!station)rows[rows.Count-1].enabled=!account.Owns(item.id);}
                if(shopTab==4 && station)foreach(var owned in account.Equipment(item.id,item.cost)){var copy=owned;Add("Sell "+item.displayName+BayLabel(copy.instanceId)+" — $"+RestaurantAccount.Resale(copy.paid),()=>Confirm("Sell "+item.displayName+BayLabel(copy.instanceId)+" for $"+RestaurantAccount.Resale(copy.paid)+"? Recipes may lock if this is your last one.",()=>Sell(copy,item.cost)));}
            }
            if(shopTab==4)foreach(var owned in account.Equipment("extra-plate",20)){var copy=owned;Add("Sell extra plate — $"+RestaurantAccount.Resale(copy.paid),()=>Confirm("Sell one extra plate for $"+RestaurantAccount.Resale(copy.paid)+"? Applies next day.",()=>Sell(copy,20)));}
            Add("Back to Day Complete",NavigateBack);
        }
        private string BayLabel(string id)
        {var f=GetComponent<KitchenFurniture>();return f!=null && f.Find("purchase:"+id)!=null?" • Bay "+(f.AssignedSlot("purchase:"+id)+1):"";}
        private void Sell(OwnedEquipment item,int legacyPrice)
        {
            if(!RestaurantAccounts.Current.SellEquipment(item.instanceId,item.offerId,legacyPrice)){message=RestaurantAccounts.Current.Problem;return;}
            GetComponent<KitchenFurniture>()?.RemovePurchased(item.instanceId);message="Sold for $"+RestaurantAccount.Resale(item.paid)+".";BuildRows();
        }
        private static void CycleLength(int direction)
        {
            int[] lengths={0,3,6,12}; int index=Array.IndexOf(lengths,SessionOptions.ShiftOrders);
            SessionOptions.ShiftOrders=lengths[(index+direction+lengths.Length)%lengths.Length];
        }
        private void StartTraining(string training) => Confirm("Start "+training+"? Current progress will reset.",()=>{SessionOptions.Training=training; Load("RestaurantDevelopment");});
        private void Volume(string name,Func<float> read,Action<float> write)
        {
            Action<int> change=dir=>{write(Mathf.Round(Mathf.Clamp01(read()+dir*.1f)*10)/10); hud.settings.Apply();};
            Add(name+": "+Mathf.RoundToInt(read()*100)+"%   ← / →",()=>change(1),change);
        }
        private void Save() { message=hud.settings.Save() ? "Settings saved." : "Settings could not be saved; existing saved data was preserved."; }
        public void RequestCareerReset(){resetReturnPage=Page;SetPage("Reset career");}
        public void RequestRestart() => Confirm("Restart this mode? Unpaid earnings and current food/orders will be lost. Saved money and purchases stay.",()=>hud.chef.GetComponent<ChefInput>().RestartSlice());
        public bool OpenKitchenLayout()
        {
            var furniture=GetComponent<KitchenFurniture>();
            if(furniture==null || !furniture.Begin()){message="Arrange equipment between paid service days.";return false;}
            if(!IsOpen)Open();SetPage("Kitchen layout");return true;
        }
        public void OpenRestaurant(){if(!IsOpen)Open();SetPage("Restaurant");}
        private void Buy(string id,int cost)
        {
            var account=RestaurantAccounts.Current;
            if(hud.shift?.Day!=null && !hud.shift.Day.Closed && !hud.shift.Day.AwaitingMenu){message="Purchases open after the 10 PM close.";return;}
            message=account.Buy(id,cost)?"Purchased — available next day. "+System.Array.FindAll(DailyMenu.Catalog,r=>r.Unlocked(account)).Length+" recipes now unlocked.":!string.IsNullOrEmpty(account.Problem)?account.Problem:"Already owned, insufficient cash, or today's earnings are not yet saved.";
        }
        private void Confirm(string text,Action action) { if(!IsOpen) Open(); pending=action; confirmation=text;confirmReturnPage=Page; SetPage("Confirm"); }
        public void ActivateSelection() { if(StartupSequence.BlocksMenu)return; if(GetComponent<DayPresentation>()?.Transitioning==true)return; if(!rows[Mathf.Clamp(Selection,0,rows.Count-1)].enabled) return; rows[Mathf.Clamp(Selection,0,rows.Count-1)].select(); hud.audioFeedback?.Click(); if(IsOpen) BuildRows(); }
        private void Update()
        {
            foreach(var pad in Gamepad.all)if(pad.buttonSouth.wasPressedThisFrame)WebInputFocus.FocusFromGamepad();
            Tick(WebInputFocus.HasFocus);
        }
        public void Tick(bool focused)
        {
            if(StartupSequence.BlocksMenu || !focused || GetComponent<DayPresentation>()?.Transitioning==true) return;
            if(IsOpen && Page=="Kitchen layout")
            {
                var furniture=GetComponent<KitchenFurniture>();
                if(back.WasPressedThisFrame()){if(furniture.Back())SetPage("Restaurant");return;}
                if(toggle.WasPressedThisFrame()){if(furniture.Save())SetPage("Restaurant");return;}
                if(rotateLayout.WasPressedThisFrame())furniture.Rotate();
                if(cycleFurniture.WasPressedThisFrame())furniture.CyclePiece();
                var movement=navigate.ReadValue<Vector2>();
                if(movement.sqrMagnitude<.25f)navigationHeld=false;
                else if(!navigationHeld || Time.unscaledTime>=nextNavigation){furniture.Navigate(Mathf.Abs(movement.x)>Mathf.Abs(movement.y)?new Vector2(Mathf.Sign(movement.x),0):new Vector2(0,Mathf.Sign(movement.y)));nextNavigation=Time.unscaledTime+(navigationHeld ? .16f:.35f);navigationHeld=true;}
                if(accept.WasPressedThisFrame()){furniture.Confirm();if(!furniture.Editing)SetPage("Restaurant");}return;
            }
            if(toggle.WasPressedThisFrame()) { if(IsOpen) {if(IsFrontEnd || BetweenDays) NavigateBack(); else Close();} else Open(); return; }
            if(!IsOpen) return;
            if(back.WasPressedThisFrame()) { NavigateBack(); return; }
            var axis=navigate.ReadValue<Vector2>();
            if(axis.sqrMagnitude<.25f) navigationHeld=false;
            else if(!navigationHeld || Time.unscaledTime>=nextNavigation)
            {
                if(Mathf.Abs(axis.y)>=Mathf.Abs(axis.x)) Selection=(Selection+(axis.y>0 ? -1:1)+rows.Count)%rows.Count;
                else { rows[Selection].adjust?.Invoke(axis.x>0 ? 1:-1); BuildRows(); }
                nextNavigation=Time.unscaledTime+(navigationHeld ? .16f:.35f); navigationHeld=true;
            }
            if(accept.WasPressedThisFrame()) ActivateSelection();
        }
        private static void Load(string name)
        {
            gateAfterLoad=true;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(name);
#endif
        }
        private void OnGUI()
        {
            if(!IsOpen) return;
            if(Page=="Kitchen layout"){GetComponent<KitchenFurniture>().Draw(()=>SetPage("Restaurant"),()=>SetPage("Restaurant"));return;}
            var presentation=GetComponent<DayPresentation>();
            var opacity=presentation!=null ? presentation.MenuOpacity:1;
            var previousColor=GUI.color; var previousEnabled=GUI.enabled;
            GUI.depth=-100;
            var matrix=GUI.matrix; GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.color=new Color(0,0,0,(presentation!=null && presentation.NightMenu ? .22f:1)*opacity); GUI.DrawTexture(new Rect(0,0,1280,720),Texture2D.whiteTexture); GUI.color=new Color(1,1,1,opacity);
            if(IsFrontEnd && mainMenuBackground!=null)
            {
                GUI.DrawTexture(new Rect(0,0,1280,720),mainMenuBackground,ScaleMode.ScaleAndCrop);
                GUI.color=new Color(0,0,0,.57f*opacity);GUI.DrawTexture(new Rect(0,0,1280,720),Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);
            }
            if(buttonStyle==null) { buttonStyle=new GUIStyle(GUI.skin.button) {alignment=TextAnchor.MiddleLeft}; textStyle=new GUIStyle(GUI.skin.label) {fontSize=22,wordWrap=true,alignment=TextAnchor.MiddleCenter}; }
            var style=buttonStyle; style.fontSize=Mathf.RoundToInt(21*Display.TextScale); var text=textStyle;
            text.normal.textColor=Color.white; style.normal.textColor=Color.white; style.hover.textColor=Color.white; style.active.textColor=Color.white;
            GUI.Label(new Rect(260,25,760,48),Page=="Title" ? "THROWN TOGETHER" : Page=="Main" ? "PAUSED" : Page=="Levels" ? (practiceLevel ? "CHOOSE A PRACTICE KITCHEN":"CHOOSE YOUR LEVEL") : Page=="Recipes" ? "RECIPE BOOK" : Page=="Restaurant" ? (hud.shift?.Day?.Closed==true?"DAY "+hud.shift.Day.DayNumber+" COMPLETE":"RESTAURANT SETUP") : Page=="Shop" ? "APPLIANCES, COUNTERS & DISHES" : Page=="Today's Menu" ? "DAY "+RestaurantAccounts.Current.Data.nextDay+" — CHOOSE MENU" : Page.ToUpperInvariant(),text);
            GUI.Label(Page=="Restaurant"?new Rect(180,73,920,60):new Rect(260,73,760,60),Page=="Reset career" ? "Erase all career days, money, purchases, hires, menu choices and saved kitchen layouts? Settings and chef appearance stay." : Page=="Confirm" ? confirmation : Page=="Today's Menu" ? DailyMenu.Resolve(RestaurantAccounts.Current).Length+" selected • Minimum 3 • A: toggle dish\nServe 4 different menu dishes: +5% meal revenue (max $15)" : Page=="Restaurant" && hud.shift?.Day?.Closed==true ? hud.shift.Day.Clock+" • "+hud.shift.Day.Served+" served / "+hud.shift.Day.LostCustomers+" lost • Earned $"+hud.shift.Day.NetIncome+" (speed $"+hud.shift.Day.Bonuses+", variety $"+hud.shift.Day.VarietyBonus+", waste $"+hud.shift.Day.WasteFees+")\nAvailable $"+RestaurantAccounts.Current.Data.cash+" • B returns here" : IsFrontEnd ? "D-pad / stick: navigate • A: select • B: back\nChoose a kitchen and start cooking." : BetweenDays?"Available $"+RestaurantAccounts.Current.Data.cash+" • D-pad: navigate • A: select • B: Day Complete":"Paused • D-pad / stick: navigate • A: select • B: back\nY / Escape: close • Xbox Menu belongs to Edge",text);
            int visible=Page=="Recipes"?6:Page=="Shop"?7:(Page=="Restaurant" || Page=="Today's Menu")?8:9;
            bool menu=Page=="Today's Menu";
            int begin=menu?1:0, end=menu?rows.Count-1:rows.Count;
            int first=Mathf.Clamp(Selection-visible+1,begin,Mathf.Max(begin,end-visible));
            for(int n=0;n<(menu?Mathf.Min(visible,end-first)+2:Mathf.Min(visible,end-first));n++)
            {
                int i=menu && n>=Mathf.Min(visible,end-first)?(n==Mathf.Min(visible,end-first)?0:rows.Count-1):first+n;
                GUI.enabled=rows[i].enabled && !StartupSequence.BlocksMenu && (presentation==null || !presentation.Transitioning);
                GUI.backgroundColor=i==Selection ? new Color(.2f,.8f,.6f):Color.gray;
                var rowRect=Page=="Recipes" ? new Rect(35,160+(i-first)*55,345,48):WardrobePage ? new Rect(65,145+(i-first)*47,670,42):(Page=="Restaurant" || Page=="Today's Menu") ? new Rect(260,140+(i-first)*42,760,37):new Rect(260,145+(i-first)*47,760,42);
                if(menu && (i==0 || i==rows.Count-1))rowRect=new Rect(i==0?260:650,500,370,40);
                if(Page=="Shop")rowRect.y+=40;
                GUI.color=new Color(.025f,.035f,.05f,.94f*opacity);GUI.DrawTexture(rowRect,Texture2D.whiteTexture);GUI.color=new Color(1,1,1,opacity);
                if(GUI.Button(rowRect,(i==Selection ? ">  ":"    ")+rows[i].label,style)) { Selection=i; ActivateSelection(); break; }
            }
            GUI.enabled=true; GUI.backgroundColor=Color.white;
            if(Page=="Shop")for(int tab=0;tab<shopTabs.Length;tab++){GUI.backgroundColor=tab==shopTab?new Color(.2f,.8f,.6f):Color.gray;if(GUI.Button(new Rect(260+tab*152,140,148,34),shopTabs[tab])){shopTab=tab;Selection=0;BuildRows();}}GUI.backgroundColor=Color.white;
            if(end-begin>visible)
            {
                bool recipes=Page=="Recipes";
                if(!recipes){float top=Page=="Shop"?185:140, height=Page=="Shop"?324:331;GUI.color=new Color(.2f,.25f,.28f);GUI.DrawTexture(new Rect(1030,top,8,height),Texture2D.whiteTexture);GUI.color=new Color(.3f,.85f,.65f);float thumb=height*visible/(end-begin);GUI.DrawTexture(new Rect(1030,top+(height-thumb)*(first-begin)/(end-begin-visible),8,thumb),Texture2D.whiteTexture);GUI.color=Color.white;}
                if(GUI.Button(recipes?new Rect(35,520,165,32):new Rect(260,575,170,30),"↑ Scroll up"))Selection=Mathf.Max(0,Selection-visible);
                GUI.Label(recipes?new Rect(35,558,345,32):new Rect(435,575,400,30),(first>begin?"↑ More above   ":"")+(first-begin+1)+"–"+Mathf.Min(end-begin,first-begin+visible)+" / "+(end-begin)+(first+visible<end?"   More below ↓":""),text);
                if(GUI.Button(recipes?new Rect(210,520,170,32):new Rect(850,575,170,30),"↓ Scroll down"))Selection=Mathf.Min(rows.Count-1,Selection+visible);
            }
            if(WardrobePage && wardrobePreview!=null && wardrobePreview.Image!=null)
            {
                GUI.DrawTexture(new Rect(780,145,360,450),wardrobePreview.Image,ScaleMode.ScaleToFit);
                GUI.Label(new Rect(65,572,670,42),Page=="Accessories" ? "Hair is tucked away under headwear." : "Three builds. Same movement and reach.",text);
            }
            if(Page=="Levels") GUI.Label(new Rect(260,435,760,115),hud.GetComponent<KitchenLayout>().choices[Mathf.Min(Selection,2)].description,text);
            if(Page=="Recipes" && recipeBook!=null && recipeBook.recipes.Length>0)
            {
                if(Selection<recipeBook.recipes.Length) recipeIndex=Selection;
                var recipe=recipeBook.recipes[recipeIndex];
                GUI.color=new Color(.12f,.17f,.18f);GUI.DrawTexture(new Rect(410,145,830,445),Texture2D.whiteTexture);GUI.color=Color.white;
                FoodIcon.Draw(new Rect(438,158,70,60),recipe);
                GUI.Label(new Rect(520,154,690,60),recipe.displayName+(recipe.requiredState==FoodState.Cut ? " — no frying":""),text);
                var instructions=new GUIStyle(text) {alignment=TextAnchor.UpperLeft,fontSize=Mathf.RoundToInt(18*Display.TextScale)};
                GUI.Label(new Rect(435,230,780,350),RecipeBook.Instructions(recipe),instructions);
            }
            if(Page=="Kitchen") GUI.Label(new Rect(260,370,760,135),hud.GetComponent<KitchenLayout>().choices[Mathf.Min(Selection,2)].description,text);
            if(Page=="Co-op") GUI.Label(new Rect(200,390,880,145),"1. Xbox Edge: hold Menu, then Use game controls.\n2. Resume. First pad controls P1; A on another joins P2.\nChoose both looks in Choose your chef. A: use. Y: menu.\nDisconnected? Food stays safe. Reconnect that pad or press A on an unused one.",text);
            GUI.Label(new Rect(240,610,800,65),message,text);
            if(Page=="Results")
            {
                var summary=hud.GetComponent<SessionSummary>();
                int completed=hud.shift!=null ? hud.shift.CompletedCount : hud.order.Phase==OrderPhase.Complete ? 1:0;
                GUI.Label(new Rect(200,215,880,240),"Dishes completed: "+completed+"\nTime: "+TimeSpan.FromSeconds(summary.ElapsedSeconds).ToString(@"mm\:ss")+
                    "\n\nP1  "+summary.PlayerOne.Description+"\nP2  "+summary.PlayerTwo.Description+"\n\nTeam contributions count successful actions, not points.",text);
            }
            if(hud.coop!=null && !BetweenDays && Page!="Shop" && Page!="Today's Menu") GUI.Label(new Rect(180,620,920,75),hud.coop.DeviceSummary,text);
            GUI.matrix=matrix; GUI.depth=0; GUI.color=previousColor; GUI.enabled=previousEnabled;
        }
    }
}
