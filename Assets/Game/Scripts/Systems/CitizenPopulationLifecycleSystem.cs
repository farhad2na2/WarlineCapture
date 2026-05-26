using System;
using UnityEngine;

internal sealed class CitizenPopulationLifecycleSystem
{
    private const float LogicalCitizenUpdateIntervalSeconds = 0.2f;
    private const float VisibleCitizenSyncIntervalSeconds = 0.12f;
    private const float TotalsRefreshIntervalSeconds = 0.25f;

    private float _nextLogicalCitizenUpdateAt;
    private float _nextVisibleCitizenSyncAt;
    private float _nextTotalsRefreshAt;

    public void Reset()
    {
        _nextLogicalCitizenUpdateAt = 0f;
        _nextVisibleCitizenSyncAt = 0f;
        _nextTotalsRefreshAt = 0f;
    }

    public void Update(
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenDangerSystem dangerSystem,
        CitizenPopulationDiagnosticSystem diagnosticSystem,
        CitizenPopulationStateSystem state,
        Action updateLogicalCitizens,
        Action syncVisibleCitizens,
        Action<bool> recalculateTotals,
        float now)
    {
        CitizenPopulationDiagnosticSystem.FrameTimings timings = diagnosticSystem.BeginFrame();
        try
        {
            if (!buildingReadSystem.HasRuntimeBuildingQuery())
                return;

            bool refreshedBuildings = buildingReadSystem.RefreshRuntimeBuildingListsIfDue(now);
            diagnosticSystem.MarkBuildings(ref timings);

            if (UnitPathfindingSystem.HasPendingPathJob)
            {
                diagnosticSystem.MarkSkippedForPathfinding(ref timings);
                RecalculateTotalsIfDue(syncSummaryEntity: false, now, recalculateTotals);
                diagnosticSystem.MarkTotals(ref timings);
                return;
            }

            ecsProjection.ResolveEntityManager();
            ecsProjection.EnsurePopulationSummaryEntity();
            diagnosticSystem.MarkResolve(ref timings);

            dangerSystem.RefreshDangerSourcesIfNeeded(now);
            diagnosticSystem.MarkDanger(ref timings);

            if (now >= _nextLogicalCitizenUpdateAt)
            {
                _nextLogicalCitizenUpdateAt = now + LogicalCitizenUpdateIntervalSeconds;
                if (!refreshedBuildings)
                    buildingReadSystem.RefreshRuntimeBuildingLists(now, force: true);
                updateLogicalCitizens();
            }

            diagnosticSystem.MarkLogical(ref timings);

            if (now >= _nextVisibleCitizenSyncAt)
            {
                _nextVisibleCitizenSyncAt = now + VisibleCitizenSyncIntervalSeconds;
                syncVisibleCitizens();
            }

            diagnosticSystem.MarkVisible(ref timings);
            RecalculateTotalsIfDue(syncSummaryEntity: true, now, recalculateTotals);
            diagnosticSystem.MarkTotals(ref timings);
        }
        finally
        {
            diagnosticSystem.EndFrame(ref timings, state);
        }
    }

    private void RecalculateTotalsIfDue(bool syncSummaryEntity, float now, Action<bool> recalculateTotals)
    {
        if (now < _nextTotalsRefreshAt)
            return;

        _nextTotalsRefreshAt = now + TotalsRefreshIntervalSeconds;
        recalculateTotals(syncSummaryEntity);
    }
}
