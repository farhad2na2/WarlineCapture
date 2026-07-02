using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitAttackOrderRequestSystem : ISystem
    {
        private EntityQuery _queueQuery;
        private EntityQuery _selectedAttackQuery;
        private EntityTypeHandle _entityType;

        public void OnCreate(ref SystemState state)
        {
            _queueQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<UnitAttackOrderQueueComponent>(),
                ComponentType.ReadWrite<UnitAttackOrderRequestElement>(),
                ComponentType.ReadWrite<UnitAttackOrderResultElement>());
            _selectedAttackQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitCombat>(),
                ComponentType.ReadOnly<UnitAttack>(),
                ComponentType.ReadOnly<LocalTransform>());
            _entityType = state.GetEntityTypeHandle();
            EnsureCommandEntity(state.EntityManager, _queueQuery);
            state.RequireForUpdate(_queueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            _entityType.Update(ref state);
            ProcessPendingRequests(state.EntityManager, _queueQuery, _selectedAttackQuery, _entityType);
        }

        public static int EnqueueSelectedAttackTarget(EntityManager em, Entity targetEntity)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitAttackOrderQueueComponent queue = em.GetComponentData<UnitAttackOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Add(new UnitAttackOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                TargetEntity = targetEntity,
                Kind = UnitAttackOrderRequestKind.SelectedAttackTarget
            });
            return queue.LastRequestId;
        }

        public static int EnqueueDirectAttackTarget(
            EntityManager em,
            Entity sourceEntity,
            Entity targetEntity,
            Unity.Mathematics.int2 targetCell,
            Unity.Mathematics.float3 targetPosition)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitAttackOrderQueueComponent queue = em.GetComponentData<UnitAttackOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Add(new UnitAttackOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                TargetCell = targetCell,
                TargetPosition = targetPosition,
                Kind = UnitAttackOrderRequestKind.DirectAttackTarget
            });
            return queue.LastRequestId;
        }

        public static int EnqueueSourceAttackTarget(
            EntityManager em,
            Entity sourceEntity,
            Entity targetEntity)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitAttackOrderQueueComponent queue = em.GetComponentData<UnitAttackOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Add(new UnitAttackOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                Kind = UnitAttackOrderRequestKind.SourceAttackTarget
            });
            return queue.LastRequestId;
        }

        public static int EnqueueSourceBaseBreachAttackTarget(
            EntityManager em,
            Entity sourceEntity,
            Entity targetEntity,
            Entity breachTarget,
            Unity.Mathematics.int2 breachCell,
            Unity.Mathematics.float3 breachPosition)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitAttackOrderQueueComponent queue = em.GetComponentData<UnitAttackOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Add(new UnitAttackOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                BreachTargetEntity = breachTarget,
                BreachCell = breachCell,
                BreachPosition = breachPosition,
                Kind = UnitAttackOrderRequestKind.SourceBaseBreachAttackTarget
            });
            return queue.LastRequestId;
        }

        public static int EnqueueRadarAttackTarget(
            EntityManager em,
            Entity launcher,
            byte factionId,
            bool requireAirTarget)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitAttackOrderQueueComponent queue = em.GetComponentData<UnitAttackOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Add(new UnitAttackOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                SourceEntity = launcher,
                FactionId = factionId,
                RequireAirTarget = requireAirTarget ? (byte)1 : (byte)0,
                Kind = UnitAttackOrderRequestKind.RadarAttackTarget
            });
            return queue.LastRequestId;
        }

        public static int EnqueueClearCommandedAttackOrder(EntityManager em, Entity entity)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitAttackOrderQueueComponent queue = em.GetComponentData<UnitAttackOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Add(new UnitAttackOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                SourceEntity = entity,
                Kind = UnitAttackOrderRequestKind.ClearCommandedAttackOrder
            });
            return queue.LastRequestId;
        }

        public static int EnqueueClearAccidentalAirSelectionMove(EntityManager em, Entity entity)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitAttackOrderQueueComponent queue = em.GetComponentData<UnitAttackOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Add(new UnitAttackOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                SourceEntity = entity,
                Kind = UnitAttackOrderRequestKind.ClearAccidentalAirSelectionMove
            });
            return queue.LastRequestId;
        }

        public static bool EnqueueAndProcessSelectedAttackTarget(EntityManager em, Entity targetEntity)
        {
            int requestId = EnqueueSelectedAttackTarget(em, targetEntity);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitAttackOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static bool EnqueueAndProcessDirectAttackTarget(
            EntityManager em,
            Entity sourceEntity,
            Entity targetEntity,
            Unity.Mathematics.int2 targetCell,
            Unity.Mathematics.float3 targetPosition)
        {
            int requestId = EnqueueDirectAttackTarget(em, sourceEntity, targetEntity, targetCell, targetPosition);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitAttackOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static bool EnqueueAndProcessRadarAttackTarget(
            EntityManager em,
            Entity launcher,
            byte factionId,
            bool requireAirTarget,
            out UnitAttackOrderResultElement result)
        {
            int requestId = EnqueueRadarAttackTarget(em, launcher, factionId, requireAirTarget);
            ProcessPendingRequests(em);
            if (TryGetResult(em, requestId, out result))
                return result.Issued != 0;

            result = default;
            return false;
        }

        public static bool EnqueueAndProcessClearCommandedAttackOrder(EntityManager em, Entity entity)
        {
            int requestId = EnqueueClearCommandedAttackOrder(em, entity);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitAttackOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static bool EnqueueAndProcessClearAccidentalAirSelectionMove(EntityManager em, Entity entity)
        {
            int requestId = EnqueueClearAccidentalAirSelectionMove(em, entity);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitAttackOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static void ProcessPendingRequests(EntityManager em)
        {
            using EntityQuery queueQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitAttackOrderQueueComponent>());
            using EntityQuery selectedAttackQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitCombat>(),
                ComponentType.ReadOnly<UnitAttack>(),
                ComponentType.ReadOnly<LocalTransform>());
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ProcessPendingRequests(em, queueQuery, selectedAttackQuery, entityType);
        }

        internal static void ProcessPendingRequests(
            EntityManager em,
            EntityQuery selectedAttackQuery,
            EntityTypeHandle entityType)
        {
            using EntityQuery queueQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitAttackOrderQueueComponent>());
            ProcessPendingRequests(em, queueQuery, selectedAttackQuery, entityType);
        }

        public static bool TryGetResult(EntityManager em, int requestId, out UnitAttackOrderResultElement result)
        {
            result = default;
            Entity queueEntity = EnsureCommandEntity(em);
            DynamicBuffer<UnitAttackOrderResultElement> results = em.GetBuffer<UnitAttackOrderResultElement>(queueEntity);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].RequestId == requestId)
                {
                    result = results[i];
                    return true;
                }
            }

            return false;
        }

        private static void ProcessPendingRequests(
            EntityManager em,
            EntityQuery queueQuery,
            EntityQuery selectedAttackQuery,
            EntityTypeHandle entityType)
        {
            Entity queueEntity = EnsureCommandEntity(em, queueQuery);
            DynamicBuffer<UnitAttackOrderRequestElement> requests = em.GetBuffer<UnitAttackOrderRequestElement>(queueEntity);
            if (requests.Length == 0)
                return;

            using NativeList<UnitAttackOrderRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
                pendingRequests.Add(requests[i]);
            requests.Clear();

            DynamicBuffer<UnitAttackOrderResultElement> results = em.GetBuffer<UnitAttackOrderResultElement>(queueEntity);
            results.Clear();

            using NativeList<Entity> selectedEntities = new(selectedAttackQuery.CalculateEntityCount(), Allocator.Temp);
            CollectSelectedAttackSourceEntities(selectedAttackQuery, entityType, selectedEntities);

            var targetOrderSystem = new UnitTargetOrderSystem();
            NativeArray<UnitAttackOrderRequestElement> pendingRequestArray = pendingRequests.AsArray();
            for (int i = 0; i < pendingRequestArray.Length; i++)
            {
                UnitAttackOrderRequestElement request = pendingRequestArray[i];
                UnitTargetOrderSystem.AttackOrderIssueResult issueResult = ApplyRequest(
                    em,
                    targetOrderSystem,
                    selectedEntities.AsArray(),
                    request);

                results = em.GetBuffer<UnitAttackOrderResultElement>(queueEntity);
                results.Add(ToResult(request, issueResult));
            }
        }

        private static UnitTargetOrderSystem.AttackOrderIssueResult ApplyRequest(
            EntityManager em,
            UnitTargetOrderSystem targetOrderSystem,
            NativeArray<Entity> selectedEntities,
            UnitAttackOrderRequestElement request)
        {
            switch (request.Kind)
            {
                case UnitAttackOrderRequestKind.SelectedAttackTarget:
                    return targetOrderSystem.IssueAttackTarget(em, selectedEntities, request.TargetEntity);
                case UnitAttackOrderRequestKind.SourceAttackTarget:
                    return IssueSourceAttackTarget(em, targetOrderSystem, request, useBaseBreach: false);
                case UnitAttackOrderRequestKind.SourceBaseBreachAttackTarget:
                    return IssueSourceAttackTarget(em, targetOrderSystem, request, useBaseBreach: true);
                case UnitAttackOrderRequestKind.DirectAttackTarget:
                    return IssueDirectAttackTarget(em, targetOrderSystem, request);
                case UnitAttackOrderRequestKind.RadarAttackTarget:
                    return IssueRadarAttackTarget(em, targetOrderSystem, request);
                case UnitAttackOrderRequestKind.ClearCommandedAttackOrder:
                    return ClearCommandedAttackOrder(em, targetOrderSystem, request);
                case UnitAttackOrderRequestKind.ClearAccidentalAirSelectionMove:
                    return ClearAccidentalAirSelectionMove(em, targetOrderSystem, request);
                default:
                    return new UnitTargetOrderSystem.AttackOrderIssueResult(
                        TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable),
                        0,
                        Entity.Null,
                        default);
            }
        }

        private static UnitTargetOrderSystem.AttackOrderIssueResult IssueSourceAttackTarget(
            EntityManager em,
            UnitTargetOrderSystem targetOrderSystem,
            UnitAttackOrderRequestElement request,
            bool useBaseBreach)
        {
            if (request.SourceEntity == Entity.Null ||
                request.TargetEntity == Entity.Null)
            {
                return new UnitTargetOrderSystem.AttackOrderIssueResult(
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable),
                    0,
                    Entity.Null,
                    default);
            }

            UnitTargetOrderSystem.TryResolveBaseBreachTargetDelegate resolver = null;
            if (useBaseBreach)
            {
                resolver = (
                    byte factionId,
                    Entity targetEntity,
                    Unity.Mathematics.int2 targetCell,
                    Unity.Mathematics.int2 attackerCell,
                    out Entity breachTarget,
                    out Unity.Mathematics.int2 breachCell,
                    out Unity.Mathematics.float3 breachPosition) =>
                {
                    breachTarget = request.BreachTargetEntity;
                    breachCell = request.BreachCell;
                    breachPosition = request.BreachPosition;
                    return targetEntity == request.TargetEntity &&
                           breachTarget != Entity.Null &&
                           em.Exists(breachTarget);
                };
            }

            NativeArray<Entity> sourceEntity = new(1, Allocator.Temp);
            try
            {
                sourceEntity[0] = request.SourceEntity;
                return targetOrderSystem.IssueAttackTarget(em, sourceEntity, request.TargetEntity, resolver);
            }
            finally
            {
                sourceEntity.Dispose();
            }
        }

        private static UnitTargetOrderSystem.AttackOrderIssueResult IssueDirectAttackTarget(
            EntityManager em,
            UnitTargetOrderSystem targetOrderSystem,
            UnitAttackOrderRequestElement request)
        {
            if (request.SourceEntity == Entity.Null ||
                request.TargetEntity == Entity.Null ||
                !em.Exists(request.SourceEntity) ||
                !em.Exists(request.TargetEntity))
            {
                return new UnitTargetOrderSystem.AttackOrderIssueResult(
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable),
                    0,
                    Entity.Null,
                    default);
            }

            targetOrderSystem.IssueDirectAttackTarget(
                em,
                request.SourceEntity,
                request.TargetEntity,
                request.TargetCell,
                request.TargetPosition);
            return new UnitTargetOrderSystem.AttackOrderIssueResult(
                TacticalCommandResult.Success(),
                1,
                request.TargetEntity,
                request.TargetPosition);
        }

        private static UnitTargetOrderSystem.AttackOrderIssueResult IssueRadarAttackTarget(
            EntityManager em,
            UnitTargetOrderSystem targetOrderSystem,
            UnitAttackOrderRequestElement request)
        {
            if (request.SourceEntity == Entity.Null ||
                !em.Exists(request.SourceEntity) ||
                !targetOrderSystem.TryFindRadarTargetForMissileLauncher(
                    em,
                    request.FactionId,
                    request.RequireAirTarget != 0,
                    request.SourceEntity,
                    out Entity target,
                    out Unity.Mathematics.int2 targetCell,
                    out Unity.Mathematics.float3 targetPosition))
            {
                return new UnitTargetOrderSystem.AttackOrderIssueResult(
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable),
                    0,
                    Entity.Null,
                    default);
            }

            targetOrderSystem.IssueDirectAttackTarget(em, request.SourceEntity, target, targetCell, targetPosition);
            return new UnitTargetOrderSystem.AttackOrderIssueResult(
                TacticalCommandResult.Success(),
                1,
                target,
                targetPosition);
        }

        private static UnitTargetOrderSystem.AttackOrderIssueResult ClearCommandedAttackOrder(
            EntityManager em,
            UnitTargetOrderSystem targetOrderSystem,
            UnitAttackOrderRequestElement request)
        {
            if (request.SourceEntity == Entity.Null ||
                !em.Exists(request.SourceEntity))
            {
                return new UnitTargetOrderSystem.AttackOrderIssueResult(
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable),
                    0,
                    Entity.Null,
                    default);
            }

            targetOrderSystem.ClearCommandedAttackOrderComponents(em, request.SourceEntity);
            return new UnitTargetOrderSystem.AttackOrderIssueResult(
                TacticalCommandResult.Success(),
                1,
                Entity.Null,
                default);
        }

        private static UnitTargetOrderSystem.AttackOrderIssueResult ClearAccidentalAirSelectionMove(
            EntityManager em,
            UnitTargetOrderSystem targetOrderSystem,
            UnitAttackOrderRequestElement request)
        {
            if (request.SourceEntity == Entity.Null ||
                !em.Exists(request.SourceEntity))
            {
                return new UnitTargetOrderSystem.AttackOrderIssueResult(
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable),
                    0,
                    Entity.Null,
                    default);
            }

            targetOrderSystem.ClearAccidentalAirSelectionMove(em, request.SourceEntity);
            return new UnitTargetOrderSystem.AttackOrderIssueResult(
                TacticalCommandResult.Success(),
                1,
                Entity.Null,
                default);
        }

        private static UnitAttackOrderResultElement ToResult(
            UnitAttackOrderRequestElement request,
            UnitTargetOrderSystem.AttackOrderIssueResult issueResult)
        {
            bool issued = issueResult.CommandResult.Accepted && issueResult.IssuedCount > 0;
            return new UnitAttackOrderResultElement
            {
                RequestId = request.RequestId,
                TargetEntity = issueResult.TargetEntity,
                TargetPosition = issueResult.TargetPosition,
                IssuedCount = issueResult.IssuedCount,
                ReasonCode = (int)issueResult.CommandResult.ReasonCode,
                Issued = issued ? (byte)1 : (byte)0,
                HasCommandResult = (byte)1,
                Accepted = issueResult.CommandResult.Accepted ? (byte)1 : (byte)0,
                Message = new FixedString64Bytes(issueResult.CommandResult.Message ?? string.Empty)
            };
        }

        private static void CollectSelectedAttackSourceEntities(
            EntityQuery selectedAttackQuery,
            EntityTypeHandle entityType,
            NativeList<Entity> selectedEntities)
        {
            selectedEntities.Clear();
            if (selectedAttackQuery.IsEmptyIgnoreFilter)
                return;

            using NativeArray<ArchetypeChunk> chunks = selectedAttackQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < chunkEntities.Length; i++)
                    selectedEntities.Add(chunkEntities[i]);
            }
        }

        internal static Entity EnsureCommandEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitAttackOrderQueueComponent>());
            return EnsureCommandEntity(em, query);
        }

        private static Entity EnsureCommandEntity(EntityManager em, EntityQuery query)
        {
            Entity entity;
            if (!query.IsEmptyIgnoreFilter)
            {
                entity = query.GetSingletonEntity();
                EnsureBuffers(em, entity);
                return entity;
            }

            entity = em.CreateEntity(typeof(UnitAttackOrderQueueComponent));
            em.SetName(entity, "UnitAttackOrders");
            EnsureBuffers(em, entity);
            return entity;
        }

        private static void EnsureBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<UnitAttackOrderRequestElement>(entity))
                em.AddBuffer<UnitAttackOrderRequestElement>(entity);
            if (!em.HasBuffer<UnitAttackOrderResultElement>(entity))
                em.AddBuffer<UnitAttackOrderResultElement>(entity);
        }
    }
}
