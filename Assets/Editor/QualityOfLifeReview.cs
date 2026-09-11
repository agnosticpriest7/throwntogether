using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    public static class QualityOfLifeReview
    {
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.55f,.57f,.60f);
            var light=new GameObject("Review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(45,-35,0);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ChefVisual.prefab");
            for(int row=0;row<2;row++)for(int i=0;i<4;i++)
            {
                var go=Object.Instantiate(prefab);go.transform.position=new Vector3((i-1.5f)*1.7f,0,row*2.5f);go.transform.rotation=Quaternion.Euler(0,-12,0);
                var visual=go.GetComponent<ChefAppearance>();visual.usePlayerSelection=false;
                if(row==1)CustomerPresentation.ApplyCustomerLook(visual,42+i);
                else visual.Apply(new ChefAppearanceData{build=i%3,bodyColor=4+i,clothing=i%2==0?3:4,clothingColor=4+i,hair=1+i,headwear=0,hairColor=i+1,eyes=i%2+4,mouth=i%2+4});
            }
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3.5f;camera.backgroundColor=new Color(.13f,.17f,.19f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.transform.position=new Vector3(0,5.4f,9.5f);camera.transform.LookAt(new Vector3(0,.8f,1.25f));
            ShaderUtil.allowAsyncCompilation=false;
            var rt=RenderTexture.GetTemporary(1600,1000,24);camera.targetTexture=rt;camera.Render();camera.Render();camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
            Directory.CreateDirectory("Builds/QualityOfLifeReview");File.WriteAllBytes("Builds/QualityOfLifeReview/wardrobe-and-customers.png",image.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);
        }
    }
}
