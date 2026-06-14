#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class NonEcsSystemConversionArchitectureTests
{
    private const string GameScriptsRoot = "Assets/Game/Scripts";
    private const string InventoryPath = "Design/Architecture/non_ecs_to_ecs_system_inventory.md";

    private static readonly Regex TypeDeclarationRegex = new(
        @"^[ \t]*(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly)\s+)*(?<kind>class|struct)\s+(?<name>[A-Za-z_]\w*)\s*(?<bases>:[^{;\r\n]+)?",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex EcsSystemBaseRegex = new(
        @"\b(ISystem|SystemBase|ComponentSystemBase|ComponentSystem|JobComponentSystem)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex MonoBehaviourBaseRegex = new(
        @"\b(MonoBehaviour|UnityEngine\.MonoBehaviour)\b",
        RegexOptions.CultureInvariant);

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new NonEcsSystemConversionArchitectureTests();
            tests.RuntimeSystemInventoryCanBeEnumerated();
            tests.GeneratedInventoryContainsEveryRuntimeNonEcsSystem();
            Debug.Log("[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[NonEcsSystemConversionArchitectureValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void RuntimeSystemInventoryCanBeEnumerated()
    {
        List<SystemDeclaration> declarations = EnumerateSystemDeclarations().ToList();
        List<SystemDeclaration> ecsSystems = declarations.Where(IsUnityEcsSystem).ToList();
        List<SystemDeclaration> monoBehaviours = declarations.Where(IsMonoBehaviour).ToList();
        List<SystemDeclaration> editorSystems = declarations.Where(IsEditorOnlyPath).ToList();
        List<SystemDeclaration> conversionDenominator = declarations
            .Where(declaration => !IsUnityEcsSystem(declaration))
            .Where(declaration => !IsMonoBehaviour(declaration))
            .Where(declaration => !IsEditorOnlyPath(declaration))
            .ToList();

        Debug.Log(
            "[NonEcsSystemInventory] " +
            $"totalSystemDeclarations={declarations.Count} " +
            $"unityEcs={ecsSystems.Count} " +
            $"monoBehaviour={monoBehaviours.Count} " +
            $"editorOnly={editorSystems.Count} " +
            $"runtimeNonEcsDenominator={conversionDenominator.Count}");
        Debug.Log("[NonEcsSystemInventory] editorOnlySystems:\n" + FormatDeclarations(editorSystems));
        Debug.Log("[NonEcsSystemInventory] firstWaveCandidates:\n" + FormatFirstWaveCandidates(conversionDenominator));

        Assert.Greater(
            declarations.Count,
            0,
            "The non-ECS system conversion inventory should find runtime `*System` declarations.");
        Assert.Greater(
            conversionDenominator.Count,
            0,
            "The conversion denominator should contain current plain runtime non-ECS `*System` declarations.");
    }

    [Test]
    public void GeneratedInventoryContainsEveryRuntimeNonEcsSystem()
    {
        Assert.IsTrue(
            File.Exists(InventoryPath),
            $"The non-ECS system conversion inventory is missing at `{InventoryPath}`.");

        HashSet<string> current = EnumerateSystemDeclarations()
            .Where(declaration => !IsUnityEcsSystem(declaration))
            .Where(declaration => !IsMonoBehaviour(declaration))
            .Where(declaration => !IsEditorOnlyPath(declaration))
            .Select(ToInventoryKey)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> inventoried = ParseInventoryRows(File.ReadAllLines(InventoryPath));

        string[] missing = current.Except(inventoried, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] stale = inventoried.Except(current, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();

        Assert.IsEmpty(
            missing,
            "Every plain runtime non-ECS `*System` must be present in the generated conversion inventory. Missing:\n" +
            string.Join(Environment.NewLine, missing));
        Assert.IsEmpty(
            stale,
            "The generated conversion inventory contains stale rows that no longer match runtime non-ECS `*System` declarations:\n" +
            string.Join(Environment.NewLine, stale));
    }

    private static IEnumerable<SystemDeclaration> EnumerateSystemDeclarations()
    {
        foreach (string path in EnumerateSourceFiles(GameScriptsRoot))
        {
            string text = File.ReadAllText(path);
            foreach (Match match in TypeDeclarationRegex.Matches(text))
            {
                string name = match.Groups["name"].Value;
                if (!name.EndsWith("System", StringComparison.Ordinal))
                    continue;

                yield return new SystemDeclaration(
                    NormalizePath(path),
                    name,
                    match.Groups["bases"].Success ? match.Groups["bases"].Value.TrimStart(':').Trim() : string.Empty);
            }
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        return Directory.Exists(root)
            ? Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)
            : Array.Empty<string>();
    }

    private static bool IsUnityEcsSystem(SystemDeclaration declaration)
    {
        return EcsSystemBaseRegex.IsMatch(declaration.Bases);
    }

    private static bool IsMonoBehaviour(SystemDeclaration declaration)
    {
        return MonoBehaviourBaseRegex.IsMatch(declaration.Bases);
    }

    private static bool IsEditorOnlyPath(SystemDeclaration declaration)
    {
        return declaration.Path.Contains("/Editor/", StringComparison.Ordinal);
    }

    private static string FormatFirstWaveCandidates(IReadOnlyCollection<SystemDeclaration> conversionDenominator)
    {
        string[] firstWave =
        {
            "SelectionMoveCommandRequestSystem",
            "SelectedMoveOrderCommandSystem",
            "SelectionAttackCommandRequestSystem",
            "AttackOrderCommandSystem",
            "SelectionScanCommandRequestSystem",
            "ScanIntelCommandSystem",
            "TransportBoardingCommandSystem",
            "UnitTransportRopeDisembarkCommandSystem",
            "BuildingTargetMoveOrderSystem",
            "CitizenMovementCommandSystem"
        };

        Dictionary<string, SystemDeclaration> byName = conversionDenominator
            .GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        return string.Join(
            Environment.NewLine,
            firstWave.Select(name => byName.TryGetValue(name, out SystemDeclaration declaration)
                ? $"{declaration.Path}: {declaration.Name}"
                : $"missing: {name}"));
    }

    private static string FormatDeclarations(IReadOnlyCollection<SystemDeclaration> declarations)
    {
        if (declarations.Count == 0)
            return "(none)";

        return string.Join(
            Environment.NewLine,
            declarations
                .OrderBy(declaration => declaration.Path, StringComparer.Ordinal)
                .ThenBy(declaration => declaration.Name, StringComparer.Ordinal)
                .Select(declaration => $"{declaration.Path}: {declaration.Name}"));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static HashSet<string> ParseInventoryRows(IEnumerable<string> lines)
    {
        HashSet<string> rows = new(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            if (!line.StartsWith("| `Assets/", StringComparison.Ordinal))
                continue;

            string[] columns = line.Split('|');
            if (columns.Length < 4)
                continue;

            string path = UnwrapCode(columns[1].Trim());
            string name = UnwrapCode(columns[2].Trim());
            if (path.Length == 0 || name.Length == 0)
                continue;

            rows.Add($"{path}|{name}");
        }

        return rows;
    }

    private static string ToInventoryKey(SystemDeclaration declaration)
    {
        return $"{declaration.Path}|{declaration.Name}";
    }

    private static string UnwrapCode(string value)
    {
        return value.Length >= 2 && value[0] == '`' && value[^1] == '`'
            ? value[1..^1]
            : value;
    }

    private readonly struct SystemDeclaration
    {
        public readonly string Path;
        public readonly string Name;
        public readonly string Bases;

        public SystemDeclaration(string path, string name, string bases)
        {
            Path = path;
            Name = name;
            Bases = bases;
        }
    }
}
#endif
