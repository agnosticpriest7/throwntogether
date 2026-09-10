using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ThrownTogether.Tests
{
    public sealed class KitchenArtTests
    {
        [Test] public void ProductionSinkOmitsThePrototypeDemonstrationDish()
        {
            var prototype=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VisualDirection/Prefabs/Sink.prefab");
            var production=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Kitchen/SinkWithoutDemoPlate.asset");
            Assert.That(production,Is.Not.Null);
            var renderer=prototype.GetComponentInChildren<Renderer>();
            int ceramic=System.Array.FindIndex(renderer.sharedMaterials,m=>m.name=="VD_Ceramic");
            Assert.That(ceramic,Is.GreaterThanOrEqualTo(0));
            Assert.That(prototype.GetComponentInChildren<MeshFilter>().sharedMesh.GetIndexCount(ceramic),Is.GreaterThan(0));
            Assert.That(production.GetIndexCount(ceramic),Is.Zero,"An empty live sink must not display a permanent sample plate.");
        }
        [Test] public void ReviewScenesRemainExcludedFromPlayableConfiguration()
        {
            var config=File.ReadAllText("build-config.json");
            Assert.That(config,Does.Not.Contain("VisualDirectionTest"));Assert.That(config,Does.Not.Contain("CharacterReview"));
        }
    }
}
