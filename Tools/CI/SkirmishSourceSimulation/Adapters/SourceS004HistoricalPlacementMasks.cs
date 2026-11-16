using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Explicitly historical pregame owner readback; never admission or source-fit evidence.
    internal sealed class SourceS004HistoricalPlacementMasks
    {
        internal readonly bool[] Buildings, Roads, Sidewalks;
        readonly Dictionary<string, string> currentPins;
        readonly HashSet<string> missingPins;
        SourceS004HistoricalPlacementMasks(bool[] buildings, bool[] roads, bool[] sidewalks,
            Dictionary<string, string> current, HashSet<string> missing)
        { Buildings=buildings; Roads=roads; Sidewalks=sidewalks; currentPins=current; missingPins=missing; }
        static string Hash(string path)
        {
            using var stream=File.OpenRead(path); using var hash=System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static JObject Read(JToken descriptor,int limit)
        {
            if (descriptor is not JObject d || d.Count!=2 || d["path"]?.Type!=JTokenType.String || d["sha256"]?.Type!=JTokenType.String)
                throw new InvalidDataException("Exact historical mask descriptor required");
            return JObject.Parse(new UTF8Encoding(false,true).GetString(AutonomousWorld.PinnedFile((string)d["path"],(string)d["sha256"],limit)),
                new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
        }
        static bool[] Mask(JToken token,string name,int size)
        {
            var row=token as JObject;
            if(row==null || (string)row["name"]!=name || (bool?)row["present"]!=true || (bool?)row["ownerReady"]!=true ||
                (string)row["encoding"]!="uint8-bool01-row-major-xfast" || (int?)row["count"]!=size ||
                (int?)row["byteCount"]!=size || (int?)row["actualLength"]!=size || (int?)row["expectedLength"]!=size)
                throw new InvalidDataException("Complete original historical mask required: "+name);
            byte[] bytes=Convert.FromBase64String((string)row["base64"]);
            using var hash=System.Security.Cryptography.SHA256.Create();
            if(bytes.Length!=size || bytes.Any(v=>v>1) || BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant()!=(string)row["sha256"])
                throw new InvalidDataException("Historical mask bytes differ: "+name);
            var result=new bool[size]; for(int i=0;i<size;i++)result[i]=bytes[i]!=0; return result;
        }
        internal static SourceS004HistoricalPlacementMasks Load(JToken token,string root,OperationMapDefinition map)
        {
            if(token is not JObject spec || spec.Count!=3 || (string)spec["kind"]!="HistoricalCityEdgePlacementMasks" || map==null || !Path.IsPathRooted(root))
                throw new InvalidDataException("Exact historical placement mask diagnostic required");
            var cap=Read(spec["capture"],128*1024*1024); var audit=Read(spec["audit"],4*1024*1024);
            var source=map.SourceBinding; var grid=map.GridMetadata;
            if(map.OperationMapId!="opmap.skirmish.desert_base_01" || source.SourceOperationMapId!="opmap.skirmish.cityedgeairfield_prepared" ||
                source.SourceIdentityHash!="3702e8f11717773ce79fe38461c50cfeaf0caf86e9a62b0670166d9461aea5db" ||
                source.SourceContentHash!="e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22" ||
                grid.Dimensions.x!=2048 || grid.Dimensions.y!=1024 || grid.CellSize!=1 ||
                (string)cap["kind"]!="PinnedCampaignPhysicalInputExport" || (string)cap["result"]!="Collected" ||
                (bool?)cap["worldDisposed"]!=true || (bool?)cap["missionReady"]!=false ||
                (string)cap["operationMapId"]!=source.SourceOperationMapId || (string)audit["status"]!="AuditedHistoricalCapture" ||
                (string)audit["capture_sha256"]!=(string)spec["capture"]?["sha256"] ||
                (bool?)audit["source_definition_current_match"]!=true ||
                (string)audit["capture_source_definition_recorded_sha256"]!=(string)cap["sourceSha256"] ||
                (bool?)audit["current_source_fit_certified"]!=false ||
                (bool?)audit["native_parity"]!=false || (bool?)audit["mission_ready"]!=false)
                throw new InvalidDataException("Historical capture/map binding differs");
            var input=cap["headlessPlacementInputs"] as JObject;
            if(input==null || (string)input["authority"]!="ActualNativeReadOnlySourceOwners_NotMissionState" ||
                (string)input["sourceDefinitionSha256"]!=(string)cap["sourceSha256"] || (string)input["operationMapId"]!=source.SourceOperationMapId ||
                (int?)input["gridWidth"]!=2048 || (int?)input["gridHeight"]!=1024 || (float?)input["gridCellSize"]!=1f ||
                !JToken.DeepEquals(input["gridOrigin"],new JArray(0.0,0.0,0.0)) || (int?)input["skirmishMatchQueryCount"]!=0)
                throw new InvalidDataException("Historical mask frame or inactive-world scope differs");
            var changed=((JArray)audit["read_pins"]?["changed"])?.OfType<JObject>().ToDictionary(x=>(string)x["path"],x=>x,StringComparer.Ordinal)
                ?? throw new InvalidDataException("Historical drift audit required");
            var missing=new HashSet<string>(((JArray)audit["read_pins"]?["missing"])?.Select(x=>(string)x["path"]) ?? throw new InvalidDataException("Historical missing pin audit required"),StringComparer.Ordinal);
            var pins=new Dictionary<string,string>(StringComparer.Ordinal); var recorded=cap["readPins"] as JObject ?? throw new InvalidDataException("Historical source pins required");
            foreach(var pin in recorded.Properties())
            {
                string path=Path.GetFullPath(Path.IsPathRooted(pin.Name)?pin.Name:Path.Combine(root,pin.Name)); string expected=(string)pin.Value;
                if(missing.Contains(path)){if(File.Exists(path))throw new InvalidDataException("Historical missing pin changed: "+path);continue;}
                if(changed.TryGetValue(path,out JObject drift))
                {
                    if ((string)drift["captured_sha256"]!=expected) throw new InvalidDataException("Historical captured drift differs: "+path);
                    expected=(string)drift["current_sha256"];
                }
                if(!File.Exists(path) || Hash(path)!=expected)throw new InvalidDataException("Historical current input drift: "+path);
                if(pins.TryGetValue(path,out string prior) && prior!=expected)throw new InvalidDataException("Conflicting historical pin");
                pins[path]=expected;
            }
            foreach(string key in new[]{"capture","audit"}){var d=(JObject)spec[key]; pins[Path.GetFullPath((string)d["path"])]=(string)d["sha256"];}
            return new SourceS004HistoricalPlacementMasks(Mask(input["authoredBuildings"],"authoredBuildings",2097152),
                Mask(input["authoredRoadsIncludingSidewalks"],"authoredRoadsIncludingSidewalks",2097152),
                Mask(input["sidewalks"],"sidewalks",2097152),pins,missing);
        }
        internal void VerifyInputs()
        {
            foreach(var p in currentPins)if(!File.Exists(p.Key)||Hash(p.Key)!=p.Value)throw new InvalidDataException("Historical mask source changed during run: "+p.Key);
            foreach(var p in missingPins)if(File.Exists(p))throw new InvalidDataException("Historical missing mask source appeared during run: "+p);
        }
    }
}
