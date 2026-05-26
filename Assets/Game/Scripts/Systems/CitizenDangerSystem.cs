using System.Collections.Generic;
using UnityEngine;

internal sealed class CitizenDangerSystem
{
    private const float DangerDetectRadius = 35f;
    private const float DangerScanIntervalSeconds = 1f;

    private readonly List<Vector3> _dangerWorldPositions = new();
    private float _nextDangerScanAt;

    public void Reset()
    {
        _dangerWorldPositions.Clear();
        _nextDangerScanAt = 0f;
    }

    public void RefreshDangerSourcesIfNeeded(float now)
    {
        if (now < _nextDangerScanAt)
            return;

        _nextDangerScanAt = now + DangerScanIntervalSeconds;
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
