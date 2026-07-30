using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Game.Components;

namespace Game.Rendering
{
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
        private ComponentLookup<OperationMapEntityPresentationIdentity> _operationMapIdentityLookup;
        private ComponentLookup<OperationMapAuthoredVehiclePresentation>
            _operationMapVehicleLookup;
        private ComponentLookup<Prefab> _prefabLookup;
        private ComponentLookup<UnitDetailedVisualReference> _detailedVisualLookup;
        private ComponentLookup<UnitModelInstanceReference> _modelInstanceLookup;
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
                    ComponentType.ReadOnly<SelectionObjectOutlineTag>(),
                },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
            _parentLookup = state.GetComponentLookup<Parent>(true);
            _unitGridLookup = state.GetComponentLookup<UnitGrid>(true);
            _factionLookup = state.GetComponentLookup<Faction>(true);
            _operationMapIdentityLookup =
                state.GetComponentLookup<OperationMapEntityPresentationIdentity>(true);
            _operationMapVehicleLookup =
                state.GetComponentLookup<OperationMapAuthoredVehiclePresentation>(true);
            _prefabLookup = state.GetComponentLookup<Prefab>(true);
            _detailedVisualLookup = state.GetComponentLookup<UnitDetailedVisualReference>(true);
            _modelInstanceLookup = state.GetComponentLookup<UnitModelInstanceReference>(true);
            _renderBoundsLookup = state.GetComponentLookup<Unity.Rendering.RenderBounds>();
            _meshLodLookup = state.GetComponentLookup<MeshLODComponent>();
            _meshLodGroupLookup = state.GetComponentLookup<MeshLODGroupComponent>();
            state.RequireForUpdate(_renderQuery);
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
            _operationMapIdentityLookup.Update(ref state);
            _operationMapVehicleLookup.Update(ref state);
            _prefabLookup.Update(ref state);
            _detailedVisualLookup.Update(ref state);
            _modelInstanceLookup.Update(ref state);
            _renderBoundsLookup.Update(ref state);
            _meshLodLookup.Update(ref state);
            _meshLodGroupLookup.Update(ref state);
            int totalCandidates = _renderQuery.CalculateEntityCount();
            int processed = 0;
            int applied = 0;
            int deepestUnitAncestor = 0;
            using NativeHashSet<Entity> authoredVisualRoots =
                new(64, Allocator.Temp);
            foreach (RefRO<UnitDetailedVisualReference> visualReference in SystemAPI
                         .Query<RefRO<UnitDetailedVisualReference>>()
                         .WithAll<OperationMapAuthoredVehiclePresentation>())
            {
                if (visualReference.ValueRO.Root != Entity.Null)
                    authoredVisualRoots.Add(visualReference.ValueRO.Root);
            }
            using NativeList<Entity> unitRenderEntities = new(MaxRenderEntitiesPerFrame, Allocator.TempJob);
            using NativeList<Entity> processedEntities = new(MaxRenderEntitiesPerFrame, Allocator.Temp);
            using NativeArray<int> patchCounts = new(2, Allocator.TempJob);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI
                         .Query<RefRO<Parent>>()
                         .WithAll<Unity.Rendering.RenderBounds>()
                         .WithNone<UnitMassRenderSettingsApplied, SelectionObjectOutlineTag>()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                         .WithEntityAccess())
            {
                if (processed >= MaxRenderEntitiesPerFrame)
                    break;

                processed++;

                if (!TryFindUnitAncestor(
                        entity,
                        _parentLookup,
                        _unitGridLookup,
                        _factionLookup,
                        _operationMapIdentityLookup,
                        _operationMapVehicleLookup,
                        _prefabLookup,
                        _detailedVisualLookup,
                        _modelInstanceLookup,
                        authoredVisualRoots,
                        out int depth,
                        out bool retry))
                {
                    if (!retry)
                        processedEntities.Add(entity);
                    continue;
                }

                processedEntities.Add(entity);
                deepestUnitAncestor = math.max(deepestUnitAncestor, depth);
                unitRenderEntities.Add(entity);
            }

            JobHandle patchHandle = new PatchMassRenderDataJob
            {
                Entities = unitRenderEntities.AsArray(),
                RenderBoundsLookup = _renderBoundsLookup,
                MeshLodLookup = _meshLodLookup,
                MeshLodGroupLookup = _meshLodGroupLookup,
                PatchCounts = patchCounts
            }.Schedule(state.Dependency);
            patchHandle.Complete();
            state.Dependency = patchHandle;

            applied = unitRenderEntities.Length;
            int lodComponentsPatched = patchCounts[0];
            int lodGroupsPatched = patchCounts[1];

            for (int i = 0; i < unitRenderEntities.Length; i++)
            {
                Entity entity = unitRenderEntities[i];
                if (!em.HasComponent<FactionTintTarget>(entity))
                {
                    ecb.AddComponent<FactionTintTarget>(entity);
                    ecb.AddComponent(entity, new FactionTintColor
                    {
                        Value = new float4(1f)
                    });
                    ecb.AddComponent(entity, new FactionSnivelerBaseColor
                    {
                        Value = new float4(1f)
                    });
                }
                if (em.HasComponent<SelectionObjectOutlineTag>(entity) ||
                    !em.HasComponent<RenderFilterSettings>(entity))
                {
                    continue;
                }

                RenderFilterSettings settings = em.GetSharedComponentManaged<RenderFilterSettings>(entity);
                if (settings.ShadowCastingMode == ShadowCastingMode.On && settings.ReceiveShadows && !settings.StaticShadowCaster)
                    continue;

                settings.ShadowCastingMode = ShadowCastingMode.On;
                settings.ReceiveShadows = true;
                settings.StaticShadowCaster = false;
                em.SetSharedComponentManaged(entity, settings);
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
            ComponentLookup<OperationMapEntityPresentationIdentity> operationMapIdentityLookup,
            ComponentLookup<OperationMapAuthoredVehiclePresentation> operationMapVehicleLookup,
            ComponentLookup<Prefab> prefabLookup,
            ComponentLookup<UnitDetailedVisualReference> detailedVisualLookup,
            ComponentLookup<UnitModelInstanceReference> modelInstanceLookup,
            NativeHashSet<Entity> authoredVisualRoots,
            out int foundDepth,
            out bool retry)
        {
            Entity current = entity;
            foundDepth = 0;
            retry = false;
            for (int depth = 0; depth < MaxParentSearchDepth; depth++)
            {
                if (prefabLookup.HasComponent(current))
                    return false;
                if (authoredVisualRoots.Contains(current))
                    return false;
                if (operationMapIdentityLookup.HasComponent(current))
                    return false;
                if (!parentLookup.HasComponent(current))
                    return false;

                current = parentLookup[current].Value;
                if (prefabLookup.HasComponent(current) ||
                    authoredVisualRoots.Contains(current) ||
                    operationMapIdentityLookup.HasComponent(current) ||
                    operationMapVehicleLookup.HasComponent(current))
                {
                    return false;
                }
                if (unitGridLookup.HasComponent(current) && factionLookup.HasComponent(current))
                {
                    if (!detailedVisualLookup.HasComponent(current) &&
                        !modelInstanceLookup.HasComponent(current))
                    {
                        retry = true;
                        return false;
                    }
                    foundDepth = depth + 1;
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private struct PatchMassRenderDataJob : IJob
        {
            [ReadOnly] public NativeArray<Entity> Entities;
            public ComponentLookup<Unity.Rendering.RenderBounds> RenderBoundsLookup;
            public ComponentLookup<MeshLODComponent> MeshLodLookup;
            public ComponentLookup<MeshLODGroupComponent> MeshLodGroupLookup;
            public NativeArray<int> PatchCounts;

            public void Execute()
            {
                int lodComponentsPatched = 0;
                int lodGroupsPatched = 0;
                for (int i = 0; i < Entities.Length; i++)
                {
                    Entity entity = Entities[i];
                    if (!RenderBoundsLookup.HasComponent(entity))
                        continue;

                    Unity.Rendering.RenderBounds bounds = RenderBoundsLookup[entity];
                    bounds.Value.Extents = math.max(bounds.Value.Extents, UnitRenderBoundsMinExtents);
                    RenderBoundsLookup[entity] = bounds;

                    if (!MeshLodLookup.HasComponent(entity))
                        continue;

                    MeshLODComponent meshLod = MeshLodLookup[entity];
                    if (meshLod.LODMask != AlwaysVisibleLodMask)
                    {
                        meshLod.LODMask = AlwaysVisibleLodMask;
                        MeshLodLookup[entity] = meshLod;
                        lodComponentsPatched++;
                    }

                    lodGroupsPatched += PatchLodGroup(meshLod.Group, MeshLodGroupLookup);
                    lodGroupsPatched += PatchLodGroup(meshLod.ParentGroup, MeshLodGroupLookup);
                }

                PatchCounts[0] = lodComponentsPatched;
                PatchCounts[1] = lodGroupsPatched;
            }
        }
    }
}
