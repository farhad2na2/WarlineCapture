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
 // one marksman squad (Barracks), one apc_fast, one radar, one apc_heavy (GroundStaging),
 // one drone (Intel Station) and one transport heli (Helipad), in that order, one recruit
 // at a time, through the visible Build button and catalog card. A kind the mission's
 // catalog does not offer is never affordable and is skipped.
 // Kind bits: 1 marksman, 2 apc_fast, 4 radar, 8 apc_heavy, 16 drone, 32 transport_heli.
 public enum AriaCoverageKind:byte {None,Marksman,ApcFast,Radar,ApcHeavy,Drone,TransportHeli}
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
  public const int Kinds=6,All=(1<<Kinds)-1;public const float BuySeconds=10f;
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
   for(int k=1;k<=Kinds;k++){
    int bit=1<<(k-1);
    // Unaffordable kinds wait for materials; the next affordable kind goes first.
    if((plan.Done&bit)!=0||(view.AffordMask&bit)==0||(view.CardMask&bit)==0)continue;
    plan.Current=(byte)k;plan.Deadline=view.Time+BuySeconds;kind=(AriaCoverageKind)k;
    return AriaCoverageAction.Recruit;
   }
   return AriaCoverageAction.None;
  }
 }

 // Support units are not assault roles, so the roster sweep never orders them. Once one is
 // fielded and the column has its order, it gets one ordinary order through its roster card:
 // radar and transport heli Move (Attack Move when Move is not offered) onto clear ground
 // behind the presented column; the drone Scans clear ground ahead of it. Starts only with no
 // visible selection and no other owner; bounded attempts; one order per unit kind per match.
 // Support kinds (SupportSlots: 2 bits per roster slot): 1 radar, 2 drone, 3 transport heli.
 public enum AriaSupportKind:byte {None,Radar,Drone,TransportHeli}
 public enum AriaSupportMode:byte {None,Move,Attack,Scan}
 public enum AriaSupportAction:byte {None,Waiting,ToggleSelected,SelectSlot,ArmMove,ArmAttack,ArmScan,TargetRear,TargetFront}
 public struct AriaSupportView {
  // FieldedMask: support kinds alive (bits 1 radar, 2 drone, 4 transport heli).
  public int FieldedMask;
  public bool Busy,ArmyMoving,EnemyAlive,ToggleAvailable,MoveAvailable,AttackAvailable,ScanAvailable,RearAvailable,FrontAvailable;
  public AriaSupportMode Mode;
  public int SupportSlots,SelectedMask,SelectedGroups,SelectedOffPage,AvailableSquadMask,CompletedContacts;
  public float Time;
 }
 // Stage: 0 idle, 1 select, 2 arm the command, 3 tap the ground. Done: kinds ordered or given up.
 // Tried: kinds released once; a second release gives the kind up (MaxAttempts=2 per kind).
 public struct AriaSupportState {public byte Stage,Kind,Tried,Done;public int Slot,Actions;public float Deadline,ReadyAt;}
 // Trace: 0 none, 1 triggered, 2 ordered, 3 released, 4 given up.
 public struct AriaSupportDecision {public AriaSupportAction Action;public int Slot;public byte Trace;public AriaSupportKind Kind;}
 public static class AriaSupportOrderPolicy {
  public const float OrderSeconds=12f,CooldownSeconds=6f;
  public static AriaSupportKind SlotKind(int supportSlots,int slot)=>(AriaSupportKind)((supportSlots>>(slot*2))&3);
  public static int KindBit(AriaSupportKind kind)=>kind==AriaSupportKind.None?0:1<<((int)kind-1);
  public static int Slots(AriaSupportKind slot0,AriaSupportKind slot1,AriaSupportKind slot2,AriaSupportKind slot3,AriaSupportKind slot4)=>
   (int)slot0|((int)slot1<<2)|((int)slot2<<4)|((int)slot3<<6)|((int)slot4<<8);
  public static bool Step(in AriaSupportView view,ref AriaSupportState plan,out AriaSupportDecision output){
   output=default;
   if(plan.Stage==0){
    if((view.FieldedMask&~plan.Done&7)==0||view.Busy||!view.ArmyMoving||!view.EnemyAlive||view.Mode!=AriaSupportMode.None||
       view.SelectedGroups>0||view.Time<plan.ReadyAt)return false;
    int slot=-1;
    for(int i=0;i<5&&slot<0;i++){
     var k=SlotKind(view.SupportSlots,i);
     if(k!=AriaSupportKind.None&&(plan.Done&KindBit(k))==0&&(view.AvailableSquadMask&(1<<i))!=0)slot=i;
    }
    if(slot<0)return false;
    plan.Kind=(byte)SlotKind(view.SupportSlots,slot);
    plan.Stage=1;plan.Slot=slot;plan.Deadline=view.Time+OrderSeconds;plan.Actions=view.CompletedContacts;
    output.Trace=1;
   }
   output.Kind=(AriaSupportKind)plan.Kind;
   if(view.Busy||view.Time>plan.Deadline||!view.EnemyAlive){Release(ref plan,view.Time,ref output);return false;}
   int desired=1<<plan.Slot;bool drone=plan.Kind==(byte)AriaSupportKind.Drone;
   if(plan.Stage==1){
    // The page moved under us: the slot no longer shows this support unit.
    if(SlotKind(view.SupportSlots,plan.Slot)!=(AriaSupportKind)plan.Kind){Release(ref plan,view.Time,ref output);return false;}
    bool exact=view.SelectedGroups==1&&view.SelectedOffPage==0&&view.SelectedMask==desired;
    if(!exact){
     if(view.SelectedGroups>0){output.Action=view.ToggleAvailable?AriaSupportAction.ToggleSelected:AriaSupportAction.Waiting;return true;}
     if((view.AvailableSquadMask&desired)==0){Release(ref plan,view.Time,ref output);return false;}
     output.Action=AriaSupportAction.SelectSlot;output.Slot=plan.Slot;return true;
    }
    plan.Stage=2;
   }
   if(plan.Stage==2){
    bool armed=drone?view.Mode==AriaSupportMode.Scan:view.Mode is AriaSupportMode.Move or AriaSupportMode.Attack;
    if(!armed){
     output.Action=drone?(view.ScanAvailable?AriaSupportAction.ArmScan:AriaSupportAction.Waiting)
      :view.MoveAvailable?AriaSupportAction.ArmMove:view.AttackAvailable?AriaSupportAction.ArmAttack:AriaSupportAction.Waiting;
     return true;
    }
    plan.Stage=3;plan.Actions=view.CompletedContacts;
   }
   if(view.CompletedContacts<=plan.Actions){
    bool ground=drone?view.FrontAvailable:view.RearAvailable;
    output.Action=ground?(drone?AriaSupportAction.TargetFront:AriaSupportAction.TargetRear):AriaSupportAction.Waiting;return true;
   }
   plan.Done|=(byte)KindBit((AriaSupportKind)plan.Kind);plan.Stage=0;plan.ReadyAt=view.Time+CooldownSeconds;
   output.Trace=2;output.Action=AriaSupportAction.None;return false;
  }
  static void Release(ref AriaSupportState plan,float time,ref AriaSupportDecision output){
   int bit=KindBit((AriaSupportKind)plan.Kind);bool giveUp=(plan.Tried&bit)!=0;
   if(giveUp)plan.Done|=(byte)bit;else plan.Tried|=(byte)bit;
   plan.Stage=0;plan.ReadyAt=time+CooldownSeconds;
   output.Trace=(byte)(giveUp?4:3);output.Action=AriaSupportAction.None;
  }
 }
}
