using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class RestaurantDay : MonoBehaviour
    {
        public DayServiceDefinition Settings {get;private set;}
        public DiningTable[] Tables {get;private set;}
        public float Elapsed {get;private set;}
        public bool Closed {get;private set;}
        public bool Paid {get;private set;}
        public int Served {get;private set;}
        public int BaseIncome {get;private set;}
        public int Bonuses {get;private set;}
        public int WasteFees {get;private set;}
        public RecipeDefinition[] Menu {get;private set;}=new RecipeDefinition[0];
        public bool AwaitingMenu {get;private set;}=true;
        readonly HashSet<string> servedRecipes=new HashSet<string>();
        public int VarietyBonus=>Closed?DailyMenu.VarietyRevenue(Menu.Length,servedRecipes.Count,BaseIncome):0;
        public int NetIncome=>Mathf.Max(0,BaseIncome+Bonuses+VarietyBonus-WasteFees);
        public bool StartService()
        {
            if(!AwaitingMenu || !DailyMenu.CanStart(RestaurantAccounts.Current))return false;
            int number=RestaurantAccounts.Current.StartDay();if(number==0)return false;
            Menu=DailyMenu.Resolve(RestaurantAccounts.Current);DayNumber=number;AwaitingMenu=false;return true;
        }
        private void Start(){if(AwaitingMenu)GetComponent<RestaurantMenu>().ChooseDailyMenu(()=>{if(StartService())GetComponent<RestaurantMenu>().Close();});}
        public float LastWasteAt {get;private set;}=-100;
        public bool RecordWaste(){if(Closed)return false;WasteFees+=Mathf.Max(0,Settings.wasteCost);LastWasteAt=Elapsed;return true;}
        public int LostCustomers {get;private set;}
        public float LastLostAt {get;private set;}=-100;
        public int DayNumber {get;private set;}
        public int ServiceDayNumber {get;private set;}
        public int TargetCustomers=>Settings.CustomersForDay(ServiceDayNumber);
        public int WaitingOutside=>guests.Count(g=>g.phase==0);
        public float OldestWait=>guests.Where(g=>g.phase==0).Select(g=>Elapsed-g.arrived).DefaultIfEmpty(0).Max();
        public string Clock {get {int m=660+Mathf.FloorToInt(Mathf.Clamp01(Elapsed/Settings.durationSeconds)*660);return ((m/60+11)%12+1)+":"+(m%60).ToString("00")+(m<720?" AM":" PM");}}
        readonly List<Guest> guests=new List<Guest>();
        sealed class Guest {public DiningWalker walker;public DiningTable table;public RecipeDefinition recipe;public float arrived;public int phase,look;}
        RestaurantShift shift;int spawned;float nextArrival;DiningServer server;KitchenDishwasher dishwasher;DiningBusser busser;
        readonly System.Random customerRandom=new System.Random();
        public void Begin(RestaurantShift owner,DayServiceDefinition settings)
        {
            shift=owner;Settings=settings;ServiceDayNumber=RestaurantAccounts.Current.Data.completedDays+1;nextArrival=settings.firstArrival;
            if(owner.diningExpansion!=null && Settings.purchases.Any(p=>p!=null && p.kind==RestaurantPurchaseKind.DiningTables && RestaurantAccounts.Current.Owns(p.id)))
            {owner.diningExpansion.SetActive(true);owner.seats=owner.seats.Concat(owner.expansionSeats).Distinct().ToArray();}
            Tables=owner.seats.Select(o=>
            {
                o.manualService=true;o.mealSeconds=settings.eatingSeconds;o.ResetOrder(null);
                // CustomerOrder lives on a logic-only root at the origin. Target the authored table,
                // which owns the plate slot, so player focus, highlights and server routes agree.
                var table=o.tableSlot.transform.parent.gameObject.AddComponent<DiningTable>();table.day=this;table.order=o;table.stationName="Dining table";table.SetGuestVisible(false);return table;
            }).ToArray();
            gameObject.AddComponent<KitchenFurniture>().Initialize(GetComponent<KitchenLayout>());
            ApplyPurchases();StartService();
        }
        void ApplyPurchases()
        {
            var account=RestaurantAccounts.Current;
            GetComponent<KitchenFurniture>().IncludeNewPurchases();
            foreach(var purchase in Settings.purchases)
                if(purchase!=null && account.Owns(purchase.id) && purchase.kind==RestaurantPurchaseKind.FasterFryers)
                    foreach(var fryer in FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None))
                        if(fryer.gameObject.scene==gameObject.scene && !fryer.requiresAttendance && fryer.appliance!=null && fryer.appliance.id.Contains("fryer"))fryer.processingSpeed=purchase.processingSpeed;
            GetComponent<KitchenFurniture>().ApplySaved();
            if(Settings.serverRole!=null && account.Owns(Settings.serverRole.id))
            {
                var pass=FindObjectsByType<ServiceStation>(FindObjectsSortMode.None).First(s=>s.gameObject.scene==gameObject.scene);
                server=gameObject.AddComponent<DiningServer>();server.Initialize(this,pass);
            }
            if(Settings.dishwasherRole!=null && account.Owns(Settings.dishwasherRole.id))
            {dishwasher=gameObject.AddComponent<KitchenDishwasher>();dishwasher.Initialize(this);}
            if(Settings.busserRole!=null && account.Owns(Settings.busserRole.id))
            {busser=gameObject.AddComponent<DiningBusser>();busser.Initialize(this);}
        }
        DiningWalker Walker(string label,int look,Vector3 position)
        {
            var go=new GameObject(label);go.transform.SetParent(transform);go.transform.position=position;
            var walker=go.AddComponent<DiningWalker>();walker.Initialize(Settings.walkingVisual,look,true);return walker;
        }
        public Vector3 TableApproach(DiningTable table)=>new Vector3(table.transform.position.x-1.35f,0,table.transform.position.z);
        public Vector3[] DiningRoute(Vector3 from,Vector3 to)=>Mathf.Abs(from.x-to.x)<.2f?new[]{to}:new[]{new Vector3(from.x,0,-5),new Vector3(to.x,0,-5),to};
        Vector3 Chair(DiningTable table)
        {
            var visual=table.order.GetComponent<CustomerPresentation>()?.seatedVisual;
            var p=visual!=null?visual.transform.position:table.order.customerVisual.position;p.y=0;return p;
        }
        void Spawn()
        {
            var p=Settings.sidewalkStart;
            int look=customerRandom.Next();
            var walker=Walker("Arriving customer",look,p);walker.Go(new Vector3(Settings.entrance.x,0,p.z),Settings.entrance);
            guests.Add(new Guest {walker=walker,phase=4,look=look,arrived=Elapsed,recipe=Menu[spawned%Menu.Length]});spawned++;
        }
        public void Advance(float seconds)
        {
            if(Closed || AwaitingMenu)return;
            // Bounded substeps keep queue patience and arrival order deterministic in tests and slow frames.
            float remaining=Mathf.Max(0,seconds);
            while(remaining>0 && !Closed){float dt=Mathf.Min(.1f,remaining);remaining-=dt;Tick(dt);}
        }
        void Tick(float dt)
        {
            Elapsed=Mathf.Min(Settings.durationSeconds,Elapsed+dt);
            if(Elapsed>=Settings.durationSeconds){Closed=true;RetryPayment();return;}
            while(spawned<TargetCustomers && Elapsed>=nextArrival){Spawn();nextArrival+=Settings.IntervalForDay(ServiceDayNumber);}
            foreach(var guest in guests.ToArray())
            {
                if(guest.phase==0)
                {
                    var table=Tables.FirstOrDefault(t=>t.Clean);
                    if(table!=null)
                    {
                        guest.table=table;table.ReserveSeat();guest.phase=1;
                        guest.walker.Go(new Vector3(Settings.entrance.x,0,-5),new Vector3(TableApproach(table).x,0,-5),new Vector3(TableApproach(table).x,0,Chair(table).z),Chair(table));
                    }
                    else if(Elapsed-guest.arrived>=Settings.outsidePatience)
                    {LostCustomers++;LastLostAt=Elapsed;guest.phase=5;guest.walker.name="Customer left â€” waited too long";guest.walker.Go(new Vector3(Settings.entrance.x,0,Settings.sidewalkExit.z),Settings.sidewalkExit);}
                }
                if(guest.phase==1 || guest.phase==3 || guest.phase==4 || guest.phase==5)
                {
                    guest.walker.Advance(dt,Settings.walkingSpeed);
                    if(guest.walker.Arrived)
                    {
                        if(guest.phase==1){guest.phase=2;guest.walker.gameObject.SetActive(false);guest.table.Seat(guest.recipe,guest.look);}
                        else if(guest.phase==4){guest.phase=0;guest.arrived=Elapsed;}
                        else if(guest.phase==3){guest.table.Depart();guest.phase=5;guest.walker.Go(new Vector3(Settings.entrance.x,0,Settings.sidewalkExit.z),Settings.sidewalkExit);}
                        else{guests.Remove(guest);Destroy(guest.walker.gameObject);}
                    }
                }
                else if(guest.phase==2 && (guest.table.order.Phase==OrderPhase.Dirty || guest.table.WaitingForMeal && guest.table.PatienceRemaining<=0))
                {
                    if(guest.table.WaitingForMeal){LostCustomers++;LastLostAt=Elapsed;guest.walker.name="Customer left â€” not served";}
                    guest.phase=3;guest.table.BeginDeparture();guest.walker.gameObject.SetActive(true);
                    guest.walker.Go(new Vector3(TableApproach(guest.table).x,0,guest.walker.transform.position.z),new Vector3(TableApproach(guest.table).x,0,-5),new Vector3(Settings.entrance.x,0,-5),Settings.entrance);
                }
            }
            server?.Advance(dt);dishwasher?.Advance(dt);busser?.Advance(dt);
        }
        public void RecordMeal(RecipeDefinition recipe,float waiting)
        {
            if(Closed || AwaitingMenu || !Menu.Contains(recipe))return;servedRecipes.Add(recipe.id);Served++;BaseIncome+=Mathf.Max(0,recipe.salePrice);
            Bonuses+=Mathf.RoundToInt(Settings.maximumBonus*Mathf.Clamp01(1-Mathf.Max(0,waiting)/Settings.bonusWindow));
        }
        public bool RetryPayment()
        {
            if(!Closed)return false;if(Paid)return true;
            Paid=RestaurantAccounts.Current.Settle(DayNumber,BaseIncome,Bonuses+VarietyBonus,WasteFees);return Paid;
        }
    }
}
