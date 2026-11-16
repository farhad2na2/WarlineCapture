using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed partial class GeneratedBuildingDefinitions
    {
        // Exact source-only factory capture; caller must validate native capture
        // provenance and all source pins before this diagnostic boundary.
        internal static GeneratedBuildingDefinitions LoadS004Source(string path, string hash,
            IDictionary<string, Entity> templates, SkirmishResolvedSetup setup, string setupPath, string setupHash)
        {
            SourceS004CompiledSetupInput.RequireExact(setup, setupPath, setupHash);
            byte[] bytes = AutonomousWorld.PinnedFile(path, hash, 16 * 1024 * 1024);
            var capture = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if ((string)capture["kind"] != "S004OriginalFullFactoryRunwayDefinitions" ||
                (string)capture["authority"] != "OriginalS004ProductionOverlayAndDefinitionFactory" ||
                (uint?)capture["setup_hash"] != setup.SetupHash || (int?)capture["seed"] != setup.Seed ||
                (bool?)capture["runway_reader_supplied"] != true || (bool?)capture["mission_world_created"] != false ||
                (bool?)capture["mission_ready"] != false || (capture["unsupported_values"] as JArray)?.Count != 0)
                throw new InvalidDataException("Exact source-only S004 native factory capture required");
            string[] expected = setup.Structures.Select(row => SkirmishStructureIds.VisualKey(row.StructureId))
                .Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray();
            var rows = capture["buildings"] as JArray;
            if (expected.Length != 9 || rows == null || rows.Count != expected.Length || templates == null)
                throw new InvalidDataException("Complete nine-building source factory domain required");
            var result = new GeneratedBuildingDefinitions(Path.GetDirectoryName(path), templates);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] as JObject; string key = (string)row?["prefab"]?["key"];
                var fields = row?["fields"] as JObject;
                if (key != expected[i] || fields == null || !JToken.DeepEquals(row["prefab"], fields["Prefab"]))
                    throw new InvalidDataException("Original S004 factory order, identity or fields differ");
                var definition = new RuntimeDefinition();
                foreach (var field in typeof(RuntimeDefinition).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.Name == "SourceMetadataComplete" || field.Name == "SourceRecipeAuthority") continue;
                    if (!fields.TryGetValue(field.Name, out var value)) throw new InvalidDataException("Missing actual S004 definition field " + field.Name);
                    if (PresentationOnly.Contains(field.Name)) { result.PresentationAssetBindings[key + "/" + field.Name] = (JObject)value.DeepClone(); continue; }
                    try { field.SetValue(definition, result.Decode(value, field.FieldType, key + "/" + field.Name)); }
                    catch (Exception error) { throw new InvalidDataException("Exact S004 building field decode failed: " + key + "/" + field.Name, error); }
                }
                if (definition.Prefab == null || definition.Prefab.name != key || definition.HasRunway || definition.IsWall)
                    throw new NotSupportedException("Complete ordinary source S004 building required");
                SourceBuildingProducerCoreProjection.Apply(definition, result.AssetIdentities);
                definition.SourceMetadataComplete = true;
                result.Definitions.Add(definition);
            }
            foreach (var definition in result.Definitions)
                foreach (var slot in definition.ProductionSlots ?? new List<RuntimeDefinition.ProductionSlotDefinition>())
                    if (slot.SpawnUnitPrefab != null && !templates.ContainsKey(slot.SpawnUnitPrefab.name))
                        throw new InvalidDataException("S004 factory production template missing: " + slot.SpawnUnitPrefab.name);
            return result;
        }
    }
}
