using Game.Configs;
using Game.Runtime;
public static class QuickCustomBalanceProbe
{
    public const string ProbeId = "QuickCustom_Default_Medium";
    public const string ScenarioId = "quick_custom_default_medium";
    public const string HardSwarmProbeId = "QuickCustom_Hard_Swarm";
    public const string HardSwarmScenarioId = "quick_custom_hard_swarm";

    public static BalanceReportWriter.ReportPaths RunDefaultMediumReport()
    {
        return RunReport(CreateDefaultMediumDefinition());
    }

    public static BalanceReportWriter.ReportPaths RunHardSwarmReport()
    {
        return RunReport(CreateHardSwarmDefinition());
    }

    public static BalanceReportWriter.ReportPaths RunReport(BalanceProbeDefinition definition)
    {
        AISettingsRuntimeState.ResetDefaults();

        QuickGameConfig config = definition.QuickGameConfig;
        AISettingsRuntimeState.ApplySnapshot(config.ToAISettingsSnapshot());

        BalanceMetrics metrics = BalanceMetrics.FromProbeDefinition(definition);
        return BalanceReportWriter.WriteProjectReport(metrics);
    }

    public static BalanceProbeDefinition CreateDefaultMediumDefinition()
    {
        QuickGameConfig config = QuickGameConfig.Defaults;
        config.MapSeed = 104729;

        return new BalanceProbeDefinition(
            ProbeId,
            ScenarioId,
            "Quick Custom Default Medium",
            "Baseline Quick Custom configuration using lightweight sampled runtime counters.",
            config,
            10f * 60f,
            new BalanceMetricSample(
                oilExtracted: 320,
                fuelProduced: 145,
                vehiclesOrdered: 0,
                soldiersOrdered: 0,
                ammoOrdered: 0,
                buildingsBuilt: 1,
                ownSoldiersDead: 1,
                enemySoldiersDead: 2));
    }

    public static BalanceProbeDefinition CreateHardSwarmDefinition()
    {
        QuickGameConfig config = QuickGameConfig.Defaults;
        config.EnemyType = QuickGameEnemyType.Swarm;
        config.EnemyCount = 3;
        config.Difficulty = AIDifficultySetting.Hard;
        config.StartingMoney = AIStartingMoneySetting.High;
        config.IncomeMultiplier = 1.35f;
        config.BuildSpeed = AISpeedSetting.Fast;
        config.UnitProductionSpeed = AISpeedSetting.Fast;
        config.AttackGroupSize = AIAttackGroupSizeSetting.Large;
        config.AttackFrequency = AIAttackFrequencySetting.Frequent;
        config.Aggression = AIAggressionSetting.Aggressive;
        config.Expansion = AIExpansionSetting.Fast;
        config.TargetPriority = AITargetPriority.Units;
        config.FogOfWar = true;
        config.MapSeed = 130363;

        return new BalanceProbeDefinition(
            HardSwarmProbeId,
            HardSwarmScenarioId,
            "Quick Custom Hard Swarm",
            "High-pressure Quick Custom tuning probe for frequent attacks and aggressive swarm behavior.",
            config,
            7f * 60f,
            new BalanceMetricSample(
                oilExtracted: 260,
                fuelProduced: 90,
                vehiclesOrdered: 2,
                soldiersOrdered: 9,
                ammoOrdered: 3,
                buildingsBuilt: 3,
                ownSoldiersDead: 7,
                enemySoldiersDead: 14));
    }
}
