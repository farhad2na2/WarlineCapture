using System;
using UnityEngine;
using Game.Components;
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
        public string ResourceExchangeSourceMode;
        public string ResourceExchangeRouteSummary;
        public int ResourceExchangeStartedCount;
        public int ResourceExchangeCompletedCount;
        public int ResourceExchangeCancelledCount;
        public int ResourceExchangeBlockedCount;
        public int ResourceExchangeRushCount;
        public int ResourceExchangeInputAmount;
        public int ResourceExchangeOutputAmount;
        public float ResourceExchangeDurationSeconds;
        public float ResourceExchangeCompletionRatePercent;
        public int ResourceExchangeCreditsDelta;
        public int ResourceExchangeMaterialsDelta;
        public int ResourceExchangeOilDelta;
        public int ResourceExchangeFuelDelta;
        public int ResourceExchangeRushTicketsDelta;
        public int ResourceExchangeNetResourceDelta;
        public int MaterialsCurrent;
        public int MaterialsCapacity;
        public int MaterialsFabricated;
        public int MaterialsImported;
        public int MaterialsRewarded;
        public int MaterialsExported;
        public int MaterialsGrossSpent;
        public int MaterialsConstructionSpent;
        public int MaterialsRepairSpent;
        public int MaterialsInfrastructureSpent;
        public int MaterialsUpgradeSpent;
        public float FabricationActiveSeconds;
        public float FabricationBlockedSeconds;
        public float FabricationNoOilInputBlockedSeconds;
        public float FabricationMaterialsCapacityFullBlockedSeconds;
        public float FabricationNoOilRouteBlockedSeconds;
        public float FabricationProductionDisabledSeconds;
        public float FabricationBuildingDisabledSeconds;
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

        public void ApplyResourceExchangeTelemetry(
            string sourceMode,
            ResourceExchangeQueueComponent[] queueItems,
            ResourceExchangeEconomyEventComponent[] economyEvents)
        {
            ResourceExchangeSourceMode = string.IsNullOrWhiteSpace(sourceMode)
                ? "Unspecified"
                : sourceMode;

            queueItems ??= Array.Empty<ResourceExchangeQueueComponent>();
            economyEvents ??= Array.Empty<ResourceExchangeEconomyEventComponent>();

            int exportCount = 0;
            int importCount = 0;
            ResourceExchangeInputAmount = 0;
            ResourceExchangeOutputAmount = 0;
            ResourceExchangeDurationSeconds = 0f;

            for (int i = 0; i < queueItems.Length; i++)
            {
                ResourceExchangeQueueComponent item = queueItems[i];
                if (item.RouteType == ResourceExchangeRouteType.Import)
                    importCount++;
                else
                    exportCount++;

                ResourceExchangeInputAmount += Mathf.Max(0, item.InputAmount);
                ResourceExchangeOutputAmount += Mathf.Max(0, item.OutputAmount);
                ResourceExchangeDurationSeconds += Mathf.Max(0f, item.DurationSeconds);
            }

            ResourceExchangeRouteSummary = BuildRouteSummary(exportCount, importCount);
            ResourceExchangeStartedCount = 0;
            ResourceExchangeCompletedCount = 0;
            ResourceExchangeCancelledCount = 0;
            ResourceExchangeBlockedCount = 0;
            ResourceExchangeRushCount = 0;
            ResourceExchangeCreditsDelta = 0;
            ResourceExchangeMaterialsDelta = 0;
            ResourceExchangeOilDelta = 0;
            ResourceExchangeFuelDelta = 0;
            ResourceExchangeRushTicketsDelta = 0;
            ResourceExchangeNetResourceDelta = 0;

            for (int i = 0; i < economyEvents.Length; i++)
            {
                ResourceExchangeEconomyEventComponent economyEvent = economyEvents[i];
                switch (economyEvent.ResultKind)
                {
                    case ResourceExchangeResultKind.QueueStarted:
                        ResourceExchangeStartedCount++;
                        break;
                    case ResourceExchangeResultKind.QueueCompleted:
                        ResourceExchangeCompletedCount++;
                        break;
                    case ResourceExchangeResultKind.QueueCancelled:
                        ResourceExchangeCancelledCount++;
                        break;
                    case ResourceExchangeResultKind.QueueBlocked:
                        ResourceExchangeBlockedCount++;
                        break;
                    case ResourceExchangeResultKind.RushAccepted:
                        ResourceExchangeRushCount++;
                        break;
                }

                AddResourceExchangeDelta(economyEvent.ResourceKind, economyEvent.Amount);
                ResourceExchangeNetResourceDelta += economyEvent.Amount;
            }

            ResourceExchangeCompletionRatePercent = ResourceExchangeStartedCount > 0
                ? Mathf.Clamp01((float)ResourceExchangeCompletedCount / ResourceExchangeStartedCount) * 100f
                : 0f;
        }

        public void ApplyFieldFabricationTelemetry(
            in FactionTacticalMaterialsComponent materials,
            in FactionMaterialFabricationTelemetryComponent fabrication)
        {
            MaterialsCurrent = Mathf.Max(0, materials.Current);
            MaterialsCapacity = Mathf.Max(0, materials.Capacity);
            MaterialsFabricated = Mathf.Max(0, materials.LifetimeFabricated);
            MaterialsImported = Mathf.Max(0, materials.LifetimeImported);
            MaterialsRewarded = Mathf.Max(0, materials.LifetimeRewarded);
            MaterialsExported = Mathf.Max(0, materials.LifetimeExported);
            MaterialsGrossSpent = Mathf.Max(0, materials.LifetimeSpent);
            MaterialsConstructionSpent = Mathf.Max(0, materials.LifetimeConstructionSpent);
            MaterialsRepairSpent = Mathf.Max(0, materials.LifetimeRepairSpent);
            MaterialsInfrastructureSpent = Mathf.Max(0, materials.LifetimeInfrastructureSpent);
            MaterialsUpgradeSpent = Mathf.Max(0, materials.LifetimeUpgradeSpent);

            FabricationActiveSeconds = ClampDuration(fabrication.ActiveSeconds);
            FabricationNoOilInputBlockedSeconds = ClampDuration(fabrication.NoOilInputBlockedSeconds);
            FabricationMaterialsCapacityFullBlockedSeconds =
                ClampDuration(fabrication.MaterialsCapacityFullBlockedSeconds);
            FabricationNoOilRouteBlockedSeconds = ClampDuration(fabrication.NoOilRouteBlockedSeconds);
            FabricationProductionDisabledSeconds = ClampDuration(fabrication.ProductionDisabledSeconds);
            FabricationBuildingDisabledSeconds = ClampDuration(fabrication.BuildingDisabledSeconds);
            FabricationBlockedSeconds =
                FabricationNoOilInputBlockedSeconds +
                FabricationMaterialsCapacityFullBlockedSeconds +
                FabricationNoOilRouteBlockedSeconds +
                FabricationProductionDisabledSeconds +
                FabricationBuildingDisabledSeconds;
        }

        private static float ClampDuration(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }

        private static string BuildRouteSummary(int exportCount, int importCount)
        {
            if (exportCount <= 0 && importCount <= 0)
                return "None";

            if (exportCount > 0 && importCount > 0)
                return $"Export:{exportCount} Import:{importCount}";

            return exportCount > 0
                ? $"Export:{exportCount}"
                : $"Import:{importCount}";
        }

        private void AddResourceExchangeDelta(ResourceExchangeResourceKind resourceKind, int amount)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    ResourceExchangeCreditsDelta += amount;
                    return;
                case ResourceExchangeResourceKind.Materials:
                    ResourceExchangeMaterialsDelta += amount;
                    return;
                case ResourceExchangeResourceKind.Oil:
                    ResourceExchangeOilDelta += amount;
                    return;
                case ResourceExchangeResourceKind.Fuel:
                    ResourceExchangeFuelDelta += amount;
                    return;
                case ResourceExchangeResourceKind.RushTickets:
                    ResourceExchangeRushTicketsDelta += amount;
                    return;
            }
        }
    }
}
