using Unity.Entities;

public readonly struct InitialSpawnCompletionSystem
{
    public bool Update(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity configEntity,
        InitialUnitsSpawnConfig config,
        ref InitialUnitsSpawnProgress progress,
        bool allUnitsSpawned,
        bool allBlockersSpawned,
        int maxInitialBuildingCompletionWaitFrames,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem,
        out bool progressChanged)
    {
        progressChanged = false;
        bool canCompleteInitialSpawn = CanCompleteInitialSpawn(em, configEntity, config, progress);
        if (allUnitsSpawned &&
            allBlockersSpawned &&
            !canCompleteInitialSpawn)
        {
            progress.InitialBuildingCompletionWaitFrames++;
            progressChanged = true;
            if (progress.InitialBuildingCompletionWaitFrames >= maxInitialBuildingCompletionWaitFrames)
            {
                progress.InitialBuildingsSpawned = 1;
                canCompleteInitialSpawn = true;
                diagnosticLogSystem.EnqueueWarning(em, $"[InitialSpawn] fail-open initial building completion after {progress.InitialBuildingCompletionWaitFrames} frames. The startup loading gate will clear, but initial buildings may be missing or incomplete.");
            }
        }

        if (allUnitsSpawned &&
            canCompleteInitialSpawn &&
            allBlockersSpawned)
        {
            ecb.AddComponent<InitialUnitsSpawnInitialized>(configEntity);
            ecb.RemoveComponent<InitialUnitsSpawnProgress>(configEntity);
            ecb.RemoveComponent<InitialUnitsFactionUnitSpawnProgress>(configEntity);
            return true;
        }

        return false;
    }

    private static bool CanCompleteInitialSpawn(
        EntityManager em,
        Entity configEntity,
        InitialUnitsSpawnConfig config,
        InitialUnitsSpawnProgress progress)
    {
        return progress.InitialBuildingsSpawned != 0 ||
               !RequiresInitialBuildingCompletion(em, configEntity, config);
    }

    private static bool RequiresInitialBuildingCompletion(EntityManager em, Entity configEntity, InitialUnitsSpawnConfig config)
    {
        if (config.CreateFactionBases != 0)
            return true;

        return em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity) &&
               em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity).Length > 0;
    }
}
