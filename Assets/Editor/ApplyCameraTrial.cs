using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThrownTogether.Editor
{
    public static class ApplyCameraTrial
    {
        static string Signature(Scene scene, Camera camera) => string.Join("\n", scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Component>(true))
            .Where(c => c != null && c != camera && c != camera.transform)
            .Select(c => c.GetEntityId() + ":" + EditorJsonUtility.ToJson(c)).OrderBy(s => s));

        public static void Apply() => Set(45, 7.2f);
        public static void RestoreBaseline() => Set(55, 8.1f);
        static void Set(float pitch, float size)
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated copy, preserving the open Editor.");
            foreach (string name in new[] { "RestaurantDevelopment", "RestaurantShift" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                var camera = UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;
                string before = Signature(scene, camera);
                camera.orthographic = true;
                camera.orthographicSize = size;
                camera.transform.rotation = Quaternion.Euler(pitch, 0, 0);
                camera.transform.position = new Vector3(0, .3f, 1) - camera.transform.forward * 20;
                if (Signature(scene, camera) != before) throw new Exception("Non-camera scene data changed; refusing save.");
                EditorSceneManager.SaveScene(scene);
                Debug.Log(name + ": camera-only change verified; pitch=" + pitch + " size=" + size);
            }
            Capture();
        }

        public static void Capture()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Isolated review only.");
            EditorSceneManager.OpenScene("Assets/Scenes/RestaurantShift.unity");
            var hud = UnityEngine.Object.FindAnyObjectByType<RestaurantHud>();
            var camera = hud.gameplayCamera;
            var prefab = AssetDatabase.LoadAssetAtPath<Carryable>("Assets/Prefabs/VerticalSlice/Carryable.prefab");
            foreach (var order in UnityEngine.Object.FindObjectsByType<CustomerOrder>()) order.GetComponent<CustomerPresentation>().Initialize(order, prefab);
            hud.chef.GetComponentInChildren<ChefAppearance>().Apply(ChefAppearanceData.Example(0));
            var async = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            float size = camera.orthographicSize;
            try
            {
                Render(camera, "normal");
                // Temporary existing-kit chefs/items illustrate opposing headings, not saved gameplay.
                var potato = AssetDatabase.FindAssets("t:IngredientDefinition").Select(id => AssetDatabase.LoadAssetAtPath<IngredientDefinition>(AssetDatabase.GUIDToAssetPath(id))).First(i => i.visualKind == IngredientVisualKind.Potato);
                var two = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ChefController>("Assets/Prefabs/VerticalSlice/Chef.prefab"));
                two.transform.position = new Vector3(2.1f, .03f, -2.7f);
                two.transform.rotation = Quaternion.identity;
                hud.chef.transform.rotation = Quaternion.Euler(0, 180, 0);
                foreach (var chef in new[] { hud.chef, two })
                {
                    var food = UnityEngine.Object.Instantiate(prefab);
                    food.Configure(new ItemPayload { ingredient = potato, state = FoodState.Cooked, isPlate = true });
                    chef.Hands.TryTake(food);
                    var appearance = chef.GetComponentInChildren<ChefAppearance>();
                    appearance.Apply(ChefAppearanceData.Example(chef == two ? 2 : 0));
                    appearance.Pose(1, 0, 1);
                }
                Render(camera, "two-chefs-held-food");
                camera.orthographicSize = size * Mathf.Sqrt(1.5f);
                Render(camera, "expansion-50-percent-area");
            }
            finally { camera.orthographicSize = size; ShaderUtil.allowAsyncCompilation = async; }
            Debug.Log("Review poses and expansion camera were not saved.");
        }
        static void Render(Camera camera, string name)
        {
            const string output = "Builds/CameraTrialReview";
            Directory.CreateDirectory(output);
            var target = RenderTexture.GetTemporary(1600, 1000, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            var old = camera.targetTexture; var active = RenderTexture.active;
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render(); camera.Render(); camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(output + "/" + name + ".png", image.EncodeToPNG());
            }
            finally { camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
