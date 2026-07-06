using System;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class BuildingGameplayDisposalExecutionCompositionSystemHelper
    {
        internal readonly struct Source
        {
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly BuildingPlacementStartupSystemHelper PlacementStartupSystem;
            public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
            public readonly BuildingPlacementPreviewPresentationSystemHelper PlacementPreviewSystem;
            public readonly BuildingPlacementVisualPresentationSystemHelper PlacementVisualPresentationSystem;
            public readonly BuildingRuntimeObjectPresentationSystemHelper RuntimeObjectPresentationHelper;
            public readonly UnitPathfindingPendingStateReader UnitPathfindingPendingStateReader;
            public readonly Action ExitBuildMode;

            public Source(
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                BuildingPlacementStartupSystemHelper placementStartupSystem,
                BuildingDefinitionPrefabSystemHelper definitionSystem,
                BuildingPlacementPreviewPresentationSystemHelper placementPreviewSystem,
                BuildingPlacementVisualPresentationSystemHelper placementVisualPresentationSystem,
                BuildingRuntimeObjectPresentationSystemHelper runtimeObjectPresentationHelper,
                UnitPathfindingPendingStateReader unitPathfindingPendingStateReadSystem,
                Action exitBuildMode)
            {
                RuntimeBuildingSystem = runtimeBuildingSystem;
                PlacementStartupSystem = placementStartupSystem;
                DefinitionSystem = definitionSystem;
                PlacementPreviewSystem = placementPreviewSystem;
                PlacementVisualPresentationSystem = placementVisualPresentationSystem;
                RuntimeObjectPresentationHelper = runtimeObjectPresentationHelper;
                UnitPathfindingPendingStateReader = unitPathfindingPendingStateReadSystem;
                ExitBuildMode = exitBuildMode;
            }
        }

        internal void Dispose(Source source)
        {
            source.ExitBuildMode?.Invoke();

            if (source.RuntimeBuildingSystem != null)
            {
                foreach (RuntimeBuildingEntity building in source.RuntimeBuildingSystem.Buildings.Values)
                {
                    if (building == null)
                        continue;

                    if (building.Instance != null)
                        source.RuntimeObjectPresentationHelper?.DestroyRuntimeObject(building.Instance);

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

            source.PlacementVisualPresentationSystem?.Dispose();
            source.PlacementStartupSystem?.Dispose(
                source.DefinitionSystem,
                source.PlacementPreviewSystem,
                target => source.RuntimeObjectPresentationHelper?.DestroyRuntimeObject(target));
            source.UnitPathfindingPendingStateReader?.Dispose();
        }

        private static bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = default;
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            entityManager = world.EntityManager;
            return true;
        }
    }
}
