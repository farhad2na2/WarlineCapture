# Lost and regrouped squad cards: plan (not implemented)

HUD worker, 2026-11-18. The owner's playtest asks that lost or regrouped squad cards keep a stable visual reference. The program lead allowed this only if it could be done without risk to input routing. It cannot be done safely in this pass, so this note records the plan.

## Why it is not safe as a drawer-only change

- A group with zero members alive drops out of the roster projection that feeds the tray (`UiMatchHudSquadTrayCardModel` rows behind `UiShellRuntimeGateway.TryReadMatchHudSquadTray`).
- Input addresses cards by **roster index** in two places:
  - `MatchHudSquadTrayView.TryGetExpandedSlotTarget(logicalSlot)` computes `index = _logicalPage * 5 + logicalSlot` (`MatchHudSquadTrayView.Pagination.cs:490-503`). `MatchHudSquadTouchNavigation.cs:96` and ARIA's slot-based navigation call it.
  - The gateway page model `UiExpandedSquadPage.Group0..4` and the page change use the same five-per-page windows.
- A placeholder row in the projection would shift every later index. It would also shift the ARIA slot, page and swipe geometry (`TryGetExpandedCardTarget` swipe alignment) in the middle of a match.

## Plan

1. **Stable order key.** Give each roster row a `RosterOrdinal` taken from the group's creation order, which `SkirmishArmyGroupRecord` keeps in buffer order. Keep a lost group's row with `Lost = true`, `AliveCount = 0`, not interactable and not selectable, for the rest of the match. Its ordinal stays where it was, so later cards never move up into its place.
2. **Route input by group id only.**
   - Remove `TryGetExpandedSlotTarget(logicalSlot)`, or make it resolve `slot → groupId` through the page model and then call `TryGetExpandedCardTarget(groupId)`.
   - `UiExpandedSquadPage.GroupN` already carries group ids.
   - Selection (`_selectGroup`) already uses group id.
3. **Visual.** A lost card keeps its portrait in the shared grayscale "mission disabled" treatment (`ApplyMissionDisabledTreatment`) and shows "LOST" in place of the alive count. A regrouped card shows the new group's label on the same ordinal.
4. **ARIA.** The S002 and watch planners skip `Lost` rows when choosing cards. Their page arithmetic must use the gateway's page model rather than `_rosterModels.Count`. The ARIA owner must sign off on this before it lands.
5. **Tests (EditMode, pure).**
   - Ordinal stability under loss, regroup and reinforcement.
   - Every tap path routes to the intended group id after a loss in an earlier slot.
   - No lost card is ever returned as a tap target.

**Owner of the change:** the HUD worker, together with the ARIA worker for step 4. It should land in one commit, with a normal-input S026 replay that loses a squad in page 1, slot 2 and then taps a later card.
