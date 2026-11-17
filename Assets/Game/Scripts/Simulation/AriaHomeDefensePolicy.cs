using System;
namespace Game.Simulation {
 public enum AriaHomeDefenseAction:byte {None,Waiting,Unavailable,CancelPlacement,CloseDrawer,CloseMap,InspectHome,PreviousPage,NextPage,ToggleSelected,SelectSlot,ArmAttack,AttackThreatTerrain,AttackThreatVisible,AttackHomeTerrain}
 public readonly struct AriaStrategyTarget {public readonly bool Available;public readonly AriaHomeDefenseAction Action;public readonly int Slot;public AriaStrategyTarget(bool available,AriaHomeDefenseAction action,int slot=-1){Available=available;Action=action;Slot=slot;}}
 public struct AriaHomeDefenseView {
  public bool PresentedHomeThreat,PlacementOpen,DrawerOpen,MapOpen,AttackMode;
  public bool CancelAvailable,CloseDrawerAvailable,CloseMapAvailable,FocusAvailable,PreviousAvailable,NextAvailable,ToggleAvailable,AttackAvailable,ThreatGroundAvailable,ThreatVisibleAvailable;
  public int ExpandedPageIndex,ExpandedSelectedGroups,ExpandedAssaultMask,ExpandedSelectedMask,AvailableSquadMask,CompletedContacts;public float Time;
  // BaseDamageOnly: the threat is the HUD base-health readout falling, with no
  // presented minimap contact. Defenders then attack-move onto public home ground.
  // ProportionateDefense: send one roster group first; BaseCritical escalates.
  // Callers that leave these false keep the original all-group sweep unchanged.
  public bool BaseDamageOnly,ProportionateDefense,BaseCritical,HomeGroundAvailable;
  public AriaStrategyTarget PlacementCancel=>new AriaStrategyTarget(CancelAvailable,AriaHomeDefenseAction.CancelPlacement);
  public AriaStrategyTarget CloseDrawer=>new AriaStrategyTarget(CloseDrawerAvailable,AriaHomeDefenseAction.CloseDrawer);
  public AriaStrategyTarget CloseMap=>new AriaStrategyTarget(CloseMapAvailable,AriaHomeDefenseAction.CloseMap);
  public AriaStrategyTarget FocusPlayer=>new AriaStrategyTarget(FocusAvailable,AriaHomeDefenseAction.InspectHome);
  public AriaStrategyTarget PreviousSquadPage=>new AriaStrategyTarget(PreviousAvailable,AriaHomeDefenseAction.PreviousPage);
  public AriaStrategyTarget NextSquadPage=>new AriaStrategyTarget(NextAvailable,AriaHomeDefenseAction.NextPage);
  public AriaStrategyTarget SelectedSquadToToggle=>new AriaStrategyTarget(ToggleAvailable,AriaHomeDefenseAction.ToggleSelected);
  public AriaStrategyTarget Attack=>new AriaStrategyTarget(AttackAvailable,AriaHomeDefenseAction.ArmAttack);
  public AriaStrategyTarget ThreatGround=>new AriaStrategyTarget(ThreatGroundAvailable,AriaHomeDefenseAction.AttackThreatTerrain);
  public AriaStrategyTarget Threat=>new AriaStrategyTarget(ThreatVisibleAvailable,AriaHomeDefenseAction.AttackThreatVisible);
  public AriaStrategyTarget HomeGround=>new AriaStrategyTarget(HomeGroundAvailable,AriaHomeDefenseAction.AttackHomeTerrain);
  public AriaStrategyTarget Squad(int slot)=>new AriaStrategyTarget(slot>=0&&slot<5&&(AvailableSquadMask&(1<<slot))!=0,AriaHomeDefenseAction.SelectSlot,slot);
 }
 public struct AriaHomeDefenseState {public int HomeDefenseStage,HomeDefenseAction,HomeDefensePage,DefenseStage;public float HomeDefenseObserveUntil;public AriaGroundColumnState Column;
  // Reason: 0 none, 1 minimap contact, 2 HUD base damage. Slot: chosen roster slot + 1 (0 none).
  // Scope: 0 all assault groups (legacy sweep), 1 single proportionate defender. Rounds: triggers since the last clear.
  public int HomeDefenseReason,HomeDefenseSlot,HomeDefenseScope,HomeDefenseRounds;
  // Recovery bits: 1 the camera was re-centred once this round, 2 the ground tap was repeated once this round,
  // 4 (kept across rounds) the last new press of this policy was the home focus control,
  // 8 Attack was toggled off once for the re-centre this round.
  // RecoveryAction: completed-contact count when the recovery touch was issued.
  public int HomeDefenseRecovery,HomeDefenseRecoveryAction;
  // Public HUD base-health history (see AriaBaseDamagePolicy).
  public float HomeHealthSeen,HomeHealthPeak,HomeDamageAt;}
 public struct AriaHomeDefenseDecision {public AriaHomeDefenseAction Action;public int Slot;public bool Defend;}
 public static class AriaHomeDefensePolicy {
  public static bool Step(in AriaHomeDefenseView view,ref AriaHomeDefenseState plan,out AriaHomeDefenseDecision output){
   output=default;bool handled=StepSource(view,ref plan,ref output);
   // Remember whether the newest press was the home focus, so a focus is never pressed twice in a row.
   if(output.Action==AriaHomeDefenseAction.InspectHome)plan.HomeDefenseRecovery|=RecoveryLastFocus;
   else if(output.Action>AriaHomeDefenseAction.Unavailable)plan.HomeDefenseRecovery&=~RecoveryLastFocus;
   if(!view.PresentedHomeThreat)plan.HomeDefenseRecovery=0;
   return handled;
  }
  static bool StepSource(in AriaHomeDefenseView view,ref AriaHomeDefenseState plan,ref AriaHomeDefenseDecision output)
{
            if (!view.PresentedHomeThreat)
            {
                if (plan.HomeDefenseStage != 0)
                {
                    plan.HomeDefenseStage = 0;
                    // Defensive orders do not prove the enemy-base sweep was issued.
                    plan.Column.AssaultIssued = plan.Column.PagedToAssault = plan.Column.AssaultSelecting = plan.Column.StructureOrdered = 0;
                    plan.Column.PagesOrdered = plan.Column.AssaultPageSeen = 0;
                }
                plan.HomeDefenseReason = plan.HomeDefenseSlot = plan.HomeDefenseScope = plan.HomeDefenseRounds = 0;
                return false;
            }
            output.Defend = true;
            // Finish through the same player controls before selecting defenders.
            if (view.PlacementOpen) { Target(view.PlacementCancel, false, ref output); return true; }
            if (view.DrawerOpen) { Target(view.CloseDrawer, false, ref output); return true; }
            if (view.MapOpen) { Target(view.CloseMap, false, ref output); return true; }
            if (plan.HomeDefenseStage == 0)
            {
                if (!view.FocusPlayer.Available) return true;
                plan.HomeDefenseRounds++;
                plan.HomeDefenseReason = view.BaseDamageOnly ? ReasonBaseDamage : ReasonMinimap;
                plan.HomeDefenseSlot = 0;
                // One group answers a raid. The whole roster only returns when
                // the base is about to fall or a first defender did not stop it.
                plan.HomeDefenseScope = view.ProportionateDefense && !view.BaseCritical && plan.HomeDefenseRounds <= 1 ? 1 : 0;
                plan.DefenseStage = 4;
                plan.HomeDefenseRecovery &= RecoveryLastFocus;
                plan.HomeDefenseStage = 1;
                if ((plan.HomeDefenseRecovery & RecoveryLastFocus) == 0)
                {
                    plan.HomeDefenseAction = view.CompletedContacts;
                    Target(view.FocusPlayer, false, ref output);
                    return true;
                }
                // The last press was already this focus (a round given up without a target):
                // the camera is on home; do not press the same control again.
                plan.HomeDefenseRecovery |= RecoveryRecentred;
                plan.HomeDefenseAction = view.CompletedContacts - 1;
            }
            if (plan.HomeDefenseStage == 1)
            {
                if (view.CompletedContacts <= plan.HomeDefenseAction)
                { Target(view.FocusPlayer, false, ref output); return true; }
                if (view.ExpandedPageIndex > 0)
                { Target(view.PreviousSquadPage, false, ref output); return true; }
                plan.HomeDefensePage = 0;
                plan.HomeDefenseStage = 2;
            }
            if (plan.HomeDefenseStage == 2)
            {
                if (TryDeselectSquads(view, ref output)) return true;
                // Deselecting an off-screen squad may have moved the roster.
                // Restore this sweep's window, including later defense windows.
                if (view.ExpandedPageIndex != plan.HomeDefensePage)
                {
                    Target(view.ExpandedPageIndex < plan.HomeDefensePage
                        ? view.NextSquadPage : view.PreviousSquadPage, false, ref output);
                    return true;
                }
                plan.HomeDefenseStage = 3;
            }
            if (plan.HomeDefenseStage == 3 && plan.HomeDefenseScope == 1)
            {
                if (view.ExpandedAssaultMask == 0) plan.HomeDefenseScope = 0;
                else
                {
                    int slot = LowestSetBit(view.ExpandedAssaultMask);
                    plan.HomeDefenseSlot = slot + 1;
                    if ((view.ExpandedSelectedMask & (1 << slot)) == 0)
                    { Target(view.Squad(slot), false, ref output); return true; }
                    plan.HomeDefenseStage = 4;
                }
            }
            if (plan.HomeDefenseStage == 3)
            {
                int pending = view.ExpandedAssaultMask & ~view.ExpandedSelectedMask;
                if (pending != 0)
                { Target(view.Squad(LowestSetBit(pending)), false, ref output); return true; }
                plan.HomeDefenseStage = view.ExpandedAssaultMask == 0 ? 6 : 4;
            }
            if (plan.HomeDefenseStage == StageRecentre)
            {
                // One camera re-centre on home, with Attack off, before arming it again.
                if (view.CompletedContacts <= plan.HomeDefenseRecoveryAction && view.FocusPlayer.Available)
                { Target(view.FocusPlayer, false, ref output); return true; }
                // Let the camera settle on home before deciding that no defence target exists
                // (Windows lane, 17 fps: the decision came in the frame the focus tap completed).
                if (!DefenseTarget(view).Available && view.Time < plan.HomeDefenseObserveUntil)
                { output.Action = AriaHomeDefenseAction.Waiting; return true; }
                plan.HomeDefenseStage = 4;
            }
            if (plan.HomeDefenseStage == 4)
            {
                var threat = DefenseTarget(view);
                if (!threat.Available)
                {
                    // A focus control pressed with Attack armed does not move the camera
                    // (Windows fa-IR 130365: four Focus taps with Attack armed, Blocked 115).
                    // Re-centre once with Attack off; if the ground is still not offered, give
                    // this round up and retry later instead of repeating a control.
                    if ((plan.HomeDefenseRecovery & (RecoveryRecentred | RecoveryLastFocus)) == 0 && view.FocusPlayer.Available)
                    {
                        if (view.AttackMode)
                        {
                            // One Attack toggle to disarm before the focus; if it did not disarm, give up.
                            if ((plan.HomeDefenseRecovery & RecoveryDisarmed) == 0 && view.AttackAvailable)
                            {
                                plan.HomeDefenseRecovery |= RecoveryDisarmed;
                                plan.HomeDefenseRecoveryAction = view.CompletedContacts;
                                Target(view.Attack, false, ref output); return true;
                            }
                            if (view.CompletedContacts <= plan.HomeDefenseRecoveryAction && view.AttackAvailable)
                            { Target(view.Attack, false, ref output); return true; }
                            return GiveUpRound(view, ref plan, ref output);
                        }
                        plan.HomeDefenseRecovery |= RecoveryRecentred;
                        plan.HomeDefenseRecoveryAction = view.CompletedContacts;
                        plan.HomeDefenseObserveUntil = view.Time + RecentreSeconds;
                        plan.HomeDefenseStage = StageRecentre;
                        Target(view.FocusPlayer, false, ref output);
                        return true;
                    }
                    return GiveUpRound(view, ref plan, ref output);
                }
                if (!view.AttackMode) { Target(view.Attack, false, ref output); return true; }
                plan.HomeDefenseAction = view.CompletedContacts;
                plan.HomeDefenseStage = 5;
                Target(threat, true, ref output);
                return true;
            }
            if (plan.HomeDefenseStage == 5)
            {
                if (view.CompletedContacts <= plan.HomeDefenseAction)
                { Target(DefenseTarget(view), true, ref output); return true; }
                if (view.AttackMode)
                {
                    // The ground tap completed but Attack stayed armed: repeat it once, then
                    // give the round up rather than pressing the same ground a third time.
                    var again = DefenseTarget(view);
                    if ((plan.HomeDefenseRecovery & RecoveryRetapped) == 0 && again.Available)
                    {
                        plan.HomeDefenseRecovery |= RecoveryRetapped;
                        plan.HomeDefenseAction = view.CompletedContacts;
                        Target(again, true, ref output);
                        return true;
                    }
                    return GiveUpRound(view, ref plan, ref output);
                }
                plan.HomeDefenseStage = 6;
            }
            if (plan.HomeDefenseStage == StageDisarm)
            {
                // Never leave Attack armed for the next owner: one toggle, then observe.
                if (view.AttackMode && view.AttackAvailable && view.CompletedContacts <= plan.HomeDefenseRecoveryAction &&
                    view.Time < plan.HomeDefenseObserveUntil)
                { Target(view.Attack, false, ref output); return true; }
                plan.HomeDefenseObserveUntil = view.Time + RetrySeconds;
                plan.HomeDefenseStage = 8;
                return true;
            }
            if (plan.HomeDefenseStage == 6 && plan.HomeDefenseScope == 1)
            {
                // The single defender needs travel time before escalation.
                plan.HomeDefenseObserveUntil = view.Time + SingleDefenderObserveSeconds;
                plan.HomeDefenseStage = 8;
                return true;
            }
            if (plan.HomeDefenseStage == 6)
            {
                if (view.NextSquadPage.Available)
                {
                    plan.HomeDefensePage = view.ExpandedPageIndex;
                    plan.HomeDefenseStage = 7;
                    Target(view.NextSquadPage, false, ref output);
                    return true;
                }
                plan.HomeDefenseObserveUntil = view.Time + 8f;
                plan.HomeDefenseStage = 8;
            }
            if (plan.HomeDefenseStage == 7)
            {
                if (view.ExpandedPageIndex == plan.HomeDefensePage)
                    Target(view.NextSquadPage, false, ref output);
                else
                {
                    plan.HomeDefensePage = view.ExpandedPageIndex;
                    plan.HomeDefenseStage = 2;
                }
                return true;
            }
            // A falling base escalates a single defender to the full roster now.
            if (view.Time >= plan.HomeDefenseObserveUntil || plan.HomeDefenseScope == 1 && view.BaseCritical) plan.HomeDefenseStage = 0;
            return true;
        }
 public const int ReasonNone=0,ReasonMinimap=1,ReasonBaseDamage=2;
 public const int StageDisarm=9,StageRecentre=10,RecoveryRecentred=1,RecoveryRetapped=2,RecoveryLastFocus=4,RecoveryDisarmed=8;
 public const float SingleDefenderObserveSeconds=20f,RetrySeconds=4f,DisarmSeconds=3f,RecentreSeconds=4f;
 // A presented contact is fought where it is; base damage alone sends defenders onto the home ground.
 // When the preferred ground is not offered, any other offered defence target is used.
 public static AriaStrategyTarget DefenseTarget(in AriaHomeDefenseView view){
  if(view.BaseDamageOnly)return view.HomeGround.Available?view.HomeGround:view.ThreatGround.Available?view.ThreatGround:view.Threat.Available?view.Threat:view.HomeGround;
  return view.ThreatGround.Available?view.ThreatGround:view.Threat.Available?view.Threat:view.HomeGround;
 }
 // No defence target after the one re-centre (or a ground tap that left Attack armed twice):
 // toggle Attack off, observe briefly, then a new round starts from the home focus. Different
 // controls alternate, so no control is pressed three times in a row and ARIA keeps playing.
 static bool GiveUpRound(in AriaHomeDefenseView view,ref AriaHomeDefenseState plan,ref AriaHomeDefenseDecision output){
  plan.HomeDefenseRecoveryAction=view.CompletedContacts;
  plan.HomeDefenseObserveUntil=view.Time+DisarmSeconds;
  plan.HomeDefenseStage=StageDisarm;
  if(view.AttackMode&&view.AttackAvailable&&(plan.HomeDefenseRecovery&RecoveryDisarmed)==0)
  {plan.HomeDefenseRecovery|=RecoveryDisarmed;Target(view.Attack,false,ref output);return true;}
  plan.HomeDefenseObserveUntil=view.Time+RetrySeconds;plan.HomeDefenseStage=8;output.Action=AriaHomeDefenseAction.Waiting;return true;
 }
 static bool TryDeselectSquads(in AriaHomeDefenseView view,ref AriaHomeDefenseDecision output){if(view.ExpandedSelectedGroups<=0)return false;if(view.SelectedSquadToToggle.Available)Target(view.SelectedSquadToToggle,false,ref output);else output.Action=AriaHomeDefenseAction.Waiting;return true;}
 static int LowestSetBit(int mask){int bit=0,value=mask;while((value&1)==0){value>>=1;bit++;}return bit;}
 static void Target(AriaStrategyTarget target,bool world,ref AriaHomeDefenseDecision output){if(!target.Available){output.Action=AriaHomeDefenseAction.Unavailable;return;}output.Action=target.Action;output.Slot=target.Slot;}
 }
 // Public HUD base-health readout only. Any drop is the same signal a player
 // sees on the base-health label; it stays a threat until the readout has
 // held steady for MemorySeconds (observation clock).
 public static class AriaBaseDamagePolicy {
  public const float MemorySeconds=6f,CriticalFraction=.4f;
  public static bool Observe(float time,float baseHealth,ref AriaHomeDefenseState plan){
   if(!(baseHealth>0f)||float.IsInfinity(baseHealth)||float.IsNaN(time)){plan.HomeHealthSeen=0f;return false;}
   if(baseHealth>plan.HomeHealthPeak)plan.HomeHealthPeak=baseHealth;
   if(plan.HomeHealthSeen>0f&&baseHealth<plan.HomeHealthSeen-.01f)plan.HomeDamageAt=time;
   plan.HomeHealthSeen=baseHealth;
   return plan.HomeDamageAt>0f&&time>=plan.HomeDamageAt&&time-plan.HomeDamageAt<=MemorySeconds;
  }
  public static bool Critical(float peak,float baseHealth)=>peak>0f&&baseHealth>0f&&baseHealth<=peak*CriticalFraction;
 }
 public static class AriaPublicHomeThreatPolicy {
  public const float RadiusSquared=120f*120f;
  public static bool Consider(AriaGroundPosition eligiblePublicHostile,AriaGroundPosition publicHome,ref float closestSquared){float distance=AriaGroundApproachPolicy.SquaredDistance(eligiblePublicHostile,publicHome);if(distance<closestSquared){closestSquared=distance;return true;}return false;}
 }

}
