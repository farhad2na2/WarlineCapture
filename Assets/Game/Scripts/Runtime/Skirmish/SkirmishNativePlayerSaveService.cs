using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class SkirmishNativeResumeRequest : IComponentData
    {
        internal SkirmishNativeCheckpointSnapshotService.Snapshot Snapshot;
        internal SkirmishResolvedSetup Setup;
        internal Entity Session;
        internal double Deadline;
    }
    internal sealed class SkirmishNativeSaveFeedback : IComponentData { internal bool Failed; }
    internal struct SkirmishNativeResumeFailure : IComponentData { }
    public static class SkirmishNativePlayerSaveService
    {
        public static bool TryConsumeResumeFailure(EntityManager em)
        {
            using var failures = em.CreateEntityQuery(typeof(SkirmishNativeResumeFailure));
            if (failures.IsEmptyIgnoreFilter) return false;
            em.DestroyEntity(failures);
            return true;
        }
        internal static void ReportResumeFailure(EntityManager em)
        {
            using var failures = em.CreateEntityQuery(typeof(SkirmishNativeResumeFailure));
            if (failures.IsEmptyIgnoreFilter) em.CreateEntity(typeof(SkirmishNativeResumeFailure));
        }
        private static string SavePath
        {
            get
            {
#if UNITY_EDITOR
                string validation = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
                if (!string.IsNullOrWhiteSpace(validation)) return Path.Combine(validation, "skirmish-s005-native.json");
#endif
                return Path.Combine(Application.persistentDataPath, "skirmish-s005-native.json");
            }
        }
        private static bool SupportsGenericSave(string catalogId)
        {
            var battle=SkirmishBattleCatalogConfig.Load();var authored=SkirmishExpansionCatalogConfig.Load();
            return SkirmishMissionDispatch.TryGetGenericById(battle,catalogId,out var entry) && authored!=null &&
                authored.TryGetDefinition(catalogId,out var definition) && SkirmishMissionDispatch.MatchesDefinition(entry,definition) &&
                definition.CampaignMap!=null && definition.ObjectiveConfig!=null && definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.BaseAssault;
        }
        private static string SavePathForCatalog(string catalogId)=>catalogId=="S005"?SavePath:
            SupportsGenericSave(catalogId)?Path.Combine(Path.GetDirectoryName(SavePath),"skirmish-"+catalogId.ToLowerInvariant()+"-native.json"):null;

        public static bool HasSaved(string catalogId) => TryRead(catalogId, out _, out _) || HasRecognizedPolicylessSave(catalogId);

        private static bool HasRecognizedPolicylessSave(string catalogId)
        {
            if (catalogId != "S005" || ContentAccessRuntime.Evaluate("skirmish.s005") != Game.Missions.Contracts.ContentAccessState.Allowed ||
                !SkirmishNativeCheckpointCodec.TryReadDescriptor(SavePath, out var saved) || saved.CatalogId != catalogId ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts, out SkirmishExpandedSessionComponent state) ||
                state.Phase != SkirmishSessionPhase.Playing || state.SpawnComplete == 0) return false;
            var catalog = SkirmishExpansionCatalogConfig.Load();
            return catalog != null && catalog.TryGetDefinition(catalogId, out var definition) && definition != null &&
                SkirmishSetupMatrixTable.TryLoadPackaged(out List<SkirmishSetupMatrixRow> matrix, out _) &&
                SkirmishSetupCompiler.TryCompile(definition, state.DifficultyId, state.SizeId, state.Seed,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() }, matrix,
                    out var setup, out _) && SkirmishNativeCheckpointCodec.ValidatePolicylessCompatibility(saved, setup);
        }
        public static bool HasSaveFailed(EntityManager em, Entity session) => em.Exists(session) &&
            em.HasComponent<SkirmishNativeSaveFeedback>(session) && em.GetComponentObject<SkirmishNativeSaveFeedback>(session).Failed;
        public static bool CanSave(EntityManager em, Entity session) => em.Exists(session) &&
            em.HasComponent<SkirmishExpandedSessionComponent>(session) &&
            em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy == 0 &&
            (em.GetComponentData<SkirmishExpandedSessionComponent>(session).CatalogId.ToString() == "S005" ||
             (SupportsGenericSave(em.GetComponentData<SkirmishExpandedSessionComponent>(session).CatalogId.ToString()) &&
              em.HasComponent<SkirmishLaunchDefinitionRecord>(session) && em.HasComponent<SkirmishResolvedSetupRecord>(session) &&
              em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Matches(em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session)))) &&
            em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase == SkirmishSessionPhase.Playing &&
            em.HasComponent<SkirmishResolvedSetupRecord>(session);

        internal static bool TrySaveAndExit(EntityManager em, Entity session)
        {
            if (!CanSave(em, session)) return false;
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1 || gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive != 0) return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            bool captured = SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, out var snapshot, out string failure);
            bool saved = captured && SkirmishNativeCheckpointCodec.TryWriteAtomic(SavePathForCatalog(setup.CatalogId), snapshot, setup);
            if (!em.HasComponent<SkirmishNativeSaveFeedback>(session)) em.AddComponentObject(session, new SkirmishNativeSaveFeedback());
            em.GetComponentObject<SkirmishNativeSaveFeedback>(session).Failed = !saved;
            if (!saved) { Debug.LogWarning("[SkirmishPlayerSave] result=Failed reason=" + (captured ? "AtomicWriteFailed" : failure)); return false; }
            using var pending = em.CreateEntityQuery(typeof(SkirmishReturnRequest));
            if (pending.IsEmptyIgnoreFilter)
            {
                Entity request = em.CreateEntity(typeof(SkirmishReturnRequest));
                var match = em.GetComponentData<SkirmishMatchState>(session);
                em.SetComponentData(request, new SkirmishReturnRequest { Action = SkirmishAction.AdjustSetup, Seed = match.Seed, ScenarioIndex = match.ScenarioIndex });
            }
            Debug.Log("[SkirmishPlayerSave] result=Passed session=" + snapshot.SessionId + " schema=" + snapshot.Version);
            return true;
        }

        internal static bool TryRead(string catalogId, out SkirmishNativeCheckpointSnapshotService.Snapshot snapshot, out SkirmishResolvedSetup setup)
        {
            snapshot = null; setup = null;
            string path=SavePathForCatalog(catalogId);
            if (path==null || ContentAccessRuntime.Evaluate("skirmish."+catalogId.ToLowerInvariant()) != Game.Missions.Contracts.ContentAccessState.Allowed ||
                !SkirmishNativeCheckpointCodec.TryReadDescriptor(path, out var saved) || saved.CatalogId != catalogId ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts, out SkirmishExpandedSessionComponent state) ||
                state.Phase != SkirmishSessionPhase.Playing || state.SpawnComplete == 0) return false;
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog == null || !catalog.TryGetDefinition(catalogId, out var definition) || definition == null ||
                !SkirmishSetupMatrixTable.TryLoadPackaged(out List<SkirmishSetupMatrixRow> matrix, out _) ||
                !SkirmishSetupCompiler.TryCompile(definition, state.DifficultyId, state.SizeId, state.Seed,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() }, matrix,
                    out setup, out _) || !SkirmishNativeCheckpointCodec.Validate(saved, setup)) { setup = null; return false; }
            snapshot = saved; return true;
        }

        public static bool TryRequestResume(EntityManager em, string catalogId)
        {
            if (em == default || !TryRead(catalogId, out var snapshot, out var setup)) return false;
            var battleCatalog = SkirmishBattleCatalogConfig.Load();
            if (battleCatalog == null || !battleCatalog.TryGet(catalogId, out var entry) || !entry.IsPlayable) return false;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var requests = em.CreateEntityQuery(typeof(SkirmishNativeResumeRequest));
            if (!sessions.IsEmptyIgnoreFilter || !requests.IsEmptyIgnoreFilter) return false;
            TryConsumeResumeFailure(em);
            Entity request = em.CreateEntity();
            em.AddComponentObject(request, new SkirmishNativeResumeRequest { Snapshot = snapshot, Setup = setup,
                Deadline = Time.realtimeSinceStartupAsDouble + 15 });
            return true;
        }

        internal static void CompleteAttempt(string sessionId, string catalogId = "S005")
        {
            if (SavePathForCatalog(catalogId) is string path && SkirmishNativeCheckpointCodec.TryReadDescriptor(path, out var saved) && saved.CatalogId == catalogId && saved.SessionId == sessionId && !TryDiscard(saved.CatalogId))
                Debug.LogWarning("[SkirmishPlayerSave] result=Failed reason=CompletedSaveCleanupFailed");
        }

        public static bool TryDiscard(string catalogId)
        {
            string path=SavePathForCatalog(catalogId); if(path==null)return false;
            try { if (File.Exists(path)) File.Delete(path); return true; }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) { return false; }
        }
    }
}
