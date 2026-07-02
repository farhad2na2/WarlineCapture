using System;
using UnityEngine;
using Game.Configs;

namespace Game.Runtime
{
    [Serializable]
    public readonly struct BalanceProbeDefinition
    {
        public readonly string ProbeId;
        public readonly string ScenarioId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly QuickGameConfig QuickGameConfig;
        public readonly float SampledDurationSeconds;
        public readonly BalanceMetricSample Sample;

        public BalanceProbeDefinition(
            string probeId,
            string scenarioId,
            string displayName,
            string description,
            QuickGameConfig quickGameConfig,
            float sampledDurationSeconds,
            BalanceMetricSample sample)
        {
            ProbeId = string.IsNullOrWhiteSpace(probeId) ? "BalanceProbe" : probeId;
            ScenarioId = string.IsNullOrWhiteSpace(scenarioId) ? ProbeId : scenarioId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? ProbeId : displayName;
            Description = description ?? string.Empty;
            QuickGameConfig = quickGameConfig;
            SampledDurationSeconds = Mathf.Max(0f, sampledDurationSeconds);
            Sample = sample;
        }
    }
}
