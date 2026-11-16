#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishTypedSpawnConstraintTests
    {
        static void WithPad(bool rotated,Action<EntityManager,Entity,GridConfig,SkirmishMapLayoutConfig,SkirmishResolvedForceEntry> action)
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition("S026",(definition,owned)=>
            {
                var layout=definition.MapLayout;Assert.IsTrue(layout.TryGetPad(1,SkirmishLegalPadKind.InfantrySpawn,out var pad));
                // The generic source fixture projects a600x850 envelope into a
                // smaller Campaign frame. These isolated mask controls require
                // actual4.5/6.5metre squares, not4.5/6.5 envelope-unit rectangles.
                float worldExtent=rotated?6.5f:4.5f;
                float forwardScale=layout.WorldBinding.ForwardMetres/layout.WorldWidthMetres;
                float acrossScale=layout.WorldBinding.AcrossMetres/layout.WorldDepthMetres;
                Assert.Greater(forwardScale,0);Assert.Greater(acrossScale,0);
                var serialized=new SerializedObject(layout);var pads=serialized.FindProperty("pads");
                for(int i=0;i<pads.arraySize;i++)if(pads.GetArrayElementAtIndex(i).FindPropertyRelative("PadId").stringValue==pad.PadId)
                {pads.GetArrayElementAtIndex(i).FindPropertyRelative("WidthMetres").floatValue=worldExtent/forwardScale;pads.GetArrayElementAtIndex(i).FindPropertyRelative("DepthMetres").floatValue=worldExtent/acrossScale;}
                if(rotated)
                {
                    var frame=serialized.FindProperty("worldBinding");float q=Mathf.Sqrt(.5f);
                    frame.FindPropertyRelative("ForwardX").floatValue=q;frame.FindPropertyRelative("ForwardZ").floatValue=q;
                    frame.FindPropertyRelative("AcrossX").floatValue=-q;frame.FindPropertyRelative("AcrossZ").floatValue=q;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsTrue(layout.TryGetPad(1,SkirmishLegalPadKind.InfantrySpawn,out pad));
                Assert.IsTrue(layout.TryProjectLocalToMap(pad.CenterX-pad.WidthMetres*.5f,pad.CenterZ-pad.DepthMetres*.5f,out float ax,out float az));
                Assert.IsTrue(layout.TryProjectLocalToMap(pad.CenterX+pad.WidthMetres*.5f,pad.CenterZ-pad.DepthMetres*.5f,out float bx,out float bz));
                Assert.IsTrue(layout.TryProjectLocalToMap(pad.CenterX-pad.WidthMetres*.5f,pad.CenterZ+pad.DepthMetres*.5f,out float dx,out float dz));
                Assert.AreEqual(worldExtent,math.distance(new float2(ax,az),new float2(bx,bz)),.002f,"Actual projected pad width must match the mask fixture's metre contract");
                Assert.AreEqual(worldExtent,math.distance(new float2(ax,az),new float2(dx,dz)),.002f,"Actual projected pad depth must match the mask fixture's metre contract");
                Assert.IsTrue(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z));
                using var world=new World("TypedSpawnMaskFixture");var em=world.EntityManager;
                var config=new GridConfig{Width=32,Height=32,CellSize=1,Origin=new float3(x-16,0,z-16)};
                var entity=em.CreateEntity(typeof(GridConfig));em.SetComponentData(entity,config);
                action(em,entity,config,layout,new SkirmishResolvedForceEntry{FactionId=1,RoleKind=SkirmishRoleKind.Rifle,RoleId=SkirmishRoleIds.Rifle,SpawnAnchorId=pad.AnchorId});
            });
        }
        [Test]public void FullFootprintsAndRotatedQuadRejectCrossingCells()
        {
            foreach(bool rotated in new[]{false,true})WithPad(rotated,(em,entity,grid,layout,force)=>
            {
                using var cache=new SkirmishTypedSpawnConstraint(em,entity,grid,layout);
                using var actual=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                Assert.IsTrue(cache.TryMask(force,actual,out var mask));
                try
                {
                    Assert.IsFalse(mask.IsSet(16*32+16));
                    Assert.IsTrue(mask.IsSet(13*32+13),"Full source cell must be inside quad, not only its center/AABB");
                    using var walk=new NativeArray<GridWalkable>(1024,Allocator.Persistent);var writeWalk=walk;for(int i=0;i<walk.Length;i++)writeWalk[i]=new GridWalkable{Value=1};
                    using var blocked=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                    var reserved=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                    try
                    {
                        var rng=new Unity.Mathematics.Random(1);
                        Assert.IsTrue(InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref rng,grid,walk,blocked,mask,ref reserved,new int2(16),12,new int2(3),false,out var cell));
                        var min=UnitFootprintUtility.GetMinCell(cell,new int2(3));
                        for(int z=min.y;z<min.y+3;z++)for(int x=min.x;x<min.x+3;x++)Assert.IsFalse(mask.IsSet(z*32+x));
                    }
                    finally{reserved.Dispose();}
                }
                finally{mask.Dispose();}
            });
        }
        [Test]public void ExhaustedTypedPadRefusesGlobalFallbackIncludingAirButDefaultSearchStillWorks()
        {
            WithPad(false,(em,entity,grid,layout,force)=>
            {
                using var cache=new SkirmishTypedSpawnConstraint(em,entity,grid,layout);
                using var actual=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                Assert.IsTrue(cache.TryMask(force,actual,out var mask));
                try
                {
                    using var walk=new NativeArray<GridWalkable>(1024,Allocator.Persistent);var writeWalk=walk;for(int i=0;i<walk.Length;i++)writeWalk[i]=new GridWalkable{Value=1};
                    using var blocked=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                    var reserved=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                    try
                    {
                        for(int i=0;i<1024;i++)if(!mask.IsSet(i))reserved.Set(i,true);
                        foreach(bool air in new[]{false,true})
                        {var rng=new Unity.Mathematics.Random(1);Assert.IsFalse(InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref rng,grid,walk,blocked,mask,ref reserved,new int2(16),12,new int2(1),air,out _));}
                        var ordinary=new Unity.Mathematics.Random(1);
                        Assert.IsTrue(InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref ordinary,grid,walk,blocked,actual,ref reserved,new int2(16),12,new int2(1),false,out _));
                    }
                    finally{reserved.Dispose();}
                }
                finally{mask.Dispose();}
            });
        }
        [Test]public void ExpandedLaunchMissingBindingRefusesAndLegacyDefaultDoesNotRequireConstraint()
        {
            using var world=new World("TypedSpawnMissingBinding");var em=world.EntityManager;var session=em.CreateEntity();
            var expanded=new SkirmishResolvedSetup{CatalogId="S031",DefinitionId="skirmish.s031",ObjectiveKind=SkirmishObjectiveKind.FrontlineControl};
            Assert.IsFalse(SkirmishTypedSpawnConstraint.TryForLaunch(em,session,expanded,out _));
            var legacy=new SkirmishResolvedSetup{CatalogId="S005",DefinitionId="skirmish.s005",ObjectiveKind=SkirmishObjectiveKind.BaseAssault};
            Assert.IsTrue(SkirmishTypedSpawnConstraint.TryForLaunch(em,session,legacy,out var constraint));Assert.IsNull(constraint);
            WithPad(false,(manager,entity,grid,layout,force)=>
            {
                Assert.IsFalse(SkirmishTypedSpawnConstraint.HasCompleteAllocator(manager,entity,grid));
                using var cache=new SkirmishTypedSpawnConstraint(manager,entity,grid,layout);
                var actor=manager.CreateEntity();
                var typed=typeof(SkirmishScenarioSpawnSystem).GetMethod("TryPlaceTypedOnMap",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                var ordinary=typeof(SkirmishScenarioSpawnSystem).GetMethod("TryPlaceOnMap",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                var position=new Vector3(grid.Origin.x+16,0,grid.Origin.z+16);
                Assert.IsNotNull(typed);
                var typedArguments=new object[]{manager,actor,position,1,force,cache,entity,Entity.Null};
                Assert.IsFalse((bool)typed.Invoke(null,typedArguments),"Typed path must refuse missing buffers before legacy fallback");
                Assert.AreEqual(Entity.Null,(Entity)typedArguments[7],"Rejected placement must not report an actual grid");
                Assert.IsTrue((bool)ordinary.Invoke(null,new object[]{manager,actor,position,1}),"Unconstrained legacy path remains unchanged");
                var walk=manager.AddBuffer<GridWalkable>(entity);walk.ResizeUninitialized(1024);
                using var blocked=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                using var occupied=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                manager.AddComponentData(entity,new DynamicBlockerComponent{Blocked=blocked});
                manager.AddComponentData(entity,new DynamicOccupancyComponent{Occupied=occupied});
                Assert.IsTrue(SkirmishTypedSpawnConstraint.HasCompleteAllocator(manager,entity,grid));
                manager.SetComponentData(entity,new DynamicOccupancyComponent());
                Assert.IsFalse(SkirmishTypedSpawnConstraint.HasCompleteAllocator(manager,entity,grid));
            });
        }
        [Test]public void TerrainFallbackCannotEscapeTypedPadAndLegacyPredicateStillFindsSourceTerrain()
        {
            WithPad(false,(em,entity,grid,layout,force)=>
            {
                using var cache=new SkirmishTypedSpawnConstraint(em,entity,grid,layout);
                using var actual=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                Assert.IsTrue(cache.TryMask(force,actual,out var mask));
                try
                {
                    using var builder=new BlobBuilder(Allocator.Temp);ref var root=ref builder.ConstructRoot<MapSurfaceBlob>();
                    root.CellSize=1;root.GridOrigin=grid.Origin;root.Dimensions=new int2(32);root.RuntimeEncoding=MapSurfaceRuntimeEncoding.Full;
                    var cells=builder.Allocate(ref root.Cells,1024);var samples=builder.Allocate(ref root.Samples,1024);
                    builder.Allocate(ref root.Connections,0);builder.Allocate(ref root.CompactSamples,0);
                    for(int i=0;i<1024;i++)
                    {
                        cells[i]=new MapSurfaceCell{FirstSurfaceIndex=i,SurfaceCount=1};bool denied=!mask.IsSet(i);
                        samples[i]=new MapSurfaceSample{Cell=new int2(i%32,i/32),SurfaceId=i+1,
                            SurfaceType=denied?MapSurfaceType.Blocked:MapSurfaceType.Terrain,
                            MovementMask=denied?MapSurfaceMovementMask.None:MapSurfaceMovementMask.AllGroundUnits,Normal=math.up()};
                    }
                    using var blob=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
                    em.CreateEntity(typeof(SkirmishMatchState));var terrain=em.CreateEntity(typeof(MapSurfaceComponent));
                    em.SetComponentData(terrain,new MapSurfaceComponent{SurfaceBlob=blob,GridOrigin=grid.Origin,Dimensions=new int2(32),CellSize=1,HasSurfaceData=1});
                    var actor=em.CreateEntity(typeof(UnitMovementBehavior));
                    using var walk=new NativeArray<GridWalkable>(1024,Allocator.Persistent);var writeWalk=walk;
                    for(int i=0;i<1024;i++)writeWalk[i]=new GridWalkable{Value=1};
                    using var blocked=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                    var reserved=new NativeBitArray(1024,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                    try
                    {
                        var cell=new int2(16);reserved.Set(16*32+16,true);
                        Assert.IsFalse(SkirmishSpawnPlacement.EnsureTraversable(em,grid,walk,blocked,mask,ref reserved,actor,new int2(1),ref cell));
                        cell=new int2(16);reserved.Set(16*32+16,true);
                        Assert.IsTrue(SkirmishSpawnPlacement.EnsureTraversable(em,grid,walk,blocked,actual,ref reserved,actor,new int2(1),ref cell));
                        Assert.IsTrue(mask.IsSet(cell.y*32+cell.x));
                    }
                    finally{reserved.Dispose();}
                }
                finally{mask.Dispose();}
            });
        }
        [Test]public void HistoricalSeedAndOffsetOutputsAreBitIdenticalForGroundAirAndObjectiveTruck()
        {
            foreach(int seed in new[]{104734,130368,155926})foreach(byte faction in new byte[]{1,2})
            foreach(var role in new[]{SkirmishRoleKind.Rifle,SkirmishRoleKind.TransportHeli,SkirmishRoleKind.ConvoyLogic})
            foreach(int member in new[]{0,1,7})
            {
                var id=new FixedString64Bytes(SkirmishRoleIds.ToId(role)+"."+faction+".3."+member);
                var cell=new int2(73,109);uint historical=2166136261u^unchecked((uint)seed);
                for(int i=0;i<id.Length;i++)historical=unchecked((historical^id[i])*16777619u);
                historical=math.hash(new uint3(unchecked((uint)cell.x),unchecked((uint)cell.y),historical));
                var oracle=new Unity.Mathematics.Random(math.max(1u,historical));var actual=SkirmishTypedSpawnConstraint.PlacementRandom(seed,id,cell);
                for(int i=0;i<5;i++)Assert.AreEqual(oracle.NextUInt(),actual.NextUInt(),"Historical normal RNG changed");
                var expected=role==SkirmishRoleKind.ConvoyLogic?new Vector3(3,0,7):
                    new Vector3(3+(member%4)*2.2f*(faction==2?-1f:1f),0,7-(member/4)*2.2f);
                Assert.AreEqual(expected,SkirmishTypedSpawnConstraint.RequestedWorldOnPad(3,7,faction,role,member));
            }
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishTypedSpawnConstraintTests();t.FullFootprintsAndRotatedQuadRejectCrossingCells();
            t.ExhaustedTypedPadRefusesGlobalFallbackIncludingAirButDefaultSearchStillWorks();t.ExpandedLaunchMissingBindingRefusesAndLegacyDefaultDoesNotRequireConstraint();
            t.TerrainFallbackCannotEscapeTypedPadAndLegacyPredicateStillFindsSourceTerrain();
            t.HistoricalSeedAndOffsetOutputsAreBitIdenticalForGroundAirAndObjectiveTruck();
            Debug.Log("[SkirmishTypedSpawnConstraintTests] result=Passed controls=5 nativeMissionAcceptance=Pending");
        }
    }
}
#endif
