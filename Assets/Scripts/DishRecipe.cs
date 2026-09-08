using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName = "Thrown Together/Dish recipe")]
    public sealed class DishRecipe : ScriptableObject
    {
        public string displayName = "Fries";
        public IngredientDefinition ingredient;
        public FoodState requiredState = FoodState.Cooked;
        public bool Matches(ItemPayload item) => item != null && item.isPlate && item.ingredient == ingredient && item.state == requiredState;
    }
}
