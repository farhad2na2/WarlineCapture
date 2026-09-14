using System;
using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        public static void RunRoadGateTutorial()
        {
            SessionState.SetBool("Warline.M03.Probe.RoadGateTutorial",true);
            RunTutorialBuildClicks();
        }

        private static bool IsHorizontalRoadGateTarget(BuildingGameplaySourceCompositionSystemHelper source,
            GridConfig grid,DynamicBuffer<GridRoad> roads,Vector2Int origin,Vector2Int footprint)
        {
            var center=origin+new Vector2Int(footprint.x/2,footprint.y/2);
            bool IsRoad(Vector2Int cell) => cell.x>=0 && cell.y>=0 && cell.x<grid.Width && cell.y<grid.Height &&
                (roads[cell.y*grid.Width+cell.x].Value!=0 || source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.HasRoadInFootprint(
                    source.BuildingPlacementStartupSystemHelper,grid,cell,Vector2Int.one));
            // Cell row 426 is the authored west-road convoy lane, not its sidewalk.
            return center.y==426 && IsRoad(center) && BuildingPlacementVisualUpdateCompositionSystemHelper.TryResolveRoadGateRotation(center,IsRoad,out bool vertical) && vertical;
        }

        private static void DumpRoadGateCandidates(BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementLifecycleCompositionSystemHelper.PlacementState placement,
            GridConfig grid,DynamicBuffer<GridRoad> roads,DynamicBlockerComponent blockers,Camera camera)
        {
            var footprint=source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(placement.Definition,true);
            if(source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out var em))
            {
                using var q=em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo),typeof(OperationMapBuildingComponent));
                using var entities=q.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach(var entity in entities)
                {
                    var b=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                    if(new RectInt(b.OriginCell.x,b.OriginCell.y,b.FootprintCells.x,b.FootprintCells.y).Overlaps(new RectInt(870,422,120,8)))
                        Debug.Log("[RoadGateMapOccupant] entity="+entity+" origin="+b.OriginCell+" size="+b.FootprintCells+" identity="+em.GetComponentData<OperationMapBuildingComponent>(entity).StableId);
                }
            }
            Debug.Log("[RoadGateModel] grid="+grid.CellSize+" bounds="+placement.PreviewInstance.GetComponentInChildren<Renderer>().bounds);
            for(int x=935;x<=980;x+=5)
            {
                var origin=new Vector2Int(x-footprint.x/2,426-footprint.y/2);
                var center=source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin,footprint,grid,0);
                var viewport=camera.WorldToViewportPoint(center);
                int count=0; string cells="";
                var rect=EffectiveRect(source,placement.Definition,origin,grid,true);
                for(int y=rect.yMin;y<rect.yMax;y++) for(int bx=rect.xMin;bx<rect.xMax;bx++)
                    if(blockers.Blocked.IsSet(y*grid.Width+bx)) {count++; if(count<5) cells+=bx+","+y+";";}
                Debug.Log("[RoadGateObstacle] x="+x+" policy="+CampaignMissionBuildingPlacementPolicy.IsAllowed(source,placement.Definition,new RectInt(origin,footprint))+" blocked="+count+" cells="+cells+" effective="+rect);
                bool valid=source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source,placement.Definition,origin,footprint,true,grid,roads,blockers,EffectiveRect,OverlapsOccupant);
                Debug.Log("[RoadGateCandidate] cell="+x+",426 viewport="+viewport+" roadAlignment="+IsHorizontalRoadGateTarget(source,grid,roads,origin,footprint)+" valid="+valid+" footprint="+footprint);
            }
        }

        private static void AssertRoadGatePlacement(BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementLifecycleCompositionSystemHelper.PlacementState placement,
            GridConfig grid,DynamicBuffer<GridRoad> roads,DynamicBlockerComponent blockers)
        {
            if(!placement.AutoRotateVertical || Quaternion.Angle(placement.PreviewInstance.transform.rotation,Quaternion.Euler(0,90,0))>.1f)
                throw new InvalidOperationException("Road gate did not rotate across the east-west road.");
            var footprint=source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(placement.Definition,true);
            if(!IsHorizontalRoadGateTarget(source,grid,roads,placement.OriginCell,footprint))
                throw new InvalidOperationException("Gate test must use an actual road, not open land.");
            var barracks=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Barrack.prefab");
            var ordinary=new BuildingDefinition {Prefab=barracks,FootprintCells=footprint};
            if(source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.IsPlacementValid(ordinary,placement.OriginCell,footprint,true,
                grid,roads,blockers,source.BuildingGameplayDependencyCompositionSystemHelper,source.BuildingPlacementStartupSystemHelper,
                (_,origin,_,_)=>new RectInt(origin,footprint),_=>false))
                throw new InvalidOperationException("Ordinary building incorrectly accepted the gate's road plot.");
            var renderers=placement.PreviewInstance.GetComponentsInChildren<Renderer>();
            var modelBounds=renderers[0].bounds;
            foreach(var renderer in renderers) modelBounds.Encapsulate(renderer.bounds);
            var center=source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(placement.OriginCell,footprint,grid,0);
            if(Mathf.Abs(modelBounds.center.x-center.x)>2 || Mathf.Abs(modelBounds.center.z-center.z)>2)
                throw new InvalidOperationException("Gate artwork is not centered on the chosen road plot: "+modelBounds+" expected "+center);
            var bar=UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>();
            bar.RotateButton.onClick.Invoke();
            if(placement.AutoRotateVertical || Quaternion.Angle(placement.PreviewInstance.transform.rotation,Quaternion.identity)>.1f)
                throw new InvalidOperationException("Manual Rotate was overridden.");
            bar.RotateButton.onClick.Invoke();
            if(!placement.AutoRotateVertical || !placement.IsValid)
                throw new InvalidOperationException("Manual Rotate back to the road crossing did not restore green placement.");
            Debug.Log("[M03RoadGateTutorial] result=Passed convoy lane=426 green yaw=90 manual-rotate=both-directions model-center="+modelBounds.center+" ordinary-building=blocked tutorial=real-clicks");
        }
    }
}
