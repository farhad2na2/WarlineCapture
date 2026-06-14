using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialConfiguredBuildingRequestSystem
{
    public bool Enqueue(
        EntityManager em,
        Entity boundaryEntity,
        Entity configEntity,
        NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem,
        out int requestCount)
    {
        requestCount = 0;
        if (boundaryEntity == Entity.Null)
            return false;

        var spawnableSystem = new InitialBuildingSpawnableSystem();
        DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawnsBuffer =
            em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity);
        for (int buildingIndex = 0; buildingIndex < buildingSpawnsBuffer.Length; buildingIndex++)
        {
            InitialUnitsFactionBuildingSpawnEntry building = buildingSpawnsBuffer[buildingIndex];
            if (building.Prefab == Entity.Null)
                continue;

            if (!TryGetFactionSpawnCell(factionSpawns, building.FactionId, out int2 factionSpawnCell))
            {
                diagnosticLogSystem.EnqueueWarning(em, $"[InitialSpawn] skipping initial building entry with no faction spawn. faction={building.FactionId} prefab={building.Prefab}");
                continue;
            }

            string buildingId = building.PrefabLookupKey.ToString();
            if (!spawnableSystem.TryResolveSpawnableReadModel(em, boundaryEntity, buildingId, out _))
            {
                diagnosticLogSystem.EnqueueWarning(em, $"[InitialSpawn] skipping unresolved initial building entry. faction={building.FactionId} buildingId={buildingId}");
                continue;
            }

            int2 origin = factionSpawnCell + building.OriginOffset;
            EnqueueInitialBuildingSpawnRequest(
                em,
                boundaryEntity,
                configEntity,
                building.FactionId,
                buildingId,
                origin);
            requestCount++;
        }

        return true;
    }

    private static bool TryGetFactionSpawnCell(NativeArray<InitialUnitsFactionSpawnEntry> spawns, byte factionId, out int2 spawnCell)
    {
        for (int i = 0; i < spawns.Length; i++)
        {
            if (spawns[i].FactionId != factionId)
                continue;

            spawnCell = spawns[i].SpawnCell;
            return true;
        }

        spawnCell = default;
        return false;
    }

    private static void EnqueueInitialBuildingSpawnRequest(
        EntityManager em,
        Entity boundaryEntity,
        Entity configEntity,
        byte factionId,
        string buildingId,
        int2 origin)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            new InitialBuildingBoundarySystem().GetRuntimeSpawnRequests(em, boundaryEntity);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = requests.Length + 1,
            RequestKind = BuildingRuntimeSpawnRequest.KindBuilding,
            FactionId = factionId,
            HasOwnerFaction = 1,
            BuildingId = new FixedString128Bytes(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId)),
            PreferredOrigin = origin,
            EndOrigin = default,
            RotateVertical = 0,
            AllowExistingWallOverlap = 0,
            Status = BuildingRuntimeSpawnRequest.Pending,
            PlanEntity = configEntity,
            EntryIndex = 0
        });
    }
}
