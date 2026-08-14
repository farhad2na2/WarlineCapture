#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public static class M01FirstContactBurstAotArchitectureTests
{
    public const string PassMarker =
        "[M01FirstContactBurstAotArchitectureTests] result=Passed tests=3";

    private const string RuntimePath =
        "Assets/Game/Scripts/Runtime/Missions/CampaignMissionRuntimeSystem.cs";
    private const string GuidancePath =
        "Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidanceProjectionSystem.cs";

    private static readonly string[] RuntimeBurstReasons =
    {
        "stale-result-action",
        "unsupported-result-action",
        "result-not-settled",
        "invalid-result-transition",
        "retry-unavailable",
        "retry-already-queued"
    };

    private static readonly string[] GuidanceLiterals =
    {
        "Find your squad",
        "Move to cover",
        "Confirm the threat",
        "Engage the patrol",
        "Secure the corridor",
        "Select the command squad to begin.",
        "Move the squad to the marked cover position.",
        "Inspect the armed patrol near the civilians.",
        "Attack the confirmed hostile patrol.",
        "Check the objective and secure the civilian route.",
        " Use Show Me if you need the exact target.",
        "DO IT",
        "SHOW ME",
        "anchor.ch01.m01.move_target",
        "anchor.ch01.m01.patrol_objective",
        "anchor.ch01.m01.civilian_safe_zone"
    };

    public static void RunFocusedValidation()
    {
        try
        {
            RuntimeBurstReasonsUseUnmanagedConstants();
            GuidanceBurstTextUsesUnmanagedConstants();
            BurstCompilationRemainsEnabled();
            Debug.Log(PassMarker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("[M01FirstContactBurstAotArchitectureTests] result=Failed");
            Debug.LogException(exception);
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public static void RuntimeBurstReasonsUseUnmanagedConstants()
    {
        string source = Read(RuntimePath);
        foreach (string literal in RuntimeBurstReasons)
        {
            StringAssert.Contains($"= \"{literal}\";", source);
            StringAssert.DoesNotContain($"new FixedString64Bytes(\"{literal}\")", source);
        }
    }

    [Test]
    public static void GuidanceBurstTextUsesUnmanagedConstants()
    {
        string source = Read(GuidancePath);
        foreach (string literal in GuidanceLiterals)
            StringAssert.Contains($"= \"{literal}\";", source);

        Assert.That(source, Does.Not.Match(@"new\s+FixedString(?:64|128)Bytes\s*\(\s*\"""));
        Assert.That(source, Does.Not.Match(@"\.Append\s*\(\s*\"""));
    }

    [Test]
    public static void BurstCompilationRemainsEnabled()
    {
        StringAssert.Contains("[BurstCompile]", Read(RuntimePath));
        string guidance = Read(GuidancePath);
        StringAssert.Contains("[BurstCompile, UpdateInGroup", guidance);
        StringAssert.Contains("[BurstCompile] public void OnCreate", guidance);
        StringAssert.Contains("[BurstCompile]", guidance);
    }

    private static string Read(string path)
    {
        Assert.That(File.Exists(path), Is.True, $"Missing governed source `{path}`.");
        return File.ReadAllText(path);
    }
}
#endif
