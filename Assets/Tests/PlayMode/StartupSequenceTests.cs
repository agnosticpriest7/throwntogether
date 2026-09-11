using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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
        [Test] public void PublisherFollowsStudioAtBlackAndGetsItsOwnThreeSecondHold()
        {
            var root=new GameObject("Two logo test");root.SetActive(false);
            var group=root.AddComponent<CanvasGroup>();var flow=root.AddComponent<StartupSequence>();
            var image=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));image.transform.SetParent(root.transform);
            var logo=new Texture2D(4,3);
            flow.artwork=group;flow.publisherLogo=logo;flow.studioIntro=true;flow.enabled=false;root.SetActive(true);
            try
            {
                Assert.That(StartupSequence.Hold,Is.EqualTo(3));
                flow.Advance(StartupSequence.FadeIn);flow.Advance(3);
                Assert.That(group.alpha,Is.EqualTo(1));Assert.That(flow.TryShowPublisher(),Is.False);
                flow.RequestStart();flow.Advance(StartupSequence.FadeOut/2);
                Assert.That(flow.TryShowPublisher(),Is.False);
                flow.Advance(StartupSequence.FadeOut/2);Assert.That(flow.TryShowPublisher(),Is.True);
                Assert.That(group.alpha,Is.Zero);Assert.That(flow.Elapsed,Is.Zero);
                Assert.That(image.GetComponent<RawImage>().texture,Is.SameAs(logo));
                Assert.That(image.GetComponent<AspectRatioFitter>().aspectRatio,Is.EqualTo(4f/3));
                flow.Advance(StartupSequence.FadeIn);flow.Advance(3);Assert.That(group.alpha,Is.EqualTo(1));
                flow.RequestStart();flow.Advance(StartupSequence.FadeOut);
                Assert.That(flow.TryShowPublisher(),Is.False);Assert.That(group.alpha,Is.Zero);
            }
            finally {Object.DestroyImmediate(root);Object.DestroyImmediate(logo);}
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
