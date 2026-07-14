using Unity.Collections;
using Unity.Entities;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UiActionRequestSystem : ISystem
    {
        private EntityQuery boundaryQuery;
        private EntityQuery selectionInputQuery;
        private EntityQuery buildingPlacementCommandQuery;
        private EntityQuery resourceExchangeRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiActionRequestComponent>(),
                ComponentType.ReadWrite<UiDiagnosticsOverlayComponent>(),
                ComponentType.ReadWrite<UiMatchHudPassengerDrawerStateComponent>(),
                ComponentType.ReadWrite<UiMatchHudSquadTrayStateComponent>(),
                ComponentType.ReadWrite<UiBuildDrawerStateComponent>(),
                ComponentType.ReadWrite<UiBuildCatalogRequestComponent>(),
                ComponentType.ReadWrite<UiBuildProductionRequestComponent>(),
                ComponentType.ReadWrite<UiBuildPrimaryRequestComponent>(),
                ComponentType.ReadWrite<UiShellPopupRequestComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            selectionInputQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionInputRequestQueueComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
            buildingPlacementCommandQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<BuildingUiPlacementCommandQueueComponent>(),
                ComponentType.ReadWrite<BuildingUiPlacementCommandRequestElement>());
            resourceExchangeRequestQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ResourceExchangeEnabledComponent>(),
                ComponentType.ReadWrite<ResourceExchangeRequestQueueComponent>(),
                ComponentType.ReadWrite<ResourceExchangeRequestComponent>(),
                ComponentType.ReadOnly<ResourceExchangeRecipeComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = ResolveFirstEntity(boundaryQuery);
            if (!TryResolveSelectionInputEntity(ref state, out Entity selectionInput))
                return;

            DynamicBuffer<UiActionRequestComponent> actionRequests = state.EntityManager.GetBuffer<UiActionRequestComponent>(boundary);
            if (actionRequests.Length == 0)
                return;

            bool needsPlacementCommandQueue = UiActionRequestDispatchSystemHelper.HasBuildPlacementAction(actionRequests);
            Entity placementCommand = Entity.Null;
            if (needsPlacementCommandQueue &&
                !TryResolveBuildingPlacementCommandEntity(ref state, out placementCommand))
            {
                return;
            }

            Game.Runtime.AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);

            actionRequests = state.EntityManager.GetBuffer<UiActionRequestComponent>(boundary);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
                state.EntityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
                state.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                state.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            DynamicBuffer<UiBuildCatalogRequestComponent> buildCatalogRequests =
                state.EntityManager.GetBuffer<UiBuildCatalogRequestComponent>(boundary);
            DynamicBuffer<UiBuildProductionRequestComponent> buildProductionRequests =
                state.EntityManager.GetBuffer<UiBuildProductionRequestComponent>(boundary);
            DynamicBuffer<UiBuildPrimaryRequestComponent> buildPrimaryRequests =
                state.EntityManager.GetBuffer<UiBuildPrimaryRequestComponent>(boundary);
            UiMatchHudPassengerDrawerStateComponent passengerDrawerState =
                state.EntityManager.GetComponentData<UiMatchHudPassengerDrawerStateComponent>(boundary);
            UiMatchHudSquadTrayStateComponent squadTrayState =
                state.EntityManager.GetComponentData<UiMatchHudSquadTrayStateComponent>(boundary);
            UiBuildDrawerStateComponent buildDrawerState =
                state.EntityManager.GetComponentData<UiBuildDrawerStateComponent>(boundary);
            bool hasResourceExchangeState =
                state.EntityManager.HasComponent<UiResourceExchangeStateComponent>(boundary);
            UiResourceExchangeStateComponent resourceExchangeState = hasResourceExchangeState
                ? state.EntityManager.GetComponentData<UiResourceExchangeStateComponent>(boundary)
                : default;
            UiDiagnosticsOverlayComponent diagnosticsOverlay =
                state.EntityManager.GetComponentData<UiDiagnosticsOverlayComponent>(boundary);
            RtsSelectionInputStateComponent inputState =
                state.EntityManager.GetComponentData<RtsSelectionInputStateComponent>(selectionInput);
            RtsSelectionInputRequestQueueComponent queue =
                state.EntityManager.GetComponentData<RtsSelectionInputRequestQueueComponent>(selectionInput);
            bool hasResourceExchangeRequestEntity =
                TryResolvePlayerResourceExchangeRequestEntity(
                    ref state,
                    out Entity resourceExchangeRequestEntity,
                    out ResourceExchangeEnabledComponent resourceExchangeRuntimeState);
            UiShellStateComponent shellState =
                state.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
            bool matchIntroInputLocked =
                state.EntityManager.HasComponent<MatchIntroTransitionComponent>(boundary) &&
                state.EntityManager.GetComponentData<MatchIntroTransitionComponent>(boundary).InputLocked != 0;
            bool canOpenResourceExchange =
                CanOpenResourceExchange(
                    shellState,
                    matchIntroInputLocked,
                    hasResourceExchangeRequestEntity,
                    resourceExchangeRuntimeState,
                    state.EntityManager,
                    resourceExchangeRequestEntity);
            DynamicBuffer<BuildingUiPlacementCommandRequestElement> placementRequests = default;
            BuildingUiPlacementCommandQueueComponent placementQueue = default;
            if (needsPlacementCommandQueue)
            {
                placementRequests = state.EntityManager.GetBuffer<BuildingUiPlacementCommandRequestElement>(placementCommand);
                placementQueue = state.EntityManager.GetComponentData<BuildingUiPlacementCommandQueueComponent>(placementCommand);
            }
            int frame = UnityEngine.Time.frameCount;

            for (int i = 0; i < actionRequests.Length; i++)
            {
                UiActionRequestDispatchSystemHelper.ProcessRequest(
                    actionRequests[i],
                    ref inputState,
                    ref queue,
                    commandRequests,
                    popupRequests,
                    routeRequests,
                    buildCatalogRequests,
                    buildProductionRequests,
                    buildPrimaryRequests,
                    placementRequests,
                    ref diagnosticsOverlay,
                    ref passengerDrawerState,
                    ref squadTrayState,
                    ref buildDrawerState,
                    ref resourceExchangeState,
                    hasResourceExchangeState,
                    ref placementQueue,
                    canOpenResourceExchange,
                    state.EntityManager,
                    resourceExchangeRequestEntity,
                    resourceExchangeRuntimeState,
                    hasResourceExchangeRequestEntity,
                    frame,
                    state.World);
            }

            actionRequests.Clear();
            state.EntityManager.SetComponentData(boundary, passengerDrawerState);
            state.EntityManager.SetComponentData(boundary, squadTrayState);
            state.EntityManager.SetComponentData(boundary, buildDrawerState);
            if (hasResourceExchangeState)
                state.EntityManager.SetComponentData(boundary, resourceExchangeState);
            state.EntityManager.SetComponentData(boundary, diagnosticsOverlay);
            state.EntityManager.SetComponentData(selectionInput, inputState);
            state.EntityManager.SetComponentData(selectionInput, queue);
            if (needsPlacementCommandQueue)
                state.EntityManager.SetComponentData(placementCommand, placementQueue);
        }

        private bool TryResolveSelectionInputEntity(ref SystemState state, out Entity entity)
        {
            entity = Entity.Null;
            if (!selectionInputQuery.IsEmptyIgnoreFilter)
            {
                entity = ResolveFirstEntity(selectionInputQuery);
                EnsureSelectionBuffers(ref state, entity);
                return true;
            }

            entity = state.EntityManager.CreateEntity(
                typeof(RtsSelectionInputStateComponent),
                typeof(RtsSelectionInputRequestQueueComponent));
            state.EntityManager.SetComponentData(entity, new RtsSelectionInputStateComponent
            {
                QueuedMoveOrderFrame = -1
            });
            EnsureSelectionBuffers(ref state, entity);
            return false;
        }

        private static void EnsureSelectionBuffers(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasBuffer<RtsSelectionPointerRequestElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionPointerRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<RtsSelectionCommandIntentRequestElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<RtsSelectionCommandResultElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionCommandResultElement>(entity);
        }

        private bool TryResolveBuildingPlacementCommandEntity(ref SystemState state, out Entity entity)
        {
            entity = Entity.Null;
            if (!buildingPlacementCommandQuery.IsEmptyIgnoreFilter)
            {
                entity = ResolveFirstEntity(buildingPlacementCommandQuery);
                EnsureBuildingPlacementCommandBuffers(ref state, entity);
                return true;
            }

            entity = state.EntityManager.CreateEntity(typeof(BuildingUiPlacementCommandQueueComponent));
            state.EntityManager.SetName(entity, "BuildingUiPlacementCommands");
            state.EntityManager.SetComponentData(entity, new BuildingUiPlacementCommandQueueComponent
            {
                LastRequestId = 0
            });
            EnsureBuildingPlacementCommandBuffers(ref state, entity);
            return false;
        }

        private static void EnsureBuildingPlacementCommandBuffers(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasBuffer<BuildingUiPlacementCommandRequestElement>(entity))
                state.EntityManager.AddBuffer<BuildingUiPlacementCommandRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<BuildingUiPlacementCommandResultElement>(entity))
                state.EntityManager.AddBuffer<BuildingUiPlacementCommandResultElement>(entity);
        }

        private static Entity ResolveFirstEntity(EntityQuery query)
        {
            if (query.CalculateEntityCount() == 1)
                return query.GetSingletonEntity();

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            return entities.Length > 0 ? entities[0] : Entity.Null;
        }

        private bool TryResolvePlayerResourceExchangeRequestEntity(
            ref SystemState state,
            out Entity entity,
            out ResourceExchangeEnabledComponent enabled)
        {
            entity = Entity.Null;
            enabled = default;
            using NativeArray<Entity> entities = resourceExchangeRequestQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                ResourceExchangeEnabledComponent candidate =
                    state.EntityManager.GetComponentData<ResourceExchangeEnabledComponent>(entities[i]);
                if (candidate.FactionId != FactionIdentity.PlayerFactionId)
                    continue;
                if (entity != Entity.Null)
                    return false;

                entity = entities[i];
                enabled = candidate;
            }

            return entity != Entity.Null;
        }

        private static bool CanOpenResourceExchange(
            in UiShellStateComponent shellState,
            bool matchIntroInputLocked,
            bool hasResourceExchangeRequestEntity,
            in ResourceExchangeEnabledComponent enabled,
            EntityManager entityManager,
            Entity exchangeEntity)
        {
            if (shellState.ActiveRoute != UIRoute.Match ||
                shellState.CurrentMode != UiShellMode.MatchHud ||
                shellState.IsTransitionRunning != 0 ||
                matchIntroInputLocked ||
                !hasResourceExchangeRequestEntity ||
                enabled.Enabled == 0 ||
                enabled.FactionId != FactionIdentity.PlayerFactionId ||
                enabled.MaxQueueItems <= 0 ||
                enabled.ScenarioTag.Length == 0 ||
                exchangeEntity == Entity.Null ||
                !entityManager.Exists(exchangeEntity) ||
                !entityManager.HasBuffer<ResourceExchangeRecipeComponent>(exchangeEntity))
            {
                return false;
            }

            DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
                entityManager.GetBuffer<ResourceExchangeRecipeComponent>(exchangeEntity);
            for (int i = 0; i < recipes.Length; i++)
            {
                ResourceExchangeRecipeComponent recipe = recipes[i];
                if (recipe.Enabled != 0 &&
                    (recipe.MissionTag.Length == 0 || recipe.MissionTag.Equals(enabled.ScenarioTag)))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
