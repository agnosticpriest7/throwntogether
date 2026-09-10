using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThrownTogether.Editor
{
    public static class AuthorKitchenRefinement
    {
        const string Root="Assets/Art/Kitchen";
        static readonly string[] Names={"Steel","Highlight","Body","Dark","Wood","WoodLight","WoodDark","Water","Oil","Red","Amber","Potato","Spot","Tomato","TomatoLight","Leaf","LeafLight","Cream","Mushroom"};
        static readonly string[] Colors={"AABEC3","D7E1DD","67848A","344A50","C9985E","D8AF75","99693E","5CA6B2","59442C","BE513E","E8B652","B78B4E","85603C","DA4634","EF6743","598E3D","86B850","DDD0B0","AC8C6B"};
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static string PhysicsSignature(Scene s)=>string.Join("\n",s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Select(c=>PathOf(c.transform)+"|"+EditorJsonUtility.ToJson(c)+"|"+c.transform.position.ToString("F5")+"|"+c.transform.rotation.ToString("F5")+"|"+c.transform.lossyScale.ToString("F5")).OrderBy(x=>x));
        [MenuItem("Thrown Together/Art/Apply refined kitchen modules")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Preserve unsaved work and leave Play Mode first.");
            if(File.Exists(Root+"/PrefabsV2/CounterStraight.prefab"))throw new InvalidOperationException("Refined kit already exists. Review changes instead of regenerating over it.");
            var restore=SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(Root+"/MaterialsV2");Directory.CreateDirectory(Root+"/PrefabsV2");AssetDatabase.Refresh();
            for(int i=0;i<Names.Length;i++)
            {
                var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="KT_"+Names[i],enableInstancing=true};
                ColorUtility.TryParseHtmlString("#"+Colors[i],out var color);m.SetColor("_BaseColor",color);
                m.SetFloat("_Smoothness",Names[i]=="Steel" || Names[i]=="Highlight"?.28f:.12f);m.SetFloat("_Metallic",0);
                AssetDatabase.CreateAsset(m,Root+"/MaterialsV2/"+m.name+".mat");
            }
            foreach(string path in Directory.GetFiles(Root+"/ModelsV2","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                string name=Path.GetFileNameWithoutExtension(path);var root=new GameObject(name);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                model.transform.SetParent(root.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/MaterialsV2/"+m.name+".mat") ?? throw new Exception("Unknown material "+m.name)).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/PrefabsV2/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
            }
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");string before=PhysicsSignature(scene);
                foreach(var station in UnityEngine.Object.FindObjectsByType<Interactable>())
                {
                    var art=station.GetComponent<StationArt>();if(art==null || art.visual==null)throw new Exception("Missing station art "+station.name);
                    string module="CounterEnd";
                    if(station is SourceStation source)module=source.plates?"CounterSteel":"Pantry"+source.ingredient.visualKind;
                    else if(station is ProcessingStation process)module=process.requiresAttendance?"PrepCounter":"Fryer";
                    else if(station is WashingStation)module="Sink";
                    else if(station is CounterStation)module="CounterStraight";
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/PrefabsV2/"+module+".prefab");if(prefab==null)throw new Exception("Missing module "+module);
                    UnityEngine.Object.DestroyImmediate(art.visual);
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);visual.transform.SetParent(station.transform,false);visual.name="Refined station art";
                    art.visual=visual;art.includesPantryDisplay=station is SourceStation pantry && !pantry.plates;
                    if(art.includesPantryDisplay){var old=station.transform.Find("Potato crate")?.GetComponent<Renderer>();if(old!=null)old.enabled=false;}
                }
                if(PhysicsSignature(scene)!=before)throw new Exception("Collider or gameplay transform changed; refusing scene save.");
                EditorSceneManager.SaveScene(scene);Debug.Log(name+": refined art; all original collider properties/world transforms unchanged.");
            }
            AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);
        }
        public static void ApplyAndCapture()
        {
            Apply();AuthorFirstServiceFloor.Capture();Directory.CreateDirectory("Builds/PropReview");
            foreach(string file in Directory.GetFiles("Builds/FloorReview","*.png"))File.Copy(file,"Builds/PropReview/"+Path.GetFileName(file),true);
        }
    }
}
