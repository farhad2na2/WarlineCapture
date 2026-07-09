#if ENABLE_PROFILER && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class MatchGcAllocationCallstackCaptureTests
{
    private const string SelectionCallstack =
        "#0 (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] " +
        "Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:" +
        "<Initialize>g__UpdateSelectionRuntimePhases|7 ()";

    public static void RunFocusedValidation()
    {
        try
        {
            MatchGcAllocationCallstackCaptureTests tests = new();
            tests.RawSelectionAllocationRemainsPlayerRelevant();
            tests.SelectionStackOnUnrelatedHierarchyRemainsPlayerRelevant();
            tests.UnavailableAttributionRemainsPlayerRelevant();
            tests.SmearedSelectionStackDoesNotExcludePathfindingAllocation();
            tests.SmearedToolingStackDoesNotExcludePlayerHierarchy();
            tests.ToolingHierarchyIsExcluded();
            tests.BurstCompilerThreadIsExcluded();
            tests.AcceptanceConstantsRemainStrict();
            tests.DistinctRawSamplesRetainDistinctStacks();
            tests.PermutedRawIndicesRetainTheirOwnByteValues();
            tests.RawIndexCountMismatchFailsBeforeRecording();
            tests.OutOfRangeRawIndexFailsBeforeRecording();
            tests.UnavailableRawMetadataFailsBeforeRecording();
            tests.UnavailableOrMalformedRawByteMetadataFailsBeforeRecording();
            tests.SampleByteMismatchFailsBeforeRecording();
            tests.UnresolvedItemsUseAuthoritativeMergedSampleCount();
            Debug.Log("[MatchGcAllocationAttributionValidation] result=Passed tests=16");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[MatchGcAllocationAttributionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RawSelectionAllocationRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "MatchSceneView.Update > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc",
            SelectionCallstack));
    }

    [Test]
    public void SelectionStackOnUnrelatedHierarchyRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "SimulationSystemGroup > Game.Runtime.UnitPathfindingSystem > GC.Alloc",
            SelectionCallstack));
    }

    [Test]
    public void UnavailableAttributionRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "MatchSceneView.Update > GameplayRuntimeUpdate.Selection > GC.Alloc",
            "(raw allocation attribution unavailable: rawSampleCallstackUnavailable)"));
    }

    [Test]
    public void SmearedSelectionStackDoesNotExcludePathfindingAllocation()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "SimulationSystemGroup > Game.Runtime.UnitPathfindingSystem > GC.Alloc",
            SelectionCallstack));
    }

    [Test]
    public void SmearedToolingStackDoesNotExcludePlayerHierarchy()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "SimulationSystemGroup > Game.Runtime.TransportBoardingCommandSystem > GC.Alloc",
            "#0 Unity.AI.Tracing.TraceWriter:WriteEventInternal ()"));
    }

    [Test]
    public void ToolingHierarchyIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Main Thread",
            "EditorApplication.Update > Unity.AI.MCP.Editor.Bridge.Update > GC.Alloc",
            "#0 Game.Runtime.RealPlayerAllocator:Allocate ()"));
    }

    [Test]
    public void BurstCompilerThreadIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Burst-CompilerThread-3",
            "GC.Alloc",
            "#0 Game.Runtime.RealPlayerAllocator:Allocate ()"));
    }

    [Test]
    public void AcceptanceConstantsRemainStrict()
    {
        Assert.AreEqual(1024L, ReadConstant("SteadyStatePlayerRelevantGcBudgetBytes"));
        Assert.AreEqual(180L, ReadConstant("WarmupFrameCount"));
        Assert.AreEqual(300L, ReadConstant("CaptureFrameCount"));
    }

    [Test]
    public void DistinctRawSamplesRetainDistinctStacks()
    {
        List<string> resolved = new();
        bool success = TryResolveRawSamples(
            itemBytes: 160,
            mergedSampleCount: 2,
            rawSampleIndices: new[] { 11, 22 },
            rawSampleCount: 30,
            sampleNameResolver: _ => "GC.Alloc",
            allocationBytesResolver: rawSampleIndex => rawSampleIndex == 11 ? 64L : 96L,
            callstackResolver: rawSampleIndex => rawSampleIndex == 11 ? "Stack-A" : "Stack-B",
            resolvedSample: (rawSampleIndex, bytes, callstack) =>
                resolved.Add($"{rawSampleIndex}:{bytes}:{callstack}"),
            out string failureReason);

        Assert.IsTrue(success, failureReason);
        Assert.IsEmpty(failureReason);
        CollectionAssert.AreEqual(
            new[] { "11:64:Stack-A", "22:96:Stack-B" },
            resolved);
    }

    [Test]
    public void PermutedRawIndicesRetainTheirOwnByteValues()
    {
        List<string> resolved = new();
        bool success = TryResolveRawSamples(
            itemBytes: 160,
            mergedSampleCount: 2,
            rawSampleIndices: new[] { 22, 11 },
            rawSampleCount: 30,
            sampleNameResolver: _ => "GC.Alloc",
            allocationBytesResolver: rawSampleIndex => rawSampleIndex == 11 ? 64L : 96L,
            callstackResolver: rawSampleIndex => rawSampleIndex == 11 ? "Stack-A" : "Stack-B",
            resolvedSample: (rawSampleIndex, bytes, callstack) =>
                resolved.Add($"{rawSampleIndex}:{bytes}:{callstack}"),
            out string failureReason);

        Assert.IsTrue(success, failureReason);
        CollectionAssert.AreEqual(
            new[] { "22:96:Stack-B", "11:64:Stack-A" },
            resolved);
    }

    [Test]
    public void RawIndexCountMismatchFailsBeforeRecording()
    {
        List<string> resolved = new();
        bool success = TryResolveRawSamples(
            160,
            2,
            new[] { 11 },
            30,
            _ => "GC.Alloc",
            _ => 80L,
            _ => "Stack",
            (_, _, callstack) => resolved.Add(callstack),
            out string failureReason);

        Assert.IsFalse(success);
        Assert.AreEqual("rawIndexCountMismatch:1/2", failureReason);
        Assert.IsEmpty(resolved);
    }

    [Test]
    public void OutOfRangeRawIndexFailsBeforeRecording()
    {
        List<string> resolved = new();
        bool success = TryResolveRawSamples(
            64,
            1,
            new[] { 30 },
            30,
            _ => "GC.Alloc",
            _ => 64L,
            _ => "Stack",
            (_, _, callstack) => resolved.Add(callstack),
            out string failureReason);

        Assert.IsFalse(success);
        Assert.AreEqual("rawSampleIndexOutOfRange:30/30", failureReason);
        Assert.IsEmpty(resolved);
    }

    [Test]
    public void UnavailableRawMetadataFailsBeforeRecording()
    {
        List<string> resolved = new();
        bool missingName = TryResolveRawSamples(
            64,
            1,
            new[] { 4 },
            10,
            _ => null,
            _ => 64L,
            _ => "Stack",
            (_, _, callstack) => resolved.Add(callstack),
            out string nameFailureReason);
        bool missingCallstack = TryResolveRawSamples(
            64,
            1,
            new[] { 4 },
            10,
            _ => "GC.Alloc",
            _ => 64L,
            _ => string.Empty,
            (_, _, callstack) => resolved.Add(callstack),
            out string callstackFailureReason);

        Assert.IsFalse(missingName);
        Assert.AreEqual("rawSampleNameMismatch:<null>", nameFailureReason);
        Assert.IsFalse(missingCallstack);
        Assert.AreEqual("rawSampleCallstackUnavailable", callstackFailureReason);
        Assert.IsEmpty(resolved);
    }

    [Test]
    public void UnavailableOrMalformedRawByteMetadataFailsBeforeRecording()
    {
        List<string> resolved = new();
        bool unavailable = TryResolveRawSamples(
            64,
            1,
            new[] { 4 },
            10,
            _ => "GC.Alloc",
            _ => throw new IndexOutOfRangeException(),
            _ => "Stack",
            (_, _, callstack) => resolved.Add(callstack),
            out string unavailableReason);
        bool malformed = TryResolveRawSamples(
            64,
            1,
            new[] { 4 },
            10,
            _ => "GC.Alloc",
            _ => 0L,
            _ => "Stack",
            (_, _, callstack) => resolved.Add(callstack),
            out string malformedReason);

        Assert.IsFalse(unavailable);
        Assert.AreEqual("rawSampleByteMetadataException:IndexOutOfRangeException", unavailableReason);
        Assert.IsFalse(malformed);
        Assert.AreEqual("sampleBytesNonPositive:0", malformedReason);
        Assert.IsEmpty(resolved);
    }

    [Test]
    public void SampleByteMismatchFailsBeforeRecording()
    {
        List<string> resolved = new();
        bool success = TryResolveRawSamples(
            128,
            2,
            new[] { 4, 5 },
            10,
            _ => "GC.Alloc",
            _ => 32L,
            rawSampleIndex => $"Stack-{rawSampleIndex}",
            (_, _, callstack) => resolved.Add(callstack),
            out string failureReason);

        Assert.IsFalse(success);
        Assert.AreEqual("sampleByteTotalMismatch:64/128", failureReason);
        Assert.IsEmpty(resolved);
    }

    [Test]
    public void UnresolvedItemsUseAuthoritativeMergedSampleCount()
    {
        Assert.AreEqual(7, ResolveUnresolvedSampleCount(7, 1));
        Assert.AreEqual(4, ResolveUnresolvedSampleCount(0, 4));
        Assert.AreEqual(1, ResolveUnresolvedSampleCount(0, 0));
    }

    private static bool ShouldExclude(
        string threadName,
        string hierarchyPath,
        string callstack)
    {
        MethodInfo method = typeof(MatchGcAllocationCallstackCapture).GetMethod(
            "ShouldExcludeAllocationForClassification",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (bool)method.Invoke(
            null,
            new object[]
            {
                threadName,
                hierarchyPath,
                callstack
            });
    }

    private static bool TryResolveRawSamples(
        long itemBytes,
        int mergedSampleCount,
        IReadOnlyList<int> rawSampleIndices,
        int rawSampleCount,
        Func<int, string> sampleNameResolver,
        Func<int, long> allocationBytesResolver,
        Func<int, string> callstackResolver,
        Action<int, long, string> resolvedSample,
        out string failureReason)
    {
        MethodInfo method = typeof(MatchGcAllocationCallstackCapture).GetMethod(
            "TryResolveRawAllocationSamples",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        object[] arguments =
        {
            itemBytes,
            mergedSampleCount,
            rawSampleIndices,
            rawSampleCount,
            sampleNameResolver,
            allocationBytesResolver,
            callstackResolver,
            resolvedSample,
            null
        };
        bool result = (bool)method.Invoke(null, arguments);
        failureReason = arguments[8] as string;
        return result;
    }

    private static int ResolveUnresolvedSampleCount(
        int mergedSampleCount,
        int rawItemSampleCount)
    {
        MethodInfo method = typeof(MatchGcAllocationCallstackCapture).GetMethod(
            "ResolveUnresolvedSampleCount",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (int)method.Invoke(
            null,
            new object[] { mergedSampleCount, rawItemSampleCount });
    }

    private static long ReadConstant(string fieldName)
    {
        FieldInfo field = typeof(MatchGcAllocationCallstackCapture).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field);
        return Convert.ToInt64(field.GetRawConstantValue());
    }
}
#endif
