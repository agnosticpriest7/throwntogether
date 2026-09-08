using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName = "Thrown Together/Dish recipe")]
    // Compatibility asset type keeps the existing Fries asset GUID and references.
    public sealed class DishRecipe : RecipeDefinition { }
}
