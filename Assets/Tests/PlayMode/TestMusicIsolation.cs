using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly:TestRunCallback(typeof(ThrownTogether.Tests.TestMusicIsolation))]
namespace ThrownTogether.Tests
{
    // Tests replace camera/listener roots; background entertainment is not their subject.
    public sealed class TestMusicIsolation : ITestRunCallback
    {
        private BackgroundMusic[] suspended;
        public void RunStarted(ITest tests)
        {
            BackgroundMusic.AutomaticPlaybackEnabled=false;
            suspended=Object.FindObjectsByType<BackgroundMusic>(FindObjectsSortMode.None);
            foreach(var music in suspended) music.gameObject.SetActive(false);
        }
        public void RunFinished(ITestResult result)
        {
            BackgroundMusic.AutomaticPlaybackEnabled=true;
            if(suspended!=null) foreach(var music in suspended) if(music!=null) music.gameObject.SetActive(true);
        }
        public void TestStarted(ITest test) {}
        public void TestFinished(ITestResult result) {}
    }
}
