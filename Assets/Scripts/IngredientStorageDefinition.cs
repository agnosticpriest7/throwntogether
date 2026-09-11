using UnityEngine;
namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Ingredient storage")]
    public sealed class IngredientStorageDefinition : ContentDefinition
    {
        public IngredientDefinition[] ingredients=new IngredientDefinition[0];
    }
}
