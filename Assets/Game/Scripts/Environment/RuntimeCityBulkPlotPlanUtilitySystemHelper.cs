using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using CityLayoutData = RuntimeCityLayoutUtilitySystemHelper.CityLayoutData;
    using Plan = RuntimeCityBulkPlotPlanUtilitySystemHelper.Plan;
    using PlotCandidate = RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate;

    internal sealed class RuntimeCityBulkPlotPlanUtilitySystemHelper
    {
        private readonly RuntimeCityBulkPlotPlanState _state = new();

        public RuntimeCityBulkPlotPlanState State => _state;

        public void ConfigureDistrictIntent(bool enabled)
        {
            _state.ConfigureDistrictIntent(enabled);
        }

        public readonly struct Plan
        {
            public readonly List<PlotCandidate> CentralPlots;
            public readonly List<PlotCandidate> OuterPlots;
            public readonly List<PlotCandidate> EntryPlots;
            public readonly List<PlotCandidate> MarketPlots;
            public readonly List<PlotCandidate> ResidentialPlots;
            public readonly List<PlotCandidate> UtilityPlots;

            public Plan(
                List<PlotCandidate> centralPlots,
                List<PlotCandidate> outerPlots,
                List<PlotCandidate> entryPlots,
                List<PlotCandidate> marketPlots,
                List<PlotCandidate> residentialPlots,
                List<PlotCandidate> utilityPlots)
            {
                CentralPlots = centralPlots;
                OuterPlots = outerPlots;
                EntryPlots = entryPlots;
                MarketPlots = marketPlots;
                ResidentialPlots = residentialPlots;
                UtilityPlots = utilityPlots;
            }
        }

        public Plan CreatePlan(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            CityLayoutData city,
            int townRadius,
            HashSet<Vector2Int> roadCells,
            Vector2Int centerRoadCell,
            ref Unity.Mathematics.Random rng)
        {
            return _state.CreatePlan(context, city, townRadius, roadCells, centerRoadCell, ref rng);
        }
    }

    internal sealed class RuntimeCityBulkPlotPlanState
    {
        private bool _districtIntentEnabled;

        public Vector2Int ResidentialScatterDirection =>
            _districtIntentEnabled ? new Vector2Int(1, -1) : Vector2Int.zero;

        public Vector2Int UtilityScatterDirection =>
            _districtIntentEnabled ? new Vector2Int(1, 1) : Vector2Int.zero;

        public int RuralScatterRadiusOffset => _districtIntentEnabled ? 1 : 3;

        public void ConfigureDistrictIntent(bool enabled)
        {
            _districtIntentEnabled = enabled;
        }

        public Plan CreatePlan(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            CityLayoutData city,
            int townRadius,
            HashSet<Vector2Int> roadCells,
            Vector2Int centerRoadCell,
            ref Unity.Mathematics.Random rng)
        {
            RuntimeCityConfigCompositionSystemHelper.Snapshot config = context.Config;
            List<PlotCandidate> centralPlots = context.BuildingPlotSystem.CollectRoadsidePlots(
                roadCells,
                centerRoadCell,
                townRadius,
                config.HallPlazaRadiusRoadCells + 1,
                config.HallPlazaRadiusRoadCells + 3);
            List<PlotCandidate> outerPlots = context.BuildingPlotSystem.CollectRoadsidePlots(
                roadCells,
                centerRoadCell,
                townRadius,
                config.HallPlazaRadiusRoadCells + 4,
                townRadius + 1);
            List<PlotCandidate> entryPlots = city.HasIncomingAnchor
                ? context.BuildingPlotSystem.CollectEntryRoadsidePlots(city, townRadius)
                : new List<PlotCandidate>();

            context.PrefabSelectionSystem.Shuffle(centralPlots, ref rng);
            context.PrefabSelectionSystem.Shuffle(outerPlots, ref rng);
            context.PrefabSelectionSystem.Shuffle(entryPlots, ref rng);

            if (!_districtIntentEnabled)
                return new Plan(centralPlots, outerPlots, entryPlots, centralPlots, outerPlots, outerPlots);

            int preferredMaximumDistance = townRadius;
            List<PlotCandidate> marketPlots = CreateDistrictOrderedPlots(
                centralPlots,
                outerPlots,
                centerRoadCell,
                Vector2Int.left,
                preferredMaximumDistance);
            List<PlotCandidate> residentialPlots = CreateDistrictOrderedPlots(
                null,
                outerPlots,
                centerRoadCell,
                Vector2Int.down,
                preferredMaximumDistance);
            List<PlotCandidate> utilityPlots = CreateDistrictOrderedPlots(
                null,
                outerPlots,
                centerRoadCell,
                Vector2Int.right,
                preferredMaximumDistance);
            LogResidentialCandidateSupply(config, residentialPlots, centerRoadCell);
            return new Plan(
                centralPlots,
                outerPlots,
                entryPlots,
                marketPlots,
                residentialPlots,
                utilityPlots);
        }

        private static void LogResidentialCandidateSupply(
            RuntimeCityConfigCompositionSystemHelper.Snapshot config,
            List<PlotCandidate> residentialPlots,
            Vector2Int centerRoadCell)
        {
            int southCount = 0;
            int neutralCount = 0;
            int northCount = 0;
            for (int i = 0; i < residentialPlots.Count; i++)
            {
                int offsetY = residentialPlots[i].PlotCell.y - centerRoadCell.y;
                if (offsetY < 0)
                    southCount++;
                else if (offsetY > 0)
                    northCount++;
                else
                    neutralCount++;
            }

            int ruralTarget = Mathf.RoundToInt(Mathf.Max(0, config.HouseCount) * Mathf.Clamp01(config.RuralHouseRatio));
            int roadsideTarget = Mathf.Max(0, config.HouseCount - ruralTarget);
            int reviewedCount = Mathf.Min(roadsideTarget, residentialPlots.Count);
            int preferredInTargetWindow = 0;
            for (int i = 0; i < reviewedCount; i++)
            {
                if (residentialPlots[i].PlotCell.y < centerRoadCell.y)
                    preferredInTargetWindow++;
            }

            Debug.Log(
                $"[RuntimeCityDistrictPlan] residentialCandidates={residentialPlots.Count} " +
                $"south={southCount} neutral={neutralCount} north={northCount} " +
                $"roadsideTarget={roadsideTarget} preferredInTargetWindow={preferredInTargetWindow} " +
                $"ruralTarget={ruralTarget} scatterDirection=1,-1");
        }

        internal static List<PlotCandidate> CreateDistrictOrderedPlots(
            List<PlotCandidate> primaryPlots,
            List<PlotCandidate> secondaryPlots,
            Vector2Int centerRoadCell,
            Vector2Int priorityDirection,
            int preferredMaximumDistance = int.MaxValue)
        {
            int primaryCount = primaryPlots?.Count ?? 0;
            int secondaryCount = secondaryPlots?.Count ?? 0;
            var result = new List<PlotCandidate>(primaryCount + secondaryCount);
            if (primaryPlots != null)
                result.AddRange(primaryPlots);
            if (secondaryPlots != null)
                result.AddRange(secondaryPlots);

            result.Sort((left, right) => CompareDistrictPriority(
                left,
                right,
                centerRoadCell,
                priorityDirection,
                preferredMaximumDistance));
            return result;
        }

        private static int CompareDistrictPriority(
            PlotCandidate left,
            PlotCandidate right,
            Vector2Int centerRoadCell,
            Vector2Int priorityDirection,
            int preferredMaximumDistance)
        {
            bool leftOutsidePreferredBand = left.DistanceFromCenter > preferredMaximumDistance;
            bool rightOutsidePreferredBand = right.DistanceFromCenter > preferredMaximumDistance;
            int comparison = leftOutsidePreferredBand.CompareTo(rightOutsidePreferredBand);
            if (comparison != 0)
                return comparison;

            Vector2Int leftOffset = left.PlotCell - centerRoadCell;
            Vector2Int rightOffset = right.PlotCell - centerRoadCell;
            int leftPriority = leftOffset.x * priorityDirection.x + leftOffset.y * priorityDirection.y;
            int rightPriority = rightOffset.x * priorityDirection.x + rightOffset.y * priorityDirection.y;
            comparison = rightPriority.CompareTo(leftPriority);
            if (comparison != 0)
                return comparison;

            comparison = left.DistanceFromCenter.CompareTo(right.DistanceFromCenter);
            if (comparison != 0)
                return comparison;
            comparison = left.PlotCell.x.CompareTo(right.PlotCell.x);
            return comparison != 0
                ? comparison
                : left.PlotCell.y.CompareTo(right.PlotCell.y);
        }
    }
}
