using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004BuildingDefinitionsSourceControl
{
    static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static int Main(string[] args)
    {
        try
        {
            var setup = SourceS004CompiledSetupInput.Read(Path.GetFullPath(args[0]), args[1]);
            var first = JObject.Parse(File.ReadAllText(args[2])); var second = JObject.Parse(File.ReadAllText(args[3]));
            string[] firstKeys = ((JArray)first["requested_prefab_keys"]).Values<string>().ToArray();
            string[] secondKeys = ((JArray)second["requested_prefab_keys"]).Values<string>().ToArray();
            var em = new EntityManager { LogicalAllocatorAdmitted = true }; int next = 600000;
            em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var firstImport = GeneratedReferenceTemplateImporter.Import(em, first, Path.GetDirectoryName(Path.GetFullPath(args[2])), firstKeys);
            var secondImport = GeneratedReferenceTemplateImporter.Import(em, second, Path.GetDirectoryName(Path.GetFullPath(args[3])), secondKeys);
            var templates = new Dictionary<string, Entity>(firstImport.Prefabs, StringComparer.Ordinal);
            foreach (var row in secondImport.Prefabs) templates.Add(row.Key, row.Value);
            var definitions = GeneratedBuildingDefinitions.LoadS004Source(Path.GetFullPath(args[4]), args[5], templates, setup, Path.GetFullPath(args[0]), args[1]);
            Need(definitions.Definitions.Count == 9 && definitions.Definitions.All(value => value.SourceMetadataComplete), "Nine complete actual S004 definitions required");
            var barracks = definitions.Definitions.Single(value => value.Prefab.name == "Building_Barrack");
            Need(barracks.FootprintCells.x == 28 && barracks.FootprintCells.y == 15 && barracks.MaxHealth == 1200, "Actual barracks source fields changed");
            int slots = definitions.Definitions.Sum(value => value.ProductionSlots?.Count ?? 0);
            Need(slots == 27 && definitions.Definitions.SelectMany(value => value.ProductionSlots ?? new List<RuntimeDefinition.ProductionSlotDefinition>()).All(slot => slot.SpawnUnitPrefab != null && templates.ContainsKey(slot.SpawnUnitPrefab.name)), "Original production template closure changed");
            var incomplete = new Dictionary<string, Entity>(templates, StringComparer.Ordinal);
            incomplete.Remove("Unit_Veh_APC_Heavy"); bool rejected = false;
            try { GeneratedBuildingDefinitions.LoadS004Source(Path.GetFullPath(args[4]), args[5], incomplete, setup, Path.GetFullPath(args[0]), args[1]); }
            catch (InvalidDataException) { rejected = true; }
            Need(rejected, "Missing original production template must be rejected");
            Console.WriteLine("[S004BuildingDefinitionsSourceControl] result=Passed buildings=9 slots=27 templates=23 missing=Rejected world=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
