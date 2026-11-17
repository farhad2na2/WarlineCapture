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
  // PreferredSlotMask: the opening home guard's card on this page. A single
  // proportionate defender is that guard when it is shown (it is already home).
  public int PreferredSlotMask;
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
  // Public HUD base-health history (see AriaBaseDamagePolicy).
  public float HomeHealthSeen,HomeHealthPeak,HomeDamageAt;}
 public struct AriaHomeDefenseDecision {public AriaHomeDefenseAction Action;public int Slot;public bool Defend;}
 public static class AriaHomeDefensePolicy {
  public static bool Step(in AriaHomeDefenseView view,ref AriaHomeDefenseState plan,out AriaHomeDefenseDecision output){output=default;return StepSource(view,ref plan,ref output);}
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
                // The camera step only makes the defence target touchable. When that
                // target is already on screen a player orders at once (base is burning).
                bool targetShown = view.ProportionateDefense && DefenseTarget(view).Available;
                if (!view.FocusPlayer.Available && !targetShown) return true;
                plan.HomeDefenseRounds++;
                plan.HomeDefenseReason = view.BaseDamageOnly ? ReasonBaseDamage : ReasonMinimap;
                plan.HomeDefenseSlot = 0;
                // One group answers a raid. The whole roster only returns when
                // the base is about to fall or a first defender did not stop it.
                plan.HomeDefenseScope = view.ProportionateDefense && !view.BaseCritical && plan.HomeDefenseRounds <= 1 ? 1 : 0;
                plan.DefenseStage = 4;
                plan.HomeDefenseStage = 1;
                if (!targetShown)
                {
                    plan.HomeDefenseAction = view.CompletedContacts;
                    Target(view.FocusPlayer, false, ref output);
                    return true;
                }
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
                    int preferred = view.PreferredSlotMask & view.ExpandedAssaultMask;
                    int slot = LowestSetBit(preferred != 0 ? preferred : view.ExpandedAssaultMask);
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
            if (plan.HomeDefenseStage == 4)
            {
                if (!view.AttackMode) { Target(view.Attack, false, ref output); return true; }
                var threat = DefenseTarget(view);
                if (!threat.Available) { Target(view.FocusPlayer, false, ref output); return true; }
                plan.HomeDefenseAction = view.CompletedContacts;
                plan.HomeDefenseStage = 5;
                Target(threat, true, ref output);
                return true;
            }
            if (plan.HomeDefenseStage == 5)
            {
                if (view.CompletedContacts <= plan.HomeDefenseAction || view.AttackMode)
                { Target(DefenseTarget(view), true, ref output); return true; }
                plan.HomeDefenseStage = 6;
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
 public const float SingleDefenderObserveSeconds=20f;
 // A presented contact is fought where it is; base damage alone sends defenders onto the home ground.
 static AriaStrategyTarget DefenseTarget(in AriaHomeDefenseView view)=>view.BaseDamageOnly?view.HomeGround:view.ThreatGround.Available?view.ThreatGround:view.Threat;
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
 // Opening home guard for a ground Base Assault. A player who sees that the
 // enemy can reach an undefended base keeps one squad home: the smallest
 // infantry squad on the first roster page that cannot damage the designated
 // base, so every base-damaging squad still joins the assault. Public roster
 // card facts only (role category, structure capability, alive count).
 public struct AriaHomeGuardView {
  public int PageIndex,TotalGroups,AssaultMask,StructureMask,AirMask,InfantryMask;
  public int Alive0,Alive1,Alive2,Alive3,Alive4;
  public uint Group0,Group1,Group2,Group3,Group4;
  public int Alive(int slot)=>slot switch{0=>Alive0,1=>Alive1,2=>Alive2,3=>Alive3,4=>Alive4,_=>0};
  public uint Group(int slot)=>slot switch{0=>Group0,1=>Group1,2=>Group2,3=>Group3,4=>Group4,_=>0};
 }
 public static class AriaHomeGuardPolicy {
  public const int MinimumGroups=3;
  public static uint Choose(in AriaHomeGuardView view){
   if(view.PageIndex!=0||view.TotalGroups<MinimumGroups)return 0;
   int candidates=view.AssaultMask&view.InfantryMask&~view.StructureMask&~view.AirMask;
   int best=-1;
   for(int slot=0;slot<5;slot++){
    if((candidates&(1<<slot))==0||view.Group(slot)==0||view.Alive(slot)<=0)continue;
    if(best<0||view.Alive(slot)<view.Alive(best))best=slot;
   }
   if(best<0||(view.AssaultMask&~(1<<best))==0)return 0;
   return view.Group(best);
  }
  public static int SlotMask(uint guard,in AriaHomeGuardView view){
   if(guard==0)return 0;
   for(int slot=0;slot<5;slot++)if(view.Group(slot)==guard)return 1<<slot;
   return 0;
  }
 }
 // Posting the opening guard: one ordinary attack-move of the guard card onto
 // the public home ground (on the home approach), so it meets raiders before
 // they reach firing range instead of idling beside the base. Normal roster
 // controls only; bounded by a deadline and done once per session.
 public enum AriaHomeGuardPostAction:byte{None,Waiting,PreviousPage,ToggleSelected,SelectGuard,ArmAttack,TargetHomeGround,InspectHome}
 public struct AriaHomeGuardPostView {
  public uint Guard;public int GuardMask,PageIndex,SelectedMask,SelectedGroups,CompletedContacts;public float Time;
  public bool UiBusy,AttackMode,PreviousAvailable,ToggleAvailable,AttackAvailable,HomeGroundAvailable,FocusAvailable;
 }
 public static class AriaHomeGuardPostPolicy {
  public const float DeadlineSeconds=30f;
  // Stage: 0 not started, 1 posting, 3 home-ground touch issued, 2 finished
  // (posted, given up or no guard). A terrain attack-move does not set the
  // roster's objective AttackOrdered flag, so completion is the finished touch.
  public static AriaHomeGuardPostAction Step(in AriaHomeGuardPostView view,ref byte stage,ref float deadline,ref int actions){
   if(stage==2)return AriaHomeGuardPostAction.None;
   if(view.Guard==0){stage=2;return AriaHomeGuardPostAction.None;}
   if(stage==0){stage=1;deadline=view.Time+DeadlineSeconds;}
   if(view.Time>deadline){stage=2;return AriaHomeGuardPostAction.None;}
   if(stage==3){
    if(view.CompletedContacts>actions){stage=2;return AriaHomeGuardPostAction.None;}
    return view.HomeGroundAvailable?AriaHomeGuardPostAction.TargetHomeGround:AriaHomeGuardPostAction.Waiting;
   }
   if(view.UiBusy)return AriaHomeGuardPostAction.None;
   if(view.GuardMask==0){
    if(view.PageIndex>0&&view.PreviousAvailable)return AriaHomeGuardPostAction.PreviousPage;
    stage=2;return AriaHomeGuardPostAction.None;
   }
   if(view.SelectedGroups>0&&!(view.SelectedGroups==1&&view.SelectedMask==view.GuardMask))
    return view.ToggleAvailable?AriaHomeGuardPostAction.ToggleSelected:AriaHomeGuardPostAction.Waiting;
   if((view.SelectedMask&view.GuardMask)==0)return AriaHomeGuardPostAction.SelectGuard;
   if(!view.AttackMode)return view.AttackAvailable?AriaHomeGuardPostAction.ArmAttack:AriaHomeGuardPostAction.Waiting;
   // No camera fallback: an unchanged focus touch is not verifiable feedback.
   if(!view.HomeGroundAvailable)return AriaHomeGuardPostAction.Waiting;
   stage=3;actions=view.CompletedContacts;
   return AriaHomeGuardPostAction.TargetHomeGround;
  }
 }
 public static class AriaPublicHomeThreatPolicy {
  public const float RadiusSquared=120f*120f;
  public static bool Consider(AriaGroundPosition eligiblePublicHostile,AriaGroundPosition publicHome,ref float closestSquared){float distance=AriaGroundApproachPolicy.SquaredDistance(eligiblePublicHostile,publicHome);if(distance<closestSquared){closestSquared=distance;return true;}return false;}
 }

}
