using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace ThrownTogether.Editor
{
    public static class BakeDishIcons
    {
        public static void Bake()
        {
            if(!Application.isPlaying)throw new System.InvalidOperationException("Bake in a temporary Play Mode review");
            const string folder="Assets/Resources/DishIcons";Directory.CreateDirectory(folder);
            var prefab=AssetDatabase.LoadAssetAtPath<Carryable>("Assets/Prefabs/VerticalSlice/Carryable.prefab");
            var cameraObject=new GameObject("Dish icon bake camera");var camera=cameraObject.AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;
            var origin=new Vector3(1000,1000,1000);camera.transform.position=origin+new Vector3(0,1.7f,-1.7f);camera.transform.LookAt(origin+Vector3.up*.1f);
            var texture=new RenderTexture(192,192,24,RenderTextureFormat.ARGB32);camera.targetTexture=texture;
            var lightObject=new GameObject("Dish bake light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(50,-30,0);
            var previous=RenderTexture.active;
            try
            {
                foreach(var recipe in Resources.Load<RecipeBook>("RecipeBook").recipes)
                {
                    var dish=Object.Instantiate(prefab,origin,Quaternion.identity);var payload=new ItemPayload{ingredient=recipe.ingredient,state=recipe.requiredState,isPlate=true};
                    foreach(var addition in recipe.additionalIngredients)payload.additions.Add(new IngredientPortion{ingredient=addition.ingredient,state=addition.state});
                    dish.Configure(payload);foreach(var t in dish.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                    camera.Render();RenderTexture.active=texture;var image=new Texture2D(192,192,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,192,192),0,0);image.Apply();
                    File.WriteAllBytes(folder+"/"+recipe.id+".png",image.EncodeToPNG());Object.Destroy(image);dish.gameObject.SetActive(false);Object.Destroy(dish.gameObject);
                }
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(cameraObject);Object.Destroy(lightObject);texture.Release();Object.Destroy(texture);}
            AssetDatabase.Refresh();
            foreach(var file in Directory.GetFiles(folder,"*.png")){var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        }
    }
}
