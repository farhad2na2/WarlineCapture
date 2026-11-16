using System;
using System.Linq;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // Opt-in source-host readback of the exact DynamicOccupancyRebuildSystem
    // contributor membership. It never imports or writes native state.
    internal static class SourceS073OccupancyContributorCensus
    {
        internal static JObject Capture(EntityManager em)
        {
            if (em == null)
                throw new ArgumentNullException(nameof(em));

            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(DynamicOccupancyComponent));
            if (grids.CalculateEntityCount() != 1)
                throw new NotSupportedException("Unique source grid with dynamic occupancy required");

            Entity gridEntity = grids.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            long cellCount = (long)grid.Width * grid.Height;
            if (grid.Width <= 0 || grid.Height <= 0 || cellCount > 4194304 ||
                !occupancy.Occupied.IsCreated || occupancy.Occupied.Length != cellCount || occupancy.GridSize != cellCount)
                throw new NotSupportedException("Dimensionally complete source occupancy readback required");

            // Keep this descriptor aligned with DynamicOccupancyRebuildSystem.OnCreate.
            using var contributors = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>() },
                None = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                    ComponentType.ReadOnly<UnitDeathAnimationComponent>()
                }
            });

            using var entities = contributors.ToEntityArray(Allocator.Temp);
            var rows = new JArray();
            var reconstructed = new bool[(int)cellCount];
            int withFaction = 0;
            foreach (Entity entity in entities.OrderBy(e => e.Index).ThenBy(e => e.Version))
            {
                UnitGrid unitGrid = em.GetComponentData<UnitGrid>(entity);
                UnitFootprint footprint = em.GetComponentData<UnitFootprint>(entity);
                bool hasFaction = em.HasComponent<Faction>(entity);
                int faction = hasFaction ? em.GetComponentData<Faction>(entity).Id : -1;
                bool hasPrefabKey = em.HasComponent<UnitSourcePrefabKey>(entity);
                string prefabKey = hasPrefabKey ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() : null;
                if (hasFaction) withFaction++;

                var bounds = Navigation.GridOccupancyKernel.Bounds(grid.Width, grid.Height,
                    unitGrid.Cell.x, unitGrid.Cell.y, footprint.Size.x, footprint.Size.y);
                var cells = new JArray();
                for (int y = bounds.MinZ; y < bounds.MaxZ; y++)
                for (int x = bounds.MinX; x < bounds.MaxX; x++)
                {
                    int index = y * grid.Width + x;
                    reconstructed[index] = true;
                    cells.Add(index);
                }

                rows.Add(new JObject
                {
                    ["entity_index"] = entity.Index,
                    ["entity_version"] = entity.Version,
                    ["cell"] = new JArray(unitGrid.Cell.x, unitGrid.Cell.y),
                    ["footprint"] = new JArray(footprint.Size.x, footprint.Size.y),
                    ["faction_id"] = hasFaction ? (JToken)faction : JValue.CreateNull(),
                    ["source_prefab_key"] = prefabKey,
                    ["covered_row_major_cell_indices_from_production_bounds"] = cells
                });
            }

            int actualOccupied = 0;
            int reconstructedOccupied = 0;
            int mismatches = 0;
            for (int i = 0; i < (int)cellCount; i++)
            {
                bool actual = occupancy.Occupied.IsSet(i);
                if (actual) actualOccupied++;
                if (reconstructed[i]) reconstructedOccupied++;
                if (actual != reconstructed[i]) mismatches++;
            }

            return new JObject
            {
                ["kind"] = "S073SourceDynamicOccupancyContributorCensus",
                ["scope"] = "source-host-read-only-post-original-owner-sample",
                ["source_state_imported"] = false,
                ["query"] = new JObject
                {
                    ["all"] = new JArray("UnitGrid", "UnitFootprint"),
                    ["none"] = new JArray("StaticGridBlocker", "RuntimeBuildingCombatTag", "UnitDeathAnimationComponent"),
                    ["entity_query_default_exclusions"] = new JArray("Prefab", "Disabled"),
                    ["actual_system"] = "DynamicOccupancyRebuildSystem._trackedUnitsQuery"
                },
                ["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height, ["cell_count"] = cellCount },
                ["contributor_count"] = entities.Length,
                ["contributors_with_faction_component"] = withFaction,
                ["contributors"] = rows,
                ["actual_occupied_cell_count"] = actualOccupied,
                ["reconstructed_occupied_cell_count"] = reconstructedOccupied,
                ["actual_vs_reconstructed_cell_mismatch_count"] = mismatches,
                ["actual_occupied_cells"] = JArray.FromObject(Enumerable.Range(0, (int)cellCount).Where(i => occupancy.Occupied.IsSet(i))),
                ["activation_or_admission_claimed"] = false,
                ["readiness_claimed"] = false
            };
        }
    }
}
