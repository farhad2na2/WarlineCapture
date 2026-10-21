using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    public static class SkirmishExpandedLaunchProjection
    {
        public static bool TryQueue(
            EntityManager em,
            SkirmishLaunchPayload payload,
            SkirmishResolvedSetup setup,
            UnitPrefabRegistryAuthoringConfig registry = null,
            SkirmishScenarioDefinitionConfig explicitDefinition = null)
        {
            if (em == default || payload == null || setup == null || payload.IsLegacy)
                return false;
            if (!SkirmishMissionDispatch.PayloadMatches(payload, setup)) return false;
            SkirmishMissionDispatch.TryGetLegacyIndex(payload.CatalogId,out int scenarioIndex);
            bool generic = SkirmishMissionDispatch.RequiresSourceBoundDefinition(payload.CatalogId, payload.ContentVersion);
            SkirmishScenarioDefinitionConfig definition = explicitDefinition;
            if (definition == null)
            {
                var authored = SkirmishExpansionCatalogConfig.Load();
                if (authored != null) authored.TryGetDefinition(payload.CatalogId, payload.ContentVersion, out definition);
            }
            if (generic)
            {
                if (!SkirmishMissionDispatch.TryGetGenericById(SkirmishBattleCatalogConfig.Load(), payload.CatalogId, out var entry) ||
                    !SkirmishMissionDispatch.MatchesDefinition(entry, definition) || !setup.MeasuredLayoutBound ||
                    setup.SetupHash != SkirmishSetupCompiler.ComputeHash(setup, setup.Seed) ||
                    string.IsNullOrEmpty(setup.LayoutBindingFingerprint) || definition.CampaignMap == null ||
                    ContentAccessRuntime.Evaluate(definition.DefinitionId) != Game.Missions.Contracts.ContentAccessState.Allowed ||
                    !definition.CampaignMap.TryValidateMetadata(out _) || !definition.CampaignMap.TryValidateLocalContentReferences(out _)) return false;
                scenarioIndex = entry.PlayableScenarioIndex;
            }
            if (definition != null && (definition.CatalogId != setup.CatalogId || definition.DefinitionId != setup.DefinitionId ||
                definition.ContentVersion != setup.ContentVersion || definition.OperationMapId != setup.OperationMapId || definition.MapLayoutId != setup.LayoutId)) return false;
            if (!SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(definition,setup,out var missionLogical,out _)) return false;
            var launchDefinition = definition == null ? null : new SkirmishLaunchDefinitionRecord(definition, setup, generic);
            if (!string.IsNullOrWhiteSpace(payload.SessionId) && (System.Text.Encoding.UTF8.GetByteCount(payload.SessionId) > 61 || payload.SessionId.IndexOf('\0') >= 0)) return false;
            if (string.IsNullOrWhiteSpace(payload.SessionId))
                payload.SessionId = Guid.NewGuid().ToString("N");

            using var existing = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (!existing.IsEmptyIgnoreFilter)
                return false;

            Entity entity;
            if (SkirmishLaunchProjection.TryGet(em, out Entity session, out _))
                entity = session;
            else
            {
                entity = em.CreateEntity(typeof(SkirmishMatchState));
                em.SetName(entity, "SkirmishExpandedSession");
                em.AddBuffer<SkirmishActionRequest>(entity);
                em.AddBuffer<SkirmishTrackedUnit>(entity);
            }

            em.SetComponentData(entity, new SkirmishMatchState
            {
                SessionId = new FixedString64Bytes(payload.SessionId),
                Seed = payload.Seed,
                ScenarioIndex = scenarioIndex,
                Phase = SkirmishPhase.Queued
            });

            var sessionComponent = new SkirmishExpandedSessionComponent
            {
                SessionId = new FixedString64Bytes(payload.SessionId),
                CatalogId = new FixedString64Bytes(payload.CatalogId),
                DefinitionId = new FixedString64Bytes(payload.DefinitionId),
                ContentVersion = payload.ContentVersion,
                SetupHash = setup.SetupHash,
                Seed = payload.Seed,
                SizeId = payload.SizeId,
                DifficultyId = payload.DifficultyId,
                ObjectiveKind = setup.ObjectiveKind,
                Phase = SkirmishSessionPhase.Queued,
                IsLegacy = 0,
                IsCustom = (byte)(payload.IsCustom ? 1 : 0)
            };
            if (em.HasComponent<SkirmishExpandedSessionComponent>(entity))
                em.SetComponentData(entity, sessionComponent);
            else
                em.AddComponentData(entity, sessionComponent);

            var strategyOwners = em.HasBuffer<ExternalFactionStrategy>(entity)
                ? em.GetBuffer<ExternalFactionStrategy>(entity) : em.AddBuffer<ExternalFactionStrategy>(entity);
            strategyOwners.Clear();
            strategyOwners.Add(new ExternalFactionStrategy { FactionId = 1 });
            strategyOwners.Add(new ExternalFactionStrategy { FactionId = 2 });

            var setupComponent = new SkirmishResolvedSetupComponent
            {
                SetupHash = setup.SetupHash,
                ContentVersion = setup.ContentVersion,
                DefinitionId = new FixedString64Bytes(setup.DefinitionId),
                DeadlineSeconds = setup.DeadlineSeconds,
                PlayerFaction = setup.PlayerFaction,
                EnemyFaction = setup.EnemyFaction
            };
            if (em.HasComponent<SkirmishResolvedSetupComponent>(entity))
                em.SetComponentData(entity, setupComponent);
            else
                em.AddComponentData(entity, setupComponent);

            if (em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup = setup;
            else
                em.AddComponentObject(entity, new SkirmishResolvedSetupRecord { Setup = setup });

            if (em.HasComponent<SkirmishLaunchDefinitionRecord>(entity))
                em.RemoveComponent<SkirmishLaunchDefinitionRecord>(entity);
            if (launchDefinition != null)
                em.AddComponentObject(entity, launchDefinition);
            if (Game.Configs.SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup) &&
                !SkirmishMissionLogicalBindingProvider.TryAttachCompiled(em,entity,definition,setup,missionLogical,out _))
            {
                sessionComponent.Phase=SkirmishSessionPhase.Failed;sessionComponent.FailureCode=SkirmishReasonCode.MissingReference;
                em.SetComponentData(entity,sessionComponent);return false;
            }
            // A visible expanded Launch/Continue action starts the match once
            // native loading completes. It must not depend on an Editor harness.
            if ((payload.CatalogId == "S005" || generic) && !em.HasComponent<SkirmishExpandedReplayStartPending>(entity))
                em.AddComponent<SkirmishExpandedReplayStartPending>(entity);
            if (registry != null)
                SkirmishVisualSpawnService.BindSceneRegistry(em, entity, registry);
            return true;
        }

    }
}
