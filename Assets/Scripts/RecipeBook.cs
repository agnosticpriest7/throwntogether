using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Recipe book")]
    public sealed class RecipeBook : ScriptableObject
    {
        public RecipeDefinition[] recipes;
        public static string Instructions(RecipeDefinition recipe)
        {
            if(recipe==null || recipe.ingredient==null) return "Recipe unavailable.";
            var lines=new List<string>();
            var foods=new List<IngredientDefinition>{recipe.ingredient};
            foreach(var part in recipe.additionalIngredients) foods.Add(part.ingredient);
            foreach(var food in foods)
            {
                var actions=new List<string>();
                foreach(var step in recipe.steps)
                {
                    if(step==null || step.ingredient!=food)continue;
                    string station=string.IsNullOrEmpty(step.stationLabel)?(step.input==FoodState.Raw?"Prep board":"Fryer"):step.stationLabel;
                    actions.Add(station+": "+step.duration.ToString("0.#",CultureInfo.InvariantCulture)+" seconds"+(station=="Prep board"?" (Use, stay still)":""));
                }
                lines.Add(food.displayName+" from ingredient storage → "+string.Join(" → ",actions)+". Pick up after each step.");
            }
            lines.Add("Combine each prepared component with a clean plate on an ordinary counter, in any order.");
            lines.Add("Pick up the finished plate from the counter.");
            lines.Add("Shortcut: prepared food + Use at the plate stack plates it in your hands.");
            lines.Add("Serve "+recipe.displayName+" at the matching table; hired servers use the pass. Practice uses pass delivery.");
            for(int i=0;i<lines.Count;i++) lines[i]=(i+1)+". "+lines[i];
            return string.Join("\n",lines);
        }
    }
}
