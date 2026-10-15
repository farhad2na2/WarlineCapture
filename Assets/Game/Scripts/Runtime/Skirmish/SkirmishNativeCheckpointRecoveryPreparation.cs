using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal static class SkirmishNativeCheckpointRecoveryPreparation
    {
        internal sealed class Prepared
        {
            internal ulong WorldSequence;
            internal Entity Session;
            internal bool Committed;
            internal SkirmishNativeObjectiveCheckpoint.Prepared Objective;
            internal SkirmishNativeCheckpointTransientState.Bindings Transients;
            internal readonly Dictionary<Entity, float> HiddenPassengers = new();
            internal readonly Dictionary<Entity, SkirmishNativeCheckpointParts.Part[]> Parts = new();
            internal readonly Dictionary<RuntimeBuildingEntity, SkirmishNativeProductionCheckpoint.Prepared> Production = new();
            internal readonly Dictionary<string, Entity> Identities = new(StringComparer.Ordinal);
        }

        // Read-only final preflight after the native owner has rebuilt the roster.
        // No health, resources, queues, path buffers or visuals change on failure.
        internal static bool TryPrepare(EntityManager em, Entity session,
            SkirmishNativeCheckpointSnapshotService.Snapshot saved, SkirmishResolvedSetup setup,
            Func<string, GameObject> prefab, out Prepared prepared, out string failure,
            SkirmishNativeCheckpointTransientState.Bindings transients = null)
        {
            prepared = null;
            failure = "NativeCheckpointIncompatible";
            if (em == default || !em.Exists(session) || prefab == null ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !SkirmishNativeCheckpointCodec.Validate(saved, setup)) return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (state.IsLegacy != 0 || state.SessionId.ToString() != saved.SessionId || state.SetupHash != setup.SetupHash ||
                state.CatalogId.ToString() != setup.CatalogId || state.DefinitionId.ToString() != setup.DefinitionId ||
                state.ContentVersion != setup.ContentVersion) return false;
            try
            {
                if (!string.IsNullOrEmpty(saved.MapMetadataHash))
                {
                    using var metadata = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                    if (metadata.CalculateEntityCount() != 1 ||
                        metadata.GetSingleton<OperationMapMetadataComponent>().MetadataHash.ToString() != saved.MapMetadataHash)
                        throw new ArgumentException("Loaded physical map metadata differs from checkpoint.");
                }
                var value = new Prepared { WorldSequence = em.WorldUnmanaged.SequenceNumber, Session = session, Transients = transients };
                value.Identities.Add("$session", session);
                var nativeBuildings = new Dictionary<int, int>();
                var newBuildingIds = new HashSet<int>();
                using var query = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
                });
                using var actors = query.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in actors)
                {
                    var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                    if (!owner.SessionId.Equals(state.SessionId)) continue;
                    if (!em.HasComponent<SkirmishSharedActorTag>(entity) ||
                        !value.Identities.TryAdd(owner.StableObjectId.ToString(), entity))
                        throw new ArgumentException("Native actor identity is missing or duplicated.");
                }
                if (value.Identities.Count != saved.Actors.Length + 1)
                    throw new ArgumentException("Rebuilt roster does not match checkpoint.");
                if (em.HasComponent<SkirmishSharedMaterialsInitialized>(session) != (saved.Banks.Length == 2))
                    throw new ArgumentException("Fresh native faction economy differs from checkpoint.");
                foreach (var bank in saved.Banks)
                {
                    using var bankQuery = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent));
                    using var candidates = bankQuery.ToEntityArray(Allocator.Temp);
                    Entity found = Entity.Null;
                    foreach (Entity candidate in candidates)
                    {
                        if (em.GetComponentData<FactionEconomy>(candidate).FactionId != bank.Faction) continue;
                        if (found != Entity.Null) throw new ArgumentException("Fresh native faction bank is ambiguous.");
                        found = candidate;
                    }
                    if (found == Entity.Null || !value.Identities.TryAdd("bank/" + bank.Faction, found))
                        throw new ArgumentException("Fresh native faction bank is missing.");
                }
                foreach (var actor in saved.Actors)
                {
                    if (!value.Identities.TryGetValue(actor.Id, out Entity entity) ||
                        !em.HasComponent<UnitSourcePrefabKey>(entity) ||
                        em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() != actor.PrefabKey ||
                        em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).IsStructure != actor.IsStructure)
                        throw new ArgumentException("Rebuilt native prefab does not match checkpoint.");
                    if (actor.IsStructure == 0) continue;
                    if (!em.HasComponent<RuntimeBuildingCombatInfo>(entity) ||
                        !em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(entity))
                        throw new ArgumentException("Rebuilt native structure has no owner.");
                    int newId = em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId;
                    var building = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(entity).Building;
                    if (newId <= 0 || !newBuildingIds.Add(newId) || building == null || building.Id != newId ||
                        building.CombatEntity != entity || building.ActiveTransport != null ||
                        (building.PendingProductions?.Count ?? 0) != 0 || (building.ProducedUnits?.Count ?? 0) != 0)
                        throw new ArgumentException("Rebuilt production owner is not fresh.");
                    nativeBuildings.Add(actor.OldRuntimeBuildingId, newId);
                }

                var sourceBuildings = new Dictionary<string, Entity>(StringComparer.Ordinal);
                using var mapQuery = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                    None = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
                });
                using var mapEntities = mapQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in mapEntities)
                {
                    var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                    string key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                    string alias = SkirmishNativeCheckpointSnapshotService.SourceMapIdentity(em, entity, info, key);
                    if (!sourceBuildings.TryAdd(alias, entity)) throw new ArgumentException("Source map footprint is ambiguous.");
                }
                // Removed scenery requires explicit tombstones, never silently
                // resurrect it by applying a partial source census.
                if (sourceBuildings.Count != saved.MapReferences.Length)
                    throw new ArgumentException("Source map census differs; destroyed-source recovery is incomplete.");
                foreach (var map in saved.MapReferences)
                    if (!sourceBuildings.TryGetValue(map.Id, out Entity entity) || !value.Identities.TryAdd(map.Id, entity))
                        throw new ArgumentException("Source map footprint was not reconstructed.");

                if ((saved.MissileVisuals.Length != 0 || saved.MissileStates.Length != 0 || saved.DropVisuals.Length != 0 || saved.Support.Length != 0) && transients == null)
                    throw new ArgumentException("Native transient hierarchy has not been reconstructed.");
                if (transients != null)
                    foreach (var alias in transients.Identities)
                        if (!value.Identities.TryAdd(alias.Key, alias.Value)) throw new ArgumentException("Duplicate native transient identity.");

                Entity Resolve(string id) => value.Identities.TryGetValue(id, out Entity entity) ? entity : Entity.Null;
                float worldOffset = (float)(em.WorldUnmanaged.Time.ElapsedTime - saved.WorldTime);
                float unityOffset = Time.time - saved.UnityTime;
                int frameOffset = unchecked(Time.frameCount - saved.Frame);
                SkirmishNativeCheckpointParts.Part[] Parts(SkirmishNativeCheckpointParts.Part[] parts, int? buildingId = null)
                {
                    var rebased = SkirmishNativeCheckpointParts.RebaseClocks(parts, worldOffset, unityOffset, frameOffset);
                    return SkirmishNativeCheckpointParts.RemapBuildingIds(rebased, nativeBuildings, buildingId);
                }
                foreach (var actor in saved.Actors)
                {
                    Entity entity = Resolve(actor.Id);
                    value.Parts.Add(entity, Parts(actor.Parts, actor.IsStructure == 0 ? null : nativeBuildings[actor.OldRuntimeBuildingId]));
                    if (actor.HiddenPassengerScale > 0)
                    {
                        if (!em.HasComponent<Unity.Transforms.LocalTransform>(entity) ||
                            (em.HasBuffer<UnitTransportHiddenVisualScale>(entity) && em.GetBuffer<UnitTransportHiddenVisualScale>(entity, true).Length != 0))
                            throw new ArgumentException("Rebuilt passenger visuals are not fresh.");
                        value.HiddenPassengers.Add(entity, actor.HiddenPassengerScale);
                    }
                    if (!actor.HasProduction) continue;
                    var building = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(entity).Building;
                    if (!SkirmishNativeProductionCheckpoint.TryPrepare(actor.Production, Time.time, prefab, Resolve, out var production) ||
                        (building.ProducedUnitSlots?.Length ?? 0) != production.Slots.Length)
                        throw new ArgumentException("Native paid production cannot be reconstructed.");
                    value.Production.Add(building, production);
                }
                foreach (var map in saved.MapReferences)
                {
                    Entity entity = Resolve(map.Id);
                    value.Parts.Add(entity, Parts(map.Parts, em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId));
                }
                foreach (var support in saved.Support) value.Parts.Add(Resolve(support.Id), support.Parts);
                foreach (var drop in saved.DropVisuals) value.Parts.Add(Resolve(drop.Id), Parts(drop.Parts));
                foreach (var visual in saved.MissileVisuals) value.Parts.Add(Resolve(visual.Id), Parts(visual.Parts));
                foreach (var transient in saved.MissileStates) value.Parts.Add(Resolve(transient.Id), Parts(transient.Parts));
                value.Parts.Add(session, Parts(saved.SessionParts));
                foreach (var bank in saved.Banks)
                    value.Parts.Add(Resolve("bank/" + bank.Faction), Parts(bank.Parts));
                if (setup.ObjectiveKind != Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault &&
                    !SkirmishNativeObjectiveCheckpoint.TryPrepare(em,session,saved,setup,out value.Objective,out failure)) return false;
                prepared = value;
                failure = string.Empty;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException)
            {
                failure = "NativeRecoveryPreflight:" + exception.Message;
                return false;
            }
        }
    }
}
