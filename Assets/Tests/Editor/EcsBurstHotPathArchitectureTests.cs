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
    private const int ToArrayDebtCeiling = 29;
    private const int EntityManagerMutationDebtCeiling = 40;
    private const int NonBurstOnUpdateFileDebtCeiling = 38;
    private const int BurstCompileFileFloor = 23;

    private static readonly Regex ToArrayRegex = new(
        @"\b(ToEntityArray|ToComponentDataArray)\s*(?:<[^>]+>)?\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex EntityManagerMutationRegex = new(
        @"\bEntityManager\.(AddComponent|RemoveComponent|DestroyEntity|Instantiate|CreateEntity|SetComponent)",
        RegexOptions.CultureInvariant);

    private static readonly Regex OnUpdateRegex = new(
        @"\bvoid\s+OnUpdate\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex BurstCompileRegex = new(
        @"\[BurstCompile\]",
        RegexOptions.CultureInvariant);

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

        Debug.Log("Top direct EntityManager mutation sources:\n" + FormatTopCounts(counts));

        Assert.LessOrEqual(
            total,
            EntityManagerMutationDebtCeiling,
            $"Direct EntityManager mutation debt increased from the roadmap baseline. Current={total}, ceiling={EntityManagerMutationDebtCeiling}. Frequent runtime mutations should move through EntityCommandBuffer unless explicitly documented.");
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

        Debug.Log("Non-Burst OnUpdate files:\n" + string.Join(Environment.NewLine, files.Take(30)));

        Assert.LessOrEqual(
            files.Count,
            NonBurstOnUpdateFileDebtCeiling,
            $"Non-Burst OnUpdate debt increased from the roadmap baseline. Current={files.Count}, ceiling={NonBurstOnUpdateFileDebtCeiling}. New frequent ECS work should be Burst-compatible unless it is an approved managed boundary.");
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
