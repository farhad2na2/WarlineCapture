using System.Collections.Generic;
namespace Game.Simulation {
 // Engine-free source production policy. Views borrow the owner's existing pending
 // references; they never copy/reseed world, queue, bank, pose or receipt state.
 public readonly struct SharedProductionPendingRead {
  public readonly bool HasReceipt,HasTransport;public readonly float StartedAt,TransportArrivalSeconds;
  public SharedProductionPendingRead(bool receipt,bool transport,float started,float arrival){HasReceipt=receipt;HasTransport=transport;StartedAt=started;TransportArrivalSeconds=arrival;}
 }
 public interface ISharedProductionQueueView<TBuilding,TPending> where TBuilding:class where TPending:class {
  IList<TPending> Pending(TBuilding building);bool HasActiveTransport(TBuilding building);SharedProductionPendingRead Read(TPending pending);
  void StartReceipt(TPending pending);float Remaining(TPending pending,float now,bool capTransport);bool Ready(TPending pending,float now);bool ReadyWithin(TPending pending,float now,float window);bool ShouldLaunch(TPending pending,float now);
  bool EnsureTransport(TBuilding building,TPending pending,float now,ref uint random);void Delay(TPending pending,float delta);
  bool Spawn(TBuilding building,TPending pending,ref uint random);bool Consume(TPending pending);bool RemoveAt(TBuilding building,int index);void Rebuild(TBuilding building,float now,bool preserveActiveProgress);
 }
 public static class SharedProductionQueueKernel {
  public const int MaxTransportLaunchesPerTick=1,MaxImmediateProductionSpawnsPerTick=2;
  // Extracted from original BuildingProductionUpdateCompositionSystemHelper145-223.
  public static bool UpdateBuilding<TBuilding,TPending,TView>(ref TView view,TBuilding building,float now,float delta,ref uint random,ref int remainingLaunches,ref int remainingSpawns)where TBuilding:class where TPending:class where TView:struct,ISharedProductionQueueView<TBuilding,TPending>{
   if(building==null)return false;bool hasActiveTransport=view.HasActiveTransport(building);var pendingRows=view.Pending(building);if(pendingRows==null||pendingRows.Count==0)return hasActiveTransport;
   for(int i=pendingRows.Count-1;i>=0;i--){var pending=pendingRows[i];if(pending==null){if(view.RemoveAt(building,i))view.Rebuild(building,now,i>0);continue;}var row=view.Read(pending);if(row.HasReceipt&&now>=row.StartedAt)view.StartReceipt(pending);float remaining=view.Remaining(pending,now,row.HasTransport);
    if(row.HasTransport){float window=.5f>row.TransportArrivalSeconds?.5f:row.TransportArrivalSeconds;if(view.ReadyWithin(pending,now,window)||view.ShouldLaunch(pending,now)){if(remainingLaunches<=0)continue;bool had=view.HasActiveTransport(building);if(!view.EnsureTransport(building,pending,now,ref random))view.Delay(pending,delta);else if(!had){hasActiveTransport=true;remainingLaunches--;}}continue;}
    if(remaining>0||!view.Ready(pending,now))continue;if(remainingSpawns<=0)continue;if(view.Spawn(building,pending,ref random)){remainingSpawns--;if(view.Consume(pending)&&view.RemoveAt(building,i))view.Rebuild(building,now,false);}
   }return hasActiveTransport||view.HasActiveTransport(building);
  }
 }
 public readonly struct SharedProductionTransportRead {public readonly bool Present,HasTransform;public readonly int Mode,Phase;public SharedProductionTransportRead(bool present,bool transform,int mode,int phase){Present=present;HasTransform=transform;Mode=mode;Phase=phase;}}
 public interface ISharedProductionTransportView<TBuilding>where TBuilding:class {SharedProductionTransportRead Read(TBuilding building);void Spin(TBuilding building,float delta);void Arrival(TBuilding building,float now);void Delivery(TBuilding building,float now,ref uint random);void Departure(TBuilding building,float now);void Publish();}
 public static class SharedProductionTransportDispatchKernel {
  // Original transport helper69-94: spin precedes phase callback, unknown phase
  // performs no phase callback, and readmodel publication follows valid transport.
  public static void Step<TBuilding,TView>(ref TView view,TBuilding building,float now,float delta,ref uint random)where TBuilding:class where TView:struct,ISharedProductionTransportView<TBuilding>{if(building==null)return;var t=view.Read(building);if(!t.Present||!t.HasTransform)return;if(t.Mode==0||t.Mode==2)view.Spin(building,delta);switch(t.Phase){case 0:view.Arrival(building,now);break;case 1:view.Delivery(building,now,ref random);break;case 2:view.Departure(building,now);break;}view.Publish();}
 }
}
