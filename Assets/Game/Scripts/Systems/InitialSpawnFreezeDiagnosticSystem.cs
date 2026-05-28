using Unity.Entities;
using UnityEngine;

public readonly struct InitialSpawnFreezeDiagnosticSystem
{
    private static readonly bool EnableInitialSpawnFreezeLogs = false;
    private const double FreezeLogThresholdSeconds = 0.05d;

    public double BeginFrame()
    {
        return Time.realtimeSinceStartupAsDouble;
    }

    public void LogIfExceeded(
        EntityManager em,
        double startTime,
        int spawnedUnitsForLog,
        int spawnedBlockersForLog,
        bool completedForLog,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem)
    {
        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        if (!EnableInitialSpawnFreezeLogs || elapsed < FreezeLogThresholdSeconds)
            return;

        diagnosticLogSystem.EnqueueLog(em, $"[FreezeDetect:ECS] InitialUnitsSpawnSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms units={spawnedUnitsForLog} blockers={spawnedBlockersForLog} completed={(completedForLog ? 1 : 0)}");
    }
}
