using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed class MapVehiclePlacementSpawnPrefabSystemHelper
    {
        private const int MaxPlacementsPerUpdate = 32;
        private const float UniformScaleEpsilon = 0.0001f;
        private const float AuthoredVehicleAdoptionDistance = 1f;

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
        private bool _warnedMissingConfig;
        private bool _warnedMissingPrefab;
        private int _lastClearedBlockerCells;
        private bool _isQueued;
        private bool _authoringHidden;
        private bool _isComplete;

        internal int LastClearedBlockerCells => _lastClearedBlockerCells;
        public bool IsComplete => _isComplete;

        public bool IsCompleteFor(MapVehiclePlacementConfig config, Transform authoringRoot)
        {
            if (config == null || !config.SpawnOnMatchStart)
                return true;

            bool authoringHidden =
                !config.HideAuthoringVisualsAfterSpawn ||
                authoringRoot == null ||
                _authoringHidden ||
                !authoringRoot.gameObject.activeInHierarchy;
            return _isQueued && authoringHidden;
        }

        public void Update(Context context)
        {
            if (context.Config == null || !context.Config.SpawnOnMatchStart)
                return;

            if (!TryGetProgressState(context, out EntityManager em, out Entity progressEntity, out MapVehiclePlacementProgressState progress))
                return;

            // Packed operation-map gameplay entities stream independently from the managed
            // match shell. Do not consume the one-shot placement cursor until the readiness
            // contract's authored vehicles are present. Otherwise an early startup tick can
            // permanently consume the placement cursor before the baked vehicles become
            // available for the normal neutral-vehicle adoption path.
            bool requiresPackedPresentationContract =
                context.AuthoringVehiclesRoot == null &&
                context.Config.Placements != null &&
                context.Config.Placements.Count > 0;
            if (!IsAuthoredVehiclePresentationReady(em, requiresPackedPresentationContract))
                return;

            SyncProgressSnapshot(progress);
            if (IsComplete)
                return;

            TryPublishPlacementReadModel(context);

            if (progress.Queued != 0)
            {
                ReconcileAuthoredVehicleOwnership(em, context.Config);
                MapVehiclePlacementClearanceSystemHelper.RefreshPlacementClearance(context, em, ref progress);
                HideAuthoringVisuals(context, ref progress);
                SaveProgressState(em, progressEntity, progress);
                SyncProgressSnapshot(progress);
                return;
            }

            SpawnPlacements(context, em, progressEntity, ref progress);
            // Adoption must run while authored vehicles are still neutral. Reconcile only
            // afterward so late-packed stable identities receive canonical faction/source
            // data without preventing adoption or causing a duplicate prefab spawn.
            ReconcileAuthoredVehicleOwnership(em, context.Config);
            SaveProgressState(em, progressEntity, progress);
            SyncProgressSnapshot(progress);
        }

        internal static bool IsAuthoredVehiclePresentationReady(
            EntityManager em,
            bool requireReadinessContract = false)
        {
            using EntityQuery contractQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationReadinessContract>());
            if (contractQuery.IsEmptyIgnoreFilter)
                return !requireReadinessContract;

            using NativeArray<OperationMapEntityPresentationReadinessContract> contracts =
                contractQuery.ToComponentDataArray<OperationMapEntityPresentationReadinessContract>(Allocator.Temp);
            int expectedVehicleCount = 0;
            for (int i = 0; i < contracts.Length; i++)
                expectedVehicleCount = math.max(expectedVehicleCount, contracts[i].ExpectedGameplayVehicleCount);
            if (expectedVehicleCount <= 0)
                return true;

            using EntityQuery vehicleQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>());
            return vehicleQuery.CalculateEntityCount() >= expectedVehicleCount;
        }

        internal static int ReconcileAuthoredVehicleOwnership(
            EntityManager em,
            MapVehiclePlacementConfig config)
        {
            if (config == null || config.Placements == null || config.Placements.Count == 0)
                return 0;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                ComponentType.ReadOnly<UnitDetailedVisualReference>(),
                ComponentType.ReadWrite<Faction>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int reconciled = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (em.HasComponent<Prefab>(candidate) || em.HasComponent<Disabled>(candidate))
                    continue;

                Entity visualRoot = em.GetComponentData<UnitDetailedVisualReference>(candidate).Root;
                if (visualRoot == Entity.Null ||
                    !em.Exists(visualRoot) ||
                    !em.HasComponent<OperationMapEntityPresentationIdentity>(visualRoot))
                {
                    continue;
                }

                int placementIndex =
                    em.GetComponentData<OperationMapEntityPresentationIdentity>(visualRoot).PlacementIndex;
                if (placementIndex < 0 || placementIndex >= config.Placements.Count)
                    continue;

                MapVehiclePlacementConfigEntry placement = config.Placements[placementIndex];
                FixedString64Bytes sourceKey = GetVehiclePrefabSourceKey(placement);
                if (placement == null || sourceKey.Length == 0)
                    continue;

                Faction faction = em.GetComponentData<Faction>(candidate);
                if (faction.Id != placement.FactionId)
                {
                    faction.Id = placement.FactionId;
                    em.SetComponentData(candidate, faction);
                }

                UnitSourcePrefabKey source = new() { Value = sourceKey };
                if (em.HasComponent<UnitSourcePrefabKey>(candidate))
                    em.SetComponentData(candidate, source);
                else
                    em.AddComponentData(candidate, source);
                reconciled++;
            }

            return reconciled;
        }

        private void SpawnPlacements(
            Context context,
            EntityManager em,
            Entity progressEntity,
            ref MapVehiclePlacementProgressState progress)
        {
            if (context.Config.Placements == null || context.Config.Placements.Count == 0)
            {
                WarnOnce(ref _warnedMissingConfig, context, "[MapVehiclePlacement] no baked map vehicle placements configured.");
                progress.Queued = 1;
                HideAuthoringVisuals(context, ref progress);
                SaveProgressState(em, progressEntity, progress);
                return;
            }

            context.UnitPrefabContext.EnsureEntityQueries?.Invoke(em);
            if (context.TryGetGridData == null ||
                !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            {
                return;
            }

            using EntityCommandBuffer ecb = new(Allocator.Temp);
            using NativeHashSet<Entity> claimedAuthoredVehicles =
                new(math.max(1, context.Config.Placements.Count), Allocator.Temp);
            int processed = 0;
            for (; progress.NextPlacementIndex < context.Config.Placements.Count && processed < MaxPlacementsPerUpdate; progress.NextPlacementIndex++, processed++)
            {
                MapVehiclePlacementConfigEntry placement = context.Config.Placements[progress.NextPlacementIndex];
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

                SpawnVehicle(
                    context,
                    em,
                    ecb,
                    grid,
                    progress.NextPlacementIndex,
                    placement,
                    prefabEntity,
                    claimedAuthoredVehicles,
                    ref progress);
            }

            ecb.Playback(em);

            if (progress.NextPlacementIndex >= context.Config.Placements.Count)
            {
                progress.Queued = 1;
                MapVehiclePlacementClearanceSystemHelper.RefreshPlacementClearance(context, em, ref progress);
                HideAuthoringVisuals(context, ref progress);
            }
        }

        private void SpawnVehicle(
            Context context,
            EntityManager em,
            EntityCommandBuffer ecb,
            GridConfig grid,
            int placementIndex,
            MapVehiclePlacementConfigEntry placement,
            Entity prefabEntity,
            NativeHashSet<Entity> claimedAuthoredVehicles,
            ref MapVehiclePlacementProgressState progress)
        {
            bool hasPrefab = prefabEntity != Entity.Null && em.Exists(prefabEntity);
            if (!hasPrefab)
                return;

            float3 center = ToFloat3(placement.WorldCenter);
            float3 position = ToFloat3(placement.WorldPosition);
            int2 cell = GridUtils.WorldToCell(grid, center);
            byte faction = placement.FactionId;
            bool adoptedAuthoredVehicle = TryFindAuthoredVehicleEntity(
                em,
                placementIndex,
                placement,
                claimedAuthoredVehicles,
                out Entity instance);
            if (adoptedAuthoredVehicle)
            {
                claimedAuthoredVehicles.Add(instance);
                ConfigureAdoptedVehicle(
                    em,
                    ecb,
                    instance,
                    prefabEntity,
                    faction,
                    cell,
                    position);
            }
            else
            {
                instance = _unitSpawnApplySystem.InstantiateAndConfigureSpawnedUnit(
                    em,
                    ecb,
                    prefabEntity,
                    hasPrefab,
                    faction,
                    cell,
                    position);
            }

            progress.RandomState = math.max(1u, progress.RandomState + 1u);
            var rng = new Unity.Mathematics.Random(progress.RandomState);
            _unitSpawnResetSystem.ResetSpawnedUnitRuntimeState(em, ecb, instance, prefabEntity, hasPrefab, ref rng);
            progress.RandomState = math.max(1u, rng.state);

            ApplyAuthoredTransform(
                em,
                ecb,
                instance,
                prefabEntity,
                hasPrefab,
                placement,
                adoptedAuthoredVehicle);
            FixedString64Bytes sourceKey = ResolveSpawnedVehicleSourceKey(em, prefabEntity, hasPrefab, placement);
            if (sourceKey.Length > 0)
                SetOrAddComponent(em, ecb, instance, prefabEntity, hasPrefab, new UnitSourcePrefabKey { Value = sourceKey });
            SetOrAddComponent(em, ecb, instance, prefabEntity, hasPrefab, new UnitRespawnPrefab { Prefab = prefabEntity });
        }

        internal static bool TryFindAuthoredVehicleEntity(
            EntityManager em,
            int placementIndex,
            MapVehiclePlacementConfigEntry placement,
            NativeHashSet<Entity> claimedEntities,
            out Entity entity)
        {
            entity = Entity.Null;
            if (placement == null)
                return false;

            float3 target = ToFloat3(placement.WorldPosition);
            float maximumDistanceSquared = AuthoredVehicleAdoptionDistance * AuthoredVehicleAdoptionDistance;
            float bestDistanceSquared = maximumDistanceSquared;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            // Entity-presentation candidates already carry the canonical placement identity on
            // their detailed visual root. Resolve that identity before considering the legacy
            // transform heuristic: render-bound pivots and migration transforms are not required
            // to remain within the compatibility placement's one-metre adoption radius.
            if (placementIndex >= 0)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (!IsUnclaimedNeutralAuthoredVehicle(em, candidate, claimedEntities) ||
                        !em.HasComponent<OperationMapAuthoredVehiclePresentation>(candidate) ||
                        !em.HasComponent<UnitDetailedVisualReference>(candidate))
                    {
                        continue;
                    }

                    Entity visualRoot = em.GetComponentData<UnitDetailedVisualReference>(candidate).Root;
                    if (visualRoot == Entity.Null ||
                        !em.Exists(visualRoot) ||
                        !em.HasComponent<OperationMapEntityPresentationIdentity>(visualRoot) ||
                        em.GetComponentData<OperationMapEntityPresentationIdentity>(visualRoot).PlacementIndex != placementIndex)
                    {
                        continue;
                    }

                    entity = candidate;
                    return true;
                }
            }

            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (!IsUnclaimedNeutralAuthoredVehicle(em, candidate, claimedEntities) ||
                    em.HasComponent<OperationMapAuthoredVehiclePresentation>(candidate))
                {
                    continue;
                }

                float3 candidatePosition = em.GetComponentData<LocalTransform>(candidate).Position;
                float distanceSquared = math.distancesq(target, candidatePosition);
                if (distanceSquared > bestDistanceSquared)
                    continue;

                if (entity == Entity.Null ||
                    distanceSquared < bestDistanceSquared ||
                    candidate.Index < entity.Index)
                {
                    entity = candidate;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return entity != Entity.Null;
        }

        private static bool IsUnclaimedNeutralAuthoredVehicle(
            EntityManager em,
            Entity candidate,
            NativeHashSet<Entity> claimedEntities)
        {
            return (!claimedEntities.IsCreated || !claimedEntities.Contains(candidate)) &&
                   !em.HasComponent<Prefab>(candidate) &&
                   !em.HasComponent<Disabled>(candidate) &&
                   !em.HasComponent<StaticGridBlocker>(candidate) &&
                   !em.HasComponent<UnitTransportPassenger>(candidate) &&
                   em.GetComponentData<Faction>(candidate).Id == FactionIdentity.NeutralFactionId &&
                   em.GetComponentData<UnitMovementBehavior>(candidate).UsesVehicleMotion != 0 &&
                   em.GetComponentData<UnitRespawnPrefab>(candidate).Prefab == Entity.Null;
        }

        internal static void ConfigureAdoptedVehicle(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity instance,
            Entity prefab,
            byte faction,
            int2 cell,
            float3 position)
        {
            SetOrAddExistingComponent(em, ecb, instance, new UnitGrid { Cell = cell });
            SetOrAddExistingComponent(em, ecb, instance, new UnitPrevWorldPos { Value = position });
            SetOrAddExistingComponent(
                em,
                ecb,
                instance,
                new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
            SetOrAddExistingComponent(em, ecb, instance, new Faction { Id = faction });
            SetOrAddExistingComponent(em, ecb, instance, new UnitRespawnPrefab { Prefab = prefab });
            SetOrAddExistingComponent(
                em,
                ecb,
                instance,
                new UnitAttackCooldownComponent { CooldownRemaining = 0f });
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
            MapVehiclePlacementConfigEntry placement,
            bool adoptedExistingEntity)
        {
            quaternion rotation = quaternion.EulerXYZ(math.radians(ToFloat3(placement.WorldEulerAngles)));
            float3 scale = ToFloat3(placement.WorldScale);
            if (IsUniformScale(scale, out float uniformScale))
            {
                LocalTransform transform = LocalTransform.FromPositionRotationScale(
                    ToFloat3(placement.WorldPosition),
                    rotation,
                    uniformScale);
                if (adoptedExistingEntity)
                    SetOrAddExistingComponent(em, ecb, instance, transform);
                else
                    SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, transform);
                if ((adoptedExistingEntity && em.HasComponent<PostTransformMatrix>(instance)) ||
                    (!adoptedExistingEntity && hasPrefab && em.HasComponent<PostTransformMatrix>(prefab)))
                {
                    ecb.RemoveComponent<PostTransformMatrix>(instance);
                }
                return;
            }

            LocalTransform nonUniformTransform = LocalTransform.FromPositionRotationScale(
                ToFloat3(placement.WorldPosition),
                rotation,
                1f);
            PostTransformMatrix postTransform = new() { Value = float4x4.Scale(scale) };
            if (adoptedExistingEntity)
            {
                SetOrAddExistingComponent(em, ecb, instance, nonUniformTransform);
                SetOrAddExistingComponent(em, ecb, instance, postTransform);
            }
            else
            {
                SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, nonUniformTransform);
                SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, postTransform);
            }
        }

        private static bool IsUniformScale(float3 scale, out float uniformScale)
        {
            uniformScale = math.max(UniformScaleEpsilon, scale.x);
            return math.abs(scale.x - scale.y) <= UniformScaleEpsilon &&
                   math.abs(scale.x - scale.z) <= UniformScaleEpsilon;
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

        internal static int ClearRuntimeBlockersInFootprint(
            in GridConfig grid,
            ref DynamicBlockerComponent blockerData,
            int2 centerCell,
            int2 footprintSize,
            int paddingCells = 0)
            => MapVehiclePlacementClearanceSystemHelper.ClearRuntimeBlockersInFootprint(
                grid, ref blockerData, centerCell, footprintSize, paddingCells);

        internal static int ClearRuntimeBlockerDepartureCorridor(
            in GridConfig grid,
            ref DynamicBlockerComponent blockerData,
            int2 centerCell,
            int2 footprintSize,
            float headingDegrees,
            int maxDistanceCells)
            => MapVehiclePlacementClearanceSystemHelper.ClearRuntimeBlockerDepartureCorridor(
                grid,
                ref blockerData,
                centerCell,
                footprintSize,
                headingDegrees,
                maxDistanceCells);

        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }

        private static FixedString64Bytes GetVehiclePrefabSourceKey(MapVehiclePlacementConfigEntry placement)
        {
            string sourceKey = BuildingDefinitionPrefabSystemHelper.GetSpawnableLookupKey(placement?.VehicleSourceKey);
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

        private static void SetOrAddExistingComponent<T>(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity instance,
            T component)
            where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(instance))
                ecb.SetComponent(instance, component);
            else
                ecb.AddComponent(instance, component);
        }

        private void HideAuthoringVisuals(Context context, ref MapVehiclePlacementProgressState progress)
        {
            if (progress.AuthoringHidden != 0 || context.Config == null)
            {
                return;
            }

            if (context.Config.HideAuthoringVisualsAfterSpawn &&
                context.AuthoringVehiclesRoot != null &&
                context.AuthoringVehiclesRoot.gameObject.activeSelf)
            {
                context.AuthoringVehiclesRoot.gameObject.SetActive(false);
            }

            progress.AuthoringHidden = 1;
        }

        private static bool TryGetProgressState(
            Context context,
            out EntityManager em,
            out Entity progressEntity,
            out MapVehiclePlacementProgressState progress)
        {
            em = default;
            progressEntity = Entity.Null;
            progress = default;
            if (context.UnitPrefabContext.TryGetEntityManager == null ||
                !context.UnitPrefabContext.TryGetEntityManager(out em))
            {
                return false;
            }

            progressEntity = EnsureProgressEntity(em);
            progress = em.GetComponentData<MapVehiclePlacementProgressState>(progressEntity);
            if (progress.RandomState == 0)
            {
                progress.RandomState = MapVehiclePlacementProgressState.InitialRandomState;
                em.SetComponentData(progressEntity, progress);
            }

            return true;
        }

        private static Entity EnsureProgressEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MapVehiclePlacementProgressState>());
            if (!query.IsEmptyIgnoreFilter)
                return query.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(MapVehiclePlacementProgressState));
            em.SetName(entity, "MapVehiclePlacementProgress");
            em.SetComponentData(entity, new MapVehiclePlacementProgressState
            {
                RandomState = MapVehiclePlacementProgressState.InitialRandomState
            });
            return entity;
        }

        private static void SaveProgressState(EntityManager em, Entity progressEntity, MapVehiclePlacementProgressState progress)
        {
            if (progressEntity != Entity.Null && em.Exists(progressEntity))
                em.SetComponentData(progressEntity, progress);
        }

        private void SyncProgressSnapshot(MapVehiclePlacementProgressState progress)
        {
            _lastClearedBlockerCells = progress.LastClearedBlockerCells;
            _isQueued = progress.Queued != 0;
            _authoringHidden = progress.AuthoringHidden != 0;
            _isComplete = _isQueued && _authoringHidden;
        }

        private static void WarnOnce(ref bool flag, Context context, string message)
        {
            if (flag)
                return;

            flag = true;
            context.LogWarning?.Invoke(message);
        }
    }
}
