using System.Collections.Generic;
using Game.Scripts.UI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

internal sealed class SelectionGameplayStartupSystem
{
    public readonly struct Result
    {
        public readonly System.Action<MainMenuPlayUI> BindSelectionMainMenu;
        public readonly System.Action SelectionRuntimeUpdate;
        public readonly System.Action DisposeSelection;
        public readonly SelectionUiCommandSystem SelectionUiCommand;
        public readonly SelectionUiReadModelSystem SelectionUiReadModel;
        public readonly SelectionUiCameraSystem SelectionUiCamera;
        public readonly SelectionBuildingInteractionSystem SelectionBuildingInteraction;
        public readonly SelectionScreenMarkerSystem SelectionScreenMarkers;
        public readonly SelectionRectangleView SelectionRectangleView;

        public Result(
            System.Action<MainMenuPlayUI> bindSelectionMainMenu,
            System.Action selectionRuntimeUpdate,
            System.Action disposeSelection,
            SelectionUiCommandSystem selectionUiCommand,
            SelectionUiReadModelSystem selectionUiReadModel,
            SelectionUiCameraSystem selectionUiCamera,
            SelectionBuildingInteractionSystem selectionBuildingInteraction,
            SelectionScreenMarkerSystem selectionScreenMarkers,
            SelectionRectangleView selectionRectangleView)
        {
            BindSelectionMainMenu = bindSelectionMainMenu;
            SelectionRuntimeUpdate = selectionRuntimeUpdate;
            DisposeSelection = disposeSelection;
            SelectionUiCommand = selectionUiCommand;
            SelectionUiReadModel = selectionUiReadModel;
            SelectionUiCamera = selectionUiCamera;
            SelectionBuildingInteraction = selectionBuildingInteraction;
            SelectionScreenMarkers = selectionScreenMarkers;
            SelectionRectangleView = selectionRectangleView;
        }
    }

    public Result Initialize(
        RTSSelectionSystemConfig rtsSelectionConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadBuildReadModelSystem roadBuildReadModel,
        BuildingPlacementInteractionSystem buildingInteraction,
        BuildingPlacementInteractionSystem.Context buildingInteractionContext,
        System.Func<Rect, bool> trySelectFirstBuildingInScreenRect,
        SelectionHudFeedbackSystem.ResolveSelectionPortraitSpriteDelegate resolveSelectionPortraitSprite,
        FactionVisualSettings factionVisuals)
    {
        var selectionRuntimeDiagnosticsSystem = new SelectionRuntimeDiagnosticsSystem();
        var selectionRuntimeConfigSystem = new SelectionRuntimeConfigSystem();
        var selectionRuntimeQuerySystem = new SelectionRuntimeQuerySystem();
        SelectionRuntimeConfigSystem.State runtimeConfig = selectionRuntimeConfigSystem.CreateState(rtsSelectionConfig, worldCamera);
        var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
        var rtsSelectionInputSystem = new RtsSelectionInputSystem();
        var rtsSelectionRuntimeInputSystem = new RtsSelectionRuntimeInputSystem();
        var rtsSelectionRuntimeInputContextSystem = new RtsSelectionRuntimeInputContextSystem();
        var rtsSelectionRuntimeCameraSystem = new RtsSelectionRuntimeCameraSystem();
        var rtsSelectionRuntimeCameraContextSystem = new RtsSelectionRuntimeCameraContextSystem();
        var rtsSelectionCommandResultFlushSystem = new RtsSelectionCommandResultFlushSystem();
        var rtsSelectionCommandResultContextSystem = new RtsSelectionCommandResultContextSystem();
        var rtsSelectionFocusCommandSystem = new RtsSelectionFocusCommandSystem();
        var rtsSelectionFocusCommandContextSystem = new RtsSelectionFocusCommandContextSystem();
        var rtsSelectionPointerTargetCommandSystem = new RtsSelectionPointerTargetCommandSystem();
        var rtsSelectionPointerTargetCommandContextSystem = new RtsSelectionPointerTargetCommandContextSystem();
        var rtsCameraSystem = new RtsCameraSystem();
        var rtsCameraRequestSystem = new RtsCameraRequestSystem();
        World cachedMatchIntroWorld = null;
        EntityQuery matchIntroLockQuery = default;
        bool hasMatchIntroLockQuery = false;
        var selectionUiCommand = new SelectionUiCommandSystem(IsMatchIntroGameplayInputLocked);
        var selectionUiReadModel = new SelectionUiReadModelSystem();
        var selectionUiCamera = new SelectionUiCameraSystem(rtsCameraSystem, rtsCameraRequestSystem);
        var selectionScreenMarkers = new SelectionScreenMarkerSystem();
        var selectionStateSystem = new SelectionStateSystem();
        var selectionUiQuerySystem = new SelectionUiQuerySystem();
        var focusedUnitUiReadModelSystem = new FocusedUnitUiReadModelSystem();
        var visibleUnitSelectionSystem = new VisibleUnitSelectionSystem();
        var selectionRectangleRequestSystem = new SelectionRectangleRequestSystem();
        var unitMoveOrderSystem = new UnitMoveOrderSystem();
        var selectedMoveOrderCommandSystem = new SelectedMoveOrderCommandSystem();
        var selectionMoveCommandRequestSystem = new SelectionMoveCommandRequestSystem();
        var unitTargetOrderSystem = new UnitTargetOrderSystem();
        var attackOrderCommandSystem = new AttackOrderCommandSystem();
        var selectionAttackCommandRequestSystem = new SelectionAttackCommandRequestSystem();
        var scanIntelCommandSystem = new ScanIntelCommandSystem();
        var selectionScanCommandRequestSystem = new SelectionScanCommandRequestSystem();
        var selectionOrderMarkerSystem = new SelectionOrderMarkerSystem();
        var selectionHudFeedbackSystem = new SelectionHudFeedbackSystem();
        var matchOverlayCommandTabFeedbackSystem = new MatchOverlayCommandTabFeedbackSystem();
        var focusedUnitCommandSystem = new FocusedUnitCommandSystem();
        var focusedUnitLifecycleSystem = new FocusedUnitLifecycleSystem();
        var selectedUnitOrderSnapshotSystem = new SelectedUnitOrderSnapshotSystem();
        var buildingTargetMoveOrderSystem = new BuildingTargetMoveOrderSystem();
        var transportBoardingCommandSystem = new TransportBoardingCommandSystem();
        var selectionTransportCommandRequestSystem = new SelectionTransportCommandRequestSystem();
        var focusableUnitLookupSystem = new FocusableUnitLookupSystem();
        var matchHudSquadTraySelectionSystem = new MatchHudSquadTraySelectionSystem();
        var unitTransportCapacitySystem = new UnitTransportCapacitySystem();
        var unitTransportBoardingQuerySystem = new UnitTransportBoardingQuerySystem();
        var unitTransportBoardingRuleSystem = new UnitTransportBoardingRuleSystem();
        var unitTransportApproachCellSystem = new UnitTransportApproachCellSystem();
        var unitTransportAirPickupSystem = new UnitTransportAirPickupSystem();
        var unitTransportRopeDisembarkCommandSystem = new UnitTransportRopeDisembarkCommandSystem();
        var selectionBuildingInteraction = new SelectionBuildingInteractionSystem();
        var visibleSelectionScratch = new List<Entity>();
        MainMenuPlayUI mainMenuPlayUi = null;
        RoadBuildReadModelSystem roadBuildReadState = roadBuildReadModel;
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem = buildingInteraction;
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = buildingInteractionContext;
        bool explicitAttackTargetModeActive = false;

        selectionUiCamera.Init(rtsSelectionConfig, worldCamera);
        selectionBuildingInteraction.Init(selectionStateSystem, selectionScreenMarkers, worldCamera);
        selectionHudFeedbackSystem.ResetViewCache();
        selectionOrderMarkerSystem.Initialize(
            runtimeConfig.MoveOrderMarkerPrefab,
            runtimeConfig.AttackOrderMarkerPrefab,
            runtimeConfig.OrderMarkerVisibleSeconds,
            runtimeUiRoot);

        return new Result(
            BindSelectionMainMenu,
            UpdateSelectionRuntimePhases,
            selectionOrderMarkerSystem.Dispose,
            selectionUiCommand,
            selectionUiReadModel,
            selectionUiCamera,
            selectionBuildingInteraction,
            selectionScreenMarkers,
            EnsureSelectionRectangleView(runtimeUiRoot, rtsSelectionConfig));

        void BindSelectionMainMenu(MainMenuPlayUI mainMenu)
        {
            mainMenuPlayUi = mainMenu;
            roadBuildReadState = roadBuildReadModel;
            buildingPlacementInteractionSystem = buildingInteraction;
            buildingPlacementInteractionContext = buildingInteractionContext;
            mainMenuPlayUi?.ConfigureMatchHudSelectionPanelBinding(BindMatchHudSelectionPanel);
            mainMenuPlayUi?.ConfigureMatchHudSquadTrayBinding(BindMatchHudSquadTray);
        }

        void BindMatchHudSelectionPanel(MatchHudSelectionPanelView view)
        {
            selectionHudFeedbackSystem.BindMatchHudSelectionPanel(view);
            selectionBuildingInteraction.BindMatchHudSelectionPanel(view);
        }

        void BindMatchHudSquadTray(MatchHudSquadTrayView view)
        {
            if (view == null)
                return;

            view.Bind(slot =>
            {
                selectionUiCommand.CaptureUiClickSequence();
                runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                matchHudSquadTraySelectionSystem.SelectSlot(
                    CreateSquadTraySelectionContext(),
                    view,
                    slot);
            });
        }

        void UpdateSelectionRuntimePhases()
        {
            if (rtsSelectionInputSystem.HasPendingTransportCommandRequests())
                ProcessTransportCommandRequests();
            if (rtsSelectionInputSystem.HasPendingExternalSelectionCommandRequests())
                rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
            rtsSelectionRuntimeInputSystem.ProcessQueuedMoveOrder(CreateRuntimeInputContext());
            RefreshFocusedSelectionReadModels();
            rtsSelectionCommandResultFlushSystem.UpdateOrderMarkerVisibility(CreateCommandResultFlushContext());
            UpdateAttackTargetPreviewMarkers();

            if (rtsSelectionRuntimeCameraSystem.UpdateRuntimeCameraTick(CreateRuntimeCameraContext()))
                rtsSelectionRuntimeInputSystem.UpdateNormalPointerInput(CreateRuntimeInputContext());
        }

        RtsSelectionRuntimeInputSystem.Context CreateRuntimeInputContext()
        {
            return rtsSelectionRuntimeInputContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                mainMenuPlayUi,
                runtimeConfig,
                () => explicitAttackTargetModeActive,
                value => explicitAttackTargetModeActive = value,
                () => rtsCameraSystem.IsDragging,
                value => rtsSelectionRuntimeCameraSystem.SetCameraDragging(CreateRuntimeCameraContext(), value),
                pointerPosition => IsPointerOverUI(pointerPosition, out _),
                pointerPosition => IsPointerOverGameplayUi(pointerPosition, out _),
                TryIssueAttackOrderToClickedUnit,
                TryIssueScanOrder,
                selectionOrderMarkerSystem,
                TryGetDefaultEntityManager,
                TryGetClickedCell,
                visible => selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), visible),
                TryIssueBoardTransportOrderToClickedUnit,
                QueueFocusUnitCommand,
                screenDelta => rtsSelectionRuntimeCameraSystem.PanCamera(CreateRuntimeCameraContext(), screenDelta),
                IssueMoveOrder,
                ProcessSelectionRectangleRequests,
                ClearSelectionCommandMode,
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                BuildClickDebugSummary,
                IsMatchIntroGameplayInputLocked);
        }

        RtsSelectionRuntimeCameraSystem.Context CreateRuntimeCameraContext()
        {
            return rtsSelectionRuntimeCameraContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                rtsCameraSystem,
                rtsCameraRequestSystem,
                runtimeConfig,
                mainMenuPlayUi,
                roadBuildReadState,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                TryGetDefaultEntityManager,
                IsPointerOverGameplayUi,
                UpdateLastKnownPointerPosition,
                HideOrderScreenMarkers);
        }

        RtsSelectionCommandResultFlushSystem.Context CreateCommandResultFlushContext()
        {
            return rtsSelectionCommandResultContextSystem.Create(
                rtsSelectionInputSystem,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                selectionOrderMarkerSystem,
                selectionMoveCommandRequestSystem,
                selectionAttackCommandRequestSystem,
                selectionScanCommandRequestSystem,
                selectionTransportCommandRequestSystem,
                selectedMoveOrderCommandSystem,
                attackOrderCommandSystem,
                scanIntelCommandSystem,
                transportBoardingCommandSystem,
                unitMoveOrderSystem,
                unitTargetOrderSystem,
                unitTransportCapacitySystem,
                unitTransportBoardingQuerySystem,
                unitTransportBoardingRuleSystem,
                unitTransportApproachCellSystem,
                unitTransportAirPickupSystem,
                unitTransportRopeDisembarkCommandSystem,
                selectionStateSystem,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                selectionRuntimeQuerySystem,
                TryGetDefaultEntityManager,
                EnsureRuntimeSelectionDependencies,
                ClearCurrentSelection,
                RequestMoveOrderScreenMarker,
                RequestAttackOrderScreenMarker,
                SetCameraDragging,
                focusedUnitLifecycleSystem.ClearFocusedUnit,
                TryGetClickedUnitEntity,
                TryGetClickedCell,
                TryGetClickedCell,
                TryGetClickedUnitEntity,
                TryGetClickedUnitEntity,
                TryGetClickedCell);
        }

        RtsSelectionFocusCommandSystem.Context CreateFocusCommandContext()
        {
            return rtsSelectionFocusCommandContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                unitTargetOrderSystem,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                runtimeConfig.WorldCamera,
                TryGetDefaultEntityManager,
                EnsureRuntimeSelectionDependencies,
                ClearCurrentSelection,
                QueueSelectionRectangleRequest,
                ProcessSelectionRectangleRequests,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                SetCameraDragging,
                value => explicitAttackTargetModeActive = value,
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                DescribeTransportBoardingEntity,
                ValidateControllableEntity,
                IssueHoldPositionOrder,
                IssueStopOrder,
                DestroyFocusedUnit,
                TryFocusUnitDirect,
                IssueFocusedMissileLauncherRadarAttack,
                ArmFocusedAttackTargetMode,
                CancelExplicitAttackTargetMode);
        }

        RtsSelectionPointerTargetCommandSystem.Context CreatePointerTargetCommandContext()
        {
            return rtsSelectionPointerTargetCommandContextSystem.Create(
                runtimeGameplayStateSystem,
                rtsSelectionInputSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                unitTargetOrderSystem,
                focusableUnitLookupSystem,
                transportBoardingCommandSystem,
                unitTransportCapacitySystem,
                unitTransportBoardingQuerySystem,
                unitTransportBoardingRuleSystem,
                unitTransportApproachCellSystem,
                unitTransportAirPickupSystem,
                unitTransportRopeDisembarkCommandSystem,
                buildingTargetMoveOrderSystem,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                runtimeConfig.WorldCamera,
                TryGetDefaultEntityManager,
                TryGetPointerPosition,
                () => explicitAttackTargetModeActive,
                value => explicitAttackTargetModeActive = value,
                selectionHudFeedbackSystem,
                CreateHudFeedbackContext(),
                ClearCurrentSelection,
                RequestMoveOrderScreenMarker,
                SetCameraDragging,
                ProcessAttackCommandRequests,
                ProcessScanCommandRequests,
                ProcessTransportCommandRequests,
                ProcessMoveCommandRequests,
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                DescribeTransportBoardingEntity);
        }

        SelectionHudFeedbackSystem.Context CreateHudFeedbackContext()
        {
            return new SelectionHudFeedbackSystem.Context(
                selectionUiQuerySystem,
                TryGetDefaultEntityManager,
                resolveSelectionPortraitSprite);
        }

        MatchHudSquadTraySelectionSystem.Context CreateSquadTraySelectionContext()
        {
            return new MatchHudSquadTraySelectionSystem.Context(
                runtimeConfig.WorldCamera,
                TryGetDefaultEntityManager,
                EnsureRuntimeSelectionDependencies,
                ClearCurrentSelection,
                () => buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                    buildingPlacementInteractionContext,
                    "MatchHudSquadTray"),
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity),
                selectedCount => selectionHudFeedbackSystem.ApplySquadSelection(CreateHudFeedbackContext(), selectedCount),
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic,
                selectionStateSystem,
                focusedUnitLifecycleSystem);
        }

        void UpdateAttackTargetPreviewMarkers()
        {
            if (!explicitAttackTargetModeActive)
            {
                selectionOrderMarkerSystem.UpdateAttackTargetPreviewMarkers(default, false);
                return;
            }

            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            EnsureRuntimeSelectionDependencies(em);
            selectionOrderMarkerSystem.UpdateAttackTargetPreviewMarkers(em, true);
        }

        bool TryGetDefaultEntityManager(out EntityManager em)
        {
            em = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            em = world.EntityManager;
            return true;
        }

        bool IsMatchIntroGameplayInputLocked()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            if (cachedMatchIntroWorld != world || !hasMatchIntroLockQuery)
            {
                cachedMatchIntroWorld = world;
                matchIntroLockQuery = world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                    ComponentType.ReadOnly<MatchIntroTransitionComponent>());
                hasMatchIntroLockQuery = true;
            }

            if (matchIntroLockQuery.IsEmptyIgnoreFilter)
                return false;

            MatchIntroTransitionComponent matchIntro =
                world.EntityManager.GetComponentData<MatchIntroTransitionComponent>(matchIntroLockQuery.GetSingletonEntity());
            return matchIntro.InputLocked != 0;
        }

        void EnsureRuntimeSelectionDependencies(EntityManager em)
        {
            selectionRuntimeQuerySystem.EnsureEntityQueries(em);
            focusableUnitLookupSystem.EnsureEntityQueries(em);
            visibleUnitSelectionSystem.EnsureEntityQueries(em);
            attackOrderCommandSystem.EnsureEntityQueries(em);
            selectionOrderMarkerSystem.EnsureEntityQueries(em);
            focusedUnitCommandSystem.EnsureEntityQueries(em);
            focusedUnitLifecycleSystem.EnsureEntityQueries(em);
            selectedUnitOrderSnapshotSystem.EnsureEntityQueries(em);
            buildingTargetMoveOrderSystem.EnsureEntityQueries(em);
            transportBoardingCommandSystem.EnsureEntityQueries(em);
        }

        void RefreshFocusedSelectionReadModels()
        {
            RefreshFocusedUnit();
            PublishFocusedUnitUiReadModel();
        }

        void PublishFocusedUnitUiReadModel()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            focusedUnitUiReadModelSystem.Publish(
                em,
                selectionStateSystem,
                selectionUiQuerySystem,
                unitTransportCapacitySystem,
                Time.time);
        }

        void RefreshFocusedUnit()
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return;

            EnsureRuntimeSelectionDependencies(em);
            focusedUnitLifecycleSystem.RefreshFocusedUnit(
                em,
                selectionStateSystem,
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity));
        }

        void ProcessSelectionRectangleRequests()
        {
            if (TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
                selectionHudFeedbackSystem.EnsureFeedbackQueue(defaultEntityManager);

            if (!rtsSelectionInputSystem.TryGetPointerRequests(out EntityManager em, out DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests))
                return;

            EnsureRuntimeSelectionDependencies(em);
            selectionRectangleRequestSystem.ProcessPendingRequests(
                em,
                pointerRequests,
                runtimeConfig.WorldCamera,
                selectionUiQuerySystem,
                visibleUnitSelectionSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                visibleSelectionScratch,
                ClearCurrentSelection,
                selectionStateSystem.CacheSelectedMoveEntities,
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity),
                selectedCount => selectionHudFeedbackSystem.ApplySquadSelection(CreateHudFeedbackContext(), selectedCount),
                selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
                ClearSelectedBuildingAfterRectangleSelection,
                screenRect => trySelectFirstBuildingInScreenRect != null &&
                    trySelectFirstBuildingInScreenRect(screenRect));
        }

        void ClearSelectedBuildingAfterRectangleSelection()
        {
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(buildingPlacementInteractionContext, "RTSSelection.SelectUnitsInRectangle");
        }

        void ClearSelectionCommandMode()
        {
            rtsSelectionInputSystem.ClearActiveCommandMode();
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            BattleHudRuntimeFeedbackView view = BattleHudRuntimeFeedbackSystem.ResolveActiveView();
            if (view == null ||
                BattleHudRuntimeFeedbackSystem.GetState(view).StickyCommandMode == TacticalCommandMode.None)
                matchOverlayCommandTabFeedbackSystem.ClearCommandMode(view != null ? view.CommandTabGroups : null);
        }

        void ClearCurrentSelection(EntityManager em, string reason = "Unspecified")
        {
            focusedUnitLifecycleSystem.ClearCurrentSelection(
                em,
                selectionStateSystem,
                reason,
                selectionRuntimeDiagnosticsSystem.EnqueueSelectionDiagnostic,
                () => selectionHudFeedbackSystem.ClearSelection(CreateHudFeedbackContext()));
        }

        void QueueSelectionRectangleRequest(
            Rect screenRect,
            RtsSelectionPointerRequestKind kind,
            VisibleUnitSelectionSystem.Filter filter = VisibleUnitSelectionSystem.Filter.All)
        {
            rtsSelectionInputSystem.QueueSelectionRectangleRequest(kind, screenRect, Time.frameCount, filter);
        }

        void IssueMoveOrder(Vector2 screenPosition)
        {
            rtsSelectionPointerTargetCommandSystem.IssueMoveOrder(CreatePointerTargetCommandContext(), screenPosition);
        }

        void ProcessMoveCommandRequests()
        {
            rtsSelectionCommandResultFlushSystem.ProcessMoveCommandRequests(CreateCommandResultFlushContext());
        }

        bool ProcessAttackCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessAttackCommandRequests(
                CreateCommandResultFlushContext(),
                explicitAttackTargetModeActive);
        }

        bool ProcessScanCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessScanCommandRequests(CreateCommandResultFlushContext());
        }

        bool ProcessTransportCommandRequests()
        {
            return rtsSelectionCommandResultFlushSystem.ProcessTransportCommandRequests(CreateCommandResultFlushContext());
        }

        bool TryIssueBoardTransportOrderToClickedUnit(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueBoardTransportOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool QueueFocusUnitCommand(Vector2 screenPosition)
        {
            if (!rtsSelectionInputSystem.QueueFocusUnitCommandRequest(screenPosition, Time.frameCount))
            {
                selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic($"focusCommandEnqueue result=False pos={screenPosition} frame={Time.frameCount}");
                return false;
            }

            bool processed = rtsSelectionFocusCommandSystem.ProcessExternalSelectionCommandRequests(CreateFocusCommandContext());
            selectionRuntimeDiagnosticsSystem.LogSelectionClickDiagnostic($"focusCommandProcessed result={processed} pos={screenPosition} frame={Time.frameCount}");
            return processed;
        }

        bool TryFocusUnitDirect(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryFocusUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryIssueAttackOrderToClickedUnit(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueAttackOrderToClickedUnit(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryIssueScanOrder(Vector2 screenPosition)
        {
            return rtsSelectionPointerTargetCommandSystem.TryIssueScanOrder(CreatePointerTargetCommandContext(), screenPosition);
        }

        bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
        {
            return rtsSelectionPointerTargetCommandSystem.TryGetClickedCell(
                CreatePointerTargetCommandContext(),
                screenPosition,
                em,
                out cell,
                out worldPoint);
        }

        bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
        {
            return rtsSelectionPointerTargetCommandSystem.TryGetClickedUnitEntity(
                CreatePointerTargetCommandContext(),
                screenPosition,
                em,
                out bestEntity);
        }

        string BuildClickDebugSummary(Vector2 screenPosition)
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return "world=missing";

            EnsureRuntimeSelectionDependencies(em);
            string clickedCell = TryGetClickedCell(screenPosition, em, out int2 cell, out Vector3 worldPoint)
                ? $"{cell}@{worldPoint.x:F1},{worldPoint.y:F1},{worldPoint.z:F1}"
                : "none";
            string focused = DescribeClickDebugEntity(em, selectionStateSystem.FocusedUnit);
            List<Entity> cached = selectionStateSystem.CachedSelectedMoveEntities;
            string selected0 = cached.Count > 0 ? DescribeClickDebugEntity(em, cached[0]) : "none";
            int selectedTagCount = CountSelectedTags(em);
            return $"clickedCell={clickedCell} focused={focused} cachedCount={cached.Count} selectedTags={selectedTagCount} selected0={selected0} suppress={runtimeGameplayStateSystem.SuppressNextWorldClick} ignoreUntil={rtsSelectionInputSystem.IgnoreWorldCommandsUntilFrame}";
        }

        int CountSelectedTags(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            return query.CalculateEntityCount();
        }

        string DescribeClickDebugEntity(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return "null";

            string source = em.HasComponent<UnitSourcePrefabKey>(entity)
                ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
                : em.GetName(entity);
            byte faction = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
            string grid = em.HasComponent<UnitGrid>(entity) ? em.GetComponentData<UnitGrid>(entity).Cell.ToString() : "none";
            string target = em.HasComponent<UnitTarget>(entity) ? em.GetComponentData<UnitTarget>(entity).Cell.ToString() : "none";
            string pathRequest = em.HasComponent<UnitPathRequest>(entity) ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString() : "none";
            bool selected = em.HasComponent<SelectedUnitTag>(entity);
            bool pathFollow = em.HasComponent<UnitPathFollow>(entity);
            bool manual = em.HasComponent<ManualMoveOrderTag>(entity);
            bool engage = em.HasComponent<EngageTarget>(entity);
            return $"{entity}/{source}/faction={faction}/selected={selected}/grid={grid}/target={target}/pathRequest={pathRequest}/pathFollow={pathFollow}/manual={manual}/engage={engage}";
        }

        bool TryGetFocusedUnitEntity(out EntityManager em, out Entity entity)
        {
            em = default;
            entity = Entity.Null;
            if (!TryGetDefaultEntityManager(out em))
                return false;

            return focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out entity);
        }

        bool FocusedUnitOwnedByPlayer()
        {
            return TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) &&
                   selectionUiQuerySystem.IsOwnedByPlayer(em, entity);
        }

        bool FocusedUnitCanAttack()
        {
            return TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) &&
                   selectionUiQuerySystem.CanAttack(em, entity);
        }

        void DestroyFocusedUnit()
        {
            if (!TryGetFocusedUnitEntity(out EntityManager em, out Entity entity) || !FocusedUnitOwnedByPlayer())
                return;

            focusedUnitCommandSystem.DestroyFocusedUnit(em, entity);
            focusedUnitLifecycleSystem.ClearFocusedUnit(selectionStateSystem);
        }

        bool IssueFocusedMissileLauncherRadarAttack()
        {
            if (!TryGetFocusedUnitEntity(out EntityManager em, out Entity launcher) || !FocusedUnitOwnedByPlayer())
                return false;
            if (!focusedUnitCommandSystem.TryIssueFocusedMissileLauncherRadarAttack(
                    em,
                    launcher,
                    unitTargetOrderSystem,
                    out float3 targetPosition))
            {
                return false;
            }

            selectionOrderMarkerSystem.ShowAttackOrderMarker(em, targetPosition);
            ClearCurrentSelection(em, "MissileLauncherRadarAttack");
            focusedUnitLifecycleSystem.SetFocusedUnit(selectionStateSystem, launcher);
            explicitAttackTargetModeActive = false;
            SetCameraDragging(false);
            selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success());
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), em, launcher);
            return true;
        }

        bool ArmFocusedAttackTargetMode()
        {
            bool hasFocusedUnit = TryGetFocusedUnitEntity(out _, out _);
            if (!hasFocusedUnit || !FocusedUnitOwnedByPlayer() || !FocusedUnitCanAttack())
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(
                    hasFocusedUnit ? TacticalCommandReasonCode.TargetNotAttackable : TacticalCommandReasonCode.NoSelection));
                return false;
            }

            explicitAttackTargetModeActive = true;
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), TacticalCommandMode.Attack);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), true);
            runtimeGameplayStateSystem.SelectionModeActive = false;
            runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            rtsSelectionInputSystem.IsDraggingSelection = false;
            SetCameraDragging(false);
            rtsSelectionInputSystem.SkipNextWorldReleaseAfterSelection = true;
            return true;
        }

        void CancelExplicitAttackTargetMode()
        {
            explicitAttackTargetModeActive = false;
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
        }

        void IssueHoldPositionOrder()
        {
            IssueImmediateSelectedUnitOrder(TacticalCommandMode.Hold, clearEngageTarget: true, holdPosition: true);
        }

        void IssueStopOrder()
        {
            IssueImmediateSelectedUnitOrder(TacticalCommandMode.Stop, clearEngageTarget: true, holdPosition: false);
        }

        bool IssueImmediateSelectedUnitOrder(TacticalCommandMode mode, bool clearEngageTarget, bool holdPosition)
        {
            selectionHudFeedbackSystem.ApplyCommandMode(CreateHudFeedbackContext(), mode);

            if (!TryGetDefaultEntityManager(out EntityManager em))
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                return false;
            }

            bool issued = focusedUnitCommandSystem.IssueImmediateSelectedUnitOrder(
                em,
                clearEngageTarget,
                holdPosition,
                unitMoveOrderSystem);
            if (!issued)
            {
                selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
                return false;
            }

            explicitAttackTargetModeActive = false;
            rtsSelectionInputSystem.ClearActiveCommandMode();
            rtsSelectionInputSystem.ClearQueuedMoveOrder();
            rtsSelectionInputSystem.ClearPendingMoveCommandRequests();
            runtimeGameplayStateSystem.SelectionModeActive = false;
            runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            rtsSelectionInputSystem.IsDraggingSelection = false;
            buildingPlacementInteractionSystem?.ExitBuildMode(buildingPlacementInteractionContext);
            buildingPlacementInteractionSystem?.CancelBuildingPlacement(buildingPlacementInteractionContext);
            buildingPlacementInteractionSystem?.ClearSelectedBuilding(
                buildingPlacementInteractionContext,
                $"SelectionUiCommandSystem.{mode}");
            SetCameraDragging(false);
            selectionHudFeedbackSystem.SetWorldMarkersVisible(CreateHudFeedbackContext(), false);
            selectionHudFeedbackSystem.ApplyCommandResult(CreateHudFeedbackContext(), TacticalCommandResult.Success());
            selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext());
            focusedUnitLifecycleSystem.RefreshFocusedUnit(
                em,
                selectionStateSystem,
                (entityManager, entity) => selectionHudFeedbackSystem.ApplySelection(CreateHudFeedbackContext(), entityManager, entity));
            return true;
        }

        TacticalCommandResult ValidateControllableEntity(Entity entity)
        {
            if (entity == Entity.Null || !TryGetDefaultEntityManager(out EntityManager em))
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

            if (!em.Exists(entity) || !em.HasComponent<Faction>(entity) || !em.HasComponent<UnitMove>(entity))
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
            if (!FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
            if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

            return TacticalCommandResult.Success();
        }

        void SetCameraDragging(bool isDragging)
        {
            rtsSelectionRuntimeCameraSystem.SetCameraDragging(CreateRuntimeCameraContext(), isDragging);
        }

        void HideOrderScreenMarkers()
        {
            selectionScreenMarkers.RequestHideOrderMarkers();
        }

        void RequestMoveOrderScreenMarker(Vector2 screenPosition)
        {
            selectionScreenMarkers.RequestMoveOrderMarker(screenPosition);
        }

        void RequestAttackOrderScreenMarker(Vector2 screenPosition)
        {
            selectionScreenMarkers.RequestAttackOrderMarker(screenPosition);
        }

        void UpdateLastKnownPointerPosition(Vector2 pointerPosition)
        {
            rtsSelectionInputSystem.UpdateLastKnownPointerPosition(pointerPosition);
        }

        bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
            if (GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            {
                pointerPosition = pointer.Position;
                UpdateLastKnownPointerPosition(pointerPosition);
                return true;
            }

            return rtsSelectionInputSystem.TryGetLastKnownPointerPosition(out pointerPosition);
        }

        bool IsPointerOverGameplayUi(Vector2 screenPosition, out string source)
        {
            if (mainMenuPlayUi != null)
                return mainMenuPlayUi.IsPointerOverAnyGameplayUi(screenPosition, out source);

            return IsPointerOverUI(screenPosition, out source);
        }
    }

    private static bool IsPointerOverUI(Vector2 screenPosition, out string source)
    {
        source = null;
        return false;
    }

    private static string ResolveUnitSourceName(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity))
            return string.Empty;

        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(sourceName))
                return sourceName;
        }

        return em.GetName(entity);
    }

    private static string DescribeTransportBoardingEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null)
            return "null";
        if (!em.Exists(entity))
            return $"{entity}:missing";

        string sourceName = ResolveUnitSourceName(em, entity);
        if (string.IsNullOrWhiteSpace(sourceName))
            sourceName = "<unnamed>";

        string cell = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "no-cell";
        string faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id.ToString()
            : "no-faction";
        string health = em.HasComponent<UnitHealth>(entity)
            ? $"{em.GetComponentData<UnitHealth>(entity).Current}/{em.GetComponentData<UnitHealth>(entity).Max}"
            : "no-health";
        string capacity = em.HasComponent<UnitTransportCapacity>(entity)
            ? em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity.ToString()
            : "no-capacity";
        string passengers = em.HasBuffer<UnitTransportPassengerElement>(entity)
            ? em.GetBuffer<UnitTransportPassengerElement>(entity).Length.ToString()
            : "no-passengers";

        return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health} seats={passengers}/{capacity}";
    }

    private static SelectionRectangleView EnsureSelectionRectangleView(
        Transform runtimeUiRoot,
        RTSSelectionSystemConfig rtsSelectionConfig)
    {
        if (runtimeUiRoot == null)
            return null;

        SelectionRectangleView view = runtimeUiRoot.GetComponent<SelectionRectangleView>();
        if (view == null)
            view = runtimeUiRoot.gameObject.AddComponent<SelectionRectangleView>();

        view.ApplyConfig(rtsSelectionConfig);
        return view;
    }
}
