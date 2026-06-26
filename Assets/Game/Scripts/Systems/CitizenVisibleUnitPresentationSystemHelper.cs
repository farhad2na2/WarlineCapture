using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class CitizenVisibleUnitPresentationSystemHelper
{
    private const float VisibleCitizenSpawnDistance = 140f;
    private const float VisibleCitizenDespawnDistance = 170f;
    private const float VisibleCitizenArriveDistance = 0.35f;
    private const byte VisibleCitizenOwnerFactionId = 2;
    private readonly MapSurfaceSpawnGrounding _spawnGroundingSystem = new();

    public delegate bool HandleCitizenDeathAction(int citizenId, string reason);

    public void SyncVisibleCitizens(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionCompositionSystemHelper statusTransitionSystem,
        CitizenPrefabSystem citizenPrefabSystem,
        CitizenPrefabSystem.Context citizenPrefabContext,
        CitizenPrefabSelectionSystem prefabSelectionSystem,
        CitizenPrefabSelectionSystem.State prefabSelectionState,
        CitizenTravelSystem travelSystem,
        Camera worldCamera,
        bool hasCitizenData,
        float now,
        CitizenStatusTransitionCompositionSystemHelper.StoreCitizenAction storeCitizen,
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
                !CitizenTravelSystem.ShouldCitizenBeVisible(travelSystem, state, ecsProjection, buildingReadSystem, statusTransitionSystem, worldCamera, citizen, VisibleCitizenDespawnDistance, out Vector3 worldPosition))
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
            if (!CitizenTravelSystem.ShouldCitizenBeVisible(travelSystem, state, ecsProjection, buildingReadSystem, statusTransitionSystem, worldCamera, citizen, VisibleCitizenSpawnDistance, out Vector3 worldPosition))
                continue;

            SpawnVisibleCitizen(
                state,
                ecsProjection,
                citizenPrefabSystem,
                citizenPrefabContext,
                prefabSelectionSystem,
                prefabSelectionState,
                travelSystem,
                buildingReadSystem,
                statusTransitionSystem,
                citizen,
                worldPosition);
        }
    }

    public void ClearVisibleCitizens(CitizenPopulationStateCompositionSystemHelper state, CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection)
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
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionCompositionSystemHelper statusTransitionSystem,
        CitizenTravelSystem travelSystem,
        float now,
        CitizenStatusTransitionCompositionSystemHelper.StoreCitizenAction storeCitizen,
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
            CitizenStatusTransitionCompositionSystemHelper.TryResolveCitizenArrival(statusTransitionSystem, state, citizenId, now, storeCitizen);
            return;
        }

        if (CitizenTravelSystem.TryGetCitizenBuildingApproachCell(travelSystem, buildingReadSystem, citizen.CurrentTargetBuildingId, currentCell, out int2 finalApproachGoal))
        {
            int dx = math.abs(currentCell.x - finalApproachGoal.x);
            int dy = math.abs(currentCell.y - finalApproachGoal.y);
            if (math.max(dx, dy) <= 2)
            {
                CitizenStatusTransitionCompositionSystemHelper.TryResolveCitizenArrival(statusTransitionSystem, state, citizenId, now, storeCitizen);
                return;
            }
        }

        if (CitizenStatusTransitionCompositionSystemHelper.IsTravelStatus(statusTransitionSystem, citizen.Status) && !hasPathFollow && !hasPathRequest)
        {
            if (hasLongMove)
            {
                int2 finalGoal = ecsProjection.EntityManager.GetComponentData<UnitLongDistanceMove>(visibleCitizen.UnitEntity).FinalGoal;
                CitizenMovementCommandSystem.TryEnqueueMoveCommand(ecsProjection.EntityManager, visibleCitizen.UnitEntity, finalGoal);
            }
            else if (CitizenTravelSystem.TryGetCitizenMoveGoal(travelSystem, state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, currentPosition, out int2 retryGoal))
            {
                CitizenMovementCommandSystem.TryEnqueueMoveCommand(ecsProjection.EntityManager, visibleCitizen.UnitEntity, retryGoal);
                visibleCitizen.GoalCell = retryGoal;
                visibleCitizen.TargetBuildingId = citizen.CurrentTargetBuildingId;
                state.VisibleCitizensById[citizenId] = visibleCitizen;
            }
        }

        bool segmentReached = currentCell.Equals(visibleCitizen.GoalCell);
        if ((visibleCitizen.TargetBuildingId != citizen.CurrentTargetBuildingId || segmentReached) &&
            CitizenTravelSystem.TryGetCitizenMoveGoal(travelSystem, state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, currentPosition, out int2 goalCell) &&
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
                    CitizenStatusTransitionCompositionSystemHelper.TryResolveCitizenArrival(statusTransitionSystem, state, citizenId, now, storeCitizen);
                }
                else
                {
                    Vector3 finalWorld = CitizenTravelSystem.ResolveCitizenWorldPosition(travelSystem, citizen, finalTargetPosition);
                    if ((finalWorld - currentPosition).sqrMagnitude <= VisibleCitizenArriveDistance * VisibleCitizenArriveDistance)
                        CitizenStatusTransitionCompositionSystemHelper.TryResolveCitizenArrival(statusTransitionSystem, state, citizenId, now, storeCitizen);
                }
            }
        }
    }

    public void RemoveVisibleCitizen(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
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
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenPrefabSystem citizenPrefabSystem,
        CitizenPrefabSystem.Context citizenPrefabContext,
        CitizenPrefabSelectionSystem prefabSelectionSystem,
        CitizenPrefabSelectionSystem.State prefabSelectionState,
        CitizenTravelSystem travelSystem,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenStatusTransitionCompositionSystemHelper statusTransitionSystem,
        CitizenRecordComponent citizen,
        Vector3 worldPosition)
    {
        if (!prefabSelectionSystem.TryGetCitizenPrefabSourceKey(prefabSelectionState, citizen, out FixedString64Bytes sourceKey) ||
            !ecsProjection.HasWorld)
            return;
        if (!citizenPrefabSystem.TryResolveConfiguredUnitPrefabEntity(citizenPrefabContext, sourceKey, out Entity prefabEntity) || prefabEntity == Entity.Null)
            return;
        if (!CitizenTravelSystem.TryWorldToCell(travelSystem, ecsProjection, worldPosition, out int2 spawnCell))
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
            ecb.SetComponent(instance, new Faction { Id = VisibleCitizenOwnerFactionId });
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
        if (CitizenTravelSystem.TryGetCitizenMoveGoal(travelSystem, state, ecsProjection, buildingReadSystem, statusTransitionSystem, citizen, worldPosition, out int2 resolvedGoalCell))
            goalCell = resolvedGoalCell;
        SetOrAddComponent(em, instance, new UnitSourcePrefabKey { Value = sourceKey });
        SetOrAddComponent(em, instance, new CitizenVisibleUnitState
        {
            CitizenId = citizen.CitizenId,
            SourceKey = sourceKey,
            OwnerFactionId = VisibleCitizenOwnerFactionId,
            LifeState = citizen.LifeState,
            Status = citizen.Status,
            TargetBuildingId = citizen.CurrentTargetBuildingId,
            GoalCell = goalCell
        });
        CitizenMovementCommandSystem.TryEnqueueMoveCommand(ecsProjection.EntityManager, instance, goalCell);

        state.VisibleCitizensById[citizen.CitizenId] = new VisibleCitizenComponent
        {
            CitizenId = citizen.CitizenId,
            UnitEntity = instance,
            GoalCell = goalCell,
            TargetBuildingId = citizen.CurrentTargetBuildingId
        };
    }

    private static void SetOrAddComponent<T>(EntityManager em, Entity entity, T component)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.SetComponentData(entity, component);
        else
            em.AddComponentData(entity, component);
    }
}
