using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;

public sealed class MissionHudRoadRegressionTests
{
    [Test]
    public void AuthoredMeshRoadBlocksPlacementEvenWithAnEmptyPlayerRoadBuffer()
    {
        using var world=new World("Authored roads");
        var em=world.EntityManager;var entity=em.CreateEntity();
        em.AddBuffer<MapSurfaceMeshTriangle>(entity).Add(new MapSurfaceMeshTriangle{A=new float3(2,0,2),B=new float3(2,0,8),C=new float3(8,0,2)});
        em.AddBuffer<MapSurfaceMeshInstance>(entity).Add(new MapSurfaceMeshInstance{WorldToMesh=float4x4.identity,FirstTriangle=0,TriangleCount=1});
        em.AddBuffer<MapSurfaceSceneOverlay>(entity).Add(new MapSurfaceSceneOverlay{Flags=MapSurfaceFlags.Road|MapSurfaceFlags.ExactMesh,SurfaceType=MapSurfaceType.Road,GeometryInstanceIndex=0});
        var grid=new GridConfig{Width=12,Height=12,CellSize=1};var mask=new bool[144];
        BuildingPlacementRoadSurfaceMask.Append(em,entity,grid,mask);
        var roads=em.AddBuffer<GridRoad>(entity);roads.ResizeUninitialized(144);
        for(int i=0;i<roads.Length;i++)roads[i]=default;
        int[] prefix=null;
        BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid,roads,default,mask,null,ref prefix,out int w,out int h,out bool ready);
        Assert.IsTrue(ready);
        Assert.IsFalse(Valid(new RectInt(3,3,2,2)),"Barracks wholly over a baked road must be invalid.");
        Assert.IsFalse(Valid(new RectInt(0,3,3,2)),"Even the outer edge of a building must clear the road.");
        Assert.IsTrue(Valid(new RectInt(8,8,2,2)),"Do not block empty corners of a triangle's bounds.");
        bool Valid(RectInt rect)=>BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(rect,grid,roads,default,true,prefix,w,h,null,null,null);
    }

    [Test]
    public void BlockedRoadCandidatesDoNotScanTheCityBuildingCollection()
    {
        using var world=new World("Road rejection cost");var em=world.EntityManager;var e=em.CreateEntity();
        var roads=em.AddBuffer<GridRoad>(e);roads.Add(new GridRoad{Value=1});
        bool valid=BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(new RectInt(0,0,1,1),
            new GridConfig{Width=1,Height=1,CellSize=1},roads,default,false,null,0,0,null,null,
            _=>{Assert.Fail("A road candidate must not scan runtime buildings.");return false;});
        Assert.IsFalse(valid);
    }

    [Test]
    public void RoadCacheSurvivesTheEndOfBulkStartupAndRefreshesWithTheMapRevision()
    {
        using var world=new World("Road cache lifecycle");var em=world.EntityManager;
        var entity=em.CreateEntity(typeof(MapSurfaceComponent),typeof(MapSurfaceSceneOverlayRevision));
        em.AddBuffer<MapSurfaceSceneOverlay>(entity).Add(new MapSurfaceSceneOverlay
            {Center=new float3(4,0,4),HalfExtents=new float2(1),SurfaceType=MapSurfaceType.Road});
        using var query=em.CreateEntityQuery(typeof(MapSurfaceComponent));
        var cache=new BuildingPlacementInvalidCellCacheCompositionSystemHelper();
        var grid=new GridConfig{Width=12,Height=12,CellSize=1};
        cache.RoadSurfaces.Ensure(em,query,grid);
        cache.Clear(); // This is called when startup's deferred-building transaction ends.
        Assert.IsTrue(cache.RoadSurfaces.Overlaps(grid,new Vector2Int(3,3),new Vector2Int(2,2)));
        var overlays=em.GetBuffer<MapSurfaceSceneOverlay>(entity);var overlay=overlays[0];overlay.Center=new float3(9,0,9);
        overlays[0]=overlay;
        em.SetComponentData(entity,new MapSurfaceSceneOverlayRevision{Value=1});
        cache.RoadSurfaces.Ensure(em,query,grid);
        Assert.IsFalse(cache.RoadSurfaces.Overlaps(grid,new Vector2Int(3,3),new Vector2Int(2,2)));
        Assert.IsTrue(cache.RoadSurfaces.Overlaps(grid,new Vector2Int(8,8),new Vector2Int(2,2)));
        em.DestroyEntity(entity);cache.RoadSurfaces.Ensure(em,query,grid);
        Assert.IsFalse(cache.RoadSurfaces.Overlaps(grid,new Vector2Int(8,8),new Vector2Int(2,2)));
    }

    [TestCase(0)] [TestCase(37)] [TestCase(90)]
    public void ThinRotatedRoadEdgesCannotFallBetweenPlacementCellCentres(float angle)
    {
        var grid=new GridConfig{Width=20,Height=20,CellSize=1};var mask=new bool[400];
        using var triangles=new NativeArray<MapSurfaceMeshTriangle>(new[]{new MapSurfaceMeshTriangle
            {A=new float3(0,0,0),B=new float3(0,0,4),C=new float3(.15f,0,0)}},Allocator.Temp);
        var matrix=float4x4.TRS(new float3(8.1f,0,8.1f),quaternion.RotateY(math.radians(angle)),new float3(1));
        using var instances=new NativeArray<MapSurfaceMeshInstance>(new[]{new MapSurfaceMeshInstance{WorldToMesh=math.inverse(matrix),TriangleCount=1}},Allocator.Temp);
        BuildingPlacementRoadSurfaceMask.AppendOverlay(new MapSurfaceSceneOverlay{SurfaceType=MapSurfaceType.Road,Flags=MapSurfaceFlags.ExactMesh},triangles,instances,grid,mask);
        Assert.IsTrue(mask[8*20+8],"Any positive-area road overlap must be blocked, including a sub-cell road edge.");
    }

    [Test]
    public void DeliveredMissionAndSelectionControlsHaveLargeNonOverlappingTapTargets()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        var canvasRoot=new GameObject("Mobile HUD canvas",typeof(RectTransform),typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        ((RectTransform)canvasRoot.transform).sizeDelta=new Vector2(1920,1080);
        var instance=Object.Instantiate(prefab,canvasRoot.transform);
        try
        {
            instance.GetComponentInChildren<MissionHudTouchLayoutView>(true).Apply(true);
            foreach(string name in new[]{"FieldGuide","SkipLesson","ReturnWarningCamera","SkipCameraTour","PassengerChip","DoItButton","ShowMeButton"})
            {
                var button=instance.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
                var size=((RectTransform)button.transform).rect.size;
                Assert.GreaterOrEqual(size.x,72,name+" tap width");Assert.GreaterOrEqual(size.y,72,name+" tap height");
            }
            var aria=instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
            var actions=(RectTransform)aria.transform.Find("M03Actions");var map=(RectTransform)aria.transform.Find("MinimapPanel");
            Canvas.ForceUpdateCanvases();
            var a=new Vector3[4];var b=new Vector3[4];actions.GetWorldCorners(a);map.GetWorldCorners(b);
            Assert.Less(b[1].y,a[0].y,"Minimap must leave the large guide row clear.");
            Assert.GreaterOrEqual(((RectTransform)instance.GetComponentInChildren<MatchHudSelectionPanelView>(true).CommandWheelOpenButton.transform).rect.height,100,
                "The entire portrait remains a generous Commands tap target.");
            var extraction=instance.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="M04Actions");
            foreach(var button in extraction.GetComponentsInChildren<Button>(true))
                Assert.GreaterOrEqual(((RectTransform)button.transform).rect.height,72,"M4: "+button.name);
            var cue=(RectTransform)instance.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="CommandWheelCue");
            ((RectTransform)cue.parent).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,278);
            Assert.That(cue.rect.width,Is.EqualTo(268).Within(.1),"Commands must also fit the compact passenger layout.");
        }
        finally{Object.DestroyImmediate(canvasRoot);}
        var guide=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Popups/M03_FieldGuide.prefab");
        foreach(var button in guide.GetComponentsInChildren<Button>(true))
            Assert.GreaterOrEqual(((RectTransform)button.transform).rect.height,72,"Guide: "+button.name);
    }

    [Test]
    public void AlertRefreshCannotOverridePopupSuppressionOrResurrectAnExpiredAlert()
    {
        var root=new GameObject("Alert",typeof(RectTransform));var popup=new GameObject("Popup");var wheel=new GameObject("Wheel");
        try
        {
            var view=root.AddComponent<MatchHudThreatVisibilityView>();view.SetRequested(true);
            Assert.IsTrue(root.activeSelf);
            view.SetSuppressed(popup,true);view.SetSuppressed(wheel,true);
            for(int frame=0;frame<120;frame++){view.SetRequested(true);Assert.IsFalse(root.activeSelf,"Refresh must not flash a suppressed warning.");}
            view.SetSuppressed(popup,false);Assert.IsFalse(root.activeSelf);
            view.SetSuppressed(wheel,false);Assert.IsTrue(root.activeSelf);
            view.SetSuppressed(popup,true);view.SetRequested(false);view.SetSuppressed(popup,false);
            Assert.IsFalse(root.activeSelf,"Closing a popup cannot restore an expired warning.");
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(popup);Object.DestroyImmediate(wheel);}
    }
}
