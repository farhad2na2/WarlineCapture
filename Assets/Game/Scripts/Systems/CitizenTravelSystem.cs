using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed partial class CitizenTravelSystem : SystemBase
{
    private const float MaxVisibleTravelSegmentDistance = 48f;
    private const float DeferredTravelCellsPerSecond = 10f;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static bool TryGetHouseholdReferenceWorldPosition(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenHouseholdRecordComponent household,
        out Vector3 worldPosition)
    {
        return system != null
            ? system.TryGetHouseholdReferenceWorldPosition(state, ecsProjection, buildingReadSystem, statusTransitionSystem, household, out worldPosition)
            : TryGetHouseholdReferenceWorldPositionState(state, ecsProjection, buildingReadSystem, statusTransitionSystem, household, out worldPosition);
    }

    public bool TryGetHouseholdReferenceWorldPosition(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenHouseholdRecordComponent household,
        out Vector3 worldPosition)
    {
        return TryGetHouseholdReferenceWorldPositionState(
            state,
            ecsProjection,
            buildingReadSystem,
            statusTransitionSystem,
            household,
            out worldPosition);
    }

    public static bool TryGetCitizenReferenceWorldPosition(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int citizenId,
        out Vector3 worldPosition)
    {
        return system != null
            ? system.TryGetCitizenReferenceWorldPosition(state, buildingReadSystem, citizenId, out worldPosition)
            : TryGetCitizenReferenceWorldPositionState(state, buildingReadSystem, citizenId, out worldPosition);
    }

    public bool TryGetCitizenReferenceWorldPosition(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int citizenId,
        out Vector3 worldPosition)
    {
        return TryGetCitizenReferenceWorldPositionState(state, buildingReadSystem, citizenId, out worldPosition);
    }

    public static bool ShouldCitizenBeVisible(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        Camera worldCamera,
        CitizenRecordComponent citizen,
        float maxDistance,
        out Vector3 worldPosition)
    {
        return system != null
            ? system.ShouldCitizenBeVisible(state, ecsProjection, buildingReadSystem, statusTransitionSystem, worldCamera, citizen, maxDistance, out worldPosition)
            : ShouldCitizenBeVisibleState(state, ecsProjection, buildingReadSystem, statusTransitionSystem, worldCamera, citizen, maxDistance, out worldPosition);
    }

    public bool ShouldCitizenBeVisible(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        Camera worldCamera,
        CitizenRecordComponent citizen,
        float maxDistance,
        out Vector3 worldPosition)
    {
        return ShouldCitizenBeVisibleState(
            state,
            ecsProjection,
            buildingReadSystem,
            statusTransitionSystem,
            worldCamera,
            citizen,
            maxDistance,
            out worldPosition);
    }

    public static int GetTravelOriginBuildingId(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenRecordComponent citizen)
    {
        return system != null
            ? system.GetTravelOriginBuildingId(state, citizen)
            : GetTravelOriginBuildingIdState(state, citizen);
    }

    public int GetTravelOriginBuildingId(CitizenPopulationStateCompositionSystemHelper state, CitizenRecordComponent citizen)
    {
        return GetTravelOriginBuildingIdState(state, citizen);
    }

    public static bool TryGetCitizenReferenceAnchorWorldPosition(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRecordComponent citizen,
        out Vector3 worldPosition)
    {
        return system != null
            ? system.TryGetCitizenReferenceAnchorWorldPosition(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, out worldPosition)
            : TryGetCitizenReferenceAnchorWorldPositionState(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, out worldPosition);
    }

    public bool TryGetCitizenReferenceAnchorWorldPosition(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRecordComponent citizen,
        out Vector3 worldPosition)
    {
        return TryGetCitizenReferenceAnchorWorldPositionState(
            state,
            ecsProjection,
            buildingReadSystem,
            statusTransitionSystem,
            citizen,
            out worldPosition);
    }

    public static bool TryWorldToCell(
        CitizenTravelSystem system,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        Vector3 worldPosition,
        out int2 cell)
    {
        return system != null
            ? system.TryWorldToCell(ecsProjection, worldPosition, out cell)
            : TryWorldToCellState(ecsProjection, worldPosition, out cell);
    }

    public bool TryWorldToCell(CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection, Vector3 worldPosition, out int2 cell)
    {
        return TryWorldToCellState(ecsProjection, worldPosition, out cell);
    }

    public static bool TryGetCitizenMoveGoal(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        return system != null
            ? system.TryGetCitizenMoveGoal(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, currentPosition, out goalCell)
            : TryGetCitizenMoveGoalState(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, currentPosition, out goalCell);
    }

    public bool TryGetCitizenMoveGoal(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        return TryGetCitizenMoveGoalState(
            state,
            ecsProjection,
            buildingReadSystem,
            statusTransitionSystem,
            citizen,
            currentPosition,
            out goalCell);
    }

    public static bool TryGetCitizenSegmentGoalCell(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        return system != null
            ? system.TryGetCitizenSegmentGoalCell(state, ecsProjection, buildingReadSystem, citizen, currentPosition, out goalCell)
            : TryGetCitizenSegmentGoalCellState(state, ecsProjection, buildingReadSystem, citizen, currentPosition, out goalCell);
    }

    public bool TryGetCitizenSegmentGoalCell(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        return TryGetCitizenSegmentGoalCellState(
            state,
            ecsProjection,
            buildingReadSystem,
            citizen,
            currentPosition,
            out goalCell);
    }

    public static float EstimateTravelSeconds(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenRecordComponent citizen,
        int targetBuildingId)
    {
        return system != null
            ? system.EstimateTravelSeconds(state, buildingReadSystem, citizen, targetBuildingId)
            : EstimateTravelSecondsState(state, buildingReadSystem, citizen, targetBuildingId);
    }

    public float EstimateTravelSeconds(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenRecordComponent citizen,
        int targetBuildingId)
    {
        return EstimateTravelSecondsState(state, buildingReadSystem, citizen, targetBuildingId);
    }

    public static bool TryGetCitizenBuildingApproachCell(
        CitizenTravelSystem system,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int buildingId,
        int2 referenceCell,
        out int2 goalCell)
    {
        return system != null
            ? system.TryGetCitizenBuildingApproachCell(buildingReadSystem, buildingId, referenceCell, out goalCell)
            : TryGetCitizenBuildingApproachCellState(buildingReadSystem, buildingId, referenceCell, out goalCell);
    }

    public bool TryGetCitizenBuildingApproachCell(
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int buildingId,
        int2 referenceCell,
        out int2 goalCell)
    {
        return TryGetCitizenBuildingApproachCellState(buildingReadSystem, buildingId, referenceCell, out goalCell);
    }

    public static bool TryGetCitizenBuildingApproachWorldPosition(
        CitizenTravelSystem system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int buildingId,
        CitizenRecordComponent citizen,
        out Vector3 worldPosition)
    {
        return system != null
            ? system.TryGetCitizenBuildingApproachWorldPosition(state, ecsProjection, buildingReadSystem, buildingId, citizen, out worldPosition)
            : TryGetCitizenBuildingApproachWorldPositionState(state, ecsProjection, buildingReadSystem, buildingId, citizen, out worldPosition);
    }

    public bool TryGetCitizenBuildingApproachWorldPosition(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int buildingId,
        CitizenRecordComponent citizen,
        out Vector3 worldPosition)
    {
        return TryGetCitizenBuildingApproachWorldPositionState(
            state,
            ecsProjection,
            buildingReadSystem,
            buildingId,
            citizen,
            out worldPosition);
    }

    public static Vector3 ResolveCitizenWorldPosition(
        CitizenTravelSystem system,
        CitizenRecordComponent citizen,
        Vector3 anchorPosition)
    {
        return system != null
            ? system.ResolveCitizenWorldPosition(citizen, anchorPosition)
            : ResolveCitizenWorldPositionState(citizen, anchorPosition);
    }

    public Vector3 ResolveCitizenWorldPosition(CitizenRecordComponent citizen, Vector3 anchorPosition)
    {
        return ResolveCitizenWorldPositionState(citizen, anchorPosition);
    }

    private static bool TryGetHouseholdReferenceWorldPositionState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenHouseholdRecordComponent household,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;
        if (buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(household.HomeBuildingId, out worldPosition))
            return true;

        if (TryGetCitizenReferenceWorldPositionState(state, buildingReadSystem, household.MaleCitizenId, out worldPosition))
            return true;
        if (TryGetCitizenReferenceWorldPositionState(state, buildingReadSystem, household.FemaleCitizenId, out worldPosition))
            return true;

        return false;
    }

    private static bool TryGetCitizenReferenceWorldPositionState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
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

    private static bool ShouldCitizenBeVisibleState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
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
        if (!TryGetCitizenReferenceAnchorWorldPositionState(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, out Vector3 anchorPosition))
            return false;

        Vector3 cameraPosition = worldCamera.transform.position;
        if ((anchorPosition - cameraPosition).sqrMagnitude > maxDistance * maxDistance)
            return false;

        worldPosition = anchorPosition;
        return true;
    }

    private static int GetTravelOriginBuildingIdState(CitizenPopulationStateCompositionSystemHelper state, CitizenRecordComponent citizen)
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

    private static bool TryGetCitizenReferenceAnchorWorldPositionState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
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

        int anchorBuildingId = CitizenStatusTransitionSystem.IsTravelStatus(statusTransitionSystem, citizen.Status)
            ? GetTravelOriginBuildingIdState(state, citizen)
            : citizen.CurrentTargetBuildingId;

        if (anchorBuildingId == 0)
            anchorBuildingId = citizen.HomeBuildingId;
        if (anchorBuildingId == 0)
            return false;
        if (TryGetCitizenBuildingApproachWorldPositionState(state, ecsProjection, buildingReadSystem, anchorBuildingId, citizen, out worldPosition))
            return true;

        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(anchorBuildingId, out Vector3 anchorPosition))
            return false;

        worldPosition = ResolveCitizenWorldPositionState(citizen, anchorPosition);
        return true;
    }

    private static bool TryWorldToCellState(CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection, Vector3 worldPosition, out int2 cell)
    {
        cell = default;
        if (!ecsProjection.TryGetGridConfig(out GridConfig grid))
            return false;

        cell = GridUtils.WorldToCell(grid, worldPosition);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    private static bool TryGetCitizenMoveGoalState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        goalCell = default;
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;

        return TryGetCitizenSegmentGoalCellState(state, ecsProjection, buildingReadSystem, citizen, currentPosition, out goalCell);
    }

    private static bool TryGetCitizenSegmentGoalCellState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenRecordComponent citizen,
        Vector3 currentPosition,
        out int2 goalCell)
    {
        goalCell = default;
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return false;

        int2 currentCell;
        if (!TryWorldToCellState(ecsProjection, currentPosition, out currentCell))
            currentCell = default;

        int2 targetCell;
        if (!TryGetCitizenBuildingApproachCellState(buildingReadSystem, citizen.CurrentTargetBuildingId, currentCell, out targetCell))
        {
            if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(citizen.CurrentTargetBuildingId, out Vector3 targetPosition))
                return false;

            Vector3 desiredWorld = ResolveCitizenWorldPositionState(citizen, targetPosition);
            if (!TryWorldToCellState(ecsProjection, desiredWorld, out targetCell))
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

    private static float EstimateTravelSecondsState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenRecordComponent citizen,
        int targetBuildingId)
    {
        if (buildingReadSystem == null || !buildingReadSystem.HasRuntimeBuildingQuery())
            return 0f;
        if (targetBuildingId == 0)
            return 0f;

        int originBuildingId = citizen.CurrentTargetBuildingId != 0 ? citizen.CurrentTargetBuildingId : citizen.HomeBuildingId;
        if (originBuildingId == 0)
            originBuildingId = GetTravelOriginBuildingIdState(state, citizen);
        if (originBuildingId == 0)
            return 0f;
        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(originBuildingId, out Vector3 originPosition))
            return 0f;
        if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(targetBuildingId, out Vector3 targetPosition))
            return 0f;

        float distanceCells = Vector3.Distance(originPosition, targetPosition);
        return Mathf.Max(1f, distanceCells / DeferredTravelCellsPerSecond);
    }

    private static bool TryGetCitizenBuildingApproachCellState(
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int buildingId,
        int2 referenceCell,
        out int2 goalCell)
    {
        goalCell = default;
        return buildingReadSystem != null &&
               buildingReadSystem.HasRuntimeBuildingQuery() &&
               buildingReadSystem.TryGetRuntimeBuildingApproachCell(buildingId, new int2(1, 1), referenceCell, out goalCell);
    }

    private static bool TryGetCitizenBuildingApproachWorldPositionState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
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

        if (!TryGetCitizenBuildingApproachCellState(buildingReadSystem, buildingId, referenceCell, out int2 approachCell))
            return false;

        if (!ecsProjection.TryGetGridConfig(out GridConfig grid))
            return false;

        worldPosition = GridUtils.CellToWorldCenter(grid, approachCell);
        return true;
    }

    private static Vector3 ResolveCitizenWorldPositionState(CitizenRecordComponent citizen, Vector3 anchorPosition)
    {
        int slotIndex = citizen.Gender == CitizenGender.Male ? 0 : 1;
        float xOffset = slotIndex == 0 ? -2.5f : 2.5f;
        float zOffset = ((citizen.HouseholdId & 1) == 0) ? 1.5f : -1.5f;
        return anchorPosition + new Vector3(xOffset, 0f, zOffset);
    }
}
