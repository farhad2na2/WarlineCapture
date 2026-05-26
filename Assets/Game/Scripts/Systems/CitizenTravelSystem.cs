using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class CitizenTravelSystem
{
    private const float MaxVisibleTravelSegmentDistance = 48f;
    private const float DeferredTravelCellsPerSecond = 10f;

    public bool TryGetHouseholdReferenceWorldPosition(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenHouseholdRecordComponent household,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;
        if (buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(household.HomeBuildingId, out worldPosition))
            return true;

        if (TryGetCitizenReferenceWorldPosition(state, buildingReadSystem, household.MaleCitizenId, out worldPosition))
            return true;
        if (TryGetCitizenReferenceWorldPosition(state, buildingReadSystem, household.FemaleCitizenId, out worldPosition))
            return true;

        return false;
    }

    public bool TryGetCitizenReferenceWorldPosition(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        int citizenId,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;

        int preferredBuildingId = citizen.CurrentTargetBuildingId != 0 ? citizen.CurrentTargetBuildingId : citizen.HomeBuildingId;
        return buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(preferredBuildingId, out worldPosition);
    }

    public bool ShouldCitizenBeVisible(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        Camera worldCamera,
        CitizenRecordComponent citizen,
        float maxDistance,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (worldCamera == null || buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return false;
        if (citizen.Status == CitizenStatus.AtRefugeeTent)
            return false;
        if (!TryGetCitizenReferenceAnchorWorldPosition(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, out Vector3 anchorPosition))
            return false;

        Vector3 cameraPosition = worldCamera.transform.position;
        if ((anchorPosition - cameraPosition).sqrMagnitude > maxDistance * maxDistance)
            return false;

        worldPosition = anchorPosition;
        return true;
    }

    public int GetTravelOriginBuildingId(CitizenPopulationStateSystem state, CitizenRecordComponent citizen)
    {
        if (state.TryGetHousehold(citizen.HouseholdId, out CitizenHouseholdRecordComponent household))
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

    public bool TryGetCitizenReferenceAnchorWorldPosition(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRecordComponent citizen,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (state.VisibleCitizensById.TryGetValue(citizen.CitizenId, out VisibleCitizenComponent visibleCitizen) &&
            visibleCitizen != null &&
            visibleCitizen.UnitEntity != Entity.Null &&
            ecsProjection.HasWorld &&
            ecsProjection.EntityManager.Exists(visibleCitizen.UnitEntity) &&
            ecsProjection.EntityManager.HasComponent<LocalTransform>(visibleCitizen.UnitEntity))
        {
            worldPosition = ecsProjection.EntityManager.GetComponentData<LocalTransform>(visibleCitizen.UnitEntity).Position;
            return true;
        }

        int anchorBuildingId = statusTransitionSystem.IsTravelStatus(citizen.Status)
            ? GetTravelOriginBuildingId(state, citizen)
            : citizen.CurrentTargetBuildingId;

        if (anchorBuildingId == 0)
            anchorBuildingId = citizen.HomeBuildingId;
        if (anchorBuildingId == 0)
            return false;
        if (TryGetCitizenBuildingApproachWorldPosition(state, ecsProjection, buildingReadSystem, anchorBuildingId, citizen, out worldPosition))
            return true;

        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(anchorBuildingId, out Vector3 anchorPosition))
            return false;

        worldPosition = ResolveCitizenWorldPosition(citizen, anchorPosition);
        return true;
    }

    public bool TryWorldToCell(CitizenPopulationEcsProjectionSystem ecsProjection, Vector3 worldPosition, out int2 cell)
    {
        cell = default;
        if (!ecsProjection.TryGetGridConfig(out GridConfig grid))
            return false;

        cell = GridUtils.WorldToCell(grid, worldPosition);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    public bool TryGetCitizenMoveGoal(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        goalCell = default;
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;

        return TryGetCitizenSegmentGoalCell(state, ecsProjection, buildingReadSystem, citizen, currentPosition, out goalCell);
    }

    public bool TryGetCitizenSegmentGoalCell(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        goalCell = default;
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;

        int2 currentCell;
        if (!TryWorldToCell(ecsProjection, currentPosition, out currentCell))
            currentCell = default;

        int2 targetCell;
        if (!TryGetCitizenBuildingApproachCell(buildingReadSystem, citizen.CurrentTargetBuildingId, currentCell, out targetCell))
        {
            if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(citizen.CurrentTargetBuildingId, out Vector3 targetPosition))
                return false;

            Vector3 desiredWorld = ResolveCitizenWorldPosition(citizen, targetPosition);
            if (!TryWorldToCell(ecsProjection, desiredWorld, out targetCell))
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

    public float EstimateTravelSeconds(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenRecordComponent citizen,
        int targetBuildingId)
    {
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return 0f;
        if (targetBuildingId == 0)
            return 0f;

        int originBuildingId = citizen.CurrentTargetBuildingId != 0 ? citizen.CurrentTargetBuildingId : citizen.HomeBuildingId;
        if (originBuildingId == 0)
            originBuildingId = GetTravelOriginBuildingId(state, citizen);
        if (originBuildingId == 0)
            return 0f;
        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(originBuildingId, out Vector3 originPosition))
            return 0f;
        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(targetBuildingId, out Vector3 targetPosition))
            return 0f;

        float distanceCells = Vector3.Distance(originPosition, targetPosition);
        return Mathf.Max(1f, distanceCells / DeferredTravelCellsPerSecond);
    }

    public bool TryGetCitizenBuildingApproachCell(
        CitizenBuildingReadSystem buildingReadSystem,
        int buildingId,
        int2 referenceCell,
        out int2 goalCell)
    {
        goalCell = default;
        return buildingReadSystem != null &&
               buildingReadSystem.HasRuntimeBuildingQuery() &&
               buildingReadSystem.TryGetRuntimeBuildingApproachCell(buildingId, new int2(1, 1), referenceCell, out goalCell);
    }

    public bool TryGetCitizenBuildingApproachWorldPosition(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        int buildingId,
        CitizenRecordComponent citizen,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        int2 referenceCell = default;
        if (state.VisibleCitizensById.TryGetValue(citizen.CitizenId, out VisibleCitizenComponent visibleCitizen) &&
            visibleCitizen != null &&
            visibleCitizen.UnitEntity != Entity.Null &&
            ecsProjection.HasWorld &&
            ecsProjection.EntityManager.Exists(visibleCitizen.UnitEntity) &&
            ecsProjection.EntityManager.HasComponent<UnitGrid>(visibleCitizen.UnitEntity))
        {
            referenceCell = ecsProjection.EntityManager.GetComponentData<UnitGrid>(visibleCitizen.UnitEntity).Cell;
        }

        if (!TryGetCitizenBuildingApproachCell(buildingReadSystem, buildingId, referenceCell, out int2 approachCell))
            return false;

        if (!ecsProjection.TryGetGridConfig(out GridConfig grid))
            return false;

        worldPosition = GridUtils.CellToWorldCenter(grid, approachCell);
        return true;
    }

    public Vector3 ResolveCitizenWorldPosition(CitizenRecordComponent citizen, Vector3 anchorPosition)
    {
        int slotIndex = citizen.Gender == CitizenGender.Male ? 0 : 1;
        float xOffset = slotIndex == 0 ? -2.5f : 2.5f;
        float zOffset = ((citizen.HouseholdId & 1) == 0) ? 1.5f : -1.5f;
        return anchorPosition + new Vector3(xOffset, 0f, zOffset);
    }
}
