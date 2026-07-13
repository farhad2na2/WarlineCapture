#if ENABLE_PROFILER && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
    private const string UnityAiNetworkPollCallstack =
        "#0 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()\n" +
        "#1 [./Library/PackageCache/com.unity.ai.assistant@fixture/Modules/Unity.AI.Toolkit.Accounts/" +
        "Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!" +
        "Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()\n" +
        "#2 [./Library/PackageCache/com.unity.ai.assistant@fixture/Modules/Unity.AI.Toolkit.Accounts/" +
        "Services/States/SettingsState.cs:94] Unity.AI.Toolkit.Accounts.dll!" +
        "Unity.AI.Toolkit.Accounts.Services.States::SettingsState.<PollNetworkAsync>b__46_0()";

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
            tests.CaptureToolEditorUpdateIsExcluded();
            tests.FrameworkOnlyTimerSchedulerAllocationIsExcluded();
            tests.FrameworkOnlyTimerSchedulerLoopAllocationIsExcluded();
            tests.TimerSchedulerAllocationWithGameFrameRemainsPlayerRelevant();
            tests.FrameworkTimerExclusionHasNoFirstPartyTimerApiOwner();
            tests.UnityAiAssistantEditorAwaitIsExcluded();
            tests.UnityAiAssistantEditorAwaitOnGameplayHierarchyRemainsPlayerRelevant();
            tests.UnityAiAssistantEditorAwaitWithGameFrameRemainsPlayerRelevant();
            tests.UnityAiAccountNetworkPollIsExcluded();
            tests.UnityAiDirectNetworkPollIsExcluded();
            tests.UnresolvedThreadPoolAllocationRemainsPlayerRelevant();
            tests.GenericNetworkPollRemainsPlayerRelevant();
            tests.UnityAiPollOnGameplayHierarchyRemainsPlayerRelevant();
            tests.UnrelatedUnityAiAccountOperationRemainsPlayerRelevant();
            tests.UnityAiPollWithGameFrameRemainsPlayerRelevant();
            tests.ExactShellSignatureWithZeroProbeIsVerified();
            tests.AdditionalShellCandidateDoesNotMatchExactSignature();
            tests.ChangedShellSignatureIsNotVerified();
            tests.AllocatingShellProbeIsNotVerified();
            tests.ExactSelectionMarkerAggregateIsVerified();
            tests.ChangedSelectionMarkerAggregateIsNotVerified();
            tests.FocusedReadModelRemainsPlayerRelevant();
            tests.AcceptanceConstantsRemainStrict();
            tests.DistinctRawSamplesRetainDistinctStacks();
            tests.PermutedRawIndicesRetainTheirOwnByteValues();
            tests.RawIndexCountMismatchFailsBeforeRecording();
            tests.OutOfRangeRawIndexFailsBeforeRecording();
            tests.UnavailableRawMetadataFailsBeforeRecording();
            tests.UnavailableOrMalformedRawByteMetadataFailsBeforeRecording();
            tests.SampleByteMismatchFailsBeforeRecording();
            tests.UnresolvedItemsUseAuthoritativeMergedSampleCount();
            tests.MonoCompilerHierarchyIsExcluded();
            tests.EditorGiMaintenanceHierarchyIsExcluded();
            tests.SteadyStateMutationDetectionIsFailClosed();
            Debug.Log("[MatchGcAllocationAttributionValidation] result=Passed tests=41");
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
    public void CaptureToolEditorUpdateIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Main Thread",
            "Application.Tick > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc",
            "#0 Game.Editor::MatchGcAllocationCallstackCapture.Update()"));
    }

    [Test]
    public void FrameworkOnlyTimerSchedulerAllocationIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Timer-Scheduler",
            "GC.Alloc",
            "#0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper()\n" +
            "#1 mscorlib.dll!::Scheduler.FireTimer()"));
    }

    [Test]
    public void FrameworkOnlyTimerSchedulerLoopAllocationIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Timer-Scheduler",
            "GC.Alloc",
            "#0 mscorlib.dll!::Scheduler.RunSchedulerLoop()\n" +
            "#1 mscorlib.dll!::Scheduler.SchedulerThread()"));
    }

    [Test]
    public void TimerSchedulerAllocationWithGameFrameRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Timer-Scheduler",
            "GC.Alloc",
            "#0 /Assets/Game/Scripts/RuntimeJob.cs:10 Game.RuntimeJob.Run()\n" +
            "#1 mscorlib.dll!::Scheduler.FireTimer()"));
    }

    [Test]
    public void FrameworkTimerExclusionHasNoFirstPartyTimerApiOwner()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Game", "Scripts");
        foreach (string path in Directory.EnumerateFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            Assert.IsFalse(source.Contains("System.Threading.Timer", StringComparison.Ordinal), path);
            Assert.IsFalse(source.Contains("Task.Delay(", StringComparison.Ordinal), path);
            Assert.IsFalse(source.Contains("PeriodicTimer", StringComparison.Ordinal), path);
        }
    }

    [Test]
    public void UnityAiAssistantEditorAwaitIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Main Thread",
            "Application.Tick > PlayerLoop > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc",
            "#0 mscorlib.dll!System.Threading.Tasks::Task.Delay()\n" +
            "#1 [./Library/PackageCache/com.unity.ai.assistant@fixture/Editor/Assistant/Utils/TaskUtils.cs:0] " +
            "Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()"));
    }

    [Test]
    public void UnityAiAssistantEditorAwaitOnGameplayHierarchyRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "Application.Tick > PlayerLoop > GameplayRuntimeUpdate.Selection > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc",
            "#0 [./Library/PackageCache/com.unity.ai.assistant@fixture/Editor/Assistant/Utils/TaskUtils.cs:0] " +
            "Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()"));
    }

    [Test]
    public void UnityAiAssistantEditorAwaitWithGameFrameRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "Application.Tick > PlayerLoop > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc",
            "#0 [./Library/PackageCache/com.unity.ai.assistant@fixture/Editor/Assistant/Utils/TaskUtils.cs:0] " +
            "Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()\n" +
            "#1 /Assets/Game/Scripts/RuntimeAwait.cs:10 Game.Runtime.RuntimeAwait.Update()"));
    }

    [Test]
    public void MonoCompilerHierarchyIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Main Thread",
            "Application.Tick > MonoCompiler.Tick > UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc",
            "(raw allocation attribution unavailable: rawSampleCallstackUnavailable)"));
    }

    [Test]
    public void EditorGiMaintenanceHierarchyIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Main Thread",
            "Application.Tick > Application.TickGlobalCallbacks > tickGIInEditor.Invoke > GI.UpdateScene > " +
            "UnityEditor.Experimental.Rendering::ScriptableBakedReflectionSystemWrapper.get_Internal_ScriptableBakedReflectionSystemWrapper_stateHashes() [Invoke] > GC.Alloc",
            "#0 UnityEngine.Bindings::ArrayByRefMarshallingAccessor.SetEmpty()"));
    }

    [Test]
    public void SteadyStateMutationDetectionIsFailClosed()
    {
        Assert.IsFalse(HasSteadyStateMutation(0, 0, 0));
        Assert.IsTrue(HasSteadyStateMutation(1, 0, 0));
        Assert.IsTrue(HasSteadyStateMutation(0, 1, 0));
        Assert.IsTrue(HasSteadyStateMutation(0, 0, 1));
    }

    [Test]
    public void UnityAiAccountNetworkPollIsExcluded()
    {
        Assert.IsTrue(ShouldExclude("Thread Pool Worker", "GC.Alloc", UnityAiNetworkPollCallstack));
    }

    [Test]
    public void UnityAiDirectNetworkPollIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            "#0 [./Library/PackageCache/com.unity.ai.assistant@fixture/Modules/" +
            "Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] " +
            "Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::" +
            "SettingsState.PollNetworkAsync()"));
    }

    [Test]
    public void UnityAiMcpEditorSocketReadIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            "#0 Newtonsoft.Json.dll!Newtonsoft.Json.Utilities::BufferUtils.RentBuffer()\n" +
            "#1 [./Library/PackageCache/com.unity.ai.assistant@fixture/Modules/" +
            "Unity.AI.MCP.Editor/Connection/UnixSocketTransport.cs:0] " +
            "Unity.AI.MCP.Editor.dll!::<ReadUntilDelimiterAsync>d__28.MoveNext()"));
    }

    [Test]
    public void UnityAiMcpEditorSocketWriteIsExcluded()
    {
        Assert.IsTrue(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            "#0 [./Library/PackageCache/com.unity.ai.assistant@fixture/Modules/" +
            "Unity.AI.MCP.Editor/Connection/UnixSocketTransport.cs:0] " +
            "Unity.AI.MCP.Editor.dll!::<WriteAsync>d__27.MoveNext()"));
    }

    [Test]
    public void UnresolvedThreadPoolAllocationRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            "(raw allocation attribution unavailable: rawSampleCallstackUnavailable)"));
    }

    [Test]
    public void GenericNetworkPollRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            "#0 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()"));
    }

    [Test]
    public void UnityAiMcpEditorSocketWithGameFrameRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            "#0 [./Library/PackageCache/com.unity.ai.assistant@fixture/Modules/" +
            "Unity.AI.MCP.Editor/Connection/UnixSocketTransport.cs:0] " +
            "Unity.AI.MCP.Editor.dll!::<ReadUntilDelimiterAsync>d__28.MoveNext()\n" +
            "#1 /Assets/Game/Scripts/RuntimeSocket.cs:10 Game.Runtime.Socket.Read()"));
    }

    [Test]
    public void UnityAiPollOnGameplayHierarchyRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Thread Pool Worker",
            "SimulationSystemGroup > Game.Runtime.NetworkProbe > GC.Alloc",
            UnityAiNetworkPollCallstack));
    }

    [Test]
    public void UnrelatedUnityAiAccountOperationRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            "#0 [./Library/PackageCache/com.unity.ai.assistant@fixture/Modules/" +
            "Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:140] " +
            "Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::" +
            "SettingsState.RefreshInternal()"));
    }

    [Test]
    public void UnityAiPollWithGameFrameRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Thread Pool Worker",
            "GC.Alloc",
            UnityAiNetworkPollCallstack +
            "\n#3 /Assets/Game/Scripts/RuntimeNetworkProbe.cs:10 Game.Runtime.NetworkProbe.Update()"));
    }

    [Test]
    public void ExactShellSignatureWithZeroProbeIsVerified()
    {
        Assert.IsTrue(IsExactShellCaptureOverhead(
            "GC.Alloc",
            "Main Thread",
            "Application.Tick > PlayerLoop > Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc",
            "#0 /Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50 UIShellEcsPresentationSystem.Update()",
            14352,
            299,
            299,
            0,
            0,
            300));
    }

    [Test]
    public void AdditionalShellCandidateDoesNotMatchExactSignature()
    {
        Assert.IsFalse(IsExactShellCaptureOverhead(
            "GC.Alloc",
            "Main Thread",
            "Application.UpdateScene > PlayerLoop > Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc",
            "#0 /Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50 UIShellEcsPresentationSystem.Update()",
            48,
            1,
            1,
            0,
            0,
            300));
    }

    [Test]
    public void ChangedShellSignatureIsNotVerified()
    {
        Assert.IsFalse(IsExactShellCaptureOverhead(
            "GC.Alloc",
            "Main Thread",
            "Application.Tick > PlayerLoop > Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc",
            "#0 /Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50 UIShellEcsPresentationSystem.Update()",
            14353,
            299,
            299,
            0,
            0,
            300));
    }

    [Test]
    public void AllocatingShellProbeIsNotVerified()
    {
        Assert.IsFalse(IsExactShellCaptureOverhead(
            "GC.Alloc",
            "Main Thread",
            "Application.Tick > PlayerLoop > Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc",
            "#0 /Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50 UIShellEcsPresentationSystem.Update()",
            14352,
            299,
            299,
            48,
            1,
            300));
    }

    [Test]
    public void ExactSelectionMarkerAggregateIsVerified()
    {
        Assert.IsTrue(IsExactSelectionMarkerCaptureOverheadAggregate(
            2688,
            21,
            14,
            0,
            0,
            300,
            0,
            0,
            7,
            0,
            0,
            7));
    }

    [Test]
    public void ChangedSelectionMarkerAggregateIsNotVerified()
    {
        Assert.IsFalse(IsExactSelectionMarkerCaptureOverheadAggregate(
            2689,
            21,
            14,
            0,
            0,
            300,
            0,
            0,
            7,
            0,
            0,
            7));
    }

    [Test]
    public void FocusedReadModelRemainsPlayerRelevant()
    {
        Assert.IsFalse(ShouldExclude(
            "Main Thread",
            "Application.Tick > PlayerLoop > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc",
            SelectionCallstack));
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

    private static bool HasSteadyStateMutation(
        int buildingVisualCreateCalls,
        int productionTransportCreateCalls,
        int dropVisualCreateCalls)
    {
        MethodInfo method = typeof(MatchGcAllocationCallstackCapture).GetMethod(
            "HasSteadyStateMutation",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (bool)method.Invoke(
            null,
            new object[]
            {
                buildingVisualCreateCalls,
                productionTransportCreateCalls,
                dropVisualCreateCalls
            });
    }

    private static bool IsExactShellCaptureOverhead(
        string sampleName,
        string threadName,
        string hierarchyPath,
        string callstack,
        long siteBytes,
        int siteSamples,
        int siteFrames,
        long probeBytes,
        int probeAllocationSamples,
        int probeUpdateSamples)
    {
        MethodInfo method = typeof(MatchGcAllocationCallstackCapture).GetMethod(
            "IsExactShellCaptureOverheadSignature",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (bool)method.Invoke(
            null,
            new object[]
            {
                sampleName,
                threadName,
                hierarchyPath,
                callstack,
                siteBytes,
                siteSamples,
                siteFrames,
                probeBytes,
                probeAllocationSamples,
                probeUpdateSamples
            });
    }

    private static bool IsExactSelectionMarkerCaptureOverheadAggregate(
        long aggregateBytes,
        int aggregateSamples,
        int aggregateFrames,
        long totalProbeBytes,
        int totalProbeAllocationSamples,
        int totalProbeUpdateSamples,
        long focusedProbeBytes,
        int focusedProbeAllocationSamples,
        int focusedProbeUpdateSamples,
        long panelProbeBytes,
        int panelProbeAllocationSamples,
        int panelProbeUpdateSamples)
    {
        MethodInfo method = typeof(MatchGcAllocationCallstackCapture).GetMethod(
            "IsExactSelectionMarkerCaptureOverheadAggregate",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (bool)method.Invoke(
            null,
            new object[]
            {
                aggregateBytes,
                aggregateSamples,
                aggregateFrames,
                totalProbeBytes,
                totalProbeAllocationSamples,
                totalProbeUpdateSamples,
                focusedProbeBytes,
                focusedProbeAllocationSamples,
                focusedProbeUpdateSamples,
                panelProbeBytes,
                panelProbeAllocationSamples,
                panelProbeUpdateSamples
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
