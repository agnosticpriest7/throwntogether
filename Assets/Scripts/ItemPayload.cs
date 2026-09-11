using System;
using System.Collections.Generic;
using System.Linq;
namespace ThrownTogether
{
    [Serializable] public sealed class IngredientPortion
    {
        public IngredientDefinition ingredient;
        public FoodState state;
    }
    [Serializable] public sealed class ItemPayload
    {
        public IngredientDefinition ingredient;
        public FoodState state;
        public bool isPlate;
        public bool dirty;
        public List<IngredientPortion> additions=new List<IngredientPortion>();
        public bool EmptyPlate => isPlate && !dirty && ingredient==null;
        public string Label => dirty ? "Dirty plate" : ingredient==null ? "Clean plate" :
            (isPlate ? "Plated " : "")+ingredient.NameFor(state)+(additions.Count>0 ? " + "+string.Join(" + ",additions.Select(p=>p.ingredient.NameFor(p.state))) : "");
        public bool ContainsIngredient(IngredientDefinition value)=>ingredient==value || additions.Any(p=>p.ingredient==value);
        public int IngredientCount => (ingredient==null ? 0:1)+additions.Count;
        public bool Contains(IngredientDefinition definition,FoodState required) => ingredient==definition && state==required || additions.Any(p=>p.ingredient==definition && p.state==required);
        public static ItemPayload Food(IngredientDefinition definition) => new ItemPayload {ingredient=definition};
        public static ItemPayload Plate() => new ItemPayload {isPlate=true};
        public static bool CanPlate(ItemPayload plate,ItemPayload food) => plate!=null && plate.isPlate && !plate.dirty && food!=null && !food.isPlate &&
            food.ingredient!=null && food.ingredient.Ready(food.state) && food.additions.Count==0 &&
            (plate.EmptyPlate || plate.additions.Count==0 && plate.ingredient!=food.ingredient && plate.ingredient.combineWith==food.ingredient && plate.state==plate.ingredient.platingState && food.state==food.ingredient.platingState || DailyMenu.CanAssemble(plate,food));
        public void AddFood(ItemPayload food)
        {
            if(ingredient==null) {ingredient=food.ingredient;state=food.state;}
            else additions.Add(new IngredientPortion {ingredient=food.ingredient,state=food.state});
        }
        public void MakeDirty() {isPlate=true;dirty=true;ingredient=null;additions.Clear();}
    }
}
