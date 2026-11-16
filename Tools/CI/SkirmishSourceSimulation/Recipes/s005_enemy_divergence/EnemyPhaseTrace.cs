using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Read-only, bounded source-host telemetry. These are actual ECS values, not
// native observations; recording can affect the host's wall-time scheduling.
internal static class S005EnemyPhaseTrace
{
    internal static void Capture(EntityManager em, long tick, string phase)
    {
        if (tick < 1080 || tick > 1180) return;
        using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
        using var sessionRows = sessions.ToEntityArray(Allocator.Temp);
        if (sessionRows.Length != 1) throw new InvalidOperationException("Unique source session required for diagnostic trace");
        var session = sessionRows[0];
        var sessionData = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
        var actors = new JArray();
        using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var actor in entities)
        {
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            if (!owner.SessionId.Equals(sessionData.SessionId)) continue;
            var role = em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            bool hasCell = em.HasComponent<UnitGrid>(actor), hasHealth = em.HasComponent<UnitHealth>(actor);
            var cell = hasCell ? em.GetComponentData<UnitGrid>(actor).Cell : default;
            var health = hasHealth ? em.GetComponentData<UnitHealth>(actor) : default;
            actors.Add(new JObject { ["entity_index"] = actor.Index, ["entity_version"] = actor.Version,
                ["stable_id"] = owner.StableObjectId.ToString(), ["faction"] = owner.FactionId,
                ["role"] = (int)role.Role, ["has_cell"] = hasCell, ["cell_x"] = cell.x, ["cell_y"] = cell.y,
                ["has_health"] = hasHealth, ["health"] = health.Current, ["max_health"] = health.Max });
        }
        var sortedActors = new JArray(actors.OfType<JObject>().OrderBy(x => (string)x["stable_id"], StringComparer.Ordinal)
            .ThenBy(x => (int)x["entity_index"]));
        var row = new JObject { ["tick"] = tick, ["phase"] = phase,
            ["simulation_time"] = UnityEngine.Time.time, ["enemy_materials"] = SkirmishMaterialsService.Read(em, session, 2),
            ["enemy_fuel"] = SkirmishStartingSupplyService.ReadFuel(em, session, 2),
            ["planner"] = em.HasComponent<SkirmishEnemyPlannerV1Component>(session) ? JObject.FromObject(em.GetComponentData<SkirmishEnemyPlannerV1Component>(session)) : null,
            ["production_intent"] = em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session) ? JObject.FromObject(em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session)) : null,
            ["actors"] = sortedActors, ["scope"] = "SourceHostReadOnly_SelectedComponents_NotNative" };
        string directory = Environment.GetEnvironmentVariable("WARLINE_S005_ROAD_RECEIPT_DIR");
        if (String.IsNullOrEmpty(directory)) throw new InvalidOperationException("Diagnostic output directory required");
        File.AppendAllText(Path.Combine(directory, "enemy-phase-window.jsonl"), row.ToString(Formatting.None) + "\n");
    }
}
