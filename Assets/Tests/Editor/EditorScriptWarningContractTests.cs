#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class EditorScriptWarningContractTests
{
    private const string SelfPath = "Assets/Tests/Editor/EditorScriptWarningContractTests.cs";

    private static readonly string[] SourceRoots =
    {
        "Assets/Game/Scripts/Editor",
        "Assets/Tests/Editor"
    };

    private static readonly ForbiddenPattern[] ForbiddenPatterns =
    {
        new(
            "FindObjectsSortMode",
            "Use Object.FindObjectsByType<T>(FindObjectsInactive.Include/Exclude) without a sort-mode argument."),
        new(
            "FindFirstObjectByType",
            "Use Object.FindAnyObjectByType<T>() unless a deterministic object is explicitly selected by name or path."),
        new(
            "GetInstanceID()",
            "Do not key editor collections by GetInstanceID(); key by UnityEngine.Object/GameObject reference or a stable asset identifier."),
        new(
            "enableWordWrapping",
            "Use TMP_Text.textWrappingMode = TextWrappingModes.Normal or TextWrappingModes.NoWrap.")
    };

    public static void RunEditorWarningContractBatchValidation()
    {
        try
        {
            new EditorScriptWarningContractTests().EditorScriptsMustNotUseKnownUnity6ObsoleteWarningApis();
            UnityEngine.Debug.Log("[EditorWarningContractValidation] result=Passed");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[EditorWarningContractValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    [Test]
    public void EditorScriptsMustNotUseKnownUnity6ObsoleteWarningApis()
    {
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles())
        {
            string source = File.ReadAllText(path);
            foreach (ForbiddenPattern pattern in ForbiddenPatterns)
            {
                int index = source.IndexOf(pattern.Token, StringComparison.Ordinal);
                if (index < 0)
                    continue;

                violations.Add($"{NormalizePath(path)}:{LineNumber(source, index)} uses `{pattern.Token}`. {pattern.Replacement}");
            }
        }

        if (violations.Count == 0)
            return;

        string message =
            "Editor warning guard failed. Agents: do not add code that creates Unity compiler warnings; fix obsolete APIs before handoff.\n" +
            string.Join("\n", violations.OrderBy(v => v, StringComparer.Ordinal));
        Assert.Fail(message);
    }

    private static IEnumerable<string> EnumerateSourceFiles()
    {
        foreach (string root in SourceRoots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = NormalizePath(path);
                if (string.Equals(normalized, SelfPath, StringComparison.Ordinal))
                    continue;
                if (normalized.EndsWith("ContractTests.cs", StringComparison.Ordinal))
                    continue;

                yield return path;
            }
        }
    }

    private static int LineNumber(string source, int index)
    {
        int line = 1;
        for (int i = 0; i < index; i++)
        {
            if (source[i] == '\n')
                line++;
        }

        return line;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private readonly struct ForbiddenPattern
    {
        public ForbiddenPattern(string token, string replacement)
        {
            Token = token;
            Replacement = replacement;
        }

        public string Token { get; }

        public string Replacement { get; }
    }
}
#endif
