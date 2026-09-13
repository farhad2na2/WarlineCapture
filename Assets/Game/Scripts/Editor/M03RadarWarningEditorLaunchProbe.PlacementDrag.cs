using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static int placementDragPhase;
        private static double placementDragAt;
        private static Vector2 placementDragFrom, placementDragTo;
        private static Vector2Int placementDragExpected;
        private static Vector3 placementDragCamera;
        private static Quaternion placementDragRotation;
        private static Mouse placementDragMouse;

        private static bool AdvancePlacementPointerDrag(MatchBootstrapCompositionSystemHelper bootstrap)
        {
            var source=FindPlacementSource(bootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,new HashSet<object>(),0);
            var placement=source?.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
            if(placement==null || !ReadGrid(source,out _,out var grid,out var roads,out var blockers))
                throw new InvalidOperationException("Missing live placement for pointer QA.");
            var camera=source.BuildingPlacementStartupSystemHelper.WorldCamera;
            double now=EditorApplication.timeSinceStartup;
            if(placementDragPhase==0)
            {
                Vector2Int initial=placement.OriginCell, footprint=placement.Definition.FootprintCells;
                bool found=false;
                for(int radius=8;radius<=180 && !found;radius++)
                    for(int y=initial.y-radius;y<=initial.y+radius && !found;y++)
                        for(int x=initial.x-radius;x<=initial.x+radius;x++)
                        {
                            if(x!=initial.x-radius && x!=initial.x+radius && y!=initial.y-radius && y!=initial.y+radius) continue;
                            var origin=new Vector2Int(x,y);
                            Vector3 center=source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin,footprint,grid,source.BuildingPlacementStartupSystemHelper.BuildPlaneY);
                            center+=new Vector3(grid.CellSize*.25f,0,grid.CellSize*.25f);
                            Vector3 screen=camera.WorldToViewportPoint(center);
                            if(screen.z<=0 || screen.x<.12f || screen.x>.68f || screen.y<.30f || screen.y>.85f) continue;
                            if(!source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source,placement.Definition,origin,footprint,false,grid,roads,blockers,EffectiveRect,OverlapsOccupant)) continue;
                            placementDragTo=camera.WorldToScreenPoint(center); placementDragExpected=origin; found=true; break;
                        }
                if(!found) throw new InvalidOperationException("No visible valid plot reachable by dragging.");
                placementDragFrom=camera.WorldToScreenPoint(source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(initial,footprint,grid,source.BuildingPlacementStartupSystemHelper.BuildPlaneY));
                placementDragMouse=InputSystem.AddDevice<Mouse>("PlacementQaMouse");
                QueuePlacementPointer(placementDragFrom,true);
                placementDragPhase=1; placementDragAt=now; return false;
            }
            if(placementDragPhase==1)
            {
                // Snapshot after the press has cancelled any entry camera transition.
                placementDragCamera=camera.transform.position; placementDragRotation=camera.transform.rotation;
                placementDragAt=now; placementDragPhase=2;
            }
            if(Vector3.Distance(placementDragCamera,camera.transform.position)>.03f || Quaternion.Angle(placementDragRotation,camera.transform.rotation)>.03f)
                throw new InvalidOperationException("Camera moved during placement drag/hold.");
            if(placementDragPhase==2)
            {
                float t=Mathf.Clamp01((float)(now-placementDragAt));
                QueuePlacementPointer(Vector2.Lerp(placementDragFrom,placementDragTo,t),true);
                if(t>=1) {placementDragPhase=3;placementDragAt=now;}
                return false;
            }
            if(placementDragPhase==3)
            {
                QueuePlacementPointer(placementDragTo,true);
                if(now-placementDragAt<1.5) return false;
                QueuePlacementPointer(placementDragTo,false); placementDragPhase=4; return false;
            }
            if(placement.OriginCell!=placementDragExpected)
                throw new InvalidOperationException("Pointer did not move preview to intended plot: "+placement.OriginCell+" expected "+placementDragExpected);
            if(!placement.IsValid || !bootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement())
                throw new InvalidOperationException("Pointer drag did not end on a confirmable green footprint.");
            Debug.Log("[M03PlacementDrag] result=Passed real pointer drag + 1.5s hold + release; camera stable; green footprint confirmable.");
            InputSystem.RemoveDevice(placementDragMouse); placementDragMouse=null;
            return true;
        }

        private static void QueuePlacementPointer(Vector2 position,bool pressed)
        {
            placementDragMouse.MakeCurrent();
            var state=new MouseState {position=position};
            if(pressed) state=state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(placementDragMouse,state);
        }
    }
}
