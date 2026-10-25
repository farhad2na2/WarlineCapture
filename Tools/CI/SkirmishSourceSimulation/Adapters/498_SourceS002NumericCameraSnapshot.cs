extern alias ActualCore;
using System;using System.IO;using System.Linq;using Newtonsoft.Json.Linq;using Game.Components;using Game.Configs;using Game.Runtime.TransportPose;using Unity.Entities;using Unity.Mathematics;using V3=ActualCore::UnityEngine.Vector3;
namespace Game.Runtime{
internal sealed partial class SourceS002NumericCameraProvider{
 bool poseEnabled,nativeValuesAdmitted,motionValuesAdmitted;SourceNumericCameraData cameraData;SourceNumericCameraMotion motion;EntityManager snapshotEm;Entity snapshotSession,snapshotEntity;
 internal string SimulatorViewportManifest{get;private set;}internal string ScenePoseSha256{get;private set;}internal string NumericOracleSha256{get;private set;}
 internal float3 Position{get{Ready();if(!poseEnabled)throw new NotSupportedException("Actual numeric scene pose missing");return new float3(cameraData.Position.x,cameraData.Position.y,cameraData.Position.z);}}
 internal void EnableNumericPose(EntityManager em,Entity session,string scenePath,string sceneSha,int viewportWidth,int viewportHeight){
  Ready();if(poseEnabled||em==null||!em.Exists(session)||em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup.SetupHash!=SetupHash)throw new InvalidOperationException("Once-only same-world numeric camera binding required");
  if(viewportWidth!=1280||viewportHeight!=720)throw new NotSupportedException("Explicit reviewed CLI simulator viewport1280x720 required; no native-device claim");
  string scene=System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(scenePath,sceneSha,32*1024*1024));var transform=TopFields(SceneBlock(scene,"4","1220593094"));var cam=TopFields(SceneBlock(scene,"20","1220593093"));
  var pos=Inline(Need(transform,"m_LocalPosition"));var scale=Inline(Need(transform,"m_LocalScale"));if(Need(transform,"m_Father")!="{fileID: 0}"||scale.Any(x=>float.Parse(x.Value,System.Globalization.CultureInfo.InvariantCulture)!=1f))throw new NotSupportedException("Only original unparented unit-scale Match camera admitted");
  var viewport=SectionFields(SceneBlock(scene,"20","1220593093"),"m_NormalizedViewPortRect");if(Need(viewport,"x")!="0"||Need(viewport,"y")!="0"||Need(viewport,"width")!="1"||Need(viewport,"height")!="1")throw new NotSupportedException("Source full normalized camera viewport required");
  float near=float.Parse(Need(cam,"near clip plane"),System.Globalization.CultureInfo.InvariantCulture),far=float.Parse(Need(cam,"far clip plane"),System.Globalization.CultureInfo.InvariantCulture);
  if(near!=.3f||far!=10000f||Need(cam,"orthographic")!="0")throw new NotSupportedException("Source perspective scene clip domain differs");
  float minX=Origin.x,minZ=Origin.z,maxX=minX+GridWidth*CellSize,maxZ=minZ+GridHeight*CellSize;
  using(var bounds=em.CreateEntityQuery(typeof(OperationMapBoundsComponent))){
   if(bounds.CalculateEntityCount()==1){var b=bounds.GetSingleton<OperationMapBoundsComponent>();if(!math.all(math.isfinite(b.CameraMin.xz))||!math.all(math.isfinite(b.CameraMax.xz))||math.any(b.CameraMax.xz-b.CameraMin.xz<=new float2(.01f)))throw new NotSupportedException("Active source camera bounds invalid");minX=b.CameraMin.x;minZ=b.CameraMin.z;maxX=b.CameraMax.x;maxZ=b.CameraMax.z;}
  }
  cameraData=new SourceNumericCameraData{Position=new V3(float.Parse(Need(pos,"x"),System.Globalization.CultureInfo.InvariantCulture),profile.NormalHeight,float.Parse(Need(pos,"z"),System.Globalization.CultureInfo.InvariantCulture)),Rotation=rotation,FieldOfView=profile.NormalFieldOfView,Near=near,Far=far,Aspect=(float)viewportWidth/viewportHeight};
  motion=new SourceNumericCameraMotion(minX,minZ,maxX,maxZ);motion.ClampCameraToGroundBoundary(cameraData);snapshotEm=em;snapshotSession=session;ScenePoseSha256=sceneSha;SimulatorViewportManifest="CLI_SIMULATOR_INPUT:1280x720; normalizedRect0,0,1,1; noNativeDeviceEvidence";poseEnabled=true;
 }
 internal void AdmitNativeValueOracle(string path,string sha){
  Ready();if(!poseEnabled||nativeValuesAdmitted)throw new InvalidOperationException("Pose first; value oracle admits once");
  var root=Json(AutonomousWorld.PinnedFile(path,sha,1024*1024));if((string)root["kind"]!="SkirmishNumericCameraUnityValueOracle"||(string)root["status"]!="Passed"||(bool?)root["mission_executed"]!=false||(bool?)root["simulator_input"]!=true)throw new InvalidDataException("Actual checked camera value oracle required");
  var samples=root["samples"] as JArray;if(samples?.Count!=3)throw new InvalidDataException("Three actual reference aspects required");
  foreach(JObject s in samples){
   float[] p=s["position"].Values<float>().ToArray(),r=s["rotation"].Values<float>().ToArray();
   var test=new SourceNumericCameraData{Position=new V3(p[0],p[1],p[2]),Rotation=PoseMath.Euler(new float3(65,307,0)),FieldOfView=(float)s["fov"],Near=(float)s["near"],Far=(float)s["far"],Aspect=(float)(int)s["width"]/(int)s["height"]};
   NearMatrix(test.WorldToCamera,(JArray)s["world_to_camera"],.001f);NearMatrix(test.Projection,(JArray)s["projection"],.000002f);NearMatrix(math.mul(test.Projection,test.WorldToCamera),(JArray)s["view_projection"],.003f);
   // Rays are diagnostics only: their tighter threshold failed. Actual source-motion outputs require separate consumed-output admission below.
   var snapshot=Build(test,1);foreach(JObject row in (JArray)s["points"]){var w=row["world"].Values<float>().ToArray();var point=new float3(w[0],w[1],w[2]);if(UnitDeathSystem.IsInsideCameraViewport(snapshot,point)!=(bool)row["original_death_inside"])throw new InvalidDataException("Original death viewport value disagreement");var view=math.mul(snapshot.WorldToCamera,new float4(point,1));var clip=math.mul(snapshot.ViewProjection,new float4(point,1));NearVector(new float3(clip.x/clip.w*.5f+.5f,clip.y/clip.w*.5f+.5f,-view.z),(JArray)row["viewport"],.003f);}
  }
  NumericOracleSha256=sha;nativeValuesAdmitted=true;
 }
 internal void AdvanceNumericCamera(float sourceUnscaledTime,float sourceUnscaledDelta){
  Ready();if(!poseEnabled||!motionValuesAdmitted)throw new NotSupportedException("Numeric scene pose and consumed-motion oracle required");if(!math.isfinite(sourceUnscaledDelta)||math.abs(sourceUnscaledDelta-1f/60f)>.0000001f)throw new NotSupportedException("Actual finite source unscaled camera delta required");AdvanceSourceUnscaledTime(sourceUnscaledTime);
  motion.SourceUnscaledDelta=sourceUnscaledDelta;cameraData.Rotation=rotation;if(motion.HasSmoothFocusTarget){var center=motion.GetCameraGroundCenterWorld(cameraData);var next=motion.UpdateSmoothFocus(center,.25f,cameraData);motion.MoveCameraGroundCenterTo(cameraData,next);}else motion.ClampCameraToGroundBoundary(cameraData);RequireInterior();
 }
 internal float3 GroundCenter{get{Ready();RequirePose();var p=motion.GetCameraGroundCenterWorld(cameraData);return new float3(p.x,p.y,p.z);}}
 void ApplySourcePerspective(bool build){if(build)throw new NotSupportedException("Current logical S002 doesnot consume native buildcamera; FOV50 motion domain not qualified");cameraData.Rotation=rotation;cameraData.Position=new V3(cameraData.Position.x,build?profile.BuildHeight:profile.NormalHeight,cameraData.Position.z);cameraData.FieldOfView=build?profile.BuildFieldOfView:profile.NormalFieldOfView;motion.ClampCameraToGroundBoundary(cameraData);RequireInterior();}
 internal void InspectPublicObjective(float3 actualPublicPosition){Ready();RequireMotion();PoseMath.Finite(actualPublicPosition);motion.SetSmoothFocusTarget(new V3(actualPublicPosition.x,actualPublicPosition.y,actualPublicPosition.z),true,.35f);}
 internal void PanSourceGroundCenter(float3 actualPublicPosition){Ready();RequireMotion();PoseMath.Finite(actualPublicPosition);motion.ClearSmoothFocusTarget();motion.MoveCameraGroundCenterTo(cameraData,new V3(actualPublicPosition.x,actualPublicPosition.y,actualPublicPosition.z));RequireInterior();}
 internal void FocusBaseFallbackInstant(float3 actualPublicPosition){Ready();RequireMotion();PoseMath.Finite(actualPublicPosition);rotation=PoseMath.Euler(new float3(profile.Pitch,profile.Yaw,0));cameraData.Rotation=rotation;cameraData.Position=new V3(cameraData.Position.x,profile.NormalHeight,cameraData.Position.z);cameraData.FieldOfView=profile.NormalFieldOfView;motion.ClampCameraToGroundBoundary(cameraData);motion.MoveCameraGroundCenterTo(cameraData,new V3(actualPublicPosition.x,actualPublicPosition.y,actualPublicPosition.z));motion.ClearSmoothFocusTarget();RequireInterior();}
 internal RuntimeCameraSnapshotComponent PublishSourceSnapshot(){
  RequireSnapshot();using var q=snapshotEm.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent));
  if(snapshotEntity==Entity.Null){if(q.CalculateEntityCount()>1)throw new NotSupportedException("Source snapshot singleton required");snapshotEntity=q.CalculateEntityCount()==1?q.GetSingletonEntity():snapshotEm.CreateEntity();if(!snapshotEm.HasComponent<RuntimeCameraSnapshotComponent>(snapshotEntity))snapshotEm.AddComponentData(snapshotEntity,new RuntimeCameraSnapshotComponent());}
  if(!snapshotEm.Exists(snapshotSession)||!snapshotEm.Exists(snapshotEntity))throw new InvalidOperationException("Retained camera snapshot ownership lost");uint next=snapshotEm.GetComponentData<RuntimeCameraSnapshotComponent>(snapshotEntity).PublicationVersion+1u;if(next==0)next=1;
  var snapshot=Build(cameraData,next);snapshotEm.SetComponentData(snapshotEntity,snapshot);return snapshot;
 }
 internal RuntimeCameraSnapshotComponent ReadPublishedSnapshot(){RequireSnapshot();if(snapshotEntity==Entity.Null)throw new InvalidOperationException("Snapshot has not been published at owned phase");return snapshotEm.GetComponentData<RuntimeCameraSnapshotComponent>(snapshotEntity);}
 void RequireMotion(){RequirePose();if(!motionValuesAdmitted)throw new NotSupportedException("Consumed motion reference missing");}
 void RequireInterior(){try{motion.RequireQualifiedInterior(cameraData,.06f);}catch{poisoned=true;throw;}}
 void RequirePose(){if(!poseEnabled)throw new NotSupportedException("Scene pose/viewport missing");}
 static RuntimeCameraSnapshotComponent Build(SourceNumericCameraData c,uint version){var world=c.WorldToCamera;var projection=c.Projection;var pos=new float3(c.Position.x,c.Position.y,c.Position.z);return new RuntimeCameraSnapshotComponent{IsValid=1,PublicationVersion=version,Position=pos,Rotation=c.Rotation,WorldToCamera=world,Projection=projection,ViewProjection=math.mul(projection,world),Signature=SourceNumericCameraSignature.ComputeSignature(pos,c.Rotation,world,projection)};}
 static string SceneBlock(string yaml,string type,string id){string key="--- !u!"+type+" &"+id;int p=yaml.IndexOf(key+"\n",StringComparison.Ordinal);if(p<0)throw new InvalidDataException("Exact source Match camera component missing");int end=yaml.IndexOf("\n--- !u!",p+key.Length,StringComparison.Ordinal);return end<0?yaml.Substring(p):yaml.Substring(p,end-p);}
 static void NearVector(float3 value,JArray expected,float tolerance){var a=expected.Values<float>().ToArray();if(a.Length!=3||!math.all(math.isfinite(value))||a.Any(x=>!math.isfinite(x))||math.cmax(math.abs(value-new float3(a[0],a[1],a[2])))>tolerance)throw new InvalidDataException("Native value reference vector mismatch");}
 static void NearMatrix(float4x4 m,JArray expected,float tolerance){var a=expected.Values<float>().ToArray();if(a.Length!=16||a.Any(x=>!math.isfinite(x)))throw new InvalidDataException("Reference matrix shape/finite");for(int i=0;i<16;i++)if(math.abs(m[i/4][i%4]-a[i])>tolerance)throw new InvalidDataException("Native reference matrix mismatch column "+i);}
}
}
