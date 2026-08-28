#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBasePlacementTests
{
    private const string MissionId = "saga.ch01.m02.establish_base";
    private const string ScenarioId = "scenario.ch01.m02.establish_base";
    private const string MapId = "opmap.ch01.forward_post_01";
    private const string BarracksId = "Building_Barrack";
    private const string BuildAnchorId = "anchor.ch01.m02.build_lot";
    private static readonly RectInt CanonicalPlacement = new(1750, 773, 20, 10);

    [MenuItem("Game/Validation/Run M02 Establish Base Placement Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBasePlacementTests tests = new();
            tests.CanonicalProjectionCarriesExactBuildZone();
            tests.SameVersionBuildZoneChangeReprojectsExactData();
            tests.BarracksInsideCanonicalLotIsAccepted();
            tests.BarracksCrossingAnyCanonicalLotEdgeIsRejected();
            tests.UnlistedBuildingIsRejectedForActiveM02();
            tests.MissingOrStaleMapDataFailsClosed();
            tests.DisabledMissionRuntimePreservesUnrestrictedPlacement();
            tests.WarmPlacementPolicyDoesNotAllocateManagedMemory();
            Debug.Log("[M02EstablishBasePlacementValidation] result=Passed tests=8");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBasePlacementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Placement Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(BuildingPlacementConstructionTransactionTests.RunFocusedValidation);
            RunValidation(BuildingPlacementCommitCompositionSystemHelperTests.RunFocusedValidation);
            RunValidation(BuildingPlacementValidationUtilitySystemHelperTests.RunLiveOccupancyValidation);
            RunValidation(M02EstablishBaseBuildCatalogTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseContractValidation.RunFocusedValidation);
            RunValidation(M01FirstContactContractValidation.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBasePlacementRegressionValidation] result=Passed suites=8");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBasePlacementRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }

    [Test]
    public void CanonicalProjectionCarriesExactBuildZone()
    {
        using World world = ProjectCanonicalCatalog(out Entity root);
        CampaignMissionCatalogComponent catalog =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        ref CampaignMissionBuildZoneBlob zone = ref catalog.Blob.Value.Missions[0].BuildZone;
        Assert.AreEqual(BuildAnchorId, zone.AnchorId.ToString());
        Assert.AreEqual(24, zone.HalfWidthCells);
        Assert.AreEqual(12, zone.HalfHeightCells);
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void SameVersionBuildZoneChangeReprojectsExactData()
    {
        using World world = ProjectCanonicalCatalog(out Entity root);
        CampaignMissionCatalogComponent first =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        first.Blob.Value.Missions[0].BuildZone.HalfWidthCells = 2;
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, Mission(), Scenario(), Maps(), 1, out root, out string error), error);
        CampaignMissionCatalogComponent repaired =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        Assert.AreEqual(24, repaired.Blob.Value.Missions[0].BuildZone.HalfWidthCells);
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void BarracksInsideCanonicalLotIsAccepted()
    {
        using Fixture fixture = new();
        Assert.IsTrue(fixture.IsAllowed(CanonicalPlacement));
        Assert.IsTrue(fixture.IsAllowed(new RectInt(1754, 777, 20, 10)));
    }

    [Test]
    public void BarracksCrossingAnyCanonicalLotEdgeIsRejected()
    {
        using Fixture fixture = new();
        Assert.IsFalse(fixture.IsAllowed(new RectInt(1749, 773, 20, 10)));
        Assert.IsFalse(fixture.IsAllowed(new RectInt(1750, 772, 20, 10)));
        Assert.IsFalse(fixture.IsAllowed(new RectInt(1755, 773, 20, 10)));
        Assert.IsFalse(fixture.IsAllowed(new RectInt(1750, 778, 20, 10)));
    }

    [Test]
    public void UnlistedBuildingIsRejectedForActiveM02()
    {
        using Fixture fixture = new();
        fixture.Prefab.name = "Tent_Regular";
        Assert.IsFalse(fixture.IsAllowed(CanonicalPlacement));
    }

    [Test]
    public void MissingOrStaleMapDataFailsClosed()
    {
        using (Fixture missingAnchor = new(includeBuildAnchor: false))
            Assert.IsFalse(missingAnchor.IsAllowed(CanonicalPlacement));
        using (Fixture staleMap = new(activeMapMissionId: "saga.ch01.m01.first_contact"))
            Assert.IsFalse(staleMap.IsAllowed(CanonicalPlacement));
    }

    [Test]
    public void DisabledMissionRuntimePreservesUnrestrictedPlacement()
    {
        using Fixture fixture = new(missionRuntimeEnabled: false);
        fixture.Prefab.name = "Tent_Regular";
        Assert.IsTrue(fixture.IsAllowed(new RectInt(-50, -50, 20, 10)));
    }

    [Test]
    public void WarmPlacementPolicyDoesNotAllocateManagedMemory()
    {
        using Fixture fixture = new();
        Assert.IsTrue(fixture.IsAllowed(CanonicalPlacement));
        long before = GC.GetAllocatedBytesForCurrentThread();
        bool allAllowed = true;
        for (int index = 0; index < 300; index++)
            allAllowed &= fixture.IsAllowed(CanonicalPlacement);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsTrue(allAllowed);
        Assert.AreEqual(0L, allocated);
    }

    private static World ProjectCanonicalCatalog(out Entity root)
    {
        World world = new("M02 placement projection");
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, Mission(), Scenario(), Maps(), 1,
            out root, out string error), error);
        return world;
    }

    private static MissionDefinitionConfig Mission() =>
        AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M02EstablishBaseConfigBuilder.MissionPath);

    private static ScenarioSetupConfig Scenario() =>
        AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(M02EstablishBaseConfigBuilder.ScenarioPath);

    private static OperationMapCatalogConfig Maps() =>
        AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(M02EstablishBaseConfigBuilder.OperationMapCatalogPath);

    private static void DisposeCatalog(EntityManager entityManager, Entity root)
    {
        CampaignMissionCatalogComponent catalog =
            entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        entityManager.SetComponentData(root, catalog);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly World _world;
        private readonly Entity _missionRoot;
        private readonly BlobAssetReference<CampaignMissionCatalogBlob> _catalogBlob;
        private readonly BlobAssetReference<OperationMapBlob> _mapBlob;
        private readonly BuildingGameplayEcsQueryCompositionSystemHelper _queries = new();
        private readonly BuildingDefinition _building;
        internal readonly GameObject Prefab;

        internal Fixture(
            bool includeBuildAnchor = true,
            bool missionRuntimeEnabled = true,
            string activeMapMissionId = MissionId)
        {
            _world = new("M02 placement policy");
            EntityManager entityManager = _world.EntityManager;
            _missionRoot = entityManager.CreateEntity(
                typeof(CampaignMissionRootComponent),
                typeof(CampaignMissionCatalogComponent),
                typeof(CampaignMissionRuntimeComponent));
            using (BlobBuilder builder = new(Allocator.Temp))
            {
                ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
                BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
                ref CampaignMissionDefinitionBlob mission = ref missions[0];
                mission.MissionId = MissionId;
                mission.MissionRuntimeEnabled = missionRuntimeEnabled ? (byte)1 : (byte)0;
                mission.BuildZone = new CampaignMissionBuildZoneBlob
                {
                    AnchorId = BuildAnchorId,
                    HalfWidthCells = 12,
                    HalfHeightCells = 7
                };
                BlobBuilderArray<CampaignMissionBuildEntryBlob> entries =
                    builder.Allocate(ref mission.BuildCatalog, 1);
                entries[0] = new CampaignMissionBuildEntryBlob
                {
                    BuildingConfigId = BarracksId,
                    MaxCount = 1
                };
                _catalogBlob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            }

            entityManager.SetComponentData(_missionRoot, new CampaignMissionCatalogComponent
            {
                Blob = _catalogBlob,
                SourceVersion = 1
            });
            entityManager.SetComponentData(_missionRoot, new CampaignMissionRuntimeComponent
            {
                MissionId = MissionId,
                ScenarioId = ScenarioId,
                OperationMapId = MapId,
                SessionToken = "m02-placement",
                Phase = MissionPhaseKind.Preparing,
                Version = 1,
                SourceVersion = 1
            });

            using (BlobBuilder builder = new(Allocator.Temp))
            {
                ref OperationMapBlob map = ref builder.ConstructRoot<OperationMapBlob>();
                map.OperationMapId = MapId;
                map.Grid = new OperationMapGridBlob
                {
                    Origin = float3.zero,
                    Dimensions = new int2(2048, 1024),
                    CellSize = 1f
                };
                BlobBuilderArray<OperationMapAnchorBlob> anchors =
                    builder.Allocate(ref map.Anchors, includeBuildAnchor ? 1 : 0);
                if (includeBuildAnchor)
                {
                    anchors[0] = new OperationMapAnchorBlob
                    {
                        Id = BuildAnchorId,
                        Kind = OperationMapAnchorKind.Build,
                        Position = new float3(1762.5f, 0f, 780.5f),
                        Rotation = quaternion.identity,
                        Radius = 12f,
                        FactionId = 1,
                        LaneIndex = -1
                    };
                }
                _mapBlob = builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
            }

            Entity mapRoot = entityManager.CreateEntity(
                typeof(OperationMapRootComponent),
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent));
            entityManager.SetComponentData(mapRoot, new ActiveOperationMapComponent
            {
                OperationMapId = MapId,
                ScenarioId = ScenarioId,
                MissionId = activeMapMissionId,
                Generation = 1
            });
            entityManager.SetComponentData(mapRoot, new OperationMapMetadataComponent
            {
                Blob = _mapBlob,
                Generation = 1
            });

            Prefab = new GameObject(BarracksId);
            _building = new BuildingDefinition
            {
                Prefab = Prefab,
                DisplayName = "Barracks",
                FootprintCells = new Vector2Int(20, 10)
            };
        }

        internal bool IsAllowed(RectInt placement) =>
            CampaignMissionBuildingPlacementPolicy.IsAllowed(
                _world.EntityManager, _queries, _building, placement);

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Prefab);
            if (_catalogBlob.IsCreated)
                _catalogBlob.Dispose();
            if (_mapBlob.IsCreated)
                _mapBlob.Dispose();
            _world.Dispose();
        }
    }
}
#endif
