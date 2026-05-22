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
    private const string ScriptsRoot = "Assets/Game/Scripts";
    private const string BootstrapRoot = "Assets/Game/Scripts/Bootstrap";
    private const string ScenesRoot = "Assets/Game/Scripts/Scenes";

    private static readonly string[] LegacyAILogCallFiles =
    {
        "Assets/Game/Scripts/Bootstrap/GameBootstrap.cs",
        "Assets/Game/Scripts/Systems/AICombatOrderSystem.cs",
        "Assets/Game/Scripts/Systems/AIEconomySystem.cs",
        "Assets/Game/Scripts/Systems/AIFactionControlSystem.cs",
        "Assets/Game/Scripts/Systems/AIProductionSystem.cs",
        "Assets/Game/Scripts/Systems/AISquadSystem.cs",
        "Assets/Game/Scripts/Systems/AITargetingSystem.cs"
    };

    private static readonly string[] HotAILogCallFiles =
    {
        "Assets/Game/Scripts/Systems/AICombatOrderSystem.cs",
        "Assets/Game/Scripts/Systems/AIEconomySystem.cs",
        "Assets/Game/Scripts/Systems/AIFactionControlSystem.cs",
        "Assets/Game/Scripts/Systems/AIProductionSystem.cs",
        "Assets/Game/Scripts/Systems/AISquadSystem.cs",
        "Assets/Game/Scripts/Systems/AITargetingSystem.cs"
    };

    private static readonly string[] LegacyStaticLogFacadeFiles =
    {
        "Assets/Game/Scripts/Systems/AILog.cs",
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
        StringAssert.Contains("Existing `AILog` usage is grandfathered as migration debt", contract);
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
    public void NewBootstrapRootFilesMustUseInstallerOrServiceNaming()
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
                if (!typeName.EndsWith("Installer", StringComparison.Ordinal) &&
                    !typeName.EndsWith("Service", StringComparison.Ordinal) &&
                    !typeName.EndsWith("Registry", StringComparison.Ordinal) &&
                    !typeName.EndsWith("Config", StringComparison.Ordinal))
                {
                    violations.Add($"{file} declares '{typeName}'. New bootstrap root types must be installers, services, registries, or configs.");
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
    public void SceneStartupInstallersMustNotHardcodeMissionOrRoutePolicy()
    {
        if (!Directory.Exists(ScenesRoot))
            Assert.Pass("No scene startup installer folder exists.");

        string[] installerFiles = Directory.GetFiles(ScenesRoot, "*Installer.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .ToArray();

        List<string> violations = new();
        foreach (string file in installerFiles)
        {
            string text = File.ReadAllText(file);
            if (text.Contains("ChapterOneMissionCatalog.", StringComparison.Ordinal))
                violations.Add($"{file} hardcodes a mission catalog id. Scene startup installers must read mission identity from config.");
            if (Regex.IsMatch(text, @"WarlineCaptureRoute\.[A-Za-z0-9_]+"))
                violations.Add($"{file} hardcodes a route value. Scene startup installers must read route policy from config.");
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
            "Do not add new static dependency locator helpers. Pass dependencies through bootstrap/installer composition or ECS request/response components instead:" +
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
            "Do not add new gameplay/UI-flow Controller classes. Use View for serialized references and move behavior into ECS systems, services, or shell installers:" +
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
            "BuildingPlacementSystem dependencies must be supplied by GameBootstrap/installer composition, not reacquired through runtime singleton Instance calls:" +
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
        string components = File.ReadAllText(stateComponentsFile);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", selection);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", mainMenuPlay);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", menuView);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", roadBuild);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", buildingPlacement);
        StringAssert.Contains("RuntimeGameplayStateSystem _runtimeGameplayStateSystem", gameBootstrap);
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

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
#endif
