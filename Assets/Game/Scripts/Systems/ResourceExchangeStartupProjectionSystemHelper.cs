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
            => ResourceExchangeAIStartupProjectionSystemHelper.Initialize(
                entityManager,
                this,
                config);

        internal bool TryResolveScenarioTag(out FixedString64Bytes scenarioTag)
        {
            scenarioTag = default;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CustomGameStartupStateComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            scenarioTag = query.GetSingleton<CustomGameStartupStateComponent>().GameModeId;
            return scenarioTag.Length > 0;
        }

        internal bool TryResolveCanonicalFactionEntity(byte factionId, out Entity entity)
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

        internal static bool TryResolveGate(
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
    }
}
