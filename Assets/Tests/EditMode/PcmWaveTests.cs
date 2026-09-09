using System.IO;
using NUnit.Framework;
namespace ThrownTogether.Tests
{
    public sealed class PcmWaveTests
    {
        [Test] public void EveryOriginalPlaceholderIsValidPcm16()
        {
            var paths=Directory.GetFiles("Assets/Audio/Placeholders","*.wav");
            Assert.That(paths.Length,Is.GreaterThan(0));
            foreach(var path in paths)
            {
                var data=PcmWave.Decode(File.ReadAllBytes(path),out int channels,out int rate);
                Assert.That(channels,Is.EqualTo(1)); Assert.That(rate,Is.EqualTo(22050));
                Assert.That(data.Length,Is.GreaterThan(0));
                foreach(float sample in data) Assert.That(sample,Is.InRange(-1f,1f));
            }
        }
        [Test] public void MalformedWaveFailsClearly()
        {
            Assert.Throws<InvalidDataException>(()=>PcmWave.Decode(new byte[16],out _,out _));
        }
    }
}
