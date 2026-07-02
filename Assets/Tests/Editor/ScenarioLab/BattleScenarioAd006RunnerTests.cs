using NUnit.Framework;
using UnityEditor;
using Game.Editor;
using Game.Runtime;

public sealed class BattleScenarioAd006RunnerTests
{
    [Test]
    public void RunDefault_RecordsRadarDisabledMidScenarioBehavior()
    {
        BattleScenarioResult result = BattleScenarioAd006Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd006Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(3, result.Variants.Length);
        Assert.AreEqual(2, result.Comparisons.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsTrue(result.Variants[2].RadarProviderUsed);
        Assert.Greater(result.Variants[1].LauncherEffectiveRange, result.Variants[0].LauncherEffectiveRange);
        Assert.AreEqual(
            result.Variants[0].LauncherEffectiveRange,
            result.Variants[2].LauncherEffectiveRange,
            0.01f);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsTrue(result.Variants[2].Intercepted);
        Assert.IsFalse(result.Variants[2].IncomingThreatImpacted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedLockTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Comparisons[1].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Passed);
    }

    [Test]
    public void RunDefinition_UsesAd006ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad006DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad006DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd006Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(3, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd006Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(3, result.Variants.Length);
        Assert.IsTrue(result.Variants[2].RadarProviderUsed);
        Assert.IsTrue(result.Variants[2].Intercepted);
        Assert.IsTrue(result.Comparisons[1].RadarImprovedOrMatchedOutcome);
    }
}
