using System;
using UnityEngine;

internal sealed class CitizenPopulationLifecycleSystem
{
    private const float LogicalCitizenUpdateIntervalSeconds = 0.2f;
    private const float VisibleCitizenSyncIntervalSeconds = 0.12f;
    private const float TotalsRefreshIntervalSeconds = 0.25f;

    public struct State
    {
        public float NextLogicalCitizenUpdateAt;
        public float NextVisibleCitizenSyncAt;
        public float NextTotalsRefreshAt;
    }

    private State _state;

    public static void Reset(CitizenPopulationLifecycleSystem system, ref State state)
    {
        if (system != null)
        {
            system.Reset();
            return;
        }

        ResetState(ref state);
    }

    public void Reset()
    {
        ResetState(ref _state);
    }

    public static void Update(
        CitizenPopulationLifecycleSystem system,
        ref State state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenDangerSystem dangerSystem,
        CitizenPopulationDiagnosticsSystemHelper diagnosticSystem,
        CitizenPopulationStateSystem populationState,
        Action updateLogicalCitizens,
        Action syncVisibleCitizens,
        Action<bool> recalculateTotals,
        Func<bool> hasPendingPathJob,
        float now)
    {
        if (system != null)
        {
            system.Update(
                buildingReadSystem,
                ecsProjection,
                dangerSystem,
                diagnosticSystem,
                populationState,
                updateLogicalCitizens,
                syncVisibleCitizens,
                recalculateTotals,
                hasPendingPathJob,
                now);
            return;
        }

        UpdateState(
            ref state,
            buildingReadSystem,
            ecsProjection,
            dangerSystem,
            diagnosticSystem,
            populationState,
            updateLogicalCitizens,
            syncVisibleCitizens,
            recalculateTotals,
            hasPendingPathJob,
            now);
    }

    public void Update(
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenDangerSystem dangerSystem,
        CitizenPopulationDiagnosticsSystemHelper diagnosticSystem,
        CitizenPopulationStateSystem state,
        Action updateLogicalCitizens,
        Action syncVisibleCitizens,
        Action<bool> recalculateTotals,
        Func<bool> hasPendingPathJob,
        float now)
    {
        UpdateState(
            ref _state,
            buildingReadSystem,
            ecsProjection,
            dangerSystem,
            diagnosticSystem,
            state,
            updateLogicalCitizens,
            syncVisibleCitizens,
            recalculateTotals,
            hasPendingPathJob,
            now);
    }

    private static void ResetState(ref State state)
    {
        state.NextLogicalCitizenUpdateAt = 0f;
        state.NextVisibleCitizenSyncAt = 0f;
        state.NextTotalsRefreshAt = 0f;
    }

    private static void UpdateState(
        ref State lifecycleState,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenDangerSystem dangerSystem,
        CitizenPopulationDiagnosticsSystemHelper diagnosticSystem,
        CitizenPopulationStateSystem state,
        Action updateLogicalCitizens,
        Action syncVisibleCitizens,
        Action<bool> recalculateTotals,
        Func<bool> hasPendingPathJob,
        float now)
    {
        CitizenPopulationDiagnosticsSystemHelper.FrameTimings timings = CitizenPopulationDiagnosticsSystemHelper.BeginFrame(diagnosticSystem);
        try
        {
            if (!buildingReadSystem.HasRuntimeBuildingQuery())
                return;

            bool refreshedBuildings = buildingReadSystem.RefreshRuntimeBuildingListsIfDue(now);
            CitizenPopulationDiagnosticsSystemHelper.MarkBuildings(diagnosticSystem, ref timings);

            if (hasPendingPathJob != null && hasPendingPathJob())
            {
                CitizenPopulationDiagnosticsSystemHelper.MarkSkippedForPathfinding(diagnosticSystem, ref timings);
                RecalculateTotalsIfDue(ref lifecycleState, syncSummaryEntity: false, now, recalculateTotals);
                CitizenPopulationDiagnosticsSystemHelper.MarkTotals(diagnosticSystem, ref timings);
                return;
            }

            ecsProjection.ResolveEntityManager();
            ecsProjection.EnsurePopulationSummaryEntity();
            CitizenPopulationDiagnosticsSystemHelper.MarkResolve(diagnosticSystem, ref timings);

            CitizenDangerSystem.RefreshDangerSourcesIfNeeded(dangerSystem, now);
            CitizenPopulationDiagnosticsSystemHelper.MarkDanger(diagnosticSystem, ref timings);

            if (now >= lifecycleState.NextLogicalCitizenUpdateAt)
            {
                lifecycleState.NextLogicalCitizenUpdateAt = now + LogicalCitizenUpdateIntervalSeconds;
                if (!refreshedBuildings)
                    buildingReadSystem.RefreshRuntimeBuildingLists(now, force: true);
                updateLogicalCitizens();
            }

            CitizenPopulationDiagnosticsSystemHelper.MarkLogical(diagnosticSystem, ref timings);

            if (now >= lifecycleState.NextVisibleCitizenSyncAt)
            {
                lifecycleState.NextVisibleCitizenSyncAt = now + VisibleCitizenSyncIntervalSeconds;
                syncVisibleCitizens();
            }

            CitizenPopulationDiagnosticsSystemHelper.MarkVisible(diagnosticSystem, ref timings);
            RecalculateTotalsIfDue(ref lifecycleState, syncSummaryEntity: true, now, recalculateTotals);
            CitizenPopulationDiagnosticsSystemHelper.MarkTotals(diagnosticSystem, ref timings);
        }
        finally
        {
            CitizenPopulationDiagnosticsSystemHelper.EndFrame(diagnosticSystem, ref timings, state);
        }
    }

    private static void RecalculateTotalsIfDue(
        ref State state,
        bool syncSummaryEntity,
        float now,
        Action<bool> recalculateTotals)
    {
        if (now < state.NextTotalsRefreshAt)
            return;

        state.NextTotalsRefreshAt = now + TotalsRefreshIntervalSeconds;
        recalculateTotals(syncSummaryEntity);
    }
}
