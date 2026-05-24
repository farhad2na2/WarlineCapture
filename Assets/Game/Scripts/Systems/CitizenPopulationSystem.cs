using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class CitizenPopulationSystem
{
    private static readonly bool EnableCitizenPopulationDiagnostics = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private const float VisibleCitizenSpawnDistance = 140f;
    private const float VisibleCitizenDespawnDistance = 170f;
    private const float VisibleCitizenArriveDistance = 0.35f;
    private const float DangerDetectRadius = 35f;
    private const float DangerScanIntervalSeconds = 1f;
    private const float LogicalCitizenUpdateIntervalSeconds = 0.2f;
    private const float WeekdayWorkStartHour = 8f;
    private const float WeekdayWorkEndHour = 17f;
    private const float WeekdayShoppingStartHour = 11f;
    private const float WeekdayShoppingEndHour = 13f;
    private const float WeekdayLunchStartHour = 12f;
    private const float WeekdayLunchEndHour = 13f;
    private const float WeekdayEveningWalkStartHour = 17.5f;
    private const float WeekdayEveningWalkEndHour = 18.5f;
    private const float WeekendShoppingStartHour = 10f;
    private const float WeekendShoppingEndHour = 13f;
    private const float WeekendCityHallStartHour = 13f;
    private const float WeekendCityHallEndHour = 15f;
    private const float RefugeeMorningWalkStartHour = 8.5f;
    private const float RefugeeMorningWalkEndHour = 11.5f;
    private const float RefugeeLunchShelterStartHour = 11.5f;
    private const float RefugeeLunchShelterEndHour = 13.5f;
    private const float RefugeeEveningWalkStartHour = 16f;
    private const float RefugeeEveningWalkEndHour = 18.5f;
    private const float MaxVisibleTravelSegmentDistance = 48f;
    private const float DeferredTravelCellsPerSecond = 10f;

    public enum CitizenGender : byte
    {
        Male = 0,
        Female = 1
    }

    public enum CitizenLifeState : byte
    {
        Alive = 0,
        Dead = 1
    }

    public enum CitizenStatus : byte
    {
        AtHome = 0,
        GoingToWork = 1,
        AtWork = 2,
        GoingToShop = 3,
        AtShop = 4,
        GoingToCityHall = 5,
        GoingForWalk = 6,
        ReturningHome = 7,
        Fleeing = 8,
        RefugeeSeekingShelter = 9,
        AtRefugeeTent = 10,
        RelocatingToNewHouse = 11,
        LeavingWorld = 12,
        Dead = 13
    }

    public readonly struct CitizenPopulationTotals
    {
        public readonly int Households;
        public readonly int TotalCitizens;
        public readonly int HousedCitizens;
        public readonly int RefugeeCitizens;
        public readonly int DeadCitizens;

        public CitizenPopulationTotals(int households, int totalCitizens, int housedCitizens, int refugeeCitizens, int deadCitizens)
        {
            Households = households;
            TotalCitizens = totalCitizens;
            HousedCitizens = housedCitizens;
            RefugeeCitizens = refugeeCitizens;
            DeadCitizens = deadCitizens;
        }
    }

    private struct CitizenRecord
    {
        public int CitizenId;
        public Entity CitizenEntity;
        public int HouseholdId;
        public int HomeBuildingId;
        public int WorkBuildingId;
        public int PreferredShopBuildingId;
        public int LunchShopBuildingId;
        public int PreferredWalkBuildingId;
        public int PreferredCityHallBuildingId;
        public int CurrentTargetBuildingId;
        public CitizenGender Gender;
        public CitizenLifeState LifeState;
        public CitizenStatus Status;
        public float StateStartedAt;
        public float StateEndsAt;
    }

    private struct HouseholdRecord
    {
        public int HouseholdId;
        public Entity HouseholdEntity;
        public int HomeBuildingId;
        public int MaleCitizenId;
        public int FemaleCitizenId;
        public int RefugeeTentBuildingId;
        public byte IsDisplaced;
    }

    private sealed class VisibleCitizen
    {
        public int CitizenId;
        public Entity UnitEntity;
        public int2 GoalCell;
        public int TargetBuildingId;
    }

    private BuildingRuntimeQuerySystem _buildingRuntimeQuerySystem;
    private BuildingRuntimeQuerySystem.Context _buildingRuntimeQueryContext;
    private readonly CitizenResourceSystem _citizenResourceSystem = new();
    private CitizenResourceSystem.Context _citizenResourceContext;
    private readonly CitizenPrefabSystem _citizenPrefabSystem = new();
    private CitizenPrefabSystem.Context _citizenPrefabContext;
    private DayNightSystem _dayNightSystem;
    private Camera _worldCamera;
    private World _ecsWorld;
    private EntityManager _entityManager;
    private Entity _populationSummaryEntity;
    private EntityQuery _citizenEntityQuery;
    private EntityQuery _householdEntityQuery;
    private EntityQuery _gridConfigQuery;
    private readonly Dictionary<int, int> _householdIdsByHomeBuildingId = new();
    private readonly Dictionary<int, HouseholdRecord> _householdsById = new();
    private readonly Dictionary<int, CitizenRecord> _citizensById = new();
    private readonly Dictionary<int, VisibleCitizen> _visibleCitizensById = new();
    private readonly List<int> _runtimeHouseBuildingIds = new();
    private readonly List<int> _runtimeShopBuildingIds = new();
    private readonly List<int> _runtimeCityHallBuildingIds = new();
    private readonly List<int> _runtimeRefugeeTentBuildingIds = new();
    private readonly List<int> _runtimeMilitaryCampBuildingIds = new();
    private readonly HashSet<int> _runtimeHouseBuildingIdSet = new();
    private readonly List<Vector3> _dangerWorldPositions = new();
    private readonly List<int> _scratchCitizenIds = new();
    private readonly List<int> _scratchHouseholdIds = new();
    private readonly List<int> _scratchVisibleCitizenIds = new();
    private readonly List<int> _scratchRemovedBuildingIds = new();
    private int _nextHouseholdId = 1;
    private int _nextCitizenId = 1;
    private CitizenPopulationTotals _totals;
    private float _nextDangerScanAt;
    private float _nextLogicalCitizenUpdateAt;
    private int _lastRefugeeUpkeepChargedDay;
    private GameObject[] _maleCitizenPrefabs;
    private GameObject[] _femaleCitizenPrefabs;

    public CitizenPopulationTotals Totals => _totals;

    internal void Init(
        BuildingRuntimeQuerySystem buildingRuntimeQuerySystem,
        BuildingRuntimeQuerySystem.Context buildingRuntimeQueryContext,
        DayNightSystem dayNightSystem,
        Camera worldCamera,
        CitizenResourceSystem.Context citizenResourceContext = default,
        CitizenPrefabSystem.Context citizenPrefabContext = default)
    {
        _buildingRuntimeQuerySystem = buildingRuntimeQuerySystem;
        _buildingRuntimeQueryContext = buildingRuntimeQueryContext;
        _citizenResourceContext = citizenResourceContext;
        _citizenPrefabContext = citizenPrefabContext;
        _dayNightSystem = dayNightSystem;
        _worldCamera = worldCamera;
        ResolveEntityManager();
        _householdIdsByHomeBuildingId.Clear();
        _householdsById.Clear();
        _citizensById.Clear();
        ClearVisibleCitizens();
        _runtimeHouseBuildingIds.Clear();
        _runtimeShopBuildingIds.Clear();
        _runtimeCityHallBuildingIds.Clear();
        _runtimeHouseBuildingIdSet.Clear();
        _nextHouseholdId = 1;
        _nextCitizenId = 1;
        _totals = default;
        _nextLogicalCitizenUpdateAt = 0f;
        _lastRefugeeUpkeepChargedDay = 0;
        _maleCitizenPrefabs = LoadCitizenPrefabs(CitizenGender.Male, _citizenPrefabSystem, _citizenPrefabContext);
        _femaleCitizenPrefabs = LoadCitizenPrefabs(CitizenGender.Female, _citizenPrefabSystem, _citizenPrefabContext);
        EnsurePopulationSummaryEntity();
    }

    public void Update()
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        double afterBuildings = startTime;
        double afterResolve = startTime;
        double afterDanger = startTime;
        double afterLogical = startTime;
        double afterVisible = startTime;
        double afterTotals = startTime;
        bool skippedForPathfinding = false;
        try
        {
        if (!HasRuntimeBuildingQuery())
            return;

        RefreshRuntimeBuildingLists();
        afterBuildings = Time.realtimeSinceStartupAsDouble;

        if (UnitPathfindingSystem.HasPendingPathJob)
        {
            skippedForPathfinding = true;
            RecalculateTotalsFromRecords(syncSummaryEntity: false);
            afterTotals = Time.realtimeSinceStartupAsDouble;
            return;
        }

        ResolveEntityManager();
        EnsurePopulationSummaryEntity();
        afterResolve = Time.realtimeSinceStartupAsDouble;

        RefreshDangerSourcesIfNeeded();
        afterDanger = Time.realtimeSinceStartupAsDouble;
        if (Time.time >= _nextLogicalCitizenUpdateAt)
        {
            _nextLogicalCitizenUpdateAt = Time.time + LogicalCitizenUpdateIntervalSeconds;
            SyncRemovedHouses();
            RegisterNewHouses();
            UpdateRefugeeTentState();
            UpdateDeferredCitizenTravel();
            UpdateCitizenSchedules();
            UpdateRefugeeUpkeep();
        }
        afterLogical = Time.realtimeSinceStartupAsDouble;
        SyncVisibleCitizens();
        afterVisible = Time.realtimeSinceStartupAsDouble;
        RecalculateTotals();
        afterTotals = Time.realtimeSinceStartupAsDouble;
        }
        finally
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (EnableCitizenPopulationDiagnostics && elapsed >= FreezeLogThresholdSeconds)
            {
                if (afterBuildings < startTime) afterBuildings = startTime;
                if (afterResolve < afterBuildings) afterResolve = afterBuildings;
                if (afterDanger < afterResolve) afterDanger = afterResolve;
                if (afterLogical < afterDanger) afterLogical = afterDanger;
                if (afterVisible < afterLogical) afterVisible = afterLogical;
                if (afterTotals < afterVisible) afterTotals = afterVisible;

                Debug.Log(
                    $"[CitizenPopulationDiag] frame={Time.frameCount} total={elapsed * 1000d:F1}ms " +
                    $"buildings={(afterBuildings - startTime) * 1000d:F1}ms " +
                    $"resolve={(afterResolve - afterBuildings) * 1000d:F1}ms " +
                    $"danger={(afterDanger - afterResolve) * 1000d:F1}ms " +
                    $"logical={(afterLogical - afterDanger) * 1000d:F1}ms " +
                    $"visible={(afterVisible - afterLogical) * 1000d:F1}ms " +
                    $"totals={(afterTotals - afterVisible) * 1000d:F1}ms " +
                    $"citizens={_citizensById.Count} visible={_visibleCitizensById.Count} skippedPath={skippedForPathfinding}");
            }
        }
    }

    private void RefreshRuntimeBuildingLists()
    {
        _runtimeHouseBuildingIds.Clear();
        _buildingRuntimeQuerySystem.GetRuntimeHouseBuildingIds(_buildingRuntimeQueryContext, _runtimeHouseBuildingIds);
        _runtimeShopBuildingIds.Clear();
        _buildingRuntimeQuerySystem.GetRuntimeBuildingIdsByRole(_buildingRuntimeQueryContext, BuildingRole.Shop, _runtimeShopBuildingIds);
        _runtimeCityHallBuildingIds.Clear();
        _buildingRuntimeQuerySystem.GetRuntimeBuildingIdsByRole(_buildingRuntimeQueryContext, BuildingRole.CityHall, _runtimeCityHallBuildingIds);
        _runtimeRefugeeTentBuildingIds.Clear();
        _buildingRuntimeQuerySystem.GetRuntimeBuildingIdsByRole(_buildingRuntimeQueryContext, BuildingRole.TentRefugee, _runtimeRefugeeTentBuildingIds);
        _runtimeMilitaryCampBuildingIds.Clear();
        _buildingRuntimeQuerySystem.GetRuntimeBuildingIdsByRole(_buildingRuntimeQueryContext, BuildingRole.MilitaryCamp, _runtimeMilitaryCampBuildingIds);
        _runtimeHouseBuildingIdSet.Clear();
        for (int i = 0; i < _runtimeHouseBuildingIds.Count; i++)
            _runtimeHouseBuildingIdSet.Add(_runtimeHouseBuildingIds[i]);
    }

    private bool HasRuntimeBuildingQuery()
    {
        return _buildingRuntimeQuerySystem != null;
    }

    private bool HasCitizenResourceAccess()
    {
        return _citizenResourceSystem.IsConfigured(_citizenResourceContext);
    }

    private bool TryGetRuntimeBuildingFocusWorldPosition(int buildingId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.TryGetRuntimeBuildingFocusWorldPosition(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   out worldPosition);
    }

    private bool TryGetRuntimeBuildingDestroyedState(int buildingId, out bool isDestroyed)
    {
        isDestroyed = false;
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.TryGetRuntimeBuildingDestroyedState(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   out isDestroyed);
    }

    private bool TryGetRuntimeBuildingRefugeeSettings(int buildingId, out int refugeeCapacity, out int upkeepPerCitizenPerDay)
    {
        refugeeCapacity = 0;
        upkeepPerCitizenPerDay = 0;
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.TryGetRuntimeBuildingRefugeeSettings(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   out refugeeCapacity,
                   out upkeepPerCitizenPerDay);
    }

    private bool TryGetRuntimeBuildingApproachCell(int buildingId, int2 unitFootprint, int2 referenceCell, out int2 goal)
    {
        goal = default;
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.TryGetRuntimeBuildingApproachCell(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   unitFootprint,
                   referenceCell,
                   out goal);
    }

    private bool IsRuntimeBuildingApproachCell(int buildingId, int2 currentCell, int2 unitFootprint)
    {
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.IsRuntimeBuildingApproachCell(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   currentCell,
                   unitFootprint);
    }

    public void GetTotals(out int households, out int totalCitizens, out int housedCitizens, out int refugeeCitizens, out int deadCitizens)
    {
        if (TryGetTotalsFromEcs(out CitizenPopulationSummary summary))
        {
            households = summary.Households;
            totalCitizens = summary.TotalCitizens;
            housedCitizens = summary.HousedCitizens;
            refugeeCitizens = summary.RefugeeCitizens;
            deadCitizens = summary.DeadCitizens;
            return;
        }

        households = _totals.Households;
        totalCitizens = _totals.TotalCitizens;
        housedCitizens = _totals.HousedCitizens;
        refugeeCitizens = _totals.RefugeeCitizens;
        deadCitizens = _totals.DeadCitizens;
    }

    private void ResolveEntityManager()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;
        if (_ecsWorld == world && _entityManager.World.IsCreated)
            return;

        _ecsWorld = world;
        _entityManager = world.EntityManager;
        _citizenEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CitizenTag>(), ComponentType.ReadOnly<CitizenIdentity>());
        _householdEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CitizenHouseholdTag>(), ComponentType.ReadOnly<CitizenHouseholdData>());
        _gridConfigQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
    }

    private void EnsurePopulationSummaryEntity()
    {
        if (_ecsWorld == null || !_ecsWorld.IsCreated)
            return;
        if (_populationSummaryEntity != Entity.Null && _entityManager.Exists(_populationSummaryEntity))
            return;

        EntityQuery query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CitizenPopulationSummaryTag>(), ComponentType.ReadWrite<CitizenPopulationSummary>());
        if (!query.IsEmptyIgnoreFilter)
        {
            _populationSummaryEntity = query.GetSingletonEntity();
            query.Dispose();
            return;
        }

        query.Dispose();
        _populationSummaryEntity = _entityManager.CreateEntity();
        _entityManager.AddComponentData(_populationSummaryEntity, new CitizenPopulationSummaryTag());
        _entityManager.AddComponentData(_populationSummaryEntity, default(CitizenPopulationSummary));
    }

    private void DestroyAllCitizenEntities()
    {
        if (_ecsWorld == null || !_ecsWorld.IsCreated)
            return;

        foreach (CitizenRecord citizen in _citizensById.Values)
        {
            if (citizen.CitizenEntity != Entity.Null && _entityManager.Exists(citizen.CitizenEntity))
                _entityManager.DestroyEntity(citizen.CitizenEntity);
        }

        foreach (HouseholdRecord household in _householdsById.Values)
        {
            if (household.HouseholdEntity != Entity.Null && _entityManager.Exists(household.HouseholdEntity))
                _entityManager.DestroyEntity(household.HouseholdEntity);
        }

        if (_populationSummaryEntity != Entity.Null && _entityManager.Exists(_populationSummaryEntity))
            _entityManager.DestroyEntity(_populationSummaryEntity);
    }

    private bool TryGetTotalsFromEcs(out CitizenPopulationSummary summary)
    {
        summary = default;
        if (_ecsWorld == null || !_ecsWorld.IsCreated)
            return false;
        if (_populationSummaryEntity == Entity.Null || !_entityManager.Exists(_populationSummaryEntity))
            return false;
        if (!_entityManager.HasComponent<CitizenPopulationSummary>(_populationSummaryEntity))
            return false;

        summary = _entityManager.GetComponentData<CitizenPopulationSummary>(_populationSummaryEntity);
        return true;
    }

    private HouseholdRecord StoreHousehold(HouseholdRecord household)
    {
        EnsureHouseholdEntity(ref household);
        _householdsById[household.HouseholdId] = household;
        if (household.HomeBuildingId != 0)
        _householdIdsByHomeBuildingId[household.HomeBuildingId] = household.HouseholdId;
        SyncHouseholdEntity(household);
        return household;
    }

    private CitizenRecord StoreCitizen(CitizenRecord citizen)
    {
        EnsureCitizenEntity(ref citizen);
        _citizensById[citizen.CitizenId] = citizen;
        SyncCitizenEntity(citizen);
        return citizen;
    }

    private void EnsureHouseholdEntity(ref HouseholdRecord household)
    {
        if (_ecsWorld == null || !_ecsWorld.IsCreated)
            return;
        if (household.HouseholdEntity != Entity.Null && _entityManager.Exists(household.HouseholdEntity))
            return;

        household.HouseholdEntity = _entityManager.CreateEntity();
        _entityManager.AddComponentData(household.HouseholdEntity, new CitizenHouseholdTag());
        _entityManager.AddComponentData(household.HouseholdEntity, default(CitizenHouseholdData));
    }

    private void EnsureCitizenEntity(ref CitizenRecord citizen)
    {
        if (_ecsWorld == null || !_ecsWorld.IsCreated)
            return;
        if (citizen.CitizenEntity != Entity.Null && _entityManager.Exists(citizen.CitizenEntity))
            return;

        citizen.CitizenEntity = _entityManager.CreateEntity();
        _entityManager.AddComponentData(citizen.CitizenEntity, new CitizenTag());
        _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenIdentity));
        _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenHouseholdRef));
        _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenHomeTarget));
        _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenAssignmentsData));
        _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenTimersData));
    }

    private void SyncHouseholdEntity(HouseholdRecord household)
    {
        if (_ecsWorld == null || !_ecsWorld.IsCreated)
            return;
        if (household.HouseholdEntity == Entity.Null || !_entityManager.Exists(household.HouseholdEntity))
            return;

        _entityManager.SetComponentData(household.HouseholdEntity, new CitizenHouseholdData
        {
            HouseholdId = household.HouseholdId,
            HomeBuildingId = household.HomeBuildingId,
            MaleCitizenId = household.MaleCitizenId,
            FemaleCitizenId = household.FemaleCitizenId,
            RefugeeTentBuildingId = household.RefugeeTentBuildingId,
            IsDisplaced = household.IsDisplaced
        });
    }

    private void SyncCitizenEntity(CitizenRecord citizen)
    {
        if (_ecsWorld == null || !_ecsWorld.IsCreated)
            return;
        if (citizen.CitizenEntity == Entity.Null || !_entityManager.Exists(citizen.CitizenEntity))
            return;

        Entity householdEntity = Entity.Null;
        if (_householdsById.TryGetValue(citizen.HouseholdId, out HouseholdRecord household))
            householdEntity = household.HouseholdEntity;

        _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenIdentity
        {
            CitizenId = citizen.CitizenId,
            Gender = (byte)citizen.Gender,
            LifeState = (byte)citizen.LifeState,
            Status = (byte)citizen.Status
        });
        _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenHouseholdRef
        {
            HouseholdId = citizen.HouseholdId,
            HouseholdEntity = householdEntity
        });
        _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenHomeTarget
        {
            HomeBuildingId = citizen.HomeBuildingId,
            CurrentTargetBuildingId = citizen.CurrentTargetBuildingId
        });
        _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenAssignmentsData
        {
            WorkBuildingId = citizen.WorkBuildingId,
            PreferredShopBuildingId = citizen.PreferredShopBuildingId,
            LunchShopBuildingId = citizen.LunchShopBuildingId,
            PreferredWalkBuildingId = citizen.PreferredWalkBuildingId,
            PreferredCityHallBuildingId = citizen.PreferredCityHallBuildingId
        });
        _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenTimersData
        {
            StateStartedAt = citizen.StateStartedAt,
            StateEndsAt = citizen.StateEndsAt
        });
    }

    private bool TryGetCitizenRecord(int citizenId, out CitizenRecord citizen)
    {
        if (!_citizensById.TryGetValue(citizenId, out citizen))
            return false;

        return true;
    }

    private bool TryGetHouseholdRecord(int householdId, out HouseholdRecord household)
    {
        if (!_householdsById.TryGetValue(householdId, out household))
            return false;

        return true;
    }

    public void Dispose()
    {
        DestroyAllCitizenEntities();
        _householdIdsByHomeBuildingId.Clear();
        _householdsById.Clear();
        _citizensById.Clear();
        ClearVisibleCitizens();
        _runtimeHouseBuildingIds.Clear();
        _runtimeShopBuildingIds.Clear();
        _runtimeCityHallBuildingIds.Clear();
        _runtimeRefugeeTentBuildingIds.Clear();
        _runtimeMilitaryCampBuildingIds.Clear();
        _runtimeHouseBuildingIdSet.Clear();
        _dangerWorldPositions.Clear();
        _scratchCitizenIds.Clear();
        _scratchHouseholdIds.Clear();
        _scratchVisibleCitizenIds.Clear();
        _scratchRemovedBuildingIds.Clear();
        _nextLogicalCitizenUpdateAt = 0f;
        _lastRefugeeUpkeepChargedDay = 0;
        _populationSummaryEntity = Entity.Null;
        _citizenEntityQuery = default;
        _householdEntityQuery = default;
        _gridConfigQuery = default;
        _entityManager = default;
        _ecsWorld = null;
        _dayNightSystem = null;
        _worldCamera = null;
    }

    public bool TryGetCitizenDebugSnapshot(int citizenId, out string snapshot)
    {
        snapshot = string.Empty;
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return false;

        if (_ecsWorld != null &&
            _ecsWorld.IsCreated &&
            citizen.CitizenEntity != Entity.Null &&
            _entityManager.Exists(citizen.CitizenEntity) &&
            _entityManager.HasComponent<CitizenIdentity>(citizen.CitizenEntity) &&
            _entityManager.HasComponent<CitizenHouseholdRef>(citizen.CitizenEntity) &&
            _entityManager.HasComponent<CitizenHomeTarget>(citizen.CitizenEntity) &&
            _entityManager.HasComponent<CitizenAssignmentsData>(citizen.CitizenEntity) &&
            _entityManager.HasComponent<CitizenTimersData>(citizen.CitizenEntity))
        {
            CitizenIdentity identity = _entityManager.GetComponentData<CitizenIdentity>(citizen.CitizenEntity);
            CitizenHouseholdRef householdRef = _entityManager.GetComponentData<CitizenHouseholdRef>(citizen.CitizenEntity);
            CitizenHomeTarget homeTarget = _entityManager.GetComponentData<CitizenHomeTarget>(citizen.CitizenEntity);
            CitizenAssignmentsData assignments = _entityManager.GetComponentData<CitizenAssignmentsData>(citizen.CitizenEntity);
            CitizenTimersData timers = _entityManager.GetComponentData<CitizenTimersData>(citizen.CitizenEntity);

            snapshot =
                $"citizen={identity.CitizenId} household={householdRef.HouseholdId} gender={(CitizenGender)identity.Gender} " +
                $"life={(CitizenLifeState)identity.LifeState} status={(CitizenStatus)identity.Status} home={homeTarget.HomeBuildingId} " +
                $"work={assignments.WorkBuildingId} shop={assignments.PreferredShopBuildingId} lunchShop={assignments.LunchShopBuildingId} walk={assignments.PreferredWalkBuildingId} cityHall={assignments.PreferredCityHallBuildingId} " +
                $"target={homeTarget.CurrentTargetBuildingId} " +
                $"stateStartedAt={timers.StateStartedAt:0.00} stateEndsAt={timers.StateEndsAt:0.00} ecs=1";
            return true;
        }

        snapshot =
            $"citizen={citizen.CitizenId} household={citizen.HouseholdId} gender={citizen.Gender} " +
            $"life={citizen.LifeState} status={citizen.Status} home={citizen.HomeBuildingId} " +
            $"work={citizen.WorkBuildingId} shop={citizen.PreferredShopBuildingId} lunchShop={citizen.LunchShopBuildingId} walk={citizen.PreferredWalkBuildingId} cityHall={citizen.PreferredCityHallBuildingId} " +
            $"target={citizen.CurrentTargetBuildingId} " +
            $"stateStartedAt={citizen.StateStartedAt:0.00} stateEndsAt={citizen.StateEndsAt:0.00} ecs=0";
        return true;
    }

    public bool TrySetCitizenStatusForDebug(int citizenId, CitizenStatus status, int targetBuildingId = 0, float stateDurationSeconds = 0f)
    {
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return false;

        SetCitizenStatus(ref citizen, status, targetBuildingId, stateDurationSeconds);
        StoreCitizen(citizen);
        return true;
    }

    public bool TryKillCitizenForDebug(int citizenId)
    {
        return MarkCitizenDead(citizenId, "debug");
    }

    public void NotifyVisibleCitizenDestroyed(int citizenId)
    {
        if (!_visibleCitizensById.ContainsKey(citizenId))
            return;

        MarkCitizenDead(citizenId, "visual-destroyed");
    }

    public void NotifyHomeBuildingDestroyed(int buildingId)
    {
        if (!TryFindHouseholdByHomeBuildingId(buildingId, out HouseholdRecord household))
            return;
        if (household.IsDisplaced != 0)
            return;

        DisplaceHousehold(household, "home-destroyed");
    }

    private bool TryFindHouseholdByHomeBuildingId(int buildingId, out HouseholdRecord household)
    {
        household = default;

        if (_householdIdsByHomeBuildingId.TryGetValue(buildingId, out int mappedHouseholdId) &&
            TryGetHouseholdRecord(mappedHouseholdId, out household))
        {
            return true;
        }

        PopulateHouseholdIdsFromEcs();
        for (int i = 0; i < _scratchHouseholdIds.Count; i++)
        {
            if (!TryGetHouseholdRecord(_scratchHouseholdIds[i], out household))
                continue;
            if (household.HomeBuildingId != buildingId)
                continue;

            _householdIdsByHomeBuildingId[buildingId] = household.HouseholdId;
            return true;
        }

        household = default;
        return false;
    }

    private void SyncRemovedHouses()
    {
        if (_householdIdsByHomeBuildingId.Count == 0)
            return;

        _scratchRemovedBuildingIds.Clear();
        foreach (KeyValuePair<int, int> pair in _householdIdsByHomeBuildingId)
        {
            if (_runtimeHouseBuildingIdSet.Contains(pair.Key))
                continue;

            _scratchRemovedBuildingIds.Add(pair.Key);
        }

        if (_scratchRemovedBuildingIds.Count == 0)
            return;

        for (int i = 0; i < _scratchRemovedBuildingIds.Count; i++)
        {
            int buildingId = _scratchRemovedBuildingIds[i];
            if (!_householdIdsByHomeBuildingId.TryGetValue(buildingId, out int householdId) ||
                !TryGetHouseholdRecord(householdId, out HouseholdRecord household))
                continue;

            if (household.IsDisplaced == 0)
                DisplaceHousehold(household, "home-missing");

            _householdIdsByHomeBuildingId.Remove(buildingId);
        }
    }

    private void RegisterNewHouses()
    {
        for (int i = 0; i < _runtimeHouseBuildingIds.Count; i++)
        {
            int homeBuildingId = _runtimeHouseBuildingIds[i];
            if (_householdIdsByHomeBuildingId.ContainsKey(homeBuildingId))
                continue;

            if (TryRehouseDisplacedHousehold(homeBuildingId))
                continue;

            int householdId = _nextHouseholdId++;
            int maleCitizenId = _nextCitizenId++;
            int femaleCitizenId = _nextCitizenId++;

            HouseholdRecord household = new HouseholdRecord
            {
                HouseholdId = householdId,
                HouseholdEntity = Entity.Null,
                HomeBuildingId = homeBuildingId,
                MaleCitizenId = maleCitizenId,
                FemaleCitizenId = femaleCitizenId,
                RefugeeTentBuildingId = 0,
                IsDisplaced = 0
            };

            household = StoreHousehold(household);
            int assignedWorkBuildingId = FindNearestBuilding(homeBuildingId, _runtimeShopBuildingIds);
            int assignedLunchShopBuildingId = FindNearestBuilding(homeBuildingId, _runtimeShopBuildingIds, assignedWorkBuildingId);
            if (assignedLunchShopBuildingId == 0)
                assignedLunchShopBuildingId = assignedWorkBuildingId;
            int assignedCityHallBuildingId = FindNearestBuilding(homeBuildingId, _runtimeCityHallBuildingIds);
            int assignedWalkBuildingId = assignedCityHallBuildingId != 0 ? assignedCityHallBuildingId : assignedWorkBuildingId;
            StoreCitizen(new CitizenRecord
            {
                CitizenId = maleCitizenId,
                CitizenEntity = Entity.Null,
                HouseholdId = householdId,
                HomeBuildingId = homeBuildingId,
                WorkBuildingId = assignedWorkBuildingId,
                PreferredShopBuildingId = assignedWorkBuildingId,
                LunchShopBuildingId = assignedLunchShopBuildingId,
                PreferredWalkBuildingId = assignedWalkBuildingId,
                PreferredCityHallBuildingId = assignedCityHallBuildingId,
                CurrentTargetBuildingId = homeBuildingId,
                Gender = CitizenGender.Male,
                LifeState = CitizenLifeState.Alive,
                Status = CitizenStatus.AtHome,
                StateStartedAt = Time.time,
                StateEndsAt = 0f
            });
            StoreCitizen(new CitizenRecord
            {
                CitizenId = femaleCitizenId,
                CitizenEntity = Entity.Null,
                HouseholdId = householdId,
                HomeBuildingId = homeBuildingId,
                WorkBuildingId = 0,
                PreferredShopBuildingId = assignedWorkBuildingId,
                LunchShopBuildingId = 0,
                PreferredWalkBuildingId = assignedWalkBuildingId,
                PreferredCityHallBuildingId = assignedCityHallBuildingId,
                CurrentTargetBuildingId = homeBuildingId,
                Gender = CitizenGender.Female,
                LifeState = CitizenLifeState.Alive,
                Status = CitizenStatus.AtHome,
                StateStartedAt = Time.time,
                StateEndsAt = 0f
            });

        }
    }

    private bool TryRehouseDisplacedHousehold(int newHomeBuildingId)
    {
        int householdId = FindDisplacedHouseholdForRehousing();
        if (householdId == 0)
            return false;
        if (!TryGetHouseholdRecord(householdId, out HouseholdRecord household))
            return false;

        int assignedWorkBuildingId = FindNearestBuilding(newHomeBuildingId, _runtimeShopBuildingIds);
        int assignedLunchShopBuildingId = FindNearestBuilding(newHomeBuildingId, _runtimeShopBuildingIds, assignedWorkBuildingId);
        if (assignedLunchShopBuildingId == 0)
            assignedLunchShopBuildingId = assignedWorkBuildingId;
        int assignedCityHallBuildingId = FindNearestBuilding(newHomeBuildingId, _runtimeCityHallBuildingIds);
        int assignedWalkBuildingId = assignedCityHallBuildingId != 0 ? assignedCityHallBuildingId : assignedWorkBuildingId;

        household.HomeBuildingId = newHomeBuildingId;
        household.IsDisplaced = 0;
        household.RefugeeTentBuildingId = 0;
        household = StoreHousehold(household);

        RehouseCitizen(household.MaleCitizenId, newHomeBuildingId, assignedWorkBuildingId, assignedWorkBuildingId, assignedLunchShopBuildingId, assignedWalkBuildingId, assignedCityHallBuildingId);
        RehouseCitizen(household.FemaleCitizenId, newHomeBuildingId, 0, assignedWorkBuildingId, 0, assignedWalkBuildingId, assignedCityHallBuildingId);

        return true;
    }

    private int FindDisplacedHouseholdForRehousing()
    {
        PopulateHouseholdIdsFromEcs();
        for (int i = 0; i < _scratchHouseholdIds.Count; i++)
        {
            if (!TryGetHouseholdRecord(_scratchHouseholdIds[i], out HouseholdRecord household))
                continue;
            if (household.IsDisplaced == 0)
                continue;
            if (!HasLivingCitizen(household.MaleCitizenId) && !HasLivingCitizen(household.FemaleCitizenId))
                continue;
            if (!IsCitizenAwaitingRehousing(household.MaleCitizenId) && !IsCitizenAwaitingRehousing(household.FemaleCitizenId))
                continue;

            return household.HouseholdId;
        }

        return 0;
    }

    private void RehouseCitizen(
        int citizenId,
        int newHomeBuildingId,
        int workBuildingId,
        int preferredShopBuildingId,
        int lunchShopBuildingId,
        int preferredWalkBuildingId,
        int preferredCityHallBuildingId)
    {
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return;

        citizen.HomeBuildingId = newHomeBuildingId;
        citizen.WorkBuildingId = workBuildingId;
        citizen.PreferredShopBuildingId = preferredShopBuildingId;
        citizen.LunchShopBuildingId = lunchShopBuildingId;
        citizen.PreferredWalkBuildingId = preferredWalkBuildingId;
        citizen.PreferredCityHallBuildingId = preferredCityHallBuildingId;
        SetCitizenStatus(ref citizen, CitizenStatus.RelocatingToNewHouse, newHomeBuildingId, EstimateTravelSeconds(citizen, newHomeBuildingId));
        StoreCitizen(citizen);
    }

    private void RecalculateTotals()
    {
        RecalculateTotalsFromRecords(syncSummaryEntity: true);
    }

    private void RecalculateTotalsFromRecords(bool syncSummaryEntity)
    {
        int aliveCitizens = 0;
        int deadCitizens = 0;
        int housedCitizens = 0;
        int refugeeCitizens = 0;
        foreach (CitizenRecord citizen in _citizensById.Values)
        {
            if (citizen.LifeState == CitizenLifeState.Dead)
            {
                deadCitizens++;
                continue;
            }

            aliveCitizens++;
            if (citizen.Status == CitizenStatus.RefugeeSeekingShelter || citizen.Status == CitizenStatus.AtRefugeeTent)
                refugeeCitizens++;
            else
                housedCitizens++;
        }

        _totals = new CitizenPopulationTotals(
            GetHouseholdCount(),
            aliveCitizens,
            housedCitizens,
            refugeeCitizens,
            deadCitizens);

        if (!syncSummaryEntity)
            return;

        EnsurePopulationSummaryEntity();
        if (_ecsWorld != null &&
            _ecsWorld.IsCreated &&
            _populationSummaryEntity != Entity.Null &&
            _entityManager.Exists(_populationSummaryEntity))
        {
            _entityManager.SetComponentData(_populationSummaryEntity, new CitizenPopulationSummary
            {
                Households = _totals.Households,
                TotalCitizens = _totals.TotalCitizens,
                HousedCitizens = _totals.HousedCitizens,
                RefugeeCitizens = _totals.RefugeeCitizens,
                DeadCitizens = _totals.DeadCitizens
            });
        }
    }

    private void UpdateCitizenSchedules()
    {
        if (_dayNightSystem == null || !HasCitizenData())
            return;

        UpdateHouseholdDisplacementState();

        PopulateCitizenIdsFromEcs();

        for (int i = 0; i < _scratchCitizenIds.Count; i++)
        {
            int citizenId = _scratchCitizenIds[i];
            if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
                continue;

            if (citizen.LifeState == CitizenLifeState.Dead)
                continue;

            if (citizen.Status == CitizenStatus.RefugeeSeekingShelter ||
                citizen.Status == CitizenStatus.RelocatingToNewHouse ||
                citizen.Status == CitizenStatus.LeavingWorld)
            {
                continue;
            }

            if (TryGetDangerFleeTarget(citizen, out int fleeTargetBuildingId))
            {
                if (citizen.Status != CitizenStatus.Fleeing || citizen.CurrentTargetBuildingId != fleeTargetBuildingId)
                {
                    SetCitizenStatus(ref citizen, CitizenStatus.Fleeing, fleeTargetBuildingId, 0f);
                    StoreCitizen(citizen);
                }
                continue;
            }

            if (citizen.Status == CitizenStatus.Fleeing)
                continue;

            CitizenStatus desiredStatus = GetScheduledStatus(citizen);
            int desiredTargetBuildingId = GetScheduledTargetBuildingId(citizen, desiredStatus);
            CitizenStatus nextStatus = ShouldUseTravelStatus(citizen, desiredStatus, desiredTargetBuildingId)
                ? GetTravelStatusForDesiredStatus(desiredStatus)
                : desiredStatus;

            if (citizen.Status == nextStatus && citizen.CurrentTargetBuildingId == desiredTargetBuildingId)
                continue;

            float stateDurationSeconds = IsTravelStatus(nextStatus)
                ? EstimateTravelSeconds(citizen, desiredTargetBuildingId)
                : 0f;
            SetCitizenStatus(ref citizen, nextStatus, desiredTargetBuildingId, stateDurationSeconds);
            StoreCitizen(citizen);
        }
    }

    private void UpdateRefugeeTentState()
    {
        if (!HasHouseholdData() || !HasRuntimeBuildingQuery())
            return;

        PopulateHouseholdIdsFromEcs();

        for (int i = 0; i < _scratchHouseholdIds.Count; i++)
        {
            if (!TryGetHouseholdRecord(_scratchHouseholdIds[i], out HouseholdRecord household))
                continue;
            if (household.RefugeeTentBuildingId == 0)
                continue;

            bool tentExists = TryGetRuntimeBuildingDestroyedState(household.RefugeeTentBuildingId, out bool isDestroyed);
            if (tentExists && !isDestroyed)
                continue;

            int previousTentBuildingId = household.RefugeeTentBuildingId;
            ReleaseHouseholdRefugeeAssignment(household.HouseholdId);
            if (!TryGetHouseholdRecord(household.HouseholdId, out household))
                continue;

            int replacementTentBuildingId = FindNearestAvailableRefugeeTent(household);
            if (replacementTentBuildingId != 0)
            {
                household.RefugeeTentBuildingId = replacementTentBuildingId;
                household = StoreHousehold(household);

                MoveCitizenToRefugeeState(household.MaleCitizenId, replacementTentBuildingId, "refugee-tent-lost");
                MoveCitizenToRefugeeState(household.FemaleCitizenId, replacementTentBuildingId, "refugee-tent-lost");
                continue;
            }

            MarkCitizenDead(household.MaleCitizenId, "refugee-tent-destroyed");
            MarkCitizenDead(household.FemaleCitizenId, "refugee-tent-destroyed");
        }
    }

    private void UpdateDeferredCitizenTravel()
    {
        PopulateCitizenIdsFromEcs();
        for (int i = 0; i < _scratchCitizenIds.Count; i++)
        {
            int citizenId = _scratchCitizenIds[i];
            if (_visibleCitizensById.ContainsKey(citizenId))
                continue;
            if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
                continue;
            if (citizen.LifeState == CitizenLifeState.Dead)
                continue;
            if (!IsTravelStatus(citizen.Status))
                continue;
            if (citizen.StateEndsAt <= 0f || Time.time < citizen.StateEndsAt)
                continue;

            ResolveCitizenArrival(citizenId);
        }
    }

    private void UpdateRefugeeUpkeep()
    {
        if (_dayNightSystem == null || !HasCitizenResourceAccess() || !HasRuntimeBuildingQuery())
            return;

        int currentDay = Mathf.Max(1, _dayNightSystem.DayCount);
        if (currentDay == _lastRefugeeUpkeepChargedDay)
            return;

        _lastRefugeeUpkeepChargedDay = currentDay;

        int refugeeCitizens = 0;
        int totalCost = 0;
        PopulateHouseholdIdsFromEcs();
        for (int i = 0; i < _scratchHouseholdIds.Count; i++)
        {
            if (!TryGetHouseholdRecord(_scratchHouseholdIds[i], out HouseholdRecord household))
                continue;
            if (household.RefugeeTentBuildingId == 0)
                continue;

            if (!TryGetRuntimeBuildingRefugeeSettings(household.RefugeeTentBuildingId, out _, out int upkeepPerCitizenPerDay))
                continue;

            int householdRefugees = CountLivingHouseholdRefugees(household);
            if (householdRefugees <= 0)
                continue;

            refugeeCitizens += householdRefugees;
            totalCost += householdRefugees * upkeepPerCitizenPerDay;
        }

        if (refugeeCitizens <= 0 || totalCost <= 0)
            return;

        if (_citizenResourceSystem.TrySpendDollars(_citizenResourceContext, totalCost))
            return;

        PopulateHouseholdIdsFromEcs();

        for (int i = 0; i < _scratchHouseholdIds.Count; i++)
        {
            if (!TryGetHouseholdRecord(_scratchHouseholdIds[i], out HouseholdRecord household))
                continue;

            int householdRefugees = CountLivingHouseholdRefugees(household);
            if (householdRefugees <= 0)
                continue;

            MarkCitizenDead(household.MaleCitizenId, "refugee-upkeep-unpaid");
            MarkCitizenDead(household.FemaleCitizenId, "refugee-upkeep-unpaid");
            ReleaseHouseholdRefugeeAssignment(household.HouseholdId);
        }

    }

    private void SyncVisibleCitizens()
    {
        if (_worldCamera == null || !HasCitizenData() || _ecsWorld == null || !_ecsWorld.IsCreated)
            return;

        _scratchVisibleCitizenIds.Clear();
        foreach (int citizenId in _visibleCitizensById.Keys)
            _scratchVisibleCitizenIds.Add(citizenId);

        for (int i = 0; i < _scratchVisibleCitizenIds.Count; i++)
        {
            int citizenId = _scratchVisibleCitizenIds[i];
            if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen) ||
                !ShouldCitizenBeVisible(citizen, VisibleCitizenDespawnDistance, out Vector3 worldPosition))
            {
                RemoveVisibleCitizen(citizenId);
                continue;
            }

            if (_visibleCitizensById.TryGetValue(citizenId, out VisibleCitizen visibleCitizen))
            {
                if (visibleCitizen.UnitEntity == Entity.Null || !_entityManager.Exists(visibleCitizen.UnitEntity))
                {
                    MarkCitizenDead(citizenId, "unit-destroyed");
                    continue;
                }

                if (!_entityManager.HasComponent<LocalTransform>(visibleCitizen.UnitEntity))
                {
                    RemoveVisibleCitizen(citizenId);
                    continue;
                }

                Vector3 currentPosition = _entityManager.GetComponentData<LocalTransform>(visibleCitizen.UnitEntity).Position;
                bool hasPathFollow = _entityManager.HasComponent<UnitPathFollow>(visibleCitizen.UnitEntity);
                bool hasPathRequest = _entityManager.HasComponent<UnitPathRequest>(visibleCitizen.UnitEntity);
                bool hasLongMove = _entityManager.HasComponent<UnitLongDistanceMove>(visibleCitizen.UnitEntity);
                int2 currentCell = _entityManager.HasComponent<UnitGrid>(visibleCitizen.UnitEntity)
                    ? _entityManager.GetComponentData<UnitGrid>(visibleCitizen.UnitEntity).Cell
                    : default;

                if (IsRuntimeBuildingApproachCell(citizen.CurrentTargetBuildingId, currentCell, new int2(1, 1)))
                {
                    ResolveCitizenArrival(citizenId);
                    continue;
                }

                if (TryGetCitizenBuildingApproachCell(citizen.CurrentTargetBuildingId, citizen, currentCell, out int2 finalApproachGoal))
                {
                    int dx = math.abs(currentCell.x - finalApproachGoal.x);
                    int dy = math.abs(currentCell.y - finalApproachGoal.y);
                    if (math.max(dx, dy) <= 2)
                    {
                        ResolveCitizenArrival(citizenId);
                        continue;
                    }
                }

                if (IsTravelStatus(citizen.Status) && !hasPathFollow && !hasPathRequest)
                {
                    if (hasLongMove)
                    {
                        int2 finalGoal = _entityManager.GetComponentData<UnitLongDistanceMove>(visibleCitizen.UnitEntity).FinalGoal;
                        IssueCitizenMoveCommand(visibleCitizen.UnitEntity, finalGoal);
                    }
                    else if (TryGetCitizenMoveGoal(citizen, currentPosition, out int2 retryGoal))
                    {
                        IssueCitizenMoveCommand(visibleCitizen.UnitEntity, retryGoal);
                        visibleCitizen.GoalCell = retryGoal;
                        visibleCitizen.TargetBuildingId = citizen.CurrentTargetBuildingId;
                        _visibleCitizensById[citizenId] = visibleCitizen;
                    }
                }

                bool segmentReached = currentCell.Equals(visibleCitizen.GoalCell);
                if ((visibleCitizen.TargetBuildingId != citizen.CurrentTargetBuildingId || segmentReached) &&
                    TryGetCitizenMoveGoal(citizen, currentPosition, out int2 goalCell) &&
                    !currentCell.Equals(goalCell))
                {
                    IssueCitizenMoveCommand(visibleCitizen.UnitEntity, goalCell);
                    visibleCitizen.GoalCell = goalCell;
                    visibleCitizen.TargetBuildingId = citizen.CurrentTargetBuildingId;
                    _visibleCitizensById[citizenId] = visibleCitizen;
                }

                if (currentCell.Equals(visibleCitizen.GoalCell))
                {
                    if (!IsRuntimeBuildingApproachCell(citizen.CurrentTargetBuildingId, currentCell, new int2(1, 1)))
                    {
                        if (!TryGetRuntimeBuildingFocusWorldPosition(citizen.CurrentTargetBuildingId, out Vector3 finalTargetPosition))
                        {
                            ResolveCitizenArrival(citizenId);
                        }
                        else
                        {
                            Vector3 finalWorld = ResolveCitizenWorldPosition(citizen, finalTargetPosition);
                            if ((finalWorld - currentPosition).sqrMagnitude <= VisibleCitizenArriveDistance * VisibleCitizenArriveDistance)
                                ResolveCitizenArrival(citizenId);
                        }
                    }
                }
            }
        }

        PopulateCitizenIdsFromEcs();
        for (int i = 0; i < _scratchCitizenIds.Count; i++)
        {
            int citizenId = _scratchCitizenIds[i];
            if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
                continue;
            if (_visibleCitizensById.ContainsKey(citizenId))
                continue;
            if (!ShouldCitizenBeVisible(citizen, VisibleCitizenSpawnDistance, out Vector3 worldPosition))
                continue;

            SpawnVisibleCitizen(citizen, worldPosition);
        }
    }

    private void ClearVisibleCitizens()
    {
        foreach (KeyValuePair<int, VisibleCitizen> pair in _visibleCitizensById)
        {
            if (pair.Value != null &&
                pair.Value.UnitEntity != Entity.Null &&
                _ecsWorld != null &&
                _ecsWorld.IsCreated &&
                _entityManager.Exists(pair.Value.UnitEntity))
            {
                _entityManager.DestroyEntity(pair.Value.UnitEntity);
            }
        }

        _visibleCitizensById.Clear();
    }

    private static void SetCitizenStatus(ref CitizenRecord citizen, CitizenStatus status, int targetBuildingId, float stateDurationSeconds)
    {
        citizen.Status = status;
        citizen.CurrentTargetBuildingId = targetBuildingId != 0 ? targetBuildingId : citizen.HomeBuildingId;
        citizen.StateStartedAt = Time.time;
        citizen.StateEndsAt = stateDurationSeconds > 0f ? Time.time + stateDurationSeconds : 0f;
        citizen.LifeState = status == CitizenStatus.Dead ? CitizenLifeState.Dead : CitizenLifeState.Alive;
    }

    private static bool IsTravelStatus(CitizenStatus status)
    {
        return status == CitizenStatus.GoingToWork ||
               status == CitizenStatus.GoingToShop ||
               status == CitizenStatus.GoingToCityHall ||
               status == CitizenStatus.GoingForWalk ||
               status == CitizenStatus.ReturningHome ||
               status == CitizenStatus.Fleeing ||
               status == CitizenStatus.RefugeeSeekingShelter ||
               status == CitizenStatus.RelocatingToNewHouse;
    }

    private int FindNearestBuilding(int originBuildingId, List<int> candidateBuildingIds, int excludeBuildingId = 0)
    {
        if (!HasRuntimeBuildingQuery() || candidateBuildingIds == null || candidateBuildingIds.Count == 0)
            return 0;
        if (!TryGetRuntimeBuildingFocusWorldPosition(originBuildingId, out Vector3 originPosition))
            return 0;

        return FindNearestBuilding(originPosition, candidateBuildingIds, excludeBuildingId);
    }

    private int FindNearestBuilding(Vector3 originPosition, List<int> candidateBuildingIds, int excludeBuildingId = 0)
    {
        if (!HasRuntimeBuildingQuery() || candidateBuildingIds == null || candidateBuildingIds.Count == 0)
            return 0;

        int bestBuildingId = 0;
        float bestDistanceSq = float.MaxValue;
        for (int i = 0; i < candidateBuildingIds.Count; i++)
        {
            int candidateBuildingId = candidateBuildingIds[i];
            if (excludeBuildingId != 0 && candidateBuildingId == excludeBuildingId)
                continue;
            if (!TryGetRuntimeBuildingFocusWorldPosition(candidateBuildingId, out Vector3 candidatePosition))
                continue;

            float distanceSq = (candidatePosition - originPosition).sqrMagnitude;
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestBuildingId = candidateBuildingId;
        }

        return bestBuildingId;
    }

    private int FindNearestAvailableRefugeeTent(HouseholdRecord household)
    {
        if (!HasRuntimeBuildingQuery() || _runtimeRefugeeTentBuildingIds.Count == 0)
            return 0;

        if (!TryGetHouseholdReferenceWorldPosition(household, out Vector3 originPosition))
            return 0;

        int requiredSlots = Mathf.Max(1, CountLivingHouseholdMembers(household));
        int bestBuildingId = 0;
        float bestDistanceSq = float.MaxValue;
        for (int i = 0; i < _runtimeRefugeeTentBuildingIds.Count; i++)
        {
            int candidateBuildingId = _runtimeRefugeeTentBuildingIds[i];
            if (!TryGetRuntimeBuildingRefugeeSettings(candidateBuildingId, out int refugeeCapacity, out _))
                continue;
            if (refugeeCapacity <= 0)
                continue;

            int occupiedSlots = GetAssignedRefugeeOccupancy(candidateBuildingId);
            if (occupiedSlots + requiredSlots > refugeeCapacity)
                continue;
            if (!TryGetRuntimeBuildingFocusWorldPosition(candidateBuildingId, out Vector3 candidatePosition))
                continue;

            float distanceSq = (candidatePosition - originPosition).sqrMagnitude;
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestBuildingId = candidateBuildingId;
        }

        return bestBuildingId;
    }

    private bool TryGetHouseholdReferenceWorldPosition(HouseholdRecord household, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!HasRuntimeBuildingQuery())
            return false;
        if (TryGetRuntimeBuildingFocusWorldPosition(household.HomeBuildingId, out worldPosition))
            return true;

        if (TryGetCitizenReferenceWorldPosition(household.MaleCitizenId, out worldPosition))
            return true;
        if (TryGetCitizenReferenceWorldPosition(household.FemaleCitizenId, out worldPosition))
            return true;

        return false;
    }

    private bool TryGetCitizenReferenceWorldPosition(int citizenId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!HasRuntimeBuildingQuery())
            return false;
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return false;

        int preferredBuildingId = citizen.CurrentTargetBuildingId != 0 ? citizen.CurrentTargetBuildingId : citizen.HomeBuildingId;
        return TryGetRuntimeBuildingFocusWorldPosition(preferredBuildingId, out worldPosition);
    }

    private int CountLivingHouseholdMembers(HouseholdRecord household)
    {
        int count = 0;
        if (IsCitizenAlive(household.MaleCitizenId))
            count++;
        if (IsCitizenAlive(household.FemaleCitizenId))
            count++;
        return count;
    }

    private int CountLivingHouseholdRefugees(HouseholdRecord household)
    {
        int count = 0;
        if (IsCitizenRefugee(household.MaleCitizenId))
            count++;
        if (IsCitizenRefugee(household.FemaleCitizenId))
            count++;
        return count;
    }

    private int GetAssignedRefugeeOccupancy(int refugeeTentBuildingId)
    {
        int occupied = 0;
        PopulateHouseholdIdsFromEcs();
        for (int i = 0; i < _scratchHouseholdIds.Count; i++)
        {
            if (!TryGetHouseholdRecord(_scratchHouseholdIds[i], out HouseholdRecord household))
                continue;
            if (household.RefugeeTentBuildingId != refugeeTentBuildingId)
                continue;

            occupied += CountLivingHouseholdRefugees(household);
        }

        return occupied;
    }

    private void ReleaseHouseholdRefugeeAssignment(int householdId)
    {
        if (!TryGetHouseholdRecord(householdId, out HouseholdRecord household))
            return;

        household.RefugeeTentBuildingId = 0;
        StoreHousehold(household);
    }

    private bool IsCitizenAlive(int citizenId)
    {
        return TryGetCitizenRecord(citizenId, out CitizenRecord citizen) && citizen.LifeState != CitizenLifeState.Dead;
    }

    private bool HasLivingCitizen(int citizenId)
    {
        return IsCitizenAlive(citizenId);
    }

    private bool IsCitizenRefugee(int citizenId)
    {
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return false;

        return citizen.LifeState != CitizenLifeState.Dead &&
               (citizen.Status == CitizenStatus.RefugeeSeekingShelter || citizen.Status == CitizenStatus.AtRefugeeTent);
    }

    private bool IsCitizenAwaitingRehousing(int citizenId)
    {
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return false;

        return citizen.LifeState != CitizenLifeState.Dead &&
               (citizen.Status == CitizenStatus.RefugeeSeekingShelter || citizen.Status == CitizenStatus.AtRefugeeTent);
    }

    private bool ShouldCitizenBeVisible(CitizenRecord citizen, float maxDistance, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (_worldCamera == null || !HasRuntimeBuildingQuery())
            return false;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return false;
        if (citizen.Status == CitizenStatus.AtRefugeeTent)
            return false;
        if (!TryGetCitizenReferenceAnchorWorldPosition(citizen, out Vector3 anchorPosition))
            return false;

        Vector3 cameraPosition = _worldCamera.transform.position;
        if ((anchorPosition - cameraPosition).sqrMagnitude > maxDistance * maxDistance)
            return false;

        worldPosition = anchorPosition;
        return true;
    }

    private int GetTravelOriginBuildingId(CitizenRecord citizen)
    {
        if (TryGetHouseholdRecord(citizen.HouseholdId, out HouseholdRecord household))
        {
            if (household.RefugeeTentBuildingId != 0 &&
                (citizen.Status == CitizenStatus.RefugeeSeekingShelter ||
                 citizen.Status == CitizenStatus.AtRefugeeTent ||
                 citizen.Status == CitizenStatus.GoingForWalk ||
                 citizen.Status == CitizenStatus.GoingToShop ||
                 citizen.Status == CitizenStatus.GoingToCityHall ||
                 citizen.Status == CitizenStatus.ReturningHome ||
                 citizen.Status == CitizenStatus.Fleeing))
            {
                return household.RefugeeTentBuildingId;
            }

            if (citizen.Status == CitizenStatus.RelocatingToNewHouse && household.RefugeeTentBuildingId != 0)
                return household.RefugeeTentBuildingId;
        }

        return citizen.HomeBuildingId;
    }

    private bool TryGetCitizenReferenceAnchorWorldPosition(CitizenRecord citizen, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (_visibleCitizensById.TryGetValue(citizen.CitizenId, out VisibleCitizen visibleCitizen) &&
            visibleCitizen != null &&
            visibleCitizen.UnitEntity != Entity.Null &&
            _ecsWorld != null &&
            _ecsWorld.IsCreated &&
            _entityManager.Exists(visibleCitizen.UnitEntity) &&
            _entityManager.HasComponent<LocalTransform>(visibleCitizen.UnitEntity))
        {
            worldPosition = _entityManager.GetComponentData<LocalTransform>(visibleCitizen.UnitEntity).Position;
            return true;
        }

        int anchorBuildingId = IsTravelStatus(citizen.Status)
            ? GetTravelOriginBuildingId(citizen)
            : citizen.CurrentTargetBuildingId;

        if (anchorBuildingId == 0)
            anchorBuildingId = citizen.HomeBuildingId;
        if (anchorBuildingId == 0)
            return false;
        if (TryGetCitizenBuildingApproachWorldPosition(anchorBuildingId, citizen, out worldPosition))
            return true;

        if (!TryGetRuntimeBuildingFocusWorldPosition(anchorBuildingId, out Vector3 anchorPosition))
            return false;

        worldPosition = ResolveCitizenWorldPosition(citizen, anchorPosition);
        return true;
    }

    private bool TryWorldToCell(Vector3 worldPosition, out int2 cell)
    {
        cell = default;
        if (_ecsWorld == null || !_ecsWorld.IsCreated || _gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _gridConfigQuery.GetSingletonEntity();
        GridConfig grid = _entityManager.GetComponentData<GridConfig>(gridEntity);
        cell = GridUtils.WorldToCell(grid, worldPosition);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    private bool TryGetCitizenMoveGoal(CitizenRecord citizen, Vector3 currentPosition, out int2 goalCell)
    {
        goalCell = default;
        if (!HasRuntimeBuildingQuery())
            return false;
        if (!TryGetCitizenSegmentGoalCell(citizen, currentPosition, out goalCell))
            return false;

        return true;
    }

    private bool TryGetCitizenSegmentGoalCell(CitizenRecord citizen, Vector3 currentPosition, out int2 goalCell)
    {
        goalCell = default;
        if (!HasRuntimeBuildingQuery())
            return false;

        int2 currentCell;
        if (!TryWorldToCell(currentPosition, out currentCell))
            currentCell = default;

        int2 targetCell;
        if (!TryGetCitizenBuildingApproachCell(citizen.CurrentTargetBuildingId, citizen, currentCell, out targetCell))
        {
            if (!TryGetRuntimeBuildingFocusWorldPosition(citizen.CurrentTargetBuildingId, out Vector3 targetPosition))
                return false;

            Vector3 desiredWorld = ResolveCitizenWorldPosition(citizen, targetPosition);
            if (!TryWorldToCell(desiredWorld, out targetCell))
                return false;
        }

        float2 delta = new float2(targetCell.x - currentCell.x, targetCell.y - currentCell.y);
        float distance = math.length(delta);
        if (distance > MaxVisibleTravelSegmentDistance && distance > 0.001f)
        {
            float2 dir = delta / distance;
            targetCell = currentCell + (int2)math.round(dir * MaxVisibleTravelSegmentDistance);
        }

        goalCell = targetCell;
        return true;
    }

    private float EstimateTravelSeconds(CitizenRecord citizen, int targetBuildingId)
    {
        if (!HasRuntimeBuildingQuery())
            return 0f;
        if (targetBuildingId == 0)
            return 0f;

        int originBuildingId = citizen.CurrentTargetBuildingId != 0 ? citizen.CurrentTargetBuildingId : citizen.HomeBuildingId;
        if (originBuildingId == 0)
            originBuildingId = GetTravelOriginBuildingId(citizen);
        if (originBuildingId == 0)
            return 0f;
        if (!TryGetRuntimeBuildingFocusWorldPosition(originBuildingId, out Vector3 originPosition))
            return 0f;
        if (!TryGetRuntimeBuildingFocusWorldPosition(targetBuildingId, out Vector3 targetPosition))
            return 0f;

        float distanceCells = Vector3.Distance(originPosition, targetPosition);
        return Mathf.Max(1f, distanceCells / DeferredTravelCellsPerSecond);
    }

    private bool TryGetCitizenBuildingApproachCell(int buildingId, CitizenRecord citizen, int2 referenceCell, out int2 goalCell)
    {
        goalCell = default;
        return HasRuntimeBuildingQuery() &&
               TryGetRuntimeBuildingApproachCell(buildingId, new int2(1, 1), referenceCell, out goalCell);
    }

    private bool TryGetCitizenBuildingApproachWorldPosition(int buildingId, CitizenRecord citizen, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        int2 referenceCell = default;
        if (_visibleCitizensById.TryGetValue(citizen.CitizenId, out VisibleCitizen visibleCitizen) &&
            visibleCitizen != null &&
            visibleCitizen.UnitEntity != Entity.Null &&
            _ecsWorld != null &&
            _ecsWorld.IsCreated &&
            _entityManager.Exists(visibleCitizen.UnitEntity) &&
            _entityManager.HasComponent<UnitGrid>(visibleCitizen.UnitEntity))
        {
            referenceCell = _entityManager.GetComponentData<UnitGrid>(visibleCitizen.UnitEntity).Cell;
        }

        if (!TryGetCitizenBuildingApproachCell(buildingId, citizen, referenceCell, out int2 approachCell))
            return false;

        Entity gridEntity = _gridConfigQuery.GetSingletonEntity();
        GridConfig grid = _entityManager.GetComponentData<GridConfig>(gridEntity);
        worldPosition = GridUtils.CellToWorldCenter(grid, approachCell);
        return true;
    }

    private Vector3 ResolveCitizenWorldPosition(CitizenRecord citizen, Vector3 anchorPosition)
    {
        int slotIndex = citizen.Gender == CitizenGender.Male ? 0 : 1;
        float xOffset = slotIndex == 0 ? -2.5f : 2.5f;
        float zOffset = ((citizen.HouseholdId & 1) == 0) ? 1.5f : -1.5f;
        return anchorPosition + new Vector3(xOffset, 0f, zOffset);
    }

    private void SpawnVisibleCitizen(CitizenRecord citizen, Vector3 worldPosition)
    {
        GameObject prefab = GetCitizenPrefab(citizen);
        if (prefab == null || _ecsWorld == null || !_ecsWorld.IsCreated)
            return;
        if (!_citizenPrefabSystem.TryResolveConfiguredUnitPrefabEntity(_citizenPrefabContext, prefab, out Entity prefabEntity) || prefabEntity == Entity.Null)
            return;
        if (!TryWorldToCell(worldPosition, out int2 spawnCell))
            return;

        Entity instance = _entityManager.Instantiate(prefabEntity);
        if (_entityManager.HasComponent<UnitGrid>(instance))
            _entityManager.SetComponentData(instance, new UnitGrid { Cell = spawnCell });
        if (_entityManager.HasComponent<LocalTransform>(instance))
            _entityManager.SetComponentData(instance, LocalTransform.FromPosition(worldPosition));
        if (_entityManager.HasComponent<UnitPrevWorldPos>(instance))
            _entityManager.SetComponentData(instance, new UnitPrevWorldPos { Value = worldPosition });
        if (_entityManager.HasComponent<UnitGridInitialized>(instance))
            _entityManager.RemoveComponent<UnitGridInitialized>(instance);
        if (_entityManager.HasComponent<UnitMovementBehavior>(instance))
        {
            UnitMovementBehavior movementBehavior = _entityManager.GetComponentData<UnitMovementBehavior>(instance);
            movementBehavior.AllowIdleWander = 0;
            _entityManager.SetComponentData(instance, movementBehavior);
        }
        if (_entityManager.HasComponent<UnitCombat>(instance))
        {
            UnitCombat combat = _entityManager.GetComponentData<UnitCombat>(instance);
            combat.CanAttack = 0;
            combat.AutoEngage = 0;
            _entityManager.SetComponentData(instance, combat);
        }
        if (_entityManager.HasComponent<Faction>(instance))
            _entityManager.SetComponentData(instance, new Faction { Id = 2 });
        if (_entityManager.HasComponent<UnitTarget>(instance))
            _entityManager.RemoveComponent<UnitTarget>(instance);
        if (_entityManager.HasComponent<UnitPathRequest>(instance))
            _entityManager.RemoveComponent<UnitPathRequest>(instance);
        if (_entityManager.HasComponent<UnitPathFollow>(instance))
            _entityManager.RemoveComponent<UnitPathFollow>(instance);
        if (_entityManager.HasComponent<SelectedUnitTag>(instance))
            _entityManager.RemoveComponent<SelectedUnitTag>(instance);
        if (!_entityManager.HasComponent<CivilianUnitTag>(instance))
            _entityManager.AddComponentData(instance, new CivilianUnitTag());

        int2 goalCell = spawnCell;
        if (TryGetCitizenMoveGoal(citizen, worldPosition, out int2 resolvedGoalCell))
            goalCell = resolvedGoalCell;
        IssueCitizenMoveCommand(instance, goalCell);

        _visibleCitizensById[citizen.CitizenId] = new VisibleCitizen
        {
            CitizenId = citizen.CitizenId,
            UnitEntity = instance,
            GoalCell = goalCell,
            TargetBuildingId = citizen.CurrentTargetBuildingId
        };
    }

    private void RemoveVisibleCitizen(int citizenId)
    {
        if (!_visibleCitizensById.TryGetValue(citizenId, out VisibleCitizen visibleCitizen))
            return;

        if (visibleCitizen != null &&
            visibleCitizen.UnitEntity != Entity.Null &&
            _ecsWorld != null &&
            _ecsWorld.IsCreated &&
            _entityManager.Exists(visibleCitizen.UnitEntity))
            _entityManager.DestroyEntity(visibleCitizen.UnitEntity);

        _visibleCitizensById.Remove(citizenId);
    }

    private GameObject GetCitizenPrefab(CitizenRecord citizen)
    {
        GameObject[] prefabs = citizen.Gender == CitizenGender.Male ? _maleCitizenPrefabs : _femaleCitizenPrefabs;
        if (prefabs == null || prefabs.Length == 0)
            return null;

        int index = Mathf.Abs(citizen.CitizenId) % prefabs.Length;
        return prefabs[index];
    }

    private void IssueCitizenMoveCommand(Entity entity, int2 goal)
    {
        if (_entityManager.HasComponent<EngageTarget>(entity))
            _entityManager.RemoveComponent<EngageTarget>(entity);
        if (_entityManager.HasComponent<UnitPathFollow>(entity))
            _entityManager.RemoveComponent<UnitPathFollow>(entity);
        if (_entityManager.HasComponent<UnitPathRange>(entity))
            _entityManager.RemoveComponent<UnitPathRange>(entity);
        if (_entityManager.HasComponent<AutoWanderMoveTag>(entity))
            _entityManager.RemoveComponent<AutoWanderMoveTag>(entity);

        if (_entityManager.HasComponent<UnitTarget>(entity))
            _entityManager.SetComponentData(entity, new UnitTarget { Cell = goal });
        else
            _entityManager.AddComponentData(entity, new UnitTarget { Cell = goal });

        if (!_entityManager.HasComponent<UnitAirMovement>(entity))
        {
            if (_entityManager.HasComponent<UnitPathRequest>(entity))
                _entityManager.SetComponentData(entity, new UnitPathRequest { Goal = goal });
            else
                _entityManager.AddComponentData(entity, new UnitPathRequest { Goal = goal });
        }
        else if (_entityManager.HasComponent<UnitPathRequest>(entity))
        {
            _entityManager.RemoveComponent<UnitPathRequest>(entity);
        }

        if (!_entityManager.HasComponent<ManualMoveOrderTag>(entity))
            _entityManager.AddComponent<ManualMoveOrderTag>(entity);
    }

    private static GameObject[] LoadCitizenPrefabs(
        CitizenGender gender,
        CitizenPrefabSystem citizenPrefabSystem,
        CitizenPrefabSystem.Context citizenPrefabContext)
    {
        string[] unitNames = gender == CitizenGender.Male
            ? new[]
            {
                "Unit_Chr_Civilian_Male_01",
                "Unit_Chr_Civilian_Male_02"
            }
            : new[]
            {
                "Unit_Chr_Civilian_Female_01",
                "Unit_Chr_Civilian_Female_02"
            };

        List<GameObject> prefabs = new();
        if (citizenPrefabSystem == null)
            return prefabs.ToArray();

        citizenPrefabSystem.LoadConfiguredUnitSpawnPrefabs(citizenPrefabContext, unitNames, prefabs);
        return prefabs.ToArray();
    }

    private bool ShouldUseTravelStatus(CitizenRecord citizen, CitizenStatus desiredStatus, int desiredTargetBuildingId)
    {
        if (!_visibleCitizensById.ContainsKey(citizen.CitizenId))
            return false;

        CitizenStatus settledStatus = GetSettledStatus(citizen.Status);
        return settledStatus != desiredStatus || citizen.CurrentTargetBuildingId != desiredTargetBuildingId;
    }

    private static CitizenStatus GetTravelStatusForDesiredStatus(CitizenStatus desiredStatus)
    {
        return desiredStatus switch
        {
            CitizenStatus.AtWork => CitizenStatus.GoingToWork,
            CitizenStatus.AtShop => CitizenStatus.GoingToShop,
            CitizenStatus.GoingToCityHall => CitizenStatus.GoingToCityHall,
            CitizenStatus.AtHome => CitizenStatus.ReturningHome,
            CitizenStatus.AtRefugeeTent => CitizenStatus.RefugeeSeekingShelter,
            CitizenStatus.GoingForWalk => CitizenStatus.GoingForWalk,
            CitizenStatus.Fleeing => CitizenStatus.Fleeing,
            _ => desiredStatus
        };
    }

    private static CitizenStatus GetSettledStatus(CitizenStatus status)
    {
        return status switch
        {
            CitizenStatus.GoingToWork => CitizenStatus.AtWork,
            CitizenStatus.GoingToShop => CitizenStatus.AtShop,
            CitizenStatus.ReturningHome => CitizenStatus.AtHome,
            CitizenStatus.Fleeing => CitizenStatus.AtHome,
            CitizenStatus.RefugeeSeekingShelter => CitizenStatus.AtRefugeeTent,
            CitizenStatus.RelocatingToNewHouse => CitizenStatus.AtHome,
            _ => status
        };
    }

    private void ResolveCitizenArrival(int citizenId)
    {
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return;

        CitizenStatus settledStatus = GetSettledStatus(citizen.Status);
        if (settledStatus == citizen.Status)
            return;

        SetCitizenStatus(ref citizen, settledStatus, citizen.CurrentTargetBuildingId, 0f);
        StoreCitizen(citizen);
    }

    private bool MarkCitizenDead(int citizenId, string reason)
    {
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return false;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return false;

        SetCitizenStatus(ref citizen, CitizenStatus.Dead, citizen.CurrentTargetBuildingId, 0f);
        StoreCitizen(citizen);
        RemoveVisibleCitizen(citizenId);
        RecalculateTotals();
        return true;
    }

    private void UpdateHouseholdDisplacementState()
    {
        if (!HasHouseholdData() || !HasRuntimeBuildingQuery())
            return;

        PopulateHouseholdIdsFromEcs();

        for (int i = 0; i < _scratchHouseholdIds.Count; i++)
        {
            int householdId = _scratchHouseholdIds[i];
            if (!TryGetHouseholdRecord(householdId, out HouseholdRecord household))
                continue;
            if (household.IsDisplaced != 0)
                continue;

            bool homeExists = TryGetRuntimeBuildingDestroyedState(household.HomeBuildingId, out bool isDestroyed);
            if (homeExists && !isDestroyed)
                continue;

            DisplaceHousehold(household, homeExists ? "home-destroyed" : "home-missing");
        }
    }

    private void DisplaceHousehold(HouseholdRecord household, string reason)
    {
        int refugeeTentBuildingId = FindNearestAvailableRefugeeTent(household);
        household.IsDisplaced = 1;
        household.RefugeeTentBuildingId = refugeeTentBuildingId;
        household = StoreHousehold(household);

        if (refugeeTentBuildingId != 0)
        {
            MoveCitizenToRefugeeState(household.MaleCitizenId, refugeeTentBuildingId, reason);
            MoveCitizenToRefugeeState(household.FemaleCitizenId, refugeeTentBuildingId, reason);
            return;
        }

        MarkCitizenDead(household.MaleCitizenId, $"{reason}-no-refugee");
        MarkCitizenDead(household.FemaleCitizenId, $"{reason}-no-refugee");
    }

    private void MoveCitizenToRefugeeState(int citizenId, int refugeeTentBuildingId, string reason)
    {
        if (!TryGetCitizenRecord(citizenId, out CitizenRecord citizen))
            return;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return;

        SetCitizenStatus(ref citizen, CitizenStatus.RefugeeSeekingShelter, refugeeTentBuildingId, EstimateTravelSeconds(citizen, refugeeTentBuildingId));
        StoreCitizen(citizen);
    }

    private void PopulateCitizenIdsFromEcs()
    {
        _scratchCitizenIds.Clear();
        foreach (int citizenId in _citizensById.Keys)
            _scratchCitizenIds.Add(citizenId);
    }

    private void PopulateHouseholdIdsFromEcs()
    {
        _scratchHouseholdIds.Clear();
        foreach (int householdId in _householdsById.Keys)
            _scratchHouseholdIds.Add(householdId);
    }

    private bool HasCitizenData()
    {
        return _citizensById.Count > 0;
    }

    private bool HasHouseholdData()
    {
        if (_ecsWorld != null && _ecsWorld.IsCreated)
            return !_householdEntityQuery.IsEmptyIgnoreFilter;

        return _householdsById.Count > 0;
    }

    private int GetCitizenCount()
    {
        if (_ecsWorld != null && _ecsWorld.IsCreated)
            return _citizenEntityQuery.CalculateEntityCount();

        return _citizensById.Count;
    }

    private int GetHouseholdCount()
    {
        if (_ecsWorld != null && _ecsWorld.IsCreated)
            return _householdEntityQuery.CalculateEntityCount();

        return _householdsById.Count;
    }

    private void RefreshDangerSourcesIfNeeded()
    {
        if (Time.time < _nextDangerScanAt)
            return;

        _nextDangerScanAt = Time.time + DangerScanIntervalSeconds;
        _dangerWorldPositions.Clear();
        Transform[] sceneTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform transform = sceneTransforms[i];
            if (transform == null)
                continue;

            string name = transform.name;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (name.IndexOf("fire", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("burn", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("smoke", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            _dangerWorldPositions.Add(transform.position);
        }
    }

    private bool TryGetDangerFleeTarget(CitizenRecord citizen, out int fleeTargetBuildingId)
    {
        fleeTargetBuildingId = 0;
        if (_dangerWorldPositions.Count == 0 || !HasRuntimeBuildingQuery())
            return false;
        if (!TryGetRuntimeBuildingFocusWorldPosition(citizen.CurrentTargetBuildingId, out Vector3 citizenPosition))
            return false;

        float detectRadiusSq = DangerDetectRadius * DangerDetectRadius;
        bool dangerNearby = false;
        for (int i = 0; i < _dangerWorldPositions.Count; i++)
        {
            if ((_dangerWorldPositions[i] - citizenPosition).sqrMagnitude > detectRadiusSq)
                continue;

            dangerNearby = true;
            break;
        }

        if (!dangerNearby)
            return false;

        bool homeSafe = IsBuildingSafeFromDanger(citizen.HomeBuildingId);
        if (homeSafe)
        {
            fleeTargetBuildingId = citizen.HomeBuildingId;
            return true;
        }

        fleeTargetBuildingId = FindNearestSafeBuilding(citizen.CurrentTargetBuildingId);
        return fleeTargetBuildingId != 0;
    }

    private bool IsBuildingSafeFromDanger(int buildingId)
    {
        if (!HasRuntimeBuildingQuery() || !TryGetRuntimeBuildingFocusWorldPosition(buildingId, out Vector3 buildingPosition))
            return false;

        float detectRadiusSq = DangerDetectRadius * DangerDetectRadius;
        for (int i = 0; i < _dangerWorldPositions.Count; i++)
        {
            if ((_dangerWorldPositions[i] - buildingPosition).sqrMagnitude <= detectRadiusSq)
                return false;
        }

        return true;
    }

    private int FindNearestSafeBuilding(int originBuildingId)
    {
        int safeTarget = FindNearestSafeBuildingFromList(originBuildingId, _runtimeCityHallBuildingIds);
        if (safeTarget != 0)
            return safeTarget;

        safeTarget = FindNearestSafeBuildingFromList(originBuildingId, _runtimeRefugeeTentBuildingIds);
        if (safeTarget != 0)
            return safeTarget;

        safeTarget = FindNearestSafeBuildingFromList(originBuildingId, _runtimeMilitaryCampBuildingIds);
        if (safeTarget != 0)
            return safeTarget;

        return FindNearestSafeBuildingFromList(originBuildingId, _runtimeHouseBuildingIds, originBuildingId);
    }

    private int FindNearestSafeBuildingFromList(int originBuildingId, List<int> candidates, int excludeBuildingId = 0)
    {
        if (candidates == null || candidates.Count == 0)
            return 0;

        int bestId = 0;
        float bestDistanceSq = float.MaxValue;
        if (!TryGetRuntimeBuildingFocusWorldPosition(originBuildingId, out Vector3 originPosition))
            return 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            int candidateId = candidates[i];
            if (excludeBuildingId != 0 && candidateId == excludeBuildingId)
                continue;
            if (!IsBuildingSafeFromDanger(candidateId))
                continue;
            if (!TryGetRuntimeBuildingFocusWorldPosition(candidateId, out Vector3 candidatePosition))
                continue;

            float distanceSq = (candidatePosition - originPosition).sqrMagnitude;
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestId = candidateId;
        }

        return bestId;
    }

    private CitizenStatus GetScheduledStatus(CitizenRecord citizen)
    {
        if (citizen.LifeState == CitizenLifeState.Dead)
            return CitizenStatus.Dead;

        if (TryGetHouseholdRecord(citizen.HouseholdId, out HouseholdRecord household) &&
            household.IsDisplaced != 0 &&
            household.RefugeeTentBuildingId != 0)
        {
            if (_dayNightSystem == null || IsNightSchedule())
                return CitizenStatus.AtRefugeeTent;

            float refugeeHour = _dayNightSystem.CurrentHour;
            bool morningWalk = refugeeHour >= RefugeeMorningWalkStartHour && refugeeHour < RefugeeMorningWalkEndHour;
            bool lunchShelter = refugeeHour >= RefugeeLunchShelterStartHour && refugeeHour < RefugeeLunchShelterEndHour;
            bool eveningWalk = refugeeHour >= RefugeeEveningWalkStartHour && refugeeHour < RefugeeEveningWalkEndHour;

            if ((morningWalk || eveningWalk) &&
                !lunchShelter &&
                citizen.PreferredWalkBuildingId != 0)
            {
                return CitizenStatus.GoingForWalk;
            }

            return CitizenStatus.AtRefugeeTent;
        }

        if (_dayNightSystem == null || IsNightSchedule())
            return CitizenStatus.AtHome;

        bool isWeekend = IsWeekend(GetDayOfWeek());
        float currentHour = _dayNightSystem.CurrentHour;
        if (isWeekend)
        {
            if (currentHour >= WeekendShoppingStartHour && currentHour < WeekendShoppingEndHour && citizen.PreferredShopBuildingId != 0)
                return CitizenStatus.AtShop;
            if (currentHour >= WeekendCityHallStartHour && currentHour < WeekendCityHallEndHour && citizen.PreferredCityHallBuildingId != 0)
                return CitizenStatus.GoingToCityHall;
            return CitizenStatus.AtHome;
        }

        if (currentHour >= WeekdayEveningWalkStartHour &&
            currentHour < WeekdayEveningWalkEndHour &&
            citizen.PreferredWalkBuildingId != 0)
        {
            return CitizenStatus.GoingForWalk;
        }

        if (citizen.Gender == CitizenGender.Male &&
            citizen.LunchShopBuildingId != 0 &&
            currentHour >= WeekdayLunchStartHour &&
            currentHour < WeekdayLunchEndHour)
        {
            return CitizenStatus.AtShop;
        }

        if (citizen.Gender == CitizenGender.Male &&
            citizen.WorkBuildingId != 0 &&
            currentHour >= WeekdayWorkStartHour &&
            currentHour < WeekdayWorkEndHour)
        {
            return CitizenStatus.AtWork;
        }

        if (citizen.Gender == CitizenGender.Female &&
            citizen.PreferredShopBuildingId != 0 &&
            currentHour >= WeekdayShoppingStartHour &&
            currentHour < WeekdayShoppingEndHour &&
            ShouldCitizenShopOnWeekday(citizen))
        {
            return CitizenStatus.AtShop;
        }

        return CitizenStatus.AtHome;
    }

    private int GetScheduledTargetBuildingId(CitizenRecord citizen, CitizenStatus status)
    {
        if (TryGetHouseholdRecord(citizen.HouseholdId, out HouseholdRecord household) &&
            household.IsDisplaced != 0 &&
            household.RefugeeTentBuildingId != 0)
        {
            return status switch
            {
                CitizenStatus.GoingForWalk => citizen.PreferredWalkBuildingId != 0 ? citizen.PreferredWalkBuildingId : household.RefugeeTentBuildingId,
                CitizenStatus.AtRefugeeTent => household.RefugeeTentBuildingId,
                _ => household.RefugeeTentBuildingId
            };
        }

        return status switch
        {
            CitizenStatus.AtWork => citizen.WorkBuildingId != 0 ? citizen.WorkBuildingId : citizen.HomeBuildingId,
            CitizenStatus.AtShop => ResolveShopTarget(citizen),
            CitizenStatus.GoingToCityHall => citizen.PreferredCityHallBuildingId != 0 ? citizen.PreferredCityHallBuildingId : citizen.HomeBuildingId,
            CitizenStatus.GoingForWalk => citizen.PreferredWalkBuildingId != 0 ? citizen.PreferredWalkBuildingId : citizen.HomeBuildingId,
            _ => citizen.HomeBuildingId
        };
    }

    private int GetDayOfWeek()
    {
        if (_dayNightSystem == null)
            return 1;

        return ((_dayNightSystem.DayCount - 1) % 7) + 1;
    }

    private bool IsNightSchedule()
    {
        return _dayNightSystem == null || _dayNightSystem.IsNightTime;
    }

    private static bool IsWeekend(int dayOfWeek)
    {
        return dayOfWeek == 6 || dayOfWeek == 7;
    }

    private bool ShouldCitizenShopOnWeekday(CitizenRecord citizen)
    {
        if (_dayNightSystem == null)
            return false;

        return ((citizen.HouseholdId + _dayNightSystem.DayCount) & 1) == 0;
    }

    private int GetSchedulePhase()
    {
        if (_dayNightSystem == null || IsNightSchedule())
            return 0;

        bool isWeekend = IsWeekend(GetDayOfWeek());
        float currentHour = _dayNightSystem.CurrentHour;
        if (isWeekend)
        {
            if (currentHour >= WeekendShoppingStartHour && currentHour < WeekendShoppingEndHour)
                return 1;
            if (currentHour >= WeekendCityHallStartHour && currentHour < WeekendCityHallEndHour)
                return 2;
            return 3;
        }

        if (currentHour >= WeekdayEveningWalkStartHour && currentHour < WeekdayEveningWalkEndHour)
            return 4;
        if (currentHour >= WeekdayLunchStartHour && currentHour < WeekdayLunchEndHour)
            return 2;
        if (currentHour >= WeekdayWorkStartHour && currentHour < WeekdayWorkEndHour)
            return 1;
        return 3;
    }

    private int ResolveShopTarget(CitizenRecord citizen)
    {
        if (_dayNightSystem != null &&
            !IsWeekend(GetDayOfWeek()) &&
            citizen.Gender == CitizenGender.Male &&
            citizen.LunchShopBuildingId != 0 &&
            _dayNightSystem.CurrentHour >= WeekdayLunchStartHour &&
            _dayNightSystem.CurrentHour < WeekdayLunchEndHour)
        {
            return citizen.LunchShopBuildingId;
        }

        return citizen.PreferredShopBuildingId != 0 ? citizen.PreferredShopBuildingId : citizen.HomeBuildingId;
    }

}
