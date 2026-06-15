using Unity.Entities;
using UnityEngine;

internal sealed partial class CitizenPopulationCompositionSystem : SystemBase
{
    public sealed class Result
    {
        public readonly CitizenResourceSystem CitizenResourceSystem = ResolveCitizenResourceSystem();
        public CitizenResourceSystem.Context CitizenResourceContext;
        public readonly CitizenPrefabSystem CitizenPrefabSystem = ResolveCitizenPrefabSystem();
        public CitizenPrefabSystem.Context CitizenPrefabContext;
        public readonly CitizenPopulationStateSystem State = new();
        public readonly CitizenPopulationEcsProjectionSystem EcsProjection = new();
        public readonly CitizenPopulationTotalsSystem TotalsSystem = ResolveCitizenPopulationTotalsSystem();
        public readonly CitizenPopulationReadModelSystem ReadModel = ResolveCitizenPopulationReadModelSystem();
        public CitizenPopulationReadModelSystem.State ReadModelState;
        public readonly CitizenBuildingReadSystem BuildingReadSystem = new();
        public readonly CitizenHouseholdRegistrationSystem HouseholdRegistrationSystem = ResolveCitizenHouseholdRegistrationSystem();
        public readonly CitizenRefugeeSystem RefugeeSystem = ResolveCitizenRefugeeSystem();
        public CitizenRefugeeSystem.State RefugeeState;
        public readonly CitizenScheduleSystem ScheduleSystem = ResolveCitizenScheduleSystem();
        public readonly CitizenStatusTransitionSystem StatusTransitionSystem = ResolveCitizenStatusTransitionSystem();
        public readonly CitizenDangerSystem DangerSystem = ResolveCitizenDangerSystem();
        public readonly CitizenTravelSystem TravelSystem = ResolveCitizenTravelSystem();
        public readonly CitizenPrefabSelectionSystem PrefabSelectionSystem = ResolveCitizenPrefabSelectionSystem();
        public readonly CitizenVisibleUnitSystem VisibleUnitSystem = new();
        public readonly CitizenPopulationEventSystem EventSystem = ResolveCitizenPopulationEventSystem();
        public readonly CitizenPopulationDebugSystem DebugSystem = ResolveCitizenPopulationDebugSystem();
        public readonly CitizenPopulationDiagnosticSystem DiagnosticSystem = ResolveCitizenPopulationDiagnosticSystem();
        public readonly CitizenPopulationLifecycleSystem LifecycleSystem = ResolveCitizenPopulationLifecycleSystem();
        public CitizenPopulationLifecycleSystem.State LifecycleState;
        public readonly CitizenPopulationRuntimeUpdateSystem RuntimeUpdateSystem = new();
        public readonly UnitPathfindingPendingStateReader UnitPathfindingPendingStateReader = new();
        public DayNightSystem DayNightSystem;
        public Camera WorldCamera;
        public bool PopulationEnabled;
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static Result Create()
    {
        return new Result();
    }

    public static void Init(
        CitizenPopulationCompositionSystem system,
        Result result,
        BuildingRuntimeQuerySystem buildingRuntimeQuerySystem,
        BuildingRuntimeQuerySystem.Context buildingRuntimeQueryContext,
        DayNightSystem dayNightSystem,
        Camera worldCamera,
        bool populationEnabled,
        CitizenResourceSystem.Context citizenResourceContext,
        CitizenPrefabSystem.Context citizenPrefabContext)
    {
        if (system != null)
        {
            system.Init(
                result,
                buildingRuntimeQuerySystem,
                buildingRuntimeQueryContext,
                dayNightSystem,
                worldCamera,
                populationEnabled,
                citizenResourceContext,
                citizenPrefabContext);
            return;
        }

        InitState(
            result,
            buildingRuntimeQuerySystem,
            buildingRuntimeQueryContext,
            dayNightSystem,
            worldCamera,
            populationEnabled,
            citizenResourceContext,
            citizenPrefabContext);
    }

    public void Init(
        Result result,
        BuildingRuntimeQuerySystem buildingRuntimeQuerySystem,
        BuildingRuntimeQuerySystem.Context buildingRuntimeQueryContext,
        DayNightSystem dayNightSystem,
        Camera worldCamera,
        bool populationEnabled,
        CitizenResourceSystem.Context citizenResourceContext,
        CitizenPrefabSystem.Context citizenPrefabContext)
    {
        InitState(
            result,
            buildingRuntimeQuerySystem,
            buildingRuntimeQueryContext,
            dayNightSystem,
            worldCamera,
            populationEnabled,
            citizenResourceContext,
            citizenPrefabContext);
    }

    private static void InitState(
        Result result,
        BuildingRuntimeQuerySystem buildingRuntimeQuerySystem,
        BuildingRuntimeQuerySystem.Context buildingRuntimeQueryContext,
        DayNightSystem dayNightSystem,
        Camera worldCamera,
        bool populationEnabled,
        CitizenResourceSystem.Context citizenResourceContext,
        CitizenPrefabSystem.Context citizenPrefabContext)
    {
        result.BuildingReadSystem.Init(buildingRuntimeQuerySystem, buildingRuntimeQueryContext);
        result.CitizenResourceContext = citizenResourceContext;
        result.CitizenPrefabContext = citizenPrefabContext;
        result.DayNightSystem = dayNightSystem;
        result.WorldCamera = worldCamera;
        result.PopulationEnabled = populationEnabled;
        result.RuntimeUpdateSystem.Bind(result);
        result.EcsProjection.ResolveEntityManager();
        result.VisibleUnitSystem.ClearVisibleCitizens(result.State, result.EcsProjection);
        result.State.Reset();
        CitizenPopulationReadModelSystem.Reset(result.ReadModel, ref result.ReadModelState);
        CitizenPopulationLifecycleSystem.Reset(result.LifecycleSystem, ref result.LifecycleState);
        CitizenRefugeeSystem.Reset(result.RefugeeSystem, ref result.RefugeeState);
        CitizenDangerSystem.Reset(result.DangerSystem);
        result.PrefabSelectionSystem?.Init(result.CitizenPrefabSystem, result.CitizenPrefabContext);
        result.EventSystem?.Init(
            result.State,
            result.BuildingReadSystem,
            result.HouseholdRegistrationSystem,
            result.RefugeeSystem,
            result.TravelSystem,
            result.EcsProjection,
            result.StatusTransitionSystem,
            result.RuntimeUpdateSystem.StoreHousehold,
            result.RuntimeUpdateSystem.StoreCitizen,
            result.RuntimeUpdateSystem.HandleCitizenDeath);
        result.EcsProjection.EnsurePopulationSummaryEntity();
        CitizenPopulationReadModelSystem.Refresh(
            result.ReadModel,
            ref result.ReadModelState,
            result.TotalsSystem,
            result.State,
            result.EcsProjection,
            syncSummaryEntity: true);
    }

    public static void Dispose(CitizenPopulationCompositionSystem system, Result result)
    {
        if (system != null)
        {
            system.Dispose(result);
            return;
        }

        DisposeState(result);
    }

    public void Dispose(Result result)
    {
        DisposeState(result);
    }

    private static void DisposeState(Result result)
    {
        result.EcsProjection.DestroyAllCitizenEntities(result.State);
        result.VisibleUnitSystem.ClearVisibleCitizens(result.State, result.EcsProjection);
        result.State.Reset();
        CitizenPopulationReadModelSystem.Reset(result.ReadModel, ref result.ReadModelState);
        result.BuildingReadSystem.Dispose();
        CitizenDangerSystem.Reset(result.DangerSystem);
        CitizenPopulationLifecycleSystem.Reset(result.LifecycleSystem, ref result.LifecycleState);
        CitizenRefugeeSystem.Reset(result.RefugeeSystem, ref result.RefugeeState);
        result.PrefabSelectionSystem?.Reset();
        result.EventSystem?.Reset();
        result.RuntimeUpdateSystem.Reset();
        result.UnitPathfindingPendingStateReader.Dispose();
        result.EcsProjection.Reset();
        result.DayNightSystem = null;
        result.WorldCamera = null;
        result.PopulationEnabled = false;
    }

    private static CitizenPrefabSystem ResolveCitizenPrefabSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPrefabSystem>()
            : null;
    }

    private static CitizenResourceSystem ResolveCitizenResourceSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenResourceSystem>()
            : null;
    }

    private static CitizenPrefabSelectionSystem ResolveCitizenPrefabSelectionSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPrefabSelectionSystem>()
            : null;
    }

    private static CitizenPopulationTotalsSystem ResolveCitizenPopulationTotalsSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPopulationTotalsSystem>()
            : null;
    }

    private static CitizenPopulationReadModelSystem ResolveCitizenPopulationReadModelSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPopulationReadModelSystem>()
            : null;
    }

    private static CitizenPopulationLifecycleSystem ResolveCitizenPopulationLifecycleSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPopulationLifecycleSystem>()
            : null;
    }

    private static CitizenScheduleSystem ResolveCitizenScheduleSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenScheduleSystem>()
            : null;
    }

    private static CitizenStatusTransitionSystem ResolveCitizenStatusTransitionSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenStatusTransitionSystem>()
            : null;
    }

    private static CitizenTravelSystem ResolveCitizenTravelSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenTravelSystem>()
            : null;
    }

    private static CitizenDangerSystem ResolveCitizenDangerSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenDangerSystem>()
            : null;
    }

    private static CitizenHouseholdRegistrationSystem ResolveCitizenHouseholdRegistrationSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenHouseholdRegistrationSystem>()
            : null;
    }

    private static CitizenRefugeeSystem ResolveCitizenRefugeeSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenRefugeeSystem>()
            : null;
    }

    private static CitizenPopulationDebugSystem ResolveCitizenPopulationDebugSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPopulationDebugSystem>()
            : null;
    }

    private static CitizenPopulationDiagnosticSystem ResolveCitizenPopulationDiagnosticSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPopulationDiagnosticSystem>()
            : null;
    }

    private static CitizenPopulationEventSystem ResolveCitizenPopulationEventSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<CitizenPopulationEventSystem>()
            : null;
    }
}
