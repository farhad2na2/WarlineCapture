using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Spawning;

// Original compact decoder and overlay sampler are linked unchanged.
sealed class StartupGrounding
{
    MapSurfaceBlob blob;
    long workPerHeight;
    int width,height,overlayCount,triangleCount,instanceCount;
    float minimum,step;
    readonly List<(string Path,long Bytes,string Hash)> artifactPins=new();
    readonly Dictionary<(int X,int Z,int WorldX,int WorldZ),int> prepared=new();
    bool hasPrepared;
    MapSurfaceSceneOverlay[] overlays;
    NativeArray<MapSurfaceMeshTriangle> triangles;
    NativeArray<MapSurfaceMeshInstance> instances;
    static void Need(bool ok,string message) { if(!ok)throw new ArgumentException(message); }
    static float Finite(float x) { Need(float.IsFinite(x),"Nonfinite grounding fact");return x; }
    // Binary geometry retains raw IEEE values so native NaN/ordered-skip behavior survives.
    static float3 V3(BinaryReader r)=>new float3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    static float2 V2(BinaryReader r)=>new float2(r.ReadSingle(),r.ReadSingle());
    static float4 V4(BinaryReader r)=>new float4(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    internal static StartupGrounding Read(JsonElement root,string artifactRoot)
    {
        var meta=root.GetProperty("surface_runtime");
        SpawnJson.Shape(meta,new[]{"runtime_encoding","width","height","cell_size","origin","compact_min_height","compact_height_step","compact_count","connection_count","overlay_count","triangle_count","instance_count","compact_artifact","overlays_artifact","triangles_artifact","instances_artifact"},"overlays_buffer_present","triangles_buffer_present","instances_buffer_present","same_blob_as_all_allocator_transactions","ground_offset");
        int width=meta.GetProperty("width").GetInt32(),height=meta.GetProperty("height").GetInt32();long cells=(long)width*height;
        Need(meta.GetProperty("runtime_encoding").GetInt32()==1&&width>0&&height>0&&cells<=16777216&&meta.GetProperty("compact_count").GetInt64()==cells&&meta.GetProperty("connection_count").GetInt32()==0,"Complete compact single-layer unconnected surface required");
        float size=Finite(meta.GetProperty("cell_size").GetSingle()),minimum=Finite(meta.GetProperty("compact_min_height").GetSingle()),step=Finite(meta.GetProperty("compact_height_step").GetSingle());
        Need(size>0&&step>0&&float.IsFinite(minimum+ushort.MaxValue*step),"Invalid compact grounding scale");
        var o=meta.GetProperty("origin");Need(o.ValueKind==JsonValueKind.Array&&o.GetArrayLength()==3,"Exact grounding origin required");
        var origin=new float3(Finite(o[0].GetSingle()),Finite(o[1].GetSingle()),Finite(o[2].GetSingle()));
        var grid=root.GetProperty("grid");var go=grid.GetProperty("Origin");
        Need(grid.GetProperty("Width").GetInt32()==width&&grid.GetProperty("Height").GetInt32()==height&&grid.GetProperty("CellSize").GetSingle()==size&&go.GetProperty("x").GetSingle()==origin.x&&go.GetProperty("y").GetSingle()==origin.y&&go.GetProperty("z").GetSingle()==origin.z,"Grounding/grid frame differs");
        var table=root.GetProperty("artifacts");long allocated=0;
        var capturedPins=new List<(string Path,long Bytes,string Hash)>();
        byte[] Artifact(string field,string encoding,int count,int stride,bool header)
        {
            string name=meta.GetProperty(field).GetString();
            Need(!string.IsNullOrWhiteSpace(name)&&Path.GetFileName(name)==name&&name!="."&&name!="..","Grounding artifact escape");
            var entry=table.GetProperty(name);SpawnJson.Shape(entry,new[]{"bytes","encoding","sha256"});
            long expected=(long)count*stride+(header?4:0);allocated+=expected;
            Need(allocated<=512L*1024*1024&&entry.GetProperty("bytes").GetInt64()==expected&&entry.GetProperty("encoding").GetString()==encoding,"Grounding artifact shape/encoding differs");
            string path=Path.Combine(Path.GetFullPath(artifactRoot),name);
            for(var item=new FileInfo(path) as FileSystemInfo;item!=null;item=item is FileInfo f?f.Directory:((DirectoryInfo)item).Parent)
                Need((item.Attributes&FileAttributes.ReparsePoint)==0,"Grounding artifact symlink rejected");
            Need(new FileInfo(path).Length==expected,"Grounding artifact length differs");var bytes=File.ReadAllBytes(path);
            Need(bytes.LongLength==expected&&Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()==entry.GetProperty("sha256").GetString(),"Grounding artifact bytes differ");
            if(header)Need(System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes)==count,"Grounding artifact count header differs");
            capturedPins.Add((path,expected,entry.GetProperty("sha256").GetString()));return bytes;
        }
        int Count(string name,int max) { int value=meta.GetProperty(name).GetInt32();Need(value>=0&&value<=max,"Grounding geometry budget exceeded");return value; }
        int oc=Count("overlay_count",1048576),tc=Count("triangle_count",4194304),ic=Count("instance_count",1048576);
        void Presence(string field,int count)
        {
            if(meta.TryGetProperty(field,out var present))
                Need(present.GetBoolean() || count == 0,"Grounding buffer/count presence differs");
        }
        Presence("overlays_buffer_present",oc);Presence("triangles_buffer_present",tc);Presence("instances_buffer_present",ic);
        if(meta.TryGetProperty("same_blob_as_all_allocator_transactions",out var sameBlob))
            Need(sameBlob.GetBoolean(),"Grounding surface changed during allocator transactions");
        if(meta.TryGetProperty("ground_offset",out var offset))
            Need(Finite(offset.GetSingle()) == 0f,"Unsupported grounding offset");
        var samples=new MapSurfaceCompactSample[(int)cells];
        using(var r=new BinaryReader(new MemoryStream(Artifact("compact_artifact","compact12-le",(int)cells,12,false))))
            for(int i=0;i<samples.Length;i++)samples[i]=new MapSurfaceCompactSample{PackedHeight=r.ReadUInt16(),NormalX=r.ReadSByte(),NormalY=r.ReadSByte(),NormalZ=r.ReadSByte(),LayerId=r.ReadInt16(),SurfaceType=(MapSurfaceType)r.ReadByte(),MovementMask=(MapSurfaceMovementMask)r.ReadUInt16(),Flags=(MapSurfaceFlags)r.ReadUInt16()};
        var result=new StartupGrounding{blob=new MapSurfaceBlob{Dimensions=new int2(width,height),CellSize=size,GridOrigin=origin,RuntimeEncoding=MapSurfaceRuntimeEncoding.SingleLayerCompact,CompactMinHeight=minimum,CompactHeightStep=step,CompactSamples=new BlobArray<MapSurfaceCompactSample>(samples)},overlays=new MapSurfaceSceneOverlay[oc]};
        using(var r=new BinaryReader(new MemoryStream(Artifact("overlays_artifact","overlay77-le",oc,77,true))))
        {
            r.ReadInt32();for(int i=0;i<oc;i++)
            {
                var v=new MapSurfaceSceneOverlay{Center=V3(r),YawRadians=r.ReadSingle(),HalfExtents=V2(r),Height=r.ReadSingle(),Normal=V3(r),TriangleA=V2(r),TriangleB=V2(r),TriangleC=V2(r),GeometryInstanceIndex=r.ReadInt32(),SurfaceType=(MapSurfaceType)r.ReadByte(),MovementMask=(MapSurfaceMovementMask)r.ReadUInt16(),Flags=(MapSurfaceFlags)r.ReadUInt16(),LayerId=r.ReadInt32()};
                Need((v.Flags&MapSurfaceFlags.ExactMesh)==0||(uint)v.GeometryInstanceIndex<(uint)ic,"Missing grounding mesh instance");result.overlays[i]=v;
            }
        }
        var ts=new MapSurfaceMeshTriangle[tc];
        using(var r=new BinaryReader(new MemoryStream(Artifact("triangles_artifact","triangle36-le",tc,36,true))))
        {r.ReadInt32();for(int i=0;i<tc;i++)ts[i]=new MapSurfaceMeshTriangle{A=V3(r),B=V3(r),C=V3(r)};}
        var ins=new MapSurfaceMeshInstance[ic];
        using(var r=new BinaryReader(new MemoryStream(Artifact("instances_artifact","instance72-le",ic,72,true))))
        {
            r.ReadInt32();for(int i=0;i<ic;i++)
            {
                var v=new MapSurfaceMeshInstance{WorldToMesh=new float4x4(V4(r),V4(r),V4(r),V4(r)),FirstTriangle=r.ReadInt32(),TriangleCount=r.ReadInt32()};
                Need(v.FirstTriangle>=0&&v.TriangleCount>=0&&(long)v.FirstTriangle+v.TriangleCount<=tc,"Incomplete grounding triangle range");ins[i]=v;
            }
        }
        result.triangles=new NativeArray<MapSurfaceMeshTriangle>(ts);result.instances=new NativeArray<MapSurfaceMeshInstance>(ins);
        Need(MapSurfaceBlobAccess.IsCompactSingleLayer(ref result.blob),"Unsupported grounding blob");
        result.workPerHeight = 9L + oc;
        foreach(var overlay in result.overlays)
            if(SpawnGroundingKernel.CanSampleOverlay((ushort)overlay.MovementMask) && (overlay.Flags & MapSurfaceFlags.ExactMesh) != 0)
                result.workPerHeight += ins[overlay.GeometryInstanceIndex].TriangleCount;
        Need(result.workPerHeight <= 512L * 1024 * 1024,"Grounding inspection budget exceeded");
        result.width=width;result.height=height;result.minimum=minimum;result.step=step;
        result.overlayCount=oc;result.triangleCount=tc;result.instanceCount=ic;
        result.artifactPins.AddRange(capturedPins);
        return result;
    }
    // All placement cells must already have been derived by the caller. This
    // invokes one process for the entire actor batch; native transforms are absent.
    internal void Prepare(IReadOnlyList<StartupGroundingQuery> queries)
    {
        Need(!hasPrepared,"Grounding batch already prepared");
        Need(queries!=null&&queries.Count>=1&&queries.Count<=512,"1..512 derived grounding queries required");
        Need(workPerHeight*queries.Count<=512L*1024*1024,"Grounding inspection budget exceeded");
        string configPath=Environment.GetEnvironmentVariable("WARLINE_STARTUP_GROUNDING_WORKER_CONFIG");
        string configHash=Environment.GetEnvironmentVariable("WARLINE_STARTUP_GROUNDING_WORKER_CONFIG_SHA256");
        Need(!string.IsNullOrWhiteSpace(configPath)&&!string.IsNullOrWhiteSpace(configHash),"Pinned standalone Mono grounding worker configuration required; rebuild with worker support");
        string HashFile(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        Need(new FileInfo(configPath).Length<=1024*1024&&HashFile(configPath)==configHash,"Grounding worker configuration bytes differ");
        using var configDoc=JsonDocument.Parse(File.ReadAllBytes(configPath));var config=configDoc.RootElement;
        Need(config.GetProperty("schema_version").GetInt32()==1&&config.GetProperty("scope").GetString()=="OriginalCompactAndGeometry_MonoWidenedFloat_BatchV1","Grounding worker configuration unsupported");
        void VerifyPins()
        {
            Need(HashFile(configPath)==configHash,"Grounding worker configuration drift");
            foreach(var pin in config.GetProperty("pins").EnumerateObject())
                Need(Path.IsPathRooted(pin.Name)&&File.Exists(pin.Name)&&HashFile(pin.Name)==pin.Value.GetString(),"Grounding worker source/runtime bytes differ: "+pin.Name);
        }
        string mono=config.GetProperty("mono_path").GetString(),worker=config.GetProperty("worker_path").GetString(),math=config.GetProperty("math_path").GetString();
        Need(File.Exists(mono),"Editor-distribution standalone Mono is missing; rebuild against the matching installed Editor runtime (no fallback)");
        VerifyPins();
        var pins=config.GetProperty("pins");
        Need(pins.TryGetProperty(mono,out _)&&pins.TryGetProperty(worker,out _),"Grounding worker executable/runtime pins required");
        Need(pins.TryGetProperty(math,out var mathPin),"Grounding worker mathematics pin required");
        string directory=Path.Combine(config.GetProperty("evidence_root").GetString(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        string input=Path.Combine(directory,"input.bin"),output=Path.Combine(directory,"output.bin");
        using(var writer=new BinaryWriter(new FileStream(input,FileMode.CreateNew,FileAccess.Write)))
        {
            writer.Write(0x53475231u);writer.Write(1);writer.Write(width);writer.Write(height);writer.Write(minimum);writer.Write(step);
            writer.Write(overlayCount);writer.Write(triangleCount);writer.Write(instanceCount);writer.Write(queries.Count);
            Need(artifactPins.Count==4,"Complete grounding geometry descriptors required");
            foreach(var artifact in artifactPins){writer.Write(artifact.Path);writer.Write(artifact.Bytes);writer.Write(artifact.Hash);}
            for(int i=0;i<queries.Count;i++)
            {
                var q=queries[i];Need((uint)q.X<(uint)width&&(uint)q.Z<(uint)height&&float.IsFinite(q.WorldX)&&float.IsFinite(q.WorldZ),"Invalid derived grounding query");
                writer.Write(i);writer.Write(q.X);writer.Write(q.Z);writer.Write(q.WorldX);writer.Write(q.WorldZ);
            }
        }
        var start=new ProcessStartInfo(mono){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(string arg in new[]{"--optimize=-float32",worker,input,output,math,mathPin.GetString()})start.ArgumentList.Add(arg);
        // Inherited Mono options must not alter this deliberately pinned arithmetic mode.
        start.Environment.Remove("MONO_ENV_OPTIONS");start.Environment.Remove("MONO_PATH");
        using(var process=Process.Start(start))
        {
            var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
            if(!process.WaitForExit(120000))
            {
                process.Kill(entireProcessTree:true);process.WaitForExit();
                File.WriteAllText(Path.Combine(directory,"worker.log"),stdout.GetAwaiter().GetResult()+stderr.GetAwaiter().GetResult());
                throw new ArgumentException("Grounding worker timed out; evidence "+directory);
            }
            string log=stdout.GetAwaiter().GetResult()+stderr.GetAwaiter().GetResult();File.WriteAllText(Path.Combine(directory,"worker.log"),log);
            Need(process.ExitCode==0&&log.Contains("[StartupGroundingWorker] result=Passed")&&!log.Contains("result=Failed"),"Grounding worker failed; evidence "+directory);
        }
        VerifyPins();
        Need(new FileInfo(output).Length==12L+queries.Count*8L,"Grounding worker output length differs");
        using(var reader=new BinaryReader(File.OpenRead(output)))
        {
            Need(reader.ReadUInt32()==0x53475231u&&reader.ReadInt32()==1&&reader.ReadInt32()==queries.Count,"Grounding worker output protocol differs");
            for(int i=0;i<queries.Count;i++)
            {
                Need(reader.ReadInt32()==i,"Grounding worker output ordinal differs");int bits=reader.ReadInt32();Need(float.IsFinite(BitConverter.Int32BitsToSingle(bits)),"Grounding worker height is nonfinite");var q=queries[i];
                var key=(q.X,q.Z,BitConverter.SingleToInt32Bits(q.WorldX),BitConverter.SingleToInt32Bits(q.WorldZ));
                if(prepared.TryGetValue(key,out int prior))Need(prior==bits,"Duplicate grounding query output differs");else prepared.Add(key,bits);
            }
            Need(reader.BaseStream.Position==reader.BaseStream.Length,"Grounding worker trailing output rejected");
        }
        hasPrepared=true;
        File.WriteAllText(Path.Combine(directory,"result.json"),JsonSerializer.Serialize(new{result="Passed",queries=queries.Count,backend="EditorDistributionStandaloneMono_WidenedFloat32Disabled",config_sha256=configHash,input_sha256=HashFile(input),output_sha256=HashFile(output),native_runs=0,full_mission_initialized=false,player_ready=false}));
    }
    internal float HeightAt(int x,int z,float wx,float wz)
    {
        Need(hasPrepared,"Grounding batch must be prepared from derived cells before reading heights");
        Need(prepared.TryGetValue((x,z,BitConverter.SingleToInt32Bits(wx),BitConverter.SingleToInt32Bits(wz)),out int bits),"Grounding query not in derived batch");
        return BitConverter.Int32BitsToSingle(bits);
    }
}
internal readonly struct StartupGroundingQuery
{
    internal readonly int X,Z;
    internal readonly float WorldX,WorldZ;
    internal StartupGroundingQuery(int x,int z,float worldX,float worldZ){X=x;Z=z;WorldX=worldX;WorldZ=worldZ;}
}
