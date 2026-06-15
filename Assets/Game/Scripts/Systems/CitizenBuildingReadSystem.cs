using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed partial class CitizenBuildingReadSystem : SystemBase
{
    private const float RuntimeBuildingListRefreshIntervalSeconds = 0.25f;

    private BuildingRuntimeQuerySystem _buildingRuntimeQuerySystem;
    private BuildingRuntimeQuerySystem.Context _buildingRuntimeQueryContext;
    private readonly List<int> _runtimeHouseBuildingIds = new();
    private readonly List<int> _runtimeShopBuildingIds = new();
    private readonly List<int> _runtimeCityHallBuildingIds = new();
    private readonly List<int> _runtimeRefugeeTentBuildingIds = new();
    private readonly List<int> _runtimeMilitaryCampBuildingIds = new();
    private readonly HashSet<int> _runtimeHouseBuildingIdSet = new();
    private float _nextRuntimeBuildingListRefreshAt;

    public IReadOnlyList<int> HouseBuildingIds => _runtimeHouseBuildingIds;
    public IReadOnlyList<int> ShopBuildingIds => _runtimeShopBuildingIds;
    public IReadOnlyList<int> CityHallBuildingIds => _runtimeCityHallBuildingIds;
    public IReadOnlyList<int> RefugeeTentBuildingIds => _runtimeRefugeeTentBuildingIds;
    public IReadOnlyList<int> MilitaryCampBuildingIds => _runtimeMilitaryCampBuildingIds;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void Init(BuildingRuntimeQuerySystem buildingRuntimeQuerySystem, BuildingRuntimeQuerySystem.Context buildingRuntimeQueryContext)
    {
        _buildingRuntimeQuerySystem = buildingRuntimeQuerySystem;
        _buildingRuntimeQueryContext = buildingRuntimeQueryContext;
        ClearRuntimeBuildingLists();
        _nextRuntimeBuildingListRefreshAt = 0f;
    }

    public void Dispose()
    {
        ClearRuntimeBuildingLists();
        _buildingRuntimeQuerySystem = null;
        _buildingRuntimeQueryContext = default;
        _nextRuntimeBuildingListRefreshAt = 0f;
    }

    public bool HasRuntimeBuildingQuery()
    {
        return _buildingRuntimeQuerySystem != null;
    }

    public bool RefreshRuntimeBuildingListsIfDue(float now)
    {
        if (now < _nextRuntimeBuildingListRefreshAt)
            return false;

        RefreshRuntimeBuildingLists(now, force: true);
        return true;
    }

    public void RefreshRuntimeBuildingLists(float now, bool force)
    {
        if (!force && now < _nextRuntimeBuildingListRefreshAt)
            return;
        if (!HasRuntimeBuildingQuery())
            return;

        _nextRuntimeBuildingListRefreshAt = now + RuntimeBuildingListRefreshIntervalSeconds;
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

    public bool IsRuntimeHouseBuilding(int buildingId)
    {
        return _runtimeHouseBuildingIdSet.Contains(buildingId);
    }

    public bool TryGetRuntimeBuildingFocusWorldPosition(int buildingId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.TryGetRuntimeBuildingFocusWorldPosition(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   out worldPosition);
    }

    public bool TryGetRuntimeBuildingDestroyedState(int buildingId, out bool isDestroyed)
    {
        isDestroyed = false;
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.TryGetRuntimeBuildingDestroyedState(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   out isDestroyed);
    }

    public bool TryGetRuntimeBuildingRefugeeSettings(int buildingId, out int refugeeCapacity, out int upkeepPerCitizenPerDay)
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

    public bool TryGetRuntimeBuildingApproachCell(int buildingId, int2 unitFootprint, int2 referenceCell, out int2 goal)
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

    public bool IsRuntimeBuildingApproachCell(int buildingId, int2 currentCell, int2 unitFootprint)
    {
        return HasRuntimeBuildingQuery() &&
               _buildingRuntimeQuerySystem.IsRuntimeBuildingApproachCell(
                   _buildingRuntimeQueryContext,
                   buildingId,
                   currentCell,
                   unitFootprint);
    }

    public int FindNearestBuilding(int originBuildingId, IReadOnlyList<int> candidateBuildingIds, int excludeBuildingId = 0)
    {
        if (!HasRuntimeBuildingQuery() || candidateBuildingIds == null || candidateBuildingIds.Count == 0)
            return 0;
        if (!TryGetRuntimeBuildingFocusWorldPosition(originBuildingId, out Vector3 originPosition))
            return 0;

        return FindNearestBuilding(originPosition, candidateBuildingIds, excludeBuildingId);
    }

    public int FindNearestBuilding(Vector3 originPosition, IReadOnlyList<int> candidateBuildingIds, int excludeBuildingId = 0)
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

    private void ClearRuntimeBuildingLists()
    {
        _runtimeHouseBuildingIds.Clear();
        _runtimeShopBuildingIds.Clear();
        _runtimeCityHallBuildingIds.Clear();
        _runtimeRefugeeTentBuildingIds.Clear();
        _runtimeMilitaryCampBuildingIds.Clear();
        _runtimeHouseBuildingIdSet.Clear();
    }
}
