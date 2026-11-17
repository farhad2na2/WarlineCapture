using System;
namespace Game.Simulation
{
 public enum AriaGroundIntent:byte{Keep,Inspect,SelectSquad,Attack}
 public enum AriaGroundAction:byte{None,Wait,Unavailable,PreviousPage,NextPage,ToggleSelected,SelectSlot,ArmAttack}
 public struct AriaGroundColumnView
 {
  public int ExpandedPageCount,ExpandedPageIndex,ExpandedSelectedGroups,ExpandedTotalGroups;
  public bool EnemyDesignatedAlive,ExpandedCanPreviousPage,ExpandedCanNextPage;
  public int ExpandedAssaultMask,ExpandedAttackOrderMask,ExpandedSelectedMask,ExpandedStructureMask,AvailableSquadMask;
  public bool PreviousAvailable,NextAvailable,ToggleAvailable,AttackAvailable;
  // Observation clock for the stall guard (0 in legacy callers disables it).
  public float Time;
  public bool SquadAvailable(int slot)=>slot>=0&&slot<5&&(AvailableSquadMask&(1<<slot))!=0;
 }
 public struct AriaGroundColumnState
 {
  public int PagesOrdered,SelectionResetPage,AssaultPageSeen;
  public byte AssaultSelecting,StructureOrdered,PagedToAssault,AssaultIssued;
  // Stall guard: the same unresolved column state (StallKey) since StallSince; Skips counts pages given up.
  public int StallKey,Skips;public float StallSince;
  // The last two pages given up (page*64 + unordered assault mask); they do not restart the sweep.
  public int SkipKeyA,SkipKeyB;
 }
 public struct AriaGroundDecision{public AriaGroundIntent Intent;public AriaGroundAction Action;public int Slot;}
 // One source-owned strategic subtree. Availability means admitted action in the host's
 // explicit transport profile; native rendering gates remain in the native adapter.
 public static class AriaGroundColumnPolicy
 {
  public static bool StepGroundAssault(in AriaGroundColumnView view,ref AriaGroundColumnState plan,out AriaGroundDecision decision)
  {
   decision=default;
   if(!view.EnemyDesignatedAlive||plan.AssaultIssued!=0)return false;
   if(view.ExpandedPageCount<=0&&view.ExpandedTotalGroups==0&&view.ExpandedAssaultMask==0&&view.ExpandedStructureMask==0&&!view.ExpandedCanPreviousPage&&!view.ExpandedCanNextPage)return false;
   if(plan.PagedToAssault==0&&view.ExpandedPageIndex>0){
    if(view.PreviousAvailable){decision.Intent=AriaGroundIntent.Inspect;decision.Action=AriaGroundAction.PreviousPage;}
    else decision.Action=AriaGroundAction.Wait;
    return true;
   }
   if(plan.PagedToAssault==0)plan.PagedToAssault=1;
   return TryStructureColumn(view,ref plan,ref decision);
  }
  public static bool Step(in AriaGroundColumnView view,ref AriaGroundColumnState plan,out AriaGroundDecision decision)
  {decision=default;return TryStructureColumn(view,ref plan,ref decision);}
  static bool TryStructureColumn(in AriaGroundColumnView view,ref AriaGroundColumnState plan,ref AriaGroundDecision output)
{
            // Clear a stale selection before returning to an earlier page (lane 12 S050: on page 0 the
            // deselect paged to the selected card on page 1, and page 1 paged straight back to page 0
            // because page 0 was not yet counted; the two rules alternated until the watchdog).
            if (plan.AssaultSelecting == 0 && view.ExpandedSelectedGroups > 0)
            {
                // A selection made only of this page's unordered assault groups is exactly what the
                // sweep would select next: keep it. Clearing and re-selecting the same card after every
                // roster change (new recruits arriving) pressed one card until Blocked 115 (lane 10
                // S050 and S006).
                if (SelectionIsPendingAssault(view)) plan.AssaultSelecting = 1;
                else
                {
                    plan.SelectionResetPage = view.ExpandedPageIndex;
                    plan.AssaultSelecting = 2;
                }
            }
            if (plan.AssaultSelecting == 2)
            {
                if (TryDeselectSquads(view, ref output)) return true;
                // Resume the intended assault window after normal card/swipe input.
                if (view.ExpandedPageIndex != plan.SelectionResetPage)
                {
                    output.Intent = AriaGroundIntent.Inspect;
                    output.Action = view.ExpandedPageIndex < plan.SelectionResetPage
                        ? (view.NextAvailable ? AriaGroundAction.NextPage : AriaGroundAction.Unavailable)
                        : (view.PreviousAvailable ? AriaGroundAction.PreviousPage : AriaGroundAction.Unavailable);
                    return true;
                }
                plan.AssaultSelecting = 0;
            }
            // PagesOrdered is the contiguous prefix of publicly accepted pages.
            // A long/inertial swipe may skip a page; return to the missing page
            // before accepting later pages or declaring the roster sweep complete.
            if (plan.PagesOrdered < view.ExpandedPageCount &&
                view.ExpandedPageIndex > plan.PagesOrdered)
            {
                output.Intent = AriaGroundIntent.Inspect;
                if (view.PreviousAvailable)
                    output.Action = view.PreviousAvailable ? AriaGroundAction.PreviousPage : AriaGroundAction.Wait;
                else output.Action = AriaGroundAction.Wait;
                return true;
            }
            int pending = view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask & ~view.ExpandedSelectedMask;
            bool skipPage = false;
            if (pending != 0)
            {
                int slot = LowestSetBit(pending);
                bool cardAvailable = view.SquadAvailable(slot);
                // A card that is never offered (S002 155923: page 1 cards unresolved for 147 s)
                // must not hold the sweep: after CardStallSeconds the page is given up.
                if (!cardAvailable)
                {
                    if (!Stalled(view, ref plan, KeyCardUnavailable + view.ExpandedPageIndex * 64 + pending, CardStallSeconds))
                        return false;
                    skipPage = true;
                }
            }
            if (pending != 0 && !skipPage)
            {
                int slot = LowestSetBit(pending);
                plan.AssaultSelecting = 1;
                output.Intent = AriaGroundIntent.SelectSquad;
                output.Action = AriaGroundAction.SelectSlot; output.Slot = slot;
                return true;
            }

            if ((view.ExpandedStructureMask & view.ExpandedAttackOrderMask) != 0)
                plan.StructureOrdered = 1;

            bool assaultOrdered = skipPage || view.ExpandedAssaultMask == 0 ||
                (view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask) == 0;
            // Already accepted groups keep fighting while the remaining cohort is selected.
            // Selection above deliberately excludes their AttackOrder bits, so requiring them
            // selected here would prevent the next explicit Attack command from ever arming.
            int remainingAssault = view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask;
            bool assaultSelected = remainingAssault != 0 &&
                (remainingAssault & ~view.ExpandedSelectedMask) == 0;
            if (!assaultOrdered && assaultSelected)
            {
                // Keep pressing Attack until the drawer shows the order.
                // Latching on the request left the tank unselected when the tap missed.
                // Paging away while Attack is covered clears that selection.
                // Bounded: an order that never shows (S049: Attack / tap base alternating for
                // 170 s until the progress watchdog stopped ARIA) gives the page up.
                if (Stalled(view, ref plan, KeyOrderNotShown + view.ExpandedPageIndex * 64 + remainingAssault, OrderStallSeconds))
                    assaultOrdered = skipPage = true;
                else
                {
                    if (!view.AttackAvailable)
                    {
                        output.Intent = AriaGroundIntent.Inspect;
                        output.Action = AriaGroundAction.Wait;
                        return true;
                    }

                    output.Intent = AriaGroundIntent.Attack;
                    output.Action = AriaGroundAction.ArmAttack;
                    return true;
                }
            }
            if (!skipPage && plan.StallKey != 0) plan.StallKey = 0;
            if (skipPage)
            {
                // The page counts as inspected; the sweep may complete without its structure order.
                plan.Skips++; plan.StallKey = 0; plan.StructureOrdered = 1;
                plan.SkipKeyB = plan.SkipKeyA;
                plan.SkipKeyA = SkipKey(view.ExpandedPageIndex, view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask);
            }

            bool pageInspected = assaultOrdered;
            if (pageInspected)
            {
                if (view.ExpandedPageIndex == plan.PagesOrdered)
                    plan.PagesOrdered++;
                plan.AssaultPageSeen = view.ExpandedPageIndex + 1;
            }
            if (plan.PagedToAssault == 2)
            {
                if (view.ExpandedPageIndex > 0 && view.PreviousAvailable)
                { output.Intent = AriaGroundIntent.Inspect; output.Action = view.PreviousAvailable ? AriaGroundAction.PreviousPage : AriaGroundAction.Wait; return true; }
                if (view.ExpandedPageIndex == 0 && plan.StructureOrdered != 0 &&
                    plan.PagesOrdered >= (view.ExpandedPageCount > 0 ? view.ExpandedPageCount : 1))
                { plan.AssaultIssued = 1; return false; }
            }
            if (view.NextAvailable)
            {
                plan.PagedToAssault = 1;
                plan.AssaultSelecting = 0;
                output.Intent = AriaGroundIntent.SelectSquad;
                output.Action = view.NextAvailable ? AriaGroundAction.NextPage : AriaGroundAction.Wait;
                return true;
            }
            if (view.PreviousAvailable)
            {
                plan.PagedToAssault = 2;
                plan.AssaultSelecting = 0;
                output.Intent = AriaGroundIntent.Inspect;
                output.Action = view.PreviousAvailable ? AriaGroundAction.PreviousPage : AriaGroundAction.Wait;
                return true;
            }
            // A one-page roster completes in place. Multi-page sweeps complete only
            // after the observed Previous actions have returned the tray to page zero.
            if (view.ExpandedPageIndex == 0 && plan.StructureOrdered != 0 && plan.PagesOrdered >= view.ExpandedPageCount)
            { plan.AssaultIssued = 1; return false; }
            if (!pageInspected || plan.StructureOrdered == 0)
            {
                output.Intent = AriaGroundIntent.Inspect;
                output.Action = AriaGroundAction.Wait;
                return true;
            }
            return false;
        }
  public const int KeyCardUnavailable=1<<20,KeyOrderNotShown=2<<20;
  public static int SkipKey(int page,int unorderedAssault)=>unorderedAssault==0?0:(page+1)*64+unorderedAssault;
  // A page given up earlier (same unordered groups) does not reopen a completed sweep.
  public static bool Skipped(int skipKeyA,int skipKeyB,int page,int unorderedAssault){
   int key=SkipKey(page,unorderedAssault);return key!=0&&(key==skipKeyA||key==skipKeyB);
  }
  public const float CardStallSeconds=8f,OrderStallSeconds=20f;
  // True once the same unresolved column state has lasted `seconds` on the observation clock.
  static bool Stalled(in AriaGroundColumnView view,ref AriaGroundColumnState plan,int key,float seconds){
   if(view.Time<=0f)return false;
   if(plan.StallKey!=key){plan.StallKey=key;plan.StallSince=view.Time;return false;}
   return view.Time-plan.StallSince>=seconds;
  }
  public static bool SelectionIsPendingAssault(in AriaGroundColumnView view)
  {
   int pendingAssault=view.ExpandedAssaultMask&~view.ExpandedAttackOrderMask;
   int selected=view.ExpandedSelectedMask;
   if(selected==0||(selected&~pendingAssault)!=0)return false;
   int onPage=0;for(int bits=selected;bits!=0;bits&=bits-1)onPage++;
   return onPage==view.ExpandedSelectedGroups; // nothing selected off this page
  }
  static bool TryDeselectSquads(in AriaGroundColumnView view,ref AriaGroundDecision output)
  {if(view.ExpandedSelectedGroups<=0)return false;output.Action=view.ToggleAvailable?AriaGroundAction.ToggleSelected:AriaGroundAction.Wait;return true;}
  static int LowestSetBit(int mask){int bit=0,value=mask;while((value&1)==0){value>>=1;bit++;}return bit;}
 }
 // Attack armed with a selection but neither the enemy base nor its approach ground offered:
 // the planner searches (map navigation or the enemy focus control). S026 lane 6: this search
 // looped for 50+ s with Attack armed, so the column never latched and nothing else could act;
 // q1: the enemy focus pressed three times with Attack armed stopped ARIA (115). Bounded here.
 public struct AriaFindBaseState {public byte Active,Taps,Run;public int Actions,LastTarget;}
 public static class AriaFindBasePolicy
 {
  public const int MaxTaps=4,MaxSameControl=2;
  public static void Reset(ref AriaFindBaseState s){s=default;}
  // Called each step the search branch is taken. nextTarget: the control the branch would press
  // (0 for map navigation). continuing: the previous step was this search (or its map navigation);
  // touches made by other owners in between are not counted as search taps.
  // True: give the search up (disarm Attack, latch the column).
  public static bool GiveUp(ref AriaFindBaseState s,int completedActions,int nextTarget,bool continuing=true)
  {
   if(s.Active==0){s.Active=1;s.Actions=completedActions;s.Taps=0;s.Run=0;s.LastTarget=0;}
   else if(!continuing){s.Actions=completedActions;s.Run=0;}
   else if(completedActions>s.Actions)
   {
    s.Taps=(byte)Math.Min(255,s.Taps+completedActions-s.Actions);s.Actions=completedActions;
    s.Run=(byte)(nextTarget!=0&&nextTarget==s.LastTarget?s.Run+1:0);
   }
   if(nextTarget!=0)s.LastTarget=nextTarget;
   return s.Taps>=MaxTaps||s.Run>=MaxSameControl;
  }
  // The search gave up: the current page's unordered groups are remembered as skipped so the
  // completed sweep is not reopened, and the column counts as issued.
  public static void LatchGivenUp(ref int skipKeyA,ref int skipKeyB,ref int skips,int page,int unorderedAssault)
  {skipKeyB=skipKeyA;skipKeyA=AriaGroundColumnPolicy.SkipKey(page,unorderedAssault);skips++;}
 }
 // Closing the owned full map: Close presses that complete while the map stays open are counted.
 // After two, the popup's "center on HQ" control is pressed once, so the next Close is a fresh
 // press of a different control sequence; after MaxCloses nothing more is pressed (the caller's
 // inspection deadline decides). Lane 8 S026: Close pressed three times, Blocked 115.
 public enum AriaMapCloseAction:byte {Close,CenterHome,Wait}
 public static class AriaMapCloseRecovery
 {
  public const int ClosesBeforeCentre=2,MaxCloses=4;
  // centered: 0 unused, 1 pressed (in flight), 2 completed. lastActions: completed touches seen (0 = not started).
  public static AriaMapCloseAction Step(ref byte closes,ref byte centered,ref int lastActions,int completedActions,bool centerAvailable)
  {
   if(lastActions==0)lastActions=completedActions;
   else if(completedActions>lastActions)
   {
    if(centered==1)centered=2;else closes++;
    lastActions=completedActions;
   }
   if(centered==1)return AriaMapCloseAction.CenterHome;
   if(closes>=ClosesBeforeCentre&&centered==0&&centerAvailable){centered=1;return AriaMapCloseAction.CenterHome;}
   if(closes>=MaxCloses)return AriaMapCloseAction.Wait;
   return AriaMapCloseAction.Close;
  }
 }
 public struct AriaGroundProgressState{public float LastProgressAt;public float EnemyHealth,PlayerHealth;public int Infantry;}
 public static class AriaGroundProgressPolicy
 {
  public static bool Observe(float time,float enemyHealth,float playerHealth,int infantry,bool newMilestone,bool supplyProgress,bool assaultAdvance,ref AriaGroundProgressState plan,out bool blocked)
  {
   bool changed=plan.LastProgressAt==0||plan.EnemyHealth!=enemyHealth||plan.PlayerHealth!=playerHealth||plan.Infantry!=infantry||newMilestone||supplyProgress||assaultAdvance;
   if(changed){plan.LastProgressAt=time;plan.EnemyHealth=enemyHealth;plan.PlayerHealth=playerHealth;plan.Infantry=infantry;}
   blocked=time-plan.LastProgressAt>180f;
   return changed;
  }
 }
 public enum AriaObjectiveContact:byte{VisibleEnemyTarget,PublicTerrainApproach,InspectObjective}
 public static class AriaGroundObjectivePolicy
 {
  // Exact current native AttackMode/SelectionVisible preference order. Inspection has no command effect.
  public static AriaObjectiveContact Contact(bool visibleEnemyTarget,bool publicTerrainApproach)
   =>visibleEnemyTarget?AriaObjectiveContact.VisibleEnemyTarget:publicTerrainApproach?AriaObjectiveContact.PublicTerrainApproach:AriaObjectiveContact.InspectObjective;
 }
}
