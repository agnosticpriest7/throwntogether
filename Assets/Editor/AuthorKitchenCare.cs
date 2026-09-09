using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    public static class AuthorKitchenCare
    {
        public static void Create()
        {
            if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Author only with a clean, stopped scene.");
            var returnPath=EditorSceneManager.GetActiveScene().path;
            const string data="Assets/Data/VerticalSlice/";
            var lettuce=AssetDatabase.LoadAssetAtPath<IngredientDefinition>(data+"Lettuce.asset");
            if(lettuce==null) {lettuce=ScriptableObject.CreateInstance<IngredientDefinition>();AssetDatabase.CreateAsset(lettuce,data+"Lettuce.asset");}
            lettuce.id="lettuce";lettuce.displayName="Lettuce";lettuce.visualKind=IngredientVisualKind.Lettuce;lettuce.platingState=FoodState.Cut;
            lettuce.stateNames=new[]{"Whole lettuce","Chopped lettuce","Cooked lettuce"};lettuce.stateColors=new[]{new Color(.22f,.62f,.09f),new Color(.3f,.74f,.12f),new Color(.18f,.34f,.05f)};
            var tomato=AssetDatabase.LoadAssetAtPath<IngredientDefinition>(data+"Tomato.asset");
            lettuce.combineWith=tomato;tomato.combineWith=lettuce;EditorUtility.SetDirty(lettuce);EditorUtility.SetDirty(tomato);
            var prep=AssetDatabase.LoadAssetAtPath<ProcessingRecipe>(data+"PrepLettuce.asset");
            if(prep==null) {prep=ScriptableObject.CreateInstance<ProcessingRecipe>();AssetDatabase.CreateAsset(prep,data+"PrepLettuce.asset");}
            prep.ingredient=lettuce;prep.input=FoodState.Raw;prep.output=FoodState.Cut;prep.duration=1.5f;EditorUtility.SetDirty(prep);
            var salad=AssetDatabase.LoadAssetAtPath<DishRecipe>(data+"TomatoSalad.asset");
            salad.id="garden-salad";salad.displayName="Garden Salad";salad.description="Chop lettuce and tomato, add both to a clean plate on a counter, then pick up and serve. No frying.";
            salad.additionalIngredients=new[]{new IngredientPortion{ingredient=lettuce,state=FoodState.Cut}};
            salad.steps=new[]{AssetDatabase.LoadAssetAtPath<ProcessingRecipe>(data+"PrepTomato.asset"),prep};EditorUtility.SetDirty(salad);
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var hud=Object.FindAnyObjectByType<RestaurantHud>();var layout=hud.GetComponent<KitchenLayout>();
                var sources=Object.FindObjectsByType<SourceStation>(FindObjectsSortMode.None);
                var source=sources.First(s=>!s.plates);var greens=sources.FirstOrDefault(s=>s.ingredient==lettuce);
                if(greens==null) {greens=Object.Instantiate(source);greens.name="LETTUCE";greens.stationName="LETTUCE";greens.ingredient=lettuce;}
                var board=Object.FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None).First(s=>s.recipe.input==FoodState.Raw);
                board.requiresAttendance=true;
                if(!board.appliance.supportedProcesses.Contains(prep)) board.appliance.supportedProcesses=board.appliance.supportedProcesses.Concat(new[]{prep}).ToArray();EditorUtility.SetDirty(board.appliance);
                var counter=Object.FindObjectsByType<CounterStation>(FindObjectsSortMode.None).First(c=>c.stationName=="COUNTER");
                var sink=Object.FindAnyObjectByType<WashingStation>();
                if(sink==null) {var clone=Object.Instantiate(counter);clone.name="SINK";sink=clone.gameObject.AddComponent<WashingStation>();sink.stationName="SINK";sink.slot=clone.slot;Object.DestroyImmediate(clone);}
                var rack=Object.FindAnyObjectByType<DishReturnStation>();
                if(rack==null) {var clone=Object.Instantiate(counter);clone.name="DISH RETURN";rack=clone.gameObject.AddComponent<DishReturnStation>();rack.stationName="DISH RETURN";rack.stack=clone.slot.transform;Object.DestroyImmediate(clone);}
                foreach(var order in Object.FindObjectsByType<CustomerOrder>(FindObjectsSortMode.None)) {order.dishReturn=rack;EditorUtility.SetDirty(order);}
                layout.anchors=layout.anchors.Take(9).Concat(new[]{greens.transform,sink.transform,rack.transform}).ToArray();
                Vector3[][] extra={new[]{P(3.6f,4.6f),P(3,-4.5f),P(3.6f,-2.6f)},new[]{P(3.6f,5),P(3,-4.5f),P(3.6f,-2.5f)},new[]{P(-2.5f,-4.3f),P(1,-4.3f),P(3.6f,-2.5f)}};
                for(int i=0;i<layout.choices.Length;i++) {layout.choices[i].stations=layout.choices[i].stations.Take(9).Concat(extra[i]).ToArray();EditorUtility.SetDirty(layout.choices[i]);}
                layout.Apply(0);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(returnPath);
        }
        private static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    }
}
