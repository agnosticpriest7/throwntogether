using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ThrownTogether.Editor
{
    // Explicit, one-time additive authoring. Does not reimport or overwrite the original Blender model.
    public static class AuthorQualityOfLife
    {
        const string Folder="Assets/Art/Characters/WardrobeExtras";
        static GameObject root;static Mesh sphere,cube;
        static readonly List<CombineInstance> pieces=new List<CombineInstance>();
        static void Shape(Mesh mesh,Vector3 p,Vector3 scale)=>pieces.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p,Quaternion.identity,scale)});
        static void Ball(float x,float y,float z,float sx,float sy,float sz)=>Shape(sphere,new Vector3(x,y,z),new Vector3(sx,sy,sz));
        static void Box(float x,float y,float z,float sx,float sy,float sz)=>Shape(cube,new Vector3(x,y,z),new Vector3(sx,sy,sz));
        static void Save(string name,string bone,string material)
        {
            var mesh=new Mesh{name=name};mesh.CombineMeshes(pieces.ToArray(),true,true);pieces.Clear();
            AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");
            var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Characters/"+material+".mat");
            go.transform.SetParent(root.GetComponentsInChildren<Transform>(true).First(t=>t.name==bone),true);go.SetActive(false);
        }
        public static void Run()
        {
            if(Directory.Exists(Folder))throw new InvalidOperationException("Wardrobe extras already authored; review before regenerating.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(temp);
            temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(temp);
            root=PrefabUtility.LoadPrefabContents("Assets/Resources/ChefVisual.prefab");
            try
            {
                // Hair uses a few rounded clumps, kept above the eyebrows and off the carry space.
                for(int i=0;i<7;i++){float x=(i%4-1.5f)*.20f;float z=i<4?.10f:-.16f;Ball(x,1.80f+(i%2)*.045f,z,.31f,.25f,.38f);}Save("C_HairStyle2_curls","B_Head","C_Hair");
                Ball(-.16f,1.83f,.02f,.61f,.17f,.58f);Ball(.23f,1.81f,.04f,.26f,.16f,.57f);Ball(-.36f,1.72f,-.05f,.20f,.28f,.45f);Save("C_HairStyle3_part","B_Head","C_Hair");
                Ball(0,1.80f,-.02f,.80f,.16f,.62f);Ball(-.39f,1.86f,-.07f,.32f,.34f,.33f);Ball(.39f,1.86f,-.07f,.32f,.34f,.33f);Save("C_HairStyle4_buns","B_Head","C_Hair");
                // Additional faces share the existing flat, unlit ink language.
                Ball(-.19f,1.49f,.365f,.08f,.105f,.008f);Box(.19f,1.49f,.366f,.11f,.018f,.008f);Save("C_Eyes4","B_Head","C_Ink");
                for(int side=-1;side<=1;side+=2){Ball(side*.19f,1.49f,.366f,.096f,.126f,.008f);Box(side*.19f,1.586f,.364f,.11f,.016f,.008f);}Save("C_Eyes5","B_Head","C_Ink");
                Ball(0,1.32f,.366f,.062f,.082f,.009f);Save("C_Mouth4","B_Head","C_Ink");
                for(int i=0;i<13;i++){float x=-.11f+i*.22f/12;Ball(x,1.34f-.043f*Mathf.Sin(Mathf.PI*i/12),.366f,.026f,.018f,.008f);}Save("C_Mouth5","B_Head","C_Ink");
                for(int build=0;build<3;build++)
                {
                    float width=new[]{.54f,.62f,.73f}[build],depth=new[]{.40f,.44f,.51f}[build],front=depth/2+.034f;
                    Box(0,.78f,front,.018f,.43f,.01f);
                    for(int row=0;row<3;row++)for(int side=-1;side<=1;side+=2)Ball(side*.09f,.64f+row*.12f,front+.008f,.035f,.035f,.015f);
                    Save("C_Jacket"+build,"B_Torso","C_Cloth");
                    for(int row=0;row<3;row++)Box(0,.63f+row*.12f,front,width*.67f,.037f,.012f);
                    Save("C_Stripes"+build,"B_Torso","C_Cloth");
                    Box(0,1.045f,.13f,width*.60f,.11f,.21f);Box(.10f,.93f,front,.10f,.21f,.04f);Save("C_CustomerScarf"+build,"B_Torso","C_Cloth");
                    for(int side=-1;side<=1;side+=2)Box(side*width*.23f,.78f,front,width*.34f,.39f,.025f);
                    Save("C_CustomerVest"+build,"B_Torso","C_Cloth");
                }
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/ChefVisual.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var day=AssetDatabase.LoadAssetAtPath<DayServiceDefinition>("Assets/Resources/ServiceDay.asset");
            var role=ScriptableObject.CreateInstance<EmployeeRoleDefinition>();role.id="hire-dishwasher";role.displayName="Hire dishwasher";role.description="Washes the sink queue and returns clean plates to stock. Players clear tables.";role.hireCost=125;
            AssetDatabase.CreateAsset(role,"Assets/Data/ServiceDay/Dishwasher.asset");day.dishwasherRole=role;day.seatedPatience=90;EditorUtility.SetDirty(day);AssetDatabase.SaveAssets();
            Debug.Log("Quality-of-life additive wardrobe assets and dishwasher offer authored.");
        }
    }
}
