using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(AITargetingSystem))]
[UpdateBefore(typeof(UnitEngagementSystem))]
public partial struct AICombatOrderSystem : ISystem
{
    private const float OrderRefreshSeconds = 2f;
    private EntityQuery _buildingPlacementRuntimeQuery;

    public void OnCreate(ref SystemState state)
    {
        _buildingPlacementRuntimeQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
        state.RequireForUpdate(_buildingPlacementRuntimeQuery);
        state.RequireForUpdate<AISquad>();
        state.RequireForUpdate<AISquadUnit>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!InitialUnitsRuntimeState.PlayRequested)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        EntityManager em = state.EntityManager;
        EntityCommandBuffer ecb = new(Allocator.Temp);
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        NativeArray<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true).ToNativeArray(Allocator.Temp)
            : default;
        BuildingPlacementSystem buildingPlacement = GetBuildingPlacement(ref state);

        EntityQuery squadQuery = em.CreateEntityQuery(ComponentType.ReadWrite<AISquad>(), ComponentType.ReadOnly<AISquadUnit>());
        using NativeArray<Entity> squadEntities = squadQuery.ToEntityArray(Allocator.Temp);
        squadQuery.Dispose();

        for (int i = 0; i < squadEntities.Length; i++)
        {
            Entity squadEntity = squadEntities[i];
            AISquad squad = em.GetComponentData<AISquad>(squadEntity);
            if (!IsFactionAIControlled(squad.FactionId, hasControls, controls))
                continue;

            if (squad.TargetEntity == Entity.Null ||
                !em.Exists(squad.TargetEntity) ||
                !em.HasComponent<UnitHealth>(squad.TargetEntity) ||
                em.GetComponentData<UnitHealth>(squad.TargetEntity).Current <= 0)
            {
                continue;
            }

            if (now - squad.LastOrderTime < OrderRefreshSeconds && CountMembersNeedingOrder(em, squadEntity, squad) == 0)
                continue;

            float3 targetPosition = ResolveTargetPosition(em, squad.TargetEntity, squad.TargetCell);
            DynamicBuffer<AISquadUnit> members = em.GetBuffer<AISquadUnit>(squadEntity);
            int issued = 0;
            for (int memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                Entity unit = members[memberIndex].Unit;
                if (!CanReceiveCombatOrder(em, unit, squad.FactionId))
                    continue;

                IssueEngageOrder(em, ecb, buildingPlacement, unit, squad.TargetEntity, squad.TargetCell, targetPosition);
                issued++;
            }

            if (issued <= 0)
                continue;

            squad.LastOrderTime = now;
            squad.LastLogTime = now;
            em.SetComponentData(squadEntity, squad);
            AILog.Log($"[AICombat] faction={squad.FactionId} squad={squad.SquadId} order=Attack target={squad.TargetEntity} units={issued}");
        }

        if (controls.IsCreated)
            controls.Dispose();
        ecb.Playback(em);
        ecb.Dispose();
    }

    private static int CountMembersNeedingOrder(EntityManager em, Entity squadEntity, AISquad squad)
    {
        if (!em.HasBuffer<AISquadUnit>(squadEntity))
            return 0;

        DynamicBuffer<AISquadUnit> members = em.GetBuffer<AISquadUnit>(squadEntity);
        int count = 0;
        for (int i = 0; i < members.Length; i++)
        {
            Entity unit = members[i].Unit;
            if (!CanReceiveCombatOrder(em, unit, squad.FactionId))
                continue;
            if (em.HasComponent<BaseBreachOrder>(unit) &&
                em.GetComponentData<BaseBreachOrder>(unit).FinalTarget == squad.TargetEntity)
                continue;
            if (!em.HasComponent<EngageTarget>(unit))
            {
                count++;
                continue;
            }

            EngageTarget engage = em.GetComponentData<EngageTarget>(unit);
            if (engage.Target == squad.TargetEntity)
                continue;

            count++;
        }

        return count;
    }

    private static bool CanReceiveCombatOrder(EntityManager em, Entity unit, byte factionId)
    {
        if (unit == Entity.Null ||
            !em.Exists(unit) ||
            !em.HasComponent<Faction>(unit) ||
            em.GetComponentData<Faction>(unit).Id != factionId ||
            !em.HasComponent<AIControlledTag>(unit) ||
            !em.HasComponent<UnitHealth>(unit) ||
            em.GetComponentData<UnitHealth>(unit).Current <= 0 ||
            !em.HasComponent<UnitCombat>(unit) ||
            !em.HasComponent<UnitAttack>(unit) ||
            !em.HasComponent<LocalTransform>(unit) ||
            em.HasComponent<StaticGridBlocker>(unit))
        {
            return false;
        }

        UnitCombat combat = em.GetComponentData<UnitCombat>(unit);
        return combat.CanAttack != 0;
    }

    private static float3 ResolveTargetPosition(EntityManager em, Entity target, int2 targetCell)
    {
        if (em.HasComponent<LocalTransform>(target))
            return em.GetComponentData<LocalTransform>(target).Position;

        return new float3(targetCell.x, 0f, targetCell.y);
    }

    private static void IssueEngageOrder(
        EntityManager em,
        EntityCommandBuffer ecb,
        BuildingPlacementSystem buildingPlacement,
        Entity unit,
        Entity target,
        int2 targetCell,
        float3 targetPosition)
    {
        Entity engageTarget = target;
        int2 engageCell = targetCell;
        float3 engagePosition = targetPosition;
        bool issuedBreachOrder = false;

        if (buildingPlacement != null &&
            em.HasComponent<Faction>(unit) &&
            em.HasComponent<UnitGrid>(unit))
        {
            byte attackerFaction = em.GetComponentData<Faction>(unit).Id;
            int2 attackerCell = em.GetComponentData<UnitGrid>(unit).Cell;
            if (buildingPlacement.TryResolveBaseBreachTarget(
                    attackerFaction,
                    target,
                    targetCell,
                    attackerCell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition,
                    out _))
            {
                engageTarget = breachTarget;
                engageCell = breachCell;
                engagePosition = breachPosition;
                issuedBreachOrder = true;
            }
        }

        if (issuedBreachOrder)
        {
            RemoveIfPresent<EngageTarget>(em, ecb, unit);
            SetPathRequest(em, ecb, unit, engageCell);
            if (!em.HasComponent<ManualMoveOrderTag>(unit))
                ecb.AddComponent<ManualMoveOrderTag>(unit);
            if (!em.HasComponent<AICombatOrderTag>(unit))
                ecb.AddComponent<AICombatOrderTag>(unit);
        }
        else
        {
            EngageTarget order = new()
            {
                Target = engageTarget,
                Cell = engageCell,
                Position = engagePosition,
                IsCommanded = 1
            };

            if (em.HasComponent<EngageTarget>(unit))
                em.SetComponentData(unit, order);
            else
                ecb.AddComponent(unit, order);
            if (!em.HasComponent<AICombatOrderTag>(unit))
                ecb.AddComponent<AICombatOrderTag>(unit);
        }

        if (issuedBreachOrder)
        {
            BaseBreachOrder breachOrder = new()
            {
                FinalTarget = target,
                FinalCell = targetCell,
                FinalPosition = targetPosition,
                BreachTarget = engageTarget,
                BreachCell = engageCell,
                BreachPosition = engagePosition,
                Stage = BaseBreachOrder.StageMovingToEnemyBreach,
                IsCommanded = 1
            };

            if (em.HasComponent<BaseBreachOrder>(unit))
                em.SetComponentData(unit, breachOrder);
            else
                ecb.AddComponent(unit, breachOrder);
        }
        else
        {
            RemoveIfPresent<BaseBreachOrder>(em, ecb, unit);
        }

        RemoveIfPresent<ManualMoveGroupMemberTag>(em, ecb, unit);
        if (!issuedBreachOrder)
            RemoveIfPresent<ManualMoveOrderTag>(em, ecb, unit);
        RemoveIfPresent<AutoWanderMoveTag>(em, ecb, unit);
        RemoveIfPresent<UnitPathFollow>(em, ecb, unit);
        RemoveIfPresent<UnitPathRange>(em, ecb, unit);
        if (!issuedBreachOrder)
        {
            RemoveIfPresent<UnitPathRequest>(em, ecb, unit);
            RemoveIfPresent<UnitTarget>(em, ecb, unit);
        }
    }

    private static void SetPathRequest(EntityManager em, EntityCommandBuffer ecb, Entity entity, int2 goal)
    {
        if (em.HasComponent<UnitTarget>(entity))
            em.SetComponentData(entity, new UnitTarget { Cell = goal });
        else
            ecb.AddComponent(entity, new UnitTarget { Cell = goal });

        if (em.HasComponent<UnitPathRequest>(entity))
            em.SetComponentData(entity, new UnitPathRequest { Goal = goal });
        else
            ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
    }

    private static void RemoveIfPresent<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

    private static bool IsFactionAIControlled(byte factionId, bool hasControls, NativeArray<FactionControlEntry> controls)
    {
        if (!hasControls)
            return factionId != 0;

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return factionId != 0;
    }

    private BuildingPlacementSystem GetBuildingPlacement(ref SystemState state)
    {
        if (_buildingPlacementRuntimeQuery.IsEmptyIgnoreFilter)
            return null;

        Entity entity = _buildingPlacementRuntimeQuery.GetSingletonEntity();
        return state.EntityManager.GetComponentObject<BuildingPlacementRuntimeComponent>(entity).BuildingPlacement;
    }
}
