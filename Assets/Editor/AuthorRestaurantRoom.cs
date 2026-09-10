using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace ThrownTogether.Editor
{
    public static class AuthorRestaurantRoom
    {
        const string Root="Assets/Art/RestaurantRoom";
        static readonly string[] Names={"Wood","WoodLight","WoodDark","Cream","Trim","Dark","Fabric","FabricLight","Glass","GlassLight","Brass","Glow"};
        static readonly string[] Colors={"B58B5C","CEA778","795B40","D8D0BC","536F73","34484C","AE584B","C87864","85B2BF","B8D4D4","B49459","FFE1A5"};
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static string Signature(Scene s)=>string.Join("\n",s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Select(c=>PathOf(c.transform)+EditorJsonUtility.ToJson(c)+c.transform.position.ToString("F5")+c.transform.rotation.ToString("F5")+c.transform.lossyScale.ToString("F5")).Concat(s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarrySlot>(true)).Select(c=>PathOf(c.transform)+c.transform.position.ToString("F5"))).OrderBy(x=>x));
        static GameObject Place(string module,Transform parent,Vector3 position,float yaw=0,Vector3? scale=null)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+module+".prefab"));
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=scale??Vector3.one;return go;
        }
        [MenuItem("Thrown Together/Art/Apply dining and cutaway room kit")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Preserve unsaved work and leave Play Mode first.");
            if(File.Exists(Root+"/Prefabs/DiningTable.prefab"))throw new InvalidOperationException("Room kit already exists; inspect rather than overwrite.");
            string restore=SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
            for(int i=0;i<Names.Length;i++)
            {
                var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="RM_"+Names[i],enableInstancing=true};ColorUtility.TryParseHtmlString("#"+Colors[i],out var color);m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",Names[i].Contains("Glass")?.32f:.16f);m.SetFloat("_Metallic",0);
                if(Names[i]=="Glow"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.2f);}
                AssetDatabase.CreateAsset(m,Root+"/Materials/"+m.name+".mat");
            }
            foreach(string path in Directory.GetFiles(Root+"/Models","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                string name=Path.GetFileNameWithoutExtension(path);var wrapper=new GameObject(name);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(wrapper.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
                foreach(var renderer in wrapper.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+m.name+".mat")??throw new Exception(m.name)).ToArray();
                PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(wrapper);
            }
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");string before=Signature(scene);
                foreach(var order in UnityEngine.Object.FindObjectsByType<CustomerOrder>())
                {
                    var table=order.tableSlot.transform.parent;table.Find("Table").GetComponent<Renderer>().enabled=false;
                    Place("DiningTable",table,Vector3.zero).name="Dining table art";
                    var chair=order.customerVisual.Find("Chair");if(chair==null)throw new Exception("Missing original chair");chair.GetComponent<Renderer>().enabled=false;
                    // Same parent as the old chair preserves existing visibility/eating animation behavior.
                    Place("DiningChair",order.customerVisual,Vector3.zero).name="Dining chair art";
                }
                var old=GameObject.Find("Modular cutaway walls");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
                var room=new GameObject("Restaurant room art").transform;
                for(int i=0;i<9;i++)
                {
                    float left=-8.4f+i*1.9f,w=Mathf.Min(1.9f,8.4f-left);
                    Place(i==2||i==4||i==7?"WindowWall":"WallPanel",room,new Vector3(left+w/2,0,7.6f),0,new Vector3(w/1.9f,1,1));
                }
                for(int i=0;i<7;i++)
                {
                    float front=-5.6f+i*1.9f,w=Mathf.Min(1.9f,7.6f-front);
                    Place("WallLow",room,new Vector3(-8.4f,0,front+w/2),-90,new Vector3(w/1.9f,1,1));
                    Place("WallLow",room,new Vector3(8.4f,0,front+w/2),90,new Vector3(w/1.9f,1,1));
                }
                GameObject.Find("Front boundary").GetComponent<Renderer>().enabled=false;
                foreach(var segment in new[]{new Vector2(-8.4f,-2.8f),new Vector2(-.6f,8.4f)})
                for(float x=segment.x;x<segment.y-.001f;x+=1.9f){float w=Mathf.Min(1.9f,segment.y-x);Place("WallLow",room,new Vector3(x+w/2,0,-5.6f),0,new Vector3(w/1.9f,.32f,1));}
                Place("EntranceCutaway",room,new Vector3(-1.7f,0,-5.6f));
                foreach(float z in new[]{4.6f,-2.6f})
                {
                    Place("WallSconce",room,new Vector3(8.24f,.88f,z),90);
                    var light=new GameObject("Warm dining fill").AddComponent<Light>();light.transform.SetParent(room,false);light.transform.localPosition=new Vector3(7.55f,1.65f,z-.25f);light.type=LightType.Point;light.color=new Color(1,.77f,.52f);light.intensity=1;light.range=4;light.shadows=LightShadows.None;
                }
                var key=GameObject.Find("Key light").GetComponent<Light>();key.color=new Color(1,.96f,.90f);key.intensity=1.05f;key.shadowStrength=.58f;key.shadows=LightShadows.Soft;
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.66f,.74f,.81f);RenderSettings.ambientEquatorColor=new Color(.53f,.59f,.59f);RenderSettings.ambientGroundColor=new Color(.35f,.39f,.40f);
                if(Signature(scene)!=before)throw new Exception("Original physics or carry slot changed; scene not saved.");
                EditorSceneManager.SaveScene(scene);Debug.Log(name+": room art applied; original colliders and carry slots unchanged.");
            }
            foreach(string name in new[]{"Steel","Highlight"}){var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Kitchen/MaterialsV2/KT_"+name+".mat");m.SetFloat("_Smoothness",name=="Steel"?.38f:.42f);EditorUtility.SetDirty(m);}
            AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);
        }
        public static void ApplyAndCapture(){Apply();AuthorFirstServiceFloor.Capture();Directory.CreateDirectory("Builds/RoomReview");foreach(string f in Directory.GetFiles("Builds/FloorReview","*.png"))File.Copy(f,"Builds/RoomReview/"+Path.GetFileName(f),true);}
    }
}
