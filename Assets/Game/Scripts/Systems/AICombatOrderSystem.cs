using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(AITargetingSystem))]
[UpdateBefore(typeof(UnitEngagementSystem))]
public partial struct AICombatOrderSystem : ISystem
{
    private const float EvaluationIntervalSeconds = 0.25f;
    private const float OrderRefreshSeconds = 2f;
    private EntityQuery _runtimeBuildingCombatQuery;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityTypeHandle _entityType;
    private ComponentTypeHandle<RuntimeBuildingCombatInfo> _runtimeBuildingCombatInfoType;
    private ComponentTypeHandle<UnitHealth> _unitHealthType;
    private ComponentTypeHandle<LocalTransform> _localTransformType;
    private BufferLookup<AISquadUnit> _squadUnitLookup;
    private ComponentLookup<Faction> _factionLookup;
    private ComponentLookup<AIControlledTag> _aiControlledLookup;
    private ComponentLookup<UnitHealth> _unitHealthLookup;
    private ComponentLookup<UnitCombat> _unitCombatLookup;
    private ComponentLookup<UnitAttack> _unitAttackLookup;
    private ComponentLookup<LocalTransform> _unitTransformLookup;
    private ComponentLookup<StaticGridBlocker> _staticGridBlockerLookup;
    private ComponentLookup<BaseBreachOrder> _baseBreachOrderLookup;
    private ComponentLookup<EngageTarget> _engageTargetLookup;
    private EntityStorageInfoLookup _entityStorageInfoLookup;
    private NativeList<RuntimeBuildingCombatRecord> _runtimeBuildingCombatRecords;
    private float _nextEvaluationTime;

    private readonly struct RuntimeBuildingCombatRecord
    {
        public readonly Entity Entity;
        public readonly RuntimeBuildingCombatInfo Info;
        public readonly UnitHealth Health;
        public readonly LocalTransform Transform;

        public RuntimeBuildingCombatRecord(
            Entity entity,
            RuntimeBuildingCombatInfo info,
            UnitHealth health,
            LocalTransform transform)
        {
            Entity = entity;
            Info = info;
            Health = health;
            Transform = transform;
        }
    }

    private readonly struct RuntimeBuildingCombatData
    {
        public readonly NativeList<RuntimeBuildingCombatRecord> Records;

        public RuntimeBuildingCombatData(NativeList<RuntimeBuildingCombatRecord> records)
        {
            Records = records;
        }

        public int Length => Records.IsCreated ? Records.Length : 0;

        public RuntimeBuildingCombatRecord this[int index] => Records[index];
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
        _entityType = state.GetEntityTypeHandle();
        _runtimeBuildingCombatInfoType = state.GetComponentTypeHandle<RuntimeBuildingCombatInfo>(true);
        _unitHealthType = state.GetComponentTypeHandle<UnitHealth>(true);
        _localTransformType = state.GetComponentTypeHandle<LocalTransform>(true);
        _squadUnitLookup = state.GetBufferLookup<AISquadUnit>(true);
        _factionLookup = state.GetComponentLookup<Faction>(true);
        _aiControlledLookup = state.GetComponentLookup<AIControlledTag>(true);
        _unitHealthLookup = state.GetComponentLookup<UnitHealth>(true);
        _unitCombatLookup = state.GetComponentLookup<UnitCombat>(true);
        _unitAttackLookup = state.GetComponentLookup<UnitAttack>(true);
        _unitTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        _staticGridBlockerLookup = state.GetComponentLookup<StaticGridBlocker>(true);
        _baseBreachOrderLookup = state.GetComponentLookup<BaseBreachOrder>(true);
        _engageTargetLookup = state.GetComponentLookup<EngageTarget>(true);
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        _runtimeBuildingCombatRecords = new NativeList<RuntimeBuildingCombatRecord>(Allocator.Persistent);
        state.RequireForUpdate<AISquad>();
        state.RequireForUpdate<AISquadUnit>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_runtimeBuildingCombatRecords.IsCreated)
            _runtimeBuildingCombatRecords.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        if (now < _nextEvaluationTime)
            return;

        _nextEvaluationTime = now + EvaluationIntervalSeconds;
        EntityManager em = state.EntityManager;
        EntityCommandBuffer ecb = default;
        bool hasEcb = false;
        bool shouldLog = ShouldQueueDiagnostics(ref state);
        if (shouldLog)
            EnsureDiagnosticLogQueue(ref state);

        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        DynamicBuffer<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true)
            : default;
        RuntimeBuildingCombatData runtimeBuildings = default;
        GridBreachContext gridBreachContext = default;
        bool breachContextCreated = false;
        UpdateOrderRefreshLookups(ref state);

        foreach (var (squadRef, squadEntity) in SystemAPI
                     .Query<RefRW<AISquad>>()
                     .WithAll<AISquadUnit>()
                     .WithEntityAccess())
        {
            AISquad squad = squadRef.ValueRO;
            if (!IsFactionAIControlled(squad.FactionId, hasControls, controls))
                continue;

            if (squad.TargetEntity == Entity.Null ||
                !em.Exists(squad.TargetEntity) ||
                !em.HasComponent<UnitHealth>(squad.TargetEntity) ||
                em.GetComponentData<UnitHealth>(squad.TargetEntity).Current <= 0)
            {
                continue;
            }

            if (now - squad.LastOrderTime < OrderRefreshSeconds &&
                CountMembersNeedingOrder(squadEntity, squad, state.Dependency) == 0)
                continue;

            EnsureBreachContext(
                ref state,
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

                if (!hasEcb)
                {
                    ecb = new EntityCommandBuffer(Allocator.Temp);
                    hasEcb = true;
                }

                IssueEngageOrder(em, ecb, runtimeBuildings, gridBreachContext, unit, squad.TargetEntity, squad.TargetCell, targetPosition);
                issued++;
            }

            if (issued <= 0)
                continue;

            squad.LastOrderTime = now;
            squad.LastLogTime = now;
            squadRef.ValueRW = squad;
            if (shouldLog)
                EnqueueDiagnostic(ref state, $"[AICombat] faction={squad.FactionId} squad={squad.SquadId} order=Attack target={squad.TargetEntity} units={issued}");
        }

        if (hasEcb)
        {
            ecb.Playback(em);
            ecb.Dispose();
        }
    }

    private void EnsureBreachContext(
        ref SystemState state,
        ref RuntimeBuildingCombatData runtimeBuildings,
        ref GridBreachContext gridBreachContext,
        ref bool breachContextCreated)
    {
        if (breachContextCreated)
            return;

        _entityType.Update(ref state);
        _runtimeBuildingCombatInfoType.Update(ref state);
        _unitHealthType.Update(ref state);
        _localTransformType.Update(ref state);

        _runtimeBuildingCombatRecords.Clear();
        using NativeArray<ArchetypeChunk> chunks = _runtimeBuildingCombatQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(_entityType);
            NativeArray<RuntimeBuildingCombatInfo> infos = chunk.GetNativeArray(ref _runtimeBuildingCombatInfoType);
            NativeArray<UnitHealth> healths = chunk.GetNativeArray(ref _unitHealthType);
            NativeArray<LocalTransform> transforms = chunk.GetNativeArray(ref _localTransformType);

            for (int i = 0; i < entities.Length; i++)
            {
                _runtimeBuildingCombatRecords.Add(new RuntimeBuildingCombatRecord(
                    entities[i],
                    infos[i],
                    healths[i],
                    transforms[i]));
            }
        }

        runtimeBuildings = new RuntimeBuildingCombatData(_runtimeBuildingCombatRecords);
        gridBreachContext = TryGetGridBreachContext(ref state, out GridBreachContext foundGridContext)
            ? foundGridContext
            : default;
        breachContextCreated = true;
    }

    private void UpdateOrderRefreshLookups(ref SystemState state)
    {
        _squadUnitLookup.Update(ref state);
        _factionLookup.Update(ref state);
        _aiControlledLookup.Update(ref state);
        _unitHealthLookup.Update(ref state);
        _unitCombatLookup.Update(ref state);
        _unitAttackLookup.Update(ref state);
        _unitTransformLookup.Update(ref state);
        _staticGridBlockerLookup.Update(ref state);
        _baseBreachOrderLookup.Update(ref state);
        _engageTargetLookup.Update(ref state);
        _entityStorageInfoLookup.Update(ref state);
    }

    private int CountMembersNeedingOrder(Entity squadEntity, AISquad squad, JobHandle dependency)
    {
        using NativeReference<int> count = new(Allocator.TempJob);

        new CountMembersNeedingOrderJob
        {
            SquadEntity = squadEntity,
            Squad = squad,
            SquadUnitLookup = _squadUnitLookup,
            FactionLookup = _factionLookup,
            AIControlledLookup = _aiControlledLookup,
            UnitHealthLookup = _unitHealthLookup,
            UnitCombatLookup = _unitCombatLookup,
            UnitAttackLookup = _unitAttackLookup,
            UnitTransformLookup = _unitTransformLookup,
            StaticGridBlockerLookup = _staticGridBlockerLookup,
            BaseBreachOrderLookup = _baseBreachOrderLookup,
            EngageTargetLookup = _engageTargetLookup,
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            Count = count
        }.Schedule(dependency).Complete();

        return count.Value;
    }

    [BurstCompile]
    private struct CountMembersNeedingOrderJob : IJob
    {
        public Entity SquadEntity;
        public AISquad Squad;
        [ReadOnly] public BufferLookup<AISquadUnit> SquadUnitLookup;
        [ReadOnly] public ComponentLookup<Faction> FactionLookup;
        [ReadOnly] public ComponentLookup<AIControlledTag> AIControlledLookup;
        [ReadOnly] public ComponentLookup<UnitHealth> UnitHealthLookup;
        [ReadOnly] public ComponentLookup<UnitCombat> UnitCombatLookup;
        [ReadOnly] public ComponentLookup<UnitAttack> UnitAttackLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> UnitTransformLookup;
        [ReadOnly] public ComponentLookup<StaticGridBlocker> StaticGridBlockerLookup;
        [ReadOnly] public ComponentLookup<BaseBreachOrder> BaseBreachOrderLookup;
        [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        public NativeReference<int> Count;

        public void Execute()
        {
            if (!SquadUnitLookup.HasBuffer(SquadEntity))
            {
                Count.Value = 0;
                return;
            }

            DynamicBuffer<AISquadUnit> members = SquadUnitLookup[SquadEntity];
            int count = 0;
            for (int i = 0; i < members.Length; i++)
            {
                Entity unit = members[i].Unit;
                if (!CanReceiveCombatOrder(unit))
                    continue;
                if (BaseBreachOrderLookup.HasComponent(unit) &&
                    BaseBreachOrderLookup[unit].FinalTarget == Squad.TargetEntity)
                {
                    continue;
                }

                if (!EngageTargetLookup.HasComponent(unit))
                {
                    count++;
                    continue;
                }

                EngageTarget engage = EngageTargetLookup[unit];
                if (engage.Target == Squad.TargetEntity)
                    continue;

                count++;
            }

            Count.Value = count;
        }

        private bool CanReceiveCombatOrder(Entity unit)
        {
            if (unit == Entity.Null ||
                !EntityStorageInfoLookup.Exists(unit) ||
                !FactionLookup.HasComponent(unit) ||
                FactionLookup[unit].Id != Squad.FactionId ||
                !AIControlledLookup.HasComponent(unit) ||
                !UnitHealthLookup.HasComponent(unit) ||
                UnitHealthLookup[unit].Current <= 0 ||
                !UnitCombatLookup.HasComponent(unit) ||
                !UnitAttackLookup.HasComponent(unit) ||
                !UnitTransformLookup.HasComponent(unit) ||
                StaticGridBlockerLookup.HasComponent(unit))
            {
                return false;
            }

            UnitCombat combat = UnitCombatLookup[unit];
            return combat.CanAttack != 0;
        }
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
                ecb.SetComponent(unit, order);
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
                ecb.SetComponent(unit, breachOrder);
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

        RuntimeBuildingCombatRecord breachRecord = runtimeBuildings[breachIndex];
        RuntimeBuildingCombatInfo breachInfo = breachRecord.Info;
        breachTarget = breachRecord.Entity;
        breachCell = GetCenterCell(breachInfo);
        breachPosition = breachRecord.Transform.Position;

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
            RuntimeBuildingCombatInfo info = runtimeBuildings[i].Info;
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
            RuntimeBuildingCombatInfo info = runtimeBuildings[i].Info;
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
            RuntimeBuildingCombatRecord record = runtimeBuildings[i];
            RuntimeBuildingCombatInfo info = record.Info;
            if (info.OwnerFactionId != ownerFactionId ||
                (info.IsWall == 0 && info.IsGate == 0) ||
                record.Health.Current > 0)
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
            RuntimeBuildingCombatInfo info = runtimeBuildings[i].Info;
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
            RuntimeBuildingCombatInfo info = runtimeBuildings[i].Info;
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
            RuntimeBuildingCombatRecord record = runtimeBuildings[i];
            if (record.Entity != entity)
                continue;

            index = i;
            info = record.Info;
            health = record.Health;
            transform = record.Transform;
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
        RuntimeBuildingCombatRecord record = runtimeBuildings[index];
        RuntimeBuildingCombatInfo info = record.Info;
        return (info.IsWall != 0 || info.IsGate != 0) && record.Health.Current > 0;
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
            ecb.SetComponent(entity, new UnitTarget { Cell = goal });
        else
            ecb.AddComponent(entity, new UnitTarget { Cell = goal });

        if (em.HasComponent<UnitPathRequest>(entity))
            ecb.SetComponent(entity, new UnitPathRequest { Goal = goal });
        else
            ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
    }

    private static void RemoveIfPresent<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

    private static bool IsFactionAIControlled(byte factionId, bool hasControls, DynamicBuffer<FactionControlEntry> controls)
    {
        if (!hasControls)
            return FactionIdentity.IsAiControlledByDefault(factionId);

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return FactionIdentity.IsAiControlledByDefault(factionId);
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
        return InitialUnitsRuntimeState.VerboseAILogs ||
            SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
    }

    private void EnqueueDiagnostic(ref SystemState state, FixedString512Bytes message)
    {
        if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
            return;

        EntityManager em = state.EntityManager;
        Entity queueEntity = _diagnosticLogQueueQuery.GetSingletonEntity();
        DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
        logs.Add(new AIDiagnosticLogComponent { Message = message });
    }

    private void EnsureDiagnosticLogQueue(ref SystemState state)
    {
        if (!_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
            return;

        EntityManager em = state.EntityManager;
        Entity queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
        em.SetName(queueEntity, "AIDiagnosticLogQueue");
        em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
    }
}
