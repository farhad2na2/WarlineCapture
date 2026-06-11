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
    private EntityQuery _runtimeBuildingCombatQuery;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityQuery _squadQuery;

    private readonly struct RuntimeBuildingCombatData
    {
        public readonly NativeArray<Entity> Entities;
        public readonly NativeArray<RuntimeBuildingCombatInfo> Infos;
        public readonly NativeArray<UnitHealth> Healths;
        public readonly NativeArray<LocalTransform> Transforms;

        public RuntimeBuildingCombatData(
            NativeArray<Entity> entities,
            NativeArray<RuntimeBuildingCombatInfo> infos,
            NativeArray<UnitHealth> healths,
            NativeArray<LocalTransform> transforms)
        {
            Entities = entities;
            Infos = infos;
            Healths = healths;
            Transforms = transforms;
        }

        public int Length => Entities.IsCreated ? Entities.Length : 0;
    }

    private readonly struct GridBreachContext
    {
        public readonly bool IsValid;
        public readonly GridConfig Grid;
        public readonly NativeArray<GridWalkable> Walkable;
        public readonly NativeBitArray Blocked;
        public readonly NativeArray<byte> FriendlyPassFactionIds;
        public readonly NativeBitArray Occupied;

        public GridBreachContext(
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray blocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied)
        {
            IsValid = true;
            Grid = grid;
            Walkable = walkable;
            Blocked = blocked;
            FriendlyPassFactionIds = friendlyPassFactionIds;
            Occupied = occupied;
        }
    }

    public void OnCreate(ref SystemState state)
    {
        _runtimeBuildingCombatQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<LocalTransform>());
        _diagnosticLogQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        _squadQuery = state.GetEntityQuery(ComponentType.ReadWrite<AISquad>(), ComponentType.ReadOnly<AISquadUnit>());
        state.RequireForUpdate<AISquad>();
        state.RequireForUpdate<AISquadUnit>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        EntityManager em = state.EntityManager;
        EntityCommandBuffer ecb = new(Allocator.Temp);
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        NativeArray<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true).ToNativeArray(Allocator.Temp)
            : default;
        bool shouldLog = ShouldQueueDiagnostics(ref state);
        NativeArray<Entity> runtimeBuildingEntities = default;
        NativeArray<RuntimeBuildingCombatInfo> runtimeBuildingInfos = default;
        NativeArray<UnitHealth> runtimeBuildingHealths = default;
        NativeArray<LocalTransform> runtimeBuildingTransforms = default;
        RuntimeBuildingCombatData runtimeBuildings = default;
        GridBreachContext gridBreachContext = default;
        bool breachContextCreated = false;

        using NativeArray<Entity> squadEntities = _squadQuery.ToEntityArray(Allocator.Temp);

        try
        {
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

                EnsureBreachContext(
                    ref state,
                    ref runtimeBuildingEntities,
                    ref runtimeBuildingInfos,
                    ref runtimeBuildingHealths,
                    ref runtimeBuildingTransforms,
                    ref runtimeBuildings,
                    ref gridBreachContext,
                    ref breachContextCreated);

                float3 targetPosition = ResolveTargetPosition(em, squad.TargetEntity, squad.TargetCell);
                DynamicBuffer<AISquadUnit> members = em.GetBuffer<AISquadUnit>(squadEntity);
                int issued = 0;
                for (int memberIndex = 0; memberIndex < members.Length; memberIndex++)
                {
                    Entity unit = members[memberIndex].Unit;
                    if (!CanReceiveCombatOrder(em, unit, squad.FactionId))
                        continue;

                    IssueEngageOrder(em, ecb, runtimeBuildings, gridBreachContext, unit, squad.TargetEntity, squad.TargetCell, targetPosition);
                    issued++;
                }

                if (issued <= 0)
                    continue;

                squad.LastOrderTime = now;
                squad.LastLogTime = now;
                em.SetComponentData(squadEntity, squad);
                if (shouldLog)
                    EnqueueDiagnostic(ref state, $"[AICombat] faction={squad.FactionId} squad={squad.SquadId} order=Attack target={squad.TargetEntity} units={issued}");
            }
        }
        finally
        {
            if (runtimeBuildingEntities.IsCreated)
                runtimeBuildingEntities.Dispose();
            if (runtimeBuildingInfos.IsCreated)
                runtimeBuildingInfos.Dispose();
            if (runtimeBuildingHealths.IsCreated)
                runtimeBuildingHealths.Dispose();
            if (runtimeBuildingTransforms.IsCreated)
                runtimeBuildingTransforms.Dispose();
        }

        if (controls.IsCreated)
            controls.Dispose();
        ecb.Playback(em);
        ecb.Dispose();
    }

    private void EnsureBreachContext(
        ref SystemState state,
        ref NativeArray<Entity> runtimeBuildingEntities,
        ref NativeArray<RuntimeBuildingCombatInfo> runtimeBuildingInfos,
        ref NativeArray<UnitHealth> runtimeBuildingHealths,
        ref NativeArray<LocalTransform> runtimeBuildingTransforms,
        ref RuntimeBuildingCombatData runtimeBuildings,
        ref GridBreachContext gridBreachContext,
        ref bool breachContextCreated)
    {
        if (breachContextCreated)
            return;

        runtimeBuildingEntities = _runtimeBuildingCombatQuery.ToEntityArray(Allocator.Temp);
        runtimeBuildingInfos = _runtimeBuildingCombatQuery.ToComponentDataArray<RuntimeBuildingCombatInfo>(Allocator.Temp);
        runtimeBuildingHealths = _runtimeBuildingCombatQuery.ToComponentDataArray<UnitHealth>(Allocator.Temp);
        runtimeBuildingTransforms = _runtimeBuildingCombatQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        runtimeBuildings = new RuntimeBuildingCombatData(
            runtimeBuildingEntities,
            runtimeBuildingInfos,
            runtimeBuildingHealths,
            runtimeBuildingTransforms);
        gridBreachContext = TryGetGridBreachContext(ref state, out GridBreachContext foundGridContext)
            ? foundGridContext
            : default;
        breachContextCreated = true;
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
        RuntimeBuildingCombatData runtimeBuildings,
        GridBreachContext gridBreachContext,
        Entity unit,
        Entity target,
        int2 targetCell,
        float3 targetPosition)
    {
        Entity engageTarget = target;
        int2 engageCell = targetCell;
        float3 engagePosition = targetPosition;
        bool issuedBreachOrder = false;

        if (em.HasComponent<Faction>(unit) &&
            em.HasComponent<UnitGrid>(unit))
        {
            byte attackerFaction = em.GetComponentData<Faction>(unit).Id;
            int2 attackerCell = em.GetComponentData<UnitGrid>(unit).Cell;
            if (TryResolveBaseBreachTarget(
                    runtimeBuildings,
                    gridBreachContext,
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

    private static bool TryResolveBaseBreachTarget(
        RuntimeBuildingCombatData runtimeBuildings,
        GridBreachContext gridBreachContext,
        byte attackerFactionId,
        Entity finalTarget,
        int2 finalTargetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition,
        out string reason)
    {
        breachTarget = Entity.Null;
        breachCell = default;
        breachPosition = default;
        reason = string.Empty;

        if (TryFindRuntimeBuilding(runtimeBuildings, finalTarget, out _, out RuntimeBuildingCombatInfo finalTargetInfo, out _, out _) &&
            (finalTargetInfo.IsWall != 0 || finalTargetInfo.IsGate != 0))
        {
            return false;
        }

        if (!TryFindEnemyWallPerimeterContainingCell(
                runtimeBuildings,
                attackerFactionId,
                finalTargetCell,
                out byte breachedFactionId,
                out RectInt breachedPerimeter))
        {
            return false;
        }

        if (HasOpenBaseBreach(runtimeBuildings, breachedFactionId, breachedPerimeter))
            return false;

        if (!TryFindBreachBuilding(runtimeBuildings, breachedFactionId, attackerCell, preferGate: true, out int breachIndex, out reason) &&
            !TryFindBreachBuilding(runtimeBuildings, breachedFactionId, attackerCell, preferGate: false, out breachIndex, out reason))
        {
            return false;
        }

        RuntimeBuildingCombatInfo breachInfo = runtimeBuildings.Infos[breachIndex];
        breachTarget = runtimeBuildings.Entities[breachIndex];
        breachCell = GetCenterCell(breachInfo);
        breachPosition = runtimeBuildings.Transforms[breachIndex].Position;

        if (gridBreachContext.IsValid &&
            BuildingBarrierSystem.TryFindBreachApproachCell(
                gridBreachContext.Grid,
                gridBreachContext.Walkable,
                gridBreachContext.Blocked,
                gridBreachContext.FriendlyPassFactionIds,
                gridBreachContext.Occupied,
                ToVector2Int(breachInfo.OriginCell),
                ToVector2Int(breachInfo.FootprintCells),
                breachedPerimeter,
                new int2(1, 1),
                attackerCell,
                attackerFactionId,
                out int2 outsideApproachCell))
        {
            breachCell = outsideApproachCell;
        }

        return true;
    }

    private static bool TryFindEnemyWallPerimeterContainingCell(
        RuntimeBuildingCombatData runtimeBuildings,
        byte attackerFactionId,
        int2 targetCell,
        out byte breachedFactionId,
        out RectInt breachedPerimeter)
    {
        breachedFactionId = 0;
        breachedPerimeter = default;
        bool hasPerimeter = false;
        int bestArea = int.MaxValue;

        FixedList128Bytes<byte> processedFactions = default;
        for (int i = 0; i < runtimeBuildings.Length; i++)
        {
            RuntimeBuildingCombatInfo info = runtimeBuildings.Infos[i];
            if (!IsActiveWallOrGate(runtimeBuildings, i) || info.OwnerFactionId == attackerFactionId)
                continue;
            if (ContainsFaction(processedFactions, info.OwnerFactionId))
                continue;

            processedFactions.Add(info.OwnerFactionId);

            RectInt perimeter = BuildFactionPerimeter(runtimeBuildings, info.OwnerFactionId);
            if (!ContainsCell(perimeter, targetCell))
                continue;

            int area = math.max(1, perimeter.width) * math.max(1, perimeter.height);
            if (area >= bestArea)
                continue;

            hasPerimeter = true;
            bestArea = area;
            breachedFactionId = info.OwnerFactionId;
            breachedPerimeter = perimeter;
        }

        return hasPerimeter;
    }

    private static bool ContainsFaction(FixedList128Bytes<byte> factions, byte factionId)
    {
        for (int i = 0; i < factions.Length; i++)
        {
            if (factions[i] == factionId)
                return true;
        }

        return false;
    }

    private static RectInt BuildFactionPerimeter(RuntimeBuildingCombatData runtimeBuildings, byte factionId)
    {
        bool hasRect = false;
        RectInt result = default;
        for (int i = 0; i < runtimeBuildings.Length; i++)
        {
            RuntimeBuildingCombatInfo info = runtimeBuildings.Infos[i];
            if (!IsActiveWallOrGate(runtimeBuildings, i) || info.OwnerFactionId != factionId)
                continue;

            RectInt rect = ToRect(info);
            result = hasRect ? UnionRects(result, rect) : rect;
            hasRect = true;
        }

        return result;
    }

    private static bool HasOpenBaseBreach(RuntimeBuildingCombatData runtimeBuildings, byte ownerFactionId, RectInt perimeterRect)
    {
        for (int i = 0; i < runtimeBuildings.Length; i++)
        {
            RuntimeBuildingCombatInfo info = runtimeBuildings.Infos[i];
            if (info.OwnerFactionId != ownerFactionId ||
                (info.IsWall == 0 && info.IsGate == 0) ||
                runtimeBuildings.Healths[i].Current > 0)
            {
                continue;
            }

            RectInt rect = ToRect(info);
            if (!RectTouchesPerimeter(rect, perimeterRect))
                continue;
            if (HasActiveWallOrGateOverlapping(runtimeBuildings, rect, ownerFactionId))
                continue;

            return true;
        }

        return false;
    }

    private static bool HasActiveWallOrGateOverlapping(RuntimeBuildingCombatData runtimeBuildings, RectInt rect, byte ownerFactionId)
    {
        for (int i = 0; i < runtimeBuildings.Length; i++)
        {
            RuntimeBuildingCombatInfo info = runtimeBuildings.Infos[i];
            if (!IsActiveWallOrGate(runtimeBuildings, i) || info.OwnerFactionId != ownerFactionId)
                continue;

            if (RectsOverlap(rect, ToRect(info)))
                return true;
        }

        return false;
    }

    private static bool TryFindBreachBuilding(
        RuntimeBuildingCombatData runtimeBuildings,
        byte breachedFactionId,
        int2 attackerCell,
        bool preferGate,
        out int breachIndex,
        out string reason)
    {
        breachIndex = -1;
        reason = preferGate ? "Gate" : "Wall";
        int bestScore = int.MaxValue;

        for (int i = 0; i < runtimeBuildings.Length; i++)
        {
            RuntimeBuildingCombatInfo info = runtimeBuildings.Infos[i];
            if (!IsActiveWallOrGate(runtimeBuildings, i) || info.OwnerFactionId != breachedFactionId)
                continue;

            bool isGate = info.IsGate != 0;
            bool isWall = info.IsWall != 0;
            if (preferGate ? !isGate : (!isWall || isGate))
                continue;

            int2 center = GetCenterCell(info);
            int2 delta = center - attackerCell;
            int score = delta.x * delta.x + delta.y * delta.y;
            if (score >= bestScore)
                continue;

            bestScore = score;
            breachIndex = i;
        }

        return breachIndex >= 0;
    }

    private static bool TryFindRuntimeBuilding(
        RuntimeBuildingCombatData runtimeBuildings,
        Entity entity,
        out int index,
        out RuntimeBuildingCombatInfo info,
        out UnitHealth health,
        out LocalTransform transform)
    {
        for (int i = 0; i < runtimeBuildings.Length; i++)
        {
            if (runtimeBuildings.Entities[i] != entity)
                continue;

            index = i;
            info = runtimeBuildings.Infos[i];
            health = runtimeBuildings.Healths[i];
            transform = runtimeBuildings.Transforms[i];
            return true;
        }

        index = -1;
        info = default;
        health = default;
        transform = default;
        return false;
    }

    private static bool IsActiveWallOrGate(RuntimeBuildingCombatData runtimeBuildings, int index)
    {
        RuntimeBuildingCombatInfo info = runtimeBuildings.Infos[index];
        return (info.IsWall != 0 || info.IsGate != 0) && runtimeBuildings.Healths[index].Current > 0;
    }

    private static int2 GetCenterCell(RuntimeBuildingCombatInfo info)
    {
        int2 footprint = math.max(info.FootprintCells, new int2(1, 1));
        return info.OriginCell + footprint / 2;
    }

    private static RectInt ToRect(RuntimeBuildingCombatInfo info)
    {
        int2 footprint = math.max(info.FootprintCells, new int2(1, 1));
        return new RectInt(info.OriginCell.x, info.OriginCell.y, footprint.x, footprint.y);
    }

    private static Vector2Int ToVector2Int(int2 value)
    {
        return new Vector2Int(value.x, value.y);
    }

    private static bool ContainsCell(RectInt rect, int2 cell)
    {
        return cell.x >= rect.xMin &&
               cell.x < rect.xMax &&
               cell.y >= rect.yMin &&
               cell.y < rect.yMax;
    }

    private static RectInt UnionRects(RectInt a, RectInt b)
    {
        int xMin = math.min(a.xMin, b.xMin);
        int yMin = math.min(a.yMin, b.yMin);
        int xMax = math.max(a.xMax, b.xMax);
        int yMax = math.max(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private static bool RectTouchesPerimeter(RectInt rect, RectInt perimeterRect)
    {
        return RectsOverlap(rect, perimeterRect) ||
               (rect.xMin <= perimeterRect.xMin && rect.xMax > perimeterRect.xMin && rect.yMin < perimeterRect.yMax && rect.yMax > perimeterRect.yMin) ||
               (rect.xMin < perimeterRect.xMax && rect.xMax >= perimeterRect.xMax && rect.yMin < perimeterRect.yMax && rect.yMax > perimeterRect.yMin) ||
               (rect.yMin <= perimeterRect.yMin && rect.yMax > perimeterRect.yMin && rect.xMin < perimeterRect.xMax && rect.xMax > perimeterRect.xMin) ||
               (rect.yMin < perimeterRect.yMax && rect.yMax >= perimeterRect.yMax && rect.xMin < perimeterRect.xMax && rect.xMax > perimeterRect.xMin);
    }

    private static bool RectsOverlap(RectInt a, RectInt b)
    {
        return a.xMin < b.xMax &&
               a.xMax > b.xMin &&
               a.yMin < b.yMax &&
               a.yMax > b.yMin;
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
            return FactionIdentitySystem.IsAiControlledByDefault(factionId);

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return FactionIdentitySystem.IsAiControlledByDefault(factionId);
    }

    private static bool TryGetGridBreachContext(ref SystemState state, out GridBreachContext context)
    {
        context = default;
        EntityManager em = state.EntityManager;
        using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        if (gridQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = gridQuery.GetSingletonEntity();
        if (!em.HasBuffer<GridWalkable>(gridEntity) ||
            !em.HasComponent<DynamicBlockerComponent>(gridEntity) ||
            !em.HasComponent<DynamicOccupancyComponent>(gridEntity))
        {
            return false;
        }

        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancyData = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        if (!blockerData.Blocked.IsCreated || !occupancyData.Occupied.IsCreated)
            return false;

        context = new GridBreachContext(
            em.GetComponentData<GridConfig>(gridEntity),
            em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray(),
            blockerData.Blocked,
            blockerData.FriendlyPassFactionIds,
            occupancyData.Occupied);
        return true;
    }

    private bool ShouldQueueDiagnostics(ref SystemState state)
    {
        if (Application.isBatchMode)
            return false;

        return SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
    }

    private void EnqueueDiagnostic(ref SystemState state, FixedString512Bytes message)
    {
        EntityManager em = state.EntityManager;
        Entity queueEntity;
        if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
        {
            queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "AIDiagnosticLogQueue");
            em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
        }
        else
        {
            queueEntity = _diagnosticLogQueueQuery.GetSingletonEntity();
        }

        DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
        logs.Add(new AIDiagnosticLogComponent { Message = message });
    }
}
