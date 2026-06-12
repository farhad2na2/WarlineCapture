using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

public sealed class SelectedUnitOrderSnapshotSystem
{
    private struct PreservedOrderState
    {
        public Entity Entity;
        public bool HadEngageTarget;
        public EngageTarget EngageTarget;
        public bool HadUnitTarget;
        public UnitTarget UnitTarget;
        public bool HadUnitPathRequest;
        public UnitPathRequest UnitPathRequest;
        public bool HadUnitPathFollow;
        public UnitPathFollow UnitPathFollow;
        public bool HadUnitPathRange;
        public UnitPathRange UnitPathRange;
    }

    private World _queryWorld;
    private EntityQuery _selectedTagQuery;
    private readonly List<PreservedOrderState> _preservedOrders = new();

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _preservedOrders.Clear();
        _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
    }

    public void Clear()
    {
        _preservedOrders.Clear();
    }

    public void PreserveSelectedUnitOrders(EntityManager em)
    {
        EnsureEntityQueries(em);
        _preservedOrders.Clear();

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = _selectedTagQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var state = new PreservedOrderState
                {
                    Entity = entity,
                    HadEngageTarget = em.HasComponent<EngageTarget>(entity),
                    HadUnitTarget = em.HasComponent<UnitTarget>(entity),
                    HadUnitPathRequest = em.HasComponent<UnitPathRequest>(entity),
                    HadUnitPathFollow = em.HasComponent<UnitPathFollow>(entity),
                    HadUnitPathRange = em.HasComponent<UnitPathRange>(entity)
                };

                if (state.HadEngageTarget)
                    state.EngageTarget = em.GetComponentData<EngageTarget>(entity);
                if (state.HadUnitTarget)
                    state.UnitTarget = em.GetComponentData<UnitTarget>(entity);
                if (state.HadUnitPathRequest)
                    state.UnitPathRequest = em.GetComponentData<UnitPathRequest>(entity);
                if (state.HadUnitPathFollow)
                    state.UnitPathFollow = em.GetComponentData<UnitPathFollow>(entity);
                if (state.HadUnitPathRange)
                    state.UnitPathRange = em.GetComponentData<UnitPathRange>(entity);

                _preservedOrders.Add(state);
            }
        }
    }

    public void RestorePreservedUnitOrders(EntityManager em)
    {
        for (int i = 0; i < _preservedOrders.Count; i++)
        {
            PreservedOrderState state = _preservedOrders[i];
            if (!em.Exists(state.Entity))
                continue;

            RestoreComponent(em, state.Entity, state.HadEngageTarget, state.EngageTarget);
            RestoreComponent(em, state.Entity, state.HadUnitTarget, state.UnitTarget);
            RestoreComponent(em, state.Entity, state.HadUnitPathRequest, state.UnitPathRequest);
            RestoreComponent(em, state.Entity, state.HadUnitPathFollow, state.UnitPathFollow);
            RestoreComponent(em, state.Entity, state.HadUnitPathRange, state.UnitPathRange);
        }

        _preservedOrders.Clear();
    }

    private static void RestoreComponent<T>(EntityManager em, Entity entity, bool shouldExist, T value)
        where T : unmanaged, IComponentData
    {
        if (shouldExist)
        {
            if (em.HasComponent<T>(entity))
                em.SetComponentData(entity, value);
            else
                em.AddComponentData(entity, value);
        }
        else if (em.HasComponent<T>(entity))
        {
            em.RemoveComponent<T>(entity);
        }
    }
}
