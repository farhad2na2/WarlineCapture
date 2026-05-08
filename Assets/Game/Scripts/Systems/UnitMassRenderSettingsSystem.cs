using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateAfter(typeof(UnitModelSpawnSystem))]
public partial struct UnitMassRenderSettingsSystem : ISystem
{
    private static readonly bool EnableMassRenderDiagnostics = false;
    private static readonly bool EnableMassRenderFreezeLogs = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private const int MaxRenderEntitiesPerFrame = 12000;
    private const int MaxParentSearchDepth = 32;
    private const int DiagnosticIntervalFrames = 180;
    private const int AlwaysVisibleLodMask = 0xFF;
    private const float AlwaysVisibleLodDistance = 1048576f;
    private static readonly float3 UnitRenderBoundsMinExtents = new float3(64f, 64f, 64f);

    private EntityQuery _renderQuery;
    private int _nextDiagnosticFrame;

    public void OnCreate(ref SystemState state)
    {
        _renderQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Parent>(),
                ComponentType.ReadOnly<Unity.Rendering.RenderBounds>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitMassRenderSettingsApplied>(),
            },
            Options = EntityQueryOptions.IncludeDisabledEntities
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_renderQuery.IsEmptyIgnoreFilter)
            return;

        double startTime = Time.realtimeSinceStartupAsDouble;
        EntityManager em = state.EntityManager;
        var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
        var unitGridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true);
        var factionLookup = SystemAPI.GetComponentLookup<Faction>(true);

        using NativeArray<Entity> entities = _renderQuery.ToEntityArray(Allocator.Temp);
        using NativeList<Entity> unitRenderEntities = new(MaxRenderEntitiesPerFrame, Allocator.Temp);
        using NativeList<Entity> processedEntities = new(MaxRenderEntitiesPerFrame, Allocator.Temp);
        int processed = 0;
        int applied = 0;
        int lodComponentsPatched = 0;
        int lodGroupsPatched = 0;
        int deepestUnitAncestor = 0;
        for (int i = 0; i < entities.Length && processed < MaxRenderEntitiesPerFrame; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            processed++;
            processedEntities.Add(entity);
            if (TryFindUnitAncestor(entity, parentLookup, unitGridLookup, factionLookup, out int depth))
            {
                deepestUnitAncestor = math.max(deepestUnitAncestor, depth);
                unitRenderEntities.Add(entity);
            }
        }

        for (int i = 0; i < unitRenderEntities.Length; i++)
        {
            Entity entity = unitRenderEntities[i];
            if (!em.Exists(entity))
                continue;

            Unity.Rendering.RenderBounds bounds = em.GetComponentData<Unity.Rendering.RenderBounds>(entity);
            bounds.Value.Extents = math.max(bounds.Value.Extents, UnitRenderBoundsMinExtents);
            em.SetComponentData(entity, bounds);

            if (em.HasComponent<RenderFilterSettings>(entity))
            {
                RenderFilterSettings settings = em.GetSharedComponentManaged<RenderFilterSettings>(entity);
                if (settings.ShadowCastingMode != ShadowCastingMode.On || !settings.ReceiveShadows || settings.StaticShadowCaster)
                {
                    settings.ShadowCastingMode = ShadowCastingMode.On;
                    settings.ReceiveShadows = true;
                    settings.StaticShadowCaster = false;
                    em.SetSharedComponentManaged(entity, settings);
                }
            }

            if (em.HasComponent<MeshLODComponent>(entity))
            {
                MeshLODComponent meshLod = em.GetComponentData<MeshLODComponent>(entity);
                if (meshLod.LODMask != AlwaysVisibleLodMask)
                {
                    meshLod.LODMask = AlwaysVisibleLodMask;
                    em.SetComponentData(entity, meshLod);
                    lodComponentsPatched++;
                }

                lodGroupsPatched += PatchLodGroup(em, meshLod.Group);
                lodGroupsPatched += PatchLodGroup(em, meshLod.ParentGroup);
            }

            applied++;
        }

        for (int i = 0; i < processedEntities.Length; i++)
        {
            Entity entity = processedEntities[i];
            if (!em.Exists(entity) || em.HasComponent<UnitMassRenderSettingsApplied>(entity))
                continue;

            em.AddComponent<UnitMassRenderSettingsApplied>(entity);
        }

        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        if (EnableMassRenderDiagnostics &&
            (lodComponentsPatched > 0 || lodGroupsPatched > 0 || entities.Length > processed) &&
            Time.frameCount >= _nextDiagnosticFrame)
        {
            _nextDiagnosticFrame = Time.frameCount + DiagnosticIntervalFrames;
            Debug.Log($"[UnitMassRenderDiag] frame={Time.frameCount} processed={processed} applied={applied} remaining={math.max(0, entities.Length - processed)} lodComponentsPatched={lodComponentsPatched} lodGroupsPatched={lodGroupsPatched} deepestUnitAncestor={deepestUnitAncestor} bounds={UnitRenderBoundsMinExtents.x:F0}");
        }

        if (EnableMassRenderFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect:ECS] UnitMassRenderSettingsSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms processed={processed} applied={applied} remaining={math.max(0, entities.Length - processed)} lodComponentsPatched={lodComponentsPatched} lodGroupsPatched={lodGroupsPatched}");
    }

    private static int PatchLodGroup(EntityManager em, Entity group)
    {
        if (group == Entity.Null || !em.Exists(group) || !em.HasComponent<MeshLODGroupComponent>(group))
            return 0;

        MeshLODGroupComponent lodGroup = em.GetComponentData<MeshLODGroupComponent>(group);
        bool changed =
            lodGroup.ParentMask != AlwaysVisibleLodMask ||
            !math.all(lodGroup.LODDistances0 == new float4(AlwaysVisibleLodDistance)) ||
            !math.all(lodGroup.LODDistances1 == new float4(AlwaysVisibleLodDistance));

        if (!changed)
            return 0;

        lodGroup.ParentMask = AlwaysVisibleLodMask;
        lodGroup.LODDistances0 = new float4(AlwaysVisibleLodDistance);
        lodGroup.LODDistances1 = new float4(AlwaysVisibleLodDistance);
        em.SetComponentData(group, lodGroup);
        return 1;
    }

    private static bool TryFindUnitAncestor(
        Entity entity,
        ComponentLookup<Parent> parentLookup,
        ComponentLookup<UnitGrid> unitGridLookup,
        ComponentLookup<Faction> factionLookup,
        out int foundDepth)
    {
        Entity current = entity;
        foundDepth = 0;
        for (int depth = 0; depth < MaxParentSearchDepth; depth++)
        {
            if (!parentLookup.HasComponent(current))
                return false;

            current = parentLookup[current].Value;
            if (unitGridLookup.HasComponent(current) && factionLookup.HasComponent(current))
            {
                foundDepth = depth + 1;
                return true;
            }
        }

        return false;
    }
}
