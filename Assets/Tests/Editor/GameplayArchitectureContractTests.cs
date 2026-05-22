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
        "Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs",
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

    private static readonly string[] LegacyStaticInstanceFiles =
    {
        "Assets/Game/Scripts/Authorings/FactionVisualSettingsAuthoring.cs",
        "Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs",
        "Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs"
    };

    private static readonly string[] LegacyStaticDependencyLocatorFiles = Array.Empty<string>();

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
        StringAssert.Contains("New domain gameplay types should end in `Entity`, `Component`, or `System`", contract);
        StringAssert.Contains("`*View` are serialized-reference binders only", contract);
        StringAssert.Contains("Existing `AILog` usage is grandfathered as migration debt", contract);
        StringAssert.Contains("`BuildingPlacementSystem` is legacy facade debt", contract);
        StringAssert.Contains("validity belong in `BuildingPlacementValidationSystem`", contract);
        StringAssert.Contains("registry ownership, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`", contract);
        StringAssert.Contains("animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`", contract);
        StringAssert.Contains("destruction state, cleanup timing, blocker cleanup, and combat-health destruction checks belong in `BuildingCombatSystem`", contract);
        StringAssert.Contains("Resource storage classification, capacity display math, resource totals, faction economy snapshots, and sell/drain behavior belong in `FactionResourceSystem`", contract);
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
