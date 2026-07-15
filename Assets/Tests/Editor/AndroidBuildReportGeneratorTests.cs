using System;
using System.Collections.Generic;
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
            tests.ReleaseBuildOptionsRequireDetailedReportAndCleanBuildCache();
            tests.ReleaseBuildScriptOptionsIncludeCleanCacheWithoutDebugOrProfilerFlags();
            tests.ReleaseBuildOptionsRejectDebugAndProfilerFlags();
            Debug.Log("[AndroidBuildReportGeneratorValidation] result=Passed tests=11");
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
        Assert.IsTrue(report.DetailedBuildReport);
        Assert.AreEqual("Build/AndroidAPK/WarlineCapture.apk", report.ArtifactPath);
        Assert.AreEqual(75ul, report.ArtifactBytes);
        Assert.AreEqual(new string('a', 64), report.ArtifactSha256);
        Assert.IsTrue(report.AllIncludedTexturePathsExported);
        Assert.AreEqual(1, report.BuildReportIncludedTextures.Count);
        Assert.AreEqual("Assets/A.asset", report.BuildReportIncludedTextures[0].SourceAssetPath);
        StringAssert.Contains("\"buildReportIncludedAssets\"", json);
        StringAssert.Contains("\"allIncludedTexturePathsExported\": true", json);
        StringAssert.Contains("\"buildReportIncludedTextures\"", json);
        StringAssert.Contains("Compressed APK/AAB package", json);
        StringAssert.Contains("not a per-asset compressed-byte attribution", markdown);
    }

    [Test]
    public void ReleaseBuildOptionsRequireDetailedReportAndCleanBuildCache()
    {
        Assert.Throws<InvalidOperationException>(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(BuildOptions.None));
        Assert.Throws<InvalidOperationException>(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(BuildOptions.DetailedBuildReport));
        Assert.Throws<InvalidOperationException>(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(BuildOptions.CleanBuildCache));
        Assert.DoesNotThrow(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(
                BuildOptions.DetailedBuildReport | BuildOptions.CleanBuildCache));
    }

    [Test]
    public void ReleaseBuildScriptOptionsIncludeCleanCacheWithoutDebugOrProfilerFlags()
    {
        const BuildOptions required =
            BuildOptions.DetailedBuildReport |
            BuildOptions.CleanBuildCache;
        const BuildOptions forbidden =
            BuildOptions.Development |
            BuildOptions.AllowDebugging |
            BuildOptions.ConnectWithProfiler |
            BuildOptions.EnableDeepProfilingSupport;

        Assert.AreEqual(required, BuildScript.ReleaseAndroidBuildOptions & required);
        Assert.AreEqual(BuildOptions.None, BuildScript.ReleaseAndroidBuildOptions & forbidden);
        Assert.DoesNotThrow(
            () => AndroidBuildReportGenerator.ValidateReleaseBuildOptions(
                BuildScript.ReleaseAndroidBuildOptions));
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
            StringAssert.Contains("clean release", exception.Message);
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
