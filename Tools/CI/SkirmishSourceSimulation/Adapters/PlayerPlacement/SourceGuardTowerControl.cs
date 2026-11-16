using System;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace Game.Runtime
{
    // Semantic source read only: no preview, native UI or placement authority is implied.
    internal static class SourceGuardTowerControl
    {
        internal static JObject Read(EntityManager em)
        {
            SourceConfiguredBuildingAdmission.Require(em);
            using var query = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),
                typeof(BuildingConfiguredSpawnableReadModel), typeof(SourceConfiguredBuildingFactsKnown));
            if (query.CalculateEntityCount() != 1)
                throw new NotSupportedException("Unique same-world configured building publication required");

            var rows = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(query.GetSingletonEntity());
            var key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_GuardTower");
            int matches = 0;
            BuildingConfiguredSpawnableReadModel row = default;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].BuildingId.ToString() == key) { row = rows[i]; matches++; }
            if (matches != 1 || row.CanRequest > 1 || row.MaterialsCost < 0 || row.Price < 0 ||
                row.FootprintCells.x <= 0 || row.FootprintCells.y <= 0)
                throw new NotSupportedException("Unique complete actual guard-tower control metadata required");

            return new JObject {
                ["source"] = "BuildingConfiguredSpawnableReadModel",
                ["prefab"] = row.BuildingId.ToString(),
                ["display_name"] = row.DisplayName.ToString(),
                ["can_request"] = row.CanRequest == 1,
                ["materials_cost"] = row.MaterialsCost,
                ["price"] = row.Price,
                ["footprint"] = new JArray(row.FootprintCells.x, row.FootprintCells.y),
                ["native_control_visible_known"] = false,
                ["native_touch_reachability_known"] = false,
                ["placement_command_bound"] = false
            };
        }
    }
}
