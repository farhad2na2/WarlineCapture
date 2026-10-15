#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        static string openingDriverPath;
        static int openingDriverGates,openingDriverCanvases,openingDriverQueued,openingDriverCoroutineStarted,openingDriverEofResumed,openingDriverPersisted;
        static double openingDriverDue;
        static string openingDriverQueuedPhase;
        static void StartOpeningDriverDiagnostic()
        {
            string output=Environment.GetEnvironmentVariable("WARLINE_S005_PLANE_CANARY_OUTPUT");
            if(string.IsNullOrEmpty(output)||!Path.IsPathRooted(output))throw new InvalidOperationException("Absolute owned Plane output required for diagnostic.");
            openingDriverPath=output+".opening-driver.jsonl";
            using(File.Open(openingDriverPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}
            openingDriverGates=openingDriverCanvases=openingDriverQueued=openingDriverCoroutineStarted=openingDriverEofResumed=openingDriverPersisted=0;
            openingDriverQueuedPhase=null;openingDriverDue=0;
            EditorApplication.update-=TickOpeningDriverDiagnostic;EditorApplication.update+=TickOpeningDriverDiagnostic;
            WriteOpeningDriverDiagnostic("Started");
        }
        static void TickOpeningDriverDiagnostic()
        {
            if(EditorApplication.timeSinceStartup<openingDriverDue)return;
            openingDriverDue=EditorApplication.timeSinceStartup+5;
            WriteOpeningDriverDiagnostic("EditorWatchdog");
            if(!SessionState.GetBool(Key,false)&&!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.update-=TickOpeningDriverDiagnostic;
        }
        static void ObserveOpeningDriverGate(){openingDriverGates++;}
        static void ObserveOpeningDriverCanvas(){openingDriverCanvases++;}
        static void ObserveOpeningDriverCapture(string phase,string point)
        {
            openingDriverQueuedPhase=phase;
            if(point=="Queued")openingDriverQueued++;
            else if(point=="CoroutineBeforeEof")openingDriverCoroutineStarted++;
            else if(point=="CoroutineAfterEof")openingDriverEofResumed++;
            else if(point=="Persisted")openingDriverPersisted++;
            WriteOpeningDriverDiagnostic(point);
        }
        static void WriteOpeningDriverDiagnostic(string point)
        {
            if(string.IsNullOrEmpty(openingDriverPath))return;
            var camera=Camera.main;
            var rails=UnityEngine.Object.FindObjectsByType<Game.UI.Runtime.MatchHudRightQuickRailView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            var windows=Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w=>w.GetType().FullName=="UnityEditor.GameView").Select(w=>new JObject {
                ["instance_id"]=w.GetEntityId().ToString(),["focused"]=EditorWindow.focusedWindow==w,["x"]=w.position.x,["y"]=w.position.y,
                ["width"]=w.position.width,["height"]=w.position.height });
            var row=new JObject { ["schema_version"]=1,["point"]=point,["utc"]=DateTimeOffset.UtcNow.ToString("O"),
                ["editor_time"]=EditorApplication.timeSinceStartup,["frame"]=Time.frameCount,["playing"]=EditorApplication.isPlaying,
                ["play_transition"]=EditorApplication.isPlayingOrWillChangePlaymode,["paused"]=EditorApplication.isPaused,
                ["compiling"]=EditorApplication.isCompiling,["updating"]=EditorApplication.isUpdating,
                ["application_focused"]=Application.isFocused,["run_in_background"]=Application.runInBackground,["game_views"]=new JArray(windows),
                ["opening_required"]=openingFramingRequired,["opening_complete"]=openingFramingComplete,["opening_stage"]=openingFramingStage,
                ["opening_still_frames"]=openingFramingSettledFrames,["opening_last_frame"]=openingFramingLastFrame,
                ["capture_pending"]=openingFramingCapturePending,["queued_phase"]=openingDriverQueuedPhase,
                ["gate_calls"]=openingDriverGates,["canvas_calls"]=openingDriverCanvases,["queued"]=openingDriverQueued,
                ["coroutine_started"]=openingDriverCoroutineStarted,["eof_resumed"]=openingDriverEofResumed,["persisted"]=openingDriverPersisted,
                ["rail_count"]=rails.Length,["rail_active"]=rails.Length==1&&rails[0].isActiveAndEnabled,["actual_zoom_controls"]=OpeningZoomControls(),
                ["touch_present"]=playerResumeTouch!=null,["touch_busy"]=playerResumeTouch!=null&&playerResumeTouch.IsBusy,
                ["player_resume_step"]=playerResumeStep,["plane_canary_active"]=planeCanaryActive,
                ["plane_canary_attempt"]=planeCanaryAttempt??"",["plane_canary_last_frame"]=planeCanaryLastFrame,
                ["plane_canary_ticking"]=planeCanaryTicking,["readability_certified"]=false };
            if(camera!=null)
            {
                row["camera"]=new JObject { ["instance_id"]=camera.GetEntityId().ToString(),["active"]=camera.isActiveAndEnabled,
                    ["position"]=Vector(camera.transform.position),["rotation_euler"]=Vector(camera.transform.eulerAngles),
                    ["fov"]=camera.fieldOfView,["orthographic"]=camera.orthographic,["pixel_width"]=camera.pixelWidth,["pixel_height"]=camera.pixelHeight,
                    ["target_texture"]=camera.targetTexture!=null,["target_display"]=camera.targetDisplay,
                    ["previous_position"]=Vector(openingFramingPreviousPosition),
                    ["position_delta"]=Vector3.Distance(camera.transform.position,openingFramingPreviousPosition),
                    ["rotation_delta"]=Quaternion.Angle(camera.transform.rotation,openingFramingPreviousRotation),
                    ["fov_delta"]=Mathf.Abs(camera.fieldOfView-openingFramingPreviousFov) };
            }
            File.AppendAllText(openingDriverPath,row.ToString(Formatting.None)+"\n",new System.Text.UTF8Encoding(false));
        }
        static JArray OpeningZoomControls()
        {
            var rows=new JArray();
            foreach(var rail in UnityEngine.Object.FindObjectsByType<Game.UI.Runtime.MatchHudRightQuickRailView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                rows.Add(new JObject { ["owner_type"]="MatchHudRightQuickRailView",["owner_active"]=rail.isActiveAndEnabled,
                    ["zoom_in"]=OpeningButton(rail.ZoomInButton),["zoom_out"]=OpeningButton(rail.ZoomOutButton),["gesture_route"]="DiscreteClick" });
            foreach(var minimap in UnityEngine.Object.FindObjectsByType<Game.UI.Runtime.MatchHudMinimapView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                rows.Add(new JObject { ["owner_type"]="MatchHudMinimapView",["owner_active"]=minimap.isActiveAndEnabled,
                    ["zoom_in"]=OpeningButton(minimap.ZoomInButton),["zoom_out"]=OpeningButton(minimap.ZoomOutButton),["gesture_route"]="PointerPressHeld" });
            return rows;
        }
        static JObject OpeningButton(UnityEngine.UI.Button button)
        {
            if(button==null)return new JObject { ["exists"]=false };
            bool presented=TryPresentedButtonPoint(button,out var point);
            return new JObject { ["exists"]=true,["name"]=button.name,["active"]=button.isActiveAndEnabled,
                ["interactable"]=button.interactable,["presented_input_point"]=presented,
                ["point_x"]=presented?(JToken)point.x:JValue.CreateNull(),["point_y"]=presented?(JToken)point.y:JValue.CreateNull() };
        }
        static JArray Vector(Vector3 v)=>new JArray(v.x,v.y,v.z);
    }
}
#endif
