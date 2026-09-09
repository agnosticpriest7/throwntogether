using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThrownTogether.Editor
{
    public static class AuthorCharacters
    {
        [MenuItem("Thrown Together/Characters/Import modular prototype")]
        public static void Import()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Leave Play Mode before authoring characters.");
            const string modelPath="Assets/Art/Characters/ModularChef.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.animationType=ModelImporterAnimationType.Generic; importer.importAnimation=false;
            importer.optimizeGameObjects=false; importer.isReadable=false; importer.SaveAndReimport();
            Directory.CreateDirectory("Assets/Materials/Characters"); Directory.CreateDirectory("Assets/Resources");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root=new GameObject("ChefVisual");
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(source); model.transform.SetParent(root.transform,false);
                var animator=model.GetComponent<Animator>(); if(animator!=null) Object.DestroyImmediate(animator);
                foreach(var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    var original=r.sharedMaterial; string path="Assets/Materials/Characters/"+original.name+".mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(mat==null)
                    {
                        bool ink=original.name=="C_Ink" || original.name=="C_Tooth";
                        mat=new Material(Shader.Find(ink ? "Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));
                        mat.color=original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor"):original.color;
                        if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",.15f);
                        if(ink) mat.SetFloat("_Cull",0);
                        AssetDatabase.CreateAsset(mat,path);
                    }
                    r.sharedMaterial=mat;
                    if(r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen=true;
                }
                var appearance=root.AddComponent<ChefAppearance>(); appearance.Apply(ChefAppearanceData.Example(0));
                var visual=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/ChefVisual.prefab");
                const string chefPath="Assets/Prefabs/VerticalSlice/Chef.prefab";
                var chef=PrefabUtility.LoadPrefabContents(chefPath);
                try
                {
                    foreach(string name in new[]{"Apron","Head","Chef hat","Facing marker","ChefVisual"})
                    {var child=chef.transform.Find(name);if(child!=null) Object.DestroyImmediate(child.gameObject);}
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(visual); instance.transform.SetParent(chef.transform,false);
                    PrefabUtility.SaveAsPrefabAsset(chef,chefPath);
                }
                finally {PrefabUtility.UnloadPrefabContents(chef);}
            }
            finally {Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
            Debug.Log("Imported modular chef prototype: shared rig, 3 builds, independent cosmetics. Existing controller and carry slot preserved.");
        }

        [MenuItem("Thrown Together/Characters/Create review scene")]
        public static void CreateReviewScene()
        {
            const string path="Assets/Scenes/CharacterReview.unity";
            if(File.Exists(path)) throw new System.InvalidOperationException("Review scene already exists; edit it directly.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save current scene first.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ChefVisual.prefab");
            for(int build=0;build<3;build++) for(int look=0;look<4;look++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name=ChefWardrobe.Builds[build]+" - "+ChefWardrobe.Colors[look];
                go.transform.position=new Vector3((look-1.5f)*1.7f,0,(build-1)*2.2f); go.transform.rotation=Quaternion.Euler(0,165,0);
                var a=go.GetComponent<ChefAppearance>(); a.usePlayerSelection=false;
                var data=ChefAppearanceData.Example(look); data.build=build; a.Apply(data);
            }
            var camera=new GameObject("Review Camera").AddComponent<Camera>(); camera.transform.position=new Vector3(0,9,-10);camera.transform.LookAt(Vector3.up*.6f);
            camera.orthographic=true;camera.orthographicSize=4.9f;camera.backgroundColor=new Color(.16f,.20f,.22f);camera.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.transform.rotation=Quaternion.Euler(45,-30,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.7f,.7f,.7f);
            EditorSceneManager.SaveScene(scene,path);
        }
    }
}
