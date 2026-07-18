using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class ArchitectureMenuMatchLifecycleStressPlayModeTests
{
    private const int WarmupCycleCount = 1;
    private const int MeasuredCycleCount = 10;

    [UnityTest]
    public IEnumerator SnapshotCollector_DistinguishesStableMenuAndMatch()
    {
        var context = new Aph805MenuMatchMenuLifecyclePlayModeTests.TransitionContext();
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.PrepareStableMenu(context);

        using var collector = new ArchitectureMenuMatchLifecycleSnapshotCollector(
            context.World,
            context.Menu.ContentSystem);
        ArchitectureMenuMatchLifecycleSnapshot menuBefore = collector.Capture(
            cycle: 0,
            ArchitectureLifecycleCheckpointPhase.Menu);
        AssertStableMenu(menuBefore);

        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(context);
        ArchitectureMenuMatchLifecycleSnapshot match = collector.Capture(
            cycle: 0,
            ArchitectureLifecycleCheckpointPhase.Match);
        AssertStableMatch(match, menuBefore.WorldSequence);

        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(context);
        ArchitectureMenuMatchLifecycleSnapshot menuAfter = collector.Capture(
            cycle: 0,
            ArchitectureLifecycleCheckpointPhase.Menu);
        AssertStableMenu(menuAfter);
        Assert.That(menuAfter.WorldSequence, Is.EqualTo(menuBefore.WorldSequence));
        Assert.That(menuAfter.ManagedSystemCount, Is.EqualTo(menuBefore.ManagedSystemCount));
        Assert.That(menuAfter.ShellFlowSystemHandle, Is.EqualTo(menuBefore.ShellFlowSystemHandle));
        Assert.That(menuAfter.ActionRequestSystemHandle, Is.EqualTo(menuBefore.ActionRequestSystemHandle));
        Assert.That(menuAfter.OperationMapRootCount, Is.EqualTo(menuBefore.OperationMapRootCount));
        Assert.That(menuAfter.MatchViewCount, Is.EqualTo(menuBefore.MatchViewCount));
        Assert.That(menuAfter.MatchHudCount, Is.EqualTo(menuBefore.MatchHudCount));
        Assert.That(menuAfter.MissileTrailRootCount, Is.EqualTo(menuBefore.MissileTrailRootCount));
        Assert.That(menuAfter.TotalEntityCount, Is.GreaterThanOrEqualTo(menuBefore.TotalEntityCount),
            "The first Match may initialize the named persistent scenario/catalog cache; measured cycles must plateau afterward.");
    }

    [UnityTest]
    [Timeout(300000)]
    public IEnumerator TenProductionCycles_PreserveWorldAndStableLifecycleCounts()
    {
        var context = new Aph805MenuMatchMenuLifecyclePlayModeTests.TransitionContext();
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.PrepareStableMenu(context);

        using var collector = new ArchitectureMenuMatchLifecycleSnapshotCollector(
            context.World,
            context.Menu.ContentSystem);
        for (int warmup = 0; warmup < WarmupCycleCount; warmup++)
        {
            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(context);
            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(context);
        }

        ArchitectureMenuMatchLifecycleSnapshot menuBaseline = collector.Capture(
            cycle: 0,
            ArchitectureLifecycleCheckpointPhase.Menu);
        AssertStableMenu(menuBaseline);

        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(context);
        ArchitectureMenuMatchLifecycleSnapshot matchBaseline = collector.Capture(
            cycle: 0,
            ArchitectureLifecycleCheckpointPhase.Match);
        AssertStableMatch(matchBaseline, menuBaseline.WorldSequence);
        TestContext.Out.WriteLine(menuBaseline.ToCompactString());
        TestContext.Out.WriteLine(matchBaseline.ToCompactString());
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(context);

        for (int cycle = 1; cycle <= MeasuredCycleCount; cycle++)
        {
            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(context);
            ArchitectureMenuMatchLifecycleSnapshot match = collector.Capture(
                cycle,
                ArchitectureLifecycleCheckpointPhase.Match);
            AssertStableMatch(match, menuBaseline.WorldSequence);
            AssertMatchesBaseline(matchBaseline, match);

            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(context);
            ArchitectureMenuMatchLifecycleSnapshot menu = collector.Capture(
                cycle,
                ArchitectureLifecycleCheckpointPhase.Menu);
            AssertStableMenu(menu);
            AssertMatchesBaseline(menuBaseline, menu);

            if (cycle % 5 == 0)
            {
                TestContext.Out.WriteLine(match.ToCompactString());
                TestContext.Out.WriteLine(menu.ToCompactString());
            }
        }
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnsureMatchIsUnloaded();
    }

    private static void AssertStableMenu(ArchitectureMenuMatchLifecycleSnapshot snapshot)
    {
        Assert.That(snapshot.Phase, Is.EqualTo(ArchitectureLifecycleCheckpointPhase.Menu));
        Assert.That(snapshot.LifecycleRootCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.OperationMapRootCount, Is.Zero, snapshot.ToCompactString());
        Assert.That(snapshot.MenuViewCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.MatchViewCount, Is.Zero, snapshot.ToCompactString());
        Assert.That(snapshot.MatchHudCount, Is.Zero, snapshot.ToCompactString());
        Assert.That(snapshot.EnabledAudioListenerCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.MissileTrailRootCount, Is.Zero, snapshot.ToCompactString());
        Assert.That(snapshot.AudioRuntimeViewCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.PathPoolOwnerCount, Is.Zero, snapshot.ToCompactString());
        Assert.That(snapshot.PathPoolLength, Is.Zero, snapshot.ToCompactString());
        Assert.That(snapshot.PathPoolCapacity, Is.Zero, snapshot.ToCompactString());
        Assert.That(snapshot.ShellFlowSystemHandle, Is.Not.EqualTo(Unity.Entities.SystemHandle.Null), snapshot.ToCompactString());
        Assert.That(snapshot.ActionRequestSystemHandle, Is.Not.EqualTo(Unity.Entities.SystemHandle.Null), snapshot.ToCompactString());
    }

    private static void AssertStableMatch(
        ArchitectureMenuMatchLifecycleSnapshot snapshot,
        ulong expectedWorldSequence)
    {
        Assert.That(snapshot.Phase, Is.EqualTo(ArchitectureLifecycleCheckpointPhase.Match));
        Assert.That(snapshot.WorldSequence, Is.EqualTo(expectedWorldSequence), snapshot.ToCompactString());
        Assert.That(snapshot.LifecycleRootCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.OperationMapRootCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.MenuViewCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.MatchViewCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.MatchHudCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.EnabledAudioListenerCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.AudioRuntimeViewCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.PathPoolOwnerCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.ShellFlowSystemHandle, Is.Not.EqualTo(Unity.Entities.SystemHandle.Null), snapshot.ToCompactString());
        Assert.That(snapshot.ActionRequestSystemHandle, Is.Not.EqualTo(Unity.Entities.SystemHandle.Null), snapshot.ToCompactString());
    }

    private static void AssertMatchesBaseline(
        ArchitectureMenuMatchLifecycleSnapshot expected,
        ArchitectureMenuMatchLifecycleSnapshot actual)
    {
        string message = $"Expected [{expected.ToCompactString()}] but observed [{actual.ToCompactString()}].";
        Assert.That(actual.Phase, Is.EqualTo(expected.Phase), message);
        Assert.That(actual.WorldSequence, Is.EqualTo(expected.WorldSequence), message);
        Assert.That(actual.LoadedSceneCount, Is.EqualTo(expected.LoadedSceneCount), message);
        Assert.That(actual.SceneRootCount, Is.EqualTo(expected.SceneRootCount), message);
        Assert.That(actual.TotalEntityCount, Is.EqualTo(expected.TotalEntityCount), message);
        Assert.That(actual.ManagedSystemCount, Is.EqualTo(expected.ManagedSystemCount), message);
        Assert.That(actual.ShellFlowSystemHandle, Is.EqualTo(expected.ShellFlowSystemHandle), message);
        Assert.That(actual.ActionRequestSystemHandle, Is.EqualTo(expected.ActionRequestSystemHandle), message);
        Assert.That(actual.LifecycleRootCount, Is.EqualTo(expected.LifecycleRootCount), message);
        Assert.That(actual.OperationMapRootCount, Is.EqualTo(expected.OperationMapRootCount), message);
        Assert.That(actual.MenuViewCount, Is.EqualTo(expected.MenuViewCount), message);
        Assert.That(actual.MatchViewCount, Is.EqualTo(expected.MatchViewCount), message);
        Assert.That(actual.MatchHudCount, Is.EqualTo(expected.MatchHudCount), message);
        Assert.That(actual.EnabledAudioListenerCount, Is.EqualTo(expected.EnabledAudioListenerCount), message);
        Assert.That(actual.MissileTrailRootCount, Is.EqualTo(expected.MissileTrailRootCount), message);
        Assert.That(actual.AudioRuntimeViewCount, Is.EqualTo(expected.AudioRuntimeViewCount), message);
        Assert.That(actual.AudioPoolSize, Is.EqualTo(expected.AudioPoolSize), message);
        Assert.That(actual.ActiveAudioSourceCount, Is.EqualTo(expected.ActiveAudioSourceCount), message);
        Assert.That(actual.PathPoolOwnerCount, Is.EqualTo(expected.PathPoolOwnerCount), message);
        Assert.That(actual.PathPoolLength, Is.EqualTo(expected.PathPoolLength), message);
        Assert.That(actual.PathPoolCapacity, Is.EqualTo(expected.PathPoolCapacity), message);
        Assert.That(actual.MissileTrailCreatedCount, Is.EqualTo(expected.MissileTrailCreatedCount), message);
        Assert.That(actual.MissileTrailActiveCount, Is.EqualTo(expected.MissileTrailActiveCount), message);
        Assert.That(actual.ImpactVfxCreatedCount, Is.EqualTo(expected.ImpactVfxCreatedCount), message);
        Assert.That(actual.ImpactVfxActiveCount, Is.EqualTo(expected.ImpactVfxActiveCount), message);
    }
}
