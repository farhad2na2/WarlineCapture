using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class CitizenPopulationCompositionSystemHelper
    {
        public sealed class Result
        {
            public readonly CitizenResourceCompositionSystemHelper CitizenResourceCompositionSystemHelper = ResolveCitizenResourceSystem();
            public CitizenResourceCompositionSystemHelper.Context CitizenResourceContext;
            public readonly CitizenPrefabSystem CitizenPrefabSystem = new();
            public CitizenPrefabSystem.Context CitizenPrefabContext;
            public readonly CitizenPopulationStateCompositionSystemHelper State = new();
            public readonly CitizenPopulationEcsProjectionCompositionSystemHelper EcsProjection = new();
            public readonly CitizenPopulationTotalsCompositionSystemHelper TotalsSystem = ResolveCitizenPopulationTotalsSystem();
            public readonly CitizenPopulationReadModelCompositionSystemHelper ReadModel = new();
            public CitizenPopulationReadModelCompositionSystemHelper.State ReadModelState;
            public readonly CitizenBuildingReadCompositionSystemHelper BuildingReadSystem = new();
            public readonly CitizenHouseholdRegistrationCompositionSystemHelper HouseholdRegistrationSystem = ResolveCitizenHouseholdRegistrationSystem();
            public readonly CitizenRefugeeCompositionSystemHelper RefugeeSystem = ResolveCitizenRefugeeSystem();
            public CitizenRefugeeCompositionSystemHelper.State RefugeeState;
            public readonly CitizenScheduleCompositionSystemHelper ScheduleSystem = ResolveCitizenScheduleCompositionSystemHelper();
            public readonly CitizenStatusTransitionCompositionSystemHelper StatusTransitionSystem = ResolveCitizenStatusTransitionCompositionSystemHelper();
            public readonly CitizenDangerCompositionSystemHelper DangerSystem = ResolveCitizenDangerSystem();
            public readonly CitizenTravelSystem TravelSystem = ResolveCitizenTravelSystem();
            public readonly CitizenPrefabSelectionSystem PrefabSelectionSystem = new();
            public CitizenPrefabSelectionSystem.State PrefabSelectionState;
            public readonly CitizenVisibleUnitPresentationSystemHelper VisibleUnitSystem = new();
            public readonly CitizenPopulationEventCompositionSystemHelper EventSystem = ResolveCitizenPopulationEventCompositionSystemHelper();
            public readonly CitizenPopulationDebugDiagnosticsSystemHelper DebugSystem = ResolveCitizenPopulationDebugSystem();
            public readonly CitizenPopulationDiagnosticsSystemHelper DiagnosticSystem = new();
            public readonly CitizenPopulationLifecycleCompositionSystemHelper LifecycleSystem = ResolveCitizenPopulationLifecycleCompositionSystemHelper();
            public CitizenPopulationLifecycleCompositionSystemHelper.State LifecycleState;
            public readonly CitizenPopulationRuntimeUpdateCompositionSystemHelper RuntimeUpdateSystem = new();
            public readonly UnitPathfindingPendingStateReader UnitPathfindingPendingStateReader = new();
            public DayNightSystem DayNightSystem;
            public Camera WorldCamera;
            public bool PopulationEnabled;
        }

        public static Result Create()
        {
            return new Result();
        }

        public static void Init(
            CitizenPopulationCompositionSystemHelper system,
            Result result,
            BuildingRuntimeReadModelCompositionSystemHelper buildingRuntimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context buildingRuntimeQueryContext,
            DayNightSystem dayNightSystem,
            Camera worldCamera,
            bool populationEnabled,
            CitizenResourceCompositionSystemHelper.Context citizenResourceContext,
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
            BuildingRuntimeReadModelCompositionSystemHelper buildingRuntimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context buildingRuntimeQueryContext,
            DayNightSystem dayNightSystem,
            Camera worldCamera,
            bool populationEnabled,
            CitizenResourceCompositionSystemHelper.Context citizenResourceContext,
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
            BuildingRuntimeReadModelCompositionSystemHelper buildingRuntimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context buildingRuntimeQueryContext,
            DayNightSystem dayNightSystem,
            Camera worldCamera,
            bool populationEnabled,
            CitizenResourceCompositionSystemHelper.Context citizenResourceContext,
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
            if (result.EcsProjection.HasWorld)
                result.UnitPathfindingPendingStateReader.Bind(result.EcsProjection.EntityManager);
            result.VisibleUnitSystem.ClearVisibleCitizens(result.State, result.EcsProjection);
            result.State.Reset();
            CitizenPopulationReadModelCompositionSystemHelper.Reset(result.ReadModel, ref result.ReadModelState);
            CitizenPopulationLifecycleCompositionSystemHelper.Reset(result.LifecycleSystem, ref result.LifecycleState);
            CitizenRefugeeCompositionSystemHelper.Reset(result.RefugeeSystem, ref result.RefugeeState);
            CitizenDangerCompositionSystemHelper.Reset(result.DangerSystem);
            result.PrefabSelectionSystem.Init(
                ref result.PrefabSelectionState,
                result.CitizenPrefabSystem,
                result.CitizenPrefabContext);
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
            CitizenPopulationReadModelCompositionSystemHelper.Refresh(
                result.ReadModel,
                ref result.ReadModelState,
                result.TotalsSystem,
                result.State,
                result.EcsProjection,
                syncSummaryEntity: true);
        }

        public static void Dispose(CitizenPopulationCompositionSystemHelper system, Result result)
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
            CitizenPopulationReadModelCompositionSystemHelper.Reset(result.ReadModel, ref result.ReadModelState);
            result.BuildingReadSystem.Dispose();
            CitizenDangerCompositionSystemHelper.Reset(result.DangerSystem);
            CitizenPopulationLifecycleCompositionSystemHelper.Reset(result.LifecycleSystem, ref result.LifecycleState);
            CitizenRefugeeCompositionSystemHelper.Reset(result.RefugeeSystem, ref result.RefugeeState);
            result.PrefabSelectionSystem.Reset(ref result.PrefabSelectionState);
            result.EventSystem?.Reset();
            result.RuntimeUpdateSystem.Reset();
            result.UnitPathfindingPendingStateReader.Dispose();
            result.EcsProjection.Reset();
            result.DayNightSystem = null;
            result.WorldCamera = null;
            result.PopulationEnabled = false;
        }

        private static CitizenResourceCompositionSystemHelper ResolveCitizenResourceSystem()
        {
            return new CitizenResourceCompositionSystemHelper();
        }

        private static CitizenPopulationTotalsCompositionSystemHelper ResolveCitizenPopulationTotalsSystem()
        {
            return new CitizenPopulationTotalsCompositionSystemHelper();
        }

        private static CitizenPopulationLifecycleCompositionSystemHelper ResolveCitizenPopulationLifecycleCompositionSystemHelper()
        {
            return new CitizenPopulationLifecycleCompositionSystemHelper();
        }

        private static CitizenScheduleCompositionSystemHelper ResolveCitizenScheduleCompositionSystemHelper()
        {
            return new CitizenScheduleCompositionSystemHelper();
        }

        private static CitizenStatusTransitionCompositionSystemHelper ResolveCitizenStatusTransitionCompositionSystemHelper()
        {
            return new CitizenStatusTransitionCompositionSystemHelper();
        }

        private static CitizenTravelSystem ResolveCitizenTravelSystem()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            return world != null && world.IsCreated
                ? world.GetOrCreateSystemManaged<CitizenTravelSystem>()
                : null;
        }

        private static CitizenDangerCompositionSystemHelper ResolveCitizenDangerSystem()
        {
            return new CitizenDangerCompositionSystemHelper();
        }

        private static CitizenHouseholdRegistrationCompositionSystemHelper ResolveCitizenHouseholdRegistrationSystem()
        {
            return new CitizenHouseholdRegistrationCompositionSystemHelper();
        }

        private static CitizenRefugeeCompositionSystemHelper ResolveCitizenRefugeeSystem()
        {
            return new CitizenRefugeeCompositionSystemHelper();
        }

        private static CitizenPopulationDebugDiagnosticsSystemHelper ResolveCitizenPopulationDebugSystem()
        {
            return new CitizenPopulationDebugDiagnosticsSystemHelper();
        }

        private static CitizenPopulationEventCompositionSystemHelper ResolveCitizenPopulationEventCompositionSystemHelper()
        {
            return new CitizenPopulationEventCompositionSystemHelper();
        }
    }
}
