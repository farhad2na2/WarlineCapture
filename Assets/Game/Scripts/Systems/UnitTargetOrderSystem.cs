using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitTargetOrderSystem : ISystem
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

        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled command helper; selection/composition calls methods directly.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
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

            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            ComponentTypeHandle<ThreatDetector> detectorType = entityManager.GetComponentTypeHandle<ThreatDetector>(true);
            ComponentTypeHandle<Faction> factionType = entityManager.GetComponentTypeHandle<Faction>(true);
            ComponentTypeHandle<UnitGrid> gridType = entityManager.GetComponentTypeHandle<UnitGrid>(true);
            ComponentTypeHandle<UnitHealth> healthType = entityManager.GetComponentTypeHandle<UnitHealth>(true);
            ComponentTypeHandle<LocalTransform> transformType = entityManager.GetComponentTypeHandle<LocalTransform>(true);
            using var detectorChunks = detectorQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var targetChunks = targetQuery.ToArchetypeChunkArray(Allocator.Temp);

            int2 launcherCell = entityManager.HasComponent<UnitGrid>(launcher)
                ? entityManager.GetComponentData<UnitGrid>(launcher).Cell
                : default;
            int bestLauncherDistance = int.MaxValue;

            for (int chunkIndex = 0; chunkIndex < targetChunks.Length; chunkIndex++)
            {
                ArchetypeChunk targetChunk = targetChunks[chunkIndex];
                NativeArray<Entity> targetEntities = targetChunk.GetNativeArray(entityType);
                NativeArray<Faction> targetFactions = targetChunk.GetNativeArray(ref factionType);
                NativeArray<UnitGrid> targetGrids = targetChunk.GetNativeArray(ref gridType);
                NativeArray<UnitHealth> targetHealths = targetChunk.GetNativeArray(ref healthType);
                NativeArray<LocalTransform> targetTransforms = targetChunk.GetNativeArray(ref transformType);

                for (int i = 0; i < targetEntities.Length; i++)
                {
                    Entity target = targetEntities[i];
                    if (target == launcher)
                        continue;
                    if (entityManager.HasComponent<RuntimeBuildingCombatTag>(target))
                        continue;

                    Faction targetFaction = targetFactions[i];
                    if (targetFaction.Id == factionId)
                        continue;

                    UnitHealth targetHealth = targetHealths[i];
                    if (targetHealth.Current <= 0)
                        continue;

                    bool isAirTarget = entityManager.HasComponent<UnitAirMovement>(target);
                    if ((requireAirTarget && !isAirTarget) || (!requireAirTarget && isAirTarget))
                        continue;
                    if (!requireAirTarget && !entityManager.HasComponent<UnitMove>(target))
                        continue;

                    int2 targetCell = targetGrids[i].Cell;
                    if (!IsInFriendlyDetectorRadius(detectorChunks, factionType, healthType, detectorType, gridType, factionId, detectorKind, targetCell))
                        continue;

                    int launcherDistance = ChebyshevDistanceValue(launcherCell, targetCell);
                    if (launcherDistance >= bestLauncherDistance)
                        continue;

                    bestTarget = target;
                    bestTargetCell = targetCell;
                    bestTargetPosition = targetTransforms[i].Position;
                    bestLauncherDistance = launcherDistance;
                }
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
            EntityCommandBuffer orderEcb = new(Allocator.Temp);
            try
            {
                for (int i = 0; i < selectedEntities.Length; i++)
                {
                    Entity entity = selectedEntities[i];
                    bool loadedTransportDeploySource = IsLoadedTransportDeploySource(entityManager, entity);
                    if (!loadedTransportDeploySource && !ValidateAttackSource(entityManager, entity).Accepted)
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

                    if (loadedTransportDeploySource)
                    {
                        PlaybackInterruptedOrderClear(entityManager, entity, removeEngageTarget: true);
                        SetOrAdd(
                            entityManager,
                            orderEcb,
                            entity,
                            new UnitTransportDeployOrder
                            {
                                TargetEntity = targetEntity,
                                TargetCell = targetCell,
                                TargetPosition = targetTransform.Position,
                                AttackAfterDeploy = 1
                            });
                        issuedCount++;
                        continue;
                    }

                    PlaybackInterruptedOrderClear(entityManager, entity, removeEngageTarget: issuedBreachOrder);
                    if (issuedBreachOrder)
                    {
                        UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(entityManager, entity, engageCell);
                    }
                    else
                    {
                        SetOrAdd(
                            entityManager,
                            orderEcb,
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
                        SetOrAdd(entityManager, orderEcb, entity, breachOrder);
                    }
                    issuedCount++;
                    issuedGroundMissileOrder |= isGroundMissileLauncher;
                }

                orderEcb.Playback(entityManager);
            }
            finally
            {
                orderEcb.Dispose();
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
            EntityCommandBuffer ecb = new(Allocator.Temp);
            try
            {
                ClearInterruptedOrderComponents(entityManager, ecb, sourceEntity, removeEngageTarget: false);
                SetOrAdd(
                    entityManager,
                    ecb,
                    sourceEntity,
                    new EngageTarget
                    {
                        Target = targetEntity,
                        Cell = targetCell,
                        Position = targetPosition,
                        IsCommanded = 1
                    });

                ecb.Playback(entityManager);
            }
            finally
            {
                ecb.Dispose();
            }
        }

        public void ClearCommandedAttackOrderComponents(EntityManager entityManager, Entity entity)
        {
            PlaybackInterruptedOrderClear(entityManager, entity, removeEngageTarget: true);
        }

        public TacticalCommandResult ValidateAttackSource(EntityManager entityManager, Entity entity)
        {
            if (entity == Entity.Null || !entityManager.Exists(entity))
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
            if (!entityManager.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) ||
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

            if (!FactionIdentity.IsHostileToPlayer(entityManager.GetComponentData<Faction>(targetEntity).Id))
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

        private static bool IsLoadedTransportDeploySource(EntityManager entityManager, Entity entity)
        {
            if (entity == Entity.Null ||
                !entityManager.Exists(entity) ||
                !entityManager.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) ||
                !entityManager.HasComponent<UnitMove>(entity) ||
                !entityManager.HasBuffer<UnitTransportPassengerElement>(entity))
            {
                return false;
            }

            return entityManager.GetBuffer<UnitTransportPassengerElement>(entity).Length > 0;
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
                if (ChebyshevDistanceValue(detectorCell, targetCell) <= threatDetector.RadiusCells)
                    return true;
            }

            return false;
        }

        private static bool IsInFriendlyDetectorRadius(
            NativeArray<ArchetypeChunk> detectorChunks,
            ComponentTypeHandle<Faction> factionType,
            ComponentTypeHandle<UnitHealth> healthType,
            ComponentTypeHandle<ThreatDetector> detectorType,
            ComponentTypeHandle<UnitGrid> gridType,
            byte factionId,
            int detectorKind,
            int2 targetCell)
        {
            for (int chunkIndex = 0; chunkIndex < detectorChunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = detectorChunks[chunkIndex];
                NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
                NativeArray<UnitHealth> healths = chunk.GetNativeArray(ref healthType);
                NativeArray<ThreatDetector> detectors = chunk.GetNativeArray(ref detectorType);
                NativeArray<UnitGrid> grids = chunk.GetNativeArray(ref gridType);

                for (int i = 0; i < factions.Length; i++)
                {
                    Faction detectorFaction = factions[i];
                    if (detectorFaction.Id != factionId)
                        continue;

                    UnitHealth detectorHealth = healths[i];
                    if (detectorHealth.Current <= 0)
                        continue;

                    ThreatDetector threatDetector = detectors[i];
                    if (threatDetector.Kind != detectorKind || threatDetector.RadiusCells <= 0)
                        continue;

                    int2 detectorCell = grids[i].Cell;
                    if (ChebyshevDistanceValue(detectorCell, targetCell) <= threatDetector.RadiusCells)
                        return true;
                }
            }

            return false;
        }

        public int ChebyshevDistance(int2 a, int2 b)
        {
            return ChebyshevDistanceValue(a, b);
        }

        private static int ChebyshevDistanceValue(int2 a, int2 b)
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

            EntityCommandBuffer ecb = new(Allocator.Temp);
            try
            {
                RemoveIfPresent<UnitTarget>(entityManager, ecb, entity);
                RemoveIfPresent<UnitPathRequest>(entityManager, ecb, entity);
                RemoveIfPresent<UnitPathFollow>(entityManager, ecb, entity);
                RemoveIfPresent<UnitPathRange>(entityManager, ecb, entity);
                RemoveIfPresent<ManualMoveOrderTag>(entityManager, ecb, entity);
                ecb.Playback(entityManager);
            }
            finally
            {
                ecb.Dispose();
            }
        }

        private static void SetOrAdd<T>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, T value)
            where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(entity))
                ecb.SetComponent(entity, value);
            else
                ecb.AddComponent(entity, value);
        }

        private static void RemoveIfPresent<T>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity)
            where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(entity))
                ecb.RemoveComponent<T>(entity);
        }

        private static void PlaybackInterruptedOrderClear(
            EntityManager entityManager,
            Entity entity,
            bool removeEngageTarget)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            try
            {
                ClearInterruptedOrderComponents(entityManager, ecb, entity, removeEngageTarget);
                ecb.Playback(entityManager);
            }
            finally
            {
                ecb.Dispose();
            }
        }

        private static void ClearInterruptedOrderComponents(
            EntityManager entityManager,
            EntityCommandBuffer ecb,
            Entity entity,
            bool removeEngageTarget)
        {
            RemoveIfPresent<ManualMoveOrderTag>(entityManager, ecb, entity);
            RemoveIfPresent<ManualMoveGroupMemberTag>(entityManager, ecb, entity);
            RemoveIfPresent<AutoWanderMoveTag>(entityManager, ecb, entity);
            RemoveIfPresent<HoldPositionOrderTag>(entityManager, ecb, entity);
            RemoveIfPresent<UnitPathFollow>(entityManager, ecb, entity);
            RemoveIfPresent<UnitPathRange>(entityManager, ecb, entity);
            RemoveIfPresent<UnitPathRequest>(entityManager, ecb, entity);
            RemoveIfPresent<UnitPathRetryCooldown>(entityManager, ecb, entity);
            RemoveIfPresent<UnitLongDistanceMove>(entityManager, ecb, entity);
            RemoveIfPresent<UnitTarget>(entityManager, ecb, entity);
            RemoveIfPresent<BaseBreachOrder>(entityManager, ecb, entity);
            RemoveIfPresent<UnitTransportBoardingTarget>(entityManager, ecb, entity);
            RemoveIfPresent<UnitTransportDeployOrder>(entityManager, ecb, entity);
            RemoveIfPresent<UnitTransportRopeDisembarkRequest>(entityManager, ecb, entity);
            RemoveIfPresent<UnitTransportAirdropRequest>(entityManager, ecb, entity);
            RemoveIfPresent<UnitResourceHaulOrder>(entityManager, ecb, entity);
            if (removeEngageTarget)
                RemoveIfPresent<EngageTarget>(entityManager, ecb, entity);
        }
    }
}
