using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class UiShellEcsGateway : IUiShellRuntimeGateway
{
    private static readonly UiShellEcsGateway Shared = new();
    private static World cachedWorld;
    private static EntityQuery boundaryQuery;
    private static EntityQuery focusedSelectionQuery;
    private static EntityQuery selectionInputQuery;
    private static EntityQuery selectedUnitsQuery;
    private static EntityQuery minimapMarkerQuery;
    private static EntityQuery gridConfigQuery;
    private static bool hasBoundaryQuery;
    private static bool hasFocusedSelectionQuery;
    private static bool hasSelectionInputQuery;
    private static bool hasSelectedUnitsQuery;
    private static bool hasMinimapMarkerQuery;
    private static bool hasGridConfigQuery;

    private UiShellEcsGateway()
    {
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RegisterAsRuntimeGateway()
    {
        cachedWorld = null;
        boundaryQuery = default;
        focusedSelectionQuery = default;
        selectionInputQuery = default;
        hasBoundaryQuery = false;
        hasFocusedSelectionQuery = false;
        hasSelectionInputQuery = false;
        hasSelectedUnitsQuery = false;
        hasMinimapMarkerQuery = false;
        hasGridConfigQuery = false;
        UiShellRuntimeGateway.Register(Shared);
    }

    public static bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        DynamicBuffer<UiShellRouteRequestComponent> requests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        requests.Add(new UiShellRouteRequestComponent
        {
            Intent = intent,
            Route = route,
            PushHistory = pushHistory ? (byte)1 : (byte)0
        });
        return true;
    }

    public static bool TryEnqueueUiAction(UiActionKind kind, int payloadId)
    {
        if (kind == UiActionKind.None ||
            !TryGetBoundary(out EntityManager entityManager, out Entity boundary))
        {
            return false;
        }

        EnsureUiActionRequestBuffer(entityManager, boundary);
        DynamicBuffer<UiActionRequestComponent> requests =
            entityManager.GetBuffer<UiActionRequestComponent>(boundary);
        requests.Add(new UiActionRequestComponent
        {
            Kind = kind,
            PayloadId = payloadId
        });
        return true;
    }

    public static bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
    {
        loading = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasComponent<UiShellLoadingProgressComponent>(boundary))
            return false;

        UiShellLoadingProgressComponent component =
            entityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
        loading = new UiShellLoadingProgressModel(
            component.Progress01,
            component.Status.ToString(),
            component.IsComplete != 0);
        return true;
    }

    public static bool TrySetLoadingProgress(float progress01, string status, bool complete)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = Mathf.Clamp01(progress01),
            Status = new FixedString64Bytes(status ?? string.Empty),
            IsComplete = complete ? (byte)1 : (byte)0
        });
        return true;
    }

    public static bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics)
    {
        diagnostics = UiDiagnosticsOverlayModel.Default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureDiagnosticsOverlayState(entityManager, boundary);
        UiDiagnosticsOverlayComponent component =
            entityManager.GetComponentData<UiDiagnosticsOverlayComponent>(boundary);
        diagnostics = new UiDiagnosticsOverlayModel(
            Mathf.Max(0, component.Fps),
            component.LogVisible != 0,
            component.LogText.ToString());
        return true;
    }

    public static bool TryReadShellState(out UiShellStateModel state)
    {
        state = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasComponent<UiShellStateComponent>(boundary))
            return false;

        UiShellStateComponent component = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        state = new UiShellStateModel(
            component.CurrentMode,
            component.ActiveRoute,
            component.Phase,
            component.TransitionSequenceId,
            component.IsTransitionRunning != 0);
        return true;
    }

    public static bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
    {
        profile = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureCommanderProfileState(entityManager, boundary);
        UiShellCommanderProfileComponent component =
            entityManager.GetComponentData<UiShellCommanderProfileComponent>(boundary);
        profile = new UiShellCommanderProfileModel(
            component.Name.ToString(),
            component.Subtitle.ToString(),
            component.PortraitClass.ToString());
        return true;
    }

    public static bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)
    {
        resources = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureMainMenuResourcesState(entityManager, boundary);
        UiShellMainMenuResourcesComponent component =
            entityManager.GetComponentData<UiShellMainMenuResourcesComponent>(boundary);
        resources = new UiShellMainMenuResourcesModel(
            component.CreditsText.ToString(),
            component.SuppliesText.ToString(),
            component.CommandText.ToString());
        return true;
    }

    public static bool TryReadMissionResult(out UiMissionResultPopupModel result)
    {
        result = UiMissionResultPopupModel.VictoryDefault;
        return false;
    }

    public static bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection)
    {
        selection = UiMatchHudSelectionPanelModel.Hidden;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world)
        {
            cachedWorld = world;
            hasBoundaryQuery = false;
            hasFocusedSelectionQuery = false;
            hasSelectionInputQuery = false;
            hasSelectedUnitsQuery = false;
            hasMinimapMarkerQuery = false;
            hasGridConfigQuery = false;
        }

        if (!hasFocusedSelectionQuery)
        {
            focusedSelectionQuery =
                world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
            hasFocusedSelectionQuery = true;
        }

        if (focusedSelectionQuery.IsEmptyIgnoreFilter)
            return TryBuildSelectedGroupModel(world.EntityManager, out selection);

        FocusedUnitUiReadModelComponent component =
            focusedSelectionQuery.GetSingleton<FocusedUnitUiReadModelComponent>();
        if (component.HasFocusedUnit == 0)
            return TryBuildSelectedGroupModel(world.EntityManager, out selection);

        string title = component.Label.ToString();
        if (string.IsNullOrWhiteSpace(title))
            title = "SELECTED UNIT";

        string subtitle = component.Description.ToString();
        if (string.IsNullOrWhiteSpace(subtitle))
            subtitle = component.IsVehicle != 0 ? "VEHICLE" : "TACTICAL ASSET";

        string order = ToSelectionOrderText(component.Status);
        string healthText = component.HealthText.ToString();
        if (string.IsNullOrWhiteSpace(healthText))
        {
            healthText = component.HasHealth != 0 && component.HealthMax > 0
                ? $"{component.HealthCurrent} / {component.HealthMax}"
                : "HEALTH -";
        }

        float health01 = component.HasHealth != 0 && component.HealthMax > 0
            ? Mathf.Clamp01((float)component.HealthCurrent / component.HealthMax)
            : 0f;

        bool owned = component.OwnedByPlayer != 0;
        selection = new UiMatchHudSelectionPanelModel(
            true,
            title,
            subtitle,
            order,
            healthText,
            health01,
            component.IsVehicle == 0,
            owned,
            owned,
            ResolveBoardEnabled(world.EntityManager, component.FocusedUnit));
        return true;
    }

    private static bool TryBuildSelectedGroupModel(EntityManager entityManager, out UiMatchHudSelectionPanelModel selection)
    {
        selection = UiMatchHudSelectionPanelModel.Hidden;
        EnsureSelectedUnitsQuery(entityManager);
        if (selectedUnitsQuery.IsEmptyIgnoreFilter)
            return true;

        SelectedGroupSummary summary = BuildSelectedGroupSummary(entityManager);
        if (summary.SelectedCount <= 0)
            return true;

        selection = new UiMatchHudSelectionPanelModel(
            true,
            summary.Title,
            summary.Subtitle,
            summary.OrderText,
            string.IsNullOrWhiteSpace(summary.HealthText) ? "-" : summary.HealthText,
            summary.Health01,
            false,
            true,
            true,
            ResolveSelectedBoardEnabled(entityManager));
        return true;
    }

    private static SelectedGroupSummary BuildSelectedGroupSummary(EntityManager entityManager)
    {
        SelectedGroupSummary summary = new();
        EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = selectedUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!entityManager.Exists(entity))
                    continue;

                summary.SelectedCount++;
                bool vehicle = IsVehicleUnit(entityManager, entity);
                bool aircraft = entityManager.HasComponent<UnitAirComponent>(entity) ||
                                entityManager.HasComponent<UnitAirMovement>(entity);
                if (aircraft)
                    summary.AircraftCount++;
                else if (vehicle)
                    summary.VehicleCount++;
                else
                    summary.SoldierCount++;

                if (entityManager.HasComponent<UnitHealth>(entity))
                {
                    UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
                    summary.HealthCurrent += math.max(0, health.Current);
                    summary.HealthMax += math.max(0, health.Max);
                }

                string order = ResolveEntityOrderText(entityManager, entity);
                if (summary.OrderText == null)
                    summary.OrderText = order;
                else if (summary.OrderText != order)
                    summary.MixedOrders = true;
            }
        }

        summary.OrderText = summary.MixedOrders ? "Mixed orders" : summary.OrderText ?? "Idle";
        if (summary.HealthMax > 0)
        {
            summary.Health01 = Mathf.Clamp01((float)summary.HealthCurrent / summary.HealthMax);
            summary.HealthText = $"{summary.HealthCurrent} / {summary.HealthMax}";
        }
        else
        {
            summary.Health01 = 0f;
            summary.HealthText = "HEALTH -";
        }

        if (summary.SelectedCount == summary.SoldierCount)
        {
            summary.Title = summary.SelectedCount == 1 ? "SOLDIER" : $"{summary.SelectedCount} SOLDIERS";
            summary.Subtitle = "INFANTRY GROUP";
        }
        else if (summary.SelectedCount == summary.VehicleCount)
        {
            summary.Title = summary.SelectedCount == 1 ? "VEHICLE" : $"{summary.SelectedCount} VEHICLES";
            summary.Subtitle = "ARMORED GROUP";
        }
        else if (summary.SelectedCount == summary.AircraftCount)
        {
            summary.Title = summary.SelectedCount == 1 ? "AIRCRAFT" : $"{summary.SelectedCount} AIRCRAFT";
            summary.Subtitle = "AIR GROUP";
        }
        else
        {
            summary.Title = $"{summary.SelectedCount} SELECTED";
            summary.Subtitle = "MIXED GROUP";
        }

        return summary;
    }

    private static string ResolveEntityOrderText(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<UnitTransportBoardingTarget>(entity))
            return "Boarding transport";
        if (entityManager.HasComponent<EngageTarget>(entity))
            return "Engaging target";
        if (entityManager.HasComponent<ManualMoveOrderTag>(entity) ||
            entityManager.HasComponent<ManualMoveGroupMemberTag>(entity))
        {
            return "Moving";
        }

        if (entityManager.HasComponent<HoldPositionOrderTag>(entity))
            return "Holding";
        return "Idle";
    }

    private static void EnsureSelectedUnitsQuery(EntityManager entityManager)
    {
        if (hasSelectedUnitsQuery && cachedWorld == entityManager.World)
            return;

        selectedUnitsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        hasSelectedUnitsQuery = true;
    }

    private static bool ResolveSelectedBoardEnabled(EntityManager entityManager)
    {
        EnsureSelectedUnitsQuery(entityManager);
        if (selectedUnitsQuery.IsEmptyIgnoreFilter)
            return false;

        EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = selectedUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                if (ResolveBoardEnabled(entityManager, entities[i]))
                    return true;
            }
        }

        return false;
    }

    private static bool ResolveBoardEnabled(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.Exists(entity) ||
            !entityManager.HasComponent<Faction>(entity) ||
            !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id))
        {
            return false;
        }

        if (entityManager.HasComponent<UnitTransportPassenger>(entity) ||
            entityManager.HasComponent<UnitTransportCargoPassenger>(entity))
        {
            return false;
        }

        if (IsTransportWithOpenCapacity(entityManager, entity))
            return true;

        return IsSoldierBoardingCandidate(entityManager, entity);
    }

    private static bool IsSoldierBoardingCandidate(EntityManager entityManager, Entity entity)
    {
        return entityManager.HasComponent<UnitMove>(entity) &&
               !IsVehicleUnit(entityManager, entity) &&
               !entityManager.HasComponent<UnitAirComponent>(entity) &&
               !entityManager.HasComponent<UnitAirMovement>(entity);
    }

    private static bool IsTransportWithOpenCapacity(EntityManager entityManager, Entity entity)
    {
        int capacity = 0;
        if (entityManager.HasComponent<UnitTransportCapacity>(entity))
            capacity += math.max(0, entityManager.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity);
        if (entityManager.HasComponent<UnitTransportCargoCapacity>(entity))
        {
            UnitTransportCargoCapacity cargoCapacity = entityManager.GetComponentData<UnitTransportCargoCapacity>(entity);
            capacity += math.max(0, cargoCapacity.SoldierCapacity) + math.max(0, cargoCapacity.VehicleCapacity);
        }

        if (capacity <= 0)
            return false;

        int occupied = entityManager.HasBuffer<UnitTransportPassengerElement>(entity)
            ? entityManager.GetBuffer<UnitTransportPassengerElement>(entity, true).Length
            : 0;
        return occupied < capacity;
    }

    private static bool IsVehicleUnit(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<UnitFootprint>(entity) ||
            !entityManager.HasComponent<UnitMovementBehavior>(entity))
        {
            return false;
        }

        return UnitVehicleMovementUtility.IsVehicle(
            entityManager.GetComponentData<UnitFootprint>(entity),
            entityManager.GetComponentData<UnitMovementBehavior>(entity));
    }

    private struct SelectedGroupSummary
    {
        public int SelectedCount;
        public int SoldierCount;
        public int VehicleCount;
        public int AircraftCount;
        public int HealthCurrent;
        public int HealthMax;
        public bool MixedOrders;
        public string Title;
        public string Subtitle;
        public string OrderText;
        public string HealthText;
        public float Health01;
    }

    public static bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState)
    {
        commandState = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        TacticalCommandMode activeCommandMode = TacticalCommandMode.None;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            if (!hasSelectionInputQuery)
            {
                selectionInputQuery =
                    world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RtsSelectionInputStateComponent>());
                hasSelectionInputQuery = true;
            }

            if (!selectionInputQuery.IsEmptyIgnoreFilter)
            {
                RtsSelectionInputStateComponent inputState =
                    selectionInputQuery.GetSingleton<RtsSelectionInputStateComponent>();
                activeCommandMode = (TacticalCommandMode)inputState.ActiveCommandMode;
            }
        }

        bool buildDrawerVisible = false;
        if (entityManager.HasComponent<UiShellActivePopupComponent>(boundary))
        {
            UiShellActivePopupComponent activePopup =
                entityManager.GetComponentData<UiShellActivePopupComponent>(boundary);
            buildDrawerVisible =
                activePopup.Visible != 0 &&
                activePopup.PopupKind == UiShellPopupKind.BuildDrawer;
        }

        commandState = new UiMatchHudCommandStateModel(activeCommandMode, buildDrawerVisible);
        return true;
    }

    public static bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer)
    {
        passengerDrawer = UiMatchHudPassengerDrawerModel.Hidden;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (!hasFocusedSelectionQuery)
        {
            focusedSelectionQuery =
                world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
            hasFocusedSelectionQuery = true;
        }

        if (focusedSelectionQuery.IsEmptyIgnoreFilter)
            return true;

        Entity focusedEntity = focusedSelectionQuery.GetSingletonEntity();
        FocusedUnitUiReadModelComponent selection =
            world.EntityManager.GetComponentData<FocusedUnitUiReadModelComponent>(focusedEntity);
        if (selection.HasFocusedUnit == 0 || selection.TransportPassengerCapacity <= 0)
            return true;

        bool drawerVisible = false;
        if (entityManager.HasComponent<UiMatchHudPassengerDrawerStateComponent>(boundary))
        {
            UiMatchHudPassengerDrawerStateComponent drawerState =
                entityManager.GetComponentData<UiMatchHudPassengerDrawerStateComponent>(boundary);
            drawerVisible = drawerState.Visible != 0;
        }

        int passengerCount = Mathf.Max(0, selection.PassengerCount);
        int capacity = Mathf.Max(0, selection.TransportPassengerCapacity);
        UiMatchHudPassengerRowModel row0 = default;
        UiMatchHudPassengerRowModel row1 = default;
        UiMatchHudPassengerRowModel row2 = default;
        int rowCount = 0;

        if (world.EntityManager.HasBuffer<FocusedUnitPassengerUiReadModelElement>(focusedEntity))
        {
            DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers =
                world.EntityManager.GetBuffer<FocusedUnitPassengerUiReadModelElement>(focusedEntity, true);
            int limit = Mathf.Min(passengers.Length, UiMatchHudPassengerDrawerModel.MaxRows);
            for (int i = 0; i < limit; i++)
            {
                UiMatchHudPassengerRowModel row = ToPassengerRow(passengers[i]);
                switch (i)
                {
                    case 0:
                        row0 = row;
                        break;
                    case 1:
                        row1 = row;
                        break;
                    case 2:
                        row2 = row;
                        break;
                }
            }

            rowCount = limit;
        }

        passengerDrawer = new UiMatchHudPassengerDrawerModel(
            true,
            drawerVisible,
            passengerCount,
            capacity,
            rowCount,
            row0,
            row1,
            row2);
        return true;
    }

    public static bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray)
    {
        squadTray = UiMatchHudSquadTrayModel.Default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        MatchHudSquadTraySlot selectedSlot = MatchHudSquadTraySlot.None;
        if (entityManager.HasComponent<UiMatchHudSquadTrayStateComponent>(boundary))
        {
            UiMatchHudSquadTrayStateComponent state =
                entityManager.GetComponentData<UiMatchHudSquadTrayStateComponent>(boundary);
            selectedSlot = state.SelectedSlot;
        }

        UiMatchHudSquadTrayModel defaults = UiMatchHudSquadTrayModel.Default;
        squadTray = new UiMatchHudSquadTrayModel(
            selectedSlot,
            defaults.Card0,
            defaults.Card1,
            defaults.Card2,
            defaults.Card3,
            defaults.Card4);
        return true;
    }

    public static bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header)
    {
        header = UiMatchHudHeaderModel.Default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureMatchHudHeaderState(entityManager, boundary);
        UiMatchHudHeaderComponent component = entityManager.GetComponentData<UiMatchHudHeaderComponent>(boundary);
        header = new UiMatchHudHeaderModel(
            component.OrderText.ToString(),
            component.SquadText.ToString(),
            component.CreditsText.ToString(),
            component.FuelText.ToString(),
            component.SupplyText.ToString(),
            component.CivilianRiskText.ToString());
        return true;
    }

    public static bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
    {
        statusSurfaces = UiMatchHudStatusSurfacesModel.Default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureMatchHudStatusSurfacesState(entityManager, boundary);
        UiMatchHudStatusSurfacesComponent component =
            entityManager.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
        statusSurfaces = new UiMatchHudStatusSurfacesModel(
            component.ObjectivesTitle.ToString(),
            new UiMatchHudObjectiveRowModel(component.Objective0Text.ToString(), component.Objective0IconKind),
            new UiMatchHudObjectiveRowModel(component.Objective1Text.ToString(), component.Objective1IconKind),
            new UiMatchHudObjectiveRowModel(component.Objective2Text.ToString(), component.Objective2IconKind),
            component.ElapsedText.ToString(),
            component.ThreatVisible != 0,
            component.ThreatTitle.ToString(),
            component.ThreatSubtitle.ToString(),
            component.JumpEnabled != 0,
            component.FeedbackVisible != 0,
            component.FeedbackText.ToString(),
            component.BoardAllVisible != 0,
            component.BoardAllEnabled != 0,
            component.CancelVisible != 0,
            component.CancelEnabled != 0);
        return true;
    }

    public static bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
    {
        minimap = UiMatchHudMinimapModel.Default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureMatchHudMinimapState(entityManager, boundary);
        UiMatchHudMinimapComponent component = entityManager.GetComponentData<UiMatchHudMinimapComponent>(boundary);
        UiMatchHudMinimapMarkerModel friendlyA = default;
        UiMatchHudMinimapMarkerModel friendlyB = default;
        UiMatchHudMinimapMarkerModel hostileA = default;
        UiMatchHudMinimapMarkerModel neutral = default;
        bool hasRuntimeMarkers = TryReadRuntimeMinimapMarkers(
            out friendlyA,
            out friendlyB,
            out hostileA,
            out neutral);

        minimap = new UiMatchHudMinimapModel(
            component.ViewportLeftPercent,
            component.ViewportTopPercent,
            component.ViewportWidthPercent,
            component.ViewportHeightPercent,
            component.ZoomInEnabled != 0,
            component.ZoomOutEnabled != 0,
            component.FocusEnabled != 0,
            hasRuntimeMarkers
                ? friendlyA
                : new UiMatchHudMinimapMarkerModel(false, component.FriendlyALeftPercent, component.FriendlyATopPercent),
            hasRuntimeMarkers
                ? friendlyB
                : new UiMatchHudMinimapMarkerModel(false, component.FriendlyBLeftPercent, component.FriendlyBTopPercent),
            hasRuntimeMarkers
                ? hostileA
                : new UiMatchHudMinimapMarkerModel(false, component.HostileALeftPercent, component.HostileATopPercent),
            hasRuntimeMarkers
                ? neutral
                : new UiMatchHudMinimapMarkerModel(false, component.CivilianLeftPercent, component.CivilianTopPercent));
        return true;
    }

    private static bool TryReadRuntimeMinimapMarkers(
        out UiMatchHudMinimapMarkerModel friendlyA,
        out UiMatchHudMinimapMarkerModel friendlyB,
        out UiMatchHudMinimapMarkerModel hostileA,
        out UiMatchHudMinimapMarkerModel neutral)
    {
        friendlyA = default;
        friendlyB = default;
        hostileA = default;
        neutral = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        EnsureMinimapMarkerQuery(entityManager);
        EnsureGridConfigQuery(entityManager);
        if (minimapMarkerQuery.IsEmptyIgnoreFilter || gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        Entity markerEntity = minimapMarkerQuery.GetSingletonEntity();
        DynamicBuffer<MatchHudMinimapMarkerElement> markers =
            entityManager.GetBuffer<MatchHudMinimapMarkerElement>(markerEntity, true);
        if (markers.Length == 0)
            return false;

        GridConfig grid = gridConfigQuery.GetSingleton<GridConfig>();
        bool hasFriendlyA = false;
        bool hasFriendlyB = false;
        bool hasHostileA = false;
        bool hasNeutral = false;
        for (int i = 0; i < markers.Length; i++)
        {
            MatchHudMinimapMarkerElement marker = markers[i];
            UiMatchHudMinimapMarkerModel model = ToMinimapMarkerModel(marker.Position, grid);
            if (FactionIdentity.IsPlayerControlled(marker.FactionId))
            {
                if (!hasFriendlyA)
                {
                    friendlyA = model;
                    hasFriendlyA = true;
                }
                else if (!hasFriendlyB)
                {
                    friendlyB = model;
                    hasFriendlyB = true;
                }
            }
            else if (FactionIdentity.IsHostileToPlayer(marker.FactionId))
            {
                if (!hasHostileA)
                {
                    hostileA = model;
                    hasHostileA = true;
                }
            }
            else if (!hasNeutral)
            {
                neutral = model;
                hasNeutral = true;
            }

            if (hasFriendlyA && hasFriendlyB && hasHostileA && hasNeutral)
                break;
        }

        return hasFriendlyA || hasFriendlyB || hasHostileA || hasNeutral;
    }

    private static void EnsureMinimapMarkerQuery(EntityManager entityManager)
    {
        if (hasMinimapMarkerQuery && cachedWorld == entityManager.World)
            return;

        minimapMarkerQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MatchHudMinimapMarkerBoundary>(),
            ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
        hasMinimapMarkerQuery = true;
    }

    private static void EnsureGridConfigQuery(EntityManager entityManager)
    {
        if (hasGridConfigQuery && cachedWorld == entityManager.World)
            return;

        gridConfigQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        hasGridConfigQuery = true;
    }

    private static UiMatchHudMinimapMarkerModel ToMinimapMarkerModel(float3 worldPosition, GridConfig grid)
    {
        float width = math.max(1f, grid.Width * grid.CellSize);
        float height = math.max(1f, grid.Height * grid.CellSize);
        float left = math.saturate((worldPosition.x - grid.Origin.x) / width) * 100f;
        float top = (1f - math.saturate((worldPosition.z - grid.Origin.z) / height)) * 100f;
        return new UiMatchHudMinimapMarkerModel(true, left, top);
    }

    public static bool TryReadBuildDrawer(out UiBuildDrawerModel drawer)
    {
        drawer = UiBuildDrawerModel.Empty;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureBuildDrawerState(entityManager, boundary);
        UiBuildDrawerDetailComponent detail = entityManager.GetComponentData<UiBuildDrawerDetailComponent>(boundary);
        UiBuildDrawerActiveProductionComponent active =
            entityManager.GetComponentData<UiBuildDrawerActiveProductionComponent>(boundary);
        DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
            entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary, true);
        DynamicBuffer<UiBuildDrawerQueueRowComponent> queue =
            entityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary, true);

        UiBuildDrawerCatalogItemModel catalog0 = default;
        UiBuildDrawerCatalogItemModel catalog1 = default;
        UiBuildDrawerCatalogItemModel catalog2 = default;
        UiBuildDrawerCatalogItemModel catalog3 = default;
        UiBuildDrawerCatalogItemModel catalog4 = default;
        UiBuildDrawerCatalogItemModel catalog5 = default;
        UiBuildDrawerCatalogItemModel catalog6 = default;
        int catalogCount = Mathf.Min(catalog.Length, UiBuildDrawerModel.MaxCatalogItems);
        for (int i = 0; i < catalogCount; i++)
        {
            UiBuildDrawerCatalogItemModel item = ToBuildDrawerCatalogItem(catalog[i]);
            switch (i)
            {
                case 0:
                    catalog0 = item;
                    break;
                case 1:
                    catalog1 = item;
                    break;
                case 2:
                    catalog2 = item;
                    break;
                case 3:
                    catalog3 = item;
                    break;
                case 4:
                    catalog4 = item;
                    break;
                case 5:
                    catalog5 = item;
                    break;
                case 6:
                    catalog6 = item;
                    break;
            }
        }

        UiBuildDrawerQueueRowModel queue0 = default;
        UiBuildDrawerQueueRowModel queue1 = default;
        int queueCount = Mathf.Min(queue.Length, UiBuildDrawerModel.MaxQueueRows);
        for (int i = 0; i < queueCount; i++)
        {
            UiBuildDrawerQueueRowModel row = ToBuildDrawerQueueRow(queue[i]);
            if (i == 0)
                queue0 = row;
            else if (i == 1)
                queue1 = row;
        }

        drawer = new UiBuildDrawerModel(
            detail.Name.ToString(),
            detail.Role.ToString(),
            detail.Description.ToString(),
            detail.FootprintText.ToString(),
            detail.RequirementsText.ToString(),
            detail.PlacementText.ToString(),
            detail.ProductionTimeText.ToString(),
            detail.CreditsCostText.ToString(),
            detail.SuppliesCostText.ToString(),
            detail.InstructionText.ToString(),
            detail.ProductionTitle.ToString(),
            detail.ProductionCountText.ToString(),
            detail.BuildEnabled != 0,
            detail.RushEnabled != 0,
            detail.ClearEnabled != 0,
            detail.NoProductionVisible != 0,
            new UiBuildDrawerActiveProductionModel(
                active.Visible != 0,
                active.CancelEnabled != 0,
                active.Name.ToString(),
                active.PercentText.ToString(),
                active.Progress01),
            catalogCount,
            catalog0,
            catalog1,
            catalog2,
            catalog3,
            catalog4,
            catalog5,
            catalog6,
            queueCount,
            queue0,
            queue1);
        return true;
    }

    public static bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)
    {
        placementBar = UiBuildPlacementConfirmationBarModel.Hidden;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureBuildPlacementConfirmationBarState(entityManager, boundary);
        UiBuildPlacementConfirmationBarComponent component =
            entityManager.GetComponentData<UiBuildPlacementConfirmationBarComponent>(boundary);
        placementBar = new UiBuildPlacementConfirmationBarModel(
            component.Visible != 0,
            component.CanConfirm != 0,
            component.CanCancel != 0,
            component.CanRotate != 0,
            component.Title.ToString(),
            component.Status.ToString(),
            component.CostText.ToString(),
            component.DurationText.ToString(),
            component.InstructionText.ToString());
        return true;
    }

    public static bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
    {
        category = ArmoryCatalogCategory.Characters;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureArmoryCategoryState(entityManager, boundary);
        category = entityManager.GetComponentData<UiShellArmoryCategoryComponent>(boundary).Category;
        return true;
    }

    public static bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureArmoryCategoryState(entityManager, boundary);
        DynamicBuffer<UiShellArmoryCategoryRequestComponent> requests =
            entityManager.GetBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        requests.Add(new UiShellArmoryCategoryRequestComponent
        {
            Category = category
        });
        return true;
    }

    public static bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
    {
        if (commands == null)
            return false;

        commands.Clear();
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasBuffer<UiShellPresentationCommandComponent>(boundary))
            return false;

        DynamicBuffer<UiShellPresentationCommandComponent> buffer =
            entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        if (buffer.Length == 0)
            return false;

        for (int i = 0; i < buffer.Length; i++)
        {
            UiShellPresentationCommandComponent command = buffer[i];
            commands.Add(new UiShellPresentationCommandModel(
                command.Kind,
                command.Region,
                command.Route,
                command.TargetMode,
                command.SequenceId));
        }

        buffer.Clear();
        return commands.Count > 0;
    }

    public static bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasBuffer<UiShellTransitionCompleteComponent>(boundary))
            return false;

        DynamicBuffer<UiShellTransitionCompleteComponent> completions =
            entityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        completions.Add(new UiShellTransitionCompleteComponent
        {
            Kind = completion.Kind,
            Region = completion.Region,
            SequenceId = completion.SequenceId
        });
        return true;
    }

    private static string ToSelectionOrderText(int status)
    {
        return status switch
        {
            1 => "MOVING",
            2 => "ENGAGING TARGET",
            3 => "RETURNING TO BASE",
            4 => "MISSILE LAUNCHED",
            5 => "AIRSPACE CLEAR",
            6 => "TRACKING AIR TARGET",
            7 => "INTERCEPTING MISSILE",
            8 => "RELOADING",
            _ => "IDLE"
        };
    }

    private static UiMatchHudPassengerRowModel ToPassengerRow(FocusedUnitPassengerUiReadModelElement passenger)
    {
        string name = passenger.DisplayName.ToString();
        if (string.IsNullOrWhiteSpace(name))
            name = "PASSENGER";

        int healthMax = Mathf.Max(0, passenger.HealthMax);
        int healthCurrent = Mathf.Clamp(passenger.HealthCurrent, 0, healthMax);
        string healthText = healthMax > 0 ? $"{healthCurrent} / {healthMax}" : "HEALTH -";
        float health01 = healthMax > 0 ? Mathf.Clamp01((float)healthCurrent / healthMax) : 0f;
        return new UiMatchHudPassengerRowModel(name, "ONBOARD", healthText, health01);
    }

    private static UiBuildDrawerCatalogItemModel ToBuildDrawerCatalogItem(
        UiBuildDrawerCatalogItemComponent item)
    {
        return new UiBuildDrawerCatalogItemModel(
            item.Visible != 0,
            item.Enabled != 0,
            item.Title.ToString(),
            item.Role.ToString(),
            item.CreditsText.ToString(),
            item.SuppliesText.ToString(),
            item.TimeText.ToString());
    }

    private static UiBuildDrawerQueueRowModel ToBuildDrawerQueueRow(UiBuildDrawerQueueRowComponent row)
    {
        return new UiBuildDrawerQueueRowModel(
            row.Visible != 0,
            row.ActionEnabled != 0,
            row.NumberText.ToString(),
            row.Name.ToString(),
            row.TimeText.ToString());
    }

    private static bool TryGetBoundary(out EntityManager entityManager, out Entity boundary)
    {
        entityManager = default;
        boundary = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world)
        {
            cachedWorld = world;
            hasBoundaryQuery = false;
            hasFocusedSelectionQuery = false;
            hasSelectionInputQuery = false;
        }

        if (!hasBoundaryQuery)
        {
            boundaryQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entityManager = world.EntityManager;
        boundary = boundaryQuery.GetSingletonEntity();
        return true;
    }

    private static void EnsureArmoryCategoryState(EntityManager entityManager, Entity boundary)
    {
        if (!entityManager.HasComponent<UiShellArmoryCategoryComponent>(boundary))
        {
            entityManager.AddComponentData(boundary, new UiShellArmoryCategoryComponent
            {
                Category = ArmoryCatalogCategory.Characters
            });
        }

        if (!entityManager.HasBuffer<UiShellArmoryCategoryRequestComponent>(boundary))
            entityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
    }

    private static void EnsureDiagnosticsOverlayState(EntityManager entityManager, Entity boundary)
    {
        if (entityManager.HasComponent<UiDiagnosticsOverlayComponent>(boundary))
            return;

        entityManager.AddComponentData(boundary, new UiDiagnosticsOverlayComponent
        {
            Fps = 0,
            LogVisible = 0,
            LogText = new FixedString4096Bytes("Runtime log ready.")
        });
    }

    private static void EnsureUiActionRequestBuffer(EntityManager entityManager, Entity boundary)
    {
        if (!entityManager.HasBuffer<UiActionRequestComponent>(boundary))
            entityManager.AddBuffer<UiActionRequestComponent>(boundary);
    }

    private static void EnsureBuildDrawerState(EntityManager entityManager, Entity boundary)
    {
        if (!entityManager.HasComponent<UiBuildDrawerDetailComponent>(boundary))
        {
            entityManager.AddComponentData(boundary, new UiBuildDrawerDetailComponent
            {
                Name = new FixedString64Bytes("GUARD TOWER"),
                Role = new FixedString32Bytes("DEFENSE"),
                Description = new FixedString128Bytes("Provides overwatch and expands line of sight."),
                FootprintText = new FixedString32Bytes("3 x 3"),
                RequirementsText = new FixedString64Bytes("HQ LEVEL 1"),
                PlacementText = new FixedString64Bytes("VALID GROUND"),
                ProductionTimeText = new FixedString32Bytes("00:18"),
                CreditsCostText = new FixedString32Bytes("420"),
                SuppliesCostText = new FixedString32Bytes("80"),
                InstructionText = new FixedString128Bytes("Tap a valid footprint to place the structure."),
                ProductionTitle = new FixedString32Bytes("QUEUE"),
                ProductionCountText = new FixedString32Bytes("2/3"),
                BuildEnabled = 1,
                RushEnabled = 1,
                ClearEnabled = 1,
                NoProductionVisible = 0
            });
        }

        if (!entityManager.HasComponent<UiBuildDrawerActiveProductionComponent>(boundary))
        {
            entityManager.AddComponentData(boundary, new UiBuildDrawerActiveProductionComponent
            {
                Visible = 1,
                CancelEnabled = 1,
                Name = new FixedString64Bytes("BARRACKS"),
                PercentText = new FixedString32Bytes("65%"),
                Progress01 = 0.65f
            });
        }

        DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog;
        if (entityManager.HasBuffer<UiBuildDrawerCatalogItemComponent>(boundary))
        {
            catalog = entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
        }
        else
        {
            catalog = entityManager.AddBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
        }

        if (catalog.Length == 0)
        {
            catalog.Add(new UiBuildDrawerCatalogItemComponent
            {
                Visible = 1,
                Enabled = 1,
                Title = new FixedString64Bytes("GUARD TOWER"),
                Role = new FixedString32Bytes("DEFENSE"),
                CreditsText = new FixedString32Bytes("420"),
                SuppliesText = new FixedString32Bytes("80"),
                TimeText = new FixedString32Bytes("00:18")
            });
            catalog.Add(new UiBuildDrawerCatalogItemComponent
            {
                Visible = 1,
                Enabled = 0,
                Title = new FixedString64Bytes("BARRACKS"),
                Role = new FixedString32Bytes("INFANTRY"),
                CreditsText = new FixedString32Bytes("900"),
                SuppliesText = new FixedString32Bytes("120"),
                TimeText = new FixedString32Bytes("00:30")
            });
        }

        DynamicBuffer<UiBuildDrawerQueueRowComponent> queue;
        if (entityManager.HasBuffer<UiBuildDrawerQueueRowComponent>(boundary))
        {
            queue = entityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary);
        }
        else
        {
            queue = entityManager.AddBuffer<UiBuildDrawerQueueRowComponent>(boundary);
        }

        if (queue.Length == 0)
        {
            queue.Add(new UiBuildDrawerQueueRowComponent
            {
                Visible = 1,
                ActionEnabled = 1,
                NumberText = new FixedString32Bytes("1"),
                Name = new FixedString64Bytes("BARRACKS"),
                TimeText = new FixedString32Bytes("00:14")
            });
        }
    }

    private static void EnsureBuildPlacementConfirmationBarState(EntityManager entityManager, Entity boundary)
    {
        if (entityManager.HasComponent<UiBuildPlacementConfirmationBarComponent>(boundary))
            return;

        entityManager.AddComponentData(boundary, new UiBuildPlacementConfirmationBarComponent
        {
            Visible = 0,
            CanConfirm = 0,
            CanCancel = 0,
            CanRotate = 0,
            Title = new FixedString64Bytes("PLACE BUILDING"),
            Status = new FixedString64Bytes("VALID GROUND"),
            CostText = new FixedString32Bytes("2,000"),
            DurationText = new FixedString32Bytes("00:30"),
            InstructionText = new FixedString128Bytes("DRAG TO POSITION, CONFIRM TO BUILD")
        });
    }

    private static void EnsureCommanderProfileState(EntityManager entityManager, Entity boundary)
    {
        if (entityManager.HasComponent<UiShellCommanderProfileComponent>(boundary))
            return;

        entityManager.AddComponentData(boundary, new UiShellCommanderProfileComponent
        {
            Name = new FixedString64Bytes("COL. ALEX MORGAN"),
            Subtitle = new FixedString64Bytes("VICTORY IS PLANNED"),
            PortraitClass = new FixedString64Bytes("commander-portrait-default")
        });
    }

    private static void EnsureMainMenuResourcesState(EntityManager entityManager, Entity boundary)
    {
        if (entityManager.HasComponent<UiShellMainMenuResourcesComponent>(boundary))
            return;

        entityManager.AddComponentData(boundary, new UiShellMainMenuResourcesComponent
        {
            CreditsText = new FixedString32Bytes("12,450"),
            SuppliesText = new FixedString32Bytes("1,280"),
            CommandText = new FixedString32Bytes("78/100")
        });
    }

    private static void EnsureMatchHudHeaderState(EntityManager entityManager, Entity boundary)
    {
        if (entityManager.HasComponent<UiMatchHudHeaderComponent>(boundary))
            return;

        entityManager.AddComponentData(boundary, new UiMatchHudHeaderComponent
        {
            OrderText = new FixedString32Bytes("MOVE ORDER"),
            SquadText = new FixedString32Bytes("RIFLE SQUAD"),
            CreditsText = new FixedString32Bytes("187,540"),
            FuelText = new FixedString32Bytes("2,860"),
            SupplyText = new FixedString32Bytes("92/120"),
            CivilianRiskText = new FixedString32Bytes("MED")
        });
    }

    private static void EnsureMatchHudStatusSurfacesState(EntityManager entityManager, Entity boundary)
    {
        if (entityManager.HasComponent<UiMatchHudStatusSurfacesComponent>(boundary))
            return;

        entityManager.AddComponentData(boundary, new UiMatchHudStatusSurfacesComponent
        {
            ObjectivesTitle = new FixedString32Bytes("OBJECTIVES"),
            Objective0Text = new FixedString64Bytes("Neutralize hostile patrol"),
            Objective1Text = new FixedString64Bytes("Protect civilians"),
            Objective2Text = new FixedString64Bytes("Keep losses low"),
            Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked,
            Objective1IconKind = UiMatchHudObjectiveIconKind.Checked,
            Objective2IconKind = UiMatchHudObjectiveIconKind.Star,
            ElapsedText = new FixedString32Bytes("ELAPSED: 07:42"),
            ThreatVisible = 1,
            ThreatTitle = new FixedString64Bytes("HOSTILE CELL SPOTTED"),
            ThreatSubtitle = new FixedString64Bytes("Market quarter, 140m"),
            JumpEnabled = 1,
            FeedbackVisible = 1,
            FeedbackText = new FixedString64Bytes("Blocked: civilian zone"),
            BoardAllVisible = 1,
            BoardAllEnabled = 1,
            CancelVisible = 1,
            CancelEnabled = 1
        });
    }

    private static void EnsureMatchHudMinimapState(EntityManager entityManager, Entity boundary)
    {
        if (entityManager.HasComponent<UiMatchHudMinimapComponent>(boundary))
            return;

        entityManager.AddComponentData(boundary, new UiMatchHudMinimapComponent
        {
            ViewportLeftPercent = 26f,
            ViewportTopPercent = 34f,
            ViewportWidthPercent = 40f,
            ViewportHeightPercent = 34f,
            ZoomInEnabled = 1,
            ZoomOutEnabled = 1,
            FocusEnabled = 1,
            FriendlyAVisible = 1,
            FriendlyALeftPercent = 47f,
            FriendlyATopPercent = 57f,
            FriendlyBVisible = 1,
            FriendlyBLeftPercent = 58f,
            FriendlyBTopPercent = 63f,
            HostileAVisible = 1,
            HostileALeftPercent = 55f,
            HostileATopPercent = 37f,
            CivilianVisible = 1,
            CivilianLeftPercent = 75f,
            CivilianTopPercent = 52f
        });
    }

    bool IUiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
    {
        return TryEnqueueRouteRequest(intent, route, pushHistory);
    }

    bool IUiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind kind, int payloadId)
    {
        return TryEnqueueUiAction(kind, payloadId);
    }

    bool IUiShellRuntimeGateway.TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
    {
        return TryReadLoadingProgress(out loading);
    }

    bool IUiShellRuntimeGateway.TrySetLoadingProgress(float progress01, string status, bool complete)
    {
        return TrySetLoadingProgress(progress01, status, complete);
    }

    bool IUiShellRuntimeGateway.TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics)
    {
        return TryReadDiagnosticsOverlay(out diagnostics);
    }

    bool IUiShellRuntimeGateway.TryReadShellState(out UiShellStateModel state)
    {
        return TryReadShellState(out state);
    }

    bool IUiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
    {
        return TryReadCommanderProfile(out profile);
    }

    bool IUiShellRuntimeGateway.TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)
    {
        return TryReadMainMenuResources(out resources);
    }

    bool IUiShellRuntimeGateway.TryReadMissionResult(out UiMissionResultPopupModel result)
    {
        return TryReadMissionResult(out result);
    }

    bool IUiShellRuntimeGateway.TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection)
    {
        return TryReadMatchHudSelection(out selection);
    }

    bool IUiShellRuntimeGateway.TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState)
    {
        return TryReadMatchHudCommandState(out commandState);
    }

    bool IUiShellRuntimeGateway.TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer)
    {
        return TryReadMatchHudPassengerDrawer(out passengerDrawer);
    }

    bool IUiShellRuntimeGateway.TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray)
    {
        return TryReadMatchHudSquadTray(out squadTray);
    }

    bool IUiShellRuntimeGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header)
    {
        return TryReadMatchHudHeader(out header);
    }

    bool IUiShellRuntimeGateway.TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
    {
        return TryReadMatchHudStatusSurfaces(out statusSurfaces);
    }

    bool IUiShellRuntimeGateway.TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
    {
        return TryReadMatchHudMinimap(out minimap);
    }

    bool IUiShellRuntimeGateway.TryReadBuildDrawer(out UiBuildDrawerModel drawer)
    {
        return TryReadBuildDrawer(out drawer);
    }

    bool IUiShellRuntimeGateway.TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)
    {
        return TryReadBuildPlacementConfirmationBar(out placementBar);
    }

    bool IUiShellRuntimeGateway.TryReadArmoryCategory(out ArmoryCatalogCategory category)
    {
        return TryReadArmoryCategory(out category);
    }

    bool IUiShellRuntimeGateway.TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
    {
        return TryEnqueueArmoryCategory(category);
    }

    bool IUiShellRuntimeGateway.TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
    {
        return TryConsumePresentationCommands(commands);
    }

    bool IUiShellRuntimeGateway.TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
    {
        return TryEnqueueTransitionComplete(completion);
    }
}
