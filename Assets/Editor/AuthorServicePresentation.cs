using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ThrownTogether.Editor
{
    public static class AuthorServicePresentation
    {
        const string Root="Assets/Art/ServicePass";
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static string Signature(Scene scene)=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Select(c=>PathOf(c.transform)+EditorJsonUtility.ToJson(c)+c.transform.position.ToString("F5")+c.transform.rotation.ToString("F5")+c.transform.lossyScale.ToString("F5")).Concat(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarrySlot>(true)).Select(c=>PathOf(c.transform)+c.transform.position.ToString("F5"))).OrderBy(x=>x));
        [MenuItem("Thrown Together/Art/Apply service presentation")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Preserve unsaved work and leave Play Mode first.");
            if(File.Exists(Root+"/Prefabs/ServicePass.prefab"))throw new InvalidOperationException("Service kit already authored; inspect before editing.");
            string restore=SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Root+"/Models","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                string name=Path.GetFileNameWithoutExtension(path);var wrapper=new GameObject(name);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(wrapper.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
                foreach(var renderer in wrapper.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Kitchen/MaterialsV2/"+m.name+".mat")??throw new Exception("Unknown shared material "+m.name)).ToArray();
                PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(wrapper);
            }
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");string before=Signature(scene);
                foreach(var station in UnityEngine.Object.FindObjectsByType<Interactable>().Where(s=>s is ServiceStation||s is DishReturnStation))
                {
                    var art=station.GetComponent<StationArt>();if(art==null||art.visual==null)throw new Exception("Missing authored counter");
                    var overlay=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+(station is ServiceStation?"ServicePass":"DishReturnRack")+".prefab"));
                    overlay.transform.SetParent(station.transform,false);overlay.name="Service presentation art";art.includesServiceDisplay=true;EditorUtility.SetDirty(art);
                    Debug.Log(station.name+": visual only; slot "+(station is ServiceStation service?service.pickupSlot.transform.position:((DishReturnStation)station).stack.position));
                }
                if(Signature(scene)!=before)throw new Exception("Original physics/slots changed; scene not saved.");
                EditorSceneManager.SaveScene(scene);Debug.Log(name+": service art saved with original physics and carry slots unchanged.");
            }
            AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);
        }
        public static void ApplyAndCapture()
        {
            Apply();AuthorFirstServiceFloor.Capture();Directory.CreateDirectory("Builds/ServiceReview");File.Copy("Builds/FloorReview/gameplay.png","Builds/ServiceReview/gameplay.png",true);
            var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;camera.orthographicSize=2.8f;camera.transform.position=new Vector3(3.3f,.6f,1)-camera.transform.forward*20;
            var target=RenderTexture.GetTemporary(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Builds/ServiceReview/closer.png",texture.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);RenderTexture.ReleaseTemporary(target);}
        }
    }
}
