using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;

namespace ThrownTogether.Tests
{
    public class BootstrapSmokeTests
    {
        [Test]
        public void BootstrapSceneAndRenderPipelineAreAvailable()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(
                "Assets/Scenes/Bootstrap.unity"), Is.Not.Null);
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.Not.Null,
                "The active quality level must resolve to a render pipeline asset.");
            Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText));
        }
    }
}
