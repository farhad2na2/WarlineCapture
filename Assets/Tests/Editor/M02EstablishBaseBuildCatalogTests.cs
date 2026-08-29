#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseBuildCatalogTests
{
    private const string Marker = "[M02EstablishBaseBuildCatalogValidation] result=Passed tests=10";
    private const string MissionId = "saga.ch01.m02.establish_base";
    private const string ScenarioId = "scenario.ch01.m02.establish_base";
    private const string MapId = "opmap.ch01.forward_post_01";
    private const string BarracksId = "Building_Barrack";

    [MenuItem("Game/Validation/Run M02 Establish Base Build Catalog Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseBuildCatalogTests tests = new();
            tests.CanonicalProjectionCarriesExactBarracksCatalog();
            tests.SameVersionCatalogContentChangeReprojectsBuildCatalog();
            tests.M02GatewayReturnsExactCatalogEntry();
            tests.M02BuildDrawerSourceExposesOnlyBarracks();
            tests.CompletedBarracksExposesOnlyCanonicalRifle();
            tests.CompletedBarracksFallsBackToBuildingPlacementUnitRegistry();
            tests.CanonicalDrawerSourcesResolveActualRifleItem();
            tests.MissionCatalogMissingFromGlobalSourceFailsClosed();
            tests.DisabledMissionRuntimeDoesNotRestrictCatalog();
            tests.UnrestrictedBuildDrawerSourcePreservesFullCatalog();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseBuildCatalogValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Build Catalog Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(BuildDrawerCatalogQueryUiSystemHelperTests.RunFocusedValidation);
            RunValidation(M02EstablishBaseContractValidation.RunFocusedValidation);
            RunValidation(M02EstablishBaseLaunchTests.RunFocusedValidation);
            RunValidation(M01FirstContactContractValidation.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseBuildCatalogRegressionValidation] result=Passed suites=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseBuildCatalogRegressionValidation] result=Failed");
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
    public void CanonicalProjectionCarriesExactBarracksCatalog()
    {
        using World world = ProjectCanonicalCatalog(1, out Entity root);
        CampaignMissionCatalogComponent catalog =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[0];
        Assert.AreEqual(1, definition.BuildCatalog.Length);
        Assert.AreEqual(BarracksId, definition.BuildCatalog[0].BuildingConfigId.ToString());
        Assert.AreEqual(1, definition.BuildCatalog[0].MaxCount);
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void SameVersionCatalogContentChangeReprojectsBuildCatalog()
    {
        MissionDefinitionConfig mission = LoadMission();
        ScenarioSetupConfig scenario = LoadScenario();
        OperationMapCatalogConfig maps = LoadMaps();
        using World world = new(nameof(SameVersionCatalogContentChangeReprojectsBuildCatalog));
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, mission, scenario, maps, 1, out Entity root, out string error), error);
        CampaignMissionCatalogComponent first =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        first.Blob.Value.Missions[0].BuildCatalog[0].MaxCount = 2;

        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, mission, scenario, maps, 1, out root, out error), error);
        CampaignMissionCatalogComponent repaired =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        Assert.AreEqual(1, repaired.Blob.Value.Missions[0].BuildCatalog[0].MaxCount);
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void M02GatewayReturnsExactCatalogEntry()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = ProjectCanonicalCatalog(1, out Entity root);
        try
        {
            SetActiveMission(world.EntityManager, root, MissionId);
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.IsTrue(UiShellRuntimeGateway.TryReadMissionBuildCatalog(
                out UiMissionBuildCatalogModel catalog));
            Assert.AreEqual(MissionId, catalog.MissionId);
            Assert.AreEqual(1, catalog.EntryCount);
            Assert.IsTrue(UiShellRuntimeGateway.TryReadMissionBuildCatalogEntry(
                0, out UiMissionBuildCatalogEntryModel entry));
            Assert.AreEqual(BarracksId, entry.BuildingConfigId);
            Assert.AreEqual(1, entry.MaxCount);
            Assert.IsFalse(UiShellRuntimeGateway.TryReadMissionBuildCatalogEntry(1, out _));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            DisposeCatalog(world.EntityManager, root);
        }
    }

    [Test]
    public void M02BuildDrawerSourceExposesOnlyBarracks()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = ProjectCanonicalCatalog(1, out Entity root);
        GameObject barracks = new(BarracksId);
        GameObject tent = new("Tent_Regular");
        GameObject barrier = new("Building_Road_Barrier");
        GameObject rifle = new("Unit_Chr_Soldier_Male_02_Alt_04");
        try
        {
            SetActiveMission(world.EntityManager, root, MissionId);
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            PrefabSource units = new(new[] { rifle }, Array.Empty<GameObject>());
            PrefabSource buildings = new(Array.Empty<GameObject>(), new[] { tent, barracks, barrier });
            BuildDrawerMissionCatalogPrefabSource filtered = new();
            filtered.Refresh(units, buildings);

            Assert.AreEqual(0, filtered.UnitSpawnPrefabs.Count);
            Assert.AreEqual(1, filtered.BuildingSpawnPrefabs.Count);
            Assert.AreSame(barracks, filtered.BuildingSpawnPrefabs[0]);
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            World.DefaultGameObjectInjectionWorld = previous;
            DisposeCatalog(world.EntityManager, root);
            UnityEngine.Object.DestroyImmediate(barracks);
            UnityEngine.Object.DestroyImmediate(tent);
            UnityEngine.Object.DestroyImmediate(barrier);
            UnityEngine.Object.DestroyImmediate(rifle);
        }
    }

    [Test]
    public void MissionCatalogMissingFromGlobalSourceFailsClosed()
    {
        GameObject tent = new("Tent_Regular");
        try
        {
            BuildDrawerMissionCatalogPrefabSource filtered = new();
            filtered.ApplyForTests(
                new PrefabSource(Array.Empty<GameObject>(), Array.Empty<GameObject>()),
                new PrefabSource(Array.Empty<GameObject>(), new[] { tent }),
                true,
                new[] { new UiMissionBuildCatalogEntryModel(BarracksId, 1) });
            Assert.AreEqual(0, filtered.UnitSpawnPrefabs.Count);
            Assert.AreEqual(0, filtered.BuildingSpawnPrefabs.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tent);
        }
    }

    [Test]
    public void CompletedBarracksExposesOnlyCanonicalRifle()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = ProjectCanonicalCatalog(1, out Entity root);
        GameObject rifle = new("Unit_Chr_Soldier_Male_02_Alt_04");
        GameObject other = new("Unit_Chr_Soldier_Male_01_Alt_01");
        GameObject barracks = new(BarracksId);
        try
        {
            SetActiveMission(world.EntityManager, root, MissionId);
            if (!world.EntityManager.HasComponent<CampaignMissionAttemptFactsComponent>(root))
                world.EntityManager.AddComponent<CampaignMissionAttemptFactsComponent>(root);
            world.EntityManager.SetComponentData(root, new CampaignMissionAttemptFactsComponent
            {
                RequiredBuildingPlacedCount = 1,
                RequiredBuildingCompletedCount = 1
            });
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            PrefabSource units = new(new[] { other, rifle }, Array.Empty<GameObject>());
            PrefabSource buildings = new(Array.Empty<GameObject>(), new[] { barracks });
            BuildDrawerMissionCatalogPrefabSource filtered = new();
            filtered.Refresh(units, buildings);

            Assert.IsTrue(UiShellRuntimeGateway.TryReadMissionBuildCatalog(
                out UiMissionBuildCatalogModel catalog));
            Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", catalog.RequiredUnitConfigId);
            Assert.IsTrue(catalog.RequiredProducerCompleted);
            Assert.IsTrue(catalog.CanRequestRequiredUnit);
            Assert.AreEqual(1, filtered.UnitSpawnPrefabs.Count);
            Assert.AreSame(rifle, filtered.UnitSpawnPrefabs[0]);
            Assert.AreEqual(1, filtered.BuildingSpawnPrefabs.Count);
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            World.DefaultGameObjectInjectionWorld = previous;
            DisposeCatalog(world.EntityManager, root);
            UnityEngine.Object.DestroyImmediate(rifle);
            UnityEngine.Object.DestroyImmediate(other);
            UnityEngine.Object.DestroyImmediate(barracks);
        }
    }

    [Test]
    public void CompletedBarracksFallsBackToBuildingPlacementUnitRegistry()
    {
        GameObject rifle = new("Unit_Chr_Soldier_Male_02_Alt_04");
        GameObject barracks = new(BarracksId);
        try
        {
            PrefabSource missingDedicatedRegistry = new(null, Array.Empty<GameObject>());
            PrefabSource placementRegistry = new(new[] { rifle }, new[] { barracks });
            BuildDrawerMissionCatalogPrefabSource filtered = new();
            filtered.ApplyForTests(
                missingDedicatedRegistry,
                placementRegistry,
                true,
                new[] { new UiMissionBuildCatalogEntryModel(BarracksId, 1) },
                rifle.name);

            Assert.AreEqual(1, filtered.UnitSpawnPrefabs.Count);
            Assert.AreSame(rifle, filtered.UnitSpawnPrefabs[0]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rifle);
            UnityEngine.Object.DestroyImmediate(barracks);
        }
    }

    [Test]
    public void CanonicalDrawerSourcesResolveActualRifleItem()
    {
        UnitPrefabRegistryAuthoringConfig unitRegistry =
            AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(
                "Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset");
        BuildingPlacementSystemConfig buildingPlacement =
            AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
        Assert.NotNull(unitRegistry);
        Assert.NotNull(buildingPlacement);

        BuildDrawerMissionCatalogPrefabSource filtered = new();
        filtered.ApplyForTests(
            unitRegistry,
            buildingPlacement,
            true,
            new[] { new UiMissionBuildCatalogEntryModel(BarracksId, 1) },
            "Unit_Chr_Soldier_Male_02_Alt_04");
        Assert.AreEqual(1, filtered.UnitSpawnPrefabs.Count);
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", filtered.UnitSpawnPrefabs[0].name);

        BuildDrawerCatalogQueryUiSystemHelper query = new();
        query.ConfigureMetadataResolvers(
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);
        List<BuildDrawerCatalogItem> items = new();
        query.Collect(filtered, filtered, BuildDrawerCategory.Soldiers, items);
        Assert.AreEqual(1, items.Count);
        Assert.AreSame(filtered.UnitSpawnPrefabs[0], items[0].Prefab);
        Assert.AreEqual(BuildDrawerCategory.Soldiers, items[0].Category);
    }

    [Test]
    public void DisabledMissionRuntimeDoesNotRestrictCatalog()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = CreateCatalogWorld(false, false, out Entity root,
            out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.IsFalse(UiShellRuntimeGateway.TryReadMissionBuildCatalog(
                out UiMissionBuildCatalogModel catalog));
            Assert.IsFalse(catalog.IsActive);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            blob.Dispose();
        }
    }

    [Test]
    public void UnrestrictedBuildDrawerSourcePreservesFullCatalog()
    {
        GameObject barracks = new(BarracksId);
        GameObject tent = new("Tent_Regular");
        GameObject rifle = new("Unit_Chr_Soldier_Male_02_Alt_04");
        try
        {
            PrefabSource units = new(new[] { rifle }, Array.Empty<GameObject>());
            PrefabSource buildings = new(Array.Empty<GameObject>(), new[] { barracks, tent });
            BuildDrawerMissionCatalogPrefabSource source = new();
            source.ApplyForTests(units, buildings, false, null);
            Assert.AreSame(units.UnitSpawnPrefabs, source.UnitSpawnPrefabs);
            Assert.AreSame(buildings.BuildingSpawnPrefabs, source.BuildingSpawnPrefabs);
            Assert.AreEqual(1, source.UnitSpawnPrefabs.Count);
            Assert.AreEqual(2, source.BuildingSpawnPrefabs.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(barracks);
            UnityEngine.Object.DestroyImmediate(tent);
            UnityEngine.Object.DestroyImmediate(rifle);
        }
    }

    private static World ProjectCanonicalCatalog(uint sourceVersion, out Entity root)
    {
        World world = new($"M02 build catalog {sourceVersion}");
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, LoadMission(), LoadScenario(), LoadMaps(), sourceVersion,
            out root, out string error), error);
        return world;
    }

    private static World CreateCatalogWorld(
        bool missionRuntimeEnabled,
        bool includeBarracks,
        out Entity root,
        out BlobAssetReference<CampaignMissionCatalogBlob> blob)
    {
        World world = new("Mission build catalog gateway");
        root = world.EntityManager.CreateEntity(
            typeof(CampaignMissionRootComponent),
            typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionRuntimeComponent));
        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        missions[0].MissionId = MissionId;
        missions[0].MissionRuntimeEnabled = missionRuntimeEnabled ? (byte)1 : (byte)0;
        BlobBuilderArray<CampaignMissionBuildEntryBlob> entries = builder.Allocate(
            ref missions[0].BuildCatalog, includeBarracks ? 1 : 0);
        if (includeBarracks)
        {
            entries[0] = new CampaignMissionBuildEntryBlob
            {
                BuildingConfigId = BarracksId,
                MaxCount = 1
            };
        }

        blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        world.EntityManager.SetComponentData(root, new CampaignMissionCatalogComponent
        {
            Blob = blob,
            SourceVersion = 1
        });
        SetActiveMission(world.EntityManager, root, MissionId);
        return world;
    }

    private static void SetActiveMission(EntityManager entityManager, Entity root, string missionId)
    {
        entityManager.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = missionId,
            ScenarioId = ScenarioId,
            OperationMapId = MapId,
            SessionToken = "m02-build-catalog",
            Phase = MissionPhaseKind.Preparing,
            Version = 1,
            SourceVersion = 1
        });
    }

    private static MissionDefinitionConfig LoadMission() =>
        AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M02EstablishBaseConfigBuilder.MissionPath);

    private static ScenarioSetupConfig LoadScenario() =>
        AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(M02EstablishBaseConfigBuilder.ScenarioPath);

    private static OperationMapCatalogConfig LoadMaps() =>
        AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(M02EstablishBaseConfigBuilder.OperationMapCatalogPath);

    private static void DisposeCatalog(EntityManager entityManager, Entity root)
    {
        CampaignMissionCatalogComponent catalog =
            entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        entityManager.SetComponentData(root, catalog);
    }

    private sealed class PrefabSource : ICatalogPrefabSource
    {
        public PrefabSource(
            IReadOnlyList<GameObject> units,
            IReadOnlyList<GameObject> buildings)
        {
            UnitSpawnPrefabs = units;
            BuildingSpawnPrefabs = buildings;
        }

        public IReadOnlyList<GameObject> UnitSpawnPrefabs { get; }
        public IReadOnlyList<GameObject> BuildingSpawnPrefabs { get; }
    }
}
#endif
