using Game.Simulation;
namespace Game.Runtime {
 internal sealed partial class AutonomousWorld {
  double nextActualPending,nextActualActive;bool actualActiveObserved;
  internal void AdvanceActualPaidProduction(){
   if(ActualDeliveryOwner==null)throw new System.InvalidOperationException("Actual paid owner is not bound");
   bool pendingObserved=false;
   if(BuildingProductionCadenceKernel.BeginPending(SimulationSeconds,false,ref nextActualPending))
    pendingObserved=ActualDeliveryOwner.AdvancePending((float)SimulationSeconds,FixedDeltaTime);
   if(BuildingProductionCadenceKernel.ShouldUpdateActive(SimulationSeconds,false,actualActiveObserved,pendingObserved,nextActualActive)){
    bool observed=ActualDeliveryOwner.AdvanceActive((float)SimulationSeconds,FixedDeltaTime);
    BuildingProductionCadenceKernel.RecordActiveResult(SimulationSeconds,observed,ref actualActiveObserved,ref nextActualActive);
   }
  }
 }
}
