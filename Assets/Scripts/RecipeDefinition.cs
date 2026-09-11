using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Recipe")]
    public class RecipeDefinition : ContentDefinition
    {
        public IngredientDefinition ingredient;
        public string[] requiredPurchases=new string[0];
        public bool Unlocked(RestaurantAccount account)=>System.Array.TrueForAll(requiredPurchases,account.Owns);
        public string LockReason=>"Requires "+string.Join(" + ",System.Array.ConvertAll(requiredPurchases,p=>p=="griddle"?"Griddle":p=="grill"?"Grill":p));
        public bool HasPortion(IngredientDefinition food,FoodState state)=>ingredient==food && requiredState==state || System.Array.Exists(additionalIngredients,p=>p.ingredient==food && p.state==state);
        public bool AcceptsPortions(ItemPayload item)
        {
            if(item.ingredient!=null && !HasPortion(item.ingredient,item.state))return false;
            foreach(var p in item.additions)if(!HasPortion(p.ingredient,p.state))return false;
            return true;
        }
        public int salePrice=10;
        public FoodState requiredState=FoodState.Cooked;
        public ProcessingRecipe[] steps = new ProcessingRecipe[0];
        public IngredientPortion[] additionalIngredients=new IngredientPortion[0];
        public bool Matches(ItemPayload item)
        {
            if(ingredient==null || item==null || !item.isPlate || item.dirty || item.IngredientCount!=1+additionalIngredients.Length || !item.Contains(ingredient,requiredState)) return false;
            foreach(var part in additionalIngredients) if(!item.Contains(part.ingredient,part.state)) return false;
            return true;
        }
    }
}
