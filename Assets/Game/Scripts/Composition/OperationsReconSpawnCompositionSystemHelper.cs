using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Composition
{
    /// <summary>One-time conversion of validated authoring to shared-world entity prefabs.</summary>
    internal static class OperationsReconSpawnCompositionSystemHelper
    {
        internal static bool TrySpawn(EntityManager em, Entity session, OperationsReconMissionConfig definition, out string error)
        {
            error = string.Empty;
            var roster = em.GetBuffer<OperationsReconRosterElement>(session);
            if (roster.Length > 0) return true;
            using var registryQuery = em.CreateEntityQuery(typeof(UnitPrefabRegistryTag), typeof(UnitPrefabRegistryEntry));
            using var surfaceQuery = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if (registryQuery.CalculateEntityCount() != 1 || surfaceQuery.CalculateEntityCount() != 1) return false;
            var surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            if (!surface.SurfaceBlob.IsCreated) return false;
            var registry = em.GetBuffer<UnitPrefabRegistryEntry>(registryQuery.GetSingletonEntity(), true);
            var prefabs = new Dictionary<string, Entity>();
            foreach (var entry in registry)
                if (em.HasComponent<UnitSourcePrefabKey>(entry.Prefab))
                    prefabs[em.GetComponentData<UnitSourcePrefabKey>(entry.Prefab).Value.ToString()] = entry.Prefab;
            foreach (var force in definition.forces)
                if (!prefabs.ContainsKey(force.sourceKey))
                { error = "Unit prefab is not ready: " + force.sourceKey; return false; }

            // Resolve every placement before creating any unit. A bad asset fails as a
            // whole instead of leaving a half-spawned force that can accidentally win.
            var placements = new List<float3>();
            var occupied = new HashSet<int2>();
            foreach (var force in definition.forces)
                for (int i = 0; i < force.count; i++)
                {
                    if (!TryPlace(ref surface, force.position, occupied, out float3 position))
                    { error = "Insufficient walkable spawn space for " + force.sourceKey; return false; }
                    placements.Add(position);
                }
            int index = 0;
            em.AddBuffer<OperationsReconSpawnRecord>(session);
            uint seed = em.GetComponentData<OperationsReconMissionComponent>(session).Seed;
            var patrolRoute = em.AddBuffer<OperationsReconPatrolWaypoint>(session);
            foreach (var point in definition.patrolRoute) patrolRoute.Add(new OperationsReconPatrolWaypoint { Position = point });
            foreach (var force in definition.forces)
                for (int i = 0; i < force.count; i++)
                {
                    Entity unit = em.Instantiate(prefabs[force.sourceKey]);
                    float3 position = placements[index++];
                    Set(em, unit, LocalTransform.FromPosition(position));
                    Set(em, unit, new UnitGrid { Cell = Cell(surface, position) });
                    Set(em, unit, new UnitPrevWorldPos { Value = position });
                    Set(em, unit, new UnitMoveVisualComponent());
                    Set(em, unit, new Faction { Id = force.faction });
                    Set(em, unit, new OperationsReconMemberComponent { Session = session, StableIndex = index });
                    em.GetBuffer<OperationsReconSpawnRecord>(session).Add(new OperationsReconSpawnRecord { Unit = unit, StableIndex = index });
                    if (em.HasComponent<UnitIdleWanderComponent>(unit))
                    {
                        var wander = em.GetComponentData<UnitIdleWanderComponent>(unit);
                        wander.RandomState = math.max(1u, math.hash(new uint2(seed, (uint)index)));
                        em.SetComponentData(unit, wander);
                    }
                    if (em.HasComponent<SelectedUnitTag>(unit)) em.RemoveComponent<SelectedUnitTag>(unit);
                    // O001 has a finite original roster; normal skirmish respawn is inapplicable.
                    if (em.HasComponent<UnitRespawnPrefab>(unit)) em.RemoveComponent<UnitRespawnPrefab>(unit);
                    if (force.faction == 1)
                        em.GetBuffer<OperationsReconRosterElement>(session).Add(new OperationsReconRosterElement
                        { Unit = unit, Recon = force.recon ? (byte)1 : (byte)0 });
                    if (force.wave != 0)
                    {
                        em.AddComponentData(unit, new OperationsReconReserveComponent { Session = session, Wave = force.wave });
                        em.AddComponent<Disabled>(unit);
                    }
                    if (force.patrol)
                        em.AddComponentData(unit, new OperationsReconPatrolComponent
                        { Session = session, Offset = new float3(i % 2 * 2, 0, i / 2 * 2) });
                }
            em.AddComponentData(session, new OperationsReconWaveComponent
            { WarningSeconds = definition.reinforcementWarningSeconds, EvidenceWarningSeconds = definition.evidenceReinforcementWarningSeconds });
            return true;
        }

        private static bool TryPlace(ref MapSurfaceComponent surface, float3 near, HashSet<int2> occupied, out float3 position)
        {
            int2 center = Cell(surface, near);
            for (int radius = 0; radius <= 12; radius++)
            for (int z = -radius; z <= radius; z++)
            for (int x = -radius; x <= radius; x++)
            {
                int2 cell = center + new int2(x, z);
                if (occupied.Contains(cell) || !MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, cell, out var sample) ||
                    sample.SurfaceType == MapSurfaceType.Blocked || (sample.MovementMask & MapSurfaceMovementMask.Infantry) == 0) continue;
                occupied.Add(cell);
                position = surface.GridOrigin + new float3((cell.x + .5f) * surface.CellSize, 0, (cell.y + .5f) * surface.CellSize);
                position.y = sample.Height;
                return true;
            }
            position = default; return false;
        }
        private static int2 Cell(MapSurfaceComponent surface, float3 position) => (int2)math.floor((position.xz - surface.GridOrigin.xz) / surface.CellSize);
        private static void Set<T>(EntityManager em, Entity entity, T component) where T : unmanaged, IComponentData
        { if (em.HasComponent<T>(entity)) em.SetComponentData(entity, component); else em.AddComponentData(entity, component); }
    }
}
