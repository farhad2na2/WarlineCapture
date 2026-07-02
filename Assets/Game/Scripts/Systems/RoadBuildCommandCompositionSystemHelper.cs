using System;
using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    public sealed class RoadBuildCommandCompositionSystemHelper
    {
        public struct Context
        {
            public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
            public readonly RoadBuildSessionCompositionSystemHelper SessionSystem;
            public readonly RoadBuildSessionCompositionSystemHelper.Context SessionContext;
            public readonly Action ClearRoadBuildDragState;

            public Context(
                RuntimeGameplayStateSystem runtimeGameplayStateSystem,
                RoadBuildSessionCompositionSystemHelper sessionSystem,
                RoadBuildSessionCompositionSystemHelper.Context sessionContext,
                Action clearRoadBuildDragState)
            {
                RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
                SessionSystem = sessionSystem;
                SessionContext = sessionContext;
                ClearRoadBuildDragState = clearRoadBuildDragState;
            }
        }

        public int EnqueueEnterRoadBuildMode(EntityManager em)
        {
            return EnqueueRoadBuildCommand(em, RoadBuildCommandRequestElement.KindEnterRoadBuildMode);
        }

        public bool EnqueueAndProcessEnterRoadBuildMode(EntityManager em, Context context)
        {
            int requestId = EnqueueEnterRoadBuildMode(em);
            ProcessPendingRoadBuildCommands(em, context);
            return TryGetRoadBuildCommandResult(em, requestId, out RoadBuildCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public int EnqueueConfirmRoadBuildSession(EntityManager em)
        {
            return EnqueueRoadBuildCommand(em, RoadBuildCommandRequestElement.KindConfirmRoadBuildSession);
        }

        public bool EnqueueAndProcessConfirmRoadBuildSession(EntityManager em, Context context)
        {
            int requestId = EnqueueConfirmRoadBuildSession(em);
            ProcessPendingRoadBuildCommands(em, context);
            return TryGetRoadBuildCommandResult(em, requestId, out RoadBuildCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public int EnqueueCancelRoadBuildSession(EntityManager em)
        {
            return EnqueueRoadBuildCommand(em, RoadBuildCommandRequestElement.KindCancelRoadBuildSession);
        }

        public bool EnqueueAndProcessCancelRoadBuildSession(EntityManager em, Context context)
        {
            int requestId = EnqueueCancelRoadBuildSession(em);
            ProcessPendingRoadBuildCommands(em, context);
            return TryGetRoadBuildCommandResult(em, requestId, out RoadBuildCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public int EnqueueExitBuildMode(EntityManager em)
        {
            return EnqueueRoadBuildCommand(em, RoadBuildCommandRequestElement.KindExitBuildMode);
        }

        public bool EnqueueAndProcessExitBuildMode(EntityManager em, Context context)
        {
            int requestId = EnqueueExitBuildMode(em);
            ProcessPendingRoadBuildCommands(em, context);
            return TryGetRoadBuildCommandResult(em, requestId, out RoadBuildCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public bool TryGetRoadBuildCommandResult(
            EntityManager em,
            int requestId,
            out RoadBuildCommandResultElement result)
        {
            result = default;
            Entity queueEntity = EnsureRoadBuildCommandEntity(em);
            DynamicBuffer<RoadBuildCommandResultElement> results =
                em.GetBuffer<RoadBuildCommandResultElement>(queueEntity);
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

        public void ProcessPendingRoadBuildCommands(EntityManager em, Context context)
        {
            Entity queueEntity = EnsureRoadBuildCommandEntity(em);
            DynamicBuffer<RoadBuildCommandRequestElement> requests =
                em.GetBuffer<RoadBuildCommandRequestElement>(queueEntity);
            if (requests.Length == 0)
                return;

            using NativeList<RoadBuildCommandRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
                pendingRequests.Add(requests[i]);
            requests.Clear();

            DynamicBuffer<RoadBuildCommandResultElement> results =
                em.GetBuffer<RoadBuildCommandResultElement>(queueEntity);
            results.Clear();

            NativeArray<RoadBuildCommandRequestElement> pendingArray = pendingRequests.AsArray();
            for (int i = 0; i < pendingArray.Length; i++)
            {
                RoadBuildCommandRequestElement request = pendingArray[i];
                bool accepted = ProcessRoadBuildCommand(context, request, out byte resultCode);
                results = em.GetBuffer<RoadBuildCommandResultElement>(queueEntity);
                results.Add(new RoadBuildCommandResultElement
                {
                    RequestId = request.RequestId,
                    RequestKind = request.RequestKind,
                    Accepted = accepted ? (byte)1 : (byte)0,
                    ResultCode = resultCode
                });
            }
        }

        private static bool ProcessRoadBuildCommand(
            Context context,
            RoadBuildCommandRequestElement request,
            out byte resultCode)
        {
            if (context.SessionSystem == null)
            {
                resultCode = RoadBuildCommandResultElement.MissingSession;
                return false;
            }

            if (context.SessionContext.State == null)
            {
                resultCode = RoadBuildCommandResultElement.MissingSessionState;
                return false;
            }

            bool requiresRuntimeState =
                request.RequestKind == RoadBuildCommandRequestElement.KindEnterRoadBuildMode ||
                request.RequestKind == RoadBuildCommandRequestElement.KindExitBuildMode;
            switch (request.RequestKind)
            {
                case RoadBuildCommandRequestElement.KindEnterRoadBuildMode:
                    if (context.SessionSystem.ActivateRoadBuildMode(context.SessionContext))
                    {
                        resultCode = RoadBuildCommandResultElement.Completed;
                        return true;
                    }

                    resultCode = RoadBuildCommandResultElement.Rejected;
                    return false;

                case RoadBuildCommandRequestElement.KindConfirmRoadBuildSession:
                    context.SessionSystem.ConfirmRoadBuildSession(context.SessionContext);
                    resultCode = RoadBuildCommandResultElement.Completed;
                    return true;

                case RoadBuildCommandRequestElement.KindCancelRoadBuildSession:
                    if (context.SessionSystem.CancelRoadBuildSession(context.SessionContext))
                    {
                        resultCode = RoadBuildCommandResultElement.Completed;
                        return true;
                    }

                    resultCode = RoadBuildCommandResultElement.Rejected;
                    return false;

                case RoadBuildCommandRequestElement.KindExitBuildMode:
                    context.ClearRoadBuildDragState?.Invoke();
                    context.SessionSystem.ExitBuildMode(context.SessionContext);
                    resultCode = RoadBuildCommandResultElement.Completed;
                    return true;

                default:
                    resultCode = RoadBuildCommandResultElement.Rejected;
                    return false;
            }
        }

        private static int EnqueueRoadBuildCommand(EntityManager em, byte requestKind)
        {
            Entity queueEntity = EnsureRoadBuildCommandEntity(em);
            RoadBuildCommandQueueComponent queue =
                em.GetComponentData<RoadBuildCommandQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<RoadBuildCommandRequestElement>(queueEntity).Add(new RoadBuildCommandRequestElement
            {
                RequestId = queue.LastRequestId,
                RequestKind = requestKind
            });
            return queue.LastRequestId;
        }

        private static Entity EnsureRoadBuildCommandEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RoadBuildCommandQueueComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                Entity existing = query.GetSingletonEntity();
                EnsureRoadBuildCommandBuffers(em, existing);
                return existing;
            }

            Entity entity = em.CreateEntity(typeof(RoadBuildCommandQueueComponent));
            em.SetName(entity, "RoadBuildCommands");
            EnsureRoadBuildCommandBuffers(em, entity);
            return entity;
        }

        private static void EnsureRoadBuildCommandBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<RoadBuildCommandRequestElement>(entity))
                em.AddBuffer<RoadBuildCommandRequestElement>(entity);
            if (!em.HasBuffer<RoadBuildCommandResultElement>(entity))
                em.AddBuffer<RoadBuildCommandResultElement>(entity);
        }
    }
}
