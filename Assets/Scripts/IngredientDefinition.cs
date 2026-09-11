using UnityEngine;

namespace ThrownTogether
{
    public enum FoodState { Raw, Cut, Cooked, Griddled, Grilled }
    public enum IngredientVisualKind { Potato, Mushroom, Tomato, Lettuce, Egg, Chicken }

    [CreateAssetMenu(menuName = "Thrown Together/Ingredient")]
    public sealed class IngredientDefinition : ContentDefinition
    {
        public IngredientVisualKind visualKind;
        public IngredientDefinition combineWith;
        public string requiredPurchase="";
        public FoodState[] additionalPlatingStates=new FoodState[0];
        public bool Ready(FoodState state)=>state==platingState || System.Array.IndexOf(additionalPlatingStates,state)>=0;
        public bool Unlocked=>string.IsNullOrEmpty(requiredPurchase) || RestaurantAccounts.Current.Owns(requiredPurchase);
        public FoodState platingState=FoodState.Cooked;
        public string[] stateNames = { "Raw potato", "Cut potato", "Fries" };
        public Color[] stateColors = { new Color(.65f,.4f,.16f), new Color(.95f,.85f,.5f), new Color(1,.64f,.09f) };
        public string NameFor(FoodState state) => stateNames[Mathf.Min((int)state,stateNames.Length-1)];
        public Color ColorFor(FoodState state) => stateColors[Mathf.Min((int)state,stateColors.Length-1)];
    }
}
