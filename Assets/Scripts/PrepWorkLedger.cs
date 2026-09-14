using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace ThrownTogether
{
    // An exclusive reservation on one unit of missing prep work. Immutable, and only the
    // owning ledger creates one.
    public sealed class PrepWorkClaim
    {
        public Guid Id {get;}
        public Guid WorkerId {get;}
        public string Key {get;}
        internal PrepWorkClaim(Guid id,Guid workerId,string key)
        {Id=id;WorkerId=workerId;Key=key;}
    }

    // Who is already fetching or making what, so two workers never chase one portion.
    //
    // The ledger is not the authority on demand: the caller supplies aggregate deficits
    // (fired tickets minus physical portions already present) and replaces that snapshot
    // whenever the world changes. Claims are deliberately independent of the snapshot,
    // because a worker carrying food still holds their reservation after the deficit that
    // justified it disappears.
    //
    // Pure C#, deterministic, main thread only. No Unity types, recipes, paths or timers.
    public sealed class PrepWorkLedger
    {
        // Ordered exactly as supplied. Small enough that a scan beats a second index.
        readonly List<KeyValuePair<string,int>> deficits=new List<KeyValuePair<string,int>>();
        readonly List<PrepWorkClaim> claims=new List<PrepWorkClaim>();
        static bool Blank(string key)=>string.IsNullOrWhiteSpace(key);
        // Validates everything before touching state, so a rejected snapshot changes nothing.
        public void SetDeficits(IEnumerable<KeyValuePair<string,int>> deficits)
        {
            if(deficits==null)throw new ArgumentNullException(nameof(deficits));
            var next=new List<KeyValuePair<string,int>>();
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in deficits)
            {
                if(Blank(entry.Key))throw new ArgumentException("Prep work keys cannot be blank.",nameof(deficits));
                if(entry.Value<0)throw new ArgumentException("Missing portions cannot be negative: "+entry.Key,nameof(deficits));
                if(!seen.Add(entry.Key))throw new ArgumentException("Duplicate prep work key: "+entry.Key,nameof(deficits));
                next.Add(entry);
            }
            this.deficits.Clear();this.deficits.AddRange(next);
        }
        int Deficit(string key)
        {
            if(Blank(key))return 0;
            foreach(var entry in deficits)if(string.Equals(entry.Key,key,StringComparison.Ordinal))return entry.Value;
            return 0;
        }
        bool Known(string key)
        {
            if(Blank(key))return false;
            foreach(var entry in deficits)if(string.Equals(entry.Key,key,StringComparison.Ordinal))return true;
            return false;
        }
        // Counts live claims, including ones whose key has since left the snapshot.
        public int Reserved(string key)
        {
            if(Blank(key))return 0;
            int count=0;
            foreach(var claim in claims)if(string.Equals(claim.Key,key,StringComparison.Ordinal))count++;
            return count;
        }
        public int Available(string key)=>Math.Max(0,Deficit(key)-Reserved(key));
        public IReadOnlyList<PrepWorkClaim> Claims=>new ReadOnlyCollection<PrepWorkClaim>(new List<PrepWorkClaim>(claims));
        bool Busy(Guid workerId)
        {
            foreach(var claim in claims)if(claim.WorkerId==workerId)return true;
            return false;
        }
        bool Taken(Guid id)
        {
            foreach(var claim in claims)if(claim.Id==id)return true;
            return false;
        }
        // One live claim per worker. Nothing is stolen and nothing switches on its own.
        public PrepWorkClaim TryClaim(Guid workerId,string key)
        {
            if(workerId==Guid.Empty || !Known(key) || Busy(workerId) || Available(key)<=0)return null;
            Guid id;
            do{id=Guid.NewGuid();}while(id==Guid.Empty || Taken(id));
            var claim=new PrepWorkClaim(id,workerId,key);claims.Add(claim);return claim;
        }
        // Only the owner can release, and only the claim they actually hold.
        public bool Release(Guid workerId,Guid claimId)
        {
            if(workerId==Guid.Empty || claimId==Guid.Empty)return false;
            for(int i=0;i<claims.Count;i++)
                if(claims[i].Id==claimId && claims[i].WorkerId==workerId){claims.RemoveAt(i);return true;}
            return false;
        }
    }
}
