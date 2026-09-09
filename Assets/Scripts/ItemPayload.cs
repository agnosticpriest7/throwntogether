using System;

namespace ThrownTogether
{
    [Serializable]
    public sealed class ItemPayload
    {
        public IngredientDefinition ingredient;
        public FoodState state;
        public bool isPlate;
        public bool EmptyPlate => isPlate && ingredient == null;
        public string Label => ingredient == null ? "Clean plate" : (isPlate ? "Plated " : "") + ingredient.NameFor(state);
        public static ItemPayload Food(IngredientDefinition definition) => new ItemPayload { ingredient = definition };
        public static ItemPayload Plate() => new ItemPayload { isPlate = true };
        public static bool CanPlate(ItemPayload plate, ItemPayload food) => plate != null && plate.EmptyPlate &&
            food != null && !food.isPlate && food.ingredient != null && food.state == food.ingredient.platingState;
    }
}
