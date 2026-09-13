using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class BuildingPlacementPointerTests
{
    [Test]
    public void CrowdedMissionCenterFindsLegalOuterPlotWithinFixedBudget()
    {
        var bounds=new RectInt(100,200,240,180);
        int calls=0;
        bool Valid(Vector2Int point)
        {
            calls++; Assert.That(bounds.Contains(point),Is.True);
            return point.x>=300 && point.y>=330;
        }
        Assert.That(BuildingPlacementOriginSearch.TryFindAcrossBounds(bounds,new Vector2Int(220,290),Valid,out var result),Is.True);
        Assert.That(result.x,Is.GreaterThanOrEqualTo(300));
        Assert.That(result.y,Is.GreaterThanOrEqualTo(330));
        Assert.That(calls,Is.LessThanOrEqualTo(169));
        calls=0;
        Assert.That(BuildingPlacementOriginSearch.TryFindAcrossBounds(new RectInt(0,0,4000,4000),Vector2Int.zero,
            _=>{calls++;return false;},out _),Is.False);
        Assert.That(calls,Is.LessThanOrEqualTo(169),"No unbounded map scan when every plot is blocked.");
    }

    [Test]
    public void TapAndDragReachNewPlotAndReleaseUsesFinalPosition()
    {
        var input=new BuildingPlacementInputUiSystemHelper();
        var placement=new BuildingPlacementLifecycleCompositionSystemHelper.PlacementState
        {
            Definition=new BuildingDefinition {FootprintCells=new Vector2Int(2,2)},
            OriginCell=Vector2Int.zero, LastPointerMovedAt=-10
        };
        GridConfig grid=default;
        bool ReadGrid(out GridConfig value) {value=grid;return true;}
        bool ReadCell(Vector2 screen,GridConfig unused,out Vector2Int cell)
        {cell=Vector2Int.FloorToInt(screen);return true;}
        void Update(Vector2 point)
        {
            Assert.That(input.ApplyPointerHover(placement,input.ShouldUpdateCellFromPointer,point,grid,10,ReadCell,
                BuildingPlacementGridCameraSystemHelper.CenterCellToOrigin),Is.False,"A paused finger must never request camera follow.");
            placement.IsValid=placement.OriginCell==new Vector2Int(19,29);
        }
        var context=new BuildingPlacementInputUiSystemHelper.ActivePlacementPointerContext(ReadGrid,ReadCell,
            BuildingPlacementGridCameraSystemHelper.CenterCellToOrigin,null,_=>false,_=>false,Update);
        input.UpdateActivePlacementPointer(placement,new GamePointerState(new Vector2(10,10),true,true,false),context);
        Assert.That(placement.OriginCell,Is.EqualTo(new Vector2Int(9,9)),"World tap can relocate an invalid preview.");
        input.UpdateActivePlacementPointer(placement,new GamePointerState(new Vector2(15,15),true,false,false),context);
        input.UpdateActivePlacementPointer(placement,new GamePointerState(new Vector2(20,30),false,false,true),context);
        Assert.That(placement.OriginCell,Is.EqualTo(new Vector2Int(19,29)));
        Assert.That(placement.IsValid,Is.True);
        Assert.That(input.IsDraggingPlacement,Is.False);
        input.UpdateActivePlacementPointer(placement,new GamePointerState(new Vector2(50,50),false,false,false),context);
        Assert.That(placement.OriginCell,Is.EqualTo(new Vector2Int(19,29)),"Hover after release must not move a valid preview.");
    }

    [Test]
    public void PlacementUiPressDoesNotMovePreview()
    {
        var input=new BuildingPlacementInputUiSystemHelper();
        var placement=new BuildingPlacementLifecycleCompositionSystemHelper.PlacementState
        {Definition=new BuildingDefinition {FootprintCells=Vector2Int.one},OriginCell=new Vector2Int(4,5)};
        bool Cell(Vector2 point,GridConfig grid,out Vector2Int cell) {cell=new Vector2Int(30,30);return true;}
        input.TryBeginDrag(placement,Vector2.zero,true,false,true,default,Cell,BuildingPlacementGridCameraSystemHelper.CenterCellToOrigin);
        Assert.That(input.IsDraggingPlacement,Is.False);
        Assert.That(placement.OriginCell,Is.EqualTo(new Vector2Int(4,5)));
    }
}
