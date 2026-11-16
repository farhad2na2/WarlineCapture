// Source-only staged host admission. No native scene or active-water claim.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Runtime;

internal sealed class SourceDerivedMapRuntime
{
    internal SourcePlacementDomain Domain;
    internal List<Tuple<string,bool,Dictionary<Type,object>>> Navigation;
    internal bool[] Water;
    internal JObject Descriptor;
    internal string WaterSha;
    internal GeneratedS002DropClearance Clearance;
    internal string NavigationSha, BuildingSha, RoadSha, SidewalkSha, ClearanceSha;

    static JObject Read(JToken descriptor, long maximum)
    {
        return JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.Descriptor(descriptor, maximum)),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
    }

    static bool SameFrame(JToken candidate, JObject expected)
    {
        var row=candidate as JObject; var a=row?["origin"] as JArray; var b=expected?["origin"] as JArray;
        return row!=null && a!=null && b!=null && a.Count==3 && b.Count==3 &&
            (int?)row["width"]==(int?)expected["width"] && (int?)row["height"]==(int?)expected["height"] &&
            (float?)row["cell_size"]==(float?)expected["cell_size"] &&
            (float?)a[0]==(float?)b[0] && (float?)a[1]==(float?)b[1] && (float?)a[2]==(float?)b[2];
    }

    internal static SourceDerivedMapRuntime Prepare(JObject run, JObject initial)
    {
        var descriptor = run["source_derived_map"] as JObject;
        if (descriptor == null) return null;
        if (descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String || descriptor["sha256"]?.Type != JTokenType.String)
            throw new InvalidDataException("Exact source-derived map descriptor required");
        var package = Read(descriptor, 4 * 1024 * 1024);
        var map = package["static_map"] as JObject; var frame = package["frame"] as JObject;
        var capture = package["capture"] as JObject; var external = package["external_inputs"] as JObject;
        if ((string)package["kind"] != "SourceDerivedCampaignMapPackage" || (int?)package["schema_version"] != 1 ||
            (bool?)package["mission_initialized"] != false || (bool?)package["mission_ready"] != false ||
            (bool?)package["source_fit_certified"] != false || (bool?)package["live_occupancy_measured"] != false ||
            map == null || frame == null || capture == null || external == null || !JToken.DeepEquals(map, initial["static_map"]))
            throw new InvalidDataException("Unqualified source-derived package must match selected map");
        var origin = frame["origin"] as JArray;
        if (origin == null || origin.Count != 3) throw new InvalidDataException("Exact source grid origin required");
        int width = (int)frame["width"], height = (int)frame["height"];
        float cell = (float)frame["cell_size"], x = (float)origin[0], y = (float)origin[1], z = (float)origin[2];
        var domain = SourcePlacementDomain.LoadSourceDerivedCapture((string)capture["path"], (string)capture["sha256"],
            (string)package["source_root"], external, (string)map["definition"]?["sha256"],
            (string)map["operation_map_id"], width, height, cell, x, y, z);
        var water = Read(package["water_descriptor"], 4 * 1024 * 1024);
        if ((string)water["kind"] != "SourceDerivedCampaignWaterGeometry" || !JToken.DeepEquals(water["source_map"], map) ||
            !SameFrame(water["frame"], frame) || (bool?)water["active_mission_water_qualified"] != false ||
            (bool?)water["source_fit_certified"] != false || (bool?)water["mission_ready"] != false ||
            (int?)water["footprint_count"] != domain.WaterFootprintCount)
            throw new InvalidDataException("Source-derived water descriptor differs from selected geometry");
        var mask = domain.BuildWaterMask(true, width, height, cell, x, y, z);
        var bytes = mask.Select(value => value ? (byte)1 : (byte)0).ToArray();
        var expectedMask = water["mask"] as JObject;
        if (expectedMask == null || (int?)expectedMask["bytes"] != bytes.Length ||
            AutonomousWorld.Hash(bytes) != (string)expectedMask["sha256"] ||
            !AutonomousWorld.PinnedFile((string)expectedMask["path"], (string)expectedMask["sha256"], 4194304).SequenceEqual(bytes))
            throw new InvalidDataException("Source-derived water raster differs from pinned descriptor");
        var raw = Read(capture, 256 * 1024 * 1024);
        var rows = raw["fixedMapReadDomain"]?["authoredNavigationBlockers"] as JArray;
        if (rows == null) throw new InvalidDataException("Original captured navigation rows required");
        var navigation = GeneratedAuthoredNavigationImporter.DecodeRows(rows, Path.GetDirectoryName((string)capture["path"]));
        var clearanceDescriptor = package["clearance_descriptor"] as JObject;
        var clearance = Read(clearanceDescriptor, 8 * 1024 * 1024);
        if ((string)clearance["kind"] != "SourceDerivedCampaignMapDropClearanceProjection" ||
            !JToken.DeepEquals(clearance["package_capture"], capture) || !SameFrame(clearance["frame"], frame) ||
            !JToken.DeepEquals(clearance["water_descriptor"], package["water_descriptor"]) ||
            (string)clearance["static_map"]?["operation_map_id"] != (string)map["operation_map_id"] ||
            !JToken.DeepEquals(clearance["static_map"]?["definition"], map["definition"]))
            throw new InvalidDataException("Source-derived clearance does not match package identity");
        var clearanceOwner = new GeneratedS002DropClearance((string)clearanceDescriptor["path"], (string)clearanceDescriptor["sha256"]);
        var placement = raw["headlessPlacementInputs"];
        return new SourceDerivedMapRuntime { Domain = domain, Navigation = navigation, Water = mask,
            Descriptor = (JObject)descriptor.DeepClone(), WaterSha = AutonomousWorld.Hash(bytes), Clearance=clearanceOwner,
            NavigationSha=AutonomousWorld.Hash(System.Text.Encoding.UTF8.GetBytes(rows.ToString(Formatting.None))),
            BuildingSha=(string)placement?["authoredBuildings"]?["sha256"], RoadSha=(string)placement?["authoredRoadsIncludingSidewalks"]?["sha256"],
            SidewalkSha=(string)placement?["sidewalks"]?["sha256"], ClearanceSha=(string)clearanceDescriptor["sha256"] };
    }

    static string MaskHash(bool[] values) => values == null ? null : AutonomousWorld.Hash(values.Select(value=>value?(byte)1:(byte)0).ToArray());
    internal JObject InstalledReceipt(bool[] buildings,bool[] roads,bool[] sidewalks,bool[] water,int navigationCount)
    {
        if (MaskHash(buildings)!=BuildingSha || MaskHash(roads)!=RoadSha || MaskHash(sidewalks)!=SidewalkSha ||
            MaskHash(water)!=WaterSha || navigationCount!=Navigation.Count)
            throw new InvalidDataException("Installed source-derived placement or navigation differs from preflight");
        return new JObject {
        ["descriptor"] = Descriptor.DeepClone(), ["installed_water_sha256"] = WaterSha,
        ["installed_buildings_sha256"] = BuildingSha, ["installed_roads_sha256"] = RoadSha,
        ["installed_sidewalks_sha256"] = SidewalkSha, ["navigation_rows_sha256"] = NavigationSha,
        ["installed_navigation_count"] = navigationCount, ["clearance_descriptor_sha256"] = ClearanceSha,
        ["source_only"] = true, ["active_water_qualified"] = false,
        ["source_fit_certified"] = false, ["mission_ready"] = false };
    }
}
