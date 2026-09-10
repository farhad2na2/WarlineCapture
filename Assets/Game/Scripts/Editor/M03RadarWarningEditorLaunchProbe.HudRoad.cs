using System;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string HudRoadKey="Warline.M03.Probe.HudRoad";
        private static int hudRoadStep,hudRoadFrame,steadyWarningFrames;
        private static double hudRoadNext;
        public static void RunHudRoadValidation()=>RunChecked(()=>
        {
            SessionState.SetBool(HudRoadKey,true); hudRoadStep=steadyWarningFrames=0;hudRoadFrame=0;hudRoadNext=0;
            SessionState.SetString("Warline.M03.ReadinessOutput","/private/tmp/warline-m03-hud-road");
            MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);
            Run();
        });
        private static bool AdvanceHudRoadValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(HudRoadKey,false) || hudRoadStep>=13) return false;
            if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<6000 ||
                Time.frameCount<hudRoadFrame || EditorApplication.timeSinceStartup<hudRoadNext) return true;
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(); if(match==null) return true;
            var command=match.MatchBootstrap.BuildingUiCommandContract;
            var camera=match.MatchBootstrap.WorldCamera;
            var volume=GameObject.Find("PlacementOutline")?.transform.Find("PlacementVolume")?.GetComponent<Renderer>();
            switch(hudRoadStep)
            {
                case 0:
                    GameLocalization.SetLocale("fa-IR",false);
                    em.World.GetExistingSystemManaged<RtsCameraRequestSystem>().QueueSetSmoothPerspectiveTarget(
                        em,240,60,camera.transform.eulerAngles.y,60,.6f,true);
                    using(var requests=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)))
                        em.SetComponentData(requests.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
                        {Requested=1,Smooth=1,SmoothTimeSeconds=.8f,World=new float3(900,0,393)});
                    NextHudRoad(2);break;
                case 1:
                    var watch=System.Diagnostics.Stopwatch.StartNew();
                    BeginPaidPlacement(command,"Building_Barrack",40000,90);watch.Stop();
                    if(watch.Elapsed.TotalSeconds>3) throw new InvalidOperationException("Placement opened too slowly: "+watch.Elapsed);
                    Debug.Log("[M03HudRoad] placementOpenMilliseconds="+watch.Elapsed.TotalMilliseconds);
                    em.World.GetExistingSystemManaged<RtsCameraRequestSystem>().QueueSetSmoothPerspectiveTarget(
                        em,240,60,camera.transform.eulerAngles.y,60,.6f,true);
                    roadMouse=InputSystem.AddDevice<Mouse>("M3 road rejection QA");NextHudRoad(2);break;
                case 2:
                    if(volume==null) return true;
                    var previewCenter=volume.bounds.center;previewCenter.y=volume.bounds.min.y;
                    roadPointer=camera.WorldToScreenPoint(previewCenter);SendRoadPointer(roadPointer,false);NextHudRoad();break;
                case 3:
                    SendRoadPointer(roadPointer,true);NextHudRoad();break;
                case 4:
                    if(volume==null) return true;
                    roadPointer=camera.WorldToScreenPoint(new Vector3(883.5f,volume.bounds.min.y,426.5f));
                    SendRoadPointer(roadPointer,true);NextHudRoad();break;
                case 5:
                    SendRoadPointer(roadPointer,false);NextHudRoad();break;
                case 6:
                    if(volume==null) return true;
                    if(!volume.bounds.Contains(new Vector3(883.5f,volume.bounds.center.y,426.5f)))
                        throw new InvalidOperationException("Drag missed the actual road: "+volume.bounds);
                    if(command.CanConfirmBuildingPlacement || command.ConfirmBuildingPlacement())
                        throw new InvalidOperationException("Road placement was accepted.");
                    AssertBudget(em,50000,100);
                    var bar=UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>();
                    if(bar==null || bar.ConfirmButton.interactable) throw new InvalidOperationException("Road preview did not disable Place.");
                    NextHudRoad();break;
                case 7:
                    var alert=UnityEngine.Object.FindObjectsByType<MatchHudThreatVisibilityView>(FindObjectsInactive.Include).Single();
                    if(!alert.gameObject.activeInHierarchy) throw new InvalidOperationException("Alert disappeared during building placement.");
                    if(++steadyWarningFrames<120){hudRoadFrame=Time.frameCount+1;return true;}
                    ScreenCapture.CaptureScreenshot(Output+"/road-rejected-fa-20x9.png");
                    command.RotateBuildingPlacement();NextHudRoad();break;
                case 8:
                    if(command.CanConfirmBuildingPlacement || command.ConfirmBuildingPlacement()) throw new InvalidOperationException("Rotated building accepted on road.");
                    AssertBudget(em,50000,100);command.CancelBuildingPlacement();
                    ReleaseRoadMouse();NextHudRoad();break;
                case 9:
                    BeginPaidPlacement(command,"Building_GuardTower",22000,50);NextHudRoad(2);break;
                case 10:
                    StageLegalPlacementPreview(match.MatchBootstrap);NextHudRoad();break;
                case 11:
                    if(!ConfirmPaidPlacement(command)) throw new InvalidOperationException("Legal tower placement failed: "+command.PlacementStatusText);
                    AssertBudget(em,28000,50);ReleaseRoadMouse();NextHudRoad();break;
                case 12:
                    Debug.Log("[M03HudRoad] result=Passed roadRejected=bothRotations budgetUnchanged=true warningSteadyFrames="+steadyWarningFrames+" legalTowerPaid=true");
                    ScreenCapture.CaptureScreenshot(Output+"/hud-integrated-fa-20x9.png");
                    Complete(true,"real M3 mouse drag; road rejection before and after rotation; no charge on rejection; production-validated tower preview accepts payment through the actual Place button; 120 steady alert frames; integrated Persian HUD");
                    break;
            }
            return true;
        }
        private static void NextHudRoad(double delay=.12)
        {hudRoadStep++;hudRoadFrame=Time.frameCount+4;hudRoadNext=EditorApplication.timeSinceStartup+delay;Debug.Log("[M03HudRoad] step="+hudRoadStep);}

    }
}
