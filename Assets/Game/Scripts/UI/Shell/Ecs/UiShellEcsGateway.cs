using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed class UiShellEcsGateway : IUiShellRuntimeGateway, IUiAssistantPanelStateGateway
    {
        private static readonly UiShellEcsGateway Shared = new();
        private static World cachedWorld;
        private static EntityQuery boundaryQuery;
        private static EntityQuery focusedSelectionQuery;
        private static EntityQuery selectionInputQuery;
        private static EntityQuery selectedUnitsQuery;
        private static EntityQuery minimapMarkerQuery;
        private static EntityQuery gridConfigQuery;
        private static EntityQuery resourceStorageQuery;
        private static EntityQuery assistantMatchStartQuery;
        private static FixedString4096Bytes cachedDiagnosticsLogFixedText;
        private static string cachedDiagnosticsLogText;
        private static bool hasBoundaryQuery;
        private static bool hasFocusedSelectionQuery;
        private static bool hasSelectionInputQuery;
        private static bool hasSelectedUnitsQuery;
        private static bool hasMinimapMarkerQuery;
        private static bool hasGridConfigQuery;
        private static bool hasResourceStorageQuery;
        private static bool hasAssistantMatchStartQuery;
        private static bool hasCachedDiagnosticsLogText;
        private static bool hasCachedMatchHudHeader;
        private static World cachedMatchHudHeaderWorld;
        private static Entity cachedMatchHudHeaderBoundary;
        private static UiMatchHudHeaderComponent cachedMatchHudHeaderComponent;
        private static byte cachedMatchHudHeaderResourceSource;
        private static uint cachedMatchHudHeaderResourceVersion;
        private static int cachedMatchHudHeaderOil;
        private static int cachedMatchHudHeaderFuel;
        private static bool cachedMatchHudHeaderShowOil;
        private static UiMatchHudHeaderModel cachedMatchHudHeader;
        private static bool hasCachedAssistantPanel;
        private static World cachedAssistantPanelWorld;
        private static Entity cachedAssistantPanelBoundary;
        private static uint cachedAssistantPanelSourceVersion;
        private static uint cachedAssistantPanelRecommendationVersion;
        private static uint cachedAssistantPanelObjectiveVersion;
        private static uint cachedAssistantPanelMessageReadModelVersion;
        private static uint cachedAssistantPanelThreatVersion;
        private static uint cachedAssistantPanelTargetLockVersion;
        private static uint cachedAssistantPanelNarrationStateVersion;
        private static bool cachedAssistantPanelNarrationPulse;
        private static uint cachedAssistantPanelSettingsVersion;
        private static uint cachedAssistantPanelVersion;
        private static int cachedAssistantPanelGoalCount;
        private static int cachedAssistantPanelMessageCount;
        private static int cachedAssistantPanelRecommendationCount;
        private static AssistantControlState cachedAssistantPanelControlState;
        private static UiAssistantPanelModel cachedAssistantPanel;
        private static bool hasCachedAssistantHighlight;
        private static World cachedAssistantHighlightWorld;
        private static Entity cachedAssistantHighlightBoundary;
        private static uint cachedAssistantHighlightVersion;
        private static int cachedAssistantHighlightRequestId;
        private static UiAssistantHighlightModel cachedAssistantHighlight;

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
            hasResourceStorageQuery = false;
            hasAssistantMatchStartQuery = false;
            hasCachedDiagnosticsLogText = false;
            hasCachedMatchHudHeader = false;
            cachedMatchHudHeaderWorld = null;
            cachedMatchHudHeaderBoundary = Entity.Null;
            cachedMatchHudHeaderComponent = default;
            cachedMatchHudHeaderResourceSource = 0;
            cachedMatchHudHeaderResourceVersion = 0;
            cachedMatchHudHeaderOil = 0;
            cachedMatchHudHeaderFuel = 0;
            cachedMatchHudHeaderShowOil = false;
            cachedMatchHudHeader = UiMatchHudHeaderModel.Default;
            hasCachedAssistantPanel = false;
            cachedAssistantPanelWorld = null;
            cachedAssistantPanelBoundary = Entity.Null;
            cachedAssistantPanelSourceVersion = 0;
            cachedAssistantPanelRecommendationVersion = 0;
            cachedAssistantPanelObjectiveVersion = 0;
            cachedAssistantPanelMessageReadModelVersion = 0;
            cachedAssistantPanelThreatVersion = 0;
            cachedAssistantPanelTargetLockVersion = 0;
            cachedAssistantPanelNarrationStateVersion = 0;
            cachedAssistantPanelNarrationPulse = false;
            cachedAssistantPanelSettingsVersion = 0;
            cachedAssistantPanelVersion = 0;
            cachedAssistantPanelGoalCount = 0;
            cachedAssistantPanelMessageCount = 0;
            cachedAssistantPanelRecommendationCount = 0;
            cachedAssistantPanelControlState = AssistantControlState.Player;
            cachedAssistantPanel = UiAssistantPanelModel.Empty;
            hasCachedAssistantHighlight = false;
            cachedAssistantHighlightWorld = null;
            cachedAssistantHighlightBoundary = Entity.Null;
            cachedAssistantHighlightVersion = 0;
            cachedAssistantHighlightRequestId = 0;
            cachedAssistantHighlight = UiAssistantHighlightModel.Empty;
            cachedDiagnosticsLogFixedText = default;
            cachedDiagnosticsLogText = string.Empty;
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

        public static bool TryEnqueueAssistantCommandIntent(
            UiAssistantCommandIntentKind kind,
            bool fromTakeover)
        {
            if (kind == UiAssistantCommandIntentKind.None ||
                !TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            {
                return false;
            }

            if (!IsAssistantRuntimeActive(entityManager, boundary))
                return false;

            if (kind == UiAssistantCommandIntentKind.StopAssistantControl)
            {
                EnsureAssistantCommandIntentBuffers(entityManager, boundary);
                DynamicBuffer<AssistantCommandIntentRequestElement> stopRequests =
                    entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
                DynamicBuffer<AssistantCommandIntentResultElement> stopResults =
                    entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary, true);
                stopRequests.Add(new AssistantCommandIntentRequestElement
                {
                    RequestId = NextAssistantCommandIntentRequestId(stopRequests, stopResults),
                    Frame = Time.frameCount,
                    RecommendationId = 0,
                    Kind = AssistantCommandIntentKind.StopAssistantControl,
                    TargetKind = AssistantTargetKind.None,
                    FromTakeover = fromTakeover ? (byte)1 : (byte)0
                });
                return true;
            }

            if (fromTakeover && !AssistantSettingsPersistenceSystemHelper.TakeoverAllowed(entityManager, boundary))
                return false;

            if (!entityManager.HasBuffer<AssistantRecommendationElement>(boundary))
                return false;

            DynamicBuffer<AssistantRecommendationElement> recommendations =
                entityManager.GetBuffer<AssistantRecommendationElement>(boundary, true);
            if (recommendations.Length == 0 || recommendations[0].RecommendationId == 0)
                return false;

            AssistantRecommendationElement recommendation = recommendations[0];
            AssistantCommandIntentKind ecsKind = ToAssistantCommandIntentKind(kind, recommendation.Kind);
            if (ecsKind == AssistantCommandIntentKind.None)
                return false;

            if (ecsKind == AssistantCommandIntentKind.ShowRecommendation && recommendation.CanShow == 0)
                return false;
            if (kind == UiAssistantCommandIntentKind.ExecuteRecommendation && recommendation.CanExecute == 0)
                return false;

            EnsureAssistantCommandIntentBuffers(entityManager, boundary);
            DynamicBuffer<AssistantCommandIntentRequestElement> requests =
                entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
            DynamicBuffer<AssistantCommandIntentResultElement> results =
                entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary, true);
            requests.Add(new AssistantCommandIntentRequestElement
            {
                RequestId = NextAssistantCommandIntentRequestId(requests, results),
                Frame = Time.frameCount,
                RecommendationId = recommendation.RecommendationId,
                RecommendationSourceVersion = recommendation.SourceVersion,
                Kind = ecsKind,
                TargetKind = recommendation.TargetKind,
                SourceEntity = recommendation.SourceEntity,
                TargetEntity = recommendation.TargetEntity,
                TargetCell = recommendation.TargetCell,
                WorldPosition = recommendation.WorldPosition,
                FromTakeover = fromTakeover ? (byte)1 : (byte)0
            });
            return true;
        }

        public static bool TrySetAssistantPanelOpen(bool open)
        {
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;
            if (open && !IsAssistantRuntimeActive(entityManager, boundary))
                return false;

            if (!entityManager.HasComponent<AssistantStateComponent>(boundary))
                return false;
            AssistantStateComponent assistant = entityManager.GetComponentData<AssistantStateComponent>(boundary);
            byte next = open ? (byte)1 : (byte)0;
            if (assistant.PanelOpen == next)
                return true;

            assistant.PanelOpen = next;
            assistant.UiDirty = 1;
            entityManager.SetComponentData(boundary, assistant);
            return true;
        }

        private static AssistantCommandIntentKind ToAssistantCommandIntentKind(
            UiAssistantCommandIntentKind kind,
            AssistantRecommendationKind recommendationKind)
        {
            return kind switch
            {
                UiAssistantCommandIntentKind.ShowRecommendation => AssistantCommandIntentKind.ShowRecommendation,
                UiAssistantCommandIntentKind.ExecuteRecommendation => ToExecutableIntentKind(recommendationKind),
                UiAssistantCommandIntentKind.StopAssistantControl => AssistantCommandIntentKind.StopAssistantControl,
                _ => AssistantCommandIntentKind.None
            };
        }

        private static AssistantCommandIntentKind ToExecutableIntentKind(AssistantRecommendationKind recommendationKind)
        {
            return recommendationKind switch
            {
                AssistantRecommendationKind.Select => AssistantCommandIntentKind.SelectEntity,
                AssistantRecommendationKind.Move => AssistantCommandIntentKind.MoveToWorldPosition,
                AssistantRecommendationKind.Attack => AssistantCommandIntentKind.AttackEntity,
                AssistantRecommendationKind.CameraFocus => AssistantCommandIntentKind.FocusCamera,
                AssistantRecommendationKind.Stop => AssistantCommandIntentKind.StopAssistantControl,
                _ => AssistantCommandIntentKind.None
            };
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

            EnsureLoadingProgressRequestBuffer(entityManager, boundary);
            DynamicBuffer<UiShellLoadingProgressRequestComponent> requests =
                entityManager.GetBuffer<UiShellLoadingProgressRequestComponent>(boundary);
            requests.Add(new UiShellLoadingProgressRequestComponent
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
            bool logVisible = component.LogVisible != 0;
            diagnostics = new UiDiagnosticsOverlayModel(
                Mathf.Max(0, component.Fps),
                logVisible,
                logVisible ? GetDiagnosticsLogText(component.LogText) : string.Empty);
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

            summary.OrderText = summary.MixedOrders
                ? GameText.Get("selection.order.mixed_orders", "Mixed orders")
                : summary.OrderText ?? GameText.Get("selection.order.idle", "Idle");
            if (summary.HealthMax > 0)
            {
                summary.Health01 = Mathf.Clamp01((float)summary.HealthCurrent / summary.HealthMax);
                summary.HealthText = GameText.Format("selection.health.summary_value", "{0} / {1}", summary.HealthCurrent, summary.HealthMax);
            }
            else
            {
                summary.Health01 = 0f;
                summary.HealthText = GameText.Get("selection.health.summary_empty", "HEALTH -");
            }

            if (summary.SelectedCount == summary.SoldierCount)
            {
                summary.Title = summary.SelectedCount == 1
                    ? GameText.Get("selection.shell.title.soldier", "SOLDIER")
                    : GameText.Format("selection.title.soldiers", "{0} SOLDIERS", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.infantry_group", "INFANTRY GROUP");
            }
            else if (summary.SelectedCount == summary.VehicleCount)
            {
                summary.Title = summary.SelectedCount == 1
                    ? GameText.Get("selection.shell.title.vehicle", "VEHICLE")
                    : GameText.Format("selection.title.vehicles", "{0} VEHICLES", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.armored_group", "ARMORED GROUP");
            }
            else if (summary.SelectedCount == summary.AircraftCount)
            {
                summary.Title = summary.SelectedCount == 1
                    ? GameText.Get("selection.shell.title.aircraft", "AIRCRAFT")
                    : GameText.Format("selection.title.aircraft", "{0} AIRCRAFT", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.air_group", "AIR GROUP");
            }
            else
            {
                summary.Title = GameText.Format("selection.shell.title.selected", "{0} SELECTED", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.mixed_group", "MIXED GROUP");
            }

            return summary;
        }

        private static string ResolveEntityOrderText(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<UnitTransportBoardingTarget>(entity))
                return GameText.Get("selection.order.boarding_transport", "Boarding transport");
            if (entityManager.HasComponent<EngageTarget>(entity))
                return GameText.Get("selection.order.engaging_target", "Engaging target");
            if (entityManager.HasComponent<ManualMoveOrderTag>(entity) ||
                entityManager.HasComponent<ManualMoveGroupMemberTag>(entity))
            {
                return GameText.Get("selection.order.moving", "Moving");
            }

            if (entityManager.HasComponent<HoldPositionOrderTag>(entity))
                return GameText.Get("selection.order.holding", "Holding");
            return GameText.Get("selection.order.idle", "Idle");
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
            string oilText = "0";
            string fuelText = component.FuelText.ToString();
            bool showOil = false;
            bool cacheHeader = false;
            byte resourceSource = 0;
            uint resourceVersion = 0u;
            int resourceOil = 0;
            int resourceFuel = 0;
            bool hasUsableFuelSummaryBuffer =
                entityManager.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            if (TryReadPlayerUsableFuelSummary(
                    entityManager,
                    boundary,
                    out int usableOil,
                    out int usableFuel,
                    out bool usableOilVisible,
                    out uint usableFuelVersion))
            {
                if (TryReadCachedMatchHudHeader(
                        entityManager.World,
                        boundary,
                        component,
                        1,
                        usableFuelVersion,
                        usableOil,
                        usableFuel,
                        usableOilVisible,
                        out header))
                {
                    return true;
                }

                cacheHeader = true;
                resourceSource = 1;
                resourceVersion = usableFuelVersion;
                resourceOil = usableOil;
                resourceFuel = usableFuel;
                oilText = FormatCompact(usableOil);
                fuelText = FormatCompact(usableFuel);
                showOil = usableOilVisible;
            }
            else if (TryFormatLivePlayerResourceStorage(
                         entityManager,
                         out string liveOilText,
                         out string liveFuelText,
                         out bool liveOilVisible))
            {
                oilText = liveOilText;
                fuelText = liveFuelText;
                showOil = liveOilVisible;
            }
            else if (!hasUsableFuelSummaryBuffer &&
                     TryFormatPlayerResourceSummary(
                         entityManager,
                         boundary,
                         out string resourceOilText,
                         out string resourceFuelText,
                         out bool resourceOilVisible))
            {
                oilText = resourceOilText;
                fuelText = resourceFuelText;
                showOil = resourceOilVisible;
            }

            if (!showOil)
                showOil = TryHasPlayerOilResourceSummary(entityManager, boundary);

            header = new UiMatchHudHeaderModel(
                component.OrderText.ToString(),
                component.SquadText.ToString(),
                component.CreditsText.ToString(),
                fuelText,
                component.SupplyText.ToString(),
                component.CivilianRiskText.ToString(),
                oilText,
                showOil);
            if (cacheHeader)
            {
                CacheMatchHudHeader(
                    entityManager.World,
                    boundary,
                    component,
                    resourceSource,
                    resourceVersion,
                    resourceOil,
                    resourceFuel,
                    showOil,
                    header);
            }

            return true;
        }

        private static bool TryReadCachedMatchHudHeader(
            World world,
            Entity boundary,
            in UiMatchHudHeaderComponent component,
            byte resourceSource,
            uint resourceVersion,
            int oil,
            int fuel,
            bool showOil,
            out UiMatchHudHeaderModel header)
        {
            if (hasCachedMatchHudHeader &&
                cachedMatchHudHeaderWorld == world &&
                cachedMatchHudHeaderBoundary == boundary &&
                cachedMatchHudHeaderResourceSource == resourceSource &&
                cachedMatchHudHeaderResourceVersion == resourceVersion &&
                cachedMatchHudHeaderOil == oil &&
                cachedMatchHudHeaderFuel == fuel &&
                cachedMatchHudHeaderShowOil == showOil &&
                MatchHudHeaderComponentEquals(cachedMatchHudHeaderComponent, component))
            {
                header = cachedMatchHudHeader;
                return true;
            }

            header = default;
            return false;
        }

        private static void CacheMatchHudHeader(
            World world,
            Entity boundary,
            in UiMatchHudHeaderComponent component,
            byte resourceSource,
            uint resourceVersion,
            int oil,
            int fuel,
            bool showOil,
            in UiMatchHudHeaderModel header)
        {
            hasCachedMatchHudHeader = true;
            cachedMatchHudHeaderWorld = world;
            cachedMatchHudHeaderBoundary = boundary;
            cachedMatchHudHeaderComponent = component;
            cachedMatchHudHeaderResourceSource = resourceSource;
            cachedMatchHudHeaderResourceVersion = resourceVersion;
            cachedMatchHudHeaderOil = oil;
            cachedMatchHudHeaderFuel = fuel;
            cachedMatchHudHeaderShowOil = showOil;
            cachedMatchHudHeader = header;
        }

        private static bool MatchHudHeaderComponentEquals(
            in UiMatchHudHeaderComponent left,
            in UiMatchHudHeaderComponent right)
        {
            return left.OrderText.Equals(right.OrderText) &&
                   left.SquadText.Equals(right.SquadText) &&
                   left.CreditsText.Equals(right.CreditsText) &&
                   left.FuelText.Equals(right.FuelText) &&
                   left.SupplyText.Equals(right.SupplyText) &&
                   left.CivilianRiskText.Equals(right.CivilianRiskText);
        }

        private static bool TryReadPlayerUsableFuelSummary(
            EntityManager entityManager,
            Entity boundary,
            out int oil,
            out int fuel,
            out bool showOil,
            out uint version)
        {
            oil = 0;
            fuel = 0;
            showOil = false;
            version = 0u;
            if (!entityManager.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary))
                return false;

            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                entityManager.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                BuildingRuntimeFactionUsableFuelSummary summary = summaries[i];
                if (!FactionIdentity.IsPlayerControlled(summary.FactionId))
                    continue;

                oil = Mathf.Max(0, Mathf.RoundToInt(summary.StoredOilBarrels));
                fuel = Mathf.Max(0, Mathf.RoundToInt(summary.StoredFuelBarrels));
                showOil = summary.OilStorageCapacity > 0 || summary.StoredOilBarrels > 0.001f;
                version = summary.Version;
                return true;
            }

            return false;
        }

        private static bool TryFormatLivePlayerResourceStorage(
            EntityManager entityManager,
            out string oilText,
            out string fuelText,
            out bool showOil)
        {
            oilText = string.Empty;
            fuelText = string.Empty;
            showOil = false;
            EnsureResourceStorageQuery(entityManager);
            if (resourceStorageQuery.IsEmptyIgnoreFilter)
                return false;

            float oil = 0f;
            float fuel = 0f;
            bool foundPlayerStorage = false;
            using NativeArray<BuildingResourceStorageComponent> storages =
                resourceStorageQuery.ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
            using NativeArray<Faction> factions =
                resourceStorageQuery.ToComponentDataArray<Faction>(Allocator.Temp);
            int count = math.min(storages.Length, factions.Length);
            for (int i = 0; i < count; i++)
            {
                if (!FactionIdentity.IsPlayerControlled(factions[i].Id))
                    continue;

                BuildingResourceStorageComponent storage = storages[i];
                if (!IsUsableHeaderResourceStorage(storage))
                    continue;

                foundPlayerStorage = true;
                oil += Mathf.Max(0f, storage.StoredOilBarrels);
                fuel += Mathf.Max(0f, storage.StoredFuelBarrels);
                showOil |= storage.OilStorageCapacity > 0 || storage.StoredOilBarrels > 0.001f;
            }

            if (!foundPlayerStorage)
                return false;

            oilText = FormatCompact(Mathf.Max(0, Mathf.RoundToInt(oil)));
            fuelText = FormatCompact(Mathf.Max(0, Mathf.RoundToInt(fuel)));
            return true;
        }

        private static bool IsUsableHeaderResourceStorage(in BuildingResourceStorageComponent storage)
        {
            bool hasStorage = storage.OilStorageCapacity > 0 || storage.FuelStorageCapacity > 0;
            bool producesResource = storage.OilBarrelsPerDay > 0f || storage.FuelBarrelsPerDay > 0f;
            return hasStorage && !producesResource;
        }

        private static bool TryFormatPlayerResourceSummary(
            EntityManager entityManager,
            Entity boundary,
            out string oilText,
            out string fuelText,
            out bool showOil)
        {
            oilText = string.Empty;
            fuelText = string.Empty;
            showOil = false;
            if (!entityManager.HasBuffer<BuildingRuntimeFactionSummary>(boundary))
                return false;

            DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
                entityManager.GetBuffer<BuildingRuntimeFactionSummary>(boundary, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                BuildingRuntimeFactionSummary summary = summaries[i];
                if (!FactionIdentity.IsPlayerControlled(summary.FactionId))
                    continue;

                int oil = Mathf.Max(0, Mathf.RoundToInt(summary.StoredOilBarrels));
                int fuel = Mathf.Max(0, Mathf.RoundToInt(summary.StoredFuelBarrels));
                oilText = FormatCompact(oil);
                fuelText = FormatCompact(fuel);
                showOil = oil > 0 || summary.OilBarrelsPerDay > 0f;
                return true;
            }

            return false;
        }

        private static bool TryHasPlayerOilResourceSummary(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasBuffer<BuildingRuntimeFactionSummary>(boundary))
                return false;

            DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
                entityManager.GetBuffer<BuildingRuntimeFactionSummary>(boundary, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                BuildingRuntimeFactionSummary summary = summaries[i];
                if (!FactionIdentity.IsPlayerControlled(summary.FactionId))
                    continue;

                return summary.StoredOilBarrels > 0.001f || summary.OilBarrelsPerDay > 0f;
            }

            return false;
        }

        private static void EnsureResourceStorageQuery(EntityManager entityManager)
        {
            if (hasResourceStorageQuery && cachedWorld == entityManager.World)
                return;

            resourceStorageQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingResourceStorageComponent>(),
                ComponentType.ReadOnly<Faction>());
            hasResourceStorageQuery = true;
        }

        private static string FormatCompact(int value)
        {
            if (value >= 1000000)
                return $"{value / 1000000f:0.#}M";
            if (value >= 10000)
                return $"{value / 1000f:0.#}K";
            return value.ToString();
        }

        private static uint NextManagedAssistantPanelVersion(uint version)
        {
            uint next = version + 1u;
            return next == 0u ? 1u : next;
        }

        private static uint AssistantHighlightVersion(AssistantPreviewHighlightElement highlight)
        {
            uint combined = (uint)math.max(1, highlight.RequestId);
            combined = combined * 397u ^ (uint)math.max(0, highlight.RecommendationId);
            combined = combined * 31u ^ (uint)highlight.TargetKind;
            combined = combined * 17u ^ (uint)math.asint(highlight.WorldPosition.x);
            combined = combined * 17u ^ (uint)math.asint(highlight.WorldPosition.y);
            combined = combined * 17u ^ (uint)math.asint(highlight.WorldPosition.z);
            return combined == 0u ? 1u : combined;
        }

        private static void BuildAssistantGoalRows(
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            out UiAssistantGoalRowModel goal0,
            out UiAssistantGoalRowModel goal1,
            out UiAssistantGoalRowModel goal2)
        {
            goal0 = goals.Length > 0 ? ToGoalRow(goals[0]) : UiAssistantGoalRowModel.Empty;
            goal1 = goals.Length > 1 ? ToGoalRow(goals[1]) : UiAssistantGoalRowModel.Empty;
            goal2 = goals.Length > 2 ? ToGoalRow(goals[2]) : UiAssistantGoalRowModel.Empty;
        }

        private static UiAssistantGoalRowModel ToGoalRow(AssistantGoalReadModelElement goal)
        {
            return new UiAssistantGoalRowModel(
                goal.Title.Length > 0,
                goal.GoalId,
                goal.Title.ToString(),
                goal.Body.ToString(),
                (byte)goal.State,
                (byte)goal.Priority,
                goal.IsPrimary != 0);
        }

        private static void BuildAssistantMessageRows(
            DynamicBuffer<AssistantMessageElement> messages,
            out UiAssistantMessageRowModel alert0,
            out UiAssistantMessageRowModel alert1,
            out UiAssistantMessageRowModel alert2,
            out UiAssistantMessageRowModel report0,
            out UiAssistantMessageRowModel report1)
        {
            alert0 = UiAssistantMessageRowModel.Empty;
            alert1 = UiAssistantMessageRowModel.Empty;
            alert2 = UiAssistantMessageRowModel.Empty;
            report0 = UiAssistantMessageRowModel.Empty;
            report1 = UiAssistantMessageRowModel.Empty;
            int alertCount = 0;
            int reportCount = 0;
            float now = Time.time;
            for (int priority = (int)AssistantMessagePriority.Critical;
                 priority >= (int)AssistantMessagePriority.Low;
                 priority--)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    AssistantMessageElement message = messages[i];
                    if ((int)message.Priority != priority ||
                        message.Text.Length == 0 ||
                        message.Acknowledged != 0 ||
                        (message.ExpiresAt > 0f && now >= message.ExpiresAt))
                    {
                        continue;
                    }

                    UiAssistantMessageRowModel row = ToMessageRow(message, now);
                    if (message.Priority >= AssistantMessagePriority.High)
                    {
                        if (alertCount == 0) alert0 = row;
                        else if (alertCount == 1) alert1 = row;
                        else if (alertCount == 2) alert2 = row;
                        alertCount++;
                    }
                    else
                    {
                        if (reportCount == 0) report0 = row;
                        else if (reportCount == 1) report1 = row;
                        reportCount++;
                    }

                    if (alertCount >= 3 && reportCount >= 2)
                        return;
                }
            }
        }

        private static UiAssistantMessageRowModel ToMessageRow(AssistantMessageElement message, float now)
        {
            byte ageState = message.ExpiresAt > 0f && message.ExpiresAt - now < 1f
                ? (byte)3
                : now - message.CreatedAt < 5f
                    ? (byte)1
                    : (byte)2;
            return new UiAssistantMessageRowModel(
                true,
                message.MessageId,
                MessageTitle(message.RelatedKind),
                message.Text.ToString(),
                (byte)message.Priority,
                (byte)message.RelatedKind,
                ageState,
                message.RequiresNarration != 0,
                false);
        }

        private static string MessageTitle(AssistantRecommendationKind kind)
        {
            return kind switch
            {
                AssistantRecommendationKind.DefensiveAlert => "THREAT",
                AssistantRecommendationKind.Logistics => "LOGISTICS",
                AssistantRecommendationKind.Move => "COMMAND",
                AssistantRecommendationKind.Attack => "COMMAND",
                AssistantRecommendationKind.Select => "COMMAND",
                _ => "REPORT"
            };
        }

        private static UiAssistantTargetLockModel BuildAssistantTargetLockModel(
            AssistantTargetLockReadModelComponent targetLock)
        {
            if (targetLock.Visible == 0)
                return UiAssistantTargetLockModel.Empty;

            string distanceText = targetLock.HasDistance != 0
                ? $"{Mathf.RoundToInt(targetLock.Distance)} m"
                : string.Empty;
            string healthText = targetLock.HasHealth != 0
                ? $"{targetLock.HealthCurrent}/{targetLock.HealthMax}"
                : string.Empty;
            return new UiAssistantTargetLockModel(
                true,
                (byte)targetLock.State,
                (byte)targetLock.TargetKind,
                targetLock.TargetName.ToString(),
                targetLock.SourceName.ToString(),
                distanceText,
                healthText,
                FactionRelationText(targetLock.FactionRelation),
                TargetReadinessText(targetLock.State),
                targetLock.Reason.ToString());
        }

        private static UiAssistantNarrationModel BuildAssistantNarrationModel(
            EntityManager entityManager,
            AssistantSettingsComponent settings,
            AssistantNarrationStateComponent narrationState,
            DynamicBuffer<AssistantNarrationRequestElement> requests,
            bool waveformPulse)
        {
            AssistantNarrationRequestElement request = requests.IsCreated && requests.Length > 0
                ? requests[requests.Length - 1]
                : default;
            UiAssistantNarrationStateKind state = UiAssistantNarrationStateKind.Off;
            if (request.RequestId != 0)
            {
                Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(entityManager);
                AudioSettingsComponent audioSettings =
                    entityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
                state = AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                    settings,
                    audioSettings,
                    request,
                    narrationState);
            }

            return new UiAssistantNarrationModel(
                (byte)state,
                (byte)request.Priority,
                NarrationStateText(state),
                settings.SubtitlesEnabled != 0 ? request.Text.ToString() : string.Empty,
                state == UiAssistantNarrationStateKind.Failed
                    ? narrationState.LastAudioFailureReason.ToString()
                    : string.Empty,
                state == UiAssistantNarrationStateKind.Presented && waveformPulse);
        }

        private static string NarrationStateText(UiAssistantNarrationStateKind state)
        {
            return state switch
            {
                UiAssistantNarrationStateKind.TextOnly => "TEXT ONLY",
                UiAssistantNarrationStateKind.Queued => "QUEUED",
                UiAssistantNarrationStateKind.Accepted => "ACCEPTED",
                UiAssistantNarrationStateKind.Presented => "PRESENTED",
                UiAssistantNarrationStateKind.Failed => "FAILED",
                _ => "OFF"
            };
        }

        private static string TargetReadinessText(AssistantTargetLockState state)
        {
            return state switch
            {
                AssistantTargetLockState.Preview => "PREVIEW",
                AssistantTargetLockState.Executable => "READY",
                AssistantTargetLockState.Executing => "ACTIVE",
                AssistantTargetLockState.Invalid => "BLOCKED",
                _ => "BLOCKED"
            };
        }

        private static string FactionRelationText(AssistantFactionRelation relation)
        {
            return relation switch
            {
                AssistantFactionRelation.Friendly => "FRIENDLY",
                AssistantFactionRelation.Hostile => "HOSTILE",
                AssistantFactionRelation.Neutral => "NEUTRAL",
                AssistantFactionRelation.Protected => "PROTECTED",
                _ => string.Empty
            };
        }

        private static uint AssistantSettingsVersion(AssistantSettingsComponent settings)
        {
            return (uint)settings.GuidanceLevel |
                   (uint)settings.NarrationMode << 4 |
                   (uint)settings.AllowTakeover << 8 |
                   (uint)settings.SubtitlesEnabled << 9 |
                   (uint)settings.LargeTextEnabled << 10 |
                   (uint)settings.HighContrastEnabled << 11;
        }

        private static string PriorityText(AssistantMessagePriority priority)
        {
            return priority switch
            {
                AssistantMessagePriority.Critical => "CRITICAL",
                AssistantMessagePriority.High => "HIGH",
                AssistantMessagePriority.Normal => "NORMAL",
                _ => "LOW"
            };
        }

        private static string ControlStateText(AssistantControlState state)
        {
            return state switch
            {
                AssistantControlState.Guided => "GUIDED",
                AssistantControlState.AssistantPreview => "PREVIEW",
                AssistantControlState.AssistantTakeover => "ARIA CONTROL",
                AssistantControlState.PlayerOverridePending => "PLAYER OVERRIDE",
                _ => "PLAYER CONTROL"
            };
        }

        private static string ControlStateDetailText(AssistantControlState state)
        {
            return state switch
            {
                AssistantControlState.Guided => "ARIA is guiding the next action. You keep final control.",
                AssistantControlState.AssistantPreview => "ARIA is previewing a recommendation. STOP clears the preview.",
                AssistantControlState.AssistantTakeover => "ARIA is executing a bounded action. STOP returns control.",
                AssistantControlState.PlayerOverridePending => "Player input detected. ARIA is returning control.",
                _ => "You are issuing orders directly."
            };
        }

        private static bool CanStopAssistantControl(AssistantControlState state)
        {
            return state == AssistantControlState.Guided ||
                   state == AssistantControlState.AssistantPreview ||
                   state == AssistantControlState.AssistantTakeover ||
                   state == AssistantControlState.PlayerOverridePending;
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

        public static bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel)
        {
            assistantPanel = UiAssistantPanelModel.Empty;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!IsAssistantRuntimeActive(entityManager, boundary))
            {
                hasCachedAssistantPanel = false;
                return false;
            }

            if (!entityManager.HasComponent<AssistantStateComponent>(boundary) ||
                !entityManager.HasComponent<AssistantRecommendationReadModelComponent>(boundary) ||
                !entityManager.HasComponent<AssistantMessageReadModelComponent>(boundary) ||
                !entityManager.HasComponent<AssistantThreatReadModelStateComponent>(boundary) ||
                !entityManager.HasComponent<AssistantTargetLockReadModelComponent>(boundary) ||
                !entityManager.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary) ||
                !entityManager.HasBuffer<AssistantGoalReadModelElement>(boundary) ||
                !entityManager.HasBuffer<AssistantRecommendationElement>(boundary) ||
                !entityManager.HasBuffer<AssistantMessageElement>(boundary))
            {
                return false;
            }

            AssistantStateComponent assistantState =
                entityManager.GetComponentData<AssistantStateComponent>(boundary);
            AssistantRecommendationReadModelComponent recommendationReadModel =
                entityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);
            AssistantMessageReadModelComponent messageReadModel =
                entityManager.GetComponentData<AssistantMessageReadModelComponent>(boundary);
            AssistantThreatReadModelStateComponent threatReadModel =
                entityManager.GetComponentData<AssistantThreatReadModelStateComponent>(boundary);
            AssistantTargetLockReadModelComponent targetLockReadModel =
                entityManager.GetComponentData<AssistantTargetLockReadModelComponent>(boundary);
            MatchObjectiveRuntimeStateComponent objectiveState =
                entityManager.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
            DynamicBuffer<AssistantGoalReadModelElement> goals =
                entityManager.GetBuffer<AssistantGoalReadModelElement>(boundary, true);
            DynamicBuffer<AssistantRecommendationElement> recommendations =
                entityManager.GetBuffer<AssistantRecommendationElement>(boundary, true);
            DynamicBuffer<AssistantMessageElement> messages =
                entityManager.GetBuffer<AssistantMessageElement>(boundary, true);
            AssistantSettingsComponent settings = entityManager.HasComponent<AssistantSettingsComponent>(boundary)
                ? entityManager.GetComponentData<AssistantSettingsComponent>(boundary)
                : default;
            uint settingsVersion = AssistantSettingsVersion(settings);
            bool hasNarrationRequests = entityManager.HasBuffer<AssistantNarrationRequestElement>(boundary);
            DynamicBuffer<AssistantNarrationRequestElement> narrationRequests = hasNarrationRequests
                ? entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary, true)
                : default;
            AssistantNarrationStateComponent narrationState =
                entityManager.HasComponent<AssistantNarrationStateComponent>(boundary)
                    ? entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary)
                    : default;
            bool narrationPulse = narrationState.LastPresentedAt > 0f &&
                                  Time.time - narrationState.LastPresentedAt <= 0.8f;

            if (hasCachedAssistantPanel &&
                cachedAssistantPanelWorld == entityManager.World &&
                cachedAssistantPanelBoundary == boundary &&
                cachedAssistantPanelSourceVersion == assistantState.SourceVersion &&
                cachedAssistantPanelRecommendationVersion == recommendationReadModel.Version &&
                cachedAssistantPanelObjectiveVersion == objectiveState.Version &&
                cachedAssistantPanelMessageReadModelVersion == messageReadModel.Version &&
                cachedAssistantPanelThreatVersion == threatReadModel.Version &&
                cachedAssistantPanelTargetLockVersion == targetLockReadModel.Version &&
                cachedAssistantPanelNarrationStateVersion == narrationState.Version &&
                cachedAssistantPanelNarrationPulse == narrationPulse &&
                cachedAssistantPanelSettingsVersion == settingsVersion &&
                cachedAssistantPanelGoalCount == goals.Length &&
                cachedAssistantPanelMessageCount == messages.Length &&
                cachedAssistantPanelRecommendationCount == recommendations.Length &&
                cachedAssistantPanelControlState == assistantState.ControlState)
            {
                assistantPanel = cachedAssistantPanel;
                return true;
            }

            AssistantRecommendationElement topRecommendation =
                recommendations.Length > 0 ? recommendations[0] : default;
            BuildAssistantGoalRows(
                goals,
                out UiAssistantGoalRowModel goal0,
                out UiAssistantGoalRowModel goal1,
                out UiAssistantGoalRowModel goal2);
            BuildAssistantMessageRows(
                messages,
                out UiAssistantMessageRowModel alert0,
                out UiAssistantMessageRowModel alert1,
                out UiAssistantMessageRowModel alert2,
                out UiAssistantMessageRowModel report0,
                out UiAssistantMessageRowModel report1);
            UiAssistantTargetLockModel targetLock = BuildAssistantTargetLockModel(targetLockReadModel);
            UiAssistantNarrationModel narration = BuildAssistantNarrationModel(
                entityManager,
                settings,
                narrationState,
                hasNarrationRequests ? narrationRequests : default,
                narrationPulse);
            cachedAssistantPanelVersion = NextManagedAssistantPanelVersion(cachedAssistantPanelVersion);
            assistantPanel = new UiAssistantPanelModel(
                cachedAssistantPanelVersion,
                objectiveState.MatchActive != 0,
                objectiveState.ElapsedWholeSeconds,
                goal0,
                goal1,
                goal2,
                alert0,
                alert1,
                alert2,
                report0,
                report1,
                targetLock,
                narration,
                topRecommendation.RecommendationId != 0,
                topRecommendation.RecommendationId != 0 ? topRecommendation.Title.ToString() : string.Empty,
                topRecommendation.RecommendationId != 0
                    ? topRecommendation.Reason.ToString()
                    : string.Empty,
                topRecommendation.RecommendationId != 0 ? PriorityText(topRecommendation.Priority) : string.Empty,
                topRecommendation.RecommendationId != 0 ? topRecommendation.ActionLabel.ToString() : string.Empty,
                topRecommendation.CanShow != 0,
                topRecommendation.CanExecute != 0,
                CanStopAssistantControl(assistantState.ControlState),
                topRecommendation.CanTakeControl != 0,
                ControlStateText(assistantState.ControlState),
                ControlStateDetailText(assistantState.ControlState),
                settings.LargeTextEnabled != 0,
                settings.HighContrastEnabled != 0);

            hasCachedAssistantPanel = true;
            cachedAssistantPanelWorld = entityManager.World;
            cachedAssistantPanelBoundary = boundary;
            cachedAssistantPanelSourceVersion = assistantState.SourceVersion;
            cachedAssistantPanelRecommendationVersion = recommendationReadModel.Version;
            cachedAssistantPanelObjectiveVersion = objectiveState.Version;
            cachedAssistantPanelMessageReadModelVersion = messageReadModel.Version;
            cachedAssistantPanelThreatVersion = threatReadModel.Version;
            cachedAssistantPanelTargetLockVersion = targetLockReadModel.Version;
            cachedAssistantPanelNarrationStateVersion = narrationState.Version;
            cachedAssistantPanelNarrationPulse = narrationPulse;
            cachedAssistantPanelSettingsVersion = settingsVersion;
            cachedAssistantPanelGoalCount = goals.Length;
            cachedAssistantPanelMessageCount = messages.Length;
            cachedAssistantPanelRecommendationCount = recommendations.Length;
            cachedAssistantPanelControlState = assistantState.ControlState;
            cachedAssistantPanel = assistantPanel;
            return true;
        }

        public static bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight)
        {
            assistantHighlight = UiAssistantHighlightModel.Empty;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!entityManager.HasBuffer<AssistantPreviewHighlightElement>(boundary))
                return false;

            DynamicBuffer<AssistantPreviewHighlightElement> highlights =
                entityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary, true);
            if (highlights.Length == 0 || highlights[0].Active == 0)
                return false;

            AssistantPreviewHighlightElement highlight = highlights[0];
            uint version = AssistantHighlightVersion(highlight);
            if (hasCachedAssistantHighlight &&
                cachedAssistantHighlightWorld == entityManager.World &&
                cachedAssistantHighlightBoundary == boundary &&
                cachedAssistantHighlightVersion == version &&
                cachedAssistantHighlightRequestId == highlight.RequestId)
            {
                assistantHighlight = cachedAssistantHighlight;
                return true;
            }

            assistantHighlight = new UiAssistantHighlightModel(
                version,
                true,
                highlight.RequestId,
                highlight.RecommendationId,
                (byte)highlight.TargetKind,
                highlight.WorldPosition.x,
                highlight.WorldPosition.y,
                highlight.WorldPosition.z,
                highlight.Strength);

            hasCachedAssistantHighlight = true;
            cachedAssistantHighlightWorld = entityManager.World;
            cachedAssistantHighlightBoundary = boundary;
            cachedAssistantHighlightVersion = version;
            cachedAssistantHighlightRequestId = highlight.RequestId;
            cachedAssistantHighlight = assistantHighlight;
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

            Entity gridEntity = gridConfigQuery.GetSingletonEntity();
            GridConfig grid = entityManager.GetComponentData<GridConfig>(gridEntity);
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
                ComponentType.ReadOnly<MatchHudMinimapMarkerStateComponent>(),
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
            UiBuildDrawerStateComponent drawerState =
                entityManager.GetComponentData<UiBuildDrawerStateComponent>(boundary);
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
                    ResolveBuildDrawerSprite(active.ThumbnailSpriteKey),
                    active.Name.ToString(),
                    active.PercentText.ToString(),
                    active.Progress01),
                ResolveBuildDrawerSprite(detail.PreviewSpriteKey),
                drawerState.ActiveCategory,
                drawerState.BuildingsCount,
                drawerState.VehiclesCount,
                drawerState.AircraftsCount,
                drawerState.SoldiersCount,
                drawerState.SelectedCatalogSlot,
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

        public static bool TryReadResourceExchange(out UiResourceExchangeModel exchange)
        {
            exchange = UiResourceExchangeModel.Empty;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureResourceExchangeUiState(entityManager, boundary);
            UiResourceExchangeStateComponent state =
                entityManager.GetComponentData<UiResourceExchangeStateComponent>(boundary);
            UiResourceExchangeDetailComponent detail =
                entityManager.GetComponentData<UiResourceExchangeDetailComponent>(boundary);
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards =
                entityManager.GetBuffer<UiResourceExchangeRecipeCardComponent>(boundary, true);
            DynamicBuffer<UiResourceExchangeQueueRowComponent> queue =
                entityManager.GetBuffer<UiResourceExchangeQueueRowComponent>(boundary, true);

            UiResourceExchangeRecipeCardModel card0 = default;
            UiResourceExchangeRecipeCardModel card1 = default;
            UiResourceExchangeRecipeCardModel card2 = default;
            UiResourceExchangeRecipeCardModel card3 = default;
            UiResourceExchangeRecipeCardModel card4 = default;
            UiResourceExchangeRecipeCardModel card5 = default;
            UiResourceExchangeRecipeCardModel card6 = default;
            int cardCount = Mathf.Min(cards.Length, UiResourceExchangeModel.MaxRecipeCards);
            for (int i = 0; i < cardCount; i++)
            {
                UiResourceExchangeRecipeCardModel card = ToResourceExchangeRecipeCard(cards[i], i);
                switch (i)
                {
                    case 0:
                        card0 = card;
                        break;
                    case 1:
                        card1 = card;
                        break;
                    case 2:
                        card2 = card;
                        break;
                    case 3:
                        card3 = card;
                        break;
                    case 4:
                        card4 = card;
                        break;
                    case 5:
                        card5 = card;
                        break;
                    case 6:
                        card6 = card;
                        break;
                }
            }

            UiResourceExchangeQueueRowModel row0 = default;
            UiResourceExchangeQueueRowModel row1 = default;
            UiResourceExchangeQueueRowModel row2 = default;
            UiResourceExchangeQueueRowModel row3 = default;
            int rowCount = Mathf.Min(queue.Length, UiResourceExchangeModel.MaxQueueRows);
            for (int i = 0; i < rowCount; i++)
            {
                UiResourceExchangeQueueRowModel row = ToResourceExchangeQueueRow(queue[i], i);
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
                    case 3:
                        row3 = row;
                        break;
                }
            }

            exchange = new UiResourceExchangeModel(
                state.Version,
                state.ActiveTab == UiResourceExchangeTab.Import
                    ? UiResourceExchangeTabKind.Import
                    : UiResourceExchangeTabKind.Export,
                state.SelectedRecipeSlot,
                state.ExportRecipeCount,
                state.ImportRecipeCount,
                state.QueueCount,
                state.ActiveCount,
                state.CompletedCount,
                state.MaxQueueItems,
                state.QueueCapacityText.ToString(),
                state.CreditsText.ToString(),
                state.MaterialsText.ToString(),
                state.OilText.ToString(),
                state.FuelText.ToString(),
                state.RushTicketsText.ToString(),
                state.ExchangeEnabled != 0,
                state.RushAllEnabled != 0,
                state.ClearCompletedEnabled != 0,
                new UiResourceExchangeDetailModel(
                    detail.RecipeId.ToString(),
                    detail.Name.ToString(),
                    detail.RouteText.ToString(),
                    detail.RateText.ToString(),
                    detail.AmountText.ToString(),
                    detail.InputCostText.ToString(),
                    detail.OutputPreviewText.ToString(),
                    detail.DurationText.ToString(),
                    detail.RequirementsText.ToString(),
                    detail.InstructionText.ToString(),
                    detail.ConfirmEnabled != 0,
                    detail.WarningVisible != 0),
                cardCount,
                card0,
                card1,
                card2,
                card3,
                card4,
                card5,
                card6,
                rowCount,
                row0,
                row1,
                row2,
                row3);
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
                    command.SequenceId,
                    command.PopupKind));
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
                item.Selected != 0,
                ResolveBuildDrawerSprite(item.ThumbnailSpriteKey),
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
                ResolveBuildDrawerSprite(row.ThumbnailSpriteKey),
                row.NumberText.ToString(),
                row.Name.ToString(),
                row.TimeText.ToString());
        }

        private static UiResourceExchangeRecipeCardModel ToResourceExchangeRecipeCard(
            UiResourceExchangeRecipeCardComponent card,
            int slotIndex)
        {
            return new UiResourceExchangeRecipeCardModel(
                card.Visible != 0,
                card.Enabled != 0,
                card.Selected != 0,
                card.Locked != 0,
                card.WarningVisible != 0,
                slotIndex,
                card.RecipeId.ToString(),
                card.Title.ToString(),
                card.InputText.ToString(),
                card.OutputText.ToString(),
                card.DurationText.ToString(),
                card.ReasonText.ToString());
        }

        private static UiResourceExchangeQueueRowModel ToResourceExchangeQueueRow(
            UiResourceExchangeQueueRowComponent row,
            int slotIndex)
        {
            return new UiResourceExchangeQueueRowModel(
                row.Visible != 0,
                row.RushEnabled != 0,
                row.CancelEnabled != 0,
                row.CompletedVisible != 0,
                row.State == UiResourceExchangeQueueState.Blocked,
                row.QueueItemId,
                slotIndex,
                ToResourceExchangeQueueStateKind(row.State),
                row.NumberText.ToString(),
                row.Name.ToString(),
                row.InputText.ToString(),
                row.OutputText.ToString(),
                row.TimeText.ToString(),
                row.PercentText.ToString(),
                row.StateText.ToString(),
                row.Progress01);
        }

        private static UiResourceExchangeQueueStateKind ToResourceExchangeQueueStateKind(
            UiResourceExchangeQueueState state)
        {
            switch (state)
            {
                case UiResourceExchangeQueueState.Pending:
                    return UiResourceExchangeQueueStateKind.Pending;
                case UiResourceExchangeQueueState.InProgress:
                    return UiResourceExchangeQueueStateKind.InProgress;
                case UiResourceExchangeQueueState.Completed:
                    return UiResourceExchangeQueueStateKind.Completed;
                case UiResourceExchangeQueueState.Cancelled:
                    return UiResourceExchangeQueueStateKind.Cancelled;
                case UiResourceExchangeQueueState.Blocked:
                    return UiResourceExchangeQueueStateKind.Blocked;
                default:
                    return UiResourceExchangeQueueStateKind.None;
            }
        }

        private static Sprite ResolveBuildDrawerSprite(FixedString64Bytes spriteKey)
        {
            return UiBuildDrawerReadModelSource.ResolveSprite(spriteKey.ToString());
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
                hasSelectedUnitsQuery = false;
                hasMinimapMarkerQuery = false;
                hasGridConfigQuery = false;
                hasResourceStorageQuery = false;
                hasAssistantMatchStartQuery = false;
            }

            if (!hasBoundaryQuery)
            {
                boundaryQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
                hasBoundaryQuery = true;
            }

            if (boundaryQuery.IsEmptyIgnoreFilter)
                return false;

            entityManager = world.EntityManager;
            boundary = boundaryQuery.GetSingletonEntity();
            return true;
        }

        private static bool IsAssistantRuntimeActive(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiShellStateComponent>(boundary))
                return false;

            UiShellStateComponent shell = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            if (shell.ActiveRoute != UIRoute.Match ||
                shell.CurrentMode != UiShellMode.MatchHud ||
                shell.IsTransitionRunning != 0)
            {
                return false;
            }

            if (!hasAssistantMatchStartQuery || cachedWorld != entityManager.World)
            {
                assistantMatchStartQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<MatchStartQueueComponent>());
                hasAssistantMatchStartQuery = true;
            }

            return !assistantMatchStartQuery.IsEmptyIgnoreFilter &&
                   assistantMatchStartQuery.GetSingleton<MatchStartQueueComponent>().HasStarted != 0;
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

        private static string GetDiagnosticsLogText(FixedString4096Bytes logText)
        {
            if (hasCachedDiagnosticsLogText && cachedDiagnosticsLogFixedText.Equals(logText))
                return cachedDiagnosticsLogText;

            cachedDiagnosticsLogFixedText = logText;
            cachedDiagnosticsLogText = logText.ToString();
            hasCachedDiagnosticsLogText = true;
            return cachedDiagnosticsLogText;
        }

        private static void EnsureUiActionRequestBuffer(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasBuffer<UiActionRequestComponent>(boundary))
                entityManager.AddBuffer<UiActionRequestComponent>(boundary);
        }

        private static void EnsureAssistantCommandIntentBuffers(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasBuffer<AssistantCommandIntentRequestElement>(boundary))
                entityManager.AddBuffer<AssistantCommandIntentRequestElement>(boundary);

            if (!entityManager.HasBuffer<AssistantCommandIntentResultElement>(boundary))
                entityManager.AddBuffer<AssistantCommandIntentResultElement>(boundary);

            if (!entityManager.HasBuffer<AssistantCommandDispatchElement>(boundary))
                entityManager.AddBuffer<AssistantCommandDispatchElement>(boundary);

            if (!entityManager.HasBuffer<AssistantPreviewHighlightElement>(boundary))
                entityManager.AddBuffer<AssistantPreviewHighlightElement>(boundary);
        }

        private static int NextAssistantCommandIntentRequestId(
            DynamicBuffer<AssistantCommandIntentRequestElement> requests,
            DynamicBuffer<AssistantCommandIntentResultElement> results)
        {
            int requestId = 0;
            for (int i = 0; i < requests.Length; i++)
                requestId = math.max(requestId, requests[i].RequestId);
            for (int i = 0; i < results.Length; i++)
                requestId = math.max(requestId, results[i].RequestId);

            return requestId + 1;
        }

        private static void EnsureLoadingProgressRequestBuffer(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasBuffer<UiShellLoadingProgressRequestComponent>(boundary))
                entityManager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        }

        private static void EnsureBuildDrawerState(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiBuildDrawerStateComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiBuildDrawerStateComponent
                {
                    ActiveCategory = BuildDrawerCategory.Buildings,
                    SelectedCatalogSlot = 0,
                    BuildingsCount = 2
                });
            }

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
                    Selected = 1,
                    Category = BuildDrawerCategory.Buildings,
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
                    Selected = 0,
                    Category = BuildDrawerCategory.Buildings,
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

        private static void EnsureResourceExchangeUiState(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiResourceExchangeStateComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiResourceExchangeStateComponent
                {
                    ActiveTab = UiResourceExchangeTab.Export,
                    SelectedRecipeSlot = 0,
                    QueueCapacityText = new FixedString32Bytes("0/0"),
                    CreditsText = new FixedString32Bytes("0"),
                    MaterialsText = new FixedString32Bytes("0"),
                    OilText = new FixedString32Bytes("0"),
                    FuelText = new FixedString32Bytes("0"),
                    RushTicketsText = new FixedString32Bytes("0")
                });
            }

            if (!entityManager.HasComponent<UiResourceExchangeDetailComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiResourceExchangeDetailComponent
                {
                    Name = new FixedString64Bytes("RESOURCE EXCHANGE"),
                    RouteText = new FixedString32Bytes("EXPORT"),
                    RequirementsText = new FixedString64Bytes("Exchange unavailable."),
                    InstructionText = new FixedString128Bytes("Resource Exchange is not enabled for this scenario.")
                });
            }

            if (!entityManager.HasBuffer<UiResourceExchangeRecipeCardComponent>(boundary))
                entityManager.AddBuffer<UiResourceExchangeRecipeCardComponent>(boundary);

            if (!entityManager.HasBuffer<UiResourceExchangeQueueRowComponent>(boundary))
                entityManager.AddBuffer<UiResourceExchangeQueueRowComponent>(boundary);
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
                Title = new FixedString64Bytes(GameText.Get("build.placement.title.default", "PLACE BUILDING")),
                Status = new FixedString64Bytes(GameText.Get("build.placement.status.valid_ground", "VALID GROUND")),
                CostText = new FixedString32Bytes("2,000"),
                DurationText = new FixedString32Bytes("00:30"),
                InstructionText = new FixedString128Bytes(GameText.Get("build.placement.instruction.confirm", "DRAG TO POSITION, CONFIRM TO BUILD"))
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
                Objective0Text = default,
                Objective1Text = default,
                Objective2Text = default,
                Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked,
                Objective1IconKind = UiMatchHudObjectiveIconKind.Unchecked,
                Objective2IconKind = UiMatchHudObjectiveIconKind.Unchecked,
                ElapsedText = default,
                ThreatVisible = 0,
                ThreatTitle = default,
                ThreatSubtitle = default,
                ThreatAudioEventId = default,
                JumpEnabled = 0,
                FeedbackVisible = 0,
                FeedbackText = default,
                FeedbackAudioEventId = default,
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

        bool IUiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(
            UiAssistantCommandIntentKind kind,
            bool fromTakeover)
        {
            return TryEnqueueAssistantCommandIntent(kind, fromTakeover);
        }

        bool IUiAssistantPanelStateGateway.TrySetAssistantPanelOpen(bool open)
        {
            return TrySetAssistantPanelOpen(open);
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

        bool IUiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel)
        {
            return TryReadMatchHudAssistantPanel(out assistantPanel);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight)
        {
            return TryReadMatchHudAssistantHighlight(out assistantHighlight);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
        {
            return TryReadMatchHudMinimap(out minimap);
        }

        bool IUiShellRuntimeGateway.TryReadBuildDrawer(out UiBuildDrawerModel drawer)
        {
            return TryReadBuildDrawer(out drawer);
        }

        bool IUiShellRuntimeGateway.TryReadResourceExchange(out UiResourceExchangeModel exchange)
        {
            return TryReadResourceExchange(out exchange);
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
}
