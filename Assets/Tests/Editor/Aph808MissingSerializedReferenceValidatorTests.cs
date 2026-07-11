namespace Game.Tests.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    public sealed class Aph808MissingSerializedReferenceValidatorTests
    {
        private static readonly string[] ExpectedBuildScenes =
        {
            "Assets/Game/Scenes/Match.unity",
            "Assets/Game/Scenes/Menu.unity"
        };

        [Test]
        public void ResolveTargetPaths_IsDeterministicAndIncludesOnlyExplicitScopes()
        {
            string[] prefabs =
            {
                "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab",
                "Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab"
            };

            IReadOnlyList<string> result = Aph808MissingSerializedReferenceValidator.ResolveTargetPaths(
                ExpectedBuildScenes.Reverse(),
                prefabs.Reverse());

            CollectionAssert.AreEqual(
                ExpectedBuildScenes.Concat(prefabs.OrderBy(path => path, StringComparer.Ordinal)).ToArray(),
                result);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void ResolveTargetPaths_FailsClosedUnlessExactlyTwoBuildScenesAreEnabled(int sceneCount)
        {
            string[] scenes = ExpectedBuildScenes.Take(sceneCount).Concat(
                sceneCount > ExpectedBuildScenes.Length
                    ? new[] { "Assets/Game/Scenes/Unexpected.unity" }
                    : Array.Empty<string>()).ToArray();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                Aph808MissingSerializedReferenceValidator.ResolveTargetPaths(
                    scenes,
                    new[] { "Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab" }));

            StringAssert.Contains("exactly 2 enabled build scenes", exception.Message);
        }

        [TestCase("Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab")]
        [TestCase("Assets/Game/Prefabs/UI/Shell/Content/Nested/Unexpected.prefab")]
        [TestCase("Assets/Game/Prefabs/UI/Shell/Content/NotAPrefab.asset")]
        public void ResolveTargetPaths_RejectsTargetsOutsideRuntimeContentPrefabScope(string path)
        {
            Assert.Throws<InvalidOperationException>(() =>
                Aph808MissingSerializedReferenceValidator.ResolveTargetPaths(
                    ExpectedBuildScenes,
                    new[] { path }));
        }

        [Test]
        public void BrokenObjectReferenceRequiresNullObjectAndNonzeroInstanceId()
        {
            Assert.That(
                Aph808MissingSerializedReferenceValidator.IsBrokenObjectReference(
                    null,
                    EntityId.FromULong(173)),
                Is.True);
            Assert.That(
                Aph808MissingSerializedReferenceValidator.IsBrokenObjectReference(null, EntityId.None),
                Is.False);
        }

        [Test]
        public void SceneSetupRestorationRequiresLoadedActiveScene()
        {
            Assert.IsFalse(Aph808MissingSerializedReferenceValidator.HasRestorableSceneSetup(Array.Empty<SceneSetup>()));
            Assert.IsFalse(Aph808MissingSerializedReferenceValidator.HasRestorableSceneSetup(new[]
            {
                new SceneSetup { path = string.Empty, isLoaded = false, isActive = false }
            }));
            Assert.IsTrue(Aph808MissingSerializedReferenceValidator.HasRestorableSceneSetup(new[]
            {
                new SceneSetup { path = "Assets/Game/Scenes/Menu.unity", isLoaded = true, isActive = true }
            }));
        }

        [Test]
        public void FailureMessageListsEveryIssueWithoutDiscardingDiagnostics()
        {
            string message = Aph808MissingSerializedReferenceValidator.BuildFailureMessage(new[]
            {
                "Assets/Game/Scenes/Menu.unity :: Root[0]: missing script at component index 2",
                "Assets/Game/Scenes/Match.unity :: Camera[0] :: View.target: broken object reference"
            });

            StringAssert.Contains("2 missing serialized reference issue(s)", message);
            StringAssert.Contains("Menu.unity", message);
            StringAssert.Contains("Match.unity", message);
        }

        [Test]
        public void ValidatorSourceDoesNotUseHierarchyOrObjectLookupFallbacks()
        {
            const string sourcePath =
                "Assets/Game/Scripts/Editor/Aph808MissingSerializedReferenceValidator.cs";
            string source = File.ReadAllText(sourcePath);

            StringAssert.DoesNotContain("transform.Find(", source);
            StringAssert.DoesNotContain("GameObject.Find(", source);
            StringAssert.DoesNotContain("FindObjectOfType", source);
            StringAssert.DoesNotContain("FindFirstObjectByType", source);
            StringAssert.Contains("EditorBuildSettings.scenes", source);
            StringAssert.Contains("AssetDatabase.FindAssets", source);
        }
    }
}
