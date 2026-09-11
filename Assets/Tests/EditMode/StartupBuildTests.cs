using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class StartupBuildTests
    {
        [System.Serializable] private sealed class Config {public string[] scenes;}
        [Test] public void BootAndTitleLaunchBeforeExistingPlayableScenes()
        {
            var config=JsonUtility.FromJson<Config>(File.ReadAllText(Path.Combine(Application.dataPath,"../build-config.json")));
            Assert.That(config.scenes,Is.EqualTo(new[]{"Assets/Scenes/Boot.unity","Assets/Scenes/MainMenu.unity","Assets/Scenes/RestaurantDevelopment.unity","Assets/Scenes/RestaurantShift.unity"}));
            for(int i=0;i<config.scenes.Length;i++)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(config.scenes[i]),Is.Not.Null);
                Assert.That(EditorBuildSettings.scenes[i].path,Is.EqualTo(config.scenes[i]));Assert.That(EditorBuildSettings.scenes[i].enabled,Is.True);
            }
        }
    }
}
