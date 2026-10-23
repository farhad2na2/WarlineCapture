using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;

// Fresh S002 post-spawn logical ledgers. Native session values are assertions;
// physical building stores, production graph and army groups remain separate.
static class StartupSessionState
{
    static int Int(JsonElement setup, string name) => setup.GetProperty(name).GetInt32();
    static JsonObject Stock(JsonElement setup, bool player)
    {
        var value = new JsonObject
        {
            ["Materials"] = Int(setup, "MaterialsEach"), ["Oil"] = Int(setup, "OilEach"),
            ["Fuel"] = Int(setup, "UsableFuelEach"),
            ["MaterialsCapacity"] = Int(setup, "MaterialsCapacityEach"),
            ["OilCapacity"] = Int(setup, "OilCapacityEach"), ["FuelCapacity"] = Int(setup, "FuelCapacityEach")
        };
        if (player) value["FactionId"] = 1;
        return value;
    }
    static JsonObject Capacity(JsonElement setup, StartupActorState.Inputs inputs, byte faction)
    {
        var component = new SkirmishCapacityComponent
        {
            FactionId = faction, InfantryCap = Int(setup, "InfantryCapEach"),
            GroundCap = Int(setup, "GroundCapEach"), AirCap = Int(setup, "TacticalAirCapEach"),
            SupplyCap = Int(setup, "SupplyCapEach"), NextReservationId = faction == 1 ? 0u : 1000u
        };
        int infantry = 0, ground = 0, air = 0, supply = 0;
        foreach (var force in inputs.Forces.Where(f => f.Faction == faction))
        {
            var category = (SkirmishPopulationCategory)force.Category;
            if (category == SkirmishPopulationCategory.Infantry) infantry++;
            else if (category == SkirmishPopulationCategory.Ground) ground++;
            else if (category == SkirmishPopulationCategory.Air) air++;
            else continue; // Native spawn excludes logistics/objective support from this ledger.
            supply = checked(supply + force.Supply);
        }
        var snapshot = SkirmishCapacityLedger.ToSnapshot(component);
        SkirmishCapacityLedger.SeedLive(ref snapshot, infantry, ground, air, supply);
        component = SkirmishCapacityLedger.FromSnapshot(component, snapshot);
        var result = JsonSerializer.SerializeToNode(component, new JsonSerializerOptions { IncludeFields = true }).AsObject();
        if (faction != 1) result.Remove("FactionId");
        return result;
    }
    internal static object Derive(JsonElement root, StartupActorState.Inputs inputs)
    {
        var setup = root.GetProperty("resolved_setup");
        SpawnJson.Need(root.GetProperty("session_phase").GetString() == "Playing" && root.GetProperty("tick_clock").GetInt32() == 0,
            "Fresh post-spawn Playing tick-zero session required");
        SpawnJson.Need(setup.GetProperty("ObjectiveKind").GetByte() == (byte)SkirmishObjectiveKind.BaseAssault,
            "S002 BaseAssault startup required");
        SpawnJson.Need(inputs.Forces.All(f => f.Faction == 1 || f.Faction == 2), "Two authored starting factions required");
        var values = new JsonObject
        {
            ["player"] = Stock(setup, true), ["enemy"] = Stock(setup, false),
            ["capacity"] = Capacity(setup, inputs, 1), ["enemy_capacity"] = Capacity(setup, inputs, 2),
            ["objective"] = new JsonObject
            {
                ["Kind"] = (byte)SkirmishObjectiveKind.BaseAssault, ["State"] = (byte)SkirmishObjectiveStateKind.Active,
                ["Outcome"] = (byte)SkirmishOutcomeKind.None, ["Reason"] = (byte)SkirmishEndReasonKind.None, ["Terminal"] = 0
            },
            ["clock"] = new JsonObject { ["ElapsedSeconds"] = 0, ["DeadlineSeconds"] = Int(setup, "DeadlineSeconds"), ["Paused"] = 0, ["Playing"] = 1 }
        };
        var expected = JsonSerializer.SerializeToElement(values);
        var observed = root.GetProperty("session_facts");
        SpawnJson.Shape(observed, values.Select(v => v.Key).ToArray());
        foreach (var row in expected.EnumerateObject())
        {
            SpawnJson.Shape(observed.GetProperty(row.Name), row.Value.EnumerateObject().Select(p => p.Name).ToArray());
            SpawnJson.Expect(row.Value, observed.GetProperty(row.Name), "derived session " + row.Name);
        }
        var authored = SpawnJson.Array(setup.GetProperty("Structures"), 128);
        SpawnJson.Need(authored.Length == 18, "Complete authored S002 eighteen-structure set required");
        var actualRows = root.GetProperty("actors").EnumerateArray().Where(a => a.GetProperty("is_structure").GetBoolean()).ToArray();
        SpawnJson.Need(actualRows.Length == authored.Length, "Complete native structure census required");
        var actual = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var a in actualRows) SpawnJson.Need(actual.TryAdd(a.GetProperty("stable_id").GetString(), a), "Duplicate native structure identity");
        var structures = new List<object>(); var ids = new HashSet<string>(StringComparer.Ordinal); var designated = new int[3];
        foreach (var structure in authored)
        {
            string type = SpawnJson.Id(structure.GetProperty("StructureId")); byte faction = structure.GetProperty("FactionId").GetByte();
            SpawnJson.Need(faction == 1 || faction == 2, "Two authored structure factions required");
            string id = type + "." + faction, key = SkirmishStructureIds.VisualKey(type);
            SpawnJson.Need(!string.IsNullOrEmpty(key) && ids.Add(id), "Unique supported authored structure required");
            SpawnJson.Need(actual.TryGetValue(id, out var native) && native.GetProperty("source_key").GetString() == key && native.GetProperty("faction").GetByte() == faction,
                "Derived structure identity/key/faction differs at " + id);
            bool isBase = structure.GetProperty("DesignatedBase").GetBoolean();
            string objectiveId = structure.GetProperty("ObjectiveRoleId").GetString();
            if (isBase)
            {
                designated[faction]++;
                SpawnJson.Need(type == SkirmishStructureIds.Barracks && objectiveId == (faction == 1 ? SkirmishObjectiveIds.BasePlayer : SkirmishObjectiveIds.BaseEnemy), "Authored designated base binding differs");
            }
            structures.Add(new
            {
                id, structure_id = type, source_key = key, faction, producer = (byte)SkirmishStructureIds.ProducerFor(type),
                designated_base = isBase, objective_role = isBase ? (byte)(faction == 1 ? SkirmishObjectiveRoleKind.PlayerBase : SkirmishObjectiveRoleKind.EnemyBase) : (byte)0,
                objective_id = isBase ? objectiveId : null,
                ground_staging_queues = type == SkirmishStructureIds.GroundStaging ? new[] { 1, 1 } : null,
                physical_state_initialized = false
            });
        }
        SpawnJson.Need(designated[1] == 1 && designated[2] == 1, "Exactly one original designated base per faction required");
        return new { logical_components = values, structure_bindings = structures, logical_component_count = 6, structure_count = structures.Count,
            physical_economy_initialized = false, army_groups_initialized = false, full_session_initialized = false };
    }
}
