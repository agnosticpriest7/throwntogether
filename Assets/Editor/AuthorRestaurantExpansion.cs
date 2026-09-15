using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ThrownTogether.Editor
{
    public static class AuthorRestaurantExpansion
    {
        const string Folder="Assets/Art/RestaurantExpansion";
        public static void Run()
        {
            if(Application.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene work and exit Play Mode first.");
            var restore=SceneManager.GetActiveScene().path;
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var settings=AssetDatabase.LoadAssetAtPath<DayServiceDefinition>("Assets/Resources/ServiceDay.asset");
            foreach(var offer in new[]{(RestaurantExpansion.KitchenId,"Kitchen extension — 7 new bays",500),(RestaurantExpansion.DiningId,"Dining extension — 2 more tables",600)})
            {
                if(settings.purchases.Any(p=>p.id==offer.Item1))continue;
                var asset=ScriptableObject.CreateInstance<RestaurantUpgradeDefinition>();asset.id=offer.Item1;asset.displayName=offer.Item2;asset.cost=offer.Item3;asset.kind=RestaurantPurchaseKind.RoomExpansion;
                asset.description="Permanent room space; opens immediately during management.";
                AssetDatabase.CreateAsset(asset,Folder+"/"+offer.Item1+".asset");settings.purchases=settings.purchases.Concat(new[]{asset}).ToArray();
            }
            EditorUtility.SetDirty(settings);
            foreach(var name in new[]{"RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var hud=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>();
                if(hud.GetComponent<RestaurantExpansion>()!=null)continue;
                var expansion=hud.gameObject.AddComponent<RestaurantExpansion>();
                expansion.kitchenArea=Area(name,false);expansion.diningArea=Area(name,true);
                // Clone the complete inactive bundle so all table/seat/order references remap together.
                var shift=UnityEngine.Object.FindAnyObjectByType<RestaurantShift>();var tables=UnityEngine.Object.Instantiate(shift.diningExpansion,expansion.diningArea.transform);
                tables.name="Extension tables 5 and 6";tables.transform.position+=Vector3.right*4.2f;
                expansion.additionalSeats=tables.GetComponentsInChildren<CustomerOrder>(true);
                int number=5;foreach(var order in expansion.additionalSeats){order.name="Extension order "+number;order.tableSlot.transform.parent.name="Extension table "+number++;}
                tables.SetActive(true);
                var room=GameObject.Find("Restaurant room art").transform;
                var parts=room.Cast<Transform>().ToArray();
                expansion.westEdge=parts.Where(p=>Mathf.Abs(p.position.x+8.4f)<.03f).Concat(new[]{GameObject.Find("Left wall").transform}).ToArray();
                expansion.eastEdge=parts.Where(p=>p.position.x>12.3f && p.position.z<7.5f && p.position.z>-5.5f).Concat(new[]{GameObject.Find("Right wall").transform}).ToArray();
                expansion.wideBoundaries=new[]{GameObject.Find("Back wall").transform,GameObject.Find("Front boundary").transform};
                expansion.kitchenArea.SetActive(false);expansion.diningArea.SetActive(false);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);
        }
        static GameObject Area(string scene,bool dining)
        {
            float min=dining?12.6f:-12.2f,max=dining?16.8f:-8.4f;
            var area=new GameObject(dining?"Purchased dining extension":"Purchased kitchen extension");
            var floor=new GameObject("Extension floor");floor.transform.SetParent(area.transform);
            var collision=floor.AddComponent<BoxCollider>();collision.center=new Vector3((min+max)/2,-.15f,1);collision.size=new Vector3(max-min,.3f,13.2f);
            var original=GameObject.Find(scene+(dining?"Dining":"Kitchen")+"Floor art");
            var mesh=Floor(min,max,dining);string path=Folder+"/"+scene+(dining?"Dining":"Kitchen")+".asset";mesh.name=System.IO.Path.GetFileNameWithoutExtension(path);var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            floor.AddComponent<MeshFilter>().sharedMesh=mesh;floor.AddComponent<MeshRenderer>().sharedMaterials=original.GetComponent<MeshRenderer>().sharedMaterials;
            foreach(float z in new[]{7.6f,-5.6f})for(float x=min;x<max-.01f;x+=1.9f)
            {
                float w=Mathf.Min(1.9f,max-x);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RestaurantRoom/Prefabs/"+(z>0?"WallPanel":"WallLow")+".prefab");
                var wall=(GameObject)PrefabUtility.InstantiatePrefab(prefab);wall.transform.SetParent(area.transform);wall.transform.position=new Vector3(x+w/2,0,z);wall.transform.localScale=new Vector3(w/1.9f,z>0?1:.32f,1);
            }
            return area;
        }
        static Mesh Floor(float min,float max,bool wood)
        {
            var vertices=new List<Vector3>();var triangles=Enumerable.Range(0,7).Select(_=>new List<int>()).ToArray();
            void Quad(float x,float z,float w,float d,int material,float y)
            {int n=vertices.Count;vertices.AddRange(new[]{new Vector3(x,y,z),new Vector3(x,y,z+d),new Vector3(x+w,y,z+d),new Vector3(x+w,y,z)});triangles[material].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            Quad(min,-5.6f,max-min,13.2f,6,-.025f);
            float width=wood?.4f:.8f,length=wood?1.65f:.825f,gap=wood?.009f:.017f;
            int row=0;for(float x=min;x<max-.001f;x+=width,row++)
            {int col=0;for(float start=-5.6f-(wood?(row%3)*length/3:0);start<7.6f;start+=length,col++)
            {float z=Mathf.Max(start,-5.6f),end=Mathf.Min(start+length,7.6f);if(end-z<gap*2)continue;Quad(x+gap,z+gap,Mathf.Min(width,max-x)-gap*2,end-z-gap*2,wood?(row*7+col*3)%5:(row+col)%3,.006f);}}
            var mesh=new Mesh{name=wood?"Extension oak planks":"Extension kitchen tiles"};mesh.SetVertices(vertices);mesh.subMeshCount=7;for(int i=0;i<7;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
