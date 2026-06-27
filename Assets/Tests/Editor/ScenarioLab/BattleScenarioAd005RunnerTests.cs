using NUnit.Framework;
using UnityEditor;

public sealed class BattleScenarioAd005RunnerTests
{
    [Test]
    public void RunDefault_InterceptsTwoGroundMissilesWithTwoLaunchersAndRadarSupport()
    {
        BattleScenarioResult result = BattleScenarioAd005Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd005Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.AreEqual(1, result.Comparisons.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.Greater(result.Variants[1].LauncherEffectiveRange, result.Variants[0].LauncherEffectiveRange);
        Assert.Less(result.Variants[1].LauncherEffectiveLockSeconds, result.Variants[0].LauncherEffectiveLockSeconds);
        Assert.Greater(result.Variants[1].LauncherEffectiveTrackingQuality, result.Variants[0].LauncherEffectiveTrackingQuality);
        Assert.IsTrue(result.Variants[1].InterceptorLaunched);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsFalse(result.Variants[1].IncomingThreatImpacted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedLockTime);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
        Assert.IsTrue(result.Passed);
    }

    [Test]
    public void RunDefinition_UsesAd005ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad005DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad005DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd005Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(2, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd005Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(2, result.Variants.Length);
        Assert.IsTrue(result.Variants[1].RadarProviderUsed);
        Assert.IsTrue(result.Variants[1].Intercepted);
        Assert.IsTrue(result.Comparisons[0].RadarImprovedOrMatchedOutcome);
    }
}
