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
 // Coverage recruits after the breacher (gate 3: every available unit type is used):
 // one marksman squad (Barracks), one apc_fast and one radar (GroundStaging), in that
 // order, one recruit at a time, through the visible Build button and catalog card.
 // Kind bits: 1 marksman, 2 apc_fast, 4 radar.
 public enum AriaCoverageKind:byte {None,Marksman,ApcFast,Radar}
 public enum AriaCoverageAction:byte {None,Waiting,Recruit}
 public struct AriaCoverageView {
  // AssaultIssued: the first assault and structure orders were accepted.
  // BreacherSettled: the breacher step finished (bought, timed out or not needed).
  // OtherRecruitPending: a rifle, rocketeer or breacher purchase is still queued.
  public bool AssaultIssued,BreacherSettled,Busy,EnemyAlive,OtherRecruitPending;
  // Per kind bit: affordable now, queued, alive on the field, catalog target offered.
  public int AffordMask,PendingMask,FieldedMask,CardMask;
  public float Time;
 }
 // Done: kinds covered or given up for this match. Current: kind being bought (0 none).
 public struct AriaCoverageState {public byte Done,Current;public float Deadline;}
 public static class AriaCoverageRecruitPolicy {
  public const int All=7;public const float BuySeconds=10f;
  public static int Bit(AriaCoverageKind kind)=>kind==AriaCoverageKind.None?0:1<<((int)kind-1);
  public static AriaCoverageAction Step(in AriaCoverageView view,ref AriaCoverageState plan,out AriaCoverageKind kind){
   // A kind already queued or fielded is covered, whoever bought it.
   plan.Done|=(byte)((view.PendingMask|view.FieldedMask)&All);
   kind=(AriaCoverageKind)plan.Current;
   if(plan.Current!=0){
    int bit=Bit(kind);
    if((plan.Done&bit)!=0){plan.Current=0;return AriaCoverageAction.None;}
    // Bounded: a purchase that does not appear in time is not retried this match.
    if(view.Time>plan.Deadline||view.Busy||!view.EnemyAlive){plan.Done|=(byte)bit;plan.Current=0;return AriaCoverageAction.None;}
    return (view.CardMask&bit)!=0?AriaCoverageAction.Recruit:AriaCoverageAction.Waiting;
   }
   kind=AriaCoverageKind.None;
   if(!view.AssaultIssued||!view.BreacherSettled||view.Busy||!view.EnemyAlive||view.OtherRecruitPending||
      (view.PendingMask&All)!=0)return AriaCoverageAction.None;
   for(int k=1;k<=3;k++){
    int bit=1<<(k-1);
    // Unaffordable kinds wait for materials; the next affordable kind goes first.
    if((plan.Done&bit)!=0||(view.AffordMask&bit)==0||(view.CardMask&bit)==0)continue;
    plan.Current=(byte)k;plan.Deadline=view.Time+BuySeconds;kind=(AriaCoverageKind)k;
    return AriaCoverageAction.Recruit;
   }
   return AriaCoverageAction.None;
  }
 }

 // The radar is not an assault role, so the roster sweep never orders it. Once fielded
 // and the column is moving, it gets one ordinary order: select its card on the current
 // roster page, arm Attack, tap clear ground behind the column (Attack Move). Starts only
 // with no visible selection and no other owner; bounded attempts, once per match.
 public enum AriaRadarAction:byte {None,Waiting,ToggleSelected,SelectSlot,ArmAttack,TargetRear}
 public struct AriaRadarView {
  public bool RadarFielded,Busy,ArmyMoving,EnemyAlive,AttackMode,ToggleAvailable,AttackAvailable,RearAvailable;
  // RadarMask: radar cards on the current roster page.
  public int RadarMask,SelectedMask,SelectedGroups,SelectedOffPage,AvailableSquadMask,CompletedContacts;
  public float Time;
 }
 // Stage: 0 idle, 1 select, 2 arm Attack, 3 tap rear ground, 4 finished for the match.
 public struct AriaRadarState {public byte Stage,Attempts;public int Slot,Actions;public float Deadline,ReadyAt;}
 // Trace: 0 none, 1 triggered, 2 ordered, 3 released, 4 given up.
 public struct AriaRadarDecision {public AriaRadarAction Action;public int Slot;public byte Trace;}
 public static class AriaRadarOrderPolicy {
  public const int MaxAttempts=2;public const float OrderSeconds=12f,CooldownSeconds=6f;
  public static bool Step(in AriaRadarView view,ref AriaRadarState plan,out AriaRadarDecision output){
   output=default;
   if(plan.Stage==4)return false;
   if(plan.Stage==0){
    if(!view.RadarFielded||view.Busy||!view.ArmyMoving||!view.EnemyAlive||view.AttackMode||view.SelectedGroups>0||
       view.Time<plan.ReadyAt)return false;
    int pick=view.RadarMask&view.AvailableSquadMask;if(pick==0)return false;
    int bit=0;while((pick&1)==0){pick>>=1;bit++;}
    plan.Stage=1;plan.Slot=bit;plan.Attempts++;plan.Deadline=view.Time+OrderSeconds;plan.Actions=view.CompletedContacts;
    output.Trace=1;
   }
   if(view.Busy||view.Time>plan.Deadline||!view.EnemyAlive){Release(ref plan,view.Time,ref output);return false;}
   int desired=1<<plan.Slot;
   if(plan.Stage==1){
    // The page moved under us: the slot no longer shows the radar.
    if((view.RadarMask&desired)==0&&!(view.SelectedGroups==1&&view.SelectedMask==desired)){Release(ref plan,view.Time,ref output);return false;}
    bool exact=view.SelectedGroups==1&&view.SelectedOffPage==0&&view.SelectedMask==desired;
    if(!exact){
     if(view.SelectedGroups>0){output.Action=view.ToggleAvailable?AriaRadarAction.ToggleSelected:AriaRadarAction.Waiting;return true;}
     if((view.AvailableSquadMask&desired)==0){Release(ref plan,view.Time,ref output);return false;}
     output.Action=AriaRadarAction.SelectSlot;output.Slot=plan.Slot;return true;
    }
    plan.Stage=2;
   }
   if(plan.Stage==2){
    if(!view.AttackMode){output.Action=view.AttackAvailable?AriaRadarAction.ArmAttack:AriaRadarAction.Waiting;return true;}
    plan.Stage=3;plan.Actions=view.CompletedContacts;
   }
   if(view.CompletedContacts<=plan.Actions){
    output.Action=view.RearAvailable?AriaRadarAction.TargetRear:AriaRadarAction.Waiting;return true;
   }
   plan.Stage=4;output.Trace=2;output.Action=AriaRadarAction.None;return false;
  }
  static void Release(ref AriaRadarState plan,float time,ref AriaRadarDecision output){
   plan.Stage=(byte)(plan.Attempts>=MaxAttempts?4:0);plan.ReadyAt=time+CooldownSeconds;
   output.Trace=(byte)(plan.Stage==4?4:3);output.Action=AriaRadarAction.None;
  }
 }
}

