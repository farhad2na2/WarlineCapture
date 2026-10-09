using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SharedAirfieldCameraProfileTests
{
    [Test]
    public void FixedCampaignDaylight_SurvivesLateSceneEnvironmentRestore()
    {
        var skybox = RenderSettings.skybox;
        var originalSky = RenderSettings.ambientSkyColor;
        var originalEquator = RenderSettings.ambientEquatorColor;
        var originalFog = RenderSettings.fog;
        var go = new GameObject("Shared Campaign daylight regression");
        try
        {
            var lighting = go.AddComponent<Game.Composition.SkirmishS004CampaignLighting>();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            lighting.Configure(scene, scene);
            // Reproduce the late day/night shutdown/scene settings restoration.
            RenderSettings.ambientSkyColor = new Color(.212f, .227f, .259f);
            RenderSettings.ambientEquatorColor = new Color(.114f, .125f, .133f);
            RenderSettings.fog = true;
            RenderSettings.skybox = null;
            typeof(Game.Composition.SkirmishS004CampaignLighting).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(lighting, null);
            Assert.AreEqual(new Color(.79f, .86f, .96f), RenderSettings.ambientSkyColor);
            Assert.AreEqual(new Color(.72f, .73f, .74f), RenderSettings.ambientEquatorColor);
            Assert.IsFalse(RenderSettings.fog);
            Assert.AreSame(skybox, RenderSettings.skybox);
        }
        finally
        {
            // Edit-mode fixtures do not run this play-mode MonoBehaviour's
            // lifecycle; invoke the same disposal callback explicitly.
            var lighting = go.GetComponent<Game.Composition.SkirmishS004CampaignLighting>();
            typeof(Game.Composition.SkirmishS004CampaignLighting).GetMethod("OnDestroy",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(lighting, null);
            Object.DestroyImmediate(go);
        }
        Assert.AreEqual(originalSky, RenderSettings.ambientSkyColor, "Disposal must restore the previous scene environment.");
        Assert.AreEqual(originalEquator, RenderSettings.ambientEquatorColor);
        Assert.AreEqual(originalFog, RenderSettings.fog);
    }

    public static void RunFixedCampaignDaylightValidation()
    {
        try
        {
            new SharedAirfieldCameraProfileTests().FixedCampaignDaylight_SurvivesLateSceneEnvironmentRestore();
            Debug.Log("[FixedCampaignDaylightValidation] result=Passed lateRestore=Repaired previousEnvironment=Restored");
            ValidationExit.Passed();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[FixedCampaignDaylightValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void SharedMapConsumersKeepAuthoredAnglesThroughBuildAndNormalModes()
    {
        Check("opmap.skirmish.s005", "opmap.skirmish.cityedgeairfield_prepared", 100, true);
        Check("opmap.campaign.airfield", "opmap.skirmish.cityedgeairfield_prepared", 100, true);
        Check("opmap.skirmish.cityedgeairfield_prepared", "", 100, true);
    }

    [Test]
    public void SharedCameraKeepsZoomCapAndDoesNotOverrideOtherMaps()
    {
        Check("opmap.skirmish.s005", "opmap.skirmish.cityedgeairfield_prepared", 40, true);
        Check("opmap.campaign.other", "opmap.campaign.other", 100, false);
    }

    private static void Check(string id, string source, float maxHeight, bool expected)
    {
        using var world = new World("shared camera profile test");
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = id;
        root.SourceOperationMapId = source;
        root.BattleCameraId = "battle";
        var cameras = builder.Allocate(ref root.Cameras, 1);
        cameras[0] = new OperationMapCameraBlob
        {
            Id = "battle", Position = new float3(0, 75, 0),
            Rotation = (quaternion)Quaternion.Euler(50, -90, 0), FieldOfView = 50
        };
        using var blob = builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
        var em = world.EntityManager;
        var map = em.CreateEntity(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
        em.SetComponentData(map, new OperationMapMetadataComponent
        { Blob = blob, PhysicalSourceValidated = 1, Generation = 7 });
        var state = SelectionRuntimeConfigStartupSystemHelper.CreateStateFromConfig(null, null);
        state.MaxZoomHeight = maxHeight;
        var before = state;
        Assert.AreEqual(expected, SelectionRuntimeConfigStartupSystemHelper.TryApplySharedAirfieldCamera(em, ref state, out var generation));
        if (!expected)
        {
            Assert.AreEqual(before.NormalModeYaw, state.NormalModeYaw);
            Assert.AreEqual(before.BuildModeYaw, state.BuildModeYaw);
            return;
        }
        Assert.AreEqual(7, generation);
        Assert.That(Mathf.DeltaAngle(-90, state.NormalModeYaw), Is.EqualTo(0).Within(.001));
        Assert.AreEqual(state.NormalModeYaw, state.BuildModeYaw);
        Assert.That(state.NormalModePitch, Is.EqualTo(50).Within(.001));
        Assert.AreEqual(state.NormalModePitch, state.BuildModePitch);
        Assert.AreEqual(Mathf.Min(75, maxHeight), state.NormalModeZoomHeight);
        Assert.That(state.BuildModeZoomHeight, Is.LessThanOrEqualTo(Mathf.Max(maxHeight, before.BuildModeZoomHeight)));
        Assert.AreEqual(50, state.NormalModeFieldOfView);
        Assert.That(state.BuildModeFieldOfView, Is.GreaterThanOrEqualTo(50));
    }

    [Test]
    public void AuthoredBaseFocusCannotBypassMobileHeightLimit()
    {
        const string key = "WARLINE_FORCE_MOBILE_CAMERA_LIMITS";
        string previous = System.Environment.GetEnvironmentVariable(key);
        try
        {
            System.Environment.SetEnvironmentVariable(key, "1");
            Assert.AreEqual(40, SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(75));
            Assert.AreEqual(24, SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(24));
            System.Environment.SetEnvironmentVariable(key, null);
            if (!Application.isMobilePlatform)
                Assert.AreEqual(75, SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(75));
        }
        finally { System.Environment.SetEnvironmentVariable(key, previous); }
    }

    public static void RunFocusedValidation()
    {
        var suite = new SharedAirfieldCameraProfileTests();
        suite.SharedMapConsumersKeepAuthoredAnglesThroughBuildAndNormalModes();
        suite.SharedCameraKeepsZoomCapAndDoesNotOverrideOtherMaps();
        suite.AuthoredBaseFocusCannotBypassMobileHeightLimit();
        Debug.Log("[SharedAirfieldCameraProfileTests] result=Passed sharedConsumers=3 zoomCap=Preserved otherMaps=Preserved");
    }
}
