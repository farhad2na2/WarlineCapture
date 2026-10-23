using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class GridCoordinateKernelTests
    {
        public static int RunChecks()
        {
            var suite = new GridCoordinateKernelTests();
            suite.OriginalSourcePinned(); suite.OriginalNativeAndSharedCoordinatesMatch(); suite.NegativeBoundariesAndCellCenters();
            Debug.Log("[GridCoordinateParity] result=Passed tests=3 cases=8192 managed=Passed burst=Passed missionReady=0");
            return 3;
        }
        [Test] public void OriginalSourcePinned()
        {
            var path = Path.Combine(Application.dataPath, "Tests/Editor/GridCoordinateOriginalSource.cs.txt");
            using var sha = SHA256.Create();
            Assert.AreEqual("ec3416ec19f9104420552573b6e7117701529118bd92318f1943013e3048db9a",
                BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n", "\n")))).Replace("-", "").ToLowerInvariant());
        }
        private struct Input { public GridCoordinateFrame Frame; public float X,Z; public int CellX,CellZ; }
        private struct Output { public int2 OriginalCell,SharedCell,NativeCell; public float3 OriginalCenter,SharedCenter,NativeCenter; }
        [BurstCompile(CompileSynchronously=true)] private struct PairJob : IJob
        {
            [ReadOnly] public NativeArray<Input> Inputs;
            public NativeArray<Output> Outputs;
            public NativeArray<byte> Managed;
            [BurstDiscard] private static void MarkManaged(ref byte value) { value=1; }
            public void Execute()
            {
                byte managed=0;MarkManaged(ref managed);Managed[0]=managed;
                for(int i=0;i<Inputs.Length;i++)
                {
                    var input=Inputs[i];var f=input.Frame;
                    var grid=new GridConfig {Origin=new float3(f.OriginX,f.OriginY,f.OriginZ),CellSize=f.CellSize,Width=64,Height=64};
                    var position=new float3(input.X,0,input.Z);var cell=new int2(input.CellX,input.CellZ);
                    // Saved GridUtils original statements, independent of shared conversion.
                    float3 local=position-grid.Origin;
                    var originalCell=new int2((int)math.floor(local.x/grid.CellSize),(int)math.floor(local.z/grid.CellSize));
                    var originalCenter=grid.Origin+new float3((cell.x+0.5f)*grid.CellSize,0f,(cell.y+0.5f)*grid.CellSize);
                    var sharedCell=GridCoordinateKernel.WorldToCell(f,input.X,input.Z);
                    var sharedCenter=GridCoordinateKernel.CellToWorldCenter(f,input.CellX,input.CellZ);
                    Outputs[i]=new Output {OriginalCell=originalCell,SharedCell=new int2(sharedCell.X,sharedCell.Z),NativeCell=GridUtils.WorldToCell(grid,position),
                        OriginalCenter=originalCenter,SharedCenter=new float3(sharedCenter.X,sharedCenter.Y,sharedCenter.Z),NativeCenter=GridUtils.CellToWorldCenter(grid,cell)};
                }
            }
        }
        private static void Verify(NativeArray<Output> values)
        {
            foreach(var value in values)
            {
                Assert.AreEqual(value.OriginalCell,value.SharedCell);Assert.AreEqual(value.OriginalCell,value.NativeCell);
                Assert.AreEqual(math.asint(value.OriginalCenter),math.asint(value.SharedCenter));
                Assert.AreEqual(math.asint(value.OriginalCenter),math.asint(value.NativeCenter));
            }
        }
        [Test] public void OriginalNativeAndSharedCoordinatesMatch()
        {
            using var inputsOwner=new NativeArray<Input>(8192,Allocator.TempJob);var inputs=inputsOwner;
            using var outputs=new NativeArray<Output>(inputs.Length,Allocator.TempJob);using var managed=new NativeArray<byte>(1,Allocator.TempJob);
            var random=new System.Random(50419);float[] sizes={.001f,.1f,.3f,1,2,64,1000};
            for(int i=0;i<inputs.Length;i++)
            {
                var frame=new GridCoordinateFrame {OriginX=(float)(random.NextDouble()*20000-10000),OriginY=i%3==0?-0f:random.Next(-20,20),OriginZ=(float)(random.NextDouble()*20000-10000),CellSize=sizes[i%sizes.Length]};
                int x=random.Next(-10000,10000),z=random.Next(-10000,10000);
                float wx=i%2==0?frame.OriginX+x*frame.CellSize:(float)(random.NextDouble()*100000-50000);
                float wz=i%2==0?frame.OriginZ+z*frame.CellSize:(float)(random.NextDouble()*100000-50000);
                inputs[i]=new Input {Frame=frame,X=wx,Z=wz,CellX=x,CellZ=z};
            }
            var job=new PairJob{Inputs=inputs,Outputs=outputs,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);Verify(outputs);
            job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Actual Burst required");Verify(outputs);
        }
        [Test] public void NegativeBoundariesAndCellCenters()
        {
            var frame=new GridCoordinateFrame {OriginX=10,OriginY=7,OriginZ=-10,CellSize=2};
            var below=GridCoordinateKernel.WorldToCell(frame,9.999f,-10.001f);Assert.AreEqual(-1,below.X);Assert.AreEqual(-1,below.Z);
            var exact=GridCoordinateKernel.WorldToCell(frame,10,-10);Assert.AreEqual(0,exact.X);Assert.AreEqual(0,exact.Z);
            for(int z=-8;z<=8;z++)for(int x=-8;x<=8;x++)
            {var center=GridCoordinateKernel.CellToWorldCenter(frame,x,z);var roundTrip=GridCoordinateKernel.WorldToCell(frame,center.X,center.Z);Assert.AreEqual(x,roundTrip.X);Assert.AreEqual(z,roundTrip.Z);Assert.AreEqual(7,center.Y);}
        }
    }
}
