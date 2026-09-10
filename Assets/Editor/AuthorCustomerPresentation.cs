using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ThrownTogether.Editor
{
    public static class AuthorCustomerPresentation
    {
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static string Signature(Scene scene)=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Select(c=>PathOf(c.transform)+EditorJsonUtility.ToJson(c)+c.transform.position.ToString("F5")+c.transform.rotation.ToString("F5")+c.transform.lossyScale.ToString("F5")).Concat(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarrySlot>(true)).Select(c=>PathOf(c.transform)+c.transform.position.ToString("F5"))).OrderBy(x=>x));
        public static void ApplyAndCapture()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch project to preserve live Editor work.");
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");string before=Signature(scene);int index=0;
                foreach(var order in UnityEngine.Object.FindObjectsByType<CustomerOrder>().OrderByDescending(o=>o.customerVisual.position.z))
                {
                    if(order.GetComponent<CustomerPresentation>()!=null)throw new InvalidOperationException("Customer presentation already authored; inspect before rerunning.");
                    foreach(var renderer in order.customerVisual.GetComponentsInChildren<Renderer>())if(renderer.transform.parent==order.customerVisual&&renderer.name!="Chair")renderer.enabled=false;
                    var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ChefVisual.prefab"));actor.name="Seated customer art";actor.transform.SetParent(order.customerVisual,false);actor.transform.localPosition=new Vector3(0,.35f,.16f);actor.transform.localRotation=Quaternion.Euler(0,180,0);actor.transform.localScale=Vector3.one*.85f;
                    var appearance=actor.GetComponent<ChefAppearance>();appearance.enabled=false;appearance.usePlayerSelection=false;
                    var presentation=order.gameObject.AddComponent<CustomerPresentation>();presentation.seatedVisual=appearance;presentation.variant=index++;
                }
                if(Signature(scene)!=before)throw new Exception("Original colliders or slots changed; scene not saved.");
                EditorSceneManager.SaveScene(scene);Debug.Log(name+": seated art saved, original colliders and slots unchanged.");
            }
            Capture();
        }
        public static void Capture()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Capture is isolated batch only.");
            EditorSceneManager.OpenScene("Assets/Scenes/RestaurantShift.unity");
            var prefab=AssetDatabase.LoadAssetAtPath<Carryable>("Assets/Prefabs/VerticalSlice/Carryable.prefab");
            foreach(var order in UnityEngine.Object.FindObjectsByType<CustomerOrder>())order.GetComponent<CustomerPresentation>().Initialize(order,prefab);
            UnityEngine.Object.FindAnyObjectByType<ChefController>().GetComponentInChildren<ChefAppearance>().Apply(ChefAppearanceData.Example(0));
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>())for(int i=0;i<renderer.sharedMaterials.Length;i++)
            {
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);if(!block.isEmpty)continue;renderer.GetPropertyBlock(block,i);if(!block.isEmpty)continue;
                var material=renderer.sharedMaterials[i];if(material!=null&&material.HasProperty("_BaseColor")){block.SetColor("_BaseColor",material.GetColor("_BaseColor"));renderer.SetPropertyBlock(block,i);}
            }
            Directory.CreateDirectory("Builds/CustomerReview");var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;Render(camera,"gameplay");
            camera.orthographicSize=1.6f;camera.transform.position=new Vector3(6,1,5.5f)-camera.transform.forward*20;Render(camera,"seated-close");
            foreach(var presentation in UnityEngine.Object.FindObjectsByType<CustomerPresentation>())presentation.ApplyPose(OrderPhase.Eating,.31f,false);Render(camera,"eating-close");
            Debug.Log("Customer review captures complete; no review poses saved.");
        }
        static void Render(Camera camera,string name)
        {
            var target=RenderTexture.GetTemporary(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Builds/CustomerReview/"+name+".png",texture.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);RenderTexture.ReleaseTemporary(target);}
        }
    }
}
