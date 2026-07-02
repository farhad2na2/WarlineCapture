using System;

namespace Game.Runtime
{
    public static class TransportBoardingScenarioRuntimeRunner
    {
        public static bool CanRunDefinition(BattleScenarioDefinition definition)
        {
            return definition != null &&
                   TransportBoardingScenarioCatalog.IsTransportBoardingScenarioId(definition.ScenarioId);
        }

        public static BattleScenarioResult RunDefinition(BattleScenarioDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (!TransportBoardingScenarioCatalog.TryGetScenario(definition.ScenarioId, out TransportBoardingScenarioDescriptor descriptor))
                throw new ArgumentException($"Unsupported transport boarding scenario id '{definition.ScenarioId}'.", nameof(definition));

            BattleScenarioVariant[] variants = definition.ScenarioVariants;
            if (variants.Length == 0)
                variants = new[] { CreateDefaultVariant(descriptor) };

            var metrics = new BattleScenarioMetrics[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                metrics[i] = new BattleScenarioMetrics
                {
                    ScenarioId = definition.ScenarioId,
                    VariantId = string.IsNullOrWhiteSpace(variants[i].VariantId) ? descriptor.ScenarioId : variants[i].VariantId,
                    Seed = definition.RandomSeed,
                    DurationSeconds = 0f,
                    Frames = 0,
                    FailureReason = BattleScenarioFailureReason.InvalidSetup
                };
            }

            // TB execution is owned by the focused ECS tests and upcoming visual playback.
            // This dispatch result keeps TB IDs registered without faking a completed gameplay run.
            return new BattleScenarioResult
            {
                ScenarioId = definition.ScenarioId,
                GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                FixedDeltaTime = definition.FixedDeltaTime,
                Variants = metrics,
                Comparisons = Array.Empty<BattleScenarioComparison>(),
                Passed = false,
                FailureReason = BattleScenarioFailureReason.InvalidSetup
            };
        }

        private static BattleScenarioVariant CreateDefaultVariant(TransportBoardingScenarioDescriptor descriptor)
        {
            return new BattleScenarioVariant
            {
                VariantId = descriptor.ScenarioId,
                Label = descriptor.DisplayName,
                SupportMode = BattleScenarioSupportMode.None,
                IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
                IncomingThreatSpeedMultiplier = 1f,
                IncomingThreatStartDistance = 0f,
                IncomingThreatAltitude = 0f,
                LauncherCount = 0,
                RadarDistanceFromLauncher = 0f,
                ExpectedOutcome = BattleScenarioExpectedOutcome.Baseline
            };
        }
    }
}
