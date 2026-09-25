#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishSquadPaginationTestRunner
    {
        private static TestRunnerApi _api;
        private static Callbacks _callbacks;

        public static void RunFocusedValidation()
        {
            string fixture = Environment.GetEnvironmentVariable("WARLINE_PAGINATION_TEST_FIXTURE");
            if (string.IsNullOrEmpty(fixture))
                fixture = "Game.Tests.Editor.SkirmishExpandedArmyTests";
            string output = Environment.GetEnvironmentVariable("WARLINE_PAGINATION_TEST_XML");
            if (string.IsNullOrEmpty(output))
                output = "/private/tmp/skirmish-pagination-tests.xml";
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new Callbacks(output);
            _api.RegisterCallbacks(_callbacks);
            _api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                testNames = new[] { fixture }
            }));
        }

        private sealed class Callbacks : ICallbacks
        {
            private readonly string _output;

            public Callbacks(string output) => _output = output;
            public void RunStarted(ITestAdaptor testsToRun) =>
                Debug.Log("[SkirmishSquadPaginationTests] started=" + testsToRun.FullName);
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                    Debug.LogError("[SkirmishSquadPaginationTests] failed=" + result.FullName + " " + result.Message);
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_output));
                File.WriteAllText(_output, result.ToXml().OuterXml);
                bool passed = result.TestStatus == TestStatus.Passed && result.PassCount > 0 && result.FailCount == 0;
                Debug.Log("[SkirmishSquadPaginationTests] result=" + (passed ? "Passed" : "Failed") +
                          " passed=" + result.PassCount + " failed=" + result.FailCount +
                          " skipped=" + result.SkipCount + " xml=" + _output);
                EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }
}
#endif
