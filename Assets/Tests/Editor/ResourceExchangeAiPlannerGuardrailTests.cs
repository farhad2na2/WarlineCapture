using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using NUnit.Framework;
using UnityEngine;

public sealed class ResourceExchangeAiPlannerGuardrailTests
{
    private const string SystemsRoot = "Assets/Game/Scripts/Systems";

    private static readonly string[] ForbiddenPlannerTokens =
    {
        "SystemBase",
        "MonoBehaviour",
        "GameObject",
        "Transform",
        "Camera.main",
        "Resources.Load",
        "FindObject",
        "FindObjects",
        "FindFirstObjectByType",
        "FindAnyObjectByType",
        "GameObject.Find",
        "GetComponent<",
        "GetComponents",
        "System.Linq",
        ".Where(",
        ".Select(",
        ".OrderBy(",
        ".GroupBy(",
        "ToComponentDataArray",
        "ToEntityArray"
    };

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ResourceExchangeAiGateIsDataOnlyContract),
                test => test.ResourceExchangeAiGateIsDataOnlyContract(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangeAiPlannerScriptsAvoidManagedPerFrameScanTokens),
                test => test.ResourceExchangeAiPlannerScriptsAvoidManagedPerFrameScanTokens(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangeAiPlannerQueueRequestsRequireExplicitAiGateRead),
                test => test.ResourceExchangeAiPlannerQueueRequestsRequireExplicitAiGateRead(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangeAiRecoveryUsesBurstSystemContract),
                test => test.ResourceExchangeAiRecoveryUsesBurstSystemContract(),
                ref passed);

            Debug.Log($"[ResourceExchangeAiPlannerGuardrail] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeAiPlannerGuardrail] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ResourceExchangeAiGateIsDataOnlyContract()
    {
        ResourceExchangeEnabledComponent enabled = new()
        {
            Enabled = 1,
            FactionId = 2,
            AllowAiExchange = 1
        };
        ResourceExchangeSummaryComponent summary = new()
        {
            FactionId = enabled.FactionId,
            Enabled = enabled.Enabled,
            AllowAiExchange = enabled.AllowAiExchange
        };

        Assert.AreEqual(1, enabled.AllowAiExchange);
        Assert.AreEqual(enabled.AllowAiExchange, summary.AllowAiExchange);
    }

    [Test]
    public void ResourceExchangeAiPlannerScriptsAvoidManagedPerFrameScanTokens()
    {
        List<string> plannerScripts = FindResourceExchangeAiPlannerScripts();
        for (int pathIndex = 0; pathIndex < plannerScripts.Count; pathIndex++)
            AssertNoForbiddenTokens(plannerScripts[pathIndex], ForbiddenPlannerTokens);
    }

    [Test]
    public void ResourceExchangeAiPlannerQueueRequestsRequireExplicitAiGateRead()
    {
        List<string> plannerScripts = FindResourceExchangeAiPlannerScripts();
        for (int pathIndex = 0; pathIndex < plannerScripts.Count; pathIndex++)
        {
            string path = plannerScripts[pathIndex];
            string contents = File.ReadAllText(path);
            if (contents.IndexOf("EnqueueStartRequest", StringComparison.Ordinal) < 0)
                continue;

            StringAssert.Contains(
                "AllowAiExchange",
                contents,
                $"{path} queues Resource Exchange starts but does not read the explicit AI exchange scenario gate.");
        }
    }

    [Test]
    public void ResourceExchangeAiRecoveryUsesBurstSystemContract()
    {
        const string path = SystemsRoot + "/ResourceExchangeAIRecoverySystem.cs";
        Assert.IsTrue(File.Exists(path), $"Missing AI recovery system: {path}");
        string contents = File.ReadAllText(path);
        StringAssert.Contains("partial struct ResourceExchangeAIRecoverySystem : ISystem", contents);
        StringAssert.Contains("[BurstCompile]", contents);
        StringAssert.Contains("AllowAiExchange", contents);
        StringAssert.Contains("UpdateBefore(typeof(ResourceExchangeRequestValidationSystem))", contents);
        StringAssert.Contains("AIMaterialsRecoveryNeedComponent", contents);
    }

    private static List<string> FindResourceExchangeAiPlannerScripts()
    {
        var result = new List<string>(8);
        if (!Directory.Exists(SystemsRoot))
            return result;

        string[] files = Directory.GetFiles(SystemsRoot, "*ResourceExchange*.cs", SearchOption.AllDirectories);
        for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
        {
            string normalized = files[fileIndex].Replace('\\', '/');
            string fileName = Path.GetFileNameWithoutExtension(normalized);
            if (fileName.IndexOf("AI", StringComparison.Ordinal) < 0 &&
                fileName.IndexOf("Ai", StringComparison.Ordinal) < 0)
            {
                continue;
            }

            result.Add(normalized);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static void AssertNoForbiddenTokens(string path, IReadOnlyList<string> forbiddenTokens)
    {
        string contents = File.ReadAllText(path);
        for (int tokenIndex = 0; tokenIndex < forbiddenTokens.Count; tokenIndex++)
        {
            string token = forbiddenTokens[tokenIndex];
            StringAssert.DoesNotContain(
                token,
                contents,
                $"{path} must keep Resource Exchange AI planner logic ECS/data-driven. Forbidden token `{token}` suggests a managed per-frame scan, managed planner boundary, or scene lookup.");
        }
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeAiPlannerGuardrailTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeAiPlannerGuardrailTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeAiPlannerGuardrail] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeAiPlannerGuardrail] failed {name}\n{exception}");
            throw;
        }
    }
}
