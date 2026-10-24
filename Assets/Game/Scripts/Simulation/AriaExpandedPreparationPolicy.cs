namespace Game.Simulation {
 public enum AriaPreparationAction:byte{None,RecruitLogisticsTruck,RecruitTanker,RecruitRifle}
 public struct AriaPreparationView {public bool AirProfile,CombinedArmsField,LogisticsTruckCommitted,CanAffordLogisticsTruck,LogisticsTargetAvailable,CanAffordTanker,TankerTargetAvailable,RifleRecruitPending,CanAffordRifle,RifleTargetAvailable;public int Infantry,OwnStructureAttackUnits,TankersCommitted;public float Time;}
 public struct AriaPreparationState {public byte AssaultStarted;public float OpeningUntil;}
 public static class AriaExpandedPreparationPolicy {
  public static AriaPreparationAction Supply(in AriaPreparationView view,ref AriaPreparationState plan){
   bool openingNeedsInfantry=!view.CombinedArmsField&&plan.AssaultStarted==0&&view.Infantry<20&&(plan.OpeningUntil==0||view.Time<plan.OpeningUntil);
   bool supplyReady=view.AirProfile&&!openingNeedsInfantry&&(!view.CombinedArmsField||view.OwnStructureAttackUnits>=8);
   if(supplyReady&&!view.CombinedArmsField)plan.AssaultStarted=1;
   if(supplyReady&&!view.LogisticsTruckCommitted&&view.CanAffordLogisticsTruck&&view.LogisticsTargetAvailable)return AriaPreparationAction.RecruitLogisticsTruck;
   if(supplyReady&&view.LogisticsTruckCommitted&&view.TankersCommitted<2&&view.CanAffordTanker&&view.TankerTargetAvailable)return AriaPreparationAction.RecruitTanker;
   return AriaPreparationAction.None;
  }
  public static AriaPreparationAction FallbackGroundRecruit(in AriaPreparationView view)=>!view.AirProfile&&!view.RifleRecruitPending&&view.Infantry<16&&view.CanAffordRifle&&view.RifleTargetAvailable?AriaPreparationAction.RecruitRifle:AriaPreparationAction.None;
 }
 public struct AriaEconomicProgressView {public bool AirProfile,LogisticsTruckCommitted,ReadinessEligible,PadPresent,AirQueueOffered;public int OwnMaterials;}
 public struct AriaEconomicProgressState {public int EconomicMilestones,MaterialsHighWater;}
 public readonly struct AriaEconomicProgressResult {public readonly bool NewMilestone,SupplyProgress;public AriaEconomicProgressResult(bool milestone,bool supply){NewMilestone=milestone;SupplyProgress=supply;}}
 public static class AriaEconomicProgressPolicy {
  public static AriaEconomicProgressResult Observe(in AriaEconomicProgressView view,ref AriaEconomicProgressState plan){
   int milestones=(view.LogisticsTruckCommitted?1:0)|(view.ReadinessEligible?2:0)|(view.PadPresent?4:0);
   bool newMilestone=view.AirProfile&&(milestones&~plan.EconomicMilestones)!=0;
   if(newMilestone)plan.MaterialsHighWater=view.OwnMaterials;
   bool savingForAir=view.AirProfile&&view.LogisticsTruckCommitted&&!view.AirQueueOffered;
   int savedMaterials=System.Math.Min(view.OwnMaterials,300);
   bool supplyProgress=savingForAir&&savedMaterials>plan.MaterialsHighWater;
   plan.EconomicMilestones|=milestones;if(supplyProgress)plan.MaterialsHighWater=savedMaterials;
   return new AriaEconomicProgressResult(newMilestone,supplyProgress);
  }
 }

}
