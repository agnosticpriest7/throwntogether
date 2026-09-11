using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace ThrownTogether.Editor
{
    public static class AuthorStartup
    {
        private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        public static void Run()
        {
            if(!Application.isBatchMode || File.Exists("Assets/Scenes/Boot.unity") || File.Exists("Assets/Scenes/MainMenu.unity"))throw new System.Exception("Author startup only in a fresh isolated snapshot");
            foreach(var name in new[]{"AgnosticStudios","ThrownTogetherTitle"})
            {
                string path="Assets/Art/Startup/"+name+".jpg";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            }
            Make(true);Make(false);
            EditorBuildSettings.scenes=new[]{"Boot","MainMenu","RestaurantDevelopment","RestaurantShift"}.SelectSceneSettings();
            AssetDatabase.SaveAssets();
        }
        private static EditorBuildSettingsScene[] SelectSceneSettings(this string[] names)
        {
            var scenes=new EditorBuildSettingsScene[names.Length];for(int i=0;i<names.Length;i++)scenes[i]=new EditorBuildSettingsScene("Assets/Scenes/"+names[i]+".unity",true);return scenes;
        }
        private static void Make(bool intro)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Startup camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=0;
            var root=new GameObject("Startup UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var backdrop=Rect("Black backdrop",root.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();backdrop.color=Color.black;backdrop.raycastTarget=false;
            var content=Rect("Replaceable artwork",root.transform,Vector2.zero,Vector2.one);var group=content.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Startup/"+(intro?"AgnosticStudios":"ThrownTogetherTitle")+".jpg");
            var image=Rect("Logo or title image",content,intro?new Vector2(.1f,.04f):Vector2.zero,intro?new Vector2(.9f,.96f):Vector2.one);
            var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=(float)texture.width/texture.height;
            var raw=image.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
            if(!intro)
            {
                var panel=Rect("Start prompt backdrop",content,new Vector2(.28f,.035f),new Vector2(.72f,.135f));panel.gameObject.AddComponent<Image>().color=new Color(0,0,0,.82f);
                var text=Rect("Press A to Start",panel,Vector2.zero,Vector2.one).gameObject.AddComponent<Text>();text.text="Press A to Start\nEnter / Space / Click";text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=25;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.94f,.79f);text.raycastTarget=false;
            }
            var flow=root.AddComponent<StartupSequence>();flow.studioIntro=intro;flow.artwork=group;flow.canvas=canvas;
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+(intro?"Boot":"MainMenu")+".unity");
        }
    }
}
