using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using RoadVisualType = Game.Runtime.RoadNetworkCompositionSystemHelper.RoadVisualType;
using CombinedRoadVisualData = Game.Runtime.RoadGridProjectionSystem.CombinedRoadVisualData;

namespace Game.Tests.Editor
{
    public sealed class RoadPreviewPresentationSystemHelperTests
    {
        private static readonly MethodInfo GetPreviewObjectMethod = typeof(RoadPreviewPresentationSystemHelper)
            .GetMethod("GetPreviewObject", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PreviewObjectsField = typeof(RoadPreviewPresentationSystemHelper)
            .GetField("_previewObjects", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void RunFocusedValidation()
        {
            try
            {
                RoadPreviewPresentationSystemHelperTests tests = new();
                tests.Constructor_RequiresNonNegativeExactCapacity();
                tests.Pool_ReusesByTypeAndDestroysReleaseAfterTotalCapacityExhaustion();
                tests.Dispose_DestroysActivePooledObjectsAndOwnedMaterialCopies();
                tests.Structure_HasOneStandardLifecycleOwnerAndExistingCompositionTeardown();
                Debug.Log("[RoadPreviewPresentationPoolValidation] result=Passed tests=4");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[RoadPreviewPresentationPoolValidation] result=Failed");
                throw;
            }
        }

        [Test]
        public void Constructor_RequiresNonNegativeExactCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoadPreviewPresentationSystemHelper(-1));
            using RoadPreviewPresentationSystemHelper defaultHelper = new();
            using RoadPreviewPresentationSystemHelper boundedHelper = new(2);
            Assert.AreEqual(RoadPreviewPresentationSystemHelper.DefaultPoolCapacity, defaultHelper.PoolCapacity);
            Assert.AreEqual(2, boundedHelper.PoolCapacity);
        }

        [Test]
        public void Pool_ReusesByTypeAndDestroysReleaseAfterTotalCapacityExhaustion()
        {
            using Fixture fixture = new(poolCapacity: 2);

            GameObject firstEnd = fixture.Acquire(RoadVisualType.End);
            fixture.TrackActive(firstEnd);
            fixture.Helper.ClearPreview();
            Assert.AreEqual(1, fixture.Helper.PooledObjectCount);

            GameObject reusedEnd = fixture.Acquire(RoadVisualType.End);
            Assert.AreSame(firstEnd, reusedEnd);
            GameObject firstStraight = fixture.Acquire(RoadVisualType.Straight);
            fixture.TrackActive(reusedEnd);
            fixture.TrackActive(firstStraight);
            fixture.Helper.ClearPreview();
            Assert.AreEqual(2, fixture.Helper.PooledObjectCount);

            GameObject activeEnd = fixture.Acquire(RoadVisualType.End);
            GameObject activeStraight = fixture.Acquire(RoadVisualType.Straight);
            GameObject overflowEnd = fixture.Acquire(RoadVisualType.End);
            fixture.TrackActive(activeEnd);
            fixture.TrackActive(activeStraight);
            fixture.TrackActive(overflowEnd);
            fixture.Helper.ClearPreview();

            Assert.AreEqual(2, fixture.Helper.PoolCapacity);
            Assert.AreEqual(2, fixture.Helper.PooledObjectCount);
            Assert.AreEqual(2, fixture.Helper.RetainedObjectCount);
            Assert.AreEqual(3, fixture.Helper.CreatedObjectCount);
            Assert.AreEqual(1, fixture.Helper.DestroyedObjectCount);
            Assert.IsTrue(overflowEnd == null, "The release beyond capacity must be destroyed instead of retained.");
        }

        [Test]
        public void Dispose_DestroysActivePooledObjectsAndOwnedMaterialCopies()
        {
            using Fixture fixture = new(poolCapacity: 2);
            GameObject pooled = fixture.Acquire(RoadVisualType.End);
            Material pooledCopy = pooled.GetComponent<Renderer>().sharedMaterial;
            fixture.TrackActive(pooled);
            fixture.Helper.ClearPreview();

            GameObject active = fixture.Acquire(RoadVisualType.Straight);
            Material activeCopy = active.GetComponent<Renderer>().sharedMaterial;
            fixture.TrackActive(active);
            fixture.Helper.DisposePreview();
            fixture.Helper.Dispose();

            Assert.IsTrue(fixture.Helper.IsDisposed);
            Assert.AreEqual(0, fixture.Helper.ActiveObjectCount);
            Assert.AreEqual(0, fixture.Helper.PooledObjectCount);
            Assert.AreEqual(0, fixture.Helper.RetainedObjectCount);
            Assert.AreEqual(fixture.Helper.CreatedObjectCount, fixture.Helper.DestroyedObjectCount);
            Assert.IsTrue(pooled == null);
            Assert.IsTrue(active == null);
            Assert.IsTrue(pooledCopy == null);
            Assert.IsTrue(activeCopy == null);
            Assert.AreEqual(0, fixture.Root.transform.childCount);
            Assert.NotNull(fixture.SourceMaterial, "Borrowed source material must remain owned by the fixture.");
        }

        [Test]
        public void Structure_HasOneStandardLifecycleOwnerAndExistingCompositionTeardown()
        {
            const string helperPath = "Assets/Game/Scripts/Systems/RoadPreviewPresentationSystemHelper.cs";
            const string ownerPath = "Assets/Game/Scripts/Systems/RoadBuildCompositionSourceCompositionSystemHelper.cs";
            const string disposalPath = "Assets/Game/Scripts/Systems/RoadBuildDisposalCompositionSystemHelper.cs";
            string helperSource = System.IO.File.ReadAllText(helperPath);
            string ownerSource = System.IO.File.ReadAllText(ownerPath);
            string disposalSource = System.IO.File.ReadAllText(disposalPath);

            StringAssert.Contains("RoadPreviewPresentationSystemHelper : IDisposable", helperSource);
            StringAssert.Contains("public void Dispose()", helperSource);
            StringAssert.Contains("public void DisposePreview()", helperSource);
            StringAssert.Contains("_pooledObjectCount >= _poolCapacity", helperSource);
            Assert.AreEqual(1, Count(ownerSource, "new RoadPreviewPresentationSystemHelper()"));
            Assert.AreEqual(1, Count(disposalSource, "context.PreviewSystem?.DisposePreview();"));
        }

        private static int Count(string text, string value)
        {
            int count = 0;
            int start = 0;
            while ((start = text.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
            {
                count++;
                start += value.Length;
            }

            return count;
        }

        private sealed class Fixture : IDisposable
        {
            public readonly RoadPreviewPresentationSystemHelper Helper;
            public readonly GameObject Root = new("RoadPreviewPoolFixtureRoot");
            public readonly Mesh Mesh = new();
            public readonly Material SourceMaterial;
            private readonly RoadPreviewPresentationSystemHelper.Context _context;

            public Fixture(int poolCapacity)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Assert.NotNull(shader);
                SourceMaterial = new Material(shader);
                CombinedRoadVisualData visualData = new()
                {
                    Mesh = Mesh,
                    Materials = new[] { SourceMaterial }
                };
                Dictionary<RoadVisualType, CombinedRoadVisualData> visualDataByType = new()
                {
                    [RoadVisualType.End] = visualData,
                    [RoadVisualType.Straight] = visualData
                };
                Helper = new RoadPreviewPresentationSystemHelper(poolCapacity);
                _context = new RoadPreviewPresentationSystemHelper.Context(
                    visualDataByType,
                    Root.transform,
                    Vector3.zero,
                    0f,
                    1f,
                    0.5f,
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            public GameObject Acquire(RoadVisualType type)
            {
                Assert.NotNull(GetPreviewObjectMethod);
                return (GameObject)GetPreviewObjectMethod.Invoke(Helper, new object[] { _context, type });
            }

            public void TrackActive(GameObject preview)
            {
                Assert.NotNull(PreviewObjectsField);
                ((List<GameObject>)PreviewObjectsField.GetValue(Helper)).Add(preview);
            }

            public void Dispose()
            {
                Helper.Dispose();
                if (Root != null)
                    UnityEngine.Object.DestroyImmediate(Root);
                if (Mesh != null)
                    UnityEngine.Object.DestroyImmediate(Mesh);
                if (SourceMaterial != null)
                    UnityEngine.Object.DestroyImmediate(SourceMaterial);
            }
        }
    }
}
