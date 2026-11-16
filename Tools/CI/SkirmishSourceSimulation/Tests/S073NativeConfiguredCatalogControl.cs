using System;
using System.Collections.Generic;
using System.IO;
using Game.Runtime;
using Newtonsoft.Json.Linq;

internal static class S073NativeConfiguredCatalogControl
{
    private static void Need(bool value, string detail) { if (!value) throw new Exception(detail); }
    private static void Main(string[] args)
    {
        Need(args.Length == 3, "Repository, receipt and receipt digest required");
        var capture = JObject.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[1]), "catalog.json")));
        var definitions = new List<RuntimeDefinition>();
        foreach (JObject row in (JArray)capture["entries"])
        {
            var prefab = SourceUnitPrefab.FromAssetIdentity((string)row["prefab_name"], (string)row["asset_path"],
                (string)row["guid"], (long)row["local_file_id"]);
            definitions.Add(new RuntimeDefinition { Prefab = prefab, DisplayName = prefab.name, Description = "fixture",
                CreditsCost = (int)row["effective_price"], MaterialsCost = (int)row["effective_materials_cost"] });
        }
        var result = S073NativeConfiguredCatalog.Bind(args[0], args[1], args[2], definitions);
        Need(result.Count == 6 && result[definitions[1].Prefab].CanRequest && result[definitions[1].Prefab].Price == 22000,
             "Native metadata did not bind to the exact source identity");
        definitions[1].CreditsCost++;
        bool rejected = false;
        try { S073NativeConfiguredCatalog.Bind(args[0], args[1], args[2], definitions); }
        catch (InvalidDataException) { rejected = true; }
        Need(rejected, "Mismatched source definition cost was accepted");
        definitions[1].CreditsCost--;
        var previous = definitions[1].Prefab;
        definitions[1].Prefab = SourceUnitPrefab.FromAssetIdentity(previous.name, previous.SourceAssetPath,
            previous.SourceAssetGuid, previous.SourceAssetLocalId + 1);
        rejected = false;
        try { S073NativeConfiguredCatalog.Bind(args[0], args[1], args[2], definitions); }
        catch (InvalidDataException) { rejected = true; }
        Need(rejected, "Same name with a different local identity was accepted");
        Console.WriteLine("[S073NativeConfiguredCatalogControl] result=Passed scope=AuthoredMetadataFixture_NotNativeMissionReadiness");
    }
}
