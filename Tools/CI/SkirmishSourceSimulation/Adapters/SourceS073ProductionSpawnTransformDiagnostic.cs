using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Transforms;

// Opt-in read-only observer for the exact source definition -> retained runtime
// building production-spawn vector handoff. It does not create a Unity Instance,
// write ECS state, publish production rows, or claim native transform parity.
internal static class SourceS073ProductionSpawnTransformDiagnostic
{
    internal const string OptIn = "WARLINE_S073_PRODUCTION_SPAWN_TRANSFORM_DIAGNOSTIC";

    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidDataException(message);
    }

    static string Sha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
    }

    static string FloatBits(float value) =>
        BitConverter.ToUInt32(BitConverter.GetBytes(value), 0).ToString("x8", CultureInfo.InvariantCulture);

    static JArray VectorBits(IEnumerable<Vector3> values)
    {
        var rows = new JArray();
        if (values == null) return rows;
        foreach (var value in values)
            rows.Add(new JObject
            {
                ["x"] = FloatBits(value.x), ["y"] = FloatBits(value.y), ["z"] = FloatBits(value.z)
            });
        return rows;
    }

    static JArray ReadPinnedDefinitionVectorBits(JObject definition)
    {
        JObject fields = definition["fields"] as JObject ?? throw new InvalidDataException("Pinned S073 definition fields required");
        JObject positions = fields["ProductionSpawnLocalPositions"] as JObject ?? throw new InvalidDataException("Pinned production spawn position field required");
        string codec = (string)positions["codec"];
        if (codec == "null-v1") return new JArray();
        Require(codec == "definition-list-v1", "Pinned production spawn positions must use exact definition-list codec");
        JArray items = positions["items"] as JArray ?? throw new InvalidDataException("Pinned production position items required");
        var rows = new JArray();
        foreach (JObject item in items)
        {
            Require((string)item["codec"] == "struct-fields-v1" && (string)item["type"] == "UnityEngine.Vector3",
                "Pinned production position must be an exact Unity Vector3");
            JObject vectorFields = item["fields"] as JObject ?? throw new InvalidDataException("Pinned Vector3 fields required");
            JObject x = vectorFields["UnityEngine.Vector3::x"] as JObject;
            JObject y = vectorFields["UnityEngine.Vector3::y"] as JObject;
            JObject z = vectorFields["UnityEngine.Vector3::z"] as JObject;
            foreach (JObject component in new[] { x, y, z })
                Require(component != null && (string)component["codec"] == "f32-bits-v1" &&
                    ((string)component["bits"])?.Length == 8,
                    "Pinned production Vector3 requires exact f32 bits");
            rows.Add(new JObject { ["x"] = (string)x["bits"], ["y"] = (string)y["bits"], ["z"] = (string)z["bits"] });
        }
        return rows;
    }

    static JObject ReadWorldIdentity(EntityManager em)
    {
        using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
        Require(matches.CalculateEntityCount() == 1, "One S073 source match required for transform diagnostic");
        var match = matches.GetSingleton<SkirmishMatchState>();
        return new JObject
        {
            ["world_sequence_number"] = em.WorldUnmanaged.SequenceNumber,
            ["session_id"] = match.SessionId.ToString(),
            ["scenario_index"] = match.ScenarioIndex,
            ["seed"] = match.Seed,
            ["phase"] = match.Phase.ToString(),
            ["elapsed_seconds_bits"] = FloatBits(match.ElapsedSeconds)
        };
    }

    static JObject CaptureBuilding(EntityManager em, RuntimeBuildingEntity building,
        IReadOnlyDictionary<string, JArray> pinnedPositions,
        IReadOnlyDictionary<string, JArray> definitionPositions)
    {
        string key = building?.Definition?.Prefab?.name;
        if (string.IsNullOrEmpty(key)) throw new InvalidDataException("Registered S073 building requires a source definition identity");
        if (!pinnedPositions.TryGetValue(key, out JArray expected))
            throw new InvalidDataException("Registered building is absent from byte-pinned source definition export: " + key);
        if (!definitionPositions.TryGetValue(key, out JArray definition))
            throw new InvalidDataException("Registered building definition is absent from exact imported definition set: " + key);

        JArray runtime = VectorBits(building.ProductionSpawnLocalPositions);
        bool hasTransform = building.CombatEntity != Entity.Null && em.Exists(building.CombatEntity) &&
                            em.HasComponent<LocalTransform>(building.CombatEntity);
        JObject transform = null;
        if (hasTransform)
        {
            LocalTransform value = em.GetComponentData<LocalTransform>(building.CombatEntity);
            transform = new JObject
            {
                ["entity"] = $"{building.CombatEntity.Index}:{building.CombatEntity.Version}",
                ["position_bits"] = new JArray(FloatBits(value.Position.x), FloatBits(value.Position.y), FloatBits(value.Position.z)),
                ["rotation_bits"] = new JArray(FloatBits(value.Rotation.value.x), FloatBits(value.Rotation.value.y),
                    FloatBits(value.Rotation.value.z), FloatBits(value.Rotation.value.w)),
                ["scale_bits"] = FloatBits(value.Scale)
            };
        }
        return new JObject
        {
            ["runtime_building_id"] = building.Id,
            ["building_key"] = key,
            ["faction_id"] = building.HasOwnerFaction ? (int?)building.OwnerFactionId : null,
            ["combat_entity_exists"] = building.CombatEntity != Entity.Null && em.Exists(building.CombatEntity),
            ["definition_matches_pinned_readback"] = JToken.DeepEquals(definition, expected),
            ["runtime_positions_match_definition"] = JToken.DeepEquals(runtime, definition),
            ["runtime_positions_match_pinned_readback"] = JToken.DeepEquals(runtime, expected),
            ["pinned_readback_positions"] = expected.DeepClone(),
            ["definition_positions"] = definition.DeepClone(),
            ["runtime_building_positions"] = runtime,
            ["instance_is_null"] = building.Instance == null,
            ["ecs_local_transform_available"] = hasTransform,
            ["ecs_local_transform"] = transform
        };
    }

    static JObject CaptureState(EntityManager em, IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings,
        IReadOnlyDictionary<string, JArray> pinnedPositions,
        IReadOnlyDictionary<string, JArray> definitionPositions)
    {
        var rows = new JArray();
        foreach (var pair in buildings.OrderBy(pair => pair.Key))
            rows.Add(CaptureBuilding(em, pair.Value, pinnedPositions, definitionPositions));
        return new JObject
        {
            ["source_world"] = ReadWorldIdentity(em),
            ["registered_building_count"] = buildings.Count,
            ["buildings"] = rows
        };
    }

    internal static JObject Capture(EntityManager em, IReadOnlyList<RuntimeDefinition> definitions,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings, string readbackPath, string expectedSha256)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptIn), "1", StringComparison.Ordinal)) return null;
        if (em == null || definitions == null || buildings == null || string.IsNullOrWhiteSpace(readbackPath) ||
            expectedSha256 == null || expectedSha256.Length != 64 || expectedSha256.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
            throw new NotSupportedException("Exact source manager, definitions, building registry and byte-pinned definition readback required");

        byte[] bytes = File.ReadAllBytes(readbackPath);
        string actualSha256 = Sha256(bytes);
        Require(actualSha256 == expectedSha256, "S073 building definition readback bytes changed before spawn-transform census");
        JObject root = JObject.Parse(Encoding.UTF8.GetString(bytes), new JsonLoadSettings
        {
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
        });
        Require((int?)root["schema_version"] == 1 && (string)root["kind"] == "S073LegacyAuthoredBuildingDefinitions" &&
            (string)root["catalog_id"] == "S073" && (string)root["authority"] == "OriginalLegacyPresetConstruction_NoRuntimeRecipeOverlay" &&
            (bool?)root["mission_world_allocated"] == false && (bool?)root["full_mission_initialized"] == false,
            "Byte-pinned definition export is outside the S073 source-only scope");

        JArray retainedRows = root["definitions"] as JArray ?? throw new InvalidDataException("Pinned S073 definition rows required");
        var pinnedPositions = new Dictionary<string, JArray>(StringComparer.Ordinal);
        foreach (JObject row in retainedRows)
        {
            string key = (string)row["key"];
            Require(!string.IsNullOrWhiteSpace(key) && !pinnedPositions.ContainsKey(key), "Unique pinned building definition keys required");
            pinnedPositions.Add(key, ReadPinnedDefinitionVectorBits(row));
        }

        var definitionPositions = new Dictionary<string, JArray>(StringComparer.Ordinal);
        foreach (RuntimeDefinition definition in definitions)
        {
            if (definition == null || definition.Prefab == null || definitionPositions.ContainsKey(definition.Prefab.name))
                throw new InvalidDataException("Unique decoded S073 building definitions required");
            definitionPositions.Add(definition.Prefab.name, VectorBits(definition.ProductionSpawnLocalPositions));
        }

        JObject before = CaptureState(em, buildings, pinnedPositions, definitionPositions);
        JObject after = CaptureState(em, buildings, pinnedPositions, definitionPositions);
        Require(JToken.DeepEquals(before, after), "S073 production spawn-transform read-only guard changed during census");
        return new JObject
        {
            ["kind"] = "S073ProductionSpawnTransformDiagnostic",
            ["schema_version"] = 1,
            ["scope"] = "ReadOnlyDefinitionToRetainedBuildingPositionBitsAndEcsTransformObservation",
            ["readback_path"] = Path.GetFullPath(readbackPath),
            ["readback_sha256"] = actualSha256,
            ["readback_sha256_verified"] = true,
            ["source_guard_before"] = before,
            ["source_guard_after"] = after,
            ["source_guard_unchanged"] = true,
            ["source_world_mutated"] = false,
            ["instance_synthesized"] = false,
            ["native_gameobject_transform_equivalence_claimed"] = false,
            ["mission_ready"] = false
        };
    }
}
