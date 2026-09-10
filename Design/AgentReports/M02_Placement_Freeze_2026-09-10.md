# M2 placement freeze — 2026-09-10

## Reproduction and cause

The user's Editor log recorded a **68,475 ms frame gap** after the M2 barracks selection narration. The authorized Editor close preserved its log and a process sample in temporary storage. A new isolated-save Play-mode probe then reproduced the issue through the real Build → Barracks → Place buttons: the final button callback took **73,347 ms**.

Two defects combined:

1. The enlarged barracks model occupied approximately 15×28 cells, across the narrow side of the authored 40×20 tutorial lot. Runtime definition creation preferred model bounds and discarded the larger authored reservation. No legal initial position could be found.
2. The failed initial-position search expanded over the entire 2048×1024 map using nested square loops for every search radius, repeatedly examining interior and clamped edge cells, then attempted another full-grid fallback before returning to the UI.

## Changes

- Rotate the enlarged Barracks `Model` and its attached geometry along the lot's long axis through Unity's prefab API. Preserve its 2× scale. Runtime and catalog definitions now reserve the component-wise maximum of the authored footprint and visual bounds, preserving both gameplay space and physical clearance. The canonical Barracks resolves to 40×20 and its model fits inside that area.
- Search only the mission's legal origin rectangle. M2's exact-fit lot has one candidate. Keep the preview on its designated lot if temporarily occupied; the normal preview and confirmation validators still reject blocked construction.
- Replace the whole-map fallback with direct perimeter traversal within 32 cells, at most 4,225 unique candidates. Reject oversized footprints and empty mission bounds without scanning. Retain a draggable invalid preview when no nearby valid position exists.
- Keep mission origin selection in the existing mission placement policy, leaving the adapter below its ratcheted architecture ceiling. No budgets or baselines were weakened.
- Fix placement command test fixtures to use their own `EntityManager` instead of a missing global default world.

The first runtime check after bounding the search reduced the placement callback to **43.6 ms** and exposed the footprint mismatch immediately. The subsequent prefab repair verified the final 40×20 runtime footprint and containing model bounds.

## Editor replay

The final real-button replay passed in the user's Farsi locale with an isolated save:

- **Place Building: 46.8 ms**, with a valid 40×20 preview at (1006,330).
- **Confirm: 53.5 ms**.
- One barracks placed and construction completed.
- Rifle recruitment accepted and completed through the real production controls.
- Mission outcome: **Victory**.

The captured preview and recruited squad were visually inspected. The probe stops when the mission records Victory; this run does not claim a new end-screen or Android audit.

## Regression validation

The final focused EditMode matrix passed **299/299**, with zero failures or skipped tests (236.8 seconds). It covers placement search and validation, canonical runtime/catalog footprints, material costs, M2 placement/production/guidance/Do It/canonical data, and all test names matching `Architecture`. Earlier fixture and architecture failures were corrected without relaxing assertions or budgets.

Results: `/private/tmp/warline-m02-placement-tests-final-03.xml`; Editor log: `/private/tmp/warline-m02-placement-tests-final-03.log`.

M3's live Editor launch also passed using the corrected shared prefab: 20 defense members, seven hostiles, physical forward post bound, completed Barracks, RTS camera returned, simulation active, and the expected starting budget. The ready HUD capture was visually inspected. Log: `/private/tmp/warline-m03-barracks-regression.log`.

## Local evidence

- Preserved user log: `/private/tmp/warline-m2-placement-user-editor.log`.
- Reproduction: `/private/tmp/warline-m02-placement-baseline.log` and `/private/tmp/warline-m02-placement-baseline-audit.txt`.
- Search-only diagnostic: `/private/tmp/warline-m02-placement-search-fixed.log`.
- Final M2 replay: `/private/tmp/warline-m02-placement-final.log`.
- Prefab verification: `/private/tmp/warline-m02-barracks-asset-repair.log`.
- Play-mode screenshots and timing audit stay in `/private/tmp/warline-m02-placement`; no evidence images are added to the repository.
