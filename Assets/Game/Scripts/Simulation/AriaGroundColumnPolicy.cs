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
  public bool SquadAvailable(int slot)=>slot>=0&&slot<5&&(AvailableSquadMask&(1<<slot))!=0;
 }
 public struct AriaGroundColumnState
 {
  public int PagesOrdered,SelectionResetPage,AssaultPageSeen;
  public byte AssaultSelecting,StructureOrdered,PagedToAssault,AssaultIssued;
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
            if (plan.AssaultSelecting == 0 && view.ExpandedSelectedGroups > 0)
            {
                plan.SelectionResetPage = view.ExpandedPageIndex;
                plan.AssaultSelecting = 2;
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
            int pending = view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask & ~view.ExpandedSelectedMask;
            if (pending != 0)
            {
                int slot = LowestSetBit(pending);
                bool cardAvailable = view.SquadAvailable(slot);
                if (!cardAvailable)
                    return false;
                plan.AssaultSelecting = 1;
                output.Intent = AriaGroundIntent.SelectSquad;
                output.Action = AriaGroundAction.SelectSlot; output.Slot = slot;
                return true;
            }

            if ((view.ExpandedStructureMask & view.ExpandedAttackOrderMask) != 0)
                plan.StructureOrdered = 1;

            bool assaultOrdered = view.ExpandedAssaultMask == 0 ||
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
  static bool TryDeselectSquads(in AriaGroundColumnView view,ref AriaGroundDecision output)
  {if(view.ExpandedSelectedGroups<=0)return false;output.Action=view.ToggleAvailable?AriaGroundAction.ToggleSelected:AriaGroundAction.Wait;return true;}
  static int LowestSetBit(int mask){int bit=0,value=mask;while((value&1)==0){value>>=1;bit++;}return bit;}
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
