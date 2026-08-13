#if UNITY_INCLUDE_TESTS
using System.Collections;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TestTools;

public sealed class M01FirstContactLifecyclePlayModeTests
{
    private static readonly FixedString64Bytes Mission = new("saga.ch01.m01.first_contact");
    private static readonly FixedString64Bytes Scenario = new("scenario.ch01.m01.first_contact");
    private static readonly FixedString64Bytes Map = new("opmap.ch01.district_edge_01");
    private static readonly FixedString64Bytes Session = new("m01-lifecycle");

    [UnityTest]
    public IEnumerator CommandSquadLossPublishesDeterministicNoPenaltyDefeat()
    {
        CampaignMissionRuntimeComponent current = Runtime(MissionPhaseKind.Engage, 3);
        CampaignMissionAttemptFactsComponent facts = new()
        {
            CommandSquadSpawned = 1, CommandSquadAlive = 0, SquadLossCount = 4,
            HostileTotalCount = 3, HostileDefeatedCount = 1
        };
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(in current, in facts, out var next), Is.True);
        Assert.That(next.Phase, Is.EqualTo(MissionPhaseKind.Result));
        Assert.That(next.Outcome, Is.EqualTo(MissionOutcomeKind.Defeat));
        Assert.That(next.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CampaignOperations));
        Assert.That(next.Version, Is.EqualTo(4));
        Assert.That(facts.SquadLossCount, Is.EqualTo(4), "failure evaluation must not apply a retry penalty");
        yield break;
    }

    [UnityTest]
    public IEnumerator AcceptedRetryRemovesPriorAttemptAndClearsAttemptLocalQueues()
    {
        using World world = new(nameof(AcceptedRetryRemovesPriorAttemptAndClearsAttemptLocalQueues));
        Fixture fixture = CreateFixture(world);
        try
        {
            AddAttemptLocalState(world.EntityManager, fixture.Root, 7);
            Update<CampaignMissionLaunchSystem>(world);
            AssertAttemptReset(world.EntityManager, fixture.Root, 1);
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator EightRetriesHaveStableEntityAndQueueCounts()
    {
        using World world = new(nameof(EightRetriesHaveStableEntityAndQueueCounts));
        Fixture fixture = CreateFixture(world);
        try
        {
            for (int attempt = 1; attempt <= 8; attempt++)
            {
                if (attempt > 1) Enqueue(world.EntityManager, fixture.Root, attempt);
                AddAttemptLocalState(world.EntityManager, fixture.Root, attempt);
                Update<CampaignMissionLaunchSystem>(world);
                AssertAttemptReset(world.EntityManager, fixture.Root, attempt);
            }
            DynamicBuffer<CampaignMissionLaunchResultElement> results =
                world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(fixture.Root);
            Assert.That(results.Length, Is.EqualTo(1), "only the current launch result may remain buffered");
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator CatalogSystemTeardownRemovesRemainingMissionEntities()
    {
        using World world = new(nameof(CatalogSystemTeardownRemovesRemainingMissionEntities));
        Fixture fixture = CreateFixture(world);
        EntityManager em = world.EntityManager;
        AddAttemptLocalState(em, fixture.Root, 1);
        SystemHandle handle = world.GetOrCreateSystem<CampaignMissionCatalogDisposalSystem>();
        world.DestroySystem(handle);
        using EntityQuery units = em.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>());
        Assert.That(units.CalculateEntityCount(), Is.Zero);
        fixture.Catalog = default;
        yield break;
    }

    private static Fixture CreateFixture(World world)
    {
        EntityManager em = world.EntityManager;
        BlobAssetReference<CampaignMissionCatalogBlob> catalog = CreateCatalog();
        Entity root = em.CreateEntity(
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionLaunchQueueComponent), typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent), typeof(CampaignMissionResultComponent));
        em.SetComponentData(root, new CampaignMissionCatalogComponent
            { Blob = catalog, SourceVersion = 11, OwnsBlob = 1 });
        em.SetComponentData(root, Runtime(MissionPhaseKind.Result, 9));
        em.SetComponentData(root, new CampaignMissionAttemptFactsComponent { SquadLossCount = 3 });
        em.AddBuffer<CampaignMissionLaunchRequestElement>(root);
        em.AddBuffer<CampaignMissionLaunchResultElement>(root);
        em.AddBuffer<CampaignMissionActionRequestElement>(root);
        em.AddBuffer<CampaignMissionActionResultElement>(root);
        em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
        em.AddBuffer<CampaignMissionSettlementResultElement>(root);
        Entity active = em.CreateEntity(typeof(ActiveOperationMapComponent));
        em.SetComponentData(active, new ActiveOperationMapComponent
            { MissionId = Mission, ScenarioId = Scenario, OperationMapId = Map });
        Entity readiness = em.CreateEntity(typeof(OperationMapReadinessComponent));
        em.SetComponentData(readiness, new OperationMapReadinessComponent
            { RequiredFlags = OperationMapReadinessFlags.Metadata, ReadyFlags = OperationMapReadinessFlags.Metadata });
        Enqueue(em, root, 1);
        return new Fixture { Catalog = catalog, Root = root };
    }

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateCatalog()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob root = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        root.SchemaVersion = MissionLaunchPayloadFactory.CurrentSchemaVersion;
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref root.Missions, 1);
        missions[0].MissionId = Mission;
        missions[0].ScenarioId = Scenario;
        missions[0].OperationMapId = Map;
        BlobAssetReference<CampaignMissionCatalogBlob> result =
            builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }

    private static void Enqueue(EntityManager em, Entity root, int attempt)
    {
        em.GetBuffer<CampaignMissionLaunchRequestElement>(root).Add(new CampaignMissionLaunchRequestElement
        {
            SchemaVersion = MissionLaunchPayloadFactory.CurrentSchemaVersion,
            MissionId = Mission, ScenarioId = Scenario, OperationMapId = Map,
            LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
            RunKind = attempt == 1 ? MissionRunKind.Retry : MissionRunKind.Replay,
            Guidance = Game.Narrative.Contracts.NarrativeGuidanceMode.Contextual,
            TransitionToken = (ulong)(100 + attempt), SessionToken = Session,
            AttemptOrdinal = attempt, DeterministicSeed = 1701
        });
    }

    private static void AddAttemptLocalState(EntityManager em, Entity root, int ordinal)
    {
        for (int i = 0; i < 7; i++)
        {
            Entity unit = em.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
            em.SetComponentData(unit, new CampaignMissionUnitRoleComponent { SessionToken = Session });
        }
        em.GetBuffer<CampaignMissionActionRequestElement>(root).Add(default);
        em.GetBuffer<CampaignMissionActionResultElement>(root).Add(default);
        em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Add(default);
        em.GetBuffer<CampaignMissionSettlementResultElement>(root).Add(default);
        em.SetComponentData(root, new CampaignMissionResultComponent { SourceVersion = (uint)ordinal, Stars = 3 });
    }

    private static void AssertAttemptReset(EntityManager em, Entity root, int attempt)
    {
        using EntityQuery units = em.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>());
        Assert.That(units.CalculateEntityCount(), Is.Zero);
        Assert.That(em.GetBuffer<CampaignMissionActionRequestElement>(root).Length, Is.Zero);
        Assert.That(em.GetBuffer<CampaignMissionActionResultElement>(root).Length, Is.Zero);
        Assert.That(em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length, Is.Zero);
        Assert.That(em.GetBuffer<CampaignMissionSettlementResultElement>(root).Length, Is.Zero);
        Assert.That(em.GetComponentData<CampaignMissionResultComponent>(root).SourceVersion, Is.Zero);
        Assert.That(em.GetComponentData<CampaignMissionAttemptFactsComponent>(root), Is.EqualTo(default(CampaignMissionAttemptFactsComponent)));
        CampaignMissionRuntimeComponent runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
        Assert.That(runtime.AttemptOrdinal, Is.EqualTo(attempt));
        Assert.That(runtime.SessionToken, Is.EqualTo(Session));
        Assert.That(runtime.DeterministicSeed, Is.EqualTo(1701));
    }

    private static CampaignMissionRuntimeComponent Runtime(MissionPhaseKind phase, uint version) => new()
    {
        MissionId = Mission, ScenarioId = Scenario, OperationMapId = Map, SessionToken = Session,
        Phase = phase, Outcome = phase == MissionPhaseKind.Result ? MissionOutcomeKind.Defeat : MissionOutcomeKind.None,
        ReturnDestination = phase == MissionPhaseKind.Result ? MissionReturnDestinationKind.CampaignOperations :
            MissionReturnDestinationKind.None,
        LaunchOrigin = MissionLaunchOriginKind.CampaignOperations, RunKind = MissionRunKind.Retry,
        SourceVersion = 11, Version = version, AttemptOrdinal = 0, DeterministicSeed = 1701
    };

    private static void Update<T>(World world) where T : unmanaged, ISystem
    {
        SystemHandle handle = world.GetOrCreateSystem<T>();
        world.Unmanaged.GetUnsafeSystemRef<T>(handle).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private struct Fixture
    {
        public BlobAssetReference<CampaignMissionCatalogBlob> Catalog;
        public Entity Root;
        public void Dispose() { if (Catalog.IsCreated) Catalog.Dispose(); }
    }
}
#endif
