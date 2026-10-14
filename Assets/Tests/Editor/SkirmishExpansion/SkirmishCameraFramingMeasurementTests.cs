using System;
using Game.Components;
using Game.Editor;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Synthetic API/geometry controls only. No real mission/readability acceptance.
    public sealed class SkirmishCameraFramingMeasurementTests
    {
        private GameObject cameraObject;
        private RenderTexture texture;
        private Camera camera;
        [SetUp] public void SetUp()
        {
            cameraObject = new GameObject("SyntheticFramingCamera"); camera = cameraObject.AddComponent<Camera>();
            texture = new RenderTexture(1024, 768, 0); camera.targetTexture = texture;
            camera.pixelRect = new Rect(80, 40, 800, 600); camera.aspect = 4f / 3f;
            camera.fieldOfView = 90; camera.nearClipPlane = .3f; camera.farClipPlane = 1000;
        }
        [TearDown] public void TearDown()
        { UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(texture); }

        [Test] public void PerspectiveProjectionMatchesIndependentAnalyticCubeAndPixelOffset()
        {
            Assert.IsTrue(SkirmishCameraFramingMeasurement.TryProject(camera, new Bounds(new Vector3(0, 0, 10), Vector3.one * 2), out var rect));
            // Vertical focal length = 600/(2*tan(90/2))=300. Closest depth9;
            // projected horizontal/vertical span = 2*300/9, actual pixelRect origin.
            Assert.AreEqual(600f / 9f, rect.width, .01f); Assert.AreEqual(600f / 9f, rect.height, .01f);
            Assert.AreEqual(480, rect.center.x, .01f); Assert.AreEqual(340, rect.center.y, .01f);
        }
        [Test] public void OrthographicProjectionAndAspectAreMeasuredWithoutPerspectiveAssumption()
        {
            camera.orthographic = true; camera.orthographicSize = 5;
            Assert.IsTrue(SkirmishCameraFramingMeasurement.TryProject(camera, new Bounds(new Vector3(0, 0, 10), Vector3.one * 2), out var rect));
            Assert.AreEqual(120, rect.height, .01f); Assert.AreEqual(120, rect.width, .01f);
        }
        [Test] public void NearPlaneBehindCameraFarPlaneNonfiniteAndDegenerateBoundsReject()
        {
            foreach (var bounds in new[] { new Bounds(Vector3.zero, Vector3.one * 2), new Bounds(new Vector3(0, 0, -10), Vector3.one),
                new Bounds(new Vector3(0, 0, 1001), Vector3.one), new Bounds(new Vector3(float.NaN, 0, 10), Vector3.one),
                new Bounds(new Vector3(0, 0, 10), Vector3.zero) })
                Assert.IsFalse(SkirmishCameraFramingMeasurement.TryProject(camera, bounds, out _));
        }
        [Test] public void MissingAuthoredConstraintsDoNotBecomeDefaultReadiness()
        { Assert.Contains("AuthoredFramingConstraintsMissingOrInvalid", SkirmishCameraFramingMeasurement.Evaluate(Sample(), default)); }
        [Test] public void NativeRoleDeterminesInfantryAndTransportPlaneCoverageChecks()
        {
            var sample = Sample(); var limits = SyntheticLimits();
            sample.NativeCategory = "Infantry"; sample.ProjectedPixels = new Rect(470, 335, 20, 10);
            Assert.Contains("InfantryBelowAuthoredPixelHeight", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
            limits.MinimumInfantryHeightPixels = 1; // Fraction still requires12px in this600px fixture.
            Assert.Contains("InfantryBelowAuthoredPixelHeight", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
            sample.CurrentVisual = sample.DesiredVisual = (byte)UnitRenderVisualKind.Low;
            Assert.Contains("InfantryRequiresFullOrMidMesh", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
            sample.CurrentVisual = sample.DesiredVisual = (byte)UnitRenderVisualKind.Detail;
            sample.NativeCategory = "Air"; sample.NativeRole = "TransportPlane"; sample.ProjectedPixels = new Rect(100, 100, 700, 300);
            Assert.Contains("TransportPlaneExceedsAuthoredCoverage", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
            sample.ProjectedPixels = new Rect(-10, 100, 100, 100);
            Assert.Contains("ActorBoundsOutsideSafeFrame", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
        }
        [Test] public void AuthoredHeightFovAndNativeLodTransitionCannotBeSilentlyAccepted()
        {
            var sample = Sample(); var limits = SyntheticLimits(); sample.CameraPosition.y = 51;
            Assert.Contains("CameraOutsideAuthoredZoomOrFov", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
            sample = Sample(); sample.FieldOfView = 100;
            Assert.Contains("CameraOutsideAuthoredZoomOrFov", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
            sample = Sample(); sample.CurrentVisual = (byte)UnitRenderVisualKind.Far;
            Assert.Contains("NativeMeshVisualNotSettled", SkirmishCameraFramingMeasurement.Evaluate(sample, limits));
        }
        [Test] public void CollectorReadsOwnedEntityActualActiveRootBoundsAndFarStateFails()
        {
            using var world = new World("SyntheticFramingWorld"); var em = world.EntityManager;
            var actor = em.CreateEntity(); var root = em.CreateEntity(); var mesh = em.CreateEntity();
            em.AddComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = "synthetic-attempt", StableObjectId = "synthetic-rifle" });
            em.AddComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Rifle, Category = SkirmishPopulationCategory.Infantry });
            em.AddComponentData(actor, LocalTransform.FromPosition(new float3(0, 0, 10)));
            em.AddComponentData(actor, new UnitRenderVisualComponent { Current = (byte)UnitRenderVisualKind.Detail, Desired = (byte)UnitRenderVisualKind.Detail });
            em.AddComponentData(actor, new UnitDetailedVisualReference { Root = root });
            em.AddBuffer<LinkedEntityGroup>(root).Add(new LinkedEntityGroup { Value = mesh });
            em.AddComponentData(mesh, new WorldRenderBounds { Value = new AABB { Center = new float3(0, 0, 10), Extents = new float3(1) } });
            var result = SkirmishCameraFramingMeasurement.Capture(em, actor, camera);
            Assert.IsTrue(result.Valid, result.Failure); Assert.AreEqual("synthetic-rifle", result.StableActorId); Assert.AreEqual(1, result.RenderBoundsCount);
            Assert.AreEqual(root.Index, result.ActiveRootIndex); Assert.AreEqual(root.Version, result.ActiveRootVersion);
            Assert.AreEqual(mesh.Index, result.RenderBounds[0].EntityIndex); Assert.AreEqual(mesh.Version, result.RenderBounds[0].EntityVersion);
            Assert.AreEqual(10, result.RenderBounds[0].WorldBounds.center.z); Assert.AreEqual(16, result.ProjectionColumnMajor.Length);
            Assert.AreEqual(16, result.WorldToCameraColumnMajor.Length); Assert.AreEqual(1024,result.TargetWidth); Assert.AreEqual(768,result.TargetHeight);
            Assert.AreEqual("UnparentedLocalTransform", result.ActorTransformSource);
            Assert.AreEqual(10, result.ActorPosition.z);
            em.AddComponentData(actor, new Parent { Value = root });
            result = SkirmishCameraFramingMeasurement.Capture(em, actor, camera);
            Assert.IsFalse(result.Valid); Assert.AreEqual("ParentedActorWorldPositionUnavailable", result.Failure);
            Assert.IsNull(result.ActorTransformSource, "Local coordinates cannot be described as world measurements.");
            em.RemoveComponent<Parent>(actor);
            em.SetComponentData(actor, new UnitRenderVisualComponent { Current = (byte)UnitRenderVisualKind.Far, Desired = (byte)UnitRenderVisualKind.Far });
            result = SkirmishCameraFramingMeasurement.Capture(em, actor, camera);
            Assert.IsFalse(result.Valid); Assert.AreEqual("NativeVisualIsFarImpostorOrUnknown", result.Failure);
        }
        private static SkirmishCameraActorMeasurement Sample() => new SkirmishCameraActorMeasurement { Valid = true,
            CameraPosition = new Vector3(0, 20, 0), FieldOfView = 60, CameraPixels = new Rect(80,40,800,600),
            ProjectedPixels = new Rect(400,300,40,40), CurrentVisual = 1, DesiredVisual = 1, NativeRole = "Rifle", NativeCategory = "Infantry" };
        private static SkirmishMissionCameraFramingConstraints SyntheticLimits() => new SkirmishMissionCameraFramingConstraints {
            SchemaVersion = 1, CatalogId = "S005", LayoutBindingSha256 = new string('a',64), MinimumHeight = 10, MaximumHeight = 50,
            MinimumFieldOfView = 30, MaximumFieldOfView = 90, MinimumInfantryHeightPixels = 20, MinimumInfantryHeightFraction = .02f,
            MaximumPlaneWidthFraction = .7f, MaximumPlaneHeightFraction = .7f, SafeEdgePixels = 10 };

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishCameraFramingMeasurementTests();
            foreach (Action check in new Action[] { suite.PerspectiveProjectionMatchesIndependentAnalyticCubeAndPixelOffset,
                suite.OrthographicProjectionAndAspectAreMeasuredWithoutPerspectiveAssumption,
                suite.NearPlaneBehindCameraFarPlaneNonfiniteAndDegenerateBoundsReject, suite.MissingAuthoredConstraintsDoNotBecomeDefaultReadiness,
                suite.NativeRoleDeterminesInfantryAndTransportPlaneCoverageChecks, suite.AuthoredHeightFovAndNativeLodTransitionCannotBeSilentlyAccepted,
                suite.CollectorReadsOwnedEntityActualActiveRootBoundsAndFarStateFails })
            { suite.SetUp(); try { check(); } finally { suite.TearDown(); } }
            Debug.Log("[SkirmishCameraFramingMeasurementTests] result=Passed tests=7 actualMissionCapture=0 readabilityCertified=0");
        }
    }
}
