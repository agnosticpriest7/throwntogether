using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName = "Thrown Together/Processing recipe")]
    public sealed class ProcessingRecipe : ContentDefinition
    {
        public IngredientDefinition ingredient;
        public FoodState input;
        public FoodState output;
        [Min(.01f)] public float duration = 1.5f;
        public bool Accepts(ItemPayload item) => ingredient != null && item != null && !item.isPlate && item.ingredient == ingredient && item.state == input;
    }
}
