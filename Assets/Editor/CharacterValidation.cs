using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ThrownTogether.Editor
{
    [InitializeOnLoad]
    public static class CharacterValidation
    {
        private static TestRunnerApi api;
        static CharacterValidation()
        {
            api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results());
        }
        public static void Run(bool playMode)
        {
            Directory.CreateDirectory("TestResults");
            SessionState.SetString("CharacterValidationOutput",playMode ? "TestResults/characters-play.xml":"TestResults/characters-edit.xml");
            api.Execute(new ExecutionSettings(new Filter {testMode=playMode ? TestMode.PlayMode:TestMode.EditMode}));
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) {}
            public void TestStarted(ITestAdaptor test) {}
            public void TestFinished(ITestResultAdaptor result) {}
            public void RunFinished(ITestResultAdaptor result)
            {
                string path=SessionState.GetString("CharacterValidationOutput","");if(string.IsNullOrEmpty(path)) return;
                TestRunnerApi.SaveResultToFile(result,path);SessionState.EraseString("CharacterValidationOutput");
                Debug.Log("Character validation: "+result.PassCount+" passed, "+result.FailCount+" failed. "+path);
            }
        }
    }
}
