using System;
using Game.Components;
using Game.Runtime;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEditor;

public sealed class RoadGatePlacementValidation
{
    public static void Run()
    {
        try
        {
            var tests = new RoadGatePlacementValidation();
            tests.CanonicalGateFootprintContainsClosedGeometry();
            tests.OnlyRoadGateCanOverlapRoadAndStillRejectsObstacles();
            tests.RoadCrossingRotationAndManualOverrideAreConsistent();
            tests.RotatedGateDragKeepsItsCenterUnderThePointer();
            Debug.Log("[RoadGatePlacement] result=Passed road-exception=gate-only blockers=preserved rotation=90,manual pointer=rotated-footprint");
            BuildingBarrierUtilitySystemHelperTests.RunFocusedValidation();
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); BuildingPlacementConstructionTransactionTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Construction transaction regression.");
                ValidationExit.ClearLastExitCode(); BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Placement command regression.");
                ValidationExit.ClearLastExitCode(); MissionReadinessArchitectureValidation.Run();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Architecture regression.");
            }
            M03RadarWarningEditorLaunchProbe.RunRoadGateTutorial();
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [Test]
    public void CanonicalGateFootprintContainsClosedGeometry()
    {
        var root = PrefabUtility.LoadPrefabContents(RoadGatePlacementAssetRepair.PrefabPath);
        try
        {
            var configured = root.GetComponent<Game.Authoring.BuildingDefinitionAuthoring>().ConfiguredFootprintCells;
            Assert.AreEqual(new Vector2Int(8,2), configured, "Road gate must not inherit a building-sized reservation.");
            var arm = Array.Find(root.GetComponentsInChildren<Transform>(true), item => item.name == "Door_Z");
            var euler = arm.localEulerAngles; euler.z=0; arm.localEulerAngles=euler;
            Assert.IsTrue(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(root,out var bounds));
            Assert.LessOrEqual(bounds.size.x,configured.x);
            Assert.LessOrEqual(bounds.size.z,configured.y);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [Test]
    public void OnlyRoadGateCanOverlapRoadAndStillRejectsObstacles()
    {
        using var world = new World("Gate road permission");
        var em = world.EntityManager; var entity = em.CreateEntity();
        var roads = em.AddBuffer<GridRoad>(entity); roads.ResizeUninitialized(400);
        for (int i=0;i<400;i++) roads[i] = new GridRoad {Value=1};
        var grid = new GridConfig {Width=20,Height=20,CellSize=1};
        using var blocked = new NativeBitArray(400,Allocator.Temp);
        var blockers = new DynamicBlockerComponent {GridSize=400,Blocked=blocked};
        int[] prefix = null;
        BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid,roads,blockers,null,null,
            ref prefix,out int w,out int h,out bool ready);
        var rect = new RectInt(4,4,3,3);
        bool Valid(bool gate,bool occupied=false) => BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
            rect,grid,roads,blockers,ready,prefix,w,h,null,(_,_,_)=>true,_=>occupied,gate);
        Assert.IsFalse(Valid(false),"Buildings may never overlap roads.");
        Assert.IsTrue(Valid(true),"Gate may overlap road even when cached invalid prefix includes it.");
        Assert.IsFalse(Valid(true,true),"Gate may not overlap another building.");
        blocked.Set(5*20+5,true);
        Assert.IsFalse(Valid(true),"Gate may not ignore physical blockers on a road.");
        blocked.Set(5*20+5,false);
        var cache = new BuildingPlacementInvalidCellCacheCompositionSystemHelper();
        var gatePrefab = new GameObject("Building_Road_Barrier");
        var barracksPrefab = new GameObject("Building_Barrack");
        try
        {
            foreach (var prefab in new[]{gatePrefab,barracksPrefab})
                Assert.AreEqual(prefab==gatePrefab,cache.IsPlacementValid(new BuildingDefinition {Prefab=prefab},
                    rect.position,rect.size,false,grid,roads,blockers,new BuildingGameplayDependencyCompositionSystemHelper(),
                    new BuildingPlacementStartupSystemHelper(),(_,_,_,_)=>rect,_=>false),prefab.name);
        }
        finally {UnityEngine.Object.DestroyImmediate(gatePrefab);UnityEngine.Object.DestroyImmediate(barracksPrefab);}
    }

    [Test]
    public void RoadCrossingRotationAndManualOverrideAreConsistent()
    {
        Assert.IsTrue(BuildingPlacementVisualUpdateCompositionSystemHelper.TryResolveRoadGateRotation(
            new Vector2Int(50,50),cell=>cell.y>=46 && cell.y<=54,out bool vertical));
        Assert.IsTrue(vertical,"East-west road needs the gate rotated 90 degrees across it.");
        Assert.IsTrue(BuildingPlacementVisualUpdateCompositionSystemHelper.TryResolveRoadGateRotation(
            new Vector2Int(50,50),cell=>cell.x>=46 && cell.x<=54,out vertical));
        Assert.IsFalse(vertical,"North-south road needs the unrotated gate across it.");
        var state = new BuildingPlacementLifecycleCompositionSystemHelper.PlacementState
            {Definition=new BuildingDefinition(),ManualRotation=true,AutoRotateVertical=true};
        Assert.IsTrue(new BuildingBarrierUtilitySystemHelper().ResolvePlacementRotateVertical(default,null,state));
        state.AutoRotateVertical=false;
        Assert.IsFalse(new BuildingBarrierUtilitySystemHelper().ResolvePlacementRotateVertical(default,null,state));
    }

    [Test]
    public void RotatedGateDragKeepsItsCenterUnderThePointer()
    {
        var state = new BuildingPlacementLifecycleCompositionSystemHelper.PlacementState
            {Definition=new BuildingDefinition{FootprintCells=new Vector2Int(20,10)},AutoRotateVertical=true};
        var input = new BuildingPlacementInputUiSystemHelper();
        bool Cell(Vector2 pointer,GridConfig _,out Vector2Int cell) {cell=Vector2Int.FloorToInt(pointer);return true;}
        input.TryBeginDrag(state,new Vector2(50,50),false,false,true,default,Cell,BuildingPlacementGridCameraSystemHelper.CenterCellToOrigin);
        Assert.AreEqual(new Vector2Int(45,40),state.OriginCell);
    }
}
