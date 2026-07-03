using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RuntimeGameplayStateSystem : ISystem
    {
        private static Unity.Entities.World s_CachedStateWorld;
        private static Entity s_CachedStateEntity;

        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled state facade; public accessors create/read the backing entity.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public bool PlayRequested
        {
            get => ReadGameplayState().PlayRequested != 0;
            set => WriteGameplayState(state =>
            {
                state.PlayRequested = ToByte(value);
                return state;
            });
        }

        public bool SimulationActive
        {
            get => ReadGameplayState().SimulationActive != 0;
            set => WriteGameplayState(state =>
            {
                state.SimulationActive = ToByte(value);
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
                state.SimulationActive = 0;
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
                RuntimeGameplayLegacyMirrorComponent mirror = entityManager.GetComponentData<RuntimeGameplayLegacyMirrorComponent>(entity);
                if (mirror.HasGameplayState == 0 || !GameplayStateEquals(state, mirror.GameplayState))
                {
                    entityManager.SetComponentData(entity, state);
                    mirror.HasGameplayState = 1;
                    mirror.GameplayState = state;
                    entityManager.SetComponentData(entity, mirror);
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
                RuntimeGameplayLegacyMirrorComponent mirror = entityManager.GetComponentData<RuntimeGameplayLegacyMirrorComponent>(entity);
                if (mirror.HasCameraInput == 0 || !CameraInputEquals(input, mirror.CameraInput))
                {
                    entityManager.SetComponentData(entity, input);
                    mirror.HasCameraInput = 1;
                    mirror.CameraInput = input;
                    entityManager.SetComponentData(entity, mirror);
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
                RuntimeGameplayLegacyMirrorComponent mirror = entityManager.GetComponentData<RuntimeGameplayLegacyMirrorComponent>(entity);
                if (mirror.HasCameraFocusRequest == 0 || !CameraFocusRequestEquals(request, mirror.CameraFocusRequest))
                {
                    entityManager.SetComponentData(entity, request);
                    mirror.HasCameraFocusRequest = 1;
                    mirror.CameraFocusRequest = request;
                    entityManager.SetComponentData(entity, mirror);
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
            if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
            {
                entityManager.SetComponentData(entity, state);
                RuntimeGameplayLegacyMirrorComponent mirror = entityManager.GetComponentData<RuntimeGameplayLegacyMirrorComponent>(entity);
                mirror.HasGameplayState = 1;
                mirror.GameplayState = state;
                entityManager.SetComponentData(entity, mirror);
            }
        }

        private void WriteCameraInput(System.Func<RuntimeCameraInputComponent, RuntimeCameraInputComponent> mutate)
        {
            RuntimeCameraInputComponent input = mutate(LegacyCameraInput());
            ApplyLegacyCameraInput(input);
            if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
            {
                entityManager.SetComponentData(entity, input);
                RuntimeGameplayLegacyMirrorComponent mirror = entityManager.GetComponentData<RuntimeGameplayLegacyMirrorComponent>(entity);
                mirror.HasCameraInput = 1;
                mirror.CameraInput = input;
                entityManager.SetComponentData(entity, mirror);
            }
        }

        private void WriteCameraFocusRequest(System.Func<RuntimeCameraFocusRequestComponent, RuntimeCameraFocusRequestComponent> mutate)
        {
            RuntimeCameraFocusRequestComponent request = mutate(LegacyCameraFocusRequest());
            ApplyLegacyCameraFocusRequest(request);
            if (TryGetStateEntity(out EntityManager entityManager, out Entity entity))
            {
                entityManager.SetComponentData(entity, request);
                RuntimeGameplayLegacyMirrorComponent mirror = entityManager.GetComponentData<RuntimeGameplayLegacyMirrorComponent>(entity);
                mirror.HasCameraFocusRequest = 1;
                mirror.CameraFocusRequest = request;
                entityManager.SetComponentData(entity, mirror);
            }
        }

        private static bool TryGetStateEntity(out EntityManager entityManager, out Entity entity)
        {
            entityManager = default;
            entity = Entity.Null;
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            entityManager = world.EntityManager;
            if (s_CachedStateWorld == world &&
                s_CachedStateEntity != Entity.Null &&
                entityManager.Exists(s_CachedStateEntity) &&
                entityManager.HasComponent<RuntimeGameplayStateComponent>(s_CachedStateEntity))
            {
                entity = s_CachedStateEntity;
                EnsureStateComponents(entityManager, entity);
                return true;
            }

            s_CachedStateWorld = world;
            s_CachedStateEntity = Entity.Null;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (query.CalculateEntityCount() > 0)
            {
                entity = query.GetSingletonEntity();
                s_CachedStateEntity = entity;
                EnsureStateComponents(entityManager, entity);
                return true;
            }

            entity = entityManager.CreateEntity(
                typeof(RuntimeGameplayStateComponent),
                typeof(RuntimeCameraInputComponent),
                typeof(RuntimeCameraFocusRequestComponent),
                typeof(RuntimeGameplayLegacyMirrorComponent));
            entityManager.SetName(entity, "RuntimeGameplayState");
            entityManager.SetComponentData(entity, LegacyGameplayState());
            entityManager.SetComponentData(entity, LegacyCameraInput());
            entityManager.SetComponentData(entity, LegacyCameraFocusRequest());
            entityManager.SetComponentData(entity, new RuntimeGameplayLegacyMirrorComponent
            {
                HasGameplayState = 1,
                HasCameraInput = 1,
                HasCameraFocusRequest = 1,
                GameplayState = LegacyGameplayState(),
                CameraInput = LegacyCameraInput(),
                CameraFocusRequest = LegacyCameraFocusRequest()
            });
            s_CachedStateEntity = entity;
            return true;
        }

        private static void EnsureStateComponents(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<RuntimeCameraInputComponent>(entity))
                entityManager.AddComponentData(entity, LegacyCameraInput());
            if (!entityManager.HasComponent<RuntimeCameraFocusRequestComponent>(entity))
                entityManager.AddComponentData(entity, LegacyCameraFocusRequest());
            if (!entityManager.HasComponent<RuntimeGameplayLegacyMirrorComponent>(entity))
            {
                entityManager.AddComponentData(entity, new RuntimeGameplayLegacyMirrorComponent
                {
                    HasGameplayState = 1,
                    HasCameraInput = 1,
                    HasCameraFocusRequest = 1,
                    GameplayState = LegacyGameplayState(),
                    CameraInput = LegacyCameraInput(),
                    CameraFocusRequest = LegacyCameraFocusRequest()
                });
            }
        }

        private static RuntimeGameplayStateComponent LegacyGameplayState()
        {
            return new RuntimeGameplayStateComponent
            {
                PlayRequested = ToByte(InitialUnitsRuntimeState.PlayRequested),
                SimulationActive = ToByte(InitialUnitsRuntimeState.SimulationActive),
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
            InitialUnitsRuntimeState.SimulationActive = state.SimulationActive != 0;
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
                left.SimulationActive == right.SimulationActive &&
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
}
