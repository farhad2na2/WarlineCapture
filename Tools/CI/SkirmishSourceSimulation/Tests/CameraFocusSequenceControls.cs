extern alias ActualCore;
using System; using System.Collections.Generic; using Game.Simulation;
internal static class CameraFocusSequenceControls {
 static void Check(bool v){if(!v)throw new Exception("camera focus sequence control failed");}
 static string Sequence(CameraFocusRequest r){var a=new List<CameraFocusAction>();CameraFocusSequence.Emit(r,a.Add);return string.Join(",",a.ConvertAll(x=>x.Kind.ToString()));}
 static void Numeric(){
  Game.Runtime.TransportPose.PoseMath.AdmitInstalledMono();
  var owner=(Game.Runtime.SourceS002NumericCameraProvider)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Game.Runtime.SourceS002NumericCameraProvider));
  var t=owner.GetType();System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  void Set(string n, object v)=>t.GetField(n,flags).SetValue(owner,v);
  var data=new Game.Runtime.SourceNumericCameraData{Position=new ActualCore::UnityEngine.Vector3(5000,100,5000),Rotation=Game.Runtime.TransportPose.PoseMath.Euler(new Unity.Mathematics.float3(58,0,0)),FieldOfView=60,Near=.3f,Far=10000,Aspect=1280f/720f};
  var motion=new Game.Runtime.SourceNumericCameraMotion(0,0,10000,10000);Set("poseEnabled",true);Set("motionValuesAdmitted",true);Set("cameraData",data);Set("motion",motion);
  var expected=new Game.Runtime.SourceNumericCameraData{Position=data.Position,Rotation=data.Rotation,FieldOfView=60,Near=.3f,Far=10000,Aspect=1280f/720f};var reference=new Game.Runtime.SourceNumericCameraMotion(0,0,10000,10000);
  expected.Rotation=Game.Runtime.TransportPose.PoseMath.Euler(new Unity.Mathematics.float3(58,0,0));expected.Position=new ActualCore::UnityEngine.Vector3(expected.Position.x,40,expected.Position.z);expected.FieldOfView=60;reference.ClampCameraToGroundBoundary(expected);reference.MoveCameraGroundCenterTo(expected,new ActualCore::UnityEngine.Vector3(5100,0,5100));reference.ClearSmoothFocusTarget();
  owner.FocusIntroSettlePending=true;owner.FocusSmoothPerspectiveTarget=true;owner.FocusZoomTransitionActive=true;owner.FocusHeightVelocity=3;owner.FocusPitchVelocity=4;owner.FocusYawVelocity=5;owner.FocusFovVelocity=6;
  owner.ExecuteImmediateFocus(new CameraFocusRequest{ExplicitPerspective=true,Height=40,Pitch=58,FieldOfView=60,FocusX=5100,FocusZ=5100});
  Check(data.Position.x==expected.Position.x&&data.Position.y==expected.Position.y&&data.Position.z==expected.Position.z&&data.FieldOfView==expected.FieldOfView&&Unity.Mathematics.math.all(data.Rotation.value==expected.Rotation.value));
  Check(!owner.FocusIntroSettlePending&&!owner.FocusSmoothPerspectiveTarget&&!owner.FocusZoomTransitionActive&&owner.FocusHeightVelocity==0&&owner.FocusPitchVelocity==0&&owner.FocusYawVelocity==0&&owner.FocusFovVelocity==0&&owner.LastFocusActionCount==6);
  var before=data.Position;bool rejected=false;try{owner.ExecuteImmediateFocus(new CameraFocusRequest{ExplicitPerspective=true,Smooth=true});}catch(NotSupportedException){rejected=true;}Check(rejected&&data.Position.x==before.x&&data.Position.y==before.y&&data.Position.z==before.z&&owner.LastFocusActionCount==6); rejected=false;try{owner.ExecuteImmediateFocus(new CameraFocusRequest{TacticalReveal=2});}catch(NotSupportedException){rejected=true;}Check(rejected&&data.Position.x==before.x&&data.Position.y==before.y&&data.Position.z==before.z&&owner.LastFocusActionCount==6);
 }
 static int Main(){try{
 Check(Sequence(new CameraFocusRequest{ExplicitPerspective=true})=="ClearIntroSettle,ClearPerspective,CompleteZoom,InstantPerspective,MoveFocus,ClearFocus");
 Check(Sequence(new CameraFocusRequest{ExplicitPerspective=true,Smooth=true})=="ClearIntroSettle,ClearPerspective,CompleteZoom,SmoothPerspective,SmoothFocus");
 Check(Sequence(new CameraFocusRequest{TacticalReveal=2})=="ClearIntroSettle,InstantPerspective,CompleteZoom,MoveFocus,ClearFocus");
 Check(Sequence(new CameraFocusRequest{TacticalReveal=3,Smooth=true,Duration=.2f})=="ClearIntroSettle,CompleteZoom,SmoothPerspective,SmoothFocus");
 Check(Sequence(new CameraFocusRequest())=="MoveFocus,ClearFocus");
 var rows=new List<CameraFocusAction>();CameraFocusSequence.Tactical(4,.2f,40,58,7,60,false,0,rows.Add);Check(rows.Count==3&&rows[1].Kind==CameraFocusActionKind.CompleteZoom&&rows[2].A==40&&rows[2].B==58&&rows[2].C==7&&rows[2].D==60);
 rows.Clear();CameraFocusSequence.Emit(new CameraFocusRequest{ExplicitPerspective=true,Height=40,Pitch=58,Yaw=0,FieldOfView=60,FocusX=820,FocusZ=606},rows.Add);Check(rows[3].A==40&&rows[3].B==58&&rows[3].D==60&&rows[4].A==820&&rows[4].C==606);
 rows.Clear();CameraFocusSequence.Emit(new CameraFocusRequest{TacticalReveal=2,Smooth=true,Duration=.2f},rows.Add);Check(rows[2].Duration==.6f&&rows[3].Duration==.2f);
 var legacy=CameraFocusRequestPolicy.Calculate(new CameraFocusRequestPolicyInput{Opening=true,BasinScenario=true,BaseX=800,BaseY=2,BaseZ=600});Check(legacy.X==820&&legacy.Y==2&&legacy.Z==606&&legacy.Height==60&&legacy.Pitch==58&&legacy.Yaw==0&&legacy.FieldOfView==60);
 var s002=CameraFocusRequestPolicy.Calculate(new CameraFocusRequestPolicyInput{S002=true,BaseX=800,BaseY=2,BaseZ=600,HasMissionView=true,ViewHeight=75,ViewPitch=50,ViewYaw=10,ViewFieldOfView=44});Check(s002.X==830&&s002.Z==600&&s002.Height==75&&s002.Pitch==50&&s002.Yaw==10&&s002.FieldOfView==44);
 var envelope=CameraFocusRequestPolicy.Calculate(new CameraFocusRequestPolicyInput{Opening=true,AuthoredOpening=true,MinimumX=0,MinimumY=2,MinimumZ=20,MaximumX=100,MaximumY=4,MaximumZ=40});Check(envelope.X==50&&envelope.Y==3&&envelope.Z==30&&envelope.Height==160);
 var campaign=CameraFocusRequestPolicy.Calculate(new CameraFocusRequestPolicyInput{Opening=true,S004=true,BaseX=100,BaseZ=200,CampaignHeight=70,CampaignYaw=-35});Check(campaign.X==88&&campaign.Z==192&&campaign.Height==70&&campaign.Pitch==50&&campaign.Yaw==-35&&campaign.FieldOfView==50);
 Numeric();
 Console.WriteLine("[CameraFocusSequenceControls] result=Passed checks=16");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}