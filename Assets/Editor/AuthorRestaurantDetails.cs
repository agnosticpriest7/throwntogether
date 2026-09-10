using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ThrownTogether.Editor
{
    public static class AuthorRestaurantDetails
    {
        const string Root="Assets/Art/RestaurantDetails";
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static string Signature(Scene s)=>string.Join("\n",s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Select(c=>PathOf(c.transform)+EditorJsonUtility.ToJson(c)+c.transform.position.ToString("F5")+c.transform.rotation.ToString("F5")+c.transform.lossyScale.ToString("F5")).Concat(s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarrySlot>(true)).Select(c=>PathOf(c.transform)+c.transform.position.ToString("F5"))).Concat(s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Select(c=>EditorJsonUtility.ToJson(c)+c.transform.position.ToString("F5")+c.transform.rotation.ToString("F5"))).OrderBy(x=>x));
        static Material MaterialFor(string name)
        {
            string folder=name.StartsWith("RM_")?"Assets/Art/RestaurantRoom/Materials":name.StartsWith("KT_")?"Assets/Art/Kitchen/MaterialsV2":Root+"/Materials";
            return AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat")??throw new Exception("Unknown material "+name);
        }
        static GameObject Place(string module,Transform parent,Vector3 position,float yaw=0,float scale=1)
        {
            string folder=module=="WallSconce"?"Assets/Art/RestaurantRoom":Root;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Prefabs/"+module+".prefab"));
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one*scale;return go;
        }
        public static void ApplyAndCapture()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch authoring to preserve live Editor work.");
            if(File.Exists(Root+"/Prefabs/PottedPlant.prefab"))throw new InvalidOperationException("Details already authored; inspect instead of overwriting.");
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
            string[] names={"Stone","StoneLight","Pot"},colors={"87979B","A6AFAD","956950"};
            for(int i=0;i<names.Length;i++)
            {
                var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="DT_"+names[i],enableInstancing=true};ColorUtility.TryParseHtmlString("#"+colors[i],out var color);m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(m,Root+"/Materials/"+m.name+".mat");
            }
            foreach(string path in Directory.GetFiles(Root+"/Models","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                string name=Path.GetFileNameWithoutExtension(path);var wrapper=new GameObject(name);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(wrapper.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
                foreach(var renderer in wrapper.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>MaterialFor(m.name)).ToArray();
                PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(wrapper);
            }
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");string before=Signature(scene);
                if(GameObject.Find("Restaurant finishing details")!=null)throw new Exception("Scene details already exist");
                var root=new GameObject("Restaurant finishing details").transform;
                var decor=new GameObject("Perimeter decoration").transform;decor.SetParent(root,false);
                Place("PottedPlant",decor,new Vector3(-8.05f,1.37f,7.6f),0,.65f);
                Place("PottedPlant",decor,new Vector3(3.8f,1.37f,7.6f),0,.65f);
                Place("KitchenSign",decor,new Vector3(-1.75f,.97f,7.43f));
                Place("DiningSign",decor,new Vector3(7.5f,.97f,7.43f));
                foreach(float x in new[]{-3.1f,-.3f})Place("WallSconce",decor,new Vector3(x,.16f,-5.78f),0,.8f);
                var exterior=new GameObject("Exterior framing").transform;exterior.SetParent(root,false);
                for(int i=0;i<9;i++)Place("SidewalkTile",exterior,new Vector3(-7.6f+i*1.9f,0,-6.55f));
                Place("EntranceMat",exterior,new Vector3(-1.7f,.02f,-6.5f));
                Place("EntranceAwning",exterior,new Vector3(-1.7f,.7f,-6.3f)).transform.localScale=new Vector3(1,1,.65f);
                foreach(float x in new[]{-7.3f,6.9f})Place("LowPlanter",exterior,new Vector3(x,.035f,-6.6f));
                Place("PottedPlant",exterior,new Vector3(-3.6f,.035f,-6.2f),0,.8f);
                if(Signature(scene)!=before)throw new Exception("Original physics, slots or camera changed; not saved");
                if(root.GetComponentsInChildren<Collider>().Length>0||root.GetComponentsInChildren<Light>().Length>0)throw new Exception("Decor must add neither physics nor real-time lights");
                EditorSceneManager.SaveScene(scene);Debug.Log(name+": details saved; original collider/slot/camera signatures unchanged.");
            }
            AssetDatabase.SaveAssets();Capture();
        }
        public static void Capture()
        {
            AuthorCustomerPresentation.Capture();
            Directory.CreateDirectory("Builds/DetailsReview");File.Copy("Builds/CustomerReview/gameplay.png","Builds/DetailsReview/gameplay.png",true);
            var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;
            camera.orthographicSize=2.2f;camera.transform.position=new Vector3(-1.75f,1,7.2f)-camera.transform.forward*20;Render(camera,"wall-close");
            camera.orthographicSize=3.3f;camera.transform.position=new Vector3(-1.7f,.2f,-5.9f)-camera.transform.forward*20;Render(camera,"entrance-close");
            camera.orthographicSize=10.5f;camera.transform.position=new Vector3(0,0,.5f)-camera.transform.forward*20;Render(camera,"wide");
        }
        static void Render(Camera camera,string name)
        {
            var target=RenderTexture.GetTemporary(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);var old=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes("Builds/DetailsReview/"+name+".png",texture.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);RenderTexture.ReleaseTemporary(target);}
        }
    }
}


