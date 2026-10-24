using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

// One captured initial production boundary, then computed state only. Later
// native snapshots are assertions, including the first missing-writer boundary.
internal static class ProductionChainReplay
{
    static string profilePath;
    internal static void Initialize(string path) { profilePath=Path.GetFullPath(path); }
    static void Need(bool value,string message) { WorldJson.Need(value,message); }
    static readonly (string Name,char Type)[] Storage={
        ("RuntimeBuildingId",'i'),("OwnerFactionId",'b'),("OilStorageCapacity",'i'),("FuelStorageCapacity",'i'),
        ("OilBarrelsPerDay",'f'),("FuelBarrelsPerDay",'f'),("StoredOilBarrels",'f'),("StoredFuelBarrels",'f'),
        ("ReservedOilInboundBarrels",'f'),("ReservedOilOutboundBarrels",'f'),("ReservedFuelInboundBarrels",'f'),
        ("ReservedFuelOutboundBarrels",'f'),("CivilianFuelReserveBarrels",'f'),("ReceivedHaulerFuelBarrels",'f'),("Version",'u')};
    static readonly (string Name,char Type)[] Profile={
        ("PeriodSeconds",'f'),("OilPerPeriod",'f'),("FuelPerPeriod",'f'),("OilPerFuelBatch",'f'),
        ("FuelPerBatch",'f'),("FuelBatchSeconds",'f'),("FuelBatchProgressSeconds",'f')};
    static string Hash(byte[] raw)=>Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant();
    static byte[] Read(string path,int maximum)
    {
        path=Path.GetFullPath(path);
        for(string current=path;current!=null;current=Path.GetDirectoryName(current))
            Need((File.GetAttributes(current)&FileAttributes.ReparsePoint)==0,"Production input ancestor symlink rejected: "+current);
        var info=new FileInfo(path);Need(info.Exists&&info.Length>0&&info.Length<=maximum&&
            (info.Attributes&(FileAttributes.Directory|FileAttributes.Device))==0,"Bounded regular production input required");
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        Need(stream.CanSeek&&stream.Length==info.Length,"Production input identity/length changed");
        byte[] bytes=new byte[checked((int)stream.Length)];stream.ReadExactly(bytes);
        Need(stream.ReadByte()==-1&&stream.Length==bytes.Length,"Production input changed during read");return bytes;
    }
    static void Component(BinaryWriter writer,JsonElement value,(string Name,char Type)[] fields)
    {
        Need(value.ValueKind==JsonValueKind.Object,"Complete production component required");
        WorldJson.Shape(value,fields.Select(f=>f.Name).ToArray());
        foreach(var field in fields)
        {
            var item=value.GetProperty(field.Name);
            switch(field.Type)
            {
                case 'i':writer.Write(item.GetInt32());break;
                case 'u':writer.Write(item.GetUInt32());break;
                case 'b':writer.Write(item.GetByte());break;
                default:float number=item.GetSingle();Need(float.IsFinite(number)&&Math.Abs(number)<=1e9f,"Finite bounded production field required");writer.Write(number);break;
            }
        }
    }
    static Dictionary<string,byte[]> Rows(JsonElement entry)
    {
        var result=new Dictionary<string,byte[]>(StringComparer.Ordinal);
        var rows=entry.GetProperty("readonly_query_snapshot");Need(rows.ValueKind==JsonValueKind.Array&&rows.GetArrayLength()>0&&rows.GetArrayLength()<=4096,"Complete bounded production census required");
        foreach(var row in rows.EnumerateArray())
        {
            string id=row.GetProperty("id").GetString();Need(!string.IsNullOrWhiteSpace(id),"Production row ID required");
            using var bytes=new MemoryStream();using(var writer=new BinaryWriter(bytes,System.Text.Encoding.UTF8,true))
            {Component(writer,row.GetProperty("storage"),Storage);var profile=row.GetProperty("profile");writer.Write((byte)(profile.ValueKind==JsonValueKind.Null?0:1));if(profile.ValueKind!=JsonValueKind.Null)Component(writer,profile,Profile);}
            Need(result.TryAdd(id,bytes.ToArray()),"Duplicate production row ID");
        }
        return result;
    }
    static void Match(Dictionary<string,byte[]> derived,Dictionary<string,byte[]> expected,string label)
    {
        Need(derived.Count==expected.Count&&derived.Keys.All(expected.ContainsKey),"Production census assertion differs: "+label);
        foreach(var row in derived)Need(row.Value.SequenceEqual(expected[row.Key]),"Production state assertion differs: "+label+" "+row.Key+"; intervening writer must be derived, native reseeding forbidden");
    }
    static readonly int StorageBytes=Storage.Sum(field=>field.Type=='b'?1:4);
    static int RowBytes(byte presence)=>StorageBytes+1+(presence==1?28:0);
    static void VerifyConfig(JsonElement config)
    {
        Need(config.GetProperty("protocol_version").GetInt32()==2,"Continuous production protocol2 required");
        Need(config.GetProperty("no_dotnet_fallback").GetBoolean()&&!config.GetProperty("aggregate_totals_supported").GetBoolean(),"Pinned Mono production with omitted unproven totals required");
        var options=config.GetProperty("mono_options");Need(options.GetArrayLength()==1&&options[0].GetString()=="--optimize=-float32","Exact Mono numeric lane required");
        foreach(var pin in config.GetProperty("pins").EnumerateObject())Need(Hash(Read(pin.Name,67108864))==pin.Value.GetString(),"Production runtime/source pin differs: "+pin.Name);
        foreach(string executable in new[]{"mono_path","worker_path"})Need(config.GetProperty("pins").TryGetProperty(config.GetProperty(executable).GetString(),out _),"Pinned production executable required");
    }
    internal static object Run(JsonElement row)
    {
        WorldJson.Shape(row,new[]{"id","kind","trace_path","trace_sha256","baseline_path","baseline_sha256","pairs","work_limit"});
        byte[] trace=Read(row.GetProperty("trace_path").GetString(),33554432);Need(Hash(trace)==row.GetProperty("trace_sha256").GetString(),"Production trace hash differs");
        using var document=JsonDocument.Parse(trace);var root=document.RootElement;WorldJson.UniqueKeys(root);
        Need(root.GetProperty("capture_complete").GetBoolean()&&root.GetProperty("capture_failure").ValueKind==JsonValueKind.Null,"Complete passive production capture required");
        var history=root.GetProperty("production_boundary_history");int pairs=row.GetProperty("pairs").GetInt32();
        Need(history.ValueKind==JsonValueKind.Array&&history.GetArrayLength()>0&&history.GetArrayLength()<=512&&history.GetArrayLength()%2==0&&pairs>0&&pairs<=history.GetArrayLength()/2,"Bounded paired actual production invocations required");
        var initial=Rows(history[0]);string[] ids=initial.Keys.ToArray();long work=row.GetProperty("work_limit").GetInt64();Need(work>0&&work<=2000000&&(long)pairs*ids.Length<=work,"Production work budget exceeded");
        string configPath=Path.Combine(Path.GetDirectoryName(profilePath),"production-worker.json");byte[] configBytes=Read(configPath,262144);
        using var configDocument=JsonDocument.Parse(configBytes);var config=configDocument.RootElement;WorldJson.UniqueKeys(config);VerifyConfig(config);
        byte[] baselineBytes=Read(row.GetProperty("baseline_path").GetString(),16777216);Need(Hash(baselineBytes)==row.GetProperty("baseline_sha256").GetString(),"Production native baseline hash differs");
        using var baseline=JsonDocument.Parse(baselineBytes);WorldJson.UniqueKeys(baseline.RootElement);var baselinePins=baseline.RootElement.GetProperty("pins");
        Need(config.GetProperty("native_source_pins").EnumerateObject().Count()==7,"Complete production writer/caller source closure required");
        foreach(var pin in config.GetProperty("native_source_pins").EnumerateObject())
        {
            var aliases=baselinePins.EnumerateObject().Where(p=>p.Name.Replace('\\','/')==pin.Name||p.Name.Replace('\\','/').EndsWith("/"+pin.Name,StringComparison.Ordinal)).ToArray();
            Need(aliases.Length>0&&aliases.All(p=>p.Value.GetString()==pin.Value.GetString()),"Production capture/source baseline differs: "+pin.Name);
        }
        using var request=new MemoryStream();using(var writer=new BinaryWriter(request,System.Text.Encoding.UTF8,true))
        {
            writer.Write(0x50524431);writer.Write(2);writer.Write(pairs);
            for(int tick=0;tick<pairs;tick++)
            {
                var before=history[tick*2];var after=history[tick*2+1];
                Need(before.GetProperty("stamp").GetProperty("boundary").GetString()=="before-production"&&after.GetProperty("stamp").GetProperty("boundary").GetString()=="after-production"&&!before.GetProperty("actual_loop_order_proven").GetBoolean()&&!after.GetProperty("actual_loop_order_proven").GetBoolean(),"Actual unordered production pair required");
                var stamp=before.GetProperty("stamp");var afterStamp=after.GetProperty("stamp");
                Need(stamp.GetProperty("session_id").GetString()==root.GetProperty("before_fuel").GetProperty("session_id").GetString()&&stamp.GetProperty("session_phase").GetString()=="Playing","Same playing native production session required");
                foreach(var fact in stamp.EnumerateObject().Where(p=>p.Name!="boundary"))WorldJson.Expect(fact.Value,afterStamp.GetProperty(fact.Name),"production invocation stamp "+fact.Name);
                var assertion=Rows(before);Need(assertion.Count==ids.Length&&ids.All(assertion.ContainsKey),"Production query membership changed; unsupported creation/deletion");
                writer.Write(tick);writer.Write(ids.Length);
                foreach(string scalar in new[]{"seconds_per_day_bits","delta_time_bits","oil_ratio_bits"})
                {int bits=before.GetProperty(scalar).GetInt32();Need(bits==after.GetProperty(scalar).GetInt32(),"Production invocation scalar assertions differ");writer.Write(bits);}
                // Protocol2 retains worker chunks. No later native before/after
                // storage or profile bytes enter the worker request.
                if(tick==0)for(int i=0;i<ids.Length;i++){writer.Write(i);writer.Write(initial[ids[i]]);}
            }
        }
        string directory=Path.Combine(Path.GetDirectoryName(profilePath),"production-evidence-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        string input=Path.Combine(directory,"input.bin"),output=Path.Combine(directory,"output.bin");byte[] requestBytes=request.ToArray();File.WriteAllBytes(input,requestBytes);
        var start=new ProcessStartInfo(config.GetProperty("mono_path").GetString()){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(string argument in new[]{"--optimize=-float32",config.GetProperty("worker_path").GetString(),config.GetProperty("engine_path").GetString(),input,output,Hash(requestBytes)})start.ArgumentList.Add(argument);
        using(var process=Process.Start(start))
        {
            var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
            Need(process.WaitForExit(120000),"Production worker observation timeout; process ownership retained, no takeover");
            File.WriteAllText(Path.Combine(directory,"stdout.log"),stdout.GetAwaiter().GetResult());File.WriteAllText(Path.Combine(directory,"stderr.log"),stderr.GetAwaiter().GetResult());Need(process.ExitCode==0,"Production worker failed; preserved logs: "+directory);
        }
        VerifyConfig(config);Need(Read(configPath,262144).SequenceEqual(configBytes)&&Read(input,67108864).SequenceEqual(requestBytes),"Production config/request changed");
        byte[] computed=Read(output,67108864);using var reader=new BinaryReader(new MemoryStream(computed));Need(reader.ReadInt32()==0x50524432&&reader.ReadInt32()==2&&reader.ReadInt32()==pairs,"Production continuous worker protocol differs");
        var state=initial;int profiles=0;
        for(int tick=0;tick<pairs;tick++)
        {
            Match(state,Rows(history[tick*2]),"before tick "+tick);
            Need(reader.ReadInt32()==tick&&reader.ReadInt32()==ids.Length,"Production output tick/census differs");var next=new Dictionary<string,byte[]>(StringComparer.Ordinal);
            for(int i=0;i<ids.Length;i++)
            {
                Need(reader.ReadInt32()==i,"Production output row ordinal differs");byte[] head=reader.ReadBytes(StorageBytes+1);Need(head.Length==StorageBytes+1&&head[StorageBytes]<=1,"Production output truncated/presence invalid");
                byte[] bytes=head;if(head[StorageBytes]==1){byte[] tail=reader.ReadBytes(28);Need(tail.Length==28,"Production profile truncated");bytes=head.Concat(tail).ToArray();profiles++;}Need(bytes.Length==RowBytes(head[StorageBytes]),"Production output shape differs");next.Add(ids[i],bytes);
            }
            Match(next,Rows(history[tick*2+1]),"after tick "+tick);state=next;
        }
        Need(reader.BaseStream.Position==reader.BaseStream.Length,"Trailing production output rejected");
        bool nextBeforeMatches=false;
        if(pairs<history.GetArrayLength()/2)
        {var next=Rows(history[pairs*2]);nextBeforeMatches=next.Count==state.Count&&state.All(p=>next.TryGetValue(p.Key,out var value)&&p.Value.SequenceEqual(value));}
        return new{result="ContinuousProductionPrefixMatched",pairs,storage_transitions=pairs*ids.Length,profile_transitions=profiles,
            initial_state_boundary="captured-before-first-production",later_native_states_are_assertions_only=true,worker_state_reseed_count=0,
            request_sha256=Hash(requestBytes),derived_state_sha256=Hash(computed),worker_evidence=directory,
            next_native_before_matches_computed_state=nextBeforeMatches,remaining_pairs=history.GetArrayLength()/2-pairs,
            aggregate_totals_omitted=true,native_query_order_proven=false,full_startup_derived=false,complete_frame_derived=false,mission_qualified=false};
    }
}
