using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class RestaurantDay : MonoBehaviour
    {
        public DayServiceDefinition Settings {get;private set;}
        public RestaurantExpo Expo {get;private set;}
        public KitchenPrepCook PrepCook {get;private set;}
        public KitchenCook Cook {get;private set;}
        public bool HasInstalledExpo=>FindObjectsByType<ExpoStation>(FindObjectsSortMode.None).Any(s=>s.gameObject.scene==gameObject.scene && s.isActiveAndEnabled);
        public string ExpoProblem {get;private set;}="";
        ServiceStation expoPass;
        public bool EnsureExpo()
        {
            var stations=FindObjectsByType<ExpoStation>(FindObjectsSortMode.None).Where(s=>s.gameObject.scene==gameObject.scene && s.isActiveAndEnabled).ToArray();
            foreach(var station in stations)station.Initialize(this);
            if(stations.Length==0)Expo=null;
            ExpoProblem="";return true;
        }
        internal void StageExpoPass(){if(Expo!=null && expoPass?.pickupSlot.Item!=null && Expo.BoundTicket(expoPass.pickupSlot.Item)==null)Expo.TryStage(expoPass.pickupSlot.Item);}
        // Enabled by the physical Expo when installed; no invisible Fire gate in old scenes.
        public RestaurantExpo EnableExpo()
        {
            if(Expo!=null)return Expo;
            if(Settings==null || Tables==null || Closed)return null;
            Expo=new RestaurantExpo(this,Mathf.Max(1,Settings.activeQueueCapacity));
            foreach(var table in Tables)if(table.WaitingForMeal)Expo.Seat(table);
            return Expo;
        }
        public DiningTable[] Tables {get;private set;}
        public float Elapsed {get;private set;}
        public bool Closed {get;private set;}
        public bool AdmissionsClosed=>Elapsed>=Settings.durationSeconds;
        public int CustomersRemaining=>guests.Count;
        public int CustomersArrived=>spawned;
        readonly List<StaffMember> staff=new List<StaffMember>();
        public IReadOnlyList<StaffMember> Staff=>staff;
        public bool StaffEntering=>staff.Any(s=>s!=null && s.Arriving);
        public bool StaffLeaving {get;private set;}
        internal void RegisterStaff(StaffMember member)=>staff.Add(member);
        public string StaffTransition=>StaffEntering?"Staff arriving — opening shortly":StaffLeaving?string.Join(" · ",staff.Where(s=>s!=null && !s.Gone).Select(s=>s.Status).Distinct()):"";
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
            ApplyPurchases();EnsureExpo();
            if(!GetComponent<KitchenFurniture>().Validate(out var layoutProblem)){ExpoProblem=layoutProblem;return false;}
            var homes=GetComponent<KitchenFurniture>().Homes;
            if(!homes.Validate(out var homeProblem)){ExpoProblem=homeProblem;return false;}
            foreach(var role in StaffHomes.Roles.Where(r=>RestaurantAccounts.Current.Owns(StaffHomes.PurchaseId(r))))
                if(KitchenStaffRoute.ToPoint(StaffHomes.Entrance(this),homes.Home(role))==null){ExpoProblem="Move the "+role+" home to reachable floor in Arrange Kitchen.";return false;}
            int number=RestaurantAccounts.Current.StartDay();if(number==0)return false;
            CreateStaff();
            if(Settings.cookRole!=null && RestaurantAccounts.Current.Owns("cook") && HasInstalledExpo && Cook==null)
            {var worker=new GameObject("Hired cook");worker.transform.SetParent(transform);Cook=worker.AddComponent<KitchenCook>();Cook.Initialize(this);}
            Menu=DailyMenu.Resolve(RestaurantAccounts.Current);DayNumber=number;AwaitingMenu=false;return true;
        }
        private void Start(){if(AwaitingMenu)GetComponent<RestaurantMenu>().OpenRestaurant();}
        public float LastWasteAt {get;private set;}=-100;
        public bool RecordWaste(){if(Closed)return false;WasteFees+=Mathf.Max(0,Settings.wasteCost);LastWasteAt=Elapsed;return true;}
        public int LostCustomers {get;private set;}
        public float LastLostAt {get;private set;}=-100;
        public int DayNumber {get;private set;}
        public int ServiceDayNumber {get;private set;}
        public int TargetCustomers=>Settings.CustomersForDay(ServiceDayNumber);
        public Vector3[] OutsidePositions=>guests.Where(g=>g.phase==0).Select(g=>g.walker.transform.position).ToArray();
        public Vector3 QueuePosition(int index)=>new Vector3(Settings.entrance.x-1.2f-Mathf.Max(0,index)*.95f,0,Settings.sidewalkStart.z);
        public int WaitingOutside=>guests.Count(g=>g.phase==0);
        public float OldestWait=>guests.Where(g=>g.phase==0).Select(g=>g.waited).DefaultIfEmpty(0).Max();
        public float OutsidePatienceRate=>host==null?1:.75f;
        public float OutsidePatienceRemaining
        {
            get
            {
                if(WaitingOutside==0)return Settings.outsidePatience;
                return Mathf.Max(0,Settings.outsidePatience-OldestWait)/OutsidePatienceRate;
            }
        }
        public DiningHost Host=>host;
        public bool HostRouteBlocked {get;private set;}
        public string Clock {get {int m=660+Mathf.FloorToInt(Mathf.Max(0,Elapsed/Settings.durationSeconds)*660);return ((m/60+11)%12+1)+":"+(m%60).ToString("00")+(m%1440<720?" AM":" PM");}}
        readonly List<Guest> guests=new List<Guest>();
        sealed class Guest {public readonly System.Guid id=System.Guid.NewGuid();public DiningWalker walker;public DiningTable table;public RecipeDefinition recipe;public float arrived,waited;public System.Guid hostClaim;public int phase,look;}
        RestaurantShift shift;int spawned;float nextArrival;DiningServer server;KitchenDishwasher dishwasher;DiningBusser busser;DiningHost host;
        readonly System.Random customerRandom=new System.Random();
        public void Begin(RestaurantShift owner,DayServiceDefinition settings)
        {
            shift=owner;Settings=settings;ServiceDayNumber=RestaurantAccounts.Current.Data.completedDays+1;nextArrival=settings.firstArrival;
            var extension=GetComponent<RestaurantExpansion>();extension?.Apply();
            if(owner.diningExpansion!=null && Settings.purchases.Any(p=>p!=null && p.kind==RestaurantPurchaseKind.DiningTables && RestaurantAccounts.Current.Owns(p.id)))
            {owner.diningExpansion.SetActive(true);owner.seats=owner.seats.Concat(owner.expansionSeats).Distinct().ToArray();}
            if(extension!=null)owner.seats=owner.seats.Concat(extension.ActiveSeats).Distinct().ToArray();
            Tables=owner.seats.Select(o=>
            {
                o.manualService=true;o.mealSeconds=settings.eatingSeconds;o.ResetOrder(null);
                // CustomerOrder lives on a logic-only root at the origin. Target the authored table,
                // which owns the plate slot, so player focus, highlights and server routes agree.
                var table=o.tableSlot.transform.parent.gameObject.AddComponent<DiningTable>();table.day=this;table.order=o;table.stationName="Dining table";table.SetGuestVisible(false);return table;
            }).ToArray();
            gameObject.AddComponent<KitchenFurniture>().Initialize(GetComponent<KitchenLayout>());
            ApplyPurchases();
            expoPass=FindObjectsByType<ServiceStation>().FirstOrDefault(s=>s.gameObject.scene==gameObject.scene);
            EnsureExpo();bool manage=SessionOptions.ManageBeforeService;SessionOptions.ManageBeforeService=false;if(!manage)StartService();
        }
        void ApplyPurchases()
        {
            var account=RestaurantAccounts.Current;
            RefreshExpansion();
            if(shift.diningExpansion!=null && !shift.diningExpansion.activeSelf && Settings.purchases.Any(p=>p!=null && p.kind==RestaurantPurchaseKind.DiningTables && account.Owns(p.id)))
            {
                shift.diningExpansion.SetActive(true);shift.seats=shift.seats.Concat(shift.expansionSeats).Distinct().ToArray();
                var extra=shift.expansionSeats.Select(o=>{o.manualService=true;o.mealSeconds=Settings.eatingSeconds;o.ResetOrder(null);var t=o.tableSlot.transform.parent.gameObject.AddComponent<DiningTable>();t.day=this;t.order=o;t.stationName="Dining table";t.SetGuestVisible(false);return t;});Tables=Tables.Concat(extra).ToArray();
            }
            GetComponent<KitchenFurniture>().IncludeNewPurchases();
            foreach(var purchase in Settings.purchases)
                if(purchase!=null && account.Owns(purchase.id) && purchase.kind==RestaurantPurchaseKind.FasterFryers)
                    foreach(var fryer in FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None))
                        if(fryer.gameObject.scene==gameObject.scene && !fryer.requiresAttendance && fryer.appliance!=null && fryer.appliance.id.Contains("fryer"))fryer.processingSpeed=purchase.processingSpeed;
            GetComponent<KitchenFurniture>().ApplySaved();
        }
        void CreateStaff()
        {
            var account=RestaurantAccounts.Current;
            if(server==null && Settings.serverRole!=null && account.Owns(Settings.serverRole.id))
            {
                var pass=FindObjectsByType<ServiceStation>(FindObjectsSortMode.None).First(s=>s.gameObject.scene==gameObject.scene);
                server=gameObject.AddComponent<DiningServer>();server.Initialize(this,pass);
            }
            if(dishwasher==null && Settings.dishwasherRole!=null && account.Owns(Settings.dishwasherRole.id))
            {dishwasher=gameObject.AddComponent<KitchenDishwasher>();dishwasher.Initialize(this);}
            if(busser==null && Settings.busserRole!=null && account.Owns(Settings.busserRole.id))
            {busser=gameObject.AddComponent<DiningBusser>();busser.Initialize(this);}
            if(host==null && Settings.hostRole!=null && account.Owns(Settings.hostRole.id))
            {host=gameObject.AddComponent<DiningHost>();host.Initialize(this);}
            if(PrepCook==null && Settings.prepCookRole!=null && account.Owns(Settings.prepCookRole.id))
            {var worker=new GameObject("Hired prep cook");worker.transform.SetParent(transform);PrepCook=worker.AddComponent<KitchenPrepCook>();PrepCook.Initialize(this);}
        }
        public void RefreshExpansion()
        {
            var extension=GetComponent<RestaurantExpansion>();if(extension==null)return;
            extension.Apply();
            foreach(var order in extension.ActiveSeats.Where(o=>!Tables.Any(t=>t.order==o)))
            {
                order.manualService=true;order.mealSeconds=Settings.eatingSeconds;order.ResetOrder(null);
                var table=order.tableSlot.transform.parent.gameObject.AddComponent<DiningTable>();
                table.day=this;table.order=order;table.stationName="Dining table";table.SetGuestVisible(false);
                Tables=Tables.Concat(new[]{table}).ToArray();shift.seats=shift.seats.Concat(new[]{order}).ToArray();
                var assets=FindObjectsByType<SourceStation>().First(s=>s.gameObject.scene==gameObject.scene).itemPrefab;
                order.GetComponent<CustomerPresentation>()?.Initialize(order,assets);
            }
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
        Vector3[] AdmissionRoute(Guest guest,bool toChair)
        {
            var approach=TableApproach(guest.table);var destination=toChair?Chair(guest.table):approach;
            return new[]{new Vector3(Settings.entrance.x,0,Settings.sidewalkStart.z),Settings.entrance,new Vector3(Settings.entrance.x,0,-5),new Vector3(approach.x,0,-5),new Vector3(approach.x,0,destination.z),destination};
        }
        internal HostGuestClaim TryClaimForHost(Vector3 hostPosition,out Vector3[] route)
        {
            route=null;HostRouteBlocked=false;if(host==null || AdmissionsClosed)return null;
            var guest=guests.FirstOrDefault(g=>g.phase==0);
            if(guest==null || !guest.walker.Arrived || guest.hostClaim!=System.Guid.Empty)return null;
            var table=Tables.FirstOrDefault(t=>t.Clean);if(table==null)return null;
            var inside=StaffHomes.Entrance(this);var first=KitchenStaffRoute.ToPoint(hostPosition,inside);
            if(first==null){HostRouteBlocked=true;return null;}
            route=first.Concat(new[]{Settings.entrance,new Vector3(Settings.entrance.x,0,Settings.sidewalkStart.z),guest.walker.transform.position}).ToArray();
            var claim=new HostGuestClaim(System.Guid.NewGuid(),guest.id,table);guest.hostClaim=claim.Id;guest.table=table;table.ReserveSeat();return claim;
        }
        internal bool HostClaimValid(HostGuestClaim claim)
        {
            if(claim==null)return false;var guest=guests.FirstOrDefault(g=>g.id==claim.GuestId);
            return guest!=null && guest.hostClaim==claim.Id && guest.table==claim.Table && (guest.phase==0 || guest.phase==1);
        }
        internal bool BeginHostEscort(HostGuestClaim claim,out Vector3[] route)
        {
            route=null;var guest=claim==null?null:guests.FirstOrDefault(g=>g.id==claim.GuestId);
            if(guest==null || guest.phase!=0 || guest.hostClaim!=claim.Id || guest.table!=claim.Table || AdmissionsClosed || guest.waited>=Settings.outsidePatience)
            {ReleaseHostClaim(claim);return false;}
            guest.phase=1;route=AdmissionRoute(guest,false);guest.walker.Go(AdmissionRoute(guest,true));return true;
        }
        internal void CompleteHostEscort(HostGuestClaim claim)
        {
            var guest=claim==null?null:guests.FirstOrDefault(g=>g.id==claim.GuestId);
            if(guest!=null && guest.hostClaim==claim.Id)guest.hostClaim=System.Guid.Empty;
        }
        internal void ReleaseHostClaim(HostGuestClaim claim)
        {
            var guest=claim==null?null:guests.FirstOrDefault(g=>g.id==claim.GuestId);
            if(guest==null || guest.hostClaim!=claim.Id)return;
            guest.hostClaim=System.Guid.Empty;
            if(guest.phase==0 && guest.table==claim.Table){if(claim.Table!=null)claim.Table.CancelArrival();guest.table=null;}
        }
        void TurnAway(Guest guest,string label)
        {
            if(guest.hostClaim!=System.Guid.Empty)
            {
                var claim=new HostGuestClaim(guest.hostClaim,guest.id,guest.table);ReleaseHostClaim(claim);
            }
            guest.phase=5;guest.walker.name=label;guest.walker.Go(new Vector3(guest.walker.transform.position.x,0,Settings.sidewalkExit.z-.65f),Settings.sidewalkExit+Vector3.back*.65f);
        }
        void Spawn()
        {
            var p=Settings.sidewalkStart;
            int look=customerRandom.Next();
            var walker=Walker("Arriving customer",look,p);walker.Go(QueuePosition(guests.Count(g=>g.phase==0 || g.phase==4)));
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
            if(StaffEntering){foreach(var member in staff)member.AllowWork(dt);return;}
            if(StaffLeaving)
            {
                foreach(var member in staff)member.AdvanceDeparture(dt);
                if(staff.All(s=>s.Gone)){Closed=true;RetryPayment();}
                return;
            }
            Elapsed+=dt;
            while(!AdmissionsClosed && spawned<TargetCustomers && Elapsed>=nextArrival){Spawn();nextArrival+=Settings.IntervalForDay(ServiceDayNumber);}
            int queueIndex=0;
            foreach(var guest in guests.ToArray())
            {
                if(!AdmissionsClosed && (guest.phase==0 || guest.phase==4))
                {
                    guest.walker.Go(QueuePosition(queueIndex++));
                    if(guest.phase==0){guest.waited+=dt*OutsidePatienceRate;guest.walker.Advance(dt,Settings.walkingSpeed);}
                }
                // Guests already admitted may finish; unseated arrivals go home at closing.
                if(AdmissionsClosed && (guest.phase==0 || guest.phase==4))
                {
                    TurnAway(guest,"Customer leaving — restaurant closed");
                }
                if(guest.phase==0)
                {
                    var table=Tables.FirstOrDefault(t=>t.Clean);
                    if(host==null && table!=null && queueIndex==1 && guest.walker.Arrived)
                    {
                        guest.table=table;table.ReserveSeat();guest.phase=1;
                        guest.walker.Go(AdmissionRoute(guest,true));
                    }
                    else if(guest.waited>=Settings.outsidePatience)
                    {LostCustomers++;LastLostAt=Elapsed;TurnAway(guest,"Customer left — waited too long");}
                }
                if(guest.phase==1 || guest.phase==3 || guest.phase==4 || guest.phase==5 || guest.phase==6)
                {
                    guest.walker.Advance(dt,Settings.walkingSpeed);
                    if(guest.walker.Arrived)
                    {
                        if(guest.phase==1){guest.phase=2;guest.hostClaim=System.Guid.Empty;guest.walker.gameObject.SetActive(false);guest.table.Seat(guest.recipe,guest.look);}
                        else if(guest.phase==4){guest.phase=0;guest.arrived=Elapsed;guest.waited=0;}
                        else if(guest.phase==6)
                        {
                            // Release the seat at the aisle, not at the front door. Detach ownership
                            // so this guest can never reset the next diner's order while walking out.
                            guest.table.Depart();guest.table=null;guest.phase=3;
                            guest.walker.Go(new Vector3(guest.walker.transform.position.x,0,-5),new Vector3(Settings.entrance.x,0,-5),Settings.entrance);
                        }
                        else if(guest.phase==3){guest.phase=5;guest.walker.Go(new Vector3(guest.walker.transform.position.x,0,Settings.sidewalkExit.z-.65f),Settings.sidewalkExit+Vector3.back*.65f);}
                        else{guests.Remove(guest);Destroy(guest.walker.gameObject);}
                    }
                }
                else if(guest.phase==2 && (guest.table.order.Phase==OrderPhase.Dirty || guest.table.WaitingForMeal && guest.table.PatienceRemaining<=0))
                {
                    if(guest.table.WaitingForMeal){LostCustomers++;LastLostAt=Elapsed;guest.walker.name="Customer left â€” not served";}
                    guest.phase=6;guest.table.BeginDeparture();guest.walker.gameObject.SetActive(true);
                    guest.walker.Go(new Vector3(TableApproach(guest.table).x,0,guest.walker.transform.position.z));
                }
            }
            Expo?.Refresh();StageExpoPass();host?.Advance(dt);server?.Advance(dt);dishwasher?.Advance(dt);busser?.Advance(dt);PrepCook?.Advance(dt);Cook?.Advance(dt);
            if(AdmissionsClosed && guests.Count==0)
            {
                if(staff.Count==0){Closed=true;RetryPayment();}
                else{StaffLeaving=true;foreach(var member in staff)member.BeginDeparture();}
            }
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
