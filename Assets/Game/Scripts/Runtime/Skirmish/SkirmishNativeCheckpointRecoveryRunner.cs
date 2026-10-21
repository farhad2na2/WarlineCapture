using System;
using System.Collections.Generic;
using Unity.Collections;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class SkirmishNativeCheckpointRecoveryRecord : IComponentData
    {
        internal SkirmishNativeCheckpointSnapshotService.Snapshot Snapshot;
        internal Func<string, GameObject> Prefab;
        internal double Deadline;
        internal bool StructuresRequested;
        internal bool UnitsCreated;
        internal bool Complete;
        internal bool ResumeAfterCommit;
        internal SkirmishNativeCheckpointTransientState.Bindings Transients;
        internal string Failure;
        internal string WaitingFor;
    }

    // Internal native integration lane. Player Save/Resume remains gated until
    // transient inventory and a fresh-scene normal-input journey are complete.
    internal static class SkirmishNativeCheckpointRecoveryRunner
    {
        internal static bool TryQueue(EntityManager em, Entity session,
            SkirmishNativeCheckpointSnapshotService.Snapshot snapshot, SkirmishResolvedSetup setup,
            Func<string, GameObject> prefab = null)
        {
            if (em == default || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(session) ||
                !SkirmishNativeCheckpointCodec.Validate(snapshot, setup) ||
                !SkirmishNativeCheckpointParts.TryReadComponent(snapshot.SessionParts, out SkirmishExpandedSessionComponent saved) ||
                saved.SpawnComplete == 0 || saved.Phase != SkirmishSessionPhase.Playing) return false;
            var fresh = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (fresh.IsLegacy != 0 || fresh.SpawnComplete != 0 || fresh.SessionId != saved.SessionId ||
                fresh.SetupHash != saved.SetupHash || fresh.CatalogId != saved.CatalogId ||
                fresh.DefinitionId != saved.DefinitionId || fresh.ContentVersion != saved.ContentVersion ||
                fresh.Seed != saved.Seed || fresh.SizeId != saved.SizeId || fresh.DifficultyId != saved.DifficultyId ||
                (fresh.Phase != SkirmishSessionPhase.Queued && fresh.Phase != SkirmishSessionPhase.Spawning)) return false;
            // Validate independently compiled objective binding before reconstruction
            // creates native actors, buildings, banks or transient visuals.
            if (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault &&
                !SkirmishNativeObjectiveCheckpoint.TryPrepare(em,session,snapshot,setup,out _,out _)) return false;
            em.AddComponentObject(session, new SkirmishNativeCheckpointRecoveryRecord
            {
                Snapshot = snapshot, Prefab = prefab, Deadline = Time.realtimeSinceStartupAsDouble + 240
            });
            return true;
        }

        internal static void Step(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            var record = em.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(session);
            if (record.Complete || record.Failure != null) return;
            try
            {
                if (Time.realtimeSinceStartupAsDouble > record.Deadline)
                { Fail(em, session, record, "NativeRecoveryTimedOut" + (record.WaitingFor == null ? "" : ":" + record.WaitingFor)); return; }
                if (!record.StructuresRequested)
                {
                    if (!SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, record.Snapshot, setup, out string reason))
                    {
                        record.WaitingFor = reason;
                        // Streaming can publish the surface/registry before all
                        // source structures exist. Wait for the exact saved census;
                        // missing/destroyed source recovery still fails on timeout.
                        if (reason != "NativeStructureOwnerNotReady" && reason != "NativePhysicalMapCensusIncomplete" &&
                            reason != "NativePhysicalMapFootprintMissing" &&
                            !reason.StartsWith("NativeStructureFootprintNotReady:", StringComparison.Ordinal)) Fail(em, session, record, reason);
                        return;
                    }
                    record.StructuresRequested = true;
                    record.WaitingFor = null;
                }
                if (!SkirmishNativeCheckpointStructureReconstruction.TryBindCompleted(em, session, out bool complete, out string failure))
                { Fail(em, session, record, failure); return; }
                if (!complete) return;
                if (!record.UnitsCreated)
                {
                    using var reconstructionGrids = em.CreateEntityQuery(typeof(GridConfig));
                    Entity reconstructionGrid = reconstructionGrids.CalculateEntityCount() == 1 ? reconstructionGrids.GetSingletonEntity() : Entity.Null;
                    foreach (var actor in record.Snapshot.Actors)
                    {
                        if (actor.IsStructure != 0) continue;
                        var birth = RuntimeActorBirthContext.CaptureReconstruction(em, reconstructionGrid, session, new Unity.Collections.FixedString64Bytes(actor.Id));
                        if (!SkirmishScenarioSpawnSystem.TryInstantiateCheckpointUnit(em, actor, out _, setup, birth))
                        { Fail(em, session, record, "NativeRecoveryUnitFailed:" + actor.Id); return; }
                    }
                    record.UnitsCreated = true;
                    // Restore the authoritative banks, never reseed forces or the
                    // physical starting supply grant through TrySpawnLedgers.
                    if (record.Snapshot.Banks.Length == 2) SkirmishMaterialsService.Initialize(em, session, setup);
                }
                var structures = em.GetComponentObject<SkirmishNativeCheckpointStructureReconstructionRecord>(session);
                foreach (var request in structures.Requests)
                {
                    var owner = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(request.BoundEntity);
                    if (owner.TryRestoreProduction == null || owner.RollbackProduction == null) return;
                }
                if (record.Prefab == null)
                {
                    var registry = SkirmishNativeCheckpointPrefabRegistry.Find(em, session);
                    if (registry == null) { record.WaitingFor = "NativePrefabRegistryNotReady"; return; }
                    if (!registry.Validate(out _)) { Fail(em, session, record, "NativePrefabRegistryAmbiguousOrEmpty"); return; }
                    record.Prefab = registry.Resolve;
                    record.WaitingFor = null;
                }
                if (record.Transients == null)
                {
                    var identities = new Dictionary<string, Entity>(StringComparer.Ordinal) { ["$session"] = session };
                    using var actorQuery = em.CreateEntityQuery(new EntityQueryDesc {
                        All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                        Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState });
                    using var actors = actorQuery.ToEntityArray(Allocator.Temp);
                    foreach (Entity actor in actors)
                    {
                        var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                        if (owner.SessionId.ToString() == record.Snapshot.SessionId) identities.Add(owner.StableObjectId.ToString(), actor);
                    }
                    record.Transients = SkirmishNativeCheckpointTransientState.Rebuild(em, record.Snapshot.SessionId,
                        record.Snapshot.MissileVisuals, record.Snapshot.MissileStates, identities, record.Snapshot.DropVisuals);
                    SkirmishNativeCheckpointSupportState.Rebuild(em, session, record.Snapshot.Support, record.Transients);
                }
                if (!SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(em, session, record.Snapshot, setup,
                    record.Prefab, out var prepared, out failure, record.Transients) ||
                    !SkirmishNativeCheckpointRecoveryCommit.TryCommit(em, prepared, out failure))
                { Fail(em, session, record, failure); return; }
                // The reconstruction owner has verified every functional building.
                // Normal startup publishes this marker after its own grant binding;
                // recovery must publish it too, without replaying those grants.
                if (!em.HasComponent<SkirmishSharedBuildingsReady>(session))
                    em.AddComponent<SkirmishSharedBuildingsReady>(session);
                if (record.ResumeAfterCommit)
                {
                    SkirmishExpandedSessionControlService.TryResume(em, session);
                    Debug.Log("[SkirmishPlayerResume] result=Passed session=" + em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
                }
                record.Complete = true;
                record.Transients = null;
                record.Prefab = null;
                record.Snapshot = null;
                em.RemoveComponent<SkirmishNativeCheckpointStructureReconstructionRecord>(session);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            { Fail(em, session, record, "NativeRecovery:" + exception.Message); }
        }

        private static void Fail(EntityManager em, Entity session, SkirmishNativeCheckpointRecoveryRecord record, string failure)
        {
            record.Failure = failure;
            Debug.LogWarning("[SkirmishNativeRecovery] result=Failed reason=" + failure);
            record.Prefab = null;
            SkirmishNativeCheckpointTransientState.DestroyCreated(em, record.Transients);
            record.Transients = null;
            SkirmishNativeCheckpointStructureReconstruction.Cancel(em, session);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            SkirmishScenarioSpawnSystem.DestroyAttemptOwned(em, state.SessionId);
            state.Phase = SkirmishSessionPhase.Failed;
            state.FailureCode = SkirmishReasonCode.IncompatibleSave;
            em.SetComponentData(session, state);
        }
    }
}
