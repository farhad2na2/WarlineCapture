using NUnit.Framework;
using UnityEditor;
using Game.Editor;
using Game.Runtime;

public sealed class BattleScenarioDr001RunnerTests
{
    [Test]
    public void RunDefault_DroneReconTriggersAirThreatWarningAfterEnteringDetectorRadius()
    {
        BattleScenarioResult result = BattleScenarioDr001Runner.RunDefault();

        Assert.AreEqual(BattleScenarioDr001Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(1, result.Variants.Length);
        Assert.AreEqual(0, result.Comparisons.Length);
        Assert.IsTrue(result.Passed);

        BattleScenarioMetrics metrics = result.Variants[0];
        Assert.IsTrue(metrics.TrackingStarted, "Out-of-range quiet tick should be observed first.");
        Assert.IsTrue(metrics.Detected);
        Assert.IsTrue(metrics.Locked, "Warning type should be Air.");
        Assert.IsTrue(metrics.InterceptorLaunched, "One threat should be counted.");
        Assert.IsTrue(metrics.Intercepted);
        Assert.AreEqual(BattleScenarioFailureReason.None, metrics.FailureReason);
        Assert.Greater(metrics.InterceptDistanceFromDefendedTarget, 0f, "Warning ETA should be captured.");
        Assert.LessOrEqual(metrics.IncomingThreatDistanceAtDetection, metrics.LauncherEffectiveRange);
    }

    [Test]
    public void RunDefinition_UsesDr001ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Dr001DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Dr001DefinitionPath}");
        Assert.AreEqual(BattleScenarioDr001Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(1, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioDr001Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(1, result.Variants.Length);
        Assert.IsTrue(result.Variants[0].Detected);
        Assert.IsTrue(result.Variants[0].Locked);
    }
}
