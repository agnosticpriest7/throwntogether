using UnityEngine;
namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Restaurant Shift")]
    public sealed class ShiftDefinition : ContentDefinition
    {
        public RecipeDefinition[] orders=new RecipeDefinition[0];
    }
}
