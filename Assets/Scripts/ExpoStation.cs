using UnityEngine;
namespace ThrownTogether
{
    // The physical Expo counter. It owns no queue rules and no ticket state: it only
    // routes the acting chef to that chef's own browser. RestaurantExpo stays the single
    // authority, and the installer decides where this station lives.
    public sealed class ExpoStation : Interactable
    {
        public RestaurantDay Day {get;private set;}
        // Called by the installer once the day's Settings and Tables exist. Invalid input
        // leaves the station unavailable rather than adopting a day from another scene.
        public void Initialize(RestaurantDay day)
        {
            if(day==null || day.gameObject.scene!=gameObject.scene)return;
            Day=day;day.EnableExpo();
        }
        // Null whenever the station, day or service cannot accept firing right now.
        public RestaurantExpo Expo=>isActiveAndEnabled && Day!=null && Day.gameObject.scene==gameObject.scene &&
            !Day.Closed && !Day.AwaitingMenu ? Day.Expo : null;
        public override string Status => stationName+(Expo!=null ? "\n"+Expo.ActiveCount+" / "+Expo.Capacity+" active":"");
        public override string Prompt(ChefController chef)
        {
            var expo=Expo;
            if(expo==null)return stationName+" — closed";
            return expo.ActiveCount>=expo.Capacity
                ? "Expo — kitchen queue full ("+expo.ActiveCount+" / "+expo.Capacity+")"
                : "Expo — fire orders ("+expo.ActiveCount+" / "+expo.Capacity+" active)";
        }
        public override bool Interact(ChefController chef)
        {
            if(chef==null || Expo==null || RestaurantMenu.GameplayBlocked)return false;
            var input=chef.GetComponent<ChefInput>();
            return input!=null && input.OpenExpo(this);
        }
    }
}
