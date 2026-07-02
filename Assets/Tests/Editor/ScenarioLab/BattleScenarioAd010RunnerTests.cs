using NUnit.Framework;
using UnityEditor;
using Game.Editor;
using Game.Runtime;

public sealed class BattleScenarioAd010RunnerTests
{
    [Test]
    public void RunDefault_InterceptionGeometrySweepInterceptsAllGeometryVariants()
    {
        BattleScenarioResult result = BattleScenarioAd010Runner.RunDefault();

        Assert.AreEqual(BattleScenarioAd010Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(4, result.Variants.Length);
        Assert.AreEqual(0, result.Comparisons.Length);
        Assert.IsTrue(result.Passed);

        for (int i = 0; i < result.Variants.Length; i++)
        {
            BattleScenarioMetrics metrics = result.Variants[i];
            Assert.IsTrue(metrics.RadarProviderUsed, metrics.VariantId);
            Assert.IsFalse(metrics.SatelliteProviderUsed, metrics.VariantId);
            Assert.IsTrue(metrics.Detected, metrics.VariantId);
            Assert.IsTrue(metrics.TrackingStarted, metrics.VariantId);
            Assert.IsTrue(metrics.Locked, metrics.VariantId);
            Assert.IsTrue(metrics.InterceptorLaunched, metrics.VariantId);
            Assert.IsTrue(metrics.Intercepted, metrics.VariantId);
            Assert.IsFalse(metrics.IncomingThreatImpacted, metrics.VariantId);
            Assert.AreEqual(BattleScenarioFailureReason.None, metrics.FailureReason, metrics.VariantId);
            Assert.Greater(metrics.ClosestInterceptorDistanceToThreat, -1f, metrics.VariantId);
        }
    }

    [Test]
    public void RunDefinition_UsesAd010ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad010DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Ad010DefinitionPath}");
        Assert.AreEqual(BattleScenarioAd010Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(4, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioAd010Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(4, result.Variants.Length);
        for (int i = 0; i < result.Variants.Length; i++)
            Assert.IsTrue(result.Variants[i].Intercepted, result.Variants[i].VariantId);
    }
}
