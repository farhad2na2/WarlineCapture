using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed partial class RuntimeGameplayStateSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    private Unity.Entities.World _cachedWorld;
    private Entity _stateEntity;
    private bool _hasCachedLegacyGameplayState;
    private bool _hasCachedLegacyCameraInput;
    private bool _hasCachedLegacyCameraFocusRequest;
    private RuntimeGameplayStateComponent _lastLegacyGameplayState;
    private RuntimeCameraInputComponent _lastLegacyCameraInput;
    private RuntimeCameraFocusRequestComponent _lastLegacyCameraFocusRequest;

    public bool PlayRequested
    {
        get => ReadGameplayState().PlayRequested != 0;
        set => WriteGameplayState(state =>
        {
            state.PlayRequested = ToByte(value);
            return state;
        });
    }

    public bool SelectionModeActive
    {
        get => ReadGameplayState().SelectionModeActive != 0;
        set => WriteGameplayState(state =>
        {
            state.SelectionModeActive = ToByte(value);
            return state;
        });
    }

    public bool BuildModeActive
    {
        get => ReadGameplayState().BuildModeActive != 0;
        set => WriteGameplayState(state =>
        {
            state.BuildModeActive = ToByte(value);
            return state;
        });
    }

    public bool FullscreenMapOpen
    {
        get => ReadGameplayState().FullscreenMapOpen != 0;
        set => WriteGameplayState(state =>
        {
            state.FullscreenMapOpen = ToByte(value);
            return state;
        });
    }

    public bool FullscreenMapIsoMode
    {
        get => ReadGameplayState().FullscreenMapIsoMode != 0;
        set => WriteGameplayState(state =>
        {
            state.FullscreenMapIsoMode = ToByte(value);
            return state;
        });
    }

    public bool SuppressNextWorldClick
    {
        get => ReadGameplayState().SuppressNextWorldClick != 0;
        set => WriteGameplayState(state =>
        {
            state.SuppressNextWorldClick = ToByte(value);
            return state;
        });
    }

    public bool PlayerAutoModeEnabled
    {
        get => ReadGameplayState().PlayerAutoModeEnabled != 0;
        set => WriteGameplayState(state =>
        {
            state.PlayerAutoModeEnabled = ToByte(value);
            return state;
        });
    }

    public bool ZoomInHeld
    {
        get => ReadCameraInput().ZoomInHeld != 0;
        set => WriteCameraInput(input =>
        {
            input.ZoomInHeld = ToByte(value);
            return input;
        });
    }

    public bool ZoomOutHeld
    {
        get => ReadCameraInput().ZoomOutHeld != 0;
        set => WriteCameraInput(input =>
        {
            input.ZoomOutHeld = ToByte(value);
            return input;
        });
    }

    public bool InitialCameraFocusRequested
    {
        get => ReadCameraFocusRequest().Requested != 0;
        set => WriteCameraFocusRequest(request =>
        {
            request.Requested = ToByte(value);
            return request;
        });
    }

    public Vector3 InitialCameraFocusWorld
    {
        get
        {
            RuntimeCameraFocusRequestComponent request = ReadCameraFocusRequest();
            return new Vector3(request.World.x, request.World.y, request.World.z);
        }
        set => WriteCameraFocusRequest(request =>
        {
            request.World = new float3(value.x, value.y, value.z);
            return request;
        });
    }

    public void ResetForGameplayStart()
    {
        WriteGameplayState(state =>
        {
            state.PlayRequested = 1;
            state.SelectionModeActive = 0;
            state.BuildModeActive = 0;
            state.FullscreenMapOpen = 0;
            state.FullscreenMapIsoMode = 0;
            state.SuppressNextWorldClick = 1;
            return state;
        });

        WriteCameraInput(input =>
        {
            input.ZoomInHeld = 0;
            input.ZoomOutHeld = 0;
            return input;
        });

        WriteCameraFocusRequest(request =>
        {
            request.Requested = 0;
            return request;
        });
    }

    public bool TryConsumeInitialCameraFocus(out Vector3 focusWorld)
    {
        RuntimeCameraFocusRequestComponent request = ReadCameraFocusRequest();
        focusWorld = new Vector3(request.World.x, request.World.y, request.World.z);
        if (request.Requested == 0)
            return false;

        WriteCameraFocusRequest(state =>
        {
            state.Requested = 0;
            return state;
        });
        return true;
    }

    public RuntimeGameplayStateComponent ReadGameplayState()
    {
        RuntimeGameplayStateComponent state = LegacyGameplayState();
        if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
        {
            if (!_hasCachedLegacyGameplayState || !GameplayStateEquals(state, _lastLegacyGameplayState))
            {
                entityManager.SetComponentData(entity, state);
                CacheLegacyGameplayState(state);
                return state;
            }

            return entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity);
        }

        return state;
    }

    public RuntimeCameraInputComponent ReadCameraInput()
    {
        RuntimeCameraInputComponent input = LegacyCameraInput();
        if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
        {
            if (!_hasCachedLegacyCameraInput || !CameraInputEquals(input, _lastLegacyCameraInput))
            {
                entityManager.SetComponentData(entity, input);
                CacheLegacyCameraInput(input);
                return input;
            }

            return entityManager.GetComponentData<RuntimeCameraInputComponent>(entity);
        }

        return input;
    }

    public RuntimeCameraFocusRequestComponent ReadCameraFocusRequest()
    {
        RuntimeCameraFocusRequestComponent request = LegacyCameraFocusRequest();
        if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
        {
            if (!_hasCachedLegacyCameraFocusRequest || !CameraFocusRequestEquals(request, _lastLegacyCameraFocusRequest))
            {
                entityManager.SetComponentData(entity, request);
                CacheLegacyCameraFocusRequest(request);
                return request;
            }

            return entityManager.GetComponentData<RuntimeCameraFocusRequestComponent>(entity);
        }

        return request;
    }

    private void WriteGameplayState(System.Func<RuntimeGameplayStateComponent, RuntimeGameplayStateComponent> mutate)
    {
        RuntimeGameplayStateComponent state = mutate(LegacyGameplayState());
        ApplyLegacyGameplayState(state);
        CacheLegacyGameplayState(state);
        if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
            entityManager.SetComponentData(entity, state);
    }

    private void WriteCameraInput(System.Func<RuntimeCameraInputComponent, RuntimeCameraInputComponent> mutate)
    {
        RuntimeCameraInputComponent input = mutate(LegacyCameraInput());
        ApplyLegacyCameraInput(input);
        CacheLegacyCameraInput(input);
        if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
            entityManager.SetComponentData(entity, input);
    }

    private void WriteCameraFocusRequest(System.Func<RuntimeCameraFocusRequestComponent, RuntimeCameraFocusRequestComponent> mutate)
    {
        RuntimeCameraFocusRequestComponent request = mutate(LegacyCameraFocusRequest());
        ApplyLegacyCameraFocusRequest(request);
        CacheLegacyCameraFocusRequest(request);
        if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
            entityManager.SetComponentData(entity, request);
    }

    private bool TryGetStateEntity(out EntityManager entityManager, out Entity entity)
    {
        entityManager = default;
        entity = Entity.Null;
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        if (_cachedWorld == world &&
            _stateEntity != Entity.Null &&
            entityManager.Exists(_stateEntity) &&
            entityManager.HasComponent<RuntimeGameplayStateComponent>(_stateEntity))
        {
            entity = _stateEntity;
            EnsureStateComponents(entityManager, entity);
            return true;
        }

        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
        if (query.CalculateEntityCount() > 0)
        {
            entity = query.GetSingletonEntity();
            EnsureStateComponents(entityManager, entity);
            CacheStateEntity(world, entity);
            return true;
        }

        entity = entityManager.CreateEntity(
            typeof(RuntimeGameplayStateComponent),
            typeof(RuntimeCameraInputComponent),
            typeof(RuntimeCameraFocusRequestComponent));
        entityManager.SetName(entity, "RuntimeGameplayState");
        entityManager.SetComponentData(entity, LegacyGameplayState());
        entityManager.SetComponentData(entity, LegacyCameraInput());
        entityManager.SetComponentData(entity, LegacyCameraFocusRequest());
        CacheStateEntity(world, entity);
        return true;
    }

    private void CacheStateEntity(Unity.Entities.World world, Entity entity)
    {
        _cachedWorld = world;
        _stateEntity = entity;
    }

    private void CacheLegacyGameplayState(RuntimeGameplayStateComponent state)
    {
        _lastLegacyGameplayState = state;
        _hasCachedLegacyGameplayState = true;
    }

    private void CacheLegacyCameraInput(RuntimeCameraInputComponent input)
    {
        _lastLegacyCameraInput = input;
        _hasCachedLegacyCameraInput = true;
    }

    private void CacheLegacyCameraFocusRequest(RuntimeCameraFocusRequestComponent request)
    {
        _lastLegacyCameraFocusRequest = request;
        _hasCachedLegacyCameraFocusRequest = true;
    }

    private static void EnsureStateComponents(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<RuntimeCameraInputComponent>(entity))
            entityManager.AddComponentData(entity, LegacyCameraInput());
        if (!entityManager.HasComponent<RuntimeCameraFocusRequestComponent>(entity))
            entityManager.AddComponentData(entity, LegacyCameraFocusRequest());
    }

    private static RuntimeGameplayStateComponent LegacyGameplayState()
    {
        return new RuntimeGameplayStateComponent
        {
            PlayRequested = ToByte(InitialUnitsRuntimeState.PlayRequested),
            SelectionModeActive = ToByte(InitialUnitsRuntimeState.SelectionModeActive),
            BuildModeActive = ToByte(InitialUnitsRuntimeState.BuildModeActive),
            FullscreenMapOpen = ToByte(InitialUnitsRuntimeState.FullscreenMapOpen),
            FullscreenMapIsoMode = ToByte(InitialUnitsRuntimeState.FullscreenMapIsoMode),
            SuppressNextWorldClick = ToByte(InitialUnitsRuntimeState.SuppressNextWorldClick),
            PlayerAutoModeEnabled = ToByte(InitialUnitsRuntimeState.PlayerAutoModeEnabled)
        };
    }

    private static RuntimeCameraInputComponent LegacyCameraInput()
    {
        return new RuntimeCameraInputComponent
        {
            ZoomInHeld = ToByte(InitialUnitsRuntimeState.ZoomInHeld),
            ZoomOutHeld = ToByte(InitialUnitsRuntimeState.ZoomOutHeld)
        };
    }

    private static RuntimeCameraFocusRequestComponent LegacyCameraFocusRequest()
    {
        Vector3 focus = InitialUnitsRuntimeState.InitialCameraFocusWorld;
        return new RuntimeCameraFocusRequestComponent
        {
            Requested = ToByte(InitialUnitsRuntimeState.InitialCameraFocusRequested),
            World = new float3(focus.x, focus.y, focus.z)
        };
    }

    private static void ApplyLegacyGameplayState(RuntimeGameplayStateComponent state)
    {
        InitialUnitsRuntimeState.PlayRequested = state.PlayRequested != 0;
        InitialUnitsRuntimeState.SelectionModeActive = state.SelectionModeActive != 0;
        InitialUnitsRuntimeState.BuildModeActive = state.BuildModeActive != 0;
        InitialUnitsRuntimeState.FullscreenMapOpen = state.FullscreenMapOpen != 0;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = state.FullscreenMapIsoMode != 0;
        InitialUnitsRuntimeState.SuppressNextWorldClick = state.SuppressNextWorldClick != 0;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = state.PlayerAutoModeEnabled != 0;
    }

    private static void ApplyLegacyCameraInput(RuntimeCameraInputComponent input)
    {
        InitialUnitsRuntimeState.ZoomInHeld = input.ZoomInHeld != 0;
        InitialUnitsRuntimeState.ZoomOutHeld = input.ZoomOutHeld != 0;
    }

    private static void ApplyLegacyCameraFocusRequest(RuntimeCameraFocusRequestComponent request)
    {
        InitialUnitsRuntimeState.InitialCameraFocusRequested = request.Requested != 0;
        InitialUnitsRuntimeState.InitialCameraFocusWorld = new Vector3(request.World.x, request.World.y, request.World.z);
    }

    private static byte ToByte(bool value)
    {
        return value ? (byte)1 : (byte)0;
    }

    private static bool GameplayStateEquals(RuntimeGameplayStateComponent left, RuntimeGameplayStateComponent right)
    {
        return left.PlayRequested == right.PlayRequested &&
            left.SelectionModeActive == right.SelectionModeActive &&
            left.BuildModeActive == right.BuildModeActive &&
            left.FullscreenMapOpen == right.FullscreenMapOpen &&
            left.FullscreenMapIsoMode == right.FullscreenMapIsoMode &&
            left.SuppressNextWorldClick == right.SuppressNextWorldClick &&
            left.PlayerAutoModeEnabled == right.PlayerAutoModeEnabled;
    }

    private static bool CameraInputEquals(RuntimeCameraInputComponent left, RuntimeCameraInputComponent right)
    {
        return left.ZoomInHeld == right.ZoomInHeld &&
            left.ZoomOutHeld == right.ZoomOutHeld;
    }

    private static bool CameraFocusRequestEquals(RuntimeCameraFocusRequestComponent left, RuntimeCameraFocusRequestComponent right)
    {
        return left.Requested == right.Requested &&
            left.World.Equals(right.World);
    }
}
