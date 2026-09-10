using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string RoadBarrierKey="Warline.M03.Probe.RoadBarrier";
        private static bool RoadBarrierActive=>SessionState.GetBool(RoadBarrierKey,false);
        private static Mouse roadMouse;
        private static int roadStep,roadNextFrame,roadDragSteps;
        private static Vector2 roadPointer;
        private static RuntimeBuildingEntity roadBarrier;
        private static RectInt roadFootprint;
        private static readonly HashSet<int> roadOriginalBuildings=new();
        private static readonly HashSet<Entity> roadDetours=new(),roadCrossed=new();
        private static bool roadBarrierVerified;
        public static void RunRoadBarrierDefense()=>RunHudRoadValidation();
        private static bool AdvanceRoadBarrierValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!RoadBarrierActive || runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<6000) return false;
            if(Time.frameCount<roadNextFrame) return true;
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if(match==null) return true;
            var command=match.MatchBootstrap.BuildingUiCommandContract;
            var buildings=match.MatchBootstrap.BuildingUiQueryContext.RuntimeBuildings;
            var camera=match.MatchBootstrap.WorldCamera;
            switch(roadStep)
            {
                case -1:
                    using(var requests=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)))
                        em.SetComponentData(requests.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
                        {Requested=1,Smooth=1,SmoothTimeSeconds=.8f,World=new float3(891.5f,0,426.5f)});
                    NextRoadStep(); roadNextFrame=Time.frameCount+180; return true;
                case 0:
                    foreach(var b in buildings) roadOriginalBuildings.Add(b.Key);
                    BeginPaidPlacement(command,"Building_Road_Barrier",6000,15);
                    roadMouse=InputSystem.AddDevice<Mouse>("M3 road barrier QA");
                    NextRoadStep(); return true;
                case 1:
                    var volume=GameObject.Find("PlacementOutline")?.transform.Find("PlacementVolume")?.GetComponent<Renderer>();
                    if(volume==null || camera==null) return true;
                    Vector3 center=volume.bounds.center; center.y=volume.bounds.min.y;
                    roadPointer=camera.WorldToScreenPoint(center);
                    Debug.Log($"[M03RoadBarrierProbe] initial ghost={volume.bounds} camera={camera.name}/{camera.transform.position}/{camera.transform.eulerAngles} pixel={camera.pixelRect} Screen={Screen.width}/{Screen.height} pointer={roadPointer}");
                    SendRoadPointer(roadPointer,false); NextRoadStep(); return true;
                case 2:
                    SendRoadPointer(roadPointer,true); NextRoadStep(); return true;
                case 3:
                    var outline=GameObject.Find("PlacementOutline")?.transform.Find("PlacementVolume")?.GetComponent<Renderer>();
                    if(outline==null || camera==null) return true;
                    roadPointer=camera.WorldToScreenPoint(new Vector3(883.5f,outline.bounds.min.y,426.5f));
                    if(++roadDragSteps>60) throw new InvalidOperationException("Placement drag did not reach the road.");
                    Debug.Log($"[M03RoadBarrierProbe] drag={roadDragSteps} ghost={outline.bounds.center} camera={camera.transform.position} targetScreen={roadPointer}");
                    var pixel=camera.pixelRect;
                    if(roadPointer.x<pixel.xMin+pixel.width*.2f || roadPointer.x>pixel.xMax-pixel.width*.2f || roadPointer.y<pixel.yMin+pixel.height*.2f || roadPointer.y>pixel.yMax-pixel.height*.2f)
                    {
                        roadPointer=new Vector2(Mathf.Clamp(roadPointer.x,pixel.xMin+pixel.width*.2f,pixel.xMax-pixel.width*.2f),Mathf.Clamp(roadPointer.y,pixel.yMin+pixel.height*.2f,pixel.yMax-pixel.height*.2f));
                        SendRoadPointer(roadPointer,true); roadNextFrame=Time.frameCount+8; return true;
                    }
                    SendRoadPointer(roadPointer,true); NextRoadStep(); return true;
                case 4:
                    SendRoadPointer(roadPointer,false); NextRoadStep(); return true;
                case 5:
                    if(!command.CanConfirmBuildingPlacement)
                        throw new InvalidOperationException("Road barrier drag is invalid: "+command.PlacementStatusText);
                    if(!ConfirmPaidPlacement(command)) return true;
                    AssertBudget(em,44000,85); ReleaseRoadMouse(); NextRoadStep(); return true;
                case 6:
                    roadBarrier=buildings.Values.SingleOrDefault(b=>!roadOriginalBuildings.Contains(b.Id) && b.Definition?.Prefab.name=="Building_Road_Barrier");
                    if(roadBarrier==null)
                    {
                        if(facts.ElapsedMilliseconds>45000) throw new InvalidOperationException("Paid road barrier did not finish construction.");
                        return false;
                    }
                    var size=em.Exists(roadBarrier.BlockerEntity) && em.HasComponent<GridBlockerSize>(roadBarrier.BlockerEntity)
                        ? em.GetComponentData<GridBlockerSize>(roadBarrier.BlockerEntity).Size
                        : new int2(roadBarrier.Definition.FootprintCells.x,roadBarrier.Definition.FootprintCells.y);
                    roadFootprint=new RectInt(roadBarrier.OriginCell.x,roadBarrier.OriginCell.y,size.x,size.y);
                    if(!roadFootprint.Contains(new Vector2Int(883,426)))
                        throw new InvalidOperationException("The actual paid footprint missed the convoy road: "+roadFootprint);
                    Debug.Log("[M03RoadBarrierProbe] actual footprint="+roadFootprint+" paid=6000/15 input=mouse-drag-and-confirm");
                    ScreenCapture.CaptureScreenshot(Output+"/road-barrier-built.png");
                    roadStep=7; break;
            }
            if(roadStep<7) return false;
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
            {
                var e=member.Entity;
                if(member.ElementIndex<0 || !Alive(em,e) || !em.HasComponent<UnitMovementBehavior>(e) ||
                    em.GetComponentData<UnitMovementBehavior>(e).UsesVehicleMotion==0) continue;
                var cell=em.GetComponentData<UnitGrid>(e).Cell;
                var size=em.GetComponentData<UnitFootprint>(e).Size;
                var min=UnitFootprintUtility.GetMinCell(cell,size);
                if(cell.x>=roadFootprint.xMin-3 && cell.x<=roadFootprint.xMax+3 &&
                    (min.y+size.y<=roadFootprint.yMin || min.y>=roadFootprint.yMax)) roadDetours.Add(e);
                if(cell.x>=roadFootprint.xMax+5 && roadDetours.Contains(e) && roadCrossed.Add(e))
                    Debug.Log($"[M03RoadBarrierProbe] actual detour element={member.ElementIndex} entity={e} armed={em.GetComponentData<UnitCombat>(e).CanAttack} crossed={cell}");
            }
            if(roadCrossed.Count==3 && !roadBarrierVerified)
            {
                roadBarrierVerified=true;
                Debug.Log("[M03RoadBarrierProbe] result=Passed real paid road obstruction; both armed cars and unarmed APC used actual alternate paths; no position/health/path edits");
                ScreenCapture.CaptureScreenshot(Output+"/road-barrier-detours.png");
            }
            return false;
        }
        private static void NextRoadStep() {roadStep++; roadNextFrame=Time.frameCount+4;}
        private static void SendRoadPointer(Vector2 point,bool pressed)
        {roadMouse.MakeCurrent(); InputSystem.QueueStateEvent(roadMouse,new MouseState {position=point}.WithButton(MouseButton.Left,pressed));}
        private static void ReleaseRoadMouse()
        {if(roadMouse!=null && roadMouse.added) InputSystem.RemoveDevice(roadMouse); roadMouse=null;}
    }
}
