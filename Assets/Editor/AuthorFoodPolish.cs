using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    public static class AuthorFoodPolish
    {
        const string MeshPath="Assets/Art/Kitchen/PlateProfile.asset";
        public static void ApplyAndCapture()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Run in isolated batch project to preserve the live Editor.");
            if(File.Exists(MeshPath))throw new InvalidOperationException("Plate already authored; inspect before regenerating.");
            // Revolved closed ceramic profile: underside, beveled outer lip, shallow well.
            var profile=new[]{new Vector2(0,0),new Vector2(.30f,0),new Vector2(.405f,.045f),new Vector2(.41f,.06f),new Vector2(.385f,.075f),new Vector2(.305f,.035f),new Vector2(0,.035f)};
            const int sides=32;var vertices=new List<Vector3>();var triangles=new List<int>();
            foreach(var p in profile)for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;vertices.Add(new Vector3(p.x*Mathf.Cos(a),p.y,p.x*Mathf.Sin(a)));}
            for(int i=0;i<profile.Length-1;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=(i+1)*sides+j,c=i*sides+(j+1)%sides,d=(i+1)*sides+(j+1)%sides;triangles.AddRange(new[]{a,b,c,c,b,d});}
            var mesh=new Mesh{name="Shallow ceramic plate"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,MeshPath);
            const string prefabPath="Assets/Prefabs/VerticalSlice/Carryable.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try{prefab.GetComponent<Carryable>().plateMesh=mesh;PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            AssetDatabase.SaveAssets();CreateMushroomCap();Capture();
        }
        public static void CreateMushroomCap()
        {
            const string path="Assets/Art/Kitchen/MushroomSliceCap.asset";
            if(File.Exists(path))throw new InvalidOperationException("Mushroom cap already authored.");
            const int steps=16,n=18;var v=new List<Vector3>();var t=new List<int>();
            for(int layer=0;layer<2;layer++)
            {
                float y=layer-.5f;v.Add(new Vector3(0,y,0));
                for(int j=0;j<=steps;j++){float a=j*Mathf.PI/steps;v.Add(new Vector3(Mathf.Cos(a)*.5f,y,Mathf.Sin(a)*.5f));}
            }
            for(int j=1;j<=steps;j++){t.AddRange(new[]{0,j,j+1,n,n+j+1,n+j});}
            for(int j=1;j<=steps+1;j++){int next=j==steps+1?1:j+1;t.AddRange(new[]{j,n+j,next,next,n+j,n+next});}
            var mesh=new Mesh{name="Mushroom semicircle slice cap"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            const string prefabPath="Assets/Prefabs/VerticalSlice/Carryable.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try{prefab.GetComponent<Carryable>().mushroomCapMesh=mesh;PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}AssetDatabase.SaveAssets();
        }
        public static void AddCapAndCapture(){CreateMushroomCap();Capture();}
        public static void Capture()
        {
            AuthorFirstServiceFloor.Capture();
            var hud=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>();var camera=hud.gameplayCamera;camera.orthographicSize=8.1f;camera.transform.position=new Vector3(0,.3f,1)-camera.transform.forward*20;
            var chef=UnityEngine.Object.FindAnyObjectByType<ChefController>();
            var ingredients=AssetDatabase.FindAssets("t:IngredientDefinition").Select(id=>AssetDatabase.LoadAssetAtPath<IngredientDefinition>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
            var itemPrefab=AssetDatabase.LoadAssetAtPath<Carryable>("Assets/Prefabs/VerticalSlice/Carryable.prefab");
            var potato=ingredients.First(i=>i.visualKind==IngredientVisualKind.Potato);var lettuce=ingredients.First(i=>i.visualKind==IngredientVisualKind.Lettuce);var tomato=ingredients.First(i=>i.visualKind==IngredientVisualKind.Tomato);
            var fries=new ItemPayload{isPlate=true,ingredient=potato,state=FoodState.Cooked};var salad=new ItemPayload{isPlate=true,ingredient=lettuce,state=FoodState.Cut};salad.additions.Add(new IngredientPortion{ingredient=tomato,state=FoodState.Cut});
            chef.transform.position=new Vector3(-3.4f,.03f,-2.8f);chef.transform.rotation=Quaternion.Euler(0,180,0);Give(chef,fries,itemPrefab,0);
            // Instantiate a clean chef prefab for P2 to avoid cloning the held item/slot state.
            var two=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ChefController>("Assets/Prefabs/VerticalSlice/Chef.prefab"));two.transform.position=new Vector3(-3.4f,.03f,.1f);two.transform.rotation=Quaternion.identity;Give(two,salad,itemPrefab,2);
            Directory.CreateDirectory("Builds/FoodReview");Render(camera,"gameplay");
            camera.orthographicSize=3;camera.transform.position=new Vector3(-3.4f,.6f,-1.3f)-camera.transform.forward*20;Render(camera,"carried-close");
            var mushroom=ingredients.First(i=>i.visualKind==IngredientVisualKind.Mushroom);
            var sample=UnityEngine.Object.Instantiate(itemPrefab);sample.Configure(new ItemPayload{isPlate=true,ingredient=mushroom,state=FoodState.Cooked});sample.transform.position=new Vector3(-.8f,1.30f,.55f);
            camera.orthographicSize=1.35f;camera.transform.position=new Vector3(-.8f,1,.55f)-camera.transform.forward*20;Render(camera,"mushrooms-counter");
            Debug.Log("Food polish authored and reviewed with two temporary chefs; no scene saved.");
        }
        static void Give(ChefController chef,ItemPayload payload,Carryable prefab,int look)
        {
            var item=UnityEngine.Object.Instantiate(prefab);item.Configure(payload);chef.Hands.TryTake(item);
            var appearance=chef.GetComponentInChildren<ChefAppearance>();appearance.Apply(ChefAppearanceData.Example(look));appearance.Pose(1,0,1);
        }
        static void Render(Camera camera,string name)
        {
            var target=RenderTexture.GetTemporary(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Builds/FoodReview/"+name+".png",texture.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);RenderTexture.ReleaseTemporary(target);}
        }
    }
}
