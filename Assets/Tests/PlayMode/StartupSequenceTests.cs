using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class StartupSequenceTests
    {
        [Test] public void StudioFadeAndSkipAreBoundedAndDuplicateSafe()
        {
            var root=new GameObject("Startup test");root.SetActive(false);
            var group=root.AddComponent<CanvasGroup>();var flow=root.AddComponent<StartupSequence>();
            flow.artwork=group;flow.studioIntro=true;flow.enabled=false;root.SetActive(true);
            try
            {
                Assert.That(StartupSequence.BlocksMenu,Is.True);Assert.That(group.alpha,Is.Zero);
                flow.Advance(.25f);Assert.That(group.alpha,Is.EqualTo(.5f).Within(.001f));
                flow.Advance(.25f);Assert.That(group.alpha,Is.EqualTo(1));
                flow.Advance(StartupSequence.Hold);Assert.That(group.alpha,Is.EqualTo(1));
                Assert.That(flow.RequestStart(),Is.True);Assert.That(flow.RequestStart(),Is.False);
                flow.Advance(.375f);Assert.That(group.alpha,Is.EqualTo(.5f).Within(.001f));
                flow.Advance(.375f);Assert.That(group.alpha,Is.Zero);
            }
            finally {Object.DestroyImmediate(root);}
            Assert.That(StartupSequence.BlocksMenu,Is.False);
        }
        [Test] public void EarlyStartFadesFromCurrentOpacityWithoutFlashing()
        {
            var root=new GameObject("Title test");root.SetActive(false);var group=root.AddComponent<CanvasGroup>();
            var flow=root.AddComponent<StartupSequence>();flow.artwork=group;flow.enabled=false;root.SetActive(true);
            try
            {
                flow.Advance(.25f);float before=group.alpha;Assert.That(flow.RequestStart(),Is.True);
                flow.Advance(0);Assert.That(group.alpha,Is.EqualTo(before));flow.Advance(.75f);Assert.That(group.alpha,Is.Zero);
            }
            finally {Object.DestroyImmediate(root);}
        }
    }
}
