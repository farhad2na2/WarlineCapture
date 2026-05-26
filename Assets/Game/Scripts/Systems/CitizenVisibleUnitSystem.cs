using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class CitizenVisibleUnitSystem
{
    private const float VisibleCitizenSpawnDistance = 140f;
    private const float VisibleCitizenDespawnDistance = 170f;
    private const float VisibleCitizenArriveDistance = 0.35f;

    public delegate bool HandleCitizenDeathAction(int citizenId, string reason);

    public void SyncVisibleCitizens(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenMovementCommandSystem movementCommandSystem,
        CitizenPrefabSystem citizenPrefabSystem,
        CitizenPrefabSystem.Context citizenPrefabContext,
        CitizenPrefabSelectionSystem prefabSelectionSystem,
        CitizenTravelSystem travelSystem,
        Camera worldCamera,
        bool hasCitizenData,
        float now,
        CitizenStatusTransitionSystem.StoreCitizenAction storeCitizen,
        HandleCitizenDeathAction handleCitizenDeath)
    {
        if (worldCamera == null || !hasCitizenData || !ecsProjection.HasWorld)
            return;

        state.ScratchVisibleCitizenIds.Clear();
        foreach (int citizenId in state.VisibleCitizensById.Keys)
            state.ScratchVisibleCitizenIds.Add(citizenId);

        for (int i = 0; i < state.ScratchVisibleCitizenIds.Count; i++)
        {
            int citizenId = state.ScratchVisibleCitizenIds[i];
            if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen) ||
                !travelSystem.ShouldCitizenBeVisible(state, ecsProjection, buildingReadSystem, statusTransitionSystem, worldCamera, citizen, VisibleCitizenDespawnDistance, out Vector3 worldPosition))
            {
                RemoveVisibleCitizen(state, ecsProjection, citizenId);
                continue;
            }

            if (state.VisibleCitizensById.TryGetValue(citizenId, out VisibleCitizenComponent visibleCitizen))
                SyncVisibleCitizenTravel(state, ecsProjection, buildingReadSystem, statusTransitionSystem, movementCommandSystem, travelSystem, now, storeCitizen, handleCitizenDeath, citizenId, citizen, visibleCitizen);
        }

        state.PopulateCitizenIds();
        for (int i = 0; i < state.ScratchCitizenIds.Count; i++)
        {
            int citizenId = state.ScratchCitizenIds[i];
            if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
                continue;
            if (state.VisibleCitizensById.ContainsKey(citizenId))
                continue;
            if (!travelSystem.ShouldCitizenBeVisible(state, ecsProjection, buildingReadSystem, statusTransitionSystem, worldCamera, citizen, VisibleCitizenSpawnDistance, out Vector3 worldPosition))
                continue;

            SpawnVisibleCitizen(
                state,
                ecsProjection,
                citizenPrefabSystem,
                citizenPrefabContext,
                prefabSelectionSystem,
                travelSystem,
                buildingReadSystem,
                statusTransitionSystem,
                movementCommandSystem,
                citizen,
                worldPosition);
        }
    }

    public void ClearVisibleCitizens(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        foreach (KeyValuePair<int, VisibleCitizenComponent> pair in state.VisibleCitizensById)
        {
            if (pair.Value != null &&
                pair.Value.UnitEntity != Entity.Null &&
                ecsProjection.HasWorld &&
                ecsProjection.EntityManager.Exists(pair.Value.UnitEntity))
            {
                ecsProjection.EntityManager.DestroyEntity(pair.Value.UnitEntity);
            }
        }

        state.VisibleCitizensById.Clear();
    }

    private void SyncVisibleCitizenTravel(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenMovementCommandSystem movementCommandSystem,
        CitizenTravelSystem travelSystem,
        float now,
        CitizenStatusTransitionSystem.StoreCitizenAction storeCitizen,
        HandleCitizenDeathAction handleCitizenDeath,
        int citizenId,
        CitizenRecordComponent citizen,
        VisibleCitizenComponent visibleCitizen)
    {
        if (visibleCitizen.UnitEntity == Entity.Null || !ecsProjection.EntityManager.Exists(visibleCitizen.UnitEntity))
        {
            handleCitizenDeath(citizenId, "unit-destroyed");
            return;
        }

        if (!ecsProjection.EntityManager.HasComponent<LocalTransform>(visibleCitizen.UnitEntity))
        {
            RemoveVisibleCitizen(state, ecsProjection, citizenId);
            return;
        }

        Vector3 currentPosition = ecsProjection.EntityManager.GetComponentData<LocalTransform>(visibleCitizen.UnitEntity).Position;
        bool hasPathFollow = ecsProjection.EntityManager.HasComponent<UnitPathFollow>(visibleCitizen.UnitEntity);
        bool hasPathRequest = ecsProjection.EntityManager.HasComponent<UnitPathRequest>(visibleCitizen.UnitEntity);
        bool hasLongMove = ecsProjection.EntityManager.HasComponent<UnitLongDistanceMove>(visibleCitizen.UnitEntity);
        int2 currentCell = ecsProjection.EntityManager.HasComponent<UnitGrid>(visibleCitizen.UnitEntity)
            ? ecsProjection.EntityManager.GetComponentData<UnitGrid>(visibleCitizen.UnitEntity).Cell
            : default;

        if (buildingReadSystem.IsRuntimeBuildingApproachCell(citizen.CurrentTargetBuildingId, currentCell, new int2(1, 1)))
        {
            statusTransitionSystem.TryResolveCitizenArrival(state, citizenId, now, storeCitizen);
            return;
        }

        if (travelSystem.TryGetCitizenBuildingApproachCell(buildingReadSystem, citizen.CurrentTargetBuildingId, currentCell, out int2 finalApproachGoal))
        {
            int dx = math.abs(currentCell.x - finalApproachGoal.x);
            int dy = math.abs(currentCell.y - finalApproachGoal.y);
            if (math.max(dx, dy) <= 2)
            {
                statusTransitionSystem.TryResolveCitizenArrival(state, citizenId, now, storeCitizen);
                return;
            }
        }

        if (statusTransitionSystem.IsTravelStatus(citizen.Status) && !hasPathFollow && !hasPathRequest)
        {
            if (hasLongMove)
            {
                int2 finalGoal = ecsProjection.EntityManager.GetComponentData<UnitLongDistanceMove>(visibleCitizen.UnitEntity).FinalGoal;
                movementCommandSystem.IssueCitizenMoveCommand(ecsProjection, visibleCitizen.UnitEntity, finalGoal);
            }
            else if (travelSystem.TryGetCitizenMoveGoal(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, currentPosition, out int2 retryGoal))
            {
                movementCommandSystem.IssueCitizenMoveCommand(ecsProjection, visibleCitizen.UnitEntity, retryGoal);
                visibleCitizen.GoalCell = retryGoal;
                visibleCitizen.TargetBuildingId = citizen.CurrentTargetBuildingId;
                state.VisibleCitizensById[citizenId] = visibleCitizen;
            }
        }

        bool segmentReached = currentCell.Equals(visibleCitizen.GoalCell);
        if ((visibleCitizen.TargetBuildingId != citizen.CurrentTargetBuildingId || segmentReached) &&
            travelSystem.TryGetCitizenMoveGoal(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, currentPosition, out int2 goalCell) &&
            !currentCell.Equals(goalCell))
        {
            movementCommandSystem.IssueCitizenMoveCommand(ecsProjection, visibleCitizen.UnitEntity, goalCell);
            visibleCitizen.GoalCell = goalCell;
            visibleCitizen.TargetBuildingId = citizen.CurrentTargetBuildingId;
            state.VisibleCitizensById[citizenId] = visibleCitizen;
        }

        if (currentCell.Equals(visibleCitizen.GoalCell))
        {
            if (!buildingReadSystem.IsRuntimeBuildingApproachCell(citizen.CurrentTargetBuildingId, currentCell, new int2(1, 1)))
            {
                if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(citizen.CurrentTargetBuildingId, out Vector3 finalTargetPosition))
                {
                    statusTransitionSystem.TryResolveCitizenArrival(state, citizenId, now, storeCitizen);
                }
                else
                {
                    Vector3 finalWorld = travelSystem.ResolveCitizenWorldPosition(citizen, finalTargetPosition);
                    if ((finalWorld - currentPosition).sqrMagnitude <= VisibleCitizenArriveDistance * VisibleCitizenArriveDistance)
                        statusTransitionSystem.TryResolveCitizenArrival(state, citizenId, now, storeCitizen);
                }
            }
        }
    }

    public void RemoveVisibleCitizen(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        int citizenId)
    {
        if (!state.VisibleCitizensById.TryGetValue(citizenId, out VisibleCitizenComponent visibleCitizen))
            return;

        if (visibleCitizen != null &&
            visibleCitizen.UnitEntity != Entity.Null &&
            ecsProjection.HasWorld &&
            ecsProjection.EntityManager.Exists(visibleCitizen.UnitEntity))
        {
            ecsProjection.EntityManager.DestroyEntity(visibleCitizen.UnitEntity);
        }

        state.VisibleCitizensById.Remove(citizenId);
    }

    public void SpawnVisibleCitizen(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenPrefabSystem citizenPrefabSystem,
        CitizenPrefabSystem.Context citizenPrefabContext,
        CitizenPrefabSelectionSystem prefabSelectionSystem,
        CitizenTravelSystem travelSystem,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenMovementCommandSystem movementCommandSystem,
        CitizenRecordComponent citizen,
        Vector3 worldPosition)
    {
        GameObject prefab = prefabSelectionSystem.GetCitizenPrefab(citizen);
        if (prefab == null || !ecsProjection.HasWorld)
            return;
        if (!citizenPrefabSystem.TryResolveConfiguredUnitPrefabEntity(citizenPrefabContext, prefab, out Entity prefabEntity) || prefabEntity == Entity.Null)
            return;
        if (!travelSystem.TryWorldToCell(ecsProjection, worldPosition, out int2 spawnCell))
            return;

        Entity instance = ecsProjection.EntityManager.Instantiate(prefabEntity);
        if (ecsProjection.EntityManager.HasComponent<UnitGrid>(instance))
            ecsProjection.EntityManager.SetComponentData(instance, new UnitGrid { Cell = spawnCell });
        if (ecsProjection.EntityManager.HasComponent<LocalTransform>(instance))
            ecsProjection.EntityManager.SetComponentData(instance, LocalTransform.FromPosition(worldPosition));
        if (ecsProjection.EntityManager.HasComponent<UnitPrevWorldPos>(instance))
            ecsProjection.EntityManager.SetComponentData(instance, new UnitPrevWorldPos { Value = worldPosition });
        if (ecsProjection.EntityManager.HasComponent<UnitGridInitialized>(instance))
            ecsProjection.EntityManager.RemoveComponent<UnitGridInitialized>(instance);
        if (ecsProjection.EntityManager.HasComponent<UnitMovementBehavior>(instance))
        {
            UnitMovementBehavior movementBehavior = ecsProjection.EntityManager.GetComponentData<UnitMovementBehavior>(instance);
            movementBehavior.AllowIdleWander = 0;
            ecsProjection.EntityManager.SetComponentData(instance, movementBehavior);
        }
        if (ecsProjection.EntityManager.HasComponent<UnitCombat>(instance))
        {
            UnitCombat combat = ecsProjection.EntityManager.GetComponentData<UnitCombat>(instance);
            combat.CanAttack = 0;
            combat.AutoEngage = 0;
            ecsProjection.EntityManager.SetComponentData(instance, combat);
        }
        if (ecsProjection.EntityManager.HasComponent<Faction>(instance))
            ecsProjection.EntityManager.SetComponentData(instance, new Faction { Id = 2 });
        if (ecsProjection.EntityManager.HasComponent<UnitTarget>(instance))
            ecsProjection.EntityManager.RemoveComponent<UnitTarget>(instance);
        if (ecsProjection.EntityManager.HasComponent<UnitPathRequest>(instance))
            ecsProjection.EntityManager.RemoveComponent<UnitPathRequest>(instance);
        if (ecsProjection.EntityManager.HasComponent<UnitPathFollow>(instance))
            ecsProjection.EntityManager.RemoveComponent<UnitPathFollow>(instance);
        if (ecsProjection.EntityManager.HasComponent<SelectedUnitTag>(instance))
            ecsProjection.EntityManager.RemoveComponent<SelectedUnitTag>(instance);
        if (!ecsProjection.EntityManager.HasComponent<CivilianUnitTag>(instance))
            ecsProjection.EntityManager.AddComponentData(instance, new CivilianUnitTag());

        int2 goalCell = spawnCell;
        if (travelSystem.TryGetCitizenMoveGoal(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, worldPosition, out int2 resolvedGoalCell))
            goalCell = resolvedGoalCell;
        movementCommandSystem.IssueCitizenMoveCommand(ecsProjection, instance, goalCell);

        state.VisibleCitizensById[citizen.CitizenId] = new VisibleCitizenComponent
        {
            CitizenId = citizen.CitizenId,
            UnitEntity = instance,
            GoalCell = goalCell,
            TargetBuildingId = citizen.CurrentTargetBuildingId
        };
    }
}
