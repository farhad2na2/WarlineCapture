# Copy-ready implementation task

Implement the approved five-squad pagination design in WarlineCapture.

Read `Design/ImplementationPlans/SquadPagination/SPEC.md` completely and inspect `approved-direction.png` in that directory before editing. Follow the repository's AGENTS.md and applicable skill instructions. The user has already approved the first mockup: five squad selection cards with separate navigation. Do not ask for the same visual-direction approval again. Implement the specification; do not build an Army drawer or redesign unrelated mission UI.

Work sequentially through the specification's stages:

1. Shared five-group page projection and selection-neutral, non-wrapping Previous/Next actions.
2. UI contracts/gateways and five real cards, stable group identity, persistent multi-selection, explicit Clear and off-page selection summary.
3. Native builder/prefab layout and English/Persian localization, preserving Campaign/legacy behavior.
4. ARIA public Previous/Next/Clear targets, all five squad bits, acknowledged non-wrapping sweeps and explicit per-batch selection cleanup. Update the aircraft harness and existing tests.
5. Automated checks, native captures, full normal-input mission including ARIA/result/return, and evidence report.

Important traps already found in the source:

- `StandardPageSize` and `PresentedSlots` are currently 4.
- `TryPresentedSlot(4)` currently pages; it must become real fifth-group selection.
- `TryAdvancePage` currently clears selection and wraps. Both behaviors must change.
- The portrait resolver excludes slot4, and card5 gets a runtime NEXT arrow.
- ARIA and its tests use `Squad4` as NEXT and assume wraparound. Merely adding a new arrow without migrating them is incorrect.
- Paging while an order mode is active must consume pointer input without issuing a world order.
- The mockup's 12 squads and unit composition are examples, not mission data to hardcode.

The spec was audited at commit `2bea6e6f1349595f3fd80da03a0350005e8bb555`. Check current code before applying its file map; preserve unrelated work. Verify which actual visible scenario the user calls “Skirmish 5”; the audit did not establish a catalog ID. Do not create/publish a scenario or claim a different mission proves this one.

Use RTK for shell output. For Unity on macOS use the checked repository wrapper with explicit log and timeout, keep Hub open/signed in, and never add batchmode or bypass the wrapper. Read the unity-cli skill before connected Editor operations. Preserve failed validation evidence. Do not terminate an active Editor to run validation.

Complete the implementation and relevant validation without broad refactoring or new dependencies. Do not claim success from compilation, injected outcomes or older wins. Finish with changed-file summary, evidence links and separate statuses for visual direction, automated checks, normal-input playthroughs and real player/device acceptance. State any pending gate explicitly.
