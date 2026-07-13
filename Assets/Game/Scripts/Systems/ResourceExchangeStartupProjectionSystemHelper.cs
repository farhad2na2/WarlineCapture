using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed class ResourceExchangeStartupProjectionSystemHelper
    {
        private readonly EntityManager entityManager;

        public readonly struct Result
        {
            public readonly bool Projected;
            public readonly Entity BoundaryEntity;
            public readonly int RecipeCount;
            public readonly ResourceExchangeReason Reason;

            public Result(
                bool projected,
                Entity boundaryEntity,
                int recipeCount,
                ResourceExchangeReason reason)
            {
                Projected = projected;
                BoundaryEntity = boundaryEntity;
                RecipeCount = recipeCount;
                Reason = reason;
            }
        }

        public readonly struct AIProjectionResult
        {
            public readonly bool ScenarioAllowsAIExchange;
            public readonly int EligibleFactionCount;
            public readonly int ProjectedFactionCount;
            public readonly ResourceExchangeReason Reason;

            public AIProjectionResult(
                bool scenarioAllowsAIExchange,
                int eligibleFactionCount,
                int projectedFactionCount,
                ResourceExchangeReason reason)
            {
                ScenarioAllowsAIExchange = scenarioAllowsAIExchange;
                EligibleFactionCount = eligibleFactionCount;
                ProjectedFactionCount = projectedFactionCount;
                Reason = reason;
            }
        }

        public ResourceExchangeStartupProjectionSystemHelper(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public Result Initialize(
            ResourceExchangeRecipeConfigSet config,
            byte factionId = FactionIdentity.PlayerFactionId)
        {
            if (config == null)
                return new Result(false, Entity.Null, 0, ResourceExchangeReason.InvalidRecipe);

            ResourceExchangeReason configReason =
                ResourceExchangeRecipeConfigValidator.ValidateRecipeAndScenarioGateSet(
                    config.Recipes,
                    config.ScenarioGates);
            if (configReason != ResourceExchangeReason.None)
                return new Result(false, Entity.Null, 0, configReason);

            if (!TryResolveScenarioTag(out FixedString64Bytes scenarioTag))
                return new Result(false, Entity.Null, 0, ResourceExchangeReason.InvalidScenarioGate);
            if (!TryResolveGate(config, scenarioTag, out ResourceExchangeScenarioGateConfigEntry gate))
                return new Result(false, Entity.Null, 0, ResourceExchangeReason.InvalidScenarioGate);
            if (!TryResolveCanonicalFactionEntity(factionId, out Entity factionEntity))
                return new Result(false, Entity.Null, 0, ResourceExchangeReason.ExchangeUnavailable);

            EnsureComponent<ResourceExchangeEnabledComponent>(factionEntity);
            EnsureComponent<ResourceExchangeWalletComponent>(factionEntity);
            EnsureComponent<ResourceExchangeRequestQueueComponent>(factionEntity);
            EnsureComponent<ResourceExchangeSummaryComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeRecipeComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeRequestComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeQueueComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeResultComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeEconomyEventComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangePhysicalReservationComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeDeltaFlyoutComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeToastComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeAriaAnnouncementComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeVisualRequestComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangeVfxMarkerComponent>(factionEntity);
            EnsureBufferComponent<ResourceExchangePresentationAnchorComponent>(factionEntity);

            DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
                entityManager.GetBuffer<ResourceExchangeRecipeComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeRequestComponent> requests =
                entityManager.GetBuffer<ResourceExchangeRequestComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeQueueComponent> queue =
                entityManager.GetBuffer<ResourceExchangeQueueComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeResultComponent> results =
                entityManager.GetBuffer<ResourceExchangeResultComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
                entityManager.GetBuffer<ResourceExchangeEconomyEventComponent>(factionEntity);
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations =
                entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts =
                entityManager.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeToastComponent> toasts =
                entityManager.GetBuffer<ResourceExchangeToastComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> announcements =
                entityManager.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests =
                entityManager.GetBuffer<ResourceExchangeVisualRequestComponent>(factionEntity);
            DynamicBuffer<ResourceExchangeVfxMarkerComponent> vfxMarkers =
                entityManager.GetBuffer<ResourceExchangeVfxMarkerComponent>(factionEntity);
            DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors =
                entityManager.GetBuffer<ResourceExchangePresentationAnchorComponent>(factionEntity);

            recipes.Clear();
            requests.Clear();
            queue.Clear();
            results.Clear();
            economyEvents.Clear();
            reservations.Clear();
            deltaFlyouts.Clear();
            toasts.Clear();
            announcements.Clear();
            visualRequests.Clear();
            vfxMarkers.Clear();
            anchors.Clear();

            int recipeCount = ProjectRecipes(config, scenarioTag, recipes);
            byte enabled = gate.ExchangeEnabled && recipeCount > 0 ? (byte)1 : (byte)0;
            ResourceExchangeEnabledComponent previousEnabled =
                entityManager.GetComponentData<ResourceExchangeEnabledComponent>(factionEntity);
            entityManager.SetComponentData(factionEntity, new ResourceExchangeEnabledComponent
            {
                Enabled = enabled,
                FactionId = factionId,
                AllowRush = gate.AllowRush ? (byte)1 : (byte)0,
                AllowWorldPresentation = gate.AllowWorldPresentation ? (byte)1 : (byte)0,
                AllowAiExchange = gate.AllowAiExchange ? (byte)1 : (byte)0,
                MaxQueueItems = enabled != 0 ? gate.MaxQueueItems : 0,
                ScenarioTag = scenarioTag,
                Version = previousEnabled.Version + 1u
            });
            entityManager.SetComponentData(factionEntity, new ResourceExchangeWalletComponent
            {
                FactionId = factionId,
                RushTickets = 0,
                Version = 1
            });
            entityManager.SetComponentData(factionEntity, new ResourceExchangeRequestQueueComponent());
            entityManager.SetComponentData(factionEntity, new ResourceExchangeSummaryComponent
            {
                FactionId = factionId,
                Enabled = enabled,
                AllowRush = gate.AllowRush ? (byte)1 : (byte)0,
                AllowWorldPresentation = gate.AllowWorldPresentation ? (byte)1 : (byte)0,
                AllowAiExchange = gate.AllowAiExchange ? (byte)1 : (byte)0,
                MaxQueueItems = enabled != 0 ? gate.MaxQueueItems : 0,
                LastReason = enabled != 0 ? ResourceExchangeReason.None : gate.DisabledReason,
                Version = 1
            });

            return new Result(true, factionEntity, recipeCount, ResourceExchangeReason.None);
        }

        public AIProjectionResult InitializeEligibleAIFactions(ResourceExchangeRecipeConfigSet config)
        {
            if (config == null)
                return new AIProjectionResult(false, 0, 0, ResourceExchangeReason.InvalidRecipe);

            if (!TryResolveScenarioTag(out FixedString64Bytes scenarioTag))
                return new AIProjectionResult(false, 0, 0, ResourceExchangeReason.InvalidScenarioGate);

            ResourceExchangeReason configReason =
                ResourceExchangeRecipeConfigValidator.ValidateRecipeAndScenarioGateSet(
                    config.Recipes,
                    config.ScenarioGates);
            if (configReason != ResourceExchangeReason.None)
            {
                DisableAllNonPlayerBoundaries(scenarioTag, configReason);
                return new AIProjectionResult(false, 0, 0, configReason);
            }
            if (!TryResolveGate(config, scenarioTag, out ResourceExchangeScenarioGateConfigEntry gate))
            {
                DisableAllNonPlayerBoundaries(scenarioTag, ResourceExchangeReason.InvalidScenarioGate);
                return new AIProjectionResult(false, 0, 0, ResourceExchangeReason.InvalidScenarioGate);
            }

            using EntityQuery controlQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FactionControlConfigTag>(),
                ComponentType.ReadOnly<FactionControlEntry>());
            if (controlQuery.CalculateEntityCount() != 1)
            {
                DisableAllNonPlayerBoundaries(scenarioTag, gate.DisabledReason);
                return new AIProjectionResult(
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
                    DisableExistingBoundary(eligibleFactionIds[i], scenarioTag, gate.DisabledReason);
                return new AIProjectionResult(
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
                Result result = Initialize(config, eligibleFactionIds[i]);
                if (result.Projected)
                    projectedFactionCount++;
            }

            ResourceExchangeReason reason = projectedFactionCount == eligibleFactionIds.Count
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.ExchangeUnavailable;
            return new AIProjectionResult(
                true,
                eligibleFactionIds.Count,
                projectedFactionCount,
                reason);
        }

        private void DisableAllNonPlayerBoundaries(
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
                    if (factionId != 0 && factionId != FactionIdentity.PlayerFactionId)
                        DisableExistingBoundary(factionId, scenarioTag, disabledReason);
                }
            }
        }

        private void DisableExistingBoundary(
            byte factionId,
            in FixedString64Bytes scenarioTag,
            ResourceExchangeReason disabledReason)
        {
            if (!TryResolveCanonicalFactionEntity(factionId, out Entity factionEntity) ||
                !entityManager.HasComponent<ResourceExchangeEnabledComponent>(factionEntity))
            {
                return;
            }

            ClearBufferIfPresent<ResourceExchangeRecipeComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeRequestComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeQueueComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeResultComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeEconomyEventComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangePhysicalReservationComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeDeltaFlyoutComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeToastComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeAriaAnnouncementComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeVisualRequestComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangeVfxMarkerComponent>(factionEntity);
            ClearBufferIfPresent<ResourceExchangePresentationAnchorComponent>(factionEntity);

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
            if (entityManager.HasComponent<ResourceExchangeSummaryComponent>(factionEntity))
            {
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
        }

        private bool TryResolveScenarioTag(out FixedString64Bytes scenarioTag)
        {
            scenarioTag = default;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CustomGameStartupStateComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            scenarioTag = query.GetSingleton<CustomGameStartupStateComponent>().GameModeId;
            return scenarioTag.Length > 0;
        }

        private bool TryResolveCanonicalFactionEntity(byte factionId, out Entity entity)
        {
            entity = Entity.Null;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FactionEconomy>(),
                ComponentType.ReadOnly<FactionTacticalMaterialsComponent>());
            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            ComponentTypeHandle<FactionEconomy> economyType =
                entityManager.GetComponentTypeHandle<FactionEconomy>(true);
            ComponentTypeHandle<FactionTacticalMaterialsComponent> materialsType =
                entityManager.GetComponentTypeHandle<FactionTacticalMaterialsComponent>(true);
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                NativeArray<FactionTacticalMaterialsComponent> materials =
                    chunk.GetNativeArray(ref materialsType);
                for (int i = 0; i < entities.Length; i++)
                {
                    if (economies[i].FactionId != factionId || materials[i].FactionId != factionId)
                        continue;
                    if (entity != Entity.Null)
                        return false;

                    entity = entities[i];
                }
            }

            return entity != Entity.Null;
        }

        private static bool TryResolveGate(
            ResourceExchangeRecipeConfigSet config,
            in FixedString64Bytes scenarioTag,
            out ResourceExchangeScenarioGateConfigEntry gate)
        {
            for (int i = 0; i < config.ScenarioGates.Count; i++)
            {
                ResourceExchangeScenarioGateConfigEntry candidate = config.ScenarioGates[i];
                if (candidate != null && scenarioTag.Equals(new FixedString64Bytes(candidate.ScenarioTag)))
                {
                    gate = candidate;
                    return true;
                }
            }

            gate = null;
            return false;
        }

        private static int ProjectRecipes(
            ResourceExchangeRecipeConfigSet config,
            in FixedString64Bytes scenarioTag,
            DynamicBuffer<ResourceExchangeRecipeComponent> target)
        {
            int projected = 0;
            for (int i = 0; i < config.Recipes.Count; i++)
            {
                ResourceExchangeRecipeConfigEntry source = config.Recipes[i];
                if (source == null || !scenarioTag.Equals(new FixedString64Bytes(source.MissionTag)))
                    continue;

                target.Add(new ResourceExchangeRecipeComponent
                {
                    RecipeId = new FixedString128Bytes(source.RecipeId),
                    DisplayName = new FixedString128Bytes(source.DisplayName),
                    RouteType = source.RouteType,
                    InputResource = source.InputResource,
                    OutputResource = source.OutputResource,
                    InputAmountMin = source.InputAmountMin,
                    InputAmountMax = source.InputAmountMax,
                    InputStep = source.InputStep,
                    OutputPerInput = source.OutputPerInput,
                    FeePercent = source.FeePercent,
                    DurationSecondsBase = source.DurationSecondsBase,
                    DurationSecondsPerStep = source.DurationSecondsPerStep,
                    RushTicketSecondsPerTicket = source.RushTicketSecondsPerTicket,
                    MaxRushTickets = source.MaxRushTickets,
                    RequiresStorage = source.RequiresStorage ? (byte)1 : (byte)0,
                    RequiresTransportPlane = source.RequiresTransportPlane ? (byte)1 : (byte)0,
                    RequiresTruckPresentation = source.RequiresTruckPresentation ? (byte)1 : (byte)0,
                    Enabled = source.DisabledReason == ResourceExchangeReason.None ? (byte)1 : (byte)0,
                    MissionTag = scenarioTag,
                    DisabledReason = source.DisabledReason,
                    SortOrder = source.SortOrder
                });
                projected++;
            }

            return projected;
        }

        private void EnsureComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            if (!entityManager.HasComponent<T>(entity))
                entityManager.AddComponent<T>(entity);
        }

        private void EnsureBufferComponent<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            if (!entityManager.HasBuffer<T>(entity))
                entityManager.AddBuffer<T>(entity);
        }

        private void ClearBufferIfPresent<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasBuffer<T>(entity))
                entityManager.GetBuffer<T>(entity).Clear();
        }
    }
}
