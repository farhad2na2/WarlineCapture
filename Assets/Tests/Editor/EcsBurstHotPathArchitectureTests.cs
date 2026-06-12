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
    private const int ToArrayDebtCeiling = 0;
    private const int EntityManagerMutationDebtCeiling = 1;
    private const int NonBurstOnUpdateFileDebtCeiling = 24;
    private const int BurstCompileFileFloor = 37;

    private static readonly Regex ToArrayRegex = new(
        @"\b(ToEntityArray|ToComponentDataArray)\s*(?:<[^>]+>)?\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex EntityManagerMutationRegex = new(
        @"\bEntityManager\.(AddComponent(?:Data)?|RemoveComponent|DestroyEntity|Instantiate|CreateEntity|SetComponent(?:Data)?)\s*(?:<[^>]+>)?\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex OnUpdateRegex = new(
        @"\bvoid\s+OnUpdate\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex BurstCompileRegex = new(
        @"\[BurstCompile\]",
        RegexOptions.CultureInvariant);

    private static readonly Dictionary<string, string> ClassifiedNonBurstOnUpdateFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs"] = "Phase 5 AI planning hot-path debt; contains authored policy, diagnostics, and command-event work that must be split before Burst.",
        ["Assets/Game/Scripts/Systems/AICombatOrderSystem.cs"] = "Phase 5 AI combat-order hot-path debt; command issuance and diagnostics need a data/job split.",
        ["Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Systems/AIEconomySystem.cs"] = "Phase 5 AI economy debt; managed resource summary and request-buffer policy need a result-buffer split before Burst.",
        ["Assets/Game/Scripts/Systems/AIProductionSystem.cs"] = "Phase 5 AI production debt; queue/build request policy remains managed until production data is projected ECS-native.",
        ["Assets/Game/Scripts/Systems/AISquadSystem.cs"] = "Phase 5 AI squad debt; squad membership policy and diagnostics need a chunk/job rewrite.",
        ["Assets/Game/Scripts/Systems/AITargetingSystem.cs"] = "Phase 5 AI targeting hot-path debt; target scoring and component-presence checks still need a data/job split.",
        ["Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs"] = "startup/native-container initialization boundary; not a recurring simulation hot path.",
        ["Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs"] = "startup spawn/config projection boundary; entity creation and prefab/config projection stay managed.",
        ["Assets/Game/Scripts/Systems/MapSurfaceFlatEquivalentBootstrapSystem.cs"] = "bootstrap/blob-builder boundary; not a recurring simulation hot path.",
        ["Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs"] = "pre-game diagnostics boundary; managed reporting only.",
        ["Assets/Game/Scripts/Systems/RuntimeGridDeduplicationSystem.cs"] = "startup/runtime-grid ownership boundary; native-container disposal and one-time cleanup stay managed.",
        ["Assets/Game/Scripts/Systems/SelectedUnitDebugFireSystem.cs"] = "developer debug input boundary; intentionally managed and not production gameplay policy.",
        ["Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Systems/UnitAttackSystem.cs"] = "combat hot-path debt; mixed combat simulation, VFX requests, and diagnostics need a split before Burst.",
        ["Assets/Game/Scripts/Systems/UnitDeathSystem.cs"] = "combat lifecycle debt; death state, presentation requests, and cleanup need clearer data/job boundaries.",
        ["Assets/Game/Scripts/Systems/UnitMoveTargetDiagnosticSystem.cs"] = "diagnostic boundary; managed reporting only.",
        ["Assets/Game/Scripts/Systems/UnitPathfindingDiagnosticLogFlushSystem.cs"] = "diagnostic flush boundary; managed log formatting outside gameplay hot paths.",
        ["Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs"] = "Phase 6 pathfinding orchestration debt; detached-job/native-container ownership should remain explicit before further Burst changes.",
        ["Assets/Game/Scripts/Systems/UnitRespawnSystem.cs"] = "spawn/presentation boundary; prefab instantiation, grounding warnings, and setup stay managed.",
        ["Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs"] = "transport gameplay/presentation split debt; uses managed visual hiding utility and needs a data-only request split.",
        ["Assets/Game/Scripts/Systems/UnitVisualPrefabReferenceBackfillSystem.cs"] = "GameObject/prefab reference bridge; managed presentation boundary.",
        ["Assets/Game/Scripts/Systems/VehicleDestroyedVisualSystem.cs"] = "presentation/prefab instantiate boundary; managed visual lifecycle only.",
    };

    private static readonly Dictionary<string, string> ClassifiedDirectEntityManagerMutationFiles = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/CitizenVisibleUnitSystem.cs"] = "managed citizen presentation bridge; same-frame EntityManager.Instantiate is required so the actual spawned entity can be assigned movement and tracked by VisibleCitizensById immediately.",
    };

    private static readonly Regex SystemStateMethodSignatureRegex = new(
        @"(?:public|private|internal|protected)?\s*(?:static\s+)?(?:[\w<>\[\], ]+\s+)?(?<method>\w+)\s*\([^)]*\bref\s+SystemState\s+(?<state>\w+)[^)]*\)",
        RegexOptions.CultureInvariant);

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new EcsBurstHotPathArchitectureTests();
            tests.HotPathArraySnapshotDebtMustNotIncrease();
            tests.DirectEntityManagerMutationDebtMustNotIncrease();
            tests.NonBurstOnUpdateDebtMustNotIncrease();
            tests.BurstCompileCoverageMustNotDecrease();
            tests.SystemStateTypeHandlesMustBeCreatedOnlyDuringInitialization();
            Debug.Log("[EcsBurstHotPathArchitectureValidation] result=Passed tests=5");
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
        List<string> files = EnumerateSystemFiles()
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return OnUpdateRegex.IsMatch(text) && !BurstCompileRegex.IsMatch(text);
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

        Debug.Log("Classified non-Burst OnUpdate files:\n" + FormatClassifiedNonBurstFiles(files));

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
    public void BurstCompileCoverageMustNotDecrease()
    {
        int filesWithBurstCompile = EnumerateSystemFiles()
            .Count(path => BurstCompileRegex.IsMatch(File.ReadAllText(path)));

        Assert.GreaterOrEqual(
            filesWithBurstCompile,
            BurstCompileFileFloor,
            $"Burst coverage dropped below the roadmap baseline. Current={filesWithBurstCompile}, floor={BurstCompileFileFloor}.");
    }

    [Test]
    public void SystemStateTypeHandlesMustBeCreatedOnlyDuringInitialization()
    {
        List<string> violations = new();
        foreach (string path in EnumerateSystemFiles())
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

    private static IEnumerable<string> EnumerateSystemFiles()
    {
        return Directory
            .GetFiles(SystemsRoot, "*.cs", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .OrderBy(path => path, StringComparer.Ordinal);
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
