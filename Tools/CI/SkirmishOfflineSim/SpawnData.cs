using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Game.Components;
using Game.Runtime.Spawning;

sealed class SpawnArtifacts
{
    readonly Dictionary<string,(string Path,string Encoding,int Bytes,string Hash)> values=new();
    internal SpawnArtifacts(JsonElement input,string inputPath)
    {
        string root=Path.GetDirectoryName(Path.GetFullPath(inputPath));long total=0;
        foreach(var a in SpawnJson.Array(input,4096))
        {
            SpawnJson.Shape(a,new[]{"id","path","sha256","bytes","encoding"});string id=SpawnJson.Id(a.GetProperty("id")),relative=a.GetProperty("path").GetString();
            SpawnJson.Need(!Path.IsPathRooted(relative)&&!relative.Split('/','\\').Any(p=>p==".."||p=="."||p==""),"Captured artifact path must be relative");
            string path=Path.GetFullPath(Path.Combine(root,relative));SpawnJson.Need(path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal),"Artifact escape");
            int bytes=SpawnJson.Int(a.GetProperty("bytes"),0,256*1024*1024);total+=bytes;SpawnJson.Need(total<=512L*1024*1024,"Aggregate artifact allocation exceeds512MiB");
            string hash=a.GetProperty("sha256").GetString(),encoding=a.GetProperty("encoding").GetString();SpawnJson.Need(hash.Length==64&&hash.All(c=>"0123456789abcdef".Contains(c)),"SHA256 required");
            SpawnJson.Need(values.TryAdd(id,(path,encoding,bytes,hash)),"Duplicate artifact ID");
        }
    }
    byte[] Read(JsonElement value,string encoding,int count,int stride)
    {
        SpawnJson.Shape(value,new[]{"artifact"});string id=SpawnJson.Id(value.GetProperty("artifact"));SpawnJson.Need(values.TryGetValue(id,out var a),"Unknown artifact ID");
        SpawnJson.Need(a.Encoding==encoding&&a.Bytes==checked(count*stride),"Artifact shape/encoding differs");byte[] bytes=File.ReadAllBytes(a.Path);
        SpawnJson.Need(bytes.Length==a.Bytes&&Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()==a.Hash,"Artifact bytes differ");return bytes;
    }
    internal byte[] Bytes(JsonElement value,int count,bool binary)
    {
        byte[] result;
        if(value.ValueKind==JsonValueKind.Array){SpawnJson.Need(count<=65536&&value.GetArrayLength()==count,"Complete small inline array required");result=value.EnumerateArray().Select(v=>v.GetByte()).ToArray();}
        else result=Read(value,"u8",count,1);
        if(binary)SpawnJson.Need(result.All(v=>v<=1),"Binary mask values required");return result;
    }
    internal int[] Counts(JsonElement value,int count)
    {
        if(value.ValueKind==JsonValueKind.Array){SpawnJson.Need(count<=65536&&value.GetArrayLength()==count,"Complete small inline counts required");return value.EnumerateArray().Select(v=>v.GetInt32()).ToArray();}
        var bytes=Read(value,"i32-le",count,4);var result=new int[count];for(int i=0;i<count;i++)result[i]=System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(i*4,4));return result;
    }
    internal static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    internal static object Summary(byte[] bytes)=>new{length=bytes.Length,set_count=bytes.Count(v=>v!=0),sha256=Hash(bytes)};
}

struct SpawnStorage:ISpawnCellStorage,IStaticBlockerStorage
{
    internal byte[] Walkable,Blocked,Occupied,Reserved,Friendly;
    internal int[] Counts;
    internal bool HasFriendly;
    public bool IsWalkable(int i)=>Walkable[i]!=0;
    public bool IsBlocked(int i)=>Blocked[i]!=0;
    public bool IsOccupied(int i)=>Occupied[i]!=0;
    public bool IsReserved(int i)=>Reserved[i]!=0;
    public void SetReserved(int i,bool value)=>Reserved[i]=(byte)(value?1:0);
    public int CountLength=>Counts.Length;
    public int BlockedLength=>Blocked.Length;
    public int FriendlyPassLength=>Friendly.Length;
    public bool FriendlyPassCreated=>HasFriendly;
    public int ReadCount(int i)=>Counts[i];
    public void WriteCount(int i,int value)=>Counts[i]=value;
    public void WriteBlocked(int i,bool value)=>Blocked[i]=(byte)(value?1:0);
    public void ClearBlocked()=>Array.Clear(Blocked);
    public void WriteFriendlyPass(int i,byte value)=>Friendly[i]=value;
    internal static SpawnStorage Read(JsonElement value,SpawnArtifacts artifacts,int count)
    {
        SpawnJson.Shape(value,new[]{"walkable","blocked","occupied","reserved","counts","friendly_pass","friendly_pass_created"});
        return new SpawnStorage{Walkable=artifacts.Bytes(value.GetProperty("walkable"),count,true),Blocked=artifacts.Bytes(value.GetProperty("blocked"),count,true),Occupied=artifacts.Bytes(value.GetProperty("occupied"),count,true),Reserved=artifacts.Bytes(value.GetProperty("reserved"),count,true),Counts=artifacts.Counts(value.GetProperty("counts"),count),Friendly=artifacts.Bytes(value.GetProperty("friendly_pass"),value.GetProperty("friendly_pass_created").GetBoolean()?count:0,false),HasFriendly=value.GetProperty("friendly_pass_created").GetBoolean()};
    }
    internal object Snapshot(bool arrays)
    {
        var countBytes=new byte[Counts.Length*4];for(int i=0;i<Counts.Length;i++)System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(countBytes.AsSpan(i*4,4),Counts[i]);
        return new{counts_sha256=SpawnArtifacts.Hash(countBytes),blocked=SpawnArtifacts.Summary(Blocked),reserved=SpawnArtifacts.Summary(Reserved),friendly_pass_sha256=SpawnArtifacts.Hash(Friendly),counts=arrays?Counts:null,blocked_values=arrays?Blocked.Select(v=>(int)v).ToArray():null,reserved_values=arrays?Reserved.Select(v=>(int)v).ToArray():null,friendly_pass_values=arrays?Friendly.Select(v=>(int)v).ToArray():null};
    }
}

struct SpawnSurface:ISpawnSurfaceAdmission
{
    internal byte[] Admission;internal int Width,Height,SizeX,SizeZ;
    public bool CanTraverseFootprint(int x,int z,int sx,int sz)
    {
        SpawnJson.Need(sx==SizeX&&sz==SizeZ,"Surface profile footprint differs");
        return x>=0&&z>=0&&x<Width&&z<Height&&Admission[x+z*Width]!=0;
    }
}
