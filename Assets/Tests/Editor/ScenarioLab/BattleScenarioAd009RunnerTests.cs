using NUnit.Framework;
using UnityEditor;

public sealed class BattleScenarioAd009RunnerTests
{
    [Test]
    public void RunDefault_ComparesRadarSatelliteAndCombinedSupport()
    {
        BattleScenarioResult result = BattleScenarioAd009Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd009Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(4, result.Variants.Length);
        Assert.AreEqual(3, result.Comparisons.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsFalse(result.Variants[1].SatelliteProviderUsed);
        Assert.IsFalse(result.Variants[2].RadarProviderUsed);
        Assert.IsTrue(result.Variants[2].SatelliteProviderUsed);
        Assert.IsTrue(result.Variants[3].RadarProviderUsed);
        Assert.IsTrue(result.Variants[3].SatelliteProviderUsed);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsTrue(result.Variants[2].Intercepted);
        Assert.IsTrue(result.Variants[3].Intercepted);
        Assert.Greater(result.Variants[2].LauncherEffectiveRange, result.Variants[1].LauncherEffectiveRange);
        Assert.GreaterOrEqual(result.Variants[3].LauncherEffectiveRange, result.Variants[2].LauncherEffectiveRange);
        Assert.GreaterOrEqual(
            result.Variants[3].LauncherEffectiveTurnRateDegreesPerSecond,
            result.Variants[1].LauncherEffectiveTurnRateDegreesPerSecond);
        Assert.IsTrue(result.Comparisons[1].RadarImprovedDetectionTime);
        Assert.IsTrue(result.Comparisons[2].RadarImprovedLockTime);
        Assert.IsTrue(result.Comparisons[2].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Passed);
    }

    [Test]
    public void RunDefinition_UsesAd009ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad009DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad009DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd009Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(4, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd009Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(4, result.Variants.Length);
        Assert.IsTrue(result.Variants[3].RadarProviderUsed);
        Assert.IsTrue(result.Variants[3].SatelliteProviderUsed);
        Assert.IsTrue(result.Comparisons[2].RadarImprovedOrMatchedOutcome);
    }
}
