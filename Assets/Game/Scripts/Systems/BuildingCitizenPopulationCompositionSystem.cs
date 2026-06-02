using UnityEngine;

internal sealed class BuildingCitizenPopulationCompositionSystem
{
    public CitizenPopulationCompositionSystem CreateBoundary()
    {
        return new CitizenPopulationCompositionSystem();
    }

    public CitizenPopulationCompositionSystem.Result Create()
    {
        return CitizenPopulationCompositionSystem.Create();
    }

    public void Initialize(
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
        BuildingRuntimeResourcePrefabContextSystem runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextSystem.Source runtimeResourcePrefabSource,
        BuildingRuntimeQuerySystem runtimeQuery,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        DayNightSystem dayNight,
        Camera worldCamera)
    {
        Initialize(
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition,
            runtimeResourcePrefabContextSystem,
            runtimeResourcePrefabSource,
            runtimeQuery,
            runtimeQueryContext,
            dayNight,
            worldCamera,
            populationEnabled: true);
    }

    public void Initialize(
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
        BuildingRuntimeResourcePrefabContextSystem runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextSystem.Source runtimeResourcePrefabSource,
        BuildingRuntimeQuerySystem runtimeQuery,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        DayNightSystem dayNight,
        Camera worldCamera,
        bool populationEnabled)
    {
        CitizenResourceSystem.Context resourceContext = runtimeResourcePrefabContextSystem.CreateCitizenResourceContext(runtimeResourcePrefabSource);
        CitizenPrefabSystem.Context prefabContext = runtimeResourcePrefabContextSystem.CreateCitizenPrefabContext(runtimeResourcePrefabSource);
        citizenPopulationCompositionBoundary.Init(
            citizenPopulationComposition,
            runtimeQuery,
            runtimeQueryContext,
            dayNight,
            worldCamera,
            populationEnabled,
            resourceContext,
            prefabContext);
    }

    public void Dispose(
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition)
    {
        citizenPopulationCompositionBoundary.Dispose(citizenPopulationComposition);
    }

    public void Bind(
        BuildingGameplayDependencySystem dependencySystem,
        DayNightSystem dayNight,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventSystem citizenPopulationEventSystem)
    {
        dependencySystem?.BindRuntimeDependencies(
            null,
            dayNight,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            citizenPopulationEventSystem: citizenPopulationEventSystem);
    }
}
