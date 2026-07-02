using System;

namespace Game.Runtime
{
    public static class BattleScenarioLabRuntimeRunner
    {
        public static BattleScenarioResult RunDefinition(BattleScenarioDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            string scenarioId = definition.ScenarioId;
            if (string.Equals(scenarioId, BattleScenarioAd001Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd001Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd002Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd002Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd003Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd003Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd004Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd004Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd005Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd005Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd006Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd006Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd007Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd007Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd008Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd008Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd009Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd009Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd010Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd010Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioAd011Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioAd011Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioGm001Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioGm001Runner.RunDefinition(definition);
            if (string.Equals(scenarioId, BattleScenarioDr001Runner.ScenarioId, StringComparison.Ordinal))
                return BattleScenarioDr001Runner.RunDefinition(definition);
            if (TransportBoardingScenarioRuntimeRunner.CanRunDefinition(definition))
                return TransportBoardingScenarioRuntimeRunner.RunDefinition(definition);

            throw new NotSupportedException($"No Scenario Lab runner is registered for '{scenarioId}'.");
        }

        public static bool SupportsSingleVariantPlayback(BattleScenarioDefinition definition)
        {
            return definition != null &&
                   string.Equals(definition.ScenarioId, BattleScenarioAd001Runner.ScenarioId, StringComparison.Ordinal);
        }
    }
}
