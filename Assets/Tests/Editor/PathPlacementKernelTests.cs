using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Runtime;

namespace Game.Tests.Editor
{
    public class PathPlacementKernelTests
    {
        public static int RunChecks()
        {
            var suite=new PathPlacementKernelTests();
            suite.OriginalOracleIsPinned();
            suite.NativeAdapterMatchesOriginalManagedAndBurst();
            suite.NamedPlacementBoundaries();
            return 3;
        }
        [Test] public void OriginalOracleIsPinned()
        {
            string dir=Path.Combine(Application.dataPath,"Tests/Editor");
            string original=File.ReadAllText(Path.Combine(dir,"PathPlacementOriginalSource.cs.txt")).Replace("\r\n","\n");
            using var sha=SHA256.Create();
            Assert.AreEqual("1a33ab235c311875e8ed41f645a2c96151735963fa418f3bc078021c278735ee",BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-","").ToLowerInvariant());
            Assert.AreEqual(original.Replace("internal struct UnitPathPlacementValidation","internal struct PathPlacementOriginalOracle"),File.ReadAllText(Path.Combine(dir,"PathPlacementOriginalOracle.cs")).Replace("\r\n","\n"));
        }
        [Test] public void NamedPlacementBoundaries()
        {
            using var wo=new NativeArray<GridWalkable>(144,Allocator.Temp);
            using var bo=new NativeBitArray(144,Allocator.Temp);
            using var oo=new NativeBitArray(144,Allocator.Temp);
            using var fo=new NativeArray<byte>(144,Allocator.Temp);
            using var eo=new NativeArray<Entity>(1,Allocator.Temp);
            using var go=new NativeArray<UnitGrid>(1,Allocator.Temp);
            using var po=new NativeArray<UnitFootprint>(1,Allocator.Temp);
            using var mo=new NativeArray<byte>(1,Allocator.Temp);
            var w=wo;var b=bo;var o=oo;var f=fo;var e=eo;var g=go;var p=po;var m=mo;
            for(int i=0;i<144;i++)w[i]=new GridWalkable{Value=1};
            e[0]=new Entity{Index=8,Version=2};g[0]=new UnitGrid{Cell=new int2(9,9)};p[0]=new UnitFootprint{Size=new int2(1)};
            var moving=new Entity{Index=1,Version=1};var cell=new int2(5,5);var current=new int2(3,3);var size=new int2(1);var ignored=Entity.Null;
            bool vehicle=false,manual=false;
            void Check(bool expected,string name)
            {
                var grid=new GridConfig{Width=12,Height=12,CellSize=1};
                bool actual=UnitPathPlacementValidation.CanPlaceForPathing(grid,w,b,f,o,e,g,p,m,moving,cell,size,current,vehicle,manual,1,ignored,cell,size);
                bool old=PathPlacementOriginalOracle.CanPlaceForPathing(grid,w,b,f,o,e,g,p,m,moving,cell,size,current,vehicle,manual,1,ignored,cell,size);
                Assert.AreEqual(expected,old,"original "+name);Assert.AreEqual(old,actual,name);
            }
            Check(true,"clear");b.Set(65,true);Check(false,"dynamic enemy");f[65]=1;Check(true,"friendly pass");f[65]=0;current=cell;Check(true,"self overlap dynamic");w[65]=new GridWalkable{Value=0};Check(false,"self overlap still needs walkable");w[65]=new GridWalkable{Value=1};current=new int2(3,3);b.Set(65,false);
            o.Set(65,true);Check(false,"unknown occupied");g[0]=new UnitGrid{Cell=cell};m[0]=1;Check(false,"group requires manual order");manual=true;Check(true,"manual group pass");m[0]=0;Check(false,"manual non-group");ignored=e[0];Check(true,"boarding ignored occupant");ignored=new Entity{Index=8,Version=3};Check(false,"entity version matters for live overlap");ignored=Entity.Null;
            vehicle=true;manual=false;Check(true,"vehicle passes soft unit");p[0]=new UnitFootprint{Size=new int2(2,1)};Check(false,"vehicle hard unit");current=cell;Check(true,"vehicle existing overlap");current=new int2(3,3);p[0]=new UnitFootprint{Size=new int2(1)};cell=new int2(0,0);Check(false,"vehicle clearance border");vehicle=false;Check(true,"infantry border");size=new int2(0,-2);Check(true,"clamped footprint");
        }
        private struct Case
        {
            public int2 Cell,Size,Current,IgnoredCell,IgnoredSize;
            public Entity Moving,Ignored;
            public byte Vehicle,Manual,Faction,MissingBlocked,MissingOccupied,MissingFriendly,MissingGroups;
        }
        [BurstCompile(CompileSynchronously=true)] private struct ParityJob : IJob
        {
            [ReadOnly] public NativeArray<GridWalkable> Walkable;
            [ReadOnly] public NativeBitArray Blocked,Occupied;
            [ReadOnly] public NativeArray<byte> Friendly,Groups;
            [ReadOnly] public NativeArray<Entity> Entities;
            [ReadOnly] public NativeArray<UnitGrid> Grids;
            [ReadOnly] public NativeArray<UnitFootprint> Footprints;
            [ReadOnly] public NativeArray<Case> Cases;
            public NativeArray<int> Results;
            public void Execute()
            {
                int managed=0; MarkManaged(ref managed); Results[0]=managed;
                for(int i=0;i<Cases.Length;i++)
                {
                    Case c=Cases[i];
                    var grid=new GridConfig { Width=12,Height=12,CellSize=1 };
                    var blocked=c.MissingBlocked!=0?default:Blocked;
                    var occupied=c.MissingOccupied!=0?default:Occupied;
                    var friendly=c.MissingFriendly!=0?default:Friendly;
                    var groups=c.MissingGroups!=0?default:Groups;
                    bool actual=UnitPathPlacementValidation.CanPlaceForPathing(grid,Walkable,blocked,friendly,occupied,Entities,Grids,Footprints,groups,c.Moving,c.Cell,c.Size,c.Current,c.Vehicle!=0,c.Manual!=0,c.Faction,c.Ignored,c.IgnoredCell,c.IgnoredSize);
                    bool original=PathPlacementOriginalOracle.CanPlaceForPathing(grid,Walkable,blocked,friendly,occupied,Entities,Grids,Footprints,groups,c.Moving,c.Cell,c.Size,c.Current,c.Vehicle!=0,c.Manual!=0,c.Faction,c.Ignored,c.IgnoredCell,c.IgnoredSize);
                    Results[1+i*2]=actual?1:0; Results[2+i*2]=original?1:0;
                }
            }
            [BurstDiscard] private static void MarkManaged(ref int value) { value=1; }
        }
        [Test] public void NativeAdapterMatchesOriginalManagedAndBurst()
        {
            const int count=8192;
            using var walkableOwner=new NativeArray<GridWalkable>(144,Allocator.TempJob);
            using var blockedOwner=new NativeBitArray(144,Allocator.TempJob);
            using var occupiedOwner=new NativeBitArray(144,Allocator.TempJob);
            using var friendlyOwner=new NativeArray<byte>(137,Allocator.TempJob);
            using var entitiesOwner=new NativeArray<Entity>(8,Allocator.TempJob);
            using var gridsOwner=new NativeArray<UnitGrid>(8,Allocator.TempJob);
            using var footprintsOwner=new NativeArray<UnitFootprint>(8,Allocator.TempJob);
            using var groupsOwner=new NativeArray<byte>(5,Allocator.TempJob);
            using var casesOwner=new NativeArray<Case>(count,Allocator.TempJob);
            using var resultsOwner=new NativeArray<int>(1+count*2,Allocator.TempJob);
            var w=walkableOwner;var b=blockedOwner;var o=occupiedOwner;var f=friendlyOwner;var e=entitiesOwner;var g=gridsOwner;var fp=footprintsOwner;var group=groupsOwner;var cases=casesOwner;
            var random=new System.Random(83291);
            for(int i=0;i<144;i++) {w[i]=new GridWalkable{Value=(byte)(random.Next(12)==0?0:1)}; b.Set(i,random.Next(6)==0);o.Set(i,random.Next(5)==0);if(i<f.Length)f[i]=(byte)random.Next(3);}
            for(int i=0;i<8;i++) {e[i]=new Entity{Index=i+1,Version=i%3+1};g[i]=new UnitGrid{Cell=new int2(random.Next(12),random.Next(12))};fp[i]=new UnitFootprint{Size=new int2(random.Next(1,4),random.Next(1,4))};if(i<group.Length)group[i]=(byte)(i%2);}
            for(int i=0;i<count;i++)
            {
                cases[i]=new Case{Cell=new int2(random.Next(-1,13),random.Next(-1,13)),Current=new int2(random.Next(12),random.Next(12)),Size=new int2(random.Next(-1,5),random.Next(-1,5)),Moving=e[random.Next(8)],Ignored=i%3==0?e[random.Next(8)]:Entity.Null,IgnoredCell=new int2(random.Next(12),random.Next(12)),IgnoredSize=new int2(random.Next(1,4),random.Next(1,4)),Vehicle=(byte)(i%2),Manual=(byte)((i/2)%2),Faction=(byte)(i%3),MissingBlocked=(byte)(i%11==0?1:0),MissingOccupied=(byte)(i%13==0?1:0),MissingFriendly=(byte)(i%7==0?1:0),MissingGroups=(byte)(i%17==0?1:0)};
            }
            var job=new ParityJob{Walkable=w,Blocked=b,Occupied=o,Friendly=f,Groups=group,Entities=e,Grids=g,Footprints=fp,Cases=cases,Results=resultsOwner};
            job.Execute();
            Assert.AreEqual(1,resultsOwner[0],"managed control");
            var expected=new int[count];int accepted=0;
            for(int i=0;i<count;i++) {Assert.AreEqual(resultsOwner[2+i*2],resultsOwner[1+i*2],"managed case "+i);expected[i]=resultsOwner[1+i*2];accepted+=expected[i];}
            Assert.Greater(accepted,0);Assert.Less(accepted,count);
            job.Schedule().Complete();
            Assert.AreEqual(0,resultsOwner[0],"scheduled job must actually use Burst");
            for(int i=0;i<count;i++) {Assert.AreEqual(resultsOwner[2+i*2],resultsOwner[1+i*2],"Burst oracle case "+i);Assert.AreEqual(expected[i],resultsOwner[1+i*2],"integer cross-backend case "+i);}
            Debug.Log("[PathPlacementParity] cases="+count+" accepted="+accepted+" managed=Passed burst=Passed");
        }
    }
}
