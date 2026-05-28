using Unity.Collections;
using Unity.Entities;

public readonly struct InitialBuildingSpawnableSystem
{
    public bool TryResolveSpawnableId(
        EntityManager em,
        Entity boundaryEntity,
        FixedString128Bytes configuredKey,
        string fallbackKey,
        out string buildingId,
        out BuildingConfiguredSpawnableReadModel model)
    {
        model = default;
        buildingId = configuredKey.ToString();
        if (!string.IsNullOrWhiteSpace(buildingId) &&
            TryResolveSpawnableReadModel(em, boundaryEntity, buildingId, out model))
        {
            return true;
        }

        buildingId = fallbackKey;
        return !string.IsNullOrWhiteSpace(buildingId) &&
               TryResolveSpawnableReadModel(em, boundaryEntity, buildingId, out model);
    }

    public bool TryResolveSpawnableReadModel(
        EntityManager em,
        Entity boundaryEntity,
        string buildingId,
        out BuildingConfiguredSpawnableReadModel model)
    {
        model = default;
        if (!new InitialBuildingBoundarySystem().TryGetConfiguredSpawnableReadModels(
                em,
                boundaryEntity,
                out DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables))
            return false;

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
        for (int i = 0; i < spawnables.Length; i++)
        {
            BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
            if (candidate.BuildingId.ToString() != normalized)
                continue;

            model = candidate;
            return true;
        }

        return false;
    }
}
