#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        static bool s073ReferenceArmed, s073ReferenceCaptured;
        static double s073ReferenceDeadline;
        static double s073ReferenceStartedAt, s073ReferenceNextStatusAt;
        static string s073ReferencePath;
        static int s073ReferenceExit;

        public static void RunS073ProductionCameraReference()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || s073ReferenceArmed)
                throw new InvalidOperationException("Idle Editor and unarmed diagnostic required");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            s073ReferencePath=Path.GetFullPath(Environment.GetEnvironmentVariable("WARLINE_S073_CAMERA_REFERENCE") ?? "");
            string allowed=Path.Combine(root,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar;
            if (!s073ReferencePath.StartsWith(allowed,StringComparison.Ordinal) || File.Exists(s073ReferencePath))
                throw new InvalidDataException("Fresh S073 reference output under Logs/SkirmishProduction required");
            s073ReferenceArmed=true; s073ReferenceCaptured=false; s073ReferenceExit=2;
            s073ReferenceStartedAt=EditorApplication.timeSinceStartup;
            s073ReferenceNextStatusAt=s073ReferenceStartedAt+20d;
            s073ReferenceDeadline=s073ReferenceStartedAt+480d;
            BuildingRuntimeProcessingCompositionSystemHelper.EditorBeforeProductionRequest=ObserveS073ProductionCameraReference;
            EditorApplication.update+=PollS073ProductionCameraReference;
            EditorApplication.playModeStateChanged+=FinishS073ProductionCameraReference;
            try { RunEditorJourney("en",false,false,"S073",104731); }
            catch { DisarmS073ProductionCameraReference(); throw; }
        }

        static void ObserveS073ProductionCameraReference(EntityManager em, Entity boundary, BuildingFactionUnitProductionRequest request, float now)
        {
            if (!s073ReferenceArmed || s073ReferenceCaptured || request.FactionId!=2 ||
                !string.Equals(request.UnitId.ToString(),"unit_chr_soldier_male_02_alt_04",StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                using var matches=em.CreateEntityQuery(typeof(SkirmishMatchState));
                if(matches.CalculateEntityCount()!=1)throw new InvalidDataException("Unique live Match required");
                var match=matches.GetSingleton<SkirmishMatchState>();
                if(match.ScenarioIndex!=3||match.Seed!=104731||match.SessionId.IsEmpty)
                    throw new InvalidDataException("Actual S073 legacy session mismatch");
                using var grids=em.CreateEntityQuery(typeof(GridConfig));
                if(grids.CalculateEntityCount()!=1)throw new InvalidDataException("Unique live grid required");
                var grid=grids.GetSingleton<GridConfig>();
                using var focuses=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
                using var cameras=em.CreateEntityQuery(typeof(RtsCameraRequestQueueComponent),typeof(RtsCameraStateComponent));
                if(focuses.CalculateEntityCount()!=1||cameras.CalculateEntityCount()!=1)
                    throw new InvalidDataException("Unique original focus and camera request state required");
                var focus=focuses.GetSingleton<RuntimeCameraFocusRequestComponent>();
                var cameraEntity=cameras.GetSingletonEntity(); var cameraState=em.GetComponentData<RtsCameraStateComponent>(cameraEntity);
                if(!em.HasBuffer<RtsCameraRequestElement>(cameraEntity))throw new InvalidDataException("Original camera request buffer missing");
                var requests=new JArray(); foreach(var row in em.GetBuffer<RtsCameraRequestElement>(cameraEntity,true))
                    requests.Add(new JObject{["id"]=row.RequestId,["kind"]=row.Kind.ToString(),["target_entity"]=row.TargetEntity.ToString(),["placement_token"]=row.PlacementToken,["screen_delta"]=new JArray(row.ScreenDelta.x,row.ScreenDelta.y),["world"]=V(row.WorldPosition.x,row.WorldPosition.y,row.WorldPosition.z),["rotation"]=new JArray(row.Rotation.x,row.Rotation.y,row.Rotation.z,row.Rotation.w),["values"]=new JArray(row.Value,row.Value2,row.Value3,row.Value4,row.Value5,row.Value6),["flags"]=new JArray(row.Flag,row.Flag2,row.Flag3)});
                var camera=Camera.main; var cameraJson=new JObject{["present"]=camera!=null};
                if(camera!=null) { var p=camera.transform.position;var q=camera.transform.rotation;var r=camera.transform.right;var f=camera.transform.forward;
                    cameraJson["enabled"]=camera.enabled;cameraJson["active_in_hierarchy"]=camera.gameObject.activeInHierarchy;
                    cameraJson["position"]=V(p.x,p.y,p.z);cameraJson["rotation"]=new JArray(q.x,q.y,q.z,q.w);
                    cameraJson["right"]=V(r.x,r.y,r.z);cameraJson["forward"]=V(f.x,f.y,f.z);cameraJson["field_of_view"]=camera.fieldOfView; }
                bool shellKnown=UiShellRuntimeGateway.TryReadShellState(out var shell);
                var presentation=em.World.GetExistingSystemManaged<Game.Composition.SkirmishPresentationSystem>();
                var focusedField=typeof(Game.Composition.SkirmishPresentationSystem).GetField("focusedSession",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                if(presentation==null||focusedField==null)throw new InvalidDataException("Original Skirmish presentation focus marker unavailable");
                var focusedSession=(string)focusedField.GetValue(presentation);
                var provider=Game.Rendering.Contracts.RenderBoundsGateway.Provider;var bounds=new JArray();
                float cell=Mathf.Max(.01f,grid.CellSize);const int padding=98;
                var area=new Rect(grid.Origin.x-padding*cell,grid.Origin.z-padding*cell,(grid.Width+padding*2)*cell,(grid.Height+padding*2)*cell);
                if(provider!=null){var values=new NativeList<Bounds>(Allocator.Temp);try{provider.CollectStaticBounds(em,area,cell*.25f,cell*20f,ref values);foreach(var b in values)bounds.Add(new JObject{["center"]=V(b.center.x,b.center.y,b.center.z),["extents"]=V(b.extents.x,b.extents.y,b.extents.z)});}finally{values.Dispose();}}
                var receipt=new JObject { ["kind"]="S073FirstProductionCameraReference",["scope"]="PreDispatchPassiveDiagnostic_NoMissionOutcome",
                    ["session"]=match.SessionId.ToString(),["seed"]=match.Seed,["scenario_index"]=match.ScenarioIndex,["frame"]=Time.frameCount,["world_elapsed"]=em.WorldUnmanaged.Time.ElapsedTime,["production_now"]=now,["boundary"]=boundary.ToString(),
                    ["request"]=new JObject{["id"]=request.RequestId,["faction"]=request.FactionId,["unit_id"]=request.UnitId.ToString(),["status"]=request.Status,["owner"]=request.RequestOwner.ToString(),["attempt"]=request.RequestAttempt.ToString(),["cost"]=request.Cost,["materials_cost"]=request.MaterialsCost,["resources_reserved"]=request.ResourcesReserved},
                    ["camera"]=cameraJson,["focus"]=new JObject{["requested"]=focus.Requested,["smooth"]=focus.Smooth,["explicit_perspective"]=focus.UseExplicitPerspective,["tactical_reveal"]=focus.UseTacticalRevealZoom,["explicit_yaw"]=focus.UseExplicitYaw,["yaw"]=focus.YawDegrees,["duration"]=focus.SmoothTimeSeconds,["world"]=V(focus.World.x,focus.World.y,focus.World.z),["perspective"]=new JArray(focus.Perspective.x,focus.Perspective.y,focus.Perspective.z,focus.Perspective.w)},
                    ["camera_state"]=new JObject{["last_request_id"]=em.GetComponentData<RtsCameraRequestQueueComponent>(cameraEntity).LastRequestId,["is_dragging"]=cameraState.IsDragging,["has_smooth_focus_target"]=cameraState.HasSmoothFocusTarget,["smooth_focus_target"]=V(cameraState.SmoothFocusTarget.x,cameraState.SmoothFocusTarget.y,cameraState.SmoothFocusTarget.z),["has_smooth_perspective_target"]=cameraState.HasSmoothPerspectiveTarget,["was_play_requested"]=cameraState.WasPlayRequested,["was_build_mode_active"]=cameraState.WasBuildModeActive,["zoom_transition_active"]=cameraState.IsZoomTransitionActive,["intro_zoom_settle_pending"]=cameraState.MatchIntroZoomSettlePending,["fullscreen_iso_target_height"]=cameraState.FullscreenIsoTargetHeight,["fullscreen_iso_target_size"]=cameraState.FullscreenIsoTargetOrthographicSize,["normal_iso_active"]=cameraState.NormalIsoModeActive},["camera_requests"]=requests,
                    ["shell"]=new JObject{["known"]=shellKnown,["mode"]=shellKnown?shell.CurrentMode.ToString():null,["transition_running"]=shellKnown?(JToken)new JValue(shell.IsTransitionRunning):JValue.CreateNull(),["presentation_focused_session"]=focusedSession,["focus_marker_matches_session"]=focusedSession==match.SessionId.ToString(),["marker_scope"]="Presentation focus marker only; not proof camera request was consumed"},
                    ["static_bounds"]=new JObject{["provider_present"]=provider!=null,["query_executed"]=provider!=null,["scope"]="Live provider filtered global grid envelope; actual later drop preferred cell not yet known",["grid_width"]=grid.Width,["grid_height"]=grid.Height,["grid_origin"]=V(grid.Origin.x,grid.Origin.y,grid.Origin.z),["cell_size"]=grid.CellSize,["query_rect"]=new JArray(area.x,area.y,area.width,area.height),["minimum_extent"]=cell*.25f,["maximum_extent"]=cell*20f,["bounds"]=provider!=null?(JToken)bounds:JValue.CreateNull()},
                    ["mission_outcome_claimed"]=false,["native_readiness_claimed"]=false };
                Directory.CreateDirectory(Path.GetDirectoryName(s073ReferencePath));
                using(var stream=new FileStream(s073ReferencePath,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream))writer.Write(receipt.ToString(Newtonsoft.Json.Formatting.Indented));
                s073ReferenceCaptured=true;s073ReferenceExit=0;
                Debug.Log("[S073ProductionCameraReference] result=Captured scope=PreDispatchPassiveDiagnostic missionOutcome=0");
                EditorApplication.delayCall+=StopS073ProductionCameraReference;
            }
            catch(Exception error){Debug.LogError("[S073ProductionCameraReference] result=Failed "+error);s073ReferenceExit=2;EditorApplication.delayCall+=StopS073ProductionCameraReference;}
        }
        static JArray V(float x,float y,float z)=>new JArray(x,y,z);
        static void PollS073ProductionCameraReference()
        {
            if(!s073ReferenceArmed||s073ReferenceCaptured)return;
            double now=EditorApplication.timeSinceStartup;
            if(now>=s073ReferenceNextStatusAt)
            {
                s073ReferenceNextStatusAt=now+30d;
                LogS073ProductionCameraProgress(now,"waiting");
            }
            if(now>s073ReferenceDeadline)
            {
                LogS073ProductionCameraProgress(now,"timeout");
                Debug.LogError("[S073ProductionCameraReference] result=Failed reason=timeout");
                s073ReferenceExit=2;StopS073ProductionCameraReference();
            }
        }
        static void LogS073ProductionCameraProgress(double now,string phase)
        {
            string shell="world=missing";
            try
            {
                var world=World.DefaultGameObjectInjectionWorld;
                if(world!=null&&world.IsCreated)
                {
                    var em=world.EntityManager;
                    using var query=em.CreateEntityQuery(typeof(UiShellStateComponent));
                    int count=query.CalculateEntityCount();
                    shell="shellCount="+count;
                    if(count==1)
                    {
                        var entity=query.GetSingletonEntity();
                        var state=em.GetComponentData<UiShellStateComponent>(entity);
                        string disposition=em.HasComponent<UiShellStartupDispositionComponent>(entity)
                            ? em.GetComponentData<UiShellStartupDispositionComponent>(entity).Value.ToString() : "missing";
                        string loading="missing";
                        if(em.HasComponent<UiShellLoadingProgressComponent>(entity))
                        {
                            var value=em.GetComponentData<UiShellLoadingProgressComponent>(entity);
                            loading=value.Status.ToString()+":"+value.Progress01.ToString("F2")+":"+value.IsComplete;
                        }
                        shell+=" mode="+state.CurrentMode+" transition="+state.IsTransitionRunning+
                            " route="+state.ActiveRoute+" disposition="+disposition+" loading="+loading;
                    }
                }
            }
            catch(Exception error){shell="snapshotError="+error.GetType().Name;}
            var touch=menuTouch;
            Debug.Log("[S073ProductionCameraReference] phase="+phase+" elapsed="+(now-s073ReferenceStartedAt).ToString("F1")+
                " playing="+EditorApplication.isPlaying+" stage="+stage+" menuStep="+menuStep+
                " touch="+(touch!=null)+" touchBusy="+(touch!=null&&touch.IsBusy)+
                " playerInterrupted="+(touch!=null&&touch.PlayerInterrupted)+
                " unexpectedSamples="+(touch?.UnexpectedSamples??0)+" screen="+Screen.width+"x"+Screen.height+" "+shell);
        }
        static void StopS073ProductionCameraReference(){BuildingRuntimeProcessingCompositionSystemHelper.EditorBeforeProductionRequest=null;EditorApplication.update-=PollS073ProductionCameraReference;EditorApplication.update-=Tick;SessionState.SetBool(Key,false);StopS004MenuProbe();StopS004AriaConsentProbe();RestoreBackgroundInput();if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=false;else FinishS073ProductionCameraReference(PlayModeStateChange.EnteredEditMode);}
        static void FinishS073ProductionCameraReference(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode)return;DisarmS073ProductionCameraReference();RestoreS004EditorJourneyOptions();if(SessionState.GetBool("Warline.MissionValidation.ResumeRefresh",false)){AssetDatabase.AllowAutoRefresh();SessionState.EraseBool("Warline.MissionValidation.ResumeRefresh");}EditorApplication.Exit(s073ReferenceExit);}
        static void DisarmS073ProductionCameraReference(){s073ReferenceArmed=false;BuildingRuntimeProcessingCompositionSystemHelper.EditorBeforeProductionRequest=null;EditorApplication.update-=PollS073ProductionCameraReference;EditorApplication.playModeStateChanged-=FinishS073ProductionCameraReference;}
    }
}
#endif
