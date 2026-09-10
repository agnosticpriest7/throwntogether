using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ThrownTogether.Editor
{
    // Explicit authoring, not runtime regeneration. The approved module GUIDs remain shared with the review scene.
    public static class ApplyKitchenArt
    {
        const string Modules="Assets/VisualDirection";
        const string Output="Assets/Art/Kitchen";
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Modules+"/Materials/VD_"+name+".mat");
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static string PhysicsSignature(Scene scene)=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Select(c=>PathOf(c.transform)+"|"+c.GetType().Name+"|"+EditorJsonUtility.ToJson(c)+"|"+c.transform.position.ToString("F5")+"|"+c.transform.rotation.ToString("F5")+"|"+c.transform.lossyScale.ToString("F5")).OrderBy(s=>s));

        [MenuItem("Thrown Together/Art/Apply approved kitchen direction")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current work and leave Play Mode first.");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            string restore=SceneManager.GetActiveScene().path;
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                if(UnityEngine.Object.FindObjectsByType<StationArt>().Length>0)throw new InvalidOperationException("Art already applied; inspect before revising.");
                string before=PhysicsSignature(scene);
                foreach(var station in UnityEngine.Object.FindObjectsByType<Interactable>())
                {
                    string module=station is WashingStation?"Sink":station is ProcessingStation p?(p.recipe.input==FoodState.Raw?"PrepCounter":"Fryer"):
                        station is SourceStation source && source.plates?"CounterSteel":"CounterWood";
                    var art=station.gameObject.AddComponent<StationArt>();
                    art.visual=Place(module,station.transform,Vector3.zero,Quaternion.identity,Vector3.one);
                    art.visual.name="Approved station art";
                    foreach(string old in new[]{"Cabinet","Worktop","Chopping board","Fryer basket"})
                    {
                        var renderer=station.transform.Find(old)?.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;
                    }
                    // The review sink includes a demonstration plate; production uses the real held dirty plate.
                    if(station is WashingStation) RemoveSinkDemoPlate(art.visual);
                }
                Floor(GameObject.Find("Kitchen floor"),name+"KitchenFloor",1,1,false);
                Floor(GameObject.Find("Dining floor"),name+"DiningFloor",.6f,1.2f,true);
                Walls();
                foreach(var root in scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Customer table")))
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=Mat("Wood");
                var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;
                camera.transform.rotation=Quaternion.Euler(55,0,0);camera.transform.position=new Vector3(0,.3f,1)-camera.transform.forward*20;
                camera.orthographicSize=8.1f;camera.backgroundColor=new Color(.16f,.24f,.27f);
                var light=GameObject.Find("Key light").GetComponent<Light>();light.transform.rotation=Quaternion.Euler(48,-32,0);
                light.color=new Color(1,.94f,.835f);light.intensity=1.25f;light.shadows=LightShadows.Soft;
                light.shadowStrength=.65f;light.shadowBias=.035f;light.shadowNormalBias=.18f;
                RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=new Color(.725f,.792f,.855f);RenderSettings.ambientEquatorColor=new Color(.612f,.678f,.663f);RenderSettings.ambientGroundColor=new Color(.427f,.475f,.486f);RenderSettings.fog=false;
                if(PhysicsSignature(scene)!=before)throw new InvalidOperationException("Physics changed during visual authoring; scene not saved.");
                EditorSceneManager.SaveScene(scene);
                Debug.Log(name+": art applied; all original collider properties and world transforms verified unchanged.");
            }
            EditorSceneManager.OpenScene(restore);
        }
        static GameObject Place(string module,Transform parent,Vector3 position,Quaternion rotation,Vector3 scale)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Modules+"/Prefabs/"+module+".prefab"));
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=rotation;go.transform.localScale=scale;return go;
        }
        static void RemoveSinkDemoPlate(GameObject go)
        {
            // A standalone production mesh omits the disconnected ceramic demonstration plate and center.
            // Extraction is by the authored material, never by a gameplay object or its collider.
            var filter=go.GetComponentInChildren<MeshFilter>();
            string path=Output+"/SinkWithoutDemoPlate.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                string sourcePath=AssetDatabase.GetAssetPath(filter.sharedMesh);
                var importer=(ModelImporter)AssetImporter.GetAtPath(sourcePath);bool readable=importer.isReadable;importer.isReadable=true;importer.SaveAndReimport();
                try
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath).GetComponentInChildren<MeshFilter>().sharedMesh;
                    mesh=UnityEngine.Object.Instantiate(source);mesh.name="Sink without static demonstration plate";
                    var vertices=mesh.vertices;
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var input=mesh.GetTriangles(sub);var kept=new List<int>();
                        for(int i=0;i<input.Length;i+=3)
                        {
                            // Blender-local coordinates: plate centered (-.19,-.08,1.07), radius .34; rim/tap lie outside.
                            bool plate=true;for(int j=0;j<3;j++){var v=vertices[input[i+j]];plate&=Mathf.Abs(v.x)<.6f&&Mathf.Abs(v.y)<.5f&&v.z>1.04f&&v.z<1.11f;}
                            if(!plate){kept.Add(input[i]);kept.Add(input[i+1]);kept.Add(input[i+2]);}
                        }
                        mesh.SetTriangles(kept,sub);
                    }
                    AssetDatabase.CreateAsset(mesh,path);
                }
                finally{importer.isReadable=readable;importer.SaveAndReimport();}
            }
            filter.sharedMesh=mesh;
        }
        static void Floor(GameObject old,string name,float cellX,float cellZ,bool dining)
        {
            string path=Output+"/"+name+".asset";
            Bounds bounds=old.GetComponent<Renderer>().bounds;
            string sourcePath=Modules+"/Models/FloorTile.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(sourcePath);bool readable=importer.isReadable;importer.isReadable=true;importer.SaveAndReimport();
            var pieces=new List<Mesh>();var final=new Mesh{name=name};
            Material[] materials;
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Modules+"/Prefabs/FloorTile.prefab");var filter=prefab.GetComponentInChildren<MeshFilter>();
                materials=prefab.GetComponentInChildren<Renderer>().sharedMaterials;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
                {
                    var combines=new List<CombineInstance>();
                    for(float x=bounds.min.x;x<bounds.max.x-.001f;x+=cellX)for(float z=bounds.min.z;z<bounds.max.z-.001f;z+=cellZ)
                    {
                        float w=Mathf.Min(cellX,bounds.max.x-x),d=Mathf.Min(cellZ,bounds.max.z-z);
                        combines.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=sub,transform=Matrix4x4.TRS(new Vector3(x+w/2,0,z+d/2),Quaternion.identity,new Vector3(w,1,d))*filter.transform.localToWorldMatrix});
                    }
                    var piece=new Mesh{indexFormat=IndexFormat.UInt32};piece.CombineMeshes(combines.ToArray(),true,true);pieces.Add(piece);
                }
                final.indexFormat=IndexFormat.UInt32;final.CombineMeshes(pieces.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,true);
                AssetDatabase.CreateAsset(final,path);
            }
            finally{foreach(var mesh in pieces)UnityEngine.Object.DestroyImmediate(mesh);importer.isReadable=readable;importer.SaveAndReimport();}
            var go=new GameObject(name+" art");go.AddComponent<MeshFilter>().sharedMesh=final;
            var ren=go.AddComponent<MeshRenderer>();ren.sharedMaterials=dining?materials.Select(m=>m.name.Contains("Grout")?Mat("WoodEdge"):Mat("Wood")).ToArray():materials;
            ren.shadowCastingMode=ShadowCastingMode.Off;
            old.GetComponent<Renderer>().enabled=false;
        }
        static void Walls()
        {
            var parent=new GameObject("Modular cutaway walls").transform;
            foreach(string name in new[]{"Back wall","Left wall","Right wall"})
            {
                var old=GameObject.Find(name);var b=old.GetComponent<Renderer>().bounds;bool back=name=="Back wall";
                float length=back?b.size.x:b.size.z;
                for(float offset=0;offset<length-.001f;offset+=1.9f)
                {
                    float size=Mathf.Min(1.9f,length-offset);
                    var position=back?new Vector3(b.min.x+offset+size/2,0,b.center.z):new Vector3(b.center.x,0,b.min.z+offset+size/2);
                    Place("WallShort",parent,position,Quaternion.Euler(0,back?0:name=="Left wall"?90:-90,0),new Vector3(size/1.9f,.48f,1));
                }
                old.GetComponent<Renderer>().enabled=false;
            }
            GameObject.Find("Front boundary").GetComponent<Renderer>().sharedMaterial=Mat("WoodEdge");
        }
    }
}
