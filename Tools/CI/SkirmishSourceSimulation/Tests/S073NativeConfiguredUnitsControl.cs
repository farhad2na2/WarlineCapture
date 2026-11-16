using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S073NativeConfiguredUnitsControl
{
    private static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Main(string[] args)
    {
        Need(args.Length == 7, "Repository, unit receipt/hash, identity/hash, prefab capture/hash required");
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        manager.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        var capture = JObject.Parse(File.ReadAllText(args[5]));
        string[] keys = { "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker", "Unit_Veh_Light_Armored_Car" };
        var imported = GeneratedReferenceTemplateImporter.Import(manager, capture, Path.GetDirectoryName(Path.GetFullPath(args[5])), keys);
        var units = S073NativeConfiguredUnits.Bind(manager, imported, args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
        Need(units.Count == 3 && units.TryRead(1, out var tray, out _, out int displayed, out bool requestable, out bool vehicle) &&
             tray.Template == imported.Prefabs["Unit_Veh_Truck_Tray"] && displayed == 24 && requestable && vehicle &&
             units.TryCosts(tray, 0, out int credits, out int materials) && credits == 12000 && materials == 24,
             "Native unit metadata did not join the actual imported source template");
        // The dummy building is only the required configured-boundary fixture;
        // the unit rows below are produced by the extracted original publisher.
        var dummyPrefab = SourceUnitPrefab.FromAssetIdentity("FixtureBuilding", "Assets/Fixture.prefab", new string('a', 32), 1);
        var dummyDefinition = new RuntimeDefinition { Prefab = dummyPrefab, DisplayName = "fixture", Description = "fixture", SourceMetadataComplete = true };
        var definitions = new GeneratedConfiguredDefinitionSource();
        definitions.BindGeneratedDefinitions(new[] { dummyDefinition }, true);
        definitions.BindRequestMetadata(new[] { dummyPrefab }, new Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry> {
            [dummyPrefab] = new BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry("fixture", "fixture", dummyPrefab, true, 0) });
        definitions.BindCapturedReadModelUnitDomain(units.Count, units.TryRead, units.TryCosts);
        var boundary = manager.CreateEntity();
        manager.AddComponentData(boundary, new BuildingRuntimeStateTag());
        var grid = new GridConfig { Width = 1, Height = 1, CellSize = 1 };
        var mask = new bool[1];
        var processor = new GeneratedRuntimeSpawnProcessor(new GeneratedRoadMask(grid, mask, mask, mask), new GeneratedBuildingMask(grid, mask));
        processor.PublishS073CapturedConfiguredUnits(definitions, manager, boundary);
        var published = manager.GetBuffer<BuildingConfiguredUnitReadModel>(boundary);
        Need(published.Length == 3 && published[1].Price == 24 && published[1].CreditsCost == 12000 &&
             published[1].CanRequest == 1 && published[1].IsVehicle == 1,
             "Original configured-unit publisher changed captured order or costs");
        bool rejected = false;
        try { S073NativeConfiguredUnits.Bind(new EntityManager(), imported, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); }
        catch (InvalidDataException) { rejected = true; }
        Need(rejected, "A different source manager was accepted");
        var originalTray = imported.Prefabs["Unit_Veh_Truck_Tray"];
        imported.Prefabs["Unit_Veh_Truck_Tray"] = imported.Prefabs["Unit_Veh_Truck_Tanker"];
        rejected = false;
        try { S073NativeConfiguredUnits.Bind(manager, imported, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); }
        catch (InvalidDataException) { rejected = true; }
        Need(rejected, "Wrong same-world template mapping was accepted");
        imported.Prefabs["Unit_Veh_Truck_Tray"] = originalTray;
        var changedIdentity = JObject.Parse(File.ReadAllText(args[3]));
        var trayIdentity = ((JArray)changedIdentity["objects"]).OfType<JObject>().Single(x => (string)x["unit_source_key"] == "Unit_Veh_Truck_Tray");
        trayIdentity["file_id"] = (long)trayIdentity["file_id"] + 1;
        string temporary = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temporary, changedIdentity.ToString());
            using var sha = SHA256.Create();
            string changedHash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(temporary))).Replace("-", "").ToLowerInvariant();
            rejected = false;
            try { S073NativeConfiguredUnits.Bind(manager, imported, args[0], args[1], args[2], temporary, changedHash, args[5], args[6]); }
            catch (InvalidDataException) { rejected = true; }
            Need(rejected, "Different prefab local ID was accepted");
            trayIdentity["file_id"] = (long)trayIdentity["file_id"] - 1;
            trayIdentity["asset_sha256"] = new string('0', 64);
            File.WriteAllText(temporary, changedIdentity.ToString());
            changedHash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(temporary))).Replace("-", "").ToLowerInvariant();
            rejected = false;
            try { S073NativeConfiguredUnits.Bind(manager, imported, args[0], args[1], args[2], temporary, changedHash, args[5], args[6]); }
            catch (InvalidDataException) { rejected = true; }
            Need(rejected, "Different asset bytes across capture epochs were accepted");
        }
        finally { File.Delete(temporary); }
        Console.WriteLine("[S073NativeConfiguredUnitsControl] result=Passed scope=ImportedTemplates_AuthoredMetadata_NotMissionReadiness");
    }
}
