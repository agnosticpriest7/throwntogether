using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    public static class AuthorTrashAndStaff
    {
        const string Output="Assets/Prefabs/VerticalSlice/Trash.prefab";
        static void Part(Transform parent,string name,PrimitiveType shape,Vector3 p,Vector3 scale,string material)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(material);
            if(go.GetComponent<Renderer>().sharedMaterial==null)throw new Exception("Missing bin material "+material);
        }
        public static void Run()
        {
            if(!Application.isBatchMode || File.Exists(Output))throw new Exception("Use a fresh isolated candidate for authoring");
            var root=new GameObject("Trash bin");root.AddComponent<TrashStation>().stationName="Trash bin";
            var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.53f,0);collider.size=new Vector3(.82f,1.06f,.82f);
            var visual=new GameObject("Bin visual");visual.transform.SetParent(root.transform,false);root.AddComponent<StationArt>().visual=visual;
            const string dark="Assets/Art/RestaurantRoom/Materials/RM_Dark.mat";
            const string steel="Assets/Art/Kitchen/MaterialsV2/KT_Steel.mat";
            Part(visual.transform,"Body",PrimitiveType.Cylinder,new Vector3(0,.46f,0),new Vector3(.76f,.46f,.76f),steel);
            Part(visual.transform,"Rim",PrimitiveType.Cylinder,new Vector3(0,.94f,0),new Vector3(.85f,.055f,.85f),steel);
            Part(visual.transform,"Open interior",PrimitiveType.Cylinder,new Vector3(0,1,0),new Vector3(.68f,.012f,.68f),dark);
            Part(visual.transform,"Rear lid",PrimitiveType.Cube,new Vector3(0,1.13f,.37f),new Vector3(.76f,.28f,.07f),steel);
            Part(visual.transform,"Pedal",PrimitiveType.Cube,new Vector3(0,.07f,-.47f),new Vector3(.26f,.07f,.16f),dark);
            for(int i=-1;i<=1;i++)Part(visual.transform,"Front rib",PrimitiveType.Cube,new Vector3(i*.18f,.48f,-.355f),new Vector3(.035f,.59f,.035f),dark);
            PrefabUtility.SaveAsPrefabAsset(root,Output);UnityEngine.Object.DestroyImmediate(root);
            var day=AssetDatabase.LoadAssetAtPath<DayServiceDefinition>("Assets/Resources/ServiceDay.asset");day.wasteCost=1;day.serverIdle=new Vector3(10.2f,0,-5.1f);day.busserIdle=new Vector3(11.5f,0,-5.1f);EditorUtility.SetDirty(day);
            foreach(var name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var bin=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output));bin.transform.position=new Vector3(2.45f,0,-4.9f);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Capture();
        }
        public static void Capture()
        {
            ApplyCameraTrial.Capture();var day=AssetDatabase.LoadAssetAtPath<DayServiceDefinition>("Assets/Resources/ServiceDay.asset");
            for(int i=0;i<2;i++){var visual=UnityEngine.Object.Instantiate(day.walkingVisual);visual.transform.position=i==0?day.serverIdle:day.busserIdle;visual.GetComponent<ChefAppearance>().Apply(ChefAppearanceData.Example(i==0?2:1));}
            var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;var target=RenderTexture.GetTemporary(1600,1000,24);var active=RenderTexture.active;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("Builds/CameraTrialReview/trash-and-idle-staff.png",image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
