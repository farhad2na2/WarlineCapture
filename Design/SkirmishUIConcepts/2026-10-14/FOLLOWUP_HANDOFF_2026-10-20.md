# Follow-up handoff: Skirmish battle library redesign (SCN13), 2026-10-20

This continues issue [#69](https://github.com/farhad2na2/WarlineCapture/issues/69) and replaces it as the working brief.

- Original design brief: `Design/SkirmishUIConcepts/2026-10-14/IMPLEMENTATION_HANDOFF.md` (commit `3ebb2a6f4`).
- Approved mockups: `approved-1-theater-map.jpg`, `approved-2-battlefield-map-view.jpg` and `approved-3-battlefield-grid-view.jpg` in the same folder.

## Design change: saga map replaces the grids (owner-approved 2026-10-20 08:40)

Read `Design/SkirmishUIConcepts/2026-10-20-saga-map/README.md` first.

- **Battlefield** (`skirmish-saga-battlefield-v02.jpg`): a winding road with 24 mission stops and objective chapter flags replaces the approved-3 Grid view, the MAP/GRID toggle and the six pinned map cards.
- **Theatre** (`skirmish-saga-theatre-v01.jpg`): a dotted route links the five battlefields, and short medal trails replace the 4×6 medal grid in the right panel.
- **No locking by order:** every owned mission stays playable.

Consequences for the plan below:

- P1's Grid work (#68 Grid view, item 6 grid row headers) and the Map-view card work (item 9, map card title strip) are **superseded by the saga path**. Reuse their data plumbing (catalog projection, medal flags, access state, launch), not their layouts.
- The P2 Theatre work (territories, cards, zoom pill, items 1–5) **still applies**. Add the dotted inter-battlefield route and the right-panel medal trails.
- Ask the owner before deleting superseded code; it may be removed in the same PR that adds the saga path.

## Verdict on the paused work (reviewed 2026-10-20 08:10–08:40 CEST)

**Continue from the existing branches. Do not restart.** The work is real and broadly matches the brief, but it is **not merge-ready**, and its **visual quality is currently unverified**.

### Verified independently

- **The seat harness reproduces the reported state exactly.**
  - Command: `Tools/TheatreSeatCheck` on `origin/paused/p1-v50-hooks` (`20fd01b9c`).
  - Result: `TheatreSeatCheck result=Failed count=8 seated=97 blocked=0`.
  - The 8 failures are the same ones issue #69 lists as open (see "P2 area" below).
- **WP1, the medal ledger, is implemented correctly.**
  - `SkirmishMedalSaveData[]` sits under `PlayerProfileSaveData`, with save migration v1→v2 and an empty array for old profiles.
  - Settlement acknowledges a result only after the medal is persisted. Because `SkirmishExpandedResultJournal.TrySettle` is idempotent per session, a failed save retries once with no double award.
  - Custom and legacy runs are rejected.
  - Tests exist in `Assets/Tests/Editor/SkirmishLibrary/` (ledger, view model, widgets, journey).
- **The status reporting in #69 was accurate.** I found no overclaiming.

### Not verified, and why

- **There is no visual evidence.**
  - Every native capture (v93–v98 and v44–v48) was deleted from the Grok Bot box.
  - The PRs contain no screenshots.
  - The local P1 worktrees (`WarlineCapture-wt-p1-scn13`, `-wt-p1-pr65b`) are gone; git marks them prunable.

  **Nobody can currently confirm "mockup quality".** Regenerating captures is the first job.
- **Nothing is compiled together.** P1's `-wip` items 6–10 compiled alone (v50b), but there has been no passing combined run.

### Problems found in this review (not in #69)

1. **Conflicts with `main`.** The combined tip does not merge cleanly into current `origin/main` (`b4405e72a`). Six files conflict, because main kept fixing the old list screen overnight (`82a636bc7` deep-row scrolling, `6b17ef65a` temporary catalog):
   - `.gitignore`
   - `SCN13_SkirmishSetupContent.prefab`
   - `SkirmishLaunchProjection.cs`
   - `MenuUiApprovedAuthoring.cs`
   - `SkirmishSetupPrefabBuilder.cs`
   - `QuickCustomScreenView.Scenarios.cs`

   Port main's intent into the new library; do not resolve by blindly taking "ours". Never hand-merge the prefab: regenerate it with the prefab builder.
2. **The PR base is stale.** #66 targets `feature/skirmish-library-p1` (`d57909a26`, 17 Oct), which is 20 commits ahead of main and 49 behind. Decide whether #66 retargets to `main` before merging.
3. **Batch-mode conflict.** Issue #69 says `scn13all` runs Unity "batch mode" on the Mac, which `AGENTS.md` forbids on macOS. The library's own README (`Assets/Game/Scripts/Editor/SkirmishLibrary/README.md`) says never pass `-batchmode`, and the capture code quits through `WARLINE_PLAY_CAPTURE_QUIT=1`. **Verify what `scn13all` actually passes.** If it uses `-batchmode`, it must change to `Tools/CI/invoke_unity_macos.sh` before any further runs.
4. **Maintainability.** `Assets/Game/Scripts/UI/Skirmish/TheatreSeatGeometry.cs` is a single 21,843-line file. It is also compiled into `Tools/TheatreSeatCheck`, so splitting it is safe and testable. Don't block the merge on it, but don't grow it further either; put new logic in new partial files.
5. **Unrelated scope.** The branch carries CRLF-tolerance one-liners in about 18 `Tools/CI` and `Tools/Operations` scripts (harmless, and they match a known `ProjectVersion.txt` CRLF problem), plus a deleted `mono_crash.*.json`. Keep them, but mention them in the PR description.
6. **Only 6 card previews ship** (`skirmish.preview.S001–S004, S025, S073`). Confirm this equals the set of `Playable` catalog entries. WP3 requires one real preview per published scenario, and Planned entries must use the plate crop, not fake captures.

## Branch map (all pushed)

| What | Branch | Tip |
| --- | --- | --- |
| PR [#66](https://github.com/farhad2na2/WarlineCapture/pull/66) (P2): Theatre map, territories, cards, header (WP3/WP4) | `cursor/skirmish-battle-library-wp34-77df` | `c85692429` |
| PR [#68](https://github.com/farhad2na2/WarlineCapture/pull/68) (P1): Map/Grid widgets, right panel, footer (WP1/WP2/WP5), stacked on #66 | `feature/skirmish-library-mapgrid-p1` | `7efd481f5` |
| P1 work in progress, uncompiled in a combined run (items 6–10 below) | `feature/skirmish-library-mapgrid-p1-wip` | `61f60aa18` (9 commits ahead of #68) |
| **Start here:** combined state (-wip + #66 + the two P2 hook lines from `731a722bc`) | `paused/p1-v50-hooks` | `20fd01b9c` |
| P2's local combined merges (reference only) | `p2-local-merges-paused`, `p2-local-merges-paused-p2-v9x-combined` | `237060e1a` |

## Ordered plan

1. **Get a checkout.** T7 has only about 32 GB free. Prune the dead worktrees with `git worktree prune`. Reuse one existing worktree rather than adding a third full checkout, and keep Hub open.
2. **Fix the validation path** (problem 3 above). Every Unity run on macOS goes through:

   ```bash
   Tools/CI/invoke_unity_macos.sh --unity /Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity \
     --timeout 3600 --log <abs-log> -- -quit -executeMethod <Method>
   ```

   Pass `--unity` because `ProjectVersion.txt` has CRLF line endings and the wrapper's version lookup exits 127 otherwise.
3. **Merge current `main` into `paused/p1-v50-hooks`.** Resolve the 6 conflicts by porting main's fixes, then regenerate the SCN13 prefab (`[SkirmishSetupPrefabBuilder] result=Passed`).
4. **Run a combined compile, EditMode tests and capture run**, and **commit the captures to the repo** under `Design/AgentReports/SkirmishBattleLibrary/<date>/captures/` so they cannot be lost again:
   - all three states (Theatre, Map, Grid);
   - ratios 16:9, 20:9, 4:3 and 20:9-notch;
   - EN and FA.
5. **Build side-by-side sheets** of each capture against approved-1/2/3 and show them to the owner before further polish. The owner decides "mockup quality"; agents only report.
6. **Fix the P2 area** until `TheatreSeatCheck` prints `result=Passed` (or the owner changes a gate):
   1. **Territory coverage.** The target is 0.65–0.70 of the map. Now it's about 0.50 at 4:3, 0.41–0.43 at 16:9 and 0.32–0.35 at 20:9. Do not loosen the 12 px halo or the 10% aspect gate without an owner decision. If the gate is the limit, report coverage at 15% and 20%.
   2. **Rectangle-shaped territories.** The harness flags AP (20:9 notch FA, reserve on) and CC/MP (20:9 safe EN/FA, reserve on). The 4:3 Airfield Plains shape still reads as a rectangle but the validator misses it, so tighten the validator as well.
   3. **Card centring, FA 4:3.** DB dx 56.7 (safe), MP dx 68.6 (notch) and CC dx -32.8 (section), against a 20 px tolerance. Industrial Basin also sits about 40 px low at 16:9 and 20:9 FA.
   4. **Zoom pill.** It overlaps DB at 20:9 safe FA, is missing at 4:3 EN, and sits 200–300 px from IB in FA.
   5. Fix the DB and CC outlines and the 12 px IB/AP gap.
7. **Prove the P1 area in the combined run.** The pass markers are in each commit message:
   - item 6: grid row headers, two-line names at 14–16 px with a status badge (`f2dd55906`);
   - item 7: briefing tiles Time, Map size and Army, plus the subtitle (`1f59746d4`);
   - item 8: breadcrumb and CREDITS chip with the real wallet, using loc keys `ui.skirmish.library.crumb.theatre` and `ui.common.credits` (`f8445bfef`; the FA wording needs Game Design sign-off);
   - item 9: map card title strip (v50b–v50d);
   - item 10: FA caption floor at 22 pt.
8. **Run the gates the brief requires that nobody has run yet:**
   - **Normal-input journey:** Main Menu → Skirmish → Theatre → Zoom In → objective tab → card → Start Battle → finish with ARIA Play → the result shows the medal → return lands on the same battlefield and card → the medal shows on the card, grid and theatre totals → restart → the medal persists.
   - **Access:** on a fresh profile with only S001 owned, every other entry shows a NotOwned lock and cannot launch from the UI or through `SkirmishExpandedLaunchResolver`.
   - **S002 ARIA menu probe** (`SkirmishS002AriaRunHarness.MenuProbe.cs`) and `SkirmishSetupScreenTests` still pass on the new navigation.
9. **Merge**, only after a combined run with 0 capture and flow violations at all 4 ratios × EN/FA and the owner's visual sign-off: #66, then #68 onto it, then into `main`.

## Rules that bind this work

- **Medals never gate content.** Locks come only from `ContentAccessRuntime.Evaluate("skirmish.sNNN")` and catalog `Status`. The locked tile that plays `CardLocked` must be NotOwned or unavailable, never "missing medal". Check this in review.
- Release builds advertise only `Playable` entries. The launch resolver stays authoritative.
- All text is native localised TMP (EN/FA, RTL-safe), with none baked into art. Reuse the SCN-05 typography, frames and command colours.
- Campaign map reuse policy: plates and previews come from existing Campaign sources; no new terrain.
- **Report separately:** visual review, automated checks, normal-input journey, access check, and device acceptance (pending until done on a real device). Keep failed evidence.

## Open owner decisions

- Whether to relax the territory gates (halo or aspect) if coverage cannot reach 0.65 under them.
- The FA wording for the breadcrumb and CREDITS keys (Game Design).
- From the original brief, still open:
  - medals across `contentVersion` (the code currently keeps them, audit-only);
  - whether NotOwned cards show the full briefing or a purchase panel only;
  - whether Planned battlefields are hidden in release builds.
- Whether #66 retargets from `feature/skirmish-library-p1` to `main`.
