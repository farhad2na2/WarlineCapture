using Game.Simulation;
using System.Collections.Generic;namespace Game.Runtime {
 internal sealed partial class BuildingProductionUpdateCompositionSystemHelper {
  private readonly struct NativeProductionQueueView:ISharedProductionQueueView<RuntimeBuildingEntity,RuntimeBuildingEntity.PendingProduction>{
   readonly Context context;internal NativeProductionQueueView(Context actual){context=actual;}
   public IList<RuntimeBuildingEntity.PendingProduction> Pending(RuntimeBuildingEntity b)=>b.PendingProductions;
   public bool HasActiveTransport(RuntimeBuildingEntity b)=>b.ActiveTransport!=null;
   public SharedProductionPendingRead Read(RuntimeBuildingEntity.PendingProduction p)=>new SharedProductionPendingRead(p.ReceiptId!=0,p.TransportPrefab!=null,p.StartedAt,p.TransportArrivalSeconds);
   public void StartReceipt(RuntimeBuildingEntity.PendingProduction p){if(context.TransportContext.TransportBridgeContext.TryGetEntityManager!=null&&context.TransportContext.TransportBridgeContext.TryGetEntityManager(out var world))SkirmishProductionService.StartNativeQueue(world,p.ReceiptOwner,p.ReceiptId,p.ReceiptAttempt);}
   public float Remaining(RuntimeBuildingEntity.PendingProduction p,float now,bool cap)=>context.ProductionSystem.GetProgress(p,now,cap).RemainingSeconds;
   public bool Ready(RuntimeBuildingEntity.PendingProduction p,float now)=>context.ProductionSystem.IsReady(p,now);
   public bool ReadyWithin(RuntimeBuildingEntity.PendingProduction p,float now,float window)=>context.ProductionSystem.IsReadyWithin(p,now,window);
   public bool ShouldLaunch(RuntimeBuildingEntity.PendingProduction p,float now)=>context.ProductionSystem.ShouldLaunchTransport(p,now);
   public bool EnsureTransport(RuntimeBuildingEntity b,RuntimeBuildingEntity.PendingProduction p,float now,ref uint random)=>context.TransportSystem.TryEnsureActiveProductionTransport(context.TransportContext,b,p,now,ref random);
   public void Delay(RuntimeBuildingEntity.PendingProduction p,float delta)=>context.ProductionSystem.DelayPendingProduction(p,delta);
   public bool Spawn(RuntimeBuildingEntity b,RuntimeBuildingEntity.PendingProduction p,ref uint random)=>BuildingProductionTransportPresentationSystemHelper.TrySpawnPlayerUnitNearBuilding(context.TransportContext,b,p.ProductionIndex,p.ReservedProductionSlotIndex,null,null,ref random,p);
   public bool Consume(RuntimeBuildingEntity.PendingProduction p)=>p.ConsumeUnit();
   public bool RemoveAt(RuntimeBuildingEntity b,int i)=>context.ProductionSystem.RemovePendingAt(b.PendingProductions,i);
   public void Rebuild(RuntimeBuildingEntity b,float now,bool preserve)=>context.ProductionSystem.RebuildPendingProductionTimeline(b.PendingProductions,now,preserve);
  }
 }
}
