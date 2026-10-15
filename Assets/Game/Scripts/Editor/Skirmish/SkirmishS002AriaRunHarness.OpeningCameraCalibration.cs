#if UNITY_EDITOR
using System;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        static bool openingFramingRequired,openingFramingComplete;
        static int openingFramingStage;
        static string openingFramingCapturePending;
        static double openingFramingDeadline;
        static int openingFramingLastFrame=-1,openingFramingSettledFrames;
        static Vector3 openingFramingPreviousPosition;
        static Quaternion openingFramingPreviousRotation;
        static float openingFramingPreviousFov;
        public static bool OpeningFramingViewsComplete => !openingFramingRequired||openingFramingComplete;

        // Source-frozen dispatch only: preparation/assets/pins happen beforehand.
        // Paid Plane observer remains the sole owner of purchase/flight/Follow.
        public static void RunS005NormalCameraCalibration()
        {
            var profile=SkirmishMissionViewProfile.Load("S005");
            if(profile==null||!profile.IsValid)throw new InvalidOperationException("Prepare and pin the actual logical S005 view profile before this source-frozen run.");
            openingFramingRequired=true;openingFramingComplete=false;openingFramingStage=0;
            openingFramingCapturePending=null;openingFramingLastFrame=-1;openingFramingSettledFrames=0;
            openingFramingDeadline=0;
            StartOpeningDriverDiagnostic();
            RunS005NormalPaidPlaneLineageCanary();
        }

        // Called by the existing exact end-of-frame PNG/raw writer only AFTER
        // exclusive PNG creation and all matching raw rows were persisted.
        static void ObserveS005OpeningCameraCaptureComplete(string phase)
        {
            if(openingFramingCapturePending==phase)openingFramingCapturePending=null;
        }

        // One proposed TickPlayerResumeFrame seam after native terminal checking,
        // before any ordinary setup branch. Existing Touch.Tick/audit runs first.
        // True yields this QA input frame; no ECS, economy, AI or camera writes.
        static bool TickS005OpeningCameraCalibration(EntityManager em,bool hasSession,Entity session)
        {
            ObserveOpeningDriverGate();
            if(!openingFramingRequired||openingFramingComplete)return false;
            if(!hasSession||!SkirmishLaunchProjection.IsSimulationActive(em))return true;
            if(openingFramingDeadline==0)openingFramingDeadline=EditorApplication.timeSinceStartup+180;
            if(EditorApplication.timeSinceStartup>openingFramingDeadline)throw new InvalidOperationException("Normal opening camera calibration timed out.");
            if(playerResumeStep!=0||!Mathf.Approximately(Time.timeScale,1))throw new InvalidOperationException("Opening capture lost its exact normal setup stage.");
            if(!SkirmishMissionViewProfileApplication.TryResolve(em,out var profile))throw new InvalidOperationException("Actual session does not bind the logical S005 profile.");
            if(openingFramingCapturePending!=null||playerResumeTouch==null||playerResumeTouch.IsBusy)return true;
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Actual gameplay camera unavailable.");
            if(Time.frameCount==openingFramingLastFrame)return true;
            openingFramingLastFrame=Time.frameCount;
            bool still=Vector3.Distance(camera.transform.position,openingFramingPreviousPosition)<.01f&&
                Quaternion.Angle(camera.transform.rotation,openingFramingPreviousRotation)<.05f&&Mathf.Abs(camera.fieldOfView-openingFramingPreviousFov)<.01f;
            openingFramingPreviousPosition=camera.transform.position;openingFramingPreviousRotation=camera.transform.rotation;openingFramingPreviousFov=camera.fieldOfView;
            openingFramingSettledFrames=still?openingFramingSettledFrames+1:0;
            if(openingFramingSettledFrames<15)return true; // Includes at least one normal10-frame LOD budget update.
            if(openingFramingStage==0){
                if(Mathf.Abs(camera.transform.position.y-profile.NormalHeight)>.1f||camera.orthographic)
                    throw new InvalidOperationException("Opening view is not the authored default perspective; retain native failure.");
                QueueOpeningFraming("opening-default");openingFramingStage=1;return true;
            }
            var rails=UnityEngine.Object.FindObjectsByType<MatchHudRightQuickRailView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            if(rails.Length!=1||!rails[0].isActiveAndEnabled||rails[0].ZoomInButton==null||rails[0].ZoomOutButton==null)
            {
                WriteOpeningDriverDiagnostic("ZoomRouteUnavailableAfterDefaultCapture");
                throw new InvalidOperationException("Default camera captured; unique discrete quick-rail zoom route unavailable. Actual quick-rail/minimap controls and distinct gesture routes are preserved in opening-driver evidence; no timeout or substituted camera input.");
            }
            var rail=rails[0];
            if(openingFramingStage==1){if(PlayerResumeTap(rail.ZoomOutButton)){openingFramingStage=2;openingFramingSettledFrames=0;}return true;}
            if(openingFramingStage==2){
                // ZoomOut boundary-aware result may be less than authored max.
                // Disabled actual button proves endstop only for control sequencing,
                // never visual acceptance; the raw camera value remains authoritative.
                if(rail.ZoomOutButton==null||rail.ZoomOutButton.interactable)return true;
                QueueOpeningFraming("opening-public-zoom-out-endstop");openingFramingStage=3;return true;
            }
            if(openingFramingStage==3){if(PlayerResumeTap(rail.ZoomInButton)){openingFramingStage=4;openingFramingSettledFrames=0;}return true;}
            if(openingFramingStage==4){
                if(Mathf.Abs(camera.transform.position.y-profile.NormalHeight)>.1f)return true;
                if(PlayerResumeTap(rail.ZoomInButton)){openingFramingStage=5;openingFramingSettledFrames=0;}return true;
            }
            if(openingFramingStage==5){
                if(rail.ZoomInButton==null||rail.ZoomInButton.interactable||Mathf.Abs(camera.transform.position.y-profile.MinHeight)>.1f)return true;
                QueueOpeningFraming("opening-public-zoom-in-min");openingFramingStage=6;return true;
            }
            if(openingFramingStage==6){if(PlayerResumeTap(rail.ZoomOutButton)){openingFramingStage=7;openingFramingSettledFrames=0;}return true;}
            if(openingFramingStage==7){
                if(Mathf.Abs(camera.transform.position.y-profile.NormalHeight)>.1f)return true;
                QueueOpeningFraming("opening-default-restored");openingFramingStage=8;return true;
            }
            openingFramingComplete=true;
            Debug.Log("[S005OpeningCameraCalibration] result=CapturedOnly views=4 normalTouch=1 readabilityCertified=0");
            return false;
        }
        static void QueueOpeningFraming(string phase)
        {
            openingFramingCapturePending=phase;
            QueueNormalPlaneCamera(phase,Entity.Null,0); // Existing same-frame raw/PNG owner, no copied capture path.
        }
    }
}
#endif
