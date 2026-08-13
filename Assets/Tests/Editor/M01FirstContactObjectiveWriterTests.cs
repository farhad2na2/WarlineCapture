using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class M01FirstContactObjectiveWriterTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.WriterProjectsDeterministicProgressAndCompletion()); passed++;
            RunCase(test => test.WriterProjectsCommandSquadFailure()); passed++;
            RunCase(test => test.DuplicateAndStaleSourcesAreRejected()); passed++;
            ExactlyOneProductionObjectiveWriterExists(); passed++;
            ReadersDoNotWriteAuthoritativeObjectiveTruth(); passed++;
            Debug.Log($"[M01FirstContactObjectiveWriterValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01FirstContactObjectiveWriterValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<M01FirstContactObjectiveWriterTests> testCase)
    {
        testCase(new M01FirstContactObjectiveWriterTests());
    }

    [Test]
    public void WriterProjectsDeterministicProgressAndCompletion()
    {
        using World world = CreateWorld(out Entity root, out Entity boundary, out SystemHandle writer);
        EntityManager em = world.EntityManager;
        Run(world, writer);
        MatchObjectiveRuntimeStateComponent initial =
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
            em.GetBuffer<MatchObjectiveRuntimeElement>(boundary);
        Assert.AreEqual(1u, initial.Version);
        Assert.AreEqual(1u, initial.MissionSourceVersion);
        Assert.AreEqual(2, objectives.Length);
        Assert.AreEqual("obj.ch01.m01.destroy_patrol", objectives[0].ObjectiveId.ToString());
        Assert.AreEqual("Patrol neutralized 1/3", objectives[0].Body.ToString());
        Assert.AreEqual(MatchObjectiveState.Active, objectives[0].State);

        CampaignMissionRuntimeComponent runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
        runtime.Version = 2;
        runtime.Phase = MissionPhaseKind.SecureCorridor;
        em.SetComponentData(root, runtime);
        CampaignMissionAttemptFactsComponent facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
        facts.ElapsedMilliseconds = 91234;
        facts.HostileDefeatedCount = 3;
        em.SetComponentData(root, facts);
        Run(world, writer);

        MatchObjectiveRuntimeStateComponent complete =
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        objectives = em.GetBuffer<MatchObjectiveRuntimeElement>(boundary);
        Assert.AreEqual(2u, complete.Version);
        Assert.AreEqual(2u, complete.MissionSourceVersion);
        Assert.AreEqual(91, complete.ElapsedWholeSeconds);
        Assert.AreEqual(MatchObjectiveState.Complete, objectives[0].State);
        Assert.AreEqual("Patrol neutralized 3/3", objectives[0].Body.ToString());
    }

    [Test]
    public void WriterProjectsCommandSquadFailure()
    {
        using World world = CreateWorld(out Entity root, out Entity boundary, out SystemHandle writer);
        EntityManager em = world.EntityManager;
        CampaignMissionRuntimeComponent runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
        runtime.Version = 2;
        runtime.Phase = MissionPhaseKind.Result;
        runtime.Outcome = MissionOutcomeKind.Defeat;
        runtime.ReturnDestination = MissionReturnDestinationKind.CampaignOperations;
        em.SetComponentData(root, runtime);
        CampaignMissionAttemptFactsComponent facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
        facts.CommandSquadAlive = 0;
        em.SetComponentData(root, facts);

        Run(world, writer);

        MatchObjectiveRuntimeStateComponent state =
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
            em.GetBuffer<MatchObjectiveRuntimeElement>(boundary);
        Assert.AreEqual(0, state.MatchActive);
        Assert.AreEqual(MatchObjectiveState.Failed, objectives[0].State);
        Assert.AreEqual(MatchObjectiveState.Failed, objectives[1].State);
        Assert.AreEqual("Command squad lost", objectives[1].Body.ToString());
    }

    [Test]
    public void DuplicateAndStaleSourcesAreRejected()
    {
        using World world = CreateWorld(out Entity root, out Entity boundary, out SystemHandle writer);
        EntityManager em = world.EntityManager;
        Run(world, writer);
        MatchObjectiveRuntimeStateComponent published =
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        Run(world, writer);
        Assert.AreEqual(published.Version,
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary).Version);

        CampaignMissionRuntimeComponent runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
        runtime.Version = 2;
        em.SetComponentData(root, runtime);
        CampaignMissionAttemptFactsComponent facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
        facts.HostileDefeatedCount = 2;
        em.SetComponentData(root, facts);
        Run(world, writer);
        MatchObjectiveRuntimeStateComponent newest =
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        Assert.AreEqual(2u, newest.Version);

        runtime.Version = 1;
        em.SetComponentData(root, runtime);
        facts.HostileDefeatedCount = 1;
        em.SetComponentData(root, facts);
        Run(world, writer);
        MatchObjectiveRuntimeStateComponent afterStale =
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        Assert.AreEqual(newest.Version, afterStale.Version);
        Assert.AreEqual(newest.MissionSourceVersion, afterStale.MissionSourceVersion);
        Assert.AreEqual(2, afterStale.HostileDefeatedCount);
    }

    [Test]
    public static void ExactlyOneProductionObjectiveWriterExists()
    {
        string[] writers = Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("objectives.Clear();", StringComparison.Ordinal) &&
                           File.ReadAllText(path).Contains("MatchObjectiveRuntimeElement", StringComparison.Ordinal))
            .Select(path => path.Replace('\\', '/'))
            .ToArray();
        CollectionAssert.AreEqual(
            new[] { "Assets/Game/Scripts/Runtime/Missions/CampaignMissionObjectiveProjectionSystem.cs" }, writers);
    }

    [Test]
    public static void ReadersDoNotWriteAuthoritativeObjectiveTruth()
    {
        string assistant = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs");
        StringAssert.DoesNotContain("AddBuffer<MatchObjectiveRuntimeElement>", assistant);
        StringAssert.DoesNotContain("AddComponentData(boundary, default(MatchObjectiveRuntimeStateComponent))", assistant);
        StringAssert.DoesNotContain("SetComponentData(boundary, objectiveState)", assistant);
        StringAssert.DoesNotContain("ClearBuffer<MatchObjectiveRuntimeElement>", assistant);
    }

    private static World CreateWorld(out Entity root, out Entity boundary, out SystemHandle writer)
    {
        World world = new(nameof(M01FirstContactObjectiveWriterTests));
        EntityManager em = world.EntityManager;
        root = em.CreateEntity(
            typeof(CampaignMissionRootComponent),
            typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent));
        em.SetComponentData(root, ValidRuntime());
        em.SetComponentData(root, new CampaignMissionAttemptFactsComponent
        {
            ElapsedMilliseconds = 45000,
            HostileTotalCount = 3,
            HostileDefeatedCount = 1,
            CommandSquadSpawned = 1,
            CommandSquadAlive = 1
        });
        boundary = em.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
        writer = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
        return world;
    }

    private static CampaignMissionRuntimeComponent ValidRuntime() => new()
    {
        MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        SessionToken = new FixedString64Bytes("session-objective-tests"),
        Phase = MissionPhaseKind.Engage,
        Outcome = MissionOutcomeKind.None,
        LaunchOrigin = MissionLaunchOriginKind.FirstLaunch,
        RunKind = MissionRunKind.FirstClear,
        Version = 1,
        SourceVersion = 7,
        AttemptOrdinal = 1,
        DeterministicSeed = 7001
    };

    private static void Run(World world, SystemHandle handle)
    {
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(handle)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }
}
