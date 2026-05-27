using UnityEngine;

internal sealed class CitizenPopulationCompositionSystem
{
    public sealed class Result
    {
        public readonly CitizenResourceSystem CitizenResourceSystem = new();
        public CitizenResourceSystem.Context CitizenResourceContext;
        public readonly CitizenPrefabSystem CitizenPrefabSystem = new();
        public CitizenPrefabSystem.Context CitizenPrefabContext;
        public readonly CitizenPopulationStateSystem State = new();
        public readonly CitizenPopulationEcsProjectionSystem EcsProjection = new();
        public readonly CitizenPopulationTotalsSystem TotalsSystem = new();
        public readonly CitizenPopulationReadModelSystem ReadModel = new();
        public readonly CitizenBuildingReadSystem BuildingReadSystem = new();
        public readonly CitizenHouseholdRegistrationSystem HouseholdRegistrationSystem = new();
        public readonly CitizenRefugeeSystem RefugeeSystem = new();
        public readonly CitizenScheduleSystem ScheduleSystem = new();
        public readonly CitizenStatusTransitionSystem StatusTransitionSystem = new();
        public readonly CitizenDangerSystem DangerSystem = new();
        public readonly CitizenTravelSystem TravelSystem = new();
        public readonly CitizenMovementCommandSystem MovementCommandSystem = new();
        public readonly CitizenPrefabSelectionSystem PrefabSelectionSystem = new();
        public readonly CitizenVisibleUnitSystem VisibleUnitSystem = new();
        public readonly CitizenPopulationEventSystem EventSystem = new();
        public readonly CitizenPopulationDebugSystem DebugSystem = new();
        public readonly CitizenPopulationDiagnosticSystem DiagnosticSystem = new();
        public readonly CitizenPopulationLifecycleSystem LifecycleSystem = new();
        public readonly CitizenPopulationRuntimeUpdateSystem RuntimeUpdateSystem = new();
        public readonly UnitPathfindingPendingStateReadSystem UnitPathfindingPendingStateReadSystem = new();
        public DayNightSystem DayNightSystem;
        public Camera WorldCamera;
    }

    public static Result Create()
    {
        return new Result();
    }

    public void Init(
        Result result,
        BuildingRuntimeQuerySystem buildingRuntimeQuerySystem,
        BuildingRuntimeQuerySystem.Context buildingRuntimeQueryContext,
        DayNightSystem dayNightSystem,
        Camera worldCamera,
        CitizenResourceSystem.Context citizenResourceContext,
        CitizenPrefabSystem.Context citizenPrefabContext)
    {
        result.BuildingReadSystem.Init(buildingRuntimeQuerySystem, buildingRuntimeQueryContext);
        result.CitizenResourceContext = citizenResourceContext;
        result.CitizenPrefabContext = citizenPrefabContext;
        result.DayNightSystem = dayNightSystem;
        result.WorldCamera = worldCamera;
        result.RuntimeUpdateSystem.Bind(result);
        result.EcsProjection.ResolveEntityManager();
        result.VisibleUnitSystem.ClearVisibleCitizens(result.State, result.EcsProjection);
        result.State.Reset();
        result.ReadModel.Reset();
        result.LifecycleSystem.Reset();
        result.RefugeeSystem.Reset();
        result.DangerSystem.Reset();
        result.PrefabSelectionSystem.Init(result.CitizenPrefabSystem, result.CitizenPrefabContext);
        result.EventSystem.Init(
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
    }

    public void Dispose(Result result)
    {
        result.EcsProjection.DestroyAllCitizenEntities(result.State);
        result.VisibleUnitSystem.ClearVisibleCitizens(result.State, result.EcsProjection);
        result.State.Reset();
        result.ReadModel.Reset();
        result.BuildingReadSystem.Dispose();
        result.DangerSystem.Reset();
        result.LifecycleSystem.Reset();
        result.RefugeeSystem.Reset();
        result.PrefabSelectionSystem.Reset();
        result.EventSystem.Reset();
        result.RuntimeUpdateSystem.Reset();
        result.UnitPathfindingPendingStateReadSystem.Dispose();
        result.EcsProjection.Reset();
        result.DayNightSystem = null;
        result.WorldCamera = null;
    }
}
