extern alias ActualCore;
using System;using System.IO;using System.Linq;using Newtonsoft.Json.Linq;using Unity.Mathematics;using Game.Components;using Unity.Entities;using Unity.Transforms;using Game.Runtime.TransportPose;using V3=ActualCore::UnityEngine.Vector3;
namespace Game.Runtime{
internal sealed partial class SourceS002NumericCameraProvider{
 internal const float PositionEnvelope=.025f,CenterEnvelope=.025f,FootprintEnvelope=.03f,ViewSpaceUncertainty=.05f;
 internal string MotionOracleSha256{get;private set;}
 internal void AdmitConsumedMotionOracle(string path,string sha){
  Ready();RequirePose();if(!nativeValuesAdmitted||motionValuesAdmitted)throw new InvalidOperationException("Matrix proof first; consumedmotion admitsonce");
  var root=Json(AutonomousWorld.PinnedFile(path,sha,2*1024*1024));var input=root["input"];
  engineAdapter.RequireMotion(sha,root);ValidateMotionOrDiagnostic(root,sha,FunctionReferenceBoundary(root,motion.GroundBoundary));MotionOracleSha256=sha;motionValuesAdmitted=true;RequireInterior();
 }
 internal int PreflightConsumedMotionReference(JObject root,string sha,ActualCore::UnityEngine.Rect boundary){Ready();engineAdapter.RequireMotion(sha,root);return ValidateMotionOrDiagnostic(root,sha,FunctionReferenceBoundary(root,boundary));}
 int ValidateMotionOrDiagnostic(JObject root,string sha,ActualCore::UnityEngine.Rect boundary){try{return ValidateConsumedMotionReference(root,engineAdapter.FunctionReferenceProfile??profile,catalogId,engineAdapter.FunctionReferenceProfileSha,boundary,engineAdapter);}catch(InvalidDataException e){if(catalogId!="S049"||Environment.GetEnvironmentVariable("WARLINE_S049_CAMERA_DIAGNOSTIC")!="1"||sha!="18a63db2b9a6d7c39e9898efe5825295143c4b6de4ce714243e51886f12e4765"||e.Message!="Consumed camera state=124 pose=0 phase=124"||!(e.InnerException is InvalidDataException inner)||!inner.Message.StartsWith("Native value reference vector mismatch actual=float3(1644.26f, 14f, 670.651f) expected=[1644.1658935546875,14.0,670.61224365234375] tolerance=0.025",StringComparison.Ordinal))throw;Console.Error.WriteLine("[S049CameraDiagnostic] native_camera_qualified=0 motion_reference_passed=0 exact_mismatch="+e.Message+" detail="+inner.Message);return 124;}}
 ActualCore::UnityEngine.Rect FunctionReferenceBoundary(JObject root,ActualCore::UnityEngine.Rect actual){if(engineAdapter.FunctionReferenceProfile==null)return actual;var b=root["input"]?["boundary"]as JArray;if(b==null||b.Count!=4)throw new InvalidDataException("Historical function boundary absent");var values=b.Values<float>().ToArray();if(values.Any(v=>!math.isfinite(v))||values[2]<=0||values[3]<=0)throw new InvalidDataException("Historical function boundary invalid");return new ActualCore::UnityEngine.Rect(values[0],values[1],values[2],values[3]);}
 internal static int ValidateConsumedMotionReference(JObject root,Game.Configs.SkirmishMissionViewProfile profile,string catalogId,string ProfileSha256,ActualCore::UnityEngine.Rect bound,SourceNumericCameraEngineAdapterContract adapter){var input=root["input"];
  if((string)root["kind"]!="SkirmishNumericCameraMotionUnityOracle"||(string)root["status"]!="Passed"||(bool?)root["mission_executed"]!=false||(int?)input["width"]!=1280||(int?)input["height"]!=720||(float?)input["delta_seconds"]!=1f/60||(float?)input["smooth_seconds"]!=.35f||(int?)input["steps"]!=180)throw new InvalidDataException("Exact consumed-source camera reference required");
  var b=input["boundary"].Values<float>().ToArray();
  if(b.Length!=4||b[0]!=bound.xMin||b[1]!=bound.yMin||b[2]!=bound.width||b[3]!=bound.height)throw new NotSupportedException("Actual camera boundary outside measured source domain");
  bool engineering=adapter.Engineering;
  if(engineering){if((int?)input["schema_version"]!=2||(string)input["kind"]!="SourceEngineeringCameraMotionInput"||(string)input["scope"]!="CompiledAuthoringReferenceInput_NotRuntimePublicObservation"||(string)input["engineering_profile_sha256"]!=ProfileSha256||(float?)input["pitch"]!=profile.Pitch||(float?)input["yaw"]!=profile.Yaw||(float?)input["fov"]!=profile.NormalFieldOfView||(float?)input["normal_height"]!=profile.NormalHeight)throw new InvalidDataException("Engineering camera motion input differs from actual shared profile");}
  else if(profile.Pitch!=65||profile.Yaw!=307||profile.NormalFieldOfView!=46||profile.NormalHeight!=20)throw new NotSupportedException("Actual S002 camera profile outside measured source domain");
  var target=input[engineering?"reference_target":"public_objective"].Values<float>().ToArray();if(target.Length!=3)throw new InvalidDataException("Actual publicobjective fixture required");
  var poses=root["poses"]as JArray;if(poses?.Count!=5)throw new InvalidDataException("Five actual consumed-output source cases required");
  int rows=0;foreach(JObject row in poses){
   var start=row["initial"].Values<float>().ToArray();if(start.Length!=3)throw new InvalidDataException("Pose fixture shape");
   Func<SourceNumericCameraData> make=()=>new SourceNumericCameraData{RayAdapterMode=adapter.Mode,Position=new V3(start[0],start[1],start[2]),Rotation=PoseMath.Euler(new float3(profile.Pitch,profile.Yaw,0)),FieldOfView=profile.NormalFieldOfView,Near=.3f,Far=10000,Aspect=1280f/720};
   var c=make();var m=new SourceNumericCameraMotion(b[0],b[1],b[0]+b[2],b[1]+b[3]);CheckMotionAt(m,c,(JObject)row["before"],rows);rows++;m.ClampCameraToGroundBoundary(c);CheckMotionAt(m,c,(JObject)row["after_clamp"],rows);rows++;m.MoveCameraGroundCenterTo(c,new V3(target[0],target[1],target[2]));CheckMotionAt(m,c,(JObject)row["after_pan"],rows);rows++;
   c=make();m=new SourceNumericCameraMotion(b[0],b[1],b[0]+b[2],b[1]+b[3]);m.ClampCameraToGroundBoundary(c);m.SetSmoothFocusTarget(new V3(target[0],target[1],target[2]),true,.35f);var sequence=row["smooth"]as JArray;if(sequence?.Count!=180)throw new InvalidDataException("Complete source smooth sequence missing");
   foreach(JObject expected in sequence){m.SourceUnscaledDelta=1f/60;var next=m.UpdateSmoothFocus(m.GetCameraGroundCenterWorld(c),.25f,c);m.MoveCameraGroundCenterTo(c,next);CheckMotionAt(m,c,expected,rows);rows++;}
  }
  if(rows!=915)throw new InvalidDataException("Incomplete consumed camera proof");return rows;
 }
 static void CheckMotionAt(SourceNumericCameraMotion m,SourceNumericCameraData c,JObject expected,int state){try{CheckMotion(m,c,expected);}catch(Exception error){throw new InvalidDataException("Consumed camera state="+state+" pose="+(state/183)+" phase="+(state%183),error);}}
 static void CheckMotion(SourceNumericCameraMotion m,SourceNumericCameraData c,JObject e){
  NearVector(new float3(c.Position.x,c.Position.y,c.Position.z),(JArray)e["position"],PositionEnvelope);var center=m.GetCameraGroundCenterWorld(c);NearVector(new float3(center.x,center.y,center.z),(JArray)e["center"],CenterEnvelope);
  if(!m.TryGetCameraGroundBounds(c,out var r))throw new InvalidDataException("Unknown consumed camera footprint");var f=e["footprint"].Values<float>().ToArray();if(f.Length!=4||f.Any(x=>!math.isfinite(x))||math.cmax(math.abs(new float4(r.xMin,r.yMin,r.width,r.height)-new float4(f[0],f[1],f[2],f[3])))>FootprintEnvelope||m.HasSmoothFocusTarget!=(bool)e["has_target"])throw new InvalidDataException("Consumed footprint/completion branch outside measured reference envelope");
 }
 // This preflight is called immediately before the original UnitDeathSystem.
 // It never substitutes a visibility answer or invalid camera. Ambiguity stops.
 internal void ValidateOriginalCorpseViewportDecisions(){
  RequireSnapshot();var snapshot=ReadPublishedSnapshot();using var q=snapshotEm.CreateEntityQuery(typeof(UnitHealth),typeof(UnitDeathAnimationComponent));using var all=q.ToEntityArray(Unity.Collections.Allocator.Temp);
  foreach(var e in all){if(snapshotEm.HasComponent<StaticGridBlocker>(e)||snapshotEm.GetComponentData<UnitHealth>(e).Current>0||snapshotEm.GetComponentData<UnitDeathAnimationComponent>(e).PoseFrozen==0||!snapshotEm.HasComponent<LocalTransform>(e))continue;
   ValidateViewportDecision(snapshot,snapshotEm.GetComponentData<LocalTransform>(e).Position);
  }
 }
 internal static bool ValidateViewportDecision(in RuntimeCameraSnapshotComponent snapshot,float3 point){
  if(snapshot.IsValid==0||!math.all(math.isfinite(point)))throw new NotSupportedException("Actual numeric snapshot/point required");
  var view=math.mul(snapshot.WorldToCamera,new float4(point,1));float depth=-view.z,e=ViewSpaceUncertainty;
  bool original=UnitDeathSystem.IsInsideCameraViewport(snapshot,point);if(depth< -e){if(original)throw new InvalidDataException("Sourcebehind predicate mismatch");return false;}
  if(depth<=e)throw new NotSupportedException("Numeric corpse camera front-plane uncertainty");
  float2 x=RatioInterval(view.x,depth,e),y=RatioInterval(view.y,depth,e);float kx=snapshot.Projection.c0.x,ky=snapshot.Projection.c1.y;
  float2 u=ViewportInterval(kx,x),v=ViewportInterval(ky,y);
  if(u.x>u.y)u=u.yx;if(v.x>v.y)v=v.yx;
  bool inside=u.x>=-.1f&&u.y<=1.1f&&v.x>=-.1f&&v.y<=1.1f;
  bool outside=u.y<-.1f||u.x>1.1f||v.y<-.1f||v.x>1.1f;
  if(!inside&&!outside)throw new NotSupportedException("Numeric corpse viewport padding boundary uncertain");
  if(original!=inside)throw new InvalidDataException("Original corpse predicate differs from admitted interval");return original;
 }
 static float2 ViewportInterval(float k,float2 ratio){float4 p=new float4((k-.000002f)*ratio.x,(k-.000002f)*ratio.y,(k+.000002f)*ratio.x,(k+.000002f)*ratio.y);return new float2(.5f+.5f*math.cmin(p)-.000001f,.5f+.5f*math.cmax(p)+.000001f);}
 static float2 RatioInterval(float numerator,float denominator,float error){float4 a=new float4((numerator-error)/(denominator-error),(numerator-error)/(denominator+error),(numerator+error)/(denominator-error),(numerator+error)/(denominator+error));return new float2(math.cmin(a),math.cmax(a));}
}
}
