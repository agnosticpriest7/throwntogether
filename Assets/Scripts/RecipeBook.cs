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
            var lines=new List<string>{"Take "+recipe.ingredient.displayName+" from its crate."};
            var previous=recipe.ingredient;
            foreach(var step in recipe.steps)
            {
                if(step==null) continue;
                if(step.ingredient!=previous) {lines.Add("Set the prepared food on a counter. Take "+step.ingredient.displayName+" from its crate.");previous=step.ingredient;}
                string station=step.input==FoodState.Raw ? "Prep board":"Fryer";
                lines.Add(station+": "+(step.input==FoodState.Raw ? "press Use and stay still for " : "process for ")+step.duration.ToString("0.#",CultureInfo.InvariantCulture)+" seconds, then pick up "+step.ingredient.NameFor(step.output)+".");
            }
            lines.Add("On an ordinary counter, combine all the prepared ingredients with a clean plate. Add each ingredient separately; either order works.");
            lines.Add("Shortcut: while holding prepared food, press Use at the clean-plate stack to plate it in your hands. Add any remaining ingredient on a counter.");
            lines.Add("Restaurant day: carry "+recipe.displayName+" to the matching customer's table. A hired server can collect it from the pass. Practice shifts use automatic pass delivery.");
            for(int i=0;i<lines.Count;i++) lines[i]=(i+1)+". "+lines[i];
            return string.Join("\n",lines);
        }
    }
}
