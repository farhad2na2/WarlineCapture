#if UNITY_INCLUDE_TESTS
using System.Collections;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.TestTools;

public sealed class M01FirstContactAmbientPlayModeTests
{
    private static readonly FixedString64Bytes Mission = new("saga.ch01.m01.first_contact");
    private static readonly FixedString64Bytes Scenario = new("scenario.ch01.m01.first_contact");
    private static readonly FixedString64Bytes Map = new("opmap.ch01.district_edge_01");
    private static readonly FixedString64Bytes Session = new("m01-ambient");

    [UnityTest]
    public IEnumerator AmbientCiviliansAreCappedAndGameplayInert()
    {
        using World world = new(nameof(AmbientCiviliansAreCappedAndGameplayInert));
        Fixture fixture = CreateFixture(world, 8, withPrefabs: true);
        try
        {
            Update(world);
            using EntityQuery query = AmbientQuery(world.EntityManager);
            Assert.That(query.CalculateEntityCount(), Is.EqualTo(8));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                Assert.That(world.EntityManager.HasComponent<CivilianUnitTag>(entity), Is.True);
                Assert.That(world.EntityManager.HasComponent<Faction>(entity), Is.False);
                Assert.That(world.EntityManager.HasComponent<UnitHealth>(entity), Is.False);
                Assert.That(world.EntityManager.HasComponent<UnitCombat>(entity), Is.False);
                Assert.That(world.EntityManager.HasComponent<UnitAttack>(entity), Is.False);
                Assert.That(world.EntityManager.HasComponent<SelectedUnitTag>(entity), Is.False);
                Assert.That(world.EntityManager.HasComponent<EngageTarget>(entity), Is.False);
            }
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator VictoryMovesOneWayToEvacuationWithoutWritingMissionTruth()
    {
        using World world = new(nameof(VictoryMovesOneWayToEvacuationWithoutWritingMissionTruth));
        Fixture fixture = CreateFixture(world, 8, withPrefabs: true);
        try
        {
            Update(world);
            CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<
                CampaignMissionRuntimeComponent>(fixture.Root);
            runtime.Phase = MissionPhaseKind.Result;
            runtime.Outcome = MissionOutcomeKind.Victory;
            runtime.ReturnDestination = MissionReturnDestinationKind.CommandBase;
            runtime.Version = 7;
            world.EntityManager.SetComponentData(fixture.Root, runtime);
            Update(world);
            using EntityQuery query = AmbientQuery(world.EntityManager);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            Assert.That(entities.Length, Is.EqualTo(8));
            for (int i = 0; i < entities.Length; i++)
            {
                var civilian = world.EntityManager.GetComponentData<CampaignMissionAmbientCivilianComponent>(entities[i]);
                Assert.That(civilian.Evacuating, Is.EqualTo(1));
                Assert.That(civilian.RouteIndex, Is.EqualTo(1));
                Assert.That(math.distance(
                    world.EntityManager.GetComponentData<LocalTransform>(entities[i]).Position,
                    new float3(30f, 0f, 30f)), Is.LessThanOrEqualTo(5f));
            }
            Assert.That(world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(fixture.Root),
                Is.EqualTo(runtime));
            Assert.That(world.EntityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(fixture.Root),
                Is.EqualTo(default(CampaignMissionAttemptFactsComponent)));
            runtime.Phase = MissionPhaseKind.Result;
            runtime.Outcome = MissionOutcomeKind.None;
            runtime.Version++;
            world.EntityManager.SetComponentData(fixture.Root, runtime);
            Update(world);
            using NativeArray<CampaignMissionAmbientCivilianComponent> retained =
                query.ToComponentDataArray<CampaignMissionAmbientCivilianComponent>(Allocator.Temp);
            for (int i = 0; i < retained.Length; i++)
                Assert.That(retained[i].Evacuating, Is.EqualTo(1));
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator MissingPresentationCapacityFallsBackToZeroAndNeverBlocksMission()
    {
        using World world = new(nameof(MissingPresentationCapacityFallsBackToZeroAndNeverBlocksMission));
        Fixture fixture = CreateFixture(world, 8, withPrefabs: false);
        try
        {
            Update(world);
            using EntityQuery query = AmbientQuery(world.EntityManager);
            Assert.That(query.CalculateEntityCount(), Is.Zero);
            CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<
                CampaignMissionRuntimeComponent>(fixture.Root);
            Assert.That(runtime.Phase, Is.EqualTo(MissionPhaseKind.Engage));
            Assert.That(runtime.Outcome, Is.EqualTo(MissionOutcomeKind.None));
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator InvalidOverCapacityContractFailsClosed()
    {
        using World world = new(nameof(InvalidOverCapacityContractFailsClosed));
        Fixture fixture = CreateFixture(world, 13, withPrefabs: true);
        try
        {
            Update(world);
            using EntityQuery query = AmbientQuery(world.EntityManager);
            Assert.That(query.CalculateEntityCount(), Is.Zero);
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator DefeatNeverTriggersCivilianEvacuation()
    {
        using World world = new(nameof(DefeatNeverTriggersCivilianEvacuation));
        Fixture fixture = CreateFixture(world, 8, withPrefabs: true);
        try
        {
            Update(world);
            CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<
                CampaignMissionRuntimeComponent>(fixture.Root);
            runtime.Phase = MissionPhaseKind.Result;
            runtime.Outcome = MissionOutcomeKind.Defeat;
            runtime.Version++;
            world.EntityManager.SetComponentData(fixture.Root, runtime);
            Update(world);
            using EntityQuery query = AmbientQuery(world.EntityManager);
            using NativeArray<CampaignMissionAmbientCivilianComponent> civilians =
                query.ToComponentDataArray<CampaignMissionAmbientCivilianComponent>(Allocator.Temp);
            Assert.That(civilians.Length, Is.EqualTo(8));
            for (int i = 0; i < civilians.Length; i++)
                Assert.That(civilians[i].Evacuating, Is.Zero);
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator RepeatedRetryAndTeardownHaveStableCounts()
    {
        using World world = new(nameof(RepeatedRetryAndTeardownHaveStableCounts));
        Fixture fixture = CreateFixture(world, 8, withPrefabs: true);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<
                CampaignMissionRuntimeComponent>(fixture.Root);
            runtime.AttemptOrdinal = attempt;
            runtime.Version++;
            world.EntityManager.SetComponentData(fixture.Root, runtime);
            Update(world);
            using EntityQuery query = AmbientQuery(world.EntityManager);
            Assert.That(query.CalculateEntityCount(), Is.EqualTo(8));
        }
        SystemHandle handle = world.GetExistingSystem<CampaignMissionAmbientPresentationSystem>();
        world.DestroySystem(handle);
        using (EntityQuery query = AmbientQuery(world.EntityManager))
            Assert.That(query.CalculateEntityCount(), Is.Zero);
        fixture.Dispose();
        yield break;
    }

    [UnityTest]
    public IEnumerator StablePresentationUpdatesAllocateZeroManagedBytes()
    {
        using World world = new(nameof(StablePresentationUpdatesAllocateZeroManagedBytes));
        Fixture fixture = CreateFixture(world, 8, withPrefabs: true);
        try
        {
            Update(world);
            SystemHandle handle = world.GetExistingSystem<CampaignMissionAmbientPresentationSystem>();
            UpdateAmbientSystem(world, handle);
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 32; i++)
                UpdateAmbientSystem(world, handle);
            Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    private static Fixture CreateFixture(World world, int count, bool withPrefabs)
    {
        EntityManager em = world.EntityManager;
        BlobAssetReference<CampaignMissionCatalogBlob> catalog = CreateCatalog(count);
        BlobAssetReference<OperationMapBlob> map = CreateMap();
        Entity root = em.CreateEntity(
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
        em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = catalog, SourceVersion = 1 });
        em.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = Mission, ScenarioId = Scenario, OperationMapId = Map, SessionToken = Session,
            Phase = MissionPhaseKind.Engage, RunKind = MissionRunKind.FirstClear,
            SourceVersion = 1, Version = 1, DeterministicSeed = 1701
        });
        Entity mapEntity = em.CreateEntity(typeof(OperationMapMetadataComponent));
        em.SetComponentData(mapEntity, new OperationMapMetadataComponent { Blob = map, Generation = 1 });
        if (withPrefabs) CreateRegistry(em);
        return new Fixture { Catalog = catalog, MapBlob = map, Root = root };
    }

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateCatalog(int count)
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob root = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref root.Missions, 1);
        missions[0].MissionId = Mission;
        missions[0].ScenarioId = Scenario;
        missions[0].OperationMapId = Map;
        BlobBuilderArray<CampaignMissionAmbientPresentationBlob> ambient =
            builder.Allocate(ref missions[0].AmbientPresentations, 1);
        ambient[0] = new CampaignMissionAmbientPresentationBlob
        {
            PresentationId = new FixedString64Bytes("ambient.ch01.m01.civilians"),
            AnchorId = new FixedString64Bytes("anchor.ch01.m01.civilian_safe_zone"),
            RouteId = new FixedString64Bytes("route.ch01.m01.civilian_evacuation"), InstanceCount = count
        };
        BlobAssetReference<CampaignMissionCatalogBlob> result =
            builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }

    private static BlobAssetReference<OperationMapBlob> CreateMap()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = Map;
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref root.Anchors, 2);
        anchors[0] = new OperationMapAnchorBlob
            { Id = new FixedString64Bytes("anchor.ch01.m01.civilian_safe_zone"), Position = new float3(10f, 0f, 10f), Radius = 5f, Rotation = quaternion.identity };
        anchors[1] = new OperationMapAnchorBlob
            { Id = new FixedString64Bytes("anchor.ch01.m01.civilian_evacuation"), Position = new float3(30f, 0f, 30f), Radius = 5f, Rotation = quaternion.identity };
        BlobAssetReference<OperationMapBlob> result =
            builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }

    private static void CreateRegistry(EntityManager em)
    {
        Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
        Entity prefab = em.CreateEntity(
            typeof(Prefab), typeof(UnitSourcePrefabKey), typeof(LocalTransform), typeof(Faction),
            typeof(UnitHealth), typeof(UnitCombat), typeof(UnitAttack), typeof(SelectedUnitTag));
        em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Civilian_Male_01") });
        em.SetComponentData(prefab, LocalTransform.Identity);
        registry.Add(new UnitPrefabRegistryEntry { Prefab = prefab });
    }

    private static EntityQuery AmbientQuery(EntityManager em) =>
        em.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionAmbientCivilianComponent>());

    private static void Update(World world)
    {
        SystemHandle handle = world.GetOrCreateSystem<CampaignMissionAmbientPresentationSystem>();
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionAmbientPresentationSystem>(handle)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private static void UpdateAmbientSystem(World world, SystemHandle handle)
    {
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionAmbientPresentationSystem>(handle).OnUpdate(ref state);
    }

    private struct Fixture
    {
        public BlobAssetReference<CampaignMissionCatalogBlob> Catalog;
        public BlobAssetReference<OperationMapBlob> MapBlob;
        public Entity Root;
        public void Dispose()
        {
            if (Catalog.IsCreated) Catalog.Dispose();
            if (MapBlob.IsCreated) MapBlob.Dispose();
        }
    }
}
#endif
