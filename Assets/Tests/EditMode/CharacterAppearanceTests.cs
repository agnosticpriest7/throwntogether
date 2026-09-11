using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ThrownTogether.Tests
{
    public sealed class CharacterAppearanceTests
    {
        private GameObject instance;
        private ChefAppearance visual;
        [SetUp] public void SetUp()
        {
            instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VerticalSlice/Chef.prefab"));
            visual=instance.GetComponentInChildren<ChefAppearance>();
        }
        [TearDown] public void TearDown() {Object.DestroyImmediate(instance);}
        [Test] public void AllBuildsAndClothesPreserveMotorAndCarryAnchor()
        {
            var chef=instance.GetComponent<ChefController>(); var motor=instance.GetComponent<CharacterController>();
            float radius=motor.radius,height=motor.height,speed=chef.speed,reach=chef.reach;
            Vector3 anchor=chef.Hands.transform.localPosition;
            for(int build=0;build<3;build++) for(int clothing=0;clothing<ChefWardrobe.Clothes.Length;clothing++)
            {
                var a=ChefAppearanceData.Example(0);a.build=build;a.clothing=clothing;visual.Apply(a);visual.Pose(1,18,1);
                Assert.That(visual.IsVisible("C_Torso"+build),Is.True);
                Assert.That(visual.IsVisible("C_Bib"+build),Is.EqualTo(clothing==2));
                Assert.That(visual.IsVisible("C_Waist"+build),Is.EqualTo(clothing==1));
                Assert.That(motor.radius,Is.EqualTo(radius));Assert.That(motor.height,Is.EqualTo(height));
                Assert.That(chef.speed,Is.EqualTo(speed));Assert.That(chef.reach,Is.EqualTo(reach));
                Assert.That(chef.Hands.transform.localPosition,Is.EqualTo(anchor));
                Assert.That(instance.transform.localScale,Is.EqualTo(Vector3.one));
                Assert.That(visual.GetComponentsInChildren<Collider>(true),Is.Empty);
            }
        }
        [Test] public void EveryEyeAndMouthCombinationKeepsIndependentIdentity()
        {
            for(int eyes=0;eyes<ChefWardrobe.Eyes.Length;eyes++) for(int mouth=0;mouth<ChefWardrobe.Mouths.Length;mouth++)
            {
                var a=ChefAppearanceData.Example(1);a.eyes=eyes;a.mouth=mouth;visual.Apply(a);
                for(int i=0;i<ChefWardrobe.Eyes.Length;i++)
                {Assert.That(visual.IsVisible("C_Eyes"+i),Is.EqualTo(i==eyes));Assert.That(visual.IsVisible("C_Mouth"+i),Is.EqualTo(i==mouth));}
                Assert.That(visual.IsVisible("C_Tooth3"),Is.EqualTo(mouth==3));
                Assert.That(visual.IsVisible("C_GlassesBridge"),Is.True);
            }
        }
        [Test] public void HeadwearHidesHairWithoutErasingTheChoice()
        {
            var a=ChefAppearanceData.Example(3);
            for(int hat=0;hat<4;hat++)
            {
                a.headwear=hat;visual.Apply(a);
                Assert.That(visual.IsVisible("C_Hair0"),Is.EqualTo(hat==0));Assert.That(a.hair,Is.EqualTo(1));
                Assert.That(visual.IsVisible("C_Cap"),Is.EqualTo(hat==1));
                Assert.That(visual.IsVisible("C_Beanie"),Is.EqualTo(hat==2));
                Assert.That(visual.IsVisible("C_Headband"),Is.EqualTo(hat==3));
            }
            a.headwear=0;a.glasses=0;a.clothing=0;visual.Apply(a);
            Assert.That(visual.IsVisible("C_Hair0"),Is.True);Assert.That(visual.IsVisible("C_GlassesBridge"),Is.False);
        }
        [Test] public void ExtraHairAndCivilianLooksAreIndependentFromPlayerChoices()
        {
            var before=JsonUtility.ToJson(ChefWardrobe.ForPlayer(0));
            for(int hair=2;hair<ChefWardrobe.Hair.Length;hair++)
            {
                var a=new ChefAppearanceData{hair=hair,headwear=0};visual.Apply(a);
                Assert.That(visual.GetComponentsInChildren<Renderer>().Any(r=>r.name.StartsWith("C_HairStyle"+hair+"_")),Is.True);
                a.headwear=1;visual.Apply(a);Assert.That(visual.GetComponentsInChildren<Renderer>().Any(r=>r.name.StartsWith("C_Hair")),Is.False);
            }
            var signatures=new System.Collections.Generic.HashSet<string>();
            for(int seed=0;seed<20;seed++)
            {
                CustomerPresentation.ApplyCustomerLook(visual,seed);signatures.Add(JsonUtility.ToJson(visual.appearance));
                Assert.That(visual.appearance.clothing,Is.Zero);Assert.That(visual.appearance.headwear,Is.Zero);
                Assert.That(visual.GetComponentsInChildren<Renderer>().Any(r=>r.name.StartsWith("C_Customer")),Is.True);
                var expected=JsonUtility.ToJson(visual.appearance);CustomerPresentation.ApplyCustomerLook(visual,seed);Assert.That(JsonUtility.ToJson(visual.appearance),Is.EqualTo(expected));
            }
            Assert.That(signatures.Count,Is.GreaterThan(15));Assert.That(JsonUtility.ToJson(ChefWardrobe.ForPlayer(0)),Is.EqualTo(before));
            visual.Apply(ChefWardrobe.ForPlayer(0));Assert.That(visual.GetComponentsInChildren<Renderer>().Any(r=>r.name.StartsWith("C_Customer")),Is.False);
        }
        [Test] public void ImportedFacesContainTrianglesAndUseSupportedShaders()
        {
            foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Assert.That(r.sharedMesh,Is.Not.Null,r.name);Assert.That(r.sharedMesh.vertexCount,Is.GreaterThan(0),r.name);
                Assert.That(r.sharedMaterial.shader.isSupported,Is.True,r.name);
                if(r.name.StartsWith("C_Eyes") || r.name.StartsWith("C_Mouth")) Assert.That(r.sharedMesh.GetIndexCount(0),Is.GreaterThan(6),r.name);
            }
        }
        [Test] public void CarryPoseMovesHandsForwardWithoutMovingHeadOrApron()
        {
            visual.Apply(ChefAppearanceData.Example(0));
            var arm=visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="C_Arm_L");
            var mesh=new Mesh();arm.BakeMesh(mesh);Vector3 before=arm.transform.TransformPoint(mesh.bounds.center);
            visual.Pose(1,0,1);arm.BakeMesh(mesh);Vector3 after=arm.transform.TransformPoint(mesh.bounds.center);
            Object.DestroyImmediate(mesh);
            Assert.That(after.z-before.z,Is.GreaterThan(.15f),"Arms should reach toward the existing +Z carry slot");
            Assert.That(after.y,Is.GreaterThan(before.y));
        }
    }
}
