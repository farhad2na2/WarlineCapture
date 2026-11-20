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
  // Air-capable base assault (lane 26 S004): a purchase interrupted by another owner (the base alert's home
  // defence) is retried once instead of being given up for the match.
  public bool RetryInterrupted;
  public int CompletedContacts;public float Time;
 }
 // Stage: 0 idle, 1 buying (drawer taps in flight), 2 done for the match. Retried: an interrupted purchase was released once.
 public struct AriaBreacherState {public byte Stage,Retried;public int Actions;public float Deadline;}
 public static class AriaBreacherPolicy {
  public const float BuySeconds=10f;
  public static AriaBreacherAction Step(in AriaBreacherView view,ref AriaBreacherState plan){
   if(plan.Stage==2)return AriaBreacherAction.None;
   if(view.BreacherPending||view.BreacherFielded){plan.Stage=2;return AriaBreacherAction.None;}
   if(plan.Stage==1){
    // Bounded: a purchase that does not appear in time is not retried this match.
    if(view.RetryInterrupted&&view.Busy&&view.EnemyAlive&&view.Time<=plan.Deadline&&plan.Retried==0)
    {plan.Retried=1;plan.Stage=0;return AriaBreacherAction.None;}
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
 // Gunner and Tank (bits 64, 128): producible in some catalogs without being in the starting force
 // (lane 12 S049 never fielded either).
 // Siege (bit 256) needs Full Arsenal; it is offered once the research step has unlocked it.
 // AntiAir and AttackHeli (bits 512, 1024; air-capable armies only, lane 24 S004): the AA is usually in the
 // starting force (covered at once, then ordered by AriaSupportOrderPolicy); the attack heli is bought when the
 // tactical air cap has room. Ground-only armies (S002, S026) never offer either, so their masks are unchanged.
 public enum AriaCoverageKind:byte {None,Marksman,ApcFast,Radar,ApcHeavy,Drone,TransportHeli,Gunner,Tank,Siege,AntiAir,AttackHeli}
 public enum AriaCoverageAction:byte {None,Waiting,Recruit}
 public struct AriaCoverageView {
  // AssaultIssued: the first assault and structure orders were accepted.
  // BreacherSettled: the breacher step finished (bought, timed out or not needed).
  // OtherRecruitPending: a rifle, rocketeer or breacher purchase is still queued.
  public bool AssaultIssued,BreacherSettled,Busy,EnemyAlive,OtherRecruitPending;
  // The build drawer is open (this purchase's visit): a second kind may be bought in the same visit.
  public bool DrawerOpen;
  // Air-capable base assault (lane 26 S004): a kind whose purchase another owner interrupted (home defence
  // for a base alert) is released for one more visit instead of being given up for the match.
  public bool RetryInterrupted;
  public bool PrioritizeAirCoverage;
  // Per kind bit: affordable now (above the materials reserve), queued, alive, catalog target offered.
  public int AffordMask,PendingMask,FieldedMask,CardMask;
  // Completed touches: a purchase that keeps landing taps (Build, category, scroll, card,
  // produce) is still progressing and gets more time, up to MaxBuySeconds.
  public int CompletedContacts;
  public float Time;
 }
 // Done: kinds covered or given up for this match. Current: kind being bought (0 none).
 // Batch: purchases already made in this drawer visit beyond the first (0 = first purchase).
 // Interrupted: kinds released once after an interruption (RetryInterrupted).
 public struct AriaCoverageState {public int Done,Interrupted;public byte Current,Batch;public float Deadline,Started;public int Actions;}
 public static class AriaCoverageRecruitPolicy {
  // Radar (lane 6): Build, Vehicles tab, scroll, card, produce did not fit in 10 s and gave up at +10.0 s.
  // GapSeconds: between drawer visits, so new groups join the column before the next purchase (lane 8 S050).
  // PerVisit: kinds per drawer visit (lane 12 S026 won at 218 s with six kinds still unbought).
  public const int Kinds=11,All=(1<<Kinds)-1,PerVisit=3;
  // Coverage kinds that take a tactical air slot (drone, transport heli, attack heli).
  public const int AirKinds=16|32|1024;
  const int GroundOpeningKinds=1|2|4, ExtraAircraftKinds=16|1024;
  public const float BuySeconds=10f,StepSeconds=6f,MaxBuySeconds=24f,GapSeconds=12f;
  // Purchase order: combat kinds first, then support kinds.
  static readonly byte[] Order={(byte)AriaCoverageKind.Marksman,(byte)AriaCoverageKind.ApcFast,(byte)AriaCoverageKind.Gunner,
   (byte)AriaCoverageKind.Tank,(byte)AriaCoverageKind.Radar,(byte)AriaCoverageKind.ApcHeavy,(byte)AriaCoverageKind.Drone,
   (byte)AriaCoverageKind.TransportHeli,(byte)AriaCoverageKind.Siege,(byte)AriaCoverageKind.AntiAir,(byte)AriaCoverageKind.AttackHeli};
  static readonly byte[] AirPriority={(byte)AriaCoverageKind.AttackHeli,(byte)AriaCoverageKind.Drone,(byte)AriaCoverageKind.Radar};
  public static int Bit(AriaCoverageKind kind)=>kind==AriaCoverageKind.None?0:1<<((int)kind-1);
  public static AriaCoverageAction Step(in AriaCoverageView view,ref AriaCoverageState plan,out AriaCoverageKind kind){
   // A kind already queued or fielded is covered, whoever bought it.
   plan.Done|=(view.PendingMask|view.FieldedMask)&All;
   kind=(AriaCoverageKind)plan.Current;
   if(plan.Current!=0){
    int bit=Bit(kind);
    if((plan.Done&bit)!=0){
     plan.Current=0;
     // Landed. The drawer is still open: buy the next affordable kind in the same visit.
     if(plan.Batch<PerVisit-1&&view.DrawerOpen&&!view.Busy&&view.EnemyAlive){
      plan.Batch++;
      if(TryStart(view,ref plan,out kind))return AriaCoverageAction.Recruit;
     }
     EndVisit(ref plan,view.Time);return AriaCoverageAction.None;
    }
    if(view.CompletedContacts>plan.Actions){
     plan.Actions=view.CompletedContacts;
     plan.Deadline=System.Math.Min(plan.Started+MaxBuySeconds,System.Math.Max(plan.Deadline,view.Time+StepSeconds));
    }
    // Bounded: a purchase that does not appear in time is not retried this match.
    if(view.RetryInterrupted&&view.Busy&&view.EnemyAlive&&view.Time<=plan.Deadline&&(plan.Interrupted&bit)==0)
    {plan.Interrupted|=bit;plan.Current=0;EndVisit(ref plan,view.Time);return AriaCoverageAction.None;}
    if(view.Time>plan.Deadline||view.Busy||!view.EnemyAlive){plan.Done|=bit;plan.Current=0;EndVisit(ref plan,view.Time);return AriaCoverageAction.None;}
    return (view.CardMask&bit)!=0?AriaCoverageAction.Recruit:AriaCoverageAction.Waiting;
   }
   kind=AriaCoverageKind.None;
   // Deadline doubles as the earliest next visit after a purchase (GapSeconds).
   if(!view.AssaultIssued||!view.BreacherSettled||view.Busy||!view.EnemyAlive||view.OtherRecruitPending||
      (view.PendingMask&All)!=0||view.Time<plan.Deadline)return AriaCoverageAction.None;
   plan.Batch=0;
   return TryStart(view,ref plan,out kind)?AriaCoverageAction.Recruit:AriaCoverageAction.None;
  }
  static bool TryStart(in AriaCoverageView view,ref AriaCoverageState plan,out AriaCoverageKind kind){
   if(view.PrioritizeAirCoverage&&TryStart(view,ref plan,AirPriority,out kind))return true;
   return TryStart(view,ref plan,Order,out kind);
  }
  static bool TryStart(in AriaCoverageView view,ref AriaCoverageState plan,byte[] order,out AriaCoverageKind kind){
   kind=AriaCoverageKind.None;
   for(int i=0;i<order.Length;i++){
    int k=order[i],bit=1<<(k-1);
    // Unaffordable kinds wait for materials; the next affordable kind goes first.
    if((plan.Done&bit)!=0||(view.AffordMask&bit)==0||(view.CardMask&bit)==0)continue;
    plan.Current=(byte)k;plan.Deadline=view.Time+BuySeconds;plan.Started=view.Time;plan.Actions=view.CompletedContacts;kind=(AriaCoverageKind)k;
    return true;
   }
   return false;
  }
  static void EndVisit(ref AriaCoverageState plan,float time){plan.Batch=0;plan.Deadline=time+GapSeconds;}
  // Air-capable armies: the air opening's light attack helicopter (AriaSkirmishPlanSystem RecruitAir) keeps the
  // first claim on a free tactical air slot. Until it has been queued once, coverage does not spend that slot on
  // a drone, transport heli or attack heli (Standard air cap 2: lane 24 S004 starts with a transport heli, so one
  // slot is free). Afterwards a slot freed by a loss goes to the next unused air kind. Identity otherwise.
  public static int AirOpeningAfford(int affordMask,bool airProfile,bool airOpeningSettled,bool airOpeningOffered)=>
   airProfile&&!airOpeningSettled&&airOpeningOffered?affordMask&~AirKinds:affordMask;
  public static bool CanBuyOpeningTanker(bool airCoverage,int done)=>
   !airCoverage||(done&GroundOpeningKinds)==GroundOpeningKinds;
  public static bool CanBuyLightAttackHeli(bool airCoverage,bool lightQueuedOnce,int done)=>
   !airCoverage||!lightQueuedOnce||(done&ExtraAircraftKinds)==ExtraAircraftKinds;
 }

 // Which expanded skirmishes run the generic gate-3 coverage (breacher, coverage purchases, research, producer
 // buildings, support orders). Base Assault: ground and air-capable armies alike (lane 24 S004, army A: the old
 // !AirProfile gate left breacher, marksman, apc_fast, radar, AA and transport heli unused on every seed);
 // Combined Arms keeps its own structure-force path. Objective missions (FC/BT/CE) use the objective skill rule.
 public static class AriaCoverageEligibility {
  public static bool BaseAssault(bool expandedSession,bool expandedBaseAssault,bool combinedArmsField,bool publicObjective)=>
   expandedSession&&expandedBaseAssault&&!combinedArmsField&&!publicObjective;
 }

 // Coverage purchases currently possible are finished: nothing in flight or queued, and every kind
 // affordable now is covered. Research and buildings wait for this (one purchase at a time).
 public static class AriaCoverageProgress {
  public static bool Idle(int done,byte current,int affordMask,int pendingMask)=>
   current==0&&(pendingMask&AriaCoverageRecruitPolicy.All)==0&&(affordMask&~done&AriaCoverageRecruitPolicy.All)==0;
 }

 // One readiness research per stage, through the HUD's ordinary upgrade control (Build drawer readiness
 // button), only when it unlocks something the mission offers and ARIA has not yet used (a coverage
 // kind such as siege, or a producer building), after the first assault and the possible coverage
 // purchases, and only while materials exceed the research cost plus the reserve. Yields to every
 // other owner; two attempts per stage; at most up to Full Arsenal.
 public enum AriaResearchAction:byte {None,Waiting,Upgrade}
 public struct AriaResearchView {
  public bool AssaultSeen,Busy,EnemyAlive,CoverageIdle,InProgress,CanUpgrade,UpgradeAvailable;
  // Stage: current readiness (1 Field, 2 Established, 3 Full Arsenal). NeedMask: anything unlocked by more readiness.
  public int Stage,Cost,Materials,Reserve,NeedMask,CompletedContacts;
  public float Time;
 }
 // Current: 1 while the upgrade taps are in flight. Tried/Done: bits per stage (1 << stage).
 public struct AriaResearchState {public byte Current,Tried,Done;public int Actions;public float Deadline;}
 // Trace: 0 none, 1 started, 2 queued (research running), 3 released, 4 given up.
 public struct AriaResearchDecision {public AriaResearchAction Action;public byte Trace;}
 public static class AriaResearchPolicy {
  public const float TapSeconds=10f;public const int MaxStage=3,Reserve=250;
  public static bool Step(in AriaResearchView view,ref AriaResearchState plan,out AriaResearchDecision output){
   output=default;int bit=1<<System.Math.Max(0,System.Math.Min(7,view.Stage));
   if(view.InProgress){
    if(plan.Current!=0){plan.Current=0;plan.Done|=(byte)bit;output.Trace=2;}
    return false;
   }
   if(plan.Current!=0){
    if(view.Busy||!view.EnemyAlive||view.Time>plan.Deadline){
     plan.Current=0;
     if((plan.Tried&bit)!=0){plan.Done|=(byte)bit;output.Trace=4;}else{plan.Tried|=(byte)bit;output.Trace=3;}
     return false;
    }
    output.Action=view.UpgradeAvailable?AriaResearchAction.Upgrade:AriaResearchAction.Waiting;return true;
   }
   if(view.Stage>=MaxStage||(plan.Done&bit)!=0||!view.AssaultSeen||view.Busy||!view.EnemyAlive||!view.CoverageIdle||
      view.NeedMask==0||!view.CanUpgrade||!view.UpgradeAvailable||view.Materials<view.Cost+view.Reserve)return false;
   plan.Current=1;plan.Deadline=view.Time+TapSeconds;plan.Actions=view.CompletedContacts;
   output.Trace=1;output.Action=AriaResearchAction.Upgrade;return true;
  }
 }

 // Producer buildings the mission offers that the player does not start with (bits: 1 helipad,
 // 2 satellite dish) are built once each, after the first assault and the possible coverage
 // purchases, through the ordinary Build drawer and a legal home placement (the existing pad-site
 // placement steps). Two attempts per building; a placement with no legal site gives up cleanly.
 public enum AriaBuildingAction:byte {None,Start,Continue,Cancel}
 public struct AriaBuildingView {
  public bool AssaultSeen,Busy,EnemyAlive,CoverageIdle,ResearchIdle,PlacementOpen,PlacementGaveUp;
  // Need: offered by the mission and not present. Afford: affordable above the reserve now.
  // Present: built. Card: the build card (or the Build button) is offered.
  public int NeedMask,AffordMask,PresentMask,CardMask;
  public float Time;
 }
 // Current: building being placed (1 helipad, 2 dish; 0 none). Tried/Done: per building bit.
 public struct AriaBuildingState {public byte Current,Tried,Done;public float Deadline;}
 // Trace: 0 none, 1 started, 2 built, 3 released, 4 given up.
 public struct AriaBuildingDecision {public AriaBuildingAction Action;public byte Kind,Trace;}
 public static class AriaBuildingPolicy {
  public const float PlaceSeconds=50f;
  public static int Bit(byte kind)=>kind==0?0:1<<(kind-1);
  public static bool Step(in AriaBuildingView view,ref AriaBuildingState plan,out AriaBuildingDecision output){
   output=default;
   plan.Done|=(byte)(view.PresentMask&3);
   if(plan.Current!=0){
    int bit=Bit(plan.Current);output.Kind=plan.Current;
    if((plan.Done&bit)!=0){plan.Current=0;output.Trace=2;return false;}
    if(view.Busy||!view.EnemyAlive||view.PlacementGaveUp||view.Time>plan.Deadline){
     // Never leave a placement preview open for the next owner.
     if(view.PlacementOpen){output.Action=AriaBuildingAction.Cancel;return true;}
     if((plan.Tried&bit)!=0){plan.Done|=(byte)bit;output.Trace=4;}else{plan.Tried|=(byte)bit;output.Trace=3;}
     plan.Current=0;return false;
    }
    output.Action=AriaBuildingAction.Continue;return true;
   }
   if(!view.AssaultSeen||view.Busy||!view.EnemyAlive||!view.CoverageIdle||!view.ResearchIdle||view.PlacementOpen)return false;
   for(byte k=1;k<=2;k++){
    int bit=Bit(k);
    if((plan.Done&bit)!=0||(view.NeedMask&bit)==0||(view.AffordMask&bit)==0||(view.CardMask&bit)==0)continue;
    plan.Current=k;plan.Deadline=view.Time+PlaceSeconds;output.Kind=k;output.Trace=1;output.Action=AriaBuildingAction.Start;
    return true;
   }
   return false;
  }
 }

 // Support units are not assault roles, so the roster sweep never orders them. Once one is
 // fielded and the column has its order, it gets one ordinary order through its roster card:
 // radar and transport heli Move (Attack Move when Move is not offered) onto clear ground
 // behind the presented column; the drone Scans clear ground ahead of it. Starts only with no
 // visible selection and no other owner; bounded attempts; one order per unit kind per match.
 // Support kinds (SupportSlots: 3 bits per roster slot): 1 radar, 2 drone, 3 transport heli, 4 AA.
 // AA (lane 24 S004): not an assault role, so the column sweep never orders the starting AA; it Moves
 // behind the column like the radar and covers it from the enemy air.
 public enum AriaSupportKind:byte {None,Radar,Drone,TransportHeli,AntiAir,ApcArmored}
 public enum AriaSupportMode:byte {None,Move,Attack,Scan}
 public enum AriaSupportAction:byte {None,Waiting,ToggleSelected,SelectSlot,ArmMove,ArmAttack,ArmScan,TargetRear,TargetFront,TargetHome,Focus}
 public struct AriaSupportView {
  // FieldedMask: support kinds alive (bits 1 radar, 2 drone, 4 transport heli, 8 AA).
  public int FieldedMask;
  public bool Busy,ArmyMoving,EnemyAlive,ToggleAvailable,MoveAvailable,AttackAvailable,ScanAvailable,RearAvailable,FrontAvailable;
  // Tidy (air-capable base assault, lane 26 S004): the AA and transport heli start at home, and after their roster
  // card is tapped the column's rear ground was never on screen (rear=False in every job), so the Move tap never
  // happened and the base alert released the order with the AA still selected and Move still armed.
  // In this mode: untried kinds go first; a Move falls back to clear home-side ground (HomeAvailable; the AA
  // prefers it) after at most one Home focus (FocusAvailable) pressed before arming; an armed mode is toggled
  // until it is really off (two presses at most); and the policy deselects its own unit when it is done.
  public bool Tidy,HomeAvailable,FocusAvailable;
  public bool RequireOrderResult,OrderResultAvailable,OrderAccepted;
  public AriaSupportMode Mode;
  public int SupportSlots,SelectedMask,SelectedGroups,SelectedOffPage,AvailableSquadMask,CompletedContacts;
  public float Time;
 }
 // Stage: 0 idle, 1 select, 2 arm the command, 3 tap the ground, 5 toggle the armed mode off before
 // a release (lane 8 S050: the radar order released with Move still armed), 6 deselect the own unit (Tidy).
 // Done: kinds ordered or given up. Tried: kinds released once; a second release gives the kind up.
 // Focused: the one Home focus of this order was pressed (Tidy). Presses: disarm toggles pressed (Tidy).
 public struct AriaSupportState {public byte Stage,Kind,Tried,Done,Focused,Presses;public int Slot,Actions;public float Deadline,ReadyAt;}
 // Trace: 0 none, 1 triggered, 2 ordered, 3 released, 4 given up.
 public struct AriaSupportDecision {public AriaSupportAction Action;public int Slot;public byte Trace;public AriaSupportKind Kind;}
 public static class AriaSupportOrderPolicy {
  public const float OrderSeconds=12f,CooldownSeconds=6f;
  public const int SlotBits=3,AllKinds=31;
  public static AriaSupportKind SlotKind(int supportSlots,int slot)=>(AriaSupportKind)((supportSlots>>(slot*SlotBits))&7);
  public static int KindBit(AriaSupportKind kind)=>kind==AriaSupportKind.None?0:1<<((int)kind-1);
  public static int Slots(AriaSupportKind slot0,AriaSupportKind slot1,AriaSupportKind slot2,AriaSupportKind slot3,AriaSupportKind slot4)=>
   (int)slot0|((int)slot1<<SlotBits)|((int)slot2<<(2*SlotBits))|((int)slot3<<(3*SlotBits))|((int)slot4<<(4*SlotBits));
  public const byte StageDisarm=5,StageDeselect=6,MaxDisarmPresses=2,MaxSelectPresses=3;public const float DisarmSeconds=3f,DeselectSeconds=3f,SettleSeconds=.5f;
  public static bool Step(in AriaSupportView view,ref AriaSupportState plan,out AriaSupportDecision output){
   output=default;
   if(plan.Stage==0){
    if((view.FieldedMask&~plan.Done&AllKinds)==0||view.Busy||!view.ArmyMoving||!view.EnemyAlive||view.Mode!=AriaSupportMode.None||
       view.SelectedGroups>0||view.Time<plan.ReadyAt)return false;
    int pending=view.FieldedMask&~plan.Done&AllKinds;
    int slot=view.Tidy?FindSlot(view,pending&~plan.Tried&KindBit(AriaSupportKind.TransportHeli)):-1;
    // Tidy: a kind never tried goes before one already released once (lane 26: the AA was retried while the
    // transport heli on the next card never got its turn).
    if(slot<0&&view.Tidy)slot=FindSlot(view,pending&~plan.Tried);
    if(slot<0)slot=FindSlot(view,pending);
    if(slot<0)return false;
    plan.Kind=(byte)SlotKind(view.SupportSlots,slot);
    plan.Stage=1;plan.Slot=slot;plan.Deadline=view.Time+OrderSeconds;plan.Actions=view.CompletedContacts;plan.Focused=0;plan.Presses=0;
    output.Trace=1;
   }
   output.Kind=(AriaSupportKind)plan.Kind;
   if(plan.Stage==StageDeselect){
    // Cleanup only: never touch a selection that is not exactly this order's unit, and never another owner's turn.
    if(view.Busy||view.Time>plan.Deadline||view.CompletedContacts>plan.Actions||!OwnSelection(view,plan.Slot)||!view.ToggleAvailable)
    {plan.Stage=0;return false;}
    output.Action=AriaSupportAction.ToggleSelected;return true;
   }
   if(plan.Stage==StageDisarm){
    if(view.Tidy){
     // Toggle until the mode is really off (lane 26: one press left Move armed for the home defence).
     if(view.Mode==AriaSupportMode.None||view.Time>plan.Deadline){Release(view,ref plan,ref output);return false;}
     if(view.CompletedContacts>plan.Actions){
      if(plan.Presses>=MaxDisarmPresses){Release(view,ref plan,ref output);return false;}
      if(view.Time<plan.ReadyAt){output.Action=AriaSupportAction.Waiting;return true;}
      plan.Presses++;plan.Actions=view.CompletedContacts;plan.ReadyAt=view.Time+SettleSeconds;
     }
     output.Action=ToggleFor(view.Mode,view);
     if(output.Action==AriaSupportAction.Waiting){Release(view,ref plan,ref output);return false;}
     return true;
    }
    // One toggle of the armed mode; then the release completes.
    if(view.Mode==AriaSupportMode.None||view.CompletedContacts>plan.Actions||view.Time>plan.Deadline)
    {Release(view,ref plan,ref output);return false;}
    output.Action=ToggleFor(view.Mode,view);
    if(output.Action==AriaSupportAction.Waiting){Release(view,ref plan,ref output);return false;}
    return true;
   }
   if(view.Busy||view.Time>plan.Deadline||!view.EnemyAlive)return ReleaseArmed(view,ref plan,ref output);
   int desired=1<<plan.Slot;bool drone=plan.Kind==(byte)AriaSupportKind.Drone;
   // W27c (W16 proof S010 130365, Blocked 115 on Move with selectedGroups=0): the unit's selection was lost after
   // Stage 2 began, so arming Move never took and the input layer stopped ARIA on the third press. Select it again.
   if(plan.Stage==2&&!OwnSelection(view,plan.Slot)&&!(drone?view.Mode==AriaSupportMode.Scan:view.Mode is AriaSupportMode.Move or AriaSupportMode.Attack))
    plan.Stage=1;
   if(plan.Stage==1){
    // The page moved under us: the slot no longer shows this support unit.
    if(SlotKind(view.SupportSlots,plan.Slot)!=(AriaSupportKind)plan.Kind){Release(view,ref plan,ref output);return false;}
    bool exact=view.SelectedGroups==1&&view.SelectedOffPage==0&&view.SelectedMask==desired;
    if(!exact){
     if(view.SelectedGroups>0){output.Action=view.ToggleAvailable?AriaSupportAction.ToggleSelected:AriaSupportAction.Waiting;return true;}
     if((view.AvailableSquadMask&desired)==0){Release(view,ref plan,ref output);return false;}
     // W27c: a card press that does not select the unit is not repeated until the input layer's 115.
     if(view.CompletedContacts>plan.Actions){plan.Presses++;plan.Actions=view.CompletedContacts;}
     if(plan.Presses>=MaxSelectPresses){Release(view,ref plan,ref output);return false;}
     output.Action=AriaSupportAction.SelectSlot;output.Slot=plan.Slot;return true;
    }
    plan.Stage=2;
   }
   if(plan.Stage==2){
    bool armed=drone?view.Mode==AriaSupportMode.Scan:view.Mode is AriaSupportMode.Move or AriaSupportMode.Attack;
    if(!armed){
     // Keep presenting Focus through the input driver's aim delay, until its touch completes.
     if(view.Tidy&&view.Mode==AriaSupportMode.None){
      if(plan.Focused==1&&view.CompletedContacts<=plan.Actions)
      {output.Action=view.FocusAvailable?AriaSupportAction.Focus:AriaSupportAction.Waiting;return true;}
      if(plan.Focused==0&&view.FocusAvailable&&GroundAction(view,plan.Kind)==AriaSupportAction.Waiting)
      {plan.Focused=1;plan.Actions=view.CompletedContacts;output.Action=AriaSupportAction.Focus;return true;}
     }
     output.Action=drone?(view.ScanAvailable?AriaSupportAction.ArmScan:AriaSupportAction.Waiting)
      :view.MoveAvailable?AriaSupportAction.ArmMove:view.AttackAvailable?AriaSupportAction.ArmAttack:AriaSupportAction.Waiting;
     return true;
    }
    plan.Stage=3;plan.Actions=view.CompletedContacts;
   }
   if(view.Tidy&&(!OwnSelection(view,plan.Slot)||SlotKind(view.SupportSlots,plan.Slot)!=(AriaSupportKind)plan.Kind))
    return ReleaseArmed(view,ref plan,ref output);
   if(view.CompletedContacts<=plan.Actions){
    output.Action=GroundAction(view,plan.Kind);
    return true;
   }
   if(view.RequireOrderResult){
    if(!view.OrderResultAvailable){output.Action=AriaSupportAction.Waiting;return true;}
    if(!view.OrderAccepted)return ReleaseArmed(view,ref plan,ref output);
   }
   plan.Done|=(byte)KindBit((AriaSupportKind)plan.Kind);plan.Stage=0;plan.ReadyAt=view.Time+CooldownSeconds;
   output.Trace=2;output.Action=AriaSupportAction.None;
   BeginDeselect(view,ref plan);
   return false;
  }
  static int FindSlot(in AriaSupportView view,int kinds){
   for(int slot=0;slot<5;slot++)
    if((view.AvailableSquadMask&(1<<slot))!=0&&(kinds&KindBit(SlotKind(view.SupportSlots,slot)))!=0)return slot;
   return -1;
  }
  static AriaSupportAction GroundAction(in AriaSupportView view,byte kind){
   if(kind==(byte)AriaSupportKind.Drone)
    return view.FrontAvailable?AriaSupportAction.TargetFront:view.Tidy&&view.HomeAvailable?AriaSupportAction.TargetHome:AriaSupportAction.Waiting;
   if(view.Tidy&&view.HomeAvailable&&(kind!=(byte)AriaSupportKind.Radar||!view.RearAvailable))return AriaSupportAction.TargetHome;
   return view.RearAvailable?AriaSupportAction.TargetRear:AriaSupportAction.Waiting;
  }
  static bool OwnSelection(in AriaSupportView view,int slot)=>
   view.SelectedGroups==1&&view.SelectedOffPage==0&&view.SelectedMask==1<<slot;
  // Tidy: the order is over (ordered or released); deselect this unit so the next owner starts clean.
  static void BeginDeselect(in AriaSupportView view,ref AriaSupportState plan){
   if(!view.Tidy||!OwnSelection(view,plan.Slot)||!view.ToggleAvailable)return;
   plan.Stage=StageDeselect;plan.Actions=view.CompletedContacts;plan.Deadline=view.Time+DeselectSeconds;
  }
  // Never hand back a mode this policy armed: toggle it off first (one press, bounded; Tidy: until off, two at most).
  static bool ReleaseArmed(in AriaSupportView view,ref AriaSupportState plan,ref AriaSupportDecision output){
   if(plan.Stage>=2&&view.Mode!=AriaSupportMode.None&&ToggleFor(view.Mode,view)!=AriaSupportAction.Waiting){
    plan.Stage=StageDisarm;plan.Actions=view.CompletedContacts;plan.Deadline=view.Time+DisarmSeconds;
    plan.Presses=1;plan.ReadyAt=view.Time+SettleSeconds;
    output.Action=ToggleFor(view.Mode,view);return true;
   }
   Release(view,ref plan,ref output);return false;
  }
  static AriaSupportAction ToggleFor(AriaSupportMode mode,in AriaSupportView view)=>mode switch{
   AriaSupportMode.Move=>view.MoveAvailable?AriaSupportAction.ArmMove:AriaSupportAction.Waiting,
   AriaSupportMode.Attack=>view.AttackAvailable?AriaSupportAction.ArmAttack:AriaSupportAction.Waiting,
   AriaSupportMode.Scan=>view.ScanAvailable?AriaSupportAction.ArmScan:AriaSupportAction.Waiting,
   _=>AriaSupportAction.Waiting};
  static void Release(in AriaSupportView view,ref AriaSupportState plan,ref AriaSupportDecision output){
   int bit=KindBit((AriaSupportKind)plan.Kind);bool giveUp=(plan.Tried&bit)!=0;
   if(giveUp)plan.Done|=(byte)bit;else plan.Tried|=(byte)bit;
   plan.Stage=0;plan.ReadyAt=view.Time+CooldownSeconds;
   output.Trace=(byte)(giveUp?4:3);output.Action=AriaSupportAction.None;
   // A busy release leaves the cleanup to the owner that took over (home defence deselects for itself).
   if(!view.Busy)BeginDeselect(view,ref plan);
  }
 }
}
