using UnityEngine;

namespace ThrownTogether
{
    public enum FoodState { Raw, Cut, Cooked }
    public enum IngredientVisualKind { Potato, Mushroom, Tomato }

    [CreateAssetMenu(menuName = "Thrown Together/Ingredient")]
    public sealed class IngredientDefinition : ContentDefinition
    {
        public IngredientVisualKind visualKind;
        public FoodState platingState=FoodState.Cooked;
        public string[] stateNames = { "Raw potato", "Cut potato", "Fries" };
        public Color[] stateColors = { new Color(.65f,.4f,.16f), new Color(.95f,.85f,.5f), new Color(1,.64f,.09f) };
        public string NameFor(FoodState state) => stateNames[(int)state];
        public Color ColorFor(FoodState state) => stateColors[(int)state];
    }
}
