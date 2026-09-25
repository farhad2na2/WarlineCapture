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
            UnitPrefabRegistryAuthoringConfig registry = null)
        {
            if (em == default || payload == null || setup == null || payload.IsLegacy)
                return false;
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
                ScenarioIndex = MapHint(setup.OperationMapId),
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

            if (registry != null)
                SkirmishVisualSpawnService.BindSceneRegistry(em, entity, registry);
            return true;
        }

        private static int MapHint(string operationMapId)
        {
            if (operationMapId == "opmap.skirmish.city_crossroads")
                return SkirmishPresetConfig.CityCrossroadsScenarioIndex;
            return SkirmishPresetConfig.DesertBaseScenarioIndex;
        }
    }
}
