using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class ResourceExchangeAIStartupProjectionSystemHelper
    {
        internal static ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult Initialize(
            EntityManager entityManager,
            ResourceExchangeStartupProjectionSystemHelper startupProjection,
            ResourceExchangeRecipeConfigSet config)
        {
            if (config == null)
            {
                return new ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult(
                    false,
                    0,
                    0,
                    ResourceExchangeReason.InvalidRecipe);
            }

            if (!startupProjection.TryResolveScenarioTag(out FixedString64Bytes scenarioTag))
            {
                return new ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult(
                    false,
                    0,
                    0,
                    ResourceExchangeReason.InvalidScenarioGate);
            }

            ResourceExchangeReason configReason =
                ResourceExchangeRecipeConfigValidator.ValidateRecipeAndScenarioGateSet(
                    config.Recipes,
                    config.ScenarioGates);
            if (configReason != ResourceExchangeReason.None)
            {
                DisableAllNonPlayerBoundaries(entityManager, startupProjection, scenarioTag, configReason);
                return new ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult(
                    false,
                    0,
                    0,
                    configReason);
            }
            if (!ResourceExchangeStartupProjectionSystemHelper.TryResolveGate(
                    config,
                    scenarioTag,
                    out ResourceExchangeScenarioGateConfigEntry gate))
            {
                DisableAllNonPlayerBoundaries(
                    entityManager,
                    startupProjection,
                    scenarioTag,
                    ResourceExchangeReason.InvalidScenarioGate);
                return new ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult(
                    false,
                    0,
                    0,
                    ResourceExchangeReason.InvalidScenarioGate);
            }

            using EntityQuery controlQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FactionControlConfigTag>(),
                ComponentType.ReadOnly<FactionControlEntry>());
            if (controlQuery.CalculateEntityCount() != 1)
            {
                DisableAllNonPlayerBoundaries(
                    entityManager,
                    startupProjection,
                    scenarioTag,
                    gate.DisabledReason);
                return new ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult(
                    gate.ExchangeEnabled && gate.AllowAiExchange,
                    0,
                    0,
                    ResourceExchangeReason.ExchangeUnavailable);
            }

            DynamicBuffer<FactionControlEntry> controls =
                controlQuery.GetSingletonBuffer<FactionControlEntry>(true);
            var eligibleFactionIds = new List<byte>(controls.Length);
            var seenFactionIds = new bool[byte.MaxValue + 1];
            bool hasDuplicateFactionControl = false;
            for (int i = 0; i < controls.Length; i++)
            {
                FactionControlEntry control = controls[i];
                if (control.IsPlayerFaction != 0 || control.FactionId == 0)
                    continue;

                if (seenFactionIds[control.FactionId])
                {
                    hasDuplicateFactionControl = true;
                    continue;
                }
                seenFactionIds[control.FactionId] = true;
                eligibleFactionIds.Add(control.FactionId);
            }

            bool scenarioAllowsAIExchange = gate.ExchangeEnabled && gate.AllowAiExchange;
            if (!scenarioAllowsAIExchange || hasDuplicateFactionControl)
            {
                for (int i = 0; i < eligibleFactionIds.Count; i++)
                {
                    DisableExistingBoundary(
                        entityManager,
                        startupProjection,
                        eligibleFactionIds[i],
                        scenarioTag,
                        gate.DisabledReason);
                }
                return new ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult(
                    scenarioAllowsAIExchange,
                    eligibleFactionIds.Count,
                    0,
                    hasDuplicateFactionControl
                        ? ResourceExchangeReason.ExchangeUnavailable
                        : ResourceExchangeReason.None);
            }

            int projectedFactionCount = 0;
            for (int i = 0; i < eligibleFactionIds.Count; i++)
            {
                ResourceExchangeStartupProjectionSystemHelper.Result result =
                    startupProjection.Initialize(config, eligibleFactionIds[i]);
                if (result.Projected)
                    projectedFactionCount++;
            }

            ResourceExchangeReason reason = projectedFactionCount == eligibleFactionIds.Count
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.ExchangeUnavailable;
            return new ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult(
                true,
                eligibleFactionIds.Count,
                projectedFactionCount,
                reason);
        }

        private static void DisableAllNonPlayerBoundaries(
            EntityManager entityManager,
            ResourceExchangeStartupProjectionSystemHelper startupProjection,
            in FixedString64Bytes scenarioTag,
            ResourceExchangeReason disabledReason)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FactionEconomy>(),
                ComponentType.ReadOnly<ResourceExchangeEnabledComponent>());
            ComponentTypeHandle<FactionEconomy> economyType =
                entityManager.GetComponentTypeHandle<FactionEconomy>(true);
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<FactionEconomy> economies = chunks[chunkIndex].GetNativeArray(ref economyType);
                for (int i = 0; i < economies.Length; i++)
                {
                    byte factionId = economies[i].FactionId;
                    if (factionId == 0 || factionId == FactionIdentity.PlayerFactionId)
                        continue;

                    DisableExistingBoundary(
                        entityManager,
                        startupProjection,
                        factionId,
                        scenarioTag,
                        disabledReason);
                }
            }
        }

        private static void DisableExistingBoundary(
            EntityManager entityManager,
            ResourceExchangeStartupProjectionSystemHelper startupProjection,
            byte factionId,
            in FixedString64Bytes scenarioTag,
            ResourceExchangeReason disabledReason)
        {
            if (!startupProjection.TryResolveCanonicalFactionEntity(factionId, out Entity factionEntity) ||
                !entityManager.HasComponent<ResourceExchangeEnabledComponent>(factionEntity))
            {
                return;
            }

            ClearBufferIfPresent<ResourceExchangeRecipeComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeRequestComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeQueueComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeResultComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeEconomyEventComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangePhysicalReservationComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeDeltaFlyoutComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeToastComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeAriaAnnouncementComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeVisualRequestComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangeVfxMarkerComponent>(entityManager, factionEntity);
            ClearBufferIfPresent<ResourceExchangePresentationAnchorComponent>(entityManager, factionEntity);

            ResourceExchangeEnabledComponent enabled =
                entityManager.GetComponentData<ResourceExchangeEnabledComponent>(factionEntity);
            enabled.Enabled = 0;
            enabled.FactionId = factionId;
            enabled.AllowRush = 0;
            enabled.AllowWorldPresentation = 0;
            enabled.AllowAiExchange = 0;
            enabled.MaxQueueItems = 0;
            enabled.ScenarioTag = scenarioTag;
            enabled.Version++;
            entityManager.SetComponentData(factionEntity, enabled);
            if (entityManager.HasComponent<ResourceExchangeRequestQueueComponent>(factionEntity))
                entityManager.SetComponentData(factionEntity, new ResourceExchangeRequestQueueComponent());
            if (!entityManager.HasComponent<ResourceExchangeSummaryComponent>(factionEntity))
                return;

            ResourceExchangeSummaryComponent summary =
                entityManager.GetComponentData<ResourceExchangeSummaryComponent>(factionEntity);
            summary.FactionId = factionId;
            summary.Enabled = 0;
            summary.AllowRush = 0;
            summary.AllowWorldPresentation = 0;
            summary.AllowAiExchange = 0;
            summary.QueueCount = 0;
            summary.ActiveCount = 0;
            summary.CompletedCount = 0;
            summary.MaxQueueItems = 0;
            summary.LastReason = disabledReason == ResourceExchangeReason.None
                ? ResourceExchangeReason.ExchangeUnavailable
                : disabledReason;
            summary.Version++;
            entityManager.SetComponentData(factionEntity, summary);
        }

        private static void ClearBufferIfPresent<T>(EntityManager entityManager, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasBuffer<T>(entity))
                entityManager.GetBuffer<T>(entity).Clear();
        }
    }
}
