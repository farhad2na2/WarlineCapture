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
        private EntityQuery resourceExchangeRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiActionRequestComponent>(),
                ComponentType.ReadWrite<UiDiagnosticsOverlayComponent>(),
                ComponentType.ReadWrite<UiMatchHudPassengerDrawerStateComponent>(),
                ComponentType.ReadWrite<UiMatchHudSquadTrayStateComponent>(),
                ComponentType.ReadWrite<UiShellPopupRequestComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            selectionInputQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionInputRequestQueueComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
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

            using NativeArray<UiActionRequestComponent> pending = actionRequests.ToNativeArray(Allocator.Temp);
            actionRequests.Clear();

            AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);

            UiMatchHudPassengerDrawerStateComponent passengerDrawerState =
                state.EntityManager.GetComponentData<UiMatchHudPassengerDrawerStateComponent>(boundary);
            UiMatchHudSquadTrayStateComponent squadTrayState =
                state.EntityManager.GetComponentData<UiMatchHudSquadTrayStateComponent>(boundary);
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
            bool canPresentResourceExchange =
                CanPresentResourceExchange(shellState, matchIntroInputLocked);
            int frame = UnityEngine.Time.frameCount;

            for (int i = 0; i < pending.Length; i++)
            {
                DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests = state.EntityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);
                DynamicBuffer<UiShellPopupRequestComponent> popupRequests = state.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
                DynamicBuffer<UiShellRouteRequestComponent> routeRequests = state.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
                UiActionRequestDispatchSystemHelper.ProcessRequest(
                    pending[i],
                    ref inputState,
                    ref queue,
                    commandRequests,
                    popupRequests,
                    routeRequests,
                    ref diagnosticsOverlay,
                    ref passengerDrawerState,
                    ref squadTrayState,
                    ref resourceExchangeState,
                    hasResourceExchangeState,
                    canPresentResourceExchange,
                    state.EntityManager,
                    boundary,
                    resourceExchangeRequestEntity,
                    resourceExchangeRuntimeState,
                    hasResourceExchangeRequestEntity,
                    frame,
                    state.World);
            }

            state.EntityManager.SetComponentData(boundary, passengerDrawerState);
            state.EntityManager.SetComponentData(boundary, squadTrayState);
            if (hasResourceExchangeState)
                state.EntityManager.SetComponentData(boundary, resourceExchangeState);
            state.EntityManager.SetComponentData(boundary, diagnosticsOverlay);
            state.EntityManager.SetComponentData(selectionInput, inputState);
            state.EntityManager.SetComponentData(selectionInput, queue);
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

        private static bool CanPresentResourceExchange(
            in UiShellStateComponent shellState,
            bool matchIntroInputLocked)
        {
            return shellState.ActiveRoute == UIRoute.Match &&
                   shellState.CurrentMode == UiShellMode.MatchHud &&
                   shellState.IsTransitionRunning == 0 &&
                   !matchIntroInputLocked;
        }

    }
}
