using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class CitizenVisibleUnitSystem
{
    private const float VisibleCitizenSpawnDistance = 140f;
    private const float VisibleCitizenDespawnDistance = 170f;
    private const float VisibleCitizenArriveDistance = 0.35f;
    private readonly MapSurfaceSpawnGrounding _spawnGroundingSystem = new();

    public delegate bool HandleCitizenDeathAction(int citizenId, string reason);

    public void SyncVisibleCitizens(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
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
                SyncVisibleCitizenTravel(state, ecsProjection, buildingReadSystem, statusTransitionSystem, travelSystem, now, storeCitizen, handleCitizenDeath, citizenId, citizen, visibleCitizen);
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
                citizen,
                worldPosition);
        }
    }

    public void ClearVisibleCitizens(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        if (ecsProjection.HasWorld)
        {
            EntityManager em = ecsProjection.EntityManager;
            EntityCommandBuffer ecb = new(Allocator.Temp);
            bool hasDestroyCommand = false;

            foreach (KeyValuePair<int, VisibleCitizenComponent> pair in state.VisibleCitizensById)
            {
                if (pair.Value != null &&
                    pair.Value.UnitEntity != Entity.Null &&
                    em.Exists(pair.Value.UnitEntity))
                {
                    ecb.DestroyEntity(pair.Value.UnitEntity);
                    hasDestroyCommand = true;
                }
            }

            if (hasDestroyCommand)
                ecb.Playback(em);
            ecb.Dispose();
        }

        state.VisibleCitizensById.Clear();
    }

    private void SyncVisibleCitizenTravel(
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenStatusTransitionSystem statusTransitionSystem,
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
                CitizenMovementCommandSystem.TryEnqueueMoveCommand(ecsProjection.EntityManager, visibleCitizen.UnitEntity, finalGoal);
            }
            else if (travelSystem.TryGetCitizenMoveGoal(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, currentPosition, out int2 retryGoal))
            {
                CitizenMovementCommandSystem.TryEnqueueMoveCommand(ecsProjection.EntityManager, visibleCitizen.UnitEntity, retryGoal);
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
            CitizenMovementCommandSystem.TryEnqueueMoveCommand(ecsProjection.EntityManager, visibleCitizen.UnitEntity, goalCell);
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
            EntityManager em = ecsProjection.EntityManager;
            EntityCommandBuffer ecb = new(Allocator.Temp);
            ecb.DestroyEntity(visibleCitizen.UnitEntity);
            ecb.Playback(em);
            ecb.Dispose();
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
        CitizenRecordComponent citizen,
        Vector3 worldPosition)
    {
        if (prefabSelectionSystem == null || citizenPrefabSystem == null)
            return;

        GameObject prefab = prefabSelectionSystem.GetCitizenPrefab(citizen);
        if (prefab == null || !ecsProjection.HasWorld)
            return;
        if (!citizenPrefabSystem.TryResolveConfiguredUnitPrefabEntity(citizenPrefabContext, prefab, out Entity prefabEntity) || prefabEntity == Entity.Null)
            return;
        if (!travelSystem.TryWorldToCell(ecsProjection, worldPosition, out int2 spawnCell))
            return;
        if (ecsProjection.TryGetGridConfig(out GridConfig grid))
        {
            float3 groundedWorldPosition = worldPosition;
            _spawnGroundingSystem.TryGroundCellCenter(ecsProjection.EntityManager, grid, spawnCell, ref groundedWorldPosition, out _);
            worldPosition = groundedWorldPosition;
        }

        EntityManager em = ecsProjection.EntityManager;
        Entity instance = ecsProjection.EntityManager.Instantiate(prefabEntity);
        EntityCommandBuffer ecb = new(Allocator.Temp);
        bool hasSetupCommand = false;

        if (em.HasComponent<UnitGrid>(instance))
        {
            ecb.SetComponent(instance, new UnitGrid { Cell = spawnCell });
            hasSetupCommand = true;
        }
        if (em.HasComponent<LocalTransform>(instance))
        {
            ecb.SetComponent(instance, LocalTransform.FromPosition(worldPosition));
            hasSetupCommand = true;
        }
        if (em.HasComponent<UnitPrevWorldPos>(instance))
        {
            ecb.SetComponent(instance, new UnitPrevWorldPos { Value = worldPosition });
            hasSetupCommand = true;
        }
        if (em.HasComponent<UnitGridInitialized>(instance))
        {
            ecb.RemoveComponent<UnitGridInitialized>(instance);
            hasSetupCommand = true;
        }
        if (em.HasComponent<UnitMovementBehavior>(instance))
        {
            UnitMovementBehavior movementBehavior = em.GetComponentData<UnitMovementBehavior>(instance);
            movementBehavior.AllowIdleWander = 0;
            ecb.SetComponent(instance, movementBehavior);
            hasSetupCommand = true;
        }
        if (em.HasComponent<UnitCombat>(instance))
        {
            UnitCombat combat = em.GetComponentData<UnitCombat>(instance);
            combat.CanAttack = 0;
            combat.AutoEngage = 0;
            ecb.SetComponent(instance, combat);
            hasSetupCommand = true;
        }
        if (em.HasComponent<Faction>(instance))
        {
            ecb.SetComponent(instance, new Faction { Id = 2 });
            hasSetupCommand = true;
        }
        if (em.HasComponent<UnitTarget>(instance))
        {
            ecb.RemoveComponent<UnitTarget>(instance);
            hasSetupCommand = true;
        }
        if (em.HasComponent<UnitPathRequest>(instance))
        {
            ecb.RemoveComponent<UnitPathRequest>(instance);
            hasSetupCommand = true;
        }
        if (em.HasComponent<UnitPathFollow>(instance))
        {
            ecb.RemoveComponent<UnitPathFollow>(instance);
            hasSetupCommand = true;
        }
        if (em.HasComponent<SelectedUnitTag>(instance))
        {
            ecb.RemoveComponent<SelectedUnitTag>(instance);
            hasSetupCommand = true;
        }
        if (!em.HasComponent<CivilianUnitTag>(instance))
        {
            ecb.AddComponent(instance, new CivilianUnitTag());
            hasSetupCommand = true;
        }

        if (hasSetupCommand)
            ecb.Playback(em);
        ecb.Dispose();

        int2 goalCell = spawnCell;
        if (travelSystem.TryGetCitizenMoveGoal(state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, worldPosition, out int2 resolvedGoalCell))
            goalCell = resolvedGoalCell;
        CitizenMovementCommandSystem.TryEnqueueMoveCommand(ecsProjection.EntityManager, instance, goalCell);

        state.VisibleCitizensById[citizen.CitizenId] = new VisibleCitizenComponent
        {
            CitizenId = citizen.CitizenId,
            UnitEntity = instance,
            GoalCell = goalCell,
            TargetBuildingId = citizen.CurrentTargetBuildingId
        };
    }
}
