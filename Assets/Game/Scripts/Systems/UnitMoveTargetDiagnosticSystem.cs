using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class UnitMoveTargetDiagnosticSystem : SystemBase
{
    private readonly Dictionary<Entity, int2> _lastTargets = new();
    private EntityQuery _playerUnitTargetQuery;
    private int _lastPruneFrame;

    protected override void OnCreate()
    {
        _playerUnitTargetQuery = EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitTarget>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<Faction>());
        if (!SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            Enabled = false;
    }

    protected override void OnUpdate()
    {
        if (_playerUnitTargetQuery.IsEmptyIgnoreFilter)
        {
            if (_lastTargets.Count > 0)
                _lastTargets.Clear();
            return;
        }

        EntityManager em = EntityManager;
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
                if (_lastTargets.TryGetValue(entity, out int2 previous) && previous.Equals(target.Cell))
                    continue;

                _lastTargets[entity] = target.Cell;
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"playerUnitTargetChanged frame={UnityEngine.Time.frameCount} entity={DescribeEntity(entity)} " +
                    $"previous={(previous.Equals(default) ? "none-or-default" : previous.ToString())} target={target.Cell} " +
                    $"pathRequest={ResolvePathRequest(entity)} pathFollow={EntityManager.HasComponent<UnitPathFollow>(entity)} " +
                    $"manual={EntityManager.HasComponent<ManualMoveOrderTag>(entity)} engage={EntityManager.HasComponent<EngageTarget>(entity)} " +
                    $"selected={EntityManager.HasComponent<SelectedUnitTag>(entity)} autoWander={EntityManager.HasComponent<AutoWanderMoveTag>(entity)}");
            }
        }

        if (UnityEngine.Time.frameCount - _lastPruneFrame < 120)
            return;

        _lastPruneFrame = UnityEngine.Time.frameCount;
        PruneMissingEntities();
    }

    private void PruneMissingEntities()
    {
        List<Entity> remove = null;
        foreach (Entity entity in _lastTargets.Keys)
        {
            if (EntityManager.Exists(entity))
                continue;

            remove ??= new List<Entity>();
            remove.Add(entity);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            _lastTargets.Remove(remove[i]);
    }

    private string ResolvePathRequest(Entity entity)
    {
        return EntityManager.HasComponent<UnitPathRequest>(entity)
            ? EntityManager.GetComponentData<UnitPathRequest>(entity).Goal.ToString()
            : "none";
    }

    private string DescribeEntity(Entity entity)
    {
        if (entity == Entity.Null || !EntityManager.Exists(entity))
            return "null";

        string source = EntityManager.HasComponent<UnitSourcePrefabKey>(entity)
            ? EntityManager.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : EntityManager.GetName(entity);
        byte faction = EntityManager.HasComponent<Faction>(entity)
            ? EntityManager.GetComponentData<Faction>(entity).Id
            : (byte)0;
        string grid = EntityManager.HasComponent<UnitGrid>(entity)
            ? EntityManager.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "none";
        return $"{entity}/{source}/faction={faction}/selected={EntityManager.HasComponent<SelectedUnitTag>(entity)}/grid={grid}";
    }
}
