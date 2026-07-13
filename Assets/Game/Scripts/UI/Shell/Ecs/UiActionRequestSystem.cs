using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

            bool needsPlacementCommandQueue = HasBuildPlacementAction(actionRequests);
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
                ProcessRequest(
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

        private static void ProcessRequest(
            UiActionRequestComponent request,
            ref RtsSelectionInputStateComponent inputState,
            ref RtsSelectionInputRequestQueueComponent queue,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests,
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests,
            DynamicBuffer<UiBuildCatalogRequestComponent> buildCatalogRequests,
            DynamicBuffer<UiBuildProductionRequestComponent> buildProductionRequests,
            DynamicBuffer<UiBuildPrimaryRequestComponent> buildPrimaryRequests,
            DynamicBuffer<BuildingUiPlacementCommandRequestElement> placementRequests,
            ref UiDiagnosticsOverlayComponent diagnosticsOverlay,
            ref UiMatchHudPassengerDrawerStateComponent passengerDrawerState,
            ref UiMatchHudSquadTrayStateComponent squadTrayState,
            ref UiBuildDrawerStateComponent buildDrawerState,
            ref UiResourceExchangeStateComponent resourceExchangeState,
            bool hasResourceExchangeState,
            ref BuildingUiPlacementCommandQueueComponent placementQueue,
            bool canOpenResourceExchange,
            EntityManager entityManager,
            Entity resourceExchangeRequestEntity,
            in ResourceExchangeEnabledComponent resourceExchangeRuntimeState,
            bool hasResourceExchangeRequestEntity,
            int frame,
            World world)
        {
            switch (request.Kind)
            {
                case UiActionKind.MatchMenu:
                    routeRequests.Add(new UiShellRouteRequestComponent
                    {
                        Route = UIRoute.MainMenu,
                        Intent = UiShellRouteIntent.ReturnToMainMenu,
                        PushHistory = 0
                    });
                    break;
                case UiActionKind.Pause:
                    EnqueuePopup(popupRequests, UiShellPopupKind.Pause, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.OpenSettings:
                    EnqueuePopup(popupRequests, UiShellPopupKind.Settings, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.OpenResourceExchange:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (canOpenResourceExchange)
                        EnqueuePopup(popupRequests, UiShellPopupKind.ResourceExchange, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.CloseResourceExchange:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueuePopup(popupRequests, UiShellPopupKind.ResourceExchange, UiShellPopupIntent.Hide, request.PayloadId);
                    break;
                case UiActionKind.RightBuild:
                case UiActionKind.Build:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueuePopup(popupRequests, UiShellPopupKind.BuildDrawer, UiShellPopupIntent.Show, request.PayloadId);
                    break;
                case UiActionKind.CloseBuildDrawer:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueuePopup(popupRequests, UiShellPopupKind.BuildDrawer, UiShellPopupIntent.Hide, request.PayloadId);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.CancelActiveCommandMode, frame);
                    break;
                case UiActionKind.BuildCatalogItem:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    queue.LastRequestId++;
                    buildDrawerState.SelectedCatalogSlot = request.PayloadId;
                    buildCatalogRequests.Add(new UiBuildCatalogRequestComponent
                    {
                        CatalogSlot = request.PayloadId,
                        RequestId = queue.LastRequestId
                    });
                    break;
                case UiActionKind.BuildDrawerTab:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (TryResolveBuildDrawerCategory(request.PayloadId, out BuildDrawerCategory category))
                    {
                        buildDrawerState.ActiveCategory = category;
                        buildDrawerState.SelectedCatalogSlot = 0;
                    }
                    break;
                case UiActionKind.ResourceExchangeTab:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeState &&
                        TryResolveResourceExchangeTab(request.PayloadId, out UiResourceExchangeTab exchangeTab))
                    {
                        resourceExchangeState.ActiveTab = exchangeTab;
                        resourceExchangeState.SelectedRecipeSlot = 0;
                        resourceExchangeState.SelectedInputAmount = 0;
                        resourceExchangeState.Version++;
                    }
                    break;
                case UiActionKind.ResourceExchangeRecipe:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeState)
                    {
                        resourceExchangeState.SelectedRecipeSlot = math.max(0, request.PayloadId);
                        resourceExchangeState.SelectedInputAmount = 0;
                        resourceExchangeState.Version++;
                    }
                    break;
                case UiActionKind.ResourceExchangeAmountDecrease:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    AdjustResourceExchangeAmount(
                        entityManager,
                        resourceExchangeRequestEntity,
                        hasResourceExchangeRequestEntity,
                        ref resourceExchangeState,
                        hasResourceExchangeState,
                        -1);
                    break;
                case UiActionKind.ResourceExchangeAmountIncrease:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    AdjustResourceExchangeAmount(
                        entityManager,
                        resourceExchangeRequestEntity,
                        hasResourceExchangeRequestEntity,
                        ref resourceExchangeState,
                        hasResourceExchangeState,
                        1);
                    break;
                case UiActionKind.ResourceExchangeConfirm:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueResourceExchangeConfirm(
                        entityManager,
                        resourceExchangeRequestEntity,
                        resourceExchangeRuntimeState,
                        hasResourceExchangeRequestEntity,
                        resourceExchangeState,
                        hasResourceExchangeState,
                        frame);
                    break;
                case UiActionKind.ResourceExchangeRushAll:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueRushAllRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            0,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
                    }
                    break;
                case UiActionKind.ResourceExchangeClearCompleted:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueClearCompletedRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
                    }
                    break;
                case UiActionKind.ResourceExchangeQueueRush:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity && request.PayloadId > 0)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueRushRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            request.PayloadId,
                            1,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
                    }
                    break;
                case UiActionKind.ResourceExchangeQueueCancel:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    if (hasResourceExchangeRequestEntity && request.PayloadId > 0)
                    {
                        ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(
                            entityManager,
                            resourceExchangeRequestEntity,
                            request.PayloadId,
                            resourceExchangeRuntimeState.FactionId,
                            frame);
                    }
                    break;
                case UiActionKind.BuildDrawerPrimaryBuild:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    queue.LastRequestId++;
                    buildPrimaryRequests.Add(new UiBuildPrimaryRequestComponent
                    {
                        RequestId = queue.LastRequestId
                    });
                    break;
                case UiActionKind.BuildProductionRush:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.Rush, 0);
                    break;
                case UiActionKind.BuildProductionClear:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.Clear, 0);
                    break;
                case UiActionKind.BuildProductionCancelActive:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.CancelActive, 0);
                    break;
                case UiActionKind.BuildProductionCancelQueued:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildProductionRequest(ref queue, buildProductionRequests, UiBuildProductionActionKind.CancelQueued, request.PayloadId);
                    break;
                case UiActionKind.BuildPlacementConfirm:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildPlacementRequest(
                        ref placementQueue,
                        placementRequests,
                        BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                        true);
                    break;
                case UiActionKind.BuildPlacementCancel:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildPlacementRequest(
                        ref placementQueue,
                        placementRequests,
                        BuildingUiPlacementCommandRequestElement.KindCancelPlacement,
                        true);
                    break;
                case UiActionKind.BuildPlacementRotate:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueBuildPlacementRequest(
                        ref placementQueue,
                        placementRequests,
                        BuildingUiPlacementCommandRequestElement.KindRotatePlacement,
                        false);
                    break;
                case UiActionKind.Select:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(
                        ref queue,
                        commandRequests,
                        IsActiveMode(inputState, TacticalCommandMode.Select)
                            ? RtsSelectionCommandIntentKind.ExitSelectionMode
                            : RtsSelectionCommandIntentKind.EnterSelectionMode,
                        frame);
                    break;
                case UiActionKind.Move:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterMoveTargetMode, frame);
                    break;
                case UiActionKind.Attack:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterAttackTargetMode, frame);
                    break;
                case UiActionKind.Hold:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.HoldPosition, frame);
                    break;
                case UiActionKind.Stop:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.Stop, frame);
                    break;
                case UiActionKind.Scan:
                case UiActionKind.Support:
                case UiActionKind.RightSupport:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterScanTargetMode, frame);
                    break;
                case UiActionKind.SquadSlot1:
                case UiActionKind.SquadSlot2:
                case UiActionKind.SquadSlot3:
                case UiActionKind.SquadSlot4:
                case UiActionKind.SquadSlot5:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    squadTrayState.SelectedSlot = ToSquadTraySlot(request.Kind);
                    break;
                case UiActionKind.ReturnSelection:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.ReturnToBase, frame);
                    break;
                case UiActionKind.DestroySelection:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.DestroyFocusedUnit, frame);
                    break;
                case UiActionKind.BoardSelection:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.EnterBoardTargetMode, frame);
                    break;
                case UiActionKind.TogglePassengerDrawer:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = passengerDrawerState.Visible == 0 ? (byte)1 : (byte)0;
                    EmitDrawerAudio(world, passengerDrawerState.Visible != 0);
                    break;
                case UiActionKind.ClosePassengerDrawer:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = 0;
                    EmitDrawerAudio(world, open: false);
                    break;
                case UiActionKind.ExitAllPassengers:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    passengerDrawerState.Visible = 0;
                    EmitDrawerAudio(world, open: false);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.DisembarkTransportPassenger, frame);
                    break;
                case UiActionKind.BoardAll:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.BoardAllSelectedTransport, frame);
                    break;
                case UiActionKind.CancelFeedback:
                    CaptureUiClickSequence(ref inputState, commandRequests, frame);
                    EnqueueSelectionIntent(ref queue, commandRequests, RtsSelectionCommandIntentKind.CancelActiveCommandMode, frame);
                    break;
                case UiActionKind.ToggleDiagnosticsOverlay:
                    diagnosticsOverlay.LogVisible = diagnosticsOverlay.LogVisible == 0 ? (byte)1 : (byte)0;
                    break;
                case UiActionKind.CloseDiagnosticsOverlay:
                    diagnosticsOverlay.LogVisible = 0;
                    break;
            }
        }

        private static void AdjustResourceExchangeAmount(
            EntityManager entityManager,
            Entity exchangeEntity,
            bool hasExchangeEntity,
            ref UiResourceExchangeStateComponent resourceExchangeState,
            bool hasResourceExchangeState,
            int direction)
        {
            if (!hasResourceExchangeState ||
                !hasExchangeEntity ||
                !TryResolveSelectedResourceExchangeRecipe(
                    entityManager,
                    exchangeEntity,
                    resourceExchangeState,
                    out ResourceExchangeRecipeComponent recipe))
            {
                return;
            }

            int current = NormalizeResourceExchangeInputAmount(recipe, resourceExchangeState.SelectedInputAmount);
            int step = math.max(1, recipe.InputStep);
            int next = current + (direction >= 0 ? step : -step);
            resourceExchangeState.SelectedInputAmount = NormalizeResourceExchangeInputAmount(recipe, next);
            resourceExchangeState.Version++;
        }

        private static void EnqueueResourceExchangeConfirm(
            EntityManager entityManager,
            Entity exchangeEntity,
            in ResourceExchangeEnabledComponent enabled,
            bool hasExchangeEntity,
            in UiResourceExchangeStateComponent resourceExchangeState,
            bool hasResourceExchangeState,
            int frame)
        {
            if (!hasResourceExchangeState ||
                !hasExchangeEntity ||
                !TryResolveSelectedResourceExchangeRecipe(
                    entityManager,
                    exchangeEntity,
                    resourceExchangeState,
                    out ResourceExchangeRecipeComponent recipe))
            {
                return;
            }

            int amount = NormalizeResourceExchangeInputAmount(recipe, resourceExchangeState.SelectedInputAmount);
            ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
                entityManager,
                exchangeEntity,
                recipe.RecipeId,
                amount,
                enabled.FactionId,
                frame);
        }

        private static bool TryResolveSelectedResourceExchangeRecipe(
            EntityManager entityManager,
            Entity exchangeEntity,
            in UiResourceExchangeStateComponent resourceExchangeState,
            out ResourceExchangeRecipeComponent recipe)
        {
            recipe = default;
            if (exchangeEntity == Entity.Null ||
                !entityManager.HasBuffer<ResourceExchangeRecipeComponent>(exchangeEntity))
            {
                return false;
            }

            ResourceExchangeRouteType routeType = ToResourceExchangeRouteType(resourceExchangeState.ActiveTab);
            int selectedSlot = math.max(0, resourceExchangeState.SelectedRecipeSlot);
            int visibleIndex = 0;
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
                entityManager.GetBuffer<ResourceExchangeRecipeComponent>(exchangeEntity, true);
            for (int i = 0; i < recipes.Length; i++)
            {
                ResourceExchangeRecipeComponent candidate = recipes[i];
                if (candidate.RouteType != routeType)
                    continue;

                if (visibleIndex == selectedSlot)
                {
                    recipe = candidate;
                    return true;
                }

                visibleIndex++;
            }

            return false;
        }

        private static int NormalizeResourceExchangeInputAmount(
            in ResourceExchangeRecipeComponent recipe,
            int inputAmount)
        {
            int min = math.max(1, recipe.InputAmountMin);
            int max = math.max(min, recipe.InputAmountMax);
            int step = math.max(1, recipe.InputStep);
            int amount = inputAmount > 0 ? inputAmount : min;
            amount = math.clamp(amount, min, max);
            int completedSteps = (amount - min) / step;
            return math.clamp(min + completedSteps * step, min, max);
        }

        private static ResourceExchangeRouteType ToResourceExchangeRouteType(UiResourceExchangeTab tab)
        {
            return tab == UiResourceExchangeTab.Import
                ? ResourceExchangeRouteType.Import
                : ResourceExchangeRouteType.Export;
        }

        private static void EmitDrawerAudio(World world, bool open)
        {
            UIAudioEventKind kind = open ? UIAudioEventKind.DrawerOpen : UIAudioEventKind.DrawerClose;
            if (UIAudioEventGateway.TryCreateRequest(kind, out UIAudioEventRequest request))
                UiAudioEventBridgeSystem.TryEnqueue(world, request);
        }

        private static void CaptureUiClickSequence(
            ref RtsSelectionInputStateComponent inputState,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            int frame)
        {
            inputState.QueuedMoveOrderToken++;
            inputState.HasQueuedMoveOrder = 0;
            inputState.QueuedMoveOrderScreenPosition = default;
            inputState.QueuedMoveOrderFrame = -1;
            int ignoreUntilFrame = frame + 1;
            if (inputState.IgnoreWorldCommandsUntilFrame < ignoreUntilFrame)
                inputState.IgnoreWorldCommandsUntilFrame = ignoreUntilFrame;
            inputState.IgnoreUiClickUntilRelease = 1;
            inputState.IgnoreNextLeftMouseRelease = 1;
            inputState.PointerPressedOverUi = 1;
            inputState.IsDraggingSelection = 0;
            inputState.HasLiveSelectionRect = 0;
            inputState.BoardPassengerDragArmed = 0;
            ClearPendingMoveRequests(commandRequests);
        }

        private static void ClearPendingMoveRequests(DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests)
        {
            for (int i = commandRequests.Length - 1; i >= 0; i--)
            {
                if (commandRequests[i].Kind == RtsSelectionCommandIntentKind.Move)
                    commandRequests.RemoveAt(i);
            }
        }

        private static bool IsActiveMode(RtsSelectionInputStateComponent inputState, TacticalCommandMode mode)
        {
            return (TacticalCommandMode)inputState.ActiveCommandMode == mode;
        }

        private static void EnqueueSelectionIntent(
            ref RtsSelectionInputRequestQueueComponent queue,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            RtsSelectionCommandIntentKind kind,
            int frame)
        {
            queue.LastRequestId++;
            commandRequests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = kind,
                RequestId = queue.LastRequestId,
                Frame = frame
            });
        }

        private static void EnqueueBuildProductionRequest(
            ref RtsSelectionInputRequestQueueComponent queue,
            DynamicBuffer<UiBuildProductionRequestComponent> buildProductionRequests,
            UiBuildProductionActionKind actionKind,
            int queueSlot)
        {
            queue.LastRequestId++;
            buildProductionRequests.Add(new UiBuildProductionRequestComponent
            {
                ActionKind = actionKind,
                QueueSlot = queueSlot,
                RequestId = queue.LastRequestId
            });
        }

        private static void EnqueueBuildPlacementRequest(
            ref BuildingUiPlacementCommandQueueComponent queue,
            DynamicBuffer<BuildingUiPlacementCommandRequestElement> placementRequests,
            byte requestKind,
            bool clearBuildingSelection)
        {
            queue.LastRequestId++;
            placementRequests.Add(new BuildingUiPlacementCommandRequestElement
            {
                RequestId = queue.LastRequestId,
                BuildingId = default,
                RequestKind = requestKind,
                ClearBuildingSelection = clearBuildingSelection ? (byte)1 : (byte)0
            });
        }

        private static bool HasBuildPlacementAction(DynamicBuffer<UiActionRequestComponent> actionRequests)
        {
            for (int i = 0; i < actionRequests.Length; i++)
            {
                if (actionRequests[i].Kind is
                    UiActionKind.BuildPlacementConfirm or
                    UiActionKind.BuildPlacementCancel or
                    UiActionKind.BuildPlacementRotate)
                {
                    return true;
                }
            }

            return false;
        }

        private static MatchHudSquadTraySlot ToSquadTraySlot(UiActionKind kind)
        {
            return kind switch
            {
                UiActionKind.SquadSlot1 => MatchHudSquadTraySlot.Soldiers,
                UiActionKind.SquadSlot2 => MatchHudSquadTraySlot.CombatVehicles,
                UiActionKind.SquadSlot3 => MatchHudSquadTraySlot.AttackHelicopter,
                UiActionKind.SquadSlot4 => MatchHudSquadTraySlot.Jet,
                UiActionKind.SquadSlot5 => MatchHudSquadTraySlot.Transport,
                _ => MatchHudSquadTraySlot.None
            };
        }

        private static bool TryResolveBuildDrawerCategory(int payloadId, out BuildDrawerCategory category)
        {
            if (payloadId >= (int)BuildDrawerCategory.Buildings &&
                payloadId <= (int)BuildDrawerCategory.Soldiers)
            {
                category = (BuildDrawerCategory)payloadId;
                return true;
            }

            category = BuildDrawerCategory.Buildings;
            return false;
        }

        private static bool TryResolveResourceExchangeTab(int payloadId, out UiResourceExchangeTab tab)
        {
            if (payloadId == (int)UiResourceExchangeTab.Import)
            {
                tab = UiResourceExchangeTab.Import;
                return true;
            }

            if (payloadId == (int)UiResourceExchangeTab.Export)
            {
                tab = UiResourceExchangeTab.Export;
                return true;
            }

            tab = UiResourceExchangeTab.Export;
            return false;
        }

        private static void EnqueuePopup(
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests,
            UiShellPopupKind kind,
            UiShellPopupIntent intent,
            int payloadId)
        {
            popupRequests.Add(new UiShellPopupRequestComponent
            {
                PopupKind = kind,
                Intent = intent,
                PayloadId = payloadId
            });
        }
    }
}
