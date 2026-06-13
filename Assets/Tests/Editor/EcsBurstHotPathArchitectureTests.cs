#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class EcsBurstHotPathArchitectureTests
{
    private const string SystemsRoot = "Assets/Game/Scripts/Systems";
    private const string RenderingSystemsRoot = "Assets/Game/Scripts/Rendering/Systems";
    private const string UiShellEcsRoot = "Assets/Game/Scripts/UI/Shell/Ecs";
    private const string GameScriptsRoot = "Assets/Game/Scripts";
    private const int ToArrayDebtCeiling = 0;
    private const int EntityManagerMutationDebtCeiling = 1;
    private const int NonBurstOnUpdateFileDebtCeiling = 23;
    private const int BurstEcsOnUpdateFileFloor = 49;
    private const int JobBackedEcsOnUpdateFileFloor = 42;

    private static readonly Regex ToArrayRegex = new(
        @"\b(ToEntityArray|ToComponentDataArray)\s*(?:<[^>]+>)?\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex EntityManagerMutationRegex = new(
        @"\bEntityManager\.(AddComponent(?:Data)?|RemoveComponent|DestroyEntity|Instantiate|CreateEntity|SetComponent(?:Data)?)\s*(?:<[^>]+>)?\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex OnUpdateRegex = new(
        @"\bvoid\s+OnUpdate\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex OnUpdateMethodSignatureRegex = new(
        @"(?:public|private|internal|protected)?\s*(?:override\s+)?void\s+OnUpdate\s*\([^)]*\)",
        RegexOptions.CultureInvariant);

    private static readonly Regex BurstCompileRegex = new(
        @"\[BurstCompile\]",
        RegexOptions.CultureInvariant);

    private static readonly Regex JobBackedRegex = new(
        @"\bIJob(?:Entity|Chunk|ParallelFor|For)?\b|\bJobHandle\b|\.Schedule(?:Parallel)?\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex EcsSystemTypeRegex = new(
        @"\b(?:class|struct)\s+\w+\s*:\s*[^{;\n]*(?:\bISystem\b|\bSystemBase\b|\bComponentSystemBase\b|\bComponentSystem\b|\bJobComponentSystem\b)",
        RegexOptions.CultureInvariant);

    private static readonly Regex ClassBaseRegex = new(
        @"\bclass\s+(?<name>\w+)\s*:\s*(?<bases>[^{\n]+)",
        RegexOptions.CultureInvariant);

    private static readonly Regex UnityObjectApiRegex = new(
        @"\b(GameObject|UnityEngine\.Object|Object\.Instantiate|Object\.Destroy|Resources\.Load|AssetDatabase|GameObject\.Find|Camera\.main|GetComponent(?:InChildren|sInChildren)?\s*\()",
        RegexOptions.CultureInvariant);

    private static readonly Dictionary<string, string> ManagedDiagnosticNonBurstOnUpdateFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs"] = "pre-game diagnostics boundary; managed reporting only.",
        ["Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Systems/UnitMoveTargetDiagnosticSystem.cs"] = "diagnostic boundary; managed reporting only.",
        ["Assets/Game/Scripts/Systems/UnitPathfindingDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetDiagnosticLogFlushSystem.cs"] = "render-budget diagnostic flush boundary; managed log formatting only."
    };

    private static readonly Dictionary<string, string> ManagedBootstrapNonBurstOnUpdateFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs"] = "startup/native-container initialization boundary; not a recurring simulation hot path.",
        ["Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs"] = "startup spawn/config projection boundary; entity creation and prefab/config projection stay managed.",
        ["Assets/Game/Scripts/Systems/MapSurfaceFlatEquivalentBootstrapSystem.cs"] = "bootstrap/blob-builder boundary; not a recurring simulation hot path.",
        ["Assets/Game/Scripts/Systems/RuntimeGridDeduplicationSystem.cs"] = "startup/runtime-grid ownership boundary; native-container disposal and one-time cleanup stay managed."
    };

    private static readonly Dictionary<string, string> ManagedPresentationNonBurstOnUpdateFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/UnitRespawnSystem.cs"] = "spawn/presentation boundary; prefab instantiation, grounding warnings, and setup stay managed.",
        ["Assets/Game/Scripts/Systems/UnitVisualPrefabReferenceBackfillSystem.cs"] = "GameObject/prefab reference bridge; managed presentation boundary.",
        ["Assets/Game/Scripts/Systems/VehicleDestroyedVisualSystem.cs"] = "presentation/prefab instantiate boundary; managed visual lifecycle only.",
        ["Assets/Game/Scripts/Rendering/Systems/UnitAttachedLightSystem.cs"] = "light presentation bridge; managed object/light state remains outside Burst.",
        ["Assets/Game/Scripts/Rendering/Systems/UnitFactionTintTargetBackfillSystem.cs"] = "render-material presentation bridge; managed tint/material backfill remains outside Burst.",
        ["Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs"] = "model/prefab spawn presentation bridge; GameObject and prefab work stays managed.",
        ["Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSystem.cs"] = "render-budget camera/orchestration shell; pure distance, sorting, banding, and plan helpers are Burst-covered separately."
    };

    private static readonly Dictionary<string, string> ManagedUiShellNonBurstOnUpdateFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/UI/Shell/Ecs/UiShellArmoryCategorySystem.cs"] = "UI shell state boundary; single boundary entity command consumption stays managed.",
        ["Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs"] = "UI shell bootstrap boundary; creates the shell boundary entity and buffers.",
        ["Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs"] = "UI shell transition boundary; route/popup/presentation command buffering stays managed."
    };

    private static readonly Dictionary<string, string> ManagedDebugInputNonBurstOnUpdateFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/SelectedUnitDebugFireSystem.cs"] = "developer debug input boundary; intentionally managed and not production gameplay policy."
    };

    private static readonly Dictionary<string, string> ManagedGameplayOrchestrationNonBurstOnUpdateFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs"] = "detached pathfinding job orchestration boundary; expensive path search runs in Burst `PathfindBatchJob`, while this shell owns native snapshot lifetime, pending job state, diagnostics, and result playback."
    };

    private static readonly Dictionary<string, string> TrackedHotPathDebtNonBurstOnUpdateFiles = new(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, string> ManagedBoundaryNonBurstOnUpdateFiles = BuildManagedBoundaryNonBurstFiles();
    private static readonly IReadOnlyDictionary<string, string> ClassifiedNonBurstOnUpdateFiles = BuildClassifiedNonBurstFiles();

    private static readonly Dictionary<string, string> ClassifiedDirectEntityManagerMutationFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/CitizenVisibleUnitSystem.cs"] = "managed citizen presentation bridge; same-frame EntityManager.Instantiate is required so the actual spawned entity can be assigned movement and tracked by VisibleCitizensById immediately.",
    };

    private static readonly Regex SystemStateMethodSignatureRegex = new(
        @"^[ \t]*(?:public|private|internal|protected)?[ \t]*(?:static[ \t]+)?(?:[\w<>\[\],]+[ \t]+)?(?<method>\w+)[ \t]*\([^()\r\n]*\bref[ \t]+SystemState[ \t]+(?<state>\w+)[^()\r\n]*\)",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new EcsBurstHotPathArchitectureTests();
            tests.HotPathArraySnapshotDebtMustNotIncrease();
            tests.DirectEntityManagerMutationDebtMustNotIncrease();
            tests.NonBurstOnUpdateDebtMustNotIncrease();
            tests.ManagedBoundaryNonBurstFilesMustBeSeparateFromTrackedHotPathDebt();
            tests.ManagedBoundaryClassificationsMustBeDisjointAndConcrete();
            tests.RuntimeOnUpdateMustNotReadScriptableObjectConfigAssets();
            tests.BurstCompileCoverageMustNotDecrease();
            tests.SystemStateTypeHandlesMustBeCreatedOnlyDuringInitialization();
            tests.UnitRenderBudgetPureEcsSystemsMustNotUseUnityObjectApis();
            Debug.Log("[EcsBurstHotPathArchitectureValidation] result=Passed tests=9");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[EcsBurstHotPathArchitectureValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    public static void RunTypeHandleValidation()
    {
        try
        {
            var tests = new EcsBurstHotPathArchitectureTests();
            tests.SystemStateTypeHandlesMustBeCreatedOnlyDuringInitialization();
            Debug.Log("[EcsTypeHandleArchitectureValidation] result=Passed tests=1");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[EcsTypeHandleArchitectureValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void HotPathArraySnapshotDebtMustNotIncrease()
    {
        List<FileCount> counts = CountMatches(ToArrayRegex);
        int total = counts.Sum(count => count.Count);

        Debug.Log("Top ToEntityArray/ToComponentDataArray sources:\n" + FormatTopCounts(counts));

        Assert.LessOrEqual(
            total,
            ToArrayDebtCeiling,
            $"Hot-path ECS array snapshot debt increased from the roadmap baseline. Current={total}, ceiling={ToArrayDebtCeiling}. Reduce existing debt or update the roadmap with an approved performance report before raising this ceiling.");
    }

    [Test]
    public void DirectEntityManagerMutationDebtMustNotIncrease()
    {
        List<FileCount> counts = CountMatches(EntityManagerMutationRegex);
        int total = counts.Sum(count => count.Count);
        List<string> unclassified = counts
            .Select(count => count.Path)
            .Where(path => !ClassifiedDirectEntityManagerMutationFiles.ContainsKey(path))
            .ToList();
        List<string> staleClassifications = ClassifiedDirectEntityManagerMutationFiles.Keys
            .Where(path => counts.All(count => count.Path != path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Debug.Log("Top direct EntityManager mutation sources:\n" + FormatTopCounts(counts));
        Debug.Log("Classified direct EntityManager mutation files:\n" + FormatClassifiedEntityManagerMutationFiles(counts));

        Assert.LessOrEqual(
            total,
            EntityManagerMutationDebtCeiling,
            $"Direct EntityManager mutation debt increased from the roadmap baseline. Current={total}, ceiling={EntityManagerMutationDebtCeiling}. Frequent runtime mutations should move through EntityCommandBuffer unless explicitly documented.");

        Assert.IsEmpty(
            unclassified,
            "Direct EntityManager mutation files must be classified as an approved startup/presentation exception or converted to EntityCommandBuffer before landing:\n" +
            string.Join(Environment.NewLine, unclassified));

        Assert.IsEmpty(
            staleClassifications,
            "Direct EntityManager mutation classifications are stale. Remove converted/deleted files from the classification list:\n" +
            string.Join(Environment.NewLine, staleClassifications));
    }

    [Test]
    public void NonBurstOnUpdateDebtMustNotIncrease()
    {
        List<string> files = EnumerateEcsOnUpdateSystemFiles()
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return !BurstCompileRegex.IsMatch(text);
            })
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        List<string> unclassified = files
            .Where(path => !ClassifiedNonBurstOnUpdateFiles.ContainsKey(path))
            .ToList();
        List<string> staleClassifications = ClassifiedNonBurstOnUpdateFiles.Keys
            .Where(path => !files.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        List<string> allEcsOnUpdateFiles = EnumerateEcsOnUpdateSystemFiles().ToList();
        int burstFiles = CountBurstFiles(allEcsOnUpdateFiles);
        int jobBackedFiles = CountJobBackedFiles(allEcsOnUpdateFiles);

        Debug.Log(
            $"ECS OnUpdate coverage: total={allEcsOnUpdateFiles.Count} burst={burstFiles} ({FormatPercent(burstFiles, allEcsOnUpdateFiles.Count)}) jobBacked={jobBackedFiles} ({FormatPercent(jobBackedFiles, allEcsOnUpdateFiles.Count)}) nonBurst={files.Count} ({FormatPercent(files.Count, allEcsOnUpdateFiles.Count)}) unclassified={unclassified.Count}");
        Debug.Log("Classified non-Burst ECS OnUpdate files:\n" + FormatClassifiedNonBurstFiles(files));

        Assert.LessOrEqual(
            files.Count,
            NonBurstOnUpdateFileDebtCeiling,
            $"Non-Burst OnUpdate debt increased from the roadmap baseline. Current={files.Count}, ceiling={NonBurstOnUpdateFileDebtCeiling}. New frequent ECS work should be Burst-compatible unless it is an approved managed boundary.");

        Assert.IsEmpty(
            unclassified,
            "Non-Burst OnUpdate files must be classified as an approved managed boundary or tracked hot-path debt before landing:\n" +
            string.Join(Environment.NewLine, unclassified));

        Assert.IsEmpty(
            staleClassifications,
            "Non-Burst OnUpdate classifications are stale. Remove converted/deleted files from the classification list:\n" +
            string.Join(Environment.NewLine, staleClassifications));
    }

    [Test]
    public void ManagedBoundaryNonBurstFilesMustBeSeparateFromTrackedHotPathDebt()
    {
        List<string> overlaps = ManagedBoundaryNonBurstOnUpdateFiles.Keys
            .Intersect(TrackedHotPathDebtNonBurstOnUpdateFiles.Keys, StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        List<string> files = EnumerateEcsOnUpdateSystemFiles()
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return !BurstCompileRegex.IsMatch(text);
            })
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        List<string> currentManagedBoundaries = files
            .Where(path => ManagedBoundaryNonBurstOnUpdateFiles.ContainsKey(path))
            .ToList();
        List<string> currentTrackedDebt = files
            .Where(path => TrackedHotPathDebtNonBurstOnUpdateFiles.ContainsKey(path))
            .ToList();

        Assert.IsEmpty(
            overlaps,
            "A non-Burst OnUpdate file cannot be classified as both managed boundary and tracked hot-path debt:\n" +
            string.Join(Environment.NewLine, overlaps));

        Assert.AreEqual(
            files.Count,
            currentManagedBoundaries.Count + currentTrackedDebt.Count,
            "Every current non-Burst OnUpdate file must be classified exactly once as managed boundary or tracked hot-path debt.");

        Assert.Greater(
            currentManagedBoundaries.Count,
            0,
            "Managed boundary classification unexpectedly became empty; update the guardrail intentionally if all boundaries are converted.");

    }

    [Test]
    public void ManagedBoundaryClassificationsMustBeDisjointAndConcrete()
    {
        var categories = new (string Name, IReadOnlyDictionary<string, string> Files)[]
        {
            ("diagnostic", ManagedDiagnosticNonBurstOnUpdateFiles),
            ("bootstrap", ManagedBootstrapNonBurstOnUpdateFiles),
            ("presentation", ManagedPresentationNonBurstOnUpdateFiles),
            ("ui", ManagedUiShellNonBurstOnUpdateFiles),
            ("debug", ManagedDebugInputNonBurstOnUpdateFiles),
            ("gameplay-orchestration", ManagedGameplayOrchestrationNonBurstOnUpdateFiles)
        };

        List<string> duplicateManagedClassifications = categories
            .SelectMany(category => category.Files.Keys.Select(path => (category.Name, Path: path)))
            .GroupBy(entry => entry.Path, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => entry.Name).OrderBy(name => name, StringComparer.Ordinal))}")
            .ToList();
        List<string> weakReasons = categories
            .SelectMany(category => category.Files.Select(entry => (category.Name, entry.Key, entry.Value)))
            .Where(entry => IsWeakManagedBoundaryReason(entry.Value))
            .Select(entry => $"{entry.Key}: {entry.Name}; reason=`{entry.Value}`")
            .ToList();

        Assert.IsEmpty(
            duplicateManagedClassifications,
            "Managed boundary classifications must belong to exactly one managed category:\n" +
            string.Join(Environment.NewLine, duplicateManagedClassifications));

        Assert.IsEmpty(
            weakReasons,
            "Managed boundary classifications need concrete reasons tied to a real boundary, not placeholder exemptions:\n" +
            string.Join(Environment.NewLine, weakReasons));
    }

    [Test]
    public void RuntimeOnUpdateMustNotReadScriptableObjectConfigAssets()
    {
        HashSet<string> scriptableConfigTypes = CollectScriptableObjectTypeNames();
        List<string> violations = new();

        foreach (string path in EnumerateRuntimeSystemFiles())
        {
            string text = File.ReadAllText(path);
            foreach (Match methodMatch in OnUpdateMethodSignatureRegex.Matches(text))
            {
                int bodyStart = text.IndexOf('{', methodMatch.Index + methodMatch.Length);
                if (bodyStart < 0)
                    continue;

                int bodyEnd = FindMatchingBrace(text, bodyStart);
                if (bodyEnd <= bodyStart)
                    continue;

                string body = text.Substring(bodyStart, bodyEnd - bodyStart + 1);
                foreach (string typeName in scriptableConfigTypes.OrderBy(name => name, StringComparer.Ordinal))
                {
                    Match typeMatch = Regex.Match(body, $@"\b{Regex.Escape(typeName)}\b", RegexOptions.CultureInvariant);
                    if (!typeMatch.Success)
                        continue;

                    int line = CountLines(text, bodyStart + typeMatch.Index);
                    violations.Add($"{path}:{line} OnUpdate references ScriptableObject config asset type `{typeName}`.");
                }
            }
        }

        Assert.IsEmpty(
            violations,
            "Runtime OnUpdate methods must consume ECS-native projected data, not authored ScriptableObject config assets:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void BurstCompileCoverageMustNotDecrease()
    {
        List<string> ecsOnUpdateFiles = EnumerateEcsOnUpdateSystemFiles().ToList();
        int filesWithBurstCompile = CountBurstFiles(ecsOnUpdateFiles);
        int filesWithJobBackedWork = CountJobBackedFiles(ecsOnUpdateFiles);

        Debug.Log(
            $"ECS OnUpdate Burst/job coverage: total={ecsOnUpdateFiles.Count} burst={filesWithBurstCompile} ({FormatPercent(filesWithBurstCompile, ecsOnUpdateFiles.Count)}) jobBacked={filesWithJobBackedWork} ({FormatPercent(filesWithJobBackedWork, ecsOnUpdateFiles.Count)})");

        Assert.GreaterOrEqual(
            filesWithBurstCompile,
            BurstEcsOnUpdateFileFloor,
            $"ECS OnUpdate Burst coverage dropped below the roadmap baseline. Current={filesWithBurstCompile}, floor={BurstEcsOnUpdateFileFloor}.");

        Assert.GreaterOrEqual(
            filesWithJobBackedWork,
            JobBackedEcsOnUpdateFileFloor,
            $"ECS OnUpdate job-backed coverage dropped below the roadmap baseline. Current={filesWithJobBackedWork}, floor={JobBackedEcsOnUpdateFileFloor}.");
    }

    [Test]
    public void SystemStateTypeHandlesMustBeCreatedOnlyDuringInitialization()
    {
        List<string> violations = new();
        foreach (string path in EnumerateRuntimeEcsAuditFiles())
        {
            string text = File.ReadAllText(path);
            foreach (Match methodMatch in SystemStateMethodSignatureRegex.Matches(text))
            {
                string methodName = methodMatch.Groups["method"].Value;
                if (methodName is "OnCreate" or "Initialize")
                    continue;

                int bodyStart = text.IndexOf('{', methodMatch.Index + methodMatch.Length);
                if (bodyStart < 0)
                    continue;

                int bodyEnd = FindMatchingBrace(text, bodyStart);
                if (bodyEnd <= bodyStart)
                    continue;

                string stateVariable = methodMatch.Groups["state"].Value;
                Regex handleCreationRegex = new(
                    $@"\b{Regex.Escape(stateVariable)}\s*\.\s*Get(?:Entity|Component|Buffer|SharedComponent)TypeHandle\s*\(",
                    RegexOptions.CultureInvariant);
                string body = text.Substring(bodyStart, bodyEnd - bodyStart + 1);
                foreach (Match handleMatch in handleCreationRegex.Matches(body))
                {
                    int line = CountLines(text, bodyStart + handleMatch.Index);
                    violations.Add($"{path}:{line} {methodName} creates a SystemState type handle outside initialization.");
                }
            }
        }

        Assert.IsEmpty(
            violations,
            "SystemState type handles must be cached in OnCreate or Initialize and refreshed in runtime ticks with _handle.Update(ref state):\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void UnitRenderBudgetPureEcsSystemsMustNotUseUnityObjectApis()
    {
        List<string> violations = new();
        foreach (string path in EnumerateUnitRenderBudgetSystemFiles())
        {
            string text = File.ReadAllText(path);
            foreach (Match match in UnityObjectApiRegex.Matches(text))
            {
                int line = CountLines(text, match.Index);
                violations.Add($"{path}:{line} uses `{match.Value}` in the pure render-budget ECS path.");
            }
        }

        Assert.IsEmpty(
            violations,
            "UnitRenderBudget* systems must keep model, prefab, GameObject, resource, and component lookup work in managed presentation boundaries:\n" +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> EnumerateSystemFiles()
    {
        return Directory
            .GetFiles(SystemsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static IEnumerable<string> EnumerateRuntimeSystemFiles()
    {
        return EnumerateRuntimeEcsAuditFiles();
    }

    private static IEnumerable<string> EnumerateRuntimeEcsAuditFiles()
    {
        return EnumerateFiles(SystemsRoot)
            .Concat(EnumerateFiles(RenderingSystemsRoot))
            .Concat(EnumerateFiles(UiShellEcsRoot))
            .Select(NormalizePath)
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static IEnumerable<string> EnumerateEcsOnUpdateSystemFiles()
    {
        return EnumerateRuntimeEcsAuditFiles()
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return EcsSystemTypeRegex.IsMatch(text) && OnUpdateRegex.IsMatch(text);
            })
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        return Directory.Exists(root)
            ? Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            : Array.Empty<string>();
    }

    private static int CountBurstFiles(IEnumerable<string> paths)
    {
        return paths.Count(path => BurstCompileRegex.IsMatch(File.ReadAllText(path)));
    }

    private static int CountJobBackedFiles(IEnumerable<string> paths)
    {
        return paths.Count(path => JobBackedRegex.IsMatch(File.ReadAllText(path)));
    }

    private static string FormatPercent(int numerator, int denominator)
    {
        if (denominator <= 0)
            return "0.0%";

        return $"{(double)numerator / denominator * 100d:F1}%";
    }

    private static Dictionary<string, string> BuildManagedBoundaryNonBurstFiles()
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        AddRange(result, ManagedDiagnosticNonBurstOnUpdateFiles);
        AddRange(result, ManagedBootstrapNonBurstOnUpdateFiles);
        AddRange(result, ManagedPresentationNonBurstOnUpdateFiles);
        AddRange(result, ManagedUiShellNonBurstOnUpdateFiles);
        AddRange(result, ManagedDebugInputNonBurstOnUpdateFiles);
        AddRange(result, ManagedGameplayOrchestrationNonBurstOnUpdateFiles);
        return result;
    }

    private static Dictionary<string, string> BuildClassifiedNonBurstFiles()
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        AddRange(result, ManagedBoundaryNonBurstOnUpdateFiles);
        AddRange(result, TrackedHotPathDebtNonBurstOnUpdateFiles);
        return result;
    }

    private static void AddRange(
        Dictionary<string, string> target,
        IEnumerable<KeyValuePair<string, string>> source)
    {
        foreach (KeyValuePair<string, string> entry in source)
            target.Add(entry.Key, entry.Value);
    }

    private static bool IsWeakManagedBoundaryReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return true;

        string normalized = reason.Trim();
        if (normalized.Equals("managed boundary", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("managed boundary.", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("<", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static IEnumerable<string> EnumerateUnitRenderBudgetSystemFiles()
    {
        return Directory
            .GetFiles(RenderingSystemsRoot, "UnitRenderBudget*.cs", SearchOption.TopDirectoryOnly)
            .Select(NormalizePath)
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static HashSet<string> CollectScriptableObjectTypeNames()
    {
        Dictionary<string, string[]> classBases = new(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(GameScriptsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(path);
            foreach (Match match in ClassBaseRegex.Matches(text))
            {
                string typeName = match.Groups["name"].Value;
                string[] bases = match.Groups["bases"].Value
                    .Split(',')
                    .Select(NormalizeBaseTypeName)
                    .Where(baseName => !string.IsNullOrEmpty(baseName))
                    .ToArray();

                classBases[typeName] = bases;
            }
        }

        HashSet<string> scriptableTypes = new(StringComparer.Ordinal);
        bool changed;
        do
        {
            changed = false;
            foreach (KeyValuePair<string, string[]> entry in classBases)
            {
                if (scriptableTypes.Contains(entry.Key))
                    continue;

                if (entry.Value.Any(baseName =>
                        baseName is "ScriptableObject" or "UnityEngine.ScriptableObject" ||
                        scriptableTypes.Contains(baseName)))
                {
                    scriptableTypes.Add(entry.Key);
                    changed = true;
                }
            }
        }
        while (changed);

        return scriptableTypes;
    }

    private static string NormalizeBaseTypeName(string baseType)
    {
        string normalized = baseType.Trim();
        int genericIndex = normalized.IndexOf('<');
        if (genericIndex >= 0)
            normalized = normalized[..genericIndex];

        int namespaceIndex = normalized.LastIndexOf('.');
        if (namespaceIndex >= 0 && namespaceIndex + 1 < normalized.Length)
            normalized = normalized[(namespaceIndex + 1)..];

        return normalized.Trim();
    }

    private static List<FileCount> CountMatches(Regex regex)
    {
        List<FileCount> counts = new();
        foreach (string path in EnumerateSystemFiles())
        {
            int count = regex.Matches(File.ReadAllText(path)).Count;
            if (count > 0)
                counts.Add(new FileCount(path, count));
        }

        return counts
            .OrderByDescending(count => count.Count)
            .ThenBy(count => count.Path, StringComparer.Ordinal)
            .ToList();
    }

    private static int FindMatchingBrace(string text, int openingBraceIndex)
    {
        int depth = 0;
        for (int i = openingBraceIndex; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }

    private static int CountLines(string text, int index)
    {
        int line = 1;
        int length = Math.Min(index, text.Length);
        for (int i = 0; i < length; i++)
        {
            if (text[i] == '\n')
                line++;
        }

        return line;
    }

    private static string FormatTopCounts(IReadOnlyList<FileCount> counts)
    {
        if (counts.Count == 0)
            return "<none>";

        return string.Join(
            Environment.NewLine,
            counts.Take(20).Select(count => $"{count.Path}: {count.Count}"));
    }

    private static string FormatClassifiedNonBurstFiles(IReadOnlyList<string> files)
    {
        if (files.Count == 0)
            return "<none>";

        return string.Join(
            Environment.NewLine,
            files.Select(path =>
            {
                string classification = ClassifiedNonBurstOnUpdateFiles.TryGetValue(path, out string reason)
                    ? reason
                    : "<unclassified>";
                return $"{path}: {classification}";
            }));
    }

    private static string FormatClassifiedEntityManagerMutationFiles(IReadOnlyList<FileCount> counts)
    {
        if (counts.Count == 0)
            return "<none>";

        return string.Join(
            Environment.NewLine,
            counts.Select(count =>
            {
                string classification = ClassifiedDirectEntityManagerMutationFiles.TryGetValue(count.Path, out string reason)
                    ? reason
                    : "<unclassified>";
                return $"{count.Path}: {count.Count} mutation(s); {classification}";
            }));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private readonly struct FileCount
    {
        public FileCount(string path, int count)
        {
            Path = path;
            Count = count;
        }

        public string Path { get; }
        public int Count { get; }
    }
}
#endif
