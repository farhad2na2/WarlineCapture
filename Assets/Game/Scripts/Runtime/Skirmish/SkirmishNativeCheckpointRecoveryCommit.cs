using System;
using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

namespace Game.Runtime
{
    internal static class SkirmishNativeCheckpointRecoveryCommit
    {
        internal static bool TryCommit(EntityManager em, SkirmishNativeCheckpointRecoveryPreparation.Prepared prepared,
            out string failure)
        {
            failure = "NativeRecoveryCommitUnavailable";
            if (em == default || prepared == null || prepared.Committed ||
                em.WorldUnmanaged.SequenceNumber != prepared.WorldSequence || !em.Exists(prepared.Session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(prepared.Session)) return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(prepared.Session);
            if (state.SpawnComplete != 0 ||
                (state.Phase != SkirmishSessionPhase.Queued && state.Phase != SkirmishSessionPhase.Spawning)) return false;
            var reverse = new Dictionary<Entity, string>();
            var before = new Dictionary<Entity, SkirmishNativeCheckpointParts.Part[]>();
            var owners = new Dictionary<RuntimeBuildingEntity, SkirmishNativeBuildingCheckpointRecord>();
            var committed = new List<RuntimeBuildingEntity>();
            var unsuppressed = new List<Entity>();
            var parents = new Dictionary<Entity, Parent>();
            var hidden = new Dictionary<Entity, bool>();
            bool hadMatch = em.HasComponent<SkirmishMatchState>(prepared.Session);
            var previousMatch = hadMatch ? em.GetComponentData<SkirmishMatchState>(prepared.Session) : default;
            bool applyingParts = false;
            try
            {
                foreach (var identity in prepared.Identities)
                {
                    if (!em.Exists(identity.Value) || !reverse.TryAdd(identity.Value, identity.Key))
                        throw new ArgumentException("Rebuilt native identity changed after preflight.");
                }
                var ids = new HashSet<string>(prepared.Identities.Keys, StringComparer.Ordinal);
                string Id(Entity entity) => reverse.TryGetValue(entity, out string id) ? id : null;
                foreach (var pair in prepared.Parts)
                {
                    if (!em.Exists(pair.Key) || !SkirmishNativeCheckpointParts.Validate(pair.Value, ids))
                        throw new ArgumentException("Rebuilt native components changed after preflight.");
                    before.Add(pair.Key, SkirmishNativeCheckpointParts.Capture(em, pair.Key, Id));
                }
                foreach (var pair in prepared.Production)
                {
                    Entity entity = pair.Key.CombatEntity;
                    if (!em.Exists(entity) || !em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(entity))
                        throw new ArgumentException("Native production owner disappeared.");
                    var owner = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(entity);
                    if (owner.Building != pair.Key || !em.HasComponent<RuntimeBuildingCombatInfo>(entity) ||
                        em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId != pair.Key.Id ||
                        owner.TryRestoreProduction == null || owner.RollbackProduction == null ||
                        pair.Key.ActiveTransport != null || (pair.Key.PendingProductions?.Count ?? 0) != 0 ||
                        (pair.Key.ProducedUnits?.Count ?? 0) != 0)
                        throw new ArgumentException("Native production owner is unavailable or no longer fresh.");
                    owners.Add(pair.Key, owner);
                }
                // All native owners are verified before any paid queue is committed.
                foreach (var pair in prepared.Production)
                {
                    if (!owners[pair.Key].TryRestoreProduction(pair.Value))
                        throw new InvalidOperationException("Native production visual reconstruction failed.");
                    committed.Add(pair.Key);
                }
                Entity Resolve(string id) => prepared.Identities[id];
                if (prepared.Transients != null)
                    foreach (Entity visual in prepared.Transients.Detached)
                        if (em.HasComponent<Parent>(visual))
                        { parents.Add(visual, em.GetComponentData<Parent>(visual)); em.RemoveComponent<Parent>(visual); }
                applyingParts = true;
                foreach (var pair in prepared.Parts)
                    if (pair.Key != prepared.Session)
                        SkirmishNativeCheckpointParts.Apply(em, pair.Key, pair.Value, Resolve);
                using var visualCommands = new EntityCommandBuffer(Allocator.Temp);
                foreach (var passenger in prepared.HiddenPassengers)
                {
                    hidden.Add(passenger.Key, em.HasBuffer<UnitTransportHiddenVisualScale>(passenger.Key));
                    if (!hidden[passenger.Key]) em.AddBuffer<UnitTransportHiddenVisualScale>(passenger.Key);
                    var transform = em.GetComponentData<LocalTransform>(passenger.Key);
                    transform.Scale = passenger.Value;
                    em.SetComponentData(passenger.Key, transform);
                    UnitTransportVisualUtility.SetPassengerHidden(em, passenger.Key, visualCommands);
                }
                visualCommands.Playback(em);
                // Restore the playing clock only after all actors, stores and banks.
                SkirmishNativeCheckpointParts.Apply(em, prepared.Session, prepared.Parts[prepared.Session], Resolve);
                SkirmishExpandedSessionControlService.ProjectMatchPhase(em, prepared.Session);
                foreach (var identity in prepared.Identities)
                    if (em.HasComponent<SkirmishAttemptOwnedComponent>(identity.Value) &&
                        em.HasComponent<CampaignMissionCombatSuppressedTag>(identity.Value))
                    {
                        em.RemoveComponent<CampaignMissionCombatSuppressedTag>(identity.Value);
                        unsuppressed.Add(identity.Value);
                    }
                foreach (var pair in prepared.Production)
                    if (em.HasComponent<BuildingResourceStorageComponent>(pair.Key.CombatEntity))
                    {
                        var storage = em.GetComponentData<BuildingResourceStorageComponent>(pair.Key.CombatEntity);
                        pair.Key.StoredOilBarrels = storage.StoredOilBarrels;
                        pair.Key.StoredFuelBarrels = storage.StoredFuelBarrels;
                    }
                prepared.Committed = true;
                failure = string.Empty;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                foreach (Entity entity in unsuppressed)
                    if (em.Exists(entity) && !em.HasComponent<CampaignMissionCombatSuppressedTag>(entity))
                        em.AddComponent<CampaignMissionCombatSuppressedTag>(entity);
                foreach (var passenger in hidden)
                    if (em.Exists(passenger.Key))
                    {
                        UnitTransportVisualUtility.SetPassengerVisible(em, passenger.Key, true);
                        if (!passenger.Value) em.RemoveComponent<UnitTransportHiddenVisualScale>(passenger.Key);
                    }
                if (applyingParts)
                    foreach (var pair in before)
                        if (em.Exists(pair.Key)) SkirmishNativeCheckpointParts.Apply(em, pair.Key, pair.Value, id => prepared.Identities[id]);
                foreach (var pair in parents)
                    if (em.Exists(pair.Key))
                    { if (em.HasComponent<Parent>(pair.Key)) em.SetComponentData(pair.Key, pair.Value); else em.AddComponentData(pair.Key, pair.Value); }
                if (hadMatch && em.Exists(prepared.Session)) em.SetComponentData(prepared.Session, previousMatch);
                for (int i = committed.Count - 1; i >= 0; i--)
                {
                    var building = committed[i];
                    owners[building].RollbackProduction(prepared.Production[building]);
                }
                failure = "NativeRecoveryCommit:" + exception.Message;
                return false;
            }
        }
    }
}
