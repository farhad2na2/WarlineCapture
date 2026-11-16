using System;using Unity.Entities;using Game.Components;using Game.Runtime.TransportPose;
namespace Game.Runtime{
internal sealed partial class AutonomousWorld{
 private SourceS002NumericCameraProvider sourceNumericCamera;
 internal SourceS002NumericCameraProvider SourceNumericCamera=>sourceNumericCamera;
 internal void BindSourceNumericCamera(SourceS002NumericCameraProvider sameDeliveryCamera,string scenePath,string sceneSha,int width,int height,string matrixOraclePath,string matrixOracleSha,string motionOraclePath,string motionOracleSha){
  if(disposed||sourceNumericCamera!=null||sameDeliveryCamera==null)throw new InvalidOperationException("Bind once to same delivery camera owner");
  sameDeliveryCamera.EnableNumericPose(Entities,Session,scenePath,sceneSha,width,height);
  sameDeliveryCamera.AdmitNativeValueOracle(matrixOraclePath,matrixOracleSha);sameDeliveryCamera.AdmitConsumedMotionOracle(motionOraclePath,motionOracleSha);sourceNumericCamera=sameDeliveryCamera;
 }
 // Scheduler calls immediately after original SetGeneratedClock, before production/death.
 // Contact effects are queued by actual source ARIA after objectives/HUD/ECB for next tick.
 internal void ValidateSourceNumericDeathCamera(){if(sourceNumericCamera==null)throw new InvalidOperationException("Numeric deathcamera missing");sourceNumericCamera.ValidateOriginalCorpseViewportDecisions();}
 internal void AdvanceSourceNumericCamera(){
  if(disposed||sourceNumericCamera==null)throw new InvalidOperationException("Actual numeric camera owner missing");
  sourceNumericCamera.AdvanceNumericCamera(UnityEngine.Time.unscaledTime,UnityEngine.Time.unscaledDeltaTime);sourceNumericCamera.PublishSourceSnapshot();
 }
}
}
