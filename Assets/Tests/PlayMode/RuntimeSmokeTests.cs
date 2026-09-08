using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ThrownTogether.Tests
{
    public class RuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator PlayModeAdvancesFramesAndDestroysTemporaryObjects()
        {
            Assert.That(Application.isPlaying, Is.True);
            var temporary = new GameObject("SmokeTest_Temporary");
            try
            {
                var frame = Time.frameCount;
                yield return null;
                Assert.That(Time.frameCount, Is.GreaterThan(frame));
                Object.Destroy(temporary);
                yield return null;
                Assert.That(temporary == null, Is.True);
            }
            finally
            {
                if (temporary != null)
                    Object.DestroyImmediate(temporary);
            }
        }
    }
}
