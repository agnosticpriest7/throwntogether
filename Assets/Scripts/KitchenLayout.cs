using UnityEngine;
namespace ThrownTogether
{
    [DefaultExecutionOrder(-200)]
    public sealed class KitchenLayout : MonoBehaviour
    {
        public KitchenLayoutDefinition[] choices;
        public Transform[] anchors;
        public ChefController chef;
        public Transform secondSpawn;
        public DishRecipe tomatoSalad;
        public string CurrentName { get; private set; }="First Service";
        public bool Apply(int index)
        {
            if(choices==null || index<0 || index>=choices.Length) return false;
            var layout=choices[index];
            if(layout==null || layout.stations==null || anchors==null || layout.stations.Length!=anchors.Length) return false;
            foreach(var anchor in anchors) if(anchor==null) return false;
            for(int i=0;i<anchors.Length;i++) anchors[i].position=layout.stations[i];
            var motor=chef.GetComponent<CharacterController>(); bool enabled=motor.enabled; motor.enabled=false;
            chef.transform.position=layout.playerOneSpawn; motor.enabled=enabled;
            secondSpawn.position=layout.playerTwoSpawn; CurrentName=layout.displayName;
            Physics.SyncTransforms(); return true;
        }
        private void Awake()
        {
            if(!Apply(SessionOptions.Kitchen)) { SessionOptions.Kitchen=0; Apply(0); }
            var hud=GetComponent<RestaurantHud>();
            if(hud!=null && hud.shift==null && SessionOptions.Training=="Tomato salad") hud.order.recipe=tomatoSalad;
        }
    }
}
