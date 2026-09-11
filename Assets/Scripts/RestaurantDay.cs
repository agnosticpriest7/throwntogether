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
        public int LostCustomers {get;private set;}
        public float LastLostAt {get;private set;}=-100;
        public int DayNumber {get;private set;}
        public int WaitingOutside=>guests.Count(g=>g.phase==0);
        public float OldestWait=>guests.Where(g=>g.phase==0).Select(g=>Elapsed-g.arrived).DefaultIfEmpty(0).Max();
        public string Clock {get {int m=660+Mathf.FloorToInt(Mathf.Clamp01(Elapsed/Settings.durationSeconds)*660);return ((m/60+11)%12+1)+":"+(m%60).ToString("00")+(m<720?" AM":" PM");}}
        readonly List<Guest> guests=new List<Guest>();
        sealed class Guest {public DiningWalker walker;public DiningTable table;public RecipeDefinition recipe;public float arrived;public int phase,look;}
        RestaurantShift shift;int spawned;float nextArrival;DiningServer server;
        public void Begin(RestaurantShift owner,DayServiceDefinition settings)
        {
            shift=owner;Settings=settings;DayNumber=RestaurantAccounts.Current.StartDay();nextArrival=settings.firstArrival;
            Tables=owner.seats.Select(o=>
            {
                o.manualService=true;o.mealSeconds=settings.eatingSeconds;o.ResetOrder(null);
                var table=o.gameObject.AddComponent<DiningTable>();table.day=this;table.order=o;table.stationName="Dining table";table.SetGuestVisible(false);return table;
            }).ToArray();
            ApplyPurchases();
        }
        void ApplyPurchases()
        {
            var account=RestaurantAccounts.Current;
            foreach(var purchase in Settings.purchases)
            {
                if(purchase==null||!account.Owns(purchase.id)||purchase.stationPrefab==null)continue;
                int layout=Mathf.Clamp(SessionOptions.Kitchen,0,purchase.layoutPositions.Length-1);
                if(layout<0)continue;
                var station=Instantiate(purchase.stationPrefab,purchase.layoutPositions[layout],Quaternion.identity);station.name=purchase.displayName;
            }
            foreach(var purchase in Settings.purchases)
                if(purchase!=null && account.Owns(purchase.id) && purchase.kind==RestaurantPurchaseKind.FasterFryers)
                    foreach(var fryer in FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None))
                        if(fryer.gameObject.scene==gameObject.scene && !fryer.requiresAttendance)fryer.processingSpeed=purchase.processingSpeed;
            if(Settings.serverRole!=null && account.Owns(Settings.serverRole.id))
            {
                var pass=FindObjectsByType<ServiceStation>(FindObjectsSortMode.None).First(s=>s.gameObject.scene==gameObject.scene);
                server=gameObject.AddComponent<DiningServer>();server.Initialize(this,pass);
            }
        }
        DiningWalker Walker(string label,int look,Vector3 position)
        {
            var go=new GameObject(label);go.transform.SetParent(transform);go.transform.position=position;
            var walker=go.AddComponent<DiningWalker>();walker.Initialize(Settings.walkingVisual,look,true);return walker;
        }
        Vector3 Aisle(float z)=>new Vector3(Settings.diningAisleX,0,z);
        Vector3 Chair(DiningTable table)
        {
            var visual=table.order.GetComponent<CustomerPresentation>()?.seatedVisual;
            var p=visual!=null?visual.transform.position:table.order.customerVisual.position;p.y=0;return p;
        }
        void Spawn()
        {
            var p=Settings.entrance+Vector3.right*((spawned%3-1)*.8f);
            guests.Add(new Guest {walker=Walker("Arriving customer",spawned%2,p),look=spawned%2,arrived=Elapsed,recipe=shift.definition.orders[spawned%shift.definition.orders.Length]});spawned++;
        }
        public void Advance(float seconds)
        {
            if(Closed)return;
            // Bounded substeps keep queue patience and arrival order deterministic in tests and slow frames.
            float remaining=Mathf.Max(0,seconds);
            while(remaining>0 && !Closed){float dt=Mathf.Min(.1f,remaining);remaining-=dt;Tick(dt);}
        }
        void Tick(float dt)
        {
            Elapsed=Mathf.Min(Settings.durationSeconds,Elapsed+dt);
            if(Elapsed>=Settings.durationSeconds){Closed=true;RetryPayment();return;}
            while(spawned<Settings.arrivals && Elapsed>=nextArrival){Spawn();nextArrival+=Settings.arrivalInterval;}
            foreach(var guest in guests.ToArray())
            {
                if(guest.phase==0)
                {
                    var table=Tables.FirstOrDefault(t=>t.Clean);
                    if(table!=null)
                    {
                        guest.table=table;table.ReserveSeat();guest.phase=1;
                        guest.walker.Go(new Vector3(Settings.entrance.x,0,-5),Aisle(-5),Aisle(Chair(table).z),Chair(table));
                    }
                    else if(Elapsed-guest.arrived>=Settings.outsidePatience)
                    {LostCustomers++;LastLostAt=Elapsed;guest.phase=3;guest.walker.name="Customer left — waited too long";guest.walker.Go(Settings.entrance+Vector3.right*4);}
                }
                if(guest.phase==1 || guest.phase==3)
                {
                    guest.walker.Advance(dt,Settings.walkingSpeed);
                    if(guest.walker.Arrived)
                    {
                        if(guest.phase==1){guest.phase=2;guest.walker.gameObject.SetActive(false);guest.table.Seat(guest.recipe,guest.look);}
                        else{if(guest.table!=null)guest.table.Depart();guests.Remove(guest);Destroy(guest.walker.gameObject);}
                    }
                }
                else if(guest.phase==2 && guest.table.order.Phase==OrderPhase.Dirty)
                {
                    guest.phase=3;guest.table.SetGuestVisible(false);guest.walker.gameObject.SetActive(true);
                    guest.walker.Go(Aisle(guest.walker.transform.position.z),Aisle(-5),new Vector3(Settings.entrance.x,0,-5),Settings.entrance);
                }
            }
            server?.Advance(dt);
        }
        public void RecordMeal(RecipeDefinition recipe,float waiting)
        {
            if(Closed)return;Served++;BaseIncome+=Mathf.Max(0,recipe.salePrice);
            Bonuses+=Mathf.RoundToInt(Settings.maximumBonus*Mathf.Clamp01(1-Mathf.Max(0,waiting)/Settings.bonusWindow));
        }
        public bool RetryPayment()
        {
            if(!Closed)return false;if(Paid)return true;
            Paid=RestaurantAccounts.Current.Settle(DayNumber,BaseIncome,Bonuses);return Paid;
        }
    }
}
