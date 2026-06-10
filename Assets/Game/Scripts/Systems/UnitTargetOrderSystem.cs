using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class UnitTargetOrderSystem
{
    public delegate bool TryResolveBaseBreachTargetDelegate(
        byte factionId,
        Entity targetEntity,
        int2 targetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition);

    public readonly struct AttackOrderIssueResult
    {
        public readonly TacticalCommandResult CommandResult;
        public readonly int IssuedCount;
        public readonly Entity TargetEntity;
        public readonly float3 TargetPosition;

        public AttackOrderIssueResult(TacticalCommandResult commandResult, int issuedCount, Entity targetEntity, float3 targetPosition)
        {
            CommandResult = commandResult;
            IssuedCount = issuedCount;
            TargetEntity = targetEntity;
            TargetPosition = targetPosition;
        }
    }

    public bool TryFindRadarTargetForMissileLauncher(
        EntityManager entityManager,
        byte factionId,
        bool requireAirTarget,
        Entity launcher,
        out Entity bestTarget,
        out int2 bestTargetCell,
        out float3 bestTargetPosition)
    {
        bestTarget = Entity.Null;
        bestTargetCell = default;
        bestTargetPosition = default;

        byte detectorKind = requireAirTarget
            ? (byte)ThreatDetectionKind.Air
            : (byte)ThreatDetectionKind.Ground;

        using EntityQuery detectorQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<ThreatDetector>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>());
        using EntityQuery targetQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<LocalTransform>());

        using var detectors = detectorQuery.ToEntityArray(Allocator.Temp);
        using var targets = targetQuery.ToEntityArray(Allocator.Temp);

        int2 launcherCell = entityManager.HasComponent<UnitGrid>(launcher)
            ? entityManager.GetComponentData<UnitGrid>(launcher).Cell
            : default;
        int bestLauncherDistance = int.MaxValue;

        for (int i = 0; i < targets.Length; i++)
        {
            Entity target = targets[i];
            if (!entityManager.Exists(target) || target == launcher)
                continue;
            if (entityManager.HasComponent<RuntimeBuildingCombatTag>(target))
                continue;

            Faction targetFaction = entityManager.GetComponentData<Faction>(target);
            if (targetFaction.Id == factionId)
                continue;

            UnitHealth targetHealth = entityManager.GetComponentData<UnitHealth>(target);
            if (targetHealth.Current <= 0)
                continue;

            bool isAirTarget = entityManager.HasComponent<UnitAirMovement>(target);
            if ((requireAirTarget && !isAirTarget) || (!requireAirTarget && isAirTarget))
                continue;
            if (!requireAirTarget && !entityManager.HasComponent<UnitMove>(target))
                continue;

            int2 targetCell = entityManager.GetComponentData<UnitGrid>(target).Cell;
            if (!IsInFriendlyDetectorRadius(entityManager, detectors, factionId, detectorKind, targetCell))
                continue;

            int launcherDistance = ChebyshevDistance(launcherCell, targetCell);
            if (launcherDistance >= bestLauncherDistance)
                continue;

            bestTarget = target;
            bestTargetCell = targetCell;
            bestTargetPosition = entityManager.GetComponentData<LocalTransform>(target).Position;
            bestLauncherDistance = launcherDistance;
        }

        return bestTarget != Entity.Null;
    }

    public AttackOrderIssueResult IssueAttackTarget(
        EntityManager entityManager,
        NativeArray<Entity> selectedEntities,
        Entity targetEntity,
        TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null)
    {
        TacticalCommandResult targetValidation = ValidateAttackTarget(entityManager, targetEntity);
        if (!targetValidation.Accepted)
            return new AttackOrderIssueResult(targetValidation, 0, Entity.Null, default);

        if (selectedEntities.Length == 0)
            return new AttackOrderIssueResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection), 0, Entity.Null, default);

        LocalTransform targetTransform = entityManager.GetComponentData<LocalTransform>(targetEntity);
        int2 targetCell = entityManager.HasComponent<UnitGrid>(targetEntity)
            ? entityManager.GetComponentData<UnitGrid>(targetEntity).Cell
            : default;
        int issuedCount = 0;
        bool issuedGroundMissileOrder = false;
        TacticalCommandResult missileRangeRejection = default;
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            if (!ValidateAttackSource(entityManager, entity).Accepted)
                continue;
            bool isGroundMissileLauncher = entityManager.HasComponent<GroundMissileLauncherComponent>(entity);
            if (!ValidateGroundMissileLauncherRange(entityManager, entity, targetTransform.Position, out TacticalCommandResult missileRangeResult))
            {
                missileRangeRejection = missileRangeResult;
                continue;
            }

            Entity engageTarget = targetEntity;
            int2 engageCell = targetCell;
            float3 engagePosition = targetTransform.Position;
            bool issuedBreachOrder = false;
            bool canResolveBaseBreach = !entityManager.HasComponent<GroundMissileLauncherComponent>(entity);
            if (canResolveBaseBreach &&
                tryResolveBaseBreachTarget != null &&
                entityManager.HasComponent<Faction>(entity) &&
                entityManager.HasComponent<UnitGrid>(entity) &&
                tryResolveBaseBreachTarget(
                    entityManager.GetComponentData<Faction>(entity).Id,
                    targetEntity,
                    targetCell,
                    entityManager.GetComponentData<UnitGrid>(entity).Cell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition))
            {
                engageTarget = breachTarget;
                engageCell = breachCell;
                engagePosition = breachPosition;
                issuedBreachOrder = true;
            }

            ClearInterruptedOrderComponents(entityManager, entity, removeEngageTarget: issuedBreachOrder);
            if (issuedBreachOrder)
            {
                SetOrAdd(entityManager, entity, new UnitTarget { Cell = engageCell });
                SetOrAdd(entityManager, entity, new UnitPathRequest { Goal = engageCell });
                if (!entityManager.HasComponent<ManualMoveOrderTag>(entity))
                    entityManager.AddComponent<ManualMoveOrderTag>(entity);
            }
            else
            {
                SetOrAdd(
                    entityManager,
                    entity,
                    new EngageTarget
                    {
                        Target = engageTarget,
                        Cell = engageCell,
                        Position = engagePosition,
                        IsCommanded = 1
                    });
            }

            if (issuedBreachOrder)
            {
                BaseBreachOrder breachOrder = new()
                {
                    FinalTarget = targetEntity,
                    FinalCell = targetCell,
                    FinalPosition = targetTransform.Position,
                    BreachTarget = engageTarget,
                    BreachCell = engageCell,
                    BreachPosition = engagePosition,
                    Stage = BaseBreachOrder.StageMovingToEnemyBreach,
                    IsCommanded = 1
                };
                SetOrAdd(entityManager, entity, breachOrder);
            }
            issuedCount++;
            issuedGroundMissileOrder |= isGroundMissileLauncher;
        }

        TacticalCommandResult result = issuedCount > 0
            ? TacticalCommandResult.Success(issuedGroundMissileOrder ? "Missile launched." : string.Empty)
            : missileRangeRejection.ReasonCode != TacticalCommandReasonCode.None
                ? missileRangeRejection
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        return new AttackOrderIssueResult(result, issuedCount, issuedCount > 0 ? targetEntity : Entity.Null, targetTransform.Position);
    }

    public void IssueDirectAttackTarget(
        EntityManager entityManager,
        Entity sourceEntity,
        Entity targetEntity,
        int2 targetCell,
        float3 targetPosition)
    {
        ClearInterruptedOrderComponents(entityManager, sourceEntity, removeEngageTarget: false);

        SetOrAdd(
            entityManager,
            sourceEntity,
            new EngageTarget
            {
                Target = targetEntity,
                Cell = targetCell,
                Position = targetPosition,
                IsCommanded = 1
            });
    }

    public void ClearCommandedAttackOrderComponents(EntityManager entityManager, Entity entity)
    {
        ClearInterruptedOrderComponents(entityManager, entity, removeEngageTarget: true);
    }

    public TacticalCommandResult ValidateAttackSource(EntityManager entityManager, Entity entity)
    {
        if (entity == Entity.Null || !entityManager.Exists(entity))
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (!entityManager.HasComponent<Faction>(entity) ||
            !FactionIdentitySystem.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) ||
            !entityManager.HasComponent<UnitMove>(entity) ||
            !entityManager.HasComponent<UnitCombat>(entity) ||
            entityManager.GetComponentData<UnitCombat>(entity).CanAttack == 0)
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (entityManager.HasComponent<UnitHealth>(entity) &&
            entityManager.GetComponentData<UnitHealth>(entity).Current <= 0)
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        return TacticalCommandResult.Success();
    }

    public TacticalCommandResult ValidateAttackTarget(EntityManager entityManager, Entity targetEntity)
    {
        if (targetEntity == Entity.Null ||
            !entityManager.Exists(targetEntity) ||
            !entityManager.HasComponent<Faction>(targetEntity) ||
            !entityManager.HasComponent<LocalTransform>(targetEntity))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (!FactionIdentitySystem.IsHostileToPlayer(entityManager.GetComponentData<Faction>(targetEntity).Id))
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (entityManager.HasComponent<UnitHealth>(targetEntity) &&
            entityManager.GetComponentData<UnitHealth>(targetEntity).Current <= 0)
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        return TacticalCommandResult.Success();
    }

    private static bool ValidateGroundMissileLauncherRange(
        EntityManager entityManager,
        Entity launcherEntity,
        float3 targetPosition,
        out TacticalCommandResult result)
    {
        result = TacticalCommandResult.Success();
        if (!entityManager.HasComponent<GroundMissileLauncherComponent>(launcherEntity) ||
            !entityManager.HasComponent<LocalTransform>(launcherEntity))
        {
            return true;
        }

        GroundMissileLauncherComponent launcher = entityManager.GetComponentData<GroundMissileLauncherComponent>(launcherEntity);
        float3 launcherPosition = entityManager.GetComponentData<LocalTransform>(launcherEntity).Position;
        float3 delta = targetPosition - launcherPosition;
        delta.y = 0f;
        float distance = math.length(delta);
        float minRange = math.max(0f, launcher.MinRange);
        float maxRange = math.max(minRange, launcher.MaxRange);
        if (distance < minRange)
        {
            result = TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.TargetNotAttackable,
                "Target too close for missile launcher.");
            return false;
        }

        if (distance > maxRange)
        {
            result = TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.TargetNotAttackable,
                "Target out of missile range.");
            return false;
        }

        return true;
    }

    public bool IsInFriendlyDetectorRadius(EntityManager entityManager, NativeArray<Entity> detectors, byte factionId, int detectorKind, int2 targetCell)
    {
        for (int i = 0; i < detectors.Length; i++)
        {
            Entity detector = detectors[i];
            if (!entityManager.Exists(detector))
                continue;

            Faction detectorFaction = entityManager.GetComponentData<Faction>(detector);
            if (detectorFaction.Id != factionId)
                continue;

            UnitHealth detectorHealth = entityManager.GetComponentData<UnitHealth>(detector);
            if (detectorHealth.Current <= 0)
                continue;

            ThreatDetector threatDetector = entityManager.GetComponentData<ThreatDetector>(detector);
            if (threatDetector.Kind != detectorKind || threatDetector.RadiusCells <= 0)
                continue;

            int2 detectorCell = entityManager.GetComponentData<UnitGrid>(detector).Cell;
            if (ChebyshevDistance(detectorCell, targetCell) <= threatDetector.RadiusCells)
                return true;
        }

        return false;
    }

    public int ChebyshevDistance(int2 a, int2 b)
    {
        int2 delta = math.abs(a - b);
        return math.max(delta.x, delta.y);
    }

    public bool IsBuildingEntity(EntityManager entityManager, Entity entity)
    {
        if (entity == Entity.Null || !entityManager.Exists(entity))
            return false;
        if (entityManager.HasComponent<UnitMove>(entity))
            return false;
        if (!entityManager.HasComponent<UnitHealth>(entity) || !entityManager.HasComponent<UnitRespawnPrefab>(entity))
            return false;

        return entityManager.GetComponentData<UnitRespawnPrefab>(entity).Prefab == Entity.Null;
    }

    public void ClearAccidentalAirSelectionMove(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.Exists(entity) ||
            !entityManager.HasComponent<UnitAirMovement>(entity) ||
            !entityManager.HasComponent<UnitGrid>(entity) ||
            entityManager.HasComponent<EngageTarget>(entity) ||
            !entityManager.HasComponent<UnitTarget>(entity) ||
            !entityManager.HasComponent<ManualMoveOrderTag>(entity))
        {
            return;
        }

        int2 currentCell = entityManager.GetComponentData<UnitGrid>(entity).Cell;
        int2 targetCell = entityManager.GetComponentData<UnitTarget>(entity).Cell;
        int2 delta = targetCell - currentCell;
        if (math.abs(delta.x) > 1 || math.abs(delta.y) > 1)
            return;

        entityManager.RemoveComponent<UnitTarget>(entity);
        if (entityManager.HasComponent<UnitPathRequest>(entity))
            entityManager.RemoveComponent<UnitPathRequest>(entity);
        if (entityManager.HasComponent<UnitPathFollow>(entity))
            entityManager.RemoveComponent<UnitPathFollow>(entity);
        if (entityManager.HasComponent<UnitPathRange>(entity))
            entityManager.RemoveComponent<UnitPathRange>(entity);
        if (entityManager.HasComponent<ManualMoveOrderTag>(entity))
            entityManager.RemoveComponent<ManualMoveOrderTag>(entity);
    }

    private static void SetOrAdd<T>(EntityManager entityManager, Entity entity, T value)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
            entityManager.SetComponentData(entity, value);
        else
            entityManager.AddComponentData(entity, value);
    }

    private static void RemoveIfPresent<T>(EntityManager entityManager, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
            entityManager.RemoveComponent<T>(entity);
    }

    private static void ClearInterruptedOrderComponents(
        EntityManager entityManager,
        Entity entity,
        bool removeEngageTarget)
    {
        RemoveIfPresent<ManualMoveOrderTag>(entityManager, entity);
        RemoveIfPresent<ManualMoveGroupMemberTag>(entityManager, entity);
        RemoveIfPresent<AutoWanderMoveTag>(entityManager, entity);
        RemoveIfPresent<HoldPositionOrderTag>(entityManager, entity);
        RemoveIfPresent<UnitPathFollow>(entityManager, entity);
        RemoveIfPresent<UnitPathRange>(entityManager, entity);
        RemoveIfPresent<UnitPathRequest>(entityManager, entity);
        RemoveIfPresent<UnitPathRetryCooldown>(entityManager, entity);
        RemoveIfPresent<UnitLongDistanceMove>(entityManager, entity);
        RemoveIfPresent<UnitTarget>(entityManager, entity);
        RemoveIfPresent<BaseBreachOrder>(entityManager, entity);
        RemoveIfPresent<UnitTransportBoardingTarget>(entityManager, entity);
        RemoveIfPresent<UnitTransportRopeDisembarkRequest>(entityManager, entity);
        RemoveIfPresent<UnitResourceHaulOrder>(entityManager, entity);
        if (removeEngageTarget)
            RemoveIfPresent<EngageTarget>(entityManager, entity);
    }
}
