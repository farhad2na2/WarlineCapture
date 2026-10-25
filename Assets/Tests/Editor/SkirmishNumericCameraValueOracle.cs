#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;using Newtonsoft.Json.Linq;using System.IO;using System.Linq;using System.Reflection;using System.Security.Cryptography;using UnityEngine;using UnityEditor;using Unity.Mathematics;using Game.Components;using Game.Runtime;
public static class SkirmishNumericCameraValueOracle{
 [Serializable]public sealed class Sample{public string name;public int width,height;public float fov,near,far;public float[] position,rotation,right,forward,world_to_camera,projection,view_projection;public RayRow[] rays;public PointRow[] points;}
 [Serializable]public sealed class RayRow{public float[] viewport,origin,direction;}
 [Serializable]public sealed class PointRow{public string name;public float[] world,viewport;public bool original_death_inside;}
 [Serializable]public sealed class Pin{public string path,sha256;}
 [Serializable]public sealed class Receipt{public int schema_version=1;public string kind="SkirmishNumericCameraUnityValueOracle",status="Passed",unity_version,engineering_profile_sha256;public bool native_screen_readiness=false,mission_executed=false,simulator_input=true;public Sample[] samples;public Pin[] source_pins;}
 public static void RunFocusedValidation()=>Capture(null,null,true);
 public static void CaptureEngineeringReference(string profilePath,string output)=>Capture(profilePath,output,false);
 static void Capture(string profilePath,string requestedOutput,bool exit){
  GameObject owned=null;
  try{
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-skirmishCameraOracleOutput");if(requestedOutput==null&&(i<0||i+1>=args.Length||!Path.IsPathRooted(args[i+1])))throw new ArgumentException("Explicit absolute -skirmishCameraOracleOutput required");string output=Path.GetFullPath(requestedOutput??args[i+1]);float pitch=65,yaw=307,fov=46,normalHeight=20;string profileSha=null;if(profilePath!=null){var h=JObject.Parse(File.ReadAllText(profilePath));if((string)h["kind"]!="SkirmishEngineeringCameraProfileReference"||(string)h["catalog_id"]!="S026")throw new ArgumentException("Engineering camera header required");pitch=(float)h["pitch"];yaw=(float)h["yaw"];fov=(float)h["fov"];normalHeight=(float)h["normal_height"];if(!float.IsFinite(pitch)||!float.IsFinite(yaw)||!float.IsFinite(fov)||!float.IsFinite(normalHeight)||fov<=0||normalHeight<=0)throw new ArgumentException("Finite engineering camera parameters required");profileSha=Hash(profilePath);}if(File.Exists(output))throw new IOException("Unique oracle output required");Directory.CreateDirectory(Path.GetDirectoryName(output));
   owned=new GameObject("SkirmishNumericCameraOracle_Owned_"+Guid.NewGuid().ToString("N"));var camera=owned.AddComponent<Camera>();camera.enabled=false;camera.orthographic=false;camera.nearClipPlane=.3f;camera.farClipPlane=10000;camera.fieldOfView=fov;
   int[][] sizes={new[]{1280,720},new[]{1024,768},new[]{720,1280}};var samples=new Sample[3];
   var death=typeof(UnitDeathSystem).GetMethod("IsInsideCameraViewport",BindingFlags.Static|BindingFlags.NonPublic)??throw new MissingMethodException("Original source death predicate missing");
   for(int c=0;c<sizes.Length;c++){
    int width=sizes[c][0],height=sizes[c][1];camera.rect=new Rect(0,0,1,1);camera.pixelRect=new Rect(0,0,width,height);camera.aspect=(float)width/height;camera.transform.SetPositionAndRotation(new Vector3(870.0283f,normalHeight,325.60086f),Quaternion.Euler(pitch,yaw,0));
    var w=camera.worldToCameraMatrix;var p=camera.projectionMatrix;var vp=p*w;var snapshot=new RuntimeCameraSnapshotComponent{IsValid=1,Position=camera.transform.position,Rotation=camera.transform.rotation,WorldToCamera=ToFloat(w),Projection=ToFloat(p),ViewProjection=ToFloat(vp)};
    Vector3[] uv={new Vector3(.5f,.5f),new Vector3(0,0),new Vector3(1,0),new Vector3(0,1),new Vector3(1,1)};var rays=uv.Select(v=>{var ray=camera.ViewportPointToRay(v);return new RayRow{viewport=new[]{v.x,v.y},origin=V(ray.origin),direction=V(ray.direction)};}).ToArray();
    Vector3[] positions={camera.ViewportToWorldPoint(new Vector3(.5f,.5f,50)),camera.ViewportToWorldPoint(new Vector3(-.09f,.5f,50)),camera.ViewportToWorldPoint(new Vector3(-.11f,.5f,50)),camera.transform.position-camera.transform.forward*10};string[] names={"center","inside_padding","outside_padding","behind"};
    var points=positions.Select((v,n)=>new PointRow{name=names[n],world=V(v),viewport=V(camera.WorldToViewportPoint(v)),original_death_inside=(bool)death.Invoke(null,new object[]{snapshot,(float3)v})}).ToArray();
    if(!points[0].original_death_inside||!points[1].original_death_inside||points[2].original_death_inside||points[3].original_death_inside)throw new Exception("Original death fixture failed");
    samples[c]=new Sample{name=(profilePath==null?"source_s002_aspect_":"engineering_s026_aspect_")+width+"x"+height,width=width,height=height,fov=fov,near=.3f,far=10000,position=V(camera.transform.position),rotation=new[]{camera.transform.rotation.x,camera.transform.rotation.y,camera.transform.rotation.z,camera.transform.rotation.w},right=V(camera.transform.right),forward=V(camera.transform.forward),world_to_camera=M(w),projection=M(p),view_projection=M(vp),rays=rays,points=points};
   }
   string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));string[] inputs={"Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset","Assets/Game/Scenes/Match.unity","Assets/Game/Scripts/Rendering/Systems/RuntimeCameraReferenceSystem.cs","Assets/Game/Scripts/Components/RuntimeCameraReferenceComponent.cs","Assets/Game/Scripts/Systems/UnitDeathSystem.cs","Assets/Tests/Editor/SkirmishNumericCameraValueOracle.cs"};
   if(profilePath!=null)inputs=inputs.Where(x=>x!="Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset").Concat(new[]{profilePath,"Assets/Game/Scripts/Configs/Skirmish/SkirmishEngineeringViewProfileFactory.cs"}).ToArray();
   var receipt=new Receipt{schema_version=profilePath==null?1:2,engineering_profile_sha256=profileSha,unity_version=Application.unityVersion,samples=samples,source_pins=inputs.Select(x=>new Pin{path=x,sha256=Hash(Path.Combine(root,x))}).ToArray()};
   UnityEngine.Object.DestroyImmediate(owned);owned=null;
   string temporary=output+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(receipt,true));File.Move(temporary,output);Debug.Log("[SkirmishNumericCameraOracle] result=Passed aspects=3 rays=15 deathCases=12 output="+output);if(exit)ValidationExit.Exit(0);
  }catch(Exception e){Debug.LogException(e);Debug.LogError("[SkirmishNumericCameraOracle] result=Failed");if(exit)ValidationExit.Exit(1);else throw;}
  finally{if(owned!=null)UnityEngine.Object.DestroyImmediate(owned);}
 }
 static string Hash(string p){using(var s=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static float[] M(Matrix4x4 m)=>new[]{m.m00,m.m10,m.m20,m.m30,m.m01,m.m11,m.m21,m.m31,m.m02,m.m12,m.m22,m.m32,m.m03,m.m13,m.m23,m.m33};
 static float4x4 ToFloat(Matrix4x4 m)=>new float4x4(new float4(m.m00,m.m10,m.m20,m.m30),new float4(m.m01,m.m11,m.m21,m.m31),new float4(m.m02,m.m12,m.m22,m.m32),new float4(m.m03,m.m13,m.m23,m.m33));
}
#endif
