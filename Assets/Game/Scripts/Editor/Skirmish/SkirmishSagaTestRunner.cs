#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishSagaTestRunner
    {
        private static TestRunnerApi api;
        public static void Run()
        {
            AriaSkirmishPlanValidation.RunFocusedValidation();
            SkirmishSetupPrefabBuilder.Build();
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
            string focus=Environment.GetEnvironmentVariable("WARLINE_SAGA_TEST_FOCUS");
            var names = new[] { "Game.Tests.Editor.SkirmishS002AriaHarnessTests.AssaultColumnDestroysEnemyBarracksAndPublishesVictory", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.DeadEnemyBarracksFinishesVictoryWithoutRulesSystem", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.DeadlineDrawPublishesFinishedMatchWithoutStampingVictory", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.FinishedMatchRetainsActorsAndSetupUntilExplicitCleanup", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.MissingBarracksPrefabStillAnchorsTheMeasuredBaseForPresentedOrders", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.PresentedColumnDestroysEnemyBaseBeforeDeadlineOnLiveSeeds", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.PresentedSelectionOrdersStructureColumnAndDestroysEnemyBase", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.RosterProjectionOnUpdateAppliesStructureDamageWithoutIterating", "Game.Tests.Editor.SkirmishS002AriaHarnessTests.PrefabFreeLedgerFixtureCannotClaimNativeReadiness", "Game.Tests.Editor.SkirmishMedalLedgerTests", "Game.Tests.Editor.SkirmishLibraryViewModelTests", "Game.Tests.Editor.SkirmishLibrary.SkirmishLibraryWidgetTests", "Game.Tests.Editor.SkirmishLibraryJourneyTests", "Game.Tests.Editor.SkirmishSagaMenuTests", "SkirmishSetupScreenTests" };
            api.Execute(new ExecutionSettings(new Filter { testMode=TestMode.EditMode, testNames=string.IsNullOrEmpty(focus)?names:new[]{focus} }));
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                    Debug.LogError("[SkirmishSagaTests] failed=" + result.FullName + " " + result.Message);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/tests");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "results.xml"), result.ToXml().OuterXml);
                bool pass = result.TestStatus == TestStatus.Passed && result.PassCount > 0 && result.FailCount == 0;
                Debug.Log("[SkirmishSagaTests] result=" + (pass ? "Passed" : "Failed") + " passed=" + result.PassCount + " failed=" + result.FailCount + " skipped=" + result.SkipCount);
                EditorApplication.delayCall += () => EditorApplication.Exit(pass ? 0 : 1);
            }
        }
    }
}
#endif

