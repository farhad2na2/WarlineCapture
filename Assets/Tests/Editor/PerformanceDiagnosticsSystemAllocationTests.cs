using Game.Rendering;
using Game.Composition;
using Game.Runtime;
using Game.Components;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Unity.Entities;

public sealed class PerformanceDiagnosticsSystemHelperAllocationTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(EndStepDoesNotAllocateAfterWarmup),
                test => test.EndStepDoesNotAllocateAfterWarmup(),
                ref passed);
            RunValidationStep(
                nameof(DefaultInitializationSkipsBroadProfilerMarkerRecorders),
                test => test.DefaultInitializationSkipsBroadProfilerMarkerRecorders(),
                ref passed);
            RunValidationStep(
                nameof(ReferenceResolverReturnsFalseBeforeRegistration),
                test => test.ReferenceResolverReturnsFalseBeforeRegistration(),
                ref passed);
            RunValidationStep(
                nameof(ReferenceResolverKeepsWorldsAndReplacementOwnersIsolated),
                test => test.ReferenceResolverKeepsWorldsAndReplacementOwnersIsolated(),
                ref passed);
            RunValidationStep(
                nameof(CapturePolicySuppressesAndRestoresDiagnosticLogging),
                test => test.CapturePolicySuppressesAndRestoresDiagnosticLogging(),
                ref passed);
            RunValidationStep(
                nameof(RuntimeVisualCounts_FollowReplacementDefaultWorld),
                test => test.RuntimeVisualCounts_FollowReplacementDefaultWorld(),
                ref passed);

            UnityEngine.Debug.Log($"[PerformanceDiagnosticsAllocationValidation] result=Passed tests={passed}");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError($"[PerformanceDiagnosticsAllocationValidation] result=Failed passed={passed}");
            throw;
        }
    }

    [Test]
    public void EndStepDoesNotAllocateAfterWarmup()
    {
        var diagnosticsSystem = new PerformanceDiagnosticsSystemHelper();
        double start = diagnosticsSystem.BeginStep();
        diagnosticsSystem.EndStep("UnitRenderBudgetSystem", start);
        for (int i = 0; i < 256; i++)
            diagnosticsSystem.EndStep("UnitRenderBudgetSystem", diagnosticsSystem.BeginStep());

        long timeBaselineBytes = CountAllocatedBytes(() =>
        {
            for (int i = 0; i < 128; i++)
            {
                diagnosticsSystem.BeginStep();
                diagnosticsSystem.BeginStep();
            }
        });

        diagnosticsSystem.BeginUpdate(gameplayActive: false);
        long measuredBytes = CountAllocatedBytes(() =>
        {
            for (int i = 0; i < 128; i++)
                diagnosticsSystem.EndStep("UnitRenderBudgetSystem", diagnosticsSystem.BeginStep());
        });

        Assert.LessOrEqual(
            measuredBytes,
            timeBaselineBytes,
            $"EndStep allocated beyond the warmed Unity time baseline. baseline={timeBaselineBytes}B measured={measuredBytes}B");
    }

    [Test]
    public void DefaultInitializationSkipsBroadProfilerMarkerRecorders()
    {
        var diagnosticsSystem = new PerformanceDiagnosticsSystemHelper();
        diagnosticsSystem.Initialize();
        try
        {
            FieldInfo markerRecordersField = typeof(PerformanceDiagnosticsSystemHelper).GetField(
                "_markerRecorders",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(markerRecordersField);
            object markerRecorders = markerRecordersField.GetValue(diagnosticsSystem);
            int count = (int)markerRecorders.GetType().GetProperty("Count").GetValue(markerRecorders);

            Assert.Zero(count, "Broad profiler marker recorders should be opt-in for normal match runtime.");
        }
        finally
        {
            diagnosticsSystem.Dispose();
        }
    }

    [Test]
    public void ReferenceResolverReturnsFalseBeforeRegistration()
    {
        using World world = new("PerformanceDiagnosticsReference-Empty");
        PerformanceDiagnosticsReferenceCompositionSystemHelper referenceSystem = new();

        Assert.IsFalse(referenceSystem.TryGet(world.EntityManager, out PerformanceDiagnosticsSystemHelper diagnostics));
        Assert.IsNull(diagnostics);
    }

    [Test]
    public void ReferenceResolverKeepsWorldsAndReplacementOwnersIsolated()
    {
        using World firstWorld = new("PerformanceDiagnosticsReference-First");
        using World secondWorld = new("PerformanceDiagnosticsReference-Second");
        PerformanceDiagnosticsSystemHelper first = new();
        PerformanceDiagnosticsSystemHelper replacement = new();
        PerformanceDiagnosticsSystemHelper second = new();
        PerformanceDiagnosticsReferenceCompositionSystemHelper firstReferences = new();
        PerformanceDiagnosticsReferenceCompositionSystemHelper secondReferences = new();
        try
        {
            firstReferences.Register(firstWorld.EntityManager, first);
            secondReferences.Register(secondWorld.EntityManager, second);
            firstReferences.Register(firstWorld.EntityManager, replacement);
            firstReferences.Clear(firstWorld.EntityManager, first);

            Assert.IsTrue(firstReferences.TryGet(firstWorld.EntityManager, out PerformanceDiagnosticsSystemHelper resolvedFirst));
            Assert.IsTrue(secondReferences.TryGet(secondWorld.EntityManager, out PerformanceDiagnosticsSystemHelper resolvedSecond));
            Assert.AreSame(replacement, resolvedFirst);
            Assert.AreSame(second, resolvedSecond);
        }
        finally
        {
            first.Dispose();
            replacement.Dispose();
            second.Dispose();
        }
    }

    [Test]
    public void CapturePolicySuppressesAndRestoresDiagnosticLogging()
    {
        PerformanceDiagnosticsSystemHelper diagnostics = new();
        bool logged = false;
        void OnLog(string condition, string stackTrace, LogType type) =>
            logged |= condition.StartsWith("[FreezeDetect] LateUpdate hitch", StringComparison.Ordinal);

        Application.logMessageReceived += OnLog;
        try
        {
            PerformanceDiagnosticsCapturePolicy.SetSuppressLogging(true);
            diagnostics.EndLateUpdate(Time.realtimeSinceStartupAsDouble - 1d, 0);
            Assert.IsFalse(logged);

            PerformanceDiagnosticsCapturePolicy.SetSuppressLogging(false);
            diagnostics.EndLateUpdate(Time.realtimeSinceStartupAsDouble - 1d, 0);
            Assert.IsTrue(logged);
        }
        finally
        {
            PerformanceDiagnosticsCapturePolicy.SetSuppressLogging(false);
            Application.logMessageReceived -= OnLog;
        }
    }

    [Test]
    public void RuntimeVisualCounts_FollowReplacementDefaultWorld()
    {
        World previousDefault = World.DefaultGameObjectInjectionWorld;
        var diagnostics = new PerformanceDiagnosticsSystemHelper();
        try
        {
            using (var firstWorld = new World("PerformanceDiagnostics_FirstWorld"))
            {
                World.DefaultGameObjectInjectionWorld = firstWorld;
                EntityArchetype unitArchetype = firstWorld.EntityManager.CreateArchetype(
                    typeof(UnitGrid),
                    typeof(Faction));
                for (int i = 0; i < 2; i++)
                    firstWorld.EntityManager.CreateEntity(unitArchetype);
                Assert.AreEqual(2, GetRuntimeUnitCount(diagnostics));
            }

            using (var secondWorld = new World("PerformanceDiagnostics_SecondWorld"))
            {
                World.DefaultGameObjectInjectionWorld = secondWorld;
                EntityArchetype unitArchetype = secondWorld.EntityManager.CreateArchetype(
                    typeof(UnitGrid),
                    typeof(Faction));
                for (int i = 0; i < 5; i++)
                    secondWorld.EntityManager.CreateEntity(unitArchetype);
                Assert.AreEqual(5, GetRuntimeUnitCount(diagnostics));
            }

            World.DefaultGameObjectInjectionWorld = null;
            Assert.AreEqual(0, GetRuntimeUnitCount(diagnostics));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousDefault != null && previousDefault.IsCreated
                ? previousDefault
                : null;
        }
    }

    private static int GetRuntimeUnitCount(PerformanceDiagnosticsSystemHelper diagnostics)
    {
        MethodInfo method = typeof(PerformanceDiagnosticsSystemHelper).GetMethod(
            "GetRuntimeVisualCounts",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        object[] values = { 0, 0, 0, 0, 0 };
        method.Invoke(diagnostics, values);
        return (int)values[0];
    }

    private static long CountAllocatedBytes(System.Action action)
    {
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        action();
        return System.GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static void RunValidationStep(
        string name,
        Action<PerformanceDiagnosticsSystemHelperAllocationTests> action,
        ref int passed)
    {
        var tests = new PerformanceDiagnosticsSystemHelperAllocationTests();
        action(tests);
        passed++;
    }
}
#endif
