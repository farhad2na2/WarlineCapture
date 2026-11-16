#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Runtime.Spawning;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
// BEGIN FROZEN ORIGINAL
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    public readonly struct MapSurfaceSpawnGroundingOriginalOracle
    {
        private const float MaxInfantrySupportLift = 1.5f;

        public bool TryGroundCellCenter(
            EntityManager entityManager,
            GridConfig grid,
            int2 cell,
            ref float3 worldPosition,
            out MapSurfaceSample sample,
            float groundOffset = 0f)
        {
            sample = default;
            if (!TryGetSurface(entityManager, out MapSurfaceComponent surface, out Entity surfaceEntity) ||
                !TryGetSample(surface, cell, out sample))
            {
                return false;
            }

            if (TryGetHighestInfantrySupport(surface, cell, sample, out MapSurfaceSample supportSample) &&
                supportSample.Height > sample.Height &&
                supportSample.Height - sample.Height <= MaxInfantrySupportLift)
            {
                sample = supportSample;
            }

            if(entityManager.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity))
            {
                var overlays=entityManager.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity,true);
                var triangles=entityManager.HasBuffer<MapSurfaceMeshTriangle>(surfaceEntity)?entityManager.GetBuffer<MapSurfaceMeshTriangle>(surfaceEntity,true).AsNativeArray():default;
                var instances=entityManager.HasBuffer<MapSurfaceMeshInstance>(surfaceEntity)?entityManager.GetBuffer<MapSurfaceMeshInstance>(surfaceEntity,true).AsNativeArray():default;
                float bestHeight=float.NegativeInfinity;
                for(int i=0;i<overlays.Length;i++)
                {
                    var overlay=overlays[i];
                    if((overlay.MovementMask & MapSurfaceMovementMask.AllGroundUnits)==0 ||
                        !MapSurfaceSceneOverlaySampling.TrySample(overlay,worldPosition,triangles,instances,out float height,out float3 normal) || height<=bestHeight)continue;
                    bestHeight=height;sample.Height=height;sample.Normal=normal;
                    sample.LayerId=overlay.LayerId;sample.SurfaceType=overlay.SurfaceType;
                    sample.Flags=overlay.Flags;sample.MovementMask=overlay.MovementMask;
                }
            }
            worldPosition.y = sample.Height + groundOffset;
            return true;
        }

        public bool TryGroundWorldPosition(
            EntityManager entityManager,
            GridConfig grid,
            ref float3 worldPosition,
            out int2 cell,
            out MapSurfaceSample sample,
            float groundOffset = 0f)
        {
            cell = GridUtils.WorldToCell(grid, worldPosition);
            return TryGroundCellCenter(entityManager, grid, cell, ref worldPosition, out sample, groundOffset);
        }

        private bool TryGetSurface(EntityManager entityManager, out MapSurfaceComponent surface, out Entity surfaceEntity)
        {
            surface = default; surfaceEntity=Entity.Null;

            using EntityQuery surfaceQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (surfaceQuery.IsEmptyIgnoreFilter)
                return false;

            surfaceEntity=surfaceQuery.GetSingletonEntity();
            surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            return surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
        }

        private bool TryGetSample(MapSurfaceComponent surface, int2 cell, out MapSurfaceSample sample)
        {
            sample = default;

            if ((uint)cell.x >= (uint)surface.Dimensions.x ||
                (uint)cell.y >= (uint)surface.Dimensions.y ||
                !surface.SurfaceBlob.IsCreated)
            {
                return false;
            }

            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            return MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out sample);
        }

        private bool TryGetHighestInfantrySupport(
            MapSurfaceComponent surface,
            int2 centerCell,
            MapSurfaceSample anchor,
            out MapSurfaceSample supportSample)
        {
            supportSample = anchor;
            bool found = false;
            float bestHeight = anchor.Height;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    int2 cell = centerCell + new int2(x, y);
                    if (!TryGetSample(surface, cell, out MapSurfaceSample candidate))
                        continue;
                    if (!CanUseInfantrySupportSample(anchor, candidate))
                        continue;
                    if (found && candidate.Height <= bestHeight)
                        continue;

                    supportSample = candidate;
                    bestHeight = candidate.Height;
                    found = true;
                }
            }

            return found;
        }

        private static bool CanUseInfantrySupportSample(MapSurfaceSample anchor, MapSurfaceSample candidate)
        {
            if ((candidate.MovementMask & MapSurfaceMovementMask.Infantry) == 0)
                return false;
            if (candidate.LayerId != anchor.LayerId)
                return false;

            bool anchorRoadLike = IsRoadLikeSurface(anchor.SurfaceType, anchor.Flags);
            bool candidateRoadLike = IsRoadLikeSurface(candidate.SurfaceType, candidate.Flags);
            return anchorRoadLike == candidateRoadLike;
        }

        private static bool IsRoadLikeSurface(MapSurfaceType surfaceType, MapSurfaceFlags flags)
        {
            return surfaceType == MapSurfaceType.Road ||
                   surfaceType == MapSurfaceType.DirtRoad ||
                   surfaceType == MapSurfaceType.Highway ||
                   surfaceType == MapSurfaceType.BridgeDeck ||
                   surfaceType == MapSurfaceType.Ramp ||
                   (flags & MapSurfaceFlags.Road) != 0;
        }
    }
}
// END FROZEN ORIGINAL

namespace Game.Tests.Editor
{
    public sealed class SpawnGroundingKernelTests
    {
        public static void RunFocusedValidation()
        {
            try
            {
                RunAll();
                ValidationExit.Passed();
            }
            catch(Exception e) { Debug.LogException(e); Debug.LogError("[SpawnGroundingKernelParity] result=Failed missionReady=0"); ValidationExit.Failed(); }
        }
        public static void RunGroundingAndS002StartupValidation()
        {
            try
            {
                RunAll();
                Game.Editor.SkirmishS002AriaRunHarness.RunFocusedAriaAndExit();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[SpawnGroundingAndS002Startup] result=Failed missionReady=0");
                ValidationExit.Failed();
            }
        }
        static void RunAll()
        {
            var t = new SpawnGroundingKernelTests();
            t.OriginalSnapshotAndEnumContract();
            t.SupportOrderAndFloatBoundaries();
            t.OverlayNaNTiesAndOffsets();
            t.NativeAdapterMatchesFrozenOriginal();
            Debug.Log("[SpawnGroundingKernelParity] result=Passed tests=4 nativeCases=96 missionReady=0");
        }
        [Test] public void OriginalSnapshotAndEnumContract()
        {
            string original=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/MapSurfaceSpawnGroundingOriginal.cs.txt")).Replace("\r\n","\n");
            using var sha=SHA256.Create();
            Assert.AreEqual("a6b858189e43cd542f6c743f3a1694e16e150c29f2f8f7e665350bfc0d88e938",BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(original))).Replace("-","").ToLowerInvariant());
            string tests=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/SpawnGroundingKernelTests.cs")).Replace("\r\n","\n");
            string frozen=tests.Split(new[]{"// BEGIN FROZEN ORIGINAL\n"},StringSplitOptions.None)[1].Split(new[]{"// END FROZEN ORIGINAL"},StringSplitOptions.None)[0];
            Assert.AreEqual(original.Replace("MapSurfaceSpawnGrounding","MapSurfaceSpawnGroundingOriginalOracle"),frozen);
            foreach(MapSurfaceType type in Enum.GetValues(typeof(MapSurfaceType)))
                for(int flags=0;flags<256;flags++)
                    Assert.AreEqual(type==MapSurfaceType.Road||type==MapSurfaceType.DirtRoad||type==MapSurfaceType.Highway||type==MapSurfaceType.BridgeDeck||type==MapSurfaceType.Ramp||(((MapSurfaceFlags)flags&MapSurfaceFlags.Road)!=0),SpawnGroundingKernel.IsRoadLikeSurface((byte)type,(ushort)flags));
            Assert.AreEqual(1,(ushort)MapSurfaceMovementMask.Infantry);Assert.AreEqual(7,(ushort)MapSurfaceMovementMask.AllGroundUnits);
        }
        struct Samples : ISpawnGroundingSamples<MapSurfaceSample>
        {
            public MapSurfaceSample[] Values; public int[] Visits; public int Count;
            public bool TryGetSample(int x,int z,out MapSurfaceSample s) { int i=x+z*3;Visits[Count++]=i;s=Values[i];return true; }
            public SpawnGroundingFacts Facts(MapSurfaceSample s)=>new SpawnGroundingFacts{Height=s.Height,LayerId=s.LayerId,SurfaceType=(byte)s.SurfaceType,Flags=(ushort)s.Flags,MovementMask=(ushort)s.MovementMask};
        }
        [Test] public void SupportOrderAndFloatBoundaries()
        {
            var values=new MapSurfaceSample[9];for(int i=0;i<9;i++)values[i]=new MapSurfaceSample{Height=2,SurfaceId=i,MovementMask=MapSurfaceMovementMask.Infantry};
            var c=new Samples{Values=values,Visits=new int[9]};
            Assert.IsTrue(SpawnGroundingKernel.TryGetHighestInfantrySupport<Samples,MapSurfaceSample>(ref c,1,1,values[4],out var sample));
            Assert.AreEqual(0,sample.SurfaceId);CollectionAssert.AreEqual(new[]{0,1,2,3,4,5,6,7,8},c.Visits);
            values[0].Height=float.NaN;values[8].Height=float.NaN;c.Count=0;
            Assert.IsTrue(SpawnGroundingKernel.TryGetHighestInfantrySupport<Samples,MapSurfaceSample>(ref c,1,1,values[4],out sample));Assert.AreEqual(8,sample.SurfaceId);
            foreach(float a in new[]{-10f,0f,4f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
                foreach(float b in new[]{-10f,0f,1.5f,1.500001f,5.5f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
                    Assert.AreEqual(b>a&&b-a<=1.5f,SpawnGroundingKernel.ShouldLiftToSupport(a,b));
        }
        [Test] public void OverlayNaNTiesAndOffsets()
        {
            float best=float.NegativeInfinity;
            Assert.IsFalse(SpawnGroundingKernel.AcceptOverlayHeight(float.NegativeInfinity,ref best));
            Assert.IsTrue(SpawnGroundingKernel.AcceptOverlayHeight(2,ref best));Assert.IsFalse(SpawnGroundingKernel.AcceptOverlayHeight(2,ref best));
            Assert.IsTrue(SpawnGroundingKernel.AcceptOverlayHeight(float.NaN,ref best));Assert.IsTrue(float.IsNaN(best));
            Assert.IsTrue(SpawnGroundingKernel.AcceptOverlayHeight(-100,ref best));Assert.AreEqual(-100,best);
            for(int mask=0;mask<256;mask++)Assert.AreEqual((mask&7)!=0,SpawnGroundingKernel.CanSampleOverlay((ushort)mask));
            foreach(float h in new[]{-5f,0f,float.NaN,float.PositiveInfinity})foreach(float offset in new[]{-.25f,0f,3f,float.NaN})
                Assert.AreEqual(BitConverter.SingleToInt32Bits(h+offset),BitConverter.SingleToInt32Bits(SpawnGroundingKernel.FinalHeight(h,offset)));
        }
        static void SameFloat(float expected,float actual) => Assert.AreEqual(BitConverter.SingleToInt32Bits(expected),BitConverter.SingleToInt32Bits(actual));
        static void SameSample(MapSurfaceSample e,MapSurfaceSample a)
        {
            Assert.AreEqual(e.Cell,a.Cell);Assert.AreEqual(e.SurfaceId,a.SurfaceId);Assert.AreEqual(e.LayerId,a.LayerId);
            SameFloat(e.Height,a.Height);SameFloat(e.Normal.x,a.Normal.x);SameFloat(e.Normal.y,a.Normal.y);SameFloat(e.Normal.z,a.Normal.z);SameFloat(e.SlopeDegrees,a.SlopeDegrees);
            Assert.AreEqual(e.SurfaceType,a.SurfaceType);Assert.AreEqual(e.MovementMask,a.MovementMask);Assert.AreEqual(e.Flags,a.Flags);Assert.AreEqual(e.FirstConnectionIndex,a.FirstConnectionIndex);Assert.AreEqual(e.ConnectionCount,a.ConnectionCount);
        }
        [Test] public void NativeAdapterMatchesFrozenOriginal()
        {
            var random=new System.Random(489371);
            for(int seed=0;seed<96;seed++)
            {
                using var world=new World("SpawnGroundingKernelParity");var em=world.EntityManager;
                using var builder=new BlobBuilder(Allocator.Temp);ref var root=ref builder.ConstructRoot<MapSurfaceBlob>();
                root.Dimensions=new int2(3,3);root.CellSize=1;root.RuntimeEncoding=MapSurfaceRuntimeEncoding.Full;
                var cells=builder.Allocate(ref root.Cells,9);var samples=builder.Allocate(ref root.Samples,9);
                builder.Allocate(ref root.Connections,0);builder.Allocate(ref root.CompactSamples,0);
                for(int i=0;i<9;i++)
                {
                    cells[i]=new MapSurfaceCell{FirstSurfaceIndex=i,SurfaceCount=1};
                    samples[i]=new MapSurfaceSample{Cell=new int2(i%3,i/3),SurfaceId=100+i,LayerId=random.Next(2),Height=(seed%8==0&&i%3==0)?float.NaN:(float)random.NextDouble()*4,Normal=new float3(i,1,-i),SlopeDegrees=i,SurfaceType=(MapSurfaceType)random.Next(7),MovementMask=(MapSurfaceMovementMask)random.Next(8),Flags=(MapSurfaceFlags)random.Next(4),FirstConnectionIndex=19+i,ConnectionCount=(ushort)i};
                }
                using var blob=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
                Entity entity=em.CreateEntity(typeof(MapSurfaceComponent));
                em.SetComponentData(entity,new MapSurfaceComponent{SurfaceBlob=blob,HasSurfaceData=(byte)(seed==95?0:1),Dimensions=new int2(3,3),CellSize=1});
                if(seed%2==0)
                {
                    var overlays=em.AddBuffer<MapSurfaceSceneOverlay>(entity);
                    foreach(float height in new[]{-2f,2f,2f,float.NaN,-5f,3f})
                        overlays.Add(new MapSurfaceSceneOverlay{Center=new float3(1.5f,0,1.5f),HalfExtents=new float2(2,2),Height=height,Normal=new float3(height,1,0),MovementMask=(MapSurfaceMovementMask)(seed%3==0?4:1),LayerId=20+overlays.Length,SurfaceType=MapSurfaceType.Plaza});
                }
                var grid=new GridConfig{Width=3,Height=3,CellSize=1};var a=new float3(1.5f,83,1.5f);var b=a;float offset=seed%3==0?-.25f:.15f;
                var cell=seed==94?new int2(-1,1):new int2(1,1);
                bool actual=new Game.Runtime.MapSurfaceSpawnGrounding().TryGroundCellCenter(em,grid,cell,ref a,out var actualSample,offset);
                bool expected=new Game.Runtime.MapSurfaceSpawnGroundingOriginalOracle().TryGroundCellCenter(em,grid,cell,ref b,out var expectedSample,offset);
                Assert.AreEqual(expected,actual,"seed="+seed);SameFloat(b.x,a.x);SameFloat(b.y,a.y);SameFloat(b.z,a.z);
                SameSample(expectedSample,actualSample);
            }
        }
    }
}
#endif
