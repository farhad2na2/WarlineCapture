#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class M01FirstContactPerformancePlayModeTests
{
    private const int ExpectedTests = 6;
    private static int s_Passed;

    [OneTimeSetUp]
    public void ResetPassCount() => s_Passed = 0;

    [OneTimeTearDown]
    public void PublishPassMarker()
    {
        Assert.That(s_Passed, Is.EqualTo(ExpectedTests));
        Debug.Log($"[M01FirstContactPerformancePlayMode] result=Passed tests={ExpectedTests}");
    }

    [UnityTest]
    public IEnumerator StableObjectiveProjectionAllocatesZeroBytesAndDoesNotChurn()
    {
        using World world = new(nameof(StableObjectiveProjectionAllocatesZeroBytesAndDoesNotChurn));
        EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity(
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent));
        em.SetComponentData(root, Runtime());
        em.SetComponentData(root, Facts());
        Entity boundary = em.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
        SystemHandle handle = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
        UpdateObjective(world, handle);
        for (int i = 0; i < 32; i++) UpdateObjective(world, handle);

        int entityCount = em.UniversalQuery.CalculateEntityCount();
        uint version = em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary).Version;
        int objectiveCount = em.GetBuffer<MatchObjectiveRuntimeElement>(boundary).Length;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 128; i++) UpdateObjective(world, handle);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero, "Stable objective projection must allocate 0 B after warmup.");
        Assert.That(em.UniversalQuery.CalculateEntityCount(), Is.EqualTo(entityCount));
        Assert.That(em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary).Version, Is.EqualTo(version));
        Assert.That(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary).Length, Is.EqualTo(objectiveCount).And.EqualTo(2));
        Pass();
        yield break;
    }

    [Test]
    public void StableGuidanceCadenceAllocatesZeroBytesAndDoesNotRepublish()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        CampaignMissionAttemptFactsComponent facts = Facts();
        AssistantSettingsComponent settings = new()
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
            SubtitlesEnabled = 1
        };
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default, in runtime, in facts, in settings, Entity.Null, Entity.Null,
            new float3(1784f, 0f, 744f), new float3(1808f, 0f, 792f), out var current), Is.True);
        for (int i = 0; i < 32; i++)
            Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
                in current, in runtime, in facts, in settings, Entity.Null, Entity.Null,
                default, default, out _), Is.False);

        uint version = current.Version;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 256; i++)
            CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
                in current, in runtime, in facts, in settings, Entity.Null, Entity.Null,
                default, default, out _);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero, "Stable guidance cadence must allocate 0 B after warmup.");
        Assert.That(current.Version, Is.EqualTo(version));
        Pass();
    }

    [UnityTest]
    public IEnumerator AmbientCivilianCapFallbackAndSteadyStateAllocationRemainBounded()
    {
        yield return Run(new M01FirstContactAmbientPlayModeTests().AmbientCiviliansAreCappedAndGameplayInert());
        yield return Run(new M01FirstContactAmbientPlayModeTests().MissingPresentationCapacityFallsBackToZeroAndNeverBlocksMission());
        yield return Run(new M01FirstContactAmbientPlayModeTests().InvalidOverCapacityContractFailsClosed());
        yield return Run(new M01FirstContactAmbientPlayModeTests().StablePresentationUpdatesAllocateZeroManagedBytes());
        using (World world = new("M01 missing-contract allocation"))
        {
            world.EntityManager.CreateEntity(typeof(CampaignMissionRootComponent));
            SystemHandle handle = world.GetOrCreateSystem<CampaignMissionAmbientPresentationSystem>();
            for (int i = 0; i < 32; i++) UpdateAmbient(world, handle);
            int entityCount = world.EntityManager.UniversalQuery.CalculateEntityCount();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 128; i++) UpdateAmbient(world, handle);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero,
                "The missing-contract fallback must reuse cached queries and allocate 0 managed bytes.");
            Assert.That(world.EntityManager.UniversalQuery.CalculateEntityCount(), Is.EqualTo(entityCount));
        }
        Pass();
    }

    [UnityTest]
    public IEnumerator RetryUnloadReloadAndAttemptLocalStateHaveStableCounts()
    {
        yield return Run(new M01FirstContactLifecyclePlayModeTests().EightRetriesHaveStableEntityAndQueueCounts());
        yield return Run(new M01FirstContactLifecyclePlayModeTests().CatalogSystemTeardownRemovesRemainingMissionEntities());
        yield return Run(new M01FirstContactAmbientPlayModeTests().RepeatedRetryAndTeardownHaveStableCounts());
        Pass();
    }

    [Test]
    public void MissionHotPathsContainNoUnboundedOrPerFrameManagedWork()
    {
        string[] paths =
        {
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem.cs",
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionObjectiveProjectionSystem.cs",
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.cs",
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionAmbientPresentationSystem.cs",
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionLaunchSystem.cs",
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionSpawnSystem.cs"
        };
        string[] forbidden =
        {
            "GameObject.Find(", "FindObjectsOfType", "FindFirstObjectByType", "FindAnyObjectByType",
            "GetAllEntities", "Addressables.Load", ".ToList(", ".ToArray(", "System.Linq",
            "new EntityQuery"
        };
        foreach (string path in paths)
        {
            string source = File.ReadAllText(path);
            foreach (string token in forbidden)
                Assert.That(source, Does.Not.Contain(token), $"{path} contains forbidden hot-path token {token}.");
        }
        string ambient = File.ReadAllText(paths[3]);
        Assert.That(ambient, Does.Not.Contain("using EntityQuery query = em.CreateEntityQuery"),
            "Ambient missing/invalid-contract cleanup must reuse its OnCreate query.");
        Pass();
    }

    [Test]
    public void DenseCitySourceCapacityCameraAndProxyEvidenceRemainInherited()
    {
        string finalEvidence = File.ReadAllText("Design/AgentReports/2026-08-11_dense_city_final_evidence_index.json");
        string reuseEvidence = File.ReadAllText("Design/AgentReports/M01FirstContact/m01dc_016_dense_city_reuse_gate.json");
        string cameraEvidence = File.ReadAllText("Design/AgentReports/M01FirstContact/m01dc_012_camera_minimap.json");
        Assert.That(finalEvidence, Does.Contain("\"fixedProxySlots\": 7784"));
        Assert.That(finalEvidence, Does.Contain("\"maximumGcBytesPerFrame\": 0"));
        Assert.That(finalEvidence, Does.Contain("\"allRoutesPassed\": true"));
        Assert.That(reuseEvidence, Does.Contain("04681634c40403c8d30e1de2c44a64ac598403b7cd8f0574e1359a12f5758d73"));
        Assert.That(reuseEvidence, Does.Contain("a931d70e9e04915285c805cdd70545edd1b1852cb47a5fbb4a6ca0e1e7812fe7"));
        Assert.That(reuseEvidence, Does.Contain("2fb85fd03d4b82621ba9c9168a21560e2ba8df3350e45594c5b3785a58768b8d"));
        Assert.That(cameraEvidence, Does.Contain("\"clamp\": true"));
        Assert.That(cameraEvidence, Does.Contain("[M01FirstContactCameraMinimapValidation] result=Passed tests=12"));
        Pass();
    }

    private static CampaignMissionRuntimeComponent Runtime() => new()
    {
        MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        SessionToken = new FixedString64Bytes("m01-performance"),
        Phase = MissionPhaseKind.Engage,
        Guidance = NarrativeGuidanceMode.Full,
        RunKind = MissionRunKind.FirstClear,
        ReplayTutorialEnabled = 1,
        Version = 3,
        SourceVersion = 8,
        AttemptOrdinal = 1,
        DeterministicSeed = 104729
    };

    private static CampaignMissionAttemptFactsComponent Facts() => new()
    {
        ElapsedMilliseconds = 125000,
        HostileTotalCount = 3,
        HostileDefeatedCount = 1,
        CommandSquadSpawned = 1,
        CommandSquadAlive = 1
    };

    private static void UpdateObjective(World world, SystemHandle handle)
    {
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(handle).OnUpdate(ref state);
    }

    private static void UpdateAmbient(World world, SystemHandle handle)
    {
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionAmbientPresentationSystem>(handle).OnUpdate(ref state);
    }

    private static IEnumerator Run(IEnumerator test)
    {
        while (test.MoveNext()) yield return test.Current;
    }

    private static void Pass() => s_Passed++;
}
#endif
