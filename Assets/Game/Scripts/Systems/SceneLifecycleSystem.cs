using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class SceneLifecycleSystem : SystemBase
{
    public const string MenuSceneName = "Menu";
    public const string MatchSceneName = "Match";

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    private Unity.Entities.World _world;
    private Entity _lifecycleEntity;
    private AsyncOperation _activeOperation;
    private SceneLifecycleRequestElement _activeRequest;

    public bool QueueLoadMatch(EntityManager em)
    {
        return TryEnqueue(em, new SceneLifecycleRequestElement
        {
            Kind = SceneLifecycleRequestKind.LoadAdditive,
            Scene = SceneLifecycleSceneId.Match,
            ActivateOnLoad = 1
        });
    }

    public bool QueueUnloadMatch(EntityManager em)
    {
        return TryEnqueue(em, new SceneLifecycleRequestElement
        {
            Kind = SceneLifecycleRequestKind.Unload,
            Scene = SceneLifecycleSceneId.Match
        });
    }

    public bool TryEnqueue(EntityManager em, SceneLifecycleRequestElement request)
    {
        Entity entity = EnsureLifecycleEntity(em);
        if (entity == Entity.Null || !em.Exists(entity))
            return false;

        SceneLifecycleStateComponent state = em.GetComponentData<SceneLifecycleStateComponent>(entity);
        DynamicBuffer<SceneLifecycleRequestElement> requests = em.GetBuffer<SceneLifecycleRequestElement>(entity);
        if (ShouldIgnoreDuplicateRequest(state, requests, request))
            return true;

        SceneLifecycleQueueComponent queue = em.GetComponentData<SceneLifecycleQueueComponent>(entity);
        queue.LastRequestId++;
        request.RequestId = queue.LastRequestId;
        em.SetComponentData(entity, queue);
        requests.Add(request);
        return true;
    }

    public void Update(EntityManager em)
    {
        Entity entity = EnsureLifecycleEntity(em);
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        if (TryCompleteActiveOperation(em, entity))
            return;

        SceneLifecycleStateComponent state = em.GetComponentData<SceneLifecycleStateComponent>(entity);
        if (state.IsBusy != 0)
        {
            PublishActiveOperationProgress(em, entity, state);
            return;
        }

        DynamicBuffer<SceneLifecycleRequestElement> requests = em.GetBuffer<SceneLifecycleRequestElement>(entity);
        if (requests.Length == 0)
            return;

        SceneLifecycleRequestElement request = requests[0];
        requests.RemoveAt(0);
        ProcessRequest(em, entity, request);
    }

    public Entity EnsureLifecycleEntity(EntityManager em)
    {
        Unity.Entities.World world = em.World;
        if (_world == world &&
            _lifecycleEntity != Entity.Null &&
            em.Exists(_lifecycleEntity) &&
            em.HasComponent<SceneLifecycleBoundaryComponent>(_lifecycleEntity))
        {
            EnsureBuffers(em, _lifecycleEntity);
            MirrorLoadedSceneState(em, _lifecycleEntity);
            return _lifecycleEntity;
        }

        _world = world;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SceneLifecycleBoundaryComponent>());
        if (!query.IsEmptyIgnoreFilter)
        {
            _lifecycleEntity = query.GetSingletonEntity();
            EnsureBuffers(em, _lifecycleEntity);
            MirrorLoadedSceneState(em, _lifecycleEntity);
            return _lifecycleEntity;
        }

        _lifecycleEntity = em.CreateEntity(
            typeof(SceneLifecycleBoundaryComponent),
            typeof(SceneLifecycleQueueComponent),
            typeof(SceneLifecycleStateComponent));
        em.SetName(_lifecycleEntity, "SceneLifecycleBoundary");
        EnsureBuffers(em, _lifecycleEntity);
        MirrorLoadedSceneState(em, _lifecycleEntity);
        return _lifecycleEntity;
    }

    private void ProcessRequest(EntityManager em, Entity entity, SceneLifecycleRequestElement request)
    {
        if (request.Scene != SceneLifecycleSceneId.Match)
        {
            EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Failed, SceneLifecycleResultCode.InvalidRequest, "Only Match scene lifecycle requests are supported.");
            return;
        }

        switch (request.Kind)
        {
            case SceneLifecycleRequestKind.LoadAdditive:
                BeginLoadMatch(em, entity, request);
                break;
            case SceneLifecycleRequestKind.Unload:
                BeginUnloadMatch(em, entity, request);
                break;
            default:
                EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Failed, SceneLifecycleResultCode.InvalidRequest, "Unsupported scene lifecycle request kind.");
                break;
        }
    }

    private void BeginLoadMatch(EntityManager em, Entity entity, SceneLifecycleRequestElement request)
    {
        if (IsSceneLoaded(MatchSceneName))
        {
            SetState(em, entity, request, SceneLifecycleStatusKind.Loaded, isBusy: false, isMatchLoaded: true);
            EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Loaded, SceneLifecycleResultCode.IgnoredDuplicate, "Match scene is already loaded.");
            return;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(MatchSceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            SetState(em, entity, request, SceneLifecycleStatusKind.Failed, isBusy: false, isMatchLoaded: false);
            EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Failed, SceneLifecycleResultCode.SceneOperationFailed, "Failed to start Match scene load.");
            return;
        }

        operation.allowSceneActivation = true;
        _activeOperation = operation;
        _activeRequest = request;
        SetState(em, entity, request, SceneLifecycleStatusKind.Loading, isBusy: true, isMatchLoaded: false);
        EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Loading, SceneLifecycleResultCode.Accepted, "Match scene load started.");
    }

    private void BeginUnloadMatch(EntityManager em, Entity entity, SceneLifecycleRequestElement request)
    {
        if (!IsSceneLoaded(MatchSceneName))
        {
            SetState(em, entity, request, SceneLifecycleStatusKind.Unloaded, isBusy: false, isMatchLoaded: false);
            EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Unloaded, SceneLifecycleResultCode.IgnoredDuplicate, "Match scene is already unloaded.");
            return;
        }

        StopMatchGameplay(em);

        AsyncOperation operation = SceneManager.UnloadSceneAsync(MatchSceneName);
        if (operation == null)
        {
            SetState(em, entity, request, SceneLifecycleStatusKind.Failed, isBusy: false, isMatchLoaded: true);
            EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Failed, SceneLifecycleResultCode.SceneOperationFailed, "Failed to start Match scene unload.");
            return;
        }

        _activeOperation = operation;
        _activeRequest = request;
        SetState(em, entity, request, SceneLifecycleStatusKind.Unloading, isBusy: true, isMatchLoaded: true);
        EnqueueResult(em, entity, request, SceneLifecycleStatusKind.Unloading, SceneLifecycleResultCode.Accepted, "Match scene unload started.");
    }

    private bool TryCompleteActiveOperation(EntityManager em, Entity entity)
    {
        if (_activeOperation == null || !_activeOperation.isDone)
            return false;

        SceneLifecycleStatusKind status = _activeRequest.Kind == SceneLifecycleRequestKind.Unload
            ? SceneLifecycleStatusKind.Unloaded
            : SceneLifecycleStatusKind.Loaded;
        bool isMatchLoaded = status == SceneLifecycleStatusKind.Loaded || IsSceneLoaded(MatchSceneName);
        SetState(em, entity, _activeRequest, status, false, isMatchLoaded);
        EnqueueResult(em, entity, _activeRequest, status, SceneLifecycleResultCode.Accepted, CreateCompletionMessage(_activeRequest.Kind));
        _activeOperation = null;
        _activeRequest = default;
        return true;
    }

    private static void EnsureBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<SceneLifecycleRequestElement>(entity))
            em.AddBuffer<SceneLifecycleRequestElement>(entity);
        if (!em.HasBuffer<SceneLifecycleResultElement>(entity))
            em.AddBuffer<SceneLifecycleResultElement>(entity);
    }

    private static bool ShouldIgnoreDuplicateRequest(
        SceneLifecycleStateComponent state,
        DynamicBuffer<SceneLifecycleRequestElement> requests,
        SceneLifecycleRequestElement request)
    {
        if (request.Scene != SceneLifecycleSceneId.Match)
            return false;

        if (request.Kind == SceneLifecycleRequestKind.LoadAdditive)
        {
            if (state.Status == SceneLifecycleStatusKind.Loading ||
                (state.IsMatchLoaded != 0 &&
                 state.IsBusy == 0 &&
                 !HasPendingRequest(requests, request.Scene, SceneLifecycleRequestKind.Unload)))
            {
                return true;
            }
        }
        else if (request.Kind == SceneLifecycleRequestKind.Unload)
        {
            if ((state.IsMatchLoaded == 0 && state.IsBusy == 0) ||
                state.Status == SceneLifecycleStatusKind.Unloading)
            {
                return true;
            }
        }

        for (int i = 0; i < requests.Length; i++)
        {
            SceneLifecycleRequestElement pending = requests[i];
            if (pending.Scene == request.Scene && pending.Kind == request.Kind)
                return true;
        }

        return false;
    }

    private static bool HasPendingRequest(
        DynamicBuffer<SceneLifecycleRequestElement> requests,
        SceneLifecycleSceneId scene,
        SceneLifecycleRequestKind kind)
    {
        for (int i = 0; i < requests.Length; i++)
        {
            SceneLifecycleRequestElement pending = requests[i];
            if (pending.Scene == scene && pending.Kind == kind)
                return true;
        }

        return false;
    }

    private static void MirrorLoadedSceneState(EntityManager em, Entity entity)
    {
        SceneLifecycleStateComponent state = em.GetComponentData<SceneLifecycleStateComponent>(entity);
        state.IsMatchLoaded = IsSceneLoaded(MatchSceneName) ? (byte)1 : (byte)0;
        if (state.IsBusy == 0)
        {
            state.Status = state.IsMatchLoaded != 0 ? SceneLifecycleStatusKind.Loaded : SceneLifecycleStatusKind.Unloaded;
            state.Progress01 = state.IsMatchLoaded != 0 ? 1f : 0f;
        }
        em.SetComponentData(entity, state);
    }

    private void PublishActiveOperationProgress(EntityManager em, Entity entity, SceneLifecycleStateComponent state)
    {
        if (_activeOperation == null)
            return;

        state.Progress01 = Mathf.Clamp01(_activeOperation.progress / 0.9f);
        em.SetComponentData(entity, state);
    }

    private static void SetState(
        EntityManager em,
        Entity entity,
        SceneLifecycleRequestElement request,
        SceneLifecycleStatusKind status,
        bool isBusy,
        bool isMatchLoaded)
    {
        em.SetComponentData(entity, new SceneLifecycleStateComponent
        {
            ActiveScene = request.Scene,
            Status = status,
            ActiveRequestId = request.RequestId,
            Progress01 = isBusy ? 0f : 1f,
            IsBusy = isBusy ? (byte)1 : (byte)0,
            IsMatchLoaded = isMatchLoaded ? (byte)1 : (byte)0
        });
    }

    private static void EnqueueResult(
        EntityManager em,
        Entity entity,
        SceneLifecycleRequestElement request,
        SceneLifecycleStatusKind status,
        SceneLifecycleResultCode resultCode,
        string message)
    {
        em.GetBuffer<SceneLifecycleResultElement>(entity).Add(new SceneLifecycleResultElement
        {
            Kind = request.Kind,
            Scene = request.Scene,
            Status = status,
            ResultCode = resultCode,
            RequestId = request.RequestId,
            Message = new FixedString128Bytes(message ?? string.Empty)
        });
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private static void StopMatchGameplay(EntityManager em)
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        ComponentTypeHandle<RuntimeGameplayStateComponent> stateType = em.GetComponentTypeHandle<RuntimeGameplayStateComponent>(false);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<RuntimeGameplayStateComponent> states = chunks[chunkIndex].GetNativeArray(ref stateType);
            for (int i = 0; i < states.Length; i++)
            {
                RuntimeGameplayStateComponent state = states[i];
                state.PlayRequested = 0;
                state.SelectionModeActive = 0;
                state.BuildModeActive = 0;
                states[i] = state;
            }
        }
    }

    private static string CreateCompletionMessage(SceneLifecycleRequestKind kind)
    {
        return kind == SceneLifecycleRequestKind.Unload
            ? "Match scene unload completed."
            : "Match scene load completed.";
    }
}
