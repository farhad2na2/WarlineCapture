using System;
using Game.Configs;
using Game.Runtime;
using Game.Editor;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishMissionViewProfileTests
    {
        private SkirmishMissionViewProfile Candidate()
        {
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
            return SkirmishMissionViewProfile.CreateS005EngineeringCandidate(definition);
        }
        [Test] public void EngineeringCandidateUsesActualDefinitionPinsWithoutMutatingSource()
        {
            var profile = Candidate();
            try
            {
                Assert.IsTrue(profile.IsValid); Assert.AreEqual(44, profile.NormalHeight); Assert.AreEqual(.017f, profile.InfantryMinimumViewportHeight);
                Assert.AreEqual(.45f, profile.PlaneMaximumViewportDimension); Assert.AreEqual("S005", profile.CatalogId);
                Assert.AreEqual(46, profile.MaxHeight); Assert.AreEqual(46, profile.BuildHeight);
                Assert.AreEqual(-5.31082f,profile.GroundReferenceY,.00001f,"Use actual source minimum terrain height, not assumed zero.");
                Assert.LessOrEqual((profile.MaxHeight-profile.GroundReferenceY)/Mathf.Sin(profile.Pitch*Mathf.Deg2Rad),63);
                Assert.Less(63,64,"Engineering centerline ceiling must stay below actual shared enemy Far threshold64.");
                var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
                Assert.AreEqual(definition.CampaignMap.ContentHash, profile.CampaignSourceContentHash);
                Assert.AreEqual(definition.MapLayout.ContentHash, profile.LayoutContentHash);
                Assert.IsFalse(EditorUtility.IsDirty(definition)); Assert.IsFalse(EditorUtility.IsDirty(definition.CampaignMap));
                profile.Pitch = 30; Assert.IsFalse(profile.IsValid,"Lower pitch must not silently invalidate the slant ceiling.");
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }
        [Test] public void CampaignWorldDoesNotResolveSkirmishProfileAndLegacyIdCannotBeConfigured()
        {
            using var world = new World("SyntheticCampaignViewScope");
            Assert.IsFalse(SkirmishMissionViewProfileApplication.TryResolve(world.EntityManager, out _));
            var profile = Candidate();
            try { profile.CatalogId = "S004"; Assert.IsFalse(profile.IsValid); }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }
        [Test] public void RuntimeProfileChangesLocalStateAndKeepsEstablishedPlatformCap()
        {
            var profile = Candidate();
            try
            {
                var state = SelectionRuntimeConfigStartupSystemHelper.CreateStateFromConfig(null, null);
                float beforeDefaultHeight = state.NormalModeZoomHeight;
                SkirmishMissionViewProfileApplication.Apply(profile, ref state);
                Assert.AreEqual(SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(profile.MaxHeight), state.MaxZoomHeight);
                Assert.LessOrEqual(state.NormalModeZoomHeight, state.MaxZoomHeight); Assert.AreEqual(profile.NormalFieldOfView, state.NormalModeFieldOfView);
                Assert.AreEqual(profile.Pitch, state.NormalModePitch); Assert.AreEqual(profile.Yaw, state.NormalModeYaw);
                var newDefault = SelectionRuntimeConfigStartupSystemHelper.CreateStateFromConfig(null, null);
                Assert.AreEqual(beforeDefaultHeight, newDefault.NormalModeZoomHeight, "Only the provided scoped state may change.");
                using var cameraWorld = new World("SyntheticMissionProfileUi");
                var helper = new SelectionUiCameraSystemHelper(cameraWorld.GetOrCreateSystemManaged<RtsCameraSystem>(),
                    cameraWorld.GetOrCreateSystemManaged<RtsCameraRequestSystem>());
                float Read(string name) => (float)typeof(SelectionUiCameraSystemHelper).GetField(name,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(helper);
                float oldMin=Read("_minZoomHeight"), oldMax=Read("_maxZoomHeight"), oldSpeed=Read("_zoomSpeed"),
                    oldIsoPitch=Read("_fullscreenIsoPitch"), oldIsoSize=Read("_fullscreenIsoOrthographicSize");
                helper.ApplyRuntimeCameraProfile(state); // Existing Campaign callers retain old behavior.
                Assert.AreEqual(oldMin,Read("_minZoomHeight")); Assert.AreEqual(oldMax,Read("_maxZoomHeight"));
                helper.ApplyRuntimeCameraProfile(state,true); // Explicit mission ownership.
                Assert.AreEqual(state.MinZoomHeight,Read("_minZoomHeight")); Assert.AreEqual(state.MaxZoomHeight,Read("_maxZoomHeight"));
                helper.ApplyRuntimeCameraProfile(newDefault,true); // Once on mission exit, then ordinary calls again.
                Assert.AreEqual(newDefault.MinZoomHeight,Read("_minZoomHeight")); Assert.AreEqual(newDefault.MaxZoomHeight,Read("_maxZoomHeight"));
                helper.ApplyRuntimeCameraProfile(state);
                Assert.AreEqual(newDefault.MaxZoomHeight,Read("_maxZoomHeight"),"Ordinary Campaign update must not reinstate mission clamp.");
                Assert.AreEqual(oldSpeed,Read("_zoomSpeed")); Assert.AreEqual(oldIsoPitch,Read("_fullscreenIsoPitch"));
                Assert.AreEqual(oldIsoSize,Read("_fullscreenIsoOrthographicSize"));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }
        [Test] public void AircraftFitUsesAllActualBoundsCornersAndRelativeAltitude()
        {
            var profile = Candidate(); var cameraObject = new GameObject("SyntheticAircraftFitCamera");
            var texture = new RenderTexture(1280,720,0);
            try
            {
                var camera = cameraObject.AddComponent<Camera>(); camera.targetTexture = texture; camera.pixelRect = new Rect(0,0,1280,720);
                camera.aspect = 16f/9f; camera.fieldOfView = profile.AircraftFieldOfView;
                var bounds = new Bounds(new Vector3(0,55,0), new Vector3(60,12,55)); // Synthetic, not a measured game plane.
                Assert.IsTrue(SkirmishMissionViewProfileApplication.TryAircraftPose(bounds, profile, camera.aspect, camera.nearClipPlane, out var position, out var rotation));
                camera.transform.SetPositionAndRotation(position, rotation);
                Assert.IsTrue(SkirmishCameraFramingMeasurement.TryProject(camera, bounds, out var rect));
                Assert.LessOrEqual(rect.width / camera.pixelWidth, profile.PlaneMaximumViewportDimension + .0001f);
                Assert.LessOrEqual(rect.height / camera.pixelHeight, profile.PlaneMaximumViewportDimension + .0001f);
                bounds.center += new Vector3(0,55,0);
                Assert.IsTrue(SkirmishMissionViewProfileApplication.TryAircraftPose(bounds, profile, camera.aspect, camera.nearClipPlane, out var raised, out _));
                Assert.AreEqual(55, raised.y-position.y, .001f); Assert.AreEqual(position.x, raised.x, .001f); Assert.AreEqual(position.z, raised.z, .001f);
            }
            finally { UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(profile); }
        }
        [Test] public void InvalidOrTooLargeAircraftDoesNotGetClampedIntoFalseFit()
        {
            var profile = Candidate();
            try
            {
                Assert.IsFalse(SkirmishMissionViewProfileApplication.TryAircraftPose(new Bounds(Vector3.zero,Vector3.one*10000), profile, 16f/9f,.3f,out _,out _));
                Assert.IsFalse(SkirmishMissionViewProfileApplication.TryAircraftPose(new Bounds(Vector3.zero,Vector3.one), profile, float.NaN,.3f,out _,out _));
                Assert.IsFalse(SkirmishMissionViewProfileApplication.TryAircraftPose(new Bounds(Vector3.zero,Vector3.zero), profile, 16f/9f,.3f,out _,out _));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }
        public static void RunFocusedValidation()
        {
            var suite = new SkirmishMissionViewProfileTests();
            suite.EngineeringCandidateUsesActualDefinitionPinsWithoutMutatingSource();
            suite.CampaignWorldDoesNotResolveSkirmishProfileAndLegacyIdCannotBeConfigured();
            suite.RuntimeProfileChangesLocalStateAndKeepsEstablishedPlatformCap();
            suite.AircraftFitUsesAllActualBoundsCornersAndRelativeAltitude();
            suite.InvalidOrTooLargeAircraftDoesNotGetClampedIntoFalseFit();
            Debug.Log("[SkirmishMissionViewProfileTests] result=Passed tests=5 actualMissionCapture=0 engineeringTargetsOnly=1");
        }
    }
}
