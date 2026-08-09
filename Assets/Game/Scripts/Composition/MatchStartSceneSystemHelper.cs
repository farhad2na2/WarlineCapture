using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Composition
{
    public sealed class MatchStartSceneSystemHelper
    {
        private readonly MatchSceneReferenceCompositionSystemHelper _matchSceneReferenceSystem = new();
        private World _world;
        private Entity _matchStartEntity;

        public bool QueueStartAfterMatchLoaded(EntityManager em)
        {
            Entity entity = EnsureMatchStartEntity(em);
            if (entity == Entity.Null || !em.Exists(entity))
                return false;

            MatchStartQueueComponent queue = em.GetComponentData<MatchStartQueueComponent>(entity);
            DynamicBuffer<MatchStartRequestElement> requests = em.GetBuffer<MatchStartRequestElement>(entity);
            if (queue.IsStartPending != 0 ||
                requests.Length > 0 ||
                (queue.HasStarted != 0 && _matchSceneReferenceSystem.TryGet(em, out _)))
            {
                return true;
            }

            queue.LastRequestId++;
            requests.Add(new MatchStartRequestElement
            {
                RequestId = queue.LastRequestId,
                RequireMatchLoaded = 1
            });
            em.SetComponentData(entity, queue);
            return true;
        }

        public void Update(EntityManager em)
        {
            Entity entity = EnsureMatchStartEntity(em);
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            MatchStartQueueComponent queue = em.GetComponentData<MatchStartQueueComponent>(entity);
            DynamicBuffer<MatchStartRequestElement> requests = em.GetBuffer<MatchStartRequestElement>(entity);
            if (queue.IsStartPending == 0 && requests.Length > 0)
            {
                MatchStartRequestElement request = requests[0];
                requests.RemoveAt(0);
                queue.ActiveRequestId = request.RequestId;
                queue.IsStartPending = 1;
                queue.HasStarted = 0;
                queue.LastStatus = MatchStartStatusKind.Queued;
                em.SetComponentData(entity, queue);
                EnqueueResult(em, entity, request.RequestId, MatchStartStatusKind.Queued, "Match start request queued.");
            }

            queue = em.GetComponentData<MatchStartQueueComponent>(entity);
            if (queue.IsStartPending == 0 || queue.HasStarted != 0)
                return;

            if (!_matchSceneReferenceSystem.TryGet(em, out _))
            {
                EnqueueResultIfStatusChanged(em, entity, ref queue, MatchStartStatusKind.WaitingForMatchLoaded, "Waiting for Match scene load before gameplay start.");
                return;
            }

            if (!TryStartLoadedMatch(em, out MatchStartStatusKind waitStatus, out string message, out float progress01))
            {
                SetProgress(em, entity, progress01, message);
                if (waitStatus == MatchStartStatusKind.Failed)
                {
                    queue.IsStartPending = 0;
                    queue.HasStarted = 0;
                    queue.LastStatus = MatchStartStatusKind.Failed;
                    em.SetComponentData(entity, queue);
                    EnqueueResult(em, entity, queue.ActiveRequestId, MatchStartStatusKind.Failed, message);
                    return;
                }

                EnqueueResultIfStatusChanged(em, entity, ref queue, waitStatus, message);
                return;
            }

            queue.IsStartPending = 0;
            queue.HasStarted = 1;
            queue.LastStatus = MatchStartStatusKind.Started;
            em.SetComponentData(entity, queue);
            SetProgress(em, entity, 1f, message);
            EnqueueResult(em, entity, queue.ActiveRequestId, MatchStartStatusKind.Started, message);
        }

        private Entity EnsureMatchStartEntity(EntityManager em)
        {
            World world = em.World;
            if (_world == world &&
                _matchStartEntity != Entity.Null &&
                em.Exists(_matchStartEntity) &&
                em.HasComponent<MatchStartStateComponent>(_matchStartEntity))
            {
                EnsureBuffers(em, _matchStartEntity);
                return _matchStartEntity;
            }

            _world = world;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MatchStartStateComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                _matchStartEntity = query.GetSingletonEntity();
                EnsureBuffers(em, _matchStartEntity);
                return _matchStartEntity;
            }

            _matchStartEntity = em.CreateEntity(typeof(MatchStartStateComponent), typeof(MatchStartQueueComponent));
            em.SetName(_matchStartEntity, "MatchStartBoundary");
            EnsureBuffers(em, _matchStartEntity);
            EnsureProgress(em, _matchStartEntity);
            return _matchStartEntity;
        }

        private static void EnsureBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<MatchStartRequestElement>(entity))
                em.AddBuffer<MatchStartRequestElement>(entity);
            if (!em.HasBuffer<MatchStartResultElement>(entity))
                em.AddBuffer<MatchStartResultElement>(entity);
            EnsureProgress(em, entity);
        }

        private static void EnsureProgress(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<MatchStartProgressComponent>(entity))
            {
                em.AddComponentData(entity, new MatchStartProgressComponent
                {
                    Progress01 = 0f,
                    Status = new FixedString64Bytes("Waiting for match")
                });
            }
        }

        private bool TryStartLoadedMatch(
            EntityManager entityManager,
            out MatchStartStatusKind waitStatus,
            out string message,
            out float progress01)
        {
            progress01 = 0f;
            if (!_matchSceneReferenceSystem.TryGet(entityManager, out MatchSceneView matchScene))
            {
                waitStatus = MatchStartStatusKind.WaitingForMatchLoaded;
                message = "Loaded Match scene has no MatchSceneView.";
                return false;
            }

            try
            {
                if (!matchScene.GameplayStartRequested)
                    matchScene.BeginGameplay();

                if (matchScene.GameplayStartFailed)
                {
                    waitStatus = MatchStartStatusKind.Failed;
                    message = string.IsNullOrEmpty(matchScene.GameplayStartFailureMessage)
                        ? "Match gameplay startup failed."
                        : matchScene.GameplayStartFailureMessage;
                    progress01 = matchScene.GameplayStartProgress01;
                    return false;
                }

                if (!matchScene.GameplayStartComplete)
                {
                    waitStatus = MatchStartStatusKind.Starting;
                    message = string.IsNullOrEmpty(matchScene.GameplayStartStatus)
                        ? "Starting match gameplay."
                        : matchScene.GameplayStartStatus;
                    progress01 = matchScene.GameplayStartProgress01;
                    return false;
                }

                waitStatus = MatchStartStatusKind.Started;
                message = $"Gameplay start completed from loaded Match scene. scene={matchScene.gameObject.scene.name}";
                progress01 = 1f;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                waitStatus = MatchStartStatusKind.WaitingForMatchLoaded;
                message = exception.Message;
                progress01 = 0f;
                return false;
            }
        }

        private static void EnqueueResult(EntityManager em, Entity entity, int requestId, MatchStartStatusKind status, string message)
        {
            em.GetBuffer<MatchStartResultElement>(entity).Add(new MatchStartResultElement
            {
                RequestId = requestId,
                Status = status,
                Message = new FixedString128Bytes(ToFixed128Message(message))
            });
        }

        private static void SetProgress(EntityManager em, Entity entity, float progress01, string status)
        {
            EnsureProgress(em, entity);
            em.SetComponentData(entity, new MatchStartProgressComponent
            {
                Progress01 = Mathf.Clamp01(progress01),
                Status = new FixedString64Bytes(ToFixed64Status(status))
            });
        }

        private static string ToFixed64Status(string status)
        {
            const int MaxAsciiChars = 60;
            if (string.IsNullOrEmpty(status))
                return "Starting match";
            return status.Length <= MaxAsciiChars ? status : status.Substring(0, MaxAsciiChars);
        }

        private static string ToFixed128Message(string message)
        {
            const int MaxAsciiChars = 120;
            if (string.IsNullOrEmpty(message))
                return string.Empty;
            return message.Length <= MaxAsciiChars ? message : message.Substring(0, MaxAsciiChars);
        }

        private static void EnqueueResultIfStatusChanged(
            EntityManager em,
            Entity entity,
            ref MatchStartQueueComponent queue,
            MatchStartStatusKind status,
            string message)
        {
            if (queue.LastStatus == status)
                return;

            queue.LastStatus = status;
            em.SetComponentData(entity, queue);
            EnqueueResult(em, entity, queue.ActiveRequestId, status, message);
        }
    }
}
