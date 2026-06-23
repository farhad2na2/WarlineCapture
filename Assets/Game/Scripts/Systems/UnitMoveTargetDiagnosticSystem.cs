using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitMoveTargetDiagnosticSystem : ISystem
{
    private NativeParallelHashMap<Entity, int2> _lastTargets;
    private NativeList<Entity> _missingTargetScratch;
    private EntityQuery _playerUnitTargetQuery;
    private int _lastPruneFrame;

    public void OnCreate(ref SystemState state)
    {
        _playerUnitTargetQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<UnitTarget>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<Faction>());
        _lastTargets = new NativeParallelHashMap<Entity, int2>(64, Allocator.Persistent);
        _missingTargetScratch = new NativeList<Entity>(64, Allocator.Persistent);
        if (!SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
            state.Enabled = false;
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_lastTargets.IsCreated)
            _lastTargets.Dispose();
        if (_missingTargetScratch.IsCreated)
            _missingTargetScratch.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_playerUnitTargetQuery.IsEmptyIgnoreFilter)
        {
            if (_lastTargets.Count() > 0)
                _lastTargets.Clear();
            return;
        }

        EntityManager em = state.EntityManager;
        EnsureTargetCapacity(em, _playerUnitTargetQuery.CalculateEntityCount());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<Faction> factionType = em.GetComponentTypeHandle<Faction>(true);
        ComponentTypeHandle<UnitTarget> targetType = em.GetComponentTypeHandle<UnitTarget>(true);
        using NativeArray<ArchetypeChunk> chunks = _playerUnitTargetQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
            NativeArray<UnitTarget> targets = chunk.GetNativeArray(ref targetType);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!FactionIdentity.IsPlayerControlled(factions[i].Id))
                    continue;

                UnitTarget target = targets[i];
                int2 previous = default;
                if (_lastTargets.TryGetValue(entity, out previous) && previous.Equals(target.Cell))
                    continue;

                _lastTargets[entity] = target.Cell;
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"playerUnitTargetChanged frame={UnityEngine.Time.frameCount} entity={DescribeEntity(em, entity)} " +
                    $"previous={(previous.Equals(default) ? "none-or-default" : previous.ToString())} target={target.Cell} " +
                    $"pathRequest={ResolvePathRequest(em, entity)} pathFollow={em.HasComponent<UnitPathFollow>(entity)} " +
                    $"manual={em.HasComponent<ManualMoveOrderTag>(entity)} engage={em.HasComponent<EngageTarget>(entity)} " +
                    $"selected={em.HasComponent<SelectedUnitTag>(entity)} autoWander={em.HasComponent<AutoWanderMoveTag>(entity)}");
            }
        }

        if (UnityEngine.Time.frameCount - _lastPruneFrame < 120)
            return;

        _lastPruneFrame = UnityEngine.Time.frameCount;
        PruneMissingEntities(em);
    }

    private void EnsureTargetCapacity(EntityManager em, int requiredCapacity)
    {
        requiredCapacity = math.max(64, requiredCapacity);
        if (_lastTargets.Capacity >= requiredCapacity)
            return;

        NativeParallelHashMap<Entity, int2> resizedTargets =
            new NativeParallelHashMap<Entity, int2>(requiredCapacity, Allocator.Persistent);
        NativeArray<Entity> keys = _lastTargets.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keys.Length; i++)
        {
            Entity entity = keys[i];
            if (!em.Exists(entity))
                continue;
            if (_lastTargets.TryGetValue(entity, out int2 target))
                resizedTargets.TryAdd(entity, target);
        }

        keys.Dispose();
        _lastTargets.Dispose();
        _lastTargets = resizedTargets;
    }

    private void PruneMissingEntities(EntityManager em)
    {
        _missingTargetScratch.Clear();
        NativeArray<Entity> keys = _lastTargets.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keys.Length; i++)
        {
            Entity entity = keys[i];
            if (em.Exists(entity))
                continue;

            _missingTargetScratch.Add(entity);
        }
        keys.Dispose();

        if (_missingTargetScratch.Length == 0)
            return;

        for (int i = 0; i < _missingTargetScratch.Length; i++)
            _lastTargets.Remove(_missingTargetScratch[i]);

        _missingTargetScratch.Clear();
    }

    private static string ResolvePathRequest(EntityManager em, Entity entity)
    {
        return em.HasComponent<UnitPathRequest>(entity)
            ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString()
            : "none";
    }

    private static string DescribeEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "null";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        byte faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id
            : (byte)0;
        string grid = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "none";
        return $"{entity}/{source}/faction={faction}/selected={em.HasComponent<SelectedUnitTag>(entity)}/grid={grid}";
    }
}
