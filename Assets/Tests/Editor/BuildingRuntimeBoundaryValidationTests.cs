using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class BuildingRuntimeBoundaryValidationTests
{
    private const string BuildingPlacementConfigPath = "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset";
    private const string InitialSpawnConfigPath = "Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset";
    private const string MapBuildingPlacementConfigPath = "Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset";
    private const string TentRegularPrefabPath = "Assets/Game/Prefabs/Buildings/Tent_Regular.prefab";
    private const string OilPumpPrefabPath = "Assets/Game/Prefabs/Buildings/Building_OilPump.prefab";

    private World _previousDefaultWorld;
    private World _world;
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private GameObject _runtimeRoot;
    private GameObject _buildingPrefab;
    private BuildingPlacementSystemConfig _buildingConfig;
    private BuildingGameplayCompositionSystemHelper _buildingComposition;
    private BuildingGameplayResultCompositionSystemHelper.Result _buildingGameplay;
    private bool _buildingGameplayInitialized;

    public static void RunBatchValidation()
    {
        var tests = new BuildingRuntimeBoundaryValidationTests();
        try
        {
            tests.RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges();
            tests.TearDown();
            tests.RuntimeSpawnRequestCompletionRunsDuringStartupTick();
            tests.TearDown();
            tests.Faction2InitialConfiguredBuildingSpawnsTentFromCurrentAssets();
            tests.TearDown();
            tests.MapAuthoredFaction2PlacementsDoNotSpawnOilPumpOverInitialTent();
            tests.TearDown();
            tests.RuntimeSpawnCommandEnqueuesBoundarySpawnRequest();
            tests.TearDown();
            tests.RuntimeCitySpawnUsesBoundarySpawnRequestAndPreservesUnownedBuilding();
            tests.TearDown();
            tests.RuntimeSpawnCommandEnqueuesWallRunSpawnRequest();
            tests.TearDown();
            tests.RuntimeSpawnCommandEnqueuesWallSegmentSpawnRequest();
            tests.TearDown();
            tests.RuntimeBoundaryPublishesProductionSlotSourceKeyReadModel();
            tests.TearDown();
            Debug.Log("[BuildingRuntimeBoundaryValidation] result=Passed tests=9");
            ValidationExit.Exit(0);
        }
        catch (System.Exception ex)
        {
            tests.TearDown();
            Debug.LogException(ex);
            Debug.LogError("[BuildingRuntimeBoundaryValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (_buildingGameplayInitialized)
            _buildingGameplay.Dispose?.Invoke();
        _buildingGameplayInitialized = false;
        _buildingComposition = null;

        if (_world != null && _world.IsCreated)
            _world.Dispose();
        World.DefaultGameObjectInjectionWorld = _previousDefaultWorld;
        _world = null;

        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();

        if (_runtimeRoot != null)
            Object.DestroyImmediate(_runtimeRoot);
        _runtimeRoot = null;
        if (_buildingPrefab != null)
            Object.DestroyImmediate(_buildingPrefab);
        _buildingPrefab = null;
        if (_buildingConfig != null)
            Object.DestroyImmediate(_buildingConfig);
        _buildingConfig = null;
    }

    [Test]
    public void MapAuthoredFaction2PlacementsDoNotSpawnOilPumpOverInitialTent()
    {
        MapBuildingPlacementConfig mapConfig =
            AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(MapBuildingPlacementConfigPath);
        GameObject oilPumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OilPumpPrefabPath);

        Assert.NotNull(mapConfig, $"Missing map building placement config at {MapBuildingPlacementConfigPath}.");
        Assert.NotNull(oilPumpPrefab, $"Missing oil pump prefab at {OilPumpPrefabPath}.");

        for (int i = 0; i < mapConfig.Placements.Count; i++)
        {
            MapBuildingPlacementConfigEntry placement = mapConfig.Placements[i];
            if (placement == null)
                continue;

            Assert.IsFalse(
                placement.FactionId == 2 && placement.BuildingPrefab == oilPumpPrefab,
                $"Map-authored Faction 2 OilPump placement shadows the initial Faction 2 Tent_Regular config. source={placement.SourcePath}");
        }
    }

    [Test]
    public void Faction2InitialConfiguredBuildingSpawnsTentFromCurrentAssets()
    {
        BuildingPlacementSystemConfig placementConfig =
            AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        InitialUnitsSpawnerAuthoringConfig initialConfig =
            AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(InitialSpawnConfigPath);
        GameObject tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TentRegularPrefabPath);
        GameObject oilPumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OilPumpPrefabPath);

        Assert.NotNull(placementConfig, $"Missing building placement config at {BuildingPlacementConfigPath}.");
        Assert.NotNull(initialConfig, $"Missing initial spawn config at {InitialSpawnConfigPath}.");
        Assert.NotNull(tentPrefab, $"Missing tent prefab at {TentRegularPrefabPath}.");
        Assert.NotNull(oilPumpPrefab, $"Missing oil pump prefab at {OilPumpPrefabPath}.");

        InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry faction2Building = null;
        for (int factionIndex = 0; factionIndex < initialConfig.Factions.Count; factionIndex++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = initialConfig.Factions[factionIndex];
            if (faction == null || faction.FactionId != 2 || faction.Buildings == null)
                continue;

            for (int buildingIndex = 0; buildingIndex < faction.Buildings.Count; buildingIndex++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry building = faction.Buildings[buildingIndex];
                if (building?.Prefab == tentPrefab)
                {
                    faction2Building = building;
                    break;
                }
            }
        }

        Assert.NotNull(faction2Building, "Faction 2 initial spawn config must contain Tent_Regular.");

        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("Faction2InitialConfiguredBuildingAssetValidation");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 512, 512);
        _runtimeRoot = new GameObject("Faction2InitialConfiguredBuilding_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: placementConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);

        CustomGameStartupSystem startupSystem = new(_world.EntityManager);
        CustomGameStartupSystem.Result startup = startupSystem.InitializeFromLegacyConfigs(initialConfig, null);
        Assert.IsTrue(startup.Initialized);

        using EntityQuery startupQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        Assert.AreEqual(1, startupQuery.CalculateEntityCount());
        Entity startupEntity = startupQuery.GetSingletonEntity();

        DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns =
            em.GetBuffer<InitialUnitsFactionSpawnEntry>(startupEntity, true);
        using NativeArray<InitialUnitsFactionSpawnEntry> factionSpawnArray =
            factionSpawns.ToNativeArray(Allocator.Temp);
        InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter logWriter = default;
        Assert.IsTrue(InitialUnitsSpawnSystem.EnqueueConfiguredInitialBuildingRequests(
            em,
            boundary,
            startupEntity,
            factionSpawnArray,
            ref logWriter,
            out int requestCount));
        Assert.AreEqual(2, requestCount);

        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        Assert.AreEqual(2, requests.Length);
        int tentRequestIndex = FindSpawnRequestIndex(requests, 2, "tent_regular");
        Assert.GreaterOrEqual(tentRequestIndex, 0, "Faction 2 tent initial spawn request was not queued.");

        Assert.DoesNotThrow(() => _buildingGameplay.RuntimeUpdate.UpdateStartup(_buildingGameplay.RuntimeUpdateContext));

        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary, true);
        BuildingRuntimeSpawnRequest tentRequest = requests[tentRequestIndex];
        Assert.AreEqual(BuildingRuntimeSpawnRequest.Succeeded, tentRequest.Status);
        Assert.AreNotEqual(0, tentRequest.BuildingRuntimeId);
        Assert.IsTrue(_buildingGameplay.RuntimeBuildings.TryGetValue(tentRequest.BuildingRuntimeId, out RuntimeBuildingEntity runtimeBuilding));
        Assert.AreSame(tentPrefab, runtimeBuilding.Definition.Prefab);
        Assert.AreNotSame(oilPumpPrefab, runtimeBuilding.Definition.Prefab);
    }

    [Test]
    public void RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BuildingRuntimeBoundaryValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 32, 32);
        _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2);
        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new System.Collections.Generic.List<GameObject> { _buildingPrefab });

        _runtimeRoot = new GameObject("BuildingRuntimeBoundary_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = 1,
            FactionId = 1,
            HasOwnerFaction = 1,
            BuildingId = new FixedString128Bytes("Tent_Regular"),
            PreferredOrigin = new int2(10, 10),
            Status = BuildingRuntimeSpawnRequest.Pending
        });

        Assert.DoesNotThrow(() => _buildingGameplay.RuntimeUpdate.Update(_buildingGameplay.RuntimeUpdateContext));

        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        Assert.AreEqual(BuildingRuntimeSpawnRequest.Succeeded, requests[0].Status);
        Assert.AreNotEqual(0, requests[0].BuildingRuntimeId);
        Assert.AreEqual(new int2(10, 10), requests[0].ActualOrigin);
        Assert.AreEqual(new int2(2, 2), requests[0].ActualFootprint);
    }

    [Test]
    public void RuntimeSpawnRequestCompletionRunsDuringStartupTick()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BuildingRuntimeBoundaryStartupTickTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 32, 32);
        _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2);
        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new System.Collections.Generic.List<GameObject> { _buildingPrefab });

        _runtimeRoot = new GameObject("BuildingRuntimeBoundary_StartupRuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = 1,
            FactionId = 1,
            HasOwnerFaction = 1,
            BuildingId = new FixedString128Bytes("Tent_Regular"),
            PreferredOrigin = new int2(6, 6),
            Status = BuildingRuntimeSpawnRequest.Pending
        });

        Assert.DoesNotThrow(() => _buildingGameplay.RuntimeUpdate.UpdateStartup(_buildingGameplay.RuntimeUpdateContext));

        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        Assert.AreEqual(BuildingRuntimeSpawnRequest.Succeeded, requests[0].Status);
        Assert.AreNotEqual(0, requests[0].BuildingRuntimeId);
        Assert.AreEqual(new int2(6, 6), requests[0].ActualOrigin);
        Assert.AreEqual(new int2(2, 2), requests[0].ActualFootprint);
    }

    [Test]
    public void RuntimeSpawnCommandEnqueuesBoundarySpawnRequest()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BuildingRuntimeSpawnCommandValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 32, 32);
        _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2);
        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new System.Collections.Generic.List<GameObject> { _buildingPrefab });

        _runtimeRoot = new GameObject("BuildingRuntimeSpawnCommand_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));

        Assert.IsTrue(_buildingGameplay.RuntimeSpawnCommand.TryEnqueueRuntimeBuildingSpawnRequest(
            em,
            "Tent_Regular",
            new Vector2Int(12, 11),
            FactionIdentity.PlayerFactionId,
            out int requestId));
        Assert.Greater(requestId, 0);

        Assert.DoesNotThrow(() => _buildingGameplay.RuntimeUpdate.Update(_buildingGameplay.RuntimeUpdateContext));

        Assert.IsTrue(_buildingGameplay.RuntimeSpawnCommand.TryGetRuntimeSpawnRequestResult(
            em,
            requestId,
            out BuildingRuntimeSpawnRequest request));
        Assert.AreEqual(BuildingRuntimeSpawnRequest.Succeeded, request.Status);
        Assert.AreEqual(1, request.HasOwnerFaction);
        Assert.AreNotEqual(0, request.BuildingRuntimeId);
        Assert.AreEqual(new int2(12, 11), request.ActualOrigin);
        Assert.AreEqual(new int2(2, 2), request.ActualFootprint);
    }

    [Test]
    public void RuntimeCitySpawnUsesBoundarySpawnRequestAndPreservesUnownedBuilding()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BuildingRuntimeCitySpawnCommandValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 32, 32);
        _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2);
        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new System.Collections.Generic.List<GameObject> { _buildingPrefab });

        _runtimeRoot = new GameObject("BuildingRuntimeCitySpawnCommand_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));

        Assert.IsTrue(_buildingGameplay.RuntimeCitySpawn.TrySpawnRuntimeBuilding(
            _buildingGameplay.RuntimeCitySpawnContext,
            _buildingPrefab,
            new Vector2Int(14, 13),
            out int buildingId,
            out Vector2Int actualOrigin,
            out Vector2Int actualFootprint,
            "Tent_Regular",
            "Runtime city spawn test.",
            new Vector2Int(2, 2),
            500));

        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(BuildingRuntimeSpawnRequest.Succeeded, requests[0].Status);
        Assert.AreEqual(0, requests[0].HasOwnerFaction);
        Assert.AreEqual(buildingId, requests[0].BuildingRuntimeId);
        Assert.AreEqual(new Vector2Int(14, 13), actualOrigin);
        Assert.AreEqual(new Vector2Int(2, 2), actualFootprint);
        Assert.IsTrue(_buildingGameplay.RuntimeBuildings.TryGetValue(buildingId, out RuntimeBuildingEntity building));
        Assert.IsFalse(building.HasOwnerFaction);
    }

    [Test]
    public void RuntimeSpawnCommandEnqueuesWallRunSpawnRequest()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BuildingRuntimeWallRunCommandValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 32, 32);
        _buildingPrefab = CreateBuildingPrefab("Wall_Regular", 4, 1);
        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new System.Collections.Generic.List<GameObject> { _buildingPrefab });

        _runtimeRoot = new GameObject("BuildingRuntimeWallRunCommand_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));

        Assert.IsTrue(_buildingGameplay.RuntimeSpawnCommand.TryEnqueueRuntimeWallRunSpawnRequest(
            em,
            "Wall_Regular",
            new Vector2Int(4, 6),
            new Vector2Int(7, 6),
            FactionIdentity.PlayerFactionId,
            out int requestId));

        Assert.DoesNotThrow(() => _buildingGameplay.RuntimeUpdate.Update(_buildingGameplay.RuntimeUpdateContext));

        Assert.IsTrue(_buildingGameplay.RuntimeSpawnCommand.TryGetRuntimeSpawnRequestResult(
            em,
            requestId,
            out BuildingRuntimeSpawnRequest request));
        Assert.AreEqual(BuildingRuntimeSpawnRequest.Succeeded, request.Status);
        Assert.AreEqual(BuildingRuntimeSpawnRequest.KindWallRun, request.RequestKind);
        Assert.AreEqual(4, request.SpawnedCount);
    }

    [Test]
    public void RuntimeSpawnCommandEnqueuesWallSegmentSpawnRequest()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BuildingRuntimeWallSegmentCommandValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;

        CreateGrid(em, 32, 32);
        _buildingPrefab = CreateBuildingPrefab("Wall_Regular", 4, 1);
        _buildingConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        SetPrivateField(_buildingConfig, "spawnables", new System.Collections.Generic.List<GameObject> { _buildingPrefab });

        _runtimeRoot = new GameObject("BuildingRuntimeWallSegmentCommand_RuntimeRoot");
        _buildingComposition = new BuildingGameplayCompositionSystemHelper();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintState: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));

        Assert.IsTrue(_buildingGameplay.RuntimeSpawnCommand.TryEnqueueRuntimeWallSegmentSpawnRequest(
            em,
            "Wall_Regular",
            new Vector2Int(4, 6),
            false,
            FactionIdentity.PlayerFactionId,
            false,
            out int requestId));

        Assert.DoesNotThrow(() => _buildingGameplay.RuntimeUpdate.Update(_buildingGameplay.RuntimeUpdateContext));

        Assert.IsTrue(_buildingGameplay.RuntimeSpawnCommand.TryGetRuntimeSpawnRequestResult(
            em,
            requestId,
            out BuildingRuntimeSpawnRequest request));
        Assert.AreEqual(BuildingRuntimeSpawnRequest.Succeeded, request.Status);
        Assert.AreEqual(BuildingRuntimeSpawnRequest.KindWallSegment, request.RequestKind);
        Assert.AreEqual(1, request.SpawnedCount);
        Assert.AreEqual(new int2(4, 6), request.ActualOrigin);
        Assert.AreEqual(new int2(1, 1), request.ActualFootprint);
    }

    [Test]
    public void RuntimeBoundaryPublishesProductionSlotSourceKeyReadModel()
    {
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BuildingRuntimeProductionSlotReadModelValidationTests");
        World.DefaultGameObjectInjectionWorld = _world;
        EntityManager em = _world.EntityManager;
        GameObject unitPrefab = new("Rifleman_Regular");
        try
        {
            _buildingPrefab = CreateBuildingPrefab("Tent_Regular", 2, 2);
            BuildingDefinitionAuthoring authoring = _buildingPrefab.GetComponent<BuildingDefinitionAuthoring>();
            SetPrivateField(authoring, "productions", new System.Collections.Generic.List<BuildingDefinitionAuthoring.ProductionDefinition>
            {
                new() { spawnUnitPrefab = unitPrefab }
            });

            var definitionSystem = new BuildingDefinitionPrefabSystemHelper();
            definitionSystem.ConfigureAuthoringMetadataResolvers(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            definitionSystem.RebuildSpawnablesLookup(
                new System.Collections.Generic.List<GameObject> { _buildingPrefab },
                new System.Collections.Generic.List<GameObject> { unitPrefab });
            definitionSystem.RebuildConfiguredSpawnableDefinitions(null, Object.DestroyImmediate);

            var boundarySystem = new BuildingRuntimeBoundaryProcessingCompositionSystemHelper();
            Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
            using EntityQuery boundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>());
            boundarySystem.Update(
                definitionSystem,
                new BuildingRuntimeSpawnSystem(),
                default,
                new BuildingProductionRequestBoundary(),
                default,
                new BuildingRuntimeReadModelCompositionSystemHelper(),
                default,
                new FactionResourceSystem(),
                em,
                boundaryQuery,
                new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>(),
                now: 0f,
                frameCount: 0);

            DynamicBuffer<BuildingProductionSlotReadModel> slots =
                em.GetBuffer<BuildingProductionSlotReadModel>(boundary, true);
            Assert.AreEqual(1, slots.Length);
            Assert.AreEqual(new FixedString128Bytes("tent_regular"), slots[0].BuildingId);
            Assert.AreEqual(0, slots[0].SlotIndex);
            Assert.AreEqual(new FixedString64Bytes("Rifleman_Regular"), slots[0].UnitSourceKey);
            Assert.AreEqual(new FixedString128Bytes("rifleman_regular"), slots[0].UnitId);
        }
        finally
        {
            Object.DestroyImmediate(unitPrefab);
        }
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
        });

        DynamicBuffer<GridRoad> roads = em.AddBuffer<GridRoad>(gridEntity);
        roads.ResizeUninitialized(gridSize);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = new GridRoad { Value = 0 };

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static GameObject CreateBuildingPrefab(string id, int width, int height)
    {
        GameObject prefab = new(id);
        BuildingDefinitionAuthoring authoring = prefab.AddComponent<BuildingDefinitionAuthoring>();
        SetPrivateField(authoring, "displayName", id);
        SetPrivateField(authoring, "description", "Boundary spawn test building.");
        SetPrivateField(authoring, "footprintCells", new Vector2Int(width, height));
        SetPrivateField(authoring, "maxHealth", 500);
        SetPrivateField(authoring, "canRequest", true);
        SetPrivateField(authoring, "price", 0);
        return prefab;
    }

    private static int FindSpawnRequestIndex(
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests,
        byte factionId,
        string buildingId)
    {
        FixedString128Bytes targetBuildingId =
            new(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId));
        for (int i = 0; i < requests.Length; i++)
        {
            BuildingRuntimeSpawnRequest request = requests[i];
            if (request.FactionId == factionId && request.BuildingId.Equals(targetBuildingId))
                return i;
        }

        return -1;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
