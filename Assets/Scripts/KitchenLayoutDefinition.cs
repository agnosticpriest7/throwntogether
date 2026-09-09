using UnityEngine;
namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Kitchen layout")]
    public sealed class KitchenLayoutDefinition : ContentDefinition
    {
        // Ordered anchors: potato, mushroom, tomato, prep, fryer, plates, counter, spare, pass.
        public Vector3[] stations;
        public Vector3 playerOneSpawn, playerTwoSpawn;
    }
}
