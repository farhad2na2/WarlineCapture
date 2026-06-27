using NUnit.Framework;

public sealed class BattleScenarioLabModelTests
{
    [Test]
    public void CompareRadarSupport_ReportsEarlierDetectionAndLock()
    {
        BattleScenarioMetrics baseline = new()
        {
            VariantId = "AD-001-A-NoSupport-Normal",
            Detected = true,
            DetectionTimeSeconds = 0.4f,
            Locked = true,
            LockTimeSeconds = 0.75f,
            Intercepted = true,
            FailureReason = BattleScenarioFailureReason.None
        };
        BattleScenarioMetrics supported = new()
        {
            VariantId = "AD-001-B-RadarNear-Normal",
            Detected = true,
            DetectionTimeSeconds = 0.25f,
            Locked = true,
            LockTimeSeconds = 0.5f,
            Intercepted = true,
            RadarProviderUsed = true,
            FailureReason = BattleScenarioFailureReason.None
        };

        BattleScenarioComparison comparison = BattleScenarioResultComparison.CompareRadarSupport(baseline, supported);

        Assert.AreEqual("AD-001-A-NoSupport-Normal", comparison.BaselineVariantId);
        Assert.AreEqual("AD-001-B-RadarNear-Normal", comparison.SupportedVariantId);
        Assert.IsTrue(comparison.RadarImprovedDetectionTime);
        Assert.IsTrue(comparison.RadarImprovedLockTime);
        Assert.IsTrue(comparison.RadarImprovedOrMatchedOutcome);
        Assert.Less(comparison.DetectionTimeDeltaSeconds, 0f);
        Assert.Less(comparison.LockTimeDeltaSeconds, 0f);
    }

    [Test]
    public void CompareRadarSupport_UsesClosestDistanceWhenBothVariantsMiss()
    {
        BattleScenarioMetrics baseline = new()
        {
            VariantId = "AD-001-C-NoSupport-FastThreat",
            Detected = true,
            Locked = true,
            Intercepted = false,
            ClosestInterceptorDistanceToThreat = 9f,
            FailureReason = BattleScenarioFailureReason.InterceptorTimeout
        };
        BattleScenarioMetrics supported = new()
        {
            VariantId = "AD-001-D-RadarNear-FastThreat",
            Detected = true,
            Locked = true,
            Intercepted = false,
            ClosestInterceptorDistanceToThreat = 3f,
            RadarProviderUsed = true,
            FailureReason = BattleScenarioFailureReason.InterceptorTimeout
        };

        BattleScenarioComparison comparison = BattleScenarioResultComparison.CompareRadarSupport(baseline, supported);

        Assert.IsTrue(comparison.RadarImprovedOrMatchedOutcome);
    }

    [Test]
    public void BattleScenarioReportJson_IncludesScenarioVariantAndComparison()
    {
        BattleScenarioMetrics baseline = new()
        {
            ScenarioId = "AD-001",
            VariantId = "AD-001-A-NoSupport-Normal",
            Seed = 12345,
            Detected = true,
            Locked = true,
            Intercepted = true
        };
        BattleScenarioMetrics supported = new()
        {
            ScenarioId = "AD-001",
            VariantId = "AD-001-B-RadarNear-Normal",
            Seed = 12345,
            Detected = true,
            Locked = true,
            Intercepted = true,
            RadarProviderUsed = true
        };
        BattleScenarioResult result = new()
        {
            ScenarioId = "AD-001",
            GeneratedAtUtc = "2026-06-26T00:00:00Z",
            FixedDeltaTime = 0.05f,
            Variants = new[] { baseline, supported },
            Comparisons = new[] { BattleScenarioResultComparison.CompareRadarSupport(baseline, supported) },
            Passed = true,
            FailureReason = BattleScenarioFailureReason.None
        };

        string json = BattleScenarioReportJson.ToJson(result);

        StringAssert.Contains("\"ScenarioId\": \"AD-001\"", json);
        StringAssert.Contains("AD-001-A-NoSupport-Normal", json);
        StringAssert.Contains("AD-001-B-RadarNear-Normal", json);
        StringAssert.Contains("\"RadarProviderUsed\": true", json);
        StringAssert.Contains("\"Passed\": true", json);
        StringAssert.Contains("\"FailureReason\": \"None\"", json);
    }

    [Test]
    public void FixedStepRunner_CompletesWhenStepReportsIntercept()
    {
        BattleScenarioVariant variant = BattleScenarioVariant.CreateDefault(
            "AD-001-B-RadarNear-Normal",
            BattleScenarioSupportMode.RadarNear);

        BattleScenarioMetrics metrics = BattleScenarioFixedStepRunner.RunVariant(
            "AD-001",
            variant,
            123,
            0.05f,
            1f,
            (state, result) =>
            {
                if (state.Frame < 3)
                    return BattleScenarioStepOutcome.Continue;

                result.Intercepted = true;
                result.InterceptTimeSeconds = state.TimeSeconds;
                return BattleScenarioStepOutcome.Complete;
            });

        Assert.IsTrue(metrics.Intercepted);
        Assert.AreEqual(4, metrics.Frames);
        Assert.AreEqual(0.15f, metrics.InterceptTimeSeconds, 0.0001f);
        Assert.AreEqual(BattleScenarioFailureReason.None, metrics.FailureReason);
    }

    [Test]
    public void FixedStepRunner_TimesOutWhenNoTerminalOutcomeOccurs()
    {
        BattleScenarioVariant variant = BattleScenarioVariant.CreateDefault(
            "AD-001-A-NoSupport-Normal",
            BattleScenarioSupportMode.None);

        BattleScenarioMetrics metrics = BattleScenarioFixedStepRunner.RunVariant(
            "AD-001",
            variant,
            123,
            0.1f,
            0.3f,
            (_, _) => BattleScenarioStepOutcome.Continue);

        Assert.IsFalse(metrics.Intercepted);
        Assert.AreEqual(3, metrics.Frames);
        Assert.AreEqual(0.3f, metrics.DurationSeconds, 0.0001f);
        Assert.AreEqual(BattleScenarioFailureReason.InterceptorTimeout, metrics.FailureReason);
    }
}
