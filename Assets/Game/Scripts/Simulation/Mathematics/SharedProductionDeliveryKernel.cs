using System;
using Unity.Mathematics;
namespace Game.Simulation {
 // Shared data owner: no Engine.Object, presentation callback, clock read, actor allocation or resource bank.
 // Both Unity presentation and headless owner must use these computed phase transitions.
 public static class SharedProductionDeliveryKernel {
  public struct Transport {
   public int Phase; public float PhaseStartedAt, ArrivalSeconds, DepartureSeconds, HoldSeconds, HoverEnteredAt, NextDropReadyAt;
   public float3 Entry, Hover, Exit, Position; public quaternion EntryRotation, HoverRotation, ExitRotation, Rotation;
   public bool HasDrop; public float DropStartedAt, DropDuration; public float3 DropStart, DropEnd, DropPosition; public int Delivered;
  }
  public enum Transition { None, Arrived, Delivery, Departed }
  static void Finite(float x) { if(!math.isfinite(x)) throw new ArgumentException("Finite source time required"); }
  public static Transport Helicopter(float now,float arrival,float hold,float3 entry,float3 hover,float3 exit,quaternion entryRotation,quaternion hoverRotation,quaternion exitRotation) {
   Finite(now);Finite(arrival);Finite(hold); if(!math.all(math.isfinite(entry))||!math.all(math.isfinite(hover))||!math.all(math.isfinite(exit)))throw new ArgumentException("Finite source geometry required");
   return new Transport { Phase=0,PhaseStartedAt=now,ArrivalSeconds=math.max(.5f,arrival),DepartureSeconds=math.max(4f,arrival),HoldSeconds=math.max(.5f,hold),HoverEnteredAt=-1,NextDropReadyAt=now,Entry=entry,Hover=hover,Exit=exit,Position=entry,EntryRotation=entryRotation,HoverRotation=hoverRotation,ExitRotation=exitRotation,Rotation=entryRotation };
  }
  // Source UpdateArrivalPhase96-140 / UpdateDeliveryPhase142-185 / UpdateDeparturePhase298-310.
  // It deliberately does not process delivery during the arrival update that enters phase1.
  public static Transition Advance(ref Transport t,float now) {
   Finite(now);
   if(t.Phase==0) { float duration=math.max(.5f,t.ArrivalSeconds);float a=math.clamp((now-t.PhaseStartedAt)/duration,0f,1f);t.Position=Lerp(t.Entry,t.Hover,a);t.Rotation=math.slerp(t.EntryRotation,t.HoverRotation,a);if(a<1f)return Transition.None;t.Phase=1;t.PhaseStartedAt=now;t.HoverEnteredAt=now;t.NextDropReadyAt=now;return Transition.Arrived; }
   if(t.Phase==1) {t.Position=t.Hover;t.Rotation=t.HoverRotation;return Transition.Delivery;}
   if(t.Phase==2) {float duration=math.max(.5f,t.DepartureSeconds);float a=math.clamp((now-t.PhaseStartedAt)/duration,0f,1f);t.Position=Lerp(t.Hover,t.Exit,a);t.Rotation=math.slerp(t.HoverRotation,t.ExitRotation,a);return a<1f?Transition.None:Transition.Departed;}
   throw new InvalidOperationException("Unknown source transport phase");
  }
  public static void DepartIfNoSoonPending(ref Transport t,float now,bool hasSoonPending) {if(t.Phase!=1||t.HasDrop)throw new InvalidOperationException("Departure ordering");if(!hasSoonPending&&now>=t.HoverEnteredAt+t.HoldSeconds){t.Phase=2;t.PhaseStartedAt=now;}}
  public static void BeginHelicopterDrop(ref Transport t,float now,float3 start,float3 end) {if(t.Phase!=1||t.HasDrop||now<t.NextDropReadyAt)throw new InvalidOperationException("Source drop admission/order");t.HasDrop=true;t.DropStartedAt=now;t.DropDuration=2f;t.DropStart=start;t.DropEnd=end;t.DropPosition=start;}
  public static bool AdvanceDrop(ref Transport t,float now) {if(!t.HasDrop)throw new InvalidOperationException("No active source drop");float a=math.clamp((now-t.DropStartedAt)/math.max(.01f,t.DropDuration),0f,1f);t.DropPosition=Lerp(t.DropStart,t.DropEnd,a);return a>=1f;}
  public static void FinishDrop(ref Transport t,float now,bool actorSpawned) {if(!t.HasDrop)throw new InvalidOperationException("No source drop to finish");if(actorSpawned)t.Delivered++;t.HasDrop=false;t.NextDropReadyAt=now;}
  static float3 Lerp(float3 a,float3 b,float t) {t=math.clamp(t,0f,1f);return new float3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);}
 }
}
