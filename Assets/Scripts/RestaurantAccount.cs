using System;
using UnityEngine;
namespace ThrownTogether
{
    [Serializable] public sealed class RestaurantSave
    {
        public int schemaVersion=1, cash, nextDay=1, activeDay, settledDay;
        public string[] purchases=new string[0];
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
                if(loaded==null||loaded.schemaVersion!=1||loaded.cash<0||loaded.nextDay<1||loaded.activeDay<0||loaded.settledDay<0||loaded.settledDay>loaded.activeDay||loaded.activeDay>=loaded.nextDay||loaded.purchases==null)throw new ArgumentException("Unsupported save");
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
        public bool Owns(string id)=>Array.IndexOf(Data.purchases,id)>=0;
        public int StartDay()
        {
            var next=Copy();next.activeDay=next.nextDay++;return Commit(next)?next.activeDay:0;
        }
        public bool Settle(int day,int baseIncome,int bonus)
        {
            if(day<=0||day!=Data.activeDay||day<=Data.settledDay||baseIncome<0||bonus<0)return false;
            var next=Copy();next.cash=checked(next.cash+baseIncome+bonus);next.settledDay=day;return Commit(next);
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
