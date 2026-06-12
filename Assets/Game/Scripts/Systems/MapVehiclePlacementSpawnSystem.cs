using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class MapVehiclePlacementSpawnSystem
{
    private const int MaxPlacementsPerUpdate = 32;
    private const float UniformScaleEpsilon = 0.0001f;

    public delegate bool TryGetGridDataDelegate(
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    public readonly struct Context
    {
        public readonly MapVehiclePlacementConfig Config;
        public readonly Transform AuthoringVehiclesRoot;
        public readonly RuntimeUnitPrefabSystem UnitPrefabSystem;
        public readonly RuntimeUnitPrefabSystem.Context UnitPrefabContext;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly Action<string> LogWarning;

        public Context(
            MapVehiclePlacementConfig config,
            Transform authoringVehiclesRoot,
            RuntimeUnitPrefabSystem unitPrefabSystem,
            RuntimeUnitPrefabSystem.Context unitPrefabContext,
            TryGetGridDataDelegate tryGetGridData,
            Action<string> logWarning)
        {
            Config = config;
            AuthoringVehiclesRoot = authoringVehiclesRoot;
            UnitPrefabSystem = unitPrefabSystem;
            UnitPrefabContext = unitPrefabContext;
            TryGetGridData = tryGetGridData;
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
    private uint _randomState = 0x6D2B79F5u;

    public void Update(Context context)
    {
        if (context.Config == null || !context.Config.SpawnOnMatchStart)
            return;

        if (_queued)
        {
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
            if (placement == null || placement.VehiclePrefab == null)
                continue;

            if (context.UnitPrefabSystem == null ||
                !context.UnitPrefabSystem.TryResolveConfiguredUnitPrefabEntity(
                    context.UnitPrefabContext,
                    placement.VehiclePrefab,
                    out Entity prefabEntity))
            {
                WarnOnce(
                    ref _warnedMissingPrefab,
                    context,
                    $"[MapVehiclePlacement] at least one authored vehicle could not resolve an ECS prefab. First failed source={placement.SourcePath} prefab={placement.VehiclePrefab.name}.");
                continue;
            }

            SpawnVehicle(context, em, ecb, grid, placement, prefabEntity);
        }

        ecb.Playback(em);

        if (_nextPlacementIndex >= context.Config.Placements.Count)
        {
            _queued = true;
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
        SetOrAddComponent(em, ecb, instance, prefabEntity, hasPrefab, new UnitRespawnPrefab { Prefab = prefabEntity });
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

    private static float3 ToFloat3(Vector3 value)
    {
        return new float3(value.x, value.y, value.z);
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
