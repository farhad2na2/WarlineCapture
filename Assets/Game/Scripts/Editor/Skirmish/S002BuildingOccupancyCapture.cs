#if UNITY_EDITOR
using System;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Editor
{
    // Raw causal inputs only. Skip rules and rectangles belong to the replay.
    internal static class S002BuildingOccupancyCapture
    {
        const int MaximumEntities = 1000000;
        static JArray Pair(int2 value) => new JArray(value.x, value.y);

        internal static JObject Read(EntityManager em, GridConfig grid)
        {
            // Identical descriptor/options to the production LiveUnitFootprintQuery.
            using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>()))
                return Read(em, grid, query, false);
        }

        // Prefer the production cached query when available, to retain its literal order.
        internal static JObject Read(EntityManager em, GridConfig grid, EntityQuery liveQuery) => Read(em, grid, liveQuery, true);

        static JObject Read(EntityManager em, GridConfig grid, EntityQuery liveQuery, bool productionQuery)
        {
            long cells = (long)grid.Width * grid.Height;
            if (grid.Width <= 0 || grid.Height <= 0 || cells > SkirmishOfflineDataExporter.MaximumCells ||
                float.IsNaN(grid.CellSize) || float.IsInfinity(grid.CellSize) || grid.CellSize <= 0 ||
                !math.all(math.isfinite(grid.Origin)))
                throw new InvalidOperationException("Bounded finite occupancy grid required");
            int expected = liveQuery.CalculateEntityCount();
            if (expected < 0 || expected > MaximumEntities)
                throw new InvalidOperationException("Pre-building live footprint census exceeds bound");
            var rows = new JArray();
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<UnitGrid> gridType = em.GetComponentTypeHandle<UnitGrid>(true);
            ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
            using (NativeArray<ArchetypeChunk> chunks = liveQuery.ToArchetypeChunkArray(Allocator.Temp))
            {
                for (int c = 0; c < chunks.Length; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    var entities = chunk.GetNativeArray(entityType);
                    var grids = chunk.GetNativeArray(ref gridType);
                    var footprints = chunk.GetNativeArray(ref footprintType);
                    if (entities.Length != grids.Length || entities.Length != footprints.Length)
                        throw new InvalidOperationException("Incomplete live footprint chunk inputs");
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity entity = entities[i];
                        if (rows.Count >= MaximumEntities || !em.Exists(entity) ||
                            !em.HasComponent<UnitGrid>(entity) || !em.HasComponent<UnitFootprint>(entity))
                            throw new InvalidOperationException("Live footprint census changed during observation");
                        var row = new JObject
                        {
                            ["ordinal"] = rows.Count, ["chunk_ordinal"] = c, ["index_in_chunk"] = i,
                            ["entity"] = new JObject { ["index"] = entity.Index, ["version"] = entity.Version },
                            ["has_unit_grid"] = true, ["unit_grid_cell"] = Pair(grids[i].Cell),
                            ["has_unit_footprint"] = true, ["unit_footprint_size"] = Pair(footprints[i].Size),
                            ["has_prefab"] = em.HasComponent<Prefab>(entity),
                            ["has_static_grid_blocker"] = em.HasComponent<StaticGridBlocker>(entity),
                            ["has_runtime_building_combat_tag"] = em.HasComponent<RuntimeBuildingCombatTag>(entity),
                            ["has_operation_map_building"] = em.HasComponent<OperationMapBuildingComponent>(entity),
                            ["has_runtime_building_combat_info"] = em.HasComponent<RuntimeBuildingCombatInfo>(entity),
                            ["has_unit_health"] = em.HasComponent<UnitHealth>(entity),
                            ["has_operation_map_building_destroyed"] = em.HasComponent<OperationMapBuildingDestroyedComponent>(entity)
                        };
                        if (em.HasComponent<RuntimeBuildingCombatInfo>(entity))
                        {
                            var value = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                            row["runtime_building_combat_info"] = new JObject
                            {
                                ["runtime_building_id"] = value.RuntimeBuildingId, ["owner_faction_id"] = value.OwnerFactionId,
                                ["origin_cell"] = Pair(value.OriginCell), ["footprint_cells"] = Pair(value.FootprintCells),
                                ["is_wall"] = value.IsWall, ["is_gate"] = value.IsGate
                            };
                        }
                        if (em.HasComponent<OperationMapBuildingComponent>(entity))
                        {
                            var value = em.GetComponentData<OperationMapBuildingComponent>(entity);
                            row["operation_map_building"] = new JObject
                            {
                                ["operation_map_id"] = value.OperationMapId.ToString(), ["stable_id"] = value.StableId.ToString(),
                                ["source_global_object_id"] = value.SourceGlobalObjectId.ToString(),
                                ["placement_index"] = value.PlacementIndex, ["blocker_policy"] = (byte)value.BlockerPolicy
                            };
                        }
                        if (em.HasComponent<UnitHealth>(entity))
                        {
                            var value = em.GetComponentData<UnitHealth>(entity);
                            row["unit_health"] = new JObject { ["current"] = value.Current, ["max"] = value.Max };
                        }
                        if (em.HasComponent<OperationMapBuildingDestroyedComponent>(entity))
                            row["operation_map_building_destroyed_enabled"] = em.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(entity);
                        rows.Add(row);
                    }
                }
            }
            if (rows.Count != expected || liveQuery.CalculateEntityCount() != expected)
                throw new InvalidOperationException("Live footprint query membership changed during observation");
            return new JObject
            {
                ["schema_version"] = 1, ["query"] = "All(UnitGrid,UnitFootprint);Options=Default",
                ["production_query_supplied"] = productionQuery, ["order"] = "native_chunk_then_entity_unsorted",
                ["grid_width"] = grid.Width, ["grid_height"] = grid.Height,
                ["count"] = rows.Count, ["complete"] = true, ["entities"] = rows
            };
        }
    }
}
#endif
