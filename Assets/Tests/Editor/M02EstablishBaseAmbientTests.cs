#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;

public sealed class M02EstablishBaseAmbientTests
{
    private const string FocusedMarker =
        "[M02EstablishBaseAmbientValidation] result=Passed tests=6";
    private static readonly FixedString64Bytes MissionId = "saga.ch01.m02.establish_base";
    private static readonly FixedString64Bytes ScenarioId = "scenario.ch01.m02.establish_base";
    private static readonly FixedString64Bytes MapId = "opmap.ch01.forward_post_01";
    private static readonly FixedString64Bytes SessionToken = "m02-ambient-test";

    [MenuItem("Game/Validation/Run M02 Establish Base Ambient Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseAmbientTests tests = new();
            tests.ScenarioSeparatesCalmCiviliansAndBasePersonnel();
            tests.M02BasePersonnelUseCalmLoopingRoutesAcrossTheBase();
            tests.M02BasePersonnelUseFourCanonicalSoldierVariants();
            tests.M02CivilianStaffRemainCivilianButDoNotEvacuate();
            tests.M01PanicPresentationRemainsFastAndNonLooping();
            tests.M02RuntimeCreatesGameplayInertCalmPopulation();
            UnityEngine.Debug.Log(FocusedMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.Log("[M02EstablishBaseAmbientValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void ScenarioSeparatesCalmCiviliansAndBasePersonnel()
    {
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            M02EstablishBaseConfigBuilder.ScenarioPath);
        Assert.NotNull(scenario);
        Assert.AreEqual(2, scenario.AmbientPresentations.Length);
        Assert.AreEqual("ambient.ch01.m02.civilians", scenario.AmbientPresentations[0].PresentationId);
        Assert.AreEqual(4, scenario.AmbientPresentations[0].InstanceCount);
        Assert.AreEqual("ambient.ch01.m02.base_personnel", scenario.AmbientPresentations[1].PresentationId);
        Assert.AreEqual(8, scenario.AmbientPresentations[1].InstanceCount);
    }

    [Test]
    public void M02BasePersonnelUseCalmLoopingRoutesAcrossTheBase()
    {
        CampaignMissionAmbientPresentationSystem.AmbientRouteAnchors anchors = new()
        {
            First = Anchor(826.5f, 379.5f),
            Second = Anchor(940.5f, 351.5f),
            Third = Anchor(1040.5f, 394.5f)
        };
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        int[] centerCounts = new int[3];
        for (int ordinal = 0; ordinal < 8; ordinal++)
        {
            CampaignMissionAmbientPresentationSystem.AmbientRoute route =
                CampaignMissionAmbientPresentationSystem.CreateAmbientRoute(
                    CampaignMissionAmbientPresentationSystem.BasePersonnelPresentationKind,
                    in anchors,
                    ordinal,
                    2002001);
            Assert.AreEqual(1, route.Loop);
            Assert.That(route.Speed, Is.InRange(1.8f, 2.5f));
            Assert.Greater(math.distance(route.Start, route.AlleyMerge), 2f);
            Assert.Greater(math.distance(route.AlleyMerge, route.SquadPass), 2f);
            Assert.AreEqual(route.Start, route.Exit);
            minX = math.min(minX, route.Start.x);
            maxX = math.max(maxX, route.Start.x);
            centerCounts[ordinal % 3]++;
        }
        Assert.Greater(maxX - minX, 190f);
        CollectionAssert.AreEqual(new[] { 3, 3, 2 }, centerCounts);
    }

    [Test]
    public void M02BasePersonnelUseFourCanonicalSoldierVariants()
    {
        string[] expected =
        {
            "Unit_Chr_Soldier_Male_02_Alt_02",
            "Unit_Chr_Soldier_Male_02_Alt_04",
            "Unit_Chr_Soldier_Female_01_Alt_01",
            "Unit_Chr_Soldier_Female_02_Alt_01"
        };
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.AreEqual(
                expected[index],
                CampaignMissionAmbientPresentationSystem.PresentationPrefabKey(
                    CampaignMissionAmbientPresentationSystem.BasePersonnelPresentationKind,
                    index).ToString());
        }
    }

    [Test]
    public void M02CivilianStaffRemainCivilianButDoNotEvacuate()
    {
        CampaignMissionAmbientPresentationSystem.AmbientRouteAnchors anchors = new()
        {
            First = Anchor(1060.5f, 430.5f),
            Second = Anchor(1080.5f, 450.5f),
            Third = Anchor(1040.5f, 394.5f)
        };
        CampaignMissionAmbientPresentationSystem.AmbientRoute route =
            CampaignMissionAmbientPresentationSystem.CreateAmbientRoute(
                CampaignMissionAmbientPresentationSystem.CalmCivilianPresentationKind,
                in anchors,
                0,
                2002001);
        Assert.AreEqual(1, route.Loop);
        Assert.That(route.Speed, Is.InRange(1.5f, 2.2f));
        Assert.AreEqual(
            "Unit_Chr_Civilian_Male_01",
            CampaignMissionAmbientPresentationSystem.PresentationPrefabKey(
                CampaignMissionAmbientPresentationSystem.CalmCivilianPresentationKind,
                0).ToString());
    }

    [Test]
    public void M01PanicPresentationRemainsFastAndNonLooping()
    {
        CampaignMissionAmbientPresentationSystem.AmbientRouteAnchors anchors = new()
        {
            First = Anchor(0f, 0f),
            Second = Anchor(0f, 45f),
            Third = Anchor(30f, 30f)
        };
        CampaignMissionAmbientPresentationSystem.AmbientRoute route =
            CampaignMissionAmbientPresentationSystem.CreateAmbientRoute(
                CampaignMissionAmbientPresentationSystem.PanicCivilianPresentationKind,
                in anchors,
                0,
                1701);
        Assert.AreEqual(0, route.Loop);
        Assert.That(route.Speed, Is.InRange(6.4f, 8f));
        Assert.AreNotEqual(route.Start, route.Exit);
    }

    [Test]
    public void M02RuntimeCreatesGameplayInertCalmPopulation()
    {
        World world = new(nameof(M02RuntimeCreatesGameplayInertCalmPopulation));
        BlobAssetReference<CampaignMissionCatalogBlob> catalog = default;
        BlobAssetReference<OperationMapBlob> map = default;
        try
        {
            EntityManager entityManager = world.EntityManager;
            catalog = CreateCatalog();
            map = CreateMap();
            Entity root = entityManager.CreateEntity(
                typeof(CampaignMissionRootComponent),
                typeof(CampaignMissionCatalogComponent),
                typeof(CampaignMissionRuntimeComponent));
            entityManager.SetComponentData(root, new CampaignMissionCatalogComponent
            {
                Blob = catalog,
                SourceVersion = 1
            });
            entityManager.SetComponentData(root, new CampaignMissionRuntimeComponent
            {
                MissionId = MissionId,
                ScenarioId = ScenarioId,
                OperationMapId = MapId,
                SessionToken = SessionToken,
                AttemptOrdinal = 1,
                SourceVersion = 1,
                DeterministicSeed = 2002001
            });
            Entity mapEntity = entityManager.CreateEntity(typeof(OperationMapMetadataComponent));
            entityManager.SetComponentData(mapEntity, new OperationMapMetadataComponent
            {
                Blob = map,
                Generation = 1
            });
            CreateRegistry(entityManager);

            SystemHandle handle = world.GetOrCreateSystem<CampaignMissionAmbientPresentationSystem>();
            ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
            world.Unmanaged.GetUnsafeSystemRef<CampaignMissionAmbientPresentationSystem>(handle)
                .OnUpdate(ref state);

            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionAmbientCivilianComponent>(),
                ComponentType.ReadOnly<CampaignMissionAmbientCivilianMotionComponent>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            Assert.AreEqual(12, entities.Length);
            int civilianCount = 0;
            int personnelCount = 0;
            for (int index = 0; index < entities.Length; index++)
            {
                Entity entity = entities[index];
                CampaignMissionAmbientCivilianComponent presentation =
                    entityManager.GetComponentData<CampaignMissionAmbientCivilianComponent>(entity);
                CampaignMissionAmbientCivilianMotionComponent motion =
                    entityManager.GetComponentData<CampaignMissionAmbientCivilianMotionComponent>(entity);
                bool personnel = presentation.PresentationId.Equals(
                    new FixedString64Bytes("ambient.ch01.m02.base_personnel"));
                personnelCount += personnel ? 1 : 0;
                civilianCount += personnel ? 0 : 1;
                Assert.AreEqual(!personnel, entityManager.HasComponent<CivilianUnitTag>(entity));
                Assert.AreEqual(0, presentation.Evacuating);
                Assert.AreEqual(1, motion.Loop);
                Assert.AreEqual(3, entityManager.GetComponentData<UnitResolvedAnimationIndex>(entity).Value);
                Assert.IsTrue(entityManager.HasComponent<UnitForceDetailedVisualTag>(entity));
                Assert.IsFalse(entityManager.HasComponent<Faction>(entity));
                Assert.IsFalse(entityManager.HasComponent<UnitHealth>(entity));
                Assert.IsFalse(entityManager.HasComponent<UnitCombat>(entity));
                Assert.IsFalse(entityManager.HasComponent<UnitAttack>(entity));
                Assert.IsFalse(entityManager.HasComponent<AIControlledTag>(entity));
                Assert.IsFalse(entityManager.HasComponent<SelectedUnitTag>(entity));
                Assert.IsFalse(entityManager.HasComponent<UnitMove>(entity));
                Assert.IsFalse(entityManager.HasComponent<UnitMidLodPrefabReference>(entity));
                Assert.IsFalse(entityManager.HasComponent<UnitLowLodPrefabReference>(entity));
            }
            Assert.AreEqual(4, civilianCount);
            Assert.AreEqual(8, personnelCount);
        }
        finally
        {
            world.Dispose();
            if (catalog.IsCreated)
                catalog.Dispose();
            if (map.IsCreated)
                map.Dispose();
        }
    }

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateCatalog()
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob root = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref root.Missions, 1);
        missions[0].MissionId = MissionId;
        missions[0].ScenarioId = ScenarioId;
        missions[0].OperationMapId = MapId;
        BlobBuilderArray<CampaignMissionAmbientPresentationBlob> presentations =
            builder.Allocate(ref missions[0].AmbientPresentations, 2);
        presentations[0] = new CampaignMissionAmbientPresentationBlob
        {
            PresentationId = "ambient.ch01.m02.civilians",
            AnchorId = "anchor.ch01.m02.civilian_edge",
            RouteId = "route.ch01.m02.civilian_patrol",
            InstanceCount = 4
        };
        presentations[1] = new CampaignMissionAmbientPresentationBlob
        {
            PresentationId = "ambient.ch01.m02.base_personnel",
            AnchorId = "anchor.ch01.m02.resource_focus",
            RouteId = "route.ch01.m02.base_patrol",
            InstanceCount = 8
        };
        return builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
    }

    private static BlobAssetReference<OperationMapBlob> CreateMap()
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = MapId;
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref root.Anchors, 5);
        anchors[0] = NamedAnchor("anchor.ch01.m02.civilian_edge", 1060.5f, 430.5f);
        anchors[1] = NamedAnchor("anchor.ch01.m02.civilian_evacuation", 1080.5f, 450.5f);
        anchors[2] = NamedAnchor("anchor.ch01.m02.build_lot", 1040.5f, 394.5f);
        anchors[3] = NamedAnchor("anchor.ch01.m02.resource_focus", 826.5f, 379.5f);
        anchors[4] = NamedAnchor("anchor.ch01.m02.forward_post", 940.5f, 351.5f);
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }

    private static OperationMapAnchorBlob NamedAnchor(string id, float x, float z) => new()
    {
        Id = new FixedString64Bytes(id),
        Position = new float3(x, 0f, z),
        Rotation = quaternion.identity,
        Radius = 8f
    };

    private static void CreateRegistry(EntityManager entityManager)
    {
        Entity registryEntity = entityManager.CreateEntity(typeof(UnitPrefabRegistryTag));
        entityManager.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
        for (int presentationKind = CampaignMissionAmbientPresentationSystem.CalmCivilianPresentationKind;
             presentationKind <= CampaignMissionAmbientPresentationSystem.BasePersonnelPresentationKind;
             presentationKind++)
        {
            for (int prefabIndex = 0; prefabIndex < 4; prefabIndex++)
            {
                Entity prefab = entityManager.CreateEntity(
                    typeof(Prefab),
                    typeof(UnitSourcePrefabKey),
                    typeof(LocalTransform),
                    typeof(Faction),
                    typeof(UnitHealth),
                    typeof(UnitCombat),
                    typeof(UnitAttack),
                    typeof(AIControlledTag),
                    typeof(SelectedUnitTag),
                    typeof(UnitMove),
                    typeof(UnitPrevWorldPos),
                    typeof(UnitMoveVisualComponent),
                    typeof(UnitResolvedAnimationIndex),
                    typeof(UnitMidLodPrefabReference),
                    typeof(UnitLowLodPrefabReference));
                entityManager.SetComponentData(prefab, new UnitSourcePrefabKey
                {
                    Value = CampaignMissionAmbientPresentationSystem.PresentationPrefabKey(
                        (byte)presentationKind,
                        prefabIndex)
                });
                entityManager.SetComponentData(prefab, LocalTransform.Identity);
                DynamicBuffer<UnitAnimationOrderEntry> animationOrder =
                    entityManager.AddBuffer<UnitAnimationOrderEntry>(prefab);
                animationOrder.Add(new UnitAnimationOrderEntry { Kind = (byte)UnitAnimationKind.Idle });
                animationOrder.Add(new UnitAnimationOrderEntry { Kind = (byte)UnitAnimationKind.Run });
                animationOrder.Add(new UnitAnimationOrderEntry { Kind = (byte)UnitAnimationKind.Walk });
                entityManager.GetBuffer<UnitPrefabRegistryEntry>(registryEntity).Add(
                    new UnitPrefabRegistryEntry { Prefab = prefab });
            }
        }
    }

    private static OperationMapAnchorBlob Anchor(float x, float z) => new()
    {
        Position = new float3(x, 0f, z),
        Rotation = quaternion.identity,
        Radius = 8f
    };
}
#endif
