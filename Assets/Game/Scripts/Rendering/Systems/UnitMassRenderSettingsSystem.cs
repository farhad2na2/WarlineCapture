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
    private ComponentLookup<Parent> _parentLookup;
    private ComponentLookup<UnitGrid> _unitGridLookup;
    private ComponentLookup<Faction> _factionLookup;
    private ComponentLookup<Unity.Rendering.RenderBounds> _renderBoundsLookup;
    private ComponentLookup<MeshLODComponent> _meshLodLookup;
    private ComponentLookup<MeshLODGroupComponent> _meshLodGroupLookup;
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
        _parentLookup = state.GetComponentLookup<Parent>(true);
        _unitGridLookup = state.GetComponentLookup<UnitGrid>(true);
        _factionLookup = state.GetComponentLookup<Faction>(true);
        _renderBoundsLookup = state.GetComponentLookup<Unity.Rendering.RenderBounds>();
        _meshLodLookup = state.GetComponentLookup<MeshLODComponent>();
        _meshLodGroupLookup = state.GetComponentLookup<MeshLODGroupComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_renderQuery.IsEmptyIgnoreFilter)
            return;

        double startTime = Time.realtimeSinceStartupAsDouble;
        EntityManager em = state.EntityManager;
        _parentLookup.Update(ref state);
        _unitGridLookup.Update(ref state);
        _factionLookup.Update(ref state);
        _renderBoundsLookup.Update(ref state);
        _meshLodLookup.Update(ref state);
        _meshLodGroupLookup.Update(ref state);
        int totalCandidates = _renderQuery.CalculateEntityCount();
        int processed = 0;
        int applied = 0;
        int lodComponentsPatched = 0;
        int lodGroupsPatched = 0;
        int deepestUnitAncestor = 0;
        using NativeList<Entity> unitRenderEntities = new(MaxRenderEntitiesPerFrame, Allocator.Temp);
        using NativeList<Entity> processedEntities = new(MaxRenderEntitiesPerFrame, Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<Parent>>()
                     .WithAll<Unity.Rendering.RenderBounds>()
                     .WithNone<UnitMassRenderSettingsApplied>()
                     .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                     .WithEntityAccess())
        {
            if (processed >= MaxRenderEntitiesPerFrame)
                break;

            processed++;
            processedEntities.Add(entity);

            if (!TryFindUnitAncestor(entity, _parentLookup, _unitGridLookup, _factionLookup, out int depth))
                continue;

            deepestUnitAncestor = math.max(deepestUnitAncestor, depth);
            unitRenderEntities.Add(entity);
        }

        for (int i = 0; i < unitRenderEntities.Length; i++)
        {
            Entity entity = unitRenderEntities[i];
            if (!_renderBoundsLookup.HasComponent(entity))
                continue;

            Unity.Rendering.RenderBounds bounds = _renderBoundsLookup[entity];
            bounds.Value.Extents = math.max(bounds.Value.Extents, UnitRenderBoundsMinExtents);
            _renderBoundsLookup[entity] = bounds;

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

            if (_meshLodLookup.HasComponent(entity))
            {
                MeshLODComponent meshLod = _meshLodLookup[entity];
                if (meshLod.LODMask != AlwaysVisibleLodMask)
                {
                    meshLod.LODMask = AlwaysVisibleLodMask;
                    _meshLodLookup[entity] = meshLod;
                    lodComponentsPatched++;
                }

                lodGroupsPatched += PatchLodGroup(meshLod.Group, _meshLodGroupLookup);
                lodGroupsPatched += PatchLodGroup(meshLod.ParentGroup, _meshLodGroupLookup);
            }

            applied++;
        }

        for (int i = 0; i < processedEntities.Length; i++)
            ecb.AddComponent<UnitMassRenderSettingsApplied>(processedEntities[i]);

        ecb.Playback(em);
        ecb.Dispose();

        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        if (EnableMassRenderDiagnostics &&
            (lodComponentsPatched > 0 || lodGroupsPatched > 0 || totalCandidates > processed) &&
            Time.frameCount >= _nextDiagnosticFrame)
        {
            _nextDiagnosticFrame = Time.frameCount + DiagnosticIntervalFrames;
            Debug.Log($"[UnitMassRenderDiag] frame={Time.frameCount} processed={processed} applied={applied} remaining={math.max(0, totalCandidates - processed)} lodComponentsPatched={lodComponentsPatched} lodGroupsPatched={lodGroupsPatched} deepestUnitAncestor={deepestUnitAncestor} bounds={UnitRenderBoundsMinExtents.x:F0}");
        }

        if (EnableMassRenderFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect:ECS] UnitMassRenderSettingsSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms processed={processed} applied={applied} remaining={math.max(0, totalCandidates - processed)} lodComponentsPatched={lodComponentsPatched} lodGroupsPatched={lodGroupsPatched}");
    }

    private static int PatchLodGroup(Entity group, ComponentLookup<MeshLODGroupComponent> meshLodGroupLookup)
    {
        if (group == Entity.Null || !meshLodGroupLookup.HasComponent(group))
            return 0;

        MeshLODGroupComponent lodGroup = meshLodGroupLookup[group];
        bool changed =
            lodGroup.ParentMask != AlwaysVisibleLodMask ||
            !math.all(lodGroup.LODDistances0 == new float4(AlwaysVisibleLodDistance)) ||
            !math.all(lodGroup.LODDistances1 == new float4(AlwaysVisibleLodDistance));

        if (!changed)
            return 0;

        lodGroup.ParentMask = AlwaysVisibleLodMask;
        lodGroup.LODDistances0 = new float4(AlwaysVisibleLodDistance);
        lodGroup.LODDistances1 = new float4(AlwaysVisibleLodDistance);
        meshLodGroupLookup[group] = lodGroup;
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
