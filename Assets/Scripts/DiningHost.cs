using System;
using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    internal sealed class HostGuestClaim
    {
        internal readonly Guid Id;
        internal readonly Guid GuestId;
        internal readonly DiningTable Table;
        internal HostGuestClaim(Guid id,Guid guestId,DiningTable table){Id=id;GuestId=guestId;Table=table;}
    }

    public sealed class DiningHost : MonoBehaviour
    {
        enum Phase {Idle,Approach,Escort}
        RestaurantDay day;DiningWalker walker;StaffMember member;HostGuestClaim claim;Phase phase;
        public string Status {get;private set;}="Waiting to greet customers";
        public bool Available=>member!=null && member.AvailableForWork;

        public void Initialize(RestaurantDay owner)
        {
            day=owner;var go=new GameObject("Hired host");go.transform.SetParent(transform);go.transform.position=owner.GetComponent<KitchenFurniture>().Homes.Home("host");
            walker=go.AddComponent<DiningWalker>();walker.Initialize(day.Settings.walkingVisual,3);
            member=StaffMember.Create(day,"host",walker,null);
            member.ConfigureBreaks(()=>Status,OnBreakChanged);
        }

        void OnBreakChanged(bool requested)
        {
            if(!requested)return;
            if(phase==Phase.Approach){day.ReleaseHostClaim(claim);Reset();member.BeginBreak();}
            else if(phase==Phase.Idle)member.BeginBreak();
        }

        void OnDestroy(){day?.ReleaseHostClaim(claim);}

        public void Advance(float seconds)
        {
            if(day.Closed || !member.AllowWork(seconds))return;
            if(member.BreakRequested && phase==Phase.Idle){member.BeginBreak();return;}
            if(claim!=null && !day.HostClaimValid(claim))Reset();
            if(phase==Phase.Idle)
            {
                claim=day.TryClaimForHost(walker.transform.position,out var route);
                if(claim==null)
                {
                    Status=day.HostRouteBlocked?"Cannot reach the customer queue":"Waiting to greet customers";
                    member.Idle(seconds);return;
                }
                walker.Go(route);phase=Phase.Approach;Status="Greeting next customer";
            }
            float speed=day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed("host");
            walker.Advance(seconds,phase==Phase.Escort?day.Settings.walkingSpeed:speed);
            if(!walker.Arrived)return;
            if(phase==Phase.Approach)
            {
                if(!day.BeginHostEscort(claim,out var route)){Reset();return;}
                walker.Go(route);phase=Phase.Escort;Status="Escorting customer to table";return;
            }
            // RestaurantDay releases the escort when the guest reaches the chair.
            // Reaching our shorter table-approach route alone is not completion.
        }

        void Reset(){claim=null;phase=Phase.Idle;Status="Waiting to greet customers";}
    }
}
