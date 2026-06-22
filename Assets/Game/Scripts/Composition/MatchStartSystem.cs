using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchStartSystem
{
    private readonly MatchSceneReferenceSystem _matchSceneReferenceSystem = new();
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

        if (!TryStartLoadedMatch(out MatchStartStatusKind waitStatus, out string message, out float progress01))
        {
            SetProgress(em, entity, progress01, message);
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

    private static bool IsMatchLoaded()
    {
        Scene scene = SceneManager.GetSceneByName(SceneLifecycleSystem.MatchSceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private bool TryStartLoadedMatch(out MatchStartStatusKind waitStatus, out string message, out float progress01)
    {
        progress01 = 0f;
        if (!_matchSceneReferenceSystem.TryGetLoadedMatchSceneView(out MatchSceneView matchScene))
        {
            waitStatus = MatchStartStatusKind.WaitingForMatchLoaded;
            message = "Loaded Match scene has no MatchSceneView.";
            return false;
        }

        if (RequiresUnitPrefabRegistry(matchScene) &&
            !IsUnitPrefabRegistryReady(World.DefaultGameObjectInjectionWorld))
        {
            waitStatus = MatchStartStatusKind.WaitingForRuntimeContent;
            message = "Waiting for unit prefab registry";
            progress01 = 0.05f;
            return false;
        }

        try
        {
            if (!matchScene.GameplayStartRequested)
                matchScene.BeginGameplay();

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

    private static bool RequiresUnitPrefabRegistry(MatchSceneView matchScene)
    {
        UnitPrefabRegistryAuthoringConfig config = matchScene != null &&
            matchScene.BuildingPlacementConfig != null
                ? matchScene.BuildingPlacementConfig.UnitPrefabRegistryConfig
                : null;
        return config != null && config.UnitSpawnPrefabs != null && config.UnitSpawnPrefabs.Count > 0;
    }

    private static bool IsUnitPrefabRegistryReady(World world)
    {
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
            ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasBuffer<UnitPrefabRegistryEntry>(entity) &&
                em.GetBuffer<UnitPrefabRegistryEntry>(entity).Length > 0)
            {
                return true;
            }
        }

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
