namespace Game.Simulation {
 public enum AriaInterceptAction:byte {None,Waiting,ToggleSelected,SelectSlot,ArmAttack,TargetThreat}
 // "Finish what slips past": a visible hostile vehicle that is behind most of the
 // advancing army (closer to the player's base) and still within reach of an own
 // unit is attacked by one roster group through the normal card / Attack / tap
 // controls, exactly as a player taps a damaged tank driving through a column.
 // Only fog-visible, on-screen contacts are offered (ThreatAvailable); this policy
 // never reads hidden state and issues no order of its own.
 public struct AriaInterceptView {
  // A visible, reachable on-screen hostile vehicle slipping past the column.
  public bool ThreatAvailable;
  // Another owner holds the controls or the moment: open drawer/map/placement,
  // home defence, precision attack, camera navigation, army at the enemy base.
  public bool Busy;
  // At least one roster group already carries the accepted assault order.
  public bool ArmyMoving;
  public bool AttackMode,ToggleAvailable,AttackAvailable;
  public int SelectedMask,SelectedGroups,SelectedOffPage,StructureMask,AssaultMask,AirMask,AvailableSquadMask,CompletedContacts;
  public float Time;
 }
 // Stage: 0 idle, 1 select, 2 arm Attack, 3 tap the contact, 4 let the group fight.
 // Slot: roster slot on the current page, or KeepSelection when the visible
 // selection already holds an anti-armour (structure-capable) group.
 public struct AriaInterceptState {public byte Stage,Count;public int Slot,Actions;public float Deadline,LastSeenAt,ReadyAt;}
 // Trace: 0 none, 1 triggered, 2 ordered, 3 released.
 public struct AriaInterceptDecision {public AriaInterceptAction Action;public int Slot;public byte Trace;}
 public static class AriaInterceptPolicy {
  public const int KeepSelection=5,MaxIntercepts=3;
  // Order: the whole select/Attack/tap sequence. Hold: the group fights before the
  // ordinary assault sweep re-issues its base order. Lost: contact gone (dead or fog).
  // HoldLost: a chased contact may leave sight briefly; give the ordered group a moment.
  public const float OrderSeconds=10f,HoldSeconds=12f,LostSeconds=2f,HoldLostSeconds=4f,CooldownSeconds=6f;
  // Public projection geometry (world units). A visible vehicle crossing the column
  // (ContactReach of an own mobile unit), or behind the army's median distance to home
  // by SlipMargin and within Reach of an own unit. Vehicles near the enemy base belong
  // to the base assault, not to this rule.
  public const float SlipMargin=8f,Reach=60f,ContactReach=40f,EnemyBaseRadius=60f;
  public static bool Step(in AriaInterceptView view,ref AriaInterceptState plan,out AriaInterceptDecision output){
   output=default;
   if(plan.Stage==0){
    if(!view.ThreatAvailable||view.Busy||!view.ArmyMoving||plan.Count>=MaxIntercepts||view.Time<plan.ReadyAt)return false;
    int slot=ChooseSlot(view);if(slot<0)return false;
    plan.Stage=(byte)(slot==KeepSelection?2:1);plan.Slot=slot;plan.Count++;
    plan.Deadline=view.Time+OrderSeconds;plan.LastSeenAt=view.Time;plan.Actions=view.CompletedContacts;
    output.Trace=1;
   }
   if(view.ThreatAvailable)plan.LastSeenAt=view.Time;
   bool lost=view.Time-plan.LastSeenAt>LostSeconds;
   if(plan.Stage==4){
    if(view.Busy||view.Time-plan.LastSeenAt>HoldLostSeconds||view.Time>plan.Deadline){Release(ref plan,view.Time,ref output);return false;}
    output.Action=AriaInterceptAction.Waiting;return true;
   }
   if(view.Busy||lost||view.Time>plan.Deadline){Release(ref plan,view.Time,ref output);return false;}
   if(plan.Stage==1){
    int desired=1<<plan.Slot;
    bool exact=view.SelectedGroups==1&&view.SelectedOffPage==0&&view.SelectedMask==desired;
    if(!exact){
     if(view.SelectedGroups>0){
      if(view.ToggleAvailable)output.Action=AriaInterceptAction.ToggleSelected;else output.Action=AriaInterceptAction.Waiting;
      return true;
     }
     if((view.AvailableSquadMask&desired)==0){Release(ref plan,view.Time,ref output);return false;}
     output.Action=AriaInterceptAction.SelectSlot;output.Slot=plan.Slot;return true;
    }
    plan.Stage=2;
   }
   if(plan.Stage==2){
    if(!view.AttackMode){output.Action=view.AttackAvailable?AriaInterceptAction.ArmAttack:AriaInterceptAction.Waiting;return true;}
    plan.Stage=3;plan.Actions=view.CompletedContacts;
   }
   // Stage 3: one tap on the moving contact; the touch owner refreshes its position.
   if(view.CompletedContacts<=plan.Actions){
    output.Action=view.ThreatAvailable?AriaInterceptAction.TargetThreat:AriaInterceptAction.Waiting;return true;
   }
   plan.Stage=4;plan.Deadline=view.Time+HoldSeconds;output.Trace=2;output.Action=AriaInterceptAction.Waiting;return true;
  }
  // Prefer the visible anti-armour selection, then an anti-armour card on this page,
  // then any assault card on this page. Aircraft are never sent.
  public static int ChooseSlot(in AriaInterceptView view){
   if(view.SelectedOffPage==0&&(view.SelectedMask&view.StructureMask&~view.AirMask)!=0)return KeepSelection;
   int ground=view.AssaultMask&~view.AirMask&view.AvailableSquadMask;
   int armour=ground&view.StructureMask;
   int pick=armour!=0?armour:ground;
   if(pick==0)return -1;
   int bit=0;while((pick&1)==0){pick>>=1;bit++;}
   return bit;
  }
  static void Release(ref AriaInterceptState plan,float time,ref AriaInterceptDecision output){
   plan.Stage=0;plan.Slot=0;plan.ReadyAt=time+CooldownSeconds;output.Trace=3;output.Action=AriaInterceptAction.None;
  }
  // Public projection helper for one fog-visible hostile vehicle: distances to the
  // player's base, to the nearest own mobile unit and to the enemy base, against the
  // median own-unit distance to the player's base.
  public static bool Candidate(float homeDistance,float armyMedianHomeDistance,float nearestOwnDistance,float enemyBaseDistance){
   if(enemyBaseDistance<EnemyBaseRadius)return false;
   if(nearestOwnDistance<=ContactReach)return true;
   return homeDistance+SlipMargin<armyMedianHomeDistance&&nearestOwnDistance<=Reach;
  }
 }
}
