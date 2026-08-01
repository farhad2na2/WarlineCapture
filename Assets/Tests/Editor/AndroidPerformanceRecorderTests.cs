using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Composition;
using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AndroidPerformanceRecorderTests
{
    private const string PassMarker = "[AndroidPerformanceRecorderValidation] result=Passed tests=22";
    private delegate void CaptureReleaseMetrics(long batches, long setPassCalls, long triangles, long vertices);

    public static void RunFocusedValidation()
    {
        AndroidPerformanceRecorderTests tests = new();
        try
        {
            tests.RequiredFlagIsCaseInsensitiveAndExact();
            tests.MissingFlagKeepsRecorderDisabled();
            tests.RenderVirtualizationMetricsFlagIsIndependentExactAndOptIn();
            tests.Vrp067DestructionMatrixRequiresExactCompleteArguments();
            tests.Vrp067DestructionMatrixAcceptsHouseAndShopOnly();
            tests.Vrp067DestructionMatrixRemainsIndependentFromPerformanceGate();
            tests.Vrp067MetricsRetainTheExactMaterializedEnvelope();
            tests.LegacyDevelopmentFlagRemainsEnabled();
            tests.ExplicitDevelopmentTaskRemainsEnabled();
            tests.ReleaseModeRequiresExactTaskAndFrameRate();
            tests.ReleaseModeRunsInNonDevelopmentBuild();
            tests.UnknownTaskDoesNotEnableRecorder();
            tests.ReleaseModeRecordsInjectedProvenance();
            tests.ReleaseSettingsOverrideUsesMobileSixtyWithoutChangingOtherSettings();
            tests.DevelopmentSettingsRemainUnchanged();
            tests.ReleaseReportContainsReleaseEvidenceShape();
            tests.DevelopmentReportKeepsLegacyEvidenceShape();
            tests.ReleaseMetricAggregationDoesNotAllocateAfterWarmup();
            tests.ReleaseValidationFailsWhenRequiredCountersAreUnavailable();
            tests.PercentileMatchesEvidenceGateRounding();
            tests.PercentileIgnoresUnusedCapacityAndHandlesZeroSamples();
            tests.LaunchClock_SubsystemResetRestoresApplicationEpoch();
            Debug.Log(PassMarker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[AndroidPerformanceRecorderValidation] result=Failed");
            throw;
        }
    }

    public static void RunLaunchClockLifecycleValidation()
    {
        new AndroidPerformanceRecorderTests().LaunchClock_SubsystemResetRestoresApplicationEpoch();
        Debug.Log("[AndroidPerformanceLaunchClockValidation] result=Passed");
    }

    [Test]
    public void LaunchClock_SubsystemResetRestoresApplicationEpoch()
    {
        Type recorderType = typeof(AndroidPerformanceRecorder);
        FieldInfo launchClock = recorderType.GetField(
            "s_LaunchRealtimeSeconds",
            BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo reset = recorderType.GetMethod(
            "ResetLaunchClock",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(launchClock);
        Assert.NotNull(reset);

        launchClock.SetValue(null, -1d);
        double beforeReset = Time.realtimeSinceStartupAsDouble;
        reset.Invoke(null, null);
        double resetValue = (double)launchClock.GetValue(null);

        Assert.GreaterOrEqual(resetValue, beforeReset);
        Assert.LessOrEqual(resetValue, Time.realtimeSinceStartupAsDouble);
    }

    [Test]
    public void RequiredFlagIsCaseInsensitiveAndExact()
    {
        Assert.IsTrue(ContainsRequiredFlag(new[] { "app", "-WARLINEANDROIDPERFORMANCEGATE" }));
        Assert.IsFalse(ContainsRequiredFlag(new[] { "app", "-warlineAndroidPerformanceGateExtra" }));
    }

    [Test]
    public void MissingFlagKeepsRecorderDisabled()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(recorder, Array.Empty<string>(), true);
        Assert.IsFalse(recorder.IsEnabled);
        Assert.IsNull(ReadField(recorder, "_frameTimesMs"));
        Assert.IsNull(ReadField(recorder, "_latestFrameTiming"));
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void RenderVirtualizationMetricsFlagIsIndependentExactAndOptIn()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(
            recorder,
            new[] { "app", "-warlineVrp053Metrics" },
            true);
        Assert.IsFalse(recorder.IsEnabled);
        Assert.IsTrue((bool)ReadField(
            recorder,
            "_renderVirtualizationMetricsEnabled"));
        DisposeWithoutReport(recorder);

        recorder = new AndroidPerformanceRecorder();
        InvokeInitialize(
            recorder,
            new[] { "app", "-warlineVrp053MetricsExtra" },
            true);
        Assert.IsFalse((bool)ReadField(
            recorder,
            "_renderVirtualizationMetricsEnabled"));
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void Vrp067DestructionMatrixRequiresExactCompleteArguments()
    {
        Assert.IsFalse(TryResolveVrp067Configuration(
            new[] { "app", "-warlineVrp067StateOwner", "12" },
            out _,
            out _));
        Assert.IsFalse(TryResolveVrp067Configuration(
            new[]
            {
                "app", "-warlineVrp067StateOwnerExtra", "12",
                "-warlineVrp067Family", "House"
            },
            out _,
            out _));
        Assert.IsFalse(TryResolveVrp067Configuration(
            new[]
            {
                "app", "-warlineVrp067StateOwner", "-1",
                "-warlineVrp067Family", "House"
            },
            out _,
            out _));
    }

    [Test]
    public void Vrp067DestructionMatrixAcceptsHouseAndShopOnly()
    {
        Assert.IsTrue(TryResolveVrp067Configuration(
            new[]
            {
                "app", "-warlineVrp067StateOwner", "4471",
                "-warlineVrp067Family", "house"
            },
            out int houseOwner,
            out string houseFamily));
        Assert.AreEqual(4471, houseOwner);
        Assert.AreEqual("House", houseFamily);

        Assert.IsTrue(TryResolveVrp067Configuration(
            new[]
            {
                "app", "-warlineVrp067StateOwner", "2479",
                "-warlineVrp067Family", "SHOP"
            },
            out int shopOwner,
            out string shopFamily));
        Assert.AreEqual(2479, shopOwner);
        Assert.AreEqual("Shop", shopFamily);

        Assert.IsFalse(TryResolveVrp067Configuration(
            new[]
            {
                "app", "-warlineVrp067StateOwner", "1",
                "-warlineVrp067Family", "Tent"
            },
            out _,
            out _));
    }

    [Test]
    public void Vrp067DestructionMatrixRemainsIndependentFromPerformanceGate()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(
            recorder,
            new[]
            {
                "app", "-warlineVrp067StateOwner", "4471",
                "-warlineVrp067Family", "House"
            },
            true);

        Assert.IsFalse(recorder.IsEnabled);
        Assert.IsTrue((bool)ReadField(
            recorder,
            "_vrp067DestructionMatrixEnabled"));
        Assert.AreEqual(4471, ReadField(recorder, "_vrp067StateOwnerIndex"));
        Assert.AreEqual("House", ReadField(recorder, "_vrp067Family"));
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void Vrp067MetricsRetainTheExactMaterializedEnvelope()
    {
        using World world = new(nameof(Vrp067MetricsRetainTheExactMaterializedEnvelope));
        Entity owner = world.EntityManager.CreateEntity(
            typeof(OperationMapRenderVirtualizationMetricsComponent),
            typeof(OperationMapRenderVirtualizationStateComponent));
        world.EntityManager.SetComponentData(
            owner,
            new OperationMapRenderVirtualizationMetricsComponent
            {
                EnabledSlotCount = 1352,
                ActiveCellCount = 106,
                ActivePlacementCount = 1258,
                OverflowCount = 875,
                HighestDeficit = 876
            });
        world.EntityManager.SetComponentData(
            owner,
            new OperationMapRenderVirtualizationStateComponent
            {
                ActiveEnvelopeMin = new int2(40, 11),
                ActiveEnvelopeMax = new int2(53, 20)
            });

        object[] arguments =
        {
            world.EntityManager,
            0,
            0,
            0,
            0,
            0,
            int2.zero,
            int2.zero
        };
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "ReadVrp067VirtualizationMetrics",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(null, arguments);

        Assert.AreEqual(1352, arguments[1]);
        Assert.AreEqual(106, arguments[2]);
        Assert.AreEqual(1258, arguments[3]);
        Assert.AreEqual(875, arguments[4]);
        Assert.AreEqual(876, arguments[5]);
        Assert.AreEqual(new int2(40, 11), arguments[6]);
        Assert.AreEqual(new int2(53, 20), arguments[7]);
    }

    [Test]
    public void LegacyDevelopmentFlagRemainsEnabled()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(recorder, new[] { "app", "-warlineAndroidPerformanceGate" }, true);
        Assert.IsTrue(recorder.IsEnabled);
        Assert.AreEqual("Development", ReadField(recorder, "_mode").ToString());
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void ExplicitDevelopmentTaskRemainsEnabled()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(
            recorder,
            new[] { "app", "-warlineAndroidPerformanceGate", "APH-803" },
            true);
        Assert.IsTrue(recorder.IsEnabled);
        Assert.AreEqual("Development", ReadField(recorder, "_mode").ToString());
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void ReleaseModeRequiresExactTaskAndFrameRate()
    {
        Assert.IsTrue(TryGetRequestedReleaseFrameRate(
            new[]
            {
                "app", "-warlineAndroidPerformanceGate", "aph-804",
                "-warlinePerformanceFrameRate", "60"
            },
            out int frameRate));
        Assert.AreEqual(60, frameRate);

        Assert.IsFalse(TryGetRequestedReleaseFrameRate(
            new[]
            {
                "app", "-warlineAndroidPerformanceGate", "APH-804",
                "-warlinePerformanceFrameRate", "30"
            },
            out _));
        Assert.IsFalse(TryGetRequestedReleaseFrameRate(
            new[] { "app", "-warlineAndroidPerformanceGate", "APH-804" },
            out _));
    }

    [Test]
    public void ReleaseModeRunsInNonDevelopmentBuild()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(recorder, ReleaseArguments(), false);
        Assert.IsTrue(recorder.IsEnabled);
        Assert.AreEqual("Release", ReadField(recorder, "_mode").ToString());
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void UnknownTaskDoesNotEnableRecorder()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(
            recorder,
            new[]
            {
                "app", "-warlineAndroidPerformanceGate", "APH-999",
                "-warlinePerformanceFrameRate", "60"
            },
            true);
        Assert.IsFalse(recorder.IsEnabled);
        recorder.Dispose();
    }

    [Test]
    public void ReleaseModeRecordsInjectedProvenance()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(
            recorder,
            ReleaseArguments(),
            false,
            scriptDebugging: true,
            profilerAttached: true,
            profilerMarkersEnabled: true);

        Assert.AreEqual(false, ReadField(recorder, "_developmentBuild"));
        Assert.AreEqual(true, ReadField(recorder, "_scriptDebugging"));
        Assert.AreEqual(true, ReadField(recorder, "_profilerAttached"));
        Assert.AreEqual(true, ReadField(recorder, "_profilerMarkersEnabled"));
        recorder.Dispose();
    }

    [Test]
    public void ReleaseSettingsOverrideUsesMobileSixtyWithoutChangingOtherSettings()
    {
        UISettingsModel settings = SettingsService.DefaultsForPlatform(isAndroid: true);
        settings.Graphics.Quality = UIGraphicsQuality.Ultra;
        settings.Graphics.FrameRateMode = UIFrameRateMode.Thirty;
        float masterVolume = settings.Audio.MasterVolume;

        UISettingsModel resolved = AndroidPerformanceRuntimeSettings.Resolve(settings, ReleaseArguments());

        Assert.AreEqual(UIGraphicsQuality.High, resolved.Graphics.Quality);
        Assert.AreEqual(UIFrameRateMode.Sixty, resolved.Graphics.FrameRateMode);
        Assert.AreEqual(masterVolume, resolved.Audio.MasterVolume);
    }

    [Test]
    public void DevelopmentSettingsRemainUnchanged()
    {
        UISettingsModel settings = SettingsService.DefaultsForPlatform(isAndroid: true);
        settings.Graphics.Quality = UIGraphicsQuality.Ultra;
        settings.Graphics.FrameRateMode = UIFrameRateMode.Thirty;

        UISettingsModel resolved = AndroidPerformanceRuntimeSettings.Resolve(
            settings,
            new[] { "app", "-warlineAndroidPerformanceGate" });

        Assert.AreEqual(UIGraphicsQuality.Ultra, resolved.Graphics.Quality);
        Assert.AreEqual(UIFrameRateMode.Thirty, resolved.Graphics.FrameRateMode);
    }

    [Test]
    public void ReleaseReportContainsReleaseEvidenceShape()
    {
        AndroidPerformanceRecorder recorder = CreateReportReadyRecorder(ReleaseArguments(), false);
        string json = BuildReportJson(recorder, "BuildReleaseReport");

        StringAssert.Contains("\"taskId\":\"APH-804\"", json);
        StringAssert.Contains("\"recorderMode\":\"release-performance-evidence\"", json);
        StringAssert.Contains("\"buildType\":\"release\"", json);
        StringAssert.Contains("\"developmentBuild\":false", json);
        StringAssert.Contains("\"gc\":", json);
        StringAssert.Contains("\"memory\":", json);
        StringAssert.Contains("\"battery\":", json);
        StringAssert.Contains("\"counters\":", json);
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void DevelopmentReportKeepsLegacyEvidenceShape()
    {
        AndroidPerformanceRecorder recorder = CreateReportReadyRecorder(
            new[] { "app", "-warlineAndroidPerformanceGate" },
            true);
        string json = BuildReportJson(recorder, "BuildDevelopmentReport");

        StringAssert.Contains("\"taskId\":\"APH-803\"", json);
        StringAssert.Contains("\"p95CpuFrameMs\":", json);
        StringAssert.Contains("\"peakAllocatedMemoryMB\":", json);
        StringAssert.DoesNotContain("\"recorderMode\":", json);
        StringAssert.DoesNotContain("\"battery\":", json);
        StringAssert.DoesNotContain("\"counters\":", json);
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void ReleaseMetricAggregationDoesNotAllocateAfterWarmup()
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(recorder, ReleaseArguments(), false);
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "CaptureReleaseFrameMetrics",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        CaptureReleaseMetrics capture =
            (CaptureReleaseMetrics)method.CreateDelegate(typeof(CaptureReleaseMetrics), recorder);
        capture(1L, 1L, 1L, 1L);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 128; i++)
            capture(10L, 2L, 100L, 200L);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0L, allocatedBytes);
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void ReleaseValidationFailsWhenRequiredCountersAreUnavailable()
    {
        AndroidPerformanceRecorder recorder = CreateReportReadyRecorder(ReleaseArguments(), false);
        WriteField(recorder, "_gcCounterSampleCount", 0);
        WriteField(recorder, "_renderCounterSampleCount", 0);
        object[] invocationArguments = { string.Empty };
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "TryValidateReleaseCapture",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        Assert.IsFalse((bool)method.Invoke(recorder, invocationArguments));
        StringAssert.Contains("required profiler counters were unavailable", (string)invocationArguments[0]);
        DisposeWithoutReport(recorder);
    }

    [Test]
    public void PercentileMatchesEvidenceGateRounding()
    {
        float[] samples = { 1f, 2f, 3f, 4f, 100f };
        Assert.AreEqual(100d, Percentile(samples, samples.Length, 95d));
        Assert.AreEqual(100d, Percentile(samples, samples.Length, 99d));
    }

    [Test]
    public void PercentileIgnoresUnusedCapacityAndHandlesZeroSamples()
    {
        float[] samples = { 30f, 10f, 20f, 999f, 999f };
        Assert.AreEqual(30d, Percentile(samples, 3, 95d));
        Assert.AreEqual(0d, Percentile(Array.Empty<float>(), 0, 95d));
    }

    private static IReadOnlyList<string> ReleaseArguments()
    {
        return new[]
        {
            "app", "-warlineAutoStartMatch", "-warlineAndroidPerformanceGate", "APH-804",
            "-warlinePerformanceFrameRate", "60"
        };
    }

    private static AndroidPerformanceRecorder CreateReportReadyRecorder(
        IReadOnlyList<string> arguments,
        bool isDevelopmentBuild)
    {
        AndroidPerformanceRecorder recorder = new();
        InvokeInitialize(recorder, arguments, isDevelopmentBuild);
        float[] frameTimes = (float[])ReadField(recorder, "_frameTimesMs");
        float[] cpuTimes = (float[])ReadField(recorder, "_cpuFrameTimesMs");
        float[] gpuTimes = (float[])ReadField(recorder, "_gpuFrameTimesMs");
        for (int i = 0; i < 3; i++)
        {
            frameTimes[i] = 16f + i;
            cpuTimes[i] = 10f + i;
            gpuTimes[i] = 12f + i;
        }

        WriteField(recorder, "_sampleCount", 3);
        WriteField(recorder, "_cpuTimingSampleCount", 3);
        WriteField(recorder, "_gpuTimingSampleCount", 3);
        WriteField(recorder, "_gcCounterSampleCount", 3);
        WriteField(recorder, "_renderCounterSampleCount", 3);
        WriteField(recorder, "_totalGcAllocatedBytes", 96L);
        WriteField(recorder, "_batchesTotal", 300d);
        WriteField(recorder, "_setPassCallsTotal", 30d);
        WriteField(recorder, "_trianglesTotal", 3000d);
        WriteField(recorder, "_verticesTotal", 6000d);
        WriteField(recorder, "_batteryStartPercent", 90d);
        WriteField(recorder, "_batteryEndPercent", 89d);
        WriteField(recorder, "_peakAllocatedMemoryBytes", 1048576L);
        WriteField(recorder, "_peakMonoMemoryBytes", 524288L);
        WriteField(recorder, "_peakResidentSetBytes", 2097152L);
        return recorder;
    }

    private static string BuildReportJson(AndroidPerformanceRecorder recorder, string methodName)
    {
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        object report = method.Invoke(recorder, new object[] { true, string.Empty });
        return JsonUtility.ToJson(report);
    }

    private static bool ContainsRequiredFlag(IReadOnlyList<string> arguments)
    {
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "ContainsRequiredFlag",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (bool)method.Invoke(null, new object[] { arguments });
    }

    private static bool TryGetRequestedReleaseFrameRate(
        IReadOnlyList<string> arguments,
        out int frameRate)
    {
        object[] invocationArguments = { arguments, 0 };
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "TryGetRequestedReleaseFrameRate",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        bool result = (bool)method.Invoke(null, invocationArguments);
        frameRate = (int)invocationArguments[1];
        return result;
    }

    private static bool TryResolveVrp067Configuration(
        IReadOnlyList<string> arguments,
        out int stateOwnerIndex,
        out string family)
    {
        object[] invocationArguments = { arguments, -1, string.Empty };
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "TryResolveVrp067Configuration",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        bool result = (bool)method.Invoke(null, invocationArguments);
        stateOwnerIndex = (int)invocationArguments[1];
        family = (string)invocationArguments[2];
        return result;
    }

    private static double Percentile(float[] samples, int count, double percentile)
    {
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "Percentile",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (double)method.Invoke(null, new object[] { samples, count, percentile });
    }

    private static void InvokeInitialize(
        AndroidPerformanceRecorder recorder,
        IReadOnlyList<string> arguments,
        bool isDevelopmentBuild,
        bool scriptDebugging = false,
        bool profilerAttached = false,
        bool profilerMarkersEnabled = false)
    {
        MethodInfo method = typeof(AndroidPerformanceRecorder).GetMethod(
            "Initialize",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(IReadOnlyList<string>), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
            },
            null);
        Assert.IsNotNull(method);
        method.Invoke(
            recorder,
            new object[]
            {
                arguments, isDevelopmentBuild, scriptDebugging, profilerAttached, profilerMarkersEnabled
            });
    }

    private static object ReadField(AndroidPerformanceRecorder recorder, string name)
    {
        FieldInfo field = typeof(AndroidPerformanceRecorder).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return field.GetValue(recorder);
    }

    private static void WriteField(AndroidPerformanceRecorder recorder, string name, object value)
    {
        FieldInfo field = typeof(AndroidPerformanceRecorder).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(recorder, value);
    }

    private static void DisposeWithoutReport(AndroidPerformanceRecorder recorder)
    {
        WriteField(recorder, "_finished", true);
        recorder.Dispose();
    }
}
#endif
