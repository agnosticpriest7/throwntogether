using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ThrownTogether.Tests
{
    public sealed class KitchenArtTests
    {
        [Test] public void RefinedModulesPreserveStationScaleAndRemainVisualOnly()
        {
            var paths=Directory.GetFiles("Assets/Art/Kitchen/PrefabsV2","*.prefab");Assert.That(paths.Length,Is.EqualTo(11));
            foreach(var path in paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var instance=Object.Instantiate(prefab);
                try
                {
                    Assert.That(instance.GetComponentsInChildren<Collider>(),Is.Empty,path);
                    Assert.That(instance.GetComponentsInChildren<MonoBehaviour>(),Is.Empty,path);
                    var renderer=instance.GetComponentInChildren<Renderer>();var bounds=renderer.bounds;
                    Assert.That(bounds.size.x,Is.EqualTo(1.9f).Within(.01f),path);
                    Assert.That(bounds.size.z,Is.LessThan(1.72f),path);
                    Assert.That(bounds.min.y,Is.InRange(-.02f,.03f),path);Assert.That(bounds.max.y,Is.LessThan(1.8f),path);
                    Assert.That(renderer.sharedMaterials,Is.All.Not.Null);
                    var mesh=instance.GetComponentInChildren<MeshFilter>().sharedMesh;long indices=0;
                    for(int i=0;i<mesh.subMeshCount;i++)indices+=mesh.GetIndexCount(i);
                    Assert.That(indices/3,Is.LessThan(10000),path);
                }
                finally{Object.DestroyImmediate(instance);}
            }
        }
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
