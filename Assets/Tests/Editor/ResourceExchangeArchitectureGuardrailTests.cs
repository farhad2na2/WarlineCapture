using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class ResourceExchangeArchitectureGuardrailTests
{
    private const string SourceRoot = "Assets/Game/Scripts";
    private const string UiScreensRoot = "Assets/Game/Scripts/UI/Screens";
    private const string ManagedPresentationHelperPath =
        "Assets/Game/Scripts/Systems/ResourceExchangeVisualPresentationSystemHelper.cs";
    private const string RequestValidationSystemPath =
        "Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs";

    private static readonly Regex TypeDeclarationRegex =
        new(@"\b(class|struct|interface)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static readonly Regex RequestValidationForbiddenOnUpdateRegex = new(
        @"\bCreateEntityQuery\s*\(|\bstate\s*\.\s*Dependency\s*\.\s*Complete\s*\(|\b(?:ToEntityArray|ToComponentDataArray)\s*(?:<[^>]+>)?\s*\(|\bAllocator\s*\.\s*Temp\b|\b(?:Get|TryGet|Set|Has)Singleton[A-Za-z0-9_]*\s*(?:<[^>]+>)?\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RequestValidationRequiredBufferFilterRegex = new(
        @"\.WithAll\s*<\s*ResourceExchangeResultComponent\s*>\s*\(\s*\)[\s\S]*\.WithAll\s*<\s*ResourceExchangeEconomyEventComponent\s*>\s*\(\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] RequestValidationRequiredQueryTypes =
    {
        "ResourceExchangeRequestQueueComponent",
        "ResourceExchangeEnabledComponent",
        "FactionEconomy",
        "FactionTacticalMaterialsComponent",
        "ResourceExchangeWalletComponent",
        "ResourceExchangeSummaryComponent",
        "ResourceExchangeRecipeComponent",
        "ResourceExchangeRequestComponent",
        "ResourceExchangeQueueComponent",
        "ResourceExchangeResultComponent",
        "ResourceExchangeEconomyEventComponent"
    };

    private static readonly string[] RequestValidationForbiddenManagedAllocationTokens =
    {
        "new List<",
        "new Dictionary<",
        "new HashSet<",
        "new Queue<",
        "new Stack<",
        "new string",
        "new object",
        "new[]",
        "System.Linq",
        ".ToArray(",
        ".ToList(",
        "string.Format(",
        "string.Concat(",
        "string.Join(",
        "$\""
    };

    private static readonly string[] DisallowedEcsBaseTokens =
    {
        "SystemBase",
        "ComponentSystemBase",
        "JobComponentSystem",
        "ComponentSystem"
    };

    private static readonly string[] BroadTypeSuffixes =
    {
        "Manager",
        "Controller",
        "Service",
        "Facade"
    };

    private static readonly string[] UiScreenForbiddenEcsTokens =
    {
        "using Unity.Entities",
        "EntityManager",
        "EntityQuery",
        "DynamicBuffer<",
        "EntityCommandBuffer",
        "World.DefaultGameObjectInjectionWorld",
        "GetBuffer<",
        "SetComponentData",
        "AddComponentData",
        "CreateEntity(",
        "ResourceExchangeWalletComponent",
        "ResourceExchangeQueueComponent",
        "ResourceExchangeRequestComponent",
        "ResourceExchangeEconomyEventComponent"
    };

    private static readonly string[] HotPathForbiddenManagedScanTokens =
    {
        "System.Linq",
        ".Where(",
        ".Select(",
        ".OrderBy(",
        ".GroupBy(",
        "ToEntityArray",
        "ToComponentDataArray",
        "FindObject",
        "FindObjects",
        "FindFirstObjectByType",
        "FindAnyObjectByType",
        "GameObject.Find",
        "Resources.Load",
        "Camera.main",
        "GetComponent<",
        "GetComponents"
    };

    private static readonly string[] PresentationBoundaryForbiddenAuthorityTokens =
    {
        "ResourceExchangeWalletComponent",
        "ResourceExchangeSummaryComponent",
        "ResourceExchangeRecipeComponent",
        "ResourceExchangeRequestComponent",
        "ResourceExchangeQueueComponent",
        "ResourceExchangeResultComponent",
        "ResourceExchangeEconomyEventComponent",
        "SetComponentData",
        "AddComponentData",
        "CreateEntity(",
        "EntityCommandBuffer"
    };

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ResourceExchangeRuntimeSystemsPreferISystemAndAvoidSystemBase),
                test => test.ResourceExchangeRuntimeSystemsPreferISystemAndAvoidSystemBase(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangeProductionTypesAvoidBroadShellNames),
                test => test.ResourceExchangeProductionTypesAvoidBroadShellNames(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangeUiScreensDoNotMutateEcsDirectly),
                test => test.ResourceExchangeUiScreensDoNotMutateEcsDirectly(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangeHotPathSystemsAvoidManagedScanTokens),
                test => test.ResourceExchangeHotPathSystemsAvoidManagedScanTokens(),
                ref passed);
            RunValidationStep(
                nameof(RequestValidationSystemUsesDirectMultiEntityQueryWithoutForbiddenHotPathApis),
                test => test.RequestValidationSystemUsesDirectMultiEntityQueryWithoutForbiddenHotPathApis(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangeManagedPresentationBoundaryDoesNotMutateAuthoritativeExchangeState),
                test => test.ResourceExchangeManagedPresentationBoundaryDoesNotMutateAuthoritativeExchangeState(),
                ref passed);

            Debug.Log($"[ResourceExchangeArchitectureGuardrail] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeArchitectureGuardrail] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ResourceExchangeRuntimeSystemsPreferISystemAndAvoidSystemBase()
    {
        List<string> runtimeSystems = FindRuntimeSystemFiles();
        AssertNonEmpty(runtimeSystems, "Resource Exchange runtime system files were not found.");

        for (int i = 0; i < runtimeSystems.Count; i++)
        {
            string path = runtimeSystems[i];
            string contents = File.ReadAllText(path);
            AssertNoForbiddenTokens(path, contents, DisallowedEcsBaseTokens);
            StringAssert.Contains(
                ": ISystem",
                contents,
                $"{path} must remain an unmanaged ISystem unless a documented managed boundary is required.");
        }
    }

    [Test]
    public void ResourceExchangeProductionTypesAvoidBroadShellNames()
    {
        List<string> sourceFiles = FindProductionResourceExchangeFiles();
        AssertNonEmpty(sourceFiles, "Resource Exchange production source files were not found.");

        for (int fileIndex = 0; fileIndex < sourceFiles.Count; fileIndex++)
        {
            string path = sourceFiles[fileIndex];
            MatchCollection matches = TypeDeclarationRegex.Matches(File.ReadAllText(path));
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                string typeName = matches[matchIndex].Groups[2].Value;
                for (int suffixIndex = 0; suffixIndex < BroadTypeSuffixes.Length; suffixIndex++)
                {
                    string suffix = BroadTypeSuffixes[suffixIndex];
                    Assert.IsFalse(
                        typeName.EndsWith(suffix, StringComparison.Ordinal),
                        $"{path} declares `{typeName}`. Resource Exchange code should keep role-specific type names and avoid broad `{suffix}` shells.");
                }
            }
        }
    }

    [Test]
    public void ResourceExchangeUiScreensDoNotMutateEcsDirectly()
    {
        List<string> uiScreens = FindUiScreenFiles();
        AssertNonEmpty(uiScreens, "Resource Exchange UI screen files were not found.");

        for (int i = 0; i < uiScreens.Count; i++)
        {
            string path = uiScreens[i];
            string contents = File.ReadAllText(path);
            AssertNoForbiddenTokens(path, contents, UiScreenForbiddenEcsTokens);
        }
    }

    [Test]
    public void ResourceExchangeHotPathSystemsAvoidManagedScanTokens()
    {
        List<string> runtimeSystems = FindRuntimeSystemFiles();
        AssertNonEmpty(runtimeSystems, "Resource Exchange runtime system files were not found.");

        for (int i = 0; i < runtimeSystems.Count; i++)
        {
            string path = runtimeSystems[i];
            string contents = File.ReadAllText(path);
            AssertNoForbiddenTokens(path, contents, HotPathForbiddenManagedScanTokens);
        }
    }

    [Test]
    public void RequestValidationSystemUsesDirectMultiEntityQueryWithoutForbiddenHotPathApis()
    {
        Assert.IsTrue(
            File.Exists(RequestValidationSystemPath),
            $"{RequestValidationSystemPath} was not found.");

        string contents = File.ReadAllText(RequestValidationSystemPath);
        string onUpdate = ExtractMethod(contents, "public void OnUpdate");
        Match forbiddenMatch = RequestValidationForbiddenOnUpdateRegex.Match(onUpdate);

        Assert.IsFalse(
            forbiddenMatch.Success,
            $"{RequestValidationSystemPath}.OnUpdate contains forbidden hot-path API `{forbiddenMatch.Value}`.");
        AssertNoForbiddenTokens(
            $"{RequestValidationSystemPath}.OnUpdate",
            onUpdate,
            RequestValidationForbiddenManagedAllocationTokens);

        StringAssert.Contains("foreach", onUpdate);
        StringAssert.Contains("SystemAPI.Query<", onUpdate);
        StringAssert.Contains(".WithEntityAccess()", onUpdate);
        Assert.IsTrue(
            RequestValidationRequiredBufferFilterRegex.IsMatch(onUpdate),
            "Request validation must keep result and economy-event buffers in the direct query match set.");
        StringAssert.Contains(
            "SystemAPI.GetBuffer<ResourceExchangeResultComponent>(exchangeEntity)",
            onUpdate);
        StringAssert.Contains(
            "SystemAPI.GetBuffer<ResourceExchangeEconomyEventComponent>(exchangeEntity)",
            onUpdate);

        for (int i = 0; i < RequestValidationRequiredQueryTypes.Length; i++)
        {
            StringAssert.Contains(
                RequestValidationRequiredQueryTypes[i],
                onUpdate,
                $"Request validation direct iteration must retain `{RequestValidationRequiredQueryTypes[i]}`.");
        }
    }

    [Test]
    public void ResourceExchangeManagedPresentationBoundaryDoesNotMutateAuthoritativeExchangeState()
    {
        Assert.IsTrue(
            File.Exists(ManagedPresentationHelperPath),
            $"{ManagedPresentationHelperPath} was not found.");

        string contents = File.ReadAllText(ManagedPresentationHelperPath);
        AssertNoForbiddenTokens(
            ManagedPresentationHelperPath,
            contents,
            PresentationBoundaryForbiddenAuthorityTokens);
    }

    private static List<string> FindProductionResourceExchangeFiles()
    {
        var result = new List<string>(32);
        if (!Directory.Exists(SourceRoot))
            return result;

        string[] files = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string normalized = NormalizePath(files[i]);
            if (normalized.IndexOf("/Editor/", StringComparison.Ordinal) >= 0)
                continue;

            if (normalized.IndexOf("ResourceExchange", StringComparison.Ordinal) >= 0)
                result.Add(normalized);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static List<string> FindRuntimeSystemFiles()
    {
        List<string> sourceFiles = FindProductionResourceExchangeFiles();
        var result = new List<string>(8);
        for (int i = 0; i < sourceFiles.Count; i++)
        {
            string path = sourceFiles[i];
            if (path.EndsWith("/UiResourceExchangeReadModelSystem.cs", StringComparison.Ordinal))
            {
                result.Add(path);
                continue;
            }

            if (!path.StartsWith("Assets/Game/Scripts/Systems/ResourceExchange", StringComparison.Ordinal))
                continue;

            string fileName = Path.GetFileName(path);
            if (fileName.EndsWith("System.cs", StringComparison.Ordinal))
                result.Add(path);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static List<string> FindUiScreenFiles()
    {
        var result = new List<string>(8);
        if (!Directory.Exists(UiScreensRoot))
            return result;

        string[] files = Directory.GetFiles(UiScreensRoot, "*ResourceExchange*.cs", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
            result.Add(NormalizePath(files[i]));

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static void AssertNoForbiddenTokens(string path, string contents, IReadOnlyList<string> forbiddenTokens)
    {
        for (int tokenIndex = 0; tokenIndex < forbiddenTokens.Count; tokenIndex++)
        {
            string token = forbiddenTokens[tokenIndex];
            StringAssert.DoesNotContain(
                token,
                contents,
                $"{path} contains forbidden token `{token}` for this Resource Exchange architecture boundary.");
        }
    }

    private static void AssertNonEmpty(ICollection<string> values, string message)
    {
        Assert.IsNotNull(values, message);
        Assert.IsTrue(values.Count > 0, message);
    }

    private static string ExtractMethod(string source, string signature)
    {
        int signatureStart = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.GreaterOrEqual(signatureStart, 0, $"{signature} was not found.");
        int bodyStart = source.IndexOf('{', signatureStart);
        Assert.GreaterOrEqual(bodyStart, 0, $"{signature} body was not found.");

        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(bodyStart, i - bodyStart + 1);
        }

        Assert.Fail($"{signature} body was not closed.");
        return string.Empty;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeArchitectureGuardrailTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeArchitectureGuardrailTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeArchitectureGuardrail] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeArchitectureGuardrail] failed {name}\n{exception}");
            throw;
        }
    }
}
