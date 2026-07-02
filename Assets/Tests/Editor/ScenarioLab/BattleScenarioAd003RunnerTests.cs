using NUnit.Framework;
using UnityEditor;
using Game.Editor;
using Game.Runtime;

public sealed class BattleScenarioAd003RunnerTests
{
    [Test]
    public void RunDefault_TracksAndInterceptsDroneScoutWithRadarImprovement()
    {
        BattleScenarioResult result = BattleScenarioAd003Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd003Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.AreEqual(1, result.Comparisons.Length);
        Assert.IsTrue(result.Passed);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.Greater(result.Variants[1].LauncherEffectiveRange, result.Variants[0].LauncherEffectiveRange);
        Assert.Less(result.Variants[1].LauncherEffectiveLockSeconds, result.Variants[0].LauncherEffectiveLockSeconds);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedDetectionTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedLockTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
    }

    [Test]
    public void RunDefinition_UsesAd003ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad003DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad003DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd003Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(2, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd003Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedDetectionTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
    }
}
