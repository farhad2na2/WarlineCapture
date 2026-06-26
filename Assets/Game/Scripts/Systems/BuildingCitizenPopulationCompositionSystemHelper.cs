using Unity.Entities;
using UnityEngine;

internal sealed class BuildingCitizenPopulationCompositionSystemHelper
{
    public static CitizenPopulationCompositionSystemHelper CreateBoundary(BuildingCitizenPopulationCompositionSystemHelper system)
    {
        return system != null ? system.CreateBoundary() : CreateBoundaryState();
    }

    public CitizenPopulationCompositionSystemHelper CreateBoundary()
    {
        return CreateBoundaryState();
    }

    private static CitizenPopulationCompositionSystemHelper CreateBoundaryState()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? new CitizenPopulationCompositionSystemHelper()
            : null;
    }

    public static CitizenPopulationCompositionSystemHelper.Result Create(BuildingCitizenPopulationCompositionSystemHelper system)
    {
        return system != null ? system.Create() : CreateState();
    }

    public CitizenPopulationCompositionSystemHelper.Result Create()
    {
        return CreateState();
    }

    private static CitizenPopulationCompositionSystemHelper.Result CreateState()
    {
        return CitizenPopulationCompositionSystemHelper.Create();
    }

    public static void Initialize(
        BuildingCitizenPopulationCompositionSystemHelper system,
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource,
        BuildingRuntimeReadModelCompositionSystemHelper runtimeQuery,
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
        DayNightSystem dayNight,
        Camera worldCamera)
    {
        Initialize(
            system,
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
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource,
        BuildingRuntimeReadModelCompositionSystemHelper runtimeQuery,
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
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

    public static void Initialize(
        BuildingCitizenPopulationCompositionSystemHelper system,
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource,
        BuildingRuntimeReadModelCompositionSystemHelper runtimeQuery,
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
        DayNightSystem dayNight,
        Camera worldCamera,
        bool populationEnabled)
    {
        if (system != null)
        {
            system.Initialize(
                citizenPopulationCompositionBoundary,
                citizenPopulationComposition,
                runtimeResourcePrefabContextSystem,
                runtimeResourcePrefabSource,
                runtimeQuery,
                runtimeQueryContext,
                dayNight,
                worldCamera,
                populationEnabled);
            return;
        }

        InitializeState(
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition,
            runtimeResourcePrefabContextSystem,
            runtimeResourcePrefabSource,
            runtimeQuery,
            runtimeQueryContext,
            dayNight,
            worldCamera,
            populationEnabled);
    }

    public void Initialize(
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource,
        BuildingRuntimeReadModelCompositionSystemHelper runtimeQuery,
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
        DayNightSystem dayNight,
        Camera worldCamera,
        bool populationEnabled)
    {
        InitializeState(
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition,
            runtimeResourcePrefabContextSystem,
            runtimeResourcePrefabSource,
            runtimeQuery,
            runtimeQueryContext,
            dayNight,
            worldCamera,
            populationEnabled);
    }

    private static void InitializeState(
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource,
        BuildingRuntimeReadModelCompositionSystemHelper runtimeQuery,
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
        DayNightSystem dayNight,
        Camera worldCamera,
        bool populationEnabled)
    {
        CitizenResourceSystem.Context resourceContext =
            BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateCitizenResourceContext(
                runtimeResourcePrefabContextSystem,
                runtimeResourcePrefabSource);
        CitizenPrefabSystem.Context prefabContext =
            BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateCitizenPrefabContext(
                runtimeResourcePrefabContextSystem,
                runtimeResourcePrefabSource);
        CitizenPopulationCompositionSystemHelper.Init(
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition,
            runtimeQuery,
            runtimeQueryContext,
            dayNight,
            worldCamera,
            populationEnabled,
            resourceContext,
            prefabContext);
    }

    public static void Dispose(
        BuildingCitizenPopulationCompositionSystemHelper system,
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition)
    {
        if (system != null)
        {
            system.Dispose(citizenPopulationCompositionBoundary, citizenPopulationComposition);
            return;
        }

        DisposeState(citizenPopulationCompositionBoundary, citizenPopulationComposition);
    }

    public void Dispose(
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition)
    {
        DisposeState(citizenPopulationCompositionBoundary, citizenPopulationComposition);
    }

    private static void DisposeState(
        CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition)
    {
        CitizenPopulationCompositionSystemHelper.Dispose(
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition);
    }

    public static void Bind(
        BuildingCitizenPopulationCompositionSystemHelper system,
        BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
        DayNightSystem dayNight,
        SelectionUiCameraSystemHelper selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventCompositionSystemHelper citizenPopulationEventSystem)
    {
        if (system != null)
        {
            system.Bind(
                dependencySystem,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                citizenPopulationEventSystem);
            return;
        }

        BindState(
            dependencySystem,
            dayNight,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            citizenPopulationEventSystem);
    }

    public void Bind(
        BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
        DayNightSystem dayNight,
        SelectionUiCameraSystemHelper selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventCompositionSystemHelper citizenPopulationEventSystem)
    {
        BindState(
            dependencySystem,
            dayNight,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            citizenPopulationEventSystem);
    }

    private static void BindState(
        BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
        DayNightSystem dayNight,
        SelectionUiCameraSystemHelper selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventCompositionSystemHelper citizenPopulationEventSystem)
    {
        dependencySystem?.BindRuntimeDependencies(
            null,
            dayNight,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            citizenPopulationEventSystem: citizenPopulationEventSystem);
    }
}
