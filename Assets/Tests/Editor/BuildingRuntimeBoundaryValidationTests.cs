using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingRuntimeBoundaryValidationTests
{
    private World _previousDefaultWorld;
    private World _world;
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private GameObject _runtimeRoot;
    private GameObject _buildingPrefab;
    private BuildingPlacementSystemConfig _buildingConfig;
    private BuildingGameplayCompositionSystem _buildingComposition;
    private BuildingGameplayCompositionResultSystem.Result _buildingGameplay;
    private bool _buildingGameplayInitialized;

    public static void RunBatchValidation()
    {
        var tests = new BuildingRuntimeBoundaryValidationTests();
        try
        {
            tests.RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges();
            tests.TearDown();
            Debug.Log("[BuildingRuntimeBoundaryValidation] result=Passed");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            tests.TearDown();
            Debug.LogException(ex);
            Debug.LogError("[BuildingRuntimeBoundaryValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
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
        if (_buildingPrefab != null)
            Object.DestroyImmediate(_buildingPrefab);
        if (_buildingConfig != null)
            Object.DestroyImmediate(_buildingConfig);
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
        _buildingComposition = new BuildingGameplayCompositionSystem();
        _buildingGameplay = _buildingComposition.Initialize(
            buildingPlacementConfig: _buildingConfig,
            worldCamera: null,
            runtimeTransportsRoot: _runtimeRoot.transform,
            runtimeUiRoot: _runtimeRoot.transform,
            roadFootprintQuerySystem: null,
            roadFootprintQueryContext: default,
            factionVisuals: null,
            dayNight: null,
            resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeySystem.ResolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataSystem.TryGetUnitDefinitionMetadata);
        _buildingGameplayInitialized = true;

        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeBoundaryTag));
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = 1,
            FactionId = 1,
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
