using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed partial class MapVehiclePlacementSpawnSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    private const int MaxPlacementsPerUpdate = 32;
    private const int VehicleDepartureClearancePaddingCells = UnitPathPlacementValidation.VehicleOccupancyPaddingCells;
    private const float UniformScaleEpsilon = 0.0001f;

    public delegate bool TryGetGridDataDelegate(
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    public delegate bool TryGetRuntimeBoundaryDelegate(EntityManager em, out Entity boundaryEntity);

    public readonly struct Context
    {
        public readonly MapVehiclePlacementConfig Config;
        public readonly Transform AuthoringVehiclesRoot;
        public readonly RuntimeUnitPrefabSystem UnitPrefabSystem;
        public readonly RuntimeUnitPrefabSystem.Context UnitPrefabContext;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly TryGetRuntimeBoundaryDelegate TryGetRuntimeBoundary;
        public readonly Action<string> LogWarning;

        public Context(
            MapVehiclePlacementConfig config,
            Transform authoringVehiclesRoot,
            RuntimeUnitPrefabSystem unitPrefabSystem,
            RuntimeUnitPrefabSystem.Context unitPrefabContext,
            TryGetGridDataDelegate tryGetGridData,
            Action<string> logWarning)
            : this(
                config,
                authoringVehiclesRoot,
                unitPrefabSystem,
                unitPrefabContext,
                tryGetGridData,
                null,
                logWarning)
        {
        }

        public Context(
            MapVehiclePlacementConfig config,
            Transform authoringVehiclesRoot,
            RuntimeUnitPrefabSystem unitPrefabSystem,
            RuntimeUnitPrefabSystem.Context unitPrefabContext,
            TryGetGridDataDelegate tryGetGridData,
            TryGetRuntimeBoundaryDelegate tryGetRuntimeBoundary,
            Action<string> logWarning)
        {
            Config = config;
            AuthoringVehiclesRoot = authoringVehiclesRoot;
            UnitPrefabSystem = unitPrefabSystem;
            UnitPrefabContext = unitPrefabContext;
            TryGetGridData = tryGetGridData;
            TryGetRuntimeBoundary = tryGetRuntimeBoundary;
            LogWarning = logWarning;
        }
    }

    private readonly InitialUnitSpawnApplySystem _unitSpawnApplySystem = new();
    private readonly InitialUnitSpawnResetSystem _unitSpawnResetSystem = new();
    private bool _queued;
    private bool _authoringHidden;
    private bool _warnedMissingConfig;
    private bool _warnedMissingPrefab;
    private int _nextPlacementIndex;
    private int _lastClearedBlockerCells;
    private uint _randomState = 0x6D2B79F5u;

    internal int LastClearedBlockerCells => _lastClearedBlockerCells;

    public void Update(Context context)
    {
        if (context.Config == null || !context.Config.SpawnOnMatchStart)
            return;

        TryPublishPlacementReadModel(context);

        if (_queued)
        {
            RefreshPlacementClearance(context);
            HideAuthoringVisuals(context);
            return;
        }

        SpawnPlacements(context);
    }

    private void SpawnPlacements(Context context)
    {
        if (context.Config.Placements == null || context.Config.Placements.Count == 0)
        {
            WarnOnce(ref _warnedMissingConfig, context, "[MapVehiclePlacement] no baked map vehicle placements configured.");
            _queued = true;
            HideAuthoringVisuals(context);
            return;
        }

        if (context.UnitPrefabContext.TryGetEntityManager == null ||
            !context.UnitPrefabContext.TryGetEntityManager(out EntityManager em))
        {
            return;
        }

        context.UnitPrefabContext.EnsureEntityQueries?.Invoke(em);
        if (context.TryGetGridData == null ||
            !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
        {
            return;
        }

        using EntityCommandBuffer ecb = new(Allocator.Temp);
        int processed = 0;
        for (; _nextPlacementIndex < context.Config.Placements.Count && processed < MaxPlacementsPerUpdate; _nextPlacementIndex++, processed++)
        {
            MapVehiclePlacementConfigEntry placement = context.Config.Placements[_nextPlacementIndex];
            if (placement == null || string.IsNullOrWhiteSpace(placement.VehicleSourceKey))
                continue;

            if (!context.UnitPrefabSystem.TryResolveConfiguredUnitPrefabEntity(
                    context.UnitPrefabContext,
                    GetVehiclePrefabSourceKey(placement),
                    out Entity prefabEntity))
            {
                WarnOnce(
                    ref _warnedMissingPrefab,
                    context,
                    $"[MapVehiclePlacement] at least one authored vehicle could not resolve an ECS prefab. First failed source={placement.SourcePath} sourceKey={placement.VehicleSourceKey}.");
                continue;
            }

            SpawnVehicle(context, em, ecb, grid, placement, prefabEntity);
        }

        ecb.Playback(em);

        if (_nextPlacementIndex >= context.Config.Placements.Count)
        {
            _queued = true;
            RefreshPlacementClearance(context);
            HideAuthoringVisuals(context);
        }
    }

    private void SpawnVehicle(
        Context context,
        EntityManager em,
        EntityCommandBuffer ecb,
        GridConfig grid,
        MapVehiclePlacementConfigEntry placement,
        Entity prefabEntity)
    {
        bool hasPrefab = prefabEntity != Entity.Null && em.Exists(prefabEntity);
        if (!hasPrefab)
            return;

        float3 center = ToFloat3(placement.WorldCenter);
        float3 position = ToFloat3(placement.WorldPosition);
        int2 cell = GridUtils.WorldToCell(grid, center);
        byte faction = placement.FactionId;
        Entity instance = _unitSpawnApplySystem.InstantiateAndConfigureSpawnedUnit(
            em,
            ecb,
            prefabEntity,
            hasPrefab,
            faction,
            cell,
            position);

        _randomState = math.max(1u, _randomState + 1u);
        var rng = new Unity.Mathematics.Random(_randomState);
        _unitSpawnResetSystem.ResetSpawnedUnitRuntimeState(em, ecb, instance, prefabEntity, hasPrefab, ref rng);
        _randomState = math.max(1u, rng.state);

        ApplyAuthoredTransform(em, ecb, instance, prefabEntity, hasPrefab, placement);
        FixedString64Bytes sourceKey = ResolveSpawnedVehicleSourceKey(em, prefabEntity, hasPrefab, placement);
        if (sourceKey.Length > 0)
            SetOrAddComponent(em, ecb, instance, prefabEntity, hasPrefab, new UnitSourcePrefabKey { Value = sourceKey });
        SetOrAddComponent(em, ecb, instance, prefabEntity, hasPrefab, new UnitRespawnPrefab { Prefab = prefabEntity });
    }

    private static FixedString64Bytes ResolveSpawnedVehicleSourceKey(
        EntityManager em,
        Entity prefabEntity,
        bool hasPrefab,
        MapVehiclePlacementConfigEntry placement)
    {
        if (hasPrefab &&
            prefabEntity != Entity.Null &&
            em.Exists(prefabEntity) &&
            em.HasComponent<UnitSourcePrefabKey>(prefabEntity))
        {
            return em.GetComponentData<UnitSourcePrefabKey>(prefabEntity).Value;
        }

        return GetVehiclePrefabSourceKey(placement);
    }

    private static void ApplyAuthoredTransform(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity instance,
        Entity prefab,
        bool hasPrefab,
        MapVehiclePlacementConfigEntry placement)
    {
        quaternion rotation = quaternion.EulerXYZ(math.radians(ToFloat3(placement.WorldEulerAngles)));
        float3 scale = ToFloat3(placement.WorldScale);
        if (IsUniformScale(scale, out float uniformScale))
        {
            SetOrAddComponent(
                em,
                ecb,
                instance,
                prefab,
                hasPrefab,
                LocalTransform.FromPositionRotationScale(ToFloat3(placement.WorldPosition), rotation, uniformScale));
            if (hasPrefab && em.HasComponent<PostTransformMatrix>(prefab))
                ecb.RemoveComponent<PostTransformMatrix>(instance);
            return;
        }

        SetOrAddComponent(
            em,
            ecb,
            instance,
            prefab,
            hasPrefab,
            LocalTransform.FromPositionRotationScale(ToFloat3(placement.WorldPosition), rotation, 1f));
        SetOrAddComponent(
            em,
            ecb,
            instance,
            prefab,
            hasPrefab,
            new PostTransformMatrix { Value = float4x4.Scale(scale) });
    }

    private static bool IsUniformScale(float3 scale, out float uniformScale)
    {
        uniformScale = math.max(UniformScaleEpsilon, scale.x);
        return math.abs(scale.x - scale.y) <= UniformScaleEpsilon &&
               math.abs(scale.x - scale.z) <= UniformScaleEpsilon;
    }

    private void RefreshPlacementClearance(Context context)
    {
        _lastClearedBlockerCells = 0;
        if (context.Config == null ||
            context.Config.Placements == null ||
            context.UnitPrefabContext.TryGetEntityManager == null ||
            !context.UnitPrefabContext.TryGetEntityManager(out EntityManager em) ||
            context.TryGetGridData == null ||
            !context.TryGetGridData(out _, out GridConfig grid, out _, out DynamicBlockerComponent blockerData) ||
            !blockerData.Blocked.IsCreated)
        {
            return;
        }

        context.UnitPrefabContext.EnsureEntityQueries?.Invoke(em);
        int clearedCells = 0;
        for (int i = 0; i < context.Config.Placements.Count; i++)
        {
            MapVehiclePlacementConfigEntry placement = context.Config.Placements[i];
            if (placement == null ||
                string.IsNullOrWhiteSpace(placement.VehicleSourceKey) ||
                !TryResolvePlacementFootprint(context, em, placement, out int2 footprintSize))
            {
                continue;
            }

            int2 centerCell = GridUtils.WorldToCell(grid, ToFloat3(placement.WorldCenter));
            clearedCells += ClearRuntimeBlockersInFootprint(grid, ref blockerData, centerCell, footprintSize, VehicleDepartureClearancePaddingCells);
        }

        _lastClearedBlockerCells = clearedCells;
    }

    private static void TryPublishPlacementReadModel(Context context)
    {
        if (context.UnitPrefabContext.TryGetEntityManager == null ||
            !context.UnitPrefabContext.TryGetEntityManager(out EntityManager em) ||
            context.TryGetRuntimeBoundary == null ||
            !context.TryGetRuntimeBoundary(em, out Entity boundaryEntity))
        {
            return;
        }

        PublishPlacementReadModel(context, em, boundaryEntity);
    }

    internal static int PublishPlacementReadModel(Context context, EntityManager em, Entity boundaryEntity)
    {
        if (context.Config == null ||
            context.Config.Placements == null ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity))
        {
            return 0;
        }

        DynamicBuffer<MapVehiclePlacementReadModel> buffer =
            EnsureBuffer<MapVehiclePlacementReadModel>(em, boundaryEntity);
        buffer.Clear();

        context.UnitPrefabContext.EnsureEntityQueries?.Invoke(em);
        int projected = 0;
        for (int i = 0; i < context.Config.Placements.Count; i++)
        {
            MapVehiclePlacementConfigEntry placement = context.Config.Placements[i];
            FixedString64Bytes sourceKey = GetVehiclePrefabSourceKey(placement);
            if (placement == null || sourceKey.Length == 0)
                continue;

            Entity prefabEntity = Entity.Null;
            int2 footprintCells = new(1, 1);
            byte hasPrefab = 0;
            if (context.UnitPrefabSystem.TryResolveConfiguredUnitPrefabEntity(
                    context.UnitPrefabContext,
                    sourceKey,
                    out Entity resolvedPrefab) &&
                resolvedPrefab != Entity.Null &&
                em.Exists(resolvedPrefab))
            {
                prefabEntity = resolvedPrefab;
                hasPrefab = 1;
                if (em.HasComponent<UnitFootprint>(prefabEntity))
                    footprintCells = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(prefabEntity).Size);
            }

            buffer.Add(new MapVehiclePlacementReadModel
            {
                PlacementIndex = i,
                SourcePath = ToFixedString128(placement.SourcePath),
                Category = ToFixedString128(placement.Category),
                VehicleSourceKey = sourceKey,
                Prefab = prefabEntity,
                FootprintCells = footprintCells,
                FactionId = placement.FactionId,
                HasPrefab = hasPrefab,
                WorldCenter = ToFloat3(placement.WorldCenter),
                WorldPosition = ToFloat3(placement.WorldPosition),
                WorldEulerAngles = ToFloat3(placement.WorldEulerAngles),
                WorldScale = ToFloat3(placement.WorldScale)
            });
            projected++;
        }

        return projected;
    }

    private static bool TryResolvePlacementFootprint(
        Context context,
        EntityManager em,
        MapVehiclePlacementConfigEntry placement,
        out int2 footprintSize)
    {
        footprintSize = new int2(1, 1);
        if (placement == null || string.IsNullOrWhiteSpace(placement.VehicleSourceKey))
            return false;

        if (context.UnitPrefabSystem.TryResolveConfiguredUnitPrefabEntity(
                context.UnitPrefabContext,
                GetVehiclePrefabSourceKey(placement),
                out Entity prefabEntity) &&
            prefabEntity != Entity.Null &&
            em.Exists(prefabEntity) &&
            em.HasComponent<UnitFootprint>(prefabEntity))
        {
            footprintSize = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(prefabEntity).Size);
            return true;
        }

        return false;
    }

    internal static int ClearRuntimeBlockersInFootprint(
        in GridConfig grid,
        ref DynamicBlockerComponent blockerData,
        int2 centerCell,
        int2 footprintSize,
        int paddingCells = 0)
    {
        if (!blockerData.Blocked.IsCreated || grid.Width <= 0 || grid.Height <= 0)
            return 0;

        int2 clampedSize = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(centerCell, clampedSize);
        int2 max = min + clampedSize;
        int padding = math.max(0, paddingCells);
        min = math.max(min - new int2(padding, padding), int2.zero);
        max = math.min(max + new int2(padding, padding), new int2(grid.Width, grid.Height));

        int clearedCells = 0;
        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = row + x;
                if ((uint)index >= (uint)blockerData.GridSize)
                    continue;

                if (blockerData.Blocked.IsSet(index))
                    clearedCells++;

                blockerData.Blocked.Set(index, false);
                if (blockerData.Counts.IsCreated && (uint)index < (uint)blockerData.Counts.Length)
                    blockerData.Counts[index] = 0;
                if (blockerData.FriendlyPassFactionIds.IsCreated && (uint)index < (uint)blockerData.FriendlyPassFactionIds.Length)
                    blockerData.FriendlyPassFactionIds[index] = byte.MaxValue;
            }
        }

        return clearedCells;
    }

    private static float3 ToFloat3(Vector3 value)
    {
        return new float3(value.x, value.y, value.z);
    }

    private static FixedString64Bytes GetVehiclePrefabSourceKey(MapVehiclePlacementConfigEntry placement)
    {
        string sourceKey = BuildingDefinitionSystem.GetSpawnableLookupKey(placement?.VehicleSourceKey);
        return string.IsNullOrWhiteSpace(sourceKey) ? default : new FixedString64Bytes(sourceKey);
    }

    private static FixedString128Bytes ToFixedString128(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? default : new FixedString128Bytes(value);
    }

    private static DynamicBuffer<T> EnsureBuffer<T>(EntityManager em, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        return em.HasBuffer<T>(entity)
            ? em.GetBuffer<T>(entity)
            : em.AddBuffer<T>(entity);
    }

    private static void SetOrAddComponent<T>(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity instance,
        Entity prefab,
        bool hasPrefab,
        T component)
        where T : unmanaged, IComponentData
    {
        if (hasPrefab && em.HasComponent<T>(prefab))
            ecb.SetComponent(instance, component);
        else
            ecb.AddComponent(instance, component);
    }

    private void HideAuthoringVisuals(Context context)
    {
        if (_authoringHidden ||
            context.Config == null ||
            !context.Config.HideAuthoringVisualsAfterSpawn ||
            context.AuthoringVehiclesRoot == null)
        {
            return;
        }

        context.AuthoringVehiclesRoot.gameObject.SetActive(false);
        _authoringHidden = true;
    }

    private static void WarnOnce(ref bool flag, Context context, string message)
    {
        if (flag)
            return;

        flag = true;
        context.LogWarning?.Invoke(message);
    }
}
