using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingCitizenPopulationCompositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static CitizenPopulationCompositionSystem CreateBoundary(BuildingCitizenPopulationCompositionSystem system)
    {
        return system != null ? system.CreateBoundary() : CreateBoundaryState();
    }

    public CitizenPopulationCompositionSystem CreateBoundary()
    {
        return CreateBoundaryState();
    }

    private static CitizenPopulationCompositionSystem CreateBoundaryState()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPopulationCompositionSystem>()
            : null;
    }

    public static CitizenPopulationCompositionSystem.Result Create(BuildingCitizenPopulationCompositionSystem system)
    {
        return system != null ? system.Create() : CreateState();
    }

    public CitizenPopulationCompositionSystem.Result Create()
    {
        return CreateState();
    }

    private static CitizenPopulationCompositionSystem.Result CreateState()
    {
        return CitizenPopulationCompositionSystem.Create();
    }

    public static void Initialize(
        BuildingCitizenPopulationCompositionSystem system,
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

    public static void Initialize(
        BuildingCitizenPopulationCompositionSystem system,
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
        CitizenResourceSystem.Context resourceContext =
            BuildingRuntimeResourcePrefabContextSystem.CreateCitizenResourceContext(
                runtimeResourcePrefabContextSystem,
                runtimeResourcePrefabSource);
        CitizenPrefabSystem.Context prefabContext =
            BuildingRuntimeResourcePrefabContextSystem.CreateCitizenPrefabContext(
                runtimeResourcePrefabContextSystem,
                runtimeResourcePrefabSource);
        CitizenPopulationCompositionSystem.Init(
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
        BuildingCitizenPopulationCompositionSystem system,
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition)
    {
        if (system != null)
        {
            system.Dispose(citizenPopulationCompositionBoundary, citizenPopulationComposition);
            return;
        }

        DisposeState(citizenPopulationCompositionBoundary, citizenPopulationComposition);
    }

    public void Dispose(
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition)
    {
        DisposeState(citizenPopulationCompositionBoundary, citizenPopulationComposition);
    }

    private static void DisposeState(
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition)
    {
        CitizenPopulationCompositionSystem.Dispose(
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition);
    }

    public static void Bind(
        BuildingCitizenPopulationCompositionSystem system,
        BuildingGameplayDependencySystem dependencySystem,
        DayNightSystem dayNight,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventSystem citizenPopulationEventSystem)
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
        BuildingGameplayDependencySystem dependencySystem,
        DayNightSystem dayNight,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventSystem citizenPopulationEventSystem)
    {
        BindState(
            dependencySystem,
            dayNight,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            citizenPopulationEventSystem);
    }

    private static void BindState(
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
