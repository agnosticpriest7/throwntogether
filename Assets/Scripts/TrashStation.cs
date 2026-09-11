using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class TrashStation : Interactable
    {
        RestaurantDay Day=>FindObjectsByType<RestaurantDay>().FirstOrDefault(d=>d.gameObject.scene==gameObject.scene);
        public override string Prompt(ChefController chef)
        {
            var item=chef.Hands.Item?.Payload;
            if(item==null)return "Hold unwanted food to discard";
            if(item.IngredientCount==0)return "Keep plates — wash dirty plates";
            return "Discard food"+(Day!=null?" ($"+Day.Settings.wasteCost+" waste)":"")+(item.isPlate?"; keep dirty plate":"");
        }
        public override bool Interact(ChefController chef)
        {
            var held=chef.Hands.Item;if(held==null || held.Payload.IngredientCount==0)return false;
            var day=Day;if(day!=null && !day.RecordWaste())return false;
            if(held.Payload.isPlate){held.Payload.MakeDirty();held.RefreshVisual();}
            else{chef.Hands.Release();Destroy(held.gameObject);}
            ShowSuccess();return true;
        }
    }
}
