using System;
using System.IO;
using NUnit.Framework;

public sealed class OperationMapAddressablesContentBuildIntegrationTests
{
    private const string BuildScriptPath = "Assets/Game/Scripts/Editor/BuildScript.cs";

    [Test]
    public void ReleaseAndroidBuild_ProducesAddressablesBeforeSceneResolutionAndPlayerBuild()
    {
        string source = File.ReadAllText(BuildScriptPath);
        string method = Slice(
            source,
            "public static void BuildAndroid()",
            "public static void BuildAndroidProfilerApk()");

        AssertOrdered(method,
            "SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)",
            "ConfigureAndroidBuild(buildType == \"AAB\")",
            "OperationMapAddressablesBuildReportBuilder.Run()",
            "StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject",
            "ExecuteBuild(buildPlayerOptions)");
    }

    [Test]
    public void ProfilerAndroidBuild_ProducesAddressablesBeforeSceneResolutionAndPlayerBuild()
    {
        string source = File.ReadAllText(BuildScriptPath);
        string method = Slice(
            source,
            "private static void BuildAndroidProfilerApk(bool disableBurstAot)",
            "public static void CreateDirectory(string path)");

        AssertOrdered(method,
            "SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)",
            "ConfigureAndroidBuild(false)",
            "OperationMapAddressablesBuildReportBuilder.Run()",
            "StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject",
            "ExecuteBuild(buildPlayerOptions)");
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        int end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.GreaterOrEqual(start, 0, $"Method start was not found: {startMarker}");
        Assert.Greater(end, start, $"Method end was not found: {endMarker}");
        return source.Substring(start, end - start);
    }

    private static void AssertOrdered(string source, params string[] markers)
    {
        int previous = -1;
        for (int index = 0; index < markers.Length; index++)
        {
            int current = source.IndexOf(markers[index], StringComparison.Ordinal);
            Assert.Greater(current, previous, $"Build step is absent or out of order: {markers[index]}");
            previous = current;
        }
    }
}
