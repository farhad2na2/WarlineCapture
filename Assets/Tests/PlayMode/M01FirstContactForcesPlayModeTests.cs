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

public sealed class M01FirstContactForcesPlayModeTests
{
    private static readonly FixedString64Bytes MissionId = new("saga.ch01.m01.first_contact");
    private static readonly FixedString64Bytes ScenarioId = new("scenario.ch01.m01.first_contact");
    private static readonly FixedString64Bytes MapId = new("opmap.ch01.district_edge_01");
    private static readonly FixedString64Bytes Session = new("m01-force-test");

    public static void RunFocusedValidation()
    {
        M01FirstContactForcesPlayModeTests tests = new();
        RunToCompletion(tests.SpawnCreatesExactDeterministicFourVersusThreeForce());
        RunToCompletion(tests.PatrolQueuesExactThreeOrdersOnceAfterDelay());
        RunToCompletion(tests.MissingRuntimePrefabFailsClosedWithoutPartialSpawn());
        UnityEngine.Debug.Log("[M01FirstContactForcesPlayModeValidation] result=Passed tests=3");
    }

    [UnityTest]
    public IEnumerator SpawnCreatesExactDeterministicFourVersusThreeForce()
    {
        using World world = new(nameof(SpawnCreatesExactDeterministicFourVersusThreeForce));
        Fixture fixture = CreateFixture(world);
        try
        {
            Update<CampaignMissionSpawnSystem>(world);
            using EntityQuery units = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(), ComponentType.ReadOnly<Faction>());
            using NativeArray<Entity> entities = units.ToEntityArray(Allocator.Temp);
            Assert.That(entities.Length, Is.EqualTo(7));
            int friendly = 0;
            int hostile = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.That(world.EntityManager.HasComponent<SelectedUnitTag>(entities[i]), Is.False,
                    "Mission spawn must scrub selection state inherited from runtime prefabs.");
                Faction faction = world.EntityManager.GetComponentData<Faction>(entities[i]);
                if (faction.Id == 1) friendly++;
                if (faction.Id == 2) hostile++;
                LocalTransform transform = world.EntityManager.GetComponentData<LocalTransform>(entities[i]);
                float3 center = faction.Id == 1 ? new float3(8f, 0f, 8f) : new float3(20f, 0f, 20f);
                Assert.That(math.distance(transform.Position, center), Is.LessThanOrEqualTo(4f));
            }
            Assert.That(friendly, Is.EqualTo(4));
            Assert.That(hostile, Is.EqualTo(3));
            CampaignMissionAttemptFactsComponent facts = world.EntityManager.GetComponentData<
                CampaignMissionAttemptFactsComponent>(fixture.Root);
            Assert.That(facts.CommandSquadSpawned, Is.EqualTo(1));
            Assert.That(facts.HostileTotalCount, Is.EqualTo(3));
            RuntimeCameraFocusRequestComponent focus = world.EntityManager.GetComponentData<
                RuntimeCameraFocusRequestComponent>(fixture.CameraFocus);
            Assert.That(focus.Requested, Is.EqualTo(1));
            float3 friendlyCenter = float3.zero;
            float3 hostileCenter = float3.zero;
            int friendlyCount = 0;
            int hostileCount = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                float3 position = world.EntityManager.GetComponentData<LocalTransform>(entities[i]).Position;
                if (world.EntityManager.GetComponentData<Faction>(entities[i]).Id == FactionIdentity.PlayerFactionId)
                {
                    friendlyCenter += position;
                    friendlyCount++;
                }
                else
                {
                    hostileCenter += position;
                    hostileCount++;
                }
            }
            Assert.That(friendlyCount, Is.EqualTo(4));
            Assert.That(hostileCount, Is.EqualTo(3));
            Assert.That(focus.Smooth, Is.Zero);
            Assert.That(math.distance(focus.World, hostileCenter / hostileCount), Is.LessThan(0.001f));
            CampaignMissionOpeningPresentationComponent opening = world.EntityManager.GetComponentData<
                CampaignMissionOpeningPresentationComponent>(fixture.Root);
            Assert.That(opening.Stage, Is.EqualTo(1));
            Assert.That(opening.SessionToken, Is.EqualTo(Session));
            Assert.That(math.distance(opening.FriendlyFocus, friendlyCenter / friendlyCount), Is.LessThan(0.001f));
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator PatrolQueuesExactThreeOrdersOnceAfterDelay()
    {
        using World world = new(nameof(PatrolQueuesExactThreeOrdersOnceAfterDelay));
        Fixture fixture = CreateFixture(world);
        try
        {
            Update<CampaignMissionSpawnSystem>(world);
            CampaignMissionAttemptFactsComponent facts = world.EntityManager.GetComponentData<
                CampaignMissionAttemptFactsComponent>(fixture.Root);
            world.EntityManager.SetComponentData(fixture.CameraFocus, default(RuntimeCameraFocusRequestComponent));
            facts.ElapsedMilliseconds = 2000;
            world.EntityManager.SetComponentData(fixture.Root, facts);
            using EntityQuery combatUnits = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(), ComponentType.ReadOnly<UnitCombat>());
            using NativeArray<Entity> combatEntities = combatUnits.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < combatEntities.Length; i++)
            {
                world.EntityManager.AddComponentData(combatEntities[i], new EngageTarget
                {
                    Target = combatEntities[(i + 1) % combatEntities.Length],
                    IsCommanded = 0
                });
            }
            Update<CampaignMissionPatrolOrderSystem>(world);
            RuntimeCameraFocusRequestComponent focus = world.EntityManager.GetComponentData<
                RuntimeCameraFocusRequestComponent>(fixture.CameraFocus);
            Assert.That(focus.Requested, Is.EqualTo(1));
            Assert.That(focus.Smooth, Is.EqualTo(1));
            CampaignMissionOpeningPresentationComponent opening = world.EntityManager.GetComponentData<
                CampaignMissionOpeningPresentationComponent>(fixture.Root);
            Assert.That(opening.Stage, Is.EqualTo(2));
            Assert.That(math.distance(focus.World, opening.FriendlyFocus), Is.LessThan(0.001f));
            Entity queue = UnitMoveOrderRequestSystem.EnsureQueueEntity(world.EntityManager);
            DynamicBuffer<UnitMoveOrderRequestElement> requests = world.EntityManager.GetBuffer<
                UnitMoveOrderRequestElement>(queue);
            Assert.That(requests.Length, Is.Zero);
            for (int i = 0; i < combatEntities.Length; i++)
            {
                Assert.That(world.EntityManager.GetComponentData<UnitCombat>(combatEntities[i]).CanAttack, Is.Zero,
                    "M01 units must be unable to inflict damage before the explicit Engage phase.");
                Assert.That(world.EntityManager.GetComponentData<UnitCombat>(combatEntities[i]).AutoEngage, Is.Zero,
                    "M01 units must not auto-engage before the explicit Engage phase.");
                Assert.That(world.EntityManager.HasComponent<EngageTarget>(combatEntities[i]), Is.False,
                    "M01 must scrub inherited or AI-issued combat targets before Engage.");
            }
            facts.ElapsedMilliseconds = 3000;
            world.EntityManager.SetComponentData(fixture.Root, facts);
            CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<
                CampaignMissionRuntimeComponent>(fixture.Root);
            runtime.Phase = MissionPhaseKind.Engage;
            world.EntityManager.SetComponentData(fixture.Root, runtime);
            Update<CampaignMissionPatrolOrderSystem>(world);
            requests = world.EntityManager.GetBuffer<UnitMoveOrderRequestElement>(queue);
            Assert.That(requests.Length, Is.EqualTo(3));
            for (int i = 0; i < requests.Length; i++)
            {
                Assert.That(requests[i].Kind, Is.EqualTo(UnitMoveOrderRequestKind.TargetPathOnly));
                Assert.That(requests[i].Goal, Is.EqualTo(new int2(12, 12)));
            }
            opening = world.EntityManager.GetComponentData<CampaignMissionOpeningPresentationComponent>(fixture.Root);
            Assert.That(opening.Stage, Is.EqualTo(3));
            for (int i = 0; i < combatEntities.Length; i++)
            {
                Assert.That(world.EntityManager.GetComponentData<UnitCombat>(combatEntities[i]).CanAttack, Is.EqualTo(1));
                Assert.That(world.EntityManager.GetComponentData<UnitCombat>(combatEntities[i]).AutoEngage, Is.EqualTo(1));
            }
            UnitCombat stopped = world.EntityManager.GetComponentData<UnitCombat>(combatEntities[0]);
            stopped.AutoEngage = 0;
            world.EntityManager.SetComponentData(combatEntities[0], stopped);
            Update<CampaignMissionPatrolOrderSystem>(world);
            requests = world.EntityManager.GetBuffer<UnitMoveOrderRequestElement>(queue);
            Assert.That(requests.Length, Is.EqualTo(3));
            Assert.That(world.EntityManager.GetComponentData<UnitCombat>(combatEntities[0]).AutoEngage, Is.Zero,
                "The one-shot Engage release must not override a later player STOP command.");
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    [UnityTest]
    public IEnumerator MissingRuntimePrefabFailsClosedWithoutPartialSpawn()
    {
        using World world = new(nameof(MissingRuntimePrefabFailsClosedWithoutPartialSpawn));
        Fixture fixture = CreateFixture(world, omitLastPrefab: true);
        try
        {
            Update<CampaignMissionSpawnSystem>(world);
            using EntityQuery units = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>());
            Assert.That(units.CalculateEntityCount(), Is.Zero);
            CampaignMissionAttemptFactsComponent facts = world.EntityManager.GetComponentData<
                CampaignMissionAttemptFactsComponent>(fixture.Root);
            Assert.That(facts.CommandSquadSpawned, Is.Zero);
            yield break;
        }
        finally { fixture.Dispose(); }
    }

    private static Fixture CreateFixture(World world, bool omitLastPrefab = false)
    {
        EntityManager em = world.EntityManager;
        BlobAssetReference<CampaignMissionCatalogBlob> catalog = CreateCatalog();
        BlobAssetReference<OperationMapBlob> map = CreateMap();
        Entity root = em.CreateEntity(
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
        em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = catalog, SourceVersion = 1 });
        em.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = MissionId, ScenarioId = ScenarioId, OperationMapId = MapId,
            SessionToken = Session, DeterministicSeed = 1701
        });
        Entity mapEntity = em.CreateEntity(typeof(OperationMapMetadataComponent));
        em.SetComponentData(mapEntity, new OperationMapMetadataComponent { Blob = map, Generation = 1 });
        Entity cameraFocus = em.CreateEntity(typeof(RuntimeCameraFocusRequestComponent));
        Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
        DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
        FixedString64Bytes[] keys = RuntimeKeys();
        int count = omitLastPrefab ? keys.Length - 1 : keys.Length;
        for (int i = 0; i < count; i++)
        {
            Entity prefab = em.CreateEntity(
                typeof(Prefab), typeof(UnitSourcePrefabKey), typeof(LocalTransform), typeof(SelectedUnitTag),
                typeof(UnitCombat));
            em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = keys[i] });
            em.SetComponentData(prefab, LocalTransform.Identity);
            em.SetComponentData(prefab, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefab });
        }
        return new Fixture { Catalog = catalog, Map = map, Root = root, CameraFocus = cameraFocus };
    }

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateCatalog()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob root = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref root.Missions, 1);
        ref CampaignMissionDefinitionBlob mission = ref missions[0];
        mission.MissionId = MissionId;
        mission.ScenarioId = ScenarioId;
        mission.OperationMapId = MapId;
        mission.BuildingDisabled = mission.ProductionDisabled = mission.EconomyDisabled = 1;
        mission.TransportDisabled = mission.AirDisabled = 1;
        BlobBuilderArray<CampaignMissionForceGroupBlob> groups = builder.Allocate(ref mission.ForceGroups, 2);
        AddGroup(ref builder, ref groups[0], "group.ch01.m01.command_squad", 1, 0, 4);
        AddGroup(ref builder, ref groups[1], "group.ch01.m01.hostile_patrol", 2, 4, 3);
        BlobBuilderArray<CampaignMissionPatrolRouteBlob> routes = builder.Allocate(ref mission.PatrolRoutes, 1);
        routes[0].RouteId = new FixedString64Bytes("route.ch01.m01.hostile_patrol");
        routes[0].UnitGroupId = groups[1].GroupId;
        routes[0].StartDelayMilliseconds = 3000;
        BlobBuilderArray<FixedString64Bytes> anchors = builder.Allocate(ref routes[0].AnchorIds, 3);
        anchors[0] = new FixedString64Bytes("anchor.ch01.m01.patrol_route_a");
        anchors[1] = new FixedString64Bytes("anchor.ch01.m01.patrol_route_b");
        anchors[2] = new FixedString64Bytes("anchor.ch01.m01.patrol_route_c");
        BlobAssetReference<CampaignMissionCatalogBlob> result =
            builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }

    private static void AddGroup(
        ref BlobBuilder builder, ref CampaignMissionForceGroupBlob group,
        string groupId, byte factionId, int keyOffset, int count)
    {
        group.GroupId = new FixedString64Bytes(groupId);
        group.FactionId = factionId;
        FixedString64Bytes[] keys = RuntimeKeys();
        BlobBuilderArray<CampaignMissionForceUnitBlob> units = builder.Allocate(ref group.Units, count);
        for (int i = 0; i < count; i++)
        {
            FixedString64Bytes key = keys[keyOffset + i];
            units[i] = new CampaignMissionForceUnitBlob
            {
                SourceKey = keyOffset == 0 ? new FixedString64Bytes($"unit.jrc.rifle_{i}") :
                    new FixedString64Bytes(i == 0 ? "unit.ash.courier" : i == 1 ? "unit.ash.warden" : "unit.ash.broker"),
                RuntimePrefabSourceKey = key,
                SpawnAnchorId = new FixedString64Bytes(keyOffset == 0 ?
                    "anchor.ch01.m01.player_spawn" : "anchor.ch01.m01.patrol_spawn"),
                MissionRoleId = new FixedString64Bytes(keyOffset == 0 ?
                    "role.friendly.command_squad" : "role.hostile.patrol"),
                Count = 1
            };
        }
    }

    private static BlobAssetReference<OperationMapBlob> CreateMap()
    {
        BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = MapId;
        root.Grid = new OperationMapGridBlob { Origin = float3.zero, Dimensions = new int2(64, 64), CellSize = 2f };
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref root.Anchors, 5);
        anchors[0] = Anchor("anchor.ch01.m01.player_spawn", new float3(8f, 0f, 8f), 4f);
        anchors[1] = Anchor("anchor.ch01.m01.patrol_spawn", new float3(20f, 0f, 20f), 4f);
        anchors[2] = Anchor("anchor.ch01.m01.patrol_route_a", new float3(24f, 0f, 24f), 2f);
        anchors[3] = Anchor("anchor.ch01.m01.patrol_route_b", new float3(28f, 0f, 24f), 2f);
        anchors[4] = Anchor("anchor.ch01.m01.patrol_route_c", new float3(32f, 0f, 24f), 2f);
        BlobAssetReference<OperationMapBlob> result =
            builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }

    private static OperationMapAnchorBlob Anchor(string id, float3 position, float radius) => new()
    {
        Id = new FixedString64Bytes(id), Position = position, Rotation = quaternion.identity, Radius = radius
    };

    private static FixedString64Bytes[] RuntimeKeys() => new[]
    {
        new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_02"),
        new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04"),
        new FixedString64Bytes("Unit_Chr_Soldier_Female_01_Alt_01"),
        new FixedString64Bytes("Unit_Chr_Soldier_Female_02_Alt_01"),
        new FixedString64Bytes("Unit_Chr_Insurgent_Male_03"),
        new FixedString64Bytes("Unit_Chr_Insurgent_Female_01"),
        new FixedString64Bytes("Unit_Chr_Insurgent_Female_02")
    };

    private static void Update<T>(World world) where T : unmanaged, ISystem
    {
        SystemHandle handle = world.GetOrCreateSystem<T>();
        world.Unmanaged.GetUnsafeSystemRef<T>(handle).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private static void RunToCompletion(IEnumerator test)
    {
        while (test.MoveNext())
            Assert.That(test.Current, Is.Null, "Focused validation accepts only synchronous PlayMode test steps.");
    }

    private struct Fixture
    {
        public BlobAssetReference<CampaignMissionCatalogBlob> Catalog;
        public BlobAssetReference<OperationMapBlob> Map;
        public Entity Root;
        public Entity CameraFocus;

        public void Dispose()
        {
            if (Catalog.IsCreated) Catalog.Dispose();
            if (Map.IsCreated) Map.Dispose();
        }
    }
}
#endif
