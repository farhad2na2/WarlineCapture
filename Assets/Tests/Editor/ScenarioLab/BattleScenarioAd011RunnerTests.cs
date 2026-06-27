using NUnit.Framework;
using UnityEditor;

public sealed class BattleScenarioAd011RunnerTests
{
    [Test]
    public void RunDefault_TracksLaunchesAndHitsEveryAirTargetClass()
    {
        BattleScenarioResult result = BattleScenarioAd011Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd011Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(4, result.Variants.Length);
        Assert.IsTrue(result.Passed);

        AssertVariantPassed(result.Variants[0], "AD-011-A-RadarNear-JetPatrol");
        AssertVariantPassed(result.Variants[1], "AD-011-B-RadarNear-Helicopter");
        AssertVariantPassed(result.Variants[2], "AD-011-C-RadarNear-Drone");
        AssertVariantPassed(result.Variants[3], "AD-011-D-RadarNear-AttackingJet");
    }

    [Test]
    public void RunDefinition_UsesAd011ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad011DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad011DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd011Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(4, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd011Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(4, result.Variants.Length);
        for (int i = 0; i < result.Variants.Length; i++)
            AssertVariantPassed(result.Variants[i], definition.ScenarioVariants[i].VariantId);
    }

    private static void AssertVariantPassed(BattleScenarioMetrics metrics, string variantId)
    {
        Assert.AreEqual(variantId, metrics.VariantId);
        Assert.IsTrue(metrics.RadarProviderUsed, $"{variantId} should use nearby radar support.");
        Assert.IsTrue(metrics.Detected, $"{variantId} was not detected.");
        Assert.IsTrue(metrics.TrackingStarted, $"{variantId} was not tracked.");
        Assert.IsTrue(metrics.Locked, $"{variantId} was not locked.");
        Assert.IsTrue(metrics.InterceptorLaunched, $"{variantId} did not launch an interceptor.");
        Assert.IsTrue(metrics.Intercepted, $"{variantId} was not hit/killed.");
        Assert.LessOrEqual(metrics.ClosestInterceptorDistanceToThreat, 4.5f, $"{variantId} closest approach was too loose.");
        Assert.AreEqual(BattleScenarioFailureReason.None, metrics.FailureReason);
    }
}
