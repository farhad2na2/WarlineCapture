using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal sealed class SkirmishNativeCheckpointStructureReconstructionRecord : IComponentData
    {
        internal SkirmishNativeCheckpointSnapshotService.Snapshot Snapshot;
        internal readonly List<Request> Requests = new();
        internal sealed class Request
        {
            internal int Id;
            internal SkirmishNativeCheckpointSnapshotService.Actor Actor;
            internal Entity BoundEntity;
        }
    }

    // Asynchronous native-owner stage. This is not wired to shipping Resume until
    // commit, transient-state coverage and the complete fresh-scene journey pass.
    internal static class SkirmishNativeCheckpointStructureReconstruction
    {
        internal static bool TryBegin(EntityManager em, Entity session,
            SkirmishNativeCheckpointSnapshotService.Snapshot snapshot, SkirmishResolvedSetup setup, out string failure)
        {
            failure = "NativeStructureRecoveryIncompatible";
            if (em == default || !em.Exists(session) || !SkirmishNativeCheckpointCodec.Validate(snapshot, setup) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                em.HasComponent<SkirmishNativeCheckpointStructureReconstructionRecord>(session) ||
                em.HasComponent<BuildingStartingGrantOwner>(session)) return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (state.SessionId.ToString() != snapshot.SessionId || state.SetupHash != setup.SetupHash ||
                state.CatalogId.ToString() != setup.CatalogId || state.DefinitionId.ToString() != setup.DefinitionId ||
                state.ContentVersion != setup.ContentVersion || state.Seed != setup.Seed ||
                state.SizeId != setup.SizeId || state.DifficultyId != setup.DifficultyId || state.SpawnComplete != 0 ||
                (state.Phase != Game.Skirmish.Contracts.SkirmishSessionPhase.Queued &&
                 state.Phase != Game.Skirmish.Contracts.SkirmishSessionPhase.Spawning)) return false;
            using var ownedQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
            });
            using var owners = ownedQuery.ToComponentDataArray<SkirmishAttemptOwnedComponent>(Allocator.Temp);
            foreach (var owner in owners) if (owner.SessionId.Equals(state.SessionId)) return false;
            if (!string.IsNullOrEmpty(snapshot.MapMetadataHash))
            {
                using var metadata = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                if (metadata.CalculateEntityCount() != 1 ||
                    metadata.GetSingleton<OperationMapMetadataComponent>().MetadataHash.ToString() != snapshot.MapMetadataHash)
                { failure = "NativePhysicalMapIncompatible"; return false; }
            }
            using var sourceQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
            });
            using var sourceEntities = sourceQuery.ToEntityArray(Allocator.Temp);
            var sourceAliases = new HashSet<string>(StringComparer.Ordinal);
            foreach (Entity entity in sourceEntities)
            {
                var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                string key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (!sourceAliases.Add(SkirmishNativeCheckpointSnapshotService.SourceMapIdentity(em, entity, info, key)))
                { failure = "NativePhysicalMapAmbiguous"; return false; }
            }
            if (sourceAliases.Count != snapshot.MapReferences.Length)
            { failure = "NativePhysicalMapCensusIncomplete"; return false; }
            foreach (var map in snapshot.MapReferences)
                if (!sourceAliases.Contains(map.Id)) { failure = "NativePhysicalMapFootprintMissing"; return false; }
            using var boundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingConfiguredSpawnableReadModel), typeof(BuildingRuntimeSpawnRequest));
            if (boundaries.CalculateEntityCount() != 1) { failure = "NativeStructureOwnerNotReady"; return false; }
            Entity boundary = boundaries.GetSingletonEntity();
            var definitions = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary, true);
            // The boundary exists before its first catalog publication. This is
            // the same readiness boundary used by normal starting buildings.
            if (definitions.Length == 0) { failure = "NativeStructureOwnerNotReady"; return false; }
            var staged = new SkirmishNativeCheckpointStructureReconstructionRecord { Snapshot = snapshot };
            foreach (var actor in snapshot.Actors)
            {
                if (actor.IsStructure == 0)
                {
                    if (!SkirmishScenarioSpawnSystem.CanResolveCheckpointUnit(em, actor))
                    { failure = "NativeUnitPrefabMissing:" + actor.PrefabKey; return false; }
                    continue;
                }
                if (!SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts, out RuntimeBuildingCombatInfo info) ||
                    info.OwnerFactionId < 1 || info.OwnerFactionId > 2 || info.FootprintCells.x < 1 || info.FootprintCells.y < 1)
                    return false;
                string key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(actor.PrefabKey);
                bool registered = false;
                bool keyFound = false;
                int2 configuredFootprint = default;
                foreach (var definition in definitions)
                    if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(definition.BuildingId.ToString()) == key)
                    {
                        keyFound = true;
                        configuredFootprint = definition.FootprintCells;
                        if (definition.FootprintCells.Equals(info.FootprintCells)) registered = true;
                    }
                if (!registered && keyFound)
                {
                    failure = "NativeStructureFootprintNotReady:" + actor.PrefabKey + ":saved=" + info.FootprintCells +
                        ":configured=" + configuredFootprint;
                    return false;
                }
                if (!registered) { failure = "NativeStructurePrefabOrFootprintMissing:" + actor.PrefabKey; return false; }
                staged.Requests.Add(new SkirmishNativeCheckpointStructureReconstructionRecord.Request { Actor = actor });
            }
            // Preflight every structure before submitting even one reconstruction.
            int next = 0;
            var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            foreach (var request in requests) next = Math.Max(next, request.RequestId);
            if ((long)next + staged.Requests.Count > int.MaxValue) return false;
            em.AddComponent<BuildingStartingGrantOwner>(session);
            em.AddComponentObject(session, staged);
            requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            for (int i = 0; i < staged.Requests.Count; i++)
            {
                var item = staged.Requests[i];
                SkirmishNativeCheckpointParts.TryReadComponent(item.Actor.Parts, out RuntimeBuildingCombatInfo info);
                item.Id = ++next;
                requests.Add(new BuildingRuntimeSpawnRequest
                {
                    RequestId = item.Id, RequestKind = BuildingRuntimeSpawnRequest.KindBuilding,
                    BuildingId = new FixedString128Bytes(item.Actor.PrefabKey), FactionId = info.OwnerFactionId,
                    HasOwnerFaction = 1, AuthoredStartingGrant = 1, PlanEntity = session, EntryIndex = i,
                    PreferredOrigin = info.OriginCell, RequirePreferredOrigin = 1, Status = BuildingRuntimeSpawnRequest.Pending
                });
            }
            failure = string.Empty;
            return true;
        }

        internal static bool TryBindCompleted(EntityManager em, Entity session, out bool complete, out string failure)
        {
            complete = false;
            failure = "NativeStructureRecoveryMissing";
            if (em == default || !em.Exists(session) ||
                !em.HasComponent<SkirmishNativeCheckpointStructureReconstructionRecord>(session)) return false;
            var record = em.GetComponentObject<SkirmishNativeCheckpointStructureReconstructionRecord>(session);
            using var boundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingRuntimeSpawnRequest));
            if (boundaries.CalculateEntityCount() != 1) return false;
            var results = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaries.GetSingletonEntity(), true).ToNativeArray(Allocator.Temp);
            try
            {
                complete = true;
                foreach (var item in record.Requests)
                {
                    if (item.BoundEntity != Entity.Null && em.Exists(item.BoundEntity)) continue;
                    BuildingRuntimeSpawnRequest result = default;
                    bool found = false;
                    foreach (var candidate in results)
                        if (candidate.RequestId == item.Id) { found = true; result = candidate; break; }
                    if (!found || result.PlanEntity != session || result.BuildingId.ToString() != item.Actor.PrefabKey ||
                        result.Status == BuildingRuntimeSpawnRequest.Failed)
                    { failure = "NativeStructureRecoveryFailed:" + item.Actor.Id; complete = false; return false; }
                    if (result.Status == BuildingRuntimeSpawnRequest.Pending) { complete = false; continue; }
                    Entity building = FindRuntimeBuilding(em, result.BuildingRuntimeId);
                    if (building == Entity.Null) { complete = false; continue; }
                    if (!em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(building))
                    { failure = "NativeStructureRecoveryOwnerMissing:" + item.Actor.Id; complete = false; return false; }
                    var nativeOwner = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(building).Building;
                    if (nativeOwner == null || nativeOwner.CombatEntity != building || nativeOwner.Id != result.BuildingRuntimeId)
                    { failure = "NativeStructureRecoveryOwnerMismatch:" + item.Actor.Id; complete = false; return false; }
                    SkirmishNativeCheckpointParts.TryReadComponent(item.Actor.Parts, out RuntimeBuildingCombatInfo savedInfo);
                    var actualInfo = em.GetComponentData<RuntimeBuildingCombatInfo>(building);
                    if (!actualInfo.OriginCell.Equals(savedInfo.OriginCell) || !actualInfo.FootprintCells.Equals(savedInfo.FootprintCells) ||
                        actualInfo.OwnerFactionId != savedInfo.OwnerFactionId)
                    { failure = "NativeStructureRecoveryPlacementMismatch:" + item.Actor.Id; complete = false; return false; }
                    RuntimeActorBirthContext.VerifyBuildingAdoption(em, building, result);
                    SkirmishNativeCheckpointParts.TryReadComponent(item.Actor.Parts, out SkirmishAttemptOwnedComponent identity);
                    Set(em, building, identity);
                    Set(em, building, new SkirmishSharedActorTag());
                    Set(em, building, new CampaignMissionCombatSuppressedTag());
                    Set(em, building, new UnitSourcePrefabKey { Value = new FixedString64Bytes(item.Actor.PrefabKey) });
                    Set(em, building, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
                    item.BoundEntity = building;
                }
                if (complete) em.RemoveComponent<BuildingStartingGrantOwner>(session);
                failure = string.Empty;
                return true;
            }
            finally { results.Dispose(); }
        }

        internal static void Cancel(EntityManager em, Entity session)
        {
            if (em == default || !em.Exists(session)) return;
            SkirmishStartingBuildingsService.Cancel(em, session);
            em.RemoveComponent<SkirmishNativeCheckpointStructureReconstructionRecord>(session);
        }

        private static void Set<T>(EntityManager em, Entity entity, T value) where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity)) em.SetComponentData(entity, value);
            else em.AddComponentData(entity, value);
        }

        private static Entity FindRuntimeBuilding(EntityManager em, int id)
        {
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>() },
                None = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>() }
            });
            using var entities = query.ToEntityArray(Allocator.Temp);
            Entity found = Entity.Null;
            foreach (Entity entity in entities)
                if (em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId == id)
                {
                    if (found != Entity.Null) return Entity.Null;
                    found = entity;
                }
            return found;
        }
    }
}
