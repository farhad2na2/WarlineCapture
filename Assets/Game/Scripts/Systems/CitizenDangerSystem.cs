using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed partial class CitizenDangerSystem : SystemBase
{
    private const float DangerDetectRadius = 35f;
    private const float DangerScanIntervalSeconds = 1f;

    private readonly List<Transform> _dangerSourceTransforms = new();
    private readonly List<Vector3> _dangerWorldPositions = new();
    private float _nextDangerScanAt;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static void Reset(CitizenDangerSystem system)
    {
        system?.Reset();
    }

    public void Reset()
    {
        _dangerSourceTransforms.Clear();
        _dangerWorldPositions.Clear();
        _nextDangerScanAt = 0f;
    }

    public static void RegisterDangerSource(CitizenDangerSystem system, Transform source)
    {
        system?.RegisterDangerSource(source);
    }

    public void RegisterDangerSource(Transform source)
    {
        if (source == null || _dangerSourceTransforms.Contains(source))
            return;

        _dangerSourceTransforms.Add(source);
    }

    public static void UnregisterDangerSource(CitizenDangerSystem system, Transform source)
    {
        system?.UnregisterDangerSource(source);
    }

    public void UnregisterDangerSource(Transform source)
    {
        if (source != null)
            _dangerSourceTransforms.Remove(source);
    }

    public static void RefreshDangerSourcesIfNeeded(CitizenDangerSystem system, float now)
    {
        system?.RefreshDangerSourcesIfNeeded(now);
    }

    public void RefreshDangerSourcesIfNeeded(float now)
    {
        if (now < _nextDangerScanAt)
            return;

        _nextDangerScanAt = now + DangerScanIntervalSeconds;
        _dangerWorldPositions.Clear();
        for (int i = _dangerSourceTransforms.Count - 1; i >= 0; i--)
        {
            Transform source = _dangerSourceTransforms[i];
            if (source == null)
            {
                _dangerSourceTransforms.RemoveAt(i);
                continue;
            }

            _dangerWorldPositions.Add(source.position);
        }
    }

    public static bool TryGetDangerFleeTarget(
        CitizenDangerSystem system,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenRecordComponent citizen,
        out int fleeTargetBuildingId)
    {
        fleeTargetBuildingId = 0;
        return system != null && system.TryGetDangerFleeTarget(buildingReadSystem, citizen, out fleeTargetBuildingId);
    }

    public bool TryGetDangerFleeTarget(
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenRecordComponent citizen,
        out int fleeTargetBuildingId)
    {
        fleeTargetBuildingId = 0;
        if (_dangerWorldPositions.Count == 0 || buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;
        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(citizen.CurrentTargetBuildingId, out Vector3 citizenPosition))
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

        bool homeSafe = IsBuildingSafeFromDanger(buildingReadSystem, citizen.HomeBuildingId);
        if (homeSafe)
        {
            fleeTargetBuildingId = citizen.HomeBuildingId;
            return true;
        }

        fleeTargetBuildingId = FindNearestSafeBuilding(buildingReadSystem, citizen.CurrentTargetBuildingId);
        return fleeTargetBuildingId != 0;
    }

    private bool IsBuildingSafeFromDanger(CitizenBuildingReadSystem buildingReadSystem, int buildingId)
    {
        if (buildingReadSystem == null ||
            !buildingReadSystem.HasRuntimeBuildingQuery() ||
            !buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(buildingId, out Vector3 buildingPosition))
        {
            return false;
        }

        float detectRadiusSq = DangerDetectRadius * DangerDetectRadius;
        for (int i = 0; i < _dangerWorldPositions.Count; i++)
        {
            if ((_dangerWorldPositions[i] - buildingPosition).sqrMagnitude <= detectRadiusSq)
                return false;
        }

        return true;
    }

    private int FindNearestSafeBuilding(CitizenBuildingReadSystem buildingReadSystem, int originBuildingId)
    {
        int safeTarget = FindNearestSafeBuildingFromList(buildingReadSystem, originBuildingId, buildingReadSystem.CityHallBuildingIds);
        if (safeTarget != 0)
            return safeTarget;

        safeTarget = FindNearestSafeBuildingFromList(buildingReadSystem, originBuildingId, buildingReadSystem.RefugeeTentBuildingIds);
        if (safeTarget != 0)
            return safeTarget;

        safeTarget = FindNearestSafeBuildingFromList(buildingReadSystem, originBuildingId, buildingReadSystem.MilitaryCampBuildingIds);
        if (safeTarget != 0)
            return safeTarget;

        return FindNearestSafeBuildingFromList(buildingReadSystem, originBuildingId, buildingReadSystem.HouseBuildingIds, originBuildingId);
    }

    private int FindNearestSafeBuildingFromList(
        CitizenBuildingReadSystem buildingReadSystem,
        int originBuildingId,
        IReadOnlyList<int> candidates,
        int excludeBuildingId = 0)
    {
        if (candidates == null || candidates.Count == 0)
            return 0;

        int bestId = 0;
        float bestDistanceSq = float.MaxValue;
        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(originBuildingId, out Vector3 originPosition))
            return 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            int candidateId = candidates[i];
            if (excludeBuildingId != 0 && candidateId == excludeBuildingId)
                continue;
            if (!IsBuildingSafeFromDanger(buildingReadSystem, candidateId))
                continue;
            if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(candidateId, out Vector3 candidatePosition))
                continue;

            float distanceSq = (candidatePosition - originPosition).sqrMagnitude;
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestId = candidateId;
        }

        return bestId;
    }
}
