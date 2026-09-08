using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Recipe")]
    public class RecipeDefinition : ContentDefinition
    {
        public IngredientDefinition ingredient;
        public FoodState requiredState=FoodState.Cooked;
        public ProcessingRecipe[] steps = new ProcessingRecipe[0];
        public bool Matches(ItemPayload item) => ingredient != null && item != null && item.isPlate && item.ingredient==ingredient && item.state==requiredState;
    }
}
