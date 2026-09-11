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
    // Explicit asset authoring only. Floors share the existing collision footprint;
    // changing a layout continues to move the original station roots and carry slots.
    public static class AuthorFirstServiceFloor
    {
        const string Output="Assets/Art/Kitchen";
        static readonly List<Vector3> vertices=new List<Vector3>();
        static readonly List<int>[] triangles=Enumerable.Range(0,7).Select(_=>new List<int>()).ToArray();

        [MenuItem("Thrown Together/Art/Author warm floors and First Service layout")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Leave Play Mode and save existing scene work before authoring.");
            string restore=SceneManager.GetActiveScene().path;
            var layout=AssetDatabase.LoadAssetAtPath<KitchenLayoutDefinition>("Assets/Data/VerticalSlice/Kitchen0.asset");
            layout.description="Corner pantry, left-side prep and washing, central assembly island and a clear serving edge. An open circuit for one or two chefs.";
            // Potato, mushroom, tomato, prep, fryer, plates, counter, spare, service, lettuce, sink, return.
            layout.stations=new[]{ P(-6.4f,5.8f),P(-4.4f,5.8f),P(-6.4f,4.1f),P(-6.4f,.65f),P(.8f,5.8f),P(-.8f,2.2f),P(-.8f,-1.1f),P(-.8f,.55f),P(3.2f,1),P(-4.4f,4.1f),P(-6.4f,-1.9f),P(3.2f,-1) };
            layout.playerOneSpawn=new Vector3(-3.4f,.03f,-3);
            layout.playerTwoSpawn=new Vector3(-.5f,.03f,-3.5f);
            EditorUtility.SetDirty(layout);
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var kitchen=UnityEngine.Object.FindAnyObjectByType<KitchenLayout>();
                kitchen.Apply(0);
                Floor(name,false);Floor(name,true);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);
            Debug.Log("Warm floors authored; First Service rearranged at original scale. Other layout definitions unchanged.");
        }
        static Vector3 P(float x,float z)=>new Vector3(x,0,z);
        static Material Material(string name,string hex)
        {
            string path=Output+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            ColorUtility.TryParseHtmlString("#"+hex,out var color);
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.16f);material.SetFloat("_Metallic",0);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
        }
        static void Quad(int sub,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int start=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles[sub].AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }
        static void Tile(float x,float z,float w,float d,int color,bool wood)
        {
            // Narrow recessed joints and a tiny chamfer catch light without noisy decoration.
            float gap=wood?.009f:.017f,bevel=wood?.008f:.016f,y=.006f;
            float l=x+gap,r=x+w-gap,b=z+gap,t=z+d-gap;
            Quad(color,new Vector3(l+bevel,y,b+bevel),new Vector3(l+bevel,y,t-bevel),new Vector3(r-bevel,y,t-bevel),new Vector3(r-bevel,y,b+bevel));
            Quad(5,new Vector3(l,0,b),new Vector3(l,0,t),new Vector3(l+bevel,y,t-bevel),new Vector3(l+bevel,y,b+bevel));
            Quad(5,new Vector3(l,0,t),new Vector3(r,0,t),new Vector3(r-bevel,y,t-bevel),new Vector3(l+bevel,y,t-bevel));
            Quad(5,new Vector3(r,0,t),new Vector3(r,0,b),new Vector3(r-bevel,y,b+bevel),new Vector3(r-bevel,y,t-bevel));
            Quad(5,new Vector3(r,0,b),new Vector3(l,0,b),new Vector3(l+bevel,y,b+bevel),new Vector3(r-bevel,y,b+bevel));
        }
        public static void Floor(string scene,bool dining)
        {
            string kind=dining?"Dining":"Kitchen",name=scene+kind+"Floor";
            var original=GameObject.Find(kind+" floor");var bounds=original.GetComponent<Collider>().bounds;
            var go=GameObject.Find(name+" art");
            vertices.Clear();foreach(var list in triangles)list.Clear();
            Quad(6,new Vector3(bounds.min.x,-.025f,bounds.min.z),new Vector3(bounds.min.x,-.025f,bounds.max.z),new Vector3(bounds.max.x,-.025f,bounds.max.z),new Vector3(bounds.max.x,-.025f,bounds.min.z));
            if(!dining)
            {
                // Whole cells fit the existing 12 x 13.2 footprint; no world scaling.
                int columns=15,rows=16;float w=bounds.size.x/columns,d=bounds.size.z/rows;
                for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)
                {
                    bool border=(x==1||x==columns-2)&&z>=1&&z<=rows-2 || (z==1||z==rows-2)&&x>=1&&x<=columns-2;
                    int color=border?3+(x+z)%2:(x*13+z*7)%3;
                    Tile(bounds.min.x+x*w,bounds.min.z+z*d,w,d,color,false);
                }
            }
            else
            {
                const float width=.4f,length=1.65f;
                for(int row=0;row<Mathf.CeilToInt(bounds.size.x/width);row++)
                {
                    float x=bounds.min.x+row*width,start=bounds.min.z-(row%3)*length/3;
                    for(int n=0;start+n*length<bounds.max.z;n++)
                    {
                        float z=Mathf.Max(bounds.min.z,start+n*length),end=Mathf.Min(bounds.max.z,start+(n+1)*length);
                        if(end>z+.001f)Tile(x,z,Mathf.Min(width,bounds.max.x-x),end-z,(row*7+n*3)%5,true);
                    }
                }
            }
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Output+"/"+name+".asset");mesh.Clear();mesh.SetVertices(vertices);mesh.subMeshCount=7;
            for(int i=0;i<7;i++)mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            string[] colors=dining?new[]{"B78652","BC8C58","AF7E4B","C19360","B38250","A77646","815E3F"}:new[]{"D5C3A3","DAC9AA","D2BE9D","B7795F","BD8066","BBA789","96836C"};
            go.GetComponent<MeshRenderer>().sharedMaterials=colors.Select((c,i)=>Material((dining?"OakPlank":"BistroTile")+i,c)).ToArray();
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        // Review-only material overrides and camera placement; never save this capture scene.
        public static void Capture()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Capture runs in an isolated batch project; use the gameplay camera for live Editor review.");
            EditorSceneManager.OpenScene("Assets/Scenes/RestaurantShift.unity");
            var chef=UnityEngine.Object.FindAnyObjectByType<ChefController>();
            var appearance=chef.GetComponentInChildren<ChefAppearance>();if(appearance!=null)appearance.Apply(ChefAppearanceData.Example(0));
            // Explicit per-renderer colors make immediate Editor captures independent of
            // material constant-buffer uploads normally performed during a running frame.
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>())
            for(int i=0;i<renderer.sharedMaterials.Length;i++)
            {
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                if(!block.isEmpty)continue;
                renderer.GetPropertyBlock(block,i);
                if(!block.isEmpty)continue;
                var material=renderer.sharedMaterials[i];
                if(material!=null && material.HasProperty("_BaseColor")){block.SetColor("_BaseColor",material.GetColor("_BaseColor"));renderer.SetPropertyBlock(block,i);}
            }
            var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;
            Directory.CreateDirectory("Builds/FloorReview");
            Render(camera,"Builds/FloorReview/gameplay.png");
            camera.orthographicSize=4.6f;camera.transform.position=new Vector3(-2,0,1)-camera.transform.forward*20;
            Render(camera,"Builds/FloorReview/closer.png");
            Debug.Log("Floor review captures complete.");
        }
        static void Render(Camera camera,string path)
        {
            var target=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);
            var old=camera.targetTexture;var active=RenderTexture.active;var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);RenderTexture.ReleaseTemporary(target);}
        }
        public static void ApplyAndCapture(){Apply();Capture();}
    }
}
