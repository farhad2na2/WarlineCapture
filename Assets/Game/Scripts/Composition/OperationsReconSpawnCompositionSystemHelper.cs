using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
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
            using var gridQuery = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent));
            if (registryQuery.CalculateEntityCount() != 1 || surfaceQuery.CalculateEntityCount() != 1 || gridQuery.CalculateEntityCount() != 1) return false;
            var surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            if (!surface.SurfaceBlob.IsCreated) return false;
            em.CompleteAllTrackedJobs();
            var grid = gridQuery.GetSingleton<GridConfig>();
            var blockers = gridQuery.GetSingleton<DynamicBlockerComponent>();
            var walkable = em.GetBuffer<GridWalkable>(gridQuery.GetSingletonEntity(), true);
            var objectives = new List<float3> { definition.exitPosition, definition.evidencePosition };
            foreach (var point in definition.scanPositions) objectives.Add(point);
            if (!ValidateNavigation(grid, walkable, blockers, objectives, out error))
            {
                if (error.Contains("blocked") || error.Contains("no ground route"))
                    DescribeNavigation(grid, walkable, blockers, ref surface, definition, objectives);
                return false;
            }
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
                    if (!TryPlace(ref surface, grid, walkable, blockers, force.position, occupied, out float3 position))
                    { error = "Insufficient walkable spawn space for " + force.sourceKey; return false; }
                    placements.Add(position);
                }
            int index = 0, sceneOwned = 0;
            em.AddBuffer<OperationsReconSpawnRecord>(session);
            uint seed = em.GetComponentData<OperationsReconMissionComponent>(session).Seed;
            var patrolRoute = em.AddBuffer<OperationsReconPatrolWaypoint>(session);
            foreach (var point in definition.patrolRoute) patrolRoute.Add(new OperationsReconPatrolWaypoint { Position = point });
            foreach (var force in definition.forces)
                for (int i = 0; i < force.count; i++)
                {
                    Entity unit = em.Instantiate(prefabs[force.sourceKey]);
                    // A live SubScene refresh can unload the prefab's section during the tour.
                    // These actors belong to the attempt and are destroyed by Operations on return.
                    if (em.HasComponent<SceneTag>(unit)) sceneOwned++;
                    SkirmishRuntimeActorOwnership.DetachFromSourceScene(em, unit);
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
            UnityEngine.Debug.Log($"[OperationsRoster] phase=Spawned placed={placements.Count} uniqueCells={occupied.Count} inheritedSceneTags={sceneOwned}");
            return true;
        }

        // Surface bakes alone cannot see runtime building footprints. Validate the
        // same ground grid used by player movement before allowing the mission clock.
        internal static bool ValidateNavigation(GridConfig grid, DynamicBuffer<GridWalkable> walkable,
            DynamicBlockerComponent blockers, List<float3> objectives, out string error)
        {
            error = string.Empty;
            if (objectives == null || objectives.Count == 0 || walkable.Length != grid.Width * grid.Height || !blockers.Blocked.IsCreated)
            { error = "Mission navigation is not ready."; return false; }
            var cells = new List<int2>(objectives.Count);
            foreach (var point in objectives)
            {
                int2 cell = GridUtils.WorldToCell(grid, point);
                if (!IsOpen(grid, walkable, blockers, cell))
                { error = "Mission objective is blocked at " + cell; return false; }
                cells.Add(cell);
            }
            var seen = FindConnectedCells(grid, walkable, blockers, cells, cells[0]);
            foreach (var cell in cells)
                if (!seen.Contains(cell)) { error = "Mission objective has no ground route at " + cell; return false; }
            return true;
        }

        internal static void DescribeNavigation(GridConfig grid, DynamicBuffer<GridWalkable> walkable,
            DynamicBlockerComponent blockers, ref MapSurfaceComponent surface, OperationsReconMissionConfig definition,
            List<float3> objectives)
        {
            var cells = new List<int2>(objectives.Count);
            foreach (var point in objectives) cells.Add(GridUtils.WorldToCell(grid, point));
            // Use the exit as in validation, or the first open objective if the exit is blocked.
            int reference = cells.FindIndex(cell => IsOpen(grid, walkable, blockers, cell));
            var connected = reference >= 0
                ? FindConnectedCells(grid, walkable, blockers, cells, cells[reference])
                : new HashSet<int2>();
            UnityEngine.Debug.LogWarning($"[OperationsStartup] nav reference={(reference >= 0 ? cells[reference].ToString() : "none")}");
            LogNavigationPoint("exit", definition.exitPosition, grid, walkable, blockers, ref surface, connected);
            LogNavigationPoint("evidence", definition.evidencePosition, grid, walkable, blockers, ref surface, connected);
            for (int i = 0; i < definition.scanPositions.Length; i++)
                LogNavigationPoint($"scan[{i}]", definition.scanPositions[i], grid, walkable, blockers, ref surface, connected);
            for (int i = 0; i < definition.forces.Length; i++)
            {
                var force = definition.forces[i];
                LogNavigationPoint($"force[{i}] {force.sourceKey}", force.position, grid, walkable, blockers, ref surface, connected);
            }
        }

        private static void LogNavigationPoint(string role, float3 position, GridConfig grid, DynamicBuffer<GridWalkable> walkable,
            DynamicBlockerComponent blockers, ref MapSurfaceComponent surface, HashSet<int2> connected)
        {
            int2 center = GridUtils.WorldToCell(grid, position);
            string nearest = "nearest=none";
            if (TryFindNearestConnectedCell(center, connected, out int2 cell))
                nearest = $"nearest={cell} nearestWorld={FormatWorld(GetNavigationWorld(grid, ref surface, cell))}";
            else if (TryFindExtendedConnectedCell(center, connected, out cell))
                nearest += $" extendedNearest={cell} extendedNearestWorld={FormatWorld(GetNavigationWorld(grid, ref surface, cell))}";
            UnityEngine.Debug.LogWarning($"[OperationsStartup] nav {role} cell={center} world={FormatWorld(position)} " +
                $"open={(IsOpen(grid, walkable, blockers, center) ? 1 : 0)} groundConnected={(connected.Contains(center) ? 1 : 0)} {nearest}");
        }

        private static float3 GetNavigationWorld(GridConfig grid, ref MapSurfaceComponent surface, int2 cell)
        {
            float3 world = GridUtils.CellToWorldCenter(grid, cell);
            if (MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, Cell(surface, world), out var sample))
                world.y = sample.Height;
            return world;
        }

        private static bool TryFindExtendedConnectedCell(int2 center, HashSet<int2> connected, out int2 nearest)
        {
            nearest = default;
            int nearestDistance = int.MaxValue;
            foreach (var cell in connected)
            {
                int distance = math.cmax(math.abs(cell - center));
                if (distance > nearestDistance || (distance == nearestDistance &&
                    (cell.y > nearest.y || (cell.y == nearest.y && cell.x >= nearest.x)))) continue;
                nearest = cell;
                nearestDistance = distance;
            }
            return nearestDistance < int.MaxValue;
        }

        private static string FormatWorld(float3 position) =>
            FormattableString.Invariant($"({position.x:R}, {position.y:R}, {position.z:R})");

        private static bool TryFindNearestConnectedCell(int2 center, HashSet<int2> connected, out int2 cell)
        {
            // Chebyshev rings, with ascending grid row then column within each ring.
            for (int radius = 0; radius <= 16; radius++)
            for (int z = -radius; z <= radius; z++)
            for (int x = -radius; x <= radius; x++)
            {
                if (math.max(math.abs(x), math.abs(z)) != radius) continue;
                cell = center + new int2(x, z);
                if (connected.Contains(cell)) return true;
            }
            cell = default;
            return false;
        }

        private static HashSet<int2> FindConnectedCells(GridConfig grid, DynamicBuffer<GridWalkable> walkable,
            DynamicBlockerComponent blockers, List<int2> objectives, int2 reference)
        {
            int2 min = new(grid.Width - 1, grid.Height - 1), max = int2.zero;
            foreach (var cell in objectives)
            {
                min = math.min(min, cell);
                max = math.max(max, cell);
            }
            min = math.max(int2.zero, min - 32); max = math.min(new int2(grid.Width - 1, grid.Height - 1), max + 32);
            var seen = new HashSet<int2> { reference }; var pending = new Queue<int2>(); pending.Enqueue(reference);
            int2[] steps = { new(1,0), new(-1,0), new(0,1), new(0,-1) };
            while (pending.Count > 0)
            {
                int2 current = pending.Dequeue();
                foreach (var step in steps)
                {
                    int2 next = current + step;
                    if (math.any(next < min) || math.any(next > max) || seen.Contains(next) || !IsOpen(grid, walkable, blockers, next)) continue;
                    seen.Add(next); pending.Enqueue(next);
                }
            }
            return seen;
        }

        private static bool IsOpen(GridConfig grid, DynamicBuffer<GridWalkable> walkable, DynamicBlockerComponent blockers, int2 cell)
        {
            if (!GridUtils.InBounds(cell, grid.Width, grid.Height)) return false;
            int index = GridUtils.CellToIndex(cell, grid.Width);
            return walkable[index].Value != 0 && !blockers.Blocked.IsSet(index);
        }

        private static bool TryPlace(ref MapSurfaceComponent surface, GridConfig grid, DynamicBuffer<GridWalkable> walkable,
            DynamicBlockerComponent blockers, float3 near, HashSet<int2> occupied, out float3 position)
        {
            int2 center = Cell(surface, near);
            for (int radius = 0; radius <= 12; radius++)
            for (int z = -radius; z <= radius; z++)
            for (int x = -radius; x <= radius; x++)
            {
                int2 cell = center + new int2(x, z);
                if (occupied.Contains(cell) || !IsOpen(grid, walkable, blockers, cell) || !MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, cell, out var sample) ||
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
