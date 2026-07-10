using System;
using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class AudioMemoryPlaybackCaptureTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            AudioMemoryPlaybackCaptureTests tests = new();
            tests.CreateSnapshot_SortsClipMetadataAndAggregatesRuntimeBytesByBus();
            tests.SerializeReport_IsStableAndIncludesNullableSourceCounts();
            tests.BuildMarkdown_RecordsMemoryEventTimingBusAndClipState();
            Debug.Log("[AudioMemoryPlaybackCaptureValidation] result=Passed tests=3");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[AudioMemoryPlaybackCaptureValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CreateSnapshot_SortsClipMetadataAndAggregatesRuntimeBytesByBus()
    {
        AudioMemoryPhaseSnapshot snapshot = AudioMemoryPlaybackCapture.CreateSnapshot(
            "match-after-audio",
            elapsedSeconds: 1.25d,
            totalAllocatedMemoryBytes: 10000,
            totalReservedMemoryBytes: 20000,
            monoUsedMemoryBytes: 3000,
            monoHeapMemoryBytes: 4000,
            sourcePoolSize: 8,
            activeSourceCount: 2,
            eventSnapshot: CreateEvent(),
            catalogClips: new[]
            {
                new AudioMemoryCatalogClipSnapshot
                {
                    AssetPath = "Assets/Game/Audio/Voice/voice.wav",
                    EventIds = new List<string> { "VO.Z", "VO.A", "VO.A" },
                    BusIds = new List<string> { "Voice" },
                    LoadState = "Unloaded",
                    RuntimeMemoryBytes = 500
                },
                new AudioMemoryCatalogClipSnapshot
                {
                    AssetPath = "Assets/Game/Audio/Music/music.wav",
                    EventIds = new List<string> { "Music.Match.CalmLoop" },
                    BusIds = new List<string> { "Music", "Music" },
                    LoadState = "Loaded",
                    RuntimeMemoryBytes = 1000
                }
            });

        Assert.AreEqual(1500, snapshot.CatalogRuntimeMemoryBytes);
        Assert.AreEqual(2, snapshot.CatalogClipCount);
        Assert.AreEqual(1, snapshot.LoadedCatalogClipCount);
        Assert.AreEqual("Assets/Game/Audio/Music/music.wav", snapshot.CatalogClips[0].AssetPath);
        CollectionAssert.AreEqual(new[] { "VO.A", "VO.Z" }, snapshot.CatalogClips[1].EventIds);
        Assert.AreEqual(2, snapshot.BusTotals.Count);
        AssertBus(snapshot.BusTotals[0], "Music", 1000, 1, 1);
        AssertBus(snapshot.BusTotals[1], "Voice", 500, 1, 0);
        Assert.AreEqual("Presented", snapshot.Event.Status);
        Assert.AreEqual(3161187545u, snapshot.Event.EventHash);
    }

    [Test]
    public void SerializeReport_IsStableAndIncludesNullableSourceCounts()
    {
        AudioMemoryPlaybackReport report = CreateReport(sourcePoolSize: null, activeSourceCount: null);

        string first = AudioMemoryPlaybackCapture.SerializeReport(report);
        string second = AudioMemoryPlaybackCapture.SerializeReport(report);

        Assert.AreEqual(first, second);
        StringAssert.Contains("\"taskId\": \"APH-401\"", first);
        StringAssert.Contains("\"eventId\": \"UI.Button.Primary.Click\"", first);
        StringAssert.Contains("\"eventHash\": 3161187545", first);
        StringAssert.Contains("\"sourcePoolSize\": null", first);
        StringAssert.Contains("\"activeSourceCount\": null", first);
        StringAssert.Contains("\"rawProfilerPath\": \"Design/AgentReports/test.raw\"", first);
    }

    [Test]
    public void BuildMarkdown_RecordsMemoryEventTimingBusAndClipState()
    {
        AudioMemoryPlaybackReport report = CreateReport(sourcePoolSize: 8, activeSourceCount: 2);

        string markdown = AudioMemoryPlaybackCapture.BuildMarkdown(report);

        StringAssert.Contains("Capture target: `Menu`", markdown);
        StringAssert.Contains("Raw profiler: `Design/AgentReports/test.raw`", markdown);
        StringAssert.Contains("UI.Button.Primary.Click", markdown);
        StringAssert.Contains("3161187545", markdown);
        StringAssert.Contains("Event status: `Presented`", markdown);
        StringAssert.Contains("Triggered at: `0.500 s`", markdown);
        StringAssert.Contains("Total allocated memory: `10,000 bytes`", markdown);
        StringAssert.Contains("Total reserved memory: `20,000 bytes`", markdown);
        StringAssert.Contains("Mono used memory: `3,000 bytes`", markdown);
        StringAssert.Contains("Mono heap memory: `4,000 bytes`", markdown);
        StringAssert.Contains("| UI | 256 | 1 | 1 |", markdown);
        StringAssert.Contains("Assets/Game/Audio/UI/click.wav", markdown);
        StringAssert.Contains("| Loaded | 256 |", markdown);
    }

    private static AudioMemoryPlaybackReport CreateReport(int? sourcePoolSize, int? activeSourceCount)
    {
        AudioMemoryPlaybackReport report = new()
        {
            CaptureTarget = "Menu",
            CaptureResult = "Succeeded",
            UnityVersion = "6000.5.2f1",
            JsonReportPath = "Design/AgentReports/test.json",
            MarkdownReportPath = "Design/AgentReports/test.md",
            RawProfilerPath = "Design/AgentReports/test.raw"
        };
        report.Snapshots.Add(AudioMemoryPlaybackCapture.CreateSnapshot(
            "menu-after-ui-primary-click",
            elapsedSeconds: 1.25d,
            totalAllocatedMemoryBytes: 10000,
            totalReservedMemoryBytes: 20000,
            monoUsedMemoryBytes: 3000,
            monoHeapMemoryBytes: 4000,
            sourcePoolSize: sourcePoolSize,
            activeSourceCount: activeSourceCount,
            eventSnapshot: CreateEvent(),
            catalogClips: new[]
            {
                new AudioMemoryCatalogClipSnapshot
                {
                    AssetPath = "Assets/Game/Audio/UI/click.wav",
                    EventIds = new List<string> { "UI.Button.Primary.Click" },
                    BusIds = new List<string> { "UI" },
                    LoadState = "Loaded",
                    RuntimeMemoryBytes = 256
                }
            }));
        return report;
    }

    private static AudioMemoryEventSnapshot CreateEvent()
    {
        return new AudioMemoryEventSnapshot
        {
            RequestId = 7,
            EventId = "UI.Button.Primary.Click",
            EventHash = 3161187545u,
            Status = "Presented",
            TriggeredAtSeconds = 0.5d,
            RequestedAtSeconds = 0.5d,
            ProcessedAtSeconds = 0.6d,
            ObservedAtSeconds = 0.7d
        };
    }

    private static void AssertBus(
        AudioMemoryBusSnapshot actual,
        string busId,
        long runtimeMemoryBytes,
        int clipCount,
        int loadedClipCount)
    {
        Assert.AreEqual(busId, actual.BusId);
        Assert.AreEqual(runtimeMemoryBytes, actual.RuntimeMemoryBytes);
        Assert.AreEqual(clipCount, actual.ClipCount);
        Assert.AreEqual(loadedClipCount, actual.LoadedClipCount);
    }
}
