using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    [UpdateBefore(typeof(UnitPathfindingSystem))]
    public partial struct UnitMoveOrderRequestSystem : ISystem
    {
        private EntityQuery _queueQuery;

        public void OnCreate(ref SystemState state)
        {
            _queueQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<UnitMoveOrderQueueComponent>(),
                ComponentType.ReadWrite<UnitMoveOrderRequestElement>(),
                ComponentType.ReadWrite<UnitMoveOrderResultElement>());
            EnsureCommandEntity(state.EntityManager, _queueQuery);
            state.RequireForUpdate(_queueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            ProcessPendingRequests(state.EntityManager, _queueQuery);
        }

        public static int EnqueueGroupedManualMoveOrder(
            EntityManager em,
            Entity entity,
            int2 goal,
            bool issueGroundPathNow,
            bool useGroundPathRetryCooldown,
            int resumeFrame,
            int currentFrame)
        {
            return EnqueueMoveOrder(
                em,
                entity,
                goal,
                UnitMoveOrderRequestKind.GroupedManual,
                issueGroundPathNow,
                useGroundPathRetryCooldown,
                resumeFrame,
                currentFrame);
        }

        public static int EnqueueImmediateMoveOrder(EntityManager em, Entity entity, int2 goal)
        {
            return EnqueueMoveOrder(
                em,
                entity,
                goal,
                UnitMoveOrderRequestKind.Immediate,
                issueGroundPathNow: true,
                useGroundPathRetryCooldown: false,
                resumeFrame: 0,
                currentFrame: 0);
        }

        public static bool EnqueueAndProcessImmediateMoveOrder(EntityManager em, Entity entity, int2 goal)
        {
            int requestId = EnqueueImmediateMoveOrder(em, entity, goal);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitMoveOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static int EnqueueTargetOnlyMoveOrder(EntityManager em, Entity entity, int2 goal)
        {
            return EnqueueMoveOrder(
                em,
                entity,
                goal,
                UnitMoveOrderRequestKind.TargetOnly,
                issueGroundPathNow: false,
                useGroundPathRetryCooldown: false,
                resumeFrame: 0,
                currentFrame: 0);
        }

        public static bool EnqueueAndProcessTargetOnlyMoveOrder(EntityManager em, Entity entity, int2 goal)
        {
            int requestId = EnqueueTargetOnlyMoveOrder(em, entity, goal);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitMoveOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static int EnqueueTargetPathMoveOrder(EntityManager em, Entity entity, int2 goal)
        {
            return EnqueueMoveOrder(
                em,
                entity,
                goal,
                UnitMoveOrderRequestKind.TargetPathOnly,
                issueGroundPathNow: true,
                useGroundPathRetryCooldown: false,
                resumeFrame: 0,
                currentFrame: 0);
        }

        public static bool EnqueueAndProcessTargetPathMoveOrder(EntityManager em, Entity entity, int2 goal)
        {
            int requestId = EnqueueTargetPathMoveOrder(em, entity, goal);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitMoveOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static int EnqueueClearMovementOrder(EntityManager em, Entity entity)
        {
            return EnqueueMoveOrder(
                em,
                entity,
                default,
                UnitMoveOrderRequestKind.ClearMovement,
                issueGroundPathNow: false,
                useGroundPathRetryCooldown: false,
                resumeFrame: 0,
                currentFrame: 0);
        }

        public static bool EnqueueAndProcessClearMovementOrder(EntityManager em, Entity entity)
        {
            int requestId = EnqueueClearMovementOrder(em, entity);
            ProcessPendingRequests(em);
            return TryGetResult(em, requestId, out UnitMoveOrderResultElement result) &&
                   result.Issued != 0;
        }

        public static void ClearMovementOrderComponents(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        {
            var moveOrderSystem = new UnitMoveOrderSystem();
            moveOrderSystem.ClearMovementOrderComponents(em, ecb, entity);
        }

        public static void ProcessPendingRequests(EntityManager em)
        {
            using EntityQuery queueQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitMoveOrderQueueComponent>());
            ProcessPendingRequests(em, queueQuery);
        }

        public static bool TryGetResult(EntityManager em, int requestId, out UnitMoveOrderResultElement result)
        {
            result = default;
            Entity queueEntity = EnsureCommandEntity(em);
            DynamicBuffer<UnitMoveOrderResultElement> results = em.GetBuffer<UnitMoveOrderResultElement>(queueEntity);
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

        private static int EnqueueMoveOrder(
            EntityManager em,
            Entity entity,
            int2 goal,
            UnitMoveOrderRequestKind kind,
            bool issueGroundPathNow,
            bool useGroundPathRetryCooldown,
            int resumeFrame,
            int currentFrame)
        {
            Entity queueEntity = EnsureCommandEntity(em);
            UnitMoveOrderQueueComponent queue = em.GetComponentData<UnitMoveOrderQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<UnitMoveOrderRequestElement>(queueEntity).Add(new UnitMoveOrderRequestElement
            {
                RequestId = queue.LastRequestId,
                Entity = entity,
                Goal = goal,
                Kind = kind,
                IssueGroundPathNow = issueGroundPathNow ? (byte)1 : (byte)0,
                UseGroundPathRetryCooldown = useGroundPathRetryCooldown ? (byte)1 : (byte)0,
                ResumeFrame = resumeFrame,
                CurrentFrame = currentFrame
            });
            return queue.LastRequestId;
        }

        private static void ProcessPendingRequests(EntityManager em, EntityQuery queueQuery)
        {
            Entity queueEntity = EnsureCommandEntity(em, queueQuery);
            DynamicBuffer<UnitMoveOrderRequestElement> requests = em.GetBuffer<UnitMoveOrderRequestElement>(queueEntity);
            if (requests.Length == 0)
                return;

            using NativeList<UnitMoveOrderRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
                pendingRequests.Add(requests[i]);
            requests.Clear();

            DynamicBuffer<UnitMoveOrderResultElement> results = em.GetBuffer<UnitMoveOrderResultElement>(queueEntity);
            results.Clear();

            var moveOrderSystem = new UnitMoveOrderSystem();
            NativeArray<UnitMoveOrderRequestElement> pendingRequestArray = pendingRequests.AsArray();
            for (int i = 0; i < pendingRequestArray.Length; i++)
            {
                UnitMoveOrderRequestElement request = pendingRequestArray[i];
                UnitMoveOrderSystem.MoveOrderCommandResult commandResult = default;
                if (request.Entity != Entity.Null && em.Exists(request.Entity))
                    commandResult = ApplyRequest(em, moveOrderSystem, request);

                results = em.GetBuffer<UnitMoveOrderResultElement>(queueEntity);
                results.Add(ToResult(request, commandResult));
            }
        }

        private static UnitMoveOrderSystem.MoveOrderCommandResult ApplyRequest(
            EntityManager em,
            UnitMoveOrderSystem moveOrderSystem,
            UnitMoveOrderRequestElement request)
        {
            if (request.Kind == UnitMoveOrderRequestKind.GroupedManual &&
                ShouldRejectManualMoveForFuel(em, request.Entity, request.Goal, out TacticalCommandReasonCode fuelReason))
            {
                return new UnitMoveOrderSystem.MoveOrderCommandResult
                {
                    Issued = false,
                    RejectionReasonCode = (int)fuelReason
                };
            }

            switch (request.Kind)
            {
                case UnitMoveOrderRequestKind.GroupedManual:
                    return moveOrderSystem.IssueGroupedManualMoveOrder(
                        em,
                        request.Entity,
                        request.Goal,
                        request.IssueGroundPathNow != 0,
                        request.UseGroundPathRetryCooldown != 0,
                        request.ResumeFrame,
                        request.CurrentFrame);
                case UnitMoveOrderRequestKind.Immediate:
                    moveOrderSystem.IssueImmediateMoveCommand(em, request.Entity, request.Goal);
                    return new UnitMoveOrderSystem.MoveOrderCommandResult { Issued = true };
                case UnitMoveOrderRequestKind.TargetOnly:
                    moveOrderSystem.IssueTargetOnlyMoveCommand(em, request.Entity, request.Goal);
                    return new UnitMoveOrderSystem.MoveOrderCommandResult { Issued = true };
                case UnitMoveOrderRequestKind.TargetPathOnly:
                    return ApplyTargetPathMoveOrder(em, request.Entity, request.Goal);
                case UnitMoveOrderRequestKind.ClearMovement:
                    ClearMovementOrderComponents(em, request.Entity);
                    return new UnitMoveOrderSystem.MoveOrderCommandResult { Issued = true };
                default:
                    return default;
            }
        }

        private static bool ShouldRejectManualMoveForFuel(
            EntityManager em,
            Entity entity,
            int2 goal,
            out TacticalCommandReasonCode reason)
        {
            reason = TacticalCommandReasonCode.None;
            if (entity == Entity.Null ||
                !em.Exists(entity) ||
                !em.HasComponent<UnitFuelConsumption>(entity))
            {
                return false;
            }

            UnitFuelConsumption consumption = em.GetComponentData<UnitFuelConsumption>(entity);
            if (consumption.Enabled == 0)
                return false;

            bool isAirUnit = em.HasComponent<UnitAirMovement>(entity);
            float fuelPerCell = isAirUnit
                ? math.max(0f, consumption.AirFuelPerCell)
                : math.max(0f, consumption.GroundFuelPerCell);
            if (fuelPerCell <= 0f)
                return false;

            int movedCells = 1;
            if (em.HasComponent<UnitGrid>(entity))
            {
                int2 currentCell = em.GetComponentData<UnitGrid>(entity).Cell;
                int2 delta = goal - currentCell;
                movedCells = math.abs(delta.x) + math.abs(delta.y);
                if (movedCells <= 0)
                    return false;
            }

            byte factionId = em.HasComponent<Faction>(entity)
                ? em.GetComponentData<Faction>(entity).Id
                : FactionIdentity.NeutralFactionId;
            float requiredFuel = movedCells * fuelPerCell;
            float usableFuel = CalculateUsableFuel(em, factionId);
            if (usableFuel + 0.001f >= requiredFuel)
                return false;

            reason = TacticalCommandReasonCode.InsufficientFuel;
            return true;
        }

        private static float CalculateUsableFuel(EntityManager em, byte factionId)
        {
            float usableFuel = 0f;
            using EntityQuery storageQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
            if (storageQuery.IsEmptyIgnoreFilter)
                return 0f;

            ComponentTypeHandle<BuildingResourceStorageComponent> storageType =
                em.GetComponentTypeHandle<BuildingResourceStorageComponent>(true);
            using NativeArray<ArchetypeChunk> chunks = storageQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<BuildingResourceStorageComponent> storages = chunks[chunkIndex].GetNativeArray(ref storageType);
                for (int i = 0; i < storages.Length; i++)
                {
                    BuildingResourceStorageComponent storage = storages[i];
                    if (storage.OwnerFactionId != factionId ||
                        storage.FuelStorageCapacity <= 0 ||
                        storage.FuelBarrelsPerDay > 0f ||
                        storage.OilBarrelsPerDay > 0f)
                    {
                        continue;
                    }

                    usableFuel += math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels);
                }
            }

            return usableFuel;
        }

        private static void ClearMovementOrderComponents(EntityManager em, Entity entity)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            try
            {
                ClearMovementOrderComponents(em, ecb, entity);
                ecb.Playback(em);
            }
            finally
            {
                ecb.Dispose();
            }
        }

        private static UnitMoveOrderSystem.MoveOrderCommandResult ApplyTargetPathMoveOrder(EntityManager em, Entity entity, int2 goal)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            try
            {
                UnitMoveOrderSystem.MoveOrderCommandResult result = ApplyTargetPathMoveOrder(em, ecb, entity, goal);
                ecb.Playback(em);
                return result;
            }
            finally
            {
                ecb.Dispose();
            }
        }

        internal static UnitMoveOrderSystem.MoveOrderCommandResult ApplyTargetPathMoveOrder(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity entity,
            int2 goal)
        {
            UnitMoveOrderSystem.MoveOrderCommandResult result = new()
            {
                Issued = true,
                PathRequests = 1
            };
            if (em.HasComponent<UnitTarget>(entity))
            {
                ecb.SetComponent(entity, new UnitTarget { Cell = goal });
            }
            else
            {
                ecb.AddComponent(entity, new UnitTarget { Cell = goal });
                result.StructuralAdds++;
            }

            if (em.HasComponent<UnitPathRequest>(entity))
            {
                ecb.SetComponent(entity, new UnitPathRequest { Goal = goal });
            }
            else
            {
                ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
                result.StructuralAdds++;
            }

            return result;
        }

        private static UnitMoveOrderResultElement ToResult(
            UnitMoveOrderRequestElement request,
            UnitMoveOrderSystem.MoveOrderCommandResult commandResult)
        {
            return new UnitMoveOrderResultElement
            {
                RequestId = request.RequestId,
                Entity = request.Entity,
                Goal = request.Goal,
                Issued = commandResult.Issued ? (byte)1 : (byte)0,
                StructuralAdds = commandResult.StructuralAdds,
                StructuralRemoves = commandResult.StructuralRemoves,
                PathRequests = commandResult.PathRequests,
                StaggeredPathRequests = commandResult.StaggeredPathRequests,
                MaxStaggerDelayFrames = commandResult.MaxStaggerDelayFrames,
                AirUnits = commandResult.AirUnits,
                RejectionReasonCode = commandResult.RejectionReasonCode
            };
        }

        private static Entity EnsureCommandEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitMoveOrderQueueComponent>());
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

            entity = em.CreateEntity(typeof(UnitMoveOrderQueueComponent));
            em.SetName(entity, "UnitMoveOrders");
            EnsureBuffers(em, entity);
            return entity;
        }

        private static void EnsureBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<UnitMoveOrderRequestElement>(entity))
                em.AddBuffer<UnitMoveOrderRequestElement>(entity);
            if (!em.HasBuffer<UnitMoveOrderResultElement>(entity))
                em.AddBuffer<UnitMoveOrderResultElement>(entity);
        }
    }
}
