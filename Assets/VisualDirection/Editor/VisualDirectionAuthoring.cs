using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using ThrownTogether;

// Explicit, isolated review authoring. No automatic import/startup behavior.
public static class VisualDirectionAuthoring
{
    const string Root="Assets/VisualDirection";
    public const string ScenePath=Root+"/VisualDirectionTest.unity";
    static readonly string[] Modules={"CounterWood","CounterSteel","PrepCounter","Fryer","Sink","FloorTile","WallShort"};
    static readonly string[] MaterialNames={"Steel","Cabinet","Dark","Wood","WoodEdge","Ceramic","Water","Oil","Heat","Tile","Grout","Wall"};
    static readonly string[] Colors={"A7BFC7","648894","30434B","D6A468","A96D3D","F1E8D5","58AEBB","655038","EEA54B","D3C6AC","8C918A","E5DCC7"};
    static Color Hex(string text) {ColorUtility.TryParseHtmlString("#"+text,out var color);return color;}

    [MenuItem("Thrown Together/Visual direction/Create isolated prototype")]
    public static void Create()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop Play and preserve unsaved scene work before authoring.");
        if(File.Exists(ScenePath)) throw new InvalidOperationException("Prototype exists. Inspect it rather than overwriting it.");
        Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Prefabs");
        AssetDatabase.Refresh();
        for(int i=0;i<MaterialNames.Length;i++)
        {
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="VD_"+MaterialNames[i];mat.color=Hex(Colors[i]);
            mat.SetFloat("_Smoothness",MaterialNames[i]=="Steel" ? .32f:.12f);
            mat.SetFloat("_Metallic",0);mat.enableInstancing=true;
            AssetDatabase.CreateAsset(mat,Root+"/Materials/"+mat.name+".mat");
        }
        foreach(string module in Modules)
        {
            string path=Root+"/Models/"+module+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable=false;importer.SaveAndReimport();
            var instance=new GameObject(module);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            model.transform.SetParent(instance.transform,false); // Keep Blender's imported axis conversion below a Y-up placement root.
            model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation; // Frontage toward Unity -Z.
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+m.name+".mat") ?? throw new Exception("Unknown material "+m.name)).ToArray();
            PrefabUtility.SaveAsPrefabAsset(instance,Root+"/Prefabs/"+module+".prefab");UnityEngine.Object.DestroyImmediate(instance);
        }
        // Original scene was clean; open the new scene only after import work is complete.
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        scene.name="VisualDirectionTest";
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=Hex("B9CADA");RenderSettings.ambientEquatorColor=Hex("9CADA9");RenderSettings.ambientGroundColor=Hex("6D797C");
        RenderSettings.ambientIntensity=1;RenderSettings.fog=false;
        var floor=new GameObject("Modular floor - one unit tiles");
        for(int x=0;x<12;x++)for(int z=0;z<9;z++)
            Place("FloorTile",new Vector3(x-5.5f,0,z-4f),floor.transform);
        var shell=new GameObject("Short cutaway wall samples");
        for(int i=0;i<4;i++)Place("WallShort",new Vector3(-3.8f+i*1.9f,0,4.15f),shell.transform);
        Place("WallShort",new Vector3(-5.25f,0,3.25f),shell.transform,90);
        var stations=new GameObject("Reusable module samples - original worktop dimensions");
        Place("PrepCounter",new Vector3(-3.8f,0,2.6f),stations.transform);
        Place("Fryer",new Vector3(-1.9f,0,2.6f),stations.transform);
        Place("CounterWood",new Vector3(0,0,2.6f),stations.transform);
        Place("Sink",new Vector3(1.9f,0,2.6f),stations.transform);
        Place("CounterWood",new Vector3(-.95f,0,-1.9f),stations.transform);
        Place("CounterSteel",new Vector3(.95f,0,-1.9f),stations.transform);
        var light=new GameObject("Soft key light").AddComponent<Light>();light.type=LightType.Directional;
        light.transform.rotation=Quaternion.Euler(48,-32,0);light.color=Hex("FFF0D5");light.intensity=1.25f;
        light.shadows=LightShadows.Soft;light.shadowStrength=.65f;light.shadowBias=.035f;light.shadowNormalBias=.18f;
        RenderSettings.sun=light;
        var camera=new GameObject("Gameplay camera - 55 degree orthographic").AddComponent<Camera>();
        camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=6.5f;
        camera.transform.rotation=Quaternion.Euler(55,0,0);
        camera.transform.position=new Vector3(-.25f,.45f,0)-camera.transform.forward*20;
        camera.nearClipPlane=.1f;camera.farClipPlane=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Hex("293D45");
        camera.allowHDR=false;camera.allowMSAA=true;camera.gameObject.AddComponent<AudioListener>();
        MakeChef("P1 scale reference - plated fries",new Vector3(-2.05f,0,-.05f),205,0,true);
        MakeChef("P2 scale reference - raw tomato",new Vector3(1.25f,0,.10f),150,1,false);
        EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("VisualDirectionTest created separately. Two posed chef references; no new gameplay or build-list changes.");
    }
    static GameObject Place(string module,Vector3 position,Transform parent,float yaw=0)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+module+".prefab"));
        go.transform.SetParent(parent);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);return go;
    }
    static void MakeChef(string name,Vector3 position,float yaw,int example,bool fries)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VerticalSlice/Chef.prefab"));
        go.name=name;go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var chef=go.GetComponent<ChefController>();
        foreach(var behaviour in go.GetComponents<MonoBehaviour>()) behaviour.enabled=false;
        foreach(var collider in go.GetComponentsInChildren<Collider>())collider.enabled=false;
        var appearance=go.GetComponentInChildren<ChefAppearance>();appearance.usePlayerSelection=false;appearance.Apply(ChefAppearanceData.Example(example));appearance.Pose(1,0,1);appearance.enabled=false;
        // Property blocks are not serialized: save scene-only material copies for stable Edit Mode review.
        foreach(var renderer in appearance.GetComponentsInChildren<Renderer>(true))
        {
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            if(!block.isEmpty)
            {
                var mat=new Material(renderer.sharedMaterial);mat.color=block.GetColor("_BaseColor");
                string path=Root+"/Materials/Chef"+example+"_"+renderer.name+".mat";
                AssetDatabase.CreateAsset(mat,path);renderer.sharedMaterial=mat;renderer.SetPropertyBlock(null);
            }
        }
        var arrow=go.transform.Find("Ground facing arrow");if(arrow!=null)arrow.gameObject.SetActive(false);
        MakeItem(chef,example,fries);
    }
    public static void RefreshCarriedProps()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open the isolated prototype first.");
        foreach(var chef in UnityEngine.Object.FindObjectsByType<ChefController>())
        {
            foreach(Transform child in chef.Hands.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            bool fries=chef.name.StartsWith("P1");MakeItem(chef,fries?0:1,fries);
        }
    }
    static void MakeItem(ChefController chef,int example,bool fries)
    {
        var item=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VerticalSlice/Carryable.prefab"));
        var carryable=item.GetComponent<Carryable>();
        var ingredient=AssetDatabase.FindAssets("t:IngredientDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<IngredientDefinition>(AssetDatabase.GUIDToAssetPath(g))).First(i=>i.name==(fries?"Potato":"Tomato"));
        carryable.Configure(new ItemPayload {ingredient=ingredient,state=fries?FoodState.Cooked:FoodState.Raw,isPlate=fries});
        item.transform.SetParent(chef.Hands.transform,false);item.name=fries?"Existing plated fries visual":"Existing raw tomato visual";
        foreach(var r in item.GetComponentsInChildren<Renderer>())
        {
            var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);
            Color color=block.GetColor("_BaseColor");
            string path=Root+"/Materials/Carried"+example+"_"+ColorUtility.ToHtmlStringRGBA(color)+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null)
            {
                mat=new Material(r.sharedMaterial);mat.color=color;AssetDatabase.CreateAsset(mat,path);
            }
            r.sharedMaterial=mat;r.SetPropertyBlock(null);
        }
        // Static review props retain generated geometry without entering the cooking system.
        UnityEngine.Object.DestroyImmediate(carryable);
    }

    [MenuItem("Thrown Together/Visual direction/Capture three review views")]
    public static void Capture()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open VisualDirectionTest first.");
        string folder="Builds/VisualDirectionReview";Directory.CreateDirectory(folder);
        var camera=Camera.main;float size=camera.orthographicSize;Vector3 position=camera.transform.position;
        try
        {
            CaptureOne(camera,folder+"/01-gameplay.png");
            camera.orthographicSize=3.6f;camera.transform.position=new Vector3(-.65f,.65f,1.3f)-camera.transform.forward*20;
            CaptureOne(camera,folder+"/02-close.png");
            camera.orthographicSize=9.75f;camera.transform.position=position;
            CaptureOne(camera,folder+"/03-expansion-distance.png");
        }
        finally{camera.orthographicSize=size;camera.transform.position=position;}
    }
    static void CaptureOne(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;
        var previous=RenderTexture.active;var target=camera.targetTexture;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{camera.targetTexture=target;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        Debug.Log("Review capture: "+Path.GetFullPath(path));
    }
}
