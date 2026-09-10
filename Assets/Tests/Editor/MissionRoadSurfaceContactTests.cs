using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public sealed class MissionRoadSurfaceContactTests
{
    public static void RunBuildAndChecks()
    {
        try
        {
            Game.Editor.MissionSurfaceContactRepair.Build();
            var tests=new MissionRoadSurfaceContactTests();
            tests.RaisedCurbDoesNotLiftTheWholeRoad();
            tests.MovingContactFollowsSlopedAndRotatedRoadFacesWithoutSteps();
            tests.TriangleBoundsDoNotFillEmptyCorners();
            tests.LegacyRotatedRectanglesStillResolve();
            new MapSurfaceLayeredGridFocusedTests().SpawnAndMovementUseRoadFacesAndAnimationContact(false);
            new MapSurfaceLayeredGridFocusedTests().SpawnAndMovementUseRoadFacesAndAnimationContact(true);
            UnitCharacterGroundingValidationTests.RunLocomotionContactReport();
            Debug.Log("[MissionSurfaceRegression] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch(System.Exception exception){Debug.LogException(exception);Debug.LogError("[MissionSurfaceRegression] result=Failed");ValidationExit.Failed();}
    }

    [Test]
    public void RaisedCurbDoesNotLiftTheWholeRoad()
    {
        var mesh=new Mesh();
        try
        {
            mesh.vertices=new[]{new Vector3(0,.45f,0),new Vector3(0,.45f,10),new Vector3(10,.45f,10),new Vector3(10,.45f,0),
                new Vector3(0,.813f,0),new Vector3(0,.813f,10),new Vector3(1,.813f,10),new Vector3(1,.813f,0)};
            mesh.triangles=new[]{0,1,2,0,2,3,4,5,6,4,6,7};
            var faces=Bake(mesh,Matrix4x4.identity);
            Assert.That(Sample(faces,new float3(5,0,5)),Is.EqualTo(.45f).Within(.0001f));
            Assert.That(Sample(faces,new float3(.5f,0,5)),Is.EqualTo(.813f).Within(.0001f));
            Assert.That(Sample(faces,new float3(10.1f,0,5)),Is.EqualTo(float.NegativeInfinity),"Mesh bounds must not invent support beyond the actual road.");
        }
        finally{Object.DestroyImmediate(mesh);}
    }
    [Test]
    public void MovingContactFollowsSlopedAndRotatedRoadFacesWithoutSteps()
    {
        var mesh=new Mesh();
        try
        {
            mesh.vertices=new[]{new Vector3(0,0,0),new Vector3(0,0,10),new Vector3(10,2,10),new Vector3(10,2,0)};
            mesh.triangles=new[]{0,1,2,0,2,3};
            var matrix=Matrix4x4.TRS(new Vector3(90,3,120),Quaternion.Euler(0,37,0),Vector3.one);
            var faces=Bake(mesh,matrix);
            for(int i=1;i<100;i++)
            {
                float x=i*.1f;Vector3 actual=matrix.MultiplyPoint3x4(new Vector3(x,x*.2f,5));
                Assert.That(Sample(faces,actual),Is.EqualTo(actual.y).Within(.0001f),"Position "+i);
            }
        }
        finally{Object.DestroyImmediate(mesh);}
    }
    [Test]
    public void TriangleBoundsDoNotFillEmptyCorners()
    {
        var mesh=new Mesh();
        try
        {
            mesh.vertices=new[]{Vector3.zero,new Vector3(0,0,10),new Vector3(10,0,0)};mesh.triangles=new[]{0,1,2};
            var faces=Bake(mesh,Matrix4x4.identity);
            Assert.That(Sample(faces,new float3(8,0,8)),Is.EqualTo(float.NegativeInfinity));
            Assert.That(Sample(faces,new float3(2,0,2)),Is.Zero);
            Assert.That(UnsafeUtility.SizeOf<MapSurfaceSceneOverlay>(),Is.LessThanOrEqualTo(80));
        }
        finally{Object.DestroyImmediate(mesh);}
    }
    [Test]
    public void LegacyRotatedRectanglesStillResolve()
    {
        var overlay=new MapSurfaceSceneOverlay{Center=new float3(10,2,20),Height=2,Normal=math.up(),Rotation=quaternion.RotateY(.7f),HalfExtents=new float2(4,1)};
        float3 position=overlay.Center+math.mul(overlay.Rotation,new float3(3,0,.5f));
        Assert.That(MapSurfaceSceneOverlaySampling.TrySample(overlay,position,out float height),Is.True);Assert.That(height,Is.EqualTo(2));
    }
    private static List<MapSurfaceSceneOverlayAuthoringData> Bake(Mesh mesh,Matrix4x4 matrix)
    {
        var faces=new List<MapSurfaceSceneOverlayAuthoringData>();MapSurfaceMeshOverlayBuilder.Append(faces,mesh,matrix,MapSurfaceType.Road,MapSurfaceMovementMask.AllGroundUnits,MapSurfaceFlags.Road,0);return faces;
    }
    private static float Sample(List<MapSurfaceSceneOverlayAuthoringData> faces,float3 position)
    {
        float height=float.NegativeInfinity;
        foreach(var face in faces)
        {
            using var triangles=new NativeArray<MapSurfaceMeshTriangle>(face.Geometry.Triangles,Allocator.Temp);
            using var instances=new NativeArray<MapSurfaceMeshInstance>(new[]{new MapSurfaceMeshInstance{WorldToMesh=face.WorldToMesh,FirstTriangle=0,TriangleCount=triangles.Length}},Allocator.Temp);
            if(MapSurfaceSceneOverlaySampling.TrySample(face.ToRuntimeOverlay(),position,triangles,instances,out float y,out _))height=math.max(height,y);
        }
        return height;
    }
}
