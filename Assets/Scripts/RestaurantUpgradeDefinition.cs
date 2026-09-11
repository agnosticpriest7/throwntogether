using UnityEngine;

namespace ThrownTogether
{
    public enum RestaurantPurchaseKind { CounterBay, FryerBay, FasterFryers }
    [CreateAssetMenu(menuName="Thrown Together/Restaurant upgrade")]
    public sealed class RestaurantUpgradeDefinition : ContentDefinition
    {
        public RestaurantPurchaseKind kind;
        public int cost=50;
        public GameObject stationPrefab;
        public Vector3[] layoutPositions=new Vector3[0];
        public float processingSpeed=1.25f;
    }
}
