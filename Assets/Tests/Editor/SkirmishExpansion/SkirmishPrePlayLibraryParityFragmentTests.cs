using System;
using System.IO;
using System.Reflection;
using Game.Configs;
using Game.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishPrePlayLibraryParityFragmentTests
    {
        public static void RunFocusedValidation()
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            // Existing loaded semantics are reused, not reimplemented by an offline validator.
            global::SkirmishSessionTests.RunCodedLibraryRawLaunchParityControls();
            var actual = SkirmishBattleCatalogConfig.Load();
            var authored = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(actual); Assert.IsNotNull(authored);
            Assert.IsTrue(authored.TryGetDefinition("S002", out var definition));
            var method = typeof(SkirmishS002AriaRunHarness).GetMethod("MeasurePrePlayLibraryParityFragment",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            JObject Invoke(string id, string d, int v) => (JObject)method.Invoke(null, new object[] { actual, id, d, v });
            var measured = Invoke("S002", definition.DefinitionId, definition.ContentVersion);
            Assert.AreEqual("MeasuredLibraryParityFragment", (string)measured["result"]);
            Assert.AreEqual(7, (int)measured["controls"]);
            Assert.IsFalse((bool)measured["live_api_admitted"]);
            Assert.IsFalse((bool)measured["qualified"]);
            Assert.IsFalse((bool)measured["queue_success_enabled"]);
            Assert.IsFalse((bool)measured["mission_ready"]);
            Assert.AreEqual(0, (int)measured["sampling_coverage_credit"]);
            void Refuses(TestDelegate action, string expectedMessage)
            {
                var outer = Assert.Throws<TargetInvocationException>(action);
                Assert.IsNotNull(outer.InnerException);
                Assert.IsTypeOf<InvalidDataException>(outer.InnerException);
                Assert.AreEqual(expectedMessage, outer.InnerException.Message);
            }
            Refuses(() => Invoke("S002", "skirmish.foreign", definition.ContentVersion),
                "Actual UI selection differs from exact requested definition/version.");
            Refuses(() => Invoke("S002", definition.DefinitionId, definition.ContentVersion == 1 ? 2 : 1),
                "Actual UI selection differs from exact requested definition/version.");
            Refuses(() => Invoke("S003", "skirmish.s003", 1),
                "Named S002/S005 actual catalog fragment only.");
            // S005 positive scope is ONLY the actual existing post-projection native hook.
            // No fabricated S005 catalog, no SelectScenario/PrepareScenarioAdjustment/LaunchMatch calls.
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            Debug.Log("[PrePlayLibraryParityFragmentTests] result=Passed existingParityControls=11 fragmentControls=7 s005ActualProjectionStillRequired=1 liveApiAdmitted=0");
        }
    }
}
