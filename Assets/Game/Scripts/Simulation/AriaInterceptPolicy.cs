namespace Game.Simulation {
 public enum AriaInterceptAction:byte {None,Waiting,ToggleSelected,SelectSlot,ArmAttack,TargetThreat,PanToThreat}
 // "Finish what slips past": a visible hostile vehicle crossing the advancing column,
 // or slipping past it toward the player's base, is attacked by one roster group
 // through the normal card / Attack / tap controls, exactly as a player taps a
 // damaged tank driving through a column. When the vehicle is visible but outside
 // the camera view, the player drags the view onto it while Attack is armed (the
 // game pans and issues no order on a drag). Only fog-visible contacts are known
 // (ThreatKnown); this policy never reads hidden state and issues no order itself.
 public struct AriaInterceptView {
  // A fog-visible hostile vehicle crossing or slipping past the column (public view).
  public bool ThreatKnown;
  // That vehicle is on screen and tappable (no HUD over it).
  public bool ThreatAvailable;
  // One clear world drag brings the vehicle into the view (never offered for contacts
  // farther away: those cannot be reached inside the short visibility window).
  public bool PanAvailable;
  // Another owner holds the controls or the moment: open drawer/map/placement,
  // home defence already running, precision attack, camera navigation, army at the enemy base.
  public bool Busy;
  // At least one roster group already carries the accepted assault order.
  public bool ArmyMoving;
  public bool AttackMode,ToggleAvailable,AttackAvailable;
  public int SelectedMask,SelectedGroups,SelectedOffPage,StructureMask,AssaultMask,AirMask,AvailableSquadMask,CompletedContacts;
  public float Time;
 }
 // Stage: 0 idle, 1 select, 2 arm Attack, 3 tap the contact, 4 let the group fight,
 // 5 a camera drag toward the contact is in flight (Attack stays armed),
 // 6 Attack is toggled off before the controls are handed back (no tap landed).
 // Slot: roster slot on the current page, or KeepSelection when the visible
 // selection already holds an anti-armour (structure-capable) group.
 public struct AriaInterceptState {public byte Stage,Count,Pans;public int Slot,Actions;public float Deadline,LastSeenAt,ReadyAt;}
 // Trace: 0 none, 1 triggered, 2 ordered, 3 released.
 public struct AriaInterceptDecision {public AriaInterceptAction Action;public int Slot;public byte Trace;}
 public static class AriaInterceptPolicy {
  public const int KeepSelection=5,MaxIntercepts=3;
  public const byte StageIdle=0,StageSelect=1,StageArm=2,StageTap=3,StageFight=4,StagePan=5,StageDisarm=6;
  // One drag per intercept: repeated drags on one control are touch retries.
  public const int MaxPans=1;
  public const float DisarmSeconds=4f;
  // Order: the whole select/Attack/pan/tap sequence. Hold: the group fights before the
  // ordinary assault sweep re-issues its base order. Lost: contact gone (dead or fog).
  // HoldLost: a chased contact may leave sight briefly; give the ordered group a moment.
  public const float OrderSeconds=12f,HoldSeconds=12f,LostSeconds=2f,HoldLostSeconds=4f,CooldownSeconds=6f;
  // Public projection geometry (world units). A visible vehicle crossing the column
  // (ContactReach of an own mobile unit), or within Reach of an own unit and closer to
  // the player's base than that nearest own unit by SlipMargin (it has passed it).
  // Vehicles near the enemy base belong to the base assault, not to this rule.
  public const float SlipMargin=5f,Reach=60f,ContactReach=40f,EnemyBaseRadius=60f;
  public static bool Step(in AriaInterceptView view,ref AriaInterceptState plan,out AriaInterceptDecision output){
   output=default;
   if(plan.Stage==StageIdle){
    if(!view.ThreatKnown||!(view.ThreatAvailable||view.PanAvailable)||view.Busy||!view.ArmyMoving||
       plan.Count>=MaxIntercepts||view.Time<plan.ReadyAt)return false;
    int slot=ChooseSlot(view);if(slot<0)return false;
    plan.Stage=slot==KeepSelection?StageArm:StageSelect;plan.Slot=slot;plan.Count++;plan.Pans=0;
    plan.Deadline=view.Time+OrderSeconds;plan.LastSeenAt=view.Time;plan.Actions=view.CompletedContacts;
    output.Trace=1;
   }
   if(plan.Stage==StageDisarm){
    // Never hand back an armed Attack: the next owner would aim it elsewhere.
    if(!view.AttackMode||view.CompletedContacts>plan.Actions||view.Time>plan.Deadline||!view.AttackAvailable)
    {Release(ref plan,view.Time,ref output);return false;}
    output.Action=AriaInterceptAction.ArmAttack;return true;
   }
   if(view.ThreatKnown)plan.LastSeenAt=view.Time;
   if(plan.Stage==StageFight){
    if(view.Busy||view.Time-plan.LastSeenAt>HoldLostSeconds||view.Time>plan.Deadline)return Finish(view,ref plan,ref output);
    output.Action=AriaInterceptAction.Waiting;return true;
   }
   if(view.Busy||view.Time-plan.LastSeenAt>LostSeconds||view.Time>plan.Deadline)return Finish(view,ref plan,ref output);
   if(plan.Stage==StageSelect){
    int desired=1<<plan.Slot;
    bool exact=view.SelectedGroups==1&&view.SelectedOffPage==0&&view.SelectedMask==desired;
    if(!exact){
     if(view.SelectedGroups>0){
      output.Action=view.ToggleAvailable?AriaInterceptAction.ToggleSelected:AriaInterceptAction.Waiting;
      return true;
     }
     if((view.AvailableSquadMask&desired)==0){Release(ref plan,view.Time,ref output);return false;}
     output.Action=AriaInterceptAction.SelectSlot;output.Slot=plan.Slot;return true;
    }
    plan.Stage=StageArm;
   }
   if(plan.Stage==StagePan){
    // The drag is a completed contact of its own, never the attack tap.
    if(view.CompletedContacts>plan.Actions){plan.Actions=view.CompletedContacts;plan.Stage=view.AttackMode?StageTap:StageArm;}
    else if(!view.ThreatAvailable){
     output.Action=view.PanAvailable?AriaInterceptAction.PanToThreat:AriaInterceptAction.Waiting;return true;
    }
    else plan.Stage=StageTap; // In view before the drag began: tap instead.
   }
   if(plan.Stage==StageArm){
    if(!view.AttackMode){output.Action=view.AttackAvailable?AriaInterceptAction.ArmAttack:AriaInterceptAction.Waiting;return true;}
    plan.Stage=StageTap;plan.Actions=view.CompletedContacts;
   }
   // Stage 3: one tap on the moving contact; the touch owner refreshes its position.
   if(view.CompletedContacts<=plan.Actions){
    if(view.ThreatAvailable){output.Action=AriaInterceptAction.TargetThreat;return true;}
    if(view.PanAvailable&&plan.Pans<MaxPans){plan.Stage=StagePan;plan.Pans++;output.Action=AriaInterceptAction.PanToThreat;return true;}
    output.Action=AriaInterceptAction.Waiting;return true;
   }
   plan.Stage=StageFight;plan.Deadline=view.Time+HoldSeconds;output.Trace=2;output.Action=AriaInterceptAction.Waiting;return true;
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
  // End of an intercept: toggle Attack off first when it is still armed (stage 6).
  static bool Finish(in AriaInterceptView view,ref AriaInterceptState plan,ref AriaInterceptDecision output){
   if(view.AttackMode&&view.AttackAvailable&&plan.Stage!=StageFight){
    plan.Stage=StageDisarm;plan.Actions=view.CompletedContacts;plan.Deadline=view.Time+DisarmSeconds;
    output.Action=AriaInterceptAction.ArmAttack;return true;
   }
   Release(ref plan,view.Time,ref output);return false;
  }
  static void Release(ref AriaInterceptState plan,float time,ref AriaInterceptDecision output){
   plan.Stage=StageIdle;plan.Slot=0;plan.ReadyAt=time+CooldownSeconds;output.Trace=3;output.Action=AriaInterceptAction.None;
  }
  // Public projection helper for one fog-visible hostile vehicle: its distance to the
  // player's base, to its nearest own mobile unit, that unit's distance to the
  // player's base, and the vehicle's distance to the enemy base.
  public static bool Candidate(float homeDistance,float nearestOwnDistance,float nearestOwnHomeDistance,float enemyBaseDistance){
   if(enemyBaseDistance<EnemyBaseRadius||nearestOwnDistance>Reach)return false;
   if(nearestOwnDistance<=ContactReach)return true;
   return homeDistance+SlipMargin<nearestOwnHomeDistance;
  }
  // Camera drag (screen pixels) from a fixed clear start: the scene follows the finger,
  // so dragging by (centre - contact) moves the contact toward the centre. The drag is
  // clamped to maxX/maxY; repeated drags close any remaining distance.
  // single: the whole offset fits in one drag (the contact can be in view after it).
  public static bool PanDelta(float contactX,float contactY,float centreX,float centreY,float maxX,float maxY,out float dx,out float dy,out bool single){
   dx=centreX-contactX;dy=centreY-contactY;
   float scale=1f;
   if(dx>maxX||dx<-maxX)scale=System.Math.Min(scale,maxX/System.Math.Abs(dx));
   if(dy>maxY||dy<-maxY)scale=System.Math.Min(scale,maxY/System.Math.Abs(dy));
   single=scale>=1f;
   dx*=scale;dy*=scale;
   return dx*dx+dy*dy>=20f*20f;
  }
 }

 public enum AriaColumnCameraAction:byte {None,Home,Recentre}
 // Watching the march: after the visible page's assault groups are ordered, the player
 // taps the HUD Home button once (the Advance point sits in front of the enemy base, far
 // from the column) and then keeps the marching column framed with ordinary camera drags,
 // re-centring only when it leaves the middle third of the view. Own-unit positions are
 // public to their player; no minimap, no hidden state, no orders.
 public struct AriaColumnCameraView {
  // Assault ordered for at least one group, the army not yet at the enemy base.
  public bool Marching,AtBase,EnemyAlive;
  // Every assault group on the visible roster page carries its order.
  public bool PageOrdered;
  // Any other owner holds the controls or the camera: open drawer/map/placement, Attack
  // armed, intercept/home defence/precision/navigation/inspection/pad in progress.
  public bool Busy;
  // The base-assault step is re-finding the Advance point (map navigation).
  public bool Refinding;
  public bool ColumnKnown,ColumnCentred,HomeAvailable,PanAvailable;
  public int CompletedContacts;public float Time;
 }
 // Stage: 0 idle, 1 Home tap pending, 2 re-centring drag pending.
 public struct AriaColumnCameraState {public byte Stage,HomeDone,Done,Pans,Refinds,RefindSeen;public int Actions;public float Deadline,ReadyAt;}
 // Trace: 0 none, 1 home, 2 recentre, 3 done.
 public struct AriaColumnCameraDecision {public AriaColumnCameraAction Action;public byte Trace;public int PanId;}
 public static class AriaColumnCameraPolicy {
  // Pace: one camera action at most every IntervalSeconds. Bounds: MaxPans drags per match;
  // after MaxRefinds Advance-point re-finds the rule stops (today's behaviour).
  public const float IntervalSeconds=2.5f,ActionSeconds=4f;
  public const int MaxPans=12,MaxRefinds=2;
  // Alternate drag controls: consecutive drags are separate targets, never touch retries.
  public const int PanIdA=-20016,PanIdB=-20017;
  public static bool Step(in AriaColumnCameraView view,ref AriaColumnCameraState plan,out AriaColumnCameraDecision output){
   output=default;
   if(plan.Done!=0)return false;
   if(view.Refinding&&plan.HomeDone!=0&&plan.RefindSeen==0){plan.RefindSeen=1;plan.Refinds++;}
   if(!view.Refinding)plan.RefindSeen=0;
   if(view.AtBase||!view.EnemyAlive||plan.Refinds>=MaxRefinds){plan.Done=1;plan.Stage=0;output.Trace=3;return false;}
   if(plan.Stage!=0){
    if(view.Busy||view.CompletedContacts>plan.Actions||view.Time>plan.Deadline){plan.Stage=0;plan.ReadyAt=view.Time+IntervalSeconds;return false;}
    if(plan.Stage==1){if(!view.HomeAvailable){plan.Stage=0;plan.ReadyAt=view.Time+IntervalSeconds;return false;}output.Action=AriaColumnCameraAction.Home;return true;}
    if(!view.PanAvailable){plan.Stage=0;plan.ReadyAt=view.Time+IntervalSeconds;return false;}
    output.Action=AriaColumnCameraAction.Recentre;output.PanId=PanId(plan.Pans);return true;
   }
   if(view.Busy||!view.Marching||!view.ColumnKnown||view.Time<plan.ReadyAt)return false;
   if(plan.HomeDone==0){
    if(!view.PageOrdered)return false;
    plan.HomeDone=1;
    if(view.ColumnCentred||!view.HomeAvailable)return false; // Already watching the column.
    plan.Stage=1;plan.Actions=view.CompletedContacts;plan.Deadline=view.Time+ActionSeconds;
    output.Action=AriaColumnCameraAction.Home;output.Trace=1;return true;
   }
   if(view.ColumnCentred||!view.PanAvailable||plan.Pans>=MaxPans)return false;
   plan.Stage=2;plan.Pans++;plan.Actions=view.CompletedContacts;plan.Deadline=view.Time+ActionSeconds;
   output.Action=AriaColumnCameraAction.Recentre;output.PanId=PanId(plan.Pans);output.Trace=2;return true;
  }
  public static int PanId(int pans)=>(pans&1)!=0?PanIdA:PanIdB;
  // Middle third of the view (screen pixels, any origin).
  public static bool Centred(float x,float y,float left,float bottom,float width,float height)=>
   x>=left+width/3f&&x<=left+width*2f/3f&&y>=bottom+height/3f&&y<=bottom+height*2f/3f;
 }
}
