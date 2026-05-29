using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchStartSystem
{
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
            (queue.HasStarted != 0 && IsMatchLoaded()))
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

        if (!IsMatchLoaded())
        {
            EnqueueResultIfStatusChanged(em, entity, ref queue, MatchStartStatusKind.WaitingForMatchLoaded, "Waiting for Match scene load before gameplay start.");
            return;
        }

        if (!TryStartLoadedMatch(out string message))
        {
            EnqueueResultIfStatusChanged(em, entity, ref queue, MatchStartStatusKind.WaitingForMatchLoaded, message);
            return;
        }

        queue.IsStartPending = 0;
        queue.HasStarted = 1;
        queue.LastStatus = MatchStartStatusKind.Started;
        em.SetComponentData(entity, queue);
        EnqueueResult(em, entity, queue.ActiveRequestId, MatchStartStatusKind.Started, message);
    }

    private Entity EnsureMatchStartEntity(EntityManager em)
    {
        World world = em.World;
        if (_world == world &&
            _matchStartEntity != Entity.Null &&
            em.Exists(_matchStartEntity) &&
            em.HasComponent<MatchStartBoundaryComponent>(_matchStartEntity))
        {
            EnsureBuffers(em, _matchStartEntity);
            return _matchStartEntity;
        }

        _world = world;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MatchStartBoundaryComponent>());
        if (!query.IsEmptyIgnoreFilter)
        {
            _matchStartEntity = query.GetSingletonEntity();
            EnsureBuffers(em, _matchStartEntity);
            return _matchStartEntity;
        }

        _matchStartEntity = em.CreateEntity(typeof(MatchStartBoundaryComponent), typeof(MatchStartQueueComponent));
        em.SetName(_matchStartEntity, "MatchStartBoundary");
        EnsureBuffers(em, _matchStartEntity);
        return _matchStartEntity;
    }

    private static void EnsureBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<MatchStartRequestElement>(entity))
            em.AddBuffer<MatchStartRequestElement>(entity);
        if (!em.HasBuffer<MatchStartResultElement>(entity))
            em.AddBuffer<MatchStartResultElement>(entity);
    }

    private static bool IsMatchLoaded()
    {
        Scene scene = SceneManager.GetSceneByName(SceneLifecycleSystem.MatchSceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private static bool TryStartLoadedMatch(out string message)
    {
        foreach (MatchSceneView matchScene in Resources.FindObjectsOfTypeAll<MatchSceneView>())
        {
            if (matchScene == null ||
                matchScene.gameObject == null ||
                !matchScene.gameObject.scene.IsValid() ||
                !matchScene.gameObject.scene.isLoaded)
            {
                continue;
            }

            try
            {
                matchScene.BeginGameplay();
                message = $"Gameplay start invoked from loaded Match scene. scene={matchScene.gameObject.scene.name}";
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                message = exception.Message;
                return false;
            }
        }

        message = "Loaded Match scene has no active MatchSceneView.";
        return false;
    }

    private static void EnqueueResult(EntityManager em, Entity entity, int requestId, MatchStartStatusKind status, string message)
    {
        em.GetBuffer<MatchStartResultElement>(entity).Add(new MatchStartResultElement
        {
            RequestId = requestId,
            Status = status,
            Message = new FixedString128Bytes(message ?? string.Empty)
        });
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
