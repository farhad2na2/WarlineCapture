using Unity.Entities;
using UnityEngine;

public struct InitialSpawnDiagnosticStateSystem
{
    private int _nextDiagnosticFrame;

    public void LogSpawnState(
        ref SystemState state,
        string reason,
        int diagnosticIntervalFrames,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem)
    {
        if (Time.frameCount < _nextDiagnosticFrame)
            return;

        _nextDiagnosticFrame = Time.frameCount + diagnosticIntervalFrames;
        EntityManager em = state.EntityManager;
        EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery progressQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
        EntityQuery initializedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
        EntityQuery unitGridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>());
        EntityQuery blockerDependencyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGridBlockerDependencyComponent>());
        byte depsReady = 1;
        string blockerDependencyStatus = "no-blocker-state";
        if (!blockerDependencyQuery.IsEmptyIgnoreFilter)
        {
            RuntimeGridBlockerDependencyComponent blockerState = em.GetComponentData<RuntimeGridBlockerDependencyComponent>(blockerDependencyQuery.GetSingletonEntity());
            depsReady = blockerState.ReadyForDependents;
            blockerDependencyStatus = $"ready={blockerState.ReadyForDependents} spawnOnStart={blockerState.SpawnOnStart} spawned={blockerState.Spawned} finalizing={blockerState.SpawnFinalizing} finalizeAfter={blockerState.FinalizeAfterFrames} pendingCity={blockerState.PendingCity} cityHasSpawned={blockerState.CityHasSpawned} cityGenerating={blockerState.CityGenerating}";
        }

        diagnosticLogSystem.EnqueueLog(em, $"[InitialSpawnState] frame={Time.frameCount} reason={reason} configs={configQuery.CalculateEntityCount()} progress={progressQuery.CalculateEntityCount()} initialized={initializedQuery.CalculateEntityCount()} unitGrid={unitGridQuery.CalculateEntityCount()} depsReady={depsReady} {blockerDependencyStatus}");

        configQuery.Dispose();
        progressQuery.Dispose();
        initializedQuery.Dispose();
        unitGridQuery.Dispose();
        blockerDependencyQuery.Dispose();
    }
}
