using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

static class WorldProgram
{
    static object Run(JsonElement row)
    {
        WorldJson.Need(!string.IsNullOrWhiteSpace(row.GetProperty("id").GetString()), "Case ID required");
        string kind = row.GetProperty("kind").GetString();
#if WORLD_BODY_TRANSITIONS
        if (kind == "body_transition") return CapturedBodyTransition.Run(row);
        if (kind == "vehicle_caller") return VehicleCallerReplay.Run(row);
        if (kind == "physical_production_chain") return ProductionChainReplay.Run(row);
#endif
        if (kind == "startup_import") return StartupFullMapImport.Run(row);
        if (kind == "squad_target") return SquadReplay.Run(row);
        if (kind == "world") return WorldFrameReplay.Run(row);
        if (kind == "moving_world") return MovingWorldReplay.Run(row);
        if (kind == "multi_unit_world") return MovingWorldReplay.Run(row,true);
        throw new ArgumentException("Unsupported world case kind: " + kind);
    }
    static byte[] ReadBoundedInput(string path)
    {
        var file=new System.IO.FileInfo(path);
        WorldJson.Need(file.Exists&&file.Length>0&&file.Length<=16777216&&(file.Attributes&(System.IO.FileAttributes.ReparsePoint|System.IO.FileAttributes.Directory|System.IO.FileAttributes.Device))==0,"Bounded regular world input required");
        using var stream=new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.Read);
        WorldJson.Need(stream.Length==file.Length&&stream.Length<=16777216,"World input length changed");
        var raw=new byte[checked((int)stream.Length)];stream.ReadExactly(raw);
        WorldJson.Need(stream.ReadByte()==-1,"World input grew during read");return raw;
    }

    public static int Main(string[] args)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            WorldJson.Need(args.Length == 2, "Expected candidate JSON path");

#if WORLD_BODY_TRANSITIONS
            RuntimeMathBootstrap.Initialize(args[1]);
            ProductionChainReplay.Initialize(args[1]);
            BodyTransitionSourcePins.Verify(RuntimeMathBootstrap.ProjectRoot);
#endif
            WorldSchedule.Load(args[1]);
            using var document = JsonDocument.Parse(ReadBoundedInput(args[0]));
            var root = document.RootElement;
            WorldJson.UniqueKeys(root);
            WorldJson.Shape(root, new[] { "schema_version", "cases" });
            WorldJson.Need(root.GetProperty("schema_version").GetInt32() == 1, "Unsupported schema");
            var cases = root.GetProperty("cases");
            WorldJson.Need(cases.ValueKind == JsonValueKind.Array && cases.GetArrayLength() >= 1 && cases.GetArrayLength() <= 256, "1..256 cases required");
            long work=0;
            foreach(var row in cases.EnumerateArray())
                if(row.ValueKind==JsonValueKind.Object&&row.TryGetProperty("kind",out var kind)&&kind.GetString()=="world"&&row.TryGetProperty("actors",out var actors)&&actors.ValueKind==JsonValueKind.Array&&row.TryGetProperty("frames",out var frames)&&frames.ValueKind==JsonValueKind.Array)
                    work+=(long)actors.GetArrayLength()*actors.GetArrayLength()*frames.GetArrayLength();
            foreach(var row in cases.EnumerateArray())
                if(row.ValueKind==JsonValueKind.Object&&row.TryGetProperty("kind",out var movingKind)&&(movingKind.GetString()=="moving_world"||movingKind.GetString()=="multi_unit_world")&&row.TryGetProperty("units",out var movingUnits)&&movingUnits.ValueKind==JsonValueKind.Array&&row.TryGetProperty("frames",out var movingFrames)&&movingFrames.ValueKind==JsonValueKind.Array)
                    work+=(long)movingUnits.GetArrayLength()*movingUnits.GetArrayLength()*movingFrames.GetArrayLength();
            foreach(var row in cases.EnumerateArray())
                if(row.ValueKind==JsonValueKind.Object&&row.TryGetProperty("kind",out var bodyKind)&&bodyKind.GetString()=="body_transition"&&row.TryGetProperty("roster",out var bodyRows)&&bodyRows.ValueKind==JsonValueKind.Array)
                {
                    int storageRows=row.TryGetProperty("stores",out var stores)&&stores.ValueKind==JsonValueKind.Array?stores.GetArrayLength():0;
                    work+=(long)bodyRows.GetArrayLength()*Math.Max(1,storageRows);
                }
#if WORLD_BODY_TRANSITIONS
            foreach(var row in cases.EnumerateArray())
                if(row.ValueKind==JsonValueKind.Object&&row.TryGetProperty("kind",out var callerKind)&&(callerKind.GetString()=="vehicle_caller"||callerKind.GetString()=="physical_production_chain"))
                {
                    long callerWork=row.GetProperty("work_limit").GetInt64();WorldJson.Need(callerWork>0&&callerWork<=2000000,"Bounded caller work admission required");work+=callerWork;
                }
#endif
            WorldJson.Need(work<=2000000,"Aggregate world work budget exceeded");
            var ids = new HashSet<string>();
            var results = new List<object>();
            int failed = 0;
            foreach (var row in cases.EnumerateArray())
            {
                string id = row.ValueKind == JsonValueKind.Object && row.TryGetProperty("id", out var identifier) ? identifier.ToString() : "<invalid>";
                try
                {
                    WorldJson.Need(ids.Add(id), "Duplicate case ID");
                    results.Add(new { id, status = "passed", plan = Run(row) });
                }
                catch (Exception error) { failed++; results.Add(new { id, status = "failed", error = error.Message }); }
            }
            Console.WriteLine(JsonSerializer.Serialize(new { schema_version = 1, scope = "ProductionBoundedWorldReplay",
                result = failed == 0 ? "passed" : "failed", passed = results.Count - failed, failed,
                elapsed_seconds = watch.Elapsed.TotalSeconds, native_runs = 0, full_battle_simulated = false,
                mission_qualified = false, cases = results }, WorldJson.Json));
            return failed == 0 ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { result = "failed", error = error.Message,
                native_runs = 0, full_battle_simulated = false, mission_qualified = false }, WorldJson.Json));
            return 1;
        }
    }
}
