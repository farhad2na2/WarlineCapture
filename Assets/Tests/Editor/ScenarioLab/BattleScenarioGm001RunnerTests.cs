using NUnit.Framework;
using UnityEditor;

public sealed class BattleScenarioGm001RunnerTests
{
    [Test]
    public void RunDefault_GroundMissileLauncherFiresVisibleRocketAndDamagesTarget()
    {
        BattleScenarioResult result = BattleScenarioGm001Runner.RunDefault();

        Assert.AreEqual(BattleScenarioGm001Runner.ScenarioId, result.ScenarioId);
        Assert.AreEqual(1, result.Variants.Length);
        Assert.AreEqual(0, result.Comparisons.Length);
        Assert.IsTrue(result.Passed);

        BattleScenarioMetrics metrics = result.Variants[0];
        Assert.IsTrue(metrics.Detected);
        Assert.IsTrue(metrics.TrackingStarted);
        Assert.IsTrue(metrics.Locked);
        Assert.IsTrue(metrics.InterceptorLaunched);
        Assert.IsTrue(metrics.Intercepted);
        Assert.Greater(metrics.LauncherEffectiveTrackingQuality, 0f);
        Assert.Greater(metrics.InterceptDistanceFromDefendedTarget, 0f);
        Assert.AreEqual(BattleScenarioFailureReason.None, metrics.FailureReason);
    }

    [Test]
    public void RunDefinition_UsesGm001ScenarioAsset()
    {
        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Gm001DefinitionPath);

        Assert.IsNotNull(definition, $"Missing scenario asset at {BattleScenarioLabValidationRunner.Gm001DefinitionPath}");
        Assert.AreEqual(BattleScenarioGm001Runner.ScenarioId, definition.ScenarioId);
        Assert.AreEqual(1, definition.ScenarioVariants.Length);

        BattleScenarioResult result = BattleScenarioGm001Runner.RunDefinition(definition);

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(1, result.Variants.Length);
        Assert.IsTrue(result.Variants[0].Intercepted);
    }
}
