using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Spawning;

// Compile separately against installed netstandard/math references and run only
// via pinned Mono --optimize=-float32. No native answer enters this protocol.
static class StartupGroundingWorker
{
    const uint Magic = 0x53475231;
    static void Need(bool ok,string message) { if(!ok)throw new ArgumentException(message); }
    static string Hash(byte[] value) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","").ToLowerInvariant(); }
    static int Main(string[] args)
    {
        try
        {
            Need(args.Length==4,"Expected input/output/math path/math SHA256");
            Need(Hash(File.ReadAllBytes(args[2]))==args[3],"Worker mathematics bytes differ");
            var mathAssembly=Assembly.LoadFile(Path.GetFullPath(args[2]));
            AppDomain.CurrentDomain.AssemblyResolve+=(sender,e)=>
                e.Name.StartsWith("UnityEngine.MathematicsModule,",StringComparison.Ordinal)?mathAssembly:
                e.Name.StartsWith("UnityEngine.SharedInternalsModule,",StringComparison.Ordinal)?Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[2])),"UnityEngine.SharedInternalsModule.dll")):null;
            Run(args[0],args[1]);return 0;
        }
        catch(Exception e) { Console.Error.WriteLine("[StartupGroundingWorker] result=Failed "+e);return 1; }
    }
    static float3 V3(BinaryReader r)=>new float3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    static float2 V2(BinaryReader r)=>new float2(r.ReadSingle(),r.ReadSingle());
    static float4 V4(BinaryReader r)=>new float4(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    struct Samples:ISpawnGroundingSamples<MapSurfaceSample>
    {
        public MapSurfaceBlob Blob;
        public bool TryGetSample(int x,int z,out MapSurfaceSample sample)=>MapSurfaceBlobAccess.TryGetPrimarySurface(ref Blob,new int2(x,z),out sample);
        public SpawnGroundingFacts Facts(MapSurfaceSample s)=>new SpawnGroundingFacts{Height=s.Height,LayerId=s.LayerId,SurfaceType=(byte)s.SurfaceType,MovementMask=(ushort)s.MovementMask,Flags=(ushort)s.Flags};
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run(string input,string output)
    {
        Need(new FileInfo(input).Length<=1024*1024,"Worker input exceeds1MiB");
        using(var q=new BinaryReader(File.OpenRead(input)))
        {
            Need(q.ReadUInt32()==Magic&&q.ReadInt32()==1,"Worker input protocol differs");
            int width=q.ReadInt32(),height=q.ReadInt32();float minimum=q.ReadSingle(),step=q.ReadSingle();
            int oc=q.ReadInt32(),tc=q.ReadInt32(),ic=q.ReadInt32(),count=q.ReadInt32();
            long cells=(long)width*height;
            Need(width>0&&height>0&&cells<=16777216&&!float.IsNaN(minimum)&&!float.IsInfinity(minimum)&&step>0&&!float.IsInfinity(step),"Worker compact metadata differs");
            Need(oc>=0&&oc<=1048576&&tc>=0&&tc<=4194304&&ic>=0&&ic<=1048576&&count>=1&&count<=512,"Worker geometry/query bounds exceeded");
            long loaded=0;
            byte[] Artifact(long expected)
            {
                string path=q.ReadString();long bytes=q.ReadInt64();string hash=q.ReadString();
                loaded+=expected;Need(bytes==expected&&loaded<=512L*1024*1024&&Path.IsPathRooted(path),"Worker artifact shape differs");
                for(var item=new FileInfo(path) as FileSystemInfo;item!=null;item=item is FileInfo f?f.Directory:((DirectoryInfo)item).Parent)
                    Need((item.Attributes&FileAttributes.ReparsePoint)==0,"Worker artifact symlink rejected");
                Need(new FileInfo(path).Length==expected,"Worker artifact length differs");
                var raw=File.ReadAllBytes(path);Need(raw.LongLength==expected&&Hash(raw)==hash,"Worker artifact bytes differ");return raw;
            }
            var compact=new MapSurfaceCompactSample[(int)cells];
            using(var r=new BinaryReader(new MemoryStream(Artifact(cells*12))))for(int i=0;i<compact.Length;i++)compact[i]=new MapSurfaceCompactSample{PackedHeight=r.ReadUInt16(),NormalX=r.ReadSByte(),NormalY=r.ReadSByte(),NormalZ=r.ReadSByte(),LayerId=r.ReadInt16(),SurfaceType=(MapSurfaceType)r.ReadByte(),MovementMask=(MapSurfaceMovementMask)r.ReadUInt16(),Flags=(MapSurfaceFlags)r.ReadUInt16()};
            var os=new MapSurfaceSceneOverlay[oc];
            using(var r=new BinaryReader(new MemoryStream(Artifact(4L+oc*77L))))
            {
                Need(r.ReadInt32()==oc,"Worker overlay header differs");
                for(int i=0;i<oc;i++)
                {
                    var o=new MapSurfaceSceneOverlay{Center=V3(r),YawRadians=r.ReadSingle(),HalfExtents=V2(r),Height=r.ReadSingle(),Normal=V3(r),TriangleA=V2(r),TriangleB=V2(r),TriangleC=V2(r),GeometryInstanceIndex=r.ReadInt32(),SurfaceType=(MapSurfaceType)r.ReadByte(),MovementMask=(MapSurfaceMovementMask)r.ReadUInt16(),Flags=(MapSurfaceFlags)r.ReadUInt16(),LayerId=r.ReadInt32()};
                    Need((o.Flags&MapSurfaceFlags.ExactMesh)==0||(uint)o.GeometryInstanceIndex<(uint)ic,"Worker mesh instance absent");os[i]=o;
                }
            }
            var ts=new MapSurfaceMeshTriangle[tc];
            using(var r=new BinaryReader(new MemoryStream(Artifact(4L+tc*36L)))){Need(r.ReadInt32()==tc,"Worker triangle header differs");for(int i=0;i<tc;i++)ts[i]=new MapSurfaceMeshTriangle{A=V3(r),B=V3(r),C=V3(r)};}
            var vs=new MapSurfaceMeshInstance[ic];
            using(var r=new BinaryReader(new MemoryStream(Artifact(4L+ic*72L))))
            {
                Need(r.ReadInt32()==ic,"Worker instance header differs");
                for(int i=0;i<ic;i++)
                {
                    var v=new MapSurfaceMeshInstance{WorldToMesh=new float4x4(V4(r),V4(r),V4(r),V4(r)),FirstTriangle=r.ReadInt32(),TriangleCount=r.ReadInt32()};
                    Need(v.FirstTriangle>=0&&v.TriangleCount>=0&&(long)v.FirstTriangle+v.TriangleCount<=tc,"Worker triangle range differs");vs[i]=v;
                }
            }
            long perQuery=9L+oc;
            foreach(var o in os)if(SpawnGroundingKernel.CanSampleOverlay((ushort)o.MovementMask)&&(o.Flags&MapSurfaceFlags.ExactMesh)!=0)perQuery+=vs[o.GeometryInstanceIndex].TriangleCount;
            Need(perQuery*count<=512L*1024*1024,"Worker inspection budget exceeded");
            var blob=new MapSurfaceBlob{Dimensions=new int2(width,height),RuntimeEncoding=MapSurfaceRuntimeEncoding.SingleLayerCompact,CompactMinHeight=minimum,CompactHeightStep=step,CompactSamples=new BlobArray<MapSurfaceCompactSample>(compact)};
            var triangles=new NativeArray<MapSurfaceMeshTriangle>(ts);var instances=new NativeArray<MapSurfaceMeshInstance>(vs);
            using(var writer=new BinaryWriter(new FileStream(output,FileMode.CreateNew,FileAccess.Write)))
            {
                writer.Write(Magic);writer.Write(1);writer.Write(count);
                for(int n=0;n<count;n++)
                {
                    int ordinal=q.ReadInt32(),x=q.ReadInt32(),z=q.ReadInt32();float wx=q.ReadSingle(),wz=q.ReadSingle();
                    Need(ordinal==n&&!float.IsNaN(wx)&&!float.IsInfinity(wx)&&!float.IsNaN(wz)&&!float.IsInfinity(wz),"Worker query order/position differs");
                    Need(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob,new int2(x,z),out var sample),"Worker cell unavailable");
                    var context=new Samples{Blob=blob};
                    if(SpawnGroundingKernel.TryGetHighestInfantrySupport<Samples,MapSurfaceSample>(ref context,x,z,sample,out var support)&&SpawnGroundingKernel.ShouldLiftToSupport(sample.Height,support.Height))sample=support;
                    float best=float.NegativeInfinity;
                    foreach(var o in os)
                    {
                        if(!SpawnGroundingKernel.CanSampleOverlay((ushort)o.MovementMask)||!MapSurfaceSceneOverlaySampling.TrySample(o,new float3(wx,0,wz),triangles,instances,out float h,out float3 normal)||!SpawnGroundingKernel.AcceptOverlayHeight(h,ref best))continue;
                        sample.Height=h;
                    }
                    float actual=SpawnGroundingKernel.FinalHeight(sample.Height,0f);
                    Need(!float.IsNaN(actual)&&!float.IsInfinity(actual),"Worker grounded height is nonfinite");
                    writer.Write(ordinal);writer.Write(BitConverter.ToInt32(BitConverter.GetBytes(actual),0));
                }
                Need(q.BaseStream.Position==q.BaseStream.Length,"Worker trailing input rejected");
            }
        }
        Console.WriteLine("[StartupGroundingWorker] result=Passed");
    }
}
