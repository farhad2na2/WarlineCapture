#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class GameplayArchitectureContractTests
{
    private const string ContractPath = "Design/Architecture/gameplay_solid_ecs_contract.md";
    private const string GameBootstrapAuditPath = "Design/Architecture/gamebootstrap_responsibility_audit.md";
    private const string GameBootstrapPath = "Assets/Game/Scripts/Bootstrap/GameBootstrap.cs";
    private const string RuntimeCityCompositionPath = "Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs";
    private const string ScriptsRoot = "Assets/Game/Scripts";
    private const string BootstrapRoot = "Assets/Game/Scripts/Bootstrap";
    private const string ScenesRoot = "Assets/Game/Scripts/Scenes";
    private const int LegacyGameBootstrapDirectLogCallCount = 7;

    private static readonly string[] LegacyAILogCallFiles = Array.Empty<string>();

    private static string ReadRuntimeCitySpawnerArchitectureSurface(string citySpawnerPath)
    {
        return File.ReadAllText(RuntimeCityCompositionPath);
    }

    public static void RunRuntimeCityArchitectureBatchValidation()
    {
        string[] methodNames =
        {
            nameof(RuntimeCitySpawnerSystemMustUseRuntimeCitySpawnBoundary),
            nameof(RuntimeCitySpawnerRefactorDocsMustRecordBaselineAndTargetBoundaries),
            nameof(RuntimeCitySpawnerBaselineMustStayExplicitUntilExtracted),
            nameof(RuntimeCityConfigProjectionMustLiveInRuntimeCityConfigSystem),
            nameof(RuntimeCityLayoutPlanningMustLiveInRuntimeCityLayoutSystem),
            nameof(RuntimeCityRoadLayoutPlanningMustLiveInRuntimeCityRoadLayoutSystem),
            nameof(RuntimeCityBuildingPlotPlanningMustLiveInRuntimeCityBuildingPlotSystem),
            nameof(RuntimeCityWalkabilityMustLiveInRuntimeCityWalkabilitySystem),
            nameof(RuntimeCityBuildingSpawnSequencingMustLiveInRuntimeCityBuildingSpawnSystem),
            nameof(RuntimeCitySpawnerFinalArchitectureGuardMustStayAlgorithmLight),
            nameof(RuntimeCityPrefabSelectionMustLiveInRuntimeCityPrefabSelectionSystem),
            nameof(RuntimeCityVisualRealizationMustLiveInRuntimeCityVisualSystem),
            nameof(RuntimeCitySpawnBridgeMustLiveInRuntimeCitySpawnBridgeSystem),
            nameof(RuntimeCityRoadBuildCouplingMustLiveInRuntimeCityRoadBuildBridgeSystem),
            nameof(RuntimeCityLifecycleMustLiveInRuntimeCityLifecycleSystem),
            nameof(RuntimeCityStartupGateMustLiveInRuntimeCityStartupSystem),
            nameof(RuntimeCityReadinessQueriesMustLiveInRuntimeCityReadinessQuerySystem),
            nameof(RuntimeCityGenerationSequenceMustLiveInRuntimeCityGenerationSystem),
            nameof(RuntimeCityChainConnectionPolicyMustLiveInRuntimeCityChainSystem),
            nameof(RuntimeCityRoadCommitSequenceMustLiveInRuntimeCityRoadCommitSystem),
            nameof(RuntimeCityIngressPolicyMustLiveInRuntimeCityIngressSystem),
            nameof(RuntimeCityDiagnosticsMustLiveInRuntimeCityDiagnosticSystem),
            nameof(RuntimeCityMinimapNotificationMustLiveInRuntimeCityMinimapEventSystem),
            nameof(RuntimeCityRuntimeRootOwnershipMustStayInVisualSystem),
            nameof(RuntimeCityCompositionMustOwnRuntimeCitySystemGraph),
            nameof(RuntimeCityPeerSystemsMustUseRuntimeCityReadModelSystem),
            nameof(RuntimeCitySpawnerSystemShellMustStayDeleted),
            nameof(RuntimeCityFinalContractMustTrackDeletedSpawnerShell)
        };

        try
        {
            var tests = new GameplayArchitectureContractTests();
            Type testType = typeof(GameplayArchitectureContractTests);
            for (int i = 0; i < methodNames.Length; i++)
            {
                System.Reflection.MethodInfo method = testType.GetMethod(methodNames[i]);
                Assert.NotNull(method, $"Missing runtime city architecture validation method {methodNames[i]}.");
                method.Invoke(tests, null);
            }

            UnityEngine.Debug.Log($"[RuntimeCityArchitectureValidation] result=Passed methods={methodNames.Length}");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Exception failure = ex is System.Reflection.TargetInvocationException && ex.InnerException != null
                ? ex.InnerException
                : ex;
            UnityEngine.Debug.LogException(failure);
            UnityEngine.Debug.LogError("[RuntimeCityArchitectureValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    private static readonly string[] HotAILogCallFiles = Array.Empty<string>();

    private static readonly string[] LegacyStaticLogFacadeFiles =
    {
        "Assets/Game/Scripts/UI/RuntimeLogBuffer.cs"
    };

    private static readonly string[] LegacyStaticInstanceFiles = Array.Empty<string>();

    private static readonly string[] LegacyStaticDependencyLocatorFiles = Array.Empty<string>();

    private static readonly string[] LegacyStaticRuntimeStateFiles =
    {
        "Assets/Game/Scripts/Systems/AISettingsRuntimeState.cs",
        "Assets/Game/Scripts/UI/InitialUnitsRuntimeState.cs",
        "Assets/Game/Scripts/UI/ThreatWarningRuntimeState.cs"
    };

    private static readonly string[] LegacyControllerFiles =
    {
        "Assets/Game/Scripts/TacticalMaps/M01PlayableVisualPrototypeController.cs",
        "Assets/Game/Scripts/UI/Components/BattleHudTacticalFeedbackController.cs",
        "Assets/Game/Scripts/UI/Popups/MissionResultPopupController.cs",
        "Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs",
        "Assets/Game/Scripts/UI/Screens/BuildDrawerPanelController.cs",
        "Assets/Game/Scripts/UI/Screens/CommandWheelPanelController.cs",
        "Assets/Game/Scripts/UI/Screens/CommanderProfileScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/DistrictDetailScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs",
        "Assets/Game/Scripts/UI/Screens/MatchObjectivePanelController.cs",
        "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandControlsController.cs",
        "Assets/Game/Scripts/UI/Screens/MissionBriefingScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/OperationCommandFeedScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/OperationDashboardScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/OperationEventsScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/OperationInboxScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/OperationLedgerScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/QuickCustomScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/SagaMapScreenController.cs",
        "Assets/Game/Scripts/UI/Screens/SplashScreenController.cs",
        "Assets/Game/Scripts/UI/Settings/SettingsScreenController.cs",
        "Assets/Game/Scripts/UI/Shell/WarlineCaptureModalController.cs",
        "Assets/Game/Scripts/UI/Shell/WarlineCaptureScreenController.cs"
    };

    private static readonly string[] LegacyBootstrapRootFiles =
    {
        "Assets/Game/Scripts/Bootstrap/GameBootstrap.cs",
        "Assets/Game/Scripts/Bootstrap/FactionVisualSettings.cs"
    };

    private static readonly string[] LegacyGameBootstrapDomainPolicyMethods = Array.Empty<string>();

    private static readonly string[] GameBootstrapDomainPolicyMethodTokens =
    {
        "AI",
        "AIBuild",
        "AIProduction",
        "AISquad",
        "AITarget",
        "FactionEconom",
        "FactionControl",
        "M01",
        "Mission",
        "Tactical",
        "CameraPose",
        "CameraStart",
        "FrameAnchor",
        "SpawnCell"
    };

    private static readonly string[] DomainPolicyTokens =
    {
        "M01",
        "Mission",
        "AI",
        "Combat",
        "Selection",
        "BuildingPlacement",
        "RoadBuild",
        "Tactical",
        "UnitSpawn",
        "UnitAttack"
    };

    [Test]
    public void GameplayArchitectureContractExists()
    {
        Assert.IsTrue(File.Exists(ContractPath), $"{ContractPath} must document the SOLID/ECS architecture contract.");

        string contract = File.ReadAllText(ContractPath);
        StringAssert.Contains("Bootstrap composes the application", contract);
        StringAssert.Contains("Gameplay runtime is ECS data plus ECS systems", contract);
        StringAssert.Contains("Runtime gameplay code must not introduce singleton access patterns", contract);
        StringAssert.Contains("`InitialUnitsRuntimeState` is legacy compatibility debt", contract);
        StringAssert.Contains("use `RuntimeGameplayStateSystem` as the compatibility boundary", contract);
        StringAssert.Contains("New domain gameplay types should end in `Entity`, `Component`, or `System`", contract);
        StringAssert.Contains("`*View` are serialized-reference binders only", contract);
        StringAssert.Contains("gamebootstrap_responsibility_audit.md", contract);
        StringAssert.Contains("AI startup config projection is owned by `AIStartupSystem`", contract);
        StringAssert.Contains("Faction economy startup projection is owned by `FactionEconomyStartupSystem`", contract);
        StringAssert.Contains("AI faction-control startup projection is owned by `AIFactionControlStartupSystem`", contract);
        StringAssert.Contains("AI default build and production fallback ids are owned by authored `AIPlanEntryStartupConfig` assets", contract);
        StringAssert.Contains("Mission startup is owned by `MissionStartupSystem`; M01 camera/framing policy is owned by `MissionCameraSystem`", contract);
        StringAssert.Contains("Configured faction spawn-cell resolution is owned by `InitialFactionSpawnCellSystem`", contract);
        StringAssert.Contains("Broad scene lookup and UI runtime binding are owned by `GameplaySceneBindingSystem`", contract);
        StringAssert.Contains("Performance diagnostics are owned by `PerformanceDiagnosticsSystem`", contract);
        StringAssert.Contains("Managed gameplay runtime update orchestration is owned by `GameplayRuntimeUpdateSystem`", contract);
        StringAssert.Contains("Building runtime updates inside that loop must go through `BuildingRuntimeUpdateSystem`", contract);
        StringAssert.Contains("`BuildingRuntimeUpdateSystem` ownership and context construction belong in managed composition", contract);
        StringAssert.Contains("it must invoke a narrow building runtime tick callback", contract);
        StringAssert.Contains("`GameBootstrap` must not hold a public or private `BuildingPlacementSystem` facade", contract);
        StringAssert.Contains("Managed building gameplay composition is owned by `BuildingGameplayCompositionSystem`", contract);
        StringAssert.Contains("`BuildingGameplayCompositionSystem` constructs `BuildingGameplaySystem`; the retired `BuildingPlacementSystem` facade must not exist", contract);
        StringAssert.Contains("`ManagedGameplayStartupSystem` may consume that composition result, but it must not hold or reach through `BuildingPlacementSystem`", contract);
        StringAssert.Contains("The retired `AILog` facade must not be reintroduced", contract);
        StringAssert.Contains("`BuildingPlacementSystem` must not exist", contract);
        StringAssert.Contains("active placement mutable state, active placement cost, and active placement preview handoff belong in `BuildingPlacementLifecycleSystem`", contract);
        StringAssert.Contains("active placement begin/cancel/confirm/exit command flow and selection-preservation state belong in `BuildingPlacementSessionSystem`", contract);
        StringAssert.Contains("placement grid/input/preview/commit context construction belongs in `BuildingPlacementContextSystem`", contract);
        StringAssert.Contains("wall run/origin validation, and wall overlap-cell checks belong in `BuildingPlacementValidationSystem`", contract);
        StringAssert.Contains("registry ownership, count/dictionary read access, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`", contract);
        StringAssert.Contains("Runtime building data creation, runtime registry insertion, blocker/combat entity hookup, runtime link attachment, initial production collections, produced-unit slot array setup, placement redirect side effects, and marker refresh policy belong in `BuildingRuntimeCreationSystem`", contract);
        StringAssert.Contains("Runtime building blocker entity creation, runtime building combat entity creation, path-blocking policy for runtime buildings, and runtime building combat component setup belong in `BuildingRuntimeEntitySystem`", contract);
        StringAssert.Contains("Runtime building read/query facades, faction building/unit/production counts, building role/id lists, owner/destroyed/city/refugee flags, combat entity info, focus-position queries, and building approach-cell query routing belong in `BuildingRuntimeQuerySystem`", contract);
        StringAssert.Contains("Citizen population building read paths must use `BuildingRuntimeQuerySystem`", contract);
        StringAssert.Contains("`CitizenPopulationSystem` must receive these narrow systems/contexts directly from managed composition and must not accept `BuildingPlacementSystem`", contract);
        StringAssert.Contains("Citizen upkeep spending belongs in `CitizenResourceSystem`", contract);
        StringAssert.Contains("citizen configured prefab/entity resolution belongs in `CitizenPrefabSystem`", contract);
        StringAssert.Contains("Runtime resource, runtime unit-prefab, citizen resource, citizen prefab, and building spawn-prefab context construction belongs in `BuildingRuntimeResourcePrefabContextSystem`", contract);
        StringAssert.Contains("Runtime/manual building spawn orchestration, initial test roster spawn requests, runtime wall-run/segment spawn orchestration, runtime placement footprint queries, runtime wall footprint queries, initial building origin search, and building-definition footprint cloning belong in `BuildingRuntimeSpawnSystem`; runtime/manual spawn command translation belongs in `BuildingRuntimeSpawnCommandSystem`", contract);
        StringAssert.Contains("Runtime city generated building spawn/delete/deferred-side-effect bridging belongs in `BuildingRuntimeCitySpawnSystem`", contract);
        StringAssert.Contains("Runtime city generation lifecycle state, spawned/generating flags, generation routine ownership, generation frame counters, and generation yield cadence belong in `RuntimeCityLifecycleSystem`", contract);
        StringAssert.Contains("Runtime city startup gating, spawn-on-start readiness, play-request checks, mission exclusion policy, dependency availability checks, required prefab readiness, initial-unit readiness gating, and startup gate result shaping belong in `RuntimeCityStartupSystem`", contract);
        StringAssert.Contains("Runtime city ECS readiness query ownership, grid-data query caching, grid config lookup, initial-unit readiness checks, and initial base exclusion road-rect collection belong in `RuntimeCityReadinessQuerySystem`", contract);
        StringAssert.Contains("`GameplayFeatureStartupSystem` must receive `BuildingRuntimeCitySpawnSystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition", contract);
        StringAssert.Contains("Building runtime tick orchestration and per-phase timing belong in `BuildingPlacementRuntimeTickSystem`; placement pointer/click frame flow belongs in `BuildingPlacementInputRuntimeTickSystem`; building runtime tick diagnostics threshold, enablement, timing normalization, and log formatting belong in `BuildingPlacementRuntimeTickDiagnosticsSystem`", contract);
        StringAssert.Contains("runtime tick context assembly belongs in `BuildingPlacementRuntimeTickContextSystem`, not `BuildingPlacementSystem`", contract);
        StringAssert.Contains("production progress ticking, resource production ticking, resource hauler ticking, and recent spawn reservation cleanup belong in `BuildingProductionRuntimeTickSystem`", contract);
        StringAssert.Contains("runtime boundary publish ticking belongs in `BuildingRuntimeBoundaryPublishSystem`", contract);
        StringAssert.Contains("Runtime building owner-faction assignment, combat `Faction` component projection, owner marker color projection, and gate friendly-pass blocker updates belong in `BuildingRuntimeOwnershipSystem`", contract);
        StringAssert.Contains("Runtime spawn, runtime creation, runtime ownership, runtime city-spawn, building spawn, runtime entity, runtime visual, redirect, combat, runtime query, and barrier context construction belongs in `BuildingRuntimeContextSystem`", contract);
        StringAssert.Contains("Placement redirect side-effect deferral, deferred redirect footprints, pending marker-refresh deferral, placed-building unit redirect scans, perimeter redirect-goal search, and redirect movement component mutation belong in `BuildingPlacementRedirectSystem`", contract);
        StringAssert.Contains("Building definition/configured spawnable/unit lookup, configured spawnable/unit prefab list/read access, spawnable/unit prefab lookup aliases, runtime building prefab metadata cache, prefab bounds/visual-footprint discovery, production spawn point metadata, production-slot read helpers, and runtime/configured building definition construction belong in `BuildingDefinitionSystem`", contract);
        StringAssert.Contains("Building placement config application, runtime building root creation, configured definition startup selection, build plane/camera/preview config state, and placement preview initialization belong in `BuildingPlacementStartupSystem`", contract);
        StringAssert.Contains("Building selection screen-click guards, screen-to-grid click routing, and selection-click context construction belong in `BuildingSelectionClickSystem`", contract);
        StringAssert.Contains("selection context construction, and runtime building cell hit-test/routing belong in `BuildingSelectionSystem`", contract);
        StringAssert.Contains("Building visual helper behavior, animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`; runtime building visual initialization, runtime resource animation updates, and runtime marker visibility projection belong in `BuildingRuntimeVisualSystem`", contract);
        StringAssert.Contains("Placement visual instance creation, placement visual positioning, prefab model bounds, and transformed bounds helpers belong in `BuildingPlacementVisualSystem`", contract);
        StringAssert.Contains("Building deletion orchestration, destruction state, cleanup timing, blocker cleanup, combat-health destruction checks, destroyed-entity callbacks, destroyed visual toggling, and destroyed building finalization belong in `BuildingCombatSystem`", contract);
        StringAssert.Contains("Resource storage classification, capacity display math, resource totals, faction economy snapshot contracts, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`", contract);
        StringAssert.Contains("Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`; resource-hauler update orchestration, selected-hauler assignment bridging, hauler move-order/path request bridging, building approach checks, and building approach-cell search belong in `BuildingResourceHaulerBridgeSystem`", contract);
        StringAssert.Contains("Unit production queue item initialization, player unit production queue mutation, pending production timing/progress, readiness checks, produced-unit liveness pruning, pending queue removal, ready/soon transport-pending lookup, production duration, transport settings/fallback policy, transport unit classification, and transport launch delay math belong in `BuildingProductionSystem`; production slot discovery, pending-slot reservation checks, slot occupancy cleanup, and production slot reservation belong in `BuildingProductionSlotSystem`; active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, and transport visual helpers belong in `BuildingProductionTransportSystem`; production transport ground-cell conversion, produced-unit movement orders, produced-unit rotation alignment, and transport-spawn bridging belong in `BuildingProductionTransportBridgeSystem`; produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad spawn fallback, and spawned ECS unit initialization belong in `BuildingSpawnSystem`; spawn-cell perimeter search helpers belong in `BuildingSpawnCellSystem`; spawn prefab registry lookup, prefab entity resolution, and live-unit prefab fallback lookup belong in `BuildingSpawnPrefabSystem`", contract);
        StringAssert.Contains("Production request, production queue, production update, production transport, production transport bridge, and resource hauler bridge context construction belongs in `BuildingProductionContextSystem`", contract);
        StringAssert.Contains("Selected-building unit production request routing, faction unit-production result contracts, faction unit-production request orchestration, camp item request failure policy, UI production arm consumption, friendly/faction producer lookup, production request focus, and last camp production focus memory belong in `BuildingProductionRequestSystem`", contract);
        StringAssert.Contains("Runway prefab metadata discovery, runway footprint expansion for placement validity, and nearest airport runway lookup belong in `BuildingRunwaySystem`", contract);
        StringAssert.Contains("Placement outline object lifetime, outline material/color updates, wall preview segment rebuilds, and preview segment validity tinting belong in `BuildingPlacementPreviewSystem`", contract);
        StringAssert.Contains("Placement commit expansion, wall-run origin construction, wall segment footprint/rotation helpers, wall segment runtime creation, committed placement preview consumption, and post-placement auto-select policy belong in `BuildingPlacementCommitSystem`", contract);
        StringAssert.Contains("Active placement pointer event orchestration, drag state, pointer-to-cell placement movement, wall drag axis/origin expansion, committed wall-run input state, and active-placement hit testing belong in `BuildingPlacementInputSystem`", contract);
        StringAssert.Contains("Placement/grid math, footprint center projection, center-screen placement origin resolution, screen-to-grid raycasts, placement footprint rotation, and placement focus bounds belong in `BuildingPlacementGridSystem`", contract);
        StringAssert.Contains("Placement status text, selected-building labels/descriptions, selected-building preview prefab lookup, selected-building health lookup, selected-building production prefab read models, and selected-building query context construction belong in `BuildingPlacementQuerySystem`", contract);
        StringAssert.Contains("Road barrier gate classification, gate-to-nearby-wall alignment, base-breach memory, enemy wall/gate perimeter lookup, breach-target resolution, breach-building target selection, breach approach-cell search, barrier door proximity checks, and barrier door visual open-state updates belong in `BuildingBarrierSystem`", contract);
        StringAssert.Contains("Produced-unit UI lists, pending-production UI entries, selected-building UI read models, minimap building read models, live-unit preview read models, UI progress shaping, and temporary building UI list read models belong in `BuildingUiQuerySystem`", contract);
        StringAssert.Contains("UI command/query context construction belongs in `BuildingUiContextSystem`, not `BuildingPlacementSystem`", contract);
        StringAssert.Contains("`BuildingUiCommandSystem` must not own read-model query delegates, query context construction, or pending-production UI list retrieval", contract);
        StringAssert.Contains("`MenuStartupSystem` must receive `BuildingUiCommandSystem`, `BuildingUiQuerySystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition", contract);
        StringAssert.Contains("`BuildingPlacementSystem` must not expose public building UI read/query or menu/camp command compatibility wrappers", contract);
        StringAssert.Contains("RoadBuildSystem and selection gameplay startup/building peer interactions belong behind `BuildingPlacementInteractionSystem`", contract);
        StringAssert.Contains("focused-unit lifecycle, focused entity validity checks, selected tag/focus synchronization, clear-selection selected-tag mutation, direct focus assignment, and clicked focus command routing belong in `FocusedUnitLifecycleSystem`", contract);
        StringAssert.Contains("focus command context construction belongs in `RtsSelectionFocusCommandContextSystem`", contract);
        StringAssert.Contains("pointer-target context construction belongs in `RtsSelectionPointerTargetCommandContextSystem`", contract);
        StringAssert.Contains("focused-unit UI read-model publication, focused labels/descriptions, health/capacity/status projection, focused transport passenger row projection, world-position projection, and portrait pose projection belong in `FocusedUnitUiReadModelSystem`", contract);
        StringAssert.Contains("attack-click target resolution, selected attacker query ownership, attack target validation dispatch, base-breach target resolution bridge, and attack issue result ownership belong in `AttackOrderCommandSystem`", contract);
        StringAssert.Contains("move/attack order marker prefab instantiation, runtime marker GameObject ownership, marker material property block ownership, marker show/hide timers, marker grid-blocked validation, and marker world positioning belong in `SelectionOrderMarkerSystem`", contract);
        StringAssert.Contains("HUD selection feedback, squad-selection labels, command mode feedback, command result feedback, world-marker visibility forwarding, the HUD feedback context contract, ECS feedback queue publication/consumption, and `BattleHudGameplayBridge` lookup/cache ownership belong in `SelectionHudFeedbackSystem`", contract);
        StringAssert.Contains("camera drag state, smooth focus state, zoom transition state, camera mode math, camera ground projection, camera pan/zoom mutation, and camera mode interpolation belong in `RtsCameraSystem`", contract);
        StringAssert.Contains("runtime camera context construction belongs in `RtsSelectionRuntimeCameraContextSystem`", contract);
        StringAssert.Contains("selected move-order click rejection, selected move-query consumption, manual move goal assignment orchestration, group path-request staggering, selected move-order diagnostics, and move-order command results belong in `SelectedMoveOrderCommandSystem`", contract);
        StringAssert.Contains("selection runtime cached query construction, selected-move query handles, selected-tag query handles, and grid-config query handles belong in `SelectionRuntimeQuerySystem`", contract);
        StringAssert.Contains("selected move command request consumption, selected move command execution dispatch, and ECS command result publication belong in `SelectionMoveCommandRequestSystem`", contract);
        StringAssert.Contains("selected attack command request consumption, clicked attack dispatch, ECS attack command result publication, and attack marker result payloads belong in `SelectionAttackCommandRequestSystem`", contract);
        StringAssert.Contains("command-result flush context construction belongs in `RtsSelectionCommandResultContextSystem`", contract);
        StringAssert.Contains("selected boarding-source collection, clicked/nearby transport resolution, transport boarding order creation, pending boarding-count checks, and boarding command diagnostics coordination belong in `TransportBoardingCommandSystem`", contract);
        StringAssert.Contains("transport boarding/disembark request consumption, boarding result marker payloads, focused transport disembark mutation, and transport command ECS result publication belong in `SelectionTransportCommandRequestSystem`", contract);
        StringAssert.Contains("The target architecture is no managed selection orchestration shell", contract);
        StringAssert.Contains("pointer press/release, drag, click, camera drag, selection rectangle, and command intent requests must use ECS data-only request components/buffers", contract);
        StringAssert.Contains("runtime input context construction belongs in `RtsSelectionRuntimeInputContextSystem`", contract);
        StringAssert.Contains("selection rectangle request consumption, visible unit collection for rectangle requests, selected-tag application, selected move cache update, selection focus handoff, and rectangle selection diagnostics belong in `SelectionRectangleRequestSystem`", contract);
        StringAssert.Contains("selection rectangle GUI rendering belongs in `SelectionRectangleView`, which reads `RtsSelectionInputStateComponent` through `RtsSelectionInputStateSystem` and must not own gameplay selection mutation", contract);
        StringAssert.Contains("UI command buttons must enqueue ECS selection command intents through `SelectionUiCommandSystem`", contract);
        StringAssert.Contains("UI selection read models must flow through `SelectionUiReadModelSystem`", contract);
        StringAssert.Contains("UI camera commands and selection screen-marker events must flow through `SelectionUiCameraSystem` and `SelectionScreenMarkerSystem`", contract);
        StringAssert.Contains("Mission and building camera focus delegates must flow through `SelectionUiCameraSystem`", contract);
        StringAssert.Contains("Building-side selection clearing, transport boarding click tests, and building-target move-order compatibility must flow through `SelectionBuildingInteractionSystem`", contract);
        StringAssert.Contains("Bootstrap, menu startup, and runtime update must receive selection behavior through narrow delegates and ECS/UI selection boundaries", contract);
        StringAssert.Contains("interaction context construction belongs in `BuildingPlacementInteractionContextSystem`, not `BuildingPlacementSystem`", contract);
        StringAssert.Contains("Runtime building entity-link callbacks must route through `BuildingPlacementInteractionSystem`", contract);
        StringAssert.Contains("AI/building cross-domain integration must move through `BuildingRuntimeBoundaryTag` ECS buffers", contract);
        StringAssert.Contains("`GameBootstrap` must not publish a managed `BuildingPlacementSystem` facade through ECS component objects", contract);
        StringAssert.Contains("`BuildingPlacementSystem` must not expose faction production, faction resource economy/sell, or faction count compatibility wrappers", contract);
        StringAssert.Contains("buildingplacement_retirement_audit.md", contract);
        StringAssert.Contains("do not reintroduce `BuildingPlacementSystem`", contract);
    }

    [Test]
    public void GameBootstrapResponsibilityAuditExists()
    {
        Assert.IsTrue(File.Exists(GameBootstrapAuditPath), $"{GameBootstrapAuditPath} must map bootstrap debt before refactoring.");

        string audit = File.ReadAllText(GameBootstrapAuditPath);
        StringAssert.Contains("Target Responsibility", audit);
        StringAssert.Contains("AI Startup Policy And Plan Mutation", audit);
        StringAssert.Contains("Faction Economy Startup Policy", audit);
        StringAssert.Contains("Fixed Tactical Mission Guardrails", audit);
        StringAssert.Contains("Camera And Framing Policy", audit);
        StringAssert.Contains("Gameplay Feature Runtime Updates", audit);
        StringAssert.Contains("Diagnostics And Performance Logging", audit);
        StringAssert.Contains("Broad Scene Lookup And UI Runtime Binding", audit);
        StringAssert.Contains("Recommended Migration Order", audit);
    }

    [Test]
    public void GameBootstrapDomainPolicyMethodDebtCannotGrow()
    {
        string[] violations = GetMethodNames(GameBootstrapPath)
            .Where(IsGameBootstrapDomainPolicyMethodName)
            .Where(method => !LegacyGameBootstrapDomainPolicyMethods.Contains(method, StringComparer.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Do not add new domain-policy methods to GameBootstrap. Move new AI, mission, camera, faction, spawning, or diagnostics policy into ECS systems/configs or audited startup boundaries:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void GameBootstrapDirectDebugLogDebtCannotGrow()
    {
        string code = File.ReadAllText(GameBootstrapPath);
        int directLogCallCount = Regex.Matches(code, @"Debug\.Log(?:Exception|Warning|Error)?\s*\(").Count;

        Assert.LessOrEqual(
            directLogCallCount,
            LegacyGameBootstrapDirectLogCallCount,
            "Do not add direct Debug.Log* calls to GameBootstrap. Move diagnostics into ECS diagnostic events or a shell logging service.");

        foreach (Match match in Regex.Matches(code, @"Debug\.Log(?:Warning|Error)?\s*\(\s*(?:\$)?""\[([^\]]+)\]"))
        {
            string category = match.Groups[1].Value;
            if (category.Contains("{", StringComparison.Ordinal))
                continue;

            Assert.IsTrue(
                category == "FreezeDetect" || category == "FrameRateDiag" || category == "FrameRateDiag:PreGame" || category == "PerfDiag" || category == "PerfDiag:PreGame",
                $"Unexpected GameBootstrap direct log category '{category}'. Add diagnostics through a logging boundary instead.");
        }
    }

    [Test]
    public void GameBootstrapMustDelegateAIStartupSlice()
    {
        const string aiStartupFile = "Assets/Game/Scripts/Systems/AIStartupSystem.cs";
        Assert.IsTrue(File.Exists(aiStartupFile), "AI startup config projection must live in AIStartupSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("AIStartupSystem _aiStartupSystem", bootstrap);
        StringAssert.Contains("_aiStartupSystem.LogConfigValidation", bootstrap);
        StringAssert.Contains("_aiStartupSystem.Initialize", bootstrap);

        string[] migratedMethodNames =
        {
            "LogAIConfigValidation",
            "ShouldQueueAIConfigDiagnostics",
            "TryEnqueueAIDiagnostic",
            "FlushQueuedAIDiagnostics",
            "EnsureFactionEconomiesInitialized",
            "EnsureFactionControlConfigInitialized",
            "EnsureAIBuildPlansInitialized",
            "AddBuildPlanEntries",
            "EnsureAIProductionPlansInitialized",
            "AddProductionPlanEntries",
            "EnsureAISquadPlansInitialized",
            "EnsureAITargetPrioritySettingsInitialized",
            "ShouldIncludeAIConfig"
        };

        foreach (string methodName in migratedMethodNames)
        {
            Assert.IsFalse(
                Regex.IsMatch(bootstrap, $@"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+{methodName}\s*\("),
                $"{methodName} belongs outside GameBootstrap.");
        }
    }

    [Test]
    public void AIStartupSystemMustDelegateDefaultPlanEntries()
    {
        const string aiStartupFile = "Assets/Game/Scripts/Systems/AIStartupSystem.cs";
        const string planEntryStartupFile = "Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs";
        const string planEntryConfigFile = "Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset";
        Assert.IsTrue(File.Exists(planEntryStartupFile), "AI plan entry buffer writing must live in AIPlanEntryStartupSystem.");
        Assert.IsTrue(File.Exists(planEntryConfigFile), "AI default plan entry ids must live in an authored AIPlanEntryStartupConfig asset.");

        string aiStartup = File.ReadAllText(aiStartupFile);
        StringAssert.Contains("AIPlanEntryStartupSystem _planEntryStartupSystem", aiStartup);
        StringAssert.Contains("_planEntryStartupSystem.WriteBuildPlanEntries", aiStartup);
        StringAssert.Contains("_planEntryStartupSystem.WriteProductionPlanEntries", aiStartup);
        StringAssert.Contains("AIPlanEntryStartupConfig planEntryConfig", aiStartup);

        string[] defaultIds =
        {
            "Tent_Regular",
            "Building_Barrack",
            "Building_OilPump",
            "Building_Fuel_Bladder",
            "Building_Ammunition_Depot",
            "Unit_Chr_Soldier_Male_02_Alt_04"
        };

        foreach (string defaultId in defaultIds)
        {
            Assert.IsFalse(aiStartup.Contains(defaultId, StringComparison.Ordinal), $"{defaultId} belongs in authored AIPlanEntryStartupConfig assets, not AIStartupSystem.");
            Assert.IsFalse(File.ReadAllText(planEntryStartupFile).Contains(defaultId, StringComparison.Ordinal), $"{defaultId} belongs in authored AIPlanEntryStartupConfig assets, not AIPlanEntryStartupSystem.");
            StringAssert.Contains(defaultId, File.ReadAllText(planEntryConfigFile));
        }

        Assert.IsFalse(
            Regex.IsMatch(aiStartup, @"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+Add(?:Build|Production)PlanEntries\s*\("),
            "AIStartupSystem should delegate default/preferred plan-entry population.");

        string planEntryStartup = File.ReadAllText(planEntryStartupFile);
        Assert.IsFalse(
            Regex.IsMatch(planEntryStartup, @"\bstatic\b"),
            "AIPlanEntryStartupSystem owns mutable ECS buffer population and should stay instance-scoped.");
        StringAssert.Contains("config.FallbackBuildingIds", planEntryStartup);
        StringAssert.Contains("config.FallbackProductionUnitIds", planEntryStartup);
    }

    [Test]
    public void AIStartupSystemMustDelegateFactionEconomyStartupProjection()
    {
        const string aiStartupFile = "Assets/Game/Scripts/Systems/AIStartupSystem.cs";
        const string economyStartupFile = "Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs";
        Assert.IsTrue(File.Exists(economyStartupFile), "Faction economy startup projection must live in FactionEconomyStartupSystem.");

        string aiStartup = File.ReadAllText(aiStartupFile);
        StringAssert.Contains("FactionEconomyStartupSystem _factionEconomyStartupSystem", aiStartup);
        StringAssert.Contains("_factionEconomyStartupSystem.Initialize", aiStartup);
        Assert.IsFalse(
            Regex.IsMatch(aiStartup, @"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+EnsureFactionEconomiesInitialized\s*\("),
            "AIStartupSystem should delegate faction economy projection.");
        Assert.IsFalse(aiStartup.Contains("new FactionEconomy", StringComparison.Ordinal), "AIStartupSystem must not construct FactionEconomy directly.");
        Assert.IsFalse(aiStartup.Contains("new FactionEconomyPolicy", StringComparison.Ordinal), "AIStartupSystem must not construct FactionEconomyPolicy directly.");

        string economyStartup = File.ReadAllText(economyStartupFile);
        StringAssert.Contains("new FactionEconomy", economyStartup);
        StringAssert.Contains("new FactionEconomyPolicy", economyStartup);
        Assert.IsFalse(
            Regex.IsMatch(economyStartup, @"\bstatic\b"),
            "FactionEconomyStartupSystem owns mutable ECS projection and should stay instance-scoped.");
    }

    [Test]
    public void AIStartupSystemMustDelegateFactionControlStartupProjection()
    {
        const string aiStartupFile = "Assets/Game/Scripts/Systems/AIStartupSystem.cs";
        const string factionControlStartupFile = "Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs";
        Assert.IsTrue(File.Exists(factionControlStartupFile), "Faction-control startup projection must live in AIFactionControlStartupSystem.");

        string aiStartup = File.ReadAllText(aiStartupFile);
        StringAssert.Contains("AIFactionControlStartupSystem _factionControlStartupSystem", aiStartup);
        StringAssert.Contains("_factionControlStartupSystem.Initialize", aiStartup);
        StringAssert.Contains("new Result(factionControlResult.HasPlayerAutoMode, factionControlResult.PlayerAutoModeEnabled)", aiStartup);
        Assert.IsFalse(
            Regex.IsMatch(aiStartup, @"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+EnsureFactionControlConfigInitialized\s*\("),
            "AIStartupSystem should delegate faction-control projection.");
        Assert.IsFalse(aiStartup.Contains("new FactionControlEntry", StringComparison.Ordinal), "AIStartupSystem must not construct FactionControlEntry directly.");
        Assert.IsFalse(aiStartup.Contains("CreateEntity(typeof(FactionControlConfigTag))", StringComparison.Ordinal), "AIStartupSystem must not create FactionControlConfigTag directly.");

        string factionControlStartup = File.ReadAllText(factionControlStartupFile);
        StringAssert.Contains("new FactionControlEntry", factionControlStartup);
        StringAssert.Contains("FactionControlConfigTag", factionControlStartup);
        Assert.IsFalse(
            Regex.IsMatch(factionControlStartup, @"\bstatic\b"),
            "AIFactionControlStartupSystem owns mutable ECS projection and should stay instance-scoped.");
    }

    [Test]
    public void AIStartupSystemMustNotUseStaticRuntimeHelpers()
    {
        const string aiStartupFile = "Assets/Game/Scripts/Systems/AIStartupSystem.cs";
        string code = File.ReadAllText(aiStartupFile);

        Assert.IsFalse(
            Regex.IsMatch(code, @"\bstatic\b"),
            "AIStartupSystem owns ECS startup mutation and diagnostics; keep it instance-scoped rather than adding static runtime helpers.");
        Assert.IsFalse(
            code.Contains("FixedTactical", StringComparison.Ordinal),
            "Mission-specific fixed tactical policy belongs in MissionStartupSystem, not AIStartupSystem.");
    }

    [Test]
    public void GameBootstrapMustDelegateMissionStartupAndCameraSlice()
    {
        const string missionStartupFile = "Assets/Game/Scripts/Systems/MissionStartupSystem.cs";
        const string missionCameraFile = "Assets/Game/Scripts/Systems/MissionCameraSystem.cs";
        const string initialFactionSpawnCellFile = "Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs";
        Assert.IsTrue(File.Exists(missionStartupFile), "Mission startup policy must live in MissionStartupSystem.");
        Assert.IsTrue(File.Exists(missionCameraFile), "M01 camera/framing policy must live in MissionCameraSystem.");
        Assert.IsTrue(File.Exists(initialFactionSpawnCellFile), "Configured faction spawn-cell resolution must live outside GameBootstrap.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("MissionStartupSystem _missionStartupSystem", bootstrap);
        StringAssert.Contains("InitialFactionSpawnCellSystem _initialFactionSpawnCellSystem", bootstrap);
        StringAssert.Contains("_missionStartupSystem.Initialize", bootstrap);
        StringAssert.Contains("_missionStartupSystem.FocusInitialCamera", bootstrap);
        StringAssert.Contains("_initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell", bootstrap);

        string runtimeUpdate = File.ReadAllText("Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs");
        StringAssert.Contains("missionStartupSystem.UpdateActiveMission", runtimeUpdate);
        StringAssert.Contains("missionStartupSystem.ApplyM01ProductionCameraPoseIfActive", runtimeUpdate);

        string[] migratedMethodNames =
        {
            "ApplyFixedTacticalMissionGuardrails",
            "DisableGenericAIPlansForFixedTacticalMission",
            "DisableAIBuildPlans",
            "DisableAIProductionPlans",
            "DisableAISquadPlans",
            "ApplyM01ProductionSceneVisibility",
            "FocusCameraOnConfiguredFactionBase",
            "FocusCameraOnM01CameraStart",
            "ApplyM01ProductionCameraPose",
            "ResolveM01ProductionOrthographicSize",
            "ApplyM01ProductionCameraPoseForCurrentAspect",
            "TryResolveM01ProductionFrameCenter",
            "IncludeM01FrameAnchor",
            "ApplyM01ProductionCameraPoseIfActive",
            "ClampM01CameraCenterToTacticalMap",
            "TryGetConfiguredFactionSpawnCell"
        };

        foreach (string methodName in migratedMethodNames)
        {
            Assert.IsFalse(
                Regex.IsMatch(bootstrap, $@"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+{methodName}\s*\("),
                $"{methodName} belongs in MissionStartupSystem, not GameBootstrap.");
        }

        string missionStartup = File.ReadAllText(missionStartupFile);
        StringAssert.Contains("MissionCameraSystem _missionCameraSystem", missionStartup);
        StringAssert.Contains("DisableGenericAIPlansForFixedTacticalMission", missionStartup);
        Assert.IsFalse(
            Regex.IsMatch(missionStartup, @"\bstatic\b"),
            "MissionStartupSystem owns mission startup mutation; keep it instance-scoped rather than adding static runtime helpers.");

        string[] cameraPolicyTokens =
        {
            "M01PlayableStartOrthographicSize",
            "M01PlayableCameraHeight",
            "ResolveM01ProductionOrthographicSize",
            "TryResolveM01ProductionFrameCenter",
            "IncludeM01FrameAnchor",
            "ClampM01CameraCenterToTacticalMap",
            "SetPositionAndRotation"
        };

        foreach (string token in cameraPolicyTokens)
        {
            Assert.IsFalse(
                missionStartup.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in MissionCameraSystem, not MissionStartupSystem.");
        }

        string missionCamera = File.ReadAllText(missionCameraFile);
        StringAssert.Contains("M01PlayableStartOrthographicSize", missionCamera);
        StringAssert.Contains("M01PlayableCameraHeight", missionCamera);
        StringAssert.Contains("SelectionUiCameraSystem selectionUiCameraSystem", missionCamera);
        StringAssert.Contains("ResolveM01ProductionOrthographicSize", missionCamera);
        StringAssert.Contains("TryResolveM01ProductionFrameCenter", missionCamera);
        StringAssert.Contains("ClampM01CameraCenterToTacticalMap", missionCamera);
        StringAssert.Contains("SetPositionAndRotation", missionCamera);
        Assert.IsFalse(
            missionCamera.Contains("RTSSelectionSystem", StringComparison.Ordinal) ||
            missionStartup.Contains("RTSSelectionSystem selection", StringComparison.Ordinal),
            "Mission camera focus must use SelectionUiCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(missionCamera, @"\bstatic\b"),
            "MissionCameraSystem owns camera/framing policy and must stay instance-scoped.");

        string initialFactionSpawnCell = File.ReadAllText(initialFactionSpawnCellFile);
        StringAssert.Contains("InitialUnitsSpawnConfig", initialFactionSpawnCell);
        StringAssert.Contains("InitialUnitsSpawnerAuthoringConfig", initialFactionSpawnCell);
        Assert.IsFalse(
            Regex.IsMatch(initialFactionSpawnCell, @"\bstatic\b"),
            "InitialFactionSpawnCellSystem owns configured spawn-cell lookup as injected state; keep it instance-scoped.");
    }

    [Test]
    public void GameBootstrapMustDelegatePerformanceDiagnostics()
    {
        const string performanceDiagnosticsFile = "Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs";
        Assert.IsTrue(File.Exists(performanceDiagnosticsFile), "Performance diagnostics must live in PerformanceDiagnosticsSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("PerformanceDiagnosticsSystem _performanceDiagnosticsSystem", bootstrap);
        StringAssert.Contains("_performanceDiagnosticsSystem.Initialize", bootstrap);
        StringAssert.Contains("_performanceDiagnosticsSystem.OnApplicationFocus", bootstrap);
        StringAssert.Contains("_performanceDiagnosticsSystem.OnApplicationPause", bootstrap);
        StringAssert.Contains("_performanceDiagnosticsSystem.Dispose", bootstrap);

        string[] bootstrapDiagnosticDebtTokens =
        {
            "[FreezeDetect]",
            "[FrameRateDiag]",
            "[FrameRateDiag:PreGame]",
            "[PerfDiag]",
            "[PerfDiag:PreGame]",
            "ProfilerRecorder",
            "FrameTimingManager",
            "GC.CollectionCount",
            "BuildGcDeltaString",
            "BuildProfilerMarkerDiagString"
        };

        foreach (string token in bootstrapDiagnosticDebtTokens)
        {
            Assert.IsFalse(
                bootstrap.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in PerformanceDiagnosticsSystem, not GameBootstrap.");
        }

        string diagnostics = File.ReadAllText(performanceDiagnosticsFile);
        StringAssert.Contains("[FreezeDetect]", diagnostics);
        StringAssert.Contains("FrameRateDiag", diagnostics);
        StringAssert.Contains("PerfDiag", diagnostics);
        StringAssert.Contains("ProfilerRecorder", diagnostics);
        StringAssert.Contains("FrameTimingManager", diagnostics);
        StringAssert.Contains("BeginStep", diagnostics);
        StringAssert.Contains("EndStep", diagnostics);
        Assert.IsFalse(
            Regex.IsMatch(diagnostics, @"\bstatic\b"),
            "PerformanceDiagnosticsSystem owns mutable recorder and frame state; keep it instance-scoped.");
    }

    [Test]
    public void GameBootstrapMustDelegateManagedRuntimeUpdateLoop()
    {
        const string runtimeUpdateFile = "Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs";
        const string buildingRuntimeUpdateFile = "Assets/Game/Scripts/Systems/BuildingRuntimeUpdateSystem.cs";
        const string buildingRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs";
        const string buildingRuntimeTickContextFile = "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs";
        const string buildingInputRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs";
        const string buildingRuntimeTickDiagnosticsFile = "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickDiagnosticsSystem.cs";
        const string buildingProductionRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs";
        const string buildingRuntimeBoundaryPublishFile = "Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryPublishSystem.cs";
        Assert.IsTrue(File.Exists(runtimeUpdateFile), "Managed runtime update orchestration must live in GameplayRuntimeUpdateSystem.");
        Assert.IsTrue(File.Exists(buildingRuntimeUpdateFile), "Building runtime update must live behind BuildingRuntimeUpdateSystem.");
        Assert.IsTrue(File.Exists(buildingRuntimeTickFile), "Building placement runtime frame tick orchestration must live in BuildingPlacementRuntimeTickSystem.");
        Assert.IsTrue(File.Exists(buildingRuntimeTickContextFile), "Building placement runtime frame tick context assembly must live in BuildingPlacementRuntimeTickContextSystem.");
        Assert.IsTrue(File.Exists(buildingInputRuntimeTickFile), "Building placement pointer/click frame flow must live in BuildingPlacementInputRuntimeTickSystem.");
        Assert.IsTrue(File.Exists(buildingRuntimeTickDiagnosticsFile), "Building placement runtime tick diagnostics must live in BuildingPlacementRuntimeTickDiagnosticsSystem.");
        Assert.IsTrue(File.Exists(buildingProductionRuntimeTickFile), "Building production/resource runtime frame ticks must live in BuildingProductionRuntimeTickSystem.");
        Assert.IsTrue(File.Exists(buildingRuntimeBoundaryPublishFile), "Building runtime boundary publish frame ticks must live in BuildingRuntimeBoundaryPublishSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        string managedStartup = File.ReadAllText("Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs");
        string buildingComposition = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs");
        string placement = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs");
        string buildingRuntimeTick = File.ReadAllText(buildingRuntimeTickFile);
        string buildingRuntimeTickContext = File.ReadAllText(buildingRuntimeTickContextFile);
        string buildingInputRuntimeTick = File.ReadAllText(buildingInputRuntimeTickFile);
        string buildingRuntimeTickDiagnostics = File.ReadAllText(buildingRuntimeTickDiagnosticsFile);
        string buildingProductionRuntimeTick = File.ReadAllText(buildingProductionRuntimeTickFile);
        string buildingRuntimeBoundaryPublish = File.ReadAllText(buildingRuntimeBoundaryPublishFile);
        StringAssert.Contains("GameplayRuntimeUpdateSystem _gameplayRuntimeUpdateSystem", bootstrap);
        StringAssert.Contains("BuildingRuntimeUpdateSystem BuildingRuntimeUpdate", bootstrap);
        StringAssert.Contains("_buildingRuntimeUpdateContext", bootstrap);
        StringAssert.Contains("_disposeBuildingGameplay", bootstrap);
        StringAssert.Contains("BuildingRuntimeUpdate = managedSystems.BuildingRuntimeUpdate", bootstrap);
        StringAssert.Contains("_disposeBuildingGameplay = managedSystems.DisposeBuildingGameplay", bootstrap);
        StringAssert.Contains("BuildingRuntimeUpdateSystem BuildingRuntimeUpdate", managedStartup);
        StringAssert.Contains("System.Action DisposeBuildingGameplay", managedStartup);
        StringAssert.Contains("new BuildingRuntimeUpdateSystem()", buildingComposition);
        StringAssert.Contains("new BuildingRuntimeUpdateSystem.Context(", buildingComposition);
        StringAssert.Contains("building.RuntimeTickSystem.Update", buildingComposition);
        StringAssert.Contains("BuildingPlacementRuntimeTickContextSystem _runtimeTickContextSystem", buildingComposition);
        StringAssert.Contains("_runtimeTickContextSystem.Create(CreateRuntimeTickSource(building))", buildingComposition);
        StringAssert.Contains("public readonly Action Dispose", buildingComposition);
        StringAssert.Contains("building.Dispose", buildingComposition);
        StringAssert.Contains("building.RuntimeUpdate", managedStartup);
        StringAssert.Contains("building.RuntimeUpdateContext", managedStartup);
        StringAssert.Contains("building.Dispose", managedStartup);
        StringAssert.Contains("_gameplayRuntimeUpdateSystem.Update", bootstrap);
        StringAssert.Contains("_gameplayRuntimeUpdateSystem.LateUpdate", bootstrap);
        StringAssert.Contains("_gameplayRuntimeUpdateSystem.OnGui", bootstrap);
        StringAssert.Contains("ref _gameplayStartPending", bootstrap);

        string[] bootstrapRuntimeUpdateDebtTokens =
        {
            "GameRuntimeStats.RecordMissionElapsed",
            "_missionStartupSystem.UpdateActiveMission",
            "_missionStartupSystem.ApplyM01ProductionCameraPoseIfActive",
            "RuntimeCity?.Update",
            "RuntimeGridBlockers?.Update",
            "RuntimeDecorations?.Update",
            "WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene",
            "IsGameplayStartComplete",
            "UnitAttackTraces?.LateUpdate",
            "UnitImpostors?.LateUpdate",
            "RoadBuild?.OnGui"
        };

        foreach (string token in bootstrapRuntimeUpdateDebtTokens)
        {
            Assert.IsFalse(
                bootstrap.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in GameplayRuntimeUpdateSystem, not GameBootstrap.");
        }

        string runtimeUpdate = File.ReadAllText(runtimeUpdateFile);
        string buildingRuntimeUpdate = File.ReadAllText(buildingRuntimeUpdateFile);
        string[] runtimeUpdateRequiredTokens =
        {
            "GameRuntimeStats.RecordMissionElapsed",
            "missionStartupSystem.UpdateActiveMission",
            "missionStartupSystem.ApplyM01ProductionCameraPoseIfActive",
            "runtimeCity?.Update",
            "runtimeGridBlockers?.Update",
            "runtimeDecorations?.Update",
            "WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene",
            "IsGameplayStartComplete",
            "unitAttackTraces?.LateUpdate",
            "unitImpostors?.LateUpdate",
            "roadBuild?.OnGui",
            "selectionRectangleView?.Draw"
        };

        foreach (string token in runtimeUpdateRequiredTokens)
            StringAssert.Contains(token, runtimeUpdate);

        StringAssert.Contains("BuildingRuntimeUpdateSystem", runtimeUpdate);
        StringAssert.Contains("buildingRuntimeUpdate?.Update", runtimeUpdate);
        StringAssert.Contains("Action UpdateBuildingRuntimeTick", buildingRuntimeUpdate);
        StringAssert.Contains("BuildingPlacementRuntimeTickSystem _buildingPlacementRuntimeTickSystem", placement);
        StringAssert.Contains("public readonly struct Source", buildingRuntimeTickContext);
        StringAssert.Contains("BuildingPlacementRuntimeTickSystem.Context Create(Source source)", buildingRuntimeTickContext);
        StringAssert.Contains("BuildingProductionRuntimeTickSystem _productionRuntimeTickSystem", buildingRuntimeTickContext);
        StringAssert.Contains("BuildingRuntimeBoundaryPublishSystem _runtimeBoundaryPublishSystem", buildingRuntimeTickContext);
        StringAssert.Contains("Func<BuildingPlacementInputRuntimeTickSystem.Result> UpdateInput", buildingRuntimeTickContext);
        StringAssert.Contains("BuildingPlacementRuntimeTickDiagnosticsSystem _diagnosticsSystem", buildingRuntimeTickContext);
        StringAssert.Contains("BuildingPlacementRuntimeTickDiagnosticsSystem.Context DiagnosticsContext", buildingRuntimeTickContext);
        StringAssert.Contains("_productionRuntimeTickSystem.ProcessPendingProductions", buildingRuntimeTickContext);
        StringAssert.Contains("_runtimeBoundaryPublishSystem.Update", buildingRuntimeTickContext);
        StringAssert.Contains("CreateProductionRuntimeTickContext", buildingComposition);
        StringAssert.Contains("CreateRuntimeBoundaryPublishContext", buildingComposition);
        StringAssert.Contains("CreateInputRuntimeTickContext", buildingComposition);
        StringAssert.Contains("CreateRuntimeTickDiagnosticsContext", buildingComposition);
        StringAssert.Contains("placement.ProductionContextSystem.CreateProductionUpdateContext(placement.CreateBuildingProductionContextSource())", buildingComposition);
        StringAssert.Contains("placement.RuntimeContextSystem.CreateSpawnContext(placement.CreateBuildingRuntimeContextSource())", buildingComposition);
        Assert.IsFalse(
            buildingRuntimeTickContext.Contains("BuildingPlacementSystem", StringComparison.Ordinal),
            "BuildingPlacementRuntimeTickContextSystem must consume a narrow source and must not depend on the BuildingPlacementSystem facade.");
        StringAssert.Contains("FreezeLogThresholdSeconds", buildingRuntimeTickDiagnostics);
        StringAssert.Contains("LogIfSlow", buildingRuntimeTickDiagnostics);
        StringAssert.Contains("[BuildingPlacementDiag]", buildingRuntimeTickDiagnostics);
        StringAssert.Contains("GetRuntimeBuildingCount", buildingRuntimeTickDiagnostics);
        StringAssert.Contains("GamePointerInput.TryGetPrimaryPointer", buildingInputRuntimeTick);
        StringAssert.Contains("UpdateActivePlacementPointer", buildingInputRuntimeTick);
        StringAssert.Contains("HandleBuildingSelectionClick", buildingInputRuntimeTick);
        StringAssert.Contains("ShouldIgnoreBuildingSelectionThisFrame", buildingInputRuntimeTick);
        StringAssert.Contains("IsPointerOverAnyGameplayUi", buildingInputRuntimeTick);
        StringAssert.Contains("IsPointerOverUnitCommandUi", buildingInputRuntimeTick);
        StringAssert.Contains("SuppressNextWorldClick", buildingInputRuntimeTick);
        StringAssert.Contains("ProcessPendingProductions", buildingProductionRuntimeTick);
        StringAssert.Contains("UpdateResourceProduction", buildingProductionRuntimeTick);
        StringAssert.Contains("UpdateResourceHaulers", buildingProductionRuntimeTick);
        StringAssert.Contains("CleanupRecentSpawnReservations", buildingProductionRuntimeTick);
        StringAssert.Contains("BoundarySystem?.Update", buildingRuntimeBoundaryPublish);
        StringAssert.Contains("GetBoundaryQuery", buildingRuntimeBoundaryPublish);
        StringAssert.Contains("ProcessPendingProductions", buildingRuntimeTick);
        StringAssert.Contains("UpdateBuildingRuntimeBoundary", buildingRuntimeTick);
        StringAssert.Contains("context.UpdateInput", buildingRuntimeTick);
        StringAssert.Contains("context.DiagnosticsSystem?.LogIfSlow", buildingRuntimeTick);
        Assert.IsFalse(
            buildingRuntimeTick.Contains("GamePointerInput.TryGetPrimaryPointer", StringComparison.Ordinal) ||
            buildingRuntimeTick.Contains("HandleBuildingSelectionClick", StringComparison.Ordinal) ||
            buildingRuntimeTick.Contains("ShouldIgnoreBuildingSelectionThisFrame", StringComparison.Ordinal) ||
            buildingRuntimeTick.Contains("IsPointerOverAnyGameplayUi", StringComparison.Ordinal) ||
            buildingRuntimeTick.Contains("IsPointerOverUnitCommandUi", StringComparison.Ordinal) ||
            buildingRuntimeTick.Contains("SuppressNextWorldClick", StringComparison.Ordinal),
            "Pointer/click frame flow belongs in BuildingPlacementInputRuntimeTickSystem, not BuildingPlacementRuntimeTickSystem.");
        Assert.IsFalse(
            placement.Contains("EnableBuildingPlacementDiagnostics", StringComparison.Ordinal) ||
            placement.Contains("DiagnosticsFreezeLogThresholdSeconds", StringComparison.Ordinal) ||
            placement.Contains("DiagnosticsEnabled", StringComparison.Ordinal) ||
            placement.Contains("[BuildingPlacementDiag]", StringComparison.Ordinal),
            "Building runtime tick diagnostics belong in BuildingPlacementRuntimeTickDiagnosticsSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            buildingComposition.Contains("placementFacade.Update", StringComparison.Ordinal),
            "BuildingRuntimeUpdateSystem must receive a runtime tick boundary callback, not a BuildingPlacementSystem.Update delegate.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+void\s+Update\s*\(") ||
            placement.Contains("CreateBuildingPlacementRuntimeTickContext", StringComparison.Ordinal) ||
            placement.Contains("_factionResourceSystem.UpdateResourceProduction", StringComparison.Ordinal) ||
            placement.Contains("_buildingResourceHaulerBridgeSystem.UpdateResourceHaulers", StringComparison.Ordinal) ||
            Regex.IsMatch(placement, @"\binternal\s+void\s+UpdateActivePlacementPointer\s*\(") ||
            Regex.IsMatch(placement, @"\binternal\s+void\s+HidePlacementOutline\s*\(") ||
            Regex.IsMatch(placement, @"\binternal\s+bool\s+ShouldIgnoreBuildingSelectionThisFrame\s*\(") ||
            Regex.IsMatch(placement, @"\binternal\s+void\s+SuppressNextWorldClick\s*\(") ||
            Regex.IsMatch(placement, @"\binternal\s+void\s+HandleBuildingSelectionClick\s*\(") ||
            Regex.IsMatch(placement, @"\binternal\s+bool\s+IsPointerOverAnyGameplayUi\s*\(") ||
            Regex.IsMatch(placement, @"\binternal\s+bool\s+IsPointerOverUnitCommandUi\s*\(") ||
            Regex.IsMatch(placement, @"\b(?:internal|private)\s+void\s+UpdateBuildingRuntimeBoundary\s*\(") ||
            Regex.IsMatch(placement, @"\b(?:internal|private)\s+void\s+ProcessPendingProductions\s*\("),
            "BuildingPlacementSystem must not keep a runtime Update compatibility wrapper, own runtime tick context assembly, or own production/resource/boundary tick phases.");
        Assert.IsFalse(
            runtimeUpdate.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            runtimeUpdate.Contains("buildingPlacement?.Update", StringComparison.Ordinal),
            "GameplayRuntimeUpdateSystem must call BuildingRuntimeUpdateSystem instead of the BuildingPlacementSystem facade.");
        Assert.IsFalse(
            bootstrap.Contains("BuildingPlacement?.BuildingRuntimeUpdateSystem", StringComparison.Ordinal) ||
            bootstrap.Contains("CreateBuildingRuntimeUpdateContext", StringComparison.Ordinal) ||
            bootstrap.Contains("public BuildingPlacementSystem BuildingPlacement", StringComparison.Ordinal) ||
            bootstrap.Contains("private BuildingPlacementSystem BuildingPlacement", StringComparison.Ordinal) ||
            bootstrap.Contains("BuildingPlacement = managedSystems.BuildingPlacement", StringComparison.Ordinal) ||
            placement.Contains("BuildingRuntimeUpdateSystem", StringComparison.Ordinal) ||
            placement.Contains("CreateBuildingRuntimeUpdateContext", StringComparison.Ordinal),
            "BuildingRuntimeUpdateSystem ownership/context construction belongs in managed composition, not BuildingPlacementSystem.");

        string[] orderedStepLabels =
        {
            "\"MenuCanvasInput\"",
            "\"MissionRuntime\"",
            "\"RoadBuild\"",
            "\"BuildingPlacement\"",
            "\"Selection\"",
            "\"MissionCamera\"",
            "\"RuntimeCity\"",
            "\"RuntimeGridBlockers\"",
            "\"RuntimeDecorations\"",
            "\"DayNight\"",
            "\"CitizenPopulation\"",
            "\"MenuCanvas\"",
            "\"MainMenu\""
        };

        int previousIndex = -1;
        foreach (string label in orderedStepLabels)
        {
            int index = runtimeUpdate.IndexOf(label, StringComparison.Ordinal);
            Assert.Greater(index, previousIndex, $"{label} must keep the established managed update order.");
            previousIndex = index;
        }

        Assert.IsFalse(
            Regex.IsMatch(runtimeUpdate, @"\bstatic\b"),
            "GameplayRuntimeUpdateSystem owns runtime update orchestration and should stay instance-scoped.");
    }

    [Test]
    public void GameBootstrapMustDelegateBroadSceneLookupAndUiRuntimeBinding()
    {
        const string sceneBindingFile = "Assets/Game/Scripts/Systems/GameplaySceneBindingSystem.cs";
        Assert.IsTrue(File.Exists(sceneBindingFile), "Broad scene lookup and UI runtime binding must live outside GameBootstrap.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("GameplaySceneBindingSystem _gameplaySceneBindingSystem", bootstrap);
        StringAssert.Contains("_menuStartupSystem.Initialize", bootstrap);

        string[] migratedMethodNames =
        {
            "BindRuntimeGridBlockerDebugViews",
            "BindGameplayUiRuntimeDependencies",
            "FindLoadedSceneComponent",
            "IsLoadedSceneObject"
        };

        foreach (string methodName in migratedMethodNames)
        {
            Assert.IsFalse(
                Regex.IsMatch(bootstrap, $@"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+{methodName}\s*\("),
                $"{methodName} belongs in GameplaySceneBindingSystem, not GameBootstrap.");
        }

        Assert.IsFalse(
            bootstrap.Contains("Resources.FindObjectsOfTypeAll", StringComparison.Ordinal),
            "GameBootstrap must not perform broad scene lookup directly. Use GameplaySceneBindingSystem or explicit scene references.");

        string sceneBinding = File.ReadAllText(sceneBindingFile);
        StringAssert.Contains("Resources.FindObjectsOfTypeAll", sceneBinding);
        StringAssert.Contains("BindGameplayUiRuntimeDependencies", sceneBinding);
        StringAssert.Contains("BindRuntimeGridBlockerDebugViews", sceneBinding);
        Assert.IsFalse(
            Regex.IsMatch(sceneBinding, @"\bstatic\b"),
            "GameplaySceneBindingSystem owns startup scene binding as injected instance behavior; keep it instance-scoped.");

        string menuStartup = File.ReadAllText("Assets/Game/Scripts/Systems/MenuStartupSystem.cs");
        StringAssert.Contains("sceneBindingSystem?.BindGameplayUiRuntimeDependencies", menuStartup);
    }

    [Test]
    public void GameBootstrapMustDelegateRuntimeRootCreation()
    {
        const string runtimeRootSystemFile = "Assets/Game/Scripts/Systems/RuntimeRootSystem.cs";
        string retiredRuntimeRootBootstrapFile = Path.Combine(BootstrapRoot, "RuntimeRoot" + "Installer.cs").Replace('\\', '/');
        Assert.IsFalse(File.Exists(retiredRuntimeRootBootstrapFile), "Runtime roots must follow the ECS-style System naming boundary.");
        Assert.IsTrue(File.Exists(runtimeRootSystemFile), "Runtime root creation must live in RuntimeRootSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("RuntimeRootSystem _runtimeRootSystem", bootstrap);
        StringAssert.Contains("_runtimeRootSystem.Ensure(transform, ref _runtimeBlockerRoot, ref _runtimeCityRoot, ref _runtimeUiRoot)", bootstrap);
        Assert.IsFalse(
            Regex.IsMatch(bootstrap, @"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+EnsureRuntimeRoots\s*\("),
            "Runtime root creation belongs in RuntimeRootSystem, not GameBootstrap.");
        Assert.IsFalse(bootstrap.Contains("new GameObject(\"RuntimeBlockers\")", StringComparison.Ordinal), "RuntimeBlockers root creation belongs in RuntimeRootSystem.");
        Assert.IsFalse(bootstrap.Contains("new GameObject(\"RuntimeCity\")", StringComparison.Ordinal), "RuntimeCity root creation belongs in RuntimeRootSystem.");
        Assert.IsFalse(bootstrap.Contains("new GameObject(\"RuntimeUi\")", StringComparison.Ordinal), "RuntimeUi root creation belongs in RuntimeRootSystem.");

        string runtimeRootSystem = File.ReadAllText(runtimeRootSystemFile);
        StringAssert.Contains("\"RuntimeBlockers\"", runtimeRootSystem);
        StringAssert.Contains("\"RuntimeCity\"", runtimeRootSystem);
        StringAssert.Contains("\"RuntimeUi\"", runtimeRootSystem);
        StringAssert.Contains("SetParent(owner, false)", runtimeRootSystem);
        Assert.IsFalse(
            Regex.IsMatch(runtimeRootSystem, @"\bstatic\b"),
            "RuntimeRootSystem must stay instance-scoped.");
    }

    [Test]
    public void GameBootstrapMustDelegateManagedGameplayStartup()
    {
        const string managedGameplayStartupFile = "Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs";
        Assert.IsTrue(File.Exists(managedGameplayStartupFile), "Managed gameplay construction and first-pass wiring must live in ManagedGameplayStartupSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("ManagedGameplayStartupSystem _managedGameplayStartupSystem", bootstrap);
        StringAssert.Contains("_managedGameplayStartupSystem.Initialize", bootstrap);
        StringAssert.Contains("EnsureBuildingRuntimeBoundaryEntity", bootstrap);
        StringAssert.Contains("_runtimeCameraReferenceSystem.SetWorldCamera", bootstrap);

        string[] managedStartupDebtTokens =
        {
            "new DayNightSystem()",
            "new FactionVisualSettings()",
            "new RoadBuildSystem()",
            "new BuildingPlacementSystem()",
            "new UnitAttackTraceSystem()",
            "new UnitImpostorRenderSystem()",
            "new CitizenPopulationSystem()",
            "GameStrings.Init",
            "SharedPrefabPreviewCache.Init"
        };

        foreach (string token in managedStartupDebtTokens)
        {
            Assert.IsFalse(
                bootstrap.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in ManagedGameplayStartupSystem, not GameBootstrap.");
        }

        string startup = File.ReadAllText(managedGameplayStartupFile);
        string buildingComposition = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs");
        foreach (string token in managedStartupDebtTokens)
        {
            if (token == "new BuildingPlacementSystem()")
                Assert.IsFalse(buildingComposition.Contains(token, StringComparison.Ordinal),
                    "BuildingGameplayCompositionSystem must construct BuildingGameplaySystem, not the legacy BuildingPlacementSystem facade.");
            else if (token == "new CitizenPopulationSystem()")
                StringAssert.Contains(token, buildingComposition);
            else
                StringAssert.Contains(token, startup);
        }
        Assert.IsFalse(startup.Contains("SelectionRuntimeContextSystem", StringComparison.Ordinal),
            "ManagedGameplayStartupSystem must not construct or reference the retired selection context.");
        StringAssert.Contains("new BuildingGameplaySystem()", buildingComposition);
        StringAssert.Contains("BuildingGameplayCompositionSystem _buildingGameplayCompositionSystem", startup);
        StringAssert.Contains("_buildingGameplayCompositionSystem.Initialize", startup);
        StringAssert.Contains("_buildingGameplayCompositionSystem.BindSelection", startup);
        StringAssert.Contains("_buildingGameplayCompositionSystem.CreateCitizenPopulation", startup);
        StringAssert.Contains("_buildingGameplayCompositionSystem.BindCitizenPopulation", startup);
        StringAssert.Contains("DisposeBuildingGameplay", startup);
        StringAssert.Contains("roadBuild.BindDependencies(", startup);
        StringAssert.Contains("building.Interaction", startup);
        StringAssert.Contains("building.InteractionContext", startup);
        StringAssert.Contains("BindSelectionMainMenu", startup);
        StringAssert.Contains("SelectionRuntimeUpdate", startup);
        StringAssert.Contains("DisposeSelection", startup);
        StringAssert.Contains("citizenPopulation.Init(", buildingComposition);
        StringAssert.Contains("BuildingRuntimeResourcePrefabContextSystem.Source resourcePrefabSource = Building.CreateRuntimeResourcePrefabContextSource()", buildingComposition);
        StringAssert.Contains("Building.RuntimeResourcePrefabContextSystem.CreateCitizenResourceContext(resourcePrefabSource)", buildingComposition);
        StringAssert.Contains("Building.RuntimeResourcePrefabContextSystem.CreateCitizenPrefabContext(resourcePrefabSource)", buildingComposition);
        Assert.IsFalse(
            startup.Contains("BuildingPlacementSystem BuildingPlacement", StringComparison.Ordinal) ||
            startup.Contains("BuildingPlacementSystem buildingPlacement", StringComparison.Ordinal) ||
            startup.Contains("building.PlacementFacade", StringComparison.Ordinal) ||
            startup.Contains("buildingPlacement.", StringComparison.Ordinal),
            "ManagedGameplayStartupSystem must consume BuildingGameplayCompositionSystem.Result instead of reaching through BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(startup, @"\bstatic\b"),
            "ManagedGameplayStartupSystem owns managed startup state and should stay instance-scoped.");
    }

    [Test]
    public void GameBootstrapMustDelegateMenuStartupBinding()
    {
        const string menuStartupFile = "Assets/Game/Scripts/Systems/MenuStartupSystem.cs";
        Assert.IsTrue(File.Exists(menuStartupFile), "Menu and UI startup binding must live in MenuStartupSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        string startup = File.ReadAllText(menuStartupFile);
        StringAssert.Contains("MenuStartupSystem _menuStartupSystem", bootstrap);
        StringAssert.Contains("_menuStartupSystem.Initialize", bootstrap);
        StringAssert.Contains("_menuStartupSystem.Shutdown", bootstrap);
        StringAssert.Contains("Debug.LogException", bootstrap);
        StringAssert.Contains("BuildingUiCommand", bootstrap);
        StringAssert.Contains("_buildingUiCommandContext", bootstrap);
        StringAssert.Contains("BuildingUiQuery", bootstrap);
        StringAssert.Contains("_buildingUiQueryContext", bootstrap);
        StringAssert.Contains("_buildingPlacementInteraction", bootstrap);
        StringAssert.Contains("_buildingPlacementInteractionContext", bootstrap);
        StringAssert.Contains("_bindBuildingMainMenu", bootstrap);

        string[] menuStartupDebtTokens =
        {
            "menuView.GameRequested +=",
            "menuView.GameRequested -=",
            "menuView.Init(",
            "menuView.NotifyBootstrapReady",
            "new MainMenuPlayUI()",
            "mainMenu.Init",
            "BindGameplayUiRuntimeDependencies"
        };

        foreach (string token in menuStartupDebtTokens)
        {
            Assert.IsFalse(
                bootstrap.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in MenuStartupSystem, not GameBootstrap.");
        }

        foreach (string token in menuStartupDebtTokens)
            StringAssert.Contains(token, startup);
        StringAssert.Contains("BuildingUiCommandSystem buildingUiCommand", startup);
        StringAssert.Contains("BuildingUiQuerySystem buildingUiQuery", startup);
        StringAssert.Contains("BuildingPlacementInteractionSystem buildingPlacementInteraction", startup);
        StringAssert.Contains("Action<MainMenuPlayUI> bindBuildingMainMenu", startup);
        Assert.IsFalse(
            startup.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            startup.Contains("buildingPlacement.", StringComparison.Ordinal),
            "MenuStartupSystem must use narrow building UI/interaction boundaries instead of BuildingPlacementSystem.");
        StringAssert.Contains("logException?.Invoke(exception)", startup);
        Assert.IsFalse(
            startup.Contains("Debug.LogException", StringComparison.Ordinal),
            "MenuStartupSystem must receive logging through a shell callback instead of calling Debug directly.");
        Assert.IsFalse(
            Regex.IsMatch(startup, @"\bstatic\b"),
            "MenuStartupSystem owns menu startup state and should stay instance-scoped.");
    }

    [Test]
    public void GameBootstrapMustDelegateGameplayFeatureStartup()
    {
        const string gameplayFeatureStartupFile = "Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs";
        Assert.IsTrue(File.Exists(gameplayFeatureStartupFile), "Runtime gameplay feature startup must live in GameplayFeatureStartupSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("GameplayFeatureStartupSystem _gameplayFeatureStartupSystem", bootstrap);
        StringAssert.Contains("_gameplayFeatureStartupSystem.Initialize", bootstrap);
        StringAssert.Contains("RuntimeCity = gameplaySystems.RuntimeCity", bootstrap);
        StringAssert.Contains("RuntimeGridBlockers = gameplaySystems.RuntimeGridBlockers", bootstrap);
        StringAssert.Contains("RuntimeDecorations = gameplaySystems.RuntimeDecorations", bootstrap);
        StringAssert.Contains("GameplayInitialized = true", bootstrap);
        Assert.IsFalse(
            Regex.IsMatch(bootstrap, @"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+EnsureGameplaySystemsInitialized\s*\("),
            "EnsureGameplaySystemsInitialized must not return to GameBootstrap.");

        string[] gameplayStartupDebtTokens =
        {
            "new RuntimeCityCompositionSystem()",
            "runtimeCity.Configure",
            "new RuntimeGridBlockerSystem()",
            "runtimeGridBlockers.Init",
            "BindRuntimeGridBlockerDebugViews(runtimeGridBlockers)",
            "new RuntimeDecorationSpawnerSystem()",
            "runtimeDecorations.Init"
        };

        foreach (string token in gameplayStartupDebtTokens)
        {
            Assert.IsFalse(
                bootstrap.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in GameplayFeatureStartupSystem, not GameBootstrap.");
        }

        string startup = File.ReadAllText(gameplayFeatureStartupFile);
        foreach (string token in gameplayStartupDebtTokens)
            StringAssert.Contains(token, startup);
        StringAssert.Contains("roadBuild?.BindDependencies(", startup);
        StringAssert.Contains("BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawn", startup);
        StringAssert.Contains("BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext", startup);
        StringAssert.Contains("BuildingPlacementInteractionSystem buildingPlacementInteraction", startup);
        StringAssert.Contains("BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext", startup);
        StringAssert.Contains("Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationSystem> bindBuildingGameplayFeatures", startup);
        Assert.IsFalse(
            startup.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            startup.Contains("buildingPlacement.", StringComparison.Ordinal),
            "GameplayFeatureStartupSystem must use narrow runtime city/interaction boundaries instead of BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(startup, @"\bstatic\b"),
            "GameplayFeatureStartupSystem owns feature startup state and should stay instance-scoped.");
    }

    [Test]
    public void NewBootstrapRootFilesMustBeCompositionOnly()
    {
        string[] rootBootstrapFiles = Directory.GetFiles(BootstrapRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(NormalizePath)
            .Where(path => !LegacyBootstrapRootFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        List<string> violations = new();
        foreach (string file in rootBootstrapFiles)
        {
            string text = File.ReadAllText(file);
            foreach (string token in DomainPolicyTokens)
            {
                if (text.Contains(token, StringComparison.Ordinal))
                    violations.Add($"{file} contains domain token '{token}'. Bootstrap root files must stay composition-only.");
            }
        }

        Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void NewBootstrapRootFilesMustUseCompositionBoundaryNaming()
    {
        string[] rootBootstrapFiles = Directory.GetFiles(BootstrapRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(NormalizePath)
            .Where(path => !LegacyBootstrapRootFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        List<string> violations = new();
        foreach (string file in rootBootstrapFiles)
        {
            foreach (string typeName in GetTopLevelTypeNames(file))
            {
                if (!typeName.EndsWith("System", StringComparison.Ordinal) &&
                    !typeName.EndsWith("Service", StringComparison.Ordinal) &&
                    !typeName.EndsWith("Registry", StringComparison.Ordinal) &&
                    !typeName.EndsWith("Config", StringComparison.Ordinal))
                {
                    violations.Add($"{file} declares '{typeName}'. New bootstrap root types must be systems, services, registries, or configs.");
                }
            }
        }

        Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void StaticAILogDebtCannotSpreadToNewFiles()
    {
        string[] filesWithAILogCalls = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => !path.EndsWith("/AILog.cs", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("AILog.", StringComparison.Ordinal))
            .ToArray();

        string[] newViolations = filesWithAILogCalls
            .Where(path => !LegacyAILogCallFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            newViolations,
            "Do not add new static AILog call sites. Use ECS log events or an injected logging service instead:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, newViolations));
    }

    [Test]
    public void RetiredAILogFacadeMustNotExist()
    {
        Assert.IsFalse(
            File.Exists("Assets/Game/Scripts/Systems/AILog.cs"),
            "AILog was retired after AI diagnostics migrated to ECS diagnostic events. Do not reintroduce the static facade.");
    }

    [Test]
    public void HotAiSystemsMustGuardAILogMessageConstruction()
    {
        List<string> violations = new();
        foreach (string file in HotAILogCallFiles)
        {
            string[] lines = File.ReadAllLines(file);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (!line.Contains("AILog.Log", StringComparison.Ordinal))
                    continue;

                bool guarded = false;
                int start = Math.Max(0, lineIndex - 8);
                for (int guardIndex = start; guardIndex <= lineIndex; guardIndex++)
                {
                    if (!lines[guardIndex].Contains("AILog.IsEnabled", StringComparison.Ordinal))
                        continue;

                    guarded = true;
                    break;
                }

                if (!guarded)
                    violations.Add($"{file}:{lineIndex + 1} constructs an AILog message without a nearby AILog.IsEnabled guard.");
            }
        }

        Assert.IsEmpty(
            violations,
            "AI hot systems must guard AILog message construction so disabled diagnostics do not allocate or format strings:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void AIBuildPlannerSystemMustUseEcsDiagnosticEvents()
    {
        const string buildPlannerFile = "Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs";
        string code = File.ReadAllText(buildPlannerFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AIBuildPlannerSystem must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("EnqueueDiagnostic", code);
    }

    [Test]
    public void AIProductionSystemMustUseEcsDiagnosticEvents()
    {
        const string productionFile = "Assets/Game/Scripts/Systems/AIProductionSystem.cs";
        string code = File.ReadAllText(productionFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AIProductionSystem must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("EnqueueDiagnostic", code);
    }

    [Test]
    public void AISquadSystemMustUseEcsDiagnosticEvents()
    {
        const string squadFile = "Assets/Game/Scripts/Systems/AISquadSystem.cs";
        string code = File.ReadAllText(squadFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AISquadSystem must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("EnqueueDiagnostic", code);
    }

    [Test]
    public void AITargetingSystemMustUseEcsDiagnosticEvents()
    {
        const string targetingFile = "Assets/Game/Scripts/Systems/AITargetingSystem.cs";
        string code = File.ReadAllText(targetingFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AITargetingSystem must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("EnqueueDiagnostic", code);
    }

    [Test]
    public void AICombatOrderSystemMustUseEcsDiagnosticEvents()
    {
        const string combatOrderFile = "Assets/Game/Scripts/Systems/AICombatOrderSystem.cs";
        string code = File.ReadAllText(combatOrderFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AICombatOrderSystem must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("EnqueueDiagnostic", code);
    }

    [Test]
    public void AIEconomySystemMustUseEcsDiagnosticEvents()
    {
        const string economyFile = "Assets/Game/Scripts/Systems/AIEconomySystem.cs";
        string code = File.ReadAllText(economyFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AIEconomySystem must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("EnqueueDiagnostic", code);
    }

    [Test]
    public void AIFactionControlSystemMustUseEcsDiagnosticEvents()
    {
        const string factionControlFile = "Assets/Game/Scripts/Systems/AIFactionControlSystem.cs";
        string code = File.ReadAllText(factionControlFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AIFactionControlSystem must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("EnqueueDiagnostic", code);
    }

    [Test]
    public void AIStartupConfigDiagnosticsMustUseEcsDiagnosticEvents()
    {
        const string aiStartupFile = "Assets/Game/Scripts/Systems/AIStartupSystem.cs";
        string code = File.ReadAllText(aiStartupFile);

        Assert.IsFalse(
            code.Contains("AILog.", StringComparison.Ordinal),
            "AI startup config diagnostics must use ECS diagnostic events instead of the static AILog facade.");
        StringAssert.Contains("AIDiagnosticLogComponent", code);
        StringAssert.Contains("TryEnqueueAIDiagnostic", code);
        StringAssert.Contains("FlushQueuedAIDiagnostics", code);
    }

    [Test]
    public void TransportBoardingDiagnosticsMustUseEcsDiagnosticEvents()
    {
        string[] transportDiagnosticFiles =
        {
            "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs",
            "Assets/Game/Scripts/Systems/SelectionRuntimeDiagnosticsSystem.cs"
        };

        foreach (string file in transportDiagnosticFiles)
        {
            string code = File.ReadAllText(file);
            Assert.IsFalse(
                Regex.IsMatch(code, @"Debug\.Log(?:Warning|Error)?\s*\(\s*\$?""\[TransportBoard\]"),
                $"{file} must queue transport boarding diagnostics through ECS events instead of calling Debug.Log directly.");
            Assert.IsFalse(
                code.Contains("RuntimeDiagnostics.ShouldLogTransportBoarding", StringComparison.Ordinal),
                $"{file} must read transport diagnostic enablement from RuntimeDiagnosticsStateComponent or a shell boundary.");
            StringAssert.Contains("TransportBoardingDiagnosticLogComponent", code);
            StringAssert.Contains("EnqueueTransportBoardingDiagnostic", code);
        }
        Assert.IsFalse(File.Exists("Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs"),
            "SelectionRuntimeContextSystem is retired and must not be reintroduced for diagnostics.");
    }

    [Test]
    public void ProductionScriptsMustNotReadVerboseAILogsFromLegacyRuntimeState()
    {
        string[] scriptFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => path != "Assets/Game/Scripts/UI/InitialUnitsRuntimeState.cs")
            .Where(path => path != "Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs")
            .ToArray();

        foreach (string file in scriptFiles)
        {
            string code = File.ReadAllText(file);
            Assert.IsFalse(
                code.Contains("InitialUnitsRuntimeState.VerboseAILogs", StringComparison.Ordinal),
                $"{file} must read/write VerboseAILogs through RuntimeDiagnosticsSystem or RuntimeDiagnosticsStateComponent.");
            Assert.IsFalse(
                code.Contains("InitialUnitsRuntimeState.ShouldLogAI", StringComparison.Ordinal),
                $"{file} must read AI log policy through RuntimeDiagnosticsSystem or RuntimeDiagnosticsStateComponent.");
        }
    }

    [Test]
    public void ProductionScriptsMustNotReadTransportBoardingDiagnosticsFromLegacyRuntimeState()
    {
        string[] scriptFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => path != "Assets/Game/Scripts/UI/InitialUnitsRuntimeState.cs")
            .Where(path => path != "Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs")
            .ToArray();

        foreach (string file in scriptFiles)
        {
            string code = File.ReadAllText(file);
            Assert.IsFalse(
                code.Contains("InitialUnitsRuntimeState.TransportBoardingDiagnostics", StringComparison.Ordinal),
                $"{file} must read/write TransportBoardingDiagnostics through RuntimeDiagnosticsSystem or RuntimeDiagnosticsStateComponent.");
        }
    }

    [Test]
    public void SceneStartupBoundariesMustNotHardcodeMissionOrRoutePolicy()
    {
        if (!Directory.Exists(ScenesRoot))
            Assert.Pass("No scene startup boundary folder exists.");

        string[] startupFiles = Directory.GetFiles(ScenesRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .ToArray();

        List<string> violations = new();
        foreach (string file in startupFiles)
        {
            string text = File.ReadAllText(file);
            if (text.Contains("ChapterOneMissionCatalog.", StringComparison.Ordinal))
                violations.Add($"{file} hardcodes a mission catalog id. Scene startup boundaries must read mission identity from config.");
            if (Regex.IsMatch(text, @"WarlineCaptureRoute\.[A-Za-z0-9_]+"))
                violations.Add($"{file} hardcodes a route value. Scene startup boundaries must read route policy from config.");
        }

        Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void RuntimeStaticLogFacadeDebtCannotSpread()
    {
        string[] staticLogFacades = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bstatic\s+class\s+\w*Log\w*\b"))
            .Where(path => !LegacyStaticLogFacadeFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            staticLogFacades,
            "Do not add new runtime static logging facades. Add an interface service or ECS log-event stream instead:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, staticLogFacades));
    }

    [Test]
    public void RuntimeStaticInstanceSingletonDebtCannotSpread()
    {
        string[] staticInstanceFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                @"\bstatic\s+[A-Za-z_][A-Za-z0-9_<>,\s\.]*\s+Instance\s*(?:\{|=>|;)"))
            .ToArray();

        string[] newViolations = staticInstanceFiles
            .Where(path => !LegacyStaticInstanceFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            newViolations,
            "Do not add new runtime singleton Instance declarations. Use bootstrap injection, service interfaces at the shell edge, or ECS singleton components instead:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, newViolations));
    }

    [Test]
    public void RuntimeStaticDependencyLocatorDebtCannotSpread()
    {
        string[] dependencyLocatorFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                @"\bstatic\s+[A-Za-z_][A-Za-z0-9_<>,\s\.]*\s+ResolveDependency\s*<"))
            .ToArray();

        string[] newViolations = dependencyLocatorFiles
            .Where(path => !LegacyStaticDependencyLocatorFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            newViolations,
            "Do not add new static dependency locator helpers. Pass dependencies through bootstrap composition or ECS request/response components instead:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, newViolations));
    }

    [Test]
    public void StaticRuntimeStateDebtCannotSpread()
    {
        string[] staticRuntimeStateFiles = Directory.GetFiles(ScriptsRoot, "*RuntimeState.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bstatic\s+class\b"))
            .ToArray();

        string[] newViolations = staticRuntimeStateFiles
            .Where(path => !LegacyStaticRuntimeStateFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            newViolations,
            "Do not add new static mutable gameplay runtime state files. Use ECS singleton components, normal components, buffers, or bootstrap-composed services instead:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, newViolations));
    }

    [Test]
    public void UiControllerNamingDebtCannotSpread()
    {
        string[] controllerFiles = Directory.GetFiles(ScriptsRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .ToArray();

        string[] newViolations = controllerFiles
            .Where(path => !LegacyControllerFiles.Contains(path, StringComparer.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            newViolations,
            "Do not add new gameplay/UI-flow Controller classes. Use View for serialized references and move behavior into ECS systems, services, or startup boundaries:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, newViolations));
    }

    [Test]
    public void BuildingPlacementSystemMustNotReachThroughRuntimeSingletonDependencies()
    {
        const string file = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        string text = File.ReadAllText(file);
        string[] forbiddenRuntimeSingletonReads =
        {
            "MainMenuPlayUI.Instance",
            "RoadBuildSystem.Instance",
            "RTSSelectionSystem.Instance",
            "RuntimeGridBlockerSystem.Instance",
            "RuntimeCitySpawnerSystem.Instance",
            "CitizenPopulationSystem.Instance"
        };

        string[] violations = forbiddenRuntimeSingletonReads
            .Where(token => text.Contains(token, StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "BuildingPlacementSystem dependencies must be supplied by GameBootstrap composition, not reacquired through runtime singleton Instance calls:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void UiPeerSystemsMustNotReachThroughBuildingPlacementSingleton()
    {
        string[] files =
        {
            "Assets/Game/Scripts/Systems/RoadBuildSystem.cs",
            "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs"
        };

        string[] violations = files
            .Where(file => File.ReadAllText(file).Contains("BuildingPlacementSystem.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "UI peer systems must use the BuildingPlacementSystem supplied by bootstrap composition, not BuildingPlacementSystem.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void RtsSelectionAndRoadBuildSystemsMustLiveOutsideUiOwnership()
    {
        string[] runtimeSystemFiles =
        {
            "Assets/Game/Scripts/Systems/RoadBuildSystem.cs",
            "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs"
        };

        string[] legacyUiFiles =
        {
            "Assets/Game/Scripts/UI/RoadBuildSystem.cs",
            "Assets/Game/Scripts/UI/RTSSelectionSystem.cs"
        };

        foreach (string file in runtimeSystemFiles)
        {
            Assert.IsTrue(File.Exists(file), $"{file} must live under runtime Systems ownership, not UI.");
        }

        string[] violations = legacyUiFiles
            .Where(File.Exists)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "RTS selection and road build are gameplay systems and must not be reintroduced under the UI folder:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void UiPeerSystemsMustUseBuildingPlacementInteractionBoundary()
    {
        const string interactionFile = "Assets/Game/Scripts/Systems/BuildingPlacementInteractionSystem.cs";
        const string interactionContextFile = "Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextSystem.cs";
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string roadFile = "Assets/Game/Scripts/Systems/RoadBuildSystem.cs";
        const string selectionStartupFile = "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs";
        const string runtimeCreationFile = "Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs";
        const string runtimeLinkFile = "Assets/Game/Scripts/UI/RuntimeBuildingEntityLink.cs";
        const string mainMenuPlayFile = "Assets/Game/Scripts/UI/MainMenuPlayUI.cs";
        const string managedStartupFile = "Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs";
        const string featureStartupFile = "Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs";
        const string menuStartupFile = "Assets/Game/Scripts/Systems/MenuStartupSystem.cs";
        Assert.IsTrue(File.Exists(interactionFile), "Road/selection building placement interactions must live behind BuildingPlacementInteractionSystem.");
        Assert.IsTrue(File.Exists(interactionContextFile), "Building placement interaction context construction must live behind BuildingPlacementInteractionContextSystem.");

        string interaction = File.ReadAllText(interactionFile);
        string interactionContext = File.ReadAllText(interactionContextFile);
        string placement = File.ReadAllText(placementFile);
        string road = File.ReadAllText(roadFile);
        string selectionStartup = File.ReadAllText(selectionStartupFile);
        string runtimeCreation = File.ReadAllText(runtimeCreationFile);
        string runtimeLink = File.ReadAllText(runtimeLinkFile);
        string mainMenuPlay = File.ReadAllText(mainMenuPlayFile);
        string managedStartup = File.ReadAllText(managedStartupFile);
        string buildingComposition = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs");
        string featureStartup = File.ReadAllText(featureStartupFile);
        string menuStartup = File.ReadAllText(menuStartupFile);

        StringAssert.Contains("BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem", road);
        StringAssert.Contains("BuildingPlacementInteractionSystem buildingPlacementInteractionSystem", selectionStartup);
        StringAssert.Contains("BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem", placement);
        StringAssert.Contains("BuildingPlacementInteractionContextSystem _buildingPlacementInteractionContextSystem", placement);
        StringAssert.Contains("CreateBuildingPlacementInteractionContext", placement);
        StringAssert.Contains("_buildingPlacementInteractionContextSystem.CreateContext", placement);
        StringAssert.Contains("TryResolveBaseBreachTargetDelegate", interaction);
        StringAssert.Contains("new BuildingPlacementInteractionSystem.Context", interactionContext);
        StringAssert.Contains("building.BuildingPlacementInteractionSystem", buildingComposition);
        StringAssert.Contains("building.Interaction", managedStartup);
        StringAssert.Contains("BuildingPlacementInteractionSystem buildingPlacementInteraction", featureStartup);
        StringAssert.Contains("buildingPlacementInteractionContext", featureStartup);
        StringAssert.Contains("BuildingPlacementInteractionSystem buildingPlacementInteraction", menuStartup);
        StringAssert.Contains("buildingPlacementInteractionContext", menuStartup);
        StringAssert.Contains("BuildingPlacementInteractionSystem RuntimeLinkInteractionSystem", runtimeCreation);
        StringAssert.Contains("BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem", runtimeLink);

        Assert.IsFalse(
            road.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            road.Contains("_buildingPlacementController", StringComparison.Ordinal),
            "RoadBuildSystem must use BuildingPlacementInteractionSystem instead of holding or calling BuildingPlacementSystem.");
        Assert.IsFalse(
            selectionStartup.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            selectionStartup.Contains("_buildingPlacementController", StringComparison.Ordinal),
            "Selection gameplay startup must use BuildingPlacementInteractionSystem instead of holding or calling BuildingPlacementSystem.");
        Assert.IsFalse(
            runtimeCreation.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            runtimeLink.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            runtimeLink.Contains("_buildingPlacementController", StringComparison.Ordinal),
            "Runtime building links must use BuildingPlacementInteractionSystem instead of holding or calling BuildingPlacementSystem.");
        Assert.IsFalse(
            mainMenuPlay.Contains("BuildingPlacementSystem", StringComparison.Ordinal),
            "MainMenuPlayUI must not accept unused BuildingPlacementSystem dependencies.");
        Assert.IsFalse(
            placement.Contains("new BuildingPlacementInteractionSystem.Context", StringComparison.Ordinal),
            "Building placement interaction context construction belongs in BuildingPlacementInteractionContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void AiSystemsMustNotReachThroughBuildingPlacementSingleton()
    {
        string[] files =
        {
            "Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs",
            "Assets/Game/Scripts/Systems/AICombatOrderSystem.cs",
            "Assets/Game/Scripts/Systems/AIEconomySystem.cs",
            "Assets/Game/Scripts/Systems/AIFactionControlSystem.cs",
            "Assets/Game/Scripts/Systems/AIProductionSystem.cs"
        };

        string[] violations = files
            .Where(file => File.ReadAllText(file).Contains("BuildingPlacementSystem.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "AI systems must read BuildingRuntimeBoundaryTag ECS buffers instead of BuildingPlacementSystem.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void BuildingRuntimeEcsBoundaryMustStayExplicit()
    {
        const string boundaryFile = "Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs";
        const string boundarySystemFile = "Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs";
        const string retiredRuntimeComponentFile = "Assets/Game/Scripts/Components/BuildingPlacementRuntimeComponent.cs";
        const string bootstrapFile = "Assets/Game/Scripts/Bootstrap/GameBootstrap.cs";
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        Assert.IsTrue(File.Exists(boundaryFile), "Building runtime ECS boundary components must be explicit ECS contracts.");
        Assert.IsTrue(File.Exists(boundarySystemFile), "Building runtime boundary publish/consume orchestration must live in BuildingRuntimeBoundarySystem.");
        Assert.IsFalse(File.Exists(retiredRuntimeComponentFile), "BuildingPlacementRuntimeComponent must stay retired; use BuildingRuntimeBoundaryTag buffers.");

        string boundary = File.ReadAllText(boundaryFile);
        string boundarySystem = File.ReadAllText(boundarySystemFile);
        string bootstrap = File.ReadAllText(bootstrapFile);
        string placement = File.ReadAllText(placementFile);
        string boundaryPublishSystem = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryPublishSystem.cs");
        string buildingComposition = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs");

        string[] boundaryContracts =
        {
            "BuildingRuntimeBoundaryTag",
            "BuildingConfiguredSpawnableReadModel",
            "BuildingConfiguredUnitReadModel",
            "BuildingRuntimeFactionSummary",
            "BuildingRuntimeOwnedBuildingSummary",
            "BuildingRuntimeUnitProductionSummary",
            "BuildingFactionUnitProductionRequest",
            "BuildingFactionResourceSellRequest",
            "BuildingRuntimeSpawnRequest"
        };

        foreach (string contract in boundaryContracts)
            StringAssert.Contains(contract, boundary);

        StringAssert.Contains("EnsureBuildingRuntimeBoundaryBuffers", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingConfiguredSpawnableReadModel>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingConfiguredUnitReadModel>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingRuntimeFactionSummary>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingRuntimeOwnedBuildingSummary>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingRuntimeUnitProductionSummary>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingFactionProductionSpawnPointReadModel>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingFactionUnitProductionRequest>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingFactionResourceSellRequest>", bootstrap);
        StringAssert.Contains("EnsureBuffer<BuildingRuntimeSpawnRequest>", bootstrap);
        StringAssert.Contains("EnsureBuildingRuntimeBoundaryEntity", bootstrap);
        Assert.IsFalse(
            bootstrap.Contains("AddComponentObject", StringComparison.Ordinal) ||
            bootstrap.Contains("BuildingPlacementRuntimeComponent", StringComparison.Ordinal) ||
            bootstrap.Contains("GetComponentObject<", StringComparison.Ordinal),
            "GameBootstrap must only install BuildingRuntimeBoundaryTag and buffers, not a managed BuildingPlacementSystem component object.");

        StringAssert.Contains("BuildingRuntimeBoundarySystem _buildingRuntimeBoundarySystem", placement);
        StringAssert.Contains("BuildingRuntimeBoundaryPublishSystem", boundaryPublishSystem);
        StringAssert.Contains("BoundarySystem?.Update", boundaryPublishSystem);
        StringAssert.Contains("placement.RuntimeBoundarySystem", buildingComposition);
        StringAssert.Contains("ProcessRequests", boundarySystem);
        StringAssert.Contains("ProcessResourceSellRequests", boundarySystem);
        StringAssert.Contains("FactionResourceSystem", boundarySystem);
        StringAssert.Contains("DrainFactionResource", boundarySystem);
        StringAssert.Contains("TryGetFactionResourceEconomy", boundarySystem);
        StringAssert.Contains("QueueFactionUnitProductionRequest", boundarySystem);
        StringAssert.Contains("ProcessRuntimeSpawnRequests", boundarySystem);
        StringAssert.Contains("TryResolveConfiguredBuildingDefinition", boundarySystem);
        StringAssert.Contains("TryPlaceRuntimeBuilding", boundarySystem);
        StringAssert.Contains("PublishReadModelIfDue", boundarySystem);
        StringAssert.Contains("PublishConfiguredSpawnablesReadModel", boundarySystem);
        StringAssert.Contains("PublishConfiguredUnitsReadModel", boundarySystem);
        StringAssert.Contains("PublishRuntimeUnitProductionSummaries", boundarySystem);
        StringAssert.Contains("TryGetConfiguredUnitReadModel", boundarySystem);
        StringAssert.Contains("CountRuntimeProducedUnitsForFaction", boundarySystem);
        StringAssert.Contains("CountPendingProductionsForFaction", boundarySystem);
        StringAssert.Contains("PublishRuntimeFactionSummaries", boundarySystem);
        StringAssert.Contains("BuildingRuntimeBoundaryTag", placement);
        StringAssert.Contains("PublishIntervalSeconds", boundarySystem);

        Assert.IsFalse(
            placement.Contains("PublishConfiguredSpawnablesReadModel", StringComparison.Ordinal) ||
            placement.Contains("PublishRuntimeFactionSummaries", StringComparison.Ordinal) ||
            placement.Contains("ProcessBuildingRuntimeEcsRequests", StringComparison.Ordinal) ||
            placement.Contains("PublishBuildingRuntimeEcsReadModelIfDue", StringComparison.Ordinal),
            "Building runtime boundary publish/consume logic belongs in BuildingRuntimeBoundarySystem, not BuildingPlacementSystem.");

        Assert.IsFalse(
            boundarySystem.Contains(".TrySpawnRuntimeBuilding", StringComparison.Ordinal) ||
            boundarySystem.Contains(".TryGetConfiguredSpawnable", StringComparison.Ordinal),
            "BuildingRuntimeBoundarySystem spawn requests must use BuildingRuntimeSpawnSystem and BuildingDefinitionSystem boundaries, not BuildingPlacementSystem facade spawn/config calls.");

        Assert.IsFalse(
            boundarySystem.Contains("buildingPlacement.TryQueueFactionUnitProduction", StringComparison.Ordinal) ||
            boundarySystem.Contains("BuildingPlacementSystem.FactionUnitProductionResult", StringComparison.Ordinal),
            "BuildingRuntimeBoundarySystem production requests must use BuildingProductionRequestSystem and BuildingProductionSystem ownership, not BuildingPlacementSystem facade production calls.");

        Assert.IsFalse(
            boundarySystem.Contains("buildingPlacement.TryGetConfiguredUnit", StringComparison.Ordinal) ||
            boundarySystem.Contains("buildingPlacement.CountRuntimeProducedUnitsForFaction", StringComparison.Ordinal) ||
            boundarySystem.Contains("buildingPlacement.CountPendingProductionsForFaction", StringComparison.Ordinal),
            "BuildingRuntimeBoundarySystem unit read-model and production-summary publishing must use BuildingDefinitionSystem and BuildingRuntimeQuerySystem directly.");

        Assert.IsFalse(
            boundarySystem.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            boundarySystem.Contains("buildingPlacement.", StringComparison.Ordinal) ||
            boundarySystem.Contains("SellFactionResources", StringComparison.Ordinal),
            "BuildingRuntimeBoundarySystem must not depend on BuildingPlacementSystem; resource requests and summaries must use FactionResourceSystem directly.");
    }

    [Test]
    public void AiFactionControlAndEconomyMustReadBuildingRuntimeBoundary()
    {
        const string factionControlFile = "Assets/Game/Scripts/Systems/AIFactionControlSystem.cs";
        const string economyFile = "Assets/Game/Scripts/Systems/AIEconomySystem.cs";
        string factionControl = File.ReadAllText(factionControlFile);
        string economy = File.ReadAllText(economyFile);

        StringAssert.Contains("BuildingRuntimeBoundaryTag", factionControl);
        StringAssert.Contains("BuildingRuntimeFactionSummary", factionControl);
        StringAssert.Contains("TryGetFactionBuildingCount", factionControl);
        Assert.IsFalse(
            factionControl.Contains("BuildingPlacementRuntimeComponent", StringComparison.Ordinal) ||
            factionControl.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            factionControl.Contains("CountRuntimeBuildingsForFaction", StringComparison.Ordinal),
            "AIFactionControlSystem building-count reads must come from BuildingRuntimeFactionSummary, not BuildingPlacementSystem.");

        StringAssert.Contains("BuildingRuntimeBoundaryTag", economy);
        StringAssert.Contains("BuildingRuntimeFactionSummary", economy);
        StringAssert.Contains("BuildingFactionResourceSellRequest", economy);
        StringAssert.Contains("EnqueueSellRequest", economy);
        StringAssert.Contains("ProcessCompletedSellRequests", economy);
        Assert.IsFalse(
            economy.Contains("buildingPlacement.TryGetFactionResourceEconomy", StringComparison.Ordinal) ||
            economy.Contains("BuildingPlacementSystem.FactionResourceEconomySnapshot", StringComparison.Ordinal) ||
            economy.Contains("BuildingPlacementRuntimeComponent", StringComparison.Ordinal) ||
            economy.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            economy.Contains("SellFactionResources", StringComparison.Ordinal),
            "AIEconomySystem building/resource reads and sell mutations must use ECS boundary buffers, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustNotOwnFactionResourceOrProductionResultContracts()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string factionResourceFile = "Assets/Game/Scripts/Systems/FactionResourceSystem.cs";
        const string productionRequestFile = "Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs";

        string placement = File.ReadAllText(placementFile);
        string factionResource = File.ReadAllText(factionResourceFile);
        string productionRequest = File.ReadAllText(productionRequestFile);

        StringAssert.Contains("public readonly struct FactionResourceEconomySnapshot", factionResource);
        StringAssert.Contains("public enum FactionUnitProductionResultCode", productionRequest);
        StringAssert.Contains("public readonly struct FactionUnitProductionResult", productionRequest);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+readonly\s+struct\s+FactionResourceEconomySnapshot") ||
            Regex.IsMatch(placement, @"public\s+enum\s+FactionUnitProductionResultCode") ||
            Regex.IsMatch(placement, @"public\s+readonly\s+struct\s+FactionUnitProductionResult"),
            "Faction/resource read and production result contracts belong to their owning systems, not BuildingPlacementSystem.");
    }

    [Test]
    public void AiProductionMustUseBuildingRuntimeBoundaryRequests()
    {
        const string productionFile = "Assets/Game/Scripts/Systems/AIProductionSystem.cs";
        string production = File.ReadAllText(productionFile);

        StringAssert.Contains("BuildingRuntimeBoundaryTag", production);
        StringAssert.Contains("BuildingConfiguredUnitReadModel", production);
        StringAssert.Contains("BuildingRuntimeUnitProductionSummary", production);
        StringAssert.Contains("BuildingFactionUnitProductionRequest", production);
        StringAssert.Contains("EnqueueProductionRequest", production);
        StringAssert.Contains("ProcessCompletedProductionRequests", production);
        Assert.IsFalse(
            production.Contains("BuildingPlacementRuntimeComponent", StringComparison.Ordinal) ||
            production.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            production.Contains("TryQueueFactionUnitProduction", StringComparison.Ordinal) ||
            production.Contains("TryGetConfiguredUnit", StringComparison.Ordinal) ||
            production.Contains("CountRuntimeProducedUnitsForFaction", StringComparison.Ordinal) ||
            production.Contains("CountPendingProductionsForFaction", StringComparison.Ordinal),
            "AIProductionSystem building/unit reads and production mutations must use ECS boundary buffers, not BuildingPlacementSystem.");
    }

    [Test]
    public void AiCombatOrderMustUseRuntimeBuildingCombatInfo()
    {
        const string combatOrderFile = "Assets/Game/Scripts/Systems/AICombatOrderSystem.cs";
        const string combatComponentsFile = "Assets/Game/Scripts/Components/CombatComponents.cs";
        const string runtimeEntityFile = "Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs";
        const string ownershipFile = "Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs";

        string combatOrder = File.ReadAllText(combatOrderFile);
        string combatComponents = File.ReadAllText(combatComponentsFile);
        string runtimeEntity = File.ReadAllText(runtimeEntityFile);
        string ownership = File.ReadAllText(ownershipFile);

        StringAssert.Contains("RuntimeBuildingCombatInfo", combatComponents);
        StringAssert.Contains("RuntimeBuildingCombatInfo", runtimeEntity);
        StringAssert.Contains("RuntimeBuildingCombatInfo", ownership);
        StringAssert.Contains("RuntimeBuildingCombatInfo", combatOrder);
        StringAssert.Contains("TryResolveBaseBreachTarget", combatOrder);
        Assert.IsFalse(
            combatOrder.Contains("BuildingPlacementRuntimeComponent", StringComparison.Ordinal) ||
            combatOrder.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            combatOrder.Contains("buildingPlacement", StringComparison.Ordinal),
            "AICombatOrderSystem must resolve base-breach orders from ECS RuntimeBuildingCombatInfo, not the managed BuildingPlacementSystem bridge.");
    }

    [Test]
    public void AiBuildPlannerMustUseBuildingRuntimeBoundaryRequests()
    {
        const string buildPlannerFile = "Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs";
        string buildPlanner = File.ReadAllText(buildPlannerFile);

        StringAssert.Contains("BuildingRuntimeBoundaryTag", buildPlanner);
        StringAssert.Contains("BuildingConfiguredSpawnableReadModel", buildPlanner);
        StringAssert.Contains("BuildingRuntimeFactionSummary", buildPlanner);
        StringAssert.Contains("BuildingRuntimeOwnedBuildingSummary", buildPlanner);
        StringAssert.Contains("BuildingRuntimeSpawnRequest", buildPlanner);
        StringAssert.Contains("EnqueueSpawnRequest", buildPlanner);
        StringAssert.Contains("ProcessCompletedSpawnRequests", buildPlanner);
        Assert.IsFalse(
            buildPlanner.Contains("BuildingPlacementRuntimeComponent", StringComparison.Ordinal) ||
            buildPlanner.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            buildPlanner.Contains("TrySpawnRuntimeBuilding", StringComparison.Ordinal) ||
            buildPlanner.Contains("TryGetConfiguredSpawnable", StringComparison.Ordinal) ||
            buildPlanner.Contains("CountRuntimeBuildingsForFaction", StringComparison.Ordinal),
            "AIBuildPlannerSystem building reads and spawn mutations must use ECS boundary buffers, not BuildingPlacementSystem.");
    }

    [Test]
    public void InitialSpawnSystemMustNotReachThroughBuildingPlacementSingleton()
    {
        const string file = "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs";
        string text = File.ReadAllText(file);

        StringAssert.Contains("BuildingRuntimeBoundaryTag", text);
        StringAssert.Contains("BuildingRuntimeSpawnRequest", text);
        StringAssert.Contains("BuildingConfiguredSpawnableReadModel", text);
        StringAssert.Contains("BuildingFactionProductionSpawnPointReadModel", text);
        StringAssert.Contains("CanCompleteInitialSpawn", text);
        StringAssert.Contains("RequiresInitialBuildingCompletion", text);
        Assert.IsFalse(
            text.Contains("BuildingPlacementRuntimeComponent", StringComparison.Ordinal) ||
            text.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            text.Contains("buildingPlacement", StringComparison.Ordinal) ||
            text.Contains("TrySpawnRuntimeBuilding", StringComparison.Ordinal),
            "InitialUnitsSpawnSystem must use ECS building runtime boundary buffers for initial base/building spawning, not BuildingPlacementSystem.");
    }

    [Test]
    public void RuntimeCitySpawnerSystemMustUseRuntimeCitySpawnBoundary()
    {
        const string file = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        string text = ReadRuntimeCitySpawnerArchitectureSurface(file);

        StringAssert.Contains("BuildingRuntimeCitySpawnSystem", text);
        Assert.IsFalse(
            text.Contains("BuildingPlacementSystem", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacement", StringComparison.Ordinal),
            "RuntimeCitySpawnerSystem must use the building runtime city-spawn boundary instead of the BuildingPlacementSystem facade.");
    }

    [Test]
    public void RuntimeCitySpawnerRefactorDocsMustRecordBaselineAndTargetBoundaries()
    {
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(roadmapPath), $"{roadmapPath} must track the RuntimeCitySpawnerSystem extraction plan.");
        Assert.IsTrue(File.Exists(auditPath), $"{auditPath} must inventory the baseline responsibilities before extraction.");

        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("1. Complete: Audit current responsibilities", roadmap);
        StringAssert.Contains("Current Allowed Debt", audit);
        StringAssert.Contains("must not call `BuildingPlacementSystem`", audit);

        string[] targetSystems =
        {
            "RuntimeCityConfigSystem",
            "RuntimeCityLayoutSystem",
            "RuntimeCityRoadLayoutSystem",
            "RuntimeCityBuildingPlotSystem",
            "RuntimeCityPrefabSelectionSystem",
            "RuntimeCityVisualSystem",
            "RuntimeCitySpawnBridgeSystem",
            "BuildingRuntimeCitySpawnSystem",
            "RuntimeCityRoadBuildBridgeSystem",
            "RuntimeCityWalkabilitySystem",
            "RuntimeCityBuildingSpawnSystem",
            "RuntimeCityLifecycleSystem",
            "RuntimeCityStartupSystem",
            "RuntimeCityReadinessQuerySystem",
            "RuntimeCityGenerationSystem",
            "RuntimeCityChainSystem",
            "RuntimeCityRoadCommitSystem",
            "RuntimeCityIngressSystem",
            "RuntimeCityMinimapEventSystem",
            "RuntimeCityDiagnosticSystem",
            "RuntimeCityCompositionSystem",
            "RuntimeCityReadModelSystem"
        };

        foreach (string targetSystem in targetSystems)
        {
            StringAssert.Contains(targetSystem, roadmap);
            StringAssert.Contains(targetSystem, audit);
        }
    }

    [Test]
    public void RuntimeCitySpawnerBaselineMustStayExplicitUntilExtracted()
    {
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string audit = File.ReadAllText(auditPath);

        string[] currentResponsibilityTokens =
        {
            "RuntimeCityCompositionSystem",
            "RuntimeCityGenerationSystem",
            "RuntimeCityIngressSystem",
            "RuntimeCityBuildingSpawnSystem",
            "RuntimeCityReadinessQuerySystem"
        };

        foreach (string token in currentResponsibilityTokens)
        {
            StringAssert.Contains(token, citySpawner);
            StringAssert.Contains(token, audit);
        }

        StringAssert.Contains("Deleted source file", audit);
        StringAssert.Contains("RuntimeCitySpawnerSystem.cs` must not be restored", audit);
    }

    [Test]
    public void RuntimeCityConfigProjectionMustLiveInRuntimeCityConfigSystem()
    {
        const string configSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityConfigSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(configSystemPath), "Runtime city config projection must live in RuntimeCityConfigSystem.");

        string configSystem = File.ReadAllText(configSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("2. Complete: Extract city config read model", roadmap);
        StringAssert.Contains("RuntimeCityConfigSystem.Snapshot", citySpawner);
        StringAssert.Contains("_runtimeCityConfigSystem.Apply(_config)", citySpawner);
        StringAssert.Contains("public readonly struct Snapshot", configSystem);
        StringAssert.Contains("public Snapshot Apply(RuntimeCitySpawnerSystemConfig config)", configSystem);
        StringAssert.Contains("Default(List<GameObject> emptyPrefabs)", configSystem);
        StringAssert.Contains("From(RuntimeCitySpawnerSystemConfig config", configSystem);
        StringAssert.Contains("prefab category lists", audit);

        string[] copiedAssignmentTokens =
        {
            "spawnOnStart = config.SpawnOnStart",
            "generateBuildings = config.GenerateBuildings",
            "randomSeed = config.RandomSeed",
            "cityCount = config.CityCount",
            "startCell = config.StartCell",
            "generationYieldInterval = config.GenerationYieldInterval",
            "hallPrefabs = config.HallPrefabs",
            "housePrefabs = config.HousePrefabs",
            "shopPrefabs = config.ShopPrefabs"
        };

        foreach (string token in copiedAssignmentTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityConfigSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityLayoutPlanningMustLiveInRuntimeCityLayoutSystem()
    {
        const string layoutSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityLayoutSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(layoutSystemPath), "Runtime city layout planning must live in RuntimeCityLayoutSystem.");

        string layoutSystem = File.ReadAllText(layoutSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("3. Complete: Extract city layout planning", roadmap);
        StringAssert.Contains("private readonly RuntimeCityLayoutSystem _runtimeCityLayoutSystem = new()", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityLayoutSystem", layoutSystem);
        StringAssert.Contains("public sealed class CityLayoutData", layoutSystem);
        StringAssert.Contains("using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;", layoutSystem);
        StringAssert.Contains("public int CalculateTownRadius", layoutSystem);
        StringAssert.Contains("public List<Vector2Int> BuildCityCenters", layoutSystem);
        StringAssert.Contains("public Vector2Int ClampRoadCellToBuildableArea", layoutSystem);
        StringAssert.Contains("public Vector2Int FindNearestRoadCellOutsideBaseExclusions", layoutSystem);
        StringAssert.Contains("public bool IsCityCenterFarEnough", layoutSystem);
        StringAssert.Contains("public void GetRoadGridBounds", layoutSystem);
        StringAssert.Contains("RuntimeCityLayoutSystem", audit);

        string[] retiredSpawnerLayoutTokens =
        {
            "private int CalculateTownRadius(",
            "private CityChainAxis ChooseCityChainAxis(",
            "private List<Vector2Int> BuildCityCenters(",
            "private List<Vector2Int> BuildLinearCityCenters(",
            "private Vector2Int ClampRoadCellToBuildableArea(",
            "private Vector2Int FindNearestRoadCellOutsideBaseExclusions(",
            "private static bool IsRoadCellInsideAnyBaseExclusion(",
            "private void GetRoadGridBounds(",
            "private static bool IsRoadCellWithinBounds(",
            "private bool IsCityCenterFarEnough("
        };

        foreach (string token in retiredSpawnerLayoutTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityLayoutSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityRoadLayoutPlanningMustLiveInRuntimeCityRoadLayoutSystem()
    {
        const string roadLayoutSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityRoadLayoutSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(roadLayoutSystemPath), "Runtime city road layout planning must live in RuntimeCityRoadLayoutSystem.");

        string roadLayoutSystem = File.ReadAllText(roadLayoutSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("4. Complete: Extract road layout planning", roadmap);
        StringAssert.Contains("private readonly RuntimeCityRoadLayoutSystem _runtimeCityRoadLayoutSystem = new()", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityRoadLayoutSystem", roadLayoutSystem);
        StringAssert.Contains("public struct AutobahnAnchorCandidate", roadLayoutSystem);
        StringAssert.Contains("public List<List<Vector2Int>> BuildTownRoadStrokes", roadLayoutSystem);
        StringAssert.Contains("public List<Vector2Int> BuildStraightRoadPath", roadLayoutSystem);
        StringAssert.Contains("public List<Vector2Int> BuildCityToCityAutobahnPath", roadLayoutSystem);
        StringAssert.Contains("public List<Vector2Int> BuildAutobahnPath", roadLayoutSystem);
        StringAssert.Contains("public void AddStroke", roadLayoutSystem);
        StringAssert.Contains("private static void AppendStraightSegment", roadLayoutSystem);
        StringAssert.Contains("RuntimeCityRoadLayoutSystem", audit);

        string[] retiredSpawnerRoadTokens =
        {
            "private static List<Vector2Int> BuildStraightRoadPath(",
            "private List<List<Vector2Int>> BuildTownRoadStrokes(",
            "private List<Vector2Int> BuildAutobahnPath(",
            "private static List<AutobahnAnchorCandidate> CollectAutobahnAnchorCandidates(",
            "private static int CalculateStepsToEdge(",
            "private static bool IsWithinRoadGridBounds(",
            "private static void AddStroke(",
            "private static void AppendStraightSegment(",
            "private List<Vector2Int> BuildCityToCityAutobahnPath(",
            "private static bool TrySelectDirectionalAutobahnAnchor("
        };

        foreach (string token in retiredSpawnerRoadTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityRoadLayoutSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityBuildingPlotPlanningMustLiveInRuntimeCityBuildingPlotSystem()
    {
        const string plotSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingPlotSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(plotSystemPath), "Runtime city building plot planning must live in RuntimeCityBuildingPlotSystem.");

        string plotSystem = File.ReadAllText(plotSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("5. Complete: Extract building plot selection", roadmap);
        StringAssert.Contains("private readonly RuntimeCityBuildingPlotSystem _runtimeCityBuildingPlotSystem = new()", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityBuildingPlotSystem", plotSystem);
        StringAssert.Contains("public struct PlotCandidate", plotSystem);
        StringAssert.Contains("public List<PlotCandidate> CollectRoadsidePlots", plotSystem);
        StringAssert.Contains("public List<PlotCandidate> CollectEntryRoadsidePlots", plotSystem);
        StringAssert.Contains("public List<PlotCandidate> BuildCorridorRoadsidePlots", plotSystem);
        StringAssert.Contains("public List<Vector2Int> BuildAdjacentOrigins", plotSystem);
        StringAssert.Contains("public Vector2Int GetRandomScatterPlotCell", plotSystem);
        StringAssert.Contains("public bool HasPlotSpacing", plotSystem);
        StringAssert.Contains("public Vector2Int GetCenteredOriginForPlot", plotSystem);
        StringAssert.Contains("RuntimeCityBuildingPlotSystem", audit);

        string[] retiredSpawnerPlotTokens =
        {
            "private static List<Vector2Int> BuildAdjacentOrigins(",
            "private static Vector2Int GetRandomScatterPlotCell(",
            "private static List<PlotCandidate> CollectRoadsidePlots(",
            "private static bool HasPlotSpacing(",
            "private static Vector2Int GetCenteredOriginForPlot("
        };

        foreach (string token in retiredSpawnerPlotTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityBuildingPlotSystem, not RuntimeCitySpawnerSystem.");
        }

        string[] retiredPlotWalkabilityTokens =
        {
            "public struct ReservedFootprint",
            "public void ReserveFootprint",
            "public void ReserveStandaloneEntranceCorridor",
            "public bool WouldBeTooCloseToReserved",
            "public bool CanPlaceHouseYardRect",
            "public bool DoesRectOverlapRoadCells",
            "public RectInt ExpandRect",
            "public bool TouchesRect"
        };

        foreach (string token in retiredPlotWalkabilityTokens)
        {
            Assert.IsFalse(
                plotSystem.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityWalkabilitySystem, not RuntimeCityBuildingPlotSystem.");
        }
    }

    [Test]
    public void RuntimeCityWalkabilityMustLiveInRuntimeCityWalkabilitySystem()
    {
        const string walkabilitySystemPath = "Assets/Game/Scripts/Environment/RuntimeCityWalkabilitySystem.cs";
        const string plotSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingPlotSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string buildingSpawnSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs";
        const string layoutSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityLayoutSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(walkabilitySystemPath), "Runtime city walkability and occupancy must live in RuntimeCityWalkabilitySystem.");

        string walkabilitySystem = File.ReadAllText(walkabilitySystemPath);
        string plotSystem = File.ReadAllText(plotSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string buildingSpawnSystem = File.ReadAllText(buildingSpawnSystemPath);
        string generationSystem = File.ReadAllText("Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs");
        string layoutSystem = File.ReadAllText(layoutSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("10. Complete: Extract occupancy/walkability publication", roadmap);
        StringAssert.Contains("private readonly RuntimeCityWalkabilitySystem _runtimeCityWalkabilitySystem = new()", citySpawner);
        StringAssert.Contains("using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;", buildingSpawnSystem);
        StringAssert.Contains("using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;", layoutSystem);
        StringAssert.Contains("context.WalkabilitySystem.ReserveStandaloneEntranceCorridor", generationSystem);
        StringAssert.Contains("_runtimeCityWalkabilitySystem.ReserveFootprint", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityWalkabilitySystem.CanPlaceHouseYardRect", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityWalkabilitySystem.ExpandRect", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityWalkabilitySystem.TouchesRect", buildingSpawnSystem);
        StringAssert.Contains("internal sealed class RuntimeCityWalkabilitySystem", walkabilitySystem);
        StringAssert.Contains("public struct ReservedFootprint", walkabilitySystem);
        StringAssert.Contains("public void ReserveFootprint", walkabilitySystem);
        StringAssert.Contains("public void ReserveStandaloneEntranceCorridor", walkabilitySystem);
        StringAssert.Contains("public bool WouldBeTooCloseToReserved", walkabilitySystem);
        StringAssert.Contains("public bool CanPlaceHouseYardRect", walkabilitySystem);
        StringAssert.Contains("public bool DoesRectOverlapRoadCells", walkabilitySystem);
        StringAssert.Contains("public RectInt ExpandRect", walkabilitySystem);
        StringAssert.Contains("public bool TouchesRect", walkabilitySystem);
        StringAssert.Contains("RuntimeCityWalkabilitySystem", audit);

        string[] retiredSpawnerWalkabilityTokens =
        {
            "private struct ReservedFootprint",
            "private static void ReserveFootprint(",
            "private static void ReserveStandaloneEntranceCorridor(",
            "private static bool WouldBeTooCloseToReserved(",
            "private static RectInt ExpandRect(",
            "private bool CanPlaceHouseYardRect(",
            "private static bool DoesRectOverlapRoadCells(",
            "private static bool TouchesRect("
        };

        foreach (string token in retiredSpawnerWalkabilityTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityWalkabilitySystem, not RuntimeCitySpawnerSystem.");
        }

        string[] retiredPlotWalkabilityTokens =
        {
            "public struct ReservedFootprint",
            "public void ReserveFootprint",
            "public void ReserveStandaloneEntranceCorridor",
            "public bool WouldBeTooCloseToReserved",
            "public bool CanPlaceHouseYardRect",
            "public bool DoesRectOverlapRoadCells",
            "public RectInt ExpandRect",
            "public bool TouchesRect"
        };

        foreach (string token in retiredPlotWalkabilityTokens)
        {
            Assert.IsFalse(
                plotSystem.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityWalkabilitySystem, not RuntimeCityBuildingPlotSystem.");
        }
    }

    [Test]
    public void RuntimeCityBuildingSpawnSequencingMustLiveInRuntimeCityBuildingSpawnSystem()
    {
        const string buildingSpawnSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(buildingSpawnSystemPath), "Runtime city building/decor spawn sequencing must live in RuntimeCityBuildingSpawnSystem.");

        string buildingSpawnSystem = File.ReadAllText(buildingSpawnSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string generationSystem = File.ReadAllText("Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs");
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("11. Complete: Reduce RuntimeCitySpawnerSystem to orchestrator", roadmap);
        StringAssert.Contains("private readonly RuntimeCityBuildingSpawnSystem _runtimeCityBuildingSpawnSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityBuildingSpawnSystem.Configure", citySpawner);
        StringAssert.Contains("context.BuildingSpawnSystem.EnsureCityHall", generationSystem);
        StringAssert.Contains("context.BuildingSpawnSystem.SpawnCityImportantBuildings", generationSystem);
        StringAssert.Contains("context.BuildingSpawnSystem.SpawnCorridorEntranceBuildings", generationSystem);
        StringAssert.Contains("context.BuildingSpawnSystem.SpawnCityBulkBuildingsRoutine", generationSystem);
        StringAssert.Contains("internal sealed class RuntimeCityBuildingSpawnSystem", buildingSpawnSystem);
        StringAssert.Contains("public void Configure", buildingSpawnSystem);
        StringAssert.Contains("public void SpawnCityImportantBuildings", buildingSpawnSystem);
        StringAssert.Contains("public void EnsureCityHall", buildingSpawnSystem);
        StringAssert.Contains("public IEnumerator SpawnCityBulkBuildingsRoutine", buildingSpawnSystem);
        StringAssert.Contains("public void SpawnCorridorEntranceBuildings", buildingSpawnSystem);
        StringAssert.Contains("private bool TrySpawnHall", buildingSpawnSystem);
        StringAssert.Contains("private void TrySpawnClockTower", buildingSpawnSystem);
        StringAssert.Contains("private void PlaceFromPlots", buildingSpawnSystem);
        StringAssert.Contains("private void PlaceRuralHouses", buildingSpawnSystem);
        StringAssert.Contains("private void PlaceHouseYardWalls", buildingSpawnSystem);
        StringAssert.Contains("private void PlaceCityDecorationBuildings", buildingSpawnSystem);
        StringAssert.Contains("RuntimeCityBuildingSpawnSystem", audit);

        string[] retiredSpawnerBuildingSpawnTokens =
        {
            "private void SpawnCityImportantBuildings(",
            "private void EnsureCityHall(",
            "private IEnumerator SpawnCityBulkBuildingsRoutine(",
            "private void SpawnCorridorEntranceBuildings(",
            "private bool TrySpawnHall(",
            "private void TrySpawnClockTower(",
            "private void PlaceFromPlots(",
            "private void PlaceRuralHouses(",
            "private void PlaceHouseYardWalls(",
            "private void PlaceCityDecorationBuildings("
        };

        foreach (string token in retiredSpawnerBuildingSpawnTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityBuildingSpawnSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCitySpawnerFinalArchitectureGuardMustStayAlgorithmLight()
    {
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("12. Complete: Architecture tests", roadmap);
        StringAssert.Contains("Step 12 complete", audit);
        StringAssert.Contains("_runtimeCityConfigSystem", citySpawner);
        StringAssert.Contains("_runtimeCityLayoutSystem", citySpawner);
        StringAssert.Contains("_runtimeCityRoadLayoutSystem", citySpawner);
        StringAssert.Contains("_runtimeCityBuildingPlotSystem", citySpawner);
        StringAssert.Contains("_runtimeCityWalkabilitySystem", citySpawner);
        StringAssert.Contains("_runtimeCityPrefabSelectionSystem", citySpawner);
        StringAssert.Contains("_runtimeCityBuildingSpawnSystem", citySpawner);
        StringAssert.Contains("_runtimeCityVisualSystem", citySpawner);
        StringAssert.Contains("_runtimeCitySpawnBridgeSystem", citySpawner);
        StringAssert.Contains("_runtimeCityRoadBuildBridgeSystem", citySpawner);
        StringAssert.Contains("_runtimeCityLifecycleSystem", citySpawner);
        StringAssert.Contains("_runtimeCityStartupSystem", citySpawner);
        StringAssert.Contains("_runtimeCityReadinessQuerySystem", citySpawner);
        StringAssert.Contains("_runtimeCityGenerationSystem", citySpawner);
        StringAssert.Contains("_runtimeCityChainSystem", citySpawner);
        StringAssert.Contains("_runtimeCityRoadCommitSystem", citySpawner);
        StringAssert.Contains("_runtimeCityIngressSystem", citySpawner);
        StringAssert.Contains("_runtimeCityDiagnosticSystem", citySpawner);

        string[] retiredAlgorithmTokens =
        {
            "private readonly Dictionary<GameObject, Vector2Int> _prefabFootprintCache",
            "GetComponentsInChildren<Renderer>",
            "private static GameObject GetRandomPrefab(",
            "private static void Shuffle<T>(",
            "private Vector2Int GetCachedFootprintCells(",
            "private static Vector2Int EstimateFootprintCells(",
            "private int GetMajorFootprint(",
            "private int GetMinorFootprint(",
            "private List<List<Vector2Int>> BuildTownRoadStrokes(",
            "private static List<Vector2Int> BuildStraightRoadPath(",
            "private List<Vector2Int> BuildAutobahnPath(",
            "private static void AppendStraightSegment(",
            "private struct ReservedFootprint",
            "public struct ReservedFootprint",
            "private static void ReserveFootprint(",
            "private static bool WouldBeTooCloseToReserved(",
            "private bool CanPlaceHouseYardRect(",
            "private GameObject SpawnVisualOnlyPrefab(",
            "private static bool TryGetLocalBounds(",
            "private bool TrySpawnCityBuilding(",
            "TrySpawnRuntimeBuilding(",
            "DeleteBuildingById(",
            "private void PlaceFromPlots(",
            "private void PlaceRuralHouses(",
            "private void PlaceHouseYardWalls(",
            "private void PlaceCityDecorationBuildings(",
            "private bool TrySpawnHall(",
            "private void TrySpawnClockTower(",
            "BuildingPlacementSystem.Instance",
            "private RoadBuildSystem _roadBuildController",
            "private BuildingRuntimeCitySpawnSystem _buildingRuntimeCitySpawnSystem"
        };

        foreach (string token in retiredAlgorithmTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} must stay out of RuntimeCitySpawnerSystem; use the extracted runtime city boundary systems.");
        }
    }

    [Test]
    public void RuntimeCityLifecycleMustLiveInRuntimeCityLifecycleSystem()
    {
        const string lifecycleSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(lifecycleSystemPath), "Runtime city lifecycle state must live in RuntimeCityLifecycleSystem.");

        string lifecycleSystem = File.ReadAllText(lifecycleSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string generationSystem = File.ReadAllText("Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs");
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("15. Complete: Extract city lifecycle state", roadmap);
        StringAssert.Contains("RuntimeCityLifecycleSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityLifecycleSystem _runtimeCityLifecycleSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityLifecycleSystem.Tick(CreateLifecycleContext(frameCount))", citySpawner);
        StringAssert.Contains("context.LifecycleSystem.TryBeginGeneration", generationSystem);
        StringAssert.Contains("context.LifecycleSystem.CompleteGeneration", generationSystem);
        StringAssert.Contains("_runtimeCityLifecycleSystem.CancelGeneration", citySpawner);
        StringAssert.Contains("_runtimeCityLifecycleSystem.ShouldYield", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityLifecycleSystem", lifecycleSystem);
        StringAssert.Contains("private IEnumerator _generationRoutine", lifecycleSystem);
        StringAssert.Contains("private int _generationStartedFrame", lifecycleSystem);
        StringAssert.Contains("private int _generationMoveNextCount", lifecycleSystem);
        StringAssert.Contains("private int _nextGenerationDiagnosticFrame", lifecycleSystem);
        StringAssert.Contains("private bool _spawned", lifecycleSystem);
        StringAssert.Contains("public bool TryBeginGeneration", lifecycleSystem);
        StringAssert.Contains("public void Tick", lifecycleSystem);
        StringAssert.Contains("public void CompleteGeneration", lifecycleSystem);
        StringAssert.Contains("public bool ShouldYield", lifecycleSystem);

        string[] retiredSpawnerLifecycleTokens =
        {
            "private IEnumerator _generationRoutine",
            "private int _generationStartedFrame",
            "private int _generationMoveNextCount",
            "private int _nextGenerationDiagnosticFrame",
            "private bool _spawned",
            "_generationRoutine.MoveNext()",
            "if (_generationRoutine != null)"
        };

        foreach (string token in retiredSpawnerLifecycleTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityLifecycleSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityStartupGateMustLiveInRuntimeCityStartupSystem()
    {
        const string startupSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(startupSystemPath), "Runtime city startup gate policy must live in RuntimeCityStartupSystem.");

        string startupSystem = File.ReadAllText(startupSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("16. Complete: Extract runtime city startup gate", roadmap);
        StringAssert.Contains("RuntimeCityStartupSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityStartupSystem _runtimeCityStartupSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityStartupSystem.Evaluate(CreateStartupContext(frameCount))", citySpawner);
        StringAssert.Contains("_runtimeCityStartupSystem.EvaluateManualGeneration(CreateStartupContext(frameCount))", citySpawner);
        StringAssert.Contains("private RuntimeCityStartupSystem.Context CreateStartupContext(int frameCount)", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityStartupSystem", startupSystem);
        StringAssert.Contains("public Result Evaluate(Context context)", startupSystem);
        StringAssert.Contains("public Result EvaluateManualGeneration(Context context)", startupSystem);
        StringAssert.Contains("private static Result TryCreateGenerateResult", startupSystem);
        StringAssert.Contains("private void LogInitialSpawnWait", startupSystem);
        StringAssert.Contains("private static bool HasRequiredPrefabs", startupSystem);
        StringAssert.Contains("public enum ResultKind", startupSystem);
        StringAssert.Contains("MarkSpawned", startupSystem);
        StringAssert.Contains("Generate", startupSystem);

        string[] retiredSpawnerStartupTokens =
        {
            "if (!spawnOnStart || _runtimeCityLifecycleSystem.IsSpawned)",
            "if (!_runtimeGameplayStateSystem.PlayRequested)",
            "if (Chapter01M01PlayableRuntime.IsActiveMission())\n        {",
            "if (HasPendingInitialUnitsSpawn(out int initialSpawnConfigs",
            "_nextInitialSpawnWaitDiagnosticFrame",
            "reason=waiting-initial-units",
            "if (!_runtimeCityRoadBuildBridgeSystem.HasRoadBuildSystem)",
            "if (generateBuildings && !_runtimeCitySpawnBridgeSystem.HasSpawnSystem)",
            "hallPrefabs == null || hallPrefabs.Count == 0",
            "shopPrefabs == null || shopPrefabs.Count == 0",
            "housePrefabs == null || housePrefabs.Count == 0"
        };

        foreach (string token in retiredSpawnerStartupTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityStartupSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityReadinessQueriesMustLiveInRuntimeCityReadinessQuerySystem()
    {
        const string readinessSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityReadinessQuerySystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(readinessSystemPath), "Runtime city ECS readiness queries must live in RuntimeCityReadinessQuerySystem.");

        string readinessSystem = File.ReadAllText(readinessSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("17. Complete: Extract ECS query/readiness ownership", roadmap);
        StringAssert.Contains("RuntimeCityReadinessQuerySystem", audit);
        StringAssert.Contains("private readonly RuntimeCityReadinessQuerySystem _runtimeCityReadinessQuerySystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityReadinessQuerySystem.Clear()", citySpawner);
        StringAssert.Contains("_runtimeCityReadinessQuerySystem.CollectInitialBaseExclusionRoadRects", citySpawner);
        StringAssert.Contains("_runtimeCityReadinessQuerySystem.HasPendingInitialUnitsSpawn", citySpawner);
        StringAssert.Contains("_runtimeCityReadinessQuerySystem.TryGetGridConfig", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityReadinessQuerySystem", readinessSystem);
        StringAssert.Contains("private World _queryWorld", readinessSystem);
        StringAssert.Contains("private EntityQuery _gridDataQuery", readinessSystem);
        StringAssert.Contains("public bool TryGetGridConfig", readinessSystem);
        StringAssert.Contains("public bool TryGetGridData", readinessSystem);
        StringAssert.Contains("public bool HasPendingInitialUnitsSpawn", readinessSystem);
        StringAssert.Contains("public List<RectInt> CollectInitialBaseExclusionRoadRects", readinessSystem);
        StringAssert.Contains("private void EnsureGridDataQuery", readinessSystem);
        StringAssert.Contains("public void Clear()", readinessSystem);

        string[] retiredSpawnerReadinessTokens =
        {
            "using Unity.Collections;",
            "using Unity.Entities;",
            "private World _queryWorld",
            "private EntityQuery _gridDataQuery",
            "private void EnsureEntityQueries(",
            "private bool TryGetGridData(",
            "private bool TryGetGridConfig(",
            "private static bool HasPendingInitialUnitsSpawn(",
            "private static List<RectInt> CollectInitialBaseExclusionRoadRects(",
            "World.DefaultGameObjectInjectionWorld",
            "EntityManager em =",
            "ComponentType.ReadOnly<GridConfig>()",
            "Allocator.Temp",
            "DynamicBuffer<"
        };

        foreach (string token in retiredSpawnerReadinessTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityReadinessQuerySystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityGenerationSequenceMustLiveInRuntimeCityGenerationSystem()
    {
        const string generationSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(generationSystemPath), "Runtime city generation sequence must live in RuntimeCityGenerationSystem.");

        string generationSystem = File.ReadAllText(generationSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("18. Complete: Extract city generation sequence", roadmap);
        StringAssert.Contains("RuntimeCityGenerationSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityGenerationSystem _runtimeCityGenerationSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityGenerationSystem.TryBegin(CreateGenerationContext(grid, roadCellSizeInGridCells, frameCount))", citySpawner);
        StringAssert.Contains("private RuntimeCityGenerationSystem.Context CreateGenerationContext", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityGenerationSystem", generationSystem);
        StringAssert.Contains("public bool TryBegin(Context context)", generationSystem);
        StringAssert.Contains("private IEnumerator GenerateCityRoutine", generationSystem);
        StringAssert.Contains("context.RoadBuildBridgeSystem.BeginDeferredRoadEcsSync()", generationSystem);
        StringAssert.Contains("context.RoadBuildBridgeSystem.EndDeferredRoadEcsSync()", generationSystem);
        StringAssert.Contains("context.SpawnBridgeSystem.BeginDeferredSideEffects()", generationSystem);
        StringAssert.Contains("context.SpawnBridgeSystem.EndDeferredSideEffects()", generationSystem);
        StringAssert.Contains("new Unity.Mathematics.Random(generationSeed)", generationSystem);
        StringAssert.Contains("var cities = new List<CityLayoutData>", generationSystem);
        StringAssert.Contains("SpawnCityBulkBuildingsRoutine", generationSystem);
        StringAssert.Contains("context.LifecycleSystem.CompleteGeneration", generationSystem);
        StringAssert.Contains("context.MinimapEvents?.PublishStaticMinimapChanged()", generationSystem);
        StringAssert.Contains("context.ChainSystem.TryPlanNextCity", generationSystem);
        StringAssert.Contains("context.RoadCommitSystem.CommitCityRoadNetwork", generationSystem);

        string[] retiredSpawnerGenerationTokens =
        {
            "private IEnumerator GenerateCityRoutine",
            "_runtimeCityRoadBuildBridgeSystem.BeginDeferredRoadEcsSync(",
            "_runtimeCityRoadBuildBridgeSystem.EndDeferredRoadEcsSync(",
            "_runtimeCitySpawnBridgeSystem.BeginDeferredSideEffects(",
            "_runtimeCitySpawnBridgeSystem.EndDeferredSideEffects(",
            "new Unity.Mathematics.Random(generationSeed)",
            "var cities = new List<CityLayoutData>",
            "_runtimeCityBuildingSpawnSystem.SpawnCityBulkBuildingsRoutine",
            "_runtimeCityLifecycleSystem.TryBeginGeneration",
            "_runtimeCityLifecycleSystem.CompleteGeneration"
        };

        foreach (string token in retiredSpawnerGenerationTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityGenerationSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityChainConnectionPolicyMustLiveInRuntimeCityChainSystem()
    {
        const string chainSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string generationSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(chainSystemPath), "Runtime city chain planning policy must live in RuntimeCityChainSystem.");

        string chainSystem = File.ReadAllText(chainSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string generationSystem = File.ReadAllText(generationSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("19. Complete: Extract city-chain connection policy", roadmap);
        StringAssert.Contains("RuntimeCityChainSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityChainSystem _runtimeCityChainSystem = new()", citySpawner);
        StringAssert.Contains("private RuntimeCityChainSystem.Context CreateChainContext()", citySpawner);
        StringAssert.Contains("context.ChainSystem.TryPlanNextCity", generationSystem);
        StringAssert.Contains("internal sealed class RuntimeCityChainSystem", chainSystem);
        StringAssert.Contains("public bool TryPlanNextCity", chainSystem);
        StringAssert.Contains("private static readonly Vector2Int[] CardinalDirections", chainSystem);
        StringAssert.Contains("previousTravelDirection", chainSystem);
        StringAssert.Contains("aIsReverse ? 1 : -1", chainSystem);
        StringAssert.Contains("int autobahnLength = Mathf.Max", chainSystem);
        StringAssert.Contains("IsCityCenterFarEnough", chainSystem);
        StringAssert.Contains("BuildStraightRoadPath", chainSystem);
        StringAssert.Contains("context.RoadCommitSystem.PopulateCityRoadCells", chainSystem);
        StringAssert.Contains("private static bool IsCityExitPathValid", chainSystem);
        StringAssert.Contains("private static bool IsAutobahnPathValid", chainSystem);
        StringAssert.Contains("private static bool TryGetCityConnectionCell", chainSystem);
        StringAssert.Contains("context.IngressSystem.GetCityInnerConnectionCell", chainSystem);
        StringAssert.Contains("context.IngressSystem.GetCityConnectionOffset", chainSystem);
        StringAssert.Contains("context.IngressSystem.CreateCityLayout", chainSystem);

        string[] retiredSpawnerChainTokens =
        {
            "private bool TryPlanNextCity(",
            "private bool IsCityExitPathValid(",
            "private bool IsAutobahnPathValid(",
            "private static bool TryGetCityConnectionCell(",
            "private static readonly Vector2Int[] CardinalDirections",
            "private static readonly Vector2Int North",
            "private static readonly Vector2Int East",
            "private static readonly Vector2Int South",
            "private static readonly Vector2Int West",
            "private Vector2Int GetCityInnerConnectionCell(",
            "private int GetCityConnectionOffset(",
            "private CityLayoutData CreateCityLayout(",
            "ConnectCitiesWithAutobahn("
        };

        foreach (string token in retiredSpawnerChainTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityChainSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityIngressPolicyMustLiveInRuntimeCityIngressSystem()
    {
        const string ingressSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityIngressSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string generationSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs";
        const string chainSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(ingressSystemPath), "Runtime city incoming connector and ingress policy must live in RuntimeCityIngressSystem.");

        string ingressSystem = File.ReadAllText(ingressSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string generationSystem = File.ReadAllText(generationSystemPath);
        string chainSystem = File.ReadAllText(chainSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("21. Complete: Extract incoming connector/ingress helpers", roadmap);
        StringAssert.Contains("RuntimeCityIngressSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityIngressSystem _runtimeCityIngressSystem = new()", citySpawner);
        StringAssert.Contains("private RuntimeCityIngressSystem.Context CreateIngressContext()", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityIngressSystem", ingressSystem);
        StringAssert.Contains("public CityLayoutData CreateCityLayout", ingressSystem);
        StringAssert.Contains("BuildTownRoadStrokes", ingressSystem);
        StringAssert.Contains("AddStroke", ingressSystem);
        StringAssert.Contains("public Vector2Int GetCityInnerConnectionCell", ingressSystem);
        StringAssert.Contains("public int GetCityConnectionOffset", ingressSystem);
        StringAssert.Contains("public void PruneIngressCorridorStrokes", ingressSystem);
        StringAssert.Contains("context.IngressSystem.CreateCityLayout", generationSystem);
        StringAssert.Contains("context.IngressSystem.CreateCityLayout", chainSystem);
        StringAssert.Contains("context.IngressSystem.GetCityInnerConnectionCell", chainSystem);
        StringAssert.Contains("context.IngressSystem.GetCityConnectionOffset", chainSystem);

        string[] retiredSpawnerIngressTokens =
        {
            "private CityLayoutData CreateCityLayout(",
            "private static void PruneIngressCorridorStrokes(",
            "_runtimeCityRoadLayoutSystem.BuildTownRoadStrokes(",
            "_runtimeCityRoadLayoutSystem.AddStroke(",
            "_runtimeCityChainSystem.GetCityInnerConnectionCell("
        };

        foreach (string token in retiredSpawnerIngressTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityIngressSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityRoadCommitSequenceMustLiveInRuntimeCityRoadCommitSystem()
    {
        const string roadCommitSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string generationSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs";
        const string chainSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(roadCommitSystemPath), "Runtime city road commit sequence must live in RuntimeCityRoadCommitSystem.");

        string roadCommitSystem = File.ReadAllText(roadCommitSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string generationSystem = File.ReadAllText(generationSystemPath);
        string chainSystem = File.ReadAllText(chainSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("20. Complete: Extract city road commit sequence", roadmap);
        StringAssert.Contains("RuntimeCityRoadCommitSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityRoadCommitSystem _runtimeCityRoadCommitSystem = new()", citySpawner);
        StringAssert.Contains("private RuntimeCityRoadCommitSystem.Context CreateRoadCommitContext()", citySpawner);
        StringAssert.Contains("context.RoadCommitSystem.CommitCityRoadNetwork", generationSystem);
        StringAssert.Contains("context.RoadCommitSystem.TryCommitSourceExitRoad", generationSystem);
        StringAssert.Contains("context.RoadCommitSystem.TryCommitAutobahn", generationSystem);
        StringAssert.Contains("context.RoadCommitSystem.TryCreateStandaloneConnector", generationSystem);
        StringAssert.Contains("context.RoadCommitSystem.PopulateCityRoadCells", chainSystem);
        StringAssert.Contains("internal sealed class RuntimeCityRoadCommitSystem", roadCommitSystem);
        StringAssert.Contains("public void CommitCityRoadNetwork", roadCommitSystem);
        StringAssert.Contains("public void PopulateCityRoadCells", roadCommitSystem);
        StringAssert.Contains("public bool TryCommitSourceExitRoad", roadCommitSystem);
        StringAssert.Contains("public bool TryCommitAutobahn", roadCommitSystem);
        StringAssert.Contains("public bool TryCreateStandaloneConnector", roadCommitSystem);
        StringAssert.Contains("CreateRoadStrokeFromRoadCells", roadCommitSystem);
        StringAssert.Contains("CreateAutobahnStrokeFromRoadCells", roadCommitSystem);
        StringAssert.Contains("CreateStandaloneStraightRoadChainFromConnector", roadCommitSystem);
        StringAssert.Contains("TryGetStandaloneStraightChainEndRoadCell", roadCommitSystem);

        string[] retiredSpawnerRoadCommitTokens =
        {
            "private void CommitCityRoadNetwork(",
            "private static void PopulateCityRoadCells(",
            "_runtimeCityRoadBuildBridgeSystem.CreateRoadStrokeFromRoadCells",
            "_runtimeCityRoadBuildBridgeSystem.CreateAutobahnStrokeFromRoadCells",
            "_runtimeCityRoadBuildBridgeSystem.CreateStandaloneStraightRoadChainFromConnector",
            "_runtimeCityRoadBuildBridgeSystem.TryGetStandaloneStraightChainEndRoadCell"
        };

        foreach (string token in retiredSpawnerRoadCommitTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityRoadCommitSystem, not RuntimeCitySpawnerSystem.");
        }

        string[] retiredGenerationRoadCommitTokens =
        {
            "CreateRoadStrokeFromRoadCells(sourceExitRoad)",
            "CreateAutobahnStrokeFromRoadCells(extendedAutobahnPath",
            "CreateStandaloneStraightRoadChainFromConnector(",
            "TryGetStandaloneStraightChainEndRoadCell(",
            "extendedAutobahnPath.Add",
            "for (int exitIndex = 0; exitIndex < sourceExitRoad.Count",
            "for (int pathIndex = 0; pathIndex < extendedAutobahnPath.Count"
        };

        foreach (string token in retiredGenerationRoadCommitTokens)
        {
            Assert.IsFalse(
                generationSystem.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityRoadCommitSystem, not RuntimeCityGenerationSystem.");
        }
    }

    [Test]
    public void RuntimeCityDiagnosticsMustLiveInRuntimeCityDiagnosticSystem()
    {
        const string diagnosticSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityDiagnosticSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string lifecycleSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs";
        const string startupSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs";
        const string generationSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs";
        const string roadCommitSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs";
        const string buildingSpawnSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(diagnosticSystemPath), "Runtime city diagnostics must live in RuntimeCityDiagnosticSystem.");

        string diagnosticSystem = File.ReadAllText(diagnosticSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string lifecycleSystem = File.ReadAllText(lifecycleSystemPath);
        string startupSystem = File.ReadAllText(startupSystemPath);
        string generationSystem = File.ReadAllText(generationSystemPath);
        string roadCommitSystem = File.ReadAllText(roadCommitSystemPath);
        string buildingSpawnSystem = File.ReadAllText(buildingSpawnSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("22. Complete: Extract diagnostics/events", roadmap);
        StringAssert.Contains("RuntimeCityDiagnosticSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityDiagnosticSystem _runtimeCityDiagnosticSystem = new()", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityDiagnosticSystem", diagnosticSystem);
        StringAssert.Contains("public void LogLifecycleStart", diagnosticSystem);
        StringAssert.Contains("public void LogLifecycleGenerating", diagnosticSystem);
        StringAssert.Contains("public void LogLifecycleEnded", diagnosticSystem);
        StringAssert.Contains("public void LogLifecycleCompleted", diagnosticSystem);
        StringAssert.Contains("public void LogInitialSpawnWait", diagnosticSystem);
        StringAssert.Contains("public void LogCityPlanningFailed", diagnosticSystem);
        StringAssert.Contains("public void LogSourceExitRoadFailed", diagnosticSystem);
        StringAssert.Contains("public void LogAutobahnFailed", diagnosticSystem);
        StringAssert.Contains("public void LogHallPlacementFailed", diagnosticSystem);
        StringAssert.Contains("Debug.Log(", diagnosticSystem);
        StringAssert.Contains("Debug.LogWarning", diagnosticSystem);
        StringAssert.Contains("[RuntimeCityState]", diagnosticSystem);
        StringAssert.Contains("context.Diagnostics?.LogLifecycleStart", lifecycleSystem);
        StringAssert.Contains("context.Diagnostics?.LogLifecycleGenerating", lifecycleSystem);
        StringAssert.Contains("context.Diagnostics?.LogLifecycleEnded", lifecycleSystem);
        StringAssert.Contains("context.Diagnostics?.LogLifecycleCompleted", lifecycleSystem);
        StringAssert.Contains("context.Diagnostics?.LogInitialSpawnWait", startupSystem);
        StringAssert.Contains("context.Diagnostics?.LogCityPlanningFailed", generationSystem);
        StringAssert.Contains("context.Diagnostics?.LogSourceExitRoadFailed", roadCommitSystem);
        StringAssert.Contains("context.Diagnostics?.LogAutobahnFailed", roadCommitSystem);
        StringAssert.Contains("_runtimeCityDiagnosticSystem?.LogHallPlacementFailed", buildingSpawnSystem);

        string[] runtimeCityFiles =
        {
            RuntimeCityCompositionPath,
            lifecycleSystemPath,
            startupSystemPath,
            generationSystemPath,
            roadCommitSystemPath,
            buildingSpawnSystemPath
        };

        string[] retiredDiagnosticTokens =
        {
            "Debug.Log(",
            "Debug.LogWarning(",
            "[RuntimeCityState]",
            "Failed to plan city",
            "Failed to create source exit road",
            "Failed to create autobahn",
            "Hall could not be placed"
        };

        foreach (string runtimeCityFile in runtimeCityFiles)
        {
            string text = File.ReadAllText(runtimeCityFile);
            foreach (string token in retiredDiagnosticTokens)
            {
                Assert.IsFalse(
                    text.Contains(token, StringComparison.Ordinal),
                    $"{token} belongs in RuntimeCityDiagnosticSystem, not {runtimeCityFile}.");
            }
        }
    }

    [Test]
    public void RuntimeCityMinimapNotificationMustLiveInRuntimeCityMinimapEventSystem()
    {
        const string minimapEventSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityMinimapEventSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string generationSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(minimapEventSystemPath), "Runtime city minimap notification must live in RuntimeCityMinimapEventSystem.");

        string minimapEventSystem = File.ReadAllText(minimapEventSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string generationSystem = File.ReadAllText(generationSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("23. Complete: Move minimap notification to result/event boundary", roadmap);
        StringAssert.Contains("RuntimeCityMinimapEventSystem", audit);
        StringAssert.Contains("private readonly RuntimeCityMinimapEventSystem _runtimeCityMinimapEventSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityMinimapEventSystem.Configure(mainMenuPlayUi)", citySpawner);
        StringAssert.Contains("_runtimeCityMinimapEventSystem.Flush()", citySpawner);
        StringAssert.Contains("_runtimeCityMinimapEventSystem.Clear()", citySpawner);
        StringAssert.Contains("internal sealed class RuntimeCityMinimapEventSystem", minimapEventSystem);
        StringAssert.Contains("public void PublishStaticMinimapChanged", minimapEventSystem);
        StringAssert.Contains("public void Flush", minimapEventSystem);
        StringAssert.Contains("_mainMenuPlayUi?.NotifyStaticMinimapChanged()", minimapEventSystem);
        StringAssert.Contains("context.MinimapEvents?.PublishStaticMinimapChanged()", generationSystem);

        string[] retiredSpawnerMinimapTokens =
        {
            "private MainMenuPlayUI _mainMenuPlayUi",
            "private void NotifyStaticMinimapChanged(",
            "_mainMenuPlayUi?.NotifyStaticMinimapChanged()"
        };

        foreach (string token in retiredSpawnerMinimapTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityMinimapEventSystem, not RuntimeCitySpawnerSystem.");
        }

        string[] retiredGenerationMinimapTokens =
        {
            "public readonly Action NotifyStaticMinimapChanged",
            "Action notifyStaticMinimapChanged",
            "NotifyStaticMinimapChanged = notifyStaticMinimapChanged",
            "context.NotifyStaticMinimapChanged?.Invoke()"
        };

        foreach (string token in retiredGenerationMinimapTokens)
        {
            Assert.IsFalse(
                generationSystem.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityMinimapEventSystem, not RuntimeCityGenerationSystem.");
        }
    }

    [Test]
    public void RuntimeCityPrefabSelectionMustLiveInRuntimeCityPrefabSelectionSystem()
    {
        const string prefabSelectionSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityPrefabSelectionSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string buildingSpawnSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(prefabSelectionSystemPath), "Runtime city prefab selection must live in RuntimeCityPrefabSelectionSystem.");

        string prefabSelectionSystem = File.ReadAllText(prefabSelectionSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string buildingSpawnSystem = File.ReadAllText(buildingSpawnSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("6. Complete: Extract prefab selection", roadmap);
        StringAssert.Contains("private readonly RuntimeCityPrefabSelectionSystem _runtimeCityPrefabSelectionSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityPrefabSelectionSystem.IsConfiguredPrefab(prefab, housePrefabs)", citySpawner);
        StringAssert.Contains("_runtimeCityPrefabSelectionSystem.GetRandomPrefab", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityPrefabSelectionSystem.Shuffle", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityPrefabSelectionSystem.GetCachedFootprintCells", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityPrefabSelectionSystem.GetMajorFootprint", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityPrefabSelectionSystem.GetMinorFootprint", buildingSpawnSystem);
        StringAssert.Contains("internal sealed class RuntimeCityPrefabSelectionSystem", prefabSelectionSystem);
        StringAssert.Contains("private readonly Dictionary<GameObject, Vector2Int> _prefabFootprintCache = new()", prefabSelectionSystem);
        StringAssert.Contains("public bool IsConfiguredPrefab", prefabSelectionSystem);
        StringAssert.Contains("public GameObject GetRandomPrefab", prefabSelectionSystem);
        StringAssert.Contains("public void Shuffle<T>", prefabSelectionSystem);
        StringAssert.Contains("public Vector2Int GetCachedFootprintCells", prefabSelectionSystem);
        StringAssert.Contains("public int GetMajorFootprint", prefabSelectionSystem);
        StringAssert.Contains("public int GetMinorFootprint", prefabSelectionSystem);
        StringAssert.Contains("private static Vector2Int EstimateFootprintCells", prefabSelectionSystem);
        StringAssert.Contains("RuntimeCityPrefabSelectionSystem", audit);

        string[] retiredSpawnerPrefabTokens =
        {
            "private readonly Dictionary<GameObject, Vector2Int> _prefabFootprintCache",
            "private static GameObject GetRandomPrefab(",
            "private static void Shuffle<T>(",
            "private Vector2Int GetCachedFootprintCells(",
            "private static Vector2Int EstimateFootprintCells(",
            "private int GetMajorFootprint(",
            "private int GetMinorFootprint(",
            "for (int i = 0; i < housePrefabs.Count; i++)"
        };

        foreach (string token in retiredSpawnerPrefabTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityPrefabSelectionSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityVisualRealizationMustLiveInRuntimeCityVisualSystem()
    {
        const string visualSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityVisualSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string buildingSpawnSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(visualSystemPath), "Runtime city visual realization must live in RuntimeCityVisualSystem.");

        string visualSystem = File.ReadAllText(visualSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string buildingSpawnSystem = File.ReadAllText(buildingSpawnSystemPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("7. Complete: Extract visual realization", roadmap);
        StringAssert.Contains("private readonly RuntimeCityVisualSystem _runtimeCityVisualSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityVisualSystem.SetRuntimeRoot(runtimeRoot)", citySpawner);
        StringAssert.Contains("_runtimeCityVisualSystem.Dispose()", citySpawner);
        StringAssert.Contains("_runtimeCityVisualSystem.EnsureCityVisualRoot()", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCityVisualSystem.SpawnVisualOnlyPrefab", buildingSpawnSystem);
        StringAssert.Contains("internal sealed class RuntimeCityVisualSystem", visualSystem);
        StringAssert.Contains("public void SetRuntimeRoot", visualSystem);
        StringAssert.Contains("public void EnsureCityVisualRoot", visualSystem);
        StringAssert.Contains("public GameObject SpawnVisualOnlyPrefab", visualSystem);
        StringAssert.Contains("public Vector3 GetFootprintCenter", visualSystem);
        StringAssert.Contains("private static bool TryGetLocalBounds", visualSystem);
        StringAssert.Contains("private static void SetChildVisibleByName", visualSystem);
        StringAssert.Contains("private static Transform FindDescendantByName", visualSystem);
        StringAssert.Contains("FindDescendantByName(prefab.transform, \"CombinedMesh\")", visualSystem);
        StringAssert.Contains("RuntimeCityVisualSystem", audit);

        string[] retiredSpawnerVisualTokens =
        {
            "private Transform _runtimeRoot",
            "_runtimeRoot = runtimeRoot",
            "_runtimeRoot = null",
            "private Transform _cityVisualRoot",
            "private void EnsureCityVisualRoot(",
            "private GameObject SpawnVisualOnlyPrefab(",
            "private static bool TryGetLocalBounds(",
            "private static void SetChildVisibleByName(",
            "private static Transform FindDescendantByName(",
            "private Vector3 GetFootprintCenter("
        };

        foreach (string token in retiredSpawnerVisualTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityVisualSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityRuntimeRootOwnershipMustStayInVisualSystem()
    {
        const string visualSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityVisualSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        string visualSystem = File.ReadAllText(visualSystemPath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("24. Complete: Remove runtime root ownership from the spawner", roadmap);
        StringAssert.Contains("RuntimeCityVisualSystem", audit);
        StringAssert.Contains("_runtimeCityVisualSystem.SetRuntimeRoot(runtimeRoot)", citySpawner);
        StringAssert.Contains("private Transform _runtimeRoot", visualSystem);
        StringAssert.Contains("public void SetRuntimeRoot", visualSystem);
        StringAssert.Contains("_cityVisualRoot.SetParent(_runtimeRoot, false)", visualSystem);

        string[] retiredSpawnerRuntimeRootTokens =
        {
            "private Transform _runtimeRoot",
            "_runtimeRoot = runtimeRoot",
            "_runtimeRoot = null",
            "new GameObject(\"RuntimeCityVisuals\")",
            ".SetParent(runtimeRoot"
        };

        foreach (string token in retiredSpawnerRuntimeRootTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityVisualSystem or RuntimeRootSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityCompositionMustOwnRuntimeCitySystemGraph()
    {
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(RuntimeCityCompositionPath), "Runtime city system graph composition must live in RuntimeCityCompositionSystem.");
        Assert.IsFalse(File.Exists(citySpawnerPath), "RuntimeCitySpawnerSystem shell must stay deleted after step 27.");

        string composition = File.ReadAllText(RuntimeCityCompositionPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("25. Complete: Move composition out of the spawner constructor path", roadmap);
        StringAssert.Contains("RuntimeCityCompositionSystem", audit);
        StringAssert.Contains("public sealed class RuntimeCityCompositionSystem", composition);

        string[] composedSystemTokens =
        {
            "private readonly RuntimeCityConfigSystem _runtimeCityConfigSystem = new()",
            "private readonly RuntimeCityLayoutSystem _runtimeCityLayoutSystem = new()",
            "private readonly RuntimeCityRoadLayoutSystem _runtimeCityRoadLayoutSystem = new()",
            "private readonly RuntimeCityBuildingPlotSystem _runtimeCityBuildingPlotSystem = new()",
            "private readonly RuntimeCityWalkabilitySystem _runtimeCityWalkabilitySystem = new()",
            "private readonly RuntimeCityPrefabSelectionSystem _runtimeCityPrefabSelectionSystem = new()",
            "private readonly RuntimeCityBuildingSpawnSystem _runtimeCityBuildingSpawnSystem = new()",
            "private readonly RuntimeCityVisualSystem _runtimeCityVisualSystem = new()",
            "private readonly RuntimeCitySpawnBridgeSystem _runtimeCitySpawnBridgeSystem = new()",
            "private readonly RuntimeCityRoadBuildBridgeSystem _runtimeCityRoadBuildBridgeSystem = new()",
            "private readonly RuntimeCityLifecycleSystem _runtimeCityLifecycleSystem = new()",
            "private readonly RuntimeCityStartupSystem _runtimeCityStartupSystem = new()",
            "private readonly RuntimeCityReadinessQuerySystem _runtimeCityReadinessQuerySystem = new()",
            "private readonly RuntimeCityGenerationSystem _runtimeCityGenerationSystem = new()",
            "private readonly RuntimeCityChainSystem _runtimeCityChainSystem = new()",
            "private readonly RuntimeCityRoadCommitSystem _runtimeCityRoadCommitSystem = new()",
            "private readonly RuntimeCityIngressSystem _runtimeCityIngressSystem = new()",
            "private readonly RuntimeCityMinimapEventSystem _runtimeCityMinimapEventSystem = new()",
            "private readonly RuntimeCityDiagnosticSystem _runtimeCityDiagnosticSystem = new()",
            "private RuntimeCityGenerationSystem.Context CreateGenerationContext",
            "private RuntimeCityStartupSystem.Context CreateStartupContext",
            "private RuntimeCityChainSystem.Context CreateChainContext",
            "private RuntimeCityRoadCommitSystem.Context CreateRoadCommitContext",
            "private RuntimeCityIngressSystem.Context CreateIngressContext"
        };

        foreach (string token in composedSystemTokens)
        {
            StringAssert.Contains(token, composition);
        }
    }

    [Test]
    public void RuntimeCityPeerSystemsMustUseRuntimeCityReadModelSystem()
    {
        const string readModelPath = "Assets/Game/Scripts/Environment/RuntimeCityReadModelSystem.cs";
        const string gridBlockerPath = "Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs";
        const string decorationPath = "Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs";
        const string startupPath = "Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(readModelPath), "Runtime city peer systems must consume a narrow read model.");

        string readModel = File.ReadAllText(readModelPath);
        string composition = File.ReadAllText(RuntimeCityCompositionPath);
        string gridBlockers = File.ReadAllText(gridBlockerPath);
        string decorations = File.ReadAllText(decorationPath);
        string startup = File.ReadAllText(startupPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("26. Complete: Migrate peer dependencies off `RuntimeCitySpawnerSystem`", roadmap);
        StringAssert.Contains("RuntimeCityReadModelSystem", audit);
        StringAssert.Contains("public sealed class RuntimeCityReadModelSystem", readModel);
        StringAssert.Contains("public bool SpawnOnStartEnabled", readModel);
        StringAssert.Contains("public bool HasSpawned", readModel);
        StringAssert.Contains("public bool IsGenerating", readModel);
        StringAssert.Contains("public void Publish(bool spawnOnStartEnabled, bool hasSpawned, bool isGenerating)", readModel);
        StringAssert.Contains("private readonly RuntimeCityReadModelSystem _runtimeCityReadModelSystem = new()", composition);
        StringAssert.Contains("public RuntimeCityReadModelSystem ReadModel => _runtimeCityReadModelSystem", composition);
        StringAssert.Contains("_runtimeCityReadModelSystem.Publish(SpawnOnStartEnabled, HasSpawned, IsGenerating)", composition);
        StringAssert.Contains("public RuntimeCityReadModelSystem ReadModel => _runtimeCityReadModelSystem", composition);
        StringAssert.Contains("runtimeCity.ReadModel", startup);

        string[] peerSources = { gridBlockers, decorations };
        foreach (string peerSource in peerSources)
        {
            StringAssert.Contains("RuntimeCityReadModelSystem", peerSource);
            Assert.IsFalse(peerSource.Contains("RuntimeCitySpawnerSystem", StringComparison.Ordinal));
            Assert.IsFalse(peerSource.Contains("_citySpawner", StringComparison.Ordinal));
            Assert.IsFalse(peerSource.Contains("citySpawner", StringComparison.Ordinal));
        }
    }

    [Test]
    public void RuntimeCitySpawnerSystemShellMustStayDeleted()
    {
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsFalse(File.Exists(citySpawnerPath), "RuntimeCitySpawnerSystem.cs must not be restored.");
        StringAssert.Contains("27. Complete: Delete the spawner shell", File.ReadAllText(roadmapPath));
        StringAssert.Contains("Deleted source file: `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs`", File.ReadAllText(auditPath));

        string[] sourceFiles = Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Configs/", StringComparison.Ordinal))
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .ToArray();

        string[] forbiddenTokens =
        {
            "new RuntimeCitySpawnerSystem(",
            "RuntimeCitySpawnerSystem runtimeCity",
            "RuntimeCitySpawnerSystem RuntimeCity",
            "RuntimeCitySpawner { get",
            "runtimeCitySpawner?.Update",
            "runtimeCitySpawner.ReadModel"
        };

        var violations = new List<string>();
        foreach (string file in sourceFiles)
        {
            string text = File.ReadAllText(file);
            foreach (string token in forbiddenTokens)
            {
                if (text.Contains(token, StringComparison.Ordinal))
                    violations.Add($"{file}: {token}");
            }
        }

        Assert.IsEmpty(
            violations,
            "Runtime city production code must depend on RuntimeCityCompositionSystem or narrower boundaries, not the deleted RuntimeCitySpawnerSystem shell:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void RuntimeCityFinalContractMustTrackDeletedSpawnerShell()
    {
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        string contract = File.ReadAllText(ContractPath);
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("28. Complete: Architecture contract and guards", roadmap);
        StringAssert.Contains("Step 28 complete", audit);
        StringAssert.Contains("RuntimeCityCompositionSystem` must not own `World`, `EntityQuery`, `EntityManager`, `Allocator`", contract);
        StringAssert.Contains("RuntimeCityCompositionSystem` must not own `GenerateCityRoutine`", contract);
        StringAssert.Contains("RuntimeCityCompositionSystem` and `RuntimeCityGenerationSystem` must not own road commit loops", contract);
        StringAssert.Contains("RuntimeCitySpawnerSystem.cs` must not be restored as a public compatibility shell", contract);
        StringAssert.Contains("Serialized runtime-city config names", contract);
        StringAssert.Contains("RuntimeCitySpawnerSystemConfig", contract);
        StringAssert.Contains("RuntimeCitySpawnerSystemSceneConfigAsset", contract);
        StringAssert.Contains("Game_RuntimeCitySpawner_Config.asset", contract);
        StringAssert.Contains("serialized data/config naming, not runtime orchestration code", contract);

        string[] retiredShellOwnershipTokens =
        {
            "`RuntimeCitySpawnerSystem` must not call",
            "`RuntimeCitySpawnerSystem` must not own",
            "`RuntimeCitySpawnerSystem` and `RuntimeCityGenerationSystem`",
            "`RuntimeCitySpawnerSystem` may pass",
            "`RuntimeCitySpawnerSystem` may remain"
        };

        foreach (string token in retiredShellOwnershipTokens)
        {
            Assert.IsFalse(
                contract.Contains(token, StringComparison.Ordinal),
                $"Final contract must not describe runtime ownership through the deleted shell: {token}");
        }
    }

    [Test]
    public void RuntimeCitySpawnBridgeMustLiveInRuntimeCitySpawnBridgeSystem()
    {
        const string spawnBridgePath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnBridgeSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string buildingSpawnSystemPath = "Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(spawnBridgePath), "Runtime city generated building spawn bridge must live in RuntimeCitySpawnBridgeSystem.");

        string spawnBridge = File.ReadAllText(spawnBridgePath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string buildingSpawnSystem = File.ReadAllText(buildingSpawnSystemPath);
        string generationSystem = File.ReadAllText("Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs");
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("8. Complete: Extract ECS spawn request bridge", roadmap);
        StringAssert.Contains("private readonly RuntimeCitySpawnBridgeSystem _runtimeCitySpawnBridgeSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCitySpawnBridgeSystem.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext)", citySpawner);
        StringAssert.Contains("_runtimeCitySpawnBridgeSystem.Clear()", citySpawner);
        StringAssert.Contains("_runtimeCitySpawnBridgeSystem.HasSpawnSystem", citySpawner);
        StringAssert.Contains("context.SpawnBridgeSystem.BeginDeferredSideEffects()", generationSystem);
        StringAssert.Contains("context.SpawnBridgeSystem.EndDeferredSideEffects()", generationSystem);
        StringAssert.Contains("_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding", buildingSpawnSystem);
        StringAssert.Contains("_runtimeCitySpawnBridgeSystem.DeleteCityBuilding", buildingSpawnSystem);
        StringAssert.Contains("internal sealed class RuntimeCitySpawnBridgeSystem", spawnBridge);
        StringAssert.Contains("private BuildingRuntimeCitySpawnSystem _buildingRuntimeCitySpawnSystem", spawnBridge);
        StringAssert.Contains("private BuildingRuntimeCitySpawnSystem.Context _buildingRuntimeCitySpawnContext", spawnBridge);
        StringAssert.Contains("public bool HasSpawnSystem", spawnBridge);
        StringAssert.Contains("public void Configure", spawnBridge);
        StringAssert.Contains("public void BeginDeferredSideEffects", spawnBridge);
        StringAssert.Contains("public void EndDeferredSideEffects", spawnBridge);
        StringAssert.Contains("public bool TrySpawnCityBuilding", spawnBridge);
        StringAssert.Contains("public bool DeleteCityBuilding", spawnBridge);
        StringAssert.Contains("TrySpawnRuntimeBuilding", spawnBridge);
        StringAssert.Contains("RuntimeCitySpawnBridgeSystem", audit);

        string[] retiredSpawnerSpawnBridgeTokens =
        {
            "private BuildingRuntimeCitySpawnSystem _buildingRuntimeCitySpawnSystem",
            "private BuildingRuntimeCitySpawnSystem.Context _buildingRuntimeCitySpawnContext",
            "private bool TrySpawnCityBuilding(",
            "private bool DeleteCityBuilding(",
            "_buildingRuntimeCitySpawnSystem?.BeginDeferredSideEffects",
            "_buildingRuntimeCitySpawnSystem?.EndDeferredSideEffects",
            "_runtimeCitySpawnBridgeSystem.BeginDeferredSideEffects(",
            "_runtimeCitySpawnBridgeSystem.EndDeferredSideEffects(",
            "_buildingRuntimeCitySpawnSystem.TrySpawnRuntimeBuilding",
            "_buildingRuntimeCitySpawnSystem.DeleteBuildingById"
        };

        foreach (string token in retiredSpawnerSpawnBridgeTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCitySpawnBridgeSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void RuntimeCityRoadBuildCouplingMustLiveInRuntimeCityRoadBuildBridgeSystem()
    {
        const string roadBuildBridgePath = "Assets/Game/Scripts/Environment/RuntimeCityRoadBuildBridgeSystem.cs";
        const string citySpawnerPath = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        const string roadmapPath = "Design/Architecture/runtime_city_spawner_refactor_roadmap.md";
        const string auditPath = "Design/Architecture/runtime_city_spawner_responsibility_audit.md";

        Assert.IsTrue(File.Exists(roadBuildBridgePath), "Runtime city road build coupling must live in RuntimeCityRoadBuildBridgeSystem.");

        string roadBuildBridge = File.ReadAllText(roadBuildBridgePath);
        string citySpawner = ReadRuntimeCitySpawnerArchitectureSurface(citySpawnerPath);
        string generationSystem = File.ReadAllText("Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs");
        string roadCommitSystem = File.ReadAllText("Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs");
        string roadmap = File.ReadAllText(roadmapPath);
        string audit = File.ReadAllText(auditPath);

        StringAssert.Contains("9. Complete: Extract RoadBuild coupling", roadmap);
        StringAssert.Contains("private readonly RuntimeCityRoadBuildBridgeSystem _runtimeCityRoadBuildBridgeSystem = new()", citySpawner);
        StringAssert.Contains("_runtimeCityRoadBuildBridgeSystem.Configure(roadBuildController)", citySpawner);
        StringAssert.Contains("_runtimeCityRoadBuildBridgeSystem.Clear()", citySpawner);
        StringAssert.Contains("_runtimeCityRoadBuildBridgeSystem.HasRoadBuildSystem", citySpawner);
        StringAssert.Contains("_runtimeCityRoadBuildBridgeSystem.TryGetRoadCellSizeInGridCells", citySpawner);
        StringAssert.Contains("context.RoadBuildBridgeSystem.BeginDeferredRoadEcsSync()", generationSystem);
        StringAssert.Contains("context.RoadBuildBridgeSystem.EndDeferredRoadEcsSync()", generationSystem);
        StringAssert.Contains("context.RoadBuildBridgeSystem.CreateRoadStrokeFromRoadCells", roadCommitSystem);
        StringAssert.Contains("context.RoadBuildBridgeSystem.CreateAutobahnStrokeFromRoadCells", roadCommitSystem);
        StringAssert.Contains("context.RoadBuildBridgeSystem.CreateStandaloneStraightRoadChainFromConnector", roadCommitSystem);
        StringAssert.Contains("context.RoadBuildBridgeSystem.TryGetStandaloneStraightChainEndRoadCell", roadCommitSystem);
        StringAssert.Contains("internal sealed class RuntimeCityRoadBuildBridgeSystem", roadBuildBridge);
        StringAssert.Contains("private RoadBuildSystem _roadBuildSystem", roadBuildBridge);
        StringAssert.Contains("public bool HasRoadBuildSystem", roadBuildBridge);
        StringAssert.Contains("public void Configure(RoadBuildSystem roadBuildSystem)", roadBuildBridge);
        StringAssert.Contains("public bool TryGetRoadCellSizeInGridCells", roadBuildBridge);
        StringAssert.Contains("public void BeginDeferredRoadEcsSync", roadBuildBridge);
        StringAssert.Contains("public void EndDeferredRoadEcsSync", roadBuildBridge);
        StringAssert.Contains("public bool CreateRoadStrokeFromRoadCells", roadBuildBridge);
        StringAssert.Contains("public bool CreateAutobahnStrokeFromRoadCells", roadBuildBridge);
        StringAssert.Contains("public bool CreateStandaloneStraightRoadChainFromConnector", roadBuildBridge);
        StringAssert.Contains("public bool TryGetStandaloneStraightChainEndRoadCell", roadBuildBridge);
        StringAssert.Contains("RuntimeCityRoadBuildBridgeSystem", audit);

        string[] retiredSpawnerRoadBuildTokens =
        {
            "private RoadBuildSystem _roadBuildController",
            "_roadBuildController = roadBuildController",
            "_roadBuildController == null",
            "_roadBuildController?.BeginDeferredRoadEcsSync",
            "_roadBuildController?.EndDeferredRoadEcsSync",
            "_runtimeCityRoadBuildBridgeSystem.BeginDeferredRoadEcsSync(",
            "_runtimeCityRoadBuildBridgeSystem.EndDeferredRoadEcsSync(",
            "_roadBuildController.TryGetRoadCellSizeInGridCells",
            "_roadBuildController.CreateRoadStrokeFromRoadCells",
            "_roadBuildController.CreateAutobahnStrokeFromRoadCells",
            "_roadBuildController.CreateStandaloneStraightRoadChainFromConnector",
            "_roadBuildController.TryGetStandaloneStraightChainEndRoadCell"
        };

        foreach (string token in retiredSpawnerRoadBuildTokens)
        {
            Assert.IsFalse(
                citySpawner.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in RuntimeCityRoadBuildBridgeSystem, not RuntimeCitySpawnerSystem.");
        }
    }

    [Test]
    public void CitizenPopulationSystemMustNotReachThroughBuildingPlacementSingleton()
    {
        const string file = "Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs";
        string text = File.ReadAllText(file);

        StringAssert.Contains("BuildingRuntimeQuerySystem", text);
        StringAssert.Contains("CitizenResourceSystem", text);
        StringAssert.Contains("CitizenPrefabSystem", text);
        string placement = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs");
        string startup = File.ReadAllText("Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs");
        string buildingComposition = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs");
        string runtimeResourcePrefabContext = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabContextSystem.cs");
        StringAssert.Contains("CreateRuntimeBuildingQueryContext", placement);
        StringAssert.Contains("RuntimeResourceSystem", placement);
        StringAssert.Contains("RuntimeUnitPrefabSystem", placement);
        StringAssert.Contains("BuildingRuntimeResourcePrefabContextSystem _buildingRuntimeResourcePrefabContextSystem", placement);
        StringAssert.Contains("CreateRuntimeResourcePrefabContextSource", placement);
        StringAssert.Contains("CreateRuntimeUnitPrefabContext", runtimeResourcePrefabContext);
        StringAssert.Contains("CreateCitizenResourceContext", runtimeResourcePrefabContext);
        StringAssert.Contains("CreateCitizenPrefabContext", runtimeResourcePrefabContext);
        StringAssert.Contains("CreateBuildingSpawnPrefabContext", runtimeResourcePrefabContext);
        StringAssert.Contains("Func<Source> CreateCurrentSource", runtimeResourcePrefabContext);
        StringAssert.Contains("source.CreateCurrentSource != null ? source.CreateCurrentSource() : source", runtimeResourcePrefabContext);
        StringAssert.Contains("_buildingGameplayCompositionSystem.CreateCitizenPopulation", startup);
        StringAssert.Contains("Building.RuntimeQuerySystem", buildingComposition);
        StringAssert.Contains("Building.CreateRuntimeBuildingQueryContext()", buildingComposition);
        StringAssert.Contains("BuildingRuntimeResourcePrefabContextSystem.Source resourcePrefabSource = Building.CreateRuntimeResourcePrefabContextSource()", buildingComposition);
        StringAssert.Contains("Building.RuntimeResourcePrefabContextSystem.CreateCitizenResourceContext(resourcePrefabSource)", buildingComposition);
        StringAssert.Contains("Building.RuntimeResourcePrefabContextSystem.CreateCitizenPrefabContext(resourcePrefabSource)", buildingComposition);
        Assert.IsFalse(
            placement.Contains("_resourceDollars", StringComparison.Ordinal) ||
            placement.Contains("CreateCitizenResourceContext", StringComparison.Ordinal) ||
            placement.Contains("CreateCitizenPrefabContext", StringComparison.Ordinal),
            "BuildingPlacementSystem must not own the citizen dollar backing store or citizen context factories; use RuntimeResourceSystem/RuntimeUnitPrefabSystem.");
        Assert.IsFalse(
            placement.Contains("CreateRuntimeUnitPrefabContext", StringComparison.Ordinal) ||
            placement.Contains("new BuildingSpawnPrefabSystem.Context", StringComparison.Ordinal),
            "Runtime unit-prefab and building spawn-prefab context construction belongs in BuildingRuntimeResourcePrefabContextSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            text.Contains("BuildingPlacementSystem", StringComparison.Ordinal),
            "CitizenPopulationSystem must receive narrow building/resource/prefab contexts instead of BuildingPlacementSystem.");
        Assert.IsFalse(
            text.Contains("_buildingPlacementSystem.GetRuntimeHouseBuildingIds", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.GetRuntimeBuildingIdsByRole", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.TryGetRuntimeBuildingFocusWorldPosition", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.TryGetRuntimeBuildingDestroyedState", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.TryGetRuntimeBuildingRefugeeSettings", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.TryGetRuntimeBuildingApproachCell", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.IsRuntimeBuildingApproachCell", StringComparison.Ordinal),
            "CitizenPopulationSystem building read paths must use BuildingRuntimeQuerySystem instead of BuildingPlacementSystem facade read methods.");
        Assert.IsFalse(
            text.Contains("_buildingPlacementSystem.TrySpendDollars", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.TryResolveConfiguredUnitPrefabEntity", StringComparison.Ordinal) ||
            text.Contains("_buildingPlacementSystem.TryResolveConfiguredUnitSpawnPrefab", StringComparison.Ordinal),
            "CitizenPopulationSystem resource and prefab paths must use CitizenResourceSystem/CitizenPrefabSystem instead of BuildingPlacementSystem facade methods.");
    }

    [Test]
    public void MenuViewMustUseBuildingUiCommandBoundaryForBuildingUi()
    {
        const string menuFile = "Assets/Game/Scripts/UI/MenuView.cs";
        const string startupFile = "Assets/Game/Scripts/Systems/MenuStartupSystem.cs";
        const string commandFile = "Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs";
        const string queryFile = "Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs";
        Assert.IsTrue(File.Exists(commandFile), "Menu/camp resource UI commands must live behind BuildingUiCommandSystem.");
        Assert.IsTrue(File.Exists(queryFile), "Menu/camp resource UI read models must live behind BuildingUiQuerySystem.");

        string menu = File.ReadAllText(menuFile);
        string startup = File.ReadAllText(startupFile);
        string buildingComposition = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs");
        string command = File.ReadAllText(commandFile);
        string query = File.ReadAllText(queryFile);

        StringAssert.Contains("BuildingUiCommandSystem _buildingUiCommandSystem", menu);
        StringAssert.Contains("BuildingUiQuerySystem _buildingUiQuerySystem", menu);
        StringAssert.Contains("BuildingUiCommandSystem buildingUiCommand", startup);
        StringAssert.Contains("BuildingUiCommandSystem.Context buildingUiCommandContext", startup);
        StringAssert.Contains("BuildingUiQuerySystem buildingUiQuery", startup);
        StringAssert.Contains("BuildingUiQuerySystem.Context buildingUiQueryContext", startup);
        StringAssert.Contains("building.BuildingUiCommandSystem", buildingComposition);
        StringAssert.Contains("building.CreateBuildingUiCommandContext()", buildingComposition);
        StringAssert.Contains("building.BuildingUiQuerySystem", buildingComposition);
        StringAssert.Contains("building.CreateBuildingUiQueryContext()", buildingComposition);
        StringAssert.Contains("TryRequestCampItem", command);
        StringAssert.Contains("GetCampRequestFailure", command);
        StringAssert.Contains("GetFriendlyPendingProductionUiEntries", query);
        StringAssert.Contains("HasActiveBuilding", query);
        StringAssert.Contains("SelectedBuildingDisplayName", query);
        StringAssert.Contains("ConfirmBuildingPlacement", command);
        StringAssert.Contains("DeleteSelectedBuilding", command);
        StringAssert.Contains("TryGetSelectedBuildingHealth", query);
        StringAssert.Contains("TryGetSelectedBuildingPreviewPrefab", query);
        StringAssert.Contains("IsRuntimeBuildingWall", query);
        StringAssert.Contains("IsRuntimeBuildingCityGenerated", query);
        StringAssert.Contains("TryGetRuntimeBuildingOwnerFaction", query);
        StringAssert.Contains("HasVisibleSelectableBuilding", query);
        StringAssert.Contains("TryResolveLiveUnitPreviewPrefab", query);
        StringAssert.Contains("CancelBuildingPlacement", command);
        StringAssert.Contains("FocusLastCampProductionRequest", command);
        StringAssert.Contains("ClearSelectedBuilding", command);
        StringAssert.Contains("ExitBuildMode", command);
        Assert.IsFalse(
            command.Contains("GetFriendlyPendingProductionUiEntries", StringComparison.Ordinal) ||
            command.Contains("HasActiveBuilding", StringComparison.Ordinal) ||
            command.Contains("SelectedBuildingDisplayName", StringComparison.Ordinal) ||
            command.Contains("TryGetSelectedBuildingHealth", StringComparison.Ordinal) ||
            command.Contains("TryGetSelectedBuildingPreviewPrefab", StringComparison.Ordinal) ||
            command.Contains("IsRuntimeBuildingWall", StringComparison.Ordinal) ||
            command.Contains("IsRuntimeBuildingCityGenerated", StringComparison.Ordinal) ||
            command.Contains("TryGetRuntimeBuildingOwnerFaction", StringComparison.Ordinal) ||
            command.Contains("HasVisibleSelectableBuilding", StringComparison.Ordinal) ||
            command.Contains("TryResolveLiveUnitPreviewPrefab", StringComparison.Ordinal),
            "BuildingUiCommandSystem must not own building UI read-model queries.");
        Assert.IsFalse(
            menu.Contains("_buildingPlacementSystem", StringComparison.Ordinal) ||
            menu.Contains("BuildingPlacementSystem buildingPlacementSystem", StringComparison.Ordinal),
            "MenuView must receive narrow UI command/query boundaries instead of a BuildingPlacementSystem facade instance.");
        Assert.IsFalse(
            menu.Contains("BuildingPlacementSystem", StringComparison.Ordinal),
            "MenuView must not reference BuildingPlacementSystem nested UI/data contracts.");

        string[] forbiddenMenuFacadeCalls =
        {
            "_buildingPlacementSystem.CurrentDollars",
            "_buildingPlacementSystem.GetFriendlyPendingProductionUiEntries",
            "_buildingPlacementSystem.ConfiguredSpawnableCount",
            "_buildingPlacementSystem.TryGetConfiguredSpawnable",
            "_buildingPlacementSystem.ConfiguredUnitCount",
            "_buildingPlacementSystem.TryGetConfiguredUnit",
            "_buildingPlacementSystem.IsConfiguredSpawnablePrefab",
            "_buildingPlacementSystem.GetCampRequestFailure",
            "_buildingPlacementSystem.TryRequestCampItem",
            "_buildingPlacementSystem.HasActiveBuilding",
            "_buildingPlacementSystem.SelectedBuildingDisplayName",
            "_buildingPlacementSystem.ConfirmBuildingPlacement",
            "_buildingPlacementSystem.DeleteSelectedBuilding",
            "_buildingPlacementSystem.TryGetSelectedBuildingHealth",
            "_buildingPlacementSystem.TryGetSelectedBuildingPreviewPrefab",
            "_buildingPlacementSystem.IsRuntimeBuildingWall",
            "_buildingPlacementSystem.IsRuntimeBuildingCityGenerated",
            "_buildingPlacementSystem.TryGetRuntimeBuildingOwnerFaction",
            "_buildingPlacementSystem.HasVisibleSelectableBuilding",
            "_buildingPlacementSystem.TryResolveLiveUnitPreviewPrefab",
            "_buildingPlacementSystem.CancelBuildingPlacement",
            "_buildingPlacementSystem.FocusLastCampProductionRequest",
            "_buildingPlacementSystem.ClearSelectedBuilding",
            "_buildingPlacementSystem.ExitBuildMode"
        };

        foreach (string token in forbiddenMenuFacadeCalls)
        {
            Assert.IsFalse(
                menu.Contains(token, StringComparison.Ordinal),
                $"MenuView must use BuildingUiCommandSystem instead of {token}.");
        }
    }

    [Test]
    public void CodebaseMustNotReadBuildingPlacementSingleton()
    {
        string[] violations = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("BuildingPlacementSystem.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Do not read BuildingPlacementSystem.Instance. Use bootstrap composition or BuildingRuntimeBoundaryTag buffers:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void EditorTestsMustNotReadBuildingPlacementSingleton()
    {
        string[] violations = Directory.GetFiles("Assets/Tests/Editor", "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => path != "Assets/Tests/Editor/GameplayArchitectureContractTests.cs")
            .Where(path => File.ReadAllText(path).Contains("BuildingPlacementSystem.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Editor tests must pass BuildingPlacementSystem explicitly instead of reading BuildingPlacementSystem.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void CompositionBoundSystemsMustNotReachThroughRoadBuildSingleton()
    {
        string[] files =
        {
            "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs",
            RuntimeCityCompositionPath
        };

        string[] violations = files
            .Where(file => File.ReadAllText(file).Contains("RoadBuildSystem.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Systems that receive RoadBuildSystem from bootstrap composition must not reacquire it through RoadBuildSystem.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void CodebaseMustNotReadRoadBuildSingleton()
    {
        string[] violations = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("RoadBuildSystem.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Gameplay code must use composed RoadBuildSystem references instead of RoadBuildSystem.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void CodebaseMustNotReadRtsSelectionSingleton()
    {
        string[] violations = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("RTSSelectionSystem.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Gameplay code must use composed RTSSelectionSystem references instead of RTSSelectionSystem.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void CodebaseMustNotReadMainMenuSingleton()
    {
        string[] violations = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("MainMenuPlayUI.Instance", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Gameplay code must use composed MainMenuPlayUI references instead of MainMenuPlayUI.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void SelectionRuntimeContextSystemMustStayDeleted()
    {
        const string retiredContextFile = "Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs";
        const string retiredContextMetaFile = "Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs.meta";
        const string selectionStartupFile = "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs";
        const string managedStartupFile = "Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs";
        const string runtimeUpdateFile = "Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs";

        string selectionStartup = File.ReadAllText(selectionStartupFile);
        string managedStartup = File.ReadAllText(managedStartupFile);
        string runtimeUpdate = File.ReadAllText(runtimeUpdateFile);

        Assert.IsFalse(File.Exists(retiredContextFile), "SelectionRuntimeContextSystem.cs must stay deleted.");
        Assert.IsFalse(File.Exists(retiredContextMetaFile), "SelectionRuntimeContextSystem.cs.meta must stay deleted.");
        Assert.IsFalse(selectionStartup.Contains("SelectionRuntimeContextSystem", StringComparison.Ordinal), "SelectionGameplayStartupSystem must not construct the retired selection context.");
        Assert.IsFalse(managedStartup.Contains("SelectionRuntimeContextSystem", StringComparison.Ordinal), "ManagedGameplayStartupSystem must not reference the retired selection context.");
        Assert.IsFalse(runtimeUpdate.Contains("SelectionRuntimeContextSystem", StringComparison.Ordinal), "GameplayRuntimeUpdateSystem must not tick the retired selection context.");
    }

    [Test]
    public void SelectionGameplayStartupMustComposeNarrowSelectionBoundaries()
    {
        const string selectionStartupFile = "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs";
        string selectionStartup = File.ReadAllText(selectionStartupFile);

        foreach (string token in new[]
        {
            "new SelectionRuntimeDiagnosticsSystem()",
            "new SelectionRuntimeConfigSystem()",
            "new SelectionRuntimeQuerySystem()",
            "new RtsSelectionRuntimeInputSystem()",
            "new RtsSelectionRuntimeCameraSystem()",
            "new RtsSelectionCommandResultFlushSystem()",
            "new RtsSelectionFocusCommandSystem()",
            "new RtsSelectionPointerTargetCommandSystem()",
            "new SelectionUiReadModelSystem()",
            "new SelectionUiCommandSystem()",
            "new SelectionBuildingInteractionSystem()"
        })
        {
            StringAssert.Contains(token, selectionStartup);
        }

        StringAssert.Contains("void UpdateSelectionRuntimePhases()", selectionStartup);
        StringAssert.Contains("ProcessExternalSelectionCommandRequests", selectionStartup);
        StringAssert.Contains("ProcessQueuedMoveOrder", selectionStartup);
        StringAssert.Contains("UpdateRuntimeCameraTick", selectionStartup);
        StringAssert.Contains("UpdateNormalPointerInput", selectionStartup);
        Assert.IsFalse(selectionStartup.Contains("public void Update(", StringComparison.Ordinal), "Selection gameplay startup must not become a managed Update shell.");
    }

    [Test]
    public void SelectionRuntimeOwnerSystemsMustOwnFormerContextSlices()
    {
        string state = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionStateSystem.cs");
        string input = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionInputSystem.cs");
        string pointerTarget = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandSystem.cs");
        string focusCommand = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionFocusCommandSystem.cs");
        string commandResultFlush = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionCommandResultFlushSystem.cs");
        string runtimeCamera = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystem.cs");
        string runtimeInput = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionRuntimeInputSystem.cs");
        string uiReadModel = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionUiReadModelSystem.cs");
        string hudFeedback = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionHudFeedbackSystem.cs");
        string buildingInteraction = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionBuildingInteractionSystem.cs");

        StringAssert.Contains("CacheSelectedMoveEntities", state);
        StringAssert.Contains("RtsSelectionInputStateComponent", input);
        StringAssert.Contains("TryIssueAttackOrderToClickedUnit", pointerTarget);
        StringAssert.Contains("TryIssueMoveOrderToBuilding", pointerTarget);
        StringAssert.Contains("ProcessExternalSelectionCommandRequests", focusCommand);
        StringAssert.Contains("ProcessMoveCommandRequests", commandResultFlush);
        StringAssert.Contains("ProcessAttackCommandRequests", commandResultFlush);
        StringAssert.Contains("ProcessTransportCommandRequests", commandResultFlush);
        StringAssert.Contains("UpdateRuntimeCameraTick", runtimeCamera);
        StringAssert.Contains("UpdateNormalPointerInput", runtimeInput);
        StringAssert.Contains("FocusedUnitUiReadModelSystem _focusedUnitUiReadModelSystem", uiReadModel);
        StringAssert.Contains("SelectionHudFeedbackQueueComponent", hudFeedback);
        StringAssert.Contains("IsBoardablePlayerTransportClick", buildingInteraction);
    }

    [Test]
    public void AssistantCommandsMustUseEcsRequestBoundary()
    {
        const string componentsFile = "Assets/Game/Scripts/Components/M01AssistantCommandComponents.cs";
        const string requestSystemFile = "Assets/Game/Scripts/Tutorial/Assistant/M01AssistantCommandRequestSystem.cs";
        const string runtimeFile = "Assets/Game/Scripts/Tutorial/Assistant/M01AssistantCommandRuntime.cs";
        const string executorFile = "Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs";
        const string contextProviderFile = "Assets/Game/Scripts/Tutorial/Assistant/AssistantContextProvider.cs";
        Assert.IsTrue(File.Exists(componentsFile), "M01 assistant commands must use ECS command request/result data.");
        Assert.IsTrue(File.Exists(requestSystemFile), "M01 assistant command behavior must live behind an ECS request processor.");

        string components = File.ReadAllText(componentsFile);
        string requestSystem = File.ReadAllText(requestSystemFile);
        string runtime = File.ReadAllText(runtimeFile);
        string executor = File.ReadAllText(executorFile);
        string contextProvider = File.ReadAllText(contextProviderFile);
        StringAssert.Contains("M01AssistantCommandRequestElement : IBufferElementData", components);
        StringAssert.Contains("M01AssistantCommandResultElement : IBufferElementData", components);
        StringAssert.Contains("ProcessPendingRequests", requestSystem);
        StringAssert.Contains("SelectionHudFeedbackSystem", requestSystem);
        StringAssert.Contains("ExecuteAssistantCommand", runtime);
        StringAssert.Contains("M01AssistantCommandRequestSystem", runtime);
        Assert.IsFalse(runtime.Contains("RTSSelectionSystem", StringComparison.Ordinal), "M01AssistantCommandRuntime must not depend on RTSSelectionSystem.");
        Assert.IsFalse(executor.Contains("RTSSelectionSystem", StringComparison.Ordinal), "CommandIntentExecutor must not depend on RTSSelectionSystem.");
        Assert.IsFalse(contextProvider.Contains("RTSSelectionSystem", StringComparison.Ordinal), "AssistantContextProvider typed command readiness must not depend on RTSSelectionSystem.");
        Assert.IsFalse(runtime.Contains("BattleHudGameplayBridge.ResolveActive()?.ApplyCommandResult", StringComparison.Ordinal), "Assistant command results must flow through ECS feedback/results, not direct HUD bridge calls.");
    }

    [Test]
    public void RuntimeGameplayStateBoundaryMustNotUseRetiredSelectionContext()
    {
        const string mainMenuPlayFile = "Assets/Game/Scripts/UI/MainMenuPlayUI.cs";
        const string menuViewFile = "Assets/Game/Scripts/UI/MenuView.cs";
        const string roadBuildFile = "Assets/Game/Scripts/Systems/RoadBuildSystem.cs";
        const string buildingPlacementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string gameBootstrapFile = "Assets/Game/Scripts/Bootstrap/GameBootstrap.cs";
        const string stateSystemFile = "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs";
        const string stateComponentsFile = "Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs";
        Assert.IsTrue(File.Exists(stateSystemFile), "Runtime gameplay state access must go through RuntimeGameplayStateSystem.");
        Assert.IsTrue(File.Exists(stateComponentsFile), "Runtime gameplay state must have ECS singleton components.");

        string[] files =
        {
            mainMenuPlayFile,
            menuViewFile,
            roadBuildFile,
            buildingPlacementFile,
            gameBootstrapFile
        };

        foreach (string file in files)
        {
            string code = File.ReadAllText(file);
            Assert.IsFalse(
                code.Contains("SelectionRuntimeContextSystem", StringComparison.Ordinal),
                $"{file} must not reference the retired selection context.");
            Assert.IsFalse(
                code.Contains("InitialUnitsRuntimeState.PlayRequested", StringComparison.Ordinal),
                $"{file} must use RuntimeGameplayStateSystem for PlayRequested.");
        }

        string stateSystem = File.ReadAllText(stateSystemFile);
        string components = File.ReadAllText(stateComponentsFile);
        StringAssert.Contains("ResetForGameplayStart", stateSystem);
        StringAssert.Contains("RuntimeGameplayStateComponent : IComponentData", components);
        StringAssert.Contains("RuntimeCameraInputComponent : IComponentData", components);
        StringAssert.Contains("RuntimeCameraFocusRequestComponent : IComponentData", components);
    }

    [Test]
    public void ProductionScriptsMustNotReadPlayRequestedFromLegacyRuntimeState()
    {
        string[] scriptFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => path != "Assets/Game/Scripts/UI/InitialUnitsRuntimeState.cs")
            .Where(path => path != "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs")
            .ToArray();

        foreach (string file in scriptFiles)
        {
            string code = File.ReadAllText(file);
            Assert.IsFalse(
                code.Contains("InitialUnitsRuntimeState.PlayRequested", StringComparison.Ordinal),
                $"{file} must read/write PlayRequested through RuntimeGameplayStateSystem or RuntimeGameplayStateComponent.");
        }
    }

    [Test]
    public void ProductionScriptsMustNotReadPlayerAutoModeFromLegacyRuntimeState()
    {
        string[] scriptFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => path != "Assets/Game/Scripts/UI/InitialUnitsRuntimeState.cs")
            .Where(path => path != "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs")
            .ToArray();

        foreach (string file in scriptFiles)
        {
            string code = File.ReadAllText(file);
            Assert.IsFalse(
                code.Contains("InitialUnitsRuntimeState.PlayerAutoModeEnabled", StringComparison.Ordinal),
                $"{file} must read/write PlayerAutoModeEnabled through RuntimeGameplayStateSystem or RuntimeGameplayStateComponent.");
        }
    }

    [Test]
    public void ProductionScriptsMustNotReadWorldCameraFromLegacyRuntimeState()
    {
        string[] scriptFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .Where(path => path != "Assets/Game/Scripts/UI/InitialUnitsRuntimeState.cs")
            .Where(path => path != "Assets/Game/Scripts/Systems/RuntimeCameraReferenceSystem.cs")
            .ToArray();

        foreach (string file in scriptFiles)
        {
            string code = File.ReadAllText(file);
            Assert.IsFalse(
                code.Contains("InitialUnitsRuntimeState.WorldCamera", StringComparison.Ordinal),
                $"{file} must read/write WorldCamera through RuntimeCameraReferenceSystem or RuntimeCameraReferenceComponent.");
        }
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedValidationSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string validationFile = "Assets/Game/Scripts/Systems/BuildingPlacementValidationSystem.cs";
        const string placementContextFile = "Assets/Game/Scripts/Systems/BuildingPlacementContextSystem.cs";
        Assert.IsTrue(File.Exists(validationFile), "The building validation slice must live in BuildingPlacementValidationSystem.");
        Assert.IsTrue(File.Exists(placementContextFile), "Placement context construction must live in BuildingPlacementContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string validation = File.ReadAllText(validationFile);
        string placementContext = File.ReadAllText(placementContextFile);
        StringAssert.Contains("BuildingPlacementValidationSystem.RebuildInvalidPrefix", placement);
        StringAssert.Contains("BuildingPlacementValidationSystem.IsPlacementRectValid", placement);
        StringAssert.Contains("_buildingPlacementValidationSystem.AreAllPendingWallRunsValid", placement);
        StringAssert.Contains("_buildingPlacementValidationSystem.AreWallPlacementOriginsValid", placement);
        StringAssert.Contains("_buildingPlacementContextSystem.CreateWallValidationContext", placement);
        StringAssert.Contains("new BuildingPlacementValidationSystem.WallValidationContext", placementContext);
        StringAssert.Contains("IsWallFootprintValid", validation);
        StringAssert.Contains("DoWallSegmentsConflict", validation);
        StringAssert.Contains("AreAllPendingWallRunsValid", validation);
        StringAssert.Contains("AreWallPlacementOriginsValid", validation);
        StringAssert.Contains("IsWallPlacementValid", validation);
        StringAssert.Contains("IsLinearWallOverlapCell", validation);
        StringAssert.Contains("IsPerpendicularWallOverlapCell", validation);
        Assert.IsFalse(
            placement.Contains("private static bool DoWallSegmentsConflict", StringComparison.Ordinal),
            "Wall segment conflict rules belong in BuildingPlacementValidationSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+AreAllPendingWallRunsValid\b"),
            "Pending wall-run validation belongs in BuildingPlacementValidationSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+AreWallPlacementOriginsValid\b"),
            "Wall origin validation belongs in BuildingPlacementValidationSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+IsWallPlacementValid\b"),
            "Wall segment placement validity belongs in BuildingPlacementValidationSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+IsLinearWallOverlapCell\b|\bprivate\s+bool\s+IsPerpendicularWallOverlapCell\b"),
            "Wall overlap-cell checks belong in BuildingPlacementValidationSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateRuntimeBuildingRegistrySlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string runtimeBuildingFile = "Assets/Game/Scripts/Systems/RuntimeBuildingSystem.cs";
        const string runtimeCreationFile = "Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs";
        const string runtimeContextFile = "Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs";
        const string combatFile = "Assets/Game/Scripts/Systems/BuildingCombatSystem.cs";
        const string buildingCompositionFile = "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs";
        Assert.IsTrue(File.Exists(runtimeBuildingFile), "The runtime building registry slice must live in RuntimeBuildingSystem.");
        Assert.IsTrue(File.Exists(runtimeCreationFile), "Runtime building creation orchestration must live in BuildingRuntimeCreationSystem.");
        Assert.IsTrue(File.Exists(runtimeContextFile), "Runtime creation context construction must live in BuildingRuntimeContextSystem.");
        Assert.IsTrue(File.Exists(combatFile), "Runtime building destruction/removal orchestration must live in BuildingCombatSystem.");

        string placement = File.ReadAllText(placementFile);
        string buildingComposition = File.ReadAllText(buildingCompositionFile);
        string runtimeCreation = File.ReadAllText(runtimeCreationFile);
        string runtimeContext = File.ReadAllText(runtimeContextFile);
        string combat = File.ReadAllText(combatFile);
        StringAssert.Contains("RuntimeBuildingSystem<RuntimeBuildingData>", placement);
        StringAssert.Contains("RuntimeBuildingRegistry => _runtimeBuildingSystem", placement);
        StringAssert.Contains("RuntimeBuildingSystem<RuntimeBuildingData> registry = placement.RuntimeBuildingRegistry", buildingComposition);
        StringAssert.Contains("registry.Buildings", buildingComposition);
        StringAssert.Contains("registry.Count", buildingComposition);
        StringAssert.Contains("BuildingRuntimeCreationSystem _buildingRuntimeCreationSystem", placement);
        StringAssert.Contains("_buildingRuntimeCreationSystem.RegisterRuntimeBuilding", placement);
        StringAssert.Contains("BuildingRuntimeContextSystem _buildingRuntimeContextSystem", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateCreationContext(CreateBuildingRuntimeContextSource())", placement);
        StringAssert.Contains("new BuildingRuntimeCreationSystem.Context", runtimeContext);
        StringAssert.Contains("context.RuntimeBuildingSystem.RemoveBuilding", combat);
        StringAssert.Contains("_runtimeBuildingSystem.SelectBuilding", placement);
        StringAssert.Contains("context.RuntimeBuildingSystem.ClearSelection", combat);
        StringAssert.Contains("context.RuntimeBuildingSystem.AllocateId()", runtimeCreation);
        StringAssert.Contains("context.RuntimeBuildingSystem.AddBuilding", runtimeCreation);
        StringAssert.Contains("new RuntimeBuildingData", runtimeCreation);
        StringAssert.Contains("PendingProductions = new List<RuntimeBuildingData.PendingProduction>()", runtimeCreation);
        StringAssert.Contains("ProducedUnits = new List<Entity>()", runtimeCreation);
        StringAssert.Contains("ProducedUnitSlots = new Entity", runtimeCreation);
        StringAssert.Contains("AttachRuntimeLink", runtimeCreation);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bint\s+_nextBuildingId\b"),
            "Runtime building id allocation belongs in RuntimeBuildingSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bint\?\s+_selectedBuildingId\b|\bint\?\s+_activeBuildingId\b"),
            "Active/selected runtime building ids belong in RuntimeBuildingSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("RuntimeBuildingCount", StringComparison.Ordinal) ||
            placement.Contains("RuntimeBuildings =>", StringComparison.Ordinal) ||
            placement.Contains("_runtimeBuildings", StringComparison.Ordinal),
            "Runtime building count/dictionary read surfaces belong in RuntimeBuildingSystem, not BuildingPlacementSystem facade properties.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+RuntimeBuildingData\s*\{"),
            "Runtime building data creation belongs in BuildingRuntimeCreationSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+AttachRuntimeLink\b"),
            "Runtime building link attachment belongs in BuildingRuntimeCreationSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingRuntimeCreationSystem.Context", StringComparison.Ordinal),
            "Runtime creation context construction belongs in BuildingRuntimeContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateRuntimeEntityCreationSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string runtimeEntityFile = "Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs";
        Assert.IsTrue(File.Exists(runtimeEntityFile), "Runtime blocker/combat entity creation must live in BuildingRuntimeEntitySystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeEntity = File.ReadAllText(runtimeEntityFile);
        StringAssert.Contains("BuildingRuntimeEntitySystem _buildingRuntimeEntitySystem", placement);
        StringAssert.Contains("CreateBuildingRuntimeEntityContext", placement);
        StringAssert.Contains("_buildingRuntimeEntitySystem.CreateBlockerEntity", placement);
        StringAssert.Contains("_buildingRuntimeEntitySystem.ShouldRuntimeBuildingBlockPathing", placement);
        StringAssert.Contains("_buildingRuntimeEntitySystem.CreateBuildingCombatEntity", placement);

        StringAssert.Contains("CreateBlockerEntity", runtimeEntity);
        StringAssert.Contains("ShouldRuntimeBuildingBlockPathing", runtimeEntity);
        StringAssert.Contains("CreateBuildingCombatEntity", runtimeEntity);
        StringAssert.Contains("GridBlockerSize", runtimeEntity);
        StringAssert.Contains("StaticGridBlocker", runtimeEntity);
        StringAssert.Contains("RuntimeBuildingCombatTag", runtimeEntity);
        StringAssert.Contains("UnitHealth", runtimeEntity);
        StringAssert.Contains("ThreatDetector", runtimeEntity);

        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+Entity\s+CreateBlockerEntity[\s\S]*?em\.CreateEntity\s*\("),
            "Runtime blocker entity creation belongs in BuildingRuntimeEntitySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+Entity\s+CreateBuildingCombatEntity[\s\S]*?em\.CreateEntity\s*\("),
            "Runtime building combat entity creation belongs in BuildingRuntimeEntitySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"AddComponent<RuntimeBuildingCombatTag>\s*\("),
            "Runtime building combat tag setup belongs in BuildingRuntimeEntitySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"AddComponentData\s*\(\s*entity\s*,\s*new\s+GridBlockerSize"),
            "Runtime blocker size setup belongs in BuildingRuntimeEntitySystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegatePlacementRedirectSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string redirectFile = "Assets/Game/Scripts/Systems/BuildingPlacementRedirectSystem.cs";
        const string buildingCompositionFile = "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs";
        Assert.IsTrue(File.Exists(redirectFile), "Placement redirect side effects must live in BuildingPlacementRedirectSystem.");

        string placement = File.ReadAllText(placementFile);
        string redirect = File.ReadAllText(redirectFile);
        string buildingComposition = File.ReadAllText(buildingCompositionFile);
        StringAssert.Contains("BuildingPlacementRedirectSystem _buildingPlacementRedirectSystem", placement);
        StringAssert.Contains("CreateBuildingPlacementRedirectContext", placement);
        StringAssert.Contains("_buildingPlacementRedirectSystem.BeginDeferredRuntimeBuildingSideEffects", placement);
        StringAssert.Contains("_buildingPlacementRedirectSystem.EndDeferredRuntimeBuildingSideEffects", placement);
        StringAssert.Contains("tickDomains.Redirect.FlushPendingMarkerRefresh", buildingComposition);
        StringAssert.Contains("_buildingPlacementRedirectSystem.RedirectUnitsAroundPlacedBuilding", placement);
        StringAssert.Contains("_buildingPlacementRedirectSystem.AddDeferredRedirectFootprint", placement);
        StringAssert.Contains("_buildingPlacementRedirectSystem.MarkPendingMarkerRefresh", placement);

        StringAssert.Contains("BeginDeferredRuntimeBuildingSideEffects", redirect);
        StringAssert.Contains("EndDeferredRuntimeBuildingSideEffects", redirect);
        StringAssert.Contains("FlushPendingMarkerRefresh", redirect);
        StringAssert.Contains("RedirectUnitsAroundPlacedBuildings", redirect);
        StringAssert.Contains("DoesRemainingPathIntersectFootprint", redirect);
        StringAssert.Contains("TryFindNearestPerimeterCell", redirect);
        StringAssert.Contains("ReserveBuildingBuffer", redirect);
        StringAssert.Contains("RemoveComponent<ManualMoveOrderTag>", redirect);

        Assert.IsFalse(
            placement.Contains("_deferredRedirectFootprints", StringComparison.Ordinal),
            "Deferred redirect footprints belong in BuildingPlacementRedirectSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("_pendingMarkerRefresh", StringComparison.Ordinal),
            "Pending marker refresh deferral belongs in BuildingPlacementRedirectSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("_deferRuntimeBuildingSideEffectsDepth", StringComparison.Ordinal),
            "Runtime side-effect defer depth belongs in BuildingPlacementRedirectSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+RedirectUnitsAroundPlacedBuildings\b"),
            "Placed-building unit redirect scans belong in BuildingPlacementRedirectSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+DoesRemainingPathIntersectFootprint\b"),
            "Redirect path intersection checks belong in BuildingPlacementRedirectSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryFindNearestPerimeterCell\b"),
            "Redirect perimeter-goal search belongs in BuildingPlacementRedirectSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+ReserveBuildingBuffer\b"),
            "Redirect reservation buffers belong in BuildingPlacementRedirectSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\b(?:internal|private)\s+void\s+FlushPendingMarkerRefresh\s*\("),
            "Runtime marker-refresh ticks must be wired to BuildingPlacementRedirectSystem from composition, not wrapped by BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedRuntimeQuerySlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string runtimeQueryFile = "Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs";
        Assert.IsTrue(File.Exists(runtimeQueryFile), "Runtime building read/query behavior must live in BuildingRuntimeQuerySystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeQuery = File.ReadAllText(runtimeQueryFile);
        StringAssert.Contains("BuildingRuntimeQuerySystem _buildingRuntimeQuerySystem", placement);
        StringAssert.Contains("CreateBuildingRuntimeQueryContext", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.GetRuntimeHouseBuildingIds", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.GetRuntimeBuildingIdsByRole", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.TryGetRuntimeBuildingFocusWorldPosition", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.TryGetRuntimeBuildingDestroyedState", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.TryGetRuntimeBuildingRefugeeSettings", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.IsRuntimeBuildingCityGenerated", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.IsRuntimeBuildingWall", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.TryGetRuntimeBuildingOwnerFaction", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.TryGetRuntimeBuildingCombatInfo", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.TryGetRuntimeBuildingApproachCell", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.IsRuntimeBuildingApproachCell", placement);

        StringAssert.Contains("CountRuntimeBuildingsForFaction", runtimeQuery);
        StringAssert.Contains("CountRuntimeProducedUnitsForFaction", runtimeQuery);
        StringAssert.Contains("CountPendingProductionsForFaction", runtimeQuery);
        StringAssert.Contains("GetRuntimeHouseBuildingIds", runtimeQuery);
        StringAssert.Contains("GetRuntimeBuildingIdsByRole", runtimeQuery);
        StringAssert.Contains("TryGetRuntimeBuildingFocusWorldPosition", runtimeQuery);
        StringAssert.Contains("TryGetRuntimeBuildingDestroyedState", runtimeQuery);
        StringAssert.Contains("TryGetRuntimeBuildingRefugeeSettings", runtimeQuery);
        StringAssert.Contains("TryGetRuntimeBuildingCombatInfo", runtimeQuery);
        StringAssert.Contains("TryGetRuntimeBuildingApproachCell", runtimeQuery);
        StringAssert.Contains("IsRuntimeBuildingApproachCell", runtimeQuery);

        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+int\s+CountRuntimeBuildingsForFaction\([\s\S]*?foreach\s*\(KeyValuePair<int,\s*RuntimeBuildingData>[\s\S]*?public\s+int\s+CountRuntimeBuildingsForFaction\(byte\s+factionId,\s+string\s+buildingId\)"),
            "Faction building count iteration belongs in BuildingRuntimeQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+int\s+CountRuntimeProducedUnitsForFaction\([\s\S]*?PruneProducedUnits[\s\S]*?public\s+int\s+CountPendingProductionsForFaction"),
            "Produced-unit count pruning and iteration belong in BuildingRuntimeQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+int\s+Count(RuntimeBuildings|RuntimeProducedUnits|PendingProductions)ForFaction"),
            "Faction count compatibility wrappers must not remain on BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+void\s+GetRuntimeHouseBuildingIds\([\s\S]*?foreach\s*\(KeyValuePair<int,\s*RuntimeBuildingData>[\s\S]*?public\s+void\s+GetRuntimeBuildingIdsByRole"),
            "Runtime house id queries belong in BuildingRuntimeQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+bool\s+TryGetRuntimeBuildingCombatInfo\([\s\S]*?TryFindRuntimeBuildingByCombatEntity[\s\S]*?public\s+bool\s+TryResolveBaseBreachTarget"),
            "Runtime combat entity info queries belong in BuildingRuntimeQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+bool\s+TryGetRuntimeBuildingApproachCell\([\s\S]*?TryFindBuildingApproachCell[\s\S]*?public\s+bool\s+IsRuntimeBuildingApproachCell"),
            "Runtime approach-cell query routing belongs in BuildingRuntimeQuerySystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedDefinitionSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string definitionDataFile = "Assets/Game/Scripts/Systems/BuildingDefinition.cs";
        const string runtimeBuildingDataFile = "Assets/Game/Scripts/Systems/RuntimeBuildingData.cs";
        const string definitionFile = "Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs";
        const string startupFile = "Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs";
        const string runtimeSpawnFile = "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs";
        Assert.IsTrue(File.Exists(definitionDataFile), "BuildingDefinition must be a standalone domain data contract, not nested under BuildingPlacementSystem.");
        Assert.IsTrue(File.Exists(runtimeBuildingDataFile), "RuntimeBuildingData must be a standalone domain data contract, not nested under BuildingPlacementSystem.");
        Assert.IsTrue(File.Exists(definitionFile), "Building definition and prefab metadata behavior must live in BuildingDefinitionSystem.");
        Assert.IsTrue(File.Exists(startupFile), "Building placement config/init ownership must live in BuildingPlacementStartupSystem.");

        string placement = File.ReadAllText(placementFile);
        string definitionData = File.ReadAllText(definitionDataFile);
        string runtimeBuildingData = File.ReadAllText(runtimeBuildingDataFile);
        string definition = File.ReadAllText(definitionFile);
        string startup = File.ReadAllText(startupFile);
        string runtimeSpawn = File.ReadAllText(runtimeSpawnFile);

        StringAssert.Contains("BuildingDefinitionSystem _buildingDefinitionSystem", placement);
        StringAssert.Contains("_buildingPlacementStartupSystem.Init", placement);
        StringAssert.Contains("_buildingPlacementStartupSystem.ApplyConfigIfAvailable", placement);
        StringAssert.Contains("definitionSystem.RebuildSpawnablesLookup", startup);
        StringAssert.Contains("definitionSystem.RebuildConfiguredSpawnableDefinitions", startup);
        StringAssert.Contains("new GameObject(\"RuntimeBuildings\")", startup);
        StringAssert.Contains("previewSystem.Init", startup);
        StringAssert.Contains("context.DefinitionSystem.CreateRuntimeBuildingDefinition", runtimeSpawn);
        StringAssert.Contains("TryGetConfiguredSpawnable", definition);
        StringAssert.Contains("TryGetConfiguredUnit", definition);
        StringAssert.Contains("ConfiguredUnitSpawnPrefabs", definition);
        StringAssert.Contains("TryResolveConfiguredSpawnablePrefab", definition);
        StringAssert.Contains("_buildingDefinitionSystem.TryResolveConfiguredUnitSpawnPrefab", placement);
        StringAssert.Contains("BuildingDefinitionSystem.GetProductionPrefab", placement);
        StringAssert.Contains("TryGetPrefabLocalBounds", definition);
        StringAssert.Contains("BuildingDefinitionSystem.RuntimeBuildingMatchesId", placement);
        StringAssert.Contains("UnitPrefabMatchesId", definition);

        StringAssert.Contains("CachedRuntimeBuildingMetadata", definition);
        StringAssert.Contains("RebuildConfiguredSpawnableDefinitions", definition);
        StringAssert.Contains("ConfiguredSpawnablePrefabs", definition);
        StringAssert.Contains("ConfiguredUnitSpawnPrefabs", definition);
        StringAssert.Contains("TryGetConfiguredUnit", definition);
        StringAssert.Contains("CreateRuntimeBuildingDefinition", definition);
        StringAssert.Contains("CreateDefinition", definition);
        StringAssert.Contains("TryGetPrefabLocalBounds", definition);
        StringAssert.Contains("FindProductionSpawnLocalPositions", definition);
        StringAssert.Contains("RegisterSpawnableLookupAliases", definition);
        StringAssert.Contains("internal sealed class BuildingDefinition", definitionData);
        StringAssert.Contains("internal sealed class RuntimeBuildingData", runtimeBuildingData);
        StringAssert.Contains("BuildingDefinition Definition", runtimeBuildingData);

        Assert.IsFalse(
            placement.Contains("RebuildSpawnablesLookup", StringComparison.Ordinal) ||
            placement.Contains("RebuildConfiguredSpawnableDefinitions", StringComparison.Ordinal) ||
            placement.Contains("new GameObject(\"RuntimeBuildings\")", StringComparison.Ordinal),
            "Building placement config/root startup belongs in BuildingPlacementStartupSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\[SerializeField(?:,\s*HideInInspector)?\]\s+private\s+(?:BuildingPlacementSystemConfig|Camera|float|Color)\s+"),
            "BuildingPlacementSystem must not own serialized placement config/cache fields; use BuildingPlacementStartupSystem.");

        Assert.IsFalse(
            Regex.IsMatch(placement, @"internal\s+sealed\s+class\s+BuildingDefinition\b"),
            "BuildingDefinition must not be nested inside BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"internal\s+sealed\s+class\s+RuntimeBuildingData\b"),
            "RuntimeBuildingData must not be nested inside BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(definition, @"BuildingPlacementSystem\.(?:BuildingDefinition|RuntimeBuildingData)"),
            "BuildingDefinitionSystem must use standalone building data contracts, not facade-nested types.");
        Assert.IsFalse(
            Regex.IsMatch(runtimeSpawn, @"BuildingPlacementSystem\.(?:BuildingDefinition|RuntimeBuildingData)"),
            "BuildingRuntimeSpawnSystem must use standalone building data contracts, not facade-nested types.");

        string[] placementStateDebtTokens =
        {
            "CachedRuntimeBuildingMetadata",
            "_runtimeBuildingMetadataCache",
            "_spawnablesByKey",
            "_configuredDefinitionsByPrefab",
            "_configuredSpawnableDefinitions",
            "_unitSpawnPrefabsByKey"
        };

        foreach (string token in placementStateDebtTokens)
        {
            Assert.IsFalse(
                placement.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in BuildingDefinitionSystem, not BuildingPlacementSystem.");
        }

        string[] migratedMethodPatterns =
        {
            @"\bprivate\s+(?:static\s+)?bool\s+TryGetPrefabLocalBounds\b",
            @"\bprivate\s+(?:static\s+)?Vector3\[\]\s+FindProductionSpawnLocalPositions\b",
            @"\bprivate\s+(?:static\s+)?void\s+RegisterSpawnableLookupAliases\b",
            @"\bprivate\s+BuildingDefinition\s+CreateRuntimeBuildingDefinition\b",
            @"\bprivate\s+BuildingDefinition\s+CreateDefinition\b",
            @"\bprivate\s+(?:static\s+)?bool\s+RuntimeDefinitionMatchesId\b",
            @"\bprivate\s+(?:static\s+)?bool\s+UnitPrefabMatchesId\b",
            @"\b\[SerializeField,\s*HideInInspector\]\s+private\s+List<GameObject>\s+spawnables\b",
            @"\b\[SerializeField,\s*HideInInspector\]\s+private\s+UnitPrefabRegistryAuthoringConfig\s+unitPrefabRegistryConfig\b",
            @"\b\[SerializeField,\s*HideInInspector\]\s+private\s+List<GameObject>\s+unitSpawnPrefabs\b",
            @"\bprivate\s+bool\s+TryGetConfiguredSpawnable\b",
            @"\bprivate\s+bool\s+TryGetConfiguredUnit\b",
            @"\bprivate\s+(?:static\s+)?string\s+ResolveConfiguredUnitDisplayName\b",
            @"\bprivate\s+bool\s+IsConfiguredSpawnablePrefab\b"
        };

        foreach (string pattern in migratedMethodPatterns)
        {
            Assert.IsFalse(
                Regex.IsMatch(placement, pattern),
                "Building definition/prefab metadata helpers belong in BuildingDefinitionSystem, not BuildingPlacementSystem.");
        }
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedSelectionSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string selectionFile = "Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs";
        const string selectionClickFile = "Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs";
        const string inputRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs";
        Assert.IsTrue(File.Exists(selectionFile), "Building selection behavior must live in BuildingSelectionSystem.");
        Assert.IsTrue(File.Exists(selectionClickFile), "Building selection screen-click routing must live in BuildingSelectionClickSystem.");
        Assert.IsTrue(File.Exists(inputRuntimeTickFile), "Building pointer-to-selection frame flow must live in BuildingPlacementInputRuntimeTickSystem.");

        string placement = File.ReadAllText(placementFile);
        string selection = File.ReadAllText(selectionFile);
        string selectionClick = File.ReadAllText(selectionClickFile);
        string inputRuntimeTick = File.ReadAllText(inputRuntimeTickFile);
        StringAssert.Contains("BuildingSelectionSystem _buildingSelectionSystem", placement);
        StringAssert.Contains("BuildingSelectionClickSystem _buildingSelectionClickSystem", placement);
        StringAssert.Contains("CreateBuildingSelectionContext", placement);
        StringAssert.Contains("CreateBuildingSelectionClickContext", placement);
        StringAssert.Contains("_buildingSelectionSystem.ClearSelectedBuilding", placement);
        StringAssert.Contains("_buildingSelectionSystem.SelectAndFocusBuilding", placement);
        StringAssert.Contains("_buildingSelectionSystem.ResolveBuildingFocusWorldPosition", placement);
        StringAssert.Contains("SelectionClickSystem?.HandleBuildingSelectionClick", inputRuntimeTick);

        StringAssert.Contains("SelectAndFocusBuilding", selection);
        StringAssert.Contains("ResolveBuildingFocusWorldPosition", selection);
        StringAssert.Contains("HandleBuildingSelectionClick", selection);
        StringAssert.Contains("TryAssignSelectedHaulerOrders", selection);
        StringAssert.Contains("TryIssueMoveOrderToBuilding", selection);
        StringAssert.Contains("IsBoardablePlayerTransportClick", selection);
        StringAssert.Contains("public Context CreateContext(Source source)", selection);
        StringAssert.Contains("HasPendingPathJob", selectionClick);
        StringAssert.Contains("TryGetGridCell", selectionClick);
        StringAssert.Contains("HandleCellSelection", selectionClick);
        StringAssert.Contains("public Context CreateContext(Source source)", selectionClick);
        Assert.IsFalse(
            placement.Contains("new BuildingSelectionSystem.Context", StringComparison.Ordinal) ||
            placement.Contains("new BuildingSelectionClickSystem.Context", StringComparison.Ordinal),
            "Building selection context construction belongs in BuildingSelectionSystem/BuildingSelectionClickSystem, not BuildingPlacementSystem.");

        Assert.IsFalse(
            Regex.IsMatch(placement, @"\b(?:internal|private)\s+void\s+HandleBuildingSelectionClick\s*\("),
            "Screen-click selection routing belongs in BuildingSelectionClickSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"UnitPathfindingSystem\.HasPendingPathJob\s*\)\s*return"),
            "Pending path-job click guard belongs in BuildingSelectionClickSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_runtimeBuildingSystem\.SelectBuilding\(entry\.Key\)"),
            "Building click selection routing belongs in BuildingSelectionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"TryAssignSelectedHaulerOrders\(entry\.Key\)"),
            "Building click hauler-order routing belongs in BuildingSelectionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"IsBoardablePlayerTransportClick\(screenPosition\)"),
            "Building click transport-selection guard belongs in BuildingSelectionSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedVisualSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string visualFile = "Assets/Game/Scripts/Systems/BuildingVisualSystem.cs";
        const string runtimeVisualFile = "Assets/Game/Scripts/Systems/BuildingRuntimeVisualSystem.cs";
        const string runtimeContextFile = "Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs";
        const string buildingCompositionFile = "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs";
        Assert.IsTrue(File.Exists(visualFile), "The building visual slice must live in BuildingVisualSystem.");
        Assert.IsTrue(File.Exists(runtimeVisualFile), "The runtime building visual slice must live in BuildingRuntimeVisualSystem.");
        Assert.IsTrue(File.Exists(runtimeContextFile), "Runtime visual context construction must live in BuildingRuntimeContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeVisual = File.ReadAllText(runtimeVisualFile);
        string runtimeContext = File.ReadAllText(runtimeContextFile);
        string buildingComposition = File.ReadAllText(buildingCompositionFile);
        StringAssert.Contains("BuildingVisualSystem _buildingVisualSystem", placement);
        StringAssert.Contains("BuildingRuntimeVisualSystem _buildingRuntimeVisualSystem", placement);
        StringAssert.Contains("CreateBuildingRuntimeVisualContext", placement);
        StringAssert.Contains("_buildingRuntimeVisualSystem.InitializeBuildingVisuals", placement);
        StringAssert.Contains("tickDomains.RuntimeVisual.UpdateBuildingResourceVisuals", buildingComposition);
        StringAssert.Contains("_buildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility", placement);
        StringAssert.Contains("FindDescendantByName", runtimeVisual);
        StringAssert.Contains("SetTransformVisible", runtimeVisual);
        StringAssert.Contains("ApplyMarkerColor", runtimeVisual);
        StringAssert.Contains("FindAnimatedBuildingParts", runtimeVisual);
        StringAssert.Contains("UpdateAnimatedBuildingParts", runtimeVisual);
        StringAssert.Contains("InitializeBuildingVisuals", runtimeVisual);
        StringAssert.Contains("UpdateBuildingResourceVisuals", runtimeVisual);
        StringAssert.Contains("RefreshBuildingMarkerVisibility", runtimeVisual);
        StringAssert.Contains("new BuildingRuntimeVisualSystem.Context", runtimeContext);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?Transform\s+FindDescendantByName\b"),
            "Descendant lookup belongs in BuildingVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+SetTransformVisible\b"),
            "Transform visibility belongs in BuildingVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+ApplyMarkerColor\b"),
            "Marker color application belongs in BuildingVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+UpdateAnimatedBuildingParts\b"),
            "Animated building part updates belong in BuildingVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?BuildingVisualSystem\.AnimatedPart\[\]\s+FindAnimatedBuildingParts\b|\bprivate\s+(?:static\s+)?RuntimeBuildingData\.AnimatedPart\[\]\s+FindAnimatedBuildingParts\b"),
            "Animated building part discovery belongs in BuildingVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryParseAnimatedPartName\b"),
            "Animated building part name parsing belongs in BuildingVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"building\.FactionMarker\s*=\s*_buildingVisualSystem\.FindDescendantByName"),
            "Runtime building visual initialization belongs in BuildingRuntimeVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"building\.AnimatedParts\s*=\s*_buildingVisualSystem\.FindAnimatedBuildingParts"),
            "Runtime animated-part discovery assignment belongs in BuildingRuntimeVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bStoredOilBarrels\s*<\s*building\.Definition\.OilStorageCapacity"),
            "Runtime resource animation state projection belongs in BuildingRuntimeVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"SetTransformVisible\(building\.SelectionMarker,\s*selected\)"),
            "Runtime selection marker projection belongs in BuildingRuntimeVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?float\s+NormalizeSignedAngle\b"),
            "Runtime door visual angle normalization belongs in BuildingRuntimeVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\b(?:internal|private)\s+void\s+UpdateBuildingResourceVisuals\s*\("),
            "Runtime resource visual ticks must be wired to BuildingRuntimeVisualSystem from composition, not wrapped by BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingRuntimeVisualSystem.Context", StringComparison.Ordinal),
            "Runtime visual context construction belongs in BuildingRuntimeContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedPlacementVisualSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string placementVisualFile = "Assets/Game/Scripts/Systems/BuildingPlacementVisualSystem.cs";
        Assert.IsTrue(File.Exists(placementVisualFile), "The placement visual slice must live in BuildingPlacementVisualSystem.");

        string placement = File.ReadAllText(placementFile);
        string placementVisual = File.ReadAllText(placementVisualFile);
        StringAssert.Contains("BuildingPlacementVisualSystem _buildingPlacementVisualSystem", placement);
        StringAssert.Contains("_buildingPlacementVisualSystem.CreateBuildingVisualInstance", placement);
        StringAssert.Contains("_buildingPlacementVisualSystem.PositionBuildingObject", placement);

        StringAssert.Contains("CreateBuildingVisualInstance", placementVisual);
        StringAssert.Contains("PositionBuildingObject", placementVisual);
        StringAssert.Contains("TryGetPrefabModelBounds", placementVisual);
        StringAssert.Contains("TransformBounds", placementVisual);
        StringAssert.Contains("CombinedMesh", placementVisual);
        StringAssert.Contains("FindDescendantByName(visual.transform, \"CombinedMesh\")", placementVisual);
        StringAssert.Contains("DisableSourceRenderersOutsideCombinedMesh", placementVisual);
        StringAssert.Contains("private static Transform FindDescendantByName", placementVisual);
        StringAssert.Contains("SetPositionAndRotation", placementVisual);

        Assert.IsFalse(
            Regex.IsMatch(placement, @"Object\.Instantiate\s*\("),
            "Placement visual instantiation belongs in BuildingPlacementVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"transform\.Find\s*\(\s*""CombinedMesh""\s*\)"),
            "Placement visual child selection belongs in BuildingPlacementVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"SetPositionAndRotation\s*\("),
            "Placement visual positioning belongs in BuildingPlacementVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryGetPrefabModelBounds\b"),
            "Prefab model bounds belong in BuildingPlacementVisualSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?Bounds\s+TransformBounds\b"),
            "Transformed bounds math belongs in BuildingPlacementVisualSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateRuntimeManualSpawnSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string runtimeSpawnFile = "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs";
        const string runtimeSpawnCommandFile = "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCommandSystem.cs";
        const string runtimeContextFile = "Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs";
        Assert.IsTrue(File.Exists(runtimeSpawnFile), "Runtime/manual building spawn orchestration must live in BuildingRuntimeSpawnSystem.");
        Assert.IsTrue(File.Exists(runtimeSpawnCommandFile), "Runtime/manual building spawn command translation must live in BuildingRuntimeSpawnCommandSystem.");
        Assert.IsTrue(File.Exists(runtimeContextFile), "Runtime spawn context construction must live in BuildingRuntimeContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeSpawn = File.ReadAllText(runtimeSpawnFile);
        string runtimeSpawnCommand = File.ReadAllText(runtimeSpawnCommandFile);
        string runtimeContext = File.ReadAllText(runtimeContextFile);
        StringAssert.Contains("BuildingRuntimeSpawnSystem _buildingRuntimeSpawnSystem", placement);
        StringAssert.Contains("BuildingRuntimeSpawnCommandSystem _buildingRuntimeSpawnCommandSystem", placement);
        StringAssert.Contains("CreateBuildingRuntimeContextSource", placement);
        StringAssert.Contains("CreateRuntimeSpawnCommandContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateSpawnContext(CreateBuildingRuntimeContextSource())", placement);
        StringAssert.Contains("new BuildingRuntimeSpawnSystem.Context", runtimeContext);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.SpawnInitialTestRoster", placement);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.TrySpawnRuntimeBuilding", placement);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.TrySpawnRuntimeWallRun", placement);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.TrySpawnRuntimeWallSegment", placement);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.TryGetRuntimeWallSegmentFootprint", placement);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.TryGetRuntimeBuildingPlacementFootprint", placement);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.TrySpawnInitialBuilding", placement);
        StringAssert.Contains("_buildingRuntimeSpawnCommandSystem.TryResolveInitialPlacementOrigin", placement);
        StringAssert.Contains("context.RuntimeSpawnSystem?.SpawnInitialTestRoster", runtimeSpawnCommand);
        StringAssert.Contains("context.RuntimeSpawnSystem.TrySpawnRuntimeBuilding", runtimeSpawnCommand);
        StringAssert.Contains("context.RuntimeSpawnSystem.TrySpawnRuntimeWallRun", runtimeSpawnCommand);
        StringAssert.Contains("context.RuntimeSpawnSystem.TrySpawnRuntimeWallSegment", runtimeSpawnCommand);
        StringAssert.Contains("context.RuntimeSpawnSystem.TryGetRuntimeWallSegmentFootprint", runtimeSpawnCommand);
        StringAssert.Contains("context.RuntimeSpawnSystem.TryGetRuntimeBuildingPlacementFootprint", runtimeSpawnCommand);
        StringAssert.Contains("context.RuntimeSpawnSystem.TrySpawnInitialBuilding", runtimeSpawnCommand);
        StringAssert.Contains("context.RuntimeSpawnSystem.TryResolveInitialPlacementOrigin", runtimeSpawnCommand);

        StringAssert.Contains("TrySpawnRuntimeBuilding", runtimeSpawn);
        StringAssert.Contains("TrySpawnRuntimeWallRun", runtimeSpawn);
        StringAssert.Contains("TrySpawnRuntimeWallSegment", runtimeSpawn);
        StringAssert.Contains("TryGetRuntimeWallSegmentFootprint", runtimeSpawn);
        StringAssert.Contains("TryGetRuntimeBuildingPlacementFootprint", runtimeSpawn);
        StringAssert.Contains("TryFindValidInitialBuildingOrigin", runtimeSpawn);
        StringAssert.Contains("TryResolveInitialPlacementOrigin", runtimeSpawn);
        StringAssert.Contains("CloneDefinitionWithFootprint", runtimeSpawn);
        StringAssert.Contains("BuildWallRunOrigins", runtimeSpawn);
        StringAssert.Contains("IsWallPlacementValid", runtimeSpawn);
        StringAssert.Contains("CreateRuntimeBuildingDefinition", runtimeSpawn);

        Assert.IsFalse(
            Regex.IsMatch(placement, @"BuildWallRunOrigins\s*\("),
            "Runtime wall-run origin construction belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"IsWallPlacementValid\s*\("),
            "Runtime wall spawn validation orchestration belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"CreateRuntimeBuildingDefinition\s*\("),
            "Runtime spawn definition creation orchestration belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bTryFindValidInitialBuildingOrigin\b"),
            "Initial building origin search belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"for\s*\(\s*int\s+radius\s*=\s*1\s*;\s*radius\s*<=\s*maxRadius"),
            "Active placement initial origin radius search belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"for\s*\(\s*int\s+y\s*=\s*0\s*;\s*y\s*<=\s*Mathf\.Max\(0,\s*grid\.Height\s*-\s*footprint\.y\)"),
            "Active placement full-grid origin fallback belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+Vector2Int\s*\(\s*4\s*,\s*1\s*\)"),
            "Runtime wall fallback footprint policy belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+Vector2Int\s*\(\s*10\s*,\s*10\s*\)"),
            "Runtime building fallback footprint policy belongs in BuildingRuntimeSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingRuntimeSpawnSystem.Context", StringComparison.Ordinal),
            "Runtime spawn context construction belongs in BuildingRuntimeContextSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_buildingRuntimeSpawnSystem\.(?:SpawnInitialTestRoster|TrySpawnRuntimeBuilding|TrySpawnRuntimeWallRun|TrySpawnRuntimeWallSegment|TryGetRuntimeWallSegmentFootprint|TryGetRuntimeBuildingPlacementFootprint|TrySpawnInitialBuilding|TryResolveInitialPlacementOrigin)\s*\("),
            "Runtime/manual spawn command translation belongs in BuildingRuntimeSpawnCommandSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateRuntimeOwnershipSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string runtimeOwnershipFile = "Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs";
        const string runtimeContextFile = "Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs";
        Assert.IsTrue(File.Exists(runtimeOwnershipFile), "Runtime owner-faction assignment must live in BuildingRuntimeOwnershipSystem.");
        Assert.IsTrue(File.Exists(runtimeContextFile), "Runtime ownership context construction must live in BuildingRuntimeContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeOwnership = File.ReadAllText(runtimeOwnershipFile);
        string runtimeContext = File.ReadAllText(runtimeContextFile);
        StringAssert.Contains("BuildingRuntimeOwnershipSystem _buildingRuntimeOwnershipSystem", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateOwnershipContext(CreateBuildingRuntimeContextSource())", placement);
        StringAssert.Contains("new BuildingRuntimeOwnershipSystem.Context", runtimeContext);
        StringAssert.Contains("_buildingRuntimeOwnershipSystem.SetRuntimeBuildingOwnerFaction", placement);
        StringAssert.Contains("SetRuntimeBuildingOwnerFaction", runtimeOwnership);
        StringAssert.Contains("UpdateRuntimeGateFriendlyPassFaction", runtimeOwnership);
        StringAssert.Contains("FriendlyPassGridBlocker", runtimeOwnership);
        StringAssert.Contains("em.SetComponentData(building.CombatEntity, new Faction", runtimeOwnership);
        StringAssert.Contains("ApplyMarkerColor", runtimeOwnership);
        StringAssert.Contains("ResolveFactionColor", runtimeOwnership);

        Assert.IsFalse(
            Regex.IsMatch(placement, @"building\.HasOwnerFaction\s*=\s*ownerFactionId\.HasValue"),
            "Runtime owner-faction assignment belongs in BuildingRuntimeOwnershipSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"building\.OwnerFactionId\s*=\s*ownerFactionId\.GetValueOrDefault"),
            "Runtime owner-faction assignment belongs in BuildingRuntimeOwnershipSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("FriendlyPassGridBlocker", StringComparison.Ordinal),
            "Gate friendly-pass blocker updates belong in BuildingRuntimeOwnershipSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"em\.SetComponentData\s*\(\s*building\.CombatEntity\s*,\s*new\s+Faction"),
            "Runtime combat Faction projection belongs in BuildingRuntimeOwnershipSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"GetColor\s*\(\s*building\.OwnerFactionId\s*\)"),
            "Runtime owner marker color projection belongs in BuildingRuntimeOwnershipSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingRuntimeOwnershipSystem.Context", StringComparison.Ordinal),
            "Runtime ownership context construction belongs in BuildingRuntimeContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedCombatSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string combatFile = "Assets/Game/Scripts/Systems/BuildingCombatSystem.cs";
        const string runtimeContextFile = "Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs";
        const string buildingCompositionFile = "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs";
        Assert.IsTrue(File.Exists(combatFile), "The building combat slice must live in BuildingCombatSystem.");
        Assert.IsTrue(File.Exists(runtimeContextFile), "Building combat context construction must live in BuildingRuntimeContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string combat = File.ReadAllText(combatFile);
        string runtimeContext = File.ReadAllText(runtimeContextFile);
        string buildingComposition = File.ReadAllText(buildingCompositionFile);
        StringAssert.Contains("BuildingCombatSystem _buildingCombatSystem", placement);
        StringAssert.Contains("_buildingCombatSystem.DeleteBuilding", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateCombatContext", placement);
        StringAssert.Contains("new BuildingCombatSystem.Context<RuntimeBuildingData>", runtimeContext);
        StringAssert.Contains("_buildingCombatSystem.HandleRuntimeBuildingEntityDestroyed", placement);
        StringAssert.Contains("tickDomains.Combat.UpdateDestroyedBuildings", buildingComposition);
        StringAssert.Contains("tickDomains.Combat.SyncDestroyedRuntimeBuildingCombatEntities", buildingComposition);

        StringAssert.Contains("TryMarkDestroyed", combat);
        StringAssert.Contains("CollectDestroyedCleanupIds", combat);
        StringAssert.Contains("ResolveRuntimeCombatState", combat);
        StringAssert.Contains("DestroyBlockerEntity", combat);
        StringAssert.Contains("DeleteBuilding", combat);
        StringAssert.Contains("HandleRuntimeBuildingEntityDestroyed", combat);
        StringAssert.Contains("UpdateDestroyedBuildings", combat);
        StringAssert.Contains("SyncDestroyedRuntimeBuildingCombatEntities", combat);
        StringAssert.Contains("BeginDestroyedBuildingState", combat);
        StringAssert.Contains("FinalizeDestroyedBuilding", combat);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bbuilding\.IsDestroyed\s*=\s*true\b"),
            "Destroyed state mutation belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bbuilding\.DestroyedCleanupAt\s*="),
            "Destroyed cleanup timing belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+DeleteBuilding\b"),
            "Building deletion orchestration belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+BeginDestroyedBuildingState\b"),
            "Destroyed visual/state orchestration belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+FinalizeDestroyedBuilding\b"),
            "Destroyed cleanup finalization belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+DestroyRuntimeBuildingBlockerEntity\b"),
            "Runtime blocker cleanup belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\b(?:internal|private)\s+void\s+UpdateDestroyedBuildings\s*\("),
            "Destroyed-building runtime ticks must be wired to BuildingCombatSystem from composition, not wrapped by BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\b(?:internal|private)\s+void\s+SyncDestroyedRuntimeBuildingCombatEntities\s*\("),
            "Destroyed combat sync ticks must be wired to BuildingCombatSystem from composition, not wrapped by BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedResourceSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string resourceFile = "Assets/Game/Scripts/Systems/FactionResourceSystem.cs";
        const string productionRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs";
        Assert.IsTrue(File.Exists(resourceFile), "The faction resource slice must live in FactionResourceSystem.");
        Assert.IsTrue(File.Exists(productionRuntimeTickFile), "Resource production runtime ticking must live in BuildingProductionRuntimeTickSystem.");

        string placement = File.ReadAllText(placementFile);
        string resource = File.ReadAllText(resourceFile);
        string productionRuntimeTick = File.ReadAllText(productionRuntimeTickFile);
        StringAssert.Contains("FactionResourceSystem _factionResourceSystem", placement);
        StringAssert.Contains("GetResourceTotals", resource);
        StringAssert.Contains("context.FactionResourceSystem.UpdateResourceProduction", productionRuntimeTick);
        StringAssert.Contains("TryGetPrimaryCapacityInfo", resource);
        StringAssert.Contains("TryGetFuelCapacityInfo", resource);
        StringAssert.Contains("TryGetFactionResourceEconomy", resource);
        StringAssert.Contains("DrainFactionResource", resource);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+bool\s+TryGetFactionResourceEconomy") ||
            Regex.IsMatch(placement, @"public\s+void\s+SellFactionResources"),
            "Faction resource economy and sell compatibility wrappers must not remain on BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsResourceStorageBuilding\b"),
            "Resource storage classification belongs in FactionResourceSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsFactionResourceBuilding\b"),
            "Faction resource classification belongs in FactionResourceSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?float\s+DrainFactionResource\b"),
            "Faction resource drain behavior belongs in FactionResourceSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?int\s+GetDisplayedOilCapacity\b"),
            "Resource capacity display math belongs in FactionResourceSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bbuilding\.StoredOilBarrels\s*=\s*Mathf\.Min\(capacity"),
            "Oil production mutation belongs in FactionResourceSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bbuilding\.StoredFuelBarrels\s*=\s*Mathf\.Min\(fuelCapacity"),
            "Fuel production mutation belongs in FactionResourceSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedHaulerSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string haulerFile = "Assets/Game/Scripts/Systems/ResourceHaulerSystem.cs";
        const string haulerBridgeFile = "Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs";
        const string productionContextFile = "Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs";
        const string productionRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs";
        Assert.IsTrue(File.Exists(haulerFile), "The resource hauler slice must live in ResourceHaulerSystem.");
        Assert.IsTrue(File.Exists(haulerBridgeFile), "The resource hauler building bridge must live in BuildingResourceHaulerBridgeSystem.");
        Assert.IsTrue(File.Exists(productionContextFile), "Resource hauler bridge context construction must live in BuildingProductionContextSystem.");
        Assert.IsTrue(File.Exists(productionRuntimeTickFile), "Resource hauler runtime ticking must live in BuildingProductionRuntimeTickSystem.");

        string placement = File.ReadAllText(placementFile);
        string haulerBridge = File.ReadAllText(haulerBridgeFile);
        string productionContext = File.ReadAllText(productionContextFile);
        string productionRuntimeTick = File.ReadAllText(productionRuntimeTickFile);
        StringAssert.Contains("ResourceHaulerSystem _resourceHaulerSystem", placement);
        StringAssert.Contains("BuildingResourceHaulerBridgeSystem _buildingResourceHaulerBridgeSystem", placement);
        StringAssert.Contains("_buildingProductionContextSystem.CreateResourceHaulerBridgeContext(CreateBuildingProductionContextSource())", placement);
        StringAssert.Contains("new BuildingResourceHaulerBridgeSystem.Context", productionContext);
        StringAssert.Contains("context.ResourceHaulerBridgeSystem?.UpdateResourceHaulers", productionRuntimeTick);
        StringAssert.Contains("_buildingResourceHaulerBridgeSystem.TryAssignSelectedHaulerOrders", placement);
        StringAssert.Contains("_buildingResourceHaulerBridgeSystem.TryGetRuntimeBuildingApproachCell", placement);
        StringAssert.Contains("_buildingResourceHaulerBridgeSystem.IsRuntimeBuildingApproachCell", placement);

        StringAssert.Contains("context.ResourceHaulerSystem.IsOilSourceBuilding", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.IsFuelBuilding", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.HasAvailableFuelForHauler", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.CreateOrder", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.SetTravelPhase", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.SetPhase", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.AdvanceTimedAction", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.ResetActionTimer", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.GetLoadAmount", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.GetCargo", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.TryCompleteLoad", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.RevertLoad", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.HasReceivingCapacity", haulerBridge);
        StringAssert.Contains("context.ResourceHaulerSystem.TryCompleteUnload", haulerBridge);
        StringAssert.Contains("TryIssueHaulerMoveToBuilding", haulerBridge);
        StringAssert.Contains("TryFindNearestBuilding", haulerBridge);
        StringAssert.Contains("TryFindBuildingApproachCell", haulerBridge);
        StringAssert.Contains("HasGoalOrPathRequest", haulerBridge);
        StringAssert.Contains("IsHaulerAtBuildingApproach", haulerBridge);
        StringAssert.Contains("AddComponent<ManualMoveOrderTag>", haulerBridge);
        StringAssert.Contains("UnitPathRequest", haulerBridge);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsOilSourceBuilding\b"),
            "Hauler oil source classification belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingResourceHaulerBridgeSystem.Context", StringComparison.Ordinal),
            "Resource hauler bridge context construction belongs in BuildingProductionContextSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsFuelBuilding\b"),
            "Hauler fuel destination classification belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+HasAvailableFuelForHauler\b"),
            "Hauler fuel source classification belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?float\s+GetOilReceivingFreeCapacity\b"),
            "Hauler oil receiving capacity belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?float\s+GetFuelReceivingFreeCapacity\b"),
            "Hauler fuel receiving capacity belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+UnitResourceHaulOrder\b"),
            "Hauler order construction belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\border\.Phase\s*="),
            "Hauler phase mutation belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\border\.ActionEndsAt\s*="),
            "Hauler action timer mutation belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryIssueHaulerMoveToBuilding\b"),
            "Hauler move-order/path request bridging belongs in BuildingResourceHaulerBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryFindNearestBuilding\b"),
            "Hauler nearest-building lookup belongs in BuildingResourceHaulerBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+static\s+bool\s+HasGoalOrPathRequest\b"),
            "Hauler path-request checks belong in BuildingResourceHaulerBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+static\s+bool\s+TryFindBuildingApproachCell\b"),
            "Hauler building approach search belongs in BuildingResourceHaulerBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+static\s+void\s+TryScoreBuildingApproachCandidate\b"),
            "Hauler building approach scoring belongs in BuildingResourceHaulerBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+static\s+int\s+AxisDistance\b"),
            "Hauler building approach distance math belongs in BuildingResourceHaulerBridgeSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedProductionSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string productionFile = "Assets/Game/Scripts/Systems/BuildingProductionSystem.cs";
        const string productionUpdateFile = "Assets/Game/Scripts/Systems/BuildingProductionUpdateSystem.cs";
        const string productionRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs";
        const string productionSlotFile = "Assets/Game/Scripts/Systems/BuildingProductionSlotSystem.cs";
        const string productionContextFile = "Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs";
        const string productionTransportFile = "Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs";
        const string productionTransportBridgeFile = "Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeSystem.cs";
        const string spawnFile = "Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs";
        const string spawnCellFile = "Assets/Game/Scripts/Systems/BuildingSpawnCellSystem.cs";
        const string spawnPrefabFile = "Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs";
        const string runwayFile = "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs";
        const string runtimeQueryFile = "Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs";
        Assert.IsTrue(File.Exists(productionFile), "The building production slice must live in BuildingProductionSystem.");
        Assert.IsTrue(File.Exists(productionUpdateFile), "The pending production runtime update loop must live in BuildingProductionUpdateSystem.");
        Assert.IsTrue(File.Exists(productionRuntimeTickFile), "The production/resource runtime tick orchestration must live in BuildingProductionRuntimeTickSystem.");
        Assert.IsTrue(File.Exists(productionSlotFile), "The production slot slice must live in BuildingProductionSlotSystem.");
        Assert.IsTrue(File.Exists(productionContextFile), "Production context construction must live in BuildingProductionContextSystem.");
        Assert.IsTrue(File.Exists(productionTransportFile), "The active production transport visual/update slice must live in BuildingProductionTransportSystem.");
        Assert.IsTrue(File.Exists(productionTransportBridgeFile), "The production transport ECS bridge must live in BuildingProductionTransportBridgeSystem.");
        Assert.IsTrue(File.Exists(spawnFile), "The produced-unit spawn slice must live in BuildingSpawnSystem.");
        Assert.IsTrue(File.Exists(spawnCellFile), "The spawn-cell perimeter helper slice must live in BuildingSpawnCellSystem.");
        Assert.IsTrue(File.Exists(spawnPrefabFile), "The spawn prefab/entity resolution slice must live in BuildingSpawnPrefabSystem.");
        Assert.IsTrue(File.Exists(runwayFile), "The runway slice must live in BuildingRunwaySystem.");
        Assert.IsTrue(File.Exists(runtimeQueryFile), "Produced-unit count read models must delegate pruning through BuildingRuntimeQuerySystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeQuery = File.ReadAllText(runtimeQueryFile);
        string productionContext = File.ReadAllText(productionContextFile);
        string productionRuntimeTick = File.ReadAllText(productionRuntimeTickFile);
        StringAssert.Contains("BuildingProductionSystem _buildingProductionSystem", placement);
        StringAssert.Contains("BuildingProductionUpdateSystem _buildingProductionUpdateSystem", placement);
        StringAssert.Contains("BuildingProductionSlotSystem _buildingProductionSlotSystem", placement);
        StringAssert.Contains("BuildingProductionTransportSystem _buildingProductionTransportSystem", placement);
        StringAssert.Contains("BuildingProductionTransportBridgeSystem _buildingProductionTransportBridgeSystem", placement);
        StringAssert.Contains("BuildingProductionContextSystem _buildingProductionContextSystem", placement);
        StringAssert.Contains("CreateBuildingProductionContextSource", placement);
        StringAssert.Contains("new BuildingProductionUpdateSystem.Context", productionContext);
        StringAssert.Contains("new BuildingProductionTransportSystem.Context", productionContext);
        StringAssert.Contains("new BuildingProductionTransportBridgeSystem.Context", productionContext);
        StringAssert.Contains("new BuildingProductionRequestSystem.Context", productionContext);
        StringAssert.Contains("new BuildingProductionSystem.QueueContext", productionContext);
        StringAssert.Contains("new BuildingResourceHaulerBridgeSystem.Context", productionContext);
        StringAssert.Contains("BuildingSpawnSystem _buildingSpawnSystem", placement);
        StringAssert.Contains("BuildingSpawnPrefabSystem _buildingSpawnPrefabSystem", placement);
        StringAssert.Contains("BuildingRunwaySystem _buildingRunwaySystem", placement);
        StringAssert.Contains("_buildingProductionSystem.TryQueuePlayerUnitFromBuilding", placement);
        StringAssert.Contains("context.ProductionUpdateSystem.UpdatePendingProductions", productionRuntimeTick);
        StringAssert.Contains("context.ProductionSystem?.PruneProducedUnits", runtimeQuery);

        string production = File.ReadAllText(productionFile);
        StringAssert.Contains("TryQueuePlayerUnitFromBuilding", production);
        StringAssert.Contains("building.PendingProductions.Add", production);
        StringAssert.Contains("context.ProductionSlotSystem?.TryReserveProductionSlot", production);
        StringAssert.Contains("ProductionTransportSettings", production);
        StringAssert.Contains("ResolveProductionTransportSettings", production);
        StringAssert.Contains("ResolveProductionDurationSeconds", production);
        StringAssert.Contains("TryResolveDefaultProductionTransportPrefab", production);

        string productionUpdate = File.ReadAllText(productionUpdateFile);
        StringAssert.Contains("UpdatePendingProductions", productionUpdate);
        StringAssert.Contains("context.TransportSystem.UpdateActiveProductionTransport", productionUpdate);
        StringAssert.Contains("context.TransportSystem.TryEnsureActiveProductionTransport", productionUpdate);
        StringAssert.Contains("context.ProductionSystem.GetProgress", productionUpdate);
        StringAssert.Contains("context.ProductionSystem.ShouldLaunchTransport", productionUpdate);
        StringAssert.Contains("context.ProductionSystem.DelayPendingProduction", productionUpdate);
        StringAssert.Contains("context.ProductionSystem.IsReady", productionUpdate);
        StringAssert.Contains("context.ProductionSystem.IsReadyWithin", productionUpdate);
        StringAssert.Contains("context.ProductionSystem.RemovePendingAt", productionUpdate);

        string productionSlot = File.ReadAllText(productionSlotFile);
        StringAssert.Contains("TryReserveProductionSlot", productionSlot);
        StringAssert.Contains("TryGetAvailableProductionSpawnSlot", productionSlot);
        StringAssert.Contains("IsProductionSlotReservedByPending", productionSlot);
        StringAssert.Contains("IsProductionSlotOccupied", productionSlot);

        string productionTransport = File.ReadAllText(productionTransportFile);
        StringAssert.Contains("TryEnsureActiveProductionTransport", productionTransport);
        StringAssert.Contains("UpdateActiveProductionTransport", productionTransport);
        StringAssert.Contains("StartActiveTransportDrop", productionTransport);
        StringAssert.Contains("UpdateActiveTransportDrop", productionTransport);
        StringAssert.Contains("BuildingProductionTransportBridgeSystem TransportBridgeSystem", productionTransport);
        StringAssert.Contains("context.TransportBridgeSystem.ResolveProductionGroundGoalCell", productionTransport);
        StringAssert.Contains("context.TransportBridgeSystem?.MoveNewestProducedUnitToCell", productionTransport);
        StringAssert.Contains("context.TransportBridgeSystem?.AlignNewestProducedUnitRotation", productionTransport);
        StringAssert.Contains("context.TransportBridgeSystem.TrySpawnPlayerUnitNearBuilding", productionTransport);
        StringAssert.Contains("context.ProductionSystem.FindNextReadyTransportPending", productionTransport);
        StringAssert.Contains("context.ProductionSystem.FindNextSoonTransportPending", productionTransport);
        StringAssert.Contains("context.ProductionSystem.RemovePendingProduction", productionTransport);

        string productionTransportBridge = File.ReadAllText(productionTransportBridgeFile);
        StringAssert.Contains("ResolveProductionGroundGoalCell", productionTransportBridge);
        StringAssert.Contains("MoveNewestProducedUnitToCell", productionTransportBridge);
        StringAssert.Contains("AlignNewestProducedUnitRotation", productionTransportBridge);
        StringAssert.Contains("TrySpawnPlayerUnitNearBuilding", productionTransportBridge);
        StringAssert.Contains("GridUtils.WorldToCell", productionTransportBridge);
        StringAssert.Contains("UnitPathRequest", productionTransportBridge);
        StringAssert.Contains("quaternion.LookRotationSafe", productionTransportBridge);
        StringAssert.Contains("context.SpawnSystem.TrySpawnPlayerUnitNearBuilding", productionTransportBridge);

        string spawn = File.ReadAllText(spawnFile);
        StringAssert.Contains("TrySpawnPlayerUnitNearBuilding", spawn);
        StringAssert.Contains("TryResolveAvailableFactionHelipadSpawn", spawn);
        StringAssert.Contains("TryFindStrictSpawnCell", spawn);
        StringAssert.Contains("ReserveDynamicOccupancy", spawn);

        string spawnCell = File.ReadAllText(spawnCellFile);
        StringAssert.Contains("FindSpawnCellAdjacentToBuilding", spawnCell);
        StringAssert.Contains("TryReservePerimeterCell", spawnCell);
        StringAssert.Contains("TryAddPerimeterCandidate", spawnCell);
        StringAssert.Contains("SpawnCellUtility.FindSpawnCellNear", spawnCell);

        string spawnPrefab = File.ReadAllText(spawnPrefabFile);
        StringAssert.Contains("TryResolveSpawnUnitPrefabFromRegistry", spawnPrefab);
        StringAssert.Contains("TryGetSpawnUnitPrefabEntity", spawnPrefab);
        StringAssert.Contains("TryGetPlayerUnitPrefabEntityFromLiveUnits", spawnPrefab);

        string runway = File.ReadAllText(runwayFile);
        StringAssert.Contains("TryGetNearestAirportRunway", runway);
        StringAssert.Contains("TryGetRunwayLocalData", runway);
        StringAssert.Contains("TryGetRunwayFootprintRect", runway);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+RuntimeBuildingData\.PendingProduction\s*\{"),
            "Pending production initialization belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryQueuePlayerUnitFromBuilding\b"),
            "Player production queue mutation belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingProductionUpdateSystem.Context", StringComparison.Ordinal) ||
            placement.Contains("new BuildingProductionTransportSystem.Context", StringComparison.Ordinal) ||
            placement.Contains("new BuildingProductionTransportBridgeSystem.Context", StringComparison.Ordinal) ||
            placement.Contains("new BuildingProductionRequestSystem.Context", StringComparison.Ordinal) ||
            placement.Contains("new BuildingProductionSystem.QueueContext", StringComparison.Ordinal),
            "Production context construction belongs in BuildingProductionContextSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"PendingProductions\.Add"),
            "Pending production queue append belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"pending\.StartedAt\s*\+="),
            "Pending production delay mutation belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"pending\.ReadyAt\s*\+="),
            "Pending production delay mutation belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_buildingProductionSystem\.(?:GetProgress|ShouldLaunchTransport|DelayPendingProduction|IsReady|IsReadyWithin|RemovePendingAt)\b"),
            "Pending production update orchestration belongs in BuildingProductionUpdateSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"Mathf\.Clamp01\(\(now\s*-\s*pending\.StartedAt\)\s*/"),
            "Pending production progress math belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"bool\s+reservedByPending\s*="),
            "Production slot pending-reservation checks belong in BuildingProductionSlotSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"bool\s+alive\s*=\s*unit\s*!="),
            "Produced-unit liveness pruning belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+RuntimeBuildingData\.PendingProduction\s+FindNextReadyTransportPending\b"),
            "Ready transport-pending lookup belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+RuntimeBuildingData\.PendingProduction\s+FindNextSoonTransportPending\b"),
            "Soon transport-pending lookup belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"PendingProductions\.IndexOf"),
            "Pending production lookup/removal belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"PendingProductions\.RemoveAt"),
            "Pending production removal belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?float\s+ResolveProductionDurationSeconds\b"),
            "Production duration policy belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+ResolveProductionTransportSettings\b"),
            "Production transport settings policy belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?GameObject\s+TryResolveDefaultProductionTransportPrefab\b"),
            "Default production transport fallback policy belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsLikelyGroundVehiclePrefab\b"),
            "Production transport vehicle classification belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?Vector2Int\s+ResolveEffectiveProductionFootprintCells\b"),
            "Production footprint policy for transport fallback belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsHelicopterUnitPrefab\b"),
            "Production helicopter classification belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryEnsureActiveProductionTransport\b"),
            "Active production transport creation belongs in BuildingProductionTransportSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+UpdateActiveProductionTransport\b"),
            "Active production transport updates belong in BuildingProductionTransportSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+StartActiveTransportDrop\b"),
            "Production transport drop visual setup belongs in BuildingProductionTransportSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+UpdateActiveTransportDrop\b"),
            "Production transport drop visual updates belong in BuildingProductionTransportSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryAcquireProductionTransportLane\b"),
            "Production transport lane reservation belongs in BuildingProductionTransportSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"GridUtils\.WorldToCell\(grid,\s*worldPosition\)"),
            "Production transport ground-cell conversion belongs in BuildingProductionTransportBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+UnitPathRequest\s*\{\s*Goal\s*=\s*goalCell\s*\}"),
            "Produced-unit transport move orders belong in BuildingProductionTransportBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"quaternion\.LookRotationSafe\(\(float3\)forward,\s*math\.up\(\)\)"),
            "Produced-unit transport rotation alignment belongs in BuildingProductionTransportBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_buildingSpawnSystem\.TrySpawnPlayerUnitNearBuilding\("),
            "Production transport spawn bridging belongs in BuildingProductionTransportBridgeSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"BuildingProductionTransportSystem\.(?:TrySpawnPlayerUnitNearBuildingDelegate|ResolveProductionGroundGoalCellDelegate|BuildingCellAction|BuildingForwardAction)"),
            "BuildingPlacementSystem must not keep production transport wrapper delegates.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:int2|void|bool)\s+(?:ResolveProductionGroundGoalCell|MoveNewestProducedUnitToCell|AlignNewestProducedUnitRotation|TrySpawnPlayerUnitNearBuilding)\b"),
            "Production transport wrapper methods belong in BuildingProductionTransportSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryResolveHelicopterSpawnForFaction\b"),
            "Helipad spawn fallback belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryFindStrictSpawnCell\b"),
            "Strict spawn-cell search belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryFindStrictSpawnCellAdjacentToBuilding\b"),
            "Adjacent spawn-cell search belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?int2\s+FindSpawnCellAdjacentToBuilding\b"),
            "Spawn-cell perimeter search belongs in BuildingSpawnCellSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryReservePerimeterCell\b"),
            "Spawn-cell perimeter reservation belongs in BuildingSpawnCellSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+TryAddPerimeterCandidate\b"),
            "Spawn-cell perimeter candidate filtering belongs in BuildingSpawnCellSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+ReserveDynamicOccupancy\b"),
            "Produced-unit dynamic occupancy reservation belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+ReserveRecentSpawnBuffers\b"),
            "Recent spawn reservation buffering belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryGetSpawnUnitPrefabEntity\b"),
            "Spawn prefab entity resolution belongs in BuildingSpawnPrefabSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryResolveSpawnUnitPrefabFromRegistry\b"),
            "Spawn prefab registry lookup belongs in BuildingSpawnPrefabSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryGetAvailableProductionSpawnSlot\b"),
            "Production spawn slot discovery belongs in BuildingProductionSlotSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryGetNearestAirportRunway\b"),
            "Nearest airport runway lookup belongs in BuildingRunwaySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryGetRunwayLocalData\b"),
            "Runway prefab metadata discovery belongs in BuildingRunwaySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryGetRunwayFootprintRect\b"),
            "Runway placement footprint expansion belongs in BuildingRunwaySystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedProductionRequestSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string requestFile = "Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs";
        Assert.IsTrue(File.Exists(requestFile), "The building production request slice must live in BuildingProductionRequestSystem.");

        string placement = File.ReadAllText(placementFile);
        string request = File.ReadAllText(requestFile);
        StringAssert.Contains("BuildingProductionRequestSystem _buildingProductionRequestSystem", placement);
        StringAssert.Contains("_buildingProductionRequestSystem.CreateUnitFromBuilding", placement);
        StringAssert.Contains("_buildingProductionRequestSystem.TryRequestCampItem", placement);
        StringAssert.Contains("_buildingProductionRequestSystem.GetCampRequestFailure", placement);
        StringAssert.Contains("_buildingProductionRequestSystem.FocusLastCampProductionRequest", placement);
        StringAssert.Contains("_buildingProductionRequestSystem.ArmNextProductionFromUi", placement);
        StringAssert.Contains("_buildingProductionRequestSystem.CanCreateUnitFromSelectedBuilding", placement);
        StringAssert.Contains("_buildingProductionRequestSystem.CanQueueUnitFromBuilding", placement);

        StringAssert.Contains("TryRequestCampItem", request);
        StringAssert.Contains("GetCampRequestFailure", request);
        StringAssert.Contains("TryQueueFactionUnitProduction", request);
        StringAssert.Contains("TryFindFirstFactionProducerBuilding", request);
        StringAssert.Contains("TryFindFirstFriendlyProducerBuilding", request);
        StringAssert.Contains("TryGetRequiredProducerDisplayName", request);
        StringAssert.Contains("SelectBuildingForProductionRequest", request);
        StringAssert.Contains("RememberCampProductionFocus", request);
        StringAssert.Contains("ResolveProductionRequestFocusWorldPosition", request);
        StringAssert.Contains("ConsumeUiProductionArm", request);

        Assert.IsFalse(
            placement.Contains("_armedProductionFrame", StringComparison.Ordinal),
            "UI production arm state belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("_lastCampProductionFocusBuilding", StringComparison.Ordinal) ||
            placement.Contains("_lastCampProductionFocusPrefab", StringComparison.Ordinal),
            "Last camp production focus memory belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryFindFirstFriendlyProducerBuilding\b"),
            "Friendly producer lookup belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryFindFirstFactionProducerBuilding\b"),
            "Faction producer lookup belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\b(?:public|internal)\s+bool\s+TryQueueFactionUnitProduction\b"),
            "Faction production compatibility wrappers must not remain on BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryGetRequiredProducerDisplayName\b"),
            "Required producer display lookup belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+SelectBuildingForProductionRequest\b"),
            "Production request focus belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+RememberCampProductionFocus\b"),
            "Deferred camp production focus memory belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+Vector3\s+ResolveProductionRequestFocusWorldPosition\b"),
            "Production request focus position policy belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+ConsumeUiProductionArm\b"),
            "UI production arm consumption belongs in BuildingProductionRequestSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedPreviewSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string previewFile = "Assets/Game/Scripts/Systems/BuildingPlacementPreviewSystem.cs";
        const string startupFile = "Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs";
        Assert.IsTrue(File.Exists(previewFile), "The placement preview slice must live in BuildingPlacementPreviewSystem.");

        string placement = File.ReadAllText(placementFile);
        string preview = File.ReadAllText(previewFile);
        string startup = File.ReadAllText(startupFile);
        StringAssert.Contains("BuildingPlacementPreviewSystem _buildingPlacementPreviewSystem", placement);
        StringAssert.Contains("previewSystem.Init", startup);
        StringAssert.Contains("_buildingPlacementPreviewSystem.UpdateOutline", placement);
        StringAssert.Contains("_buildingPlacementPreviewSystem.UpdateWallOutline", placement);
        StringAssert.Contains("_buildingPlacementPreviewSystem.RebuildWallPreview", placement);

        StringAssert.Contains("RebuildWallPreview", preview);
        StringAssert.Contains("UpdateWallOutline", preview);
        StringAssert.Contains("SetPreviewSegmentValid", preview);
        StringAssert.Contains("CreatePlacementMaterial", preview);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+CreatePlacementOutline\b"),
            "Placement outline object lifetime belongs in BuildingPlacementPreviewSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?Material\s+CreatePlacementMaterial\b"),
            "Placement outline material setup belongs in BuildingPlacementPreviewSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+UpdateWallPlacementOutline\b"),
            "Wall preview outline bounds belong in BuildingPlacementPreviewSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+SetPreviewSegmentValid\b"),
            "Preview segment validity tinting belongs in BuildingPlacementPreviewSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedCommitSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string commitFile = "Assets/Game/Scripts/Systems/BuildingPlacementCommitSystem.cs";
        const string placementContextFile = "Assets/Game/Scripts/Systems/BuildingPlacementContextSystem.cs";
        Assert.IsTrue(File.Exists(commitFile), "The placement commit slice must live in BuildingPlacementCommitSystem.");
        Assert.IsTrue(File.Exists(placementContextFile), "Placement commit context construction must live in BuildingPlacementContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string commit = File.ReadAllText(commitFile);
        string placementContext = File.ReadAllText(placementContextFile);
        StringAssert.Contains("BuildingPlacementCommitSystem _buildingPlacementCommitSystem", placement);
        StringAssert.Contains("_buildingPlacementCommitSystem.CommitPlacement", placement);
        StringAssert.Contains("_buildingPlacementContextSystem.CreateCommitRequest", placement);
        StringAssert.Contains("_buildingPlacementContextSystem.CreateCommitContext", placement);
        StringAssert.Contains("new BuildingPlacementCommitSystem.CommitRequest", placementContext);
        StringAssert.Contains("new BuildingPlacementCommitSystem.CommitContext", placementContext);

        StringAssert.Contains("CommitPlacement", commit);
        StringAssert.Contains("CommitWallPlacement", commit);
        StringAssert.Contains("CommitSinglePlacement", commit);
        StringAssert.Contains("BuildFinalWallRuns", commit);
        StringAssert.Contains("BuildWallRunOrigins", commit);
        StringAssert.Contains("GetWallSegmentFootprint", commit);
        StringAssert.Contains("ResolvePlacementWorldRotation", commit);
        StringAssert.Contains("ShouldAutoSelectAfterPlacement", commit);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?List<Vector2Int>\s+BuildWallRunOrigins\b"),
            "Wall-run origin construction belongs in BuildingPlacementCommitSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?Vector2Int\s+GetWallSegmentFootprint\b"),
            "Wall segment footprint helpers belong in BuildingPlacementCommitSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?Quaternion\s+ResolvePlacementWorldRotation\b"),
            "Wall placement rotation helpers belong in BuildingPlacementCommitSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+ShouldAutoSelectAfterPlacement\b"),
            "Post-placement auto-select policy belongs in BuildingPlacementCommitSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"RuntimeBuildingData\s+lastBuilding\s*="),
            "Wall placement commit expansion belongs in BuildingPlacementCommitSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"Destroy\s*\(\s*placement\.PreviewInstance\s*\)"),
            "Committed placement preview consumption belongs in BuildingPlacementCommitSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingPlacementCommitSystem.CommitRequest", StringComparison.Ordinal) ||
            placement.Contains("new BuildingPlacementCommitSystem.CommitContext", StringComparison.Ordinal),
            "Placement commit request/context construction belongs in BuildingPlacementContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedInputSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string inputFile = "Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs";
        const string inputRuntimeTickFile = "Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs";
        const string placementContextFile = "Assets/Game/Scripts/Systems/BuildingPlacementContextSystem.cs";
        Assert.IsTrue(File.Exists(inputFile), "The placement input slice must live in BuildingPlacementInputSystem.");
        Assert.IsTrue(File.Exists(inputRuntimeTickFile), "Placement pointer frame orchestration must live in BuildingPlacementInputRuntimeTickSystem.");
        Assert.IsTrue(File.Exists(placementContextFile), "Placement input context construction must live in BuildingPlacementContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string input = File.ReadAllText(inputFile);
        string inputRuntimeTick = File.ReadAllText(inputRuntimeTickFile);
        string placementContext = File.ReadAllText(placementContextFile);
        StringAssert.Contains("BuildingPlacementInputSystem _buildingPlacementInputSystem", placement);
        StringAssert.Contains("PlacementInputSystem?.UpdateActivePlacementPointer", inputRuntimeTick);
        StringAssert.Contains("_buildingPlacementInputSystem.ApplyPointerHover", placement);
        StringAssert.Contains("_buildingPlacementInputSystem.BuildWallPlacementOrigins", placement);
        StringAssert.Contains("_buildingPlacementContextSystem.CreateActivePlacementPointerContext", placement);
        StringAssert.Contains("new BuildingPlacementInputSystem.ActivePlacementPointerContext", placementContext);

        StringAssert.Contains("UpdateActivePlacementPointer", input);
        StringAssert.Contains("ActivePlacementPointerContext", input);
        StringAssert.Contains("TryBeginDrag", input);
        StringAssert.Contains("ApplyPointerHover", input);
        StringAssert.Contains("IsPointerOverPlacement", input);
        StringAssert.Contains("BuildWallPlacementOrigins", input);
        StringAssert.Contains("BuildFinalWallRuns", input);
        StringAssert.Contains("CommitCurrentWallRun", input);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_buildingPlacementInputSystem\.TryBeginDrag\s*\("),
            "Active placement pointer press orchestration belongs in BuildingPlacementInputSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_buildingPlacementInputSystem\.HandlePointerRelease\s*\("),
            "Active placement pointer release orchestration belongs in BuildingPlacementInputSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_buildingPlacementInputSystem\.HandlePointerNotPressed\s*\("),
            "Active placement pointer release-state orchestration belongs in BuildingPlacementInputSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+UpdateWallDragAxis\b"),
            "Wall drag axis mutation belongs in BuildingPlacementInputSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?List<Vector2Int>\s+BuildWallPlacementOrigins\b"),
            "Wall origin expansion belongs in BuildingPlacementInputSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+CommitCurrentWallRun\b"),
            "Committed wall-run input state belongs in BuildingPlacementInputSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsPointerOverActivePlacement\b"),
            "Active-placement hit testing belongs in BuildingPlacementInputSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingPlacementInputSystem.ActivePlacementPointerContext", StringComparison.Ordinal),
            "Placement input context construction belongs in BuildingPlacementContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateActivePlacementLifecycleSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string lifecycleFile = "Assets/Game/Scripts/Systems/BuildingPlacementLifecycleSystem.cs";
        const string sessionFile = "Assets/Game/Scripts/Systems/BuildingPlacementSessionSystem.cs";
        const string placementContextFile = "Assets/Game/Scripts/Systems/BuildingPlacementContextSystem.cs";
        Assert.IsTrue(File.Exists(lifecycleFile), "The active placement lifecycle slice must live in BuildingPlacementLifecycleSystem.");
        Assert.IsTrue(File.Exists(sessionFile), "The active placement session command slice must live in BuildingPlacementSessionSystem.");
        Assert.IsTrue(File.Exists(placementContextFile), "Placement lifecycle context construction must live in BuildingPlacementContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string lifecycle = File.ReadAllText(lifecycleFile);
        string session = File.ReadAllText(sessionFile);
        string placementContext = File.ReadAllText(placementContextFile);
        StringAssert.Contains("BuildingPlacementLifecycleSystem _buildingPlacementLifecycleSystem", placement);
        StringAssert.Contains("BuildingPlacementSessionSystem _buildingPlacementSessionSystem", placement);
        StringAssert.Contains("_buildingPlacementLifecycleSystem.HasPendingBuildingPlacement", placement);
        StringAssert.Contains("_buildingPlacementLifecycleSystem.CanConfirmBuildingPlacement", placement);
        StringAssert.Contains("_buildingPlacementLifecycleSystem.ActivePlacement", placement);
        StringAssert.Contains("_buildingPlacementSessionSystem.BeginPlacement", placement);
        StringAssert.Contains("_buildingPlacementSessionSystem.ConfirmBuildingPlacement", placement);
        StringAssert.Contains("_buildingPlacementSessionSystem.CancelBuildingPlacement", placement);
        StringAssert.Contains("_buildingPlacementSessionSystem.ExitBuildMode", placement);
        StringAssert.Contains("_buildingPlacementSessionSystem.NotifyPlacementUiPointerDown", placement);
        StringAssert.Contains("_buildingPlacementSessionSystem.SetActivePlacementCost", placement);
        StringAssert.Contains("_buildingPlacementLifecycleSystem.ReleasePreviewOwnership", placement);

        StringAssert.Contains("PlacementState : BuildingPlacementInputSystem.IPlacementState", lifecycle);
        StringAssert.Contains("PlacementState ActivePlacement", lifecycle);
        StringAssert.Contains("ActivePlacementCost", lifecycle);
        StringAssert.Contains("HasPendingBuildingPlacement", lifecycle);
        StringAssert.Contains("CanConfirmBuildingPlacement", lifecycle);
        StringAssert.Contains("SetActivePlacementCost", lifecycle);
        StringAssert.Contains("NotifyPlacementUiPointerDown", lifecycle);
        StringAssert.Contains("Begin(BuildingDefinition definition", lifecycle);
        StringAssert.Contains("Confirm(ConfirmContext context)", lifecycle);
        StringAssert.Contains("ReleasePreviewOwnership", lifecycle);
        StringAssert.Contains("Cancel(CancelContext context)", lifecycle);
        StringAssert.Contains("BeginPlacement(Context context", session);
        StringAssert.Contains("ConfirmBuildingPlacement(Context context)", session);
        StringAssert.Contains("CancelBuildingPlacement(Context context)", session);
        StringAssert.Contains("ExitBuildMode(Context context", session);
        StringAssert.Contains("NotifyPlacementUiPointerDown(Context context)", session);
        StringAssert.Contains("SetActivePlacementCost(Context context", session);
        StringAssert.Contains("_preserveBuildingSelectionOnNextExitBuildMode", session);
        StringAssert.Contains("new BuildingPlacementLifecycleSystem.CancelContext", placementContext);
        StringAssert.Contains("new BuildingPlacementLifecycleSystem.BeginContext", placementContext);
        StringAssert.Contains("new BuildingPlacementLifecycleSystem.ConfirmContext", placementContext);

        Assert.IsFalse(
            placement.Contains("_activePlacement", StringComparison.Ordinal),
            "Active placement mutable state belongs in BuildingPlacementLifecycleSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("_activePlacementCost", StringComparison.Ordinal),
            "Active placement cost state belongs in BuildingPlacementLifecycleSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("private sealed class PlacementState", StringComparison.Ordinal),
            "Active placement state shape belongs in BuildingPlacementLifecycleSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+PlacementState\s*\{"),
            "Active placement state construction belongs in BuildingPlacementLifecycleSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bDestroy\s*\(\s*(?:_activePlacement|activePlacement|placement)\.PreviewInstance\s*\)"),
            "Active preview cancellation belongs in BuildingPlacementLifecycleSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("_preserveBuildingSelectionOnNextExitBuildMode", StringComparison.Ordinal),
            "Active placement selection-preservation state belongs in BuildingPlacementSessionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"_buildingPlacementLifecycleSystem\.(?:Begin|Confirm|Cancel|NotifyPlacementUiPointerDown|SetActivePlacementCost)\s*\("),
            "Active placement command wrappers belong in BuildingPlacementSessionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingPlacementLifecycleSystem.CancelContext", StringComparison.Ordinal) ||
            placement.Contains("new BuildingPlacementLifecycleSystem.BeginContext", StringComparison.Ordinal) ||
            placement.Contains("new BuildingPlacementLifecycleSystem.ConfirmContext", StringComparison.Ordinal),
            "Placement lifecycle context construction belongs in BuildingPlacementContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedGridSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string gridFile = "Assets/Game/Scripts/Systems/BuildingPlacementGridSystem.cs";
        Assert.IsTrue(File.Exists(gridFile), "The placement/grid math slice must live in BuildingPlacementGridSystem.");

        string placement = File.ReadAllText(placementFile);
        string grid = File.ReadAllText(gridFile);
        StringAssert.Contains("BuildingPlacementGridSystem _buildingPlacementGridSystem", placement);
        StringAssert.Contains("_buildingPlacementGridSystem.GetFootprintCenter", placement);
        StringAssert.Contains("_buildingPlacementGridSystem.GetCenterScreenPlacementOrigin", placement);
        StringAssert.Contains("_buildingPlacementGridSystem.GetPlacementFootprint", placement);
        StringAssert.Contains("_buildingPlacementGridSystem.TryGetGridCell", placement);
        StringAssert.Contains("_buildingPlacementGridSystem.ResolvePlacementFocusWorldPosition", placement);
        StringAssert.Contains("BuildingPlacementGridSystem.CenterCellToOrigin", placement);

        StringAssert.Contains("GetFootprintCenter", grid);
        StringAssert.Contains("GetCenterScreenPlacementOrigin", grid);
        StringAssert.Contains("CenterCellToOrigin", grid);
        StringAssert.Contains("TryGetGridCell", grid);
        StringAssert.Contains("GetPlacementFootprint", grid);
        StringAssert.Contains("ResolvePlacementFocusWorldPosition", grid);
        StringAssert.Contains("ScreenPointToRay", grid);
        StringAssert.Contains("GridUtils.WorldToCell", grid);

        Assert.IsFalse(
            Regex.IsMatch(placement, @"ScreenPointToRay\s*\("),
            "Screen-to-grid raycast math belongs in BuildingPlacementGridSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"GridUtils\.WorldToCell\s*\(\s*grid\s*,\s*ray\.GetPoint"),
            "Screen-to-grid conversion belongs in BuildingPlacementGridSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+Plane\s*\("),
            "Grid plane raycast construction belongs in BuildingPlacementGridSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"private\s+Vector3\s+ResolvePlacementFocusWorldPosition\s*\("),
            "Placement focus bounds belong in BuildingPlacementGridSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+Vector2Int\s*\(\s*definition\.FootprintCells\.y\s*,\s*definition\.FootprintCells\.x\s*\)"),
            "Placement footprint rotation belongs in BuildingPlacementGridSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedUiQuerySlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string placementQueryFile = "Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs";
        const string uiQueryFile = "Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs";
        const string uiCommandFile = "Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs";
        const string uiContextFile = "Assets/Game/Scripts/Systems/BuildingUiContextSystem.cs";
        const string menuViewFile = "Assets/Game/Scripts/UI/MenuView.cs";
        Assert.IsTrue(File.Exists(placementQueryFile), "The selected-building scalar query slice must live in BuildingPlacementQuerySystem.");
        Assert.IsTrue(File.Exists(uiQueryFile), "The temporary building UI read model slice must live in BuildingUiQuerySystem.");
        Assert.IsTrue(File.Exists(uiCommandFile), "The building UI command slice must live in BuildingUiCommandSystem.");
        Assert.IsTrue(File.Exists(uiContextFile), "Building UI command/query context construction must live in BuildingUiContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string placementQuery = File.ReadAllText(placementQueryFile);
        string uiQuery = File.ReadAllText(uiQueryFile);
        string uiCommand = File.ReadAllText(uiCommandFile);
        string uiContext = File.ReadAllText(uiContextFile);
        string menuView = File.ReadAllText(menuViewFile);
        StringAssert.Contains("BuildingPlacementQuerySystem _buildingPlacementQuerySystem", placement);
        StringAssert.Contains("CreateBuildingPlacementQueryContext", placement);
        StringAssert.Contains("_buildingPlacementQuerySystem.GetPlacementStatusText", placement);
        StringAssert.Contains("_buildingPlacementQuerySystem.GetSelectedBuildingLabel", placement);
        StringAssert.Contains("_buildingPlacementQuerySystem.GetSelectedBuildingDisplayName", placement);
        StringAssert.Contains("_buildingPlacementQuerySystem.GetSelectedBuildingDescription", placement);
        StringAssert.Contains("_buildingPlacementQuerySystem.TryGetSelectedBuildingPreviewPrefab", placement);
        StringAssert.Contains("_buildingPlacementQuerySystem.TryGetSelectedBuildingHealth", placement);

        StringAssert.Contains("GetPlacementStatusText", placementQuery);
        StringAssert.Contains("GetSelectedBuildingLabel", placementQuery);
        StringAssert.Contains("GetSelectedBuildingDisplayName", placementQuery);
        StringAssert.Contains("GetSelectedBuildingDescription", placementQuery);
        StringAssert.Contains("TryGetSelectedBuildingPreviewPrefab", placementQuery);
        StringAssert.Contains("TryGetSelectedBuildingHealth", placementQuery);
        StringAssert.Contains("public Context CreateContext(Source source)", placementQuery);
        Assert.IsFalse(
            placement.Contains("new BuildingPlacementQuerySystem.Context", StringComparison.Ordinal),
            "Selected-building query context construction belongs in BuildingPlacementQuerySystem, not BuildingPlacementSystem.");

        StringAssert.Contains("BuildingUiQuerySystem _buildingUiQuerySystem", placement);
        StringAssert.Contains("BuildingUiContextSystem _buildingUiContextSystem", placement);
        StringAssert.Contains("CreateBuildingUiQueryContext", placement);
        StringAssert.Contains("_buildingUiContextSystem.CreateCommandContext", placement);
        StringAssert.Contains("_buildingUiContextSystem.CreateQueryContext", placement);
        StringAssert.Contains("new BuildingUiCommandSystem.Context", uiContext);
        StringAssert.Contains("new BuildingUiQuerySystem.Context", uiContext);
        StringAssert.Contains("GetSelectedBuildingProducedUnits", uiQuery);
        StringAssert.Contains("GetSelectedBuildingProducedUnitEntries", uiQuery);
        StringAssert.Contains("GetFriendlyPendingProductionUiEntries", uiQuery);
        StringAssert.Contains("HasActiveBuilding", uiQuery);
        StringAssert.Contains("SelectedBuildingDisplayName", uiQuery);
        StringAssert.Contains("TryGetSelectedBuildingHealth", uiQuery);
        StringAssert.Contains("TryGetSelectedBuildingPreviewPrefab", uiQuery);
        StringAssert.Contains("IsRuntimeBuildingWall", uiQuery);
        StringAssert.Contains("IsRuntimeBuildingCityGenerated", uiQuery);
        StringAssert.Contains("TryGetRuntimeBuildingOwnerFaction", uiQuery);
        StringAssert.Contains("HasVisibleSelectableBuilding", uiQuery);
        StringAssert.Contains("TryResolveLiveUnitPreviewPrefab", uiQuery);
        StringAssert.Contains("public readonly struct ProducedUnitUiEntry", uiQuery);
        StringAssert.Contains("public readonly struct PendingProductionUiEntry", uiQuery);
        StringAssert.Contains("BuildingUiQuerySystem _buildingUiQuerySystem", menuView);
        StringAssert.Contains("_buildingUiQuerySystem.GetFriendlyPendingProductionUiEntries", menuView);
        Assert.IsFalse(
            uiCommand.Contains("GetFriendlyPendingProductionUiEntries", StringComparison.Ordinal) ||
            uiCommand.Contains("HasActiveBuilding", StringComparison.Ordinal) ||
            uiCommand.Contains("SelectedBuildingDisplayName", StringComparison.Ordinal) ||
            uiCommand.Contains("TryGetSelectedBuildingHealth", StringComparison.Ordinal) ||
            uiCommand.Contains("TryGetSelectedBuildingPreviewPrefab", StringComparison.Ordinal) ||
            uiCommand.Contains("IsRuntimeBuildingWall", StringComparison.Ordinal) ||
            uiCommand.Contains("IsRuntimeBuildingCityGenerated", StringComparison.Ordinal) ||
            uiCommand.Contains("TryGetRuntimeBuildingOwnerFaction", StringComparison.Ordinal) ||
            uiCommand.Contains("HasVisibleSelectableBuilding", StringComparison.Ordinal) ||
            uiCommand.Contains("TryResolveLiveUnitPreviewPrefab", StringComparison.Ordinal),
            "BuildingUiCommandSystem must not own building UI read-model query delegates.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+readonly\s+struct\s+(ProducedUnitUiEntry|PendingProductionUiEntry)"),
            "Building UI read-model entries belong in BuildingUiQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+(readonly\s+struct\s+(ConfiguredSpawnableEntry|ConfiguredUnitEntry)|enum\s+CampRequestFailure)"),
            "Building UI command/config contracts belong in BuildingUiCommandSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("return $\"{building.Definition.DisplayName} ({building.OriginCell.x},{building.OriginCell.y})\";", StringComparison.Ordinal),
            "Selected-building label formatting belongs in BuildingPlacementQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+bool\s+TryGetSelectedBuildingHealth\([\s\S]*?GetComponentData<UnitHealth>[\s\S]*?public\s+string\s+DeleteButtonText"),
            "Selected-building health lookup belongs in BuildingPlacementQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"entries\.Add\(new\s+ProducedUnitUiEntry\(Entity\.Null"),
            "Pending produced-unit UI entry shaping belongs in BuildingUiQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"entries\.Add\(new\s+PendingProductionUiEntry"),
            "Pending production UI entry shaping belongs in BuildingUiQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+void\s+GetSelectedBuildingProducedUnits\([\s\S]*?building\.ProducedUnits\.RemoveAt\(i\)[\s\S]*?public\s+void\s+GetSelectedBuildingProducedUnitEntries"),
            "Selected-building produced-unit UI pruning belongs in BuildingUiQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+void\s+GetSelectedBuildingProducedUnitEntries\([\s\S]*?ProducedUnitPrefabs\.Remove\(unit\)[\s\S]*?public\s+bool\s+TryGetSelectedBuildingCapacityInfo"),
            "Selected-building produced-unit prefab UI pruning belongs in BuildingUiQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+void\s+GetFriendlyPendingProductionUiEntries\([\s\S]*?entries\)\s*\{\s*foreach\s*\(KeyValuePair<int,\s*RuntimeBuildingData>"),
            "Friendly pending-production UI iteration belongs in BuildingUiQuerySystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingUiCommandSystem.Context", StringComparison.Ordinal) ||
            placement.Contains("new BuildingUiQuerySystem.Context", StringComparison.Ordinal),
            "Building UI command/query context construction belongs in BuildingUiContextSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"public\s+(?:int|GameObject|void|bool|string|CampRequestFailure)\s+(?:ConfiguredSpawnableCount|ConfiguredUnitCount|CurrentDollars|HasVisibleSelectableBuilding|SelectedBuildingPrimarySpawnUnitPrefab|SelectedBuildingSecondarySpawnUnitPrefab|SelectedBuildingTertiarySpawnUnitPrefab|SelectedBuildingQuaternarySpawnUnitPrefab|GetSelectedBuildingProductionPrefabs|GetSelectedBuildingProducedUnits|GetSelectedBuildingProducedUnitEntries|TryGetSelectedBuildingCapacityInfo|GetFriendlyPendingProductionUiEntries|TryGetSelectedBuildingCapacity2Info|IsRuntimeBuildingCityGenerated|IsRuntimeBuildingWall|TryGetRuntimeBuildingOwnerFaction|TryResolveLiveUnitPreviewPrefab|TryGetSelectedBuildingProductionPrefab|SelectedBuildingDisplayName|TryGetSelectedBuildingPreviewPrefab|TryGetSelectedBuildingHealth|TryGetConfiguredSpawnable|TryGetConfiguredUnit|IsConfiguredSpawnablePrefab|GetCampRequestFailure|TryRequestCampItem|FocusLastCampProductionRequest|BeginPlacementForConfiguredSpawnable)\b"),
            "BuildingPlacementSystem must not expose public building UI query/command compatibility wrappers after MenuView binds to narrow UI systems.");
        Assert.IsFalse(
            placement.Contains("new BuildingCombatSystem.Context<RuntimeBuildingData>", StringComparison.Ordinal),
            "Building combat context construction belongs in BuildingRuntimeContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedBarrierSlice()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string barrierFile = "Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs";
        const string runtimeContextFile = "Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs";
        const string buildingCompositionFile = "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs";
        Assert.IsTrue(File.Exists(barrierFile), "The road barrier and base-breach slice must live in BuildingBarrierSystem.");
        Assert.IsTrue(File.Exists(runtimeContextFile), "Building barrier context construction must live in BuildingRuntimeContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string barrier = File.ReadAllText(barrierFile);
        string runtimeContext = File.ReadAllText(runtimeContextFile);
        string buildingComposition = File.ReadAllText(buildingCompositionFile);
        StringAssert.Contains("BuildingBarrierSystem _buildingBarrierSystem", placement);
        StringAssert.Contains("CreateBuildingBarrierContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateBarrierContext", placement);
        StringAssert.Contains("new BuildingBarrierSystem.Context", runtimeContext);
        StringAssert.Contains("tickDomains.Barrier.UpdateRoadBarrierDoors", buildingComposition);
        StringAssert.Contains("_buildingBarrierSystem.RememberOpenBaseBreach", placement);
        StringAssert.Contains("_buildingBarrierSystem.TryResolveBaseBreachTarget", placement);
        StringAssert.Contains("_buildingBarrierSystem.GetRuntimeRoadBarrierGateRects", placement);
        StringAssert.Contains("_buildingBarrierSystem.ShouldAlignGateToNearbyWall", placement);
        StringAssert.Contains("BuildingBarrierSystem.IsWallGateDefinition", runtimeContext);
        StringAssert.Contains("IsLinearWallDefinition", barrier);

        StringAssert.Contains("RememberOpenBaseBreach", barrier);
        StringAssert.Contains("HasOpenBaseBreach", barrier);
        StringAssert.Contains("TryFindEnemyWallPerimeterContainingCell", barrier);
        StringAssert.Contains("TryFindBreachBuilding", barrier);
        StringAssert.Contains("TryResolveBaseBreachTarget", barrier);
        StringAssert.Contains("TryFindBreachApproachCell", barrier);
        StringAssert.Contains("ResolvePerimeterOutsideDirection", barrier);
        StringAssert.Contains("TryScoreBreachApproachCandidate", barrier);
        StringAssert.Contains("IsOutsidePerimeterOnSide", barrier);
        StringAssert.Contains("UpdateRoadBarrierDoors", barrier);
        StringAssert.Contains("GetRuntimeRoadBarrierGateRects", barrier);
        StringAssert.Contains("UpdateRoadBarrierDoorVisual", barrier);
        StringAssert.Contains("SetBarrierDoorOpen01", barrier);
        StringAssert.Contains("ShouldAlignGateToNearbyWall", barrier);
        StringAssert.Contains("TryResolveNearbyWallVertical", barrier);
        StringAssert.Contains("IsWallGateDefinition", barrier);
        StringAssert.Contains("IsLinearWallDefinition", barrier);

        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+RememberOpenBaseBreach\b"),
            "Base-breach memory belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+HasOpenBaseBreach\b"),
            "Open breach lookup belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryFindEnemyWallPerimeterContainingCell\b"),
            "Enemy wall/gate perimeter lookup belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryFindBreachBuilding\b"),
            "Breach-building target selection belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"TryFindRuntimeBuildingByCombatEntity"),
            "Breach final-target building lookup belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryFindBreachApproachCell\b"),
            "Breach approach-cell search belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?int2\s+ResolvePerimeterOutsideDirection\b"),
            "Breach perimeter outside-direction selection belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+TryScoreBreachApproachCandidate\b"),
            "Breach approach-cell scoring belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsOutsidePerimeterOnSide\b"),
            "Breach perimeter-side checks belong in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+UpdateRoadBarrierDoors\b"),
            "Road barrier door polling belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\binternal\s+void\s+UpdateRoadBarrierDoors\s*\("),
            "Runtime road barrier door ticks must be wired to BuildingBarrierSystem from composition, not wrapped by BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsActiveRoadGateBuilding\b"),
            "Road gate active classification belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+UpdateRoadBarrierDoorVisual\b"),
            "Barrier door visual open-state updates belong in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?void\s+SetBarrierDoorOpen01\b"),
            "Barrier door transform mutation belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+HasNearbyFriendlyUnit\b"),
            "Barrier door proximity checks belong in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("RuntimeBaseBreach", StringComparison.Ordinal),
            "Base breach runtime state belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsWallGateDefinition\b"),
            "Road barrier gate classification belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsLinearWallDefinition\b"),
            "Linear wall classification belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+bool\s+TryResolveNearbyWallVertical\b"),
            "Gate-to-nearby-wall alignment lookup belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            placement.Contains("new BuildingBarrierSystem.Context", StringComparison.Ordinal),
            "Building barrier context construction belongs in BuildingRuntimeContextSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateRemainingRuntimeContextConstruction()
    {
        const string placementFile = "Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs";
        const string runtimeContextFile = "Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs";
        Assert.IsTrue(File.Exists(runtimeContextFile), "Remaining runtime context construction must live in BuildingRuntimeContextSystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeContext = File.ReadAllText(runtimeContextFile);
        string[] requiredRuntimeContextConstructors =
        {
            "new BuildingSpawnSystem.Context",
            "new BuildingRuntimeEntitySystem.Context",
            "new BuildingRuntimeVisualSystem.Context",
            "new BuildingPlacementRedirectSystem.Context",
            "new BuildingCombatSystem.Context<RuntimeBuildingData>",
            "new BuildingRuntimeQuerySystem.Context",
            "new BuildingBarrierSystem.Context"
        };

        foreach (string token in requiredRuntimeContextConstructors)
        {
            StringAssert.Contains(token, runtimeContext);
            Assert.IsFalse(
                placement.Contains(token, StringComparison.Ordinal),
                $"{token} construction belongs in BuildingRuntimeContextSystem, not BuildingPlacementSystem.");
        }

        StringAssert.Contains("BuildingRuntimeContextSystem.RuntimeSource", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateBuildingSpawnContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateRuntimeEntityContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateRuntimeVisualContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateRedirectContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateCombatContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateRuntimeQueryContext", placement);
        StringAssert.Contains("_buildingRuntimeContextSystem.CreateBarrierContext", placement);
    }

    [Test]
    public void BuildingPlacementSystemFacadeMustNotExistAndRetirementAuditMustBeClosed()
    {
        const string auditPath = "Design/Architecture/buildingplacement_retirement_audit.md";
        const string facadePath = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string facadeMetaPath = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs.meta";
        Assert.IsTrue(File.Exists(auditPath), $"{auditPath} must record the completed facade deletion.");
        Assert.IsFalse(File.Exists(facadePath), $"{facadePath} must stay deleted.");
        Assert.IsFalse(File.Exists(facadeMetaPath), $"{facadeMetaPath} must stay deleted.");

        string audit = File.ReadAllText(auditPath);
        StringAssert.Contains("Current Status", audit);
        StringAssert.Contains("`BuildingPlacementSystem` is retired and deleted.", audit);
        StringAssert.Contains("Hard Rule", audit);
        StringAssert.Contains("`BuildingPlacementSystem` must not exist.", audit);
        StringAssert.Contains("Allowed related names", audit);
        StringAssert.Contains("BuildingPlacementSystemConfig", audit);
        StringAssert.Contains("Closed Deletion Gates", audit);
        Assert.IsFalse(audit.Contains("Allowed Production Facade References", StringComparison.Ordinal));
        Assert.IsFalse(audit.Contains("Allowed Test Facade Construction", StringComparison.Ordinal));
        Assert.IsFalse(audit.Contains("Drift Guard", StringComparison.Ordinal));
        Assert.IsFalse(audit.Contains("one-line legacy wrapper", StringComparison.Ordinal));
        Assert.IsFalse(audit.Contains("must remain a one-line", StringComparison.Ordinal));
    }

    [Test]
    public void BuildingPlacementSystemProductionReferencesMustNotExist()
    {
        string[] violations = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bBuildingPlacementSystem\b(?!Config)"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Production code must not reference the retired BuildingPlacementSystem facade. Use narrow building systems/contexts instead:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));

        string[] constructionFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"new\s+BuildingPlacementSystem\s*\("))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            constructionFiles,
            "Production code must not construct the retired BuildingPlacementSystem facade.");
    }

    [Test]
    public void BuildingPlacementSystemTestReferencesMustNotExist()
    {
        const string harnessFile = "Assets/Tests/Editor/BuildingGameplayTestHarness.cs";

        Assert.IsTrue(File.Exists(harnessFile), "Editor building validation tests may use the explicit test-only gameplay harness.");
        string harness = File.ReadAllText(harnessFile);
        StringAssert.Contains("BuildingGameplayTestHarness : BuildingGameplaySystem", harness);
        string[] productionHarnessReferences = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => File.ReadAllText(path).Contains("BuildingGameplayTestHarness", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            productionHarnessReferences,
            "BuildingGameplayTestHarness is editor-only and must not be referenced by production scripts.");

        string helper = File.ReadAllText("Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs");
        StringAssert.Contains("BuildingGameplaySystem buildingPlacement", helper);
        Assert.IsFalse(
            helper.Contains("BuildingPlacementSystem", StringComparison.Ordinal),
            "RuntimeGameplayStateTestHelper must accept the implementation boundary or narrower systems, not the legacy facade.");

        string[] violations = Directory.GetFiles("Assets/Tests/Editor", "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => !path.EndsWith("GameplayArchitectureContractTests.cs", StringComparison.Ordinal))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bBuildingPlacementSystem\b(?!Config)"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Editor tests must not reference BuildingPlacementSystem. Use BuildingGameplayTestHarness or narrower systems:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> GetTopLevelTypeNames(string file)
    {
        string text = File.ReadAllText(file);
        foreach (Match match in Regex.Matches(text, @"\b(?:public|internal|private)?\s*(?:sealed\s+|static\s+|abstract\s+|partial\s+)*class\s+([A-Za-z_][A-Za-z0-9_]*)"))
            yield return match.Groups[1].Value;
    }

    private static IEnumerable<string> GetMethodNames(string file)
    {
        string text = File.ReadAllText(file);
        foreach (Match match in Regex.Matches(
                     text,
                     @"^\s*(?:public|private|internal|protected)\s+(?:static\s+)?(?:[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*\(",
                     RegexOptions.Multiline))
        {
            yield return match.Groups[1].Value;
        }
    }

    private static bool IsGameBootstrapDomainPolicyMethodName(string methodName)
    {
        return GameBootstrapDomainPolicyMethodTokens.Any(token => methodName.Contains(token, StringComparison.Ordinal));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
#endif
