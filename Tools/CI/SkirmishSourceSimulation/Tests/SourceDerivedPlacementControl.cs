using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Runtime;
using Newtonsoft.Json.Linq;

internal static class SourceDerivedPlacementControl
{
    static string Hash(byte[] bytes) { using var sha=SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static JObject Read(string path,string sha) { var bytes=File.ReadAllBytes(path); if(Hash(bytes)!=sha)throw new InvalidDataException("Pinned control input changed"); return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes)); }
    static SourcePlacementDomain LoadCapture(JObject capture,JObject source,JObject map,JObject frame)
    {
        var bytes=System.Text.Encoding.UTF8.GetBytes(capture.ToString(Newtonsoft.Json.Formatting.None));
        var path=Path.Combine(Path.GetTempPath(),"source-derived-placement-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            File.WriteAllBytes(path,bytes); var origin=(JArray)frame["origin"];
            return SourcePlacementDomain.LoadSourceDerivedCapture(path,Hash(bytes),(string)source["source_root"],
                (JObject)source["external_inputs"],(string)map["definition"]["sha256"],(string)map["operation_map_id"],
                (int)frame["width"],(int)frame["height"],(float)frame["cell_size"],(float)origin[0],(float)origin[1],(float)origin[2]);
        }
        finally { if(File.Exists(path))File.Delete(path); }
    }
    static bool RejectsCapture(JObject capture,JObject source,JObject map,JObject frame)
    {
        try { LoadCapture(capture,source,map,frame); return false; }
        catch(InvalidDataException) { return true; }
    }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=4)throw new ArgumentException("Initialization and source-derived water descriptors with hashes required");
            var initial=Read(args[0],args[1]); var water=Read(args[2],args[3]);
            var source=initial["placement_domain"]?["s026_inactive_diagnostic"] as JObject;
            var map=initial["static_map"] as JObject; var frame=water["frame"] as JObject;
            if(source==null||map==null||frame==null||!JToken.DeepEquals(map,water["source_map"])||
                (string)water["kind"]!="SourceDerivedCampaignWaterGeometry"||(bool?)water["active_mission_water_qualified"]!=false||
                (bool?)water["source_fit_certified"]!=false||(bool?)water["mission_ready"]!=false)
                throw new InvalidDataException("Unqualified map-bound source water descriptor required");
            var origin=frame["origin"] as JArray;
            if(origin==null||origin.Count!=3)throw new InvalidDataException("Exact grid origin required");
            int width=(int)frame["width"],height=(int)frame["height"]; float cell=(float)frame["cell_size"];
            float x=(float)origin[0],y=(float)origin[1],z=(float)origin[2];
            SourcePlacementDomain Load(string mapId,int w)=>SourcePlacementDomain.LoadSourceDerivedCapture((string)source["path"],(string)source["sha256"],
                (string)source["source_root"],(JObject)source["external_inputs"],(string)map["definition"]["sha256"],mapId,w,height,cell,x,y,z);
            bool badMap=false,badFrame=false;
            try { Load("opmap.invalid",width); } catch(InvalidDataException) { badMap=true; }
            try { Load((string)map["operation_map_id"],width-1); } catch(InvalidDataException) { badFrame=true; }
            if(!badMap||!badFrame)throw new InvalidDataException("Wrong-map and frame controls must reject");
            var domain=Load((string)map["operation_map_id"],width);
            var capture=Read((string)source["path"],(string)source["sha256"]);
            var selection=(JObject)capture["compilerSelection"];
            var result=Read((string)selection["path"],(string)selection["sha256"]);
            var readPins=(JObject)capture["readPins"];
            var requestName=readPins.Properties().Select(p=>p.Name).Single(name=>name.EndsWith("/request.json",StringComparison.Ordinal));
            var resultName=readPins.Properties().Select(p=>p.Name).Single(name=>Path.GetFullPath(name)==Path.GetFullPath((string)selection["path"]));
            var legacy=(JObject)capture.DeepClone(); var legacyPins=(JObject)legacy["readPins"];
            foreach(var name in legacyPins.Properties().Select(p=>p.Name).ToArray())
                if(name!=requestName&&name!=resultName&&Path.GetFullPath(name).StartsWith(Path.GetFullPath((string)capture["capturedProjectRoot"])+Path.DirectorySeparatorChar+"T Logs"+Path.DirectorySeparatorChar,StringComparison.Ordinal)) legacyPins.Remove(name);
            if(legacyPins.Properties().Count(p=>Path.GetFullPath(p.Name).StartsWith(Path.GetFullPath((string)capture["capturedProjectRoot"])+Path.DirectorySeparatorChar+"T Logs"+Path.DirectorySeparatorChar,StringComparison.Ordinal))!=2)
                throw new InvalidDataException("Legacy two-control-pin fixture setup failed");
            LoadCapture(legacy,source,map,frame);
            var missing=(JObject)capture.DeepClone(); var missingPins=(JObject)missing["readPins"];
            string omitted=result["input_pins"].Children<JProperty>().Select(p=>p.Name).First(name=>name.Contains("/T Logs/")); missingPins.Remove(omitted);
            if(!RejectsCapture(missing,source,map,frame))throw new InvalidDataException("Missing compiler input closure pin must reject");
            var changed=(JObject)capture.DeepClone(); var changedPins=(JObject)changed["readPins"];
            string changedName=result["input_pins"].Children<JProperty>().Select(p=>p.Name).First(name=>name.Contains("/T Logs/")); changedPins[changedName]=new string('0',64);
            if(!RejectsCapture(changed,source,map,frame))throw new InvalidDataException("Changed compiler input closure hash must reject");
            var extra=(JObject)capture.DeepClone();
            ((JObject)extra["readPins"])[Path.Combine((string)capture["capturedProjectRoot"],"T Logs/SkirmishProduction/unlinked-control.json")]=new string('0',64);
            if(!RejectsCapture(extra,source,map,frame))throw new InvalidDataException("Unlinked extra T Logs pin must reject");
            var mask=domain.BuildWaterMask(true,width,height,cell,x,y,z); var data=mask.Select(v=>v?(byte)1:(byte)0).ToArray();
            if(data.Length!=(int)water["mask"]["bytes"]||Hash(data)!=(string)water["mask"]["sha256"]||
                domain.WaterFootprintCount!=(int)water["footprint_count"])
                throw new InvalidDataException("Original shared placement owner water mask differs from source descriptor");
            if(Hash(File.ReadAllBytes(args[0]))!=args[1]||Hash(File.ReadAllBytes(args[2]))!=args[3])throw new InvalidDataException("Control inputs changed");
            Console.WriteLine("[SourceDerivedPlacementControl] result=Passed waterFootprints="+domain.WaterFootprintCount+
                " waterBytes="+data.Length+" maskSha256="+Hash(data)+" wrongMapRejected=1 wrongFrameRejected=1 legacyTwoPinsAccepted=1 missingCompilerPinRejected=1 changedCompilerPinRejected=1 extraControlRejected=1 world=0 activeWater=0 ready=0");
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
