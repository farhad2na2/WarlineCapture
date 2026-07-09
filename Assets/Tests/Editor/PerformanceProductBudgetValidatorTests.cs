using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class PerformanceProductBudgetValidatorTests
{
    private const string ConfigPath =
        "Design/Architecture/performance_regression_accepted_baseline.json";

    public static void RunFocusedValidation()
    {
        try
        {
            PerformanceProductBudgetValidatorTests tests = new();
            tests.TrackedConfig_HasStrictSchemaAndApprovedThresholds();
            tests.MeasurementRequiredLimits_AreNullAndOwned();
            tests.Validator_RejectsSilentBudgetLoosening();
            tests.Validator_RejectsSchemaDriftMissingOwnershipAndEvidence();
            Debug.Log("[PerformanceProductBudgetValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[PerformanceProductBudgetValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void TrackedConfig_HasStrictSchemaAndApprovedThresholds()
    {
        string json = ReadConfig();
        PerformanceProductBudgetValidator.ValidateJson(json);

        JsonObject root = ParseConfig(json);
        Assert.AreEqual(2, root["acceptedBaselineVersion"].GetValue<int>());
        Assert.AreEqual(50d, root["editorP95FrameBudgetMs"].GetValue<double>());

        JsonNode frames = root["productBudgets"]["androidFrameP95AfterWarmup"];
        Assert.AreEqual("lessThan", frames["comparison"].GetValue<string>());
        Assert.AreEqual(33d, frames["baseline"].GetValue<double>());
        Assert.AreEqual(33d, frames["recommended"].GetValue<double>());
        Assert.AreEqual(25d, frames["highEnd"].GetValue<double>());

        JsonNode gc = root["productBudgets"]["matchSteadyStateGc"];
        Assert.AreEqual("red-baseline", gc["status"].GetValue<string>());
        Assert.AreEqual(269482L, gc["baselineAllocatedBytes"].GetValue<long>());
        Assert.AreEqual(1024L, gc["acceptanceBudgetBytes"].GetValue<long>());
        Assert.AreEqual(180, gc["warmupFrames"].GetValue<int>());
        Assert.AreEqual(300, gc["measuredFrames"].GetValue<int>());

        JsonNode memory = root["productBudgets"]["peakAllocatedMemory"];
        Assert.AreEqual("MB", memory["baseline"]["unit"].GetValue<string>());
        Assert.AreEqual(1054, memory["baseline"]["minimum"].GetValue<int>());
        Assert.AreEqual(1075, memory["baseline"]["maximum"].GetValue<int>());
        Assert.AreEqual(10d, memory["target"]["requiredReductionPercent"].GetValue<double>());
        Assert.IsTrue(memory["target"]["sameDeviceRequired"].GetValue<bool>());
        Assert.AreEqual(
            "uncertain-measurement-required",
            memory["runtimeResidency"]["status"].GetValue<string>());

        JsonNode knownProfilerApk = root["productBudgets"]["releaseEvidence"]["knownProfilerApkBaseline"];
        Assert.AreEqual(
            "baseline-evidence-only-not-release-limit",
            knownProfilerApk["status"].GetValue<string>());
        Assert.IsFalse(knownProfilerApk["isReleaseLimit"].GetValue<bool>());
    }

    [Test]
    public void MeasurementRequiredLimits_AreNullAndOwned()
    {
        JsonObject root = ParseConfig(ReadConfig());
        JsonNode productBudgets = root["productBudgets"];
        JsonNode release = productBudgets["releaseEvidence"];

        AssertMeasurementRequired(
            productBudgets["peakAllocatedMemory"]["target"],
            "absoluteReleaseLimitMB",
            "ownerTaskId",
            "APH-501");
        Assert.AreEqual(
            "APH-501",
            productBudgets["peakAllocatedMemory"]["runtimeResidency"]["ownerTaskId"].GetValue<string>());
        AssertMeasurementRequired(release["apk"], "releaseLimitBytes", "measurementOwnerTaskId", "APH-500");
        Assert.AreEqual("APH-501", release["apk"]["budgetOwnerTaskId"].GetValue<string>());
        AssertMeasurementRequired(release["aab"], "releaseLimitBytes", "measurementOwnerTaskId", "APH-500");
        Assert.AreEqual("APH-501", release["aab"]["budgetOwnerTaskId"].GetValue<string>());
        AssertMeasurementRequired(release["installedSize"], "releaseLimitBytes", "ownerTaskId", "APH-501");
        AssertMeasurementRequired(release["startupTime"], "p95LimitMs", "ownerTaskId", "APH-803");
        Assert.AreEqual("APH-809", release["visualQuality"]["ownerTaskId"].GetValue<string>());

        Assert.That(EvidenceValues(release["apk"]), Does.Contain("artifactBytes"));
        Assert.That(EvidenceValues(release["aab"]), Does.Contain("buildReportIncludedAssets"));
        Assert.That(EvidenceValues(release["installedSize"]), Does.Contain("installedBytes"));
        Assert.That(EvidenceValues(release["startupTime"]), Does.Contain("coldStartSamples"));
        Assert.That(EvidenceValues(release["visualQuality"]), Does.Contain("sameCameraBeforeAfter"));
    }

    [Test]
    public void Validator_RejectsSilentBudgetLoosening()
    {
        string json = ReadConfig();
        var mutations = new Dictionary<string, JsonNode>
        {
            ["editorP95FrameBudgetMs"] = 50.01d,
            ["currentThreadAllocatedBytesBudget"] = 1,
            ["minimumFrameCount"] = 179,
            ["minimumUnitCount"] = 699,
            ["minimumRuntimeBuildingCount"] = 599,
            ["minimumVisibleModelEstimate"] = 39,
            ["productBudgets.androidFrameP95AfterWarmup.baseline"] = 33.01d,
            ["productBudgets.androidFrameP95AfterWarmup.recommended"] = 33.01d,
            ["productBudgets.androidFrameP95AfterWarmup.highEnd"] = 25.01d,
            ["productBudgets.matchSteadyStateGc.acceptanceBudgetBytes"] = 1025,
            ["productBudgets.peakAllocatedMemory.target.requiredReductionPercent"] = 9.99d
        };

        foreach (KeyValuePair<string, JsonNode> mutation in mutations)
        {
            JsonObject mutated = ParseConfig(json);
            SetValue(mutated, mutation.Key, mutation.Value);
            Assert.Throws<InvalidDataException>(
                () => PerformanceProductBudgetValidator.ValidateJson(mutated.ToJsonString()),
                $"Expected validator to reject loosened value at {mutation.Key}.");
        }
    }

    [Test]
    public void Validator_RejectsSchemaDriftMissingOwnershipAndEvidence()
    {
        string json = ReadConfig();

        JsonObject unknownField = ParseConfig(json);
        unknownField["productBudgets"]["untrackedBudget"] = 1;
        Assert.Throws<InvalidDataException>(
            () => PerformanceProductBudgetValidator.ValidateJson(unknownField.ToJsonString()));

        JsonObject missingOwner = ParseConfig(json);
        missingOwner["productBudgets"]["releaseEvidence"]["startupTime"]["ownerTaskId"] = null;
        Assert.Throws<InvalidDataException>(
            () => PerformanceProductBudgetValidator.ValidateJson(missingOwner.ToJsonString()));

        JsonObject inventedLimit = ParseConfig(json);
        inventedLimit["productBudgets"]["releaseEvidence"]["apk"]["releaseLimitBytes"] = 500000000;
        Assert.Throws<InvalidDataException>(
            () => PerformanceProductBudgetValidator.ValidateJson(inventedLimit.ToJsonString()));

        JsonObject missingEvidence = ParseConfig(json);
        JsonArray visualEvidence = missingEvidence["productBudgets"]["releaseEvidence"]
            ["visualQuality"]["requiredEvidence"].AsArray();
        int reviewerDecisionIndex = visualEvidence
            .Select((value, index) => new { Value = value.GetValue<string>(), Index = index })
            .First(item => item.Value == "reviewerDecision")
            .Index;
        visualEvidence.RemoveAt(reviewerDecisionIndex);
        Assert.Throws<InvalidDataException>(
            () => PerformanceProductBudgetValidator.ValidateJson(missingEvidence.ToJsonString()));
    }

    private static void AssertMeasurementRequired(
        JsonNode value,
        string limitProperty,
        string ownerProperty,
        string ownerTaskId)
    {
        Assert.NotNull(value);
        JsonObject objectValue = value.AsObject();
        string statusProperty =
            limitProperty == "absoluteReleaseLimitMB" ? "absoluteReleaseLimitStatus" : "status";
        Assert.AreEqual("measurement-required", objectValue[statusProperty].GetValue<string>());
        Assert.IsTrue(objectValue.ContainsKey(limitProperty));
        Assert.IsNull(objectValue[limitProperty]);
        Assert.AreEqual(ownerTaskId, objectValue[ownerProperty].GetValue<string>());
    }

    private static IEnumerable<string> EvidenceValues(JsonNode value)
    {
        return value["requiredEvidence"].AsArray().Select(item => item.GetValue<string>());
    }

    private static JsonObject ParseConfig(string json)
    {
        return JsonNode.Parse(json)?.AsObject() ??
               throw new InvalidDataException("Tracked budget config did not contain a JSON object.");
    }

    private static void SetValue(JsonObject root, string path, JsonNode value)
    {
        string[] segments = path.Split('.');
        JsonObject parent = root;
        for (int index = 0; index < segments.Length - 1; index++)
            parent = parent[segments[index]].AsObject();

        parent[segments[segments.Length - 1]] = value;
    }

    private static string ReadConfig()
    {
        Assert.IsTrue(File.Exists(ConfigPath), $"Missing tracked budget config at {ConfigPath}.");
        return File.ReadAllText(ConfigPath);
    }
}
