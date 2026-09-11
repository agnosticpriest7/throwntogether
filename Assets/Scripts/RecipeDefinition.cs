using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Recipe")]
    public class RecipeDefinition : ContentDefinition
    {
        public IngredientDefinition ingredient;
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
