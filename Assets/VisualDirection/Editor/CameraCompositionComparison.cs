using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Review-only: invoke in an isolated batch project. Never saves a scene or asset.
public static class CameraCompositionComparison
{
    const string Output = "docs/art-reference/camera-comparison-2026-09-10";
    [Serializable] public class View
    {
        public string name;
        public Vector3 position, euler;
        public float orthographicSize;
        public bool orthographic;
        public int width = 1920, height = 1080;
    }
    [Serializable] public class Report
    {
        public string scene, sourceSceneSha256, unityVersion;
        public string method = "Isolated Unity Camera.Render; not live Unity MCP or physical co-op verification";
        public string invariants = "Same scene, assets, lights, materials, chef scales, station transforms and colliders; no scene saved";
        public View[] views;
    }

    public static void Capture()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated batch project; preserve the live Editor.");
        const string scenePath = "Assets/VisualDirection/VisualDirectionTest.unity";
        var sceneBytes = File.ReadAllBytes(scenePath);
        EditorSceneManager.OpenScene(scenePath);
        var camera = Camera.main;
        if (camera == null || !camera.orthographic) throw new InvalidOperationException("Expected the saved orthographic baseline.");
        var position = camera.transform.position;
        var rotation = camera.transform.rotation;
        var size = camera.orthographicSize;
        var target = position + camera.transform.forward * 20;
        var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        var originalMatrices = transforms.Select(t => t.localToWorldMatrix).ToArray();
        Directory.CreateDirectory(Output);
        var views = new View[6];
        var asyncCompilation = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        try
        {
            for (int variant = 0; variant < 3; variant++)
            {
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.orthographicSize = size;
                if (variant != 0)
                {
                    camera.transform.rotation = Quaternion.Euler(variant == 1 ? 45 : 35, rotation.eulerAngles.y, 0);
                    camera.transform.position = target - camera.transform.forward * 20;
                    camera.orthographicSize = variant == 1 ? 5.75f : 5.25f;
                }
                string prefix = ((char)('A' + variant)).ToString();
                views[variant * 2] = Render(camera, prefix + "-normal");
                camera.orthographicSize *= 1.5f;
                views[variant * 2 + 1] = Render(camera, prefix + "-expansion");
            }
        }
        finally
        {
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographicSize = size;
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
        }
        for (int i = 0; i < transforms.Length; i++)
            if (transforms[i].localToWorldMatrix != originalMatrices[i]) throw new InvalidOperationException("Transform changed: " + transforms[i].name);
        if (!File.ReadAllBytes(scenePath).SequenceEqual(sceneBytes)) throw new InvalidOperationException("Source scene bytes changed.");
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var report = new Report { scene = scenePath, sourceSceneSha256 = BitConverter.ToString(sha.ComputeHash(sceneBytes)).Replace("-", "").ToLowerInvariant(), unityVersion = Application.unityVersion, views = views };
            File.WriteAllText(Output + "/capture-settings.json", JsonUtility.ToJson(report, true));
        }
        Debug.Log("Camera comparison complete: six captures; all transforms restored; source scene bytes unchanged. " + Path.GetFullPath(Output));
    }

    static View Render(Camera camera, string name)
    {
        var view = new View { name = name, position = camera.transform.position, euler = camera.transform.eulerAngles, orthographicSize = camera.orthographicSize, orthographic = camera.orthographic };
        var rt = RenderTexture.GetTemporary(view.width, view.height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        var previous = RenderTexture.active;
        var target = camera.targetTexture;
        var image = new Texture2D(view.width, view.height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            // Warm the renderer before readback so A cannot capture loading shaders.
            camera.Render();
            camera.Render();
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, view.width, view.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = target;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(image);
        }
        return view;
    }
}
