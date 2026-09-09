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
            foreach(var step in recipe.steps)
            {
                if(step==null) continue;
                string station=step.input==FoodState.Raw ? "Prep board":"Fryer";
                lines.Add(station+": process for "+step.duration.ToString("0.#",CultureInfo.InvariantCulture)+" seconds, then pick up "+recipe.ingredient.NameFor(step.output)+".");
            }
            lines.Add("On an ordinary counter, combine the prepared food with a clean plate. Food or plate can go down first.");
            lines.Add("Pick up the finished plate from the counter.");
            lines.Add("Serve at the pass with the bell when a customer orders "+recipe.displayName+".");
            for(int i=0;i<lines.Count;i++) lines[i]=(i+1)+". "+lines[i];
            return string.Join("\n",lines);
        }
    }
}
