using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Capture staging for the native reconstruction owner. This is deliberately
    // separate from the fixture checkpoint API until fresh native recovery passes.
    internal static class SkirmishNativeCheckpointSnapshotService
    {
        internal const int SchemaVersion = 7;
        [Serializable] internal sealed class Snapshot
        {
            public int Version = SchemaVersion;
            public SkirmishNativeObjectiveCheckpoint.Payload Objective;
            public string SessionId;
            public string CatalogId;
            public uint SetupHash;
            public string ContentHash;
            public string OperationMapId;
            public string LayoutId;
            public string MapMetadataHash;
            public double WorldTime;
            public float UnityTime;
            public int Frame;
            public SkirmishNativeCheckpointTransientState.Visual[] MissileVisuals = Array.Empty<SkirmishNativeCheckpointTransientState.Visual>();
            public SkirmishNativeCheckpointTransientState.Transient[] MissileStates = Array.Empty<SkirmishNativeCheckpointTransientState.Transient>();
            public SkirmishNativeCheckpointTransientState.DropVisual[] DropVisuals = Array.Empty<SkirmishNativeCheckpointTransientState.DropVisual>();
            public SkirmishNativeCheckpointSupportState.Record[] Support = Array.Empty<SkirmishNativeCheckpointSupportState.Record>();
            public Actor[] Actors;
            public MapReference[] MapReferences;
            public Bank[] Banks;
            public SkirmishNativeCheckpointParts.Part[] SessionParts;
        }

        [Serializable] internal sealed class Bank
        {
            public byte Faction;
            public SkirmishNativeCheckpointParts.Part[] Parts;
        }

        [Serializable] internal sealed class Actor
        {
            public string Id;
            public string PrefabKey;
            public byte IsStructure;
            public int OldRuntimeBuildingId;
            public float HiddenPassengerScale;
            public SkirmishNativeCheckpointParts.Part[] Parts;
            public bool HasProduction;
            public SkirmishNativeProductionCheckpoint.Snapshot Production;
        }

        [Serializable] internal sealed class MapReference
        {
            public string Id;
            public string PrefabKey;
            public byte Faction;
            public int OriginX;
            public int OriginZ;
            public int OldRuntimeBuildingId;
            public SkirmishNativeCheckpointParts.Part[] Parts;
        }

        internal static bool TryCapture(EntityManager em, Entity session, out Snapshot snapshot, out string failure)
        {
            snapshot = null;
            failure = "NativePrefabRegistryUnavailable";
            var registry = SkirmishNativeCheckpointPrefabRegistry.Find(em, session);
            return registry != null && registry.Validate(out _) && TryCapture(em, session, registry.Key, out snapshot, out failure);
        }

        internal static bool TryCapture(EntityManager em, Entity session, Func<GameObject, string> prefabKey,
            out Snapshot snapshot, out string failure)
        {
            snapshot = null;
            failure = "NativeSessionMissing";
            if (em == default || session == Entity.Null || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) || prefabKey == null)
                return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (state.IsLegacy != 0) return false;

            var identities = new Dictionary<Entity, string> { [session] = "$session" };
            var usedIds = new HashSet<string>(StringComparer.Ordinal) { "$session" };
            var actors = new List<Actor>();
            var actorEntities = new List<Entity>();
            var references = new List<MapReference>();
            var referenceEntities = new List<Entity>();
            var banks = new List<Bank>();
            var bankEntities = new List<Entity>();
            if (em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
            {
                using var bankQuery = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent));
                using var candidates = bankQuery.ToEntityArray(Allocator.Temp);
                for (byte faction = 1; faction <= 2; faction++)
                {
                    Entity bank = Entity.Null;
                    foreach (Entity candidate in candidates)
                    {
                        if (em.GetComponentData<FactionEconomy>(candidate).FactionId != faction) continue;
                        if (bank != Entity.Null) { failure = "NativeFactionBankAmbiguous"; return false; }
                        bank = candidate;
                    }
                    if (bank == Entity.Null) { failure = "NativeFactionBankMissing"; return false; }
                    identities.Add(bank, "bank/" + faction);
                    usedIds.Add("bank/" + faction);
                    banks.Add(new Bank { Faction = faction });
                    bankEntities.Add(bank);
                }
            }
            using var ownedQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
            });
            using var owned = ownedQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in owned)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owner.SessionId.Equals(state.SessionId)) continue;
                string id = owner.StableObjectId.ToString();
                if (id.Length == 0 || !usedIds.Add(id) || !em.HasComponent<SkirmishSharedActorTag>(entity) ||
                    !em.HasComponent<UnitSourcePrefabKey>(entity))
                {
                    failure = "NativeActorIdentityInvalid:" + id;
                    return false;
                }
                identities.Add(entity, id);
                actorEntities.Add(entity);
                actors.Add(new Actor
                {
                    Id = id, IsStructure = owner.IsStructure,
                    PrefabKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString(),
                    OldRuntimeBuildingId = em.HasComponent<RuntimeBuildingCombatInfo>(entity)
                        ? em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId : 0
                });
            }
            if (actors.Count == 0) return false;

            // Existing physical source buildings retain their geometry. Record
            // aliases for targets and cargo stores instead of serializing handles.
            using var mapQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
            });
            using var mapEntities = mapQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in mapEntities)
            {
                if (identities.ContainsKey(entity)) continue;
                var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                string key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                string id = SourceMapIdentity(em, entity, info, key);
                if (!usedIds.Add(id))
                {
                    failure = "MapReferenceAmbiguous:" + id;
                    return false;
                }
                identities.Add(entity, id);
                referenceEntities.Add(entity);
                references.Add(new MapReference
                {
                    Id = id, PrefabKey = key, Faction = info.OwnerFactionId,
                    OriginX = info.OriginCell.x, OriginZ = info.OriginCell.y, OldRuntimeBuildingId = info.RuntimeBuildingId
                });
            }

            string Identity(Entity entity) => identities.TryGetValue(entity, out string id) ? id : null;
            try
            {
                var support = SkirmishNativeCheckpointSupportState.Capture(em, session, identities, usedIds);
                SkirmishNativeCheckpointTransientState.Capture(em, actorEntities, identities, usedIds,
                    out var missileVisuals, out var missileStates, out var dropVisuals);
                for (int i = 0; i < banks.Count; i++)
                    banks[i].Parts = SkirmishNativeCheckpointParts.Capture(em, bankEntities[i], Identity);
                for (int i = 0; i < references.Count; i++)
                {
                    // Source geometry is reused, but combat damage and physical
                    // storage must survive a fresh session too.
                    references[i].Parts = SkirmishNativeCheckpointParts.Capture(em, referenceEntities[i], Identity);
                }
                for (int i = 0; i < actors.Count; i++)
                {
                    Entity entity = actorEntities[i];
                    if (em.HasComponent<UnitTransportPassenger>(entity))
                    {
                        if (!em.HasBuffer<UnitTransportHiddenVisualScale>(entity)) throw new InvalidOperationException("NativePassengerVisualStateMissing");
                        bool found = false;
                        foreach (var scale in em.GetBuffer<UnitTransportHiddenVisualScale>(entity, true))
                            if (scale.Visual == entity)
                            {
                                if (found || !float.IsFinite(scale.PreviousScale) || scale.PreviousScale <= 0) throw new InvalidOperationException("NativePassengerVisualScaleInvalid");
                                actors[i].HiddenPassengerScale = scale.PreviousScale; found = true;
                            }
                        if (!found) throw new InvalidOperationException("NativePassengerVisualRootMissing");
                    }
                    actors[i].Parts = SkirmishNativeCheckpointParts.Capture(em, entity, Identity);
                    if (actors[i].IsStructure != 0)
                    {
                        if (!em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(entity) ||
                            em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(entity).Building is not { } building ||
                            building.CombatEntity != entity || building.Id != actors[i].OldRuntimeBuildingId)
                        {
                            failure = "NativeBuildingOwnerMissing:" + actors[i].Id;
                            return false;
                        }
                        actors[i].HasProduction = true;
                        actors[i].Production = SkirmishNativeProductionCheckpoint.Capture(
                            building, Time.time, prefabKey, Identity);
                    }
                }
                snapshot = new Snapshot
                {
                    SessionId = state.SessionId.ToString(), CatalogId = state.CatalogId.ToString(), SetupHash = state.SetupHash,
                    WorldTime = em.WorldUnmanaged.Time.ElapsedTime, UnityTime = Time.time, Frame = Time.frameCount,
                    Actors = actors.ToArray(), MapReferences = references.ToArray(),
                    Banks = banks.ToArray(), Support = support, MissileVisuals = missileVisuals, MissileStates = missileStates, DropVisuals = dropVisuals,
                    SessionParts = SkirmishNativeCheckpointParts.Capture(em, session, Identity)
                };
                if (em.HasComponent<SkirmishResolvedSetupRecord>(session))
                {
                    var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                    snapshot.ContentHash = setup?.ContentHash;
                    snapshot.OperationMapId = setup?.OperationMapId;
                    snapshot.LayoutId = setup?.LayoutId;
                }
                using var metadata = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                if (metadata.CalculateEntityCount() == 1)
                    snapshot.MapMetadataHash = metadata.GetSingleton<OperationMapMetadataComponent>().MetadataHash.ToString();
                failure = string.Empty;
                if (state.ObjectiveKind != Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault)
                {
                    if (!SkirmishNativeObjectiveCheckpoint.TryCapture(em, session, out var objectivePayload, out failure))
                    { snapshot = null; return false; }
                    snapshot.Objective = objectivePayload;
                    snapshot.Version = SkirmishNativeObjectiveCheckpoint.NativeVersion;
                    var actualSetup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                    if (!SkirmishNativeObjectiveCheckpoint.Validate(objectivePayload,snapshot,actualSetup,out failure))
                    { snapshot = null; return false; }
                }
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                snapshot = null;
                failure = "NativeCaptureIncomplete:" + exception.Message;
                return false;
            }
        }

        internal static string SourceMapIdentity(EntityManager em, Entity entity,
            RuntimeBuildingCombatInfo info, string prefabKey)
        {
            var source = em.HasComponent<OperationMapBuildingIdentity>(entity)
                ? em.GetComponentData<OperationMapBuildingIdentity>(entity) : default;
            return SourceMapIdentity(source, info, prefabKey);
        }

        internal static string SourceMapIdentity(OperationMapBuildingIdentity source,
            RuntimeBuildingCombatInfo info, string prefabKey)
        {
            // Adjacent authored wall segments can share a rounded grid cell.
            // Use the persistent source identity, never a runtime entity/index.
            if (!source.StableId.IsEmpty && !source.OperationMapId.IsEmpty)
                return "map/source/" + source.OperationMapId + "/" + source.StableId;
            return "map/" + info.OwnerFactionId + "/" + info.OriginCell.x + "/" + info.OriginCell.y + "/" + prefabKey;
        }
    }
}
