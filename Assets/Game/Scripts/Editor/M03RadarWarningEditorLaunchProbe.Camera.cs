using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string CameraKey="Warline.M03.Probe.Camera",CameraModeKey="Warline.M03.Probe.CameraMode",CameraWideKey="Warline.M03.Probe.CameraWide";
        private static readonly StringBuilder cameraRows=new();
        private static readonly HashSet<string> cameraFrames=new();
        private static bool cameraActionRequested,cameraPanZoom;
        private static int lastCameraSampleFrame;
        public static void RunCameraValidation()=>RunChecked(()=>StartCameraValidation(0,false));
        public static void RunCameraValidationWide()=>RunChecked(()=>StartCameraValidation(0,true));
        public static void RunCameraSkipValidation()=>RunChecked(()=>StartCameraValidation(1,false));
        public static void RunCameraReducedValidation()=>RunChecked(()=>StartCameraValidation(2,true));
        private static void StartCameraValidation(int mode,bool wide)
        {
            M03RadarWarningConfigBuilder.Build(); M03RadarWarningUiBuilder.Build();
            SessionState.SetBool(CameraKey,true); SessionState.SetInt(CameraModeKey,mode); SessionState.SetBool(CameraWideKey,wide);
            cameraRows.Clear(); cameraRows.AppendLine("frame,opening_ms,stage,focus_x,focus_z,height,pitch,yaw,fov,mission_ms");
            cameraFrames.Clear(); cameraActionRequested=cameraPanZoom=false; lastCameraSampleFrame=-1;
            MainMenuV3PrefabBuilder.SetGameViewResolution(wide ? 2400 : 1920,1080); Run();
        }
        private static bool AdvanceCameraValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(CameraKey,false)) return false;
            if(!em.HasComponent<CampaignMissionCameraTourState>(root)) return true;
            int mode=SessionState.GetInt(CameraModeKey,0);
            var tour=em.GetComponentData<CampaignMissionCameraTourState>(root);
            var opening=em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root);
            if(mode==2 && !cameraActionRequested && UiShellRuntimeGateway.TryReadMissionCameraTour())
                cameraActionRequested=UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.ReduceCameraMotion);
            if(tour.Captured==0) return true;
            using var snapshots=em.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent));
            if(snapshots.CalculateEntityCount()!=1 || !CampaignMissionPatrolOrderSystem.TryCaptureTourStart(snapshots.GetSingleton<RuntimeCameraSnapshotComponent>(),0,out var focus,out var pose))
                throw new InvalidOperationException("Camera snapshot missing or invalid during the M3 tour.");
            string stem="camera-"+(SessionState.GetBool(CameraWideKey,false) ? "20x9" : "16x9")+"-mode"+mode;
            if(Time.frameCount!=lastCameraSampleFrame)
            {
                lastCameraSampleFrame=Time.frameCount;
                cameraRows.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4},{9}\n",
                    Time.frameCount,opening.ElapsedMilliseconds,opening.Stage,focus.x,focus.z,pose.x,pose.y,pose.z,pose.w,facts.ElapsedMilliseconds);
            }
            if(opening.Stage<6 && facts.ElapsedMilliseconds!=0) throw new InvalidOperationException("The cinematic consumed mission time.");
            CaptureCameraFrame(stem,"stage"+opening.Stage);
            float pan=math.distance(focus.xz,tour.StartFocus.xz),zoom=math.abs(pose.x-tour.StartPerspective.x);
            if(opening.Stage==1 && pan>.2f && zoom>.2f)
            {
                cameraPanZoom=true;
                CaptureCameraFrame(stem,"first-moving");
                if(pan>2) CaptureCameraFrame(stem,"mid-post");
            }
            if(opening.Stage==3 && opening.ElapsedMilliseconds-tour.StageStartedAtMilliseconds>900) CaptureCameraFrame(stem,"mid-approach");
            if(opening.Stage==5 && opening.ElapsedMilliseconds-tour.StageStartedAtMilliseconds>900) CaptureCameraFrame(stem,"mid-return");
            if(mode==1 && !cameraActionRequested && opening.Stage==1 && pan>3)
            {
                ClickLive("SkipCameraTour"); cameraActionRequested=true;
            }
            if(runtime.Phase!=MissionPhaseKind.Engage) return true;
            if(math.distance(focus,tour.StartFocus)>.15f || math.cmax(math.abs(pose-tour.StartPerspective))>.12f)
                throw new InvalidOperationException($"Camera did not restore its captured RTS pose. focusError={math.distance(focus,tour.StartFocus)} poseError={pose-tour.StartPerspective}");
            if(mode==0 && (!cameraPanZoom || !cameraFrames.Contains("stage2") || !cameraFrames.Contains("stage4")))
                throw new InvalidOperationException("Missing simultaneous pan/zoom or either authored hold.");
            if(mode!=0 && !cameraActionRequested) throw new InvalidOperationException("Requested skip/reduced-motion path was not exercised.");
            File.WriteAllText(Output+"/"+stem+".csv",cameraRows.ToString());
            CaptureCameraFrame(stem,"returned-rts");
            if(facts.ElapsedMilliseconds<300) return true;
            SessionState.SetBool(CameraKey,false);
            Complete(true,$"camera mode={mode} aspect={(SessionState.GetBool(CameraWideKey,false) ? "20:9" : "16:9")} capturedRTS=restored focusTolerance=.15 poseTolerance=.12 simultaneousPanZoom={cameraPanZoom} openingClock=0 duration={opening.ElapsedMilliseconds}ms");
            return true;
        }
        private static void CaptureCameraFrame(string stem,string frame)
        {
            if(!cameraFrames.Add(frame)) return;
            ScreenCapture.CaptureScreenshot(Output+"/"+stem+"-"+frame+".png");
        }
    }
}
