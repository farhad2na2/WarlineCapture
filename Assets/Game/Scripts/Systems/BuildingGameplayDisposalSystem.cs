using System;
using Unity.Entities;

internal sealed class BuildingGameplayDisposalSystem
{
    internal readonly struct Source
    {
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly BuildingPlacementStartupSystem PlacementStartupSystem;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingPlacementPreviewSystem PlacementPreviewSystem;
        public readonly BuildingRuntimeObjectSystem RuntimeObjectSystem;
        public readonly Action ExitBuildMode;

        public Source(
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            BuildingPlacementStartupSystem placementStartupSystem,
            BuildingDefinitionSystem definitionSystem,
            BuildingPlacementPreviewSystem placementPreviewSystem,
            BuildingRuntimeObjectSystem runtimeObjectSystem,
            Action exitBuildMode)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            PlacementStartupSystem = placementStartupSystem;
            DefinitionSystem = definitionSystem;
            PlacementPreviewSystem = placementPreviewSystem;
            RuntimeObjectSystem = runtimeObjectSystem;
            ExitBuildMode = exitBuildMode;
        }
    }

    internal void Dispose(Source source)
    {
        source.ExitBuildMode?.Invoke();

        if (source.RuntimeBuildingSystem != null)
        {
            foreach (RuntimeBuildingData building in source.RuntimeBuildingSystem.Buildings.Values)
            {
                if (building == null)
                    continue;

                if (building.Instance != null)
                    source.RuntimeObjectSystem?.DestroyRuntimeObject(building.Instance);

                if (TryGetEntityManager(out EntityManager em))
                {
                    if (building.CombatEntity != Entity.Null && em.Exists(building.CombatEntity))
                        em.DestroyEntity(building.CombatEntity);
                    if (building.BlockerEntity != Entity.Null && em.Exists(building.BlockerEntity))
                        em.DestroyEntity(building.BlockerEntity);
                }
            }

            source.RuntimeBuildingSystem.Clear();
        }

        source.PlacementStartupSystem?.Dispose(
            source.DefinitionSystem,
            source.PlacementPreviewSystem,
            target => source.RuntimeObjectSystem?.DestroyRuntimeObject(target));
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }
}
