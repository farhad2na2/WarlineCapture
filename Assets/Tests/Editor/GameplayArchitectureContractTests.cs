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
        StringAssert.Contains("Managed gameplay runtime update extraction is paused", contract);
        StringAssert.Contains("The retired `AILog` facade must not be reintroduced", contract);
        StringAssert.Contains("`BuildingPlacementSystem` is legacy facade debt", contract);
        StringAssert.Contains("validity belong in `BuildingPlacementValidationSystem`", contract);
        StringAssert.Contains("registry ownership, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`", contract);
        StringAssert.Contains("animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`", contract);
        StringAssert.Contains("destruction state, cleanup timing, blocker cleanup, and combat-health destruction checks belong in `BuildingCombatSystem`", contract);
        StringAssert.Contains("Resource storage classification, capacity display math, resource totals, faction economy snapshots, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`", contract);
        StringAssert.Contains("Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`", contract);
        StringAssert.Contains("Unit production queue item initialization, pending production timing/progress, readiness checks, produced-unit liveness pruning, production slot reservation, pending queue removal, ready/soon transport-pending lookup, and transport launch delay math belong in `BuildingProductionSystem`", contract);
        StringAssert.Contains("Produced-unit UI lists, pending-production UI entries, UI progress shaping, and temporary building UI read models belong in `BuildingUiQuerySystem`", contract);
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

        StringAssert.Contains("_missionStartupSystem.UpdateActiveMission", bootstrap);
        StringAssert.Contains("_missionStartupSystem.ApplyM01ProductionCameraPoseIfActive", bootstrap);

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
    public void ManagedRuntimeUpdateLoopExtractionMustStayPausedUntilPerformanceContractExists()
    {
        const string runtimeUpdateFile = "Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs";
        Assert.IsFalse(File.Exists(runtimeUpdateFile), "Do not restore GameplayRuntimeUpdateSystem until a focused FPS regression contract exists.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("GameRuntimeStats.RecordMissionElapsed", bootstrap);
        StringAssert.Contains("_missionStartupSystem.UpdateActiveMission", bootstrap);
        StringAssert.Contains("_missionStartupSystem.ApplyM01ProductionCameraPoseIfActive", bootstrap);
        StringAssert.Contains("WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene", bootstrap);
        StringAssert.Contains("IsGameplayStartComplete", bootstrap);
    }

    [Test]
    public void GameBootstrapMustDelegateBroadSceneLookupAndUiRuntimeBinding()
    {
        const string sceneBindingFile = "Assets/Game/Scripts/Systems/GameplaySceneBindingSystem.cs";
        Assert.IsTrue(File.Exists(sceneBindingFile), "Broad scene lookup and UI runtime binding must live outside GameBootstrap.");

        string bootstrap = File.ReadAllText(GameBootstrapPath);
        StringAssert.Contains("GameplaySceneBindingSystem _gameplaySceneBindingSystem", bootstrap);
        StringAssert.Contains("_gameplaySceneBindingSystem.BindGameplayUiRuntimeDependencies", bootstrap);

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
        StringAssert.Contains("BuildingPlacementValidationSystem.RebuildInvalidPrefix", placement);
        StringAssert.Contains("BuildingPlacementValidationSystem.IsPlacementRectValid", placement);
        StringAssert.Contains("BuildingPlacementValidationSystem.IsWallFootprintValid", placement);
        StringAssert.Contains("BuildingPlacementValidationSystem.DoWallSegmentsConflict", placement);
        Assert.IsFalse(
            placement.Contains("private static bool DoWallSegmentsConflict", StringComparison.Ordinal),
            "Wall segment conflict rules belong in BuildingPlacementValidationSystem, not BuildingPlacementSystem.");
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateRuntimeBuildingRegistrySlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string runtimeBuildingFile = "Assets/Game/Scripts/Systems/RuntimeBuildingSystem.cs";
        Assert.IsTrue(File.Exists(runtimeBuildingFile), "The runtime building registry slice must live in RuntimeBuildingSystem.");

        string placement = File.ReadAllText(placementFile);
        StringAssert.Contains("RuntimeBuildingSystem<RuntimeBuildingData>", placement);
        StringAssert.Contains("IReadOnlyDictionary<int, RuntimeBuildingData> _runtimeBuildings => _runtimeBuildingSystem.Buildings", placement);
        StringAssert.Contains("_runtimeBuildingSystem.AllocateId()", placement);
        StringAssert.Contains("_runtimeBuildingSystem.AddBuilding", placement);
        StringAssert.Contains("_runtimeBuildingSystem.RemoveBuilding", placement);
        StringAssert.Contains("_runtimeBuildingSystem.SelectBuilding", placement);
        StringAssert.Contains("_runtimeBuildingSystem.ClearSelection", placement);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bint\s+_nextBuildingId\b"),
            "Runtime building id allocation belongs in RuntimeBuildingSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bint\?\s+_selectedBuildingId\b|\bint\?\s+_activeBuildingId\b"),
            "Active/selected runtime building ids belong in RuntimeBuildingSystem, not BuildingPlacementSystem.");
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
        StringAssert.Contains("BuildingCombatSystem _buildingCombatSystem", placement);
        StringAssert.Contains("_buildingCombatSystem.TryMarkDestroyed", placement);
        StringAssert.Contains("_buildingCombatSystem.CollectDestroyedCleanupIds", placement);
        StringAssert.Contains("_buildingCombatSystem.ResolveRuntimeCombatState", placement);
        StringAssert.Contains("_buildingCombatSystem.DestroyBlockerEntity", placement);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bbuilding\.IsDestroyed\s*=\s*true\b"),
            "Destroyed state mutation belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
        Assert.IsFalse(
            Regex.IsMatch(placement, @"\bbuilding\.DestroyedCleanupAt\s*="),
            "Destroyed cleanup timing belongs in BuildingCombatSystem, not BuildingPlacementSystem.");
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
        Assert.IsTrue(File.Exists(productionFile), "The building production slice must live in BuildingProductionSystem.");

        string placement = File.ReadAllText(placementFile);
        StringAssert.Contains("BuildingProductionSystem _buildingProductionSystem", placement);
        StringAssert.Contains("_buildingProductionSystem.InitializePendingProduction", placement);
        StringAssert.Contains("_buildingProductionSystem.GetProgress", placement);
        StringAssert.Contains("_buildingProductionSystem.ShouldLaunchTransport", placement);
        StringAssert.Contains("_buildingProductionSystem.DelayPendingProduction", placement);
        StringAssert.Contains("_buildingProductionSystem.IsReady", placement);
        StringAssert.Contains("_buildingProductionSystem.IsReadyWithin", placement);
        StringAssert.Contains("_buildingProductionSystem.PruneProducedUnits", placement);
        StringAssert.Contains("_buildingProductionSystem.TryReserveProductionSlot", placement);
        StringAssert.Contains("_buildingProductionSystem.FindNextReadyTransportPending", placement);
        StringAssert.Contains("_buildingProductionSystem.FindNextSoonTransportPending", placement);
        StringAssert.Contains("_buildingProductionSystem.RemovePendingProduction", placement);
        StringAssert.Contains("_buildingProductionSystem.RemovePendingAt", placement);
        Assert.IsFalse(
            Regex.IsMatch(placement, @"new\s+RuntimeBuildingData\.PendingProduction\s*\{"),
            "Pending production initialization belongs in BuildingProductionSystem, not BuildingPlacementSystem.");
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
            "Production slot pending-reservation checks belong in BuildingProductionSystem, not BuildingPlacementSystem.");
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
    }

    [Test]
    public void BuildingPlacementSystemMustDelegateExtractedUiQuerySlice()
    {
        const string placementFile = "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs";
        const string uiQueryFile = "Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs";
        Assert.IsTrue(File.Exists(uiQueryFile), "The temporary building UI read model slice must live in BuildingUiQuerySystem.");

        string placement = File.ReadAllText(placementFile);
        StringAssert.Contains("BuildingUiQuerySystem _buildingUiQuerySystem", placement);
        StringAssert.Contains("_buildingUiQuerySystem.GetProducedUnits", placement);
        StringAssert.Contains("_buildingUiQuerySystem.AddProducedUnitEntries", placement);
        StringAssert.Contains("_buildingUiQuerySystem.AddPendingProductionUiEntries", placement);
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
