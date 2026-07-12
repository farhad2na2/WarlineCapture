using UnityEngine;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed class BuildingGameplayStartupCompositionSystemHelper
    {
        public void Initialize(
            BuildingGameplaySourceCompositionSystemHelper childSystems,
            BuildingPlacementSystemConfig buildingPlacementConfig,
            Camera worldCamera,
            Transform runtimeUiRoot,
            RoadGridProjectionSystem.RoadFootprintState roadFootprintState,
            FactionVisualSettings factionVisuals,
            DayNightSystem dayNight)
        {
            childSystems.RuntimeResourceUtilitySystemHelper.SetInitialDollars(
                BuildingStartupConfigProjectionSystem.ResolveInitialDollars(buildingPlacementConfig));
            if (childSystems.BuildingEntityManagerAccessSystem.TryGetEntityManager(out Unity.Entities.EntityManager entityManager))
                childSystems.RuntimeResourceUtilitySystemHelper.Configure(entityManager);
            childSystems.BuildingGameplayDependencyCompositionSystemHelper.SetStartupDependencies(
                null,
                factionVisuals,
                dayNight);
            childSystems.BuildingPlacementStartupSystemHelper.ConfigureRoadFootprintState(roadFootprintState);
            childSystems.BuildingPlacementStartupSystemHelper.Init(
                buildingPlacementConfig,
                worldCamera,
                runtimeUiRoot,
                childSystems.BuildingDefinitionPrefabSystemHelper,
                childSystems.BuildingRunwaySystem,
                childSystems.BuildingPlacementPreviewPresentationSystemHelper,
                childSystems.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
        }
    }
}
