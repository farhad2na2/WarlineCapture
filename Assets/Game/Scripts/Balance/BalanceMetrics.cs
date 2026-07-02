using System;
using UnityEngine;
using Game.Configs;

namespace Game.Runtime
{
    [Serializable]
    public sealed class BalanceMetrics
    {
        public string ProbeId;
        public string ScenarioId;
        public string ProbeDisplayName;
        public string ProbeDescription;
        public int Seed;
        public string EnemyType;
        public int EnemyCount;
        public string Difficulty;
        public string StartingCredits;
        public float IncomeMultiplier;
        public string BuildSpeed;
        public string UnitProductionSpeed;
        public string AttackGroupSize;
        public string AttackFrequency;
        public string Aggression;
        public string Expansion;
        public string TargetPriority;
        public bool PlayerAutoAIEnabled;
        public string WinCondition;
        public bool FogOfWar;
        public bool IntelReveal;
        public string StartingResources;
        public float SampledDurationSeconds;
        public string Winner;
        public string ResultReason;
        public int OilExtracted;
        public int FuelProduced;
        public int VehiclesOrdered;
        public int SoldiersOrdered;
        public int AmmoOrdered;
        public int BuildingsBuilt;
        public int OwnSoldiersDead;
        public int EnemySoldiersDead;
        public string MatchDurationClassification;
        public string EconomyActivityClassification;
        public string CasualtyClassification;
        public string OverallClassification;

        public static BalanceMetrics FromProbeDefinition(BalanceProbeDefinition definition)
        {
            return FromQuickGameConfig(
                definition.ProbeId,
                definition.ScenarioId,
                definition.DisplayName,
                definition.Description,
                definition.QuickGameConfig,
                definition.SampledDurationSeconds,
                definition.Sample.ToSnapshot(Mathf.RoundToInt(definition.SampledDurationSeconds)));
        }

        public static BalanceMetrics FromQuickGameConfig(
            string probeId,
            string scenarioId,
            QuickGameConfig config,
            float sampledDurationSeconds,
            GameRuntimeStats.Snapshot snapshot)
        {
            return FromQuickGameConfig(
                probeId,
                scenarioId,
                probeId,
                string.Empty,
                config,
                sampledDurationSeconds,
                snapshot);
        }

        public static BalanceMetrics FromQuickGameConfig(
            string probeId,
            string scenarioId,
            string probeDisplayName,
            string probeDescription,
            QuickGameConfig config,
            float sampledDurationSeconds,
            GameRuntimeStats.Snapshot snapshot)
        {
            var metrics = new BalanceMetrics
            {
                ProbeId = probeId,
                ScenarioId = scenarioId,
                ProbeDisplayName = string.IsNullOrWhiteSpace(probeDisplayName) ? probeId : probeDisplayName,
                ProbeDescription = probeDescription ?? string.Empty,
                Seed = config.MapSeed,
                EnemyType = config.EnemyType.ToString(),
                EnemyCount = config.EnemyCount,
                Difficulty = config.Difficulty.ToString(),
                StartingCredits = config.StartingMoney.ToString(),
                IncomeMultiplier = config.IncomeMultiplier,
                BuildSpeed = config.BuildSpeed.ToString(),
                UnitProductionSpeed = config.UnitProductionSpeed.ToString(),
                AttackGroupSize = config.AttackGroupSize.ToString(),
                AttackFrequency = config.AttackFrequency.ToString(),
                Aggression = config.Aggression.ToString(),
                Expansion = config.Expansion.ToString(),
                TargetPriority = config.TargetPriority.ToString(),
                PlayerAutoAIEnabled = config.PlayerAutoAIEnabled,
                WinCondition = config.WinCondition.ToString(),
                FogOfWar = config.FogOfWar,
                IntelReveal = config.IntelReveal,
                StartingResources = config.StartingResources.ToString(),
                SampledDurationSeconds = Mathf.Max(0f, sampledDurationSeconds),
                Winner = "Unresolved",
                ResultReason = "Balance probe sampled configuration/runtime stats only; full match simulation is a later probe.",
                OilExtracted = snapshot.OilExtracted,
                FuelProduced = snapshot.FuelProduced,
                VehiclesOrdered = snapshot.VehiclesOrdered,
                SoldiersOrdered = snapshot.SoldiersOrdered,
                AmmoOrdered = snapshot.AmmoOrdered,
                BuildingsBuilt = snapshot.BuildingsBuilt,
                OwnSoldiersDead = snapshot.OwnSoldiersDead,
                EnemySoldiersDead = snapshot.EnemySoldiersDead
            };

            BalanceOutcomeClassifier.Classify(metrics);
            return metrics;
        }
    }
}
