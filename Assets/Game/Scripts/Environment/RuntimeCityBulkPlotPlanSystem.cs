using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;
using Plan = RuntimeCityBulkPlotPlanSystem.Plan;
using PlotCandidate = RuntimeCityBuildingPlotSystem.PlotCandidate;

internal sealed class RuntimeCityBulkPlotPlanSystem
{
    private readonly RuntimeCityBulkPlotPlanState _state = new();

    public RuntimeCityBulkPlotPlanState State => _state;

    public readonly struct Plan
    {
        public readonly List<PlotCandidate> CentralPlots;
        public readonly List<PlotCandidate> OuterPlots;
        public readonly List<PlotCandidate> EntryPlots;

        public Plan(
            List<PlotCandidate> centralPlots,
            List<PlotCandidate> outerPlots,
            List<PlotCandidate> entryPlots)
        {
            CentralPlots = centralPlots;
            OuterPlots = outerPlots;
            EntryPlots = entryPlots;
        }
    }

    public Plan CreatePlan(
        RuntimeCityBuildingSpawnContextSystem.Context context,
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
    public Plan CreatePlan(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        CityLayoutData city,
        int townRadius,
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell,
        ref Unity.Mathematics.Random rng)
    {
        RuntimeCityConfigSystem.Snapshot config = context.Config;
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

        return new Plan(centralPlots, outerPlots, entryPlots);
    }
}
