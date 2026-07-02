using NUnit.Framework;
using UnityEditor;
using Game.Editor;
using Game.Runtime;

public sealed class BattleScenarioAd001RunnerTests
{
    [Test]
    public void RunDefault_ProducesRadarImprovementMetrics()
    {
        BattleScenarioResult result = BattleScenarioAd001Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd001Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(4, result.Variants.Length);
        Assert.AreEqual(2, result.Comparisons.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.Greater(result.Variants[1].LauncherEffectiveRange, result.Variants[0].LauncherEffectiveRange);
        Assert.Less(result.Variants[1].LauncherEffectiveLockSeconds, result.Variants[0].LauncherEffectiveLockSeconds);
        Assert.Greater(result.Variants[1].LauncherEffectiveTrackingQuality, result.Variants[0].LauncherEffectiveTrackingQuality);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedLockTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Comparisons[1].RadarImprovedDetectionTime);
        Assert.IsTrue(result.Comparisons[1].RadarImprovedOrMatchedOutcome);
    }

    [Test]
    public void RunDefinition_UsesAd001ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad001DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad001DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd001Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(4, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd001Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(4, result.Variants.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Comparisons[1].RadarImprovedDetectionTime);
    }
}
