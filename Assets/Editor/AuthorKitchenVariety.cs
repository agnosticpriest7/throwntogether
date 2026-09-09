using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThrownTogether.Editor
{
    public static class AuthorKitchenVariety
    {
        private static T Asset<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset==null) {asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}
            EditorUtility.SetDirty(asset); return asset;
        }
        public static void Create()
        {
            if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Author only with a clean, stopped scene.");
            var returnPath=EditorSceneManager.GetActiveScene().path;
            const string data="Assets/Data/VerticalSlice/";
            var tomato=Asset<IngredientDefinition>(data+"Tomato.asset");
            tomato.id="tomato";tomato.displayName="Tomato";tomato.description="Cold salad ingredient: chop and plate; do not fry.";
            tomato.visualKind=IngredientVisualKind.Tomato;tomato.platingState=FoodState.Cut;
            tomato.stateNames=new[]{"Whole tomato","Sliced tomato","Cooked tomato"};
            tomato.stateColors=new[]{new Color(.9f,.10f,.035f),new Color(.95f,.16f,.055f),new Color(.55f,.15f,.045f)};
            var prep=Asset<ProcessingRecipe>(data+"PrepTomato.asset");prep.ingredient=tomato;prep.input=FoodState.Raw;prep.output=FoodState.Cut;prep.duration=1.5f;
            var salad=Asset<DishRecipe>(data+"TomatoSalad.asset");salad.id="tomato-salad";salad.displayName="Tomato Salad";salad.description="Chop tomato, combine with a clean plate on a counter, and serve. No fryer.";salad.ingredient=tomato;salad.requiredState=FoodState.Cut;salad.steps=new[]{prep};
            var mushroom=AssetDatabase.LoadAssetAtPath<IngredientDefinition>(data+"Mushrooms.asset");
            if(mushroom==null) throw new System.InvalidOperationException("Missing existing Mushroom data");
            var layouts=new KitchenLayoutDefinition[3];
            string[] names={"First Service","Prep Island","Split Line"};
            string[] descriptions={"The familiar kitchen, with a tomato crate added. Existing station positions stay the same.","Shared central prep and plating island; supplies across the back, hot food on the right. Walk around the island and share counters.","Ingredients down the left, hot prep across the back, plating below. Trade prepared dishes across the open middle; every station remains accessible solo."};
            Vector3[][] positions={
                new[]{P(-6,4.6f),P(-2.6f,4.6f),P(1.2f,-2.6f),P(-1.2f,1),P(1.2f,4.6f),P(-6,-2.6f),P(-1.2f,-2.6f),P(-6,1),P(4.8f,1)},
                new[]{P(-6,5),P(-2.7f,5),P(.6f,5),P(-2.3f,1),P(1,-2.5f),P(-6,-2.5f),P(.1f,1),P(-2.3f,-1.4f),P(4.8f,1)},
                new[]{P(-6,5),P(-6,1.8f),P(-6,-1.4f),P(-2.5f,4.8f),P(1,4.8f),P(-6,-4),P(-2.5f,-1.7f),P(1,-1.7f),P(4.8f,1)}
            };
            for(int i=0;i<3;i++)
            {
                layouts[i]=Asset<KitchenLayoutDefinition>(data+"Kitchen"+i+".asset");layouts[i].id="kitchen-"+i;layouts[i].displayName=names[i];layouts[i].description=descriptions[i];layouts[i].stations=positions[i];
                layouts[i].playerOneSpawn=i==0 ? new Vector3(-3.84f,.03f,-.56f): i==1 ? new Vector3(-4.3f,.03f,-1.5f):new Vector3(-2.6f,.03f,1);
                layouts[i].playerTwoSpawn=i==0 ? new Vector3(-2.64f,.03f,-4.52f):i==1 ? new Vector3(-4.3f,.03f,-3.6f):new Vector3(.6f,.03f,1);
            }
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var hud=Object.FindAnyObjectByType<RestaurantHud>();
                var sources=Object.FindObjectsByType<SourceStation>(FindObjectsSortMode.None);
                var potato=sources.First(s=>!s.plates && s.ingredient.visualKind==IngredientVisualKind.Potato);
                var mush=sources.FirstOrDefault(s=>!s.plates && s.ingredient==mushroom);
                if(mush==null) {mush=Object.Instantiate(potato);mush.name="MUSHROOMS";mush.stationName="MUSHROOMS";mush.ingredient=mushroom;}
                var tomatoes=sources.FirstOrDefault(s=>!s.plates && s.ingredient==tomato);
                if(tomatoes==null) {tomatoes=Object.Instantiate(potato);tomatoes.name="TOMATOES";tomatoes.stationName="TOMATOES";tomatoes.ingredient=tomato;}
                var processes=Object.FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None);
                var board=processes.First(p=>p.recipe.input==FoodState.Raw);
                if(!board.appliance.supportedProcesses.Contains(prep)) {board.appliance.supportedProcesses=board.appliance.supportedProcesses.Concat(new[]{prep}).ToArray();EditorUtility.SetDirty(board.appliance);}
                foreach(var process in processes)
                {
                    var mushroomStep=AssetDatabase.LoadAssetAtPath<ProcessingRecipe>(data+(process.recipe.input==FoodState.Raw ? "PrepMushrooms.asset":"FryMushrooms.asset"));
                    if(!process.appliance.supportedProcesses.Contains(mushroomStep)) {process.appliance.supportedProcesses=process.appliance.supportedProcesses.Concat(new[]{mushroomStep}).ToArray();EditorUtility.SetDirty(process.appliance);}
                }
                var counters=Object.FindObjectsByType<CounterStation>(FindObjectsSortMode.None).Where(c=>!(c is ProcessingStation)).ToArray();
                var layout=hud.GetComponent<KitchenLayout>() ?? hud.gameObject.AddComponent<KitchenLayout>();
                layout.choices=layouts;layout.chef=hud.chef;layout.secondSpawn=hud.coop.playerTwoSpawn;layout.tomatoSalad=salad;
                layout.anchors=new[]{potato.transform,mush.transform,tomatoes.transform,board.transform,processes.First(p=>p.recipe.input==FoodState.Cut).transform,sources.First(s=>s.plates).transform,counters.First(c=>c.stationName=="COUNTER").transform,counters.First(c=>c.stationName=="SPARE COUNTER").transform,Object.FindAnyObjectByType<ServiceStation>().transform};
                layout.Apply(0);
                if(hud.shift!=null && !hud.shift.definition.orders.Contains(salad))
                {
                    var original=hud.shift.definition.orders.Distinct().ToArray();
                    hud.shift.definition.orders=new[]{original[0],original[1],salad,original[1],salad,original[0]};EditorUtility.SetDirty(hud.shift.definition);
                }
                EditorUtility.SetDirty(layout);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(returnPath);
        }
        private static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    }
}

