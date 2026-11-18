using Game.Configs;
using NUnit.Framework;
using UnityEngine;

/// <summary>Per-mission HUD guidance data (owner designer playtest 2026-11-17, S026).</summary>
public sealed class SkirmishMissionHudGuidanceTests
{
    [Test]
    public void S026HasAFirstOrderAndARendezvousOnAnExistingAnchorId()
    {
        Assert.IsTrue(SkirmishMissionHudGuidanceCatalog.TryGet("S026", out var guidance));
        Assert.IsTrue(guidance.HasFirstOrder);
        Assert.IsTrue(guidance.HasRendezvous);
        StringAssert.StartsWith("anchor.skirmish.s026.", guidance.RendezvousAnchorId);
        Assert.IsFalse(SkirmishMissionHudGuidanceCatalog.TryGet("S002", out _), "missions without a row show nothing");
    }

    [Test]
    public void FirstOrderShowsOnlyDuringTheOpening()
    {
        SkirmishMissionHudGuidanceCatalog.TryGet("S026", out var guidance);
        Assert.IsTrue(SkirmishMissionHudGuidanceCatalog.ShowFirstOrder(guidance, 0f));
        Assert.IsTrue(SkirmishMissionHudGuidanceCatalog.ShowFirstOrder(guidance, SkirmishMissionHudGuidanceCatalog.FirstOrderSeconds - .1f));
        Assert.IsFalse(SkirmishMissionHudGuidanceCatalog.ShowFirstOrder(guidance, SkirmishMissionHudGuidanceCatalog.FirstOrderSeconds));
        Assert.IsFalse(SkirmishMissionHudGuidanceCatalog.ShowFirstOrder(default, 1f));
    }

    public static void RunFocusedValidation()
    {
        var tests = new SkirmishMissionHudGuidanceTests();
        tests.S026HasAFirstOrderAndARendezvousOnAnExistingAnchorId();
        tests.FirstOrderShowsOnlyDuringTheOpening();
        Debug.Log("[SkirmishMissionHudGuidance] result=Passed");
    }
}
