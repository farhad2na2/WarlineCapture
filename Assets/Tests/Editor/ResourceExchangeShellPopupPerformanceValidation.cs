using Game.UI.Contracts;
using Game.UI.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public sealed class ResourceExchangeShellPopupPerformanceValidation
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string PopupName = "POP12_ResourceExchangePopup";
    private const string UnchangedReportPath =
        "/private/tmp/am017-resource-exchange-shell-popup-unchanged.json";
    private const string TransitionReportPath =
        "/private/tmp/am017-resource-exchange-shell-popup-transitions.json";
    private const int WarmupFrames = 180;
    private const int MeasuredFrames = 300;
    private const int WarmupTransitions = 1;
    private const int MeasuredTransitions = 100;
    private const double P95FrameMsCeiling = 20d;

    public static void RunBatchValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(FullyBoundOpenUnchangedPopup_RecurringRefreshAllocatesZeroManagedBytes),
                validation => validation.FullyBoundOpenUnchangedPopup_RecurringRefreshAllocatesZeroManagedBytes(),
                ref passed);
            RunValidationStep(
                nameof(OpenCloseTransitions_AfterOneWarmupAllocateZeroProductionManagedBytes),
                validation => validation.OpenCloseTransitions_AfterOneWarmupAllocateZeroProductionManagedBytes(),
                ref passed);

            Debug.Log(
                $"[ResourceExchangeShellPopupPerformanceValidation] result=Passed tests={passed} " +
                $"unchangedReport={UnchangedReportPath} transitionReport={TransitionReportPath}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                $"[ResourceExchangeShellPopupPerformanceValidation] result=Failed passed={passed} " +
                $"unchangedReport={UnchangedReportPath} transitionReport={TransitionReportPath}");
            ValidationExit.Failed();
        }
    }

    [TearDown]
    public void TearDown()
    {
        UiShellRuntimeGateway.Register(null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    [Test]
    public void FullyBoundOpenUnchangedPopup_RecurringRefreshAllocatesZeroManagedBytes()
    {
        UnchangedMetrics metrics = CaptureUnchangedMetrics();
        WriteUnchangedReport(metrics);

        Debug.Log(
            $"[ResourceExchangeShellPopupPerformanceValidation] state=surface-open-fully-bound-unchanged " +
            $"warmupFrames={WarmupFrames} measuredFrames={MeasuredFrames} " +
            $"focusedUiRecurringAllocatedBytes={metrics.ProductionAllocatedBytes} " +
            $"measurementWindowAllocatedBytes={metrics.MeasurementWindowAllocatedBytes} " +
            $"harnessAllocatedBytes={metrics.HarnessAllocatedBytes} " +
            $"instrumentationControlAllocatedBytes={metrics.InstrumentationControlAllocatedBytes} " +
            $"averageFrameMs={metrics.Timing.AverageMs:F6} p95FrameMs={metrics.Timing.P95Ms:F6} " +
            $"p99FrameMs={metrics.Timing.P99Ms:F6} maxFrameMs={metrics.Timing.MaxMs:F6}");

        Assert.AreEqual(
            0L,
            metrics.InstrumentationControlAllocatedBytes,
            "Allocation/timing sampler control allocated managed memory; production attribution is invalid.");
        Assert.AreEqual(
            0L,
            metrics.HarnessAllocatedBytes,
            "The complete unchanged-state window contains unattributed managed bytes and must fail closed.");
        Assert.AreEqual(
            0L,
            metrics.MeasurementWindowAllocatedBytes,
            "The fully bound unchanged popup measurement window must allocate exactly zero managed bytes.");
        Assert.AreEqual(
            0L,
            metrics.ProductionAllocatedBytes,
            $"Fully bound unchanged Resource Exchange popup refresh allocated " +
            $"{metrics.ProductionAllocatedBytes} recurring managed bytes over {MeasuredFrames} measured frames " +
            $"after {WarmupFrames} warmup frames.");
        Assert.AreEqual(
            WarmupFrames + MeasuredFrames,
            metrics.GatewayReadCount,
            "Every deterministic warmup and measured frame must exercise the bound Resource Exchange read model.");
        Assert.AreEqual(
            metrics.ContentVersionBefore,
            metrics.ContentVersionAfter,
            "The open popup must remain unchanged throughout warmup and measurement.");
        Assert.IsTrue(metrics.RemainedFullyBound, "The popup stopped being open or fully bound during measurement.");
        Assert.LessOrEqual(
            metrics.Timing.P95Ms,
            P95FrameMsCeiling,
            $"Fully bound unchanged popup P95 must be <= {P95FrameMsCeiling:F3} ms.");
        Assert.GreaterOrEqual(metrics.Timing.TotalMs, 0d);
    }

    [Test]
    public void OpenCloseTransitions_AfterOneWarmupAllocateZeroProductionManagedBytes()
    {
        TransitionMetrics metrics = CaptureTransitionMetrics();
        WriteTransitionReport(metrics);

        Debug.Log(
            $"[ResourceExchangeShellPopupPerformanceValidation] transition=open-close " +
            $"warmupTransitions={WarmupTransitions} measuredTransitions={MeasuredTransitions} " +
            $"focusedUiOpenAllocatedBytes={metrics.OpenAllocatedBytes} " +
            $"focusedUiCloseAllocatedBytes={metrics.CloseAllocatedBytes} " +
            $"focusedUiOpenCloseAllocatedBytes={metrics.OpenAllocatedBytes + metrics.CloseAllocatedBytes} " +
            $"instrumentationControlAllocatedBytes={metrics.InstrumentationControlAllocatedBytes} " +
            $"averageOpenMs={metrics.OpenTiming.AverageMs:F6} p95OpenMs={metrics.OpenTiming.P95Ms:F6} " +
            $"maxOpenMs={metrics.OpenTiming.MaxMs:F6} averageCloseMs={metrics.CloseTiming.AverageMs:F6} " +
            $"p95CloseMs={metrics.CloseTiming.P95Ms:F6} maxCloseMs={metrics.CloseTiming.MaxMs:F6}");

        Assert.AreEqual(
            0L,
            metrics.InstrumentationControlAllocatedBytes,
            "Allocation/timing sampler control allocated managed memory; transition attribution is invalid.");
        Assert.IsTrue(metrics.EveryOpenFullyBound, "Every measured open must produce the fully bound popup.");
        Assert.IsTrue(metrics.EveryCloseDestroyedPopup, "Every measured close must remove the installed popup.");
        Assert.AreEqual(
            metrics.ExpectedContentVersionAfter,
            metrics.ContentVersionAfter,
            "Every measured open and close must record one shell content mutation.");
        Assert.AreEqual(
            0L,
            metrics.OpenAllocatedBytes,
            $"Repeated Resource Exchange popup opens allocated {metrics.OpenAllocatedBytes} production managed " +
            $"bytes over {MeasuredTransitions} measured opens after exactly one warmup open/close transition.");
        Assert.AreEqual(
            0L,
            metrics.CloseAllocatedBytes,
            $"Repeated Resource Exchange popup closes allocated {metrics.CloseAllocatedBytes} production managed " +
            $"bytes over {MeasuredTransitions} measured closes after exactly one warmup open/close transition.");
        Assert.AreEqual(
            0L,
            metrics.OpenAllocatedBytes + metrics.CloseAllocatedBytes,
            "Recurring Resource Exchange open/close production managed bytes must be exactly zero.");
        Assert.LessOrEqual(
            metrics.OpenTiming.P95Ms,
            P95FrameMsCeiling,
            $"Resource Exchange popup open P95 must be <= {P95FrameMsCeiling:F3} ms.");
        Assert.LessOrEqual(
            metrics.CloseTiming.P95Ms,
            P95FrameMsCeiling,
            $"Resource Exchange popup close P95 must be <= {P95FrameMsCeiling:F3} ms.");
        Assert.GreaterOrEqual(metrics.OpenTiming.TotalMs, 0d);
        Assert.GreaterOrEqual(metrics.CloseTiming.TotalMs, 0d);
    }

    private static UnchangedMetrics CaptureUnchangedMetrics()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain UIShellContentView.");
        Assert.NotNull(content.ShellView, "Menu scene shell content must reference UIShellView.");
        Assert.NotNull(content.ResourceExchangePopupPrefab, "Menu scene shell must reference the Resource Exchange popup prefab.");
        Assert.AreEqual(PopupName, content.ResourceExchangePopupPrefab.name);

        var gateway = new StableGateway(CreateStableExchangeModel());
        var runtimeUi = new MainMenuPlayUI();
        GameObject popup = null;
        try
        {
            UiShellRuntimeGateway.Register(gateway);
            content.BindGameplayRuntimeDependencies(null, runtimeUi);
            popup = content.InstallResourceExchangePopup();
            Assert.NotNull(popup, "Shell must install the Resource Exchange popup before measurement.");
            Assert.AreEqual(PopupName, popup.name);

            ResourceExchangePopupView popupView = popup.GetComponent<ResourceExchangePopupView>();
            ResourceExchangePopupRuntimeView runtimeView =
                popup.GetComponent<ResourceExchangePopupRuntimeView>();
            Assert.NotNull(popupView, "Installed popup must expose ResourceExchangePopupView.");
            Assert.NotNull(runtimeView, "Installed popup must expose ResourceExchangePopupRuntimeView.");
            Assert.NotNull(popupView.CloseButton, "Installed popup must retain its bound close control.");

            // EditMode does not guarantee an automatic enable callback for every prefab load path.
            runtimeView.ConfigureForTests(popupView);
            runtimeView.SendMessage("OnEnable");
            runtimeView.RefreshNow(force: true);
            Assert.IsTrue(popupView.IsOpen, "Measured popup must be open.");
            Assert.IsTrue(runtimeView.isActiveAndEnabled, "Measured popup runtime view must be active.");
            Assert.IsTrue(
                ResourceExchangePopupRuntimeView.IsActiveViewForTests(runtimeView),
                "Measured popup runtime view must own recurring refreshes.");

            Canvas.ForceUpdateCanvases();
            Vector2 popupCenter = RectTransformUtility.WorldToScreenPoint(null, popup.transform.position);
            Assert.IsTrue(
                runtimeUi.IsPointerOverAnyGameplayUi(popupCenter, out string source),
                "Runtime UI must be bound to the open popup before measurement.");
            Assert.AreEqual("ResourceExchangePopup", source);
            AssertRegionContainsOnly(content.ShellView, UIShellRegionId.PopupLayer, popup);

            int contentVersionBefore = content.ContentVersion;
            int readsBeforeWarmup = gateway.ResourceExchangeReadCount;
            for (int frame = 0; frame < WarmupFrames; frame++)
                ResourceExchangePopupRuntimeView.RefreshActiveView();

            Assert.AreEqual(
                WarmupFrames,
                gateway.ResourceExchangeReadCount - readsBeforeWarmup,
                "Warmup must exercise every requested unchanged popup frame.");
            Assert.AreEqual(contentVersionBefore, content.ContentVersion);
            WarmInstrumentation();

            CollectManagedMemory();
            long instrumentationControlAllocatedBytes =
                MeasureInstrumentationControl(MeasuredFrames);
            CollectManagedMemory();

            var frameTicks = new long[MeasuredFrames];
            long productionAllocatedBytes = 0L;
            long maximumFrameAllocatedBytes = 0L;
            int readsBeforeMeasurement = gateway.ResourceExchangeReadCount;
            long measurementWindowBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < MeasuredFrames; frame++)
            {
                long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
                long startTicks = Stopwatch.GetTimestamp();
                ResourceExchangePopupRuntimeView.RefreshActiveView();
                long stopTicks = Stopwatch.GetTimestamp();
                long frameAllocatedBytes =
                    GC.GetAllocatedBytesForCurrentThread() - allocationBefore;

                frameTicks[frame] = stopTicks - startTicks;
                productionAllocatedBytes += frameAllocatedBytes;
                if (frameAllocatedBytes > maximumFrameAllocatedBytes)
                    maximumFrameAllocatedBytes = frameAllocatedBytes;
            }

            long measurementWindowAllocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - measurementWindowBefore;
            long harnessAllocatedBytes =
                measurementWindowAllocatedBytes - productionAllocatedBytes;
            int measuredReads = gateway.ResourceExchangeReadCount - readsBeforeMeasurement;
            bool remainedFullyBound =
                popup != null &&
                popupView.IsOpen &&
                runtimeView.isActiveAndEnabled &&
                ResourceExchangePopupRuntimeView.IsActiveViewForTests(runtimeView);

            return new UnchangedMetrics(
                productionAllocatedBytes,
                maximumFrameAllocatedBytes,
                measurementWindowAllocatedBytes,
                harnessAllocatedBytes,
                instrumentationControlAllocatedBytes,
                gateway.ResourceExchangeReadCount - readsBeforeWarmup,
                measuredReads,
                contentVersionBefore,
                content.ContentVersion,
                remainedFullyBound,
                SummarizeTiming(frameTicks));
        }
        finally
        {
            content.CloseResourceExchangePopup();
            runtimeUi.Dispose();
            UiShellRuntimeGateway.Register(null);
        }
    }

    private static TransitionMetrics CaptureTransitionMetrics()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain UIShellContentView.");
        Assert.NotNull(content.ShellView, "Menu scene shell content must reference UIShellView.");
        Assert.NotNull(content.ResourceExchangePopupPrefab, "Menu scene shell must reference the Resource Exchange popup prefab.");
        Assert.AreEqual(PopupName, content.ResourceExchangePopupPrefab.name);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);

        var gateway = new StableGateway(CreateStableExchangeModel());
        var runtimeUi = new MainMenuPlayUI();
        try
        {
            UiShellRuntimeGateway.Register(gateway);
            content.BindGameplayRuntimeDependencies(null, runtimeUi);

            GameObject warmupPopup = null;
            for (int transition = 0; transition < WarmupTransitions; transition++)
            {
                warmupPopup = content.InstallResourceExchangePopup();
                PreparePopupForEditMode(warmupPopup);
                AssertFullyBoundPopup(warmupPopup);
                content.CloseResourceExchangePopup();
                Assert.IsTrue(warmupPopup == null, "Warmup close must destroy the popup.");
            }

            AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);
            WarmInstrumentation();
            CollectManagedMemory();
            long instrumentationControlAllocatedBytes =
                MeasureInstrumentationControl(MeasuredTransitions * 2);
            CollectManagedMemory();

            var openTicks = new long[MeasuredTransitions];
            var closeTicks = new long[MeasuredTransitions];
            long openAllocatedBytes = 0L;
            long closeAllocatedBytes = 0L;
            long maximumOpenAllocatedBytes = 0L;
            long maximumCloseAllocatedBytes = 0L;
            bool everyOpenFullyBound = true;
            bool everyCloseDestroyedPopup = true;
            int contentVersionBefore = content.ContentVersion;

            for (int transition = 0; transition < MeasuredTransitions; transition++)
            {
                long openAllocationBefore = GC.GetAllocatedBytesForCurrentThread();
                long openStartTicks = Stopwatch.GetTimestamp();
                GameObject popup = content.InstallResourceExchangePopup();
                long openStopTicks = Stopwatch.GetTimestamp();
                long openFrameAllocatedBytes =
                    GC.GetAllocatedBytesForCurrentThread() - openAllocationBefore;

                openTicks[transition] = openStopTicks - openStartTicks;
                openAllocatedBytes += openFrameAllocatedBytes;
                if (openFrameAllocatedBytes > maximumOpenAllocatedBytes)
                    maximumOpenAllocatedBytes = openFrameAllocatedBytes;

                PreparePopupForEditMode(popup);
                everyOpenFullyBound &= IsFullyBoundPopup(popup);

                long closeAllocationBefore = GC.GetAllocatedBytesForCurrentThread();
                long closeStartTicks = Stopwatch.GetTimestamp();
                content.CloseResourceExchangePopup();
                long closeStopTicks = Stopwatch.GetTimestamp();
                long closeFrameAllocatedBytes =
                    GC.GetAllocatedBytesForCurrentThread() - closeAllocationBefore;

                closeTicks[transition] = closeStopTicks - closeStartTicks;
                closeAllocatedBytes += closeFrameAllocatedBytes;
                if (closeFrameAllocatedBytes > maximumCloseAllocatedBytes)
                    maximumCloseAllocatedBytes = closeFrameAllocatedBytes;

                everyCloseDestroyedPopup &= popup == null;
            }

            int expectedContentVersionAfter =
                contentVersionBefore + MeasuredTransitions * 2;
            return new TransitionMetrics(
                openAllocatedBytes,
                closeAllocatedBytes,
                maximumOpenAllocatedBytes,
                maximumCloseAllocatedBytes,
                instrumentationControlAllocatedBytes,
                contentVersionBefore,
                content.ContentVersion,
                expectedContentVersionAfter,
                everyOpenFullyBound,
                everyCloseDestroyedPopup,
                SummarizeTiming(openTicks),
                SummarizeTiming(closeTicks));
        }
        finally
        {
            content.CloseResourceExchangePopup();
            runtimeUi.Dispose();
            UiShellRuntimeGateway.Register(null);
        }
    }

    private static bool IsFullyBoundPopup(GameObject popup)
    {
        if (popup == null || !popup.activeInHierarchy || popup.name != PopupName)
            return false;

        ResourceExchangePopupView popupView = popup.GetComponent<ResourceExchangePopupView>();
        ResourceExchangePopupRuntimeView runtimeView =
            popup.GetComponent<ResourceExchangePopupRuntimeView>();
        return popupView != null &&
               popupView.CloseButton != null &&
               popupView.IsOpen &&
               runtimeView != null &&
               runtimeView.isActiveAndEnabled &&
               ResourceExchangePopupRuntimeView.IsActiveViewForTests(runtimeView);
    }

    private static void PreparePopupForEditMode(GameObject popup)
    {
        Assert.NotNull(popup);
        ResourceExchangePopupView popupView = popup.GetComponent<ResourceExchangePopupView>();
        ResourceExchangePopupRuntimeView runtimeView = popup.GetComponent<ResourceExchangePopupRuntimeView>();
        Assert.NotNull(popupView);
        Assert.NotNull(runtimeView);
        runtimeView.ConfigureForTests(popupView);
        runtimeView.SendMessage("OnEnable");
        runtimeView.RefreshNow(force: true);
    }

    private static void AssertFullyBoundPopup(GameObject popup)
    {
        Assert.IsTrue(
            IsFullyBoundPopup(popup),
            "Resource Exchange transition must install an open, active, fully bound popup.");
    }

    private static long MeasureInstrumentationControl(int samples)
    {
        long allocatedBytes = 0L;
        long timingSink = 0L;
        for (int sample = 0; sample < samples; sample++)
        {
            long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
            long startTicks = Stopwatch.GetTimestamp();
            long stopTicks = Stopwatch.GetTimestamp();
            allocatedBytes += GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
            timingSink ^= stopTicks - startTicks;
        }

        GC.KeepAlive(timingSink);
        return allocatedBytes;
    }

    private static void WarmInstrumentation()
    {
        for (int sample = 0; sample < 8; sample++)
        {
            long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
            long startTicks = Stopwatch.GetTimestamp();
            long stopTicks = Stopwatch.GetTimestamp();
            GC.KeepAlive(
                GC.GetAllocatedBytesForCurrentThread() - allocationBefore + stopTicks - startTicks);
        }
    }

    private static void CollectManagedMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static TimingMetrics SummarizeTiming(long[] samples)
    {
        Assert.NotNull(samples);
        Assert.Greater(samples.Length, 0, "Timing measurement must contain at least one sample.");
        Array.Sort(samples);

        long totalTicks = 0L;
        for (int i = 0; i < samples.Length; i++)
        {
            Assert.GreaterOrEqual(samples[i], 0L, "Stopwatch samples cannot be negative.");
            totalTicks += samples[i];
        }

        return new TimingMetrics(
            TicksToMilliseconds(totalTicks),
            TicksToMilliseconds(totalTicks) / samples.Length,
            TicksToMilliseconds(PercentileSorted(samples, 0.95d)),
            TicksToMilliseconds(PercentileSorted(samples, 0.99d)),
            TicksToMilliseconds(samples[samples.Length - 1]));
    }

    private static double PercentileSorted(long[] samples, double percentile)
    {
        double position = (samples.Length - 1) * percentile;
        int lower = (int)Math.Floor(position);
        int upper = Math.Min(samples.Length - 1, lower + 1);
        double blend = position - lower;
        return samples[lower] + (samples[upper] - samples[lower]) * blend;
    }

    private static double TicksToMilliseconds(double ticks)
    {
        return ticks * 1000d / Stopwatch.Frequency;
    }

    private static void WriteUnchangedReport(UnchangedMetrics metrics)
    {
        var builder = new StringBuilder(1024);
        builder.AppendLine("{");
        AppendJson(builder, "scenario", "surface-open-fully-bound-unchanged", true);
        AppendJson(builder, "warmupFrames", WarmupFrames, true);
        AppendJson(builder, "measuredFrames", MeasuredFrames, true);
        AppendJson(builder, "instrumentationControlSamples", MeasuredFrames, true);
        AppendJson(builder, "instrumentationControlAllocatedBytes", metrics.InstrumentationControlAllocatedBytes, true);
        AppendJson(builder, "focusedUiRecurringAllocatedBytes", metrics.ProductionAllocatedBytes, true);
        AppendJson(builder, "maximumFrameAllocatedBytes", metrics.MaximumFrameAllocatedBytes, true);
        AppendJson(builder, "measurementWindowAllocatedBytes", metrics.MeasurementWindowAllocatedBytes, true);
        AppendJson(builder, "harnessAllocatedBytes", metrics.HarnessAllocatedBytes, true);
        AppendJson(builder, "gatewayReadCount", metrics.GatewayReadCount, true);
        AppendJson(builder, "measuredGatewayReadCount", metrics.MeasuredGatewayReadCount, true);
        AppendJson(builder, "contentVersionBefore", metrics.ContentVersionBefore, true);
        AppendJson(builder, "contentVersionAfter", metrics.ContentVersionAfter, true);
        AppendJson(builder, "remainedFullyBound", metrics.RemainedFullyBound, true);
        AppendJson(builder, "totalMs", metrics.Timing.TotalMs, true);
        AppendJson(builder, "averageFrameMs", metrics.Timing.AverageMs, true);
        AppendJson(builder, "p95FrameMs", metrics.Timing.P95Ms, true);
        AppendJson(builder, "p99FrameMs", metrics.Timing.P99Ms, true);
        AppendJson(builder, "maxFrameMs", metrics.Timing.MaxMs, false);
        builder.AppendLine("}");
        File.WriteAllText(UnchangedReportPath, builder.ToString());
    }

    private static void WriteTransitionReport(TransitionMetrics metrics)
    {
        var builder = new StringBuilder(1024);
        builder.AppendLine("{");
        AppendJson(builder, "scenario", "resource-exchange-popup-open-close", true);
        AppendJson(builder, "warmupTransitions", WarmupTransitions, true);
        AppendJson(builder, "measuredTransitions", MeasuredTransitions, true);
        AppendJson(builder, "instrumentationControlSamples", MeasuredTransitions * 2, true);
        AppendJson(builder, "instrumentationControlAllocatedBytes", metrics.InstrumentationControlAllocatedBytes, true);
        AppendJson(builder, "focusedUiOpenAllocatedBytes", metrics.OpenAllocatedBytes, true);
        AppendJson(builder, "focusedUiCloseAllocatedBytes", metrics.CloseAllocatedBytes, true);
        AppendJson(builder, "focusedUiOpenCloseAllocatedBytes", metrics.OpenAllocatedBytes + metrics.CloseAllocatedBytes, true);
        AppendJson(builder, "maximumOpenAllocatedBytes", metrics.MaximumOpenAllocatedBytes, true);
        AppendJson(builder, "maximumCloseAllocatedBytes", metrics.MaximumCloseAllocatedBytes, true);
        AppendJson(builder, "contentVersionBefore", metrics.ContentVersionBefore, true);
        AppendJson(builder, "contentVersionAfter", metrics.ContentVersionAfter, true);
        AppendJson(builder, "expectedContentVersionAfter", metrics.ExpectedContentVersionAfter, true);
        AppendJson(builder, "everyOpenFullyBound", metrics.EveryOpenFullyBound, true);
        AppendJson(builder, "everyCloseDestroyedPopup", metrics.EveryCloseDestroyedPopup, true);
        AppendJson(builder, "averageOpenMs", metrics.OpenTiming.AverageMs, true);
        AppendJson(builder, "p95OpenMs", metrics.OpenTiming.P95Ms, true);
        AppendJson(builder, "p99OpenMs", metrics.OpenTiming.P99Ms, true);
        AppendJson(builder, "maxOpenMs", metrics.OpenTiming.MaxMs, true);
        AppendJson(builder, "averageCloseMs", metrics.CloseTiming.AverageMs, true);
        AppendJson(builder, "p95CloseMs", metrics.CloseTiming.P95Ms, true);
        AppendJson(builder, "p99CloseMs", metrics.CloseTiming.P99Ms, true);
        AppendJson(builder, "maxCloseMs", metrics.CloseTiming.MaxMs, false);
        builder.AppendLine("}");
        File.WriteAllText(TransitionReportPath, builder.ToString());
    }

    private static void AppendJson(StringBuilder builder, string name, string value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": \"").Append(value).Append('"');
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, long value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value);
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, bool value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value ? "true" : "false");
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, double value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ")
            .Append(value.ToString("R", CultureInfo.InvariantCulture));
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeShellPopupPerformanceValidation> action,
        ref int passed)
    {
        var validation = new ResourceExchangeShellPopupPerformanceValidation();
        try
        {
            action(validation);
            passed++;
        }
        finally
        {
            validation.TearDown();
        }
    }

    private static T FindInScene<T>(Scene scene) where T : UnityEngine.Object
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void AssertRegionContainsOnly(
        UIShellView shellView,
        UIShellRegionId regionId,
        GameObject expected)
    {
        Assert.IsTrue(shellView.TryGetRegion(regionId, out UIShellRegionView region));
        Assert.NotNull(region.ContentRoot);
        Assert.AreEqual(1, region.ContentRoot.childCount);
        Assert.AreSame(expected, region.ContentRoot.GetChild(0).gameObject);
    }

    private static void AssertRegionIsEmpty(UIShellView shellView, UIShellRegionId regionId)
    {
        Assert.IsTrue(shellView.TryGetRegion(regionId, out UIShellRegionView region));
        Assert.NotNull(region.ContentRoot);
        Assert.AreEqual(0, region.ContentRoot.childCount);
    }

    private static UiResourceExchangeModel CreateStableExchangeModel()
    {
        UiResourceExchangeRecipeCardModel recipe = new(
            true,
            true,
            true,
            false,
            false,
            0,
            "exchange.export_oil_credits.standard",
            "EXPORT OIL",
            "100 OIL",
            "46 CREDITS",
            "00:30",
            string.Empty);
        UiResourceExchangeQueueRowModel row = new(
            true,
            true,
            true,
            false,
            false,
            401,
            0,
            UiResourceExchangeQueueStateKind.InProgress,
            "1",
            "Export Oil",
            "100 OIL",
            "46 CREDITS",
            "00:11",
            "65%",
            "IN PROGRESS",
            0.65f);
        UiResourceExchangeDetailModel detail = new(
            "exchange.export_oil_credits.standard",
            "Export Oil",
            "EXPORT",
            "1 OIL -> 0.47 CREDITS",
            "100",
            "100 OIL",
            "46 CREDITS",
            "00:30",
            "Requires Oil Pump",
            "Confirm to start a timed logistics exchange.",
            true,
            false);

        return new UiResourceExchangeModel(
            17,
            UiResourceExchangeTabKind.Export,
            0,
            1,
            0,
            1,
            1,
            0,
            6,
            "1/6",
            "2,400",
            "620",
            "180",
            "310",
            "7",
            true,
            true,
            false,
            detail,
            1,
            recipe,
            default,
            default,
            default,
            default,
            default,
            default,
            1,
            row,
            default,
            default,
            default);
    }

    private readonly struct TimingMetrics
    {
        public readonly double TotalMs;
        public readonly double AverageMs;
        public readonly double P95Ms;
        public readonly double P99Ms;
        public readonly double MaxMs;

        public TimingMetrics(double totalMs, double averageMs, double p95Ms, double p99Ms, double maxMs)
        {
            TotalMs = totalMs;
            AverageMs = averageMs;
            P95Ms = p95Ms;
            P99Ms = p99Ms;
            MaxMs = maxMs;
        }
    }

    private readonly struct UnchangedMetrics
    {
        public readonly long ProductionAllocatedBytes;
        public readonly long MaximumFrameAllocatedBytes;
        public readonly long MeasurementWindowAllocatedBytes;
        public readonly long HarnessAllocatedBytes;
        public readonly long InstrumentationControlAllocatedBytes;
        public readonly int GatewayReadCount;
        public readonly int MeasuredGatewayReadCount;
        public readonly int ContentVersionBefore;
        public readonly int ContentVersionAfter;
        public readonly bool RemainedFullyBound;
        public readonly TimingMetrics Timing;

        public UnchangedMetrics(
            long productionAllocatedBytes,
            long maximumFrameAllocatedBytes,
            long measurementWindowAllocatedBytes,
            long harnessAllocatedBytes,
            long instrumentationControlAllocatedBytes,
            int gatewayReadCount,
            int measuredGatewayReadCount,
            int contentVersionBefore,
            int contentVersionAfter,
            bool remainedFullyBound,
            TimingMetrics timing)
        {
            ProductionAllocatedBytes = productionAllocatedBytes;
            MaximumFrameAllocatedBytes = maximumFrameAllocatedBytes;
            MeasurementWindowAllocatedBytes = measurementWindowAllocatedBytes;
            HarnessAllocatedBytes = harnessAllocatedBytes;
            InstrumentationControlAllocatedBytes = instrumentationControlAllocatedBytes;
            GatewayReadCount = gatewayReadCount;
            MeasuredGatewayReadCount = measuredGatewayReadCount;
            ContentVersionBefore = contentVersionBefore;
            ContentVersionAfter = contentVersionAfter;
            RemainedFullyBound = remainedFullyBound;
            Timing = timing;
        }
    }

    private readonly struct TransitionMetrics
    {
        public readonly long OpenAllocatedBytes;
        public readonly long CloseAllocatedBytes;
        public readonly long MaximumOpenAllocatedBytes;
        public readonly long MaximumCloseAllocatedBytes;
        public readonly long InstrumentationControlAllocatedBytes;
        public readonly int ContentVersionBefore;
        public readonly int ContentVersionAfter;
        public readonly int ExpectedContentVersionAfter;
        public readonly bool EveryOpenFullyBound;
        public readonly bool EveryCloseDestroyedPopup;
        public readonly TimingMetrics OpenTiming;
        public readonly TimingMetrics CloseTiming;

        public TransitionMetrics(
            long openAllocatedBytes,
            long closeAllocatedBytes,
            long maximumOpenAllocatedBytes,
            long maximumCloseAllocatedBytes,
            long instrumentationControlAllocatedBytes,
            int contentVersionBefore,
            int contentVersionAfter,
            int expectedContentVersionAfter,
            bool everyOpenFullyBound,
            bool everyCloseDestroyedPopup,
            TimingMetrics openTiming,
            TimingMetrics closeTiming)
        {
            OpenAllocatedBytes = openAllocatedBytes;
            CloseAllocatedBytes = closeAllocatedBytes;
            MaximumOpenAllocatedBytes = maximumOpenAllocatedBytes;
            MaximumCloseAllocatedBytes = maximumCloseAllocatedBytes;
            InstrumentationControlAllocatedBytes = instrumentationControlAllocatedBytes;
            ContentVersionBefore = contentVersionBefore;
            ContentVersionAfter = contentVersionAfter;
            ExpectedContentVersionAfter = expectedContentVersionAfter;
            EveryOpenFullyBound = everyOpenFullyBound;
            EveryCloseDestroyedPopup = everyCloseDestroyedPopup;
            OpenTiming = openTiming;
            CloseTiming = closeTiming;
        }
    }

    private sealed class StableGateway : IUiShellRuntimeGateway
    {
        private readonly UiResourceExchangeModel _exchangeModel;

        public int ResourceExchangeReadCount { get; private set; }

        public StableGateway(UiResourceExchangeModel exchangeModel)
        {
            _exchangeModel = exchangeModel;
        }

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;
        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId) => true;
        public bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover) => false;
        public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading) { loading = default; return false; }
        public bool TrySetLoadingProgress(float progress01, string status, bool complete) => false;
        public bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics) { diagnostics = UiDiagnosticsOverlayModel.Default; return false; }
        public bool TryReadShellState(out UiShellStateModel state) { state = default; return false; }
        public bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile) { profile = default; return false; }
        public bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources) { resources = default; return false; }
        public bool TryReadMissionResult(out UiMissionResultPopupModel result) { result = UiMissionResultPopupModel.VictoryDefault; return false; }
        public bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection) { selection = UiMatchHudSelectionPanelModel.Hidden; return false; }
        public bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel state) { state = default; return false; }
        public bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header) { header = UiMatchHudHeaderModel.Default; return false; }
        public bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel surfaces) { surfaces = UiMatchHudStatusSurfacesModel.Default; return false; }
        public bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel panel) { panel = UiAssistantPanelModel.Empty; return false; }
        public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel highlight) { highlight = UiAssistantHighlightModel.Empty; return false; }
        public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap) { minimap = UiMatchHudMinimapModel.Default; return false; }
        public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel drawer) { drawer = UiMatchHudPassengerDrawerModel.Hidden; return false; }
        public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel tray) { tray = UiMatchHudSquadTrayModel.Default; return false; }
        public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer) { drawer = UiBuildDrawerModel.Empty; return false; }

        public bool TryReadResourceExchange(out UiResourceExchangeModel exchange)
        {
            ResourceExchangeReadCount++;
            exchange = _exchangeModel;
            return true;
        }

        public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel bar) { bar = UiBuildPlacementConfirmationBarModel.Hidden; return false; }
        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category) { category = ArmoryCatalogCategory.Characters; return false; }
        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) => false;
        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) => false;
        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) => false;
    }
}
#endif
