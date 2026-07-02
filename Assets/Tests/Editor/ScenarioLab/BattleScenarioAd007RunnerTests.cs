using NUnit.Framework;
using UnityEditor;
using Game.Editor;
using Game.Runtime;

public sealed class BattleScenarioAd007RunnerTests
{
    [Test]
    public void RunDefault_DetectsEarlierWhenThreatStartsInsideRadarExtendedRange()
    {
        BattleScenarioResult result = BattleScenarioAd007Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd007Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.AreEqual(1, result.Comparisons.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.Greater(result.Variants[1].LauncherEffectiveRange, result.Variants[0].LauncherEffectiveRange);
        Assert.Greater(result.Variants[1].IncomingThreatDistanceAtDetection, result.Variants[0].LauncherEffectiveRange);
        Assert.Less(result.Variants[1].DetectionTimeSeconds, result.Variants[0].DetectionTimeSeconds);
        Assert.Less(result.Variants[1].LauncherEffectiveLockSeconds, result.Variants[0].LauncherEffectiveLockSeconds);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedDetectionTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedLockTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Passed);
    }

    [Test]
    public void RunDefinition_UsesAd007ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad007DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad007DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd007Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(2, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd007Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedDetectionTime);
    }
}
