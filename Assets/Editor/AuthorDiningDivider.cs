using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    public static class AuthorDiningDivider
    {
        const string Root="Assets/Art/RestaurantRoom/Prefabs";
        static void Wall(Transform parent,float start,float end,string name)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);
            root.localPosition=new Vector3(3.6f,0,(start+end)/2);
            var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.58f,0);collider.size=new Vector3(.24f,1.16f,end-start);
            for(float z=start;z<end-.001f;z+=1.9f)
            {
                float length=Mathf.Min(1.9f,end-z);
                var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/WallPanel.prefab"));
                art.transform.SetParent(root,false);art.transform.localPosition=new Vector3(0,0,z+length/2-root.localPosition.z);art.transform.localRotation=Quaternion.Euler(0,90,0);art.transform.localScale=new Vector3(length/1.9f,.85f,1);
            }
        }
        static void Box(Transform parent,string name,Vector3 position,Vector3 size,string material,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/RestaurantRoom/Materials/RM_"+material+".mat");
            if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        public static void ApplyAndCapture()
        {
            if(!Application.isBatchMode)throw new Exception("Isolated authoring only");
            if(File.Exists(Root+"/KitchenDiningDivider.prefab"))throw new Exception("Divider already authored; inspect before rerunning");
            var divider=new GameObject("Kitchen dining divider").transform;
            Wall(divider,1.75f,7.6f,"Back divider");Wall(divider,-.25f,.25f,"Between pass counters");
            Wall(divider,-2.35f,-1.75f,"Door upper jamb");Wall(divider,-5.6f,-4.35f,"Door lower jamb");
            // Held-open leaf, not a new door interaction. The two-metre opening stays clear.
            Box(divider,"Open door leaf",new Vector3(4.3f,.58f,-4.4f),new Vector3(1.15f,1.16f,.10f),"WoodDark",true);
            Box(divider,"Door inset",new Vector3(4.3f,.57f,-4.46f),new Vector3(.99f,.94f,.035f),"Trim",false);
            Box(divider,"Door window",new Vector3(4.3f,.84f,-4.485f),new Vector3(.55f,.30f,.025f),"Glass",false);
            Box(divider,"Door handle",new Vector3(4.7f,.5f,-4.51f),new Vector3(.12f,.035f,.035f),"Brass",false);
            PrefabUtility.SaveAsPrefabAsset(divider.gameObject,Root+"/KitchenDiningDivider.prefab");UnityEngine.Object.DestroyImmediate(divider.gameObject);
            for(int i=0;i<3;i++)
            {
                var layout=AssetDatabase.LoadAssetAtPath<KitchenLayoutDefinition>("Assets/Data/VerticalSlice/Kitchen"+i+".asset");
                layout.stations[8]=new Vector3(3.6f,0,1);layout.stations[11]=new Vector3(3.6f,0,-1);
                if(i==2)layout.stations[7]=new Vector3(.6f,0,-1.7f);
                if(i==1){layout.stations[9]=new Vector3(-6,0,1);layout.stations[10]=new Vector3(-2.7f,0,-4.5f);}
                EditorUtility.SetDirty(layout);
            }
            foreach(var name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/KitchenDiningDivider.prefab"));
                foreach(var order in UnityEngine.Object.FindObjectsByType<CustomerOrder>())
                {
                    order.tableSlot.transform.parent.position+=Vector3.right*.6f;
                    order.customerVisual.position+=Vector3.right*.6f;
                }
                UnityEngine.Object.FindAnyObjectByType<KitchenLayout>().Apply(0);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();AuthorRestaurantDetails.Capture();
        }
    }
}

