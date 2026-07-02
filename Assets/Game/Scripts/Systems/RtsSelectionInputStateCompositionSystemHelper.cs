using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    public sealed class RtsSelectionInputStateCompositionSystemHelper
    {
        private Unity.Entities.World _world;
        private Entity _stateEntity;

        public bool TryRead(out EntityManager em, out RtsSelectionInputStateComponent state)
        {
            state = default;
            if (!TryResolve(out em, out Entity entity))
                return false;

            state = em.GetComponentData<RtsSelectionInputStateComponent>(entity);
            return true;
        }

        public bool TryWrite(RtsSelectionInputStateComponent state)
        {
            if (!TryResolve(out EntityManager em, out Entity entity))
                return false;

            em.SetComponentData(entity, state);
            return true;
        }

        public bool TryGetPointerRequests(out EntityManager em, out DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests)
        {
            pointerRequests = default;
            if (!TryResolve(out em, out Entity entity))
                return false;

            pointerRequests = em.GetBuffer<RtsSelectionPointerRequestElement>(entity);
            return true;
        }

        public bool TryGetCommandBuffers(
            out EntityManager em,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            out DynamicBuffer<RtsSelectionCommandResultElement> commandResults)
        {
            return TryGetCommandBuffers(out em, out _, out commandRequests, out commandResults);
        }

        public bool TryGetCommandBuffers(
            out EntityManager em,
            out Entity entity,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            out DynamicBuffer<RtsSelectionCommandResultElement> commandResults)
        {
            commandRequests = default;
            commandResults = default;
            if (!TryResolve(out em, out entity))
                return false;

            commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(entity);
            commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(entity);
            return true;
        }

        public bool TryEnqueuePointerRequest(RtsSelectionPointerRequestElement request)
        {
            if (!TryResolve(out EntityManager em, out Entity entity))
                return false;

            RtsSelectionInputRequestQueueComponent queue = em.GetComponentData<RtsSelectionInputRequestQueueComponent>(entity);
            queue.LastRequestId++;
            request.RequestId = queue.LastRequestId;
            em.SetComponentData(entity, queue);
            em.GetBuffer<RtsSelectionPointerRequestElement>(entity).Add(request);
            return true;
        }

        public bool TryEnqueueCommandRequest(RtsSelectionCommandIntentRequestElement request)
        {
            if (!TryResolve(out EntityManager em, out Entity entity))
                return false;

            RtsSelectionInputRequestQueueComponent queue = em.GetComponentData<RtsSelectionInputRequestQueueComponent>(entity);
            queue.LastRequestId++;
            request.RequestId = queue.LastRequestId;
            em.SetComponentData(entity, queue);
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(entity).Add(request);
            return true;
        }

        private bool TryResolve(out EntityManager em, out Entity entity)
        {
            em = default;
            entity = Entity.Null;

            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            em = world.EntityManager;
            if (_world == world && _stateEntity != Entity.Null && em.Exists(_stateEntity))
            {
                entity = _stateEntity;
                return true;
            }

            _world = world;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RtsSelectionInputStateComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                _stateEntity = query.GetSingletonEntity();
                entity = _stateEntity;
                EnsureRequestBuffers(em, entity);
                return true;
            }

            _stateEntity = em.CreateEntity(
                typeof(RtsSelectionInputStateComponent),
                typeof(RtsSelectionInputRequestQueueComponent));
            em.SetComponentData(_stateEntity, new RtsSelectionInputStateComponent
            {
                QueuedMoveOrderFrame = -1
            });
            EnsureRequestBuffers(em, _stateEntity);
            entity = _stateEntity;
            return true;
        }

        private static void EnsureRequestBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<RtsSelectionPointerRequestElement>(entity))
                em.AddBuffer<RtsSelectionPointerRequestElement>(entity);
            if (!em.HasBuffer<RtsSelectionCommandIntentRequestElement>(entity))
                em.AddBuffer<RtsSelectionCommandIntentRequestElement>(entity);
            if (!em.HasBuffer<RtsSelectionCommandResultElement>(entity))
                em.AddBuffer<RtsSelectionCommandResultElement>(entity);
        }
    }
}
