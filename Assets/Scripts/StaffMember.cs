using System;
using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    // Shared commute/idle presentation around the existing role's working state machine.
    public sealed class StaffMember:MonoBehaviour
    {
        enum Phase {Arriving,Working,GoingToBreak,Resting,Stowing,Leaving,Gone}
        Phase phase;RestaurantDay day;DiningWalker walker;CarrySlot hands;CounterStation output;
        Func<string> workingStatus;Action<bool> breakChanged;bool breakRequested;
        public string Role {get;private set;}
        public string DisplayName=>Role=="prep-cook"?"Prep cook":Role=="dishwasher"?"Dishwasher":Role=="busser"?"Busser":Role=="server"?"Server":Role=="host"?"Host":"Cook";
        public Vector3 Home=>day.GetComponent<KitchenFurniture>().Homes.Home(Role);
        public bool Arriving=>phase==Phase.Arriving;
        public bool Gone=>phase==Phase.Gone;
        public bool BreakRequested=>breakRequested;
        public bool OnBreak=>phase==Phase.Resting;
        public bool GoingToBreak=>phase==Phase.GoingToBreak;
        public bool AvailableForWork=>phase==Phase.Working && !breakRequested;
        public string DisplayStatus=>phase==Phase.Resting?"On break":phase==Phase.GoingToBreak?"Going to break":breakRequested?"Finishing: "+CleanStatus(workingStatus?.Invoke()):phase==Phase.Working?CleanStatus(workingStatus?.Invoke()):Status;
        public string BreakAction=>breakRequested?"Resume":"Take break";
        public string Status {get;private set;}="Arriving";
        public static StaffMember Create(RestaurantDay day,string role,DiningWalker walker,CarrySlot hands)
        {
            var member=walker.gameObject.AddComponent<StaffMember>();member.day=day;member.Role=role;member.walker=walker;member.hands=hands;
            var route=KitchenStaffRoute.ToPoint(StaffHomes.Entrance(day),member.Home);
            if(route==null)throw new System.InvalidOperationException("Staff home is unreachable: "+role);
            walker.transform.position=day.Settings.sidewalkStart+Vector3.left*(day.Staff.Count*.95f);
            walker.Go(new[]{new Vector3(day.Settings.entrance.x,0,day.Settings.sidewalkStart.z),day.Settings.entrance,StaffHomes.Entrance(day)}.Concat(route).ToArray());
            day.RegisterStaff(member);return member;
        }
        static string CleanStatus(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return "current task";
            return value.StartsWith("Finishing: ",StringComparison.OrdinalIgnoreCase)?value.Substring(11):value;
        }
        public void ConfigureBreaks(Func<string> status,Action<bool> changed=null){workingStatus=status;breakChanged=changed;}
        public bool ToggleBreak()
        {
            if(day==null || !day.CanManageBreaks || phase==Phase.Arriving || phase==Phase.Stowing || phase==Phase.Leaving || phase==Phase.Gone)return false;
            breakRequested=!breakRequested;
            if(!breakRequested && (phase==Phase.GoingToBreak || phase==Phase.Resting)){phase=Phase.Working;Status="";walker.Go(transform.position);}
            breakChanged?.Invoke(breakRequested);return true;
        }
        public bool BeginBreak(string blocked="")
        {
            if(!breakRequested || phase!=Phase.Working)return phase==Phase.GoingToBreak || phase==Phase.Resting;
            if(hands?.Item!=null){Status=string.IsNullOrEmpty(blocked)?"Finishing current task":blocked;return false;}
            if(Vector3.Distance(transform.position,Home)<=.15f){phase=Phase.Resting;Status="On break";walker.Go(transform.position);return true;}
            var route=KitchenStaffRoute.ToPoint(transform.position,Home);
            if(route==null){Status=string.IsNullOrEmpty(blocked)?"Break route blocked":blocked;return false;}
            walker.Go(route);phase=Phase.GoingToBreak;Status="Going to break";return true;
        }
        float Speed=>day.Settings.walkingSpeed*RestaurantAccounts.Current.StaffSpeed(StaffHomes.PurchaseId(Role));
        public bool AllowWork(float seconds)
        {
            if(phase==Phase.Arriving){walker.Advance(seconds,Speed);if(walker.Arrived){phase=Phase.Working;Status="";}return false;}
            if(phase==Phase.GoingToBreak)
            {
                walker.Advance(seconds,Speed,hands?.Item!=null);
                if(walker.Arrived){phase=Phase.Resting;Status="On break";}
                return false;
            }
            if(phase==Phase.Resting)return false;
            if(phase==Phase.Working && day.StaffEntering)walker.Advance(seconds,0,hands?.Item!=null);
            return phase==Phase.Working;
        }
        public void Idle(float seconds)
        {
            if(phase!=Phase.Working || hands?.Item!=null)return;
            if(Vector3.Distance(transform.position,Home)>.15f && (walker.Arrived || Vector3.Distance(walker.Destination,Home)>.1f)){var path=KitchenStaffRoute.ToPoint(transform.position,Home);if(path!=null)walker.Go(path);}
            walker.Advance(seconds,Speed);
        }
        public void BeginDeparture()
        {
            if(phase==Phase.Gone || phase==Phase.Leaving || phase==Phase.Stowing)return;
            breakRequested=false;breakChanged?.Invoke(false);
            phase=Phase.Stowing;Status="Putting food away";output=null;
        }
        public void AdvanceDeparture(float seconds)
        {
            if(phase==Phase.Gone)return;
            if(phase==Phase.Stowing)
            {
                if(hands?.Item!=null)
                {
                    if(output==null || output.slot.Item!=null)
                    {
                        output=UnityEngine.Object.FindObjectsByType<CounterStation>().Where(c=>c.gameObject.scene==gameObject.scene && c.GetType()==typeof(CounterStation) && c.slot.Item==null && KitchenStaffRoute.ToStation(transform.position,c.transform)!=null).OrderBy(c=>(c.transform.position-transform.position).sqrMagnitude).FirstOrDefault();
                        if(output==null){Status="Clear a counter so "+Role+" can leave";return;}
                        walker.Go(KitchenStaffRoute.ToStation(transform.position,output.transform));
                    }
                    walker.Advance(seconds,Speed,true);if(!walker.Arrived)return;
                    if(!output.slot.TryTake(hands.Item)){output=null;return;}
                }
                if(transform.position.z<=day.Settings.entrance.z+.2f)
                {
                    walker.Go(new Vector3(transform.position.x,0,day.Settings.sidewalkExit.z),day.Settings.sidewalkExit);phase=Phase.Leaving;Status="Leaving";
                    walker.Advance(seconds,Speed,false);return;
                }
                var route=KitchenStaffRoute.ToPoint(transform.position,StaffHomes.Entrance(day));
                if(route==null){Status="Exit route blocked: "+Role;return;}
                walker.Go(route.Concat(new[]{day.Settings.entrance,new Vector3(day.Settings.entrance.x,0,day.Settings.sidewalkExit.z),day.Settings.sidewalkExit}).ToArray());phase=Phase.Leaving;Status="Leaving";
            }
            walker.Advance(seconds,Speed,hands?.Item!=null);
            if(walker.Arrived){phase=Phase.Gone;Status="Off duty";gameObject.SetActive(false);}
        }
    }
}
