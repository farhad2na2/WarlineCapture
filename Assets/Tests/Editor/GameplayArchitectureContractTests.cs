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
        "Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs",
        "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs",
        "Assets/Game/Scripts/UI/MainMenuPlayUI.cs",
        "Assets/Game/Scripts/UI/RoadBuildSystem.cs",
        "Assets/Game/Scripts/UI/RTSSelectionSystem.cs"
    };

    private static readonly string[] LegacyStaticDependencyLocatorFiles =
    {
        "Assets/Game/Scripts/UI/BuildingPlacementSystem.cs",
        "Assets/Game/Scripts/UI/RoadBuildSystem.cs",
        "Assets/Game/Scripts/UI/RTSSelectionSystem.cs"
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
        StringAssert.Contains("New domain gameplay types should end in `Entity`, `Component`, or `System`", contract);
        StringAssert.Contains("`*View` are serialized-reference binders only", contract);
        StringAssert.Contains("Existing `AILog` usage is grandfathered as migration debt", contract);
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
            "Do not add new static dependency locator helpers. Pass dependencies through bootstrap/installer composition, command/query ports, or ECS data instead:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, newViolations));
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
