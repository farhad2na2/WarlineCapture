#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;using System.IO;using System.Linq;using System.Security.Cryptography;using UnityEngine;using Unity.Entities;using Game.Runtime;
public static class SkirmishNumericCameraMotionOracle{
 [Serializable]public sealed class Input{public int schema_version;public string kind,scope,initialization_path,initialization_sha256,boundary_source,source_projection_sha256,engineering_profile_sha256;public uint setup_hash;public int seed,width,height,steps;public float smooth_seconds,delta_seconds,pitch,yaw,fov,normal_height;public float[] public_objective,reference_target,boundary;}
 [Serializable]public sealed class State{public float[] position,center,footprint;public bool has_target;}
 [Serializable]public sealed class Pose{public string name;public float[] initial;public State before,after_clamp,after_pan;public State[] smooth;}
 [Serializable]public sealed class Receipt{public int schema_version=1;public string kind="SkirmishNumericCameraMotionUnityOracle",status="Passed",input_sha256,unity_version;public bool mission_executed=false,native_device_evidence=false;public Input input;public Pose[] poses;}
 public static void RunFocusedValidation()=>Capture(null,null,null,true);
 public static void CaptureEngineeringReference(string input,string sha,string output)=>Capture(input,sha,output,false);
 static void Capture(string inputPath,string inputSha,string output,bool exit){
  GameObject cameraObject=null;World world=null;
  try{
   if(inputPath==null){inputPath=Arg("-skirmishCameraMotionInput");inputSha=Arg("-skirmishCameraMotionInputSha");output=Arg("-skirmishCameraMotionOutput");}
   if(!Path.IsPathRooted(inputPath)||!Path.IsPathRooted(output)||File.Exists(output)||Hash(inputPath)!=inputSha)throw new ArgumentException("Exact unique motion fixture/output required");
   var input=JsonUtility.FromJson<Input>(File.ReadAllText(inputPath));bool engineering=input.schema_version==2&&input.kind=="SourceEngineeringCameraMotionInput";float pitch=engineering?input.pitch:65,yaw=engineering?input.yaw:307,fov=engineering?input.fov:46,normalHeight=engineering?input.normal_height:20;var referenceTarget=engineering?input.reference_target:input.public_objective;if((!engineering&&(input.schema_version!=1||input.kind!="SourceS002CameraMotionInput"))||input.width!=1280||input.height!=720||referenceTarget?.Length!=3||input.boundary?.Length!=4||input.delta_seconds!=1f/60||input.smooth_seconds!=.35f||input.steps!=180)throw new Exception("Actual source publictarget/boundary/fixedclock fixture required");
   if(engineering&&(string.IsNullOrEmpty(input.engineering_profile_sha256)||!float.IsFinite(pitch)||!float.IsFinite(yaw)||!float.IsFinite(fov)||!float.IsFinite(normalHeight)||fov<=0||normalHeight<=0))throw new ArgumentException("Finite bound engineering camera parameters required");
   world=new World("SkirmishNumericCameraMotionOracle_Owned");var owner=world.GetOrCreateSystemManaged<RtsCameraSystem>();
   cameraObject=new GameObject("SkirmishNumericCameraMotionOracle_Owned_"+Guid.NewGuid().ToString("N"));var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=false;camera.fieldOfView=fov;camera.nearClipPlane=.3f;camera.farClipPlane=10000;camera.rect=new Rect(0,0,1,1);camera.pixelRect=new Rect(0,0,1280,720);camera.aspect=1280f/720;
   var target=new Vector3(referenceTarget[0],referenceTarget[1],referenceTarget[2]);var bnd=input.boundary;owner.SetGroundBoundary(new Rect(bnd[0],bnd[1],bnd[2],bnd[3]));
   Vector3[] starts={new Vector3(870.0283f,normalHeight,325.60086f),new Vector3(1024,normalHeight,512),new Vector3(128,normalHeight,128),new Vector3(1920,normalHeight,896),new Vector3(16,normalHeight,16)};string[] names={"source_initial","map_center","lower_interior","upper_interior","near_edge"};var poses=new Pose[5];
   for(int c=0;c<starts.Length;c++){
    owner.ClearSmoothFocusTarget();owner.ClearSmoothPerspectiveTarget();camera.transform.SetPositionAndRotation(starts[c],Quaternion.Euler(pitch,yaw,0));var before=Capture(owner,camera);owner.ClampCameraToGroundBoundary(camera);var clamp=Capture(owner,camera);owner.MoveCameraGroundCenterTo(camera,target);var pan=Capture(owner,camera);
    camera.transform.SetPositionAndRotation(starts[c],Quaternion.Euler(pitch,yaw,0));owner.ClampCameraToGroundBoundary(camera);owner.SetSmoothFocusTarget(target,true,input.smooth_seconds);var reference=new OriginalSmoothReference(owner,input.smooth_seconds);var rows=new State[input.steps];
    for(int f=0;f<rows.Length;f++){var next=reference.UpdateSmoothFocus(owner.GetCameraGroundCenterWorld(camera),.25f,camera,input.delta_seconds);owner.MoveCameraGroundCenterTo(camera,next);rows[f]=Capture(owner,camera);}
    poses[c]=new Pose{name=names[c],initial=V(starts[c]),before=before,after_clamp=clamp,after_pan=pan,smooth=rows};
   }
   var receipt=new Receipt{schema_version=engineering?2:1,input=input,input_sha256=inputSha,unity_version=Application.unityVersion,poses=poses};UnityEngine.Object.DestroyImmediate(cameraObject);cameraObject=null;world.Dispose();world=null;
   Directory.CreateDirectory(Path.GetDirectoryName(output));string temp=output+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(receipt,true));File.Move(temp,output);Debug.Log("[SkirmishNumericCameraMotionOracle] result=Passed poses=5 smoothFrames=900");if(exit)ValidationExit.Exit(0);
  }catch(Exception e){Debug.LogException(e);Debug.LogError("[SkirmishNumericCameraMotionOracle] result=Failed");if(exit)ValidationExit.Exit(1);else throw;}
  finally{if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(world!=null&&world.IsCreated)world.Dispose();}
 }
 static State Capture(RtsCameraSystem owner,Camera camera){if(!owner.TryGetCameraGroundBounds(camera,out var r))throw new Exception("Original camera footprint unknown");return new State{position=V(camera.transform.position),center=V(owner.GetCameraGroundCenterWorld(camera)),footprint=new[]{r.xMin,r.yMin,r.width,r.height},has_target=owner.HasSmoothFocusTarget};}
 static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
 static string Arg(string name){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);if(i<0||i+1>=a.Length)throw new ArgumentException(name);return a[i+1];}
 static string Hash(string p){using(var s=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 // Exact source UpdateSmoothFocus body; only native global delta is bound to the
 // explicit fixed input. Real RtsCameraSystem supplies all camera API collaborators.
 sealed class OriginalSmoothReference{
  const float SmoothFocusCompletionDistanceSq=.01f;readonly RtsCameraSystem owner;Vector3 _smoothFocusVelocity;float _smoothFocusTimeSeconds;
  bool HasSmoothFocusTarget=>owner.HasSmoothFocusTarget;bool HasSmoothPerspectiveTarget=>owner.HasSmoothPerspectiveTarget;Vector3 SmoothFocusTarget=>owner.SmoothFocusTarget;
  internal OriginalSmoothReference(RtsCameraSystem owner,float smooth){this.owner=owner;_smoothFocusTimeSeconds=smooth;}
  Vector3 ClampFocusToCameraFootprint(Camera camera,Vector3 point)=>owner.ClampFocusToCameraFootprint(camera,point);
  void ClearSmoothFocusTarget(){owner.ClearSmoothFocusTarget();_smoothFocusVelocity=Vector3.zero;_smoothFocusTimeSeconds=0;}
public Vector3 UpdateSmoothFocus(Vector3 currentGroundCenter,float smoothTime,Camera worldCamera,float sourceUnscaledDelta)
        {
            if (!HasSmoothFocusTarget)
                return currentGroundCenter;

            // A point can be inside the map while its camera viewport extends outside it.
            // Settle against the reachable center, otherwise boundary clamping fights smoothing forever.
            Vector3 reachableTarget = ClampFocusToCameraFootprint(worldCamera, SmoothFocusTarget);
            Vector3 smoothedCenter = Vector3.SmoothDamp(
                currentGroundCenter,
                reachableTarget,
                ref _smoothFocusVelocity,
                Mathf.Max(0.01f,
                    _smoothFocusTimeSeconds > 0f ? _smoothFocusTimeSeconds : smoothTime), Mathf.Infinity, sourceUnscaledDelta);

            Vector2 remaining = new(
                reachableTarget.x - smoothedCenter.x,
                reachableTarget.z - smoothedCenter.z);
            if (remaining.sqrMagnitude > SmoothFocusCompletionDistanceSq ||
                (worldCamera != null && HasSmoothPerspectiveTarget))
                return smoothedCenter;

            smoothedCenter = reachableTarget;
            ClearSmoothFocusTarget();
            return smoothedCenter;
        }
 }
}
#endif
