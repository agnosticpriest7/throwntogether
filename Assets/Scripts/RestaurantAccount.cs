using System;
using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    [Serializable] public sealed class RestaurantSave
    {
        public int schemaVersion=1, cash, nextDay=1, activeDay, settledDay, completedDays;
        public int arrangementPaidBreak=-1;
        public string[] purchases=new string[0];
        public string[] selectedMenu=new string[0];
        public FurniturePlacement[] furniture=new FurniturePlacement[0];
    }
    // Separate from audio/display settings. Writes commit a copy, never partially debit live state.
    public sealed class RestaurantAccount
    {
        readonly ISettingsStorage storage;
        public RestaurantSave Data {get;private set;}=new RestaurantSave();
        public string Problem {get;private set;}="";
        public bool Writable {get;private set;}=true;
        public RestaurantAccount(ISettingsStorage storage)
        {
            this.storage=storage;
            try
            {
                string json=storage.Read();if(string.IsNullOrEmpty(json))return;
                if(!json.Contains("\"schemaVersion\""))throw new ArgumentException("Missing schema");
                var loaded=JsonUtility.FromJson<RestaurantSave>(json);
                if(loaded==null||loaded.schemaVersion!=1||loaded.completedDays<0||loaded.completedDays>loaded.settledDay||loaded.cash<0||loaded.nextDay<1||loaded.activeDay<0||loaded.settledDay<0||loaded.settledDay>loaded.activeDay||loaded.activeDay>=loaded.nextDay||loaded.purchases==null)throw new ArgumentException("Unsupported save");
                if(loaded.furniture==null)loaded.furniture=new FurniturePlacement[0];
                if(loaded.selectedMenu==null)loaded.selectedMenu=new string[0];
                // Optional schema-1 addition: preserve old saves and begin with their existing dishes.
                if(!json.Contains("\"selectedMenu\""))loaded.selectedMenu=System.Array.ConvertAll(System.Array.FindAll(DailyMenu.Catalog,r=>r.requiredPurchases.Length==0),r=>r.id);
                Data=loaded;
            }
            catch(Exception){Writable=false;Problem="Restaurant save cannot be read safely. Existing data preserved; earnings/purchases disabled.";}
        }
        RestaurantSave Copy()=>JsonUtility.FromJson<RestaurantSave>(JsonUtility.ToJson(Data));
        bool Commit(RestaurantSave next)
        {
            if(!Writable)return false;
            try{storage.Write(JsonUtility.ToJson(next));Data=next;Problem="";return true;}
            catch(Exception){Problem="Could not save. No money or purchase was changed. Retry when storage is available.";return false;}
        }
        // Invoked only after the player's explicit reset confirmation; settings use separate storage.
        public bool ResetCareer()
        {
            var fresh=new RestaurantSave();
            try{storage.Write(JsonUtility.ToJson(fresh));Data=fresh;Writable=true;Problem="";return true;}
            catch(Exception){Problem="Career could not be reset. Existing progress is unchanged. Please retry.";return false;}
        }
        public bool SetMenu(string[] ids)
        {
            if(ids==null || System.Array.Exists(ids,string.IsNullOrWhiteSpace))return false;
            var next=Copy();next.selectedMenu=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Distinct(ids));return Commit(next);
        }
        public int ArrangementFee=>Data.completedDays==0 || Data.arrangementPaidBreak==Data.settledDay?0:10;
        public bool SetFurniture(int kitchen,FurniturePlacement[] placements,bool charge=false)
        {
            if(Data.activeDay>Data.settledDay || kitchen<0 || kitchen>2 || placements==null)return false;
            if(Array.Exists(placements,p=>p==null || p.kitchen!=kitchen || string.IsNullOrWhiteSpace(p.id) || p.slot<0 || p.slot>=KitchenFurniture.Slots.Length || p.turns<0 || p.turns>3))return false;
            if(System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(placements,p=>p.id)).Count()!=placements.Length || System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(placements,p=>p.slot)).Count()!=placements.Length)return false;
            int fee=charge?ArrangementFee:0;
            if(Data.cash<fee){Problem="Rearranging costs $"+fee+" for this break. Your draft is retained.";return false;}
            var next=Copy();next.cash-=fee;if(charge)next.arrangementPaidBreak=next.settledDay;next.furniture=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(System.Linq.Enumerable.Where(next.furniture??new FurniturePlacement[0],p=>p!=null && p.kitchen!=kitchen),placements));return Commit(next);
        }
        public bool Owns(string id)=>Array.IndexOf(Data.purchases,id)>=0;
        public int StartDay()
        {
            var next=Copy();next.activeDay=next.nextDay++;return Commit(next)?next.activeDay:0;
        }
        public bool Settle(int day,int baseIncome,int bonus,int waste=0)
        {
            if(day<=0||day!=Data.activeDay||day<=Data.settledDay||baseIncome<0||bonus<0||waste<0)return false;
            var next=Copy();next.cash=checked(next.cash+Math.Max(0,checked(baseIncome+bonus)-waste));next.settledDay=day;next.completedDays++;return Commit(next);
        }
        public bool Buy(string id,int cost)
        {
            if(string.IsNullOrWhiteSpace(id)||cost<0||Owns(id)||Data.cash<cost||Data.activeDay>Data.settledDay)return false;
            var next=Copy();next.cash-=cost;var owned=new string[next.purchases.Length+1];Array.Copy(next.purchases,owned,next.purchases.Length);owned[owned.Length-1]=id;next.purchases=owned;return Commit(next);
        }
    }
    public static class RestaurantAccounts
    {
        static RestaurantAccount current;
        public static RestaurantAccount Current=>current??(current=new RestaurantAccount(new RestaurantStorage()));
        public static void UseStorage(ISettingsStorage storage)=>current=new RestaurantAccount(storage);
        public static void ResetCache()=>current=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>current=null;
        sealed class RestaurantStorage:ISettingsStorage
        {
            const string Key="ThrownTogether.Restaurant.v1";
            public string Read()=>PlayerPrefs.GetString(Key,"");
            public void Write(string json)
            {
                if(PlayerPrefs.HasKey(Key))PlayerPrefs.SetString(Key+".backup",PlayerPrefs.GetString(Key));
                PlayerPrefs.SetString(Key,json);PlayerPrefs.Save();
            }
        }
    }
}
