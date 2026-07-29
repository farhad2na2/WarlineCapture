using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AndroidBuildReportGeneratorTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            AndroidBuildReportGeneratorTests tests = new();
            tests.AggregationNormalizesAndCombinesSourcePathsAcrossPackedFiles();
            tests.EqualPackedSizesUseOrdinalPathTieBreak();
            tests.IncludedAssetTableIsCappedAtOneHundredRows();
            tests.CompleteTextureInventoryIncludesTexturesOutsideTopOneHundred();
            tests.CompleteTextureInventoryIsNormalizedDeduplicatedAndPathSorted();
            tests.AggregationRetainsDistinctObjectTypesInOrdinalOrder();
            tests.AccountingSeparatesAttributedUnattributedOverheadAndSummaryRemainder();
            tests.ReportCarriesRequiredEvidenceAndExplicitSizeSemantics();
            tests.ReleaseBuildOptionsRequireDetailedReport();
            tests.ReleaseBuildScriptOptionsDefaultToIncrementalAndSupportCleanCache();
            tests.ReleaseBuildCapturesGitProvenanceBeforeAndroidBuildMutation();
            tests.DenseCandidateProfilerBuildIsDevelopmentOnlyAndKeepsReleaseOptionsClosed();
            tests.ReleaseBuildOptionsRejectDebugAndProfilerFlags();
            tests.GitExecutableOverrideRequiresExistingAbsolutePath();
            Debug.Log("[AndroidBuildReportGeneratorValidation] result=Passed tests=14");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[AndroidBuildReportGeneratorValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void AggregationNormalizesAndCombinesSourcePathsAcrossPackedFiles()
    {
        AndroidPackedAssetAggregation result = Aggregate(
            new[]
            {
                Contribution("Assets\\Game\\Textures\\World.png", "UnityEngine.Texture2D", 120),
                Contribution("./Assets/Game/Textures/World.png", "UnityEngine.Texture2D", 80),
                Contribution("Assets/Game/Meshes/World.asset", "UnityEngine.Mesh", 50)
            },
            new ulong[] { 5, 7 },
            300);

        Assert.AreEqual(2, result.TotalIncludedAssetCount);
        Assert.AreEqual(250ul, result.AttributedPackedAssetBytes);
        Assert.AreEqual("Assets/Game/Textures/World.png", result.TopAssets[0].SourceAssetPath);
        Assert.AreEqual(200ul, result.TopAssets[0].PackedBytes);
    }

    [Test]
    public void EqualPackedSizesUseOrdinalPathTieBreak()
    {
        AndroidPackedAssetAggregation result = Aggregate(
            new[]
            {
                Contribution("Assets/Z.asset", "Z", 100),
                Contribution("Assets/A.asset", "A", 100),
                Contribution("Assets/M.asset", "M", 100)
            },
            Array.Empty<ulong>(),
            300);

        CollectionAssert.AreEqual(
            new[] { "Assets/A.asset", "Assets/M.asset", "Assets/Z.asset" },
            result.TopAssets.Select(asset => asset.SourceAssetPath));
    }

    [Test]
    public void IncludedAssetTableIsCappedAtOneHundredRows()
    {
        var contributions = new List<AndroidPackedAssetContribution>();
        for (int index = 0; index < 101; index++)
            contributions.Add(Contribution($"Assets/Asset{index:D3}.asset", "Asset", checked((ulong)index)));

        AndroidPackedAssetAggregation result = Aggregate(
            contributions,
            new ulong[] { 0 },
            5050);

        Assert.AreEqual(101, result.TotalIncludedAssetCount);
        Assert.AreEqual(100, result.ReportedIncludedAssetCount);
        Assert.AreEqual(100, result.TopAssets.Count);
        Assert.AreEqual(100ul, result.TopAssets[0].PackedBytes);
        Assert.AreEqual(1ul, result.TopAssets[99].PackedBytes);
    }

    [Test]
    public void CompleteTextureInventoryIncludesTexturesOutsideTopOneHundred()
    {
        var contributions = new List<AndroidPackedAssetContribution>();
        for (int index = 0; index < 100; index++)
        {
            contributions.Add(Contribution(
                $"Assets/Mesh{index:D3}.asset",
                "UnityEngine.Mesh",
                checked((ulong)(1000 - index))));
        }
        contributions.Add(Contribution(
            "Assets/Textures/LowRanked.png",
            "UnityEngine.Texture2D",
            1));

        AndroidPackedAssetAggregation result = Aggregate(
            contributions,
            Array.Empty<ulong>(),
            95051);

        Assert.AreEqual(100, result.TopAssets.Count);
        Assert.IsFalse(result.TopAssets.Any(asset => asset.SourceAssetPath.EndsWith("LowRanked.png")));
        Assert.AreEqual(1, result.AllIncludedTextures.Count);
        Assert.AreEqual("Assets/Textures/LowRanked.png", result.AllIncludedTextures[0].SourceAssetPath);
    }

    [Test]
    public void CompleteTextureInventoryIsNormalizedDeduplicatedAndPathSorted()
    {
        AndroidPackedAssetAggregation result = Aggregate(
            new[]
            {
                Contribution("Assets\\Textures\\Z.png", "UnityEngine.Texture2D", 10),
                Contribution("./Assets/Textures/A.png", "UnityEngine.Texture2D", 20),
                Contribution("Assets/Textures/A.png", "UnityEngine.Sprite", 30),
                Contribution("Assets/Mesh.asset", "UnityEngine.Mesh", 40)
            },
            Array.Empty<ulong>(),
            100);

        CollectionAssert.AreEqual(
            new[] { "Assets/Textures/A.png", "Assets/Textures/Z.png" },
            result.AllIncludedTextures.Select(asset => asset.SourceAssetPath));
        Assert.AreEqual(50ul, result.AllIncludedTextures[0].PackedBytes);
        CollectionAssert.AreEqual(
            new[] { "UnityEngine.Sprite", "UnityEngine.Texture2D" },
            result.AllIncludedTextures[0].ObjectTypes);
    }

    [Test]
    public void AggregationRetainsDistinctObjectTypesInOrdinalOrder()
    {
        AndroidPackedAssetAggregation result = Aggregate(
            new[]
            {
                Contribution("Assets/Mixed.asset", "UnityEngine.Texture2D", 10),
                Contribution("Assets/Mixed.asset", "UnityEngine.AudioClip", 20),
                Contribution("Assets/Mixed.asset", "UnityEngine.Texture2D", 30),
                Contribution("Assets/Mixed.asset", null, 40)
            },
            Array.Empty<ulong>(),
            100);

        CollectionAssert.AreEqual(
            new[] { "UnityEngine.AudioClip", "UnityEngine.Texture2D", "Unknown" },
            result.TopAssets.Single().ObjectTypes);
    }

    [Test]
    public void AccountingSeparatesAttributedUnattributedOverheadAndSummaryRemainder()
    {
        AndroidPackedAssetAggregation result = Aggregate(
            new[]
            {
                Contribution("Assets/A.asset", "A", 300),
                Contribution(null, "Generated", 30),
                Contribution("   ", "Generated", 20)
            },
            new ulong[] { 10, 20 },
            1000);

        Assert.AreEqual(300ul, result.AttributedPackedAssetBytes);
        Assert.AreEqual(50ul, result.UnattributedPackedAssetBytes);
        Assert.AreEqual(350ul, result.PackedContentBytes);
        Assert.AreEqual(30ul, result.PackedFileOverheadBytes);
        Assert.AreEqual(380ul, result.AccountedPackedFileBytes);
        Assert.AreEqual(1000ul, result.BuildReportSummaryTotalSizeBytes);
        Assert.AreEqual(620L, result.BuildReportSummaryUnaccountedBytes);
        Assert.AreEqual(2, result.UnattributedPackedEntryCount);
    }

    [Test]
    public void ReportCarriesRequiredEvidenceAndExplicitSizeSemantics()
    {
        AndroidPackedAssetAggregation aggregation = Aggregate(
            new[] { Contribution("Assets/A.asset", "UnityEngine.Texture2D", 100) },
            new ulong[] { 10 },
            150);
        var evidence = new AndroidBuildReportEvidence
        {
            PackageType = "apk",
            ExactCommit = "0123456789abcdef0123456789abcdef01234567",
            Dirty = true,
            UnityVersion = "6000.5.2f1",
            ScriptingBackend = "IL2CPP",
            TargetArchitecture = "ARM64",
            FrameTimingStatsEnabled = true,
            ArtifactPath = "Build\\AndroidAPK\\WarlineCapture.apk",
            ArtifactBytes = 75,
            ArtifactSha256 = new string('A', 64)
        };

        AndroidBuildReportDocument report = AndroidBuildReportGenerator.CreateReport(evidence, aggregation);
        string json = AndroidBuildReportGenerator.SerializeReport(report);
        string markdown = AndroidBuildReportGenerator.BuildMarkdown(report);

        Assert.AreEqual("APH-500", report.TaskId);
        Assert.AreEqual("0123456789abcdef0123456789abcdef01234567", report.ExactCommit);
        Assert.IsTrue(report.Dirty);
        Assert.AreEqual("6000.5.2f1", report.UnityVersion);
        Assert.AreEqual("release", report.ReleaseBuildType);
        Assert.AreEqual("APK", report.PackageType);
        Assert.AreEqual("Android", report.BuildTarget);
        Assert.AreEqual("IL2CPP", report.ScriptingBackend);
        Assert.AreEqual("ARM64", report.TargetArchitecture);
        Assert.IsTrue(report.FrameTimingStatsEnabled);
        Assert.IsTrue(report.DetailedBuildReport);
        Assert.AreEqual("Build/AndroidAPK/WarlineCapture.apk", report.ArtifactPath);
        Assert.AreEqual(75ul, report.ArtifactBytes);
        Assert.AreEqual(new string('a', 64), report.ArtifactSha256);
        Assert.IsTrue(report.AllIncludedTexturePathsExported);
        Assert.AreEqual(1, report.BuildReportIncludedTextures.Count);
        Assert.AreEqual("Assets/A.asset", report.BuildReportIncludedTextures[0].SourceAssetPath);
        StringAssert.Contains("\"buildReportIncludedAssets\"", json);
        StringAssert.Contains("\"frameTimingStatsEnabled\": true", json);
        StringAssert.Contains("\"allIncludedTexturePathsExported\": true", json);
        StringAssert.Contains("\"buildReportIncludedTextures\"", json);
        StringAssert.Contains("Compressed APK/AAB package", json);
        StringAssert.Contains("not a per-asset compressed-byte attribution", markdown);
    }

    [Test]
    public void ReleaseBuildOptionsRequireDetailedReport()
    {
        Assert.Throws<InvalidOperationException>(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(BuildOptions.None));
        Assert.Throws<InvalidOperationException>(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(BuildOptions.CleanBuildCache));
        Assert.DoesNotThrow(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(BuildOptions.DetailedBuildReport));
        Assert.DoesNotThrow(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(
                BuildOptions.DetailedBuildReport | BuildOptions.CleanBuildCache));
    }

    [Test]
    public void ReleaseBuildScriptOptionsDefaultToIncrementalAndSupportCleanCache()
    {
        const BuildOptions forbidden =
            BuildOptions.Development |
            BuildOptions.AllowDebugging |
            BuildOptions.ConnectWithProfiler |
            BuildOptions.EnableDeepProfilingSupport;

        Assert.AreEqual(
            BuildOptions.DetailedBuildReport,
            BuildScript.ReleaseAndroidBuildOptions);
        Assert.AreEqual(
            BuildOptions.DetailedBuildReport | BuildOptions.CleanBuildCache,
            BuildScript.CleanReleaseAndroidBuildOptions);
        Assert.AreEqual(BuildOptions.None, BuildScript.ReleaseAndroidBuildOptions & forbidden);
        Assert.AreEqual(BuildOptions.None, BuildScript.CleanReleaseAndroidBuildOptions & forbidden);
        Assert.DoesNotThrow(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(
                BuildScript.ReleaseAndroidBuildOptions));
        Assert.DoesNotThrow(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(
                BuildScript.CleanReleaseAndroidBuildOptions));
    }

    [Test]
    public void ReleaseBuildCapturesGitProvenanceBeforeAndroidBuildMutation()
    {
        const string sourcePath = "Assets/Game/Scripts/Editor/BuildScript.cs";
        string source = File.ReadAllText(sourcePath);
        int methodStart = source.IndexOf(
            "public static void BuildAndroid()",
            StringComparison.Ordinal);
        int methodEnd = source.IndexOf(
            "public static void BuildAndroidProfilerApk()",
            methodStart,
            StringComparison.Ordinal);
        Assert.GreaterOrEqual(methodStart, 0, "BuildAndroid method was not found.");
        Assert.Greater(methodEnd, methodStart, "BuildAndroid method boundary was not found.");

        string method = source.Substring(methodStart, methodEnd - methodStart);
        int provenanceCapture = method.IndexOf(
            "AndroidBuildReportGenerator.CaptureGitProvenance()",
            StringComparison.Ordinal);
        int buildTargetMutation = method.IndexOf(
            "SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)",
            StringComparison.Ordinal);
        Assert.GreaterOrEqual(provenanceCapture, 0, "Pre-build provenance capture is missing.");
        Assert.Greater(buildTargetMutation, provenanceCapture,
            "Git provenance must be captured before Android build settings can mutate the worktree.");
        StringAssert.Contains(
            "buildType,\n                buildProvenance",
            method,
            "Report generation must consume the pre-build provenance snapshot.");
    }

    [Test]
    public void DenseCandidateProfilerBuildIsDevelopmentOnlyAndKeepsReleaseOptionsClosed()
    {
        const string sourcePath = "Assets/Game/Scripts/Editor/BuildScript.cs";
        string source = File.ReadAllText(sourcePath);
        int methodStart = source.IndexOf(
            "private static void BuildDenseCityCandidateAndroidApk(bool profilerBuild)",
            StringComparison.Ordinal);
        int methodEnd = source.IndexOf(
            "public static void ValidateDenseCityCandidateAndroidApk()",
            methodStart,
            StringComparison.Ordinal);
        Assert.GreaterOrEqual(methodStart, 0, "Dense candidate build method was not found.");
        Assert.Greater(methodEnd, methodStart, "Dense candidate build method boundary was not found.");

        string method = source.Substring(methodStart, methodEnd - methodStart);
        StringAssert.Contains(
            "profilerBuild\n                    ? BuildOptions.Development",
            method);
        StringAssert.DoesNotContain("BuildOptions.ConnectWithProfiler", method);
        StringAssert.DoesNotContain("BuildOptions.EnableDeepProfilingSupport", method);
        StringAssert.Contains("Build/AndroidDenseCandidateProfiler", method);
        StringAssert.Contains("Build/AndroidDenseCandidate", method);
        StringAssert.Contains(
            "OperationMapDenseCityCandidateAndroidPackageDeployment.ValidatePackage",
            method);
        Assert.AreEqual(BuildOptions.None, BuildScript.ReleaseAndroidBuildOptions & BuildOptions.Development);
        Assert.AreEqual(BuildOptions.None, BuildScript.CleanReleaseAndroidBuildOptions & BuildOptions.Development);
    }

    [Test]
    public void GitExecutableOverrideRequiresExistingAbsolutePath()
    {
        Assert.AreEqual(
            "git",
            AndroidBuildReportGenerator.ResolveGitExecutable(null));
        Assert.Throws<InvalidOperationException>(
            () => AndroidBuildReportGenerator.ResolveGitExecutable("git.exe"));
        Assert.Throws<InvalidOperationException>(
            () => AndroidBuildReportGenerator.ResolveGitExecutable(
                Path.Combine(
                    Path.GetTempPath(),
                    $"warline-missing-git-{Guid.NewGuid():N}.exe")));

        string existingAbsolutePath = Path.GetFullPath(
            typeof(AndroidBuildReportGeneratorTests).Assembly.Location);
        Assert.AreEqual(
            existingAbsolutePath,
            AndroidBuildReportGenerator.ResolveGitExecutable(
                $"  {existingAbsolutePath}  "));
    }

    [Test]
    public void ReleaseBuildOptionsRejectDebugAndProfilerFlags()
    {
        BuildOptions[] forbiddenOptions =
        {
            BuildOptions.Development,
            BuildOptions.AllowDebugging,
            BuildOptions.ConnectWithProfiler,
            BuildOptions.EnableDeepProfilingSupport
        };

        for (int index = 0; index < forbiddenOptions.Length; index++)
        {
            BuildOptions options =
                BuildOptions.DetailedBuildReport |
                BuildOptions.CleanBuildCache |
                forbiddenOptions[index];
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(options));
            StringAssert.Contains("release APK/AAB", exception.Message);
        }
    }

    private static AndroidPackedAssetAggregation Aggregate(
        IEnumerable<AndroidPackedAssetContribution> contributions,
        IEnumerable<ulong> overheads,
        ulong summaryBytes)
    {
        return AndroidBuildReportGenerator.AggregatePackedAssets(
            contributions,
            overheads,
            summaryBytes);
    }

    private static AndroidPackedAssetContribution Contribution(
        string path,
        string objectType,
        ulong packedBytes)
    {
        return new AndroidPackedAssetContribution
        {
            SourceAssetPath = path,
            ObjectType = objectType,
            PackedBytes = packedBytes
        };
    }
}
