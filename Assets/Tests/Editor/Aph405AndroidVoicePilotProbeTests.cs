using System.Collections.Generic;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class Aph405AndroidVoicePilotProbeTests
{
    [Test]
    public void SelectEligibleClips_FiltersPolicyDeduplicatesAndSortsDeterministically()
    {
        List<Aph405VoicePilotDiscoveryDescriptor> descriptors = new()
        {
            Descriptor(8, "VO.Zulu", "Voice", "zulu", compressed: true, preload: false),
            Descriptor(4, "VO.Alpha", "Voice", "alpha", compressed: true, preload: false),
            Descriptor(4, "VO.Duplicate", "Voice", "alpha", compressed: true, preload: false),
            Descriptor(5, "VO.Preloaded", "Voice", "preloaded", compressed: true, preload: true),
            Descriptor(6, "VO.Decoded", "Voice", "decoded", compressed: false, preload: false),
            Descriptor(7, "UI.Click", "UI", "ui", compressed: true, preload: false)
        };

        List<Aph405VoicePilotDiscoveryDescriptor> selected =
            Aph405VoicePilotProbeContract.SelectEligibleClips(descriptors);

        Assert.AreEqual(2, selected.Count);
        Assert.AreEqual("VO.Alpha", selected[0].EventId);
        Assert.AreEqual("VO.Zulu", selected[1].EventId);
    }

    [Test]
    public void DiscoveryMarker_PassesOnlyForExactlyEightClips()
    {
        Assert.AreEqual(
            "[APH405VoicePilot] phase=Discovery result=Passed expected=8 actual=8",
            Aph405VoicePilotProbeContract.FormatDiscoveryMarker(8));
        Assert.AreEqual(
            "[APH405VoicePilot] phase=Discovery result=Failed expected=8 actual=7",
            Aph405VoicePilotProbeContract.FormatDiscoveryMarker(7));
    }

    [Test]
    public void ClipMarker_IsMachineParseableInvariantAndEscapesValues()
    {
        Aph405VoicePilotClipResult result = new(
            2,
            "VO.ARIA Test",
            "clip test",
            true,
            12.5d,
            0.75d,
            AudioDataLoadState.Unloaded,
            AudioDataLoadState.Loaded,
            AudioDataLoadState.Loaded,
            100L,
            200L,
            200L,
            "None");

        string marker = Aph405VoicePilotProbeContract.FormatClipMarker(result);

        StringAssert.Contains("result=Passed", marker);
        StringAssert.Contains("event=VO.ARIA%20Test", marker);
        StringAssert.Contains("clip=clip%20test", marker);
        StringAssert.Contains("firstPlayLatencyMs=12.500", marker);
        StringAssert.Contains("repeatedPlayLatencyMs=0.750", marker);
        StringAssert.Contains("beforeRuntimeMemoryBytes=100", marker);
        StringAssert.Contains("afterRepeatedRuntimeMemoryBytes=200", marker);
    }

    [Test]
    public void SummaryMarker_FailsForPartialOrFailedRun()
    {
        Assert.AreEqual(
            "[APH405VoicePilot] phase=Summary result=Passed expected=8 passed=8 failed=0",
            Aph405VoicePilotProbeContract.FormatSummaryMarker(8, 0));
        Assert.AreEqual(
            "[APH405VoicePilot] phase=Summary result=Failed expected=8 passed=7 failed=1",
            Aph405VoicePilotProbeContract.FormatSummaryMarker(7, 1));
    }

    [Test]
    public void CommandLineArgument_RequiresExactOptInToken()
    {
        string[] arguments = { "Unity", "-batchmode", "-aph405VoicePilot" };

        Assert.IsTrue(Aph405VoicePilotProbeContract.HasCommandLineArgument(
            arguments,
            Aph405VoicePilotProbeContract.EditorCommandLineArgument));
        Assert.IsFalse(Aph405VoicePilotProbeContract.HasCommandLineArgument(
            arguments,
            "aph405VoicePilot"));
        Assert.IsFalse(Aph405VoicePilotProbeContract.HasCommandLineArgument(
            null,
            Aph405VoicePilotProbeContract.EditorCommandLineArgument));
    }

    [Test]
    public void RuntimeView_DoesNotAutoRunProbeForEveryAndroidDevelopmentBuild()
    {
        const string runtimeViewPath =
            "Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationRuntimeView.cs";
        string source = System.IO.File.ReadAllText(runtimeViewPath);

        StringAssert.Contains("System.Environment.GetCommandLineArgs()", source);
        StringAssert.DoesNotContain("Application.platform == RuntimePlatform.Android", source);
    }

    private static Aph405VoicePilotDiscoveryDescriptor Descriptor(
        int identity,
        string eventId,
        string busId,
        string clipName,
        bool compressed,
        bool preload)
    {
        return new Aph405VoicePilotDiscoveryDescriptor(
            identity,
            eventId,
            busId,
            clipName,
            compressed,
            preload);
    }
}
