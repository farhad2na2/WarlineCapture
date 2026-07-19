using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RuntimeGameplayStateSystem : ISystem
    {
        private ulong _worldSequenceNumber;

        public RuntimeGameplayStateSystem(EntityManager entityManager)
        {
            _worldSequenceNumber = entityManager.World.SequenceNumber;
        }

        public void Bind(EntityManager entityManager)
        {
            _worldSequenceNumber = entityManager.World.SequenceNumber;
        }

        public void OnCreate(ref SystemState state)
        {
            // This is a disabled facade; callers access the World-owned state explicitly.
            _worldSequenceNumber = state.EntityManager.World.SequenceNumber;
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public bool PlayRequested
        {
            get => ReadGameplayState().PlayRequested != 0;
            set => WriteGameplayState(state => { state.PlayRequested = ToByte(value); return state; });
        }

        public bool SimulationActive
        {
            get => ReadGameplayState().SimulationActive != 0;
            set => WriteGameplayState(state => { state.SimulationActive = ToByte(value); return state; });
        }

        public bool SelectionModeActive
        {
            get => ReadGameplayState().SelectionModeActive != 0;
            set => WriteGameplayState(state => { state.SelectionModeActive = ToByte(value); return state; });
        }

        public bool BuildModeActive
        {
            get => ReadGameplayState().BuildModeActive != 0;
            set => WriteGameplayState(state => { state.BuildModeActive = ToByte(value); return state; });
        }

        public bool FullscreenMapOpen
        {
            get => ReadGameplayState().FullscreenMapOpen != 0;
            set => WriteGameplayState(state => { state.FullscreenMapOpen = ToByte(value); return state; });
        }

        public bool FullscreenMapIsoMode
        {
            get => ReadGameplayState().FullscreenMapIsoMode != 0;
            set => WriteGameplayState(state => { state.FullscreenMapIsoMode = ToByte(value); return state; });
        }

        public bool SuppressNextWorldClick
        {
            get => ReadGameplayState().SuppressNextWorldClick != 0;
            set => WriteGameplayState(state => { state.SuppressNextWorldClick = ToByte(value); return state; });
        }

        public bool PlayerAutoModeEnabled
        {
            get => ReadGameplayState().PlayerAutoModeEnabled != 0;
            set => WriteGameplayState(state => { state.PlayerAutoModeEnabled = ToByte(value); return state; });
        }

        public bool ZoomInHeld
        {
            get => ReadCameraInput().ZoomInHeld != 0;
            set => WriteCameraInput(input => { input.ZoomInHeld = ToByte(value); return input; });
        }

        public bool ZoomOutHeld
        {
            get => ReadCameraInput().ZoomOutHeld != 0;
            set => WriteCameraInput(input => { input.ZoomOutHeld = ToByte(value); return input; });
        }

        public bool InitialCameraFocusRequested
        {
            get => ReadCameraFocusRequest().Requested != 0;
            set => WriteCameraFocusRequest(request => { request.Requested = ToByte(value); return request; });
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
            WriteCameraInput(_ => default);
            WriteCameraFocusRequest(_ => default);
        }

        public void ResetForMatchShutdown()
        {
            WriteGameplayState(_ => default);
            WriteCameraInput(_ => default);
            WriteCameraFocusRequest(_ => default);
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
            return TryGetStateEntity(out EntityManager entityManager, out Entity entity)
                ? entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity)
                : default;
        }

        public RuntimeCameraInputComponent ReadCameraInput()
        {
            return TryGetStateEntity(out EntityManager entityManager, out Entity entity)
                ? entityManager.GetComponentData<RuntimeCameraInputComponent>(entity)
                : default;
        }

        public RuntimeCameraFocusRequestComponent ReadCameraFocusRequest()
        {
            return TryGetStateEntity(out EntityManager entityManager, out Entity entity)
                ? entityManager.GetComponentData<RuntimeCameraFocusRequestComponent>(entity)
                : default;
        }

        private void WriteGameplayState(System.Func<RuntimeGameplayStateComponent, RuntimeGameplayStateComponent> mutate)
        {
            if (!TryGetStateEntity(out EntityManager entityManager, out Entity entity))
                return;
            entityManager.SetComponentData(entity, mutate(entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity)));
        }

        private void WriteCameraInput(System.Func<RuntimeCameraInputComponent, RuntimeCameraInputComponent> mutate)
        {
            if (!TryGetStateEntity(out EntityManager entityManager, out Entity entity))
                return;
            entityManager.SetComponentData(entity, mutate(entityManager.GetComponentData<RuntimeCameraInputComponent>(entity)));
        }

        private void WriteCameraFocusRequest(System.Func<RuntimeCameraFocusRequestComponent, RuntimeCameraFocusRequestComponent> mutate)
        {
            if (!TryGetStateEntity(out EntityManager entityManager, out Entity entity))
                return;
            entityManager.SetComponentData(entity, mutate(entityManager.GetComponentData<RuntimeCameraFocusRequestComponent>(entity)));
        }

        private bool TryGetStateEntity(out EntityManager entityManager, out Entity entity)
        {
            entityManager = default;
            entity = Entity.Null;
            if (!TryGetWorld(out World world))
                return false;
            entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                entity = query.GetSingletonEntity();
                EnsureStateComponents(entityManager, entity);
                return true;
            }

            entity = entityManager.CreateEntity(
                typeof(RuntimeGameplayStateComponent),
                typeof(RuntimeCameraInputComponent),
                typeof(RuntimeCameraFocusRequestComponent));
            entityManager.SetName(entity, "RuntimeGameplayState");
            return true;
        }

        private bool TryGetWorld(out World world)
        {
            for (int i = 0; i < World.All.Count; i++)
            {
                World candidate = World.All[i];
                if (candidate.IsCreated && candidate.SequenceNumber == _worldSequenceNumber)
                {
                    world = candidate;
                    return true;
                }
            }

            world = null;
            return false;
        }

        private static void EnsureStateComponents(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<RuntimeCameraInputComponent>(entity))
                entityManager.AddComponent<RuntimeCameraInputComponent>(entity);
            if (!entityManager.HasComponent<RuntimeCameraFocusRequestComponent>(entity))
                entityManager.AddComponent<RuntimeCameraFocusRequestComponent>(entity);
        }

        private static byte ToByte(bool value)
        {
            return value ? (byte)1 : (byte)0;
        }
    }
}
