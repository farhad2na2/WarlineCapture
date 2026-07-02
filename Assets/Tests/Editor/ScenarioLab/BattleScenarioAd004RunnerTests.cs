using NUnit.Framework;
using UnityEditor;
using Game.Editor;
using Game.Runtime;

public sealed class BattleScenarioAd004RunnerTests
{
    [Test]
    public void RunDefault_InterceptsTwoIncomingGroundMissilesWithRadarSupport()
    {
        BattleScenarioResult result = BattleScenarioAd004Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd004Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.AreEqual(1, result.Comparisons.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.Greater(result.Variants[1].LauncherEffectiveRange, result.Variants[0].LauncherEffectiveRange);
        Assert.Less(result.Variants[1].LauncherEffectiveLockSeconds, result.Variants[0].LauncherEffectiveLockSeconds);
        Assert.IsTrue(result.Variants[1].InterceptorLaunched);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsFalse(result.Variants[1].IncomingThreatImpacted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedLockTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Passed);
    }

    [Test]
    public void RunDefinition_UsesAd004ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad004DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad004DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd004Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(2, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd004Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
    }
}
