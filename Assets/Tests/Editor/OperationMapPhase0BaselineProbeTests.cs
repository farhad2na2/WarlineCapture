using System;
using System.IO;
using System.Linq;
using System.Text;
using Game.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class OperationMapPhase0BaselineProbeTests
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string MatchSubScenePath = "Assets/Game/Scenes/Match/MatchSubScene.unity";
    private const string ManifestPath =
        "Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset";
    private const string IntegrityPath =
        "Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationSceneIntegrity.json";

    [Test]
    public void ComputeSha256_IsStableAndLowercase()
    {
        string hash = OperationMapPhase0BaselineProbe.ComputeSha256(Encoding.UTF8.GetBytes("abc"));

        Assert.That(
            hash,
            Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
    }

    [Test]
    public void ComputeAggregateHash_IsOrderIndependentAndRejectsDuplicatePaths()
    {
        var first = new[]
        {
            new OperationMapPhase0BaselineProbe.HashInput("b", "02"),
            new OperationMapPhase0BaselineProbe.HashInput("a", "01")
        };
        var second = first.Reverse().ToArray();

        Assert.That(
            OperationMapPhase0BaselineProbe.ComputeAggregateHash(first),
            Is.EqualTo(OperationMapPhase0BaselineProbe.ComputeAggregateHash(second)));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ComputeAggregateHash(new[]
            {
                new OperationMapPhase0BaselineProbe.HashInput("a", "01"),
                new OperationMapPhase0BaselineProbe.HashInput("a", "02")
            }));
    }

    [Test]
    public void ResolveReportOutputPath_UsesDefaultAndRejectsUnsafeLocations()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

        Assert.That(
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, null),
            Is.EqualTo(OperationMapPhase0BaselineProbe.DefaultReportPath));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "Assets", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "Packages", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "ProjectSettings", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, "relative.json"));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, "/private/tmp/probe.txt"));
    }

    [Test]
    public void HasRequiredReportShape_RequiresIdentityAndAllMajorSections()
    {
        var report = new OperationMapPhase0BaselineProbe.BaselineReport
        {
            reportSchema = OperationMapPhase0BaselineProbe.ReportSchema,
            reportSchemaVersion = OperationMapPhase0BaselineProbe.ReportSchemaVersion,
            result = "Passed",
            project = new OperationMapPhase0BaselineProbe.ProjectReport(),
            sceneSetupBeforeProbe = new(),
            scenes = new(),
            matchSceneViewReferences = new(),
            manifest = new OperationMapPhase0BaselineProbe.ManifestReport(),
            generatedOutputs = new OperationMapPhase0BaselineProbe.GeneratedOutputsReport(),
            buildSettingsScenes = new(),
            buildingPlacements = new OperationMapPhase0BaselineProbe.PlacementReport(),
            vehiclePlacements = new OperationMapPhase0BaselineProbe.PlacementReport(),
            mapData = new OperationMapPhase0BaselineProbe.MapDataReport()
        };

        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.True);
        report.result = "Failed";
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);
        Assert.That(OperationMapPhase0BaselineProbe.HasRequiredReportShape("{}"), Is.False);
    }

    [Test]
    public void Run_WritesOnlyExternalReportAndLeavesOwnedProjectFilesUnchanged()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(
            "/private/tmp",
            $"warline-operation-map-phase0-baseline-test-{Guid.NewGuid():N}.json");
        string previousOverride = Environment.GetEnvironmentVariable(
            OperationMapPhase0BaselineProbe.ReportPathEnvironmentVariable);
        string[] trackedProbeInputs =
        {
            MatchScenePath,
            MatchSubScenePath,
            ManifestPath,
            IntegrityPath
        };
        string[] beforeHashes = trackedProbeInputs
            .Select(path => OperationMapPhase0BaselineProbe.ComputeSha256(
                File.ReadAllBytes(Path.Combine(projectRoot, path))))
            .ToArray();
        var setupBefore = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            Environment.SetEnvironmentVariable(
                OperationMapPhase0BaselineProbe.ReportPathEnvironmentVariable,
                outputPath);
            OperationMapPhase0BaselineProbe.Run();

            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(
                OperationMapPhase0BaselineProbe.HasRequiredReportShape(File.ReadAllText(outputPath)),
                Is.True);
            Assert.That(
                trackedProbeInputs.Select(path => OperationMapPhase0BaselineProbe.ComputeSha256(
                    File.ReadAllBytes(Path.Combine(projectRoot, path)))),
                Is.EqualTo(beforeHashes));
            Assert.That(EditorSceneManager.GetSceneManagerSetup(), Is.EqualTo(setupBefore));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                OperationMapPhase0BaselineProbe.ReportPathEnvironmentVariable,
                previousOverride);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
