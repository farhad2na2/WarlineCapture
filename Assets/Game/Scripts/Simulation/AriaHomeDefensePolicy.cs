using System;
namespace Game.Simulation {
 public enum AriaHomeDefenseAction:byte {None,Waiting,Unavailable,CancelPlacement,CloseDrawer,CloseMap,InspectHome,PreviousPage,NextPage,ToggleSelected,SelectSlot,ArmAttack,AttackThreatTerrain,AttackThreatVisible,AttackHomeTerrain,AttackAttackerVisible,AttackAttackerTerrain,AttackHomeFarTerrain,PageToDefender}
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
  // Ranged raid (lane 14 S002 155923: a tank shelled the HQ from ~66 units and no contact was presented
  // inside the home radius). The public attacker is the nearest presented enemy marker near home, a
  // fog-visible unit or its last-seen minimap ghost: AttackerVisible is that unit on screen, AttackerGround
  // the clear ground next to it. HomeGroundFar is clear ground on the far side of home from the column,
  // used when no attacker is presented and the column-side ground already failed once.
  // AntiArmorMask: roster cards on this page whose presented role fights armour (rocketeer, tank, siege).
  public bool AttackerVisibleAvailable,AttackerGroundAvailable,HomeGroundFarAvailable;public int AntiArmorMask;
  // HUD base-under-attack warning (ISkirmishBaseAlertPresentation): BaseAlert is the warning on screen.
  // HighlightedDefender: slot + 1 of the squad card the warning highlights as nearest defender on this page
  // (0 none or on another page). DefenderKnown: a card is highlighted somewhere; DefenderPageAvailable: the
  // warning's control that pages the roster to that card is offered.
  public bool BaseAlert,DefenderKnown,DefenderPageAvailable;public int HighlightedDefender;
  public AriaStrategyTarget PlacementCancel=>new AriaStrategyTarget(CancelAvailable,AriaHomeDefenseAction.CancelPlacement);
  public AriaStrategyTarget CloseDrawer=>new AriaStrategyTarget(CloseDrawerAvailable,AriaHomeDefenseAction.CloseDrawer);
  public AriaStrategyTarget CloseMap=>new AriaStrategyTarget(CloseMapAvailable,AriaHomeDefenseAction.CloseMap);
  public AriaStrategyTarget FocusPlayer=>new AriaStrategyTarget(FocusAvailable,AriaHomeDefenseAction.InspectHome);
  public AriaStrategyTarget PreviousSquadPage=>new AriaStrategyTarget(PreviousAvailable,AriaHomeDefenseAction.PreviousPage);
  public AriaStrategyTarget NextSquadPage=>new AriaStrategyTarget(NextAvailable,AriaHomeDefenseAction.NextPage);
  public AriaStrategyTarget SelectedSquadToToggle=>new AriaStrategyTarget(ToggleAvailable,AriaHomeDefenseAction.ToggleSelected);
  public AriaStrategyTarget Attack=>new AriaStrategyTarget(AttackAvailable&&ExpandedSelectedMask!=0,AriaHomeDefenseAction.ArmAttack);
  public AriaStrategyTarget ThreatGround=>new AriaStrategyTarget(ThreatGroundAvailable,AriaHomeDefenseAction.AttackThreatTerrain);
  public AriaStrategyTarget Threat=>new AriaStrategyTarget(ThreatVisibleAvailable,AriaHomeDefenseAction.AttackThreatVisible);
  public AriaStrategyTarget HomeGround=>new AriaStrategyTarget(HomeGroundAvailable,AriaHomeDefenseAction.AttackHomeTerrain);
  public AriaStrategyTarget Attacker=>new AriaStrategyTarget(AttackerVisibleAvailable,AriaHomeDefenseAction.AttackAttackerVisible);
  public AriaStrategyTarget AttackerGround=>new AriaStrategyTarget(AttackerGroundAvailable,AriaHomeDefenseAction.AttackAttackerTerrain);
  public AriaStrategyTarget HomeGroundFar=>new AriaStrategyTarget(HomeGroundFarAvailable,AriaHomeDefenseAction.AttackHomeFarTerrain);
  public AriaStrategyTarget DefenderPage=>new AriaStrategyTarget(DefenderPageAvailable,AriaHomeDefenseAction.PageToDefender);
  public AriaStrategyTarget Squad(int slot)=>new AriaStrategyTarget(slot>=0&&slot<5&&(AvailableSquadMask&(1<<slot))!=0,AriaHomeDefenseAction.SelectSlot,slot);
 }
 public struct AriaHomeDefenseState {public int HomeDefenseStage,HomeDefenseAction,HomeDefensePage,DefenseStage;public float HomeDefenseObserveUntil;public AriaGroundColumnState Column;
  // Reason: 0 none, 1 minimap contact, 2 HUD base damage. Slot: chosen roster slot + 1 (0 none).
  // Scope: 0 all assault groups (legacy sweep), 1 single proportionate defender. Rounds: triggers since the last clear.
  public int HomeDefenseReason,HomeDefenseSlot,HomeDefenseScope,HomeDefenseRounds;
  // Single-defender round: the roster bits chosen once for this round (DefenderMask), so a moving
  // nearest-card highlight cannot add cards; at most two cards are ever selected for one round.
  public int HomeDefenseMask;
  // Recovery bits: 1 the camera was re-centred once this round, 2 the ground tap was repeated once this round,
  // 4 (kept across rounds) the last new press of this policy was the home focus control,
  // 8 Attack was toggled off once for the re-centre this round,
  // 16 (fast sweep) the squads already selected on this roster page were ordered first,
  // 32 the warning's nearest-defender control was pressed once this round.
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
                plan.HomeDefenseReason = view.BaseAlert ? ReasonBaseAlert : view.BaseDamageOnly ? ReasonBaseDamage : ReasonMinimap;
                plan.HomeDefenseSlot = 0; plan.HomeDefenseMask = 0;
                // One group answers a raid. The whole roster only returns when
                // the base is about to fall or a first defender did not stop it.
                plan.HomeDefenseScope = view.ProportionateDefense && !view.BaseCritical && plan.HomeDefenseRounds <= 1 ? 1 : 0;
                plan.DefenseStage = 4;
                plan.HomeDefenseRecovery &= RecoveryLastFocus;
                plan.HomeDefenseStage = 1;
                // The defence ground is already on screen: a Home tap would only cost ~2 s of a raid.
                // It stays allowed as the one re-centre if the ground is lost later in this round.
                if (FastSweep(view) && DefenseTarget(view, plan.HomeDefenseRounds).Available)
                    plan.HomeDefenseAction = view.CompletedContacts - 1;
                else
                {
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
            }
            if (plan.HomeDefenseStage == 1)
            {
                if (view.CompletedContacts <= plan.HomeDefenseAction)
                { Target(view.FocusPlayer, false, ref output); return true; }
                if (FastSweep(view) && plan.HomeDefenseScope == 1)
                {
                    // Nearest defender: the card the warning highlights. It is on this page, or the
                    // warning's own control pages the roster to it (one press per round). Without a
                    // warning, a page that already shows an anti-armour card is used as it is.
                    bool paged = (plan.HomeDefenseRecovery & RecoveryNearestPaged) != 0;
                    if (view.HighlightedDefender > 0 ||
                        !view.DefenderKnown && (view.ExpandedAssaultMask & view.AvailableSquadMask & view.AntiArmorMask) != 0)
                    { plan.HomeDefensePage = view.ExpandedPageIndex; plan.HomeDefenseStage = 2; }
                    else if (view.DefenderKnown && view.DefenderPageAvailable &&
                        (!paged || view.CompletedContacts <= plan.HomeDefenseRecoveryAction))
                    {
                        if (!paged) { plan.HomeDefenseRecovery |= RecoveryNearestPaged; plan.HomeDefenseRecoveryAction = view.CompletedContacts; }
                        Target(view.DefenderPage, false, ref output); return true;
                    }
                }
            }
            if (plan.HomeDefenseStage == 1)
            {
                if (view.ExpandedPageIndex > 0)
                { Target(view.PreviousSquadPage, false, ref output); return true; }
                plan.HomeDefensePage = 0;
                plan.HomeDefenseStage = 2;
            }
            if (plan.HomeDefenseStage == 2)
            {
                // An all-group sweep orders every assault squad to the same target: keep a selection
                // (here or on an earlier page) instead of spending a tap per card to clear it.
                bool keep = FastSweep(view) && (plan.HomeDefenseScope == 0
                    ? (view.ExpandedSelectedMask & ~view.ExpandedAssaultMask) == 0
                    // A single defence keeps a selection that is exactly part of its defenders on this page.
                    : (view.ExpandedSelectedMask & ~DefenderMask(view)) == 0 && view.ExpandedSelectedGroups == Bits(view.ExpandedSelectedMask));
                if (!keep && TryDeselectSquads(view, ref output)) return true;
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
                    int highlighted = HighlightedDefenderBit(view);
                    if ((plan.HomeDefenseMask & ~view.ExpandedAssaultMask) != 0) plan.HomeDefenseMask = 0;
                    if (plan.HomeDefenseMask == 0)
                    {
                        // SHOW can publish the new page before its cards are reachable by touch.
                        int selectable = view.AvailableSquadMask | view.ExpandedSelectedMask;
                        if ((view.ExpandedAssaultMask & selectable) == 0 || (highlighted & ~selectable) != 0)
                        { output.Action = AriaHomeDefenseAction.Waiting; return true; }
                        plan.HomeDefenseMask = DefenderMask(view);
                    }
                    plan.HomeDefenseSlot = LowestSetBit(plan.HomeDefenseMask) + 1;
                    int pending = plan.HomeDefenseMask & ~view.ExpandedSelectedMask;
                    if (pending != 0)
                    {
                        int next = pending & highlighted;
                        if (next == 0) next = LowestBit(pending & view.AvailableSquadMask);
                        if ((next & view.AvailableSquadMask) == 0) output.Action = AriaHomeDefenseAction.Waiting;
                        else Target(view.Squad(LowestSetBit(next)), false, ref output);
                        return true;
                    }
                    plan.HomeDefenseStage = 4;
                }
            }
            if (plan.HomeDefenseStage == 3 && FastSweep(view) && (plan.HomeDefenseRecovery & RecoveryPageOrdered) == 0 &&
                view.ExpandedAssaultMask != 0)
            {
                // Order first, then add: the squads already selected (or one anti-armour card) get the
                // order now; the rest of the page is selected and ordered right after.
                if ((view.ExpandedSelectedMask & view.ExpandedAssaultMask) == 0)
                { Target(view.Squad(PreferredDefender(view)), false, ref output); return true; }
                plan.HomeDefenseRecovery |= RecoveryPageOrdered;
                plan.HomeDefenseStage = 4;
            }
            if (plan.HomeDefenseStage == 3)
            {
                int pending = view.ExpandedAssaultMask & ~view.ExpandedSelectedMask;
                if (FastSweep(view) && (pending & view.AvailableSquadMask) != 0) pending &= view.AvailableSquadMask;
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
                if (!DefenseTarget(view, plan.HomeDefenseRounds).Available && view.Time < plan.HomeDefenseObserveUntil)
                { output.Action = AriaHomeDefenseAction.Waiting; return true; }
                plan.HomeDefenseStage = 4;
            }
            // Selection can disappear while Attack is being aimed or after a camera re-centre.
            if (view.ExpandedSelectedMask == 0 &&
                (plan.HomeDefenseStage == 4 || plan.HomeDefenseStage == 5 &&
                    (view.CompletedContacts <= plan.HomeDefenseAction || view.AttackMode)))
            {
                plan.HomeDefenseStage = 2;
                output.Action = AriaHomeDefenseAction.Waiting;
                return true;
            }
            if (plan.HomeDefenseStage == 4)
            {
                var threat = DefenseTarget(view, plan.HomeDefenseRounds);
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
                            if ((plan.HomeDefenseRecovery & RecoveryDisarmed) == 0 && view.Attack.Available)
                            {
                                plan.HomeDefenseRecovery |= RecoveryDisarmed;
                                plan.HomeDefenseRecoveryAction = view.CompletedContacts;
                                Target(view.Attack, false, ref output); return true;
                            }
                            if (view.CompletedContacts <= plan.HomeDefenseRecoveryAction && view.Attack.Available)
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
                { Target(DefenseTarget(view, plan.HomeDefenseRounds), true, ref output); return true; }
                if (view.AttackMode)
                {
                    // The ground tap completed but Attack stayed armed: repeat it once, then
                    // give the round up rather than pressing the same ground a third time.
                    var again = DefenseTarget(view, plan.HomeDefenseRounds);
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
                if (view.AttackMode && view.Attack.Available && view.CompletedContacts <= plan.HomeDefenseRecoveryAction &&
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
            if (plan.HomeDefenseStage == 6 && FastSweep(view) &&
                (view.ExpandedAssaultMask & ~view.ExpandedSelectedMask & view.AvailableSquadMask) != 0)
            {
                // The first order on this page went out; now select and order the rest of the page.
                plan.HomeDefenseStage = 3;
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
                    plan.HomeDefenseRecovery &= ~RecoveryPageOrdered;
                    plan.HomeDefenseStage = 2;
                }
                return true;
            }
            // A falling base escalates a single defender to the full roster now.
            if (view.Time >= plan.HomeDefenseObserveUntil || plan.HomeDefenseScope == 1 && view.BaseCritical) plan.HomeDefenseStage = 0;
            return true;
        }
 public const int ReasonNone=0,ReasonMinimap=1,ReasonBaseDamage=2,ReasonBaseAlert=3;
 public const int StageDisarm=9,StageRecentre=10,RecoveryRecentred=1,RecoveryRetapped=2,RecoveryLastFocus=4,RecoveryDisarmed=8,RecoveryPageOrdered=16,RecoveryNearestPaged=32;
 public const float SingleDefenderObserveSeconds=20f,RetrySeconds=4f,DisarmSeconds=3f,RecentreSeconds=4f;
 // A presented contact is fought where it is; base damage alone sends defenders onto the home ground.
 // When the preferred ground is not offered, any other offered defence target is used.
 public static AriaStrategyTarget DefenseTarget(in AriaHomeDefenseView view)=>DefenseTarget(view,1);
 // Base damage with a presented attacker near home: fight that attacker (the unit, else the ground beside it).
 // Without one, the column-side home ground first; from the second round the far side of home is tried first,
 // since the column-side ground already failed to stop the damage.
 public static AriaStrategyTarget DefenseTarget(in AriaHomeDefenseView view,int round){
  if(view.BaseDamageOnly){
   if(view.Attacker.Available)return view.Attacker;
   if(view.AttackerGround.Available)return view.AttackerGround;
   if(round>=2&&view.HomeGroundFar.Available)return view.HomeGroundFar;
   return view.HomeGround.Available?view.HomeGround:view.HomeGroundFar.Available?view.HomeGroundFar:view.ThreatGround.Available?view.ThreatGround:view.Threat.Available?view.Threat:view.HomeGround;
  }
  return view.ThreatGround.Available?view.ThreatGround:view.Threat.Available?view.Threat:view.HomeGround;
 }
 // Fast sweep is the S002 proportionate defence (the generic planner keeps its original sweep).
 static bool FastSweep(in AriaHomeDefenseView view)=>view.ProportionateDefense;
 // One card for an order-first sweep: the nearest highlighted card, else anti-armour, else the first assault card.
 static int PreferredDefender(in AriaHomeDefenseView view)=>LowestSetBit(DefenderMask(view));
 // The cards a single defence selects on this page (at most two): the card the warning highlights as the
 // nearest defender, plus one anti-armour card when the nearest one does not fight armour (a tank shelling
 // the HQ); without a highlight, one anti-armour card, else the first offered assault card.
 public static int DefenderMask(in AriaHomeDefenseView view){
  int assault=view.ExpandedAssaultMask,offered=assault&(view.AvailableSquadMask|view.ExpandedSelectedMask),armour=offered&view.AntiArmorMask;
  int nearest=offered&HighlightedDefenderBit(view);
  if(nearest!=0)return (nearest&armour)!=0||armour==0?nearest:nearest|LowestBit(armour);
  return LowestBit(armour!=0?armour:offered!=0?offered:assault);}
 static int HighlightedDefenderBit(in AriaHomeDefenseView view){
  int slot=view.HighlightedDefender-1;return slot>=0&&slot<5?view.ExpandedAssaultMask&(1<<slot):0;}
 static int LowestBit(int mask)=>mask&-mask;
 static int Bits(int mask){int n=0;while(mask!=0){mask&=mask-1;n++;}return n;}
 // No defence target after the one re-centre (or a ground tap that left Attack armed twice):
 // toggle Attack off, observe briefly, then a new round starts from the home focus. Different
 // controls alternate, so no control is pressed three times in a row and ARIA keeps playing.
 static bool GiveUpRound(in AriaHomeDefenseView view,ref AriaHomeDefenseState plan,ref AriaHomeDefenseDecision output){
  plan.HomeDefenseRecoveryAction=view.CompletedContacts;
  plan.HomeDefenseObserveUntil=view.Time+DisarmSeconds;
  plan.HomeDefenseStage=StageDisarm;
  if(view.AttackMode&&view.Attack.Available&&(plan.HomeDefenseRecovery&RecoveryDisarmed)==0)
  {plan.HomeDefenseRecovery|=RecoveryDisarmed;Target(view.Attack,false,ref output);return true;}
  plan.HomeDefenseObserveUntil=view.Time+RetrySeconds;plan.HomeDefenseStage=8;output.Action=AriaHomeDefenseAction.Waiting;return true;
 }
 static bool TryDeselectSquads(in AriaHomeDefenseView view,ref AriaHomeDefenseDecision output){if(view.ExpandedSelectedGroups<=0)return false;if(view.SelectedSquadToToggle.Available)Target(view.SelectedSquadToToggle,false,ref output);else output.Action=AriaHomeDefenseAction.Waiting;return true;}
 static int LowestSetBit(int mask){if(mask==0)return 0;int bit=0,value=mask;while((value&1)==0){value>>=1;bit++;}return bit;}
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
  // Ranged raid search radius around home (covers a tank's weapon reach of the HQ; config attackRange 120).
  public const float RangedRadiusSquared=130f*130f;
  // Sight: 1 fog-visible marker, 2 last-seen ghost. A visible unit beats a ghost; then the nearer to home.
  // bestDistance starts at RangedRadiusSquared, so nothing farther is ever chosen.
  public static bool PreferAttacker(int sight,float squaredDistance,int bestSight,float bestDistance){
   if(sight<1||sight>2||!(squaredDistance<=RangedRadiusSquared))return false;
   if(bestSight==0)return squaredDistance<=bestDistance;
   if(sight!=bestSight)return sight<bestSight;
   return squaredDistance<bestDistance;
  }
 }

}
