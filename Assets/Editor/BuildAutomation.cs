using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ThrownTogether.Editor
{
    public static class BuildAutomation
    {
        [Serializable]
        private sealed class Configuration { public string[] scenes; public string developmentVersion; }
        [Serializable] private sealed class BuildStamp { public string commit; public string builtAtUtc; public string developmentVersion; }

        // Invoke with -batchmode -quit -buildTarget WebGL (or Win64) -executeMethod ...
        public static void BuildWeb() => Build(BuildTarget.WebGL, "Builds/Web");
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/ThrownTogether.exe");

        private static void Build(BuildTarget target, string defaultOutput)
        {
            try
            {
                if (EditorUtility.scriptCompilationFailed)
                    throw new BuildFailedException("Unity script compilation failed.");
                var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var config = JsonUtility.FromJson<Configuration>(File.ReadAllText(Path.Combine(root, "build-config.json")));
                if (config?.scenes == null || config.scenes.Length == 0 ||
                    config.scenes.Distinct().Count() != config.scenes.Length)
                    throw new BuildFailedException("build-config.json must specify a nonempty, unique scene list.");
                foreach (var scene in config.scenes)
                    if (!scene.StartsWith("Assets/", StringComparison.Ordinal) || scene.Contains("..") ||
                        AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                        throw new BuildFailedException("Invalid configured scene: " + scene);

                var args = Environment.GetCommandLineArgs();
                bool diagnostics=args.Contains("-developmentDiagnostics");
                string commit="unversioned";
                int commitIndex=Array.IndexOf(args,"-buildCommit");
                if(commitIndex>=0 && commitIndex+1<args.Length) commit=args[commitIndex+1];
                if(diagnostics)
                {
                    Directory.CreateDirectory("Assets/Resources");
                    if(string.IsNullOrWhiteSpace(config.developmentVersion)) throw new BuildFailedException("Development builds require developmentVersion in build-config.json.");
                    File.WriteAllText("Assets/Resources/DevelopmentBuildStamp.json",JsonUtility.ToJson(new BuildStamp { commit=commit, builtAtUtc=DateTime.UtcNow.ToString("o"), developmentVersion=config.developmentVersion }));
                    AssetDatabase.Refresh();
                }
                else if(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/DevelopmentBuildStamp.json") != null)
                    AssetDatabase.DeleteAsset("Assets/Resources/DevelopmentBuildStamp.json");
                var output = defaultOutput;
                for (var i = 0; i < args.Length; i++)
                    if (args[i] == "-buildOutput")
                    {
                        if (++i >= args.Length) throw new BuildFailedException("Missing -buildOutput value.");
                        output = args[i];
                    }
                output = Path.GetFullPath(Path.Combine(root, output));
                if (target == BuildTarget.WebGL)
                {
                    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                    PlayerSettings.WebGL.decompressionFallback = false;
                    PlayerSettings.WebGL.threadsSupport = false;
                    PlayerSettings.WebGL.dataCaching = false;
                    PlayerSettings.WebGL.nameFilesAsHashes = true;
                    PlayerSettings.WebGL.template = diagnostics ? "PROJECT:Development" : "APPLICATION:Default";
                }
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = config.scenes,
                    locationPathName = output,
                    target = target,
                    options = BuildOptions.StrictMode,
                    extraScriptingDefines = diagnostics ? new[] { "THROWNTOGETHER_DIAGNOSTICS" } : new string[0]
                });
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new BuildFailedException($"Build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
                Debug.Log($"BUILD SUCCEEDED: {output}; {report.summary.totalSize} bytes; scenes: {string.Join(", ", config.scenes)}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
