using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Runtime
{
    internal static class SkirmishPopulationPolicy
    {
        // Empty ground can still be an enclosed pocket. Only recruit on ground
        // connected to the approach side of this Barracks, before instantiating a unit.
        internal static void ReserveDisconnectedSpawnCells(GridConfig grid, NativeArray<GridWalkable> walkable,
            NativeBitArray blocked, int2 center, int approachDirection, ref NativeBitArray reserved,
            MapSurfaceComponent surface = default)
        {
            new SpawnConnectivityJob { Grid = grid, Walkable = walkable, Blocked = blocked,
                Center = center, ApproachDirection = approachDirection, Reserved = reserved, Surface = surface }.Run();
        }

        [Unity.Burst.BurstCompile]
        private struct SpawnConnectivityJob : Unity.Jobs.IJob
        {
            [ReadOnly] public GridConfig Grid;
            [ReadOnly] public NativeArray<GridWalkable> Walkable;
            [ReadOnly] public NativeBitArray Blocked;
            [ReadOnly] public MapSurfaceComponent Surface;
            public int2 Center;
            public int ApproachDirection;
            public NativeBitArray Reserved;
            public void Execute()
            {
                var grid = Grid; var walkable = Walkable; var blocked = Blocked;
                var center = Center; var approachDirection = ApproachDirection; var reserved = Reserved;
                var surface = Surface;
                var validation = new Game.Runtime.Pathfinding.MapSurfaceTraversalValidation();
                int2 min = math.max(0, center - 96);
                int2 max = math.min(new int2(grid.Width - 1, grid.Height - 1), center + 96);
                int width = max.x - min.x + 1;
                int count = width * (max.y - min.y + 1);
                var reachable = new NativeArray<byte>(count, Allocator.Temp);
                using var queue = new NativeList<int2>(count, Allocator.Temp);
                try
                {
                    int2 desired = math.clamp(center + new int2(approachDirection * 40, 10), min, max);
                    int2 seed = default;
                    bool found = false;
                    for (int radius = 0; radius <= 24 && !found; radius++)
                        for (int y = -radius; y <= radius && !found; y++)
                            for (int x = -radius; x <= radius; x++)
                            {
                                if (math.max(math.abs(x), math.abs(y)) != radius) continue;
                                int2 cell = desired + new int2(x, y);
                                if (math.any(cell < min) || math.any(cell > max)) continue;
                                int index = cell.y * grid.Width + cell.x;
                                if (walkable[index].Value == 0 || blocked.IsSet(index) ||
                                !validation.CanTraverse(surface, surface.HasSurfaceData, cell, MapSurfaceMovementMask.Infantry)) continue;
                                seed = cell; found = true; break;
                            }
                    if (found)
                    {
                        queue.Add(seed);
                        reachable[(seed.y - min.y) * width + seed.x - min.x] = 1;
                        for (int head = 0; head < queue.Length; head++)
                            for (int direction = 0; direction < 4; direction++)
                            {
                                int2 cell = queue[head] + (direction == 0 ? new int2(1, 0) : direction == 1 ? new int2(-1, 0) : direction == 2 ? new int2(0, 1) : new int2(0, -1));
                                if (math.any(cell < min) || math.any(cell > max)) continue;
                                int local = (cell.y - min.y) * width + cell.x - min.x;
                                int index = cell.y * grid.Width + cell.x;
                                if (reachable[local] != 0 || walkable[index].Value == 0 || blocked.IsSet(index) ||
                                !validation.CanTraverse(surface, surface.HasSurfaceData, cell, MapSurfaceMovementMask.Infantry)) continue;
                                reachable[local] = 1; queue.Add(cell);
                            }
                    }
                    for (int y = min.y; y <= max.y; y++)
                        for (int x = min.x; x <= max.x; x++)
                            if (reachable[(y - min.y) * width + x - min.x] == 0)
                                reserved.Set(y * grid.Width + x, true);
                }
                finally { reachable.Dispose(); }
            }
        }

        // Barracks exits are spawn points, not lifetime parking spaces for infantry.
        // Keep vehicle slot ownership (and all campaign behavior) unchanged. The
        // spawn resolver still checks physical occupancy before placing each recruit.
        internal static bool ReleasesProductionSlot(EntityManager em, Entity unit)
        {
            if (!em.Exists(unit) || !em.HasComponent<UnitSourcePrefabKey>(unit)) return false;
            if (em.HasComponent<SkirmishUnitRoleComponent>(unit) && em.HasComponent<SkirmishSharedActorTag>(unit))
            {
                var category = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Category;
                return category == Game.Skirmish.Contracts.SkirmishPopulationCategory.Infantry ||
                    category == Game.Skirmish.Contracts.SkirmishPopulationCategory.Ground ||
                    category == Game.Skirmish.Contracts.SkirmishPopulationCategory.LogisticsSupport;
            }
            using var session = em.CreateEntityQuery(typeof(SkirmishMatchState));
            return !session.IsEmptyIgnoreFilter &&
                PopulationKey(em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString()) == "soldier";
        }

        private static string PopulationKey(string name)
        {
            string key=name.ToLowerInvariant();
            return key.Contains("soldier")?"soldier":key.Contains("truck_tray")?"truck_tray":key.Contains("truck_tanker")?"truck_tanker":key;
        }
        public static bool CanQueue(EntityManager em,IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings,RuntimeBuildingEntity producer,GameObject prefab,int productionIndex)
        {
            using var session=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(session.IsEmptyIgnoreFilter)return true;
            if(session.GetSingleton<SkirmishMatchState>().Phase!=SkirmishPhase.Playing||producer==null||prefab==null)return false;
            if(!SkirmishCatalogPolicy.Allows(em,prefab,false))return false;
            if (SkirmishNativeProduction.TrySession(em, out var expanded))
                return SkirmishNativeProduction.CanQueue(em, expanded, producer, prefab);
            string key=PopulationKey(prefab.name);
            int limit=key.Contains("soldier")?SkirmishPresetConfig.InfantryLimitPerFaction:key.Contains("truck_tray")?2:key.Contains("truck_tanker")?1:0;
            if(limit==0)return false;
            int count=0;byte faction=producer.OwnerFactionId;
            using var units=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<UnitSourcePrefabKey>()},
                None=new[]{ComponentType.ReadOnly<OperationMapBuildingComponent>(),ComponentType.ReadOnly<RuntimeBuildingCombatTag>()}});
            using var entities=units.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
                if(em.GetComponentData<Faction>(entity).Id==faction&&em.GetComponentData<UnitHealth>(entity).Current>0&&
                   PopulationKey(em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString())==key)count++;
            if(buildings!=null)foreach(var pair in buildings)
            {
                var building=pair.Value;
                if(building?.PendingProductions==null||building.OwnerFactionId!=faction)continue;
                foreach(var pending in building.PendingProductions)
                    if(pending.Prefab!=null&&PopulationKey(pending.Prefab.name)==key)count+=Mathf.Max(1,pending.RemainingQuantity);
            }
            int quantity=BuildingDefinitionPrefabSystemHelper.GetProductionQuantity(producer.Definition,productionIndex);
            return count+Mathf.Max(1,quantity)<=limit;
        }
    }
}
