#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SharedStaticBlockerKernelTests
    {
        public static int RunChecks()
        {
            var t=new SharedStaticBlockerKernelTests();t.PinnedOriginals();t.OpeningManagedAndBurst();t.CountTransitionsDoNotSaturate();t.DeferredOpeningPresenceAndPassOrder();t.NativeOriginalWriterParity(false);return 5;
        }
        public static void EmitNativeTrace()=>new SharedStaticBlockerKernelTests().NativeOriginalWriterParity(true);
        static string Read(string name)=>File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor",name)).Replace("\r\n","\n");
        static string Hash(string value){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        [Test] public void PinnedOriginals()
        {
            string system=Read("SharedBlockerOriginalStaticGridBlockerUpdateSystem.txt"),opening=Read("SharedBlockerOriginalStaticGridBlockerSurfaceOpeningComponent.txt");
            Assert.AreEqual("0899ab33dc8a1cfa0441e7a85ec2dd4c8ecc9ccec0dc20628fa301742efc7dc9",Hash(system));
            Assert.AreEqual("5964acbac90fbcb89bb1d9a4e64193e52e8a5cdf6b621aae623eac690754e8e5",Hash(opening));
            string expected="#if UNITY_EDITOR && UNITY_INCLUDE_TESTS\n"+system.Replace("namespace Game.Runtime","namespace Game.Tests.Editor").Replace("public partial struct StaticGridBlockerUpdateSystem","[DisableAutoCreation]\n    public partial struct SharedBlockerOriginalSystem").Replace("using Game.Components;","using Game.Components;\nusing Game.Runtime;").Replace("opening.IsValid","SharedBlockerOriginalOpening.Valid(opening)").Replace("opening.ContainsFullCell(grid, x, y)","SharedBlockerOriginalOpening.Contains(opening,grid,x,y)").Replace("opening.SameGeometry(oldOpening)","SharedBlockerOriginalOpening.Same(opening,oldOpening)")+"\n#endif\n";
            Assert.AreEqual(expected,Read("SharedBlockerOriginalSystem.cs"));
            Assert.AreEqual("#if UNITY_EDITOR && UNITY_INCLUDE_TESTS\n"+opening.Replace("namespace Game.Components","namespace Game.Tests.Editor").Replace("StaticGridBlockerSurfaceOpeningComponent","SharedBlockerOriginalOpeningComponent").Replace("StaticGridBlockerPrevSurfaceOpeningComponent","SharedBlockerOriginalPrevOpeningComponent").Replace("using Unity.Entities;","using Unity.Entities;\nusing Game.Components;")+"\n#endif\n",Read("SharedBlockerOriginalOpeningComponent.cs"));
        }
        public struct Storage:IStaticBlockerStorage
        {
            public NativeArray<int> Counts;public NativeArray<byte> Blocked,Friendly;
            public int CountLength=>Counts.Length;public int BlockedLength=>Blocked.Length;public int FriendlyPassLength=>Friendly.Length;public bool FriendlyPassCreated=>Friendly.IsCreated;
            public int ReadCount(int i)=>Counts[i];public void WriteCount(int i,int v)=>Counts[i]=v;public void WriteBlocked(int i,bool v)=>Blocked[i]=(byte)(v?1:0);public void ClearBlocked(){for(int i=0;i<Blocked.Length;i++)Blocked[i]=0;}public void WriteFriendlyPass(int i,byte v)=>Friendly[i]=v;
        }
        [BurstCompile(CompileSynchronously=true)] struct OpeningJob:IJob
        {
            public NativeArray<byte> Result;
            [BurstDiscard] static void ManagedOnly(ref bool value){value=false;}
            public void Execute()
            {
                bool burst=true;ManagedOnly(ref burst);Result[0]=(byte)(burst?1:0);
                for(int i=0;i<8192;i++)
                {
                    var grid=new GridConfig {Width=32,Height=32,CellSize=(i%7+1)*.125f,Origin=new float3((i%11-5)*.31f,0,(i%13-6)*.19f)};
                    float angle=(i%360)*.017453292f;var r=new float2(math.cos(angle),math.sin(angle));
                    var o=new StaticGridBlockerSurfaceOpeningComponent {Center=new float2((i%31-15)*.25f,(i%29-14)*.37f),Right=r,Forward=new float2(-r.y,r.x),HalfExtents=new float2((i%9+1)*.25f,(i%7+1)*.25f)};
                    if(i%4==0)
                    {
                        o.Center=grid.Origin.xz+new float2(i%43-6+.5f,i%41-5+.5f)*grid.CellSize;
                        o.Right=new float2(1,0);o.Forward=new float2(0,1);float half=grid.CellSize*.5f;
                        o.HalfExtents=new float2(math.asfloat(math.asint(half)+(i%3-1)),half);
                    }
                    if(i%17==0)o.Right.x=float.NaN;if(i%23==0)o.HalfExtents.y=0;
                    var k=new BlockerGrid {Width=grid.Width,Height=grid.Height,OriginX=grid.Origin.x,OriginY=grid.Origin.y,OriginZ=grid.Origin.z,CellSize=grid.CellSize};
                    bool original=SharedBlockerOriginalOpening.Contains(o,grid,i%43-6,i%41-5),shared=StaticBlockerKernel.ContainsFullCell(k,o.ToKernel(),i%43-6,i%41-5);
                    Result[i+1]=(byte)(original==shared && SharedBlockerOriginalOpening.Valid(o)==StaticBlockerKernel.IsValid(o.ToKernel())?1:0);
                }
            }
        }
        struct ReplayRow
        {
            public int X,Z,SizeX,SizeZ;public byte Faction;public bool HasOpening,HadOpening,HasHistory;
            public BlockerOpening Opening,PreviousOpening;public BlockerHistory History;
        }
        [BurstCompile(CompileSynchronously=true)] struct ReplayJob:IJob
        {
            public BlockerGrid Grid;public BlockerOwnerIdentity Identity;public Storage Storage;
            public NativeArray<BlockerEpochState> Epoch;public NativeArray<ReplayRow> New,Existing,Removed;public NativeArray<byte> Burst;
            [BurstDiscard] static void ManagedOnly(ref bool value){value=false;}
            static void OpeningChange(ref ReplayRow row,BlockerOpeningChange change)
            {
                if(change==BlockerOpeningChange.Add||change==BlockerOpeningChange.Set){row.HadOpening=true;row.PreviousOpening=row.Opening;}
                else if(change==BlockerOpeningChange.Remove){row.HadOpening=false;row.PreviousOpening=default;}
            }
            public void Execute()
            {
                bool burst=true;ManagedOnly(ref burst);Burst[0]=(byte)(burst?1:0);
                var epoch=Epoch[0];bool rebuild=StaticBlockerKernel.NeedsRebuild(Grid,epoch);
                for(int i=0;i<Existing.Length;i++)if(!StaticBlockerKernel.SameOwner(Existing[i].History.Owner,Identity))rebuild=true;
                for(int i=0;i<Removed.Length;i++)if(!StaticBlockerKernel.SameOwner(Removed[i].History.Owner,Identity))rebuild=true;
                if(rebuild){StaticBlockerKernel.Invalidate(ref epoch);StaticBlockerKernel.Clear(ref Storage);}
                StaticBlockerKernel.BeginWrite(ref epoch);var identity=Identity;identity.StorageEpoch=epoch.StorageEpoch;
                for(int i=0;i<New.Length;i++)
                {
                    var row=New[i];var rect=StaticBlockerKernel.Bounds(row.X,row.Z,row.SizeX,row.SizeZ);
                    var result=StaticBlockerKernel.ApplyNew(Grid,ref Storage,rect,row.Faction,row.HasOpening,row.Opening,identity,out row.History);
                    row.HasHistory=true;OpeningChange(ref row,result.PreviousOpeningChange);New[i]=row;
                }
                for(int i=0;i<Existing.Length;i++)
                {
                    var row=Existing[i];var rect=StaticBlockerKernel.Bounds(row.X,row.Z,row.SizeX,row.SizeZ);
                    var result=StaticBlockerKernel.ApplyExisting(Grid,ref Storage,rebuild,rect,row.Faction,row.HasOpening,row.Opening,row.HadOpening,row.PreviousOpening,identity,ref row.History);
                    OpeningChange(ref row,result.PreviousOpeningChange);Existing[i]=row;
                }
                for(int i=0;i<Removed.Length;i++)
                {
                    var row=Removed[i];var result=StaticBlockerKernel.ApplyRemoved(Grid,ref Storage,rebuild,row.History,row.HadOpening,row.PreviousOpening);
                    row.HasHistory=false;row.History=default;OpeningChange(ref row,result.PreviousOpeningChange);Removed[i]=row;
                }
                StaticBlockerKernel.Commit(Grid,ref epoch);Epoch[0]=epoch;
            }
        }
        static ReplayRow ReplayInput(Row row,bool history)=>new ReplayRow {X=row.origin==null?0:row.origin[0],Z=row.origin==null?0:row.origin[1],SizeX=row.size==null?0:row.size[0],SizeZ=row.size==null?0:row.size[1],Faction=row.faction,HasOpening=row.has_opening,HadOpening=row.had_opening,Opening=row.opening,PreviousOpening=row.previous_opening,History=row.history,HasHistory=history};
        static void ReplaySnapshot(Trace trace,Result expected,bool burst)
        {
            using var counts=new NativeArray<int>(trace.storage.counts,Allocator.TempJob);using var blocked=new NativeArray<byte>(trace.storage.blocked,Allocator.TempJob);using var friendly=new NativeArray<byte>(trace.storage.friendly_pass,Allocator.TempJob);
            using var owner=new NativeArray<BlockerEpochState>(new[]{trace.owner},Allocator.TempJob);using var detector=new NativeArray<byte>(1,Allocator.TempJob);
            using var fresh=new NativeArray<ReplayRow>(trace.@new.Select(r=>ReplayInput(r,false)).ToArray(),Allocator.TempJob);using var existing=new NativeArray<ReplayRow>(trace.existing.Select(r=>ReplayInput(r,true)).ToArray(),Allocator.TempJob);using var removed=new NativeArray<ReplayRow>(trace.removed.Select(r=>ReplayInput(r,true)).ToArray(),Allocator.TempJob);
            var job=new ReplayJob {Grid=trace.grid,Identity=trace.grid_identity,Storage=new Storage {Counts=counts,Blocked=blocked,Friendly=friendly},Epoch=owner,Burst=detector,New=fresh,Existing=existing,Removed=removed};
            if(burst)job.Schedule().Complete();else job.Execute();Assert.AreEqual(burst?1:0,detector[0]);
            Assert.AreEqual(expected.owner,owner[0],trace.id+" owner");CollectionAssert.AreEqual(expected.storage.counts,counts.ToArray(),trace.id+" counts");CollectionAssert.AreEqual(expected.storage.blocked,blocked.ToArray(),trace.id+" blocked");CollectionAssert.AreEqual(expected.storage.friendly_pass,friendly.ToArray(),trace.id+" friendly");
            CompareReplayRows(trace.@new,fresh,expected);CompareReplayRows(trace.existing,existing,expected);CompareReplayRows(trace.removed,removed,expected);
        }
        static void CompareReplayRows(Row[] input,NativeArray<ReplayRow> actual,Result expected)
        {
            for(int i=0;i<input.Length;i++){var desired=expected.changes.Single(r=>r.id==input[i].id);var row=actual[i];Assert.AreEqual(desired.has_history,row.HasHistory);if(row.HasHistory)Assert.AreEqual(desired.history,row.History);Assert.AreEqual(desired.has_previous_opening,row.HadOpening);if(row.HadOpening)Assert.AreEqual(desired.previous_opening,row.PreviousOpening);}
        }
        [Test] public void OpeningManagedAndBurst()
        {
            using var managed=new NativeArray<byte>(8193,Allocator.TempJob);using var burst=new NativeArray<byte>(8193,Allocator.TempJob);
            new OpeningJob {Result=managed}.Execute();new OpeningJob {Result=burst}.Schedule().Complete();Assert.IsTrue(BurstCompiler.IsEnabled);Assert.AreEqual(0,managed[0]);Assert.AreEqual(1,burst[0]);
            for(int i=1;i<managed.Length;i++){Assert.AreEqual(1,managed[i],"managed "+i);Assert.AreEqual(1,burst[i],"burst "+i);}
        }
        [Test] public void CountTransitionsDoNotSaturate()
        {
            using var counts=new NativeArray<int>(1,Allocator.Temp);using var bits=new NativeArray<byte>(1,Allocator.Temp);using var friendly=new NativeArray<byte>(1,Allocator.Temp);
            var storage=new Storage {Counts=counts,Blocked=bits,Friendly=friendly};
            int[] starts={int.MinValue,-2,-1,0,1,2,int.MaxValue};
            foreach(int start in starts)foreach(int delta in new[]{-1,1})
            {
                storage.WriteCount(0,start);storage.WriteBlocked(0,start>0);StaticBlockerKernel.ApplyDelta(ref storage,0,delta);
                int expected=unchecked(start+delta);Assert.AreEqual(expected,counts[0]);Assert.AreEqual(expected>0,bits[0]!=0);
            }
            int before=counts[0];StaticBlockerKernel.ApplyDelta(ref storage,-1,1);StaticBlockerKernel.ApplyDelta(ref storage,1,1);Assert.AreEqual(before,counts[0]);
        }
        [Test] public void DeferredOpeningPresenceAndPassOrder()
        {
            using var counts=new NativeArray<int>(16,Allocator.Temp);using var bits=new NativeArray<byte>(16,Allocator.Temp);using var friendly=new NativeArray<byte>(16,Allocator.Temp);
            var storage=new Storage {Counts=counts,Blocked=bits,Friendly=friendly};StaticBlockerKernel.Clear(ref storage);
            var grid=new BlockerGrid {Width=4,Height=4,CellSize=1};var owner=new BlockerOwnerIdentity {Index=2,Version=1,StorageEpoch=3};var rect=StaticBlockerKernel.Bounds(1,1,2,2);
            StaticBlockerKernel.ApplyNew(grid,ref storage,rect,1,false,default,owner,out var a);StaticBlockerKernel.ApplyNew(grid,ref storage,rect,2,false,default,owner,out var b);
            Assert.AreEqual(2,friendly[5]);Assert.AreEqual(2,counts[5]);
            var noOp=StaticBlockerKernel.ApplyExisting(grid,ref storage,false,rect,1,true,default,false,default,owner,ref a);
            Assert.IsFalse(noOp.Applied);Assert.AreEqual(BlockerOpeningChange.None,noOp.PreviousOpeningChange);Assert.AreEqual(2,friendly[5]);
            StaticBlockerKernel.ApplyRemoved(grid,ref storage,false,b,false,default);Assert.AreEqual(1,counts[5]);Assert.AreEqual(byte.MaxValue,friendly[5]);
            var epoch=new BlockerEpochState {StorageEpoch=ulong.MaxValue,AppliedRevision=ulong.MaxValue};StaticBlockerKernel.Invalidate(ref epoch);Assert.AreEqual(ulong.MaxValue,epoch.StorageEpoch);Assert.AreEqual(1,epoch.EpochExhausted);StaticBlockerKernel.BeginWrite(ref epoch);StaticBlockerKernel.Commit(grid,ref epoch);Assert.AreEqual(ulong.MaxValue,epoch.AppliedRevision);Assert.AreEqual(0,epoch.WriteInProgress);
            Assert.IsFalse(StaticBlockerKernel.SameOwner(owner,new BlockerOwnerIdentity {Index=2,Version=2,StorageEpoch=3}));
        }
        sealed class Fixture:IDisposable
        {
            public readonly World World;public EntityManager Em=>World.EntityManager;public readonly Entity Grid;public readonly List<Entity> Actors=new();readonly SystemHandle writer;
            public Fixture(bool original)
            {
                World=new World(original?"pinned-original-blocker":"shared-blocker");Grid=Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent));
                Em.SetComponentData(Grid,new GridConfig {Width=8,Height=8,CellSize=1,Origin=float3.zero});
                Em.SetComponentData(Grid,new DynamicBlockerComponent {GridSize=64,Counts=new NativeArray<int>(64,Allocator.Persistent),Blocked=new NativeBitArray(64,Allocator.Persistent),FriendlyPassFactionIds=new NativeArray<byte>(64,Allocator.Persistent),StorageEpoch=1});
                writer=original?World.CreateSystem<SharedBlockerOriginalSystem>():World.CreateSystem<StaticGridBlockerUpdateSystem>();
            }
            public DynamicBlockerComponent Data=>Em.GetComponentData<DynamicBlockerComponent>(Grid);
            public GridConfig Config=>Em.GetComponentData<GridConfig>(Grid);
            public Entity Add(int x,int z,int sx,int sz,byte pass=byte.MaxValue)
            {
                var e=Em.CreateEntity(typeof(StaticGridBlocker),typeof(UnitGrid),typeof(GridBlockerSize));Em.SetComponentData(e,new UnitGrid {Cell=new int2(x,z)});Em.SetComponentData(e,new GridBlockerSize {Size=new int2(sx,sz)});if(pass!=byte.MaxValue)Em.AddComponentData(e,new FriendlyPassGridBlocker {AllowedFactionId=pass});Actors.Add(e);return e;
            }
            public void Tick(){writer.Update(World.Unmanaged);Em.CompleteAllTrackedJobs();}
            public void Dispose(){var data=Data;data.Counts.Dispose();data.Blocked.Dispose();data.FriendlyPassFactionIds.Dispose();World.Dispose();}
        }
        static StaticGridBlockerSurfaceOpeningComponent Opening(float x,float z,float hx=.5f,float hz=.5f)=>new StaticGridBlockerSurfaceOpeningComponent {Center=new float2(x,z),Right=new float2(1,0),Forward=new float2(0,1),HalfExtents=new float2(hx,hz)};
        static void Change(Fixture f,int step)
        {
            if(step==0)
            {
                f.Em.AddComponentData(f.Add(1,2,3,3,1),Opening(2.5f,3.5f));f.Add(2,3,2,2,2);var foundation=f.Add(0,0,2,3);f.Em.AddComponent<OperationMapBuildingComponent>(foundation);f.Em.AddComponentData(foundation,new RuntimeBuildingCombatInfo {OriginCell=new int2(6,1)});f.Add(-1,-1,2,2);return;
            }
            var a=f.Actors[0];
            if(step==2){f.Em.SetComponentData(a,new UnitGrid {Cell=new int2(3,2)});f.Em.SetComponentData(a,new GridBlockerSize {Size=new int2(4,2)});f.Em.SetComponentData(a,Opening(4.5f,2.5f,1.5f));f.Em.SetComponentData(a,new FriendlyPassGridBlocker {AllowedFactionId=3});}
            if(step==3){f.Em.RemoveComponent<StaticGridBlocker>(f.Actors[1]);f.Add(3,2,2,2,4);}
            if(step==4)f.Em.RemoveComponent<StaticGridBlockerSurfaceOpeningComponent>(a);
            if(step==5)f.Em.AddComponentData(a,default(StaticGridBlockerSurfaceOpeningComponent));
            if(step==6)f.Em.SetComponentData(a,new FriendlyPassGridBlocker {AllowedFactionId=byte.MaxValue});
            if(step==7){var d=f.Data;d.Counts[63]=99;d.WriteInProgress=1;d.AppliedStorageEpoch=0;f.Em.SetComponentData(f.Grid,d);}
            if(step==8){var g=f.Config;g.Origin.y=.25f;f.Em.SetComponentData(f.Grid,g);}
            if(step==9){var h=f.Em.GetComponentData<StaticBlockerPrevBounds>(a);h.AppliedGrid=new Entity {Index=f.Grid.Index,Version=f.Grid.Version+1};f.Em.SetComponentData(a,h);}
            if(step==10){var d=f.Data;d.StorageEpoch=ulong.MaxValue;d.WriteInProgress=1;f.Em.SetComponentData(f.Grid,d);}
            if(step==11){var d=f.Data;d.AppliedRevision=ulong.MaxValue;f.Em.SetComponentData(f.Grid,d);}
            if(step==12)f.Em.DestroyEntity(a);
            if(step==13)f.Em.RemoveComponent<UnitGrid>(f.Actors[2]); // malformed history excluded by the original query domains
        }
        [Test] public void NativeOriginalWriterParity()=>NativeOriginalWriterParity(false);
        void NativeOriginalWriterParity(bool emit)
        {
            using var original=new Fixture(true);using var shared=new Fixture(false);
            for(int step=0;step<14;step++)
            {
                Change(original,step);Change(shared,step);var trace=Capture(shared,step);original.Tick();shared.Tick();Compare(original,shared,step);
                trace.expected=CaptureResult(shared);ReplaySnapshot(trace,trace.expected,false);ReplaySnapshot(trace,trace.expected,true);
                if(emit){Debug.Log("[StaticBlockerNativeTrace] "+JsonUtility.ToJson(trace));}
            }
            Debug.Log("[SharedStaticBlockerParity] result=Passed stages=14 original=Passed native=Passed scope=IsolatedWriterNotMissionReadiness");
        }
        static void Compare(Fixture original,Fixture shared,int step)
        {
            var a=original.Data;var b=shared.Data;Assert.AreEqual(a.StorageEpoch,b.StorageEpoch);Assert.AreEqual(a.AppliedStorageEpoch,b.AppliedStorageEpoch);Assert.AreEqual(a.AppliedRevision,b.AppliedRevision);Assert.AreEqual(a.EpochExhausted,b.EpochExhausted);Assert.AreEqual(a.WriteInProgress,b.WriteInProgress);Assert.IsTrue(RuntimeGridBlockerEpoch.SameLayout(a.AppliedGrid,b.AppliedGrid));
            for(int i=0;i<64;i++){Assert.AreEqual(a.Counts[i],b.Counts[i],"counts "+step+"/"+i);Assert.AreEqual(a.Blocked.IsSet(i),b.Blocked.IsSet(i),"bits "+step+"/"+i);Assert.AreEqual(a.FriendlyPassFactionIds[i],b.FriendlyPassFactionIds[i],"pass "+step+"/"+i);}
            for(int i=0;i<original.Actors.Count;i++)
            {
                var x=original.Actors[i];var y=shared.Actors[i];bool ah=original.Em.HasComponent<StaticBlockerPrevBounds>(x),bh=shared.Em.HasComponent<StaticBlockerPrevBounds>(y);Assert.AreEqual(ah,bh);
                if(ah){var ap=original.Em.GetComponentData<StaticBlockerPrevBounds>(x);var bp=shared.Em.GetComponentData<StaticBlockerPrevBounds>(y);Assert.AreEqual(ap.Min,bp.Min);Assert.AreEqual(ap.Max,bp.Max);Assert.AreEqual(ap.FriendlyPassFactionId,bp.FriendlyPassFactionId);Assert.AreEqual(ap.StorageEpoch,bp.StorageEpoch);Assert.AreEqual(ap.AppliedGrid==original.Grid,bp.AppliedGrid==shared.Grid);}
                bool ao=original.Em.HasComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(x),bo=shared.Em.HasComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(y);Assert.AreEqual(ao,bo);if(ao)Assert.IsTrue(SharedBlockerOriginalOpening.Same(original.Em.GetComponentData<StaticGridBlockerPrevSurfaceOpeningComponent>(x).Value,shared.Em.GetComponentData<StaticGridBlockerPrevSurfaceOpeningComponent>(y).Value));
            }
        }
        // Raw ordered native input/output records; no invented walkable/occupancy arrays.
        [Serializable] public sealed class Row {public string id;public int[] origin,size;public byte faction;public bool has_opening,had_opening;public BlockerOpening opening,previous_opening;public BlockerHistory history;}
        [Serializable] public sealed class Arrays {public int[] counts;public byte[] blocked,friendly_pass;public bool friendly_pass_created;public string[] unavailable_arrays={"walkable","occupied","reserved"};}
        [Serializable] public sealed class Changed {public string id;public bool has_history,has_previous_opening;public BlockerHistory history;public BlockerOpening previous_opening;}
        [Serializable] public sealed class Result {public BlockerEpochState owner;public Arrays storage;public Changed[] changes;}
        [Serializable] public sealed class Trace {public string id,kind="blockers";public BlockerGrid grid;public BlockerOwnerIdentity grid_identity;public BlockerEpochState owner;public Arrays storage;public Row[] @new,existing,removed;public bool emit_arrays=true;public Result expected;}
        static BlockerGrid GridValue(GridConfig g)=>new BlockerGrid {Width=g.Width,Height=g.Height,OriginX=g.Origin.x,OriginY=g.Origin.y,OriginZ=g.Origin.z,CellSize=g.CellSize};
        static BlockerHistory HistoryValue(StaticBlockerPrevBounds h)=>new BlockerHistory {Rect=new BlockerRect {MinX=h.Min.x,MinZ=h.Min.y,MaxX=h.Max.x,MaxZ=h.Max.y},FriendlyFaction=h.FriendlyPassFactionId,Owner=new BlockerOwnerIdentity {Index=h.AppliedGrid.Index,Version=h.AppliedGrid.Version,StorageEpoch=h.StorageEpoch}};
        static BlockerEpochState OwnerValue(DynamicBlockerComponent d)=>new BlockerEpochState {StorageEpoch=d.StorageEpoch,AppliedStorageEpoch=d.AppliedStorageEpoch,AppliedRevision=d.AppliedRevision,WriteInProgress=d.WriteInProgress,EpochExhausted=d.EpochExhausted,AppliedGrid=GridValue(d.AppliedGrid)};
        static Arrays StorageValue(DynamicBlockerComponent d)=>new Arrays {counts=d.Counts.ToArray(),blocked=Enumerable.Range(0,d.Blocked.Length).Select(i=>(byte)(d.Blocked.IsSet(i)?1:0)).ToArray(),friendly_pass=d.FriendlyPassFactionIds.ToArray(),friendly_pass_created=d.FriendlyPassFactionIds.IsCreated};
        static Row[] Rows(Fixture f,int phase)
        {
            var desc=phase==0?new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<GridBlockerSize>()},None=new[]{ComponentType.ReadOnly<StaticBlockerPrevBounds>()}}:phase==1?new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<GridBlockerSize>(),ComponentType.ReadOnly<StaticBlockerPrevBounds>()}}:new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<StaticBlockerPrevBounds>()},None=new[]{ComponentType.ReadOnly<StaticGridBlocker>()}};
            using var query=f.Em.CreateEntityQuery(desc);using var entities=query.ToEntityArray(Allocator.Temp);var rows=new List<Row>();
            foreach(var entity in entities)
            {
                var row=new Row {id=f.Actors.IndexOf(entity).ToString(),faction=f.Em.HasComponent<FriendlyPassGridBlocker>(entity)?f.Em.GetComponentData<FriendlyPassGridBlocker>(entity).AllowedFactionId:byte.MaxValue,has_opening=f.Em.HasComponent<StaticGridBlockerSurfaceOpeningComponent>(entity),had_opening=f.Em.HasComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(entity)};
                if(phase!=2){var cell=f.Em.HasComponent<OperationMapBuildingComponent>(entity)&&f.Em.HasComponent<RuntimeBuildingCombatInfo>(entity)?f.Em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OriginCell:f.Em.GetComponentData<UnitGrid>(entity).Cell;var size=f.Em.GetComponentData<GridBlockerSize>(entity).Size;row.origin=new[]{cell.x,cell.y};row.size=new[]{size.x,size.y};}
                if(row.has_opening)row.opening=f.Em.GetComponentData<StaticGridBlockerSurfaceOpeningComponent>(entity).ToKernel();if(row.had_opening)row.previous_opening=f.Em.GetComponentData<StaticGridBlockerPrevSurfaceOpeningComponent>(entity).Value.ToKernel();if(phase!=0)row.history=HistoryValue(f.Em.GetComponentData<StaticBlockerPrevBounds>(entity));rows.Add(row);
            }
            return rows.ToArray();
        }
        static Trace Capture(Fixture f,int step)=>new Trace {id="native-blocker-"+step,grid=GridValue(f.Config),grid_identity=new BlockerOwnerIdentity {Index=f.Grid.Index,Version=f.Grid.Version,StorageEpoch=f.Data.StorageEpoch},owner=OwnerValue(f.Data),storage=StorageValue(f.Data),@new=Rows(f,0),existing=Rows(f,1),removed=Rows(f,2)};
        static Result CaptureResult(Fixture f)
        {
            var changes=new List<Changed>();for(int i=0;i<f.Actors.Count;i++){var e=f.Actors[i];var row=new Changed {id=i.ToString(),has_history=f.Em.HasComponent<StaticBlockerPrevBounds>(e),has_previous_opening=f.Em.HasComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(e)};if(row.has_history)row.history=HistoryValue(f.Em.GetComponentData<StaticBlockerPrevBounds>(e));if(row.has_previous_opening)row.previous_opening=f.Em.GetComponentData<StaticGridBlockerPrevSurfaceOpeningComponent>(e).Value.ToKernel();changes.Add(row);}return new Result {owner=OwnerValue(f.Data),storage=StorageValue(f.Data),changes=changes.ToArray()};
        }
    }
}
#endif
