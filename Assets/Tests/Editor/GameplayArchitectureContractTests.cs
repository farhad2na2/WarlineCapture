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
    private const string ScriptsRoot = "Assets/Game/Scripts";
    private const string BootstrapRoot = "Assets/Game/Scripts/Bootstrap";
    private const string ScenesRoot = "Assets/Game/Scripts/Scenes";
    private const int LegacyGameBootstrapDirectLogCallCount = 7;

    private static readonly string[] LegacyAILogCallFiles = Array.Empty<string>();

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
        "Assets/Game/Scripts/Iso2D/WarlineCaptureIso2DCameraController.cs",
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
        StringAssert.Contains("The retired `AILog` facade must not be reintroduced", contract);
        StringAssert.Contains("`BuildingPlacementSystem` is legacy facade debt", contract);
        StringAssert.Contains("wall run/origin validation, and wall overlap-cell checks belong in `BuildingPlacementValidationSystem`", contract);
        StringAssert.Contains("registry ownership, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`", contract);
        StringAssert.Contains("Runtime building data creation, runtime registry insertion, blocker/combat entity hookup, runtime link attachment, initial production collections, produced-unit slot array setup, placement redirect side effects, and marker refresh policy belong in `BuildingRuntimeCreationSystem`", contract);
        StringAssert.Contains("Runtime building read/query facades, faction building/unit/production counts, building role/id lists, owner/destroyed/city/refugee flags, combat entity info, focus-position queries, and building approach-cell query routing belong in `BuildingRuntimeQuerySystem`", contract);
        StringAssert.Contains("Building definition/configured spawnable lookup, spawnable/unit prefab lookup aliases, runtime building prefab metadata cache, prefab bounds/visual-footprint discovery, production spawn point metadata, production-slot read helpers, and runtime/configured building definition construction belong in `BuildingDefinitionSystem`", contract);
        StringAssert.Contains("Building selection clearing, select-and-focus behavior, selected-building focus position resolution, and runtime building click hit-test/routing belong in `BuildingSelectionSystem`", contract);
        StringAssert.Contains("animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`", contract);
        StringAssert.Contains("Building deletion orchestration, destruction state, cleanup timing, blocker cleanup, combat-health destruction checks, destroyed-entity callbacks, destroyed visual toggling, and destroyed building finalization belong in `BuildingCombatSystem`", contract);
        StringAssert.Contains("Resource storage classification, capacity display math, resource totals, faction economy snapshots, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`", contract);
        StringAssert.Contains("Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`", contract);
        StringAssert.Contains("Unit production queue item initialization, player unit production queue mutation, pending production timing/progress, readiness checks, produced-unit liveness pruning, pending queue removal, ready/soon transport-pending lookup, production duration, transport settings/fallback policy, transport unit classification, and transport launch delay math belong in `BuildingProductionSystem`; production slot discovery, pending-slot reservation checks, slot occupancy cleanup, and production slot reservation belong in `BuildingProductionSlotSystem`; active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, and transport visual helpers belong in `BuildingProductionTransportSystem`; produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad spawn fallback, and spawned ECS unit initialization belong in `BuildingSpawnSystem`; spawn prefab registry lookup, prefab entity resolution, and live-unit prefab fallback lookup belong in `BuildingSpawnPrefabSystem`", contract);
        StringAssert.Contains("Selected-building unit production request routing, camp item request failure policy, UI production arm consumption, friendly producer lookup, production request focus, and last camp production focus memory belong in `BuildingProductionRequestSystem`", contract);
        StringAssert.Contains("Runway prefab metadata discovery, runway footprint expansion for placement validity, and nearest airport runway lookup belong in `BuildingRunwaySystem`", contract);
        StringAssert.Contains("Placement outline object lifetime, outline material/color updates, wall preview segment rebuilds, and preview segment validity tinting belong in `BuildingPlacementPreviewSystem`", contract);
        StringAssert.Contains("Placement commit expansion, wall-run origin construction, wall segment footprint/rotation helpers, wall segment runtime creation, committed placement preview consumption, and post-placement auto-select policy belong in `BuildingPlacementCommitSystem`", contract);
        StringAssert.Contains("Active placement drag state, pointer-to-cell placement movement, wall drag axis/origin expansion, committed wall-run input state, and active-placement hit testing belong in `BuildingPlacementInputSystem`", contract);
        StringAssert.Contains("Placement status text, selected-building labels/descriptions, selected-building preview prefab lookup, selected-building health lookup, and selected-building production prefab read models belong in `BuildingPlacementQuerySystem`", contract);
        StringAssert.Contains("Road barrier gate classification, gate-to-nearby-wall alignment, base-breach memory, enemy wall/gate perimeter lookup, breach-building target selection, barrier door proximity checks, and barrier door visual open-state updates belong in `BuildingBarrierSystem`", contract);
        StringAssert.Contains("Produced-unit UI lists, pending-production UI entries, UI progress shaping, and temporary building UI list read models belong in `BuildingUiQuerySystem`", contract);
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
        StringAssert.Contains("ResolveM01ProductionOrthographicSize", missionCamera);
        StringAssert.Contains("TryResolveM01ProductionFrameCenter", missionCamera);
        StringAssert.Contains("ClampM01CameraCenterToTacticalMap", missionCamera);
        StringAssert.Contains("SetPositionAndRotation", missionCamera);
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
        Assert.IsTrue(File.Exists(runtimeUpdateFile), "Managed runtime update orchestration must live in GameplayRuntimeUpdateSystem.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("GameplayRuntimeUpdateSystem _gameplayRuntimeUpdateSystem", bootstrap);
        StringAssert.Contains("_gameplayRuntimeUpdateSystem.Update", bootstrap);
        StringAssert.Contains("_gameplayRuntimeUpdateSystem.LateUpdate", bootstrap);
        StringAssert.Contains("_gameplayRuntimeUpdateSystem.OnGui", bootstrap);
        StringAssert.Contains("ref _gameplayStartPending", bootstrap);

        string[] bootstrapRuntimeUpdateDebtTokens =
        {
            "GameRuntimeStats.RecordMissionElapsed",
            "_missionStartupSystem.UpdateActiveMission",
            "_missionStartupSystem.ApplyM01ProductionCameraPoseIfActive",
            "RuntimeCitySpawner?.Update",
            "RuntimeGridBlockers?.Update",
            "RuntimeDecorations?.Update",
            "WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene",
            "IsGameplayStartComplete",
            "UnitAttackTraces?.LateUpdate",
            "UnitImpostors?.LateUpdate",
            "RoadBuild?.OnGui",
            "Selection?.OnGui"
        };

        foreach (string token in bootstrapRuntimeUpdateDebtTokens)
        {
            Assert.IsFalse(
                bootstrap.Contains(token, StringComparison.Ordinal),
                $"{token} belongs in GameplayRuntimeUpdateSystem, not GameBootstrap.");
        }

        string runtimeUpdate = File.ReadAllText(runtimeUpdateFile);
        string[] runtimeUpdateRequiredTokens =
        {
            "GameRuntimeStats.RecordMissionElapsed",
            "missionStartupSystem.UpdateActiveMission",
            "missionStartupSystem.ApplyM01ProductionCameraPoseIfActive",
            "runtimeCitySpawner?.Update",
            "runtimeGridBlockers?.Update",
            "runtimeDecorations?.Update",
            "WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene",
            "IsGameplayStartComplete",
            "unitAttackTraces?.LateUpdate",
            "unitImpostors?.LateUpdate",
            "roadBuild?.OnGui",
            "selection?.OnGui"
        };

        foreach (string token in runtimeUpdateRequiredTokens)
            StringAssert.Contains(token, runtimeUpdate);

        string[] orderedStepLabels =
        {
            "\"MenuCanvasInput\"",
            "\"MissionRuntime\"",
            "\"RoadBuild\"",
            "\"BuildingPlacement\"",
            "\"Selection\"",
            "\"MissionCamera\"",
            "\"RuntimeCitySpawner\"",
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
        StringAssert.Contains("EnsureBuildingPlacementRuntimeComponent", bootstrap);
        StringAssert.Contains("_runtimeCameraReferenceSystem.SetWorldCamera", bootstrap);

        string[] managedStartupDebtTokens =
        {
            "new DayNightSystem()",
            "new FactionVisualSettings()",
            "new RoadBuildSystem()",
            "new BuildingPlacementSystem()",
            "new RTSSelectionSystem()",
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
        foreach (string token in managedStartupDebtTokens)
            StringAssert.Contains(token, startup);
        StringAssert.Contains("roadBuild.BindDependencies(buildingPlacement)", startup);
        StringAssert.Contains("selection.BindDependencies(null, roadBuild, buildingPlacement)", startup);
        StringAssert.Contains("citizenPopulation.Init(buildingPlacement, dayNight, worldCamera)", startup);
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
        StringAssert.Contains("MenuStartupSystem _menuStartupSystem", bootstrap);
        StringAssert.Contains("_menuStartupSystem.Initialize", bootstrap);
        StringAssert.Contains("_menuStartupSystem.Shutdown", bootstrap);
        StringAssert.Contains("Debug.LogException", bootstrap);

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

        string startup = File.ReadAllText(menuStartupFile);
        foreach (string token in menuStartupDebtTokens)
            StringAssert.Contains(token, startup);
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
        StringAssert.Contains("RuntimeCitySpawner = gameplaySystems.RuntimeCitySpawner", bootstrap);
        StringAssert.Contains("RuntimeGridBlockers = gameplaySystems.RuntimeGridBlockers", bootstrap);
        StringAssert.Contains("RuntimeDecorations = gameplaySystems.RuntimeDecorations", bootstrap);
        StringAssert.Contains("GameplayInitialized = true", bootstrap);
        Assert.IsFalse(
            Regex.IsMatch(bootstrap, @"\b(?:public|private|internal|protected)\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\]\.\s]*\s+EnsureGameplaySystemsInitialized\s*\("),
            "EnsureGameplaySystemsInitialized must not return to GameBootstrap.");

        string[] gameplayStartupDebtTokens =
        {
            "new RuntimeCitySpawnerSystem()",
            "runtimeCitySpawner.Init",
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
        StringAssert.Contains("roadBuild?.BindDependencies(buildingPlacement, mainMenu, runtimeGridBlockers)", startup);
        StringAssert.Contains("buildingPlacement?.BindDependencies(", startup);
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
            "Assets/Game/Scripts/UI/RTSSelectionSystem.cs"
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
        const string file = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
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
            "Assets/Game/Scripts/UI/RoadBuildSystem.cs",
            "Assets/Game/Scripts/UI/RTSSelectionSystem.cs"
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
            "AI systems must read BuildingPlacementRuntimeComponent from ECS runtime data instead of BuildingPlacementSystem.Instance:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void InitialSpawnSystemMustNotReachThroughBuildingPlacementSingleton()
    {
        const string file = "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs";
        string text = File.ReadAllText(file);

        Assert.IsFalse(
            text.Contains("BuildingPlacementSystem.Instance", StringComparison.Ordinal),
            "InitialUnitsSpawnSystem must read BuildingPlacementRuntimeComponent from ECS runtime data instead of BuildingPlacementSystem.Instance.");
    }

    [Test]
    public void RuntimeCitySpawnerSystemMustNotReachThroughBuildingPlacementSingleton()
    {
        const string file = "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs";
        string text = File.ReadAllText(file);

        Assert.IsFalse(
            text.Contains("BuildingPlacementSystem.Instance", StringComparison.Ordinal),
            "RuntimeCitySpawnerSystem must use the BuildingPlacementSystem supplied by bootstrap composition instead of BuildingPlacementSystem.Instance.");
    }

    [Test]
    public void CitizenPopulationSystemMustNotReachThroughBuildingPlacementSingleton()
    {
        const string file = "Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs";
        string text = File.ReadAllText(file);

        Assert.IsFalse(
            text.Contains("BuildingPlacementSystem.Instance", StringComparison.Ordinal),
            "CitizenPopulationSystem must use the BuildingPlacementSystem supplied by bootstrap composition instead of BuildingPlacementSystem.Instance.");
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
            "Do not read BuildingPlacementSystem.Instance. Use bootstrap composition or BuildingPlacementRuntimeComponent:" +
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
            "Assets/Game/Scripts/UI/RTSSelectionSystem.cs",
            "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs"
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
    public void RtsSelectionSystemMustDelegateSelectionStateSlice()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string stateFile = "Assets/Game/Scripts/Systems/SelectionStateSystem.cs";
        Assert.IsTrue(File.Exists(stateFile), "The RTS selection state slice must live in SelectionStateSystem.");

        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("SelectionStateSystem _selectionStateSystem", selection);
        StringAssert.Contains("_selectionStateSystem.CacheSelectedMoveEntities", selection);
        StringAssert.Contains("_selectionStateSystem.CacheSelectedMoveEntity", selection);
        Assert.IsFalse(
            Regex.IsMatch(selection, @"readonly\s+List<Entity>\s+_cachedSelectedMoveEntities\s*=\s*new\s*\("),
            "Selected move cache ownership belongs in SelectionStateSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"\bEntity\s+_focusedUnit\s*;"),
            "Focused unit state ownership belongs in SelectionStateSystem, not RTSSelectionSystem.");
    }

    [Test]
    public void RtsSelectionSystemMustDelegateSelectionUiQuerySlice()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string uiQueryFile = "Assets/Game/Scripts/Systems/SelectionUiQuerySystem.cs";
        Assert.IsTrue(File.Exists(uiQueryFile), "Focused and selected UI read models must live in SelectionUiQuerySystem.");

        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("SelectionUiQuerySystem _selectionUiQuerySystem", selection);
        StringAssert.Contains("_selectionUiQuerySystem.ResolveFocusedUnitName", selection);
        StringAssert.Contains("_selectionUiQuerySystem.ResolveFocusedUnitDescription", selection);
        StringAssert.Contains("_selectionUiQuerySystem.GetFocusedUnitUiStatus", selection);
        StringAssert.Contains("_selectionUiQuerySystem.TryGetSelectedUnitsPortraitPose", selection);
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+string\s+ResolveFocusedUnitName\s*\("),
            "Focused unit label read models belong in SelectionUiQuerySystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+string\s+ResolveHudSelectionStatus\s*\("),
            "HUD selection status read models belong in SelectionUiQuerySystem, not RTSSelectionSystem.");
    }

    [Test]
    public void RtsSelectionSystemMustDelegateMoveOrderSlice()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string moveOrderFile = "Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs";
        Assert.IsTrue(File.Exists(moveOrderFile), "Manual move-order goal and footprint rules must live in UnitMoveOrderSystem.");

        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("UnitMoveOrderSystem _unitMoveOrderSystem", selection);
        StringAssert.Contains("_unitMoveOrderSystem.BuildSelectedCurrentFootprintCells", selection);
        StringAssert.Contains("_unitMoveOrderSystem.FindManualMoveGoal", selection);
        StringAssert.Contains("_unitMoveOrderSystem.IssueGroupedManualMoveOrder", selection);
        StringAssert.Contains("_unitMoveOrderSystem.IssueImmediateMoveCommand", selection);
        StringAssert.Contains("_unitMoveOrderSystem.ClearMovementOrderComponents", selection);
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+int2\s+FindManualMoveGoal\s*\("),
            "Manual move-goal selection belongs in UnitMoveOrderSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+HashSet<int>\s+BuildSelectedCurrentFootprintCells\s*\("),
            "Selected footprint collection belongs in UnitMoveOrderSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+void\s+IssueMoveCommand\s*\("),
            "Movement command component writes belong in UnitMoveOrderSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+void\s+ClearMovementOrderComponents\s*\("),
            "Movement command cleanup belongs in UnitMoveOrderSystem, not RTSSelectionSystem.");
    }

    [Test]
    public void RtsSelectionSystemMustDelegateTransportBoardingSlice()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string transportFile = "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs";
        Assert.IsTrue(File.Exists(transportFile), "Transport boarding rules must live in UnitTransportBoardingSystem.");

        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("UnitTransportBoardingSystem _unitTransportBoardingSystem", selection);
        StringAssert.Contains("_unitTransportBoardingSystem.IsBoardablePlayerTransport", selection);
        StringAssert.Contains("_unitTransportBoardingSystem.TryEnsureTransportCapacity", selection);
        StringAssert.Contains("_unitTransportBoardingSystem.IsTransportLandedForBoarding", selection);
        StringAssert.Contains("_unitTransportBoardingSystem.TryFindAirTransportPickupForBoarding", selection);
        StringAssert.Contains("_unitTransportBoardingSystem.TryFindTransportApproachCell", selection);
        StringAssert.Contains("_unitTransportBoardingSystem.TryFindTransportDisembarkCell", selection);
        StringAssert.Contains("_unitTransportBoardingSystem.StartRopeDisembarkTransport", selection);
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+bool\s+IsBoardablePlayerTransport\s*\("),
            "Boardable transport rules belong in UnitTransportBoardingSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+bool\s+TryEnsureTransportCapacity\s*\("),
            "Transport capacity normalization belongs in UnitTransportBoardingSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+bool\s+TryFindAirTransportPickupForBoarding\s*\("),
            "Air transport pickup-cell selection belongs in UnitTransportBoardingSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+bool\s+TryFindTransportApproachCell\s*\("),
            "Transport boarding approach-cell selection belongs in UnitTransportBoardingSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+bool\s+TryFindTransportDisembarkCell\s*\("),
            "Transport disembark-cell selection belongs in UnitTransportBoardingSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+void\s+StartRopeDisembarkTransport\s*\("),
            "Rope disembark request setup belongs in UnitTransportBoardingSystem, not RTSSelectionSystem.");
    }

    [Test]
    public void RtsSelectionSystemMustDelegateTargetOrderSlice()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string targetOrderFile = "Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs";
        Assert.IsTrue(File.Exists(targetOrderFile), "Target-order and target classification helpers must live in UnitTargetOrderSystem.");

        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("UnitTargetOrderSystem _unitTargetOrderSystem", selection);
        StringAssert.Contains("_unitTargetOrderSystem.TryFindRadarTargetForMissileLauncher", selection);
        StringAssert.Contains("_unitTargetOrderSystem.IsBuildingEntity", selection);
        StringAssert.Contains("_unitTargetOrderSystem.ClearAccidentalAirSelectionMove", selection);
        StringAssert.Contains("_unitTargetOrderSystem.IssueAttackTarget", selection);
        StringAssert.Contains("_unitTargetOrderSystem.IssueDirectAttackTarget", selection);
        StringAssert.Contains("_unitTargetOrderSystem.ValidateAttackTarget", selection);
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+bool\s+TryFindRadarTargetForMissileLauncher\s*\("),
            "Radar target lookup belongs in UnitTargetOrderSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+bool\s+IsBuildingEntity\s*\("),
            "Target classification belongs in UnitTargetOrderSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"private\s+static\s+TacticalCommandResult\s+ValidateAttackTarget\s*\("),
            "Attack target validation belongs in UnitTargetOrderSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"new\s+EngageTarget\s*\{"),
            "Attack order component writes belong in UnitTargetOrderSystem, not RTSSelectionSystem.");
    }

    [Test]
    public void RtsSelectionSystemMustDelegateCameraStateSlice()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string cameraFile = "Assets/Game/Scripts/Systems/RtsCameraSystem.cs";
        Assert.IsTrue(File.Exists(cameraFile), "RTS camera drag and smooth-focus state must live in RtsCameraSystem.");

        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("RtsCameraSystem _rtsCameraSystem", selection);
        StringAssert.Contains("_rtsCameraSystem.ResetSession", selection);
        StringAssert.Contains("_rtsCameraSystem.ClearSmoothFocusTarget", selection);
        StringAssert.Contains("_rtsCameraSystem.UpdateSmoothFocus", selection);
        StringAssert.Contains("_rtsCameraSystem.SetSmoothFocusTarget", selection);
        StringAssert.Contains("_rtsCameraSystem.ResetCameraModeSession", selection);
        StringAssert.Contains("_rtsCameraSystem.UpdatePerspectiveZoom", selection);
        StringAssert.Contains("_rtsCameraSystem.UpdateFullscreenIsoZoom", selection);
        StringAssert.Contains("_rtsCameraSystem.ApplyPerspectiveCameraModeInstant", selection);
        StringAssert.Contains("_rtsCameraSystem.UpdatePerspectiveCameraMode", selection);
        StringAssert.Contains("_rtsCameraSystem.UpdateFullscreenIsoCameraMode", selection);
        Assert.IsFalse(
            Regex.IsMatch(selection, @"\bprivate\s+bool\s+_cameraDragging\s*;"),
            "Camera drag state belongs in RtsCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"\bprivate\s+bool\s+_hasSmoothCameraFocusTarget\s*;"),
            "Smooth camera focus ownership belongs in RtsCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"\bprivate\s+Vector3\s+_smoothCameraFocus(Target|Velocity)\s*;"),
            "Smooth camera focus vectors belong in RtsCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"\bprivate\s+bool\s+_(wasPlayRequested|wasBuildModeActive|isZoomTransitionActive|normalIsoModeActive)\s*;"),
            "Camera mode transition booleans belong in RtsCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"\bprivate\s+float\s+_(zoomTransitionVelocity|pitchTransitionVelocity|yawTransitionVelocity|fieldOfViewTransitionVelocity|orthographicSizeTransitionVelocity|fullscreenIsoTargetHeight|fullscreenIsoTargetOrthographicSize)\s*;"),
            "Camera transition numeric state belongs in RtsCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"worldCamera\.transform\.(position|rotation)\s*="),
            "Camera transform writes belong in RtsCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            Regex.IsMatch(selection, @"worldCamera\.(orthographic|fieldOfView|orthographicSize)\s*="),
            "Camera mode writes belong in RtsCameraSystem, not RTSSelectionSystem.");
        Assert.IsFalse(
            selection.Contains("worldCamera.ViewportPointToRay", StringComparison.Ordinal),
            "Ground-plane camera ray queries belong in RtsCameraSystem, not RTSSelectionSystem.");
    }

    [Test]
    public void RtsSelectionSystemMustDelegateInputStateSlice()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string inputFile = "Assets/Game/Scripts/Systems/RtsSelectionInputSystem.cs";
        Assert.IsTrue(File.Exists(inputFile), "RTS pointer, drag, suppression, and queued move input state must live in RtsSelectionInputSystem.");

        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("RtsSelectionInputSystem _rtsSelectionInputSystem", selection);
        StringAssert.Contains("_rtsSelectionInputSystem.BeginPointerPress", selection);
        StringAssert.Contains("_rtsSelectionInputSystem.QueueMoveOrder", selection);
        StringAssert.Contains("_rtsSelectionInputSystem.TryConsumeQueuedMoveOrder", selection);
        StringAssert.Contains("_rtsSelectionInputSystem.UpdateLastKnownPointerPosition", selection);
        StringAssert.Contains("_rtsSelectionInputSystem.CaptureUiClickSequence", selection);
        Assert.IsFalse(
            Regex.IsMatch(selection, @"\bprivate\s+(Vector2|bool|int|float|uint|Rect)\s+_(dragStart|dragCurrent|lastPointerPosition|pointerPressedOverUi|dragging|ignoreNextLeftMouseRelease|skipNextWorldReleaseAfterSelection|ignoreWorldCommandsUntilFrame|ignoreUiClickUntilRelease|selectionModeHoldArmed|selectionModeHoldStartTime|queuedMoveOrderToken|hasQueuedMoveOrder|queuedMoveOrderScreenPosition|queuedMoveOrderFrame|lastLiveSelectionRect|hasLiveSelectionRect|lastKnownPointerPosition|hasLastKnownPointerPosition)\s*(=|;)"),
            "RTS input/session state belongs in RtsSelectionInputSystem, not RTSSelectionSystem.");
    }

    [Test]
    public void RtsSelectionSystemMustUseRuntimeGameplayStateBoundary()
    {
        const string selectionFile = "Assets/Game/Scripts/UI/RTSSelectionSystem.cs";
        const string mainMenuPlayFile = "Assets/Game/Scripts/UI/MainMenuPlayUI.cs";
        const string menuViewFile = "Assets/Game/Scripts/UI/MenuView.cs";
        const string roadBuildFile = "Assets/Game/Scripts/UI/RoadBuildSystem.cs";
        const string buildingPlacementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string gameBootstrapFile = "Assets/Game/Scripts/Bootstrap/GameBootstrap.cs";
        const string stateSystemFile = "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs";
        const string stateComponentsFile = "Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs";
        Assert.IsTrue(File.Exists(stateSystemFile), "Runtime gameplay state access must go through RuntimeGameplayStateSystem.");
        Assert.IsTrue(File.Exists(stateComponentsFile), "Runtime gameplay state must have ECS singleton components.");

        string selection = File.ReadAllText(selectionFile);
        string mainMenuPlay = File.ReadAllText(mainMenuPlayFile);
        string menuView = File.ReadAllText(menuViewFile);
        string roadBuild = File.ReadAllText(roadBuildFile);
        string buildingPlacement = File.ReadAllText(buildingPlacementFile);
        string gameBootstrap = File.ReadAllText(gameBootstrapFile);
        string stateSystem = File.ReadAllText(stateSystemFile);
        string components = File.ReadAllText(stateComponentsFile);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", selection);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", mainMenuPlay);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", menuView);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", roadBuild);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", buildingPlacement);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", gameBootstrap);
        StringAssert.Contains("ResetForGameplayStart", stateSystem);
        StringAssert.Contains("_runtimeGameplayStateSystem.ResetForGameplayStart", gameBootstrap);
        StringAssert.Contains("RuntimeGameplayStateComponent : IComponentData", components);
        StringAssert.Contains("RuntimeCameraInputComponent : IComponentData", components);
        StringAssert.Contains("RuntimeCameraFocusRequestComponent : IComponentData", components);

        string[] migratedFields =
        {
            "PlayRequested",
            "InitialCameraFocusRequested",
            "InitialCameraFocusWorld",
            "SelectionModeActive",
            "BuildModeActive",
            "FullscreenMapOpen",
            "FullscreenMapIsoMode",
            "ZoomInHeld",
            "ZoomOutHeld",
            "SuppressNextWorldClick",
            "PlayerAutoModeEnabled"
        };

        foreach (string field in migratedFields)
        {
            Assert.IsFalse(
                selection.Contains($"InitialUnitsRuntimeState.{field}", StringComparison.Ordinal),
                $"RTSSelectionSystem must use RuntimeGameplayStateSystem for {field}.");
            Assert.IsFalse(
                mainMenuPlay.Contains($"InitialUnitsRuntimeState.{field}", StringComparison.Ordinal),
                $"MainMenuPlayUI must use RuntimeGameplayStateSystem for {field}.");
            Assert.IsFalse(
                menuView.Contains($"InitialUnitsRuntimeState.{field}", StringComparison.Ordinal),
                $"MenuView must use RuntimeGameplayStateSystem for {field}.");
            Assert.IsFalse(
                roadBuild.Contains($"InitialUnitsRuntimeState.{field}", StringComparison.Ordinal),
                $"RoadBuildSystem must use RuntimeGameplayStateSystem for {field}.");
            Assert.IsFalse(
                buildingPlacement.Contains($"InitialUnitsRuntimeState.{field}", StringComparison.Ordinal),
                $"BuildingPlacementSystem must use RuntimeGameplayStateSystem for {field}.");
            Assert.IsFalse(
                gameBootstrap.Contains($"InitialUnitsRuntimeState.{field}", StringComparison.Ordinal),
                $"GameBootstrap must use RuntimeGameplayStateSystem for {field}.");
        }

        string[] bootstrapStartResetAssignments =
        {
            "PlayRequested = true",
            "SelectionModeActive = false",
            "BuildModeActive = false",
            "FullscreenMapOpen = false",
            "FullscreenMapIsoMode = false",
            "ZoomInHeld = false",
            "ZoomOutHeld = false",
            "SuppressNextWorldClick = true",
            "InitialCameraFocusRequested = false"
        };

        foreach (string assignment in bootstrapStartResetAssignments)
        {
            Assert.IsFalse(
                gameBootstrap.Contains($"_runtimeGameplayStateSystem.{assignment}", StringComparison.Ordinal),
                $"Gameplay start state reset belongs in RuntimeGameplayStateSystem.ResetForGameplayStart, not GameBootstrap.");
        }
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string validationFile = "Assets/Game/Scripts/Systems/BuildingPlacementValidationSystem.cs";
        Assert.IsTrue(File.Exists(validationFile), "The building validation slice must live in BuildingPlacementValidationSystem.");

        string placement = File.ReadAllText(placementFile);
        string validation = File.ReadAllText(validationFile);
        StringAssert.Contains("BuildingPlacementValidationSystem.RebuildInvalidPrefix", placement);
        StringAssert.Contains("BuildingPlacementValidationSystem.IsPlacementRectValid", placement);
        StringAssert.Contains("_buildingPlacementValidationSystem.AreAllPendingWallRunsValid", placement);
        StringAssert.Contains("_buildingPlacementValidationSystem.AreWallPlacementOriginsValid", placement);
        StringAssert.Contains("_buildingPlacementValidationSystem.IsWallPlacementValid", placement);
        StringAssert.Contains("CreateWallValidationContext", placement);
        StringAssert.Contains("IsWallFootprintValid", validation);
        StringAssert.Contains("DoWallSegmentsConflict", validation);
        StringAssert.Contains("AreAllPendingWallRunsValid", validation);
        StringAssert.Contains("AreWallPlacementOriginsValid", validation);
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string runtimeBuildingFile = "Assets/Game/Scripts/Systems/RuntimeBuildingSystem.cs";
        const string runtimeCreationFile = "Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs";
        const string combatFile = "Assets/Game/Scripts/Systems/BuildingCombatSystem.cs";
        Assert.IsTrue(File.Exists(runtimeBuildingFile), "The runtime building registry slice must live in RuntimeBuildingSystem.");
        Assert.IsTrue(File.Exists(runtimeCreationFile), "Runtime building creation orchestration must live in BuildingRuntimeCreationSystem.");
        Assert.IsTrue(File.Exists(combatFile), "Runtime building destruction/removal orchestration must live in BuildingCombatSystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeCreation = File.ReadAllText(runtimeCreationFile);
        string combat = File.ReadAllText(combatFile);
        StringAssert.Contains("RuntimeBuildingSystem<RuntimeBuildingData>", placement);
        StringAssert.Contains("IReadOnlyDictionary<int, RuntimeBuildingData> _runtimeBuildings => _runtimeBuildingSystem.Buildings", placement);
        StringAssert.Contains("BuildingRuntimeCreationSystem _buildingRuntimeCreationSystem", placement);
        StringAssert.Contains("_buildingRuntimeCreationSystem.RegisterRuntimeBuilding", placement);
        StringAssert.Contains("CreateBuildingRuntimeCreationContext", placement);
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
            Regex.IsMatch(placement, @"new\s+RuntimeBuildingData\s*\{"),
            "Runtime building data creation belongs in BuildingRuntimeCreationSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+void\s+AttachRuntimeLink\b"),
            "Runtime building link attachment belongs in BuildingRuntimeCreationSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedRuntimeQuerySlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string runtimeQueryFile = "Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs";
        Assert.IsTrue(File.Exists(runtimeQueryFile), "Runtime building read/query behavior must live in BuildingRuntimeQuerySystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeQuery = File.ReadAllText(runtimeQueryFile);
        StringAssert.Contains("BuildingRuntimeQuerySystem _buildingRuntimeQuerySystem", placement);
        StringAssert.Contains("CreateBuildingRuntimeQueryContext", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.CountRuntimeBuildingsForFaction", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.CountRuntimeProducedUnitsForFaction", placement);
        StringAssert.Contains("_buildingRuntimeQuerySystem.CountPendingProductionsForFaction", placement);
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string definitionFile = "Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs";
        Assert.IsTrue(File.Exists(definitionFile), "Building definition and prefab metadata behavior must live in BuildingDefinitionSystem.");

        string placement = File.ReadAllText(placementFile);
        string definition = File.ReadAllText(definitionFile);

        StringAssert.Contains("BuildingDefinitionSystem _buildingDefinitionSystem", placement);
        StringAssert.Contains("_buildingDefinitionSystem.RebuildSpawnablesLookup", placement);
        StringAssert.Contains("_buildingDefinitionSystem.RebuildConfiguredSpawnableDefinitions", placement);
        StringAssert.Contains("_buildingDefinitionSystem.CreateRuntimeBuildingDefinition", placement);
        StringAssert.Contains("_buildingDefinitionSystem.TryGetConfiguredSpawnable", placement);
        StringAssert.Contains("_buildingDefinitionSystem.TryResolveConfiguredSpawnablePrefab", placement);
        StringAssert.Contains("_buildingDefinitionSystem.TryResolveConfiguredUnitSpawnPrefab", placement);
        StringAssert.Contains("BuildingDefinitionSystem.GetProductionPrefab", placement);
        StringAssert.Contains("BuildingDefinitionSystem.TryGetPrefabLocalBounds", placement);
        StringAssert.Contains("BuildingDefinitionSystem.RuntimeBuildingMatchesId", placement);
        StringAssert.Contains("BuildingDefinitionSystem.UnitPrefabMatchesId", placement);

        StringAssert.Contains("CachedRuntimeBuildingMetadata", definition);
        StringAssert.Contains("RebuildConfiguredSpawnableDefinitions", definition);
        StringAssert.Contains("CreateRuntimeBuildingDefinition", definition);
        StringAssert.Contains("CreateDefinition", definition);
        StringAssert.Contains("TryGetPrefabLocalBounds", definition);
        StringAssert.Contains("FindProductionSpawnLocalPositions", definition);
        StringAssert.Contains("RegisterSpawnableLookupAliases", definition);

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
            @"\bprivate\s+(?:static\s+)?bool\s+UnitPrefabMatchesId\b"
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string selectionFile = "Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs";
        Assert.IsTrue(File.Exists(selectionFile), "Building selection behavior must live in BuildingSelectionSystem.");

        string placement = File.ReadAllText(placementFile);
        string selection = File.ReadAllText(selectionFile);
        StringAssert.Contains("BuildingSelectionSystem _buildingSelectionSystem", placement);
        StringAssert.Contains("CreateBuildingSelectionContext", placement);
        StringAssert.Contains("_buildingSelectionSystem.ClearSelectedBuilding", placement);
        StringAssert.Contains("_buildingSelectionSystem.SelectAndFocusBuilding", placement);
        StringAssert.Contains("_buildingSelectionSystem.ResolveBuildingFocusWorldPosition", placement);
        StringAssert.Contains("_buildingSelectionSystem.HandleBuildingSelectionClick", placement);

        StringAssert.Contains("SelectAndFocusBuilding", selection);
        StringAssert.Contains("ResolveBuildingFocusWorldPosition", selection);
        StringAssert.Contains("HandleBuildingSelectionClick", selection);
        StringAssert.Contains("TryAssignSelectedHaulerOrders", selection);
        StringAssert.Contains("TryIssueMoveOrderToBuilding", selection);
        StringAssert.Contains("IsBoardablePlayerTransportClick", selection);

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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string visualFile = "Assets/Game/Scripts/Systems/BuildingVisualSystem.cs";
        Assert.IsTrue(File.Exists(visualFile), "The building visual slice must live in BuildingVisualSystem.");

        string placement = File.ReadAllText(placementFile);
        StringAssert.Contains("BuildingVisualSystem _buildingVisualSystem", placement);
        StringAssert.Contains("_buildingVisualSystem.FindDescendantByName", placement);
        StringAssert.Contains("_buildingVisualSystem.SetTransformVisible", placement);
        StringAssert.Contains("_buildingVisualSystem.ApplyMarkerColor", placement);
        StringAssert.Contains("_buildingVisualSystem.FindAnimatedBuildingParts", placement);
        StringAssert.Contains("_buildingVisualSystem.UpdateAnimatedBuildingParts", placement);
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
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedCombatSlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string combatFile = "Assets/Game/Scripts/Systems/BuildingCombatSystem.cs";
        Assert.IsTrue(File.Exists(combatFile), "The building combat slice must live in BuildingCombatSystem.");

        string placement = File.ReadAllText(placementFile);
        string combat = File.ReadAllText(combatFile);
        StringAssert.Contains("BuildingCombatSystem _buildingCombatSystem", placement);
        StringAssert.Contains("_buildingCombatSystem.DeleteBuilding", placement);
        StringAssert.Contains("_buildingCombatSystem.HandleRuntimeBuildingEntityDestroyed", placement);
        StringAssert.Contains("_buildingCombatSystem.UpdateDestroyedBuildings", placement);
        StringAssert.Contains("_buildingCombatSystem.SyncDestroyedRuntimeBuildingCombatEntities", placement);

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
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedResourceSlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string resourceFile = "Assets/Game/Scripts/Systems/FactionResourceSystem.cs";
        Assert.IsTrue(File.Exists(resourceFile), "The faction resource slice must live in FactionResourceSystem.");

        string placement = File.ReadAllText(placementFile);
        StringAssert.Contains("FactionResourceSystem _factionResourceSystem", placement);
        StringAssert.Contains("_factionResourceSystem.TryGetPrimaryCapacityInfo", placement);
        StringAssert.Contains("_factionResourceSystem.TryGetFuelCapacityInfo", placement);
        StringAssert.Contains("_factionResourceSystem.GetResourceTotals", placement);
        StringAssert.Contains("_factionResourceSystem.TryGetFactionResourceEconomy", placement);
        StringAssert.Contains("_factionResourceSystem.DrainFactionResource", placement);
        StringAssert.Contains("_factionResourceSystem.UpdateResourceProduction", placement);
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string haulerFile = "Assets/Game/Scripts/Systems/ResourceHaulerSystem.cs";
        Assert.IsTrue(File.Exists(haulerFile), "The resource hauler slice must live in ResourceHaulerSystem.");

        string placement = File.ReadAllText(placementFile);
        StringAssert.Contains("ResourceHaulerSystem _resourceHaulerSystem", placement);
        StringAssert.Contains("_resourceHaulerSystem.IsOilSourceBuilding", placement);
        StringAssert.Contains("_resourceHaulerSystem.IsFuelBuilding", placement);
        StringAssert.Contains("_resourceHaulerSystem.HasAvailableFuelForHauler", placement);
        StringAssert.Contains("_resourceHaulerSystem.CreateOrder", placement);
        StringAssert.Contains("_resourceHaulerSystem.SetTravelPhase", placement);
        StringAssert.Contains("_resourceHaulerSystem.SetPhase", placement);
        StringAssert.Contains("_resourceHaulerSystem.AdvanceTimedAction", placement);
        StringAssert.Contains("_resourceHaulerSystem.ResetActionTimer", placement);
        StringAssert.Contains("_resourceHaulerSystem.GetLoadAmount", placement);
        StringAssert.Contains("_resourceHaulerSystem.GetCargo", placement);
        StringAssert.Contains("_resourceHaulerSystem.TryCompleteLoad", placement);
        StringAssert.Contains("_resourceHaulerSystem.RevertLoad", placement);
        StringAssert.Contains("_resourceHaulerSystem.HasReceivingCapacity", placement);
        StringAssert.Contains("_resourceHaulerSystem.TryCompleteUnload", placement);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+IsOilSourceBuilding\b"),
            "Hauler oil source classification belongs in ResourceHaulerSystem, not BuildingPlacementSystem.");
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
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedProductionSlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string productionFile = "Assets/Game/Scripts/Systems/BuildingProductionSystem.cs";
        const string productionSlotFile = "Assets/Game/Scripts/Systems/BuildingProductionSlotSystem.cs";
        const string productionTransportFile = "Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs";
        const string spawnFile = "Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs";
        const string spawnPrefabFile = "Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs";
        const string runwayFile = "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs";
        const string runtimeQueryFile = "Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs";
        Assert.IsTrue(File.Exists(productionFile), "The building production slice must live in BuildingProductionSystem.");
        Assert.IsTrue(File.Exists(productionSlotFile), "The production slot slice must live in BuildingProductionSlotSystem.");
        Assert.IsTrue(File.Exists(productionTransportFile), "The active production transport visual/update slice must live in BuildingProductionTransportSystem.");
        Assert.IsTrue(File.Exists(spawnFile), "The produced-unit spawn slice must live in BuildingSpawnSystem.");
        Assert.IsTrue(File.Exists(spawnPrefabFile), "The spawn prefab/entity resolution slice must live in BuildingSpawnPrefabSystem.");
        Assert.IsTrue(File.Exists(runwayFile), "The runway slice must live in BuildingRunwaySystem.");
        Assert.IsTrue(File.Exists(runtimeQueryFile), "Produced-unit count read models must delegate pruning through BuildingRuntimeQuerySystem.");

        string placement = File.ReadAllText(placementFile);
        string runtimeQuery = File.ReadAllText(runtimeQueryFile);
        StringAssert.Contains("BuildingProductionSystem _buildingProductionSystem", placement);
        StringAssert.Contains("BuildingProductionSlotSystem _buildingProductionSlotSystem", placement);
        StringAssert.Contains("BuildingProductionTransportSystem _buildingProductionTransportSystem", placement);
        StringAssert.Contains("BuildingSpawnSystem _buildingSpawnSystem", placement);
        StringAssert.Contains("BuildingSpawnPrefabSystem _buildingSpawnPrefabSystem", placement);
        StringAssert.Contains("BuildingRunwaySystem _buildingRunwaySystem", placement);
        StringAssert.Contains("_buildingProductionSystem.TryQueuePlayerUnitFromBuilding", placement);
        StringAssert.Contains("_buildingProductionSystem.GetProgress", placement);
        StringAssert.Contains("_buildingProductionSystem.ShouldLaunchTransport", placement);
        StringAssert.Contains("_buildingProductionSystem.DelayPendingProduction", placement);
        StringAssert.Contains("_buildingProductionSystem.IsReady", placement);
        StringAssert.Contains("_buildingProductionSystem.IsReadyWithin", placement);
        StringAssert.Contains("context.ProductionSystem?.PruneProducedUnits", runtimeQuery);
        StringAssert.Contains("_buildingProductionSystem.RemovePendingAt", placement);

        string production = File.ReadAllText(productionFile);
        StringAssert.Contains("TryQueuePlayerUnitFromBuilding", production);
        StringAssert.Contains("building.PendingProductions.Add", production);
        StringAssert.Contains("context.ProductionSlotSystem?.TryReserveProductionSlot", production);
        StringAssert.Contains("ProductionTransportSettings", production);
        StringAssert.Contains("ResolveProductionTransportSettings", production);
        StringAssert.Contains("ResolveProductionDurationSeconds", production);
        StringAssert.Contains("TryResolveDefaultProductionTransportPrefab", production);

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
        StringAssert.Contains("context.ProductionSystem.FindNextReadyTransportPending", productionTransport);
        StringAssert.Contains("context.ProductionSystem.FindNextSoonTransportPending", productionTransport);
        StringAssert.Contains("context.ProductionSystem.RemovePendingProduction", productionTransport);

        string spawn = File.ReadAllText(spawnFile);
        StringAssert.Contains("TrySpawnPlayerUnitNearBuilding", spawn);
        StringAssert.Contains("TryResolveAvailableFactionHelipadSpawn", spawn);
        StringAssert.Contains("TryFindStrictSpawnCell", spawn);
        StringAssert.Contains("ReserveDynamicOccupancy", spawn);

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
            Regex.IsMatch(placement, @"PendingProductions\.Add"),
            "Pending production queue append belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"pending\.StartedAt\s*\+="),
            "Pending production delay mutation belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"pending\.ReadyAt\s*\+="),
            "Pending production delay mutation belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
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
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryResolveHelicopterSpawnForFaction\b"),
            "Helipad spawn fallback belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryFindStrictSpawnCell\b"),
            "Strict spawn-cell search belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bprivate\s+(?:static\s+)?bool\s+TryFindStrictSpawnCellAdjacentToBuilding\b"),
            "Adjacent spawn-cell search belongs in BuildingSpawnSystem, not BuildingPlacementSystem.");
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string previewFile = "Assets/Game/Scripts/Systems/BuildingPlacementPreviewSystem.cs";
        Assert.IsTrue(File.Exists(previewFile), "The placement preview slice must live in BuildingPlacementPreviewSystem.");

        string placement = File.ReadAllText(placementFile);
        string preview = File.ReadAllText(previewFile);
        StringAssert.Contains("BuildingPlacementPreviewSystem _buildingPlacementPreviewSystem", placement);
        StringAssert.Contains("_buildingPlacementPreviewSystem.Init", placement);
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
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string commitFile = "Assets/Game/Scripts/Systems/BuildingPlacementCommitSystem.cs";
        Assert.IsTrue(File.Exists(commitFile), "The placement commit slice must live in BuildingPlacementCommitSystem.");

        string placement = File.ReadAllText(placementFile);
        string commit = File.ReadAllText(commitFile);
        StringAssert.Contains("BuildingPlacementCommitSystem _buildingPlacementCommitSystem", placement);
        StringAssert.Contains("_buildingPlacementCommitSystem.CommitPlacement", placement);
        StringAssert.Contains("BuildingPlacementCommitSystem.CommitRequest", placement);
        StringAssert.Contains("BuildingPlacementCommitSystem.CommitContext", placement);

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
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedInputSlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string inputFile = "Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs";
        Assert.IsTrue(File.Exists(inputFile), "The placement input slice must live in BuildingPlacementInputSystem.");

        string placement = File.ReadAllText(placementFile);
        string input = File.ReadAllText(inputFile);
        StringAssert.Contains("BuildingPlacementInputSystem _buildingPlacementInputSystem", placement);
        StringAssert.Contains("_buildingPlacementInputSystem.TryBeginDrag", placement);
        StringAssert.Contains("_buildingPlacementInputSystem.HandlePointerRelease", placement);
        StringAssert.Contains("_buildingPlacementInputSystem.ApplyPointerHover", placement);
        StringAssert.Contains("_buildingPlacementInputSystem.BuildWallPlacementOrigins", placement);

        StringAssert.Contains("TryBeginDrag", input);
        StringAssert.Contains("ApplyPointerHover", input);
        StringAssert.Contains("IsPointerOverPlacement", input);
        StringAssert.Contains("BuildWallPlacementOrigins", input);
        StringAssert.Contains("BuildFinalWallRuns", input);
        StringAssert.Contains("CommitCurrentWallRun", input);
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
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedUiQuerySlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string placementQueryFile = "Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs";
        const string uiQueryFile = "Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs";
        Assert.IsTrue(File.Exists(placementQueryFile), "The selected-building scalar query slice must live in BuildingPlacementQuerySystem.");
        Assert.IsTrue(File.Exists(uiQueryFile), "The temporary building UI read model slice must live in BuildingUiQuerySystem.");

        string placement = File.ReadAllText(placementFile);
        string placementQuery = File.ReadAllText(placementQueryFile);
        StringAssert.Contains("BuildingPlacementQuerySystem _buildingPlacementQuerySystem", placement);
        StringAssert.Contains("CreateBuildingPlacementQueryContext", placement);
        StringAssert.Contains("_buildingPlacementQuerySystem.GetSelectedBuildingProductionPrefab", placement);
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

        StringAssert.Contains("BuildingUiQuerySystem _buildingUiQuerySystem", placement);
        StringAssert.Contains("_buildingUiQuerySystem.GetProducedUnits", placement);
        StringAssert.Contains("_buildingUiQuerySystem.AddProducedUnitEntries", placement);
        StringAssert.Contains("_buildingUiQuerySystem.AddPendingProductionUiEntries", placement);
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
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedBarrierSlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string barrierFile = "Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs";
        Assert.IsTrue(File.Exists(barrierFile), "The road barrier and base-breach slice must live in BuildingBarrierSystem.");

        string placement = File.ReadAllText(placementFile);
        string barrier = File.ReadAllText(barrierFile);
        StringAssert.Contains("BuildingBarrierSystem _buildingBarrierSystem", placement);
        StringAssert.Contains("CreateBuildingBarrierContext", placement);
        StringAssert.Contains("_buildingBarrierSystem.UpdateRoadBarrierDoors", placement);
        StringAssert.Contains("_buildingBarrierSystem.RememberOpenBaseBreach", placement);
        StringAssert.Contains("_buildingBarrierSystem.TryFindEnemyWallPerimeterContainingCell", placement);
        StringAssert.Contains("_buildingBarrierSystem.TryFindBreachBuilding", placement);
        StringAssert.Contains("_buildingBarrierSystem.GetRuntimeRoadBarrierGateRects", placement);
        StringAssert.Contains("_buildingBarrierSystem.SetBarrierDoorOpen01", placement);
        StringAssert.Contains("_buildingBarrierSystem.ShouldAlignGateToNearbyWall", placement);
        StringAssert.Contains("BuildingBarrierSystem.IsWallGateDefinition", placement);
        StringAssert.Contains("BuildingBarrierSystem.IsLinearWallDefinition", placement);

        StringAssert.Contains("RememberOpenBaseBreach", barrier);
        StringAssert.Contains("HasOpenBaseBreach", barrier);
        StringAssert.Contains("TryFindEnemyWallPerimeterContainingCell", barrier);
        StringAssert.Contains("TryFindBreachBuilding", barrier);
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
            Regex.IsMatch(placement, @"\bprivate\s+void\s+UpdateRoadBarrierDoors\b"),
            "Road barrier door polling belongs in BuildingBarrierSystem, not BuildingPlacementSystem.");
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
