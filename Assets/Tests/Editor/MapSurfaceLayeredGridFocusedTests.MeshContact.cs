using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed partial class MapSurfaceLayeredGridFocusedTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void SpawnAndMovementUseRoadFacesAndAnimationContact(bool vehicle)
    {
        using SurfaceBlobScope scope=CreateSurface(new int2(3,1),FlatCells(3,1),
            new[]{Sample(new int2(0,0),1,0,.813f),Sample(new int2(1,0),2,0,.813f),Sample(new int2(2,0),3,0,.813f)},Array.Empty<MapSurfaceConnection>());
        using World world=new("Road contact integration");var em=world.EntityManager;
        var surface=em.CreateEntity(typeof(MapSurfaceComponent));em.SetComponentData(surface,scope.Surface);
        var mesh=new Mesh{vertices=new[]{new Vector3(0,.45f,0),new Vector3(0,.45f,1),new Vector3(3,.45f,1),new Vector3(3,.45f,0)},triangles=new[]{0,1,2,0,2,3}};
        try
        {
            var authored=new List<MapSurfaceSceneOverlayAuthoringData>();MapSurfaceMeshOverlayBuilder.Append(authored,mesh,Matrix4x4.identity,MapSurfaceType.Road,MapSurfaceMovementMask.AllGroundUnits,MapSurfaceFlags.Road,0);
            MapSurfaceMeshGeometryPresentation.Publish(em,surface,authored.ToArray());
            var overlays=em.AddBuffer<MapSurfaceSceneOverlay>(surface);overlays.Add(authored[0].ToRuntimeOverlay());
            var unit=em.CreateEntity(typeof(UnitSurfaceComponent),typeof(UnitGrid),typeof(UnitMovementBehavior),typeof(LocalTransform),typeof(UnitGroundOffsetComponent));
            em.SetComponentData(unit,new UnitMovementBehavior{UsesVehicleMotion=(byte)(vehicle?1:0)});
            em.SetComponentData(unit,new UnitGroundOffsetComponent{Value=vehicle?0:.1f});
            float contactOffset=vehicle?0:-.022f;
            if(!vehicle)
            {
                em.AddComponentData(unit,new UnitResolvedAnimationIndex{Value=1});var offsets=em.AddBuffer<UnitAnimationGroundOffset>(unit);offsets.Add(default);offsets.Add(new UnitAnimationGroundOffset{Value=contactOffset});
            }
            var tracking=world.CreateSystem<UnitSurfaceTrackingSystem>();var grounding=world.CreateSystem<UnitGroundingSystem>();
            for(int cell=0;cell<3;cell++)
            {
                float3 position=new float3(cell+.5f,4,.5f);em.SetComponentData(unit,LocalTransform.FromPosition(position));em.SetComponentData(unit,new UnitGrid{Cell=new int2(cell,0)});
                tracking.Update(world.Unmanaged);grounding.Update(world.Unmanaged);em.CompleteAllTrackedJobs();
                Assert.That(em.GetComponentData<UnitSurfaceComponent>(unit).LastSampledHeight,Is.EqualTo(.45f).Within(.0001f));
                Assert.That(em.GetComponentData<LocalTransform>(unit).Position.y-contactOffset,Is.EqualTo(.45f).Within(.0001f));
                var grid=new GridConfig{Width=3,Height=1,CellSize=1};
                Assert.That(new MapSurfaceSpawnGrounding().TryGroundWorldPosition(em,grid,ref position,out _,out _,contactOffset),Is.True);
                Assert.That(position.y-contactOffset,Is.EqualTo(.45f).Within(.0001f));
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);}
    }
}
