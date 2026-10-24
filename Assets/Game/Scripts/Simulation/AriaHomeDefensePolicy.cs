using System;
namespace Game.Simulation {
 public enum AriaHomeDefenseAction:byte {None,Waiting,Unavailable,CancelPlacement,CloseDrawer,CloseMap,InspectHome,PreviousPage,NextPage,ToggleSelected,SelectSlot,ArmAttack,AttackThreatTerrain,AttackThreatVisible}
 public readonly struct AriaStrategyTarget {public readonly bool Available;public readonly AriaHomeDefenseAction Action;public readonly int Slot;public AriaStrategyTarget(bool available,AriaHomeDefenseAction action,int slot=-1){Available=available;Action=action;Slot=slot;}}
 public struct AriaHomeDefenseView {
  public bool PresentedHomeThreat,PlacementOpen,DrawerOpen,MapOpen,AttackMode;
  public bool CancelAvailable,CloseDrawerAvailable,CloseMapAvailable,FocusAvailable,PreviousAvailable,NextAvailable,ToggleAvailable,AttackAvailable,ThreatGroundAvailable,ThreatVisibleAvailable;
  public int ExpandedPageIndex,ExpandedSelectedGroups,ExpandedAssaultMask,ExpandedSelectedMask,AvailableSquadMask,CompletedContacts;public float Time;
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
  public AriaStrategyTarget Squad(int slot)=>new AriaStrategyTarget(slot>=0&&slot<5&&(AvailableSquadMask&(1<<slot))!=0,AriaHomeDefenseAction.SelectSlot,slot);
 }
 public struct AriaHomeDefenseState {public int HomeDefenseStage,HomeDefenseAction,HomeDefensePage,DefenseStage;public float HomeDefenseObserveUntil;public AriaGroundColumnState Column;}
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
                plan.DefenseStage = 4;
                plan.HomeDefenseAction = view.CompletedContacts;
                plan.HomeDefenseStage = 1;
                Target(view.FocusPlayer, false, ref output);
                return true;
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
                var threat = view.ThreatGround.Available ? view.ThreatGround : view.Threat;
                if (!threat.Available) { Target(view.FocusPlayer, false, ref output); return true; }
                plan.HomeDefenseAction = view.CompletedContacts;
                plan.HomeDefenseStage = 5;
                Target(threat, true, ref output);
                return true;
            }
            if (plan.HomeDefenseStage == 5)
            {
                if (view.CompletedContacts <= plan.HomeDefenseAction || view.AttackMode)
                { Target(view.ThreatGround.Available ? view.ThreatGround : view.Threat, true, ref output); return true; }
                plan.HomeDefenseStage = 6;
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
            if (view.Time >= plan.HomeDefenseObserveUntil) plan.HomeDefenseStage = 0;
            return true;
        }
 static bool TryDeselectSquads(in AriaHomeDefenseView view,ref AriaHomeDefenseDecision output){if(view.ExpandedSelectedGroups<=0)return false;if(view.SelectedSquadToToggle.Available)Target(view.SelectedSquadToToggle,false,ref output);else output.Action=AriaHomeDefenseAction.Waiting;return true;}
 static int LowestSetBit(int mask){int bit=0,value=mask;while((value&1)==0){value>>=1;bit++;}return bit;}
 static void Target(AriaStrategyTarget target,bool world,ref AriaHomeDefenseDecision output){if(!target.Available){output.Action=AriaHomeDefenseAction.Unavailable;return;}output.Action=target.Action;output.Slot=target.Slot;}
 }
 public static class AriaPublicHomeThreatPolicy {
  public const float RadiusSquared=120f*120f;
  public static bool Consider(AriaGroundPosition eligiblePublicHostile,AriaGroundPosition publicHome,ref float closestSquared){float distance=AriaGroundApproachPolicy.SquaredDistance(eligiblePublicHostile,publicHome);if(distance<closestSquared){closestSquared=distance;return true;}return false;}
 }

}
