namespace Game.Simulation {
 public enum AriaPreparationAction:byte{None,RecruitLogisticsTruck,RecruitTanker,RecruitRifle}
 public enum AriaAirOpeningAction:byte{NotApplicable,WaitForPending,RecruitRifle,ContinueAssault}
 public struct AriaPreparationView {public bool AirProfile,CombinedArmsField,LogisticsTruckCommitted,CanAffordLogisticsTruck,LogisticsTargetAvailable,CanAffordTanker,TankerTargetAvailable,RifleRecruitPending,CanAffordRifle,RifleTargetAvailable;public int Infantry,OwnStructureAttackUnits,TankersCommitted;public float Time;}
 public struct AriaPreparationState {public byte AssaultStarted;public float OpeningUntil;}
 public static class AriaExpandedPreparationPolicy {
  // The decision is independent of a drawer, page, button or touch target.
  public static AriaAirOpeningAction AirOpening(in AriaPreparationView view,ref AriaPreparationState plan){
   if(!view.AirProfile||plan.AssaultStarted!=0)return AriaAirOpeningAction.NotApplicable;
   if(plan.OpeningUntil==0)plan.OpeningUntil=view.Time+75f;
   if(view.Infantry<20&&view.Time<plan.OpeningUntil){
    if(view.RifleRecruitPending)return AriaAirOpeningAction.WaitForPending;
    if(view.CanAffordRifle)return AriaAirOpeningAction.RecruitRifle;
   }
   plan.AssaultStarted=1;return AriaAirOpeningAction.ContinueAssault;
  }
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

 public enum AriaBreacherAction:byte {None,Waiting,Recruit}
 // Breacher coverage (gate 3: every available unit type is used). Breachers are
 // structure-capable infantry from the Barracks. One squad joins the assault once the
 // first assault is issued; the existing roster sweep then orders it at the base.
 // Through the visible drawer only (Build, then the catalog card); no delay to the
 // opening or the first assault; yields to every other owner.
 public struct AriaBreacherView {
  public bool AssaultIssued,Busy,EnemyAlive;
  public bool CanAffordBreacher,BreacherPending,BreacherFielded,RecruitAvailable;
  public bool RifleRecruitPending,RocketeerRecruitPending;
  public int CompletedContacts;public float Time;
 }
 // Stage: 0 idle, 1 buying (drawer taps in flight), 2 done for the match.
 public struct AriaBreacherState {public byte Stage;public int Actions;public float Deadline;}
 public static class AriaBreacherPolicy {
  public const float BuySeconds=10f;
  public static AriaBreacherAction Step(in AriaBreacherView view,ref AriaBreacherState plan){
   if(plan.Stage==2)return AriaBreacherAction.None;
   if(view.BreacherPending||view.BreacherFielded){plan.Stage=2;return AriaBreacherAction.None;}
   if(plan.Stage==1){
    // Bounded: a purchase that does not appear in time is not retried this match.
    if(view.Time>plan.Deadline||view.Busy||!view.EnemyAlive){plan.Stage=2;return AriaBreacherAction.None;}
    return view.RecruitAvailable?AriaBreacherAction.Recruit:AriaBreacherAction.Waiting;
   }
   if(!view.AssaultIssued||view.Busy||!view.EnemyAlive||view.RifleRecruitPending||view.RocketeerRecruitPending||
      !view.CanAffordBreacher||!view.RecruitAvailable)return AriaBreacherAction.None;
   plan.Stage=1;plan.Actions=view.CompletedContacts;plan.Deadline=view.Time+BuySeconds;
   return AriaBreacherAction.Recruit;
  }
 }
 public enum AriaStructureReplacement:byte {None,Wait,RecruitBreacher,RecruitRocketeer}
 public static class AriaStructureReplacementPolicy {
  // The whole structure-capable force is gone: rebuild it through the drawer. Breachers are
  // structure-capable infantry and are preferred; rocketeers remain the fallback.
  public static AriaStructureReplacement Choose(bool breacherPending,bool rocketeerPending,
   bool canAffordBreacher,bool breacherAvailable,bool canAffordRocketeer,bool rocketeerAvailable){
   if(breacherPending||rocketeerPending)return AriaStructureReplacement.Wait;
   if(canAffordBreacher&&breacherAvailable)return AriaStructureReplacement.RecruitBreacher;
   if(canAffordRocketeer&&rocketeerAvailable)return AriaStructureReplacement.RecruitRocketeer;
   return AriaStructureReplacement.None;
  }
 }
}
