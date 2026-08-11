using System;
using System.IO;
using Game.Authoring;
using Game.Components;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class GridAuthoringRuntimeDebugBindingTests
    {
        public static void RunFocusedValidation()
        {
            try
            {
                var tests = new GridAuthoringRuntimeDebugBindingTests();
                tests.RuntimeDebugBinding_UsesExplicitWorldAndIsolatesReplacement();
                tests.RuntimeDebugBoundary_IsEditorOnlyCachedAndReadOnly();
                tests.Composition_PassesExistingLifecycleWorldOnce();
                Debug.Log("[GridAuthoringRuntimeDebugBindingValidation] result=Passed tests=3");
                ValidationExit.Passed();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[GridAuthoringRuntimeDebugBindingValidation] result=Failed");
                ValidationExit.Failed();
            }
        }

        [Test]
        public void RuntimeDebugBinding_UsesExplicitWorldAndIsolatesReplacement()
        {
            World previousDefault = World.DefaultGameObjectInjectionWorld;
            World unrelatedDefault = new("GridAuthoring unrelated default");
            World first = new("GridAuthoring first explicit");
            World replacement = new("GridAuthoring replacement explicit");
            GameObject gameObject = new("GridAuthoring runtime-debug binding test");
            try
            {
                GridAuthoring authoring = gameObject.AddComponent<GridAuthoring>();
                CreateGrid(first.EntityManager, width: 11);
                CreateGrid(replacement.EntityManager, width: 22);
                CreateGrid(unrelatedDefault.EntityManager, width: 99);
                World.DefaultGameObjectInjectionWorld = unrelatedDefault;

                Assert.That(authoring.TryGetRuntimeDebugGridConfig(out _), Is.False);

                authoring.BindRuntimeDebugSources(runtimeGridBlockers: null, first);
                Assert.That(authoring.TryGetRuntimeDebugGridConfig(out GridConfig firstGrid), Is.True);
                Assert.That(firstGrid.Width, Is.EqualTo(11));

                authoring.BindRuntimeDebugSources(runtimeGridBlockers: null, replacement);
                Assert.That(authoring.TryGetRuntimeDebugGridConfig(out GridConfig replacementGrid), Is.True);
                Assert.That(replacementGrid.Width, Is.EqualTo(22));

                gameObject.SendMessage("OnDisable");
                Assert.That(authoring.TryGetRuntimeDebugGridConfig(out _), Is.False);

                authoring.BindRuntimeDebugSources(runtimeGridBlockers: null, first);
                first.Dispose();
                Assert.DoesNotThrow(() => gameObject.SendMessage("OnDisable"));
                Assert.That(authoring.TryGetRuntimeDebugGridConfig(out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                World.DefaultGameObjectInjectionWorld = previousDefault;
                if (first.IsCreated)
                    first.Dispose();
                if (replacement.IsCreated)
                    replacement.Dispose();
                if (unrelatedDefault.IsCreated)
                    unrelatedDefault.Dispose();
            }
        }

        [Test]
        public void RuntimeDebugBoundary_IsEditorOnlyCachedAndReadOnly()
        {
            string source = ReadProjectSource("Assets/Game/Scripts/Authorings/GridAuthoring.cs");
            StringAssert.DoesNotContain("DefaultGameObjectInjectionWorld", source);
            Assert.That(CountOccurrences(source, "CreateEntityQuery("), Is.EqualTo(6));
            StringAssert.Contains("public void BindRuntimeDebugSources(", source);
            StringAssert.Contains("private void OnDisable()", source);
            StringAssert.Contains("ClearRuntimeDebugSources();", source);

            int bindingStart = source.IndexOf("#if UNITY_EDITOR\n        public void BindRuntimeDebugSources(", StringComparison.Ordinal);
            int bindingEnd = source.IndexOf("#endif", bindingStart, StringComparison.Ordinal);
            int gizmoStart = source.IndexOf("#if UNITY_EDITOR\n        private void OnDisable()", StringComparison.Ordinal);
            int gizmoEnd = source.LastIndexOf("#endif", StringComparison.Ordinal);
            Assert.That(bindingStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(bindingEnd, Is.GreaterThan(bindingStart));
            Assert.That(gizmoStart, Is.GreaterThan(bindingEnd));
            Assert.That(gizmoEnd, Is.GreaterThan(gizmoStart));

            string editorRuntimeDebugSource =
                source.Substring(bindingStart, bindingEnd - bindingStart) +
                source.Substring(gizmoStart, gizmoEnd - gizmoStart);
            StringAssert.DoesNotContain("GameObject.Find", editorRuntimeDebugSource);
            StringAssert.DoesNotContain("void Update(", editorRuntimeDebugSource);
            StringAssert.DoesNotContain("void LateUpdate(", editorRuntimeDebugSource);
            StringAssert.DoesNotContain("StartCoroutine", editorRuntimeDebugSource);
            StringAssert.DoesNotContain("SetComponentData", editorRuntimeDebugSource);
            StringAssert.DoesNotContain("AddComponent", editorRuntimeDebugSource);
            StringAssert.DoesNotContain("DestroyEntity", editorRuntimeDebugSource);
        }

        [Test]
        public void Composition_PassesExistingLifecycleWorldOnce()
        {
            string sceneBindingSource = ReadProjectSource(
                "Assets/Game/Scripts/Composition/GameplaySceneBindingSceneSystemHelper.cs");
            string startupSource = ReadProjectSource(
                "Assets/Game/Scripts/Composition/GameplayFeatureStartupCompositionSystemHelper.cs");

            StringAssert.Contains("#if UNITY_EDITOR", sceneBindingSource);
            StringAssert.Contains("World runtimeWorld", sceneBindingSource);
            StringAssert.Contains(
                "grid.BindRuntimeDebugSources(runtimeGridBlockers, runtimeWorld);",
                sceneBindingSource);
            StringAssert.Contains("#if UNITY_EDITOR", startupSource);
            StringAssert.Contains(
                "buildingRuntimeCitySpawnContext.TryGetEntityManager(",
                startupSource);
            StringAssert.Contains("runtimeEntityManager.World", startupSource);
            Assert.That(
                CountOccurrences(startupSource, "BindRuntimeGridBlockerDebugViews("),
                Is.EqualTo(1));
        }

        private static void CreateGrid(EntityManager entityManager, int width)
        {
            Entity gridEntity = entityManager.CreateEntity(typeof(GridConfig));
            entityManager.SetComponentData(gridEntity, new GridConfig
            {
                Width = width,
                Height = 4,
                CellSize = 1f
            });
            entityManager.AddBuffer<GridRoad>(gridEntity);
        }

        private static string ReadProjectSource(string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return File.ReadAllText(Path.Combine(projectRoot, relativePath)).Replace("\r\n", "\n");
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
