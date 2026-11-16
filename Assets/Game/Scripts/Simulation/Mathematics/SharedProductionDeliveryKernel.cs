using System;
using Unity.Mathematics;
namespace Game.Simulation {
 // Shared data owner: no Engine.Object, presentation callback, clock read, actor allocation or resource bank.
 // Both Unity presentation and headless owner must use these computed phase transitions.
 public static class SharedProductionDeliveryKernel {
  const float ProductionTransportLaneSpacing=12f;
 // Mission-owned world frame for deterministic production delivery geometry.
 // This is plain data, independent of any camera or presentation object.
 public readonly struct ProductionDeliveryLogicalBasis {
  public readonly float3 Right;
  public readonly float3 Forward;
  public ProductionDeliveryLogicalBasis(float3 right,float3 forward){Right=right;Forward=forward;}
  public static ProductionDeliveryLogicalBasis WorldAxes => new ProductionDeliveryLogicalBasis(new float3(1f,0f,0f),new float3(0f,0f,1f));
 }
 public static bool TryResolveAxisAlignedMapFrame(int width,int height,float cellSize,float3 origin,out ProductionDeliveryLogicalBasis basis) {
  basis=default;
  if(width<=0||height<=0||!math.isfinite(cellSize)||cellSize<=0f||!math.all(math.isfinite(origin)))return false;
  // Current authored operation-map grids have origin/dimensions/cell size and no
  // delivery orientation. Their logical X/Z axes are the world X/Z axes.
  basis=ProductionDeliveryLogicalBasis.WorldAxes;
  return true;
 }
 public static bool IsValidProductionDeliveryLogicalBasis(in ProductionDeliveryLogicalBasis basis) {
  return math.all(math.isfinite(basis.Right))&&math.all(math.isfinite(basis.Forward))&&
   math.lengthsq(basis.Right)>1e-10f&&math.lengthsq(basis.Forward)>1e-10f&&
   math.abs(basis.Right.y)<=1e-5f&&math.abs(basis.Forward.y)<=1e-5f;
 }
 public static float3 ProductionTransportIngressOffset(in ProductionDeliveryLogicalBasis basis,float distance) {
  return -NormalizeLikeUnity(basis.Right)*distance;
 }
 public static float3 ProductionTransportLaneOffsetForLogicalBasis(in ProductionDeliveryLogicalBasis basis,int laneIndex,int maxConcurrent) {
  int safeMax=math.max(1,maxConcurrent);
  float centered=laneIndex-((safeMax-1)*.5f);
  float3 axis=NormalizeLikeUnity(basis.Forward);
  axis.y=0f;
  if(math.lengthsq(axis)<=.0001f)axis=new float3(0f,0f,1f);
  axis=NormalizeLikeUnity(axis);
  return axis*(centered*ProductionTransportLaneSpacing);
 }
 static float3 NormalizeLikeUnity(float3 value) {
  float magnitude=math.sqrt(math.lengthsq(value));
  return magnitude>1e-5f?value/magnitude:float3.zero;
 }
  // One Unity-backed implementation used by native presentation and the source
  // projection. The camera remains an input owned by the caller.
  public static UnityEngine.Vector3 ProductionTransportLaneOffset(bool hasCameraForward,UnityEngine.Vector3 cameraForward,int laneIndex,int maxConcurrent) {
   int safeMax=UnityEngine.Mathf.Max(1,maxConcurrent);
   float centered=laneIndex-((safeMax-1)*.5f);
   UnityEngine.Vector3 axis=hasCameraForward?cameraForward.normalized:UnityEngine.Vector3.forward;
   axis.y=0f;
   if(axis.sqrMagnitude<=.0001f)axis=UnityEngine.Vector3.forward;
   axis.Normalize();
   return axis*(centered*ProductionTransportLaneSpacing);
  }
  public struct Transport {
   public int Phase; public float PhaseStartedAt, ArrivalSeconds, DepartureSeconds, HoldSeconds, HoverEnteredAt, NextDropReadyAt;
   public float3 Entry, Hover, Exit, Position; public quaternion EntryRotation, HoverRotation, ExitRotation, Rotation;
   public bool HasDrop; public float DropStartedAt, DropDuration; public float3 DropStart, DropEnd, DropPosition; public int Delivered;
  }
  // Timing-only source policy. It intentionally carries no endpoints, actor,
  // transform, or presentation pose so headless controls can exercise the
  // original drop clock without fabricating visual state.
  public readonly struct DropTiming {
   public readonly float StartedAt,Duration;
   public DropTiming(float startedAt,float duration){StartedAt=startedAt;Duration=duration;}
  }
  // Phase clock policy is deliberately independent of route geometry and
  // presentation. The existing owners apply the returned progress to their
  // original position/rotation side effects after this shared decision.
  public struct PhaseTiming {
   public int Phase; public float PhaseStartedAt, ArrivalSeconds, DepartureSeconds, HoldSeconds, HoverEnteredAt, NextDropReadyAt;
   public PhaseTiming(int phase,float phaseStartedAt,float arrivalSeconds,float departureSeconds,float holdSeconds,float hoverEnteredAt,float nextDropReadyAt){Phase=phase;PhaseStartedAt=phaseStartedAt;ArrivalSeconds=arrivalSeconds;DepartureSeconds=departureSeconds;HoldSeconds=holdSeconds;HoverEnteredAt=hoverEnteredAt;NextDropReadyAt=nextDropReadyAt;}
  }
  public readonly struct PhaseStep {
   public readonly int EvaluatedPhase; public readonly float Progress; public readonly Transition Transition;
   public PhaseStep(int evaluatedPhase,float progress,Transition transition){EvaluatedPhase=evaluatedPhase;Progress=progress;Transition=transition;}
  }
  public enum Transition { None, Arrived, Delivery, Departed }
  static void Finite(float x) { if(!math.isfinite(x)) throw new ArgumentException("Finite source time required"); }
  public static Transport Helicopter(float now,float arrival,float hold,float3 entry,float3 hover,float3 exit,quaternion entryRotation,quaternion hoverRotation,quaternion exitRotation) {
   Finite(now);Finite(arrival);Finite(hold); if(!math.all(math.isfinite(entry))||!math.all(math.isfinite(hover))||!math.all(math.isfinite(exit)))throw new ArgumentException("Finite source geometry required");var timing=HelicopterPhaseTiming(now,arrival,hold);
   return new Transport { Phase=timing.Phase,PhaseStartedAt=timing.PhaseStartedAt,ArrivalSeconds=timing.ArrivalSeconds,DepartureSeconds=timing.DepartureSeconds,HoldSeconds=timing.HoldSeconds,HoverEnteredAt=timing.HoverEnteredAt,NextDropReadyAt=timing.NextDropReadyAt,Entry=entry,Hover=hover,Exit=exit,Position=entry,EntryRotation=entryRotation,HoverRotation=hoverRotation,ExitRotation=exitRotation,Rotation=entryRotation };
  }
  public static PhaseTiming HelicopterPhaseTiming(float now,float arrival,float hold){Finite(now);Finite(arrival);Finite(hold);return new PhaseTiming(0,now,math.max(.5f,arrival),math.max(4f,arrival),math.max(.5f,hold),-1f,now);}
  // Source UpdateArrivalPhase96-140 / UpdateDeliveryPhase142-185 / UpdateDeparturePhase298-310.
  // It deliberately does not process delivery during the arrival update that enters phase1.
  public static PhaseStep AdvancePhase(ref PhaseTiming t,float now) {
   Finite(now);
   if(t.Phase==0) { float duration=math.max(.5f,t.ArrivalSeconds);float a=math.clamp((now-t.PhaseStartedAt)/duration,0f,1f);if(a<1f)return new PhaseStep(0,a,Transition.None);t.Phase=1;t.PhaseStartedAt=now;t.HoverEnteredAt=now;t.NextDropReadyAt=now;return new PhaseStep(0,a,Transition.Arrived); }
   if(t.Phase==1) return new PhaseStep(1,1f,Transition.Delivery);
   if(t.Phase==2) {float duration=math.max(.5f,t.DepartureSeconds);float a=math.clamp((now-t.PhaseStartedAt)/duration,0f,1f);return new PhaseStep(2,a,a<1f?Transition.None:Transition.Departed);}
   throw new InvalidOperationException("Unknown source transport phase");
  }
  public static Transition Advance(ref Transport t,float now) {
   var timing=new PhaseTiming(t.Phase,t.PhaseStartedAt,t.ArrivalSeconds,t.DepartureSeconds,t.HoldSeconds,t.HoverEnteredAt,t.NextDropReadyAt);
   var step=AdvancePhase(ref timing,now);
   t.Phase=timing.Phase;t.PhaseStartedAt=timing.PhaseStartedAt;t.HoverEnteredAt=timing.HoverEnteredAt;t.NextDropReadyAt=timing.NextDropReadyAt;
   if(step.EvaluatedPhase==0){t.Position=Lerp(t.Entry,t.Hover,step.Progress);t.Rotation=math.slerp(t.EntryRotation,t.HoverRotation,step.Progress);}
   else if(step.EvaluatedPhase==1){t.Position=t.Hover;t.Rotation=t.HoverRotation;}
   else {t.Position=Lerp(t.Hover,t.Exit,step.Progress);t.Rotation=math.slerp(t.HoverRotation,t.ExitRotation,step.Progress);}
   return step.Transition;
  }
  public static void DepartIfNoSoonPending(ref Transport t,float now,bool hasSoonPending) {if(t.Phase!=1||t.HasDrop)throw new InvalidOperationException("Departure ordering");if(!hasSoonPending&&now>=t.HoverEnteredAt+t.HoldSeconds){t.Phase=2;t.PhaseStartedAt=now;}}
  public static DropTiming BeginDropTiming(float now,float duration)=>new DropTiming(now,duration);
  // Preserve the original finite and edge arithmetic exactly: clamp elapsed /
  // max(.01, duration) to [0,1], then complete at 1.
  public static float DropProgress(in DropTiming timing,float now)=>math.clamp((now-timing.StartedAt)/math.max(.01f,timing.Duration),0f,1f);
  static DropTiming BeginHelicopterDropTimingCore(int phase,bool hasDrop,float nextDropReadyAt,float now){if(phase!=1||hasDrop||now<nextDropReadyAt)throw new InvalidOperationException("Source drop admission/order");return BeginDropTiming(now,2f);}
  public static void BeginHelicopterDropTiming(ref Transport t,float now) {var timing=BeginHelicopterDropTimingCore(t.Phase,t.HasDrop,t.NextDropReadyAt,now);t.HasDrop=true;t.DropStartedAt=timing.StartedAt;t.DropDuration=timing.Duration;}
  public static void BeginHelicopterDropTiming(ref PhaseTiming phase,ref DropTiming drop,ref bool hasDrop,float now) {drop=BeginHelicopterDropTimingCore(phase.Phase,hasDrop,phase.NextDropReadyAt,now);hasDrop=true;}
  public static void BeginHelicopterDrop(ref Transport t,float now,float3 start,float3 end) {BeginHelicopterDropTiming(ref t,now);t.DropStart=start;t.DropEnd=end;t.DropPosition=start;}
  public static float AdvanceDropTiming(in DropTiming timing,bool hasDrop,float now){if(!hasDrop)throw new InvalidOperationException("No active source drop");return DropProgress(timing,now);}
  public static float AdvanceDropTiming(ref Transport t,float now)=>AdvanceDropTiming(new DropTiming(t.DropStartedAt,t.DropDuration),t.HasDrop,now);
  public static bool AdvanceDrop(ref Transport t,float now) {float a=AdvanceDropTiming(ref t,now);t.DropPosition=Lerp(t.DropStart,t.DropEnd,a);return a>=1f;}
  static void FinishDropTimingCore(ref bool hasDrop,ref int delivered,ref float nextDropReadyAt,float now,bool actorSpawned){if(!hasDrop)throw new InvalidOperationException("No source drop to finish");if(actorSpawned)delivered++;hasDrop=false;nextDropReadyAt=now;}
  public static void FinishDrop(ref Transport t,float now,bool actorSpawned) {FinishDropTimingCore(ref t.HasDrop,ref t.Delivered,ref t.NextDropReadyAt,now,actorSpawned);}
  public static void FinishDrop(ref PhaseTiming phase,ref bool hasDrop,ref int delivered,float now,bool actorSpawned){FinishDropTimingCore(ref hasDrop,ref delivered,ref phase.NextDropReadyAt,now,actorSpawned);}
  static float3 Lerp(float3 a,float3 b,float t) {t=math.clamp(t,0f,1f);return new float3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);}
 }
}
